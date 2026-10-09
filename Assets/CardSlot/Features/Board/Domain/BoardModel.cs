using System;
using System.Collections.Generic;
using System.Text;
using ClassicSpins.PrototypeFramework.Domain;

namespace Game.Domain
{
    public enum BoardResult { Playing, Won, Lost }

    /// <summary>Why an attempt is <see cref="BoardResult.Lost"/>: a card with nowhere to go (R-12) or no tap
    /// left that avoids that (R-13).</summary>
    public enum BoardLoss { None, Overflow, Stuck }

    public enum BoardStepKind
    {
        /// <summary>A card left <c>Stack</c> and landed on the target in <c>Slot</c> (R-7.1).</summary>
        CardToTarget,
        /// <summary>A card left <c>Stack</c> and went to the end of the buffer at <c>BufferIndex</c> (R-7.2).</summary>
        CardToBuffer,
        /// <summary>The buffer card at <c>BufferIndex</c> (index before removal) flew to <c>Slot</c> (R-10).</summary>
        BufferToTarget,
        /// <summary>The target in <c>Slot</c> filled up and left (R-9).</summary>
        TargetCompleted,
        /// <summary>The next queued target entered <c>Slot</c> (R-9).</summary>
        TargetEntered,
        /// <summary><c>Stack</c> ran out of cards (R-5).</summary>
        StackEmptied,
        /// <summary>A card from <c>Stack</c> had nowhere to go: the buffer was full (R-12).</summary>
        Overflow,
        Won,
        Lost,
        /// <summary>R-19 Remove on a waiting target: a card left <c>Stack</c> for the target <c>Depth</c> places behind <c>Slot</c>.</summary>
        CardToQueued,
        /// <summary>R-19 Remove on a waiting target: the buffer card at <c>BufferIndex</c> went to it.</summary>
        BufferToQueued,
        /// <summary>R-19 Remove on a waiting target: it filled up and left; the targets behind it moved up one place.</summary>
        QueuedTargetCompleted,
        /// <summary>R-21 Shuffle: the targets that held no card changed places (redraw the pegs).</summary>
        Shuffled,
    }

    /// <summary>One thing that happened while resolving a tap, in order — the view replays these
    /// (R-8: the model resolves instantly; animation only shows the result).</summary>
    public readonly struct BoardStep
    {
        public readonly BoardStepKind Kind;
        public readonly int Stack, Slot, BufferIndex, Color, Filled, Capacity;
        /// <summary>For the <c>*Queued*</c> kinds: which waiting target behind <see cref="Slot"/> (0 = right behind it).</summary>
        public readonly int Depth;

        public BoardStep(BoardStepKind kind, int stack = -1, int slot = -1, int bufferIndex = -1, int color = -1, int filled = 0, int capacity = 0, int depth = -1)
        {
            Kind = kind; Stack = stack; Slot = slot; BufferIndex = bufferIndex; Color = color; Filled = filled; Capacity = capacity; Depth = depth;
        }

        public override string ToString() =>
            $"{Kind}(stack {Stack}, slot {Slot}, depth {Depth}, buf {BufferIndex}, colour {Color}, {Filled}/{Capacity})";
    }

    /// <summary>Revive / RV Slot amounts and limits (GDD v2.0 §2.8, §7) — tuning data, passed in by the caller.</summary>
    public sealed class BoardRules
    {
        /// <summary>R-22: targets a revive removes for free before placing the pending cards.</summary>
        public int ReviveRemoves = 2;
        public int ReviveMaxPerAttempt = 3;
        /// <summary>R-23: holding cells one RV Slot adds.</summary>
        public int RvSlotAmount = 8;
        public int RvSlotMaxPerAttempt = 2;
    }

    /// <summary>
    /// The rules of one attempt at a level (GDD v2.0 §2, R-2…R-15, boosters R-19 Remove / R-20 Hand / R-21 Shuffle, R-22 Revive,
    /// R-23 RV Slot). Engine-free and deterministic: the final state depends only on the level, the sequence of taps and
    /// boosters, and the seed of the <see cref="IRandom"/> Shuffle draws from (R-15). Every mutation returns the steps
    /// the view needs to animate it; no rule waits on a view.
    /// </summary>
    public sealed class BoardModel
    {
        private struct Target
        {
            public bool Has;
            public int Id, Color, Capacity, Filled;
        }

