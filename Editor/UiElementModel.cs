using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Html5Build.Editor
{
    public class CanvasModel
    {
        public string GameName;
        public int    ReferenceWidth;   // from CanvasScaler.referenceResolution.x
        public int    ReferenceHeight;  // from CanvasScaler.referenceResolution.y
        public Color  BackgroundColor;
        public List<UiElement> Children = new List<UiElement>();
    }

    public class UiElement
    {
        public string Name;
        public string Type;    // image | button | text | container
        public bool   Active;

        // Raw RectTransform data (for Scene tab display)
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public Vector2 Pivot;

        // CSS values — computed from RectTransform math
        // Uses calc(anchor% + offset_px) for responsive 1:1 with Unity anchors
        public string CssLeft;    // e.g. "calc(50% - 80px)"  or "0px"  or "100%"
        public string CssTop;     // e.g. "calc(50% + 291px)" or "0px"
        public string CssWidth;   // e.g. "160px"             or "100%"
        public string CssHeight;  // e.g. "30px"              or "100%"

        // CSS transform
        public float   ScaleX    = 1f;   // from rt.localScale.x
        public float   ScaleY    = 1f;   // from rt.localScale.y
        public float   Rotation  = 0f;   // from rt.localEulerAngles.z (positive = Unity CCW)
        public Vector2 PivotForOrigin;   // copy of Pivot for CSS transform-origin

        // Visuals
        public Color   Color        = Color.white;
        public string  SpritePath;
        public Vector4 SpriteBorder; // x=left, y=bottom, z=right, w=top (sprite pixels)
        public float   SpriteWidth;  // sprite rect width  in pixels
        public float   SpriteHeight; // sprite rect height in pixels

        // Image fill (only relevant when Type == "image" or "button" with Image component)
        public Image.Type       ImageType  = Image.Type.Simple;
        public Image.FillMethod FillMethod = Image.FillMethod.Radial360;
        public float            FillAmount = 1f;
        public int              FillOrigin = 0;
        public bool             Clockwise  = true;

        // Text
        public string TextContent;
        public float  FontSize;
        public Color  TextColor  = Color.white;
        public string TextAlignH = "center";
        public string TextAlignV = "center";

        public List<UiElement> Children = new List<UiElement>();
    }
}
