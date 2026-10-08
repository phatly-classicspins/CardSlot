using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using NUnit.Framework;

namespace CardSlot.SkuHeadlessTests.Board
{
    /// <summary>One or more tests per rule of GDD v2.0 §2 (R-1…R-18). Colours: 0 red, 1 blue, 2 yellow, 3 green.
    /// Every stack is one colour (R-1c).</summary>
    public class BoardModelTests
    {
        // a stack at column `col` on layer 0, rows never overlap
        static StackSpec S(string id, int col, int color, int count) => new StackSpec(id, col * 220, 0, 150, 206, 0, color, count);
        static StackSpec On(string id, int x, int layer, int color, int count) => new StackSpec(id, x, 0, 150, 206, layer, color, count);

        static LevelData Level(int slots, int buffer, IEnumerable<TargetSpec> targets, params StackSpec[] stacks)
        {
            var l = new LevelData { Id = "t", Slots = slots, BufferCapacity = buffer };
            l.Targets.AddRange(targets);
            l.Stacks.AddRange(stacks);
            return l;
        }
        static TargetSpec T(int color, int cap = 3) => new TargetSpec(color, cap);
        static List<BoardStepKind> Kinds(IEnumerable<BoardStep> steps) => steps.Select(s => s.Kind).ToList();

        // ── R-1 / R-1b / R-1c ──────────────────────────────────────────────────────────────────────────
        [Test]
        public void R1_card_count_per_colour_must_equal_target_capacity()
        {
            var bad = Level(2, 6, new[] { T(0), T(1) }, S("a", 0, 0, 4), S("b", 1, 1, 3));
            Assert.That(LevelValidator.Validate(bad), Has.Some.Contains("R-1"));
            Assert.Throws<System.ArgumentException>(() => new BoardModel(bad));
        }

        [Test]
        public void R1b_stacks_on_the_same_layer_may_not_overlap_but_different_layers_may()
        {
            var same = Level(2, 6, new[] { T(0), T(1) }, On("a", 0, 0, 0, 3), On("b", 100, 0, 1, 3));
            Assert.That(LevelValidator.Validate(same), Has.Some.Contains("R-1b"));
            var stacked = Level(2, 6, new[] { T(0), T(1) }, On("a", 0, 0, 0, 3), On("b", 100, 1, 1, 3));
            Assert.That(LevelValidator.Validate(stacked), Is.Empty);
        }

