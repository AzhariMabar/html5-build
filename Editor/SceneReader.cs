using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Html5Build.Editor
{
    public static class SceneReader
    {
        // Shared scan state
        private static float _sfX, _sfY;       // world-pixels per canvas-local unit (X and Y)
        private static float _canvasLeft;       // canvas world BL corner X
        private static float _canvasBottom;     // canvas world BL corner Y
        private static int   _refW, _refH;      // canvas local size → used as HTML reference

        // ─────────────────────────────────────────────────────────────────────
        public static CanvasModel ReadActiveScene()
        {
            Canvas.ForceUpdateCanvases();

            Canvas root = FindRootCanvas();
            if (root == null)
                throw new Exception("No root Canvas found in the active scene.");

            // ── Canvas world extent ───────────────────────────────────────────
            var canvasRt = root.GetComponent<RectTransform>();
            var cc = new Vector3[4];
            canvasRt.GetWorldCorners(cc);
            // cc[0]=BL, cc[1]=TL, cc[2]=TR, cc[3]=BR
            _canvasLeft   = cc[0].x;
            _canvasBottom = cc[0].y;
            float canvasWorldW = cc[2].x - cc[0].x;
            float canvasWorldH = cc[1].y - cc[0].y;

            // ── CRITICAL: use canvas LOCAL rect (not reference resolution)
            // canvasRt.rect gives the ACTUAL canvas size in local units,
            // which CanvasScaler may expand/shrink from the reference resolution
            // depending on the device/game-view aspect ratio.
            // Dividing by this gives the correct world→canvas-local scale.
            float canvasLocalW = Mathf.Abs(canvasRt.rect.width);
            float canvasLocalH = Mathf.Abs(canvasRt.rect.height);
            _sfX = canvasLocalW > 0.01f ? canvasWorldW / canvasLocalW : 1f;
            _sfY = canvasLocalH > 0.01f ? canvasWorldH / canvasLocalH : 1f;

            // HTML reference = actual canvas local size (matches what Unity shows)
            _refW = Mathf.RoundToInt(canvasLocalW);
            _refH = Mathf.RoundToInt(canvasLocalH);

            // ── Background ───────────────────────────────────────────────────
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
                ReadElement(child, model.Children, 0f, 0f);

            return model;
        }

        // ─────────────────────────────────────────────────────────────────────
        // parentCssLeft/Top: canvas-space CSS (Y-down) position of parent top-left.
        // Used to compute child's position relative to parent div.
        // ─────────────────────────────────────────────────────────────────────
        private static void ReadElement(
            Transform t, List<UiElement> list,
            float parentCssLeft, float parentCssTop)
        {
            if (!t.gameObject.activeSelf) return;
            var rt = t.GetComponent<RectTransform>();
            if (rt == null) return;

            // ── Position via world-space pivot ────────────────────────────────
            // rt.position = world position of the RectTransform's pivot point.
            // This is UNAFFECTED by the element's own rotation, so we always get
            // the "anchor" position regardless of transform.
            float pivotWorldX = rt.position.x;
            float pivotWorldY = rt.position.y;

            // Convert world → canvas-local reference units
            float pivotRefX = (pivotWorldX - _canvasLeft)   / _sfX;
            float pivotRefY = (pivotWorldY - _canvasBottom)  / _sfY;  // Y-up

            // ── Size via local rect × lossyScale ──────────────────────────────
            // rt.rect.width/height is the computed LOCAL size (accounts for stretch,
            // sizeDelta, etc.) BEFORE any scale is applied.
            // rt.lossyScale is the cumulative world scale (canvas + parents + own).
            // Dividing by _sfX/_sfY converts from world-pixels back to canvas-local units.
            float scaleX  = _sfX > 0.001f ? rt.lossyScale.x / _sfX : 1f;
            float scaleY  = _sfY > 0.001f ? rt.lossyScale.y / _sfY : 1f;
            float visualW = Mathf.Abs(rt.rect.width)  * scaleX;
            float visualH = Mathf.Abs(rt.rect.height) * scaleY;

            // ── Canvas-space CSS position (Y-down) ────────────────────────────
            // Top-left of the UN-ROTATED bounding box in canvas CSS space.
            // left = pivot_x - pivot_fraction_of_width
            // top  = (refH - pivot_y_up) - (height * (1 - pivot.y))
            //       = refH - pivot_y_up - height + height*pivot.y
            //       = refH - (pivot_y_up + height*(1-pivot.y))
            float cssLeftGlobal = pivotRefX - visualW * rt.pivot.x;
            float cssTopGlobal  = _refH - (pivotRefY + visualH * (1f - rt.pivot.y));

            // ── Rotation ──────────────────────────────────────────────────────
            // localEulerAngles.z = element's OWN rotation, not cumulative.
            // CSS transform: rotate(Z deg) with same sign as Unity
            // (Unity left-handed positive Z = clockwise on screen = same as CSS positive).
            float rotZ = rt.localEulerAngles.z;
            if (rotZ > 180f) rotZ -= 360f;  // normalize to [-180, 180]

            var el = new UiElement
            {
                Name             = t.gameObject.name,
                Active           = true,
                AnchorMin        = rt.anchorMin,
                AnchorMax        = rt.anchorMax,
                AnchoredPosition = rt.anchoredPosition,
                SizeDelta        = rt.sizeDelta,
                Pivot            = rt.pivot,
                CssLeft          = cssLeftGlobal - parentCssLeft,
                CssTop           = cssTopGlobal  - parentCssTop,
                CssWidth         = visualW,
                CssHeight        = visualH,
                Rotation         = rotZ,
                PivotForOrigin   = rt.pivot,
            };

            // ── Component detection ───────────────────────────────────────────
            var imgComp    = t.GetComponent<Image>();
            var btnComp    = t.GetComponent<Button>();
            var tmpText    = t.GetComponent<TMP_Text>();
            var legacyText = t.GetComponent<Text>();

            if (btnComp    != null) el.Type = "button";
            else if (tmpText    != null || legacyText != null) el.Type = "text";
            else if (imgComp   != null) el.Type = "image";
            else                       el.Type = "container";

            if (imgComp != null)
            {
                el.Color = imgComp.color;
                if (imgComp.sprite != null)
                {
                    string path = AssetDatabase.GetAssetPath(imgComp.sprite);
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                        el.SpritePath = path;
                }
            }

            if (tmpText != null)
            {
                el.TextContent = tmpText.text;
                el.TextColor   = tmpText.color;
                // Font size: TMP value is in local units; scale to canvas-local visual px
                float localH   = Mathf.Abs(rt.rect.height);
                el.FontSize    = localH > 0.01f ? tmpText.fontSize * (visualH / localH) : tmpText.fontSize;
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

            // ── Recurse ───────────────────────────────────────────────────────
            // Pass this element's canvas-space top-left as parent reference for children.
            // For non-rotated parents this is exact; for rotated parents children
            // are positioned in the CSS rotated-parent coordinate space (correct via CSS inheritance).
            foreach (Transform child in t)
                ReadElement(child, el.Children, cssLeftGlobal, cssTopGlobal);
        }

        // ── TMP alignment → CSS flex values ──────────────────────────────────
        // TextAlignmentOptions bits: 0–7 = H (Left=1, Center=2, Right=4), 8–15 = V (Top=1, Mid=2, Bot=4)
        private static void ReadTmpAlign(TextAlignmentOptions a, out string h, out string v)
        {
            int flags = (int)a;
            h = (flags & 0xFF) switch
            {
                1 => "flex-start",
                2 => "center",
                4 => "flex-end",
                _ => "center",
            };
            v = ((flags >> 8) & 0xFF) switch
            {
                1 => "flex-start",
                2 => "center",
                4 => "flex-end",
                _ => "center",
            };
        }

        private static string LegacyAlignH(TextAnchor a) =>
            (a == TextAnchor.UpperLeft || a == TextAnchor.MiddleLeft || a == TextAnchor.LowerLeft)
                ? "flex-start"
                : (a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight)
                    ? "flex-end" : "center";

        private static string LegacyAlignV(TextAnchor a) =>
            (a == TextAnchor.UpperLeft || a == TextAnchor.UpperCenter || a == TextAnchor.UpperRight)
                ? "flex-start"
                : (a == TextAnchor.LowerLeft || a == TextAnchor.LowerCenter || a == TextAnchor.LowerRight)
                    ? "flex-end" : "center";

        private static Canvas FindRootCanvas()
        {
            foreach (var c in UnityEngine.Object.FindObjectsOfType<Canvas>())
                if (c.isRootCanvas) return c;
            return null;
        }
    }
}