        private sealed class Snapshot
        {
            public int[] Removed;
            public Target[] Slots;
            public List<int>[] Queue;
            public int[] Buffer;
            public BoardResult Result;
            public BoardLoss Loss;
        }

        private readonly LevelData _level;
        private readonly BoardRules _rules;
        private readonly int[] _removed;          // cards taken from the top of each stack
        private readonly Target[] _slots;
        private readonly List<int> _buffer = new List<int>();
        private readonly Stack<Snapshot> _history = new Stack<Snapshot>();
        private readonly List<BoardStep> _steps = new List<BoardStep>();
        // CR-009: the queue is dealt into columns — target i waits behind slot i % Slots — and a completed
        // slot takes the next target of its own column (the reference: the peg behind moves forward)
        private readonly List<int>[] _queue;     // per column: the waiting targets (indices into Level.Targets), nearest first
        private readonly IRandom _random;         // R-15: Shuffle is the only randomness while playing (seeded by the caller)
        private int _revivesUsed, _rvSlotsUsed;
        // the cards left unplaced when the buffer overflowed (R-7.2) — Revive / RV Slot place them
        private int _pendingStack = -1, _pendingColor = -1, _pendingCount;

        public BoardModel(LevelData level, BoardRules rules = null, int? bufferCapacityOverride = null, IRandom random = null)
        {
            var errors = LevelValidator.Validate(level);
            if (errors.Count > 0) throw new ArgumentException($"Level '{level?.Id}' is invalid: {string.Join("; ", errors)}");
            _level = level;
            _rules = rules ?? new BoardRules();
            _random = random;
            _removed = new int[level.Stacks.Count];
            _slots = new Target[level.Slots];
            BufferCapacity = bufferCapacityOverride ?? level.BufferCapacity;
            // R-2: the first n targets of the queue fill slots 0..n-1 in order
            _queue = new List<int>[_slots.Length];
            for (int s = 0; s < _slots.Length; s++) _queue[s] = new List<int>();
            for (int t = 0; t < level.Targets.Count; t++) _queue[t % _slots.Length].Add(t);
            for (int s = 0; s < _slots.Length; s++)
                if (HasNext(s)) _slots[s] = NextTarget(s);
        }

        public LevelData Level => _level;
        public BoardResult Result { get; private set; } = BoardResult.Playing;
        public BoardLoss Loss { get; private set; }
        public int BufferCapacity { get; private set; }
        /// <summary>R-13: after each tap, check whether every remaining tap overflows. The solver turns this off —
        /// it explores every tap anyway, and the check would multiply its work.</summary>
        internal bool LookAhead { get; set; } = true;
        public IReadOnlyList<int> Buffer => _buffer;
        public int StackCount => _removed.Length;
        public int SlotCount => _slots.Length;
        public int QueuedTargets { get { int n = 0; foreach (var q in _queue) n += q.Count; return n; } }
        public int TapCount { get; private set; }
        /// <summary>R-22: after any loss, while revives are left this attempt.</summary>
        public bool CanRevive => Result == BoardResult.Lost && _revivesUsed < _rules.ReviveMaxPerAttempt;
        /// <summary>R-23: while playing, or after a loss that more room can undo (cards still to place, or a stack
        /// that can still be tapped).</summary>
        public bool CanRvSlot => _rvSlotsUsed < _rules.RvSlotMaxPerAttempt
                                 && (Result == BoardResult.Playing || Result == BoardResult.Lost && (_pendingCount > 0 || AnyTappable()));
        public int RvSlotsLeft => Math.Max(0, _rules.RvSlotMaxPerAttempt - _rvSlotsUsed);
        public int RvSlotAmount => _rules.RvSlotAmount;
        public int ReviveRemoves => _rules.ReviveRemoves;

