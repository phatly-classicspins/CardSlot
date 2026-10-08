using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// <c>CardSlot/Content/Build</c> — regenerates the game-side content from its sources (G12), so nothing
    /// here is hand-edited:
    /// <list type="number">
    /// <item>copies the approved sprites <c>art/export/{gameplay,ui,bg}/*.png</c> (art v1, D-020) into
    /// <c>Assets/CardSlot/Content/Sprites/</c> and applies the import contract of D-019 (PPU 1, alpha is
    /// transparency, no mipmaps, 9-slice borders from <c>art/export/manifest.json</c>);</item>
    /// <item>builds the view/catalog prefabs under <c>Assets/CardSlot/Prefabs/</c> and wires their serialized
    /// references. Prefabs under <c>Prefabs/</c> are registered to Addressables and given an
    /// <c>AssetKeys</c> member by the framework's codegen postprocessor.</item>
    /// </list>
    /// Game.Editor references no runtime assembly (spine), so components are resolved by name and fields
    /// written through <see cref="SerializedObject"/>.
    /// </summary>
    internal static class CardSlotContentBuilder
    {
        private const string Tag = "[ContentBuilder]";
        private const string SpritesRoot = "Assets/CardSlot/Content/Sprites";
        private const string LevelsRoot = "Assets/CardSlot/Content/Levels";
        private const string PrefabsRoot = "Assets/CardSlot/Prefabs";

        [Serializable] private sealed class Manifest { public SpriteEntry[] sprites = new SpriteEntry[0]; }
        [Serializable] private sealed class SpriteEntry { public string id; public string atlas; public int w; public int h; public int[] slice; }

        [MenuItem("CardSlot/Content/Build")]
        public static void Build()
        {
            string repo = Directory.GetParent(Application.dataPath).FullName;
            string export = Path.Combine(repo, "art", "export");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(export, "manifest.json")));
            int copied = ImportSprites(export, manifest);
            BuildPrefabs();
            Debug.Log($"{Tag} {copied} sprites imported, prefabs built under {PrefabsRoot}.");
        }

        private static int ImportSprites(string export, Manifest manifest)
        {
            int n = 0;
            foreach (var s in manifest.sprites)
            {
                string src = Path.Combine(export, s.atlas, s.id + ".png");
                string dstDir = $"{SpritesRoot}/{s.atlas}";
                Directory.CreateDirectory(dstDir);
                File.Copy(src, $"{dstDir}/{s.id}.png", true);
                n++;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var s in manifest.sprites)
            {
                string path = $"{SpritesRoot}/{s.atlas}/{s.id}.png";
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) { Debug.LogError($"{Tag} no importer for {path}"); continue; }
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 1f;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                // manifest slice is [left, top, right, bottom]; Unity's border is (left, bottom, right, top)
                ti.spriteBorder = s.slice != null && s.slice.Length == 4 ? new Vector4(s.slice[0], s.slice[3], s.slice[2], s.slice[1]) : Vector4.zero;
                ti.SaveAndReimport();
            }
            return n;
        }

        private static Sprite S(string atlas, string id)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesRoot}/{atlas}/{id}.png");
            if (sprite == null) Debug.LogError($"{Tag} missing sprite {atlas}/{id}");
            return sprite;
        }

        // one sprite per card colour, glossary color_0..7 (CR-012: 8 colours)
        private const int CardColours = 8;
        private static Sprite[] PerColour(string atlas, string prefix) => Enumerable.Range(0, CardColours).Select(i => S(atlas, prefix + i)).ToArray();

        private static void BuildPrefabs()
        {
            var font = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset")
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadMainAssetAtPath).FirstOrDefault();

            Save("Gameplay/BoardView", "Game.Views.BoardView, Game.Views", so =>
            {
                Set(so, "_cardFace", PerColour("gameplay", "card_face_"));
                Set(so, "_cardUnder", PerColour("gameplay", "card_under_"));
                Set(so, "_cardMini", PerColour("gameplay", "card_mini_"));
                Set(so, "_chip", PerColour("gameplay", "chip_"));
                Set(so, "_targetBase", PerColour("gameplay", "target_base_"));
                Set(so, "_hatch", S("gameplay", "card_covered_hatch"));
                Set(so, "_targetSlot", S("gameplay", "target_slot_bg"));
                Set(so, "_trayRim", S("gameplay", "tray_rim"));
                Set(so, "_trayInner", S("gameplay", "tray_inner"));
                Set(so, "_bufferRail", S("gameplay", "buffer_rail"));
                Set(so, "_bufferRailWarn", S("gameplay", "buffer_rail_warn"));
                Set(so, "_bufferCell", S("gameplay", "buffer_cell"));
                Set(so, "_bufferCellWarn", S("gameplay", "buffer_cell_warn"));
                Set(so, "_ground", S("bg", "bg_ground"));
                Set(so, "_pill", S("ui", "pill_surface"));
                Set(so, "_roundButton", S("ui", "rbtn_secondary"));
                Set(so, "_iconPause", S("ui", "icon_pause"));
                Set(so, "_iconRestart", S("ui", "icon_restart"));
                Set(so, "_buttonPrimary", S("ui", "btn_primary"));   // R-23 RV Slot pill
                Set(so, "_iconPlay", S("ui", "icon_play"));
                Set(so, "_coin", S("ui", "coin"));                     // CR-012 C1 coin pill
                Set(so, "_hand", S("ui", "hand_pointer"));
                Set(so, "_ring", S("ui", "ftue_ring"));
                Set(so, "_font", font);
                SetBool(so, "_draw2DBoard", false);   // CR-003: the board is 3D; this view keeps HUD, labels, tap areas
            });

            // the scaffolded dialog prefab variants (Scaffold.Sync owns them; this only wires their sprites)
            foreach (var id in new[] { "Win", "Lose", "Pause", "Settings", "RestartConfirm" })
                WireDialog($"Assets/CardSlot/Content/UI/{id}/Prefabs/{id}Dialog.prefab", $"Game.Views.{id}DialogView, Game.Views", font);

            Save("Gameplay/Board3DView", "Game.Views.Board3DView, Game.Views", so =>
            {
                Set(so, "_ground", S("bg", "bg_ground"));
                SetFloat(so, "_tilt", 10f);   // D-027
            }, rectTransform: false);

            Save("Home/HomeView", "Game.Views.HomeView, Game.Views", so =>
            {
                Set(so, "_pill", S("ui", "pill_surface"));              // CR-012 C1 coin pill
                Set(so, "_coin", S("ui", "coin"));
                Set(so, "_cardFace", PerColour("gameplay", "card_face_"));
                Set(so, "_ground", S("bg", "bg_ground"));
                Set(so, "_buttonPrimary", S("ui", "btn_primary"));
                Set(so, "_font", font);
            });

            var levels = Directory.GetFiles(LevelsRoot, "level_*.json").OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<TextAsset>(p.Replace('\\', '/'))).ToArray();
            Save("Content/LevelCatalog", "Game.Infrastructure.LevelCatalog, Game.Infrastructure", so => Set(so, "_levels", levels));
        }

        private static void WireDialog(string path, string viewType, UnityEngine.Object font)
        {
            var type = Type.GetType(viewType);
            if (type == null) { Debug.LogError($"{Tag} type {viewType} not found — compile first."); return; }
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // The template carries a plain DialogViewBase ([DisallowMultipleComponent]); the dialog's own view
                // (a DialogViewBase subclass) replaces it on the variant, so LoadViewAsync still finds a DialogViewBase.
                var view = root.GetComponent(type);
                if (view == null)
                {
                    var plain = root.GetComponent("DialogViewBase");
                    if (plain != null && plain.GetType().Name == "DialogViewBase") UnityEngine.Object.DestroyImmediate(plain, true);
                    view = root.AddComponent(type);
                }
                // the template's sample "Panel" would draw under ours: deactivate it (an override, not a stripped node)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Panel" && t.GetComponent<UnityEngine.UI.Image>() != null && t.parent != null && t.parent.name != "Content") t.gameObject.SetActive(false);
                var so = new SerializedObject(view);
                Set(so, "_panel", S("ui", "panel"));
                Set(so, "_ribbon", S("ui", "ribbon_secondary"));
                Set(so, "_ribbonDanger", S("ui", "ribbon_danger"));
                Set(so, "_pill", S("ui", "pill_surface"));              // CR-012 C1 coin pill above the dim
                Set(so, "_coin", S("ui", "coin"));
                Set(so, "_buttonPrimary", S("ui", "btn_primary"));
                Set(so, "_buttonSecondary", S("ui", "btn_secondary"));
                Set(so, "_buttonDisabled", S("ui", "btn_disabled"));
                Set(so, "_iconPlay", S("ui", "icon_play"));
                Set(so, "_iconLock", S("ui", "icon_lock"));
                Set(so, "_iconClose", S("ui", "icon_close"));
                Set(so, "_iconGear", S("ui", "icon_gear"));
                Set(so, "_closeButton", S("ui", "close_btn"));
                Set(so, "_rowSunken", S("ui", "row_sunken"));
                Set(so, "_toggleOn", S("ui", "toggle_on"));
                Set(so, "_toggleOff", S("ui", "toggle_off"));
                Set(so, "_toggleKnob", S("ui", "toggle_knob"));
                Set(so, "_roundButton", S("ui", "rbtn_secondary"));
                Set(so, "_cardFace", PerColour("gameplay", "card_face_"));
                Set(so, "_font", font);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Save(string address, string componentType, Action<SerializedObject> wire, bool rectTransform = true)
        {
            var type = Type.GetType(componentType);
            if (type == null) { Debug.LogError($"{Tag} type {componentType} not found — compile first."); return; }
            string path = $"{PrefabsRoot}/{address}.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var go = rectTransform ? new GameObject(Path.GetFileName(address), typeof(RectTransform)) : new GameObject(Path.GetFileName(address));
            try
            {
                if (go.transform is RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
                var component = go.AddComponent(type);
                var so = new SerializedObject(component);
                wire(so);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"{Tag} field {field} not found on {so.targetObject.GetType().Name}"); return; }
            p.floatValue = value;
        }

        private static void SetBool(SerializedObject so, string field, bool value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"{Tag} field {field} not found on {so.targetObject.GetType().Name}"); return; }
            p.boolValue = value;
        }

        private static void Set(SerializedObject so, string field, UnityEngine.Object value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"{Tag} field {field} not found on {so.targetObject.GetType().Name}"); return; }
            p.objectReferenceValue = value;
        }

        private static void Set(SerializedObject so, string field, IReadOnlyList<UnityEngine.Object> values)
        {
            var p = so.FindProperty(field);
            if (p == null || !p.isArray) { Debug.LogError($"{Tag} array field {field} not found on {so.targetObject.GetType().Name}"); return; }
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
