using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Html5Build.Editor
{
    public static class IndexHtmlTemplate
    {
        public static string Generate(CanvasModel m)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"utf-8\" />");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            sb.AppendLine($"    <title>{HtmlEsc(m.GameName)}</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        :root { --rs: 1; }");
            sb.AppendLine("        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }");
            // Body background matches the game — no visible letterbox on any screen size.
            sb.AppendLine($"        html, body {{ width: 100%; height: 100%; overflow: hidden; background: {CssRgba(m.BackgroundColor)}; }}");
            sb.AppendLine("        #_wrapper { position: fixed; inset: 0; overflow: hidden; }");
            // #_app fills the entire viewport (inset:0).
            // --rs = min(w/REF_W, h/REF_H) so elements scale uniformly without overflow.
            // Anchor % positions (top, center, bottom) map directly to viewport edges.
            sb.AppendLine($"        #_app {{ position: absolute; inset: 0; overflow: hidden; background: {CssRgba(m.BackgroundColor)}; }}");
            // pointer-events:none on .ui makes ALL divs/spans pass-through by default.
            // Only buttons (.ui-btn) and elements with raycastTarget=true get pointer-events:auto.
            // This mirrors Unity's Raycast Target system: transparent containers never block clicks.
            sb.AppendLine("        .ui { position: absolute; pointer-events: none; }");
            // appearance:none removes native button chrome (prevents override of border-image, background, etc.)
            sb.AppendLine("        .ui-btn { appearance: none; -webkit-appearance: none; border: none; outline: none; cursor: pointer; padding: 0; background: transparent; pointer-events: auto; }");
            sb.AppendLine("        .ui-img > img { width: 100%; height: 100%; object-fit: fill; display: block; }");
            sb.AppendLine("        .ui-text { overflow: hidden; white-space: pre-wrap; word-break: break-word; }");
            sb.AppendLine("        .ui-input, .ui-dropdown { appearance: none; -webkit-appearance: none; border: none; outline: none; padding: calc(8px * var(--rs)) calc(12px * var(--rs)); pointer-events: auto; }");
            sb.AppendLine("        textarea.ui-input { resize: none; }");
            sb.AppendLine("        .ui-input::placeholder { color: currentColor; opacity: .55; }");
            sb.AppendLine("        .ui-slider { appearance: none; -webkit-appearance: none; border: none; outline: none; background: transparent; pointer-events: auto; accent-color: var(--slider-fill, #fff); }");
            sb.AppendLine("        .ui-slider::-webkit-slider-runnable-track { height: 100%; background: rgba(255,255,255,.25); border-radius: 999px; }");
            sb.AppendLine("        .ui-slider::-webkit-slider-thumb { -webkit-appearance: none; width: calc(20px * var(--rs)); height: calc(20px * var(--rs)); margin-top: calc(-10px * var(--rs)); border: 0; border-radius: 50%; background: var(--slider-fill, #fff); }");
            sb.AppendLine("        .ui-slider[data-vertical='true'] { writing-mode: vertical-lr; direction: rtl; }");
            sb.AppendLine("        .ui-toggle { display: flex; align-items: center; gap: calc(8px * var(--rs)); pointer-events: auto; }");
            sb.AppendLine("        .ui-toggle > input { width: 100%; height: 100%; margin: 0; accent-color: currentColor; cursor: pointer; }");
            sb.AppendLine("        .ui-scroll { overscroll-behavior: contain; -webkit-overflow-scrolling: touch; pointer-events: auto; }");
            WriteButtonStateStyles(sb, m.Children);
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div id=\"_wrapper\">");
            sb.AppendLine("        <div id=\"_app\">");
            WriteElements(sb, m.Children, 3);
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <script src=\"tweening.js\"></script>");
            sb.AppendLine("    <script src=\"app.js\"></script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        private static void WriteElements(StringBuilder sb, List<UiElement> elements, int indent)
        {
            string pad = Pad(indent);
            foreach (var el in elements)
            {
                string id    = ElementId(el);
                string style = BuildStyle(el);

                switch (el.Type)
                {
                    case "input":
                    {
                        string type = el.InputPassword ? "password" : el.InputContentType;
                        string maxAttr = el.InputCharacterLimit > 0 ? $" maxlength=\"{el.InputCharacterLimit}\"" : "";
                        string readOnlyAttr = el.InputReadOnly ? " readonly" : "";
                        if (el.InputMultiline)
                        {
                            sb.AppendLine($"{pad}<textarea id=\"{id}\" class=\"ui ui-input\" data-unity-control=\"input\"{maxAttr}{readOnlyAttr} placeholder=\"{AttrEsc(el.InputPlaceholder ?? "")}\" style=\"{style}\">{HtmlEsc(el.InputText ?? "")}</textarea>");
                        }
                        else
                        {
                            sb.AppendLine($"{pad}<input id=\"{id}\" class=\"ui ui-input\" data-unity-control=\"input\" type=\"{type}\"{maxAttr}{readOnlyAttr} value=\"{AttrEsc(el.InputText ?? "")}\" placeholder=\"{AttrEsc(el.InputPlaceholder ?? "")}\" style=\"{style}\" />");
                        }
                        break;
                    }

                    case "slider":
                    {
                        string step = el.SliderWholeNumbers ? "1" : "any";
                        bool vertical = el.SliderDirection == 2 || el.SliderDirection == 3;
                        bool reversed = el.SliderDirection == 1 || el.SliderDirection == 3;
                        string sliderStyle = style
                            + $"--slider-fill:{CssRgba(el.SliderFillColor)};"
                            + (reversed && !vertical ? "direction:rtl;" : "");
                        sb.AppendLine($"{pad}<input id=\"{id}\" class=\"ui ui-slider\" data-unity-control=\"slider\" data-vertical=\"{vertical.ToString().ToLowerInvariant()}\" type=\"range\" min=\"{el.SliderMin:F4}\" max=\"{el.SliderMax:F4}\" step=\"{step}\" value=\"{el.SliderValue:F4}\" style=\"{sliderStyle}\" />");
                        break;
                    }

                    case "toggle":
                    {
                        string checkedAttr = el.ToggleIsOn ? " checked" : "";
                        string disabledAttr = el.ToggleInteractable ? "" : " disabled";
                        sb.AppendLine($"{pad}<label id=\"{id}\" class=\"ui ui-toggle\" data-unity-control=\"toggle\" style=\"{style}\">");
                        sb.AppendLine($"{pad}    <input type=\"checkbox\"{checkedAttr}{disabledAttr} />");
                        sb.AppendLine($"{pad}</label>");
                        break;
                    }

                    case "dropdown":
                    {
                        string disabledAttr = el.DropdownInteractable ? "" : " disabled";
                        sb.AppendLine($"{pad}<select id=\"{id}\" class=\"ui ui-dropdown\" data-unity-control=\"dropdown\"{disabledAttr} style=\"{style}\">");
                        for (int i = 0; i < el.DropdownOptions.Count; i++)
                        {
                            string selected = i == el.DropdownValue ? " selected" : "";
                            sb.AppendLine($"{pad}    <option value=\"{i}\"{selected}>{HtmlEsc(el.DropdownOptions[i] ?? "")}</option>");
                        }
                        sb.AppendLine($"{pad}</select>");
                        break;
                    }

                    case "scroll":
                    {
                        string overflowX = el.ScrollHorizontal ? "auto" : "hidden";
                        string overflowY = el.ScrollVertical ? "auto" : "hidden";
                        sb.AppendLine($"{pad}<div id=\"{id}\" class=\"ui ui-scroll\" data-unity-control=\"scroll\" style=\"{style}overflow-x:{overflowX};overflow-y:{overflowY};\">");
                        WriteElements(sb, el.Children, indent + 1);
                        sb.AppendLine($"{pad}</div>");
                        break;
                    }

                    case "button":
                    {
                        // All buttons: image on a separate .ui-btn-bg layer so CSS filter (hover/active)
                        // applies only to the image — text children are NOT inside the filtered layer.
                        string btnStyle = BuildStyle(el, skipVisuals: true) + "overflow:visible;";
                        string disAttr  = el.BtnInteractable ? "" : " disabled";
                        sb.AppendLine($"{pad}<button id=\"{id}\" class=\"ui ui-btn\"{disAttr} style=\"{btnStyle}\">");
                        string imgDiv = BuildImageDivStyle(el);
                        if (!string.IsNullOrEmpty(imgDiv))
                            sb.AppendLine($"{pad}    <div class=\"ui-btn-bg\" style=\"position:absolute;left:0;top:0;width:100%;height:100%;{imgDiv}pointer-events:none;\"></div>");
                        WriteElements(sb, el.Children, indent + 1);
                        sb.AppendLine($"{pad}</button>");
                        break;
                    }

                    case "text":
                        sb.AppendLine($"{pad}<div id=\"{id}\" class=\"ui ui-text\" style=\"{style}\">{HtmlEsc(el.TextContent ?? "")}</div>");
                        break;

                    case "image":
                        sb.AppendLine($"{pad}<div id=\"{id}\" class=\"ui ui-img\" style=\"{style}\">");
                        // Sprite for image elements uses <img> so it responds to object-fit.
                        // BuildStyle skips background-url for image type to avoid double render.
                        if (!string.IsNullOrEmpty(el.SpritePath) && el.ImageType == Image.Type.Simple)
                            sb.AppendLine($"{pad}    <img src=\"{SrcRef(el.SpritePath)}\" alt=\"\" />");
                        WriteElements(sb, el.Children, indent + 1);
                        sb.AppendLine($"{pad}</div>");
                        break;

                    default:
                        sb.AppendLine($"{pad}<div id=\"{id}\" class=\"ui\" style=\"{style}\">");
                        WriteElements(sb, el.Children, indent + 1);
                        sb.AppendLine($"{pad}</div>");
                        break;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        private static string BuildStyle(UiElement el, bool skipVisuals = false)
        {
            var sb = new StringBuilder();

            if (!el.Active) sb.Append("display:none;");
            sb.Append($"left:{el.CssLeft};");
            sb.Append($"top:{el.CssTop};");
            sb.Append($"width:{el.CssWidth};");
            sb.Append($"height:{el.CssHeight};");

            if (el.IsRectMask || el.IsMask)
                sb.Append("overflow:hidden;");

            if (el.CanvasGroupAlpha < 0.999f)
                sb.Append($"opacity:{el.CanvasGroupAlpha:F3};");

            if (!el.CanvasGroupInteractable || !el.CanvasGroupBlocksRaycasts)
                sb.Append("pointer-events:none;");

            bool hasScale = Mathf.Abs(el.ScaleX - 1f) > 0.001f || Mathf.Abs(el.ScaleY - 1f) > 0.001f;
            bool hasRot   = Mathf.Abs(el.Rotation) > 0.01f;

            if (hasScale || hasRot)
            {
                float ox = el.PivotForOrigin.x * 100f;
                float oy = (1f - el.PivotForOrigin.y) * 100f;
                sb.Append($"transform-origin:{ox:F0}% {oy:F0}%;");

                sb.Append("transform:");
                if (hasScale)
                    sb.Append(Mathf.Approximately(el.ScaleX, el.ScaleY)
                        ? $"scale({el.ScaleX:F4})"
                        : $"scale({el.ScaleX:F4},{el.ScaleY:F4})");
                if (hasRot)
                    sb.Append($"rotate({-el.Rotation:F2}deg)");
                sb.Append(";");
            }

            if (el.Type != "text" && !skipVisuals && el.HasImage)
            {
                string src      = SrcRef(el.SpritePath);
                bool   hasTint  = src != null && !IsWhite(el.Color);
                string colorStr = CssRgba(el.Color);

                if (el.ImageType == Image.Type.Filled)
                {
                    if (src != null)
                    {
                        // Sprite as base; tint via multiply blend if not white
                        if (hasTint)
                        {
                            sb.Append($"background:{colorStr} url('{src}') center/100% 100% no-repeat;");
                            sb.Append("background-blend-mode:multiply;");
                        }
                        else
                            sb.Append($"background:url('{src}') center/100% 100% no-repeat;");
                        sb.Append(FillMask(el));
                    }
                    else if (!IsInvisible(el.Color))
                        sb.Append(FillBackground(el));
                }
                else if (el.ImageType == Image.Type.Sliced && src != null)
                {
                    SlicedBackground(sb, el, src, hasTint, colorStr);
                }
                else if (el.ImageType == Image.Type.Tiled && src != null)
                {
                    sb.Append($"background:{colorStr} url('{src}') repeat;");
                    sb.Append("background-size:auto;");
                    if (hasTint) sb.Append("background-blend-mode:multiply;");
                }
                else
                {
                    if (src != null)
                    {
                        if (hasTint)
                        {
                            sb.Append($"background:{colorStr} url('{src}') center/100% 100% no-repeat;");
                            sb.Append("background-blend-mode:multiply;");
                        }
                        else
                            sb.Append($"background:url('{src}') center/100% 100% no-repeat;");
                    }
                    else if (!IsInvisible(el.Color))
                        sb.Append($"background:{colorStr};");
                }

                // Alpha — applies to whole element (including children, same as Unity CanvasGroup)
                if (el.Color.a < 0.999f)
                    sb.Append($"opacity:{el.Color.a:F3};");
            }

            if (el.Type == "text")
            {
                sb.Append("display:flex;");
                sb.Append($"align-items:{el.TextAlignV};");
                sb.Append($"justify-content:{el.TextAlignH};");
                sb.Append($"color:{CssRgba(el.TextColor)};");
                sb.Append($"font-size:calc({el.FontSize:F0}px * var(--rs));");
                sb.Append("font-family:Arial,sans-serif;");
                // Text raycastTarget=true → clickable; false → pass-through (user-select:none always)
                sb.Append(el.Raycast ? "pointer-events:auto;" : "pointer-events:none;");
                sb.Append("user-select:none;");
            }

            if (el.Type == "input" || el.Type == "dropdown")
            {
                sb.Append($"color:{CssRgba(el.TextColor)};");
                sb.Append($"font-size:calc({Mathf.Max(el.FontSize, 14f):F0}px * var(--rs));");
                sb.Append("font-family:Arial,sans-serif;");
            }

            // Non-button elements: only add pointer-events:auto if raycastTarget is on
            // (buttons already get it from .ui-btn CSS class)
            if (el.Type != "button" && el.Type != "text" && el.Raycast)
                sb.Append("pointer-events:auto;");

            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Image.Type.Sliced — 9-slice via CSS border-image
        // SpriteBorder: x=left, y=bottom, z=right, w=top (sprite pixels)
        private static void SlicedBackground(StringBuilder sb, UiElement el, string src, bool hasTint, string colorStr)
        {
            float bT = el.SpriteBorder.w;
            float bR = el.SpriteBorder.z;
            float bB = el.SpriteBorder.y;
            float bL = el.SpriteBorder.x;
            float sW = el.SpriteWidth  > 0.1f ? el.SpriteWidth  : 192f;
            float sH = el.SpriteHeight > 0.1f ? el.SpriteHeight : 192f;

            bool hasSlice = bT + bR + bB + bL > 0.1f;

            if (!hasSlice)
            {
                if (hasTint) { sb.Append($"background:{colorStr} url('{src}') center/100% 100% no-repeat;"); sb.Append("background-blend-mode:multiply;"); }
                else           sb.Append($"background:url('{src}') center/100% 100% no-repeat;");
                return;
            }

            // border-image-slice: % of source image — which pixels form corners/edges.
            float pT = bT / sH * 100f;
            float pR = bR / sW * 100f;
            float pB = bB / sH * 100f;
            float pL = bL / sW * 100f;

            // border-image-width: scale-compensated px so that after transform:scale(S)
            // the visual corner = sprite border × --rs, matching Unity 9-slice exactly.
            // Formula: border_px / scale = CSS px → CSS px × scale = border_px ✓
            float sx = Mathf.Max(Mathf.Abs(el.ScaleX), 0.001f);
            float sy = Mathf.Max(Mathf.Abs(el.ScaleY), 0.001f);
            float bwT = bT / sy; float bwR = bR / sx; float bwB = bB / sy; float bwL = bL / sx;

            // border-style must NOT be 'none' or border-image won't paint.
            // border-width:0 keeps layout unchanged; border-image-width handles visual size.
            sb.Append("border:0 solid transparent;");
            sb.Append($"border-image-source:url('{src}');");
            sb.Append($"border-image-slice:{pT:F2}% {pR:F2}% {pB:F2}% {pL:F2}% fill;");
            sb.Append($"border-image-width:calc({bwT:F3}px * var(--rs)) calc({bwR:F3}px * var(--rs)) calc({bwB:F3}px * var(--rs)) calc({bwL:F3}px * var(--rs));");
            sb.Append("border-image-outset:0;");
            sb.Append("border-image-repeat:stretch;");

            if (hasTint)
                sb.Append($"background:{colorStr};");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Returns the full background CSS for a button's .ui-btn-bg image layer.
        // Handles all Image.Type values so the layer can receive CSS filter for state tinting
        // without affecting sibling text children.
        private static string BuildImageDivStyle(UiElement el)
        {
            if (!el.HasImage) return "";   // button has no Image component — no background div
            string src      = SrcRef(el.SpritePath);
            bool   hasTint  = src != null && !IsWhite(el.Color);
            string colorStr = CssRgba(el.Color);
            var    sb       = new StringBuilder();

            if (el.ImageType == Image.Type.Filled)
            {
                if (src != null)
                {
                    if (hasTint) { sb.Append($"background:{colorStr} url('{src}') center/100% 100% no-repeat;"); sb.Append("background-blend-mode:multiply;"); }
                    else           sb.Append($"background:url('{src}') center/100% 100% no-repeat;");
                    sb.Append(FillMask(el));
                }
                else if (!IsInvisible(el.Color))
                    sb.Append(FillBackground(el));
            }
            else if (el.ImageType == Image.Type.Sliced && src != null)
            {
                SlicedBackground(sb, el, src, hasTint, colorStr);
            }
            else if (el.ImageType == Image.Type.Tiled && src != null)
            {
                sb.Append($"background:{colorStr} url('{src}') repeat;background-size:auto;");
                if (hasTint) sb.Append("background-blend-mode:multiply;");
            }
            else
            {
                if (src != null)
                {
                    if (hasTint) { sb.Append($"background:{colorStr} url('{src}') center/100% 100% no-repeat;"); sb.Append("background-blend-mode:multiply;"); }
                    else           sb.Append($"background:url('{src}') center/100% 100% no-repeat;");
                }
                else if (!IsInvisible(el.Color))
                    sb.Append($"background:{colorStr};");
            }

            // Alpha on the layer (Filled handles alpha via its mask gradient, skip here)
            if (el.Color.a < 0.999f && el.ImageType != Image.Type.Filled)
                sb.Append($"opacity:{el.Color.a:F3};");

            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Emits per-button CSS rules for Color Tint states (hover / active / disabled).
        // Targets #id > .ui-btn-bg so only the image layer is filtered, not text children.
        private static void WriteButtonStateStyles(StringBuilder sb, List<UiElement> elements)
        {
            foreach (var el in elements)
            {
                if (el.Type == "button")
                {
                    string id = ElementId(el);

                    float normL = Luma(el.BtnNormalColor);
                    if (normL < 0.001f) normL = 1f;
                    float hoverBr  = Luma(el.BtnHighlightedColor) / normL;
                    float pressBr  = Luma(el.BtnPressedColor)     / normL;
                    float disBr    = Luma(el.BtnDisabledColor)     / normL;
                    float disAlpha = el.BtnDisabledColor.a;
                    float fade     = el.BtnFadeDuration;

                    sb.AppendLine($"        #{id} > .ui-btn-bg {{ transition:filter {fade:F2}s; }}");

                    if (!el.BtnInteractable)
                    {
                        sb.AppendLine($"        #{id} > .ui-btn-bg {{ filter:brightness({disBr:F3}) opacity({disAlpha:F3}); }}");
                        sb.AppendLine($"        #{id} {{ pointer-events:none; cursor:default; }}");
                    }
                    else
                    {
                        if (!IsClose(hoverBr, 1f))
                            sb.AppendLine($"        #{id}:hover > .ui-btn-bg {{ filter:brightness({hoverBr:F3}); }}");
                        if (!IsClose(pressBr, 1f))
                            sb.AppendLine($"        #{id}:active > .ui-btn-bg {{ filter:brightness({pressBr:F3}); }}");
                        // Disabled via JS/attribute
                        sb.AppendLine($"        #{id}:disabled > .ui-btn-bg, #{id}[disabled] > .ui-btn-bg {{ filter:brightness({disBr:F3}) opacity({disAlpha:F3}); }}");
                        sb.AppendLine($"        #{id}:disabled, #{id}[disabled] {{ pointer-events:none; cursor:default; }}");
                    }
                }
                WriteButtonStateStyles(sb, el.Children);
            }
        }

        private static float Luma(Color c)    => c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
        private static bool  IsClose(float a, float b) => Mathf.Abs(a - b) < 0.01f;

        // ─────────────────────────────────────────────────────────────────────
        // Image.Type.Filled — solid color rendered as CSS gradient
        private static string FillBackground(UiElement el)
        {
            string c    = CssRgba(el.Color);
            string grad = FillGradient(el, c, "transparent");
            return $"background:{grad};";
        }

        // Image.Type.Filled — sprite as background, gradient as mask
        private static string FillMask(UiElement el)
        {
            string grad = FillGradient(el, "white", "transparent");
            return $"-webkit-mask:{grad};mask:{grad};";
        }

        // Build the CSS gradient string for a fill effect.
        // filled = visible color, empty = hidden color.
        private static string FillGradient(UiElement el, string filled, string empty)
        {
            float amount = Mathf.Clamp01(el.FillAmount);

            switch (el.FillMethod)
            {
                case Image.FillMethod.Radial360:
                {
                    // Unity OriginRadial360: 0=Bottom, 1=Right, 2=Top, 3=Left
                    // CSS conic-gradient from-angle (clockwise from 12 o'clock):
                    //   Bottom=180, Right=90, Top=0, Left=270
                    float[] origins = { 180f, 90f, 0f, 270f };
                    float   origin  = origins[el.FillOrigin % 4];
                    float   sweep   = amount * 360f;
                    float   start   = el.Clockwise ? origin : origin - sweep;
                    return $"conic-gradient(from {start:F1}deg,{filled} 0deg,{filled} {sweep:F1}deg,{empty} {sweep:F1}deg)";
                }
                case Image.FillMethod.Radial180:
                {
                    // Unity OriginRadial180: 0=Bottom, 1=Left, 2=Top, 3=Right
                    float[] origins = { 180f, 270f, 0f, 90f };
                    float   origin  = origins[el.FillOrigin % 4];
                    float   sweep   = amount * 180f;
                    float   start   = el.Clockwise ? origin : origin - sweep;
                    return $"conic-gradient(from {start:F1}deg,{filled} 0deg,{filled} {sweep:F1}deg,{empty} {sweep:F1}deg)";
                }
                case Image.FillMethod.Radial90:
                {
                    // Unity OriginRadial90: 0=BottomLeft, 1=TopLeft, 2=TopRight, 3=BottomRight
                    float[] origins = { 180f, 270f, 0f, 90f };
                    float   origin  = origins[el.FillOrigin % 4];
                    float   sweep   = amount * 90f;
                    float   start   = el.Clockwise ? origin : origin - sweep;
                    return $"conic-gradient(from {start:F1}deg,{filled} 0deg,{filled} {sweep:F1}deg,{empty} {sweep:F1}deg)";
                }
                case Image.FillMethod.Horizontal:
                {
                    // Unity OriginHorizontal: 0=Left, 1=Right
                    float pct = amount * 100f;
                    if (el.FillOrigin == 0) // Left → Right
                        return $"linear-gradient(to right,{filled} {pct:F1}%,{empty} {pct:F1}%)";
                    else                    // Right → Left
                        return $"linear-gradient(to left,{filled} {pct:F1}%,{empty} {pct:F1}%)";
                }
                case Image.FillMethod.Vertical:
                {
                    // Unity OriginVertical: 0=Bottom, 1=Top
                    float pct = amount * 100f;
                    if (el.FillOrigin == 0) // Bottom → Top
                        return $"linear-gradient(to top,{filled} {pct:F1}%,{empty} {pct:F1}%)";
                    else                    // Top → Bottom
                        return $"linear-gradient(to bottom,{filled} {pct:F1}%,{empty} {pct:F1}%)";
                }
                default:
                    return $"linear-gradient({filled},{filled})";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        private static string CssRgba(Color c) =>
            $"rgba({R(c)},{G(c)},{B(c)},{c.a:F3})";

        private static bool IsInvisible(Color c) => c.a < 0.01f;

        // White tint = no visual change on sprite, skip multiply blend
        private static bool IsWhite(Color c) =>
            c.r > 0.99f && c.g > 0.99f && c.b > 0.99f;

        private static string SrcRef(string p) => AssetCopier.SrcRef(p);

        private static string HtmlEsc(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static string AttrEsc(string s) =>
            HtmlEsc(s).Replace("\"", "&quot;").Replace("'", "&#39;");

        private static string Pad(int n)  => new string(' ', n * 4);

        private static string SafeId(string s) =>
            string.IsNullOrEmpty(s) ? "el" :
            System.Text.RegularExpressions.Regex.Replace(s, @"[^a-zA-Z0-9_\-]", "_");

        private static string ElementId(UiElement el) =>
            string.IsNullOrEmpty(el.Id) ? SafeId(el.Name) : el.Id;

        private static int R(Color c) => Mathf.RoundToInt(c.r * 255);
        private static int G(Color c) => Mathf.RoundToInt(c.g * 255);
        private static int B(Color c) => Mathf.RoundToInt(c.b * 255);
    }
}