        public int Remaining(int stack) => _level.Stacks[stack].Count - _removed[stack];
        /// <summary>Colour of the card <paramref name="depth"/> below the current top (0 = top) — every card of a
        /// stack has the stack's colour (R-1c).</summary>
        public int CardAt(int stack, int depth) => _level.Stacks[stack].Color;
        public bool SlotHasTarget(int slot) => _slots[slot].Has;
        public int SlotColor(int slot) => _slots[slot].Color;
        public int SlotFilled(int slot) => _slots[slot].Filled;
        public int SlotCapacity(int slot) => _slots[slot].Capacity;
        /// <summary>CR-009: the colour of the target waiting behind <paramref name="slot"/> (the back-row peg), or −1.</summary>
        public int NextColorBehind(int slot) => QueuedColor(slot, 0);
        /// <summary>Colour of the <paramref name="index"/>-th target waiting in <paramref name="slot"/>'s column (0 = right behind the slot), or −1.</summary>
        public int QueuedColor(int slot, int index) => index >= 0 && index < _queue[slot].Count ? _level.Targets[_queue[slot][index]].Color : -1;
        public int QueuedCount(int slot) => _queue[slot].Count;

        private bool HasNext(int slot) => _queue[slot].Count > 0;

        /// <summary>R-3: covered when another stack of a different colour that still has cards, on a higher layer,
        /// overlaps it. A same-colour stack on top does not cover (GDD v2.0).</summary>
        public bool IsCovered(int stack)
        {
            var me = _level.Stacks[stack];
            for (int j = 0; j < _removed.Length; j++)
            {
                if (j == stack || Remaining(j) == 0) continue;
                var o = _level.Stacks[j];
                if (o.Layer > me.Layer && o.Color != me.Color && me.Overlaps(o)) return true;
            }
            return false;
        }

        /// <summary>R-4: a tap is valid on a stack that has cards and is not covered, while playing.</summary>
        public bool CanTap(int stack) =>
            Result == BoardResult.Playing && stack >= 0 && stack < _removed.Length && Remaining(stack) > 0 && !IsCovered(stack);

        public bool IsBoardEmpty()
        {
            for (int i = 0; i < _removed.Length; i++) if (Remaining(i) > 0) return false;
            return true;
        }

        /// <summary>Resolve a tap (R-6…R-13). An invalid tap changes nothing and returns no steps (R-4).</summary>
        public IReadOnlyList<BoardStep> Tap(int stack)
        {
            _steps.Clear();
            if (!CanTap(stack)) return Array.Empty<BoardStep>();
            _history.Push(Capture());
            TapCount++;
            SendStack(stack);
            return _steps.ToArray();
        }

        // R-6 (v2.0): the whole stack — one colour — leaves
        private void SendStack(int stack)
        {
            int count = Remaining(stack);
            _removed[stack] += count;
            PlaceRun(stack, _level.Stacks[stack].Color, count);
        }

        /// <summary>R-23 RV Slot: add holding cells. While playing that is all; after a loss the pending cards are
        /// placed and play goes on unless it is still lost.</summary>
        public IReadOnlyList<BoardStep> RvSlot()
        {
            _steps.Clear();
            if (!CanRvSlot) return Array.Empty<BoardStep>();
            _rvSlotsUsed++;
            BufferCapacity += _rules.RvSlotAmount;
            if (Result == BoardResult.Lost) Resume();
            return _steps.ToArray();
        }

        /// <summary>
        /// R-22 Revive: free Removes (R-19) on the <see cref="BoardRules.ReviveRemoves"/> slot targets holding the
        /// fewest cards (leftmost first), then the pending cards are placed; while it is still lost, one more free
        /// Remove and again — until play goes on, the level is won, or no target is left. Loss steps of the
        /// intermediate rounds are dropped: only the final outcome is reported.
        /// </summary>
        public IReadOnlyList<BoardStep> Revive()
        {
            _steps.Clear();
            if (!CanRevive) return Array.Empty<BoardStep>();
            _revivesUsed++;
            int removes = _rules.ReviveRemoves;
            while (true)
            {
                for (int k = 0; k < removes; k++)
                {
                    int slot = FewestFilledSlot();
                    if (slot < 0) break;
                    RemoveTarget(slot);
                }
                int mark = _steps.Count;
                Resume();
                if (Result != BoardResult.Lost || FewestFilledSlot() < 0) break;
                // still lost: drop this round's loss report (the cards it placed did move), remove one more
                for (int i = _steps.Count - 1; i >= mark; i--)
                    if (_steps[i].Kind == BoardStepKind.Lost || _steps[i].Kind == BoardStepKind.Overflow) _steps.RemoveAt(i);
                removes = 1;
            }
            return _steps.ToArray();
        }

