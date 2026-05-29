using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
            sb.AppendLine($"    <title>{Esc(m.GameName)}</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }");
            sb.AppendLine("        body { background: #000; overflow: hidden; }");
            sb.AppendLine("        #_wrapper { position: absolute; transform-origin: top left; }");
            sb.AppendLine($"        #_app {{ position: relative; width: {m.ReferenceWidth}px; height: {m.ReferenceHeight}px; overflow: hidden; background: {CssBg(m.BackgroundColor)}; }}");
            sb.AppendLine("        .ui { position: absolute; }");
            sb.AppendLine("        .ui-btn { border: none; outline: none; background: transparent; cursor: pointer; padding: 0; }");
            sb.AppendLine("        .ui-img > img { width: 100%; height: 100%; object-fit: fill; display: block; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div id=\"_wrapper\">");
            sb.AppendLine("        <div id=\"_app\">");
            WriteElements(sb, m.Children, indent: 3);
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <script src=\"app.js\"></script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        // ─── element writer ───────────────────────────────────────────────────
        private static void WriteElements(StringBuilder sb, List<UiElement> elements, int indent)
        {
            string pad = Pad(indent);
            foreach (var el in elements)
            {
                string id    = SafeId(el.Name);
                string style = BuildStyle(el);
                string src   = AssetCopier.SrcRef(el.SpritePath);

                if (el.Type == "button")
                {
                    sb.AppendLine($"{pad}<button id=\"{id}\" class=\"ui ui-btn\" style=\"{style}\">");
                    if (src != null)
                        sb.AppendLine($"{pad}    <img src=\"{src}\" alt=\"\" style=\"width:100%;height:100%;\" />");
                    WriteElements(sb, el.Children, indent + 1);
                    sb.AppendLine($"{pad}</button>");
                }
                else if (src != null)
                {
                    sb.AppendLine($"{pad}<div id=\"{id}\" class=\"ui ui-img\" style=\"{style}\">");
                    sb.AppendLine($"{pad}    <img src=\"{src}\" alt=\"\" />");
                    WriteElements(sb, el.Children, indent + 1);
                    sb.AppendLine($"{pad}</div>");
                }
                else
                {
                    sb.AppendLine($"{pad}<div id=\"{id}\" class=\"ui\" style=\"{style}\">");
                    WriteElements(sb, el.Children, indent + 1);
                    sb.AppendLine($"{pad}</div>");
                }
            }
        }

        // ─── style builder ────────────────────────────────────────────────────
        private static string BuildStyle(UiElement el)
        {
            var sb = new StringBuilder();

            // Position
            if (el.IsStretchX && el.IsStretchY)
            {
                // full-stretch shortcut: resolve to pixel values computed by SceneReader
                sb.Append($"left:{Px(el.CssLeft)};top:{Px(el.CssTop)};width:{Px(el.CssWidth)};height:{Px(el.CssHeight)};");
            }
            else if (el.IsStretchX)
            {
                sb.Append($"left:{Px(el.CssLeft)};top:{Px(el.CssTop)};width:{Px(el.CssWidth)};height:{Px(el.CssHeight)};");
            }
            else if (el.IsStretchY)
            {
                sb.Append($"left:{Px(el.CssLeft)};top:{Px(el.CssTop)};width:{Px(el.CssWidth)};height:{Px(el.CssHeight)};");
            }
            else
            {
                sb.Append($"left:{Px(el.CssLeft)};top:{Px(el.CssTop)};width:{Px(el.CssWidth)};height:{Px(el.CssHeight)};");
            }

            // Background (only when no sprite image)
            string src = AssetCopier.SrcRef(el.SpritePath);
            if (src == null && !IsTransparent(el.Color))
                sb.Append($"background:{CssBg(el.Color)};");

            return sb.ToString();
        }

        // ─── helpers ──────────────────────────────────────────────────────────
        private static string Px(float v)    => $"{v:F0}px";
        private static string Pad(int n)     => new string(' ', n * 4);
        private static string Esc(string s)  => s?.Replace("&", "&amp;").Replace("<", "&lt;") ?? "";
        private static string SafeId(string s) =>
            System.Text.RegularExpressions.Regex.Replace(s ?? "el", @"[^a-zA-Z0-9_\-]", "_");

        private static string CssBg(Color c) =>
            $"rgba({R(c)},{G(c)},{B(c)},{c.a:F3})";

        private static bool IsTransparent(Color c) => c.a < 0.01f;

        private static int R(Color c) => Mathf.RoundToInt(c.r * 255);
        private static int G(Color c) => Mathf.RoundToInt(c.g * 255);
        private static int B(Color c) => Mathf.RoundToInt(c.b * 255);
    }
}
