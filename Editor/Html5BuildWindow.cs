using System.IO;
using UnityEngine;
using UnityEditor;

namespace Html5Build.Editor
{
    public class Html5BuildWindow : EditorWindow
    {
        private enum Tab { Scene, Build }

        // ── state ─────────────────────────────────────────────────────────────
        private Tab         _tab;
        private Vector2     _scroll;
        private CanvasModel _model;
        private string      _folderName   = "build";
        private string      _lastBuildPath;
        private bool        _busy;
        private string      _status;
        private bool        _statusOk;

        // ── styles (built lazily after first layout) ──────────────────────────
        private GUIStyle _styHeader;
        private GUIStyle _stySub;
        private GUIStyle _stySection;
        private GUIStyle _styMono;
        private GUIStyle _styOk;
        private GUIStyle _styErr;
        private bool     _stylesReady;

        // ── menu ──────────────────────────────────────────────────────────────
        [MenuItem("HTML5/Build")]
        public static void Open()
        {
            var w = GetWindow<Html5BuildWindow>("HTML5 Build");
            w.minSize = new Vector2(440, 380);
            w.Show();
        }

        // ── GUI ───────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();
            DrawTabs();

            EditorGUILayout.Space(6);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_tab == Tab.Scene) DrawSceneTab();
            else                   DrawBuildTab();
            EditorGUILayout.EndScrollView();
        }

        // ── Header ────────────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("HTML5 Build", _styHeader, GUILayout.Height(28));
            EditorGUILayout.LabelField("Unity Canvas  →  HTML5", _stySub);
            EditorGUILayout.Space(4);
            HRule();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var s = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 27, fontSize = 12 };
            if (GUILayout.Toggle(_tab == Tab.Scene, "  Scene  ", s, GUILayout.Width(110))) _tab = Tab.Scene;
            if (GUILayout.Toggle(_tab == Tab.Build, "  Build  ", s, GUILayout.Width(110))) _tab = Tab.Build;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ── Scene tab ─────────────────────────────────────────────────────────
        private void DrawSceneTab()
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Scan Active Scene", GUILayout.Height(34)))
                DoScan();

            if (_model == null)
            {
                EditorGUILayout.HelpBox("Click Scan to read the Canvas from the open scene.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);
            Section("Canvas", () =>
            {
                Field("Reference", $"{_model.ReferenceWidth} × {_model.ReferenceHeight} px");
                Field("Background", ColorHex(_model.BackgroundColor));
                Field("Elements", $"{CountAll(_model.Children)}");
            });

            Section("Hierarchy", () =>
            {
                foreach (var el in _model.Children)
                    DrawRow(el, 0);
            });

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space(4);
                HRule();
                EditorGUILayout.LabelField(_status, _statusOk ? _styOk : _styErr);
            }
        }

        private void DrawRow(UiElement el, int depth)
        {
            string pad  = new string(' ', depth * 3);
            string icon = el.Type switch
            {
                "button"    => "BTN",
                "image"     => "IMG",
                "container" => "DIV",
                _           => "   "
            };
            string pos = $"L{el.CssLeft:F0} T{el.CssTop:F0}  {el.CssWidth:F0}×{el.CssHeight:F0}";
            EditorGUILayout.LabelField($"{pad}[{icon}] {el.Name}", pos, EditorStyles.miniLabel);
            foreach (var c in el.Children)
                DrawRow(c, depth + 1);
        }

        // ── Build tab ─────────────────────────────────────────────────────────
        private void DrawBuildTab()
        {
            Section("Output", () =>
            {
                _folderName = EditorGUILayout.TextField("Folder Name", _folderName);
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string fullPath    = Path.Combine(projectRoot, _folderName.Trim());
                EditorGUILayout.LabelField("Path", fullPath, EditorStyles.miniLabel);
            });

            EditorGUILayout.Space(4);
            Section("Output Structure", () =>
            {
                EditorGUILayout.LabelField("/" + _folderName,  EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  index.html",     EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  app.js",         EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  /src",           EditorStyles.miniLabel);
                EditorGUILayout.LabelField("    (images, audio, ...)", EditorStyles.miniLabel);
            });

            EditorGUILayout.Space(8);

            if (_model == null)
                EditorGUILayout.HelpBox("Go to the Scene tab and Scan first.", MessageType.Warning);

            GUI.enabled = _model != null && !_busy;
            if (GUILayout.Button(_busy ? "Building..." : "Build HTML5", GUILayout.Height(42)))
                DoBuild();
            GUI.enabled = true;

            EditorGUILayout.Space(4);
            bool canOpen = !string.IsNullOrEmpty(_lastBuildPath) &&
                           File.Exists(Path.Combine(_lastBuildPath, "index.html"));
            GUI.enabled = canOpen;
            if (GUILayout.Button("Open in Browser", GUILayout.Height(30)))
                Application.OpenURL("file://" + Path.Combine(_lastBuildPath, "index.html").Replace("\\", "/"));
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space(8);
                HRule();
                EditorGUILayout.LabelField(_status, _statusOk ? _styOk : _styErr);
            }
        }

        // ── actions ───────────────────────────────────────────────────────────
        private void DoScan()
        {
            try
            {
                _model     = SceneReader.ReadActiveScene();
                _status    = $"OK — {_model.ReferenceWidth}×{_model.ReferenceHeight}, {CountAll(_model.Children)} elements";
                _statusOk  = true;
            }
            catch (System.Exception ex)
            {
                _model    = null;
                _status   = ex.Message;
                _statusOk = false;
                Debug.LogError($"[HTML5 Build] Scan failed: {ex}");
            }
            Repaint();
        }

        private void DoBuild()
        {
            _busy = true;
            Repaint();
            try
            {
                // Always re-scan for freshest data
                _model = SceneReader.ReadActiveScene();

                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string outPath     = Path.Combine(projectRoot, _folderName.Trim());

                Html5Exporter.Export(_model, outPath);

                _lastBuildPath = outPath;
                _status        = $"Build complete  →  {outPath}";
                _statusOk      = true;
            }
            catch (System.Exception ex)
            {
                _status   = ex.Message;
                _statusOk = false;
                Debug.LogError($"[HTML5 Build] Build failed: {ex}");
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────────
        private void Section(string title, System.Action body)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(title, _stySection);
            EditorGUI.indentLevel++;
            body();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
            HRule();
        }

        private static void Field(string label, string value) =>
            EditorGUILayout.LabelField(label, value, EditorStyles.label);

        private void HRule()
        {
            var r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.28f, 0.28f, 0.28f));
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;
            _styHeader  = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            _stySub     = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.gray } };
            _stySection = new GUIStyle(EditorStyles.boldLabel) { normal   = { textColor = new Color(0.65f, 0.88f, 1f) } };
            _styMono    = new GUIStyle(EditorStyles.miniLabel);
            _styOk      = new GUIStyle(EditorStyles.miniLabel) { normal   = { textColor = new Color(0.4f, 0.85f, 0.4f) } };
            _styErr     = new GUIStyle(EditorStyles.miniLabel) { normal   = { textColor = new Color(1f, 0.4f, 0.4f) } };
        }

        private static int CountAll(System.Collections.Generic.List<UiElement> els)
        {
            int n = els.Count;
            foreach (var e in els) n += CountAll(e.Children);
            return n;
        }

        private static string ColorHex(Color c) =>
            $"#{Mathf.RoundToInt(c.r*255):X2}{Mathf.RoundToInt(c.g*255):X2}{Mathf.RoundToInt(c.b*255):X2}";
    }
}
