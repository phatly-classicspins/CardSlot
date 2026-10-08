using System;
using System.Collections.Generic;
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
    /// into rules and the model into visuals, and runs the flows of milestone 6b — win/lose dialogs with
    /// Revive and RV Slot (rewarded ads, CR-012 stage B), pause/settings, restart confirm and the level 1–2 tutorial
    /// (features/*.md). No boosters and no coins yet (stage C). Progress is saved before anything shows it (G18, G19), and a dialog that
    /// comes back <see cref="DialogCloseReason.Aborted"/> changes nothing.
    /// </summary>
    public sealed class GameplayScreen : ScreenBase
    {
        private const string FtueL1Step1 = "ftue.l1.step1", FtueL1Step2 = "ftue.l1.step2", FtueL2Buffer = "ftue.l2.buffer", FtueL2Warn = "ftue.l2.warn";
        // the level 1–2 tutorial is switched off for now (Phat: "hiện tại chưa cần ftue"); true brings it back
        private const bool FtueEnabled = false;

        private readonly GameplayParam _param;
        private readonly ILevelSource _levels;
        private readonly IAssetService _assets;
        private readonly IRenderLayerRegistry _layers;
        private readonly ISceneService _scenes;
        private readonly ILocalizationService _loc;
        private readonly IDialogService _dialogs;
        private readonly IAdsService _ads;
        private readonly LevelProgressService _progress;
        private readonly AdPacing _adPacing;
        private readonly EconomyTuning _tuning;
        private readonly ILog _log;

        private GameObject _viewPrefab, _viewInstance, _board3DPrefab, _board3DInstance;
        private BoardView _view;
        private Board3DView _board3D;
        private int _level, _levelCount;
        private LevelData _data;
        private BoardModel _board;
        private int _paidRevives;                 // revives paid with coins this attempt: the next costs more (GDD v2.0 §7)
        private bool _busy;                       // a dialog or ad is up: board taps are ignored
        private int _ftuePointAt = -1;            // level 1 step 1: the only stack a tap may go to
        private float _lastAdTime = -999f;
        private readonly List<int> _grooves = new List<int>();   // the groove of each holding-area card, in buffer order
        // CR-007: a result dialog opens a beat after the board has stopped moving
        private const double DialogBeat = 0.3;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public GameplayScreen(GameplayParam param, ILevelSource levels, IAssetService assets, IRenderLayerRegistry layers,
            ISceneService scenes, ILocalizationService loc, IDialogService dialogs, IAdsService ads, LevelProgressService progress,
            AdPacing adPacing, EconomyTuning tuning, ILog log)
        {
            _param = param; _levels = levels; _assets = assets; _layers = layers; _scenes = scenes; _loc = loc; _dialogs = dialogs;
            _ads = ads; _progress = progress; _adPacing = adPacing; _tuning = tuning; _log = log;
        }

        public override async UniTask OnLoadAsync(CancellationToken ct)
        {
            _levelCount = await _levels.CountAsync(ct);
            _progress.EnsureStartCoins();             // CR-012 C1: once per install (also granted from Home)
            // CR-003: the board is 3D under WorldRoot (Renderer content anchored in game space, rule #16) …
            _board3DPrefab = await _assets.LoadAsync(AssetKeys.Gameplay.Board3DView, ct);
            _board3DInstance = Object.Instantiate(_board3DPrefab, WorldRoot(), false);
            _layers.Stamp(_board3DInstance, RenderLayers.GamePlay);   // stamped while empty: Stamp zeroes every node's z
            _board3D = _board3DInstance.GetComponent<Board3DView>();
            // … and the HUD, labels and tap areas are screen furniture on the Ui layer
            _viewPrefab = await _assets.LoadAsync(AssetKeys.Gameplay.BoardView, ct);
            _viewInstance = Object.Instantiate(_viewPrefab, _layers.GetHost(RenderLayers.Ui), false);
            _layers.Stamp(_viewInstance, RenderLayers.Ui);
            _view = _viewInstance.GetComponent<BoardView>();
            _view.StackTapped += OnStackTapped;
            _view.RestartPressed += () => Guard(ConfirmRestart);
            _view.PausePressed += () => Guard(Pause);
            _view.RvSlotPressed += () => Guard(RvSlotDuringPlay);
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
            _paidRevives = 0;
            _grooves.Clear();
            var start = _progress.BeginLevel(level);
            _log.Info($"[GameplayScreen] level {level} attempt {start.AttemptNo} (seed {_data.Seed}).");
            Draw();
        }

        // what follows a level start once the screen is up: the tutorial
        private UniTask Intro()
        {
            Tutorial(afterTap: false, targetCompleted: false);
            return UniTask.CompletedTask;
        }

        private async UniTask Restart(int level)
        {
            await StartLevel(level, _cts.Token);
            await Intro();
        }

        private void Draw(CardFlight[] flights = null)
        {
            var v = Visual();
            _board3D.Render(v, flights);
            _view.Render(v);
            var layers = new int[v.Stacks.Length];
            for (int i = 0; i < layers.Length; i++) layers[i] = v.Stacks[i].Layer;
            _view.SetHitAreas(_board3D.StackWorldRects(v.Stacks.Length), layers);
            _view.SetHud(_loc.Get(LocKeys.GameplayLevel, _level), Coins());
        }

        // ── taps ──────────────────────────────────────────────────────────────────────────────────────
        private void OnStackTapped(int stack)
        {
            if (_busy || _board == null) return;
            if (_ftuePointAt >= 0 && stack != _ftuePointAt) return;      // ftue.l1.step1: only the pointed stack
            var steps = _board.Tap(stack);
            if (steps.Count == 0) return;                                // R-4: covered or finished — 6c adds the shake
            Draw(Flights(steps));
            if (_board.Result == BoardResult.Won) Guard(Won);
            else if (_board.Result == BoardResult.Lost) Guard(Lost);
            else Tutorial(afterTap: true, targetCompleted: steps.Any(s => s.Kind == BoardStepKind.TargetCompleted));
        }

        /// <summary>
        /// CR-007: turn one change's steps into the cards the view flies, in rule order, and keep the holding
        /// grooves up to date. A run leaves its stack from the top; a holding card keeps its groove until it leaves
        /// and a new one takes the lowest free groove (Phat: no re-sorting); a card for a peg that already filled
        /// up in this change carries how many times (<see cref="CardFlight.SwapGen"/>), and the cards that filled
        /// a peg are marked so the view lets them leave with it.
        /// </summary>
        private CardFlight[] Flights(IReadOnlyList<BoardStep> steps)
        {
            var flights = new List<CardFlight>();
            var arrival = new List<int>();                // per holding card: the flight that put it there in this change, or −1
            for (int i = 0; i < _grooves.Count; i++) arrival.Add(-1);
            var filling = new Dictionary<int, List<int>>();
            var gen = new Dictionary<int, int>();         // per slot: how many times its peg filled up so far
            var depth = new Dictionary<int, int>();       // per stack: cards already taken from its top
            int Gen(int slot) => gen.TryGetValue(slot, out int g) ? g : 0;
            int Depth(int stack) { depth.TryGetValue(stack, out int d); depth[stack] = d + 1; return d; }
            void Into(int slot, int index)
            {
                if (!filling.TryGetValue(slot, out var list)) filling[slot] = list = new List<int>();
                list.Add(index);
            }
            foreach (var s in steps)
            {
                switch (s.Kind)
                {
                    case BoardStepKind.CardToTarget:
                        flights.Add(new CardFlight { Color = s.Color, FromStack = s.Stack, FromDepth = Depth(s.Stack), FromHeld = -1, ToSlot = s.Slot, ToIndex = s.Filled - 1, ToHeld = -1, ToHeldFinal = -1, After = -1, SwapGen = Gen(s.Slot) });
                        Into(s.Slot, flights.Count - 1);
                        break;
                    case BoardStepKind.CardToBuffer:
                        int free = 0;
                        while (_grooves.Contains(free)) free++;
                        _grooves.Add(free);
                        arrival.Add(flights.Count);
                        flights.Add(new CardFlight { Color = s.Color, FromStack = s.Stack, FromDepth = Depth(s.Stack), FromHeld = -1, ToSlot = -1, ToHeld = free, ToHeldFinal = -1, After = -1 });
                        break;
                    case BoardStepKind.BufferToTarget:
                        int groove = _grooves[s.BufferIndex], from = arrival[s.BufferIndex];
                        _grooves.RemoveAt(s.BufferIndex);
                        arrival.RemoveAt(s.BufferIndex);
                        flights.Add(new CardFlight
                        {
                            Color = s.Color, FromStack = -1, FromHeld = groove, ToSlot = s.Slot, ToIndex = s.Filled - 1,
                            ToHeld = -1, ToHeldFinal = -1, After = from, SwapGen = Gen(s.Slot),
                        });
                        Into(s.Slot, flights.Count - 1);
                        break;
                    case BoardStepKind.TargetCompleted:
                        if (filling.TryGetValue(s.Slot, out var done))
                        {
                            foreach (int i in done) { var f = flights[i]; f.Completes = true; flights[i] = f; }
                            done.Clear();
                        }
                        gen[s.Slot] = Gen(s.Slot) + 1;
                        break;
                }
            }
            for (int j = 0; j < arrival.Count; j++)
                if (arrival[j] >= 0) { var f = flights[arrival[j]]; f.ToHeldFinal = _grooves[j]; flights[arrival[j]] = f; }
            return flights.ToArray();
        }

        private async UniTask AnimationsDone()
        {
            await UniTask.WaitUntil(() => _board3D == null || !_board3D.IsAnimating, cancellationToken: _cts.Token);
            await UniTask.Delay(TimeSpan.FromSeconds(DialogBeat), cancellationToken: _cts.Token);
        }

        // ── win ───────────────────────────────────────────────────────────────────────────────────────
        private async UniTask Won()
        {
            _view.HideTutorial();
            var win = _progress.CompleteLevel(_level);      // progress + the plain reward persisted before anything is shown (G19)
            await AnimationsDone();   // let the last cards land and the peg leave (CR-007)
            Draw();
            while (true)
            {
                bool adReady = win.Reward > 0 && await _ads.IsReadyAsync(AdPlacements.RewardedWinDouble);
                var args = new WinArgs(
                    _loc.Get(LocKeys.WinTitle),
                    _loc.Get(LocKeys.WinSubtitle, _level),
                    _loc.Get(LocKeys.WinReward, win.Reward),
                    adReady ? _loc.Get(LocKeys.WinClaim, _tuning.WinAdMultiplier) : _loc.Get(LocKeys.AdsNotAvailable), adReady,
                    _loc.Get(win.Final ? LocKeys.WinPlayAgain : LocKeys.WinNext),
                    _loc.Get(LocKeys.WinHome),
                    win.Final ? _loc.Get(LocKeys.HomeMoreSoon) : null,
                    Coins(), Fan());
                var result = await _dialogs.ShowAsync<WinDialog, WinChoice>(args, default, _cts.Token);
                if (result.Reason == DialogCloseReason.Aborted) return;
                if (result.Value == WinChoice.Home) { GoHome(); return; }
                if (result.Value == WinChoice.ClaimDouble)
                {
                    if (!await Rewarded(AdPlacements.RewardedWinDouble)) continue;   // ad cancelled: back to the dialog
                    _progress.GrantWinBonus(win.Reward);
                    Draw();
                }
                break;
            }
            await MaybeInterstitial(_level);
            await Restart(win.Saved ? win.NextLevel : _level);
        }

        private string Coins() => _loc.Get(LocKeys.HudCoins, _progress.Coins);

        private async UniTask MaybeInterstitial(int levelJustWon)
        {
            if (!_adPacing.ShouldShowAfterWin(levelJustWon, Time.realtimeSinceStartup - _lastAdTime)) return;
            if (!await _ads.IsReadyAsync(AdPlacements.InterstitialLevelEnd)) return;     // not ready: never block the player
            await _ads.ShowAsync(AdPlacements.InterstitialLevelEnd, _cts.Token);
            _lastAdTime = Time.realtimeSinceStartup;
            _adPacing.RecordInterstitialShown();
        }

        // ── lose: Revive / RV Slot (CR-012 stage B) ───────────────────────────────────────────────────
        private async UniTask Lost()
        {
            _view.HideTutorial();
            await AnimationsDone();
            bool adReady = await _ads.IsReadyAsync(AdPlacements.RewardedRevive);
            long price = _tuning.RevivePriceAfter(_paidRevives);
            var args = new LoseArgs(
                _loc.Get(_board.Loss == BoardLoss.Stuck ? LocKeys.LoseTitleStuck : LocKeys.LoseTitle),
                Coins(),
                _board.CanRevive ? _loc.Get(LocKeys.LoseRevive) : null,
                _loc.Get(LocKeys.LoseRevivePrice, price),
                _progress.Coins >= price,
                _loc.Get(LocKeys.LoseReviveNote, _board.ReviveRemoves),
                _board.CanRvSlot ? _loc.Get(LocKeys.LoseRvSlot, _board.RvSlotAmount) : null,
                adReady,
                _loc.Get(LocKeys.LoseNoThanks),
                _loc.Get(LocKeys.LoseFailedTitle), _loc.Get(LocKeys.LoseSubtitle, _level), _loc.Get(LocKeys.LoseRetry), _loc.Get(LocKeys.LoseHome),
                Fan());
            var result = await _dialogs.ShowAsync<LoseDialog, LoseChoice>(args, default, _cts.Token);
            if (result.Reason == DialogCloseReason.Aborted) return;
            switch (result.Value)
            {
                case LoseChoice.ReviveCoins:
                    if (_progress.TrySpendCoins(price)) { _paidRevives++; await AfterRescue(_board.Revive()); }
                    else await Lost();                                    // could not pay (or save): back to the offer
                    break;
                case LoseChoice.ReviveAd:
                    if (await Rewarded(AdPlacements.RewardedRevive)) await AfterRescue(_board.Revive());
                    else await Lost();                                    // ad cancelled: back to the offer
                    break;
                case LoseChoice.RvSlotAd:
                    if (await Rewarded(AdPlacements.RewardedRvSlot)) await AfterRescue(_board.RvSlot());
                    else await Lost();
                    break;
                case LoseChoice.Retry:
                    await Restart(_level);
                    break;
                default:
                    GoHome();
                    break;
            }
        }

        // R-23 from the board's button while playing: more holding cells, nothing else moves
        private async UniTask RvSlotDuringPlay()
        {
            if (_board == null || _board.Result != BoardResult.Playing || !_board.CanRvSlot) return;
            if (!await _ads.IsReadyAsync(AdPlacements.RewardedRvSlot)) return;   // not ready: the button simply does nothing
            if (await Rewarded(AdPlacements.RewardedRvSlot)) { _board.RvSlot(); Draw(); }
        }

        // reward only on completion (G19)
        private async UniTask<bool> Rewarded(AdPlacement placement)
        {
            if (await _ads.ShowAsync(placement, _cts.Token) != AdResult.Rewarded) return false;
            _lastAdTime = Time.realtimeSinceStartup;
            return true;
        }

        // called from inside the Lost flow (busy): chain the next flow directly instead of through Guard
        private async UniTask AfterRescue(IReadOnlyList<BoardStep> steps)
        {
            Draw(Flights(steps));
            if (_board.Result == BoardResult.Won) await Won();
            else if (_board.Result == BoardResult.Lost) await Lost();
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
            if (!FtueEnabled) return;
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
                Upcoming = Enumerable.Range(0, _board.SlotCount).Select(_board.NextColorBehind).ToArray(),   // CR-009: the peg behind each slot (−1 = none)
                Buffer = _board.Buffer.ToArray(),
                BufferCapacity = _board.BufferCapacity,
                BufferGrooves = _grooves.ToArray(),
                BufferWarn = false,                         // the holding area never turns red (Phat)
                HoldingLabel = _loc.Get(LocKeys.GameplayHoldingArea),
                BufferCountLabel = _loc.Get(LocKeys.GameplayBufferCount, _board.Buffer.Count, _board.BufferCapacity),
                NextLabel = _loc.Get(LocKeys.GameplayNext),
                // R-23: offered on the board only while playing; after a loss the lose dialog offers it
                RvSlotLabel = _board.Result == BoardResult.Playing && _board.CanRvSlot ? _loc.Get(LocKeys.GameplayRvSlot, _board.RvSlotAmount) : null,
            };
            for (int i = 0; i < _board.StackCount; i++)
            {
                var spec = _data.Stacks[i];
                int n = _board.Remaining(i);
                var colors = new int[n];
                for (int d = 0; d < n; d++) colors[d] = _board.CardAt(i, d);
                v.Stacks[i] = new StackVisual { X = spec.X, Y = spec.Y, W = spec.W, H = spec.H, Layer = spec.Layer, Colors = colors, Covered = n > 0 && _board.IsCovered(i), Hint = _board.CanTap(i) };   // Phat: every stack a tap can take from
            }
            for (int s = 0; s < _board.SlotCount; s++)
            {
                bool has = _board.SlotHasTarget(s);
                v.Targets[s] = new TargetVisual
                {
                    Has = has,
                    Color = has ? _board.SlotColor(s) : 0,
                    Filled = has ? _board.SlotFilled(s) : 0,
                    Capacity = has ? _board.SlotCapacity(s) : 0,
                    CountLabel = has ? _loc.Get(LocKeys.GameplayCount, _board.SlotFilled(s), _board.SlotCapacity(s)) : null,
                };
            }
            return v;
        }
    }
}
