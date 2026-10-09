using System.Collections.Generic;
using System.Linq;
using ClassicSpins.PrototypeFramework.Domain;
using Game.Domain;
using NUnit.Framework;

namespace CardSlot.SkuHeadlessTests.Board
{
    /// <summary>Boosters (GDD v2.0 §2.7, features/boosters.md v2, D-035): R-19 Remove, R-20 Hand, R-21 Shuffle.
    /// Colours: 0 red, 1 blue, 2 yellow, 3 green.</summary>
    public class BoosterTests
    {
        static StackSpec S(string id, int col, int color, int count) => new StackSpec(id, col * 220, 0, 150, 206, 0, color, count);
        static StackSpec On(string id, int x, int layer, int color, int count) => new StackSpec(id, x, 0, 150, 206, layer, color, count);
        static TargetSpec T(int color, int cap = 3) => new TargetSpec(color, cap);

        static LevelData Level(int slots, int buffer, IEnumerable<TargetSpec> targets, params StackSpec[] stacks)
        {
            var l = new LevelData { Id = "t", Slots = slots, BufferCapacity = buffer };
            l.Targets.AddRange(targets);
            l.Stacks.AddRange(stacks);
            return l;
        }

        // slots: red, blue; behind them yellow (col 0) and green (col 1). Green covers red and blue.
        static LevelData StuckLevel() => Level(2, 2, new[] { T(0), T(1), T(2), T(3) },
            S("y", 3, 2, 2), S("y2", 4, 2, 1), On("r", 0, 0, 0, 3), On("bl", 220, 0, 1, 3), On("g", 100, 1, 3, 3));

        // ── R-20 Hand ──────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R20_hand_sends_a_covered_stack_and_is_not_a_tap()
        {
            var b = new BoardModel(StuckLevel());
            Assert.That(b.CanTap(2), Is.False, "red is under green");
            Assert.That(b.CanHand(2), Is.True);
            var steps = b.Hand(2);
            Assert.That(b.Remaining(2), Is.EqualTo(0));
            Assert.That(steps.Count(s => s.Kind == BoardStepKind.CardToTarget && s.Stack == 2), Is.EqualTo(3));
            Assert.That(steps.Any(s => s.Kind == BoardStepKind.TargetCompleted && s.Color == 0), Is.True);
            Assert.That((b.TapCount, b.Result), Is.EqualTo((0, BoardResult.Playing)));
        }

        [Test]
        public void R20_hand_on_the_lose_screen_after_a_stuck_loss_saves_the_board()
        {
            var b = new BoardModel(StuckLevel());
            b.Tap(0);                                   // stuck: two yellows held, everything else would overflow
            Assert.That((b.Result, b.Loss), Is.EqualTo((BoardResult.Lost, BoardLoss.Stuck)));
            b.Hand(2);                                  // red completes, yellow moves up and takes the held yellows
            Assert.That((b.Result, b.Loss, b.Buffer.Count), Is.EqualTo((BoardResult.Playing, BoardLoss.None, 0)));
        }

        [Test]
        public void R20_hand_waits_while_cards_of_an_overflow_are_still_pending()
        {
            var b = new BoardModel(Level(2, 2, new[] { T(0), T(1), T(2) }, S("y", 0, 2, 3), S("r", 1, 0, 3), S("b", 2, 1, 3)));
            b.Tap(0);                                   // three yellows into two cells: one pending
            Assert.That(b.Loss, Is.EqualTo(BoardLoss.Overflow));
            Assert.That(b.CanHand(1), Is.False);
            Assert.That(b.Hand(1), Is.Empty);
            Assert.That(b.Remaining(1), Is.EqualTo(3));
        }

        [Test]
        public void R20_hand_needs_a_stack_with_cards()
        {
            var b = new BoardModel(StuckLevel());
            b.Hand(2);
            Assert.That(b.CanHand(2), Is.False);
            Assert.That(b.CanHand(-1) || b.CanHand(99), Is.False);
        }

