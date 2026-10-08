using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using NUnit.Framework;

namespace CardSlot.SkuHeadlessTests.Board
{
    /// <summary>One or more tests per rule of GDD §2 (R-1…R-18). Colours: 0 red, 1 blue, 2 yellow.</summary>
    public class BoardModelTests
    {
        // a stack at column `col` on layer 0, rows never overlap
        static StackSpec S(string id, int col, params int[] cards) => new StackSpec(id, col * 220, 0, 150, 206, 0, cards);
        static StackSpec On(string id, int x, int layer, params int[] cards) => new StackSpec(id, x, 0, 150, 206, layer, cards);

        static LevelData Level(int slots, int buffer, IEnumerable<TargetSpec> targets, params StackSpec[] stacks)
        {
            var l = new LevelData { Id = "t", Slots = slots, BufferCapacity = buffer };
            l.Targets.AddRange(targets);
            l.Stacks.AddRange(stacks);
            return l;
        }
        static TargetSpec T(int color, int cap = 3) => new TargetSpec(color, cap);
        static List<BoardStepKind> Kinds(IEnumerable<BoardStep> steps) => steps.Select(s => s.Kind).ToList();

        // ── R-1 / R-1b ─────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R1_card_count_per_colour_must_equal_target_capacity()
        {
            var bad = Level(2, 6, new[] { T(0), T(1) }, S("a", 0, 0, 0, 0), S("b", 1, 1, 1));
            Assert.That(LevelValidator.Validate(bad), Has.Some.Contains("R-1"));
            Assert.Throws<System.ArgumentException>(() => new BoardModel(bad));
        }

        [Test]
        public void R1b_stacks_on_the_same_layer_may_not_overlap_but_different_layers_may()
        {
            var same = Level(2, 6, new[] { T(0), T(1) }, On("a", 0, 0, 0, 0, 0), On("b", 100, 0, 1, 1, 1));
            Assert.That(LevelValidator.Validate(same), Has.Some.Contains("R-1b"));
            var stacked = Level(2, 6, new[] { T(0), T(1) }, On("a", 0, 0, 0, 0, 0), On("b", 100, 1, 1, 1, 1));
            Assert.That(LevelValidator.Validate(stacked), Is.Empty);
        }

        [Test]
        public void Validator_reports_out_of_range_values()
        {
            var l = Level(5, 61, new[] { T(0), T(1) }, S("a", 0, 0, 0, 0), S("b", 1, 1, 1, 1));
            var errors = LevelValidator.Validate(l);
            Assert.That(errors, Has.Some.Contains("slots"));
            Assert.That(errors, Has.Some.Contains("buffer_capacity"));
        }

        // ── R-2 ─────────────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R2_first_targets_of_the_queue_fill_the_slots_in_order()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(1), T(0), T(1) }, S("a", 0, 0, 0, 0), S("b", 1, 1, 1, 1, 1, 1, 1)));
            Assert.That(b.SlotColor(0), Is.EqualTo(1));
            Assert.That(b.SlotColor(1), Is.EqualTo(0));
            Assert.That(b.QueuedTargets, Is.EqualTo(1));
            Assert.That(b.Buffer, Is.Empty);
        }

        // ── R-3 / R-4 / R-5 ─────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R3_R4_a_covered_stack_ignores_taps_and_nothing_changes()
        {
            var l = Level(2, 6, new[] { T(0), T(1) }, On("low", 0, 0, 0, 0, 0), On("top", 100, 1, 1, 1, 1));
            var b = new BoardModel(l);
            Assert.That(b.IsCovered(0), Is.True);
            Assert.That(b.Tap(0), Is.Empty);
            Assert.That(b.Remaining(0), Is.EqualTo(3));
            Assert.That(b.TapCount, Is.EqualTo(0));
        }

        [Test]
        public void R5_an_emptied_stack_uncovers_the_one_below()
        {
            var l = Level(2, 6, new[] { T(0), T(1) }, On("low", 0, 0, 0, 0, 0), On("top", 100, 1, 1, 1, 1));
            var b = new BoardModel(l);
            var steps = b.Tap(1);
            Assert.That(Kinds(steps), Does.Contain(BoardStepKind.StackEmptied));
            Assert.That(b.IsCovered(0), Is.False);
        }

