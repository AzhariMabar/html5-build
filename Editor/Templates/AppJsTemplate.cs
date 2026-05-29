using System.Collections.Generic;
using System.Text;

namespace Html5Build.Editor
{
    public static class AppJsTemplate
    {
        public static string Generate(CanvasModel m)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"// {m.GameName} — HTML5 Build");
            sb.AppendLine($"// Reference: {m.ReferenceWidth}x{m.ReferenceHeight}");
            sb.AppendLine();

            // CSS Custom Property approach:
            // --rs (reference scale) = how much to scale reference-resolution pixels
            // #_app width/height = calc(RefW/H px * var(--rs))
            // Element offsets     = calc(Anchor% ± Offset px * var(--rs))
            // All done via CSS — no transform hacks that cause overflow issues.
            sb.AppendLine("(function () {");
            sb.AppendLine($"    var REF_W = {m.ReferenceWidth};");
            sb.AppendLine($"    var REF_H = {m.ReferenceHeight};");
            sb.AppendLine();
            sb.AppendLine("    function resize() {");
            sb.AppendLine("        var w = document.documentElement.clientWidth;");
            // Scale by WIDTH only — canvas always fills 100% of viewport width.
            // Height follows the aspect ratio (calc(1920px * --rs)) and may overflow
            // vertically; the wrapper clips it with overflow:hidden.
            // When viewport < 1080: scale < 1, all elements shrink proportionally.
            // Anchor positions remain correct relative to their anchor points.
            sb.AppendLine("        var scale = w / REF_W;");
            sb.AppendLine("        document.documentElement.style.setProperty('--rs', scale);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    window.addEventListener('resize', resize);");
            sb.AppendLine("    window.addEventListener('load',   resize);");
            sb.AppendLine("    resize();");
            sb.AppendLine("})();");
            sb.AppendLine();
            sb.AppendLine("// ─── Button handlers ─────────────────────────────────────────────────────");

            var buttons = new List<UiElement>();
            CollectButtons(m.Children, buttons);
            foreach (var btn in buttons)
            {
                string id = System.Text.RegularExpressions.Regex.Replace(btn.Name, @"[^a-zA-Z0-9_]", "_");
                sb.AppendLine($"// function {id}_onClick() {{ /* TODO */ }}");
                sb.AppendLine($"// document.getElementById('{id}')?.addEventListener('click', {id}_onClick);");
            }

            return sb.ToString();
        }

        private static void CollectButtons(List<UiElement> elements, List<UiElement> out_)
        {
            foreach (var el in elements)
            {
                if (el.Type == "button") out_.Add(el);
                CollectButtons(el.Children, out_);
            }
        }
    }
}
