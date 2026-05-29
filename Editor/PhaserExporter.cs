using UnityEngine;
using System.IO;

namespace Mabar.PhaserExporter.Editor
{
    public static class PhaserExporter
    {
        public static void Export(PhaserProjectSettings settings)
        {
            Validate(settings);

            string outPath = settings.outputPath;
            Directory.CreateDirectory(outPath);
            Directory.CreateDirectory(Path.Combine(outPath, "assets"));

            File.WriteAllText(Path.Combine(outPath, "index.html"), HtmlTemplate.Generate(settings));
            File.WriteAllText(Path.Combine(outPath, "game.js"),    GameJsTemplate.Generate(settings));

            Debug.Log($"[PhaserExporter] Export complete → {outPath}");
        }

        private static void Validate(PhaserProjectSettings s)
        {
            if (string.IsNullOrWhiteSpace(s.gameName))
                throw new System.Exception("Game name cannot be empty.");
            if (string.IsNullOrWhiteSpace(s.outputPath))
                throw new System.Exception("Output path cannot be empty.");
            if (s.width <= 0 || s.height <= 0)
                throw new System.Exception("Width and height must be greater than 0.");
        }
    }
}
