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
    /// (features/*.md), coins (C1) and the boosters Hand / Shuffle / Remove with their unlock, buy and pick flows (C2, features/boosters.md v2).
    /// Progress is saved before anything shows it (G18, G19), and a dialog that
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
        private StackPlacement[] _placements;
        private BoardModel _board;
        private int _paidRevives;                 // revives paid with coins this attempt: the next costs more (GDD v2.0 §7)
        private int _picking = -1;                // CR-012 C2: the booster (Hand / Remove) waiting for its target, or −1
        private bool _pickFromLose;               // that pick started on the lose dialog: cancelling goes back to it
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
            _view.BoosterPressed += OnBoosterPressed;
            _view.PoleTapped += OnPoleTapped;
            _view.PickCancelled += CancelPick;
            await StartLevel(Mathf.Clamp(_param.Level, 1, Mathf.Max(1, _levelCount)), ct);
        }

        public override void OnEnter()
        {
            _log.Info($"[GameplayScreen] entered level {_level}.");
            Guard(Intro);
        }

        public override void OnBackRequested()
        {
            if (_picking >= 0) { CancelPick(); return; }   // features/boosters.md: Back cancels a pick, nothing spent
            Guard(Pause);
        }

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
            _placements = StackPlacementRules.Resolve(_data);
            // R-15 / rule #14: Shuffle draws from a stream seeded here, once per attempt, and the seed is logged
            long shuffleSeed = DateTime.UtcNow.Ticks;
            _board = new BoardModel(_data, _tuning.BoardRules(), random: new Pcg32(shuffleSeed));
            EndPick();
            _paidRevives = 0;
            _grooves.Clear();
            _board3D.ResetAnimations();
            var start = _progress.BeginLevel(level);
            _log.Info($"[GameplayScreen] level {level} attempt {start.AttemptNo} (seed {_data.Seed}, shuffle seed {shuffleSeed}).");
            Draw();
        }

        // what follows a level start once the screen is up: a booster unlocked by this level (gift first), then the tutorial
        private async UniTask Intro()
        {
            foreach (var id in _progress.UnlockBoostersFor(_level))
            {
                Draw();
                if (!await ShowUnlock(id)) return;
            }
            Draw();
            Tutorial(afterTap: false, targetCompleted: false);
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
            _view.SetBoosters(Tiles());
        }

        // ── taps ──────────────────────────────────────────────────────────────────────────────────────
        private void OnStackTapped(int stack)
        {
            if (_busy || _board == null) return;
            if (_picking == (int)BoosterId.Hand) { PickStack(stack); return; }
            if (_picking >= 0) return;
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
                    // CR-012 C2: Remove on a waiting pole — cards fly to the back row and leave with that pole
                    case BoardStepKind.CardToQueued:
                        flights.Add(new CardFlight { Color = s.Color, FromStack = s.Stack, FromDepth = Depth(s.Stack), FromHeld = -1, ToSlot = s.Slot, ToIndex = s.Filled - 1, ToHeld = -1, ToHeldFinal = -1, After = -1, ToQueued = true });
                        break;
                    case BoardStepKind.BufferToQueued:
                        int qGroove = _grooves[s.BufferIndex], qFrom = arrival[s.BufferIndex];
                        _grooves.RemoveAt(s.BufferIndex);
                        arrival.RemoveAt(s.BufferIndex);
                        flights.Add(new CardFlight { Color = s.Color, FromStack = -1, FromHeld = qGroove, ToSlot = s.Slot, ToIndex = s.Filled - 1, ToHeld = -1, ToHeldFinal = -1, After = qFrom, ToQueued = true });
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
                Fan(), AnyUnlocked() ? Tiles() : null, _loc.Get(LocKeys.LoseUseBooster));
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
                case LoseChoice.Booster0:
                case LoseChoice.Booster1:
                case LoseChoice.Booster2:
                    await BoosterFromLose((BoosterId)(result.Value - LoseChoice.Booster0));
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

        // ── boosters (CR-012 C2, features/boosters.md v2) ─────────────────────────────────────────────

        private void OnBoosterPressed(int index)
        {
            if (_board == null || index < 0 || index > 2) return;
            if (_picking >= 0) { CancelPick(); return; }               // the active tile again = cancel
            if (_busy || _board.Result != BoardResult.Playing) return;
            var id = (BoosterId)index;
            if (!_progress.IsUnlocked(id)) return;                      // a locked tile does nothing
            if (_progress.Boosters(id) == 0) { Guard(() => Buy(id)); return; }
            if (id == BoosterId.Shuffle) UseShuffle();
            else BeginPick(id, fromLose: false);
        }

        // the lose dialog closed on a booster tile: use it, buy it, or pick its target; anything that does not change
        // the board brings the dialog back
        private async UniTask BoosterFromLose(BoosterId id)
        {
            if (!_progress.IsUnlocked(id)) { await Lost(); return; }
            if (_progress.Boosters(id) == 0) { await Buy(id); await Lost(); return; }
            if (id == BoosterId.Shuffle)
            {
                if (!_board.CanShuffle || !_progress.TryUseBooster(id)) { await Lost(); return; }
                await AfterRescue(_board.Shuffle());
                return;
            }
            if (id == BoosterId.Hand && !AnyHandTarget()) { await Lost(); return; }   // overflow: the pending cards come first
            BeginPick(id, fromLose: true);                              // the flow ends here; the pick finishes it
        }

        private void UseShuffle()
        {
            if (!_board.CanShuffle || !_progress.TryUseBooster(BoosterId.Shuffle)) return;
            AfterBooster(_board.Shuffle());
        }

        private void BeginPick(BoosterId id, bool fromLose)
        {
            _picking = (int)id;
            _pickFromLose = fromLose;
            _view.HideTutorial();
            if (id == BoosterId.Hand)
            {
                _view.ShowPick(_picking, _loc.Get(LocKeys.BoosterPickHand), _board3D.TrayWorldRect(), bannerAbove: true);
                return;
            }
            // Remove: the front pole of each slot and the one waiting right behind it (the poles the player can see)
            var (front, back) = _board3D.PoleWorldRects(_board.SlotCount);
            var rects = new List<Rect>(); var slots = new List<int>(); var depths = new List<int>();
            Rect band = default;
            for (int s = 0; s < _board.SlotCount; s++)
            {
                if (_board.CanRemove(s, 0) && front[s].width > 0f) { rects.Add(front[s]); slots.Add(s); depths.Add(0); }
                if (_board.CanRemove(s, 1) && back[s].width > 0f) { rects.Add(back[s]); slots.Add(s); depths.Add(1); }
            }
            foreach (var r in rects) band = band.width > 0f ? Rect.MinMaxRect(Mathf.Min(band.xMin, r.xMin), Mathf.Min(band.yMin, r.yMin), Mathf.Max(band.xMax, r.xMax), Mathf.Max(band.yMax, r.yMax)) : r;
            _view.ShowPick(_picking, _loc.Get(LocKeys.BoosterPickRemove), band, bannerAbove: false, rects.ToArray(), slots.ToArray(), depths.ToArray());
        }

        private void CancelPick()
        {
            if (_picking < 0) return;
            bool fromLose = _pickFromLose;
            EndPick();
            if (fromLose) Guard(Lost);
        }

        private void EndPick()
        {
            _picking = -1;
            _pickFromLose = false;
            _view?.HidePick();
        }

        private void PickStack(int stack)
        {
            if (!_board.CanHand(stack) || !_progress.TryUseBooster(BoosterId.Hand)) return;
            EndPick();
            AfterBooster(_board.Hand(stack));
        }

        private void OnPoleTapped(int slot, int depth)
        {
            if (_busy || _picking != (int)BoosterId.Remove) return;
            if (!_board.CanRemove(slot, depth) || !_progress.TryUseBooster(BoosterId.Remove)) return;
            EndPick();
            AfterBooster(_board.Remove(slot, depth));
        }

        // a booster is not a tap, but ends like one: animate, then win / lose as usual (a pick from the lose screen
        // that leaves the board still lost brings the lose dialog back)
        private void AfterBooster(IReadOnlyList<BoardStep> steps)
        {
            if (steps.Count == 0) return;
            Draw(Flights(steps));
            if (_board.Result == BoardResult.Won) Guard(Won);
            else if (_board.Result == BoardResult.Lost) Guard(Lost);
        }

        private bool AnyHandTarget()
        {
            for (int i = 0; i < _board.StackCount; i++) if (_board.CanHand(i)) return true;
            return false;
        }

        private bool AnyUnlocked() => _progress.IsUnlocked(BoosterId.Hand) || _progress.IsUnlocked(BoosterId.Shuffle) || _progress.IsUnlocked(BoosterId.Remove);

        // out of a booster: buy one with coins or watch an ad for one (G18 / G19); the player then taps it to use it
        private async UniTask Buy(BoosterId id)
        {
            while (true)
            {
                bool adReady = await _ads.IsReadyAsync(AdPlacements.RewardedBooster);
                int price = _tuning.Price(id);
                var args = new BoosterBuyArgs(_loc.Get(LocKeys.BoosterBuyTitle), _loc.Get(BoosterName(id)), _loc.Get(BoosterTip(id)), (int)id,
                    _loc.Get(LocKeys.BoosterPrice, price), _progress.Coins >= price, _loc.Get(LocKeys.BoosterBuyNotEnough),
                    adReady ? _loc.Get(LocKeys.BoosterBuyAd) : _loc.Get(LocKeys.AdsNotAvailable), adReady, Coins());
                var result = await _dialogs.ShowAsync<BoosterBuyDialog, BoosterBuyChoice>(args, default, _cts.Token);
                if (result.Reason == DialogCloseReason.Aborted || result.Value == BoosterBuyChoice.Close) return;
                if (result.Value == BoosterBuyChoice.Coins)
                {
                    if (!_progress.TryBuyBooster(id)) continue;          // short or not saved: nothing changed, offer again
                }
                else if (!await Rewarded(AdPlacements.RewardedBooster) || !_progress.GrantBoosterFromAd(id)) continue;
                Draw();
                return;
            }
        }

        // the unlock popup at the first start of level 5 / 8 / 10; false if the screen went away under it
        private async UniTask<bool> ShowUnlock(BoosterId id)
        {
            var tiles = Tiles();
            tiles[(int)id].Active = true;
            var args = new BoosterUnlockArgs(_loc.Get(LocKeys.BoosterUnlockTitle, _loc.Get(BoosterName(id))), _loc.Get(BoosterTip(id)),
                _loc.Get(LocKeys.BoosterUnlockGift, _tuning.BoosterUnlockGift), _loc.Get(LocKeys.BoosterUnlockOk), (int)id, tiles);
            var result = await _dialogs.ShowAsync<BoosterUnlockDialog, Unit>(args, default, _cts.Token);
            return result.Reason != DialogCloseReason.Aborted;
        }

        private static LocKey BoosterName(BoosterId id) =>
            id == BoosterId.Hand ? LocKeys.BoosterHandName : id == BoosterId.Shuffle ? LocKeys.BoosterShuffleName : LocKeys.BoosterRemoveName;

        private static LocKey BoosterTip(BoosterId id) =>
            id == BoosterId.Hand ? LocKeys.BoosterHandTip : id == BoosterId.Shuffle ? LocKeys.BoosterShuffleTip : LocKeys.BoosterRemoveTip;

        // the bar as the controller decides it: locked ("Lv N"), owned (count badge) or out (coin price)
        private BoosterTileVisual[] Tiles()
        {
            var tiles = new BoosterTileVisual[3];
            for (int i = 0; i < 3; i++)
            {
                var id = (BoosterId)i;
                int n = _progress.Boosters(id);
                bool open = _progress.IsUnlocked(id);
                tiles[i] = new BoosterTileVisual
                {
                    Name = _loc.Get(BoosterName(id)),
                    Locked = open ? null : _loc.Get(LocKeys.BoosterLocked, _tuning.UnlockLevel(id)),
                    Badge = open && n > 0 ? _loc.Get(LocKeys.BoosterCount, n) : null,
                    Price = open && n == 0 ? _loc.Get(LocKeys.BoosterPrice, _tuning.Price(id)) : null,
                    Active = _picking == i,
                };
            }
            return tiles;
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
                // Every level uses the same automatic placement rule; poses stay fixed during the attempt.
                var placement = _placements[i];
                var origin = _data.Stacks[placement.Root];
                int offset = placement.Offset;
                v.Stacks[i] = new StackVisual { X = origin.X, Y = origin.Y, W = origin.W, H = origin.H, Layer = spec.Layer, PoseLayer = origin.Layer, AuthoredCount = spec.Count, Fan = origin.Fan, Spread = true, SpreadDirection = placement.Direction, SpreadAngle = origin.SpreadAngle, PoseRoot = placement.Root, PoseOffset = offset, PoseCount = origin.Count, Colors = colors, Covered = n > 0 && _board.IsCovered(i), Hint = _board.CanTap(i) };   // Phat: every stack a tap can take from
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
