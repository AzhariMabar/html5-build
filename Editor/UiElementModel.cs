using System.Collections.Generic;
using UnityEngine;

namespace Html5Build.Editor
{
    public class CanvasModel
    {
        public string GameName;
        public int    ReferenceWidth;
        public int    ReferenceHeight;
        public Color  BackgroundColor;
        public List<UiElement> Children = new List<UiElement>();
    }

    public class UiElement
    {
        public string Name;
        public string Type;    // image | button | text | container
        public bool   Active;

        // Raw RectTransform (kept for debug/display in window)
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public Vector2 Pivot;

        // CSS rect relative to nearest positioned parent (reference-resolution px)
        public float CssLeft;
        public float CssTop;
        public float CssWidth;
        public float CssHeight;

        // Image / Panel
        public Color  Color     = Color.white;
        public string SpritePath;           // absolute Unity asset path (null = use Color)

        // Text (TMP or Legacy)
        public string TextContent;
        public float  FontSize;
        public Color  TextColor  = Color.white;
        public string TextAlignH = "center"; // CSS justify-content
        public string TextAlignV = "center"; // CSS align-items

        public List<UiElement> Children = new List<UiElement>();
    }
}
