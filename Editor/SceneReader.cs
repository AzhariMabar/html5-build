using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Html5Build.Editor
{
    // ─────────────────────────────────────────────────────────────────────────
    // Unity RectTransform → CSS calc() conversion
    //
    // CSS calc() pattern:  calc(anchor% + offset_px)
    //
    // Left:   calc( anchorMin.x*100 %  +  (anchoredPos.x - sizeDelta.x * pivot.x) px )
    // Top:    calc( (1-anchorMax.y)*100 %  +  (-anchoredPos.y - sizeDelta.y*(1-pivot.y)) px )
    //         ↑ Y flipped (Unity Y-up → CSS Y-down)
    // Width:  calc( (anchorMax.x-anchorMin.x)*100 %  +  sizeDelta.x px )
    // Height: calc( (anchorMax.y-anchorMin.y)*100 %  +  sizeDelta.y px )
    //
    // Rules:
    //  • Full stretch (anchorMin==0,anchorMax==1, sizeDelta==0) → left:0;top:0;width:100%;height:100%
    //  • Fixed size (anchorMin==anchorMax, sizeDelta==explicit size) → left:Npx; top:Npx; width:Wpx; height:Hpx
    //  • Mixed stretch → calc() with both % and px parts
    //
    // Scale & Rotation:
    //  • transform: scale(sx,sy) rotate(Zdeg)
    //  • CSS pixels are in LOCAL (unscaled) space; CSS transform applies visually on top
    //  • pivot → transform-origin
    //  • Rotation negated (Unity CCW = CSS CW, so −Unity = CSS)
    // ─────────────────────────────────────────────────────────────────────────
    public static class SceneReader
    {
        private static int _refW, _refH;

        public static CanvasModel ReadActiveScene()
        {
            Canvas.ForceUpdateCanvases();

            Canvas root = FindRootCanvas();
            if (root == null)
                throw new Exception("No root Canvas found in the active scene.");

            var scaler = root.GetComponent<CanvasScaler>();
            _refW = 1080; _refH = 1920;
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                _refW = Mathf.RoundToInt(scaler.referenceResolution.x);
                _refH = Mathf.RoundToInt(scaler.referenceResolution.y);
            }

            // Background color = first Image child of canvas
            Color bg = new Color(0.08f, 0.08f, 0.08f);
            foreach (Transform child in root.transform)
            {
                var img = child.GetComponent<Image>();
                if (img != null) { bg = img.color; break; }
            }

            var model = new CanvasModel
            {
                GameName        = Application.productName,
                ReferenceWidth  = _refW,
                ReferenceHeight = _refH,
                BackgroundColor = bg,
            };

            foreach (Transform child in root.transform)
                ReadElement(child, model.Children);

            return model;
        }

        // ─────────────────────────────────────────────────────────────────────
        private static void ReadElement(Transform t, List<UiElement> list)
        {
            if (!t.gameObject.activeSelf) return;
            var rt = t.GetComponent<RectTransform>();
            if (rt == null) return;

            var el = new UiElement
            {
                Name             = t.gameObject.name,
                Active           = true,
                AnchorMin        = rt.anchorMin,
                AnchorMax        = rt.anchorMax,
                AnchoredPosition = rt.anchoredPosition,
                SizeDelta        = rt.sizeDelta,
                Pivot            = rt.pivot,
                PivotForOrigin   = rt.pivot,
            };

            // ── CSS position/size via anchor calc() formulas ──────────────────
            // These formulas map Unity's RectTransform exactly to CSS,
            // and are RESPONSIVE because anchor % references the parent's size.
            el.CssLeft   = CalcLeft(rt);
            el.CssTop    = CalcTop(rt);
            el.CssWidth  = CalcWidth(rt);
            el.CssHeight = CalcHeight(rt);

            // ── Scale & Rotation ──────────────────────────────────────────────
            el.ScaleX = rt.localScale.x;
            el.ScaleY = rt.localScale.y;

            float rotZ = rt.localEulerAngles.z;
            if (rotZ > 180f) rotZ -= 360f;   // normalize to [-180, 180]
            el.Rotation = rotZ;               // stored as Unity value; CSS will negate

            // ── Component type ────────────────────────────────────────────────
            var imgComp    = t.GetComponent<Image>();
            var btnComp    = t.GetComponent<Button>();
            var tmpText    = t.GetComponent<TMP_Text>();
            var legacyText = t.GetComponent<Text>();

            if (btnComp    != null) el.Type = "button";
            else if (tmpText    != null || legacyText != null) el.Type = "text";
            else if (imgComp   != null) el.Type = "image";
            else                       el.Type = "container";

            // ── Visuals ───────────────────────────────────────────────────────
            if (imgComp != null)
            {
                el.Color      = imgComp.color;
                el.ImageType  = imgComp.type;
                el.FillMethod = imgComp.fillMethod;
                el.FillAmount = imgComp.fillAmount;
                el.FillOrigin = imgComp.fillOrigin;
                el.Clockwise  = imgComp.fillClockwise;

                if (imgComp.sprite != null)
                {
                    string path = AssetDatabase.GetAssetPath(imgComp.sprite);
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                    {
                        el.SpritePath   = path;
                        el.SpriteBorder = imgComp.sprite.border; // x=left, y=bottom, z=right, w=top
                    }
                }
            }

            // ── Text ──────────────────────────────────────────────────────────
            // fontSize is in TMP local units. CSS transform:scale() of parent
            // handles visual scaling automatically — no manual scale needed here.
            if (tmpText != null)
            {
                el.TextContent = tmpText.text;
                el.TextColor   = tmpText.color;
                el.FontSize    = tmpText.fontSize;
                ReadTmpAlign(tmpText.alignment, out el.TextAlignH, out el.TextAlignV);
            }
            else if (legacyText != null)
            {
                el.TextContent = legacyText.text;
                el.TextColor   = legacyText.color;
                el.FontSize    = legacyText.fontSize;
                el.TextAlignH  = LegacyAlignH(legacyText.alignment);
                el.TextAlignV  = LegacyAlignV(legacyText.alignment);
            }

            list.Add(el);

            // Recurse — no parent position tracking needed (calc % handles it)
            foreach (Transform child in t)
                ReadElement(child, el.Children);
        }

        // ─────────────────────────────────────────────────────────────────────
        // calc() formula builders
        // ─────────────────────────────────────────────────────────────────────

        // left = anchorMin.x * parentW  +  (anchoredPos.x - sizeDelta.x * pivot.x)
        private static string CalcLeft(RectTransform rt) =>
            Calc(rt.anchorMin.x * 100f,
                 rt.anchoredPosition.x - rt.sizeDelta.x * rt.pivot.x);

        // top (CSS Y-down) = (1 - anchorMax.y) * parentH  +  (-anchoredPos.y - sizeDelta.y*(1-pivot.y))
        // The (1-anchorMax.y) flips Unity Y-up → CSS Y-down
        private static string CalcTop(RectTransform rt) =>
            Calc((1f - rt.anchorMax.y) * 100f,
                 -rt.anchoredPosition.y - rt.sizeDelta.y * (1f - rt.pivot.y));

        // width = (anchorMax.x - anchorMin.x) * parentW  +  sizeDelta.x
        private static string CalcWidth(RectTransform rt) =>
            Calc((rt.anchorMax.x - rt.anchorMin.x) * 100f, rt.sizeDelta.x);

        // height = (anchorMax.y - anchorMin.y) * parentH  +  sizeDelta.y
        private static string CalcHeight(RectTransform rt) =>
            Calc((rt.anchorMax.y - rt.anchorMin.y) * 100f, rt.sizeDelta.y);

        // Produce a responsive CSS value.
        // % part = anchor fraction (auto-responsive via parent size)
        // px part = offset in reference-resolution pixels, scaled by --rs so it
        //           shrinks/grows with the viewport alongside the % anchor.
        private static string Calc(float pct, float px)
        {
            bool hasPct = Mathf.Abs(pct) > 0.01f;
            bool hasPx  = Mathf.Abs(px)  > 0.01f;

            if (!hasPct && !hasPx) return "0px";
            // Pure percentage — fully responsive, no --rs needed
            if (!hasPx)  return $"{pct:F1}%";
            // Pure pixel — scale with --rs
            if (!hasPct) return $"calc({px:F1}px * var(--rs))";
            // Mixed: anchor% + scaled offset
            string sign = px >= 0f ? "+" : "-";
            return $"calc({pct:F1}% {sign} {Mathf.Abs(px):F1}px * var(--rs))";
        }

        // ─────────────────────────────────────────────────────────────────────
        // TMP / Legacy Text alignment helpers
        // ─────────────────────────────────────────────────────────────────────
        private static void ReadTmpAlign(TextAlignmentOptions a, out string h, out string v)
        {
            int f = (int)a;
            h = (f & 0xFF) switch { 1 => "flex-start", 2 => "center", 4 => "flex-end", _ => "center" };
            v = ((f >> 8) & 0xFF) switch { 1 => "flex-start", 2 => "center", 4 => "flex-end", _ => "center" };
        }

        private static string LegacyAlignH(TextAnchor a) =>
            (a == TextAnchor.UpperLeft || a == TextAnchor.MiddleLeft || a == TextAnchor.LowerLeft) ? "flex-start" :
            (a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight) ? "flex-end" : "center";

        private static string LegacyAlignV(TextAnchor a) =>
            (a == TextAnchor.UpperLeft || a == TextAnchor.UpperCenter || a == TextAnchor.UpperRight) ? "flex-start" :
            (a == TextAnchor.LowerLeft || a == TextAnchor.LowerCenter || a == TextAnchor.LowerRight) ? "flex-end" : "center";

        private static Canvas FindRootCanvas()
        {
            foreach (var c in UnityEngine.Object.FindObjectsOfType<Canvas>())
                if (c.isRootCanvas) return c;
            return null;
        }
    }
}
