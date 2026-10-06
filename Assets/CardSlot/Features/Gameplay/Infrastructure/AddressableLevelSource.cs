using System;
using System.Collections.Generic;
using System.Threading;
using ClassicSpins.PrototypeFramework.Presentation;
using Cysharp.Threading.Tasks;
using Game.Domain;
using Game.Presentation;
using UnityEngine;

namespace Game.Infrastructure
{
    /// <summary>
    /// <see cref="ILevelSource"/> over the shipped level files: the <c>Content/LevelCatalog</c> prefab (an
    /// Addressables entry the framework registers from <c>Prefabs/</c>) lists them; each is parsed with the
    /// same <see cref="LevelJson"/> the headless tests use. Parsed levels are cached for the session.
    /// </summary>
    public sealed class AddressableLevelSource : ILevelSource, IDisposable
    {
        private readonly IAssetService _assets;
        private readonly Dictionary<int, LevelData> _cache = new Dictionary<int, LevelData>();
        private GameObject _catalogPrefab;
        private LevelCatalog _catalog;

        public AddressableLevelSource(IAssetService assets) { _assets = assets; }

        public async UniTask<int> CountAsync(CancellationToken ct) => (await Catalog(ct)).Count;

        public async UniTask<LevelData> LoadAsync(int level, CancellationToken ct)
        {
            if (_cache.TryGetValue(level, out var cached)) return cached;
            var catalog = await Catalog(ct);
            var file = catalog.Get(level);
            if (file == null) throw new ArgumentOutOfRangeException(nameof(level), $"no shipped level {level} (catalog has {catalog.Count})");
            if (!LevelJson.TryRead(file.text, out var data, out var error))
                throw new InvalidOperationException($"level {level} ({file.name}) is invalid: {error}");
            _cache[level] = data;
            return data;
        }

        private async UniTask<LevelCatalog> Catalog(CancellationToken ct)
        {
            if (_catalog != null) return _catalog;
            _catalogPrefab = await _assets.LoadAsync(AssetKeys.Content.LevelCatalog, ct);
            _catalog = _catalogPrefab.GetComponent<LevelCatalog>();
            if (_catalog == null) throw new InvalidOperationException("Content/LevelCatalog has no LevelCatalog component — run CardSlot/Content/Build");
            return _catalog;
        }

        public void Dispose()
        {
            if (_catalogPrefab != null) _assets.Release(_catalogPrefab);
            _catalogPrefab = null; _catalog = null;
        }
    }
}
