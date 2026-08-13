using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yamadev.YamaStream;
using Yamadev.YamaStream.Modules.AutoPlay;

namespace StargazingHill.Editor
{
    [InitializeOnLoad]
    public static class YamaPlayerPlaylistSync
    {
        private const string ScenePath = "Assets/StargazingHill/Scenes/StargazingHill.unity";
        private const string ConfigPath = "Assets/StargazingHill/Config/music_list.txt";
        private const string AutoPlayPrefabPath = "Packages/net.kwxxw.yama-stream/Modules/AutoPlay/AutoPlay.prefab";
        private const float DefaultAutoPlayDelaySeconds = 7f;

        private sealed class ParsedTrack
        {
            public string title;
            public string url;
            public bool autoPlay;
        }

        private sealed class ParsedPlaylist
        {
            public string name;
            public readonly List<ParsedTrack> tracks = new List<ParsedTrack>();
        }

        private static bool _syncing;

        static YamaPlayerPlaylistSync()
        {
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        [MenuItem("Stargazing Hill/YamaPlayer/Sync Playlist Config", false, 30)]
        public static void SyncMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Sync(scene, true);
            EditorSceneManager.SaveScene(scene);
        }

        public static void SyncOpenSceneFromConfig()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || Application.isPlaying) return;
            Sync(scene, true);
            EditorSceneManager.SaveScene(scene);
        }

        internal static void SyncForWorldBuild(Scene scene)
        {
            if (!scene.IsValid())
                throw new InvalidOperationException("Cannot sync YamaPlayer playlists into an invalid scene.");
            Sync(scene, false);
        }

        private static void OnSceneSaved(Scene scene)
        {
            if (_syncing || Application.isPlaying || scene.path != ScenePath) return;
            Sync(scene, false);
            _syncing = true;
            try { EditorSceneManager.SaveScene(scene); }
            finally { _syncing = false; }
        }

        private static void Sync(Scene scene, bool logSuccess)
        {
            if (!File.Exists(ConfigPath))
            {
                Debug.LogWarning("[Stargazing Hill] YamaPlayer playlist config is missing: " + ConfigPath);
                return;
            }

            YamaPlayer player = UnityEngine.Object.FindObjectOfType<YamaPlayer>(true);
            if (player == null)
            {
                Debug.LogWarning("[Stargazing Hill] YamaPlayer sync skipped because no YamaPlayer exists in the scene.");
                return;
            }

            List<ParsedPlaylist> parsed = Parse(File.ReadAllLines(ConfigPath));
            if (parsed.Count == 0)
            {
                Debug.LogWarning("[Stargazing Hill] YamaPlayer playlist config contains no playlists.");
                return;
            }

            Transform playlistRoot = FindOrCreatePlaylistRoot(player.transform);
            for (int i = playlistRoot.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(playlistRoot.GetChild(i).gameObject);

            int autoPlaylistIndex = -1;
            int autoTrackIndex = -1;
            int autoCount = 0;

            for (int playlistIndex = 0; playlistIndex < parsed.Count; playlistIndex++)
            {
                ParsedPlaylist source = parsed[playlistIndex];
                GameObject go = new GameObject(source.name);
                go.transform.SetParent(playlistRoot, false);
                PlaylistItem item = go.AddComponent<PlaylistItem>();
                item.playlistName = source.name;
                item.tracks = new PlaylistTrack[source.tracks.Count];

                for (int trackIndex = 0; trackIndex < source.tracks.Count; trackIndex++)
                {
                    ParsedTrack sourceTrack = source.tracks[trackIndex];
                    item.tracks[trackIndex] = new PlaylistTrack
                    {
                        playerType = VideoPlayerType.AVProVideoPlayer,
                        title = sourceTrack.title ?? string.Empty,
                        url = sourceTrack.url
                    };
                    if (sourceTrack.autoPlay)
                    {
                        autoCount++;
                        autoPlaylistIndex = playlistIndex;
                        autoTrackIndex = trackIndex;
                    }
                }
            }

            if (autoCount > 1)
                throw new InvalidOperationException("music_list.txt must contain at most one [AUTO] track. Found: " + autoCount);

            ConfigureDefaultLoop(player.transform);
            ConfigureAutoPlay(player.transform, autoPlaylistIndex, autoTrackIndex);

            EditorSceneManager.MarkSceneDirty(scene);
            if (logSuccess)
                Debug.Log("[Stargazing Hill] Synced YamaPlayer playlists from " + ConfigPath + ".");
        }

        private static Transform FindOrCreatePlaylistRoot(Transform playerRoot)
        {
            PlaylistManager manager = playerRoot.GetComponentInChildren<PlaylistManager>(true);
            if (manager != null) return manager.transform;

            GameObject go = new GameObject("Playlists");
            go.transform.SetParent(playerRoot, false);
            go.AddComponent<PlaylistManager>();
            return go.transform;
        }

        private static void ConfigureDefaultLoop(Transform playerRoot)
        {
            Controller controller = playerRoot.GetComponentInChildren<Controller>(true);
            if (controller == null) return;
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty loop = serialized.FindProperty("_loop");
            if (loop != null) loop.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureAutoPlay(Transform playerRoot, int playlistIndex, int trackIndex)
        {
            AutoPlay autoPlay = playerRoot.GetComponentInChildren<AutoPlay>(true);
            if (playlistIndex < 0 || trackIndex < 0)
            {
                if (autoPlay != null) autoPlay.gameObject.SetActive(false);
                return;
            }

            if (autoPlay == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AutoPlayPrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("YamaPlayer AutoPlay prefab is missing: " + AutoPlayPrefabPath);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, playerRoot) as GameObject;
                if (instance == null) throw new InvalidOperationException("Failed to instantiate YamaPlayer AutoPlay module.");
                instance.name = "AutoPlay";
                autoPlay = instance.GetComponent<AutoPlay>();
            }

            Controller controller = playerRoot.GetComponentInChildren<Controller>(true);
            if (controller == null)
                throw new InvalidOperationException("YamaPlayer Controller is missing; AutoPlay cannot be configured.");

            autoPlay.gameObject.SetActive(true);
            // YamaPlayer's SDK build hook normally injects this reference into modules, but ClientSim starts
            // the saved scene without running that hook. Persist it on the proxy and its backing Udon now so
            // AutoPlay works in ClientSim as well as an uploaded world.
            SerializedObject serialized = new SerializedObject(autoPlay);
            SerializedProperty controllerReference = serialized.FindProperty("_controller");
            SerializedProperty mode = serialized.FindProperty("_autoPlayMode");
            SerializedProperty delay = serialized.FindProperty("_delay");
            SerializedProperty playlist = serialized.FindProperty("_playlistIndex");
            SerializedProperty track = serialized.FindProperty("_playlistTrackIndex");
            if (controllerReference == null)
                throw new InvalidOperationException("YamaPlayer AutoPlay Controller property is missing.");
            controllerReference.objectReferenceValue = controller;
            if (mode != null) mode.enumValueIndex = (int)AutoPlayMode.FromPlaylist;
            if (delay != null) delay.floatValue = DefaultAutoPlayDelaySeconds;
            if (playlist != null) playlist.intValue = playlistIndex;
            if (track != null) track.intValue = trackIndex;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UdonSharpEditorUtility.CopyProxyToUdon(autoPlay);
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(autoPlay);
            if (backing == null)
                throw new InvalidOperationException("YamaPlayer AutoPlay has no backing Udon behaviour.");
            var controllerBacking = UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
            if (controllerBacking == null)
                throw new InvalidOperationException("YamaPlayer Controller has no backing Udon behaviour.");
            EditorUtility.SetDirty(autoPlay);
            EditorUtility.SetDirty(backing);
        }

        private static List<ParsedPlaylist> Parse(string[] lines)
        {
            var result = new List<ParsedPlaylist>();
            ParsedPlaylist current = null;
            Regex urlRegex = new Regex(@"https?://\S+", RegexOptions.IgnoreCase);

            foreach (string raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string line = raw.Trim();
                if (line == "Playlist") continue;

                Match urlMatch = urlRegex.Match(line);
                if (!urlMatch.Success)
                {
                    string playlistName = StripTreePrefix(line);
                    if (string.IsNullOrWhiteSpace(playlistName)) continue;
                    current = new ParsedPlaylist { name = playlistName.Trim() };
                    result.Add(current);
                    continue;
                }

                if (current == null)
                    throw new FormatException("Track found before playlist name: " + line);

                string url = urlMatch.Value.TrimEnd('|');
                string before = line.Substring(0, urlMatch.Index);
                before = StripTreePrefix(before).Trim();
                bool autoPlay = before.IndexOf("[AUTO]", StringComparison.OrdinalIgnoreCase) >= 0;
                before = Regex.Replace(before, @"\[AUTO\]", string.Empty, RegexOptions.IgnoreCase).Trim();
                before = before.TrimEnd('|').Trim();

                current.tracks.Add(new ParsedTrack
                {
                    title = before,
                    url = url,
                    autoPlay = autoPlay
                });
            }

            return result;
        }

        private static string StripTreePrefix(string value)
        {
            if (value == null) return string.Empty;
            string text = value.Trim();
            text = Regex.Replace(text, @"^[│\s]*[├└]─\s*", string.Empty);
            return text.Trim();
        }
    }
}
