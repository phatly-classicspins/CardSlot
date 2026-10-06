using System;
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
using Object = UnityEngine.Object;

namespace Game.Presentation
{
    /// <summary>
    /// The Gameplay screen controller: loads the level, owns the attempt's <see cref="BoardModel"/>, turns taps
    /// into rules and the model into visuals, and runs the flows of milestone 6b — boosters, win/lose dialogs
    /// with Continue, pause/settings, restart confirm, booster unlocks and the level 1–2 tutorial
    /// (features/*.md). Every reward/charge is saved before anything shows it (G18, G19), and a dialog that
    /// comes back <see cref="DialogCloseReason.Aborted"/> changes nothing.
    /// </summary>
    public sealed class GameplayScreen : ScreenBase
    {
        private const string FtueL1Step1 = "ftue.l1.step1", FtueL1Step2 = "ftue.l1.step2", FtueL2Buffer = "ftue.l2.buffer", FtueL2Warn = "ftue.l2.warn";

        private readonly GameplayParam _param;
        private readonly ILevelSource _levels;
        private readonly IAssetService _assets;
        private readonly IRenderLayerRegistry _layers;
        private readonly ISceneService _scenes;
        private readonly ILocalizationService _loc;
        private readonly IDialogService _dialogs;
        private readonly IAdsService _ads;
        private readonly LevelProgressService _progress;
        private readonly BoosterService _boosters;
        private readonly AdPacing _adPacing;
        private readonly IWalletService _wallet;
        private readonly EconomyTuning _tuning;
        private readonly ILog _log;

        private GameObject _viewPrefab, _viewInstance, _board3DPrefab, _board3DInstance;
        private BoardView _view;
        private Board3DView _board3D;
        private int _level, _levelCount;
        private LevelData _data;
        private BoardModel _board;
        private bool _busy;                       // a dialog or ad is up: board taps are ignored
        private int _ftuePointAt = -1;            // level 1 step 1: the only stack a tap may go to
        private float _lastAdTime = -999f;
        private BoosterId? _pendingUnlock;      // shown by Intro, after the screen has entered
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public GameplayScreen(GameplayParam param, ILevelSource levels, IAssetService assets, IRenderLayerRegistry layers,
            ISceneService scenes, ILocalizationService loc, IDialogService dialogs, IAdsService ads, LevelProgressService progress,
            BoosterService boosters, AdPacing adPacing, IWalletService wallet, EconomyTuning tuning, ILog log)
        {
            _param = param; _levels = levels; _assets = assets; _layers = layers; _scenes = scenes; _loc = loc; _dialogs = dialogs;
            _ads = ads; _progress = progress; _boosters = boosters; _adPacing = adPacing; _wallet = wallet; _tuning = tuning; _log = log;
        }

        public override async UniTask OnLoadAsync(CancellationToken ct)
        {
            _levelCount = await _levels.CountAsync(ct);
            // CR-003: the board is 3D under WorldRoot (Renderer content anchored in game space, rule #16) …
            _board3DPrefab = await _assets.LoadAsync(AssetKeys.Gameplay.Board3DView, ct);
            _board3DInstance = Object.Instantiate(_board3DPrefab, WorldRoot(), false);
            _layers.Stamp(_board3DInstance, RenderLayers.GamePlay);   // stamped while empty: Stamp zeroes every node's z
            _board3D = _board3DInstance.GetComponent<Board3DView>();
            // … and the HUD, labels, boosters and tap areas are screen furniture on the Ui layer
            _viewPrefab = await _assets.LoadAsync(AssetKeys.Gameplay.BoardView, ct);
            _viewInstance = Object.Instantiate(_viewPrefab, _layers.GetHost(RenderLayers.Ui), false);
            _layers.Stamp(_viewInstance, RenderLayers.Ui);
            _view = _viewInstance.GetComponent<BoardView>();
            _view.StackTapped += OnStackTapped;
            _view.RestartPressed += () => Guard(ConfirmRestart);
            _view.PausePressed += () => Guard(Pause);
            _view.UndoPressed += () => Guard(() => UseBooster(BoosterId.Undo));
            _view.AddSlotPressed += () => Guard(() => UseBooster(BoosterId.AddSlot));
            await StartLevel(Mathf.Clamp(_param.Level, 1, Mathf.Max(1, _levelCount)), ct);
        }

