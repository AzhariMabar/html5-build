using System;
using System.IO;
using UnityEngine;

namespace Html5Build.Editor
{
    public static class Html5Exporter
    {
        public static void Export(CanvasModel model, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new Exception("Output path is empty.");

            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, recursive: true);
            Directory.CreateDirectory(outputPath);

            string srcPath = Path.Combine(outputPath, "src");
            Directory.CreateDirectory(srcPath);

            AssetCopier.CopyAll(model, srcPath);

            File.WriteAllText(
                Path.Combine(outputPath, "index.html"),
                IndexHtmlTemplate.Generate(model));

            File.WriteAllText(
                Path.Combine(outputPath, "app.js"),
                AppJsTemplate.Generate(model));

            Debug.Log($"[HTML5 Build] Done → {outputPath}");
        }
    }
}
