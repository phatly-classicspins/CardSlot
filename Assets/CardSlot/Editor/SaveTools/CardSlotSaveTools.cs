using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Start the game over from level 1 while developing: moves the framework's save file (IUserData →
    /// <c>save.json</c> in <see cref="Application.persistentDataPath"/>) aside to a timestamped backup instead of
    /// deleting it, so an earlier save can always be put back by renaming the file.
    /// </summary>
    public static class CardSlotSaveTools
    {
        private const string SaveFile = "save.json";

        [MenuItem("CardSlot/Reset Save")]
        public static void ResetSave()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Reset Save", "Stop play mode first: the running game would write the save again.", "OK");
                return;
            }
            string dir = Application.persistentDataPath;
            string path = Path.Combine(dir, SaveFile);
            if (!File.Exists(path))
            {
                Debug.Log($"[CardSlot] Reset Save: no save at {path} — the next Play already starts from level 1.");
                return;
            }
            string backup = Path.Combine(dir, $"save.backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Move(path, backup);
            Debug.Log($"[CardSlot] Reset Save: moved the save to {backup}. The next Play starts from level 1 (tutorial included).");
        }

        [MenuItem("CardSlot/Open Save Folder")]
        public static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
