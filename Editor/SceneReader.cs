using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Html5Build.Editor
{
    public static class SceneReader
    {
        public static CanvasModel ReadActiveScene()
        {
            Canvas root = FindRootCanvas();
            if (root == null)
                throw new Exception("No root Canvas found in the active scene.");

            // CanvasScaler → reference resolution
            var scaler = root.GetComponent<CanvasScaler>();
            Vector2 refRes = (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                ? scaler.referenceResolution
                : new Vector2(1080, 1920);

            int refW = Mathf.RoundToInt(refRes.x);
            int refH = Mathf.RoundToInt(refRes.y);

            // Background color = first Image child of canvas
            Color bgColor = new Color(0.08f, 0.08f, 0.08f);
            foreach (Transform child in root.transform)
            {
                var img = child.GetComponent<Image>();
                if (img != null) { bgColor = img.color; break; }
            }

            var model = new CanvasModel
            {
                GameName        = Application.productName,
                ReferenceWidth  = refW,
                ReferenceHeight = refH,
                BackgroundColor = bgColor,
            };

            foreach (Transform child in root.transform)
                ReadElement(child, model.Children, refW, refH);

            return model;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Recursive element reader
        // ─────────────────────────────────────────────────────────────────────
        private static void ReadElement(Transform t, List<UiElement> list, float parentW, float parentH)
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
            };

            // Component type detection
            var imgComp = t.GetComponent<Image>();
            var btnComp = t.GetComponent<Button>();

            if (btnComp != null)       el.Type = "button";
            else if (imgComp != null)  el.Type = "image";
            else                       el.Type = "container";

            if (imgComp != null)
            {
                el.Color = imgComp.color;
                if (imgComp.sprite != null)
                    el.SpritePath = AssetDatabase.GetAssetPath(imgComp.sprite);
            }

            // Compute CSS rect in parent-pixel space
            ComputeCssRect(el, parentW, parentH);

            list.Add(el);

            // Children — pass computed element size as their parent dimensions
            float childParentW = el.IsStretchX ? parentW : el.CssWidth;
            float childParentH = el.IsStretchY ? parentH : el.CssHeight;
            foreach (Transform child in t)
                ReadElement(child, el.Children, childParentW, childParentH);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Unity RectTransform → CSS absolute rect
        //
        // Unity coord system: origin bottom-left, Y up
        // CSS  coord system:  origin top-left,    Y down
        //
        // For a fixed-size element (anchorMin == anchorMax):
        //   anchor_px = anchorMin * parentSize
        //   pivot_px  = anchor_px + anchoredPosition          (Y up)
        //   left      = pivot_px.x - sizeDelta.x * pivot.x
        //   bottom_y  = pivot_px.y - sizeDelta.y * pivot.y    (Y up)
        //   top_y     = bottom_y + sizeDelta.y                (Y up)
        //   css_top   = parentH - top_y                       (flip to Y down)
        //
        // For stretched element (anchorMin != anchorMax):
        //   The four edges are each offset from their respective anchor edge.
        //   sizeDelta represents the inset/outset from the stretched region.
        //   left-edge  = anchorMin.x * parentW + (anchoredPosition.x - sizeDelta.x * pivot.x ... complex)
        //   Simplified: left  = anchorMin.x * parentW + left_offset
        //              right  = anchorMax.x * parentW - right_offset
        //   where [left_offset, right_offset] come from sizeDelta + anchoredPosition in stretch mode.
        //   For the common "full stretch" (0,0→1,1 with sizeDelta=0), this resolves to 0,0,100%,100%.
        // ─────────────────────────────────────────────────────────────────────
        private static void ComputeCssRect(UiElement el, float parentW, float parentH)
        {
            bool sx = !Mathf.Approximately(el.AnchorMin.x, el.AnchorMax.x);
            bool sy = !Mathf.Approximately(el.AnchorMin.y, el.AnchorMax.y);
            el.IsStretchX = sx;
            el.IsStretchY = sy;

            if (sx && sy)
            {
                // Full stretch — resolve to pixel rect relative to parent
                float l = el.AnchorMin.x * parentW + el.AnchoredPosition.x - el.SizeDelta.x * el.Pivot.x;
                float r = el.AnchorMax.x * parentW + el.AnchoredPosition.x + el.SizeDelta.x * (1 - el.Pivot.x);
                // Y: Unity up
                float b = el.AnchorMin.y * parentH + el.AnchoredPosition.y - el.SizeDelta.y * el.Pivot.y;
                float t = el.AnchorMax.y * parentH + el.AnchoredPosition.y + el.SizeDelta.y * (1 - el.Pivot.y);

                el.CssLeft   = l;
                el.CssTop    = parentH - t;   // flip
                el.CssWidth  = r - l;
                el.CssHeight = t - b;
                return;
            }

            if (sx) // stretch horizontally only
            {
                float l = el.AnchorMin.x * parentW + el.AnchoredPosition.x - el.SizeDelta.x * el.Pivot.x;
                float r = el.AnchorMax.x * parentW + el.AnchoredPosition.x + el.SizeDelta.x * (1 - el.Pivot.x);

                float anchorYpx  = el.AnchorMin.y * parentH;
                float pivotYpx   = anchorYpx + el.AnchoredPosition.y;
                float elementTop = pivotYpx - el.SizeDelta.y * el.Pivot.y + el.SizeDelta.y;

                el.CssLeft   = l;
                el.CssTop    = parentH - elementTop;
                el.CssWidth  = r - l;
                el.CssHeight = el.SizeDelta.y;
                return;
            }

            if (sy) // stretch vertically only
            {
                float b = el.AnchorMin.y * parentH + el.AnchoredPosition.y - el.SizeDelta.y * el.Pivot.y;
                float t = el.AnchorMax.y * parentH + el.AnchoredPosition.y + el.SizeDelta.y * (1 - el.Pivot.y);

                float anchorXpx = el.AnchorMin.x * parentW;
                float pivotXpx  = anchorXpx + el.AnchoredPosition.x;

                el.CssLeft   = pivotXpx - el.SizeDelta.x * el.Pivot.x;
                el.CssTop    = parentH - t;
                el.CssWidth  = el.SizeDelta.x;
                el.CssHeight = t - b;
                return;
            }

            // Fixed size, fixed anchor point
            {
                float anchorXpx = el.AnchorMin.x * parentW;
                float anchorYpx = el.AnchorMin.y * parentH;

                float pivotXpx  = anchorXpx + el.AnchoredPosition.x;
                float pivotYpx  = anchorYpx + el.AnchoredPosition.y;

                float w = el.SizeDelta.x;
                float h = el.SizeDelta.y;

                float elemLeft   = pivotXpx - w * el.Pivot.x;
                float elemBottom = pivotYpx - h * el.Pivot.y;
                float elemTop    = elemBottom + h;

                el.CssLeft   = elemLeft;
                el.CssTop    = parentH - elemTop;   // flip Y
                el.CssWidth  = w;
                el.CssHeight = h;
            }
        }

        private static Canvas FindRootCanvas()
        {
            foreach (var c in UnityEngine.Object.FindObjectsOfType<Canvas>())
                if (c.isRootCanvas) return c;
            return null;
        }
    }
}
