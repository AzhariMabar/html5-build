using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

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
                CopyAndCompress(assetPath, dest);
            }
        }

        // Load the texture via Unity's asset pipeline (respects import settings: max size,
        // compression format, etc.), blit to a readable buffer, and re-encode as PNG.
        // Falls back to a plain file copy for non-texture assets.
        private static void CopyAndCompress(string assetPath, string destPath)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                File.Copy(assetPath, destPath, overwrite: true);
                return;
            }

            var tmp = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, tmp);
            var readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active;
            RenderTexture.active = tmp;
            readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            tmp.Release();

            File.WriteAllBytes(destPath, readable.EncodeToPNG());
            Object.DestroyImmediate(readable);
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
