using System;
using System.Collections.Generic;
using System.Text;

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
    }

    /// <summary>One thing that happened while resolving a tap, in order — the view replays these
    /// (R-8: the model resolves instantly; animation only shows the result).</summary>
    public readonly struct BoardStep
    {
        public readonly BoardStepKind Kind;
        public readonly int Stack, Slot, BufferIndex, Color, Filled, Capacity;

        public BoardStep(BoardStepKind kind, int stack = -1, int slot = -1, int bufferIndex = -1, int color = -1, int filled = 0, int capacity = 0)
        {
            Kind = kind; Stack = stack; Slot = slot; BufferIndex = bufferIndex; Color = color; Filled = filled; Capacity = capacity;
        }

        public override string ToString() =>
            $"{Kind}(stack {Stack}, slot {Slot}, buf {BufferIndex}, colour {Color}, {Filled}/{Capacity})";
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
    /// The rules of one attempt at a level (GDD v2.0 §2, R-2…R-15, R-19 Remove, R-22 Revive, R-23 RV Slot). Engine-free and deterministic: the final
    /// state depends only on the level and the sequence of taps (R-15). Every mutation returns the steps
    /// the view needs to animate it; no rule waits on a view.
    /// </summary>
    public sealed class BoardModel
    {
        private struct Target
        {
            public bool Has;
            public int Color, Capacity, Filled;
        }

        private sealed class Snapshot
        {
            public int[] Removed;
            public Target[] Slots;
            public int[] ColumnNext;
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
        private int[] _columnNext;
        private int _revivesUsed, _rvSlotsUsed;
        // the cards left unplaced when the buffer overflowed (R-7.2) — Revive / RV Slot place them
        private int _pendingStack = -1, _pendingColor = -1, _pendingCount;

        public BoardModel(LevelData level, BoardRules rules = null, int? bufferCapacityOverride = null)
        {
            var errors = LevelValidator.Validate(level);
            if (errors.Count > 0) throw new ArgumentException($"Level '{level?.Id}' is invalid: {string.Join("; ", errors)}");
            _level = level;
            _rules = rules ?? new BoardRules();
            _removed = new int[level.Stacks.Count];
            _slots = new Target[level.Slots];
            BufferCapacity = bufferCapacityOverride ?? level.BufferCapacity;
            // R-2: the first n targets of the queue fill slots 0..n-1 in order
            _columnNext = new int[_slots.Length];
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
        public int QueuedTargets { get { int used = 0; foreach (int c in _columnNext) used += c; return _level.Targets.Count - used; } }
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
        public int NextColorBehind(int slot) => HasNext(slot) ? _level.Targets[ColumnIndex(slot)].Color : -1;

        private int ColumnIndex(int slot) => slot + _columnNext[slot] * _slots.Length;
        private bool HasNext(int slot) => ColumnIndex(slot) < _level.Targets.Count;

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
            while (_pendingCount > 0 && _pendingColor == t.Color && _slots[slot].Filled < t.Capacity)
            {
                _pendingCount--;
                Fill(slot, BoardStepKind.CardToTarget, _pendingStack, -1);
            }
            if (_pendingCount == 0) _pendingStack = _pendingColor = -1;
            for (int i = 0; i < _buffer.Count && _slots[slot].Filled < t.Capacity;)
            {
                if (_buffer[i] != t.Color) { i++; continue; }
                _buffer.RemoveAt(i);
                Fill(slot, BoardStepKind.BufferToTarget, -1, i);
            }
            var order = new List<int>();
            for (int i = 0; i < _removed.Length; i++)
                if (Remaining(i) > 0 && _level.Stacks[i].Color == t.Color) order.Add(i);
            order.Sort((a, b) =>
            {
                bool ca = IsCovered(a), cb = IsCovered(b);
                if (ca != cb) return ca ? 1 : -1;
                int la = _level.Stacks[a].Layer, lb = _level.Stacks[b].Layer;
                return la != lb ? lb.CompareTo(la) : a.CompareTo(b);
            });
            foreach (int i in order)
            {
                while (Remaining(i) > 0 && _slots[slot].Filled < t.Capacity)
                {
                    _removed[i]++;
                    Fill(slot, BoardStepKind.CardToTarget, i, -1);
                }
                if (Remaining(i) == 0) _steps.Add(new BoardStep(BoardStepKind.StackEmptied, stack: i));
                if (_slots[slot].Filled >= t.Capacity) break;
            }
            Settle();
        }

        private void Fill(int slot, BoardStepKind kind, int stack, int bufferIndex)
        {
            _slots[slot].Filled++;
            _steps.Add(new BoardStep(kind, stack: stack, slot: slot, bufferIndex: bufferIndex, color: _slots[slot].Color,
                filled: _slots[slot].Filled, capacity: _slots[slot].Capacity));
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
            var t = _level.Targets[ColumnIndex(slot)];
            _columnNext[slot]++;
            return new Target { Has = true, Color = t.Color, Capacity = t.Capacity };
        }

        private Snapshot Capture() => new Snapshot
        {
            Removed = (int[])_removed.Clone(),
            Slots = (Target[])_slots.Clone(),
            ColumnNext = (int[])_columnNext.Clone(),
            Buffer = _buffer.ToArray(),
            Result = Result,
            Loss = Loss,
        };

        private void Restore(Snapshot s)
        {
            Array.Copy(s.Removed, _removed, _removed.Length);
            Array.Copy(s.Slots, _slots, _slots.Length);
            Array.Copy(s.ColumnNext, _columnNext, _columnNext.Length);
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
            sb.Append('|'); foreach (int c in _columnNext) sb.Append(c).Append(','); sb.Append('|');
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
