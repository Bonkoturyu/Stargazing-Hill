using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StargazingHill.Editor
{
    /// <summary>
    /// Exports only assets owned by Stargazing Hill. Deliberately omits IncludeDependencies so VPM
    /// packages and the locally purchased UnyStylus never become redistribution payloads merely because
    /// the generated scene references them.
    /// </summary>
    public static class StargazingUnityPackageExporter
    {
        public const string OwnedAssetRoot = "Assets/StargazingHill";
        // Unity clears the project Temp directory while quitting, so batch artifacts belong under Build.
        public const string BatchOutputPath = "Build/StargazingHill-redistributable.unitypackage";

        private static readonly string[] ExcludedOwnedPrefixes =
        {
            // Ignored upstream inputs used to bake redistributable CC0 derivatives; not shipping assets.
            OwnedAssetRoot + "/SourceDownloads/"
        };

        private static readonly string[] ForbiddenExportPrefixes =
        {
            "Assets/Rasta/",
            "Packages/net.kwxxw.yama-stream/",
            "Packages/net.ureishi.qvpen/"
        };

        [MenuItem("Stargazing Hill/Build & Export/Redistributable UnityPackage...", false, 70)]
        public static void ExportInteractive()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            StargazingWorldBuilder.ValidateForBatchMode();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = EditorUtility.SaveFilePanel(
                "Export Stargazing Hill UnityPackage",
                projectRoot,
                "StargazingHill-redistributable",
                "unitypackage");
            if (string.IsNullOrEmpty(outputPath)) return;

            ExportTo(outputPath);
            EditorUtility.DisplayDialog(
                "Stargazing Hill",
                "Exported the redistributable package. YamaPlayer, QvPen, and UnyStylus were not bundled.\n\n" +
                outputPath,
                "OK");
        }

        public static void ExportForBatchMode()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            ExportTo(Path.Combine(projectRoot, BatchOutputPath));
        }

        internal static void ExportTo(string outputPath)
        {
            string[] ownedPaths = CollectOwnedAssetPaths();
            ValidateExportSelection(ownedPaths);

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            AssetDatabase.ExportPackage(ownedPaths, outputPath, ExportPackageOptions.Default);

            var package = new FileInfo(outputPath);
            if (!package.Exists || package.Length == 0)
                throw new InvalidOperationException("UnityPackage export did not produce a non-empty file: " + outputPath);

            Debug.Log("[Stargazing Hill] Exported redistributable unitypackage with " + ownedPaths.Length +
                      " owned paths to " + outputPath + ". External YamaPlayer, QvPen, and UnyStylus assets were omitted.");
        }

        internal static string[] CollectOwnedAssetPaths()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string ownedRootFullPath = Path.Combine(projectRoot, OwnedAssetRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(ownedRootFullPath))
                throw new DirectoryNotFoundException("Owned asset root is missing: " + ownedRootFullPath);

            var paths = new List<string> { OwnedAssetRoot };
            foreach (string directory in Directory.GetDirectories(ownedRootFullPath, "*", SearchOption.AllDirectories))
            {
                string path = ToProjectPath(projectRoot, directory);
                if (!IsExcludedOwnedPath(path)) paths.Add(path);
            }
            foreach (string file in Directory.GetFiles(ownedRootFullPath, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                string path = ToProjectPath(projectRoot, file);
                if (!IsExcludedOwnedPath(path)) paths.Add(path);
            }

            paths.Sort(StringComparer.Ordinal);
            return paths.ToArray();
        }

        internal static void ValidateExportSelection(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                throw new InvalidOperationException("UnityPackage export selection is empty.");

            string ownedPrefix = OwnedAssetRoot + "/";
            bool includesScene = false;
            bool includesRestoreGuide = false;
            bool includesPicnicLayout = false;
            for (int index = 0; index < paths.Length; index++)
            {
                string path = paths[index].Replace('\\', '/');
                if (path != OwnedAssetRoot && !path.StartsWith(ownedPrefix, StringComparison.Ordinal))
                    throw new InvalidOperationException("UnityPackage selection escaped the owned root: " + path);
                if (IsExcludedOwnedPath(path))
                    throw new InvalidOperationException("Local bake input selected for export: " + path);
                for (int forbidden = 0; forbidden < ForbiddenExportPrefixes.Length; forbidden++)
                    if (path.StartsWith(ForbiddenExportPrefixes[forbidden], StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Forbidden third-party dependency selected for export: " + path);

                if (path == OwnedAssetRoot + "/Scenes/StargazingHill.unity") includesScene = true;
                if (path == OwnedAssetRoot + "/README_UNITYPACKAGE.md") includesRestoreGuide = true;
                if (path == OwnedAssetRoot + "/Editor/Data/PicnicLayout.json") includesPicnicLayout = true;
            }

            if (!includesScene) throw new InvalidOperationException("Redistributable package selection lacks the world scene.");
            if (!includesRestoreGuide) throw new InvalidOperationException("Redistributable package selection lacks its restore guide.");
            if (!includesPicnicLayout)
                throw new InvalidOperationException("Redistributable package selection lacks the saved picnic layout.");
        }

        private static string ToProjectPath(string projectRoot, string fullPath)
        {
            string relative = fullPath.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }

        private static bool IsExcludedOwnedPath(string path)
        {
            string normalized = path.Replace('\\', '/');
            for (int index = 0; index < ExcludedOwnedPrefixes.Length; index++)
            {
                string prefix = ExcludedOwnedPrefixes[index];
                if (normalized == prefix.TrimEnd('/') ||
                    normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
