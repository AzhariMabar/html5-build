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
        public string Type;   // panel | image | button | text | container
        public bool   Active;

        // Raw RectTransform
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public Vector2 Pivot;

        // Computed CSS rect in parent-space pixels
        public float CssLeft;
        public float CssTop;
        public float CssWidth;
        public float CssHeight;
        public bool  IsStretchX;
        public bool  IsStretchY;

        // Visuals
        public Color  Color      = Color.white;
        public string SpritePath;   // absolute Unity asset path
        public string TextContent;
        public float  FontSize;
        public Color  TextColor  = Color.black;

        public List<UiElement> Children = new List<UiElement>();
    }
}
