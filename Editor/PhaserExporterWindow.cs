using UnityEngine;
using UnityEditor;
using System.IO;

namespace Mabar.PhaserExporter.Editor
{
    public class PhaserExporterWindow : EditorWindow
    {
        // ── State ────────────────────────────────────────────────────────────
        private enum Tab { Settings, Export }
        private Tab     _tab;
        private Vector2 _scroll;
        private string  _lastExportPath = "";
        private bool    _isExporting;

        private PhaserProjectSettings _settings;

        private static readonly string[] PhaserVersions =
            { "3.88.0", "3.80.1", "3.60.0", "3.55.2" };
        private int _phaserVersionIndex;

        // ── Styles (lazy) ────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _richLabelStyle;
        private GUIStyle _statusStyle;
        private bool     _stylesBuilt;

        // ── Menu ─────────────────────────────────────────────────────────────
        [MenuItem("MABAR/PhaserJS Exporter")]
        public static void Open()
        {
            var win = GetWindow<PhaserExporterWindow>("PhaserJS Exporter");
            win.minSize = new Vector2(460, 420);
            win.Show();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void OnEnable() => LoadOrCreateSettings();

        // ── GUI ───────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            BuildStylesOnce();

            if (_settings == null) { LoadOrCreateSettings(); return; }

            DrawHeader();
            DrawTabBar();

            EditorGUILayout.Space(6);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_tab == Tab.Settings) DrawSettingsTab();
            else                      DrawExportTab();
            EditorGUILayout.EndScrollView();
        }

