using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StargazingHill.Editor
{
    /// <summary>
    /// Applies a narrowly-scoped compatibility fix for the VRChat Worlds SDK 3.10.4
    /// ClientSim initialization order. VPM-managed package files are intentionally not
    /// tracked, so this guard reapplies the fix after a package restore.
    /// </summary>
    [InitializeOnLoad]
    public static class VrcSdk3104ClientSimGuard
    {
        private const string PackageVersion = "3.10.4";
        private const string PackageJsonPath = "Packages/com.vrchat.worlds/package.json";
        private const string UdonManagerPath = "Packages/com.vrchat.worlds/Runtime/Udon/UdonManager.cs";

        private const string InitialRegistrationOriginal =
            "                    foreach(UdonBehaviour udonBehaviour in udonBehavioursWorkingList)\r\n" +
            "                    {\r\n" +
            "                        udonManager.RegisterUdonBehaviour(udonBehaviour);\r\n" +
            "                    }";

        private const string InitialRegistrationReplacement =
            "                    foreach(UdonBehaviour udonBehaviour in udonBehavioursWorkingList)\r\n" +
            "                    {\r\n" +
            "                        // Stargazing Hill: ClientSim can initialize this behaviour before OnSceneLoaded resumes.\r\n" +
            "                        if(!udonBehaviour.IsInitialized)\r\n" +
            "                        {\r\n" +
            "                            udonBehaviour.IsNetworkingSupported = true;\r\n" +
            "                        }\r\n" +
            "                        udonManager.RegisterUdonBehaviour(udonBehaviour);\r\n" +
            "                    }";

        private const string SceneRegistrationOriginal =
            "                        // All UdonBehaviours that exist in the scene get networking setup automatically.\r\n" +
            "                        udonBehaviour.IsNetworkingSupported = true;\r\n" +
            "                        using(_initializeProfilerMarker.Auto())";

        private const string SceneRegistrationReplacement =
            "                        // All UdonBehaviours that exist in the scene get networking setup automatically.\r\n" +
            "                        // Stargazing Hill: do not set this again after ClientSim has initialized the behaviour.\r\n" +
            "                        if(!udonBehaviour.IsInitialized)\r\n" +
            "                        {\r\n" +
            "                            udonBehaviour.IsNetworkingSupported = true;\r\n" +
            "                        }\r\n" +
            "                        using(_initializeProfilerMarker.Auto())";

        private static bool _warningShown;

        static VrcSdk3104ClientSimGuard()
        {
            EditorApplication.delayCall += ApplyIfRequired;
        }

        [MenuItem("Stargazing Hill/Advanced/Diagnostics/Repair VRChat SDK 3.10.4 ClientSim Guard", false, 95)]
        public static void ApplyIfRequired()
        {
            if (!File.Exists(PackageJsonPath) || !File.Exists(UdonManagerPath)) return;

            string packageJson = File.ReadAllText(PackageJsonPath);
            if (!packageJson.Contains("\"version\": \"" + PackageVersion + "\""))
            {
                WarnOnce("VRChat Worlds SDK is no longer 3.10.4. The ClientSim networking guard was not applied; " +
                         "review whether the upstream SDK now includes an equivalent fix.");
                return;
            }

            string source = NormalizeNewlines(File.ReadAllText(UdonManagerPath));
            string initialOriginal = NormalizeNewlines(InitialRegistrationOriginal);
            string initialReplacement = NormalizeNewlines(InitialRegistrationReplacement);
            string sceneOriginal = NormalizeNewlines(SceneRegistrationOriginal);
            string sceneReplacement = NormalizeNewlines(SceneRegistrationReplacement);

            bool initialPatched = source.Contains(initialReplacement);
            bool scenePatched = source.Contains(sceneReplacement);
            if (initialPatched && scenePatched) return;

            if ((!initialPatched && !source.Contains(initialOriginal)) ||
                (!scenePatched && !source.Contains(sceneOriginal)))
            {
                WarnOnce("VRChat Worlds SDK 3.10.4 UdonManager.cs did not match the verified source. " +
                         "The ClientSim networking guard was not applied.");
                return;
            }

            if (!initialPatched) source = source.Replace(initialOriginal, initialReplacement);
            if (!scenePatched) source = source.Replace(sceneOriginal, sceneReplacement);
            File.WriteAllText(UdonManagerPath, source);
            AssetDatabase.ImportAsset(UdonManagerPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[Stargazing Hill] Applied the VRChat SDK 3.10.4 ClientSim Udon networking guard.");
        }

        private static string NormalizeNewlines(string value)
        {
            return value.Replace("\r\n", "\n");
        }

        private static void WarnOnce(string message)
        {
            if (_warningShown) return;
            _warningShown = true;
            Debug.LogWarning("[Stargazing Hill] " + message);
        }
    }
}