        // back to playing, place what the overflow left, then win / stuck as usual
        private void Resume()
        {
            Result = BoardResult.Playing;
            Loss = BoardLoss.None;
            if (_pendingCount > 0)
            {
                int stack = _pendingStack, color = _pendingColor, count = _pendingCount;
                _pendingStack = _pendingColor = -1; _pendingCount = 0;
                PlaceRun(stack, color, count);
            }
            else CheckEnd();
        }

        private int FewestFilledSlot()
        {
            int best = -1;
            for (int s = 0; s < _slots.Length; s++)
                if (_slots[s].Has && (best < 0 || _slots[s].Filled < _slots[best].Filled)) best = s;
            return best;
        }

        /// <summary>
        /// R-19 Remove on the target in <paramref name="slot"/>: it takes cards of its colour until full — the pending
        /// cards, then the buffer (FIFO), then the board (open stacks before covered ones, higher layer first) —
        /// and leaves (R-9). R-1 guarantees there are enough. Steps reuse <see cref="BoardStepKind.CardToTarget"/>
        /// (from a stack) and <see cref="BoardStepKind.BufferToTarget"/>, so the view animates them like a tap.
        /// </summary>
        private void RemoveTarget(int slot)
        {
            var t = _slots[slot];
            if (!t.Has) return;
            int filled = t.Filled;
            Gather(t.Color, t.Capacity - t.Filled, (stack, bufferIndex) =>
            {
                _slots[slot].Filled = ++filled;
                _steps.Add(new BoardStep(stack >= 0 || bufferIndex < 0 ? BoardStepKind.CardToTarget : BoardStepKind.BufferToTarget,
                    stack: stack, slot: slot, bufferIndex: bufferIndex, color: t.Color, filled: filled, capacity: t.Capacity));
            });
            Settle();
        }

        // R-19 on the target waiting depth places behind slot: it fills the same way, leaves, and the ones behind move up
        private void RemoveQueued(int slot, int depth)
        {
            var t = MakeTarget(_queue[slot][depth]);
            int filled = 0;
            Gather(t.Color, t.Capacity, (stack, bufferIndex) =>
            {
                filled++;
                _steps.Add(new BoardStep(stack >= 0 || bufferIndex < 0 ? BoardStepKind.CardToQueued : BoardStepKind.BufferToQueued,
                    stack: stack, slot: slot, bufferIndex: bufferIndex, color: t.Color, filled: filled, capacity: t.Capacity, depth: depth));
            });
            _queue[slot].RemoveAt(depth);
            _steps.Add(new BoardStep(BoardStepKind.QueuedTargetCompleted, slot: slot, color: t.Color, depth: depth));
            Settle();
        }

        /// <summary>
        /// R-19 source order for <paramref name="need"/> cards of <paramref name="color"/>: the pending cards, then the
        /// buffer (FIFO), then the board (open stacks before covered ones, higher layer first, then id).
        /// <paramref name="take"/> gets (stack, −1) for a card from a stack or the pending run, (−1, index) for a buffer card.
        /// </summary>
        private void Gather(int color, int need, Action<int, int> take)
        {
            while (need > 0 && _pendingCount > 0 && _pendingColor == color)
            {
                _pendingCount--; need--;
                take(_pendingStack, -1);
            }
            if (_pendingCount == 0) _pendingStack = _pendingColor = -1;
            for (int i = 0; i < _buffer.Count && need > 0;)
            {
                if (_buffer[i] != color) { i++; continue; }
                _buffer.RemoveAt(i);
                need--;
                take(-1, i);
            }
            if (need == 0) return;
            var order = new List<int>();
            for (int i = 0; i < _removed.Length; i++)
                if (Remaining(i) > 0 && _level.Stacks[i].Color == color) order.Add(i);
            order.Sort((a, b) =>
            {
                bool ca = IsCovered(a), cb = IsCovered(b);
                if (ca != cb) return ca ? 1 : -1;
                int la = _level.Stacks[a].Layer, lb = _level.Stacks[b].Layer;
                return la != lb ? lb.CompareTo(la) : a.CompareTo(b);
            });
            foreach (int i in order)
            {
                while (Remaining(i) > 0 && need > 0)
                {
                    _removed[i]++; need--;
                    take(i, -1);
                }
                if (Remaining(i) == 0) _steps.Add(new BoardStep(BoardStepKind.StackEmptied, stack: i));
                if (need == 0) break;
            }
        }