        public override void OnEnter()
        {
            _log.Info($"[GameplayScreen] entered level {_level}.");
            Guard(Intro);
        }

        public override void OnBackRequested() => Guard(Pause);

        public override UniTask OnUnloadAsync(CancellationToken ct)
        {
            _cts.Cancel();
            if (_viewInstance != null) Object.Destroy(_viewInstance);
            if (_viewPrefab != null) _assets.Release(_viewPrefab);
            if (_board3DInstance != null) Object.Destroy(_board3DInstance);
            if (_board3DPrefab != null) _assets.Release(_board3DPrefab);
            _viewInstance = null; _viewPrefab = null; _view = null; _board3DInstance = null; _board3DPrefab = null; _board3D = null;
            return UniTask.CompletedTask;
        }

        // ── flow helpers ──────────────────────────────────────────────────────────────────────────────
        private void Guard(Func<UniTask> flow)
        {
            if (_busy) return;
            Run(flow).Forget();
        }

        private async UniTaskVoid Run(Func<UniTask> flow)
        {
            _busy = true;
            try { await flow(); }
            catch (OperationCanceledException) { }
            finally { _busy = false; }
        }

        private async UniTask StartLevel(int level, CancellationToken ct)
        {
            _level = level;
            _data = await _levels.LoadAsync(level, ct);
            _board = new BoardModel(_data, _tuning.BoardRules());
            var start = _progress.BeginLevel(level);
            _pendingUnlock = start.Unlocked;
            _log.Info($"[GameplayScreen] level {level} attempt {start.AttemptNo} (seed {_data.Seed}).");
            Draw();
        }

        // what follows a level start once the screen is up: the unlock popup (gift already saved), then the tutorial
        private async UniTask Intro()
        {
            if (_pendingUnlock.HasValue) { var id = _pendingUnlock.Value; _pendingUnlock = null; await ShowUnlock(id); Draw(); }
            Tutorial(afterTap: false, targetCompleted: false);
        }

        private async UniTask Restart(int level)
        {
            await StartLevel(level, _cts.Token);
            await Intro();
        }

        private void Draw()
        {
            var v = Visual();
            _board3D.Render(v);
            _view.Render(v);
            var layers = new int[v.Stacks.Length];
            for (int i = 0; i < layers.Length; i++) layers[i] = v.Stacks[i].Layer;
            _view.SetHitAreas(_board3D.StackWorldRects(v.Stacks.Length), layers);
            _view.SetHud(_loc.Get(LocKeys.GameplayLevel, _level), _data.Hard ? _loc.Get(LocKeys.GameplayHard) : null, Coins());
            _view.SetBoosters(BoosterButton(BoosterId.Undo), BoosterButton(BoosterId.AddSlot));
        }

        private BoosterVisual BoosterButton(BoosterId id)
        {
            long count = _boosters.Count(id);
            return new BoosterVisual
            {
                Visible = _progress.IsUnlocked(id),
                Enabled = BoosterService.CanApply(id, _board),
                Name = _loc.Get(id == BoosterId.Undo ? LocKeys.BoosterUndoName : LocKeys.BoosterAddSlotName),
                Badge = count > 0 ? count.ToString(CultureInfo.InvariantCulture) : null,
                Price = count > 0 ? null : _tuning.PriceOf(id).ToString(CultureInfo.InvariantCulture),
            };
        }

        // ── taps ──────────────────────────────────────────────────────────────────────────────────────
        private void OnStackTapped(int stack)
        {
            if (_busy || _board == null) return;
            if (_ftuePointAt >= 0 && stack != _ftuePointAt) return;      // ftue.l1.step1: only the pointed stack
            var steps = _board.Tap(stack);
            if (steps.Count == 0) return;                                // R-4: covered or finished — 6c adds the shake
            Draw();
            if (_board.Result == BoardResult.Won) Guard(Won);
            else if (_board.Result == BoardResult.Lost) Guard(Lost);
            else Tutorial(afterTap: true, targetCompleted: steps.Any(s => s.Kind == BoardStepKind.TargetCompleted));
        }

