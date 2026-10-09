using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Developer level jump (Phat: "nút editor để nhảy level"). In play mode it opens the Gameplay screen at the chosen
    /// level through the running game's own <c>ISceneService</c> (the same call Home makes), so every level-start rule
    /// still runs — attempts, booster unlocks and their gifts. Out of play mode it writes the level into the save, so the
    /// next Play's Home button starts there. Game.Editor references no game assembly (like the content builder), so the
    /// runtime types are reached by name.
    /// </summary>
    public sealed class LevelJumpWindow : EditorWindow
    {
        private const int LevelCount = 30;
        private int _level = 1;
        private string _status = string.Empty;

        [MenuItem("CardSlot/Level Jump %#l")]
        public static void Open()
        {
            var w = GetWindow<LevelJumpWindow>("Level Jump");
            w.minSize = new Vector2(260f, 150f);
        }

        private void OnGUI()
        {
            _level = EditorGUILayout.IntSlider("Level", _level, 1, LevelCount);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("−1")) _level = Mathf.Max(1, _level - 1);
                if (GUILayout.Button("+1")) _level = Mathf.Min(LevelCount, _level + 1);
            }
            EditorGUILayout.Space();
            bool playing = EditorApplication.isPlaying;
            if (GUILayout.Button(playing ? $"Play level {_level} now" : $"Start next Play at level {_level}", GUILayout.Height(32f)))
                _status = playing ? JumpNow(_level) : WriteSave(_level);
            EditorGUILayout.HelpBox(playing
                ? "Loads the Gameplay screen at this level (Home is skipped; progress is not changed)."
                : "Writes the level into save.json; the next Play's Home button starts it. Stop play mode before changing the save.",
                MessageType.None);
            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        // ── play mode: ISceneService.LoadAsync(SceneKeys.Gameplay, new GameplayParam(level), SceneTransition.Replace) ──
        private static string JumpNow(int level)
        {
            try
            {
                var scopeType = FindType("VContainer.Unity.LifetimeScope");
                var sceneServiceType = FindType("ClassicSpins.PrototypeFramework.Presentation.ISceneService");
                var paramType = FindType("Game.Application.GameplayParam");
                var keysType = FindType("Game.Gen.SceneKeys");
                var transitionType = FindType("ClassicSpins.PrototypeFramework.Presentation.SceneTransition");
                if (scopeType == null || sceneServiceType == null || paramType == null || keysType == null || transitionType == null)
                    return "A runtime type was not found — compile first.";

                // the root scope (no parent) owns ISceneService
                object scenes = null;
                foreach (var scope in Resources.FindObjectsOfTypeAll(scopeType).Cast<Component>().Where(c => c.gameObject.scene.IsValid()))
                {
                    var parent = scopeType.GetProperty("Parent")?.GetValue(scope);
                    var container = scopeType.GetProperty("Container")?.GetValue(scope);
                    if (parent != null || container == null) continue;
                    var resolve = container.GetType().GetMethods().FirstOrDefault(m => m.Name == "Resolve" && m.GetParameters().Length >= 1
                        && m.GetParameters()[0].ParameterType == typeof(Type));
                    if (resolve == null) continue;
                    var args = resolve.GetParameters().Select((p, i) => i == 0 ? (object)sceneServiceType : p.DefaultValue).ToArray();
                    scenes = resolve.Invoke(container, args);
                    if (scenes != null) break;
                }
                if (scenes == null) return "The game has not booted yet (no root scope) — wait for Home.";

                object key = keysType.GetField("Gameplay", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object param = Activator.CreateInstance(paramType, level);
                object replace = (object)transitionType.GetField("Replace", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                                 ?? transitionType.GetProperty("Replace", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var load = sceneServiceType.GetMethod("LoadAsync").MakeGenericMethod(paramType);
                var loadArgs = load.GetParameters().Select((p, i) => i == 0 ? key : i == 1 ? param : i == 2 ? replace ?? p.DefaultValue : p.DefaultValue).ToArray();
                load.Invoke(scenes, loadArgs);
                Debug.Log($"[LevelJump] loading level {level}.");
                return $"Loading level {level}…";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return "Jump failed: " + (e.InnerException ?? e).Message;
            }
        }

        // ── edit mode: progress.CurrentLevel in save.json ────────────────────────────────────────────────
        private static string WriteSave(int level)
        {
            string path = Path.Combine(Application.persistentDataPath, "save.json");
            if (!File.Exists(path)) return "No save yet — press Play once, then set the level.";
            string json = File.ReadAllText(path);
            var current = new Regex("(\"CurrentLevel\"\\s*:\\s*)\\d+");
            if (!current.IsMatch(json)) return "save.json has no CurrentLevel.";
            json = current.Replace(json, "${1}" + level, 1);
            // the levels before it count as won, so the jump is not a replay
            json = new Regex("(\"HighestCleared\"\\s*:\\s*)(\\d+)").Replace(json, m =>
                m.Groups[1].Value + Math.Max(int.Parse(m.Groups[2].Value), level - 1), 1);
            File.WriteAllText(path, json);
            Debug.Log($"[LevelJump] save set to level {level}.");
            return $"Next Play starts at level {level}.";
        }

        private static Type FindType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName)).FirstOrDefault(t => t != null);
    }
}
