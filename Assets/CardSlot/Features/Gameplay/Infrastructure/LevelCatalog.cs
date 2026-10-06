using UnityEngine;

namespace Game.Infrastructure
{
    /// <summary>
    /// Holds the shipped level files (Assets/CardSlot/Content/Levels/level_NNN.json) in order, on the prefab
    /// <c>Prefabs/Content/LevelCatalog</c> so they ride the framework's prefab → Addressables registration.
    /// Filled by the editor builder (CardSlot/Content/Build) — never by hand.
    /// </summary>
    public sealed class LevelCatalog : MonoBehaviour
    {
        [SerializeField] private TextAsset[] _levels = new TextAsset[0];

        public int Count => _levels.Length;
        /// <summary>The level file for 1-based <paramref name="level"/>, or null.</summary>
        public TextAsset Get(int level) => level >= 1 && level <= _levels.Length ? _levels[level - 1] : null;
    }
}