        // ── R-19 Remove ────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void R19_remove_on_the_slot_takes_open_stacks_before_covered_ones()
        {
            // slots: yellow, blue. Yellow: 2 under blue (layer 1), 1 open.
            var l = Level(2, 6, new[] { T(2), T(1) }, On("yLow", 0, 0, 2, 2), On("bTop", 100, 1, 1, 3), S("yOpen", 3, 2, 1));
            var b = new BoardModel(l);
            var steps = b.Remove(0);
            var from = steps.Where(s => s.Kind == BoardStepKind.CardToTarget).Select(s => s.Stack).ToArray();
            Assert.That(from, Is.EqualTo(new[] { 2, 0, 0 }), "open yellow first, then the covered one");
            Assert.That(steps.Any(s => s.Kind == BoardStepKind.TargetCompleted && s.Slot == 0), Is.True);
            Assert.That(b.TapCount, Is.EqualTo(0));
        }

        [Test]
        public void R19_remove_on_a_waiting_pole_takes_the_buffer_then_the_board_and_the_queue_moves_up()
        {
            // slots: blue, yellow; red waits behind blue. Tapping the 2-red stack holds both reds.
            var l = Level(2, 4, new[] { T(1), T(2), T(0) }, S("r1", 0, 0, 2), S("r2", 1, 0, 1), S("b", 2, 1, 3), S("y", 3, 2, 3));
            var b = new BoardModel(l);
            b.Tap(0);
            Assert.That(b.Buffer, Is.EqualTo(new[] { 0, 0 }));
            Assert.That(b.CanRemove(0, 1), Is.True);
            Assert.That(b.CanRemove(0, 2) || b.CanRemove(1, 1), Is.False, "nothing waits deeper, nor behind yellow");
            var steps = b.Remove(0, 1);
            Assert.That(steps.Select(s => s.Kind).Where(k => k != BoardStepKind.StackEmptied), Is.EqualTo(new[]
            {
                BoardStepKind.BufferToQueued, BoardStepKind.BufferToQueued, BoardStepKind.CardToQueued, BoardStepKind.QueuedTargetCompleted,
            }));
            Assert.That(steps.First(s => s.Kind == BoardStepKind.CardToQueued).Stack, Is.EqualTo(1));
            Assert.That((b.Buffer.Count, b.QueuedCount(0), b.Remaining(1)), Is.EqualTo((0, 0, 0)));
            Assert.That((b.SlotColor(0), b.SlotFilled(0)), Is.EqualTo((1, 0)), "the blue pole in front is untouched");
            Assert.That(b.Result, Is.EqualTo(BoardResult.Playing));
        }

        [Test]
        public void R19_remove_on_the_lose_screen_takes_the_pending_cards_first_and_play_goes_on()
        {
            // slots: red, blue; yellow waits behind red. Three yellows into two cells: one pending.
            var b = new BoardModel(Level(2, 2, new[] { T(0), T(1), T(2) }, S("y", 0, 2, 3), S("r", 1, 0, 3), S("b", 2, 1, 3)));
            b.Tap(0);
            var steps = b.Remove(0, 1);
            Assert.That(steps.First().Kind, Is.EqualTo(BoardStepKind.CardToQueued), "the pending yellow goes first");
            Assert.That(steps.Count(s => s.Kind == BoardStepKind.BufferToQueued), Is.EqualTo(2));
            Assert.That((b.Result, b.Loss, b.Buffer.Count, b.QueuedCount(0)), Is.EqualTo((BoardResult.Playing, BoardLoss.None, 0, 0)));
        }

        [Test]
        public void R19_remove_of_the_last_cards_wins()
        {
            var b = new BoardModel(Level(2, 3, new[] { T(0), T(1) }, S("r", 0, 0, 3), S("b", 1, 1, 3)));
            b.Tap(0);                                   // red done
            var steps = b.Remove(1);                    // blue is the last pole
            Assert.That(b.Result, Is.EqualTo(BoardResult.Won));
            Assert.That(steps.Last().Kind, Is.EqualTo(BoardStepKind.Won));
        }

