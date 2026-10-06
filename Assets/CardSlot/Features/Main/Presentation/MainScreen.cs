using System.Globalization;
using System.Threading;
using ClassicSpins.PrototypeFramework.Application;
using ClassicSpins.PrototypeFramework.Domain;
using ClassicSpins.PrototypeFramework.Presentation;
using Cysharp.Threading.Tasks;
using Game.Application;
using Game.Gen;
using Game.Views;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// The Main (Home) screen controller: shows the current level and coins, and Play loads Gameplay
    /// (features/level-progression.md). Plain ctor-injected class; the <see cref="HomeView"/> renders.
    /// </summary>
    public sealed class MainScreen : ScreenBase
    {
        private readonly MainParam _param;
        private readonly IAssetService _assets;
        private readonly IRenderLayerRegistry _layers;
        private readonly ISceneService _scenes;
        private readonly ILocalizationService _loc;
        private readonly LevelProgressService _progress;
        private readonly IWalletService _wallet;
        private readonly ILevelSource _levels;
        private readonly ILog _log;

        private GameObject _viewPrefab, _viewInstance;
        private HomeView _view;

        public MainScreen(MainParam param, IAssetService assets, IRenderLayerRegistry layers, ISceneService scenes,
            ILocalizationService loc, LevelProgressService progress, IWalletService wallet, ILevelSource levels, ILog log)
        {
            _param = param; _assets = assets; _layers = layers; _scenes = scenes; _loc = loc; _progress = progress;
            _wallet = wallet; _levels = levels; _log = log;
        }

        public override async UniTask OnLoadAsync(CancellationToken ct)
        {
            if (_param.ColdBoot) _progress.EnsureStartCoins();
            int count = await _levels.CountAsync(ct);
            _viewPrefab = await _assets.LoadAsync(AssetKeys.Home.HomeView, ct);
            _viewInstance = Object.Instantiate(_viewPrefab, _layers.GetHost(RenderLayers.Ui), false);
            _layers.Stamp(_viewInstance, RenderLayers.Ui);
            _view = _viewInstance.GetComponent<HomeView>();
            _view.PlayPressed += Play;
            int level = Mathf.Min(_progress.CurrentLevel, count);
            bool allCleared = _progress.HighestCleared >= count;
            _view.Show(_loc.Get(LocKeys.HomeTitle), _loc.Get(LocKeys.HomeTagline), _loc.Get(LocKeys.HomePlay, level),
                _wallet.Balance(CardSlotResources.Coin).ToString("N0", CultureInfo.InvariantCulture),
                allCleared ? _loc.Get(LocKeys.HomeMoreSoon) : null);
        }

        public override void OnEnter() => _log.Info("[MainScreen] entered.");

        public override void OnExit() => _log.Info("[MainScreen] exited.");

        public override UniTask OnUnloadAsync(CancellationToken ct)
        {
            if (_viewInstance != null) Object.Destroy(_viewInstance);
            if (_viewPrefab != null) _assets.Release(_viewPrefab);
            _viewInstance = null; _viewPrefab = null; _view = null;
            return UniTask.CompletedTask;
        }

        private void Play() =>
            _scenes.LoadAsync(SceneKeys.Gameplay, new GameplayParam(_progress.CurrentLevel), SceneTransition.Replace).Forget();
    }
}
