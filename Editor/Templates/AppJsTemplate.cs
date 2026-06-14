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
            sb.AppendLine("        var w = document.documentElement.clientWidth;");
            sb.AppendLine("        var h = document.documentElement.clientHeight;");
            sb.AppendLine("        var scale = Math.min(w / REF_W, h / REF_H);");
            sb.AppendLine("        document.documentElement.style.setProperty('--rs', scale);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    window.addEventListener('resize', resize);");
            sb.AppendLine("    window.addEventListener('load',   resize);");
            sb.AppendLine("    resize();");
            sb.AppendLine("})();");
            sb.AppendLine();

            sb.AppendLine("// ─── Native Unity UI controls ────────────────────────────────────────────");
            sb.AppendLine("window.getUIValue = function (id) {");
            sb.AppendLine("    var el = document.getElementById(id);");
            sb.AppendLine("    if (!el) return undefined;");
            sb.AppendLine("    if (el.dataset.unityControl === 'toggle') return !!el.querySelector('input')?.checked;");
            sb.AppendLine("    if (el.type === 'range' || el.type === 'number') return Number(el.value);");
            sb.AppendLine("    return el.value;");
            sb.AppendLine("};");
            sb.AppendLine();
            sb.AppendLine("window.setUIValue = function (id, value) {");
            sb.AppendLine("    var el = document.getElementById(id);");
            sb.AppendLine("    if (!el) return false;");
            sb.AppendLine("    if (el.dataset.unityControl === 'toggle') {");
            sb.AppendLine("        var checkbox = el.querySelector('input');");
            sb.AppendLine("        if (checkbox) checkbox.checked = !!value;");
            sb.AppendLine("    } else {");
            sb.AppendLine("        el.value = value;");
            sb.AppendLine("    }");
            sb.AppendLine("    el.dispatchEvent(new Event('change', { bubbles: true }));");
            sb.AppendLine("    return true;");
            sb.AppendLine("};");
            sb.AppendLine();
            sb.AppendLine("document.querySelectorAll('[data-unity-control]').forEach(function (el) {");
            sb.AppendLine("    var source = el.dataset.unityControl === 'toggle' ? el.querySelector('input') : el;");
            sb.AppendLine("    if (!source) return;");
            sb.AppendLine("    source.addEventListener('input', function () {");
            sb.AppendLine("        el.dispatchEvent(new CustomEvent('unity-input', { detail: window.getUIValue(el.id) }));");
            sb.AppendLine("    });");
            sb.AppendLine("    source.addEventListener('change', function () {");
            sb.AppendLine("        el.dispatchEvent(new CustomEvent('unity-change', { detail: window.getUIValue(el.id) }));");
            sb.AppendLine("    });");
            sb.AppendLine("    source.addEventListener('blur', function () {");
            sb.AppendLine("        el.dispatchEvent(new CustomEvent('unity-end-edit', { detail: window.getUIValue(el.id) }));");
            sb.AppendLine("    });");
            sb.AppendLine("});");
            sb.AppendLine();

            var controls = new List<UiElement>();
            CollectControls(m.Children, controls);
            foreach (var control in controls)
            {
                string id = ElementId(control);
                if (control.OnValueChangedCalls.Count > 0)
                {
                    sb.AppendLine($"document.getElementById('{id}')?.addEventListener('unity-change', function (event) {{");
                    foreach (var call in control.OnValueChangedCalls)
                        sb.AppendLine($"    {ValueChangedCallToJs(call, "event.detail")}");
                    sb.AppendLine("});");
                }

                if (control.OnEndEditCalls.Count > 0)
                {
                    sb.AppendLine($"document.getElementById('{id}')?.addEventListener('unity-end-edit', function (event) {{");
                    foreach (var call in control.OnEndEditCalls)
                        sb.AppendLine($"    {ValueChangedCallToJs(call, "event.detail")}");
                    sb.AppendLine("});");
                }
            }
            if (controls.Count > 0) sb.AppendLine();

            // ── Panel helpers ──────────────────────────────────────────────
            var panels = new List<UiElement>();
            CollectPanels(m.Children, panels);

            if (panels.Count > 0)
            {
                sb.AppendLine("// ─── Panel helpers ──────────────────────────────────────────────────────");
                sb.AppendLine("// Panels in scene:");
                foreach (var p in panels)
                    sb.AppendLine($"//   #{ElementId(p)}{(!p.Active ? " (initially hidden)" : "")}");
                sb.AppendLine();
                sb.AppendLine("function showPanel(id) {");
                sb.AppendLine("    document.querySelectorAll('#_app > [id]').forEach(function(el) {");
                sb.AppendLine("        el.style.display = 'none';");
                sb.AppendLine("    });");
                sb.AppendLine("    var el = document.getElementById(id);");
                sb.AppendLine("    if (el) el.style.display = '';");
                sb.AppendLine("}");
                sb.AppendLine();
            }

            // ── Button handlers ────────────────────────────────────────────
            sb.AppendLine("// ─── Button handlers ─────────────────────────────────────────────────────");

            var buttons = new List<UiElement>();
            CollectButtons(m.Children, buttons);
            foreach (var btn in buttons)
            {
                string id   = ElementId(btn);
                string body = BuildClickBody(btn);
                sb.AppendLine($"function {id}_onClick() {{ {body} }}");
                sb.AppendLine($"document.getElementById('{id}')?.addEventListener('click', {id}_onClick);");
                sb.AppendLine();
            }

            // ── Tween animations ───────────────────────────────────────────
            var tweened = new List<(string id, TweenConfig cfg)>();
            CollectTweened(m.Children, tweened);
            if (tweened.Count > 0)
            {
                sb.AppendLine("// ─── Tween animations (auto-generated from Tweening components) ──────────");
                foreach (var (id, cfg) in tweened)
                {
                    sb.AppendLine($"new Tweening('#{id}', {{");
                    sb.AppendLine($"    autoPlay: {(cfg.AutoPlay ? "true" : "false")},");
                    if (cfg.Loop)      sb.AppendLine("    loop: true,");
                    if (cfg.Yoyo)      sb.AppendLine("    yoyo: true,");
                    if (cfg.Repeat > 0) sb.AppendLine($"    repeat: {cfg.Repeat},");
                    sb.AppendLine("    tweenData: [");
                    foreach (var td in cfg.TweenData)
                    {
                        sb.AppendLine("        {");
                        sb.AppendLine($"            type: '{td.Type}',");
                        sb.AppendLine($"            startPoint: {{ x: {td.StartPoint.x:F2}, y: {td.StartPoint.y:F2}, z: {td.StartPoint.z:F2} }},");
                        if (td.Loop) sb.AppendLine("            loop: true,");
                        if (td.Yoyo) sb.AppendLine("            yoyo: true,");
                        sb.AppendLine("            sequence: [");
                        foreach (var p in td.Sequence)
                        {
                            sb.AppendLine("                {");
                            sb.AppendLine($"                    duration: {p.Duration:F2},");
                            sb.AppendLine($"                    delay: {p.Delay:F2},");
                            sb.AppendLine($"                    ease: '{p.Ease}',");
                            sb.AppendLine($"                    wayPoint: {{ x: {p.WayPoint.x:F2}, y: {p.WayPoint.y:F2}, z: {p.WayPoint.z:F2} }},");
                            sb.AppendLine("                },");
                        }
                        sb.AppendLine("            ],");
                        sb.AppendLine("        },");
                    }
                    sb.AppendLine("    ]");
                    sb.AppendLine("});");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static void CollectPanels(List<UiElement> elements, List<UiElement> out_)
        {
            foreach (var el in elements)
                if (el.Type == "image" || el.Type == "container")
                    out_.Add(el);
        }

        // Priority: HtmlAction (explicit override) → OnClickCalls (auto-read from inspector) → TODO
        private static string BuildClickBody(UiElement btn)
        {
            // Explicit HtmlAction override takes priority
            if (!string.IsNullOrEmpty(btn.HtmlAction))
                return ActionToJs(btn.HtmlAction);

            // Auto-translate Unity inspector OnClick wiring
            if (btn.OnClickCalls != null && btn.OnClickCalls.Count > 0)
            {
                var parts = new System.Collections.Generic.List<string>();
                foreach (var c in btn.OnClickCalls)
                {
                    string js = OnClickCallToJs(c);
                    if (!string.IsNullOrEmpty(js)) parts.Add(js);
                }
                if (parts.Count > 0) return string.Join(" ", parts);
            }

            return "/* TODO */";
        }

        // Translates one Unity persistent call to a JS statement.
        //
        // Known Unity built-ins are mapped automatically:
        //   GameObject.SetActive(true/false)  → show/hide element by ID
        //   Application.OpenURL(url)          → window.open(url, '_blank')
        //
        // Custom methods (your C# code):
        //   myScript.sebuahFunction("value")  → sebuahFunction('value')
        //   — You write the matching JS function in app.js / a custom file.
        private static string OnClickCallToJs(OnClickCall c)
        {
            switch (c.MethodName)
            {
                case "SetActive":
                    if (string.IsNullOrEmpty(c.TargetName)) return "";
                    return c.BoolArg
                        ? $"document.getElementById('{c.TargetName}').style.display = '';"
                        : $"document.getElementById('{c.TargetName}').style.display = 'none';";

                case "OpenURL":
                case "Open":       // LinkButton.Open() — url read from component field
                case "OpenUrl":
                    return $"window.open('{c.StringArg}', '_blank');";

                default:
                    // Custom C# method → call as JS function by the same name.
                    // Write a matching JS function in app.js or a custom script.
                    if (!string.IsNullOrEmpty(c.StringArg))
                        return $"{c.MethodName}('{c.StringArg}');";
                    if (c.IntArg != 0)
                        return $"{c.MethodName}({c.IntArg});";
                    if (System.Math.Abs(c.FloatArg) > 0.0001f)
                        return $"{c.MethodName}({c.FloatArg:F4});";
                    return $"{c.MethodName}();";
            }
        }

        private static string ValueChangedCallToJs(OnClickCall c, string valueExpression)
        {
            if (c.MethodName == "SetActive")
                return c.Mode == 0
                    ? $"document.getElementById('{c.TargetName}').style.display = {valueExpression} ? '' : 'none';"
                    : OnClickCallToJs(c);

            // EventDefined means Unity passes the control's current value.
            if (c.Mode == 0)
                return $"{c.MethodName}({valueExpression});";

            return OnClickCallToJs(c);
        }

        // Converts HtmlAction string to a JS statement (explicit override).
        //   panel:ID              → showPanel('ID')
        //   url:https://...       → window.open('https://...', '_blank')
        //   js:expression()       → expression()   (raw, passed through)
        private static string ActionToJs(string action)
        {
            if (action.StartsWith("panel:"))
                return $"showPanel('{action.Substring(6)}');";
            if (action.StartsWith("url:"))
                return $"window.open('{action.Substring(4)}', '_blank');";
            if (action.StartsWith("js:"))
                return action.Substring(3).TrimStart();
            return $"showPanel('{action}');";
        }

        private static void CollectButtons(List<UiElement> elements, List<UiElement> out_)
        {
            foreach (var el in elements)
            {
                if (el.Type == "button") out_.Add(el);
                CollectButtons(el.Children, out_);
            }
        }

        private static void CollectControls(List<UiElement> elements, List<UiElement> out_)
        {
            foreach (var el in elements)
            {
                if (el.Type == "input" || el.Type == "slider" || el.Type == "toggle" || el.Type == "dropdown")
                    out_.Add(el);
                CollectControls(el.Children, out_);
            }
        }

        private static void CollectTweened(List<UiElement> elements, List<(string, TweenConfig)> out_)
        {
            foreach (var el in elements)
            {
                if (el.TweenConfig != null)
                    out_.Add((ElementId(el), el.TweenConfig));
                CollectTweened(el.Children, out_);
            }
        }

        private static string SafeId(string s) =>
            string.IsNullOrEmpty(s) ? "el" :
            System.Text.RegularExpressions.Regex.Replace(s, @"[^a-zA-Z0-9_\-]", "_");

        private static string ElementId(UiElement el) =>
            string.IsNullOrEmpty(el.Id) ? SafeId(el.Name) : el.Id;
    }
}