        // ── win ───────────────────────────────────────────────────────────────────────────────────────
        private async UniTask Won()
        {
            _view.HideTutorial();
            var win = _progress.CompleteLevel(_level, _data.Hard);      // persisted before anything is shown (G19)
            Draw();
            var args = new WinArgs(
                _loc.Get(_data.Hard ? LocKeys.WinTitleHard : LocKeys.WinTitle),
                _loc.Get(LocKeys.WinSubtitle, _level),
                _loc.Get(LocKeys.WinReward, win.Reward),
                _loc.Get(win.Final ? LocKeys.WinPlayAgain : LocKeys.WinNext),
                _loc.Get(LocKeys.WinHome),
                win.Final ? _loc.Get(LocKeys.HomeMoreSoon) : null,
                Fan());
            var result = await _dialogs.ShowAsync<WinDialog, WinChoice>(args, default, _cts.Token);
            if (result.Reason == DialogCloseReason.Aborted) return;
            if (result.Value == WinChoice.Home) { GoHome(); return; }
            await MaybeInterstitial(_level);
            await Restart(win.Saved ? win.NextLevel : _level);
        }

        private async UniTask MaybeInterstitial(int levelJustWon)
        {
            if (!_adPacing.ShouldShowAfterWin(levelJustWon, Time.realtimeSinceStartup - _lastAdTime)) return;
            if (!await _ads.IsReadyAsync(AdPlacements.InterstitialLevelEnd)) return;     // not ready: never block the player
            await _ads.ShowAsync(AdPlacements.InterstitialLevelEnd, _cts.Token);
            _lastAdTime = Time.realtimeSinceStartup;
            _adPacing.RecordInterstitialShown();
        }

        // ── lose / continue ───────────────────────────────────────────────────────────────────────────
        private async UniTask Lost()
        {
            _view.HideTutorial();
            bool adReady = await _ads.IsReadyAsync(AdPlacements.RewardedContinue);
            var args = new LoseArgs(
                _board.CanContinue,
                _loc.Get(LocKeys.LoseTitle),
                _loc.Get(adReady ? LocKeys.LoseContinueFree : LocKeys.AdsNotAvailable), adReady,
                _boosters.CanBuyContinue ? $"{_loc.Get(LocKeys.LoseContinueCoins)}  {_tuning.ContinuePrice}" : _loc.Get(LocKeys.BoosterBuyNotEnough),
                _boosters.CanBuyContinue,
                _loc.Get(LocKeys.LoseNoThanks),
                _loc.Get(LocKeys.LoseFailedTitle), _loc.Get(LocKeys.LoseSubtitle, _level), _loc.Get(LocKeys.LoseRetry), _loc.Get(LocKeys.LoseHome),
                Fan());
            var result = await _dialogs.ShowAsync<LoseDialog, LoseChoice>(args, default, _cts.Token);
            if (result.Reason == DialogCloseReason.Aborted) return;
            switch (result.Value)
            {
                case LoseChoice.ContinueAd:
                    if (await _ads.ShowAsync(AdPlacements.RewardedContinue, _cts.Token) == AdResult.Rewarded)   // reward only on completion (G19)
                    {
                        _lastAdTime = Time.realtimeSinceStartup;
                        await AfterContinue(_boosters.TryContinue(PaySource.Ad, _board).Count > 0);
                    }
                    else await Lost();                                    // ad cancelled: back to the offer
                    break;
                case LoseChoice.ContinueCoins:
                    await AfterContinue(_boosters.TryContinue(PaySource.Coins, _board).Count > 0);
                    break;
                case LoseChoice.Retry:
                    await Restart(_level);
                    break;
                default:
                    GoHome();
                    break;
            }
        }

