using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Html5Build.Editor
{
    public static class AssetCopier
    {
        // Built during CopyAll; maps assetPath → actual output filename (may differ in extension)
        private static readonly Dictionary<string, string> _srcMap = new Dictionary<string, string>();

        public static void CopyAll(CanvasModel model, string srcDir)
        {
            _srcMap.Clear();
            var paths = new HashSet<string>();
            Collect(model.Children, paths);

            foreach (string assetPath in paths)
            {
                if (string.IsNullOrEmpty(assetPath)) continue;
                string outName = CopyAndCompress(assetPath, srcDir);
                if (outName != null) _srcMap[assetPath] = outName;
            }
        }

        // Returns "src/filename.ext" using the actual output name (which may be .jpg instead of .png)
        public static string SrcRef(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (_srcMap.TryGetValue(assetPath, out string outName))
                return "src/" + outName;
            return "src/" + Path.GetFileName(assetPath);
        }

        // Returns the output filename written to srcDir, or null on failure.
        private static string CopyAndCompress(string assetPath, string srcDir)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                // Non-texture asset (audio, etc.) — plain copy via resolved disk path
                string diskPath = ResolveDiskPath(assetPath);
                if (!File.Exists(diskPath)) return null;
                string fname = Path.GetFileName(assetPath);
                File.Copy(diskPath, Path.Combine(srcDir, fname), overwrite: true);
                return fname;
            }

            // Detect alpha from the source texture format BEFORE blitting,
            // so we don't have to scan every pixel of the decoded image.
            bool hasAlpha = TextureFormatHasAlpha(tex.format);

            var tmp = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, tmp);
            var readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active;
            RenderTexture.active = tmp;
            readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            tmp.Release();

            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            byte[] bytes;
            string outName;

            if (hasAlpha)
            {
                bytes   = readable.EncodeToPNG();
                outName = baseName + ".png";
            }
            else
            {
                bytes   = readable.EncodeToJPG(85);
                outName = baseName + ".jpg";
            }

            Object.DestroyImmediate(readable);
            File.WriteAllBytes(Path.Combine(srcDir, outName), bytes);
            return outName;
        }

        private static bool TextureFormatHasAlpha(TextureFormat fmt)
        {
            switch (fmt)
            {
                case TextureFormat.RGB24:
                case TextureFormat.RGB565:
                case TextureFormat.DXT1:
                case TextureFormat.DXT1Crunched:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC_RGB4Crunched:
                case TextureFormat.ETC2_RGB:
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.BC6H:
                case TextureFormat.R8:
                case TextureFormat.R16:
                case TextureFormat.RFloat:
                case TextureFormat.RHalf:
                    return false;
                default:
                    return true;
            }
        }

        // Resolve an AssetDatabase path (including "Packages/...") to a full disk path.
        private static string ResolveDiskPath(string assetPath)
        {
            string metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);
            if (!string.IsNullOrEmpty(metaPath))
            {
                string candidate = metaPath.Length > 5 ? metaPath.Substring(0, metaPath.Length - 5) : metaPath;
                string full = Path.GetFullPath(candidate);
                if (File.Exists(full)) return full;
            }
            return Path.GetFullPath(assetPath);
        }

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
