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
        // State shared during a single scan
        private static float _sf;           // screen-to-reference scale factor
        private static float _canvasLeft;   // canvas world-space origin X
        private static float _canvasBottom; // canvas world-space origin Y
        private static int   _refW;
        private static int   _refH;

        // ─────────────────────────────────────────────────────────────────────
        public static CanvasModel ReadActiveScene()
        {
            // Flush all pending layout recalculations
            Canvas.ForceUpdateCanvases();

            Canvas root = FindRootCanvas();
            if (root == null)
                throw new Exception("No root Canvas found. Make sure a Canvas is in the active scene.");

            // Reference resolution
            var scaler = root.GetComponent<CanvasScaler>();
            _refW = 1080; _refH = 1920;
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                _refW = Mathf.RoundToInt(scaler.referenceResolution.x);
                _refH = Mathf.RoundToInt(scaler.referenceResolution.y);
            }

            // Scale factor = ratio of canvas world-rect to reference resolution.
            // This handles any game-view size without requiring root.scaleFactor.
            var canvasRt = root.GetComponent<RectTransform>();
            var cc = new Vector3[4];
            canvasRt.GetWorldCorners(cc);
            _canvasLeft   = cc[0].x;
            _canvasBottom = cc[0].y;
            float canvasWorldW = cc[2].x - cc[0].x;
            _sf = canvasWorldW > 0.01f ? canvasWorldW / _refW : 1f;

            // Background = first Image child of canvas
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

            // parentCssLeft/Top = 0,0 because children of canvas are
            // positioned relative to the canvas div (top-left of canvas = CSS 0,0)
            foreach (Transform child in root.transform)
                ReadElement(child, model.Children, 0f, 0f);

            return model;
        }

        // ─────────────────────────────────────────────────────────────────────
        // parentCssLeft/Top: absolute CSS coordinates (Y-down) of the parent's
        // top-left corner within the canvas. Used to compute relative positioning.
        // ─────────────────────────────────────────────────────────────────────
        private static void ReadElement(
            Transform t, List<UiElement> list,
            float parentCssLeft, float parentCssTop)
        {
            if (!t.gameObject.activeSelf) return;

            var rt = t.GetComponent<RectTransform>();
            if (rt == null) return;

            // ── World corners → reference-resolution coordinates ──────────────
            // GetWorldCorners accounts for localScale, rotation, and all parent
            // transforms automatically. corners: [0]=BL, [1]=TL, [2]=TR, [3]=BR
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            float refLeft   = (corners[0].x - _canvasLeft)   / _sf;
            float refBottom = (corners[0].y - _canvasBottom)  / _sf;
            float refTop    = (corners[1].y - _canvasBottom)  / _sf;
            float refRight  = (corners[2].x - _canvasLeft)    / _sf;

            // CSS coordinate system: Y increases downward from canvas top
            float cssLeftGlobal = refLeft;
            float cssTopGlobal  = _refH - refTop;
            float cssW          = Mathf.Max(0f, refRight  - refLeft);
            float cssH          = Mathf.Max(0f, refTop    - refBottom);

            var el = new UiElement
            {
                Name             = t.gameObject.name,
                Active           = true,
                AnchorMin        = rt.anchorMin,
                AnchorMax        = rt.anchorMax,
                AnchoredPosition = rt.anchoredPosition,
                SizeDelta        = rt.sizeDelta,
                Pivot            = rt.pivot,
                // Position relative to parent's top-left in CSS space
                CssLeft          = cssLeftGlobal - parentCssLeft,
                CssTop           = cssTopGlobal  - parentCssTop,
                CssWidth         = cssW,
                CssHeight        = cssH,
            };

            // ── Detect component type ─────────────────────────────────────────
            var imgComp    = t.GetComponent<Image>();
            var btnComp    = t.GetComponent<Button>();
            var tmpText    = t.GetComponent<TMP_Text>();          // covers both UGUI + World
            var legacyText = t.GetComponent<Text>();

            if (btnComp    != null) el.Type = "button";
            else if (tmpText    != null || legacyText != null) el.Type = "text";
            else if (imgComp   != null) el.Type = "image";
            else                       el.Type = "container";

            // ── Image / sprite ────────────────────────────────────────────────
            if (imgComp != null)
            {
                el.Color = imgComp.color;
                if (imgComp.sprite != null)
                {
                    string path = AssetDatabase.GetAssetPath(imgComp.sprite);
                    // Only include real user assets (not built-in Unity sprites)
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                        el.SpritePath = path;
                }
            }

            // ── Text ──────────────────────────────────────────────────────────
            if (tmpText != null)
            {
                el.TextContent = tmpText.text;
                el.TextColor   = tmpText.color;

                // Font size: TMP fontSize is in local units; scale to visual px
                float localH = Mathf.Abs(rt.rect.height);
                float scaleY = (localH > 0.01f && cssH > 0.01f) ? cssH / localH : 1f;
                el.FontSize  = tmpText.fontSize * scaleY;

                ReadTmpAlign(tmpText.alignment, out el.TextAlignH, out el.TextAlignV);
            }
            else if (legacyText != null)
            {
                el.TextContent = legacyText.text;
                el.TextColor   = legacyText.color;
                el.FontSize    = legacyText.fontSize;
                el.TextAlignH  = legacyText.alignment == TextAnchor.UpperLeft   ||
                                 legacyText.alignment == TextAnchor.MiddleLeft  ||
                                 legacyText.alignment == TextAnchor.LowerLeft   ? "flex-start" :
                                 legacyText.alignment == TextAnchor.UpperRight  ||
                                 legacyText.alignment == TextAnchor.MiddleRight ||
                                 legacyText.alignment == TextAnchor.LowerRight  ? "flex-end" : "center";
                el.TextAlignV  = legacyText.alignment == TextAnchor.UpperLeft   ||
                                 legacyText.alignment == TextAnchor.UpperCenter ||
                                 legacyText.alignment == TextAnchor.UpperRight  ? "flex-start" :
                                 legacyText.alignment == TextAnchor.LowerLeft   ||
                                 legacyText.alignment == TextAnchor.LowerCenter ||
                                 legacyText.alignment == TextAnchor.LowerRight  ? "flex-end" : "center";
            }

            list.Add(el);

            // ── Recurse (pass this element's absolute CSS position as parent) ─
            foreach (Transform child in t)
                ReadElement(child, el.Children, cssLeftGlobal, cssTopGlobal);
        }

        // ── TMP alignment → CSS flex values ──────────────────────────────────
        // TextAlignmentOptions bit layout: bits 0–7 = horizontal, bits 8–15 = vertical
        // H: Left=1, Center=2, Right=4, Justified=8
        // V: Top=1, Middle=2, Bottom=4
        private static void ReadTmpAlign(TextAlignmentOptions a, out string h, out string v)
        {
            int flags = (int)a;
            h = (flags & 0xFF) switch
            {
                1 => "flex-start",
                2 => "center",
                4 => "flex-end",
                8 => "space-between",
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

        private static Canvas FindRootCanvas()
        {
            foreach (var c in UnityEngine.Object.FindObjectsOfType<Canvas>())
                if (c.isRootCanvas) return c;
            return null;
        }
    }
}