        // called from inside the Lost flow (busy): chain the next flow directly instead of through Guard
        private async UniTask AfterContinue(bool continued)
        {
            Draw();
            if (!continued) return;
            if (_board.Result == BoardResult.Won) await Won();
            else if (_board.Result == BoardResult.Lost) await Lost();
        }

        // ── boosters ──────────────────────────────────────────────────────────────────────────────────
        private async UniTask UseBooster(BoosterId id)
        {
            if (!_progress.IsUnlocked(id) || !BoosterService.CanApply(id, _board)) return;
            if (_boosters.Count(id) > 0) { _boosters.TryUse(id, PaySource.Inventory, _board); Draw(); return; }
            bool adReady = await _ads.IsReadyAsync(AdPlacements.RewardedBooster);
            var args = new BoosterBuyArgs(
                _loc.Get(LocKeys.BoosterBuyTitle),
                _loc.Get(id == BoosterId.Undo ? LocKeys.BoosterUndoName : LocKeys.BoosterAddSlotName),
                _loc.Get(id == BoosterId.Undo ? LocKeys.BoosterUndoDesc : LocKeys.BoosterAddSlotDesc),
                id == BoosterId.Undo ? 0 : 1,
                _boosters.CanBuy(id) ? _tuning.PriceOf(id).ToString(CultureInfo.InvariantCulture) : _loc.Get(LocKeys.BoosterBuyNotEnough),
                _boosters.CanBuy(id),
                _loc.Get(adReady ? LocKeys.BoosterBuyFree : LocKeys.AdsNotAvailable), adReady);
            var result = await _dialogs.ShowAsync<BoosterBuyDialog, BoosterBuyChoice>(args, default, _cts.Token);
            if (result.Reason == DialogCloseReason.Aborted) return;
            if (result.Value == BoosterBuyChoice.Coins) _boosters.TryUse(id, PaySource.Coins, _board);
            else if (result.Value == BoosterBuyChoice.Ad && await _ads.ShowAsync(AdPlacements.RewardedBooster, _cts.Token) == AdResult.Rewarded)
            {
                _lastAdTime = Time.realtimeSinceStartup;
                _boosters.TryUse(id, PaySource.Ad, _board);
            }
            Draw();
        }

        private async UniTask ShowUnlock(BoosterId id)
        {
            var args = new BoosterUnlockArgs(
                _loc.Get(id == BoosterId.Undo ? LocKeys.UnlockUndoTitle : LocKeys.UnlockAddSlotTitle),
                _loc.Get(id == BoosterId.Undo ? LocKeys.BoosterUndoDesc : LocKeys.BoosterAddSlotDesc),
                id == BoosterId.Undo ? 0 : 1,
                "x" + _tuning.UnlockGift.ToString(CultureInfo.InvariantCulture),
                _loc.Get(LocKeys.UnlockGotIt));
            await _dialogs.ShowAsync<BoosterUnlockDialog, Unit>(args, default, _cts.Token);   // gift already saved
        }

        // ── pause / restart ───────────────────────────────────────────────────────────────────────────
        private async UniTask Pause()
        {
            var args = new PauseArgs(_loc.Get(LocKeys.PauseTitle), _loc.Get(LocKeys.PauseResume), _loc.Get(LocKeys.PauseRestart), _loc.Get(LocKeys.PauseHome));
            while (true)
            {
                var result = await _dialogs.ShowAsync<PauseDialog, PauseChoice>(args, default, _cts.Token);
                if (result.Reason == DialogCloseReason.Aborted) return;
                if (result.Value == PauseChoice.Settings)
                {
                    // Settings swaps in for Pause, then Pause comes back — two panels never overlap
                    var settings = new SettingsArgs(_loc.Get(LocKeys.SettingsTitle), _loc.Get(LocKeys.SettingsSound), _loc.Get(LocKeys.SettingsMusic),
                        _loc.Get(LocKeys.SettingsHaptics), _loc.Get(LocKeys.SettingsOn), _loc.Get(LocKeys.SettingsOff));
                    var closed = await _dialogs.ShowAsync<SettingsDialog, Unit>(settings, default, _cts.Token);
                    if (closed.Reason == DialogCloseReason.Aborted) return;
                    continue;
                }
                if (result.Value == PauseChoice.Restart) await Restart(_level);
                else if (result.Value == PauseChoice.Home) GoHome();
                return;
            }
        }