        [Test]
        public void R1c_every_card_of_a_stack_has_the_stack_colour()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(0, 6), T(1) }, S("a", 0, 0, 6), S("b", 1, 1, 3)));
            Assert.That(Enumerable.Range(0, 6).Select(d => b.CardAt(0, d)), Is.All.EqualTo(0));
        }

        [Test]
        public void Validator_reports_out_of_range_values()
        {
            var l = Level(5, 61, new[] { T(0), T(1) }, S("a", 0, 0, 3), S("b", 1, 1, 3), S("c", 2, 8, 1));
            var errors = LevelValidator.Validate(l);
            Assert.That(errors, Has.Some.Contains("slots"));
            Assert.That(errors, Has.Some.Contains("buffer_capacity"));
            Assert.That(errors, Has.Some.Contains("colour 8 out of range"));
        }

        // ── R-2 ─────────────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R2_first_targets_of_the_queue_fill_the_slots_in_order()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(1), T(0), T(1) }, S("a", 0, 0, 3), S("b", 1, 1, 6)));
            Assert.That(b.SlotColor(0), Is.EqualTo(1));
            Assert.That(b.SlotColor(1), Is.EqualTo(0));
            Assert.That(b.QueuedTargets, Is.EqualTo(1));
            Assert.That(b.Buffer, Is.Empty);
        }

        // ── R-3 / R-4 / R-5 ─────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R3_R4_a_stack_covered_by_another_colour_ignores_taps_and_nothing_changes()
        {
            var l = Level(2, 6, new[] { T(0), T(1) }, On("low", 0, 0, 0, 3), On("top", 100, 1, 1, 3));
            var b = new BoardModel(l);
            Assert.That(b.IsCovered(0), Is.True);
            Assert.That(b.Tap(0), Is.Empty);
            Assert.That(b.Remaining(0), Is.EqualTo(3));
            Assert.That(b.TapCount, Is.EqualTo(0));
        }

        [Test]
        public void R3_a_same_colour_stack_on_top_does_not_cover()
        {
            var l = Level(2, 6, new[] { T(0, 6), T(1) }, On("low", 0, 0, 0, 3), On("top", 100, 1, 0, 3), S("b", 2, 1, 3));
            var b = new BoardModel(l);
            Assert.That(b.IsCovered(0), Is.False);
            Assert.That(b.CanTap(0), Is.True);
            b.Tap(0);
            Assert.That((b.Remaining(0), b.SlotFilled(0)), Is.EqualTo((0, 3)));
        }

        [Test]
        public void R5_an_emptied_stack_uncovers_the_one_below()
        {
            var l = Level(2, 6, new[] { T(0), T(1) }, On("low", 0, 0, 0, 3), On("top", 100, 1, 1, 3));
            var b = new BoardModel(l);
            var steps = b.Tap(1);
            Assert.That(Kinds(steps), Does.Contain(BoardStepKind.StackEmptied));
            Assert.That(b.IsCovered(0), Is.False);
        }

        // ── R-6 / R-7 ───────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R6_a_tap_sends_the_whole_stack()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(0, 18), T(1) }, S("a", 0, 0, 12), S("b", 1, 0, 6), S("c", 2, 1, 3)));
            var steps = b.Tap(0);
            Assert.That(steps.Count(s => s.Kind == BoardStepKind.CardToTarget), Is.EqualTo(12), "all 12 in one tap (no max_run since v2.0)");
            Assert.That(b.Remaining(0), Is.EqualTo(0));
            Assert.That(b.SlotFilled(0), Is.EqualTo(12));
        }

        [Test]
        public void R7_card_goes_to_the_lowest_matching_slot_else_to_the_end_of_the_buffer()
        {
            // both slots are red; a red stack of 2 goes to slot 0. Blue has no target yet → buffer.
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(0), T(1) }, S("a", 0, 0, 2), S("b", 1, 1, 3), S("c", 2, 0, 4)));
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
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(1), T(2) }, S("a", 0, 0, 3), S("b", 1, 1, 3), S("c", 2, 2, 3)));
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
            var l = Level(2, 9, new[] { T(0), T(1), T(2) }, S("y", 0, 2, 3), S("r", 1, 0, 3), S("bl", 2, 1, 3));
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
            var l = Level(2, 9, new[] { T(0), T(1), T(2), T(1), T(2) }, S("y", 0, 2, 6), S("r", 1, 0, 3), S("bl", 2, 1, 6));
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
            var l = Level(2, 9, new[] { T(0), T(1), T(2), T(3) }, S("b", 0, 1, 3), S("r", 1, 0, 3), S("y", 2, 2, 3), S("g", 3, 3, 3));
            var b = new BoardModel(l);
            Assert.That((b.NextColorBehind(0), b.NextColorBehind(1)), Is.EqualTo((2, 3)));
            b.Tap(0);                                    // blue completes slot 1
            Assert.That(b.SlotColor(1), Is.EqualTo(3), "green moves up from behind slot 1 (not yellow, the next in the list)");
            Assert.That(b.NextColorBehind(1), Is.EqualTo(-1));
            Assert.That(b.NextColorBehind(0), Is.EqualTo(2), "yellow still waits behind slot 0");
        }

        // ── R-11 / R-12 / R-13 ──────────────────────────────────────────────────────────────────────────
        [Test]
        public void R11_clearing_board_and_buffer_wins()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(1) }, S("a", 0, 0, 3), S("b", 1, 1, 3)));
            b.Tap(0);
            var steps = b.Tap(1);
            Assert.That(b.Result, Is.EqualTo(BoardResult.Won));
            Assert.That(steps.Last().Kind, Is.EqualTo(BoardStepKind.Won));
            Assert.That(b.Tap(0), Is.Empty, "no taps after the end");
        }

        [Test]
        public void R12_a_card_with_nowhere_to_go_overflows_and_loses()
        {
            // slot shows red only; buffer 2; a stack of 3 blue overflows on the third card
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(0), T(1) }, S("bl", 0, 1, 3), S("r", 1, 0, 6)), null, 2);
            var steps = b.Tap(0);
            Assert.That((b.Result, b.Loss), Is.EqualTo((BoardResult.Lost, BoardLoss.Overflow)));
            Assert.That(Kinds(steps).TakeLast(2), Is.EqualTo(new[] { BoardStepKind.Overflow, BoardStepKind.Lost }));
            Assert.That(b.Buffer.Count, Is.EqualTo(2));
        }

        // slots: red, blue; behind them yellow (col 0) and green (col 1). Green covers red and blue.
        static LevelData StuckLevel() => Level(2, 2, new[] { T(0), T(1), T(2), T(3) },
            S("y", 3, 2, 2), S("y2", 4, 2, 1), On("r", 0, 0, 0, 3), On("bl", 220, 0, 1, 3), On("g", 100, 1, 3, 3));

        [Test]
        public void R13_when_every_tap_left_overflows_the_attempt_is_lost_as_stuck()
        {
            var b = new BoardModel(StuckLevel());
            Assert.That(b.IsCovered(2) && b.IsCovered(3), "green covers red and blue");
            var steps = b.Tap(0);              // two yellows fill the buffer; yellow 1 and green would both overflow
            Assert.That((b.Result, b.Loss), Is.EqualTo((BoardResult.Lost, BoardLoss.Stuck)));
            Assert.That(steps.Last().Kind, Is.EqualTo(BoardStepKind.Lost));
            Assert.That(b.Buffer, Is.EqualTo(new[] { 2, 2 }), "the look-ahead leaves the state as it was");
            Assert.That(b.TapCount, Is.EqualTo(1));
        }

        [Test]
        public void R13_not_stuck_while_one_tap_still_fits()
        {
            var l = StuckLevel();
            l.BufferCapacity = 3;              // room for yellow 1 after the first tap
            var b = new BoardModel(l);
            b.Tap(0);
            Assert.That(b.Result, Is.EqualTo(BoardResult.Playing));
        }

        // ── R-15 ────────────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R15_same_taps_same_result()
        {
            var l = Level(2, 9, new[] { T(0), T(1), T(2) }, S("y", 0, 2, 3), S("r", 1, 0, 3), S("bl", 2, 1, 3));
            string Run() { var b = new BoardModel(l); var log = new List<string>(); foreach (var t in new[] { 0, 1, 2 }) log.AddRange(b.Tap(t).Select(s => s.ToString())); return string.Join("|", log) + b.Result; }
            Assert.That(Run(), Is.EqualTo(Run()));
        }

        // R-16 / R-17 (Undo, Extra Space) removed with the boosters (CR-005); R-18 Continue replaced by R-22 / R-23.

        // ── R-19 / R-22 Revive ──────────────────────────────────────────────────────────────────────────
        [Test]
        public void R22_revive_removes_the_two_emptiest_poles_then_places_the_pending_cards()
        {
            // slots: red, blue; yellow waits behind red. Three yellows into a buffer of 2: the third overflows.
            var l = Level(2, 2, new[] { T(0), T(1), T(2) }, S("y", 0, 2, 3), S("r", 1, 0, 3), S("b", 2, 1, 3));
            var b = new BoardModel(l);
            b.Tap(0);
            Assert.That((b.Result, b.Loss, b.CanRevive), Is.EqualTo((BoardResult.Lost, BoardLoss.Overflow, true)));
            var steps = b.Revive();
            // red (leftmost of two empty poles) pulls the red stack and leaves; yellow moves up and takes the 2 held;
            // blue is now the emptiest and pulls the blue stack; the pending yellow completes yellow
            Assert.That(steps.Count(s => s.Kind == BoardStepKind.TargetCompleted), Is.EqualTo(3));
            Assert.That(steps.Count(s => s.Kind == BoardStepKind.BufferToTarget), Is.EqualTo(2));
            Assert.That((b.Remaining(1), b.Remaining(2)), Is.EqualTo((0, 0)), "Remove took the red and blue stacks off the table");
            Assert.That(b.Result, Is.EqualTo(BoardResult.Won));
        }

        [Test]
        public void R22_revive_keeps_removing_one_more_pole_while_still_stuck_and_reports_no_loss_in_between()
        {
            var b = new BoardModel(StuckLevel(), new BoardRules { ReviveRemoves = 0 });
            b.Tap(0);                                    // stuck: buffer full of yellow, yellow 1 and green would overflow
            var steps = b.Revive();
            Assert.That(b.Result, Is.EqualTo(BoardResult.Playing));
            Assert.That(steps.Any(s => s.Kind == BoardStepKind.Lost || s.Kind == BoardStepKind.Overflow), Is.False);
            Assert.That(b.Remaining(2), Is.EqualTo(0), "red (covered by green) was pulled by the one extra Remove");
            Assert.That((b.SlotColor(0), b.Buffer.Count), Is.EqualTo((2, 0)), "yellow moved up and took the held cards");
        }

        [Test]
        public void R22_revive_is_only_after_a_loss_and_limited_per_attempt()
        {
            var l = Level(2, 1, new[] { T(0), T(1), T(2), T(3) }, S("y", 0, 2, 3), S("g", 1, 3, 3), S("r", 2, 0, 3), S("b", 3, 1, 3));
            var b = new BoardModel(l, new BoardRules { ReviveRemoves = 1, ReviveMaxPerAttempt = 1 }, 1);
            Assert.That(b.CanRevive, Is.False);
            Assert.That(b.Revive(), Is.Empty);
            b.Tap(0);                                    // yellow: 1 held, 2 pending
            b.Revive();                                  // red leaves, yellow moves up and completes
            Assert.That(b.Result, Is.EqualTo(BoardResult.Playing));
            b.Tap(1);                                    // green has no pole yet: overflows again
            Assert.That((b.Result, b.CanRevive), Is.EqualTo((BoardResult.Lost, false)));
        }

        // ── R-23 RV Slot ────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R23_rv_slot_while_playing_adds_cells_up_to_the_limit()
        {
            var b = new BoardModel(Level(2, 6, new[] { T(0), T(1) }, S("a", 0, 0, 3), S("b", 1, 1, 3)), new BoardRules { RvSlotAmount = 8, RvSlotMaxPerAttempt = 2 });
            Assert.That(b.RvSlotsLeft, Is.EqualTo(2));
            b.RvSlot(); b.RvSlot();
            Assert.That((b.BufferCapacity, b.RvSlotsLeft, b.CanRvSlot), Is.EqualTo((22, 0, false)));
            Assert.That(b.RvSlot(), Is.Empty);
        }

        [Test]
        public void R23_rv_slot_after_an_overflow_places_the_pending_cards_and_play_goes_on()
        {
            var l = Level(2, 6, new[] { T(0), T(0), T(1) }, S("bl", 0, 1, 3), S("r", 1, 0, 6));
            var b = new BoardModel(l, new BoardRules { RvSlotAmount = 8 }, 2);
            b.Tap(0);
            Assert.That(b.CanRvSlot, Is.True);
            var steps = b.RvSlot();
            Assert.That((b.Result, b.Loss, b.BufferCapacity), Is.EqualTo((BoardResult.Playing, BoardLoss.None, 10)));
            Assert.That(steps.Count(s => s.Kind == BoardStepKind.CardToBuffer), Is.EqualTo(1));
            b.Tap(1);                        // reds clear both red poles, blue moves up and takes the buffer
            Assert.That(b.Result, Is.EqualTo(BoardResult.Won));
        }

        [Test]
        public void R23_rv_slot_after_stuck_adds_room_so_a_tap_fits_again()
        {
            var b = new BoardModel(StuckLevel(), new BoardRules { RvSlotAmount = 4 });
            b.Tap(0);
            Assert.That(b.CanRvSlot, Is.True);
            b.RvSlot();
            Assert.That((b.Result, b.BufferCapacity), Is.EqualTo((BoardResult.Playing, 6)));
            Assert.That(b.Tap(4), Is.Not.Empty, "green fits in the buffer now");
        }
    }
}
