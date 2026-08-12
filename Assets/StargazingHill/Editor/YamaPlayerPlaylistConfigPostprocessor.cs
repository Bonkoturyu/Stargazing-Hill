using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace StargazingHill.Editor
{
    public sealed class YamaPlayerPlaylistConfigPostprocessor : AssetPostprocessor
    {
        private const string ConfigPath = "Assets/StargazingHill/Config/music_list.txt";
        private const string ScenePath = "Assets/StargazingHill/Scenes/StargazingHill.unity";

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool changed = false;
            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (importedAssets[i] == ConfigPath)
                {
                    changed = true;
                    break;
                }
            }
            if (!changed) return;

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                Scene scene = SceneManager.GetActiveScene();
                if (scene.path != ScenePath) return;
                YamaPlayerPlaylistSync.SyncOpenSceneFromConfig();
            };
        }
    }
}
