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

        // Raw RectTransform
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public Vector2 Pivot;

        // CSS rect relative to nearest positioned parent (canvas-local px)
        public float CssLeft;
        public float CssTop;
        public float CssWidth;
        public float CssHeight;

        // CSS transform
        public float  Rotation;     // local rotation Z (degrees)
        public Vector2 PivotForOrigin; // copy of Pivot for transform-origin

        // Image / Panel
        public Color  Color     = Color.white;
        public string SpritePath;

        // Text (TMP or Legacy)
        public string TextContent;
        public float  FontSize;
        public Color  TextColor  = Color.white;
        public string TextAlignH = "center";
        public string TextAlignV = "center";

        public List<UiElement> Children = new List<UiElement>();
    }
}
