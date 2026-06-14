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

    // One persistent listener entry from Button.onClick
    public class OnClickCall
    {
        public string MethodName;
        public string TargetName;   // name of the target GameObject or Component
        public int    Mode;         // UnityEngine.Events.PersistentListenerMode
        public string StringArg;
        public bool   BoolArg;
        public int    IntArg;
        public float  FloatArg;
    }

    public class UiElement
    {
        public string Id;
        public string Name;
        public string Type;    // image | button | text | container
        public bool   Active;
        public bool   IsRectMask;
        public bool   IsMask;

        // CanvasGroup
        public float CanvasGroupAlpha = 1f;
        public bool  CanvasGroupInteractable = true;
        public bool  CanvasGroupBlocksRaycasts = true;

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
        public bool    HasImage;                       // true only if Image component exists on this GameObject
        public bool    Raycast;                        // true if raycastTarget is enabled (Image / Text / Button)
        public Color   Color           = Color.white;
        public string  SpritePath;
        public Vector4 SpriteBorder;    // x=left, y=bottom, z=right, w=top (sprite pixels)
        public float   SpriteWidth;     // sprite rect width  in pixels
        public float   SpriteHeight;    // sprite rect height in pixels
        public Vector4 SpriteBorderRef; // border in canvas reference-pixels (for CSS border-image-width)

        // Image fill (only relevant when Type == "image" or "button" with Image component)
        public Image.Type       ImageType  = Image.Type.Simple;
        public Image.FillMethod FillMethod = Image.FillMethod.Radial360;
        public float            FillAmount = 1f;
        public int              FillOrigin = 0;
        public bool             Clockwise  = true;

        // HTML action (from HtmlAction component) — explicit override for onClick JS body
        // Format: "panel:ID" | "url:https://..." | "js:expression()"
        public string HtmlAction;

        // Persistent OnClick calls read directly from Button.onClick inspector wiring
        public List<OnClickCall> OnClickCalls = new List<OnClickCall>();
        public List<OnClickCall> OnValueChangedCalls = new List<OnClickCall>();
        public List<OnClickCall> OnEndEditCalls = new List<OnClickCall>();

        // Button state colors (Color Tint transition)
        public Color  BtnNormalColor      = Color.white;
        public Color  BtnHighlightedColor = Color.white;
        public Color  BtnPressedColor     = Color.white;
        public Color  BtnDisabledColor    = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        public float  BtnColorMultiplier  = 1f;
        public float  BtnFadeDuration     = 0.1f;
        public bool   BtnInteractable     = true;

        // Text
        public string TextContent;
        public float  FontSize;
        public Color  TextColor  = Color.white;
        public string TextAlignH = "center";
        public string TextAlignV = "center";

        // InputField / TMP_InputField
        public string InputText;
        public string InputPlaceholder;
        public int    InputCharacterLimit;
        public bool   InputReadOnly;
        public bool   InputMultiline;
        public bool   InputPassword;
        public string InputContentType = "text";

        // Slider
        public float SliderMin;
        public float SliderMax = 1f;
        public float SliderValue;
        public bool  SliderWholeNumbers;
        public int   SliderDirection;
        public Color SliderFillColor = Color.white;

        // Toggle
        public bool ToggleIsOn;
        public bool ToggleInteractable = true;

        // Dropdown
        public int DropdownValue;
        public bool DropdownInteractable = true;
        public List<string> DropdownOptions = new List<string>();

        // ScrollRect
        public bool ScrollHorizontal = true;
        public bool ScrollVertical = true;
        public float ScrollSensitivity = 1f;

        public List<UiElement> Children = new List<UiElement>();

        // Tweening config — null if no Tweening component on this GameObject
        public TweenConfig TweenConfig;
    }

    public class TweenConfig
    {
        public bool   AutoPlay;
        public int    NumberAnimArray = -1;
        public bool   Loop;
        public int    Repeat;
        public bool   Yoyo;
        public List<TweenDataItem> TweenData = new List<TweenDataItem>();
    }

    public class TweenDataItem
    {
        public string  Type;        // "Position"|"PositionUI"|"Scale"|"Rotation"|"Opacity"|"TextFade"|"CamShake"
        public Vector3 StartPoint;
        public bool    Loop;
        public bool    Yoyo;
        public List<TweenPathItem> Sequence = new List<TweenPathItem>();
    }

    public class TweenPathItem
    {
        public float   Duration = 1f;
        public float   Delay    = 0f;
        public string  Ease     = "Linear";
        public Vector3 WayPoint;
    }
}
