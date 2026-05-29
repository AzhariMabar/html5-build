using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Html5Build.Editor
{
    public static class AssetCopier
    {
        public static void CopyAll(CanvasModel model, string srcDir)
        {
            var paths = new HashSet<string>();
            Collect(model.Children, paths);

            foreach (string assetPath in paths)
            {
                if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) continue;
                string dest = Path.Combine(srcDir, Path.GetFileName(assetPath));
                File.Copy(assetPath, dest, overwrite: true);
            }
        }

        // Returns "src/filename.ext" for use in HTML, or null
        public static string SrcRef(string assetPath) =>
            string.IsNullOrEmpty(assetPath) ? null : "src/" + Path.GetFileName(assetPath);

        private static void Collect(List<UiElement> elements, HashSet<string> paths)
        {
            foreach (var el in elements)
            {
                if (!string.IsNullOrEmpty(el.SpritePath))
                    paths.Add(el.SpritePath);
                Collect(el.Children, paths);
            }
        }
    }
}
