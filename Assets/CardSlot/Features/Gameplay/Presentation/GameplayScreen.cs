using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ClassicSpins.PrototypeFramework.Application;
using ClassicSpins.PrototypeFramework.Domain;
using ClassicSpins.PrototypeFramework.Presentation;
using Cysharp.Threading.Tasks;
using Game.Application;
using Game.Domain;
using Game.Gen;
using Game.Views;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// The Gameplay screen controller (milestone 6a): loads the level, owns the <see cref="BoardModel"/> of
    /// the attempt, turns taps into rules and the model into a <see cref="BoardVisual"/>. Win/lose show the
    /// simple result card; dialogs, boosters and Continue arrive in 6b. Plain ctor-injected class.
    /// </summary>
    public sealed class GameplayScreen : ScreenBase
    {
        private readonly GameplayParam _param;
        private readonly ILevelSource _levels;
        private readonly IAssetService _assets;
        private readonly IRenderLayerRegistry _layers;
        private readonly ISceneService _scenes;
        private readonly ILocalizationService _loc;
        private readonly LevelProgressService _progress;
        private readonly IWalletService _wallet;
        private readonly EconomyTuning _tuning;
        private readonly ILog _log;

        private GameObject _viewPrefab, _viewInstance;
        private BoardView _view;
        private int _level, _levelCount;
        private LevelData _data;
        private BoardModel _board;
        private WinInfo _lastWin;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public GameplayScreen(GameplayParam param, ILevelSource levels, IAssetService assets, IRenderLayerRegistry layers,
            ISceneService scenes, ILocalizationService loc, LevelProgressService progress, IWalletService wallet,
            EconomyTuning tuning, ILog log)
        {
            _param = param; _levels = levels; _assets = assets; _layers = layers; _scenes = scenes; _loc = loc;
            _progress = progress; _wallet = wallet; _tuning = tuning; _log = log;
        }

        public override async UniTask OnLoadAsync(CancellationToken ct)
        {
            _levelCount = await _levels.CountAsync(ct);
            _viewPrefab = await _assets.LoadAsync(AssetKeys.Gameplay.BoardView, ct);
            _viewInstance = Object.Instantiate(_viewPrefab, _layers.GetHost(RenderLayers.GamePlay), false);
            _layers.Stamp(_viewInstance, RenderLayers.GamePlay);
            _view = _viewInstance.GetComponent<BoardView>();
            _view.StackTapped += OnStackTapped;
            _view.RestartPressed += () => StartLevel(_level).Forget();
            _view.PausePressed += GoHome;          // 6a: Pause dialog arrives in 6b
            _view.ResultPrimaryPressed += OnResultPrimary;
            _view.ResultSecondaryPressed += GoHome;
            await StartLevel(Mathf.Clamp(_param.Level, 1, Mathf.Max(1, _levelCount)));
        }

        public override void OnEnter() => _log.Info($"[GameplayScreen] entered level {_level}.");

        public override void OnBackRequested() => GoHome();

        public override UniTask OnUnloadAsync(CancellationToken ct)
        {
            _cts.Cancel();
            if (_viewInstance != null) Object.Destroy(_viewInstance);
            if (_viewPrefab != null) _assets.Release(_viewPrefab);
            _viewInstance = null; _viewPrefab = null; _view = null;
            return UniTask.CompletedTask;
        }

        private async UniTask StartLevel(int level)
        {
            _level = level;
            _data = await _levels.LoadAsync(level, _cts.Token);
            _board = new BoardModel(_data, _tuning.BoardRules());
            var start = _progress.BeginLevel(level);
            _log.Info($"[GameplayScreen] level {level} attempt {start.AttemptNo} (seed {_data.Seed}).");
            _view.HideResult();
            _view.SetHud(_loc.Get(LocKeys.GameplayLevel, level), _data.Hard ? _loc.Get(LocKeys.GameplayHard) : null, Coins());
            _view.Render(Visual());
        }

        private void OnStackTapped(int stack)
        {
            if (_board == null) return;
            var steps = _board.Tap(stack);
            if (steps.Count == 0) return;           // R-4: covered or finished — 6c adds the shake
            _view.Render(Visual());
            if (_board.Result == BoardResult.Won) OnWon();
            else if (_board.Result == BoardResult.Lost) OnLost();
        }

        private void OnWon()
        {
            _lastWin = _progress.CompleteLevel(_level, _data.Hard);   // persisted before anything is shown (G19)
            _view.SetHud(_loc.Get(LocKeys.GameplayLevel, _level), _data.Hard ? _loc.Get(LocKeys.GameplayHard) : null, Coins());
            _view.ShowResult(new ResultVisual
            {
                Title = _loc.Get(_data.Hard ? LocKeys.WinTitleHard : LocKeys.WinTitle),
                Subtitle = _loc.Get(LocKeys.WinSubtitle, _level),
                PrimaryLabel = _loc.Get(_lastWin.Final ? LocKeys.WinPlayAgain : LocKeys.WinNext),
                SecondaryLabel = _loc.Get(LocKeys.WinHome),
            });
        }

        private void OnLost()
        {
            _lastWin = default;
            _view.ShowResult(new ResultVisual
            {
                Title = _loc.Get(LocKeys.LoseTitle),
                Subtitle = _loc.Get(LocKeys.LoseSubtitle, _level),
                PrimaryLabel = _loc.Get(LocKeys.LoseRetry),
                SecondaryLabel = _loc.Get(LocKeys.LoseHome),
                Danger = true,
            });
        }

        private void OnResultPrimary()
        {
            if (_board.Result == BoardResult.Won) StartLevel(_lastWin.Saved ? _lastWin.NextLevel : _level).Forget();
            else StartLevel(_level).Forget();
        }

        private void GoHome() => _scenes.LoadAsync(SceneKeys.Main, new MainParam(ColdBoot: false), SceneTransition.Replace).Forget();

        private string Coins() => _wallet.Balance(CardSlotResources.Coin).ToString("N0", CultureInfo.InvariantCulture);

        private BoardVisual Visual()
        {
            var v = new BoardVisual
            {
                Stacks = new StackVisual[_board.StackCount],
                Targets = new TargetVisual[_board.SlotCount],
                Upcoming = _board.UpcomingColors(2).ToArray(),
                Buffer = _board.Buffer.ToArray(),
                BufferCapacity = _board.BufferCapacity,
                BufferWarn = _board.BufferCapacity - _board.Buffer.Count <= 2,
                HoldingLabel = _loc.Get(LocKeys.GameplayHoldingArea),
                BufferCountLabel = _loc.Get(LocKeys.GameplayBufferCount, _board.Buffer.Count, _board.BufferCapacity),
                NextLabel = _loc.Get(LocKeys.GameplayNext),
            };
            for (int i = 0; i < _board.StackCount; i++)
            {
                var spec = _data.Stacks[i];
                int n = _board.Remaining(i);
                var colors = new int[n];
                for (int d = 0; d < n; d++) colors[d] = _board.CardAt(i, d);
                v.Stacks[i] = new StackVisual { X = spec.X, Y = spec.Y, W = spec.W, H = spec.H, Layer = spec.Layer, Colors = colors, Covered = n > 0 && _board.IsCovered(i) };
            }
            for (int s = 0; s < _board.SlotCount; s++)
            {
                bool has = _board.SlotHasTarget(s);
                v.Targets[s] = new TargetVisual
                {
                    Has = has,
                    Color = has ? _board.SlotColor(s) : 0,
                    Filled = has ? _board.SlotFilled(s) : 0,
                    CountLabel = has ? _loc.Get(LocKeys.GameplayCount, _board.SlotFilled(s), _board.SlotCapacity(s)) : null,
                };
            }
            return v;
        }
    }
}
