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

            // --rs = uniform scale that fits the reference canvas inside the viewport (contain).
            // #_app is then centered so the background color fills any remaining space seamlessly.
            sb.AppendLine("(function () {");
            sb.AppendLine($"    var REF_W = {m.ReferenceWidth};");
            sb.AppendLine($"    var REF_H = {m.ReferenceHeight};");
            sb.AppendLine();
            sb.AppendLine("    function resize() {");
            sb.AppendLine("        var w = document.documentElement.clientWidth;");
            sb.AppendLine("        var h = document.documentElement.clientHeight;");
            // min() = "Expand" mode: scale by the more constrained axis.
            // #_app fills the full viewport, so anchors map to viewport edges.
            // Bottom-anchored elements stay at the bottom, top at the top.
            sb.AppendLine("        var scale = Math.min(w / REF_W, h / REF_H);");
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