        // ── Header ────────────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("PhaserJS HTML5 Exporter", _headerStyle, GUILayout.Height(30));
            EditorGUILayout.LabelField("by MABAR Creative", _subtitleStyle);
            EditorGUILayout.Space(4);
            Separator();
        }

        // ── Tab bar ───────────────────────────────────────────────────────────
        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var btn = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 28, fontSize = 12 };
            if (GUILayout.Toggle(_tab == Tab.Settings, "  Settings  ", btn, GUILayout.Width(130)))
                _tab = Tab.Settings;
            if (GUILayout.Toggle(_tab == Tab.Export,   "  Export    ", btn, GUILayout.Width(130)))
                _tab = Tab.Export;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ── Settings tab ──────────────────────────────────────────────────────
        private void DrawSettingsTab()
        {
            EditorGUI.BeginChangeCheck();

            Section("Game Info", () =>
            {
                _settings.gameName = EditorGUILayout.TextField("Game Title", _settings.gameName);
            });

            Section("Canvas Size", () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Width", GUILayout.Width(EditorGUIUtility.labelWidth));
                _settings.width = EditorGUILayout.IntField(_settings.width, GUILayout.Width(72));
                GUILayout.Space(12);
                EditorGUILayout.LabelField("Height", GUILayout.Width(46));
                _settings.height = EditorGUILayout.IntField(_settings.height, GUILayout.Width(72));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            });

            Section("Appearance", () =>
            {
                _settings.backgroundColor = EditorGUILayout.ColorField("Background Color", _settings.backgroundColor);
            });

            Section("PhaserJS", () =>
            {
                int idx = EditorGUILayout.Popup("Version", _phaserVersionIndex, PhaserVersions);
                if (idx != _phaserVersionIndex)
                {
                    _phaserVersionIndex = idx;
                    _settings.phaserVersion = PhaserVersions[idx];
                }
                EditorGUILayout.HelpBox("Loaded from CDN (jsDelivr). No download needed.", MessageType.Info);
            });

            Section("Output", () =>
            {
                EditorGUILayout.BeginHorizontal();
                _settings.outputPath = EditorGUILayout.TextField("Output Path", _settings.outputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(62)))
                {
                    string picked = EditorUtility.OpenFolderPanel("Select Output Folder", _settings.outputPath, "");
                    if (!string.IsNullOrEmpty(picked))
                        _settings.outputPath = picked;
                }
                EditorGUILayout.EndHorizontal();

                _settings.autoOpenBrowser = EditorGUILayout.Toggle("Open Browser After Export", _settings.autoOpenBrowser);
            });

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
            }
        }

        // ── Export tab ────────────────────────────────────────────────────────
        private void DrawExportTab()
        {
            Section("Build Summary", () =>
            {
                EditorGUILayout.LabelField($"<b>Game:</b>       {_settings.gameName}", _richLabelStyle);
                EditorGUILayout.LabelField($"<b>Resolution:</b> {_settings.width} × {_settings.height}", _richLabelStyle);
                EditorGUILayout.LabelField($"<b>PhaserJS:</b>   v{_settings.phaserVersion}", _richLabelStyle);
                EditorGUILayout.LabelField($"<b>Output:</b>     {(string.IsNullOrEmpty(_settings.outputPath) ? "<not set>" : _settings.outputPath)}", _richLabelStyle);
            });

            EditorGUILayout.Space(10);

            bool ready = !_isExporting && !string.IsNullOrEmpty(_settings.outputPath);

            GUI.enabled = ready;
            if (GUILayout.Button(_isExporting ? "Exporting..." : "Export to HTML5", GUILayout.Height(42)))
                DoExport();

            EditorGUILayout.Space(4);

            bool canPreview = !_isExporting && !string.IsNullOrEmpty(_lastExportPath) &&
                              File.Exists(Path.Combine(_lastExportPath, "index.html"));
            GUI.enabled = canPreview;
            if (GUILayout.Button("Preview in Browser", GUILayout.Height(32)))
                OpenPreview();

            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_lastExportPath))
            {
                EditorGUILayout.Space(10);
                Separator();
                EditorGUILayout.LabelField($"Last export  →  {_lastExportPath}", _statusStyle);
            }
        }

        // ── Export logic ──────────────────────────────────────────────────────
        private void DoExport()
        {
            _isExporting = true;
            Repaint();
            try
            {
                PhaserExporter.Export(_settings);
                _lastExportPath = _settings.outputPath;

                if (_settings.autoOpenBrowser)
                    OpenPreview();
                else
                {
                    bool open = EditorUtility.DisplayDialog(
                        "Export Successful",
                        $"HTML5 files saved to:\n{_settings.outputPath}",
                        "Open Folder", "OK");
                    if (open) EditorUtility.RevealInFinder(_settings.outputPath);
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed", ex.Message, "OK");
                Debug.LogError($"[PhaserExporter] {ex}");
            }
            finally
            {
                _isExporting = false;
                Repaint();
            }
        }

        private void OpenPreview()
        {
            string index = Path.Combine(_lastExportPath, "index.html");
            if (File.Exists(index))
                Application.OpenURL("file://" + index.Replace("\\", "/"));
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void Section(string title, System.Action body)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(title, _sectionTitleStyle);
            EditorGUI.indentLevel++;
            body();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
            Separator();
        }

        private void Separator()
        {
            var r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.28f, 0.28f, 0.28f));
        }

        private void BuildStylesOnce()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 17,
                alignment = TextAnchor.MiddleCenter
            };
            _subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment       = TextAnchor.MiddleCenter,
                normal          = { textColor = Color.gray }
            };
            _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.65f, 0.88f, 1f) }
            };
            _richLabelStyle = new GUIStyle(EditorStyles.label) { richText = true };
            _statusStyle    = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.4f, 0.85f, 0.4f) }
            };
        }

        // ── Settings persistence ──────────────────────────────────────────────
        private void LoadOrCreateSettings()
        {
            const string path = "Assets/PhaserExporterSettings.asset";
            _settings = AssetDatabase.LoadAssetAtPath<PhaserProjectSettings>(path);

            if (_settings == null)
            {
                _settings = CreateInstance<PhaserProjectSettings>();
                _settings.gameName   = Application.productName;
                _settings.outputPath = Path.GetFullPath(
                    Path.Combine(Application.dataPath, "..", "HTML5Build"));

                AssetDatabase.CreateAsset(_settings, path);
                AssetDatabase.SaveAssets();
            }

            for (int i = 0; i < PhaserVersions.Length; i++)
                if (PhaserVersions[i] == _settings.phaserVersion) { _phaserVersionIndex = i; break; }
        }
    }
}