        // ── boosters (GDD v2.0 §2.7; features/boosters.md v2) ─────────────────────────────────────────
        // Usable while playing, or on the lose screen: then the board goes back to playing, the booster acts, the
        // pending cards are placed, and it ends as usual (won, playing, or lost again). A booster is not a tap.

        private bool BoosterTime => Result == BoardResult.Playing || Result == BoardResult.Lost;

        /// <summary>R-20 Hand: any stack with cards, covered or not. After an overflow loss the pending cards still
        /// have nowhere to go, so Hand waits for Revive / RV Slot / Remove; after a stuck loss it may save the board.</summary>
        public bool CanHand(int stack) =>
            BoosterTime && stack >= 0 && stack < _removed.Length && Remaining(stack) > 0
            && (Result == BoardResult.Playing || _pendingCount == 0);

        public IReadOnlyList<BoardStep> Hand(int stack)
        {
            _steps.Clear();
            if (!CanHand(stack)) return Array.Empty<BoardStep>();
            Result = BoardResult.Playing;
            Loss = BoardLoss.None;
            SendStack(stack);
            return _steps.ToArray();
        }

        /// <summary>R-19 Remove: the target in <paramref name="slot"/> (<paramref name="depth"/> 0) or one waiting
        /// behind it (depth 1 = right behind the slot, …).</summary>
        public bool CanRemove(int slot, int depth) =>
            BoosterTime && slot >= 0 && slot < _slots.Length
            && (depth == 0 ? _slots[slot].Has : depth > 0 && depth - 1 < _queue[slot].Count);

        public IReadOnlyList<BoardStep> Remove(int slot, int depth = 0)
        {
            _steps.Clear();
            if (!CanRemove(slot, depth)) return Array.Empty<BoardStep>();
            bool lost = Result == BoardResult.Lost;
            if (depth == 0) RemoveTarget(slot);
            else RemoveQueued(slot, depth - 1);
            AfterBooster(lost);
            return _steps.ToArray();
        }

        /// <summary>R-21 Shuffle: needs the seeded random and at least two targets that hold no card (front or waiting).</summary>
        public bool CanShuffle => BoosterTime && _random != null && EmptyTargetPlaces().Count >= 2;

        /// <summary>
        /// R-21 Shuffle: (1) up to 3 colours — the buffer's in FIFO order (each once), then the open stacks' (higher
        /// layer first, then id); (2) each slot whose target holds no card, in slot order, takes a target of the next
        /// chosen colour; (3) the other targets that hold no card are shuffled among their places, so every column
        /// keeps its count. A target that holds cards never moves (D-035). Then settle.
        /// </summary>
        public IReadOnlyList<BoardStep> Shuffle()
        {
            _steps.Clear();
            if (!CanShuffle) return Array.Empty<BoardStep>();
            bool lost = Result == BoardResult.Lost;
            var places = EmptyTargetPlaces();
            var colors = new List<int>();
            foreach (int c in _buffer) { if (colors.Count == 3) break; if (!colors.Contains(c)) colors.Add(c); }
            if (colors.Count < 3)
            {
                var open = new List<int>();
                for (int i = 0; i < _removed.Length; i++) if (Remaining(i) > 0 && !IsCovered(i)) open.Add(i);
                open.Sort((a, b) =>
                {
                    int la = _level.Stacks[a].Layer, lb = _level.Stacks[b].Layer;
                    return la != lb ? lb.CompareTo(la) : a.CompareTo(b);
                });
                foreach (int i in open) { if (colors.Count == 3) break; int c = _level.Stacks[i].Color; if (!colors.Contains(c)) colors.Add(c); }
            }
            var fixedPlaces = new List<(int slot, int depth)>();
            int nextFront = 0;
            foreach (int c in colors)
            {
                while (nextFront < places.Count && places[nextFront].depth != 0) nextFront++;
                if (nextFront >= places.Count) break;
                var front = places[nextFront];
                int found = -1;
                for (int p = 0; p < places.Count; p++)
                {
                    if (fixedPlaces.Contains(places[p])) continue;
                    if (_level.Targets[TargetAt(places[p])].Color == c) { found = p; break; }
                }
                if (found < 0) continue;                       // no target of that colour is free to move: next colour
                Swap(front, places[found]);
                fixedPlaces.Add(front);
                nextFront++;
            }
            var rest = new List<(int slot, int depth)>();
            foreach (var p in places) if (!fixedPlaces.Contains(p)) rest.Add(p);
            var ids = new List<int>();
            foreach (var p in rest) ids.Add(TargetAt(p));
            _random.Shuffle(ids);
            for (int k = 0; k < rest.Count; k++) SetTargetAt(rest[k], ids[k]);
            _steps.Add(new BoardStep(BoardStepKind.Shuffled));
            Settle();
            AfterBooster(lost);
            return _steps.ToArray();
        }