        // ── R-6 / R-7 ───────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R6_a_tap_sends_the_whole_same_colour_run_and_stops_at_the_next_colour()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(1) }, S("a", 0, 0, 0, 1, 1), S("b", 1, 0, 1)));
            b.Tap(0);
            Assert.That(b.Remaining(0), Is.EqualTo(2));
            Assert.That(b.CardAt(0, 0), Is.EqualTo(1));
            Assert.That(b.SlotFilled(0), Is.EqualTo(2));
        }

        [Test]
        public void R6_CR008_a_tap_takes_at_most_max_run_cards_so_a_run_of_12_needs_two_taps()
        {
            var cards = new int[14];
            for (int i = 0; i < 12; i++) cards[i] = 0;
            cards[12] = cards[13] = 1;
            var l = Level(2, 20, new[] { T(0, 12), T(1, 4) }, S("a", 0, cards), S("b", 1, 1, 1));
            l.MaxRun = 6;
            var b = new BoardModel(l);
            Assert.That(b.Tap(0).Count(s => s.Kind == BoardStepKind.CardToTarget), Is.EqualTo(6), "first tap: 6 of the 12");
            Assert.That(b.Remaining(0), Is.EqualTo(8));
            Assert.That(b.Tap(0).Count(s => s.Kind == BoardStepKind.CardToTarget), Is.EqualTo(6), "second tap: the other 6");
            Assert.That(b.CardAt(0, 0), Is.EqualTo(1));
        }

        [Test]
        public void R7_card_goes_to_the_lowest_matching_slot_else_to_the_end_of_the_buffer()
        {
            // both slots are red; a red run of 2 goes to slot 0. Blue has no target → buffer.
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(0), T(1) }, S("a", 0, 0, 0), S("b", 1, 1, 1, 1, 0, 0, 0, 0)));
            var steps = b.Tap(0);
            Assert.That(steps.Where(s => s.Kind == BoardStepKind.CardToTarget).Select(s => s.Slot), Is.EqualTo(new[] { 0, 0 }));
            steps = b.Tap(1);
            Assert.That(steps.Where(s => s.Kind == BoardStepKind.CardToBuffer).Select(s => s.BufferIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(b.Buffer, Is.EqualTo(new[] { 1, 1, 1 }));
        }

        // ── R-9 / R-10 ──────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R9_a_full_target_leaves_and_the_next_target_takes_the_same_slot()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(1), T(2) }, S("a", 0, 0, 0, 0), S("b", 1, 1, 1, 1), S("c", 2, 2, 2, 2)));
            var steps = b.Tap(0);
            var kinds = Kinds(steps);
            Assert.That(kinds, Is.EqualTo(new[] { BoardStepKind.CardToTarget, BoardStepKind.CardToTarget, BoardStepKind.CardToTarget,
                BoardStepKind.TargetCompleted, BoardStepKind.TargetEntered, BoardStepKind.StackEmptied }));
            Assert.That(steps[4].Slot, Is.EqualTo(0));
            Assert.That(b.SlotColor(0), Is.EqualTo(2));
        }

        [Test]
        public void R10_buffer_cards_fly_to_a_new_matching_target_in_fifo_order()
        {
            // yellow is queued behind red; tapping yellow first parks it; clearing red brings yellow in and releases it
            var l = Level(2, 9, new[] { T(0), T(1), T(2) }, S("y", 0, 2, 2, 2), S("r", 1, 0, 0, 0), S("bl", 2, 1, 1, 1));
            var b = new BoardModel(l);
            b.Tap(0);
            Assert.That(b.Buffer, Is.EqualTo(new[] { 2, 2, 2 }));
            var steps = b.Tap(1);
            var released = steps.Where(s => s.Kind == BoardStepKind.BufferToTarget).ToList();
            Assert.That(released.Count, Is.EqualTo(3));
            Assert.That(released.All(s => s.BufferIndex == 0), "FIFO: always the front of the buffer");
            Assert.That(b.Buffer, Is.Empty);
        }

        [Test]
        public void R10_settle_cascades_a_release_that_completes_a_target()
        {
            // CR-009: target i waits behind slot i % 2 — red, then both yellows, queue behind slot 0
            var l = Level(2, 9, new[] { T(0), T(1), T(2), T(1), T(2) }, S("y", 0, 2, 2, 2, 2, 2, 2), S("r", 1, 0, 0, 0), S("bl", 2, 1, 1, 1, 1, 1, 1));
            var b = new BoardModel(l);
            b.Tap(0);                       // six yellows parked
            var kinds = Kinds(b.Tap(1));    // red done → yellow enters → 3 released → done → yellow enters → 3 released
            Assert.That(kinds.Count(k => k == BoardStepKind.BufferToTarget), Is.EqualTo(6));
            Assert.That(kinds.Count(k => k == BoardStepKind.TargetCompleted), Is.EqualTo(3));
            Assert.That(b.Buffer, Is.Empty);
        }

        [Test]
        public void CR009_a_completed_slot_takes_the_target_waiting_behind_it_not_the_next_in_the_list()
        {
            // two slots: column 0 = red, yellow · column 1 = blue, green
            var l = Level(2, 9, new[] { T(0), T(1), T(2), T(3) }, S("b", 0, 1, 1, 1), S("r", 1, 0, 0, 0), S("y", 2, 2, 2, 2), S("g", 3, 3, 3, 3));
            var b = new BoardModel(l);
            Assert.That((b.NextColorBehind(0), b.NextColorBehind(1)), Is.EqualTo((2, 3)));
            b.Tap(0);                                    // blue completes slot 1
            Assert.That(b.SlotColor(1), Is.EqualTo(3), "green moves up from behind slot 1 (not yellow, the next in the list)");
            Assert.That(b.NextColorBehind(1), Is.EqualTo(-1));
            Assert.That(b.NextColorBehind(0), Is.EqualTo(2), "yellow still waits behind slot 0");
        }

        // ── R-11 / R-12 ─────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R11_clearing_board_and_buffer_wins()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(1) }, S("a", 0, 0, 0, 0), S("b", 1, 1, 1, 1)));
            b.Tap(0);
            var steps = b.Tap(1);
            Assert.That(b.Result, Is.EqualTo(BoardResult.Won));
            Assert.That(steps.Last().Kind, Is.EqualTo(BoardStepKind.Won));
            Assert.That(b.Tap(0), Is.Empty, "no taps after the end");
        }

        [Test]
        public void R12_a_card_with_nowhere_to_go_overflows_and_loses()
        {
            // slot shows red only; buffer 2; a run of 3 blue overflows on the third card
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(0), T(1) }, S("bl", 0, 1, 1, 1), S("r", 1, 0, 0, 0, 0, 0, 0)), null, 2);
            var steps = b.Tap(0);
            Assert.That(b.Result, Is.EqualTo(BoardResult.Lost));
            Assert.That(Kinds(steps).TakeLast(2), Is.EqualTo(new[] { BoardStepKind.Overflow, BoardStepKind.Lost }));
            Assert.That(b.Buffer.Count, Is.EqualTo(2));
        }

        // ── R-15 ────────────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R15_same_taps_same_result()
        {
            var l = Level(2, 9, new[] { T(0), T(1), T(2) }, S("y", 0, 2, 2, 2), S("r", 1, 0, 0, 0), S("bl", 2, 1, 1, 1));
            string Run() { var b = new BoardModel(l); var log = new List<string>(); foreach (var t in new[] { 0, 1, 2 }) log.AddRange(b.Tap(t).Select(s => s.ToString())); return string.Join("|", log) + b.Result; }
            Assert.That(Run(), Is.EqualTo(Run()));
        }

        // R-16 / R-17 (Undo, Extra Space) removed with the boosters (CR-005); R-18 Continue stays

        [Test]
        public void R18_continue_places_the_overflowing_card_and_the_rest_of_its_run_once()
        {
            var l = Level(2, 6, new[] { T(0), T(0), T(1) }, S("bl", 0, 1, 1, 1), S("r", 1, 0, 0, 0, 0, 0, 0));
            var b = new BoardModel(l, new BoardRules { ContinueSlotAmount = 4, ContinueMaxPerAttempt = 1 }, 2);
            b.Tap(0);
            Assert.That(b.CanContinue, Is.True);
            var steps = b.Continue();
            Assert.That(b.Result, Is.EqualTo(BoardResult.Playing));
            Assert.That(b.BufferCapacity, Is.EqualTo(6));
            Assert.That(steps.Count(s => s.Kind == BoardStepKind.CardToBuffer), Is.EqualTo(1));
            Assert.That(b.Buffer.Count, Is.EqualTo(3));
            b.Tap(1);                        // reds clear both red targets, blue enters and takes the buffer
            Assert.That(b.Result, Is.EqualTo(BoardResult.Won));
        }

        [Test]
        public void R18_continue_is_only_for_an_overflow_and_limited_per_attempt()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(1) }, S("a", 0, 0, 0, 0), S("b", 1, 1, 1, 1)));
            Assert.That(b.CanContinue, Is.False);
            Assert.That(b.Continue(), Is.Empty);
            var l = Level(2, 6, new[] { T(0), T(0), T(1), T(2) }, S("bl", 0, 1, 1, 1), S("y", 1, 2, 2, 2), S("r", 2, 0, 0, 0, 0, 0, 0));
            var c = new BoardModel(l, new BoardRules { ContinueSlotAmount = 1, ContinueMaxPerAttempt = 1 }, 2);
            c.Tap(0);                        // 2 blue in, third overflows
            c.Continue();                    // +1 → 3 slots, blue in
            c.Tap(1);                        // yellow overflows again
            Assert.That(c.Result, Is.EqualTo(BoardResult.Lost));
            Assert.That(c.CanContinue, Is.False);
        }
    }
}
