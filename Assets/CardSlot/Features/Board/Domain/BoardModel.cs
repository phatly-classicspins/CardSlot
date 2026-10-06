using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Domain
{
    public enum BoardResult { Playing, Won, Lost }

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

    /// <summary>Booster and Continue amounts/limits (R-17, R-18) — tuning data, passed in by the caller.</summary>
    public sealed class BoardRules
    {
        public int AddSlotAmount = 4;
        public int AddSlotMaxPerAttempt = 1;
        public int ContinueSlotAmount = 4;
        public int ContinueMaxPerAttempt = 1;
    }

    /// <summary>
    /// The rules of one attempt at a level (GDD §2, R-2…R-18). Engine-free and deterministic: the final
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
            public int QueueNext;
            public int[] Buffer;
            public BoardResult Result;
        }

        private readonly LevelData _level;
        private readonly BoardRules _rules;
        private readonly int[] _removed;          // cards taken from the top of each stack
        private readonly Target[] _slots;
        private readonly List<int> _buffer = new List<int>();
        private readonly Stack<Snapshot> _history = new Stack<Snapshot>();
        private readonly List<BoardStep> _steps = new List<BoardStep>();
        private int _queueNext;
        private int _addSlotsUsed, _continuesUsed;
        // the run left unplaced when the buffer overflowed — Continue places it (R-18)
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
            for (int s = 0; s < _slots.Length && _queueNext < level.Targets.Count; s++)
                _slots[s] = NextTarget();
        }

        public LevelData Level => _level;
        public BoardResult Result { get; private set; } = BoardResult.Playing;
        public int BufferCapacity { get; private set; }
        public IReadOnlyList<int> Buffer => _buffer;
        public int StackCount => _removed.Length;
        public int SlotCount => _slots.Length;
        public int QueuedTargets => _level.Targets.Count - _queueNext;
        public int TapCount { get; private set; }
        public bool CanUndo => Result == BoardResult.Playing && _history.Count > 0;
        public bool CanAddSlot => Result == BoardResult.Playing && _addSlotsUsed < _rules.AddSlotMaxPerAttempt;
        public bool CanContinue => Result == BoardResult.Lost && _pendingCount > 0 && _continuesUsed < _rules.ContinueMaxPerAttempt;

        public int Remaining(int stack) => _level.Stacks[stack].Cards.Length - _removed[stack];
        /// <summary>Colour of the card <paramref name="depth"/> below the current top (0 = top).</summary>
        public int CardAt(int stack, int depth) => _level.Stacks[stack].Cards[_removed[stack] + depth];
        public bool SlotHasTarget(int slot) => _slots[slot].Has;
        public int SlotColor(int slot) => _slots[slot].Color;
        public int SlotFilled(int slot) => _slots[slot].Filled;
        public int SlotCapacity(int slot) => _slots[slot].Capacity;
        /// <summary>Colours of the next <paramref name="count"/> queued targets (the "next" chips).</summary>
        public IEnumerable<int> UpcomingColors(int count)
        {
            for (int i = _queueNext; i < _level.Targets.Count && i < _queueNext + count; i++) yield return _level.Targets[i].Color;
        }

        /// <summary>R-3: covered when another stack that still has cards, on a higher layer, overlaps it.</summary>
        public bool IsCovered(int stack)
        {
            var me = _level.Stacks[stack];
            for (int j = 0; j < _removed.Length; j++)
            {
                if (j == stack || Remaining(j) == 0) continue;
                var o = _level.Stacks[j];
                if (o.Layer > me.Layer && me.Overlaps(o)) return true;
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
            // R-6: the whole run (same-colour cards from the top) leaves the stack
            int color = CardAt(stack, 0), run = 0;
            while (run < Remaining(stack) && CardAt(stack, run) == color) run++;
            _removed[stack] += run;
            PlaceRun(stack, color, run);
            return _steps.ToArray();
        }

        /// <summary>R-16: restore the state just before the most recent tap, including every settle it
        /// caused — except buffer capacity, which boosters/Continue only ever add to.</summary>
        public bool Undo()
        {
            if (!CanUndo) return false;
            Restore(_history.Pop());
            TapCount--;
            return true;
        }

        /// <summary>R-17: Extra Space.</summary>
        public bool AddBufferSpace()
        {
            if (!CanAddSlot) return false;
            BufferCapacity += _rules.AddSlotAmount;
            _addSlotsUsed++;
            return true;
        }

        /// <summary>R-18: after an overflow, add space, put the overflowing card in the buffer and keep
        /// placing the rest of its run.</summary>
        public IReadOnlyList<BoardStep> Continue()
        {
            _steps.Clear();
            if (!CanContinue) return Array.Empty<BoardStep>();
            _continuesUsed++;
            BufferCapacity += _rules.ContinueSlotAmount;
            Result = BoardResult.Playing;
            int stack = _pendingStack, color = _pendingColor, count = _pendingCount;
            _pendingStack = _pendingColor = -1; _pendingCount = 0;
            PlaceRun(stack, color, count);
            return _steps.ToArray();
        }

        private void PlaceRun(int stack, int color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!PlaceCard(stack, color))
                {
                    _pendingStack = stack; _pendingColor = color; _pendingCount = count - i;
                    Result = BoardResult.Lost;
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
                    if (_queueNext < _level.Targets.Count)
                    {
                        _slots[s] = NextTarget();
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

        // R-11 win; R-13 stuck (only a broken level gets here — the solver rejects those)
        private void CheckEnd()
        {
            if (Result != BoardResult.Playing) return;
            if (IsBoardEmpty() && _buffer.Count == 0)
            {
                Result = BoardResult.Won;
                _steps.Add(new BoardStep(BoardStepKind.Won));
                return;
            }
            for (int i = 0; i < _removed.Length; i++) if (CanTap(i)) return;
            Result = BoardResult.Lost;
            _steps.Add(new BoardStep(BoardStepKind.Lost));
        }

        private Target NextTarget()
        {
            var t = _level.Targets[_queueNext++];
            return new Target { Has = true, Color = t.Color, Capacity = t.Capacity };
        }

        private Snapshot Capture() => new Snapshot
        {
            Removed = (int[])_removed.Clone(),
            Slots = (Target[])_slots.Clone(),
            QueueNext = _queueNext,
            Buffer = _buffer.ToArray(),
            Result = Result,
        };

        private void Restore(Snapshot s)
        {
            Array.Copy(s.Removed, _removed, _removed.Length);
            Array.Copy(s.Slots, _slots, _slots.Length);
            _queueNext = s.QueueNext;
            _buffer.Clear(); _buffer.AddRange(s.Buffer);
            Result = s.Result;
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
            sb.Append('|').Append(_queueNext).Append('|');
            foreach (var b in _buffer) sb.Append(b);
            return sb.ToString();
        }

        /// <summary>The colour of the run a tap on <paramref name="stack"/> would send, and whether every
        /// card of it lands on a target right now (no buffer). Used by the solver's move ordering.</summary>
        internal bool RunFitsTargets(int stack)
        {
            int color = CardAt(stack, 0), run = 0, room = 0;
            while (run < Remaining(stack) && CardAt(stack, run) == color) run++;
            for (int s = 0; s < _slots.Length; s++)
                if (_slots[s].Has && _slots[s].Color == color) room += _slots[s].Capacity - _slots[s].Filled;
            return room >= run;
        }
    }
}
