using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// The web (jam) build. Pack textures come in at 2048-4096 px, which a browser download can't afford: this
    /// caps them for WebGL only (normal maps and the UI pack's colour filters 512, everything else 1024; the menu's own art keeps its size), so
    /// desktop builds still get full detail. Then builds to Builds/WebGL.
    /// BIKE RUSH → Build → Web Texture Sizes / Web Build.
    /// </summary>
    public static class WebBuild
    {
        static readonly string[] Folders =
        {
            "Assets/Synty", "Assets/Simple Bicycle Physics", "Assets/Sir_bike", "Assets/Rukha93",
            "Assets/_ShiftRush/Art/Courses", "Assets/_ShiftRush/Art/Textures", "Assets/_ShiftRush/Art/VFX",
            "Assets/Heat - Complete Modern UI/Textures/Filters",
        };

        [MenuItem("BIKE RUSH/Build/Web Texture Sizes")]
        public static void CapTextures()
        {
            var guids = new List<string>();
            foreach (var f in Folders) if (AssetDatabase.IsValidFolder(f)) guids.AddRange(AssetDatabase.FindAssets("t:Texture2D", new[] { f }));
            int changed = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < guids.Count; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null || (imp.textureType == TextureImporterType.Sprite && !path.Contains("/Filters/"))) continue;
                    if (i % 50 == 0) EditorUtility.DisplayProgressBar("Web texture sizes", path, i / (float)guids.Count);
                    // Heat's full-screen colour filters are soft washes: 512 looks the same.
                    int cap = imp.textureType == TextureImporterType.NormalMap || path.Contains("/Filters/") ? 512 : 1024;
                    var def = imp.GetDefaultPlatformTextureSettings();
                    var web = imp.GetPlatformTextureSettings("WebGL");
                    int want = Mathf.Min(cap, def.maxTextureSize);
                    if (web.overridden && web.maxTextureSize == want) continue;
                    web.overridden = true;
                    web.maxTextureSize = want;
                    web.format = TextureImporterFormat.Automatic;
                    web.textureCompression = TextureImporterCompression.Compressed;
                    web.compressionQuality = 50;
                    imp.SetPlatformTextureSettings(web);
                    imp.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            Debug.Log("[BIKE//RUSH] Web texture caps set on " + changed + " of " + guids.Count + " textures.");
        }

        [MenuItem("BIKE RUSH/Build/Web Build")]
        public static void Build()
        {
            var scenes = new List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(), locationPathName = "Builds/WebGL", target = BuildTarget.WebGL, options = BuildOptions.None,
            });
            Debug.Log("[BIKE//RUSH] Web build " + report.summary.result + ": " + (report.summary.totalSize / 1048576f).ToString("0.0") + " MB.");
        }
    }
}