        private async UniTask ConfirmRestart()
        {
            var args = new RestartConfirmArgs(_loc.Get(LocKeys.GameplayRestartConfirm), _loc.Get(LocKeys.GameplayRestartNote),
                _loc.Get(LocKeys.GameplayRestartYes), _loc.Get(LocKeys.GameplayRestartNo));
            var result = await _dialogs.ShowAsync<RestartConfirmDialog, bool>(args, default, _cts.Token);
            if (result.Reason == DialogCloseReason.Aborted || !result.Value) return;
            await Restart(_level);
        }

        // ── FTUE (features/ftue.md) ───────────────────────────────────────────────────────────────────
        private void Tutorial(bool afterTap, bool targetCompleted)
        {
            _ftuePointAt = -1;
            _view.HideTutorial();
            if (_data.Ftue == "ftue.l1")
            {
                if (!_progress.IsFtueDone(FtueL1Step1))
                {
                    if (afterTap) { _progress.MarkFtueDone(FtueL1Step1); Tutorial(afterTap: false, targetCompleted: false); return; }
                    _ftuePointAt = FirstOpenStack();
                    PointAt(_ftuePointAt, LocKeys.FtueL1Step1);
                }
                else if (!_progress.IsFtueDone(FtueL1Step2))
                {
                    if (afterTap && targetCompleted) { _progress.MarkFtueDone(FtueL1Step2); return; }
                    PointAt(FirstOpenStack(), LocKeys.FtueL1Step2);
                }
            }
            else if (_data.Ftue == "ftue.l2")
            {
                if (_board.Buffer.Count > 0 && !_progress.IsFtueDone(FtueL2Buffer))
                {
                    _progress.MarkFtueDone(FtueL2Buffer);
                    _view.ShowTutorial(default, _loc.Get(LocKeys.FtueL2Buffer));
                }
                else if (afterTap && _progress.IsFtueDone(FtueL2Buffer) && !_progress.IsFtueDone(FtueL2Warn))
                {
                    _progress.MarkFtueDone(FtueL2Warn);
                    _view.ShowTutorial(default, _loc.Get(LocKeys.FtueL2Warn));
                }
            }
        }

        private void PointAt(int stack, LocKey text)
        {
            var rects = _board3D.StackWorldRects(_board.StackCount);
            _view.ShowTutorial(stack >= 0 ? rects[stack] : default, _loc.Get(text));
        }

        private int FirstOpenStack()
        {
            for (int i = 0; i < _board.StackCount; i++) if (_board.CanTap(i)) return i;
            return -1;
        }

        // ── helpers ───────────────────────────────────────────────────────────────────────────────────
        private void GoHome() => _scenes.LoadAsync(SceneKeys.Main, new MainParam(ColdBoot: false), SceneTransition.Replace).Forget();

        private string Coins() => _wallet.Balance(CardSlotResources.Coin).ToString("N0", CultureInfo.InvariantCulture);

        private int[] Fan() => _data.Targets.Select(t => t.Color).Distinct().Take(3).ToArray();

        private static Transform WorldRoot()
        {
            // No framework API for WorldRoot yet; the scene-name lookup is the documented answer (pf-world-space).
            // Newest first: on a Gameplay → Gameplay replace the outgoing scene is still loaded, and the first
            // match by name would parent the board into the scene about to be unloaded.
            for (int i = UnityEngine.SceneManagement.SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene.name != SceneKeys.Gameplay.Value) continue;
                foreach (var go in scene.GetRootGameObjects()) if (go.name == "WorldRoot") return go.transform;
            }
            throw new InvalidOperationException("Gameplay scene has no WorldRoot");
        }

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
