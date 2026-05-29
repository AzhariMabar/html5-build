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
            sb.AppendLine("(function () {");
            sb.AppendLine($"    var REF_W = {m.ReferenceWidth};");
            sb.AppendLine($"    var REF_H = {m.ReferenceHeight};");
            sb.AppendLine();
            sb.AppendLine("    function resize() {");
            sb.AppendLine("        var wrapper  = document.getElementById('_wrapper');");
            sb.AppendLine("        var scaleX   = window.innerWidth  / REF_W;");
            sb.AppendLine("        var scaleY   = window.innerHeight / REF_H;");
            sb.AppendLine("        var scale    = Math.min(scaleX, scaleY);   // FIT (no crop)");
            sb.AppendLine("        var offsetX  = (window.innerWidth  - REF_W * scale) / 2;");
            sb.AppendLine("        var offsetY  = (window.innerHeight - REF_H * scale) / 2;");
            sb.AppendLine("        wrapper.style.transform       = 'scale(' + scale + ')';");
            sb.AppendLine("        wrapper.style.transformOrigin = '0 0';");
            sb.AppendLine("        wrapper.style.left            = offsetX + 'px';");
            sb.AppendLine("        wrapper.style.top             = offsetY + 'px';");
            sb.AppendLine("        wrapper.style.width           = REF_W + 'px';");
            sb.AppendLine("        wrapper.style.height          = REF_H + 'px';");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    window.addEventListener('resize', resize);");
            sb.AppendLine("    window.addEventListener('load',   resize);");
            sb.AppendLine("    resize();");
            sb.AppendLine("})();");
            sb.AppendLine();
            sb.AppendLine("// ─── Button handlers ─────────────────────────────────────────────────────");
            sb.AppendLine("// Each button exported from Unity gets a handler stub below.");

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
