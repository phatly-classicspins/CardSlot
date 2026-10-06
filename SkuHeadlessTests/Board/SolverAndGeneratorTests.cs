using System.Collections.Generic;
using ClassicSpins.PrototypeFramework.Domain;
using Game.Domain;
using Game.Infrastructure;
using NUnit.Framework;

namespace CardSlot.SkuHeadlessTests.Board
{
    public class SolverAndGeneratorTests
    {
        static LevelData Small()
        {
            var l = new LevelData { Id = "t", Slots = 2, BufferCapacity = 6 };
            l.Targets.AddRange(new[] { new TargetSpec(0, 3), new TargetSpec(1, 3), new TargetSpec(2, 3) });
            l.Stacks.Add(new StackSpec("y", 0, 0, 150, 206, 0, 2, 2, 2));
            l.Stacks.Add(new StackSpec("r", 220, 0, 150, 206, 0, 0, 0, 0));
            l.Stacks.Add(new StackSpec("b", 440, 0, 150, 206, 0, 1, 1, 1));
            return l;
        }

        [Test]
        public void Solver_finds_a_replayable_solution_and_the_minimum_buffer()
        {
            var r = LevelSolver.Analyze(Small(), new Pcg32(1), 50);
            Assert.That(r.Status, Is.EqualTo(SolveStatus.Solved));
            Assert.That(LevelSolver.Replay(Small(), r.Solution), Is.True);
            Assert.That(r.MinBufferCapacity, Is.EqualTo(0), "red then blue then yellow never needs the buffer");
            Assert.That(r.BotWinRate, Is.EqualTo(1.0), "buffer 6 makes every order a win");
        }

        [Test]
        public void Solver_reports_an_unwinnable_level()
        {
            // yellow sits on top of the reds and the blues, and its target only appears after red or blue
            // completes: with no buffer that is a deadlock, with one buffer cell it is not
            var u = new LevelData { Id = "u", Slots = 2, BufferCapacity = 6 };
            u.Targets.AddRange(new[] { new TargetSpec(0, 3), new TargetSpec(1, 3), new TargetSpec(2, 3) });
            u.Stacks.Add(new StackSpec("a", 0, 0, 150, 254, 0, 2, 0, 0, 0));
            u.Stacks.Add(new StackSpec("b", 220, 0, 150, 254, 0, 2, 1, 1, 1));
            u.Stacks.Add(new StackSpec("c", 440, 0, 150, 206, 0, 2));
            var solution = new List<int>();
            int explored;
            Assert.That(LevelSolver.Solve(u, 0, 10_000, solution, out explored), Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(LevelSolver.Solve(u, 1, 10_000, solution, out explored), Is.EqualTo(SolveStatus.Solved));
            Assert.That(LevelSolver.Analyze(u, new Pcg32(3), 0).MinBufferCapacity, Is.EqualTo(1));
        }

        [Test]
        public void Generator_is_deterministic_for_a_seed_and_respects_R1_R1b()
        {
            var p = new GenParams { Colors = 4, TargetsPerColor = 2, MaxLayer = 2, BufferCapacity = 12 };
            LevelData a = null, b = null;
            for (long seed = 1; a == null; seed++) { a = LevelGenerator.Generate("g", p, new Pcg32(seed)); if (a != null) b = LevelGenerator.Generate("g", p, new Pcg32(seed)); }
            Assert.That(LevelValidator.Validate(a), Is.Empty);
            Assert.That(LevelJson.Write(a), Is.EqualTo(LevelJson.Write(b)));
        }

        [Test]
        public void Level_json_round_trips_and_rejects_bad_data()
        {
            var json = LevelJson.Write(Small(), new[] { "r", "b", "y" });
            Assert.That(LevelJson.TryRead(json, out var back, out var error), Is.True, error);
            Assert.That(LevelJson.Write(back, new[] { "r", "b", "y" }), Is.EqualTo(json));
            Assert.That(LevelJson.ReadSolution(json), Is.EqualTo(new[] { "r", "b", "y" }));
            Assert.That(LevelJson.TryRead(json.Replace("\"color_2\"", "\"purple\""), out _, out error), Is.False);
            Assert.That(error, Does.Contain("purple"));
            Assert.That(LevelJson.TryRead("{ not json", out _, out _), Is.False);
        }
    }
}
