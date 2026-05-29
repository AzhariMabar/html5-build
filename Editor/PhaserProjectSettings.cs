using UnityEngine;

namespace Mabar.PhaserExporter.Editor
{
    public class PhaserProjectSettings : ScriptableObject
    {
        public string gameName = "My PhaserJS Game";
        public int width = 800;
        public int height = 600;
        public Color backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        public string phaserVersion = "3.88.0";
        public string outputPath = "";
        public bool autoOpenBrowser = true;
    }
}