        // ── R-21 Shuffle ───────────────────────────────────────────────────────────────────────────────
        // slots: red (6), blue; waiting: yellow (col 0), green (col 1).
        static LevelData ShuffleLevel() => Level(2, 6, new[] { T(0, 6), T(1), T(2), T(3) },
            S("y", 0, 2, 3), S("g", 1, 3, 3), S("r1", 2, 0, 3), S("r2", 3, 0, 3), S("b", 4, 1, 3));

        [Test]
        public void R21_shuffle_brings_the_held_colour_down_and_never_moves_a_pole_with_cards()
        {
            var b = new BoardModel(ShuffleLevel(), random: new Pcg32(7));
            b.Tap(2);                                   // red 3/6
            b.Tap(1);                                   // three greens held: green waits behind blue
            Assert.That(b.Buffer, Is.EqualTo(new[] { 3, 3, 3 }));
            var steps = b.Shuffle();
            Assert.That(steps.First().Kind, Is.EqualTo(BoardStepKind.Shuffled));
            Assert.That(steps.Any(s => s.Kind == BoardStepKind.TargetCompleted && s.Color == 3), Is.True,
                "green came down to the empty slot and took the held greens");
            Assert.That(b.Buffer.Count, Is.EqualTo(0));
            Assert.That((b.SlotColor(0), b.SlotFilled(0)), Is.EqualTo((0, 3)), "the red pole holding cards stayed");
            Assert.That(b.TapCount, Is.EqualTo(2));
        }

        [Test]
        public void R21_shuffle_keeps_every_column_count_and_the_same_seed_gives_the_same_order()
        {
            var targets = new[] { T(0), T(1), T(2), T(3), T(0), T(1), T(2) };
            var l = Level(2, 9, targets, S("r", 0, 0, 6), S("b", 1, 1, 6), S("y", 2, 2, 6), S("g", 3, 3, 3));
            string Order(long seed)
            {
                var b = new BoardModel(l, random: new Pcg32(seed));
                var before = (b.QueuedCount(0), b.QueuedCount(1));
                b.Shuffle();
                Assert.That((b.QueuedCount(0), b.QueuedCount(1)), Is.EqualTo(before));
                var cols = Enumerable.Range(0, 2).Select(s =>
                    b.SlotColor(s) + ":" + string.Join(",", Enumerable.Range(0, b.QueuedCount(s)).Select(k => b.QueuedColor(s, k))));
                return string.Join("|", cols);
            }
            Assert.That(Order(11), Is.EqualTo(Order(11)));
        }

        [Test]
        public void R21_shuffle_picks_colours_from_the_open_stacks_when_nothing_is_held()
        {
            // slots: red, blue; waiting yellow, green. Only yellow is open (higher layer): yellow comes down to slot 0.
            var l = Level(2, 6, new[] { T(0), T(1), T(2), T(3) },
                On("r", 0, 0, 0, 3), On("bl", 220, 0, 1, 3), On("y", 100, 1, 2, 3), S("g", 4, 3, 3));
            var b = new BoardModel(l, random: new Pcg32(3));
            b.Shuffle();
            Assert.That(b.SlotColor(0), Is.EqualTo(2), "first colour: yellow (layer 1) goes to the first empty slot");
        }

        [Test]
        public void R21_shuffle_needs_the_seeded_random_and_two_empty_poles()
        {
            Assert.That(new BoardModel(ShuffleLevel()).CanShuffle, Is.False, "no IRandom");
            // two poles, both holding a card already: nothing may move
            var full = new BoardModel(Level(2, 6, new[] { T(0, 6), T(1, 6) }, S("r", 0, 0, 3), S("r2", 1, 0, 3), S("b", 2, 1, 3), S("b2", 3, 1, 3)),
                random: new Pcg32(1));
            full.Tap(0); full.Tap(2);
            Assert.That(full.CanShuffle, Is.False, "every pole holds cards");
            Assert.That(full.Shuffle(), Is.Empty);
        }
    }
}
