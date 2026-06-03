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
            sb.AppendLine("        .ui { position: absolute; }");
            sb.AppendLine("        .ui-btn { border: none; outline: none; cursor: pointer; padding: 0; }");
            sb.AppendLine("        .ui-img > img { width: 100%; height: 100%; object-fit: fill; display: block; }");
            sb.AppendLine("        .ui-text { overflow: hidden; white-space: pre-wrap; word-break: break-word; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div id=\"_wrapper\">");
            sb.AppendLine("        <div id=\"_app\">");
            WriteElements(sb, m.Children, 3);
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
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
                string id    = SafeId(el.Name);
                string style = BuildStyle(el);

                switch (el.Type)
                {
                    case "button":
                        sb.AppendLine($"{pad}<button id=\"{id}\" class=\"ui ui-btn\" style=\"{style}\">");
                        if (!string.IsNullOrEmpty(el.SpritePath))
                            sb.AppendLine($"{pad}    <img src=\"{SrcRef(el.SpritePath)}\" alt=\"\" style=\"position:absolute;left:0;top:0;width:100%;height:100%;pointer-events:none;\" />");
                        WriteElements(sb, el.Children, indent + 1);
                        sb.AppendLine($"{pad}</button>");
                        break;

                    case "text":
                        sb.AppendLine($"{pad}<div id=\"{id}\" class=\"ui ui-text\" style=\"{style}\">{HtmlEsc(el.TextContent ?? "")}</div>");
                        break;

                    case "image":
                        sb.AppendLine($"{pad}<div id=\"{id}\" class=\"ui ui-img\" style=\"{style}\">");
                        if (!string.IsNullOrEmpty(el.SpritePath))
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
        private static string BuildStyle(UiElement el)
        {
            var sb = new StringBuilder();

            sb.Append($"left:{el.CssLeft};");
            sb.Append($"top:{el.CssTop};");
            sb.Append($"width:{el.CssWidth};");
            sb.Append($"height:{el.CssHeight};");

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

            if (el.Type != "text")
            {
                string src = SrcRef(el.SpritePath);
                if (el.ImageType == Image.Type.Filled)
                {
                    if (src != null)
                    {
                        sb.Append($"background:url('{src}') center/100% 100% no-repeat;");
                        sb.Append(FillMask(el));
                    }
                    else if (!IsInvisible(el.Color))
                        sb.Append(FillBackground(el));
                }
                else if (el.ImageType == Image.Type.Tiled && src != null)
                    sb.Append($"background:url('{src}') repeat;background-size:auto;");
                else
                {
                    if (src != null)
                        sb.Append($"background:url('{src}') center/100% 100% no-repeat;");
                    else if (!IsInvisible(el.Color))
                        sb.Append($"background:{CssRgba(el.Color)};");
                }
            }

            if (el.Type == "text")
            {
                sb.Append("display:flex;");
                sb.Append($"align-items:{el.TextAlignV};");
                sb.Append($"justify-content:{el.TextAlignH};");
                sb.Append($"color:{CssRgba(el.TextColor)};");
                sb.Append($"font-size:calc({el.FontSize:F0}px * var(--rs));");
                sb.Append("font-family:Arial,sans-serif;");
                sb.Append("pointer-events:none;user-select:none;");
            }

            return sb.ToString();
        }

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

        private static string SrcRef(string p) =>
            !string.IsNullOrEmpty(p) ? "src/" + System.IO.Path.GetFileName(p) : null;

        private static string HtmlEsc(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static string Pad(int n)  => new string(' ', n * 4);

        private static string SafeId(string s) =>
            string.IsNullOrEmpty(s) ? "el" :
            System.Text.RegularExpressions.Regex.Replace(s, @"[^a-zA-Z0-9_\-]", "_");

        private static int R(Color c) => Mathf.RoundToInt(c.r * 255);
        private static int G(Color c) => Mathf.RoundToInt(c.g * 255);
        private static int B(Color c) => Mathf.RoundToInt(c.b * 255);
    }
}