        private void AfterBooster(bool wasLost)
        {
            if (wasLost) Resume();
            else CheckEnd();
        }

        // every target holding no card: the front ones (depth 0) in slot order, then the waiting ones nearest first
        private List<(int slot, int depth)> EmptyTargetPlaces()
        {
            var places = new List<(int slot, int depth)>();
            for (int s = 0; s < _slots.Length; s++) if (_slots[s].Has && _slots[s].Filled == 0) places.Add((s, 0));
            int deepest = 0;
            foreach (var q in _queue) deepest = Math.Max(deepest, q.Count);
            for (int d = 0; d < deepest; d++)
                for (int s = 0; s < _slots.Length; s++)
                    if (d < _queue[s].Count) places.Add((s, d + 1));
            return places;
        }

        private int TargetAt((int slot, int depth) p) => p.depth == 0 ? _slots[p.slot].Id : _queue[p.slot][p.depth - 1];

        private void SetTargetAt((int slot, int depth) p, int id)
        {
            if (p.depth == 0) _slots[p.slot] = MakeTarget(id);
            else _queue[p.slot][p.depth - 1] = id;
        }

        private void Swap((int slot, int depth) a, (int slot, int depth) b)
        {
            int ia = TargetAt(a), ib = TargetAt(b);
            SetTargetAt(a, ib);
            SetTargetAt(b, ia);
        }

        private void PlaceRun(int stack, int color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!PlaceCard(stack, color))
                {
                    _pendingStack = stack; _pendingColor = color; _pendingCount = count - i;
                    Result = BoardResult.Lost;
                    Loss = BoardLoss.Overflow;
                    _steps.Add(new BoardStep(BoardStepKind.Overflow, stack: stack, color: color));
                    _steps.Add(new BoardStep(BoardStepKind.Lost));
                    return;
                }
            }
            if (Remaining(stack) == 0) _steps.Add(new BoardStep(BoardStepKind.StackEmptied, stack: stack));
            CheckEnd();
        }

        // R-7: target (lowest slot) first, else the end of the buffer; false = overflow
        private bool PlaceCard(int stack, int color)
        {
            for (int s = 0; s < _slots.Length; s++)
            {
                if (!_slots[s].Has || _slots[s].Color != color || _slots[s].Filled >= _slots[s].Capacity) continue;
                _slots[s].Filled++;
                _steps.Add(new BoardStep(BoardStepKind.CardToTarget, stack: stack, slot: s, color: color, filled: _slots[s].Filled, capacity: _slots[s].Capacity));
                Settle();
                return true;
            }
            if (_buffer.Count >= BufferCapacity) return false;
            _buffer.Add(color);
            _steps.Add(new BoardStep(BoardStepKind.CardToBuffer, stack: stack, bufferIndex: _buffer.Count - 1, color: color));
            Settle();
            return true;
        }

        // R-9 / R-10, repeated until nothing changes
        private void Settle()
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int s = 0; s < _slots.Length; s++)
                {
                    if (!_slots[s].Has || _slots[s].Filled < _slots[s].Capacity) continue;
                    _steps.Add(new BoardStep(BoardStepKind.TargetCompleted, slot: s, color: _slots[s].Color));
                    _slots[s] = default;
                    if (HasNext(s))
                    {
                        _slots[s] = NextTarget(s);
                        _steps.Add(new BoardStep(BoardStepKind.TargetEntered, slot: s, color: _slots[s].Color, capacity: _slots[s].Capacity));
                    }
                    changed = true;
                }
                for (int s = 0; s < _slots.Length; s++)
                {
                    int i = 0;
                    while (_slots[s].Has && _slots[s].Filled < _slots[s].Capacity && i < _buffer.Count)
                    {
                        if (_buffer[i] != _slots[s].Color) { i++; continue; }
                        _buffer.RemoveAt(i);
                        _slots[s].Filled++;
                        _steps.Add(new BoardStep(BoardStepKind.BufferToTarget, slot: s, bufferIndex: i, color: _slots[s].Color, filled: _slots[s].Filled, capacity: _slots[s].Capacity));
                        changed = true;
                    }
                }
            }
        }

        // R-11 win; R-13 stuck: no valid tap, or (with LookAhead) every valid tap overflows
        private void CheckEnd()
        {
            if (Result != BoardResult.Playing) return;
            if (IsBoardEmpty() && _buffer.Count == 0)
            {
                Result = BoardResult.Won;
                _steps.Add(new BoardStep(BoardStepKind.Won));
                return;
            }
            for (int i = 0; i < _removed.Length; i++)
                if (CanTap(i) && (!LookAhead || _probing || !TapOverflows(i))) return;
            Result = BoardResult.Lost;
            Loss = BoardLoss.Stuck;
            _steps.Add(new BoardStep(BoardStepKind.Lost));
        }

        private bool _probing;

        // play the tap on the real state, read the outcome, then put everything back
        private bool TapOverflows(int stack)
        {
            var saved = _steps.ToArray();
            var snapshot = Capture();
            _probing = true;
            SendStack(stack);
            bool overflow = Loss == BoardLoss.Overflow;
            _probing = false;
            Restore(snapshot);
            _steps.Clear(); _steps.AddRange(saved);
            return overflow;
        }

        private bool AnyTappable()
        {
            for (int i = 0; i < _removed.Length; i++)
                if (Remaining(i) > 0 && !IsCovered(i)) return true;
            return false;
        }

        private Target NextTarget(int slot)
        {
            int id = _queue[slot][0];
            _queue[slot].RemoveAt(0);
            return MakeTarget(id);
        }

        private Target MakeTarget(int id)
        {
            var t = _level.Targets[id];
            return new Target { Has = true, Id = id, Color = t.Color, Capacity = t.Capacity };
        }

        private static List<int>[] CloneQueue(List<int>[] queue)
        {
            var copy = new List<int>[queue.Length];
            for (int c = 0; c < queue.Length; c++) copy[c] = new List<int>(queue[c]);
            return copy;
        }

        private Snapshot Capture() => new Snapshot
        {
            Removed = (int[])_removed.Clone(),
            Slots = (Target[])_slots.Clone(),
            Queue = CloneQueue(_queue),
            Buffer = _buffer.ToArray(),
            Result = Result,
            Loss = Loss,
        };

        private void Restore(Snapshot s)
        {
            Array.Copy(s.Removed, _removed, _removed.Length);
            Array.Copy(s.Slots, _slots, _slots.Length);
            for (int c = 0; c < _queue.Length; c++) { _queue[c].Clear(); _queue[c].AddRange(s.Queue[c]); }
            _buffer.Clear(); _buffer.AddRange(s.Buffer);
            Result = s.Result;
            Loss = s.Loss;
            _pendingStack = _pendingColor = -1; _pendingCount = 0;
        }

        // ── for the solver (same assembly): step back regardless of result, and a state key ──────────
        internal void UndoForSearch()
        {
            Restore(_history.Pop());
            TapCount--;
        }

        internal string StateKey()
        {
            var sb = new StringBuilder(64);
            foreach (var r in _removed) sb.Append(r).Append(',');
            sb.Append('|');
            foreach (var t in _slots) sb.Append(t.Has ? t.Color : -1).Append(':').Append(t.Filled).Append(',');
            foreach (var q in _queue) { sb.Append('|'); foreach (int id in q) sb.Append(id).Append(','); }
            sb.Append('|');
            foreach (var b in _buffer) sb.Append(b);
            return sb.ToString();
        }

        /// <summary>Whether every card a tap on <paramref name="stack"/> would send lands on a target right now
        /// (no buffer). Used by the solver's move ordering.</summary>
        internal bool RunFitsTargets(int stack)
        {
            int color = _level.Stacks[stack].Color, run = Remaining(stack), room = 0;
            for (int s = 0; s < _slots.Length; s++)
                if (_slots[s].Has && _slots[s].Color == color) room += _slots[s].Capacity - _slots[s].Filled;
            return room >= run;
        }
    }
}
