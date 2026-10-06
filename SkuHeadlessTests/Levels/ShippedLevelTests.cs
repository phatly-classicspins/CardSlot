using System.IO;
using System.Linq;
using Game.Domain;
using Game.Infrastructure;
using NUnit.Framework;

namespace CardSlot.SkuHeadlessTests.Levels
{
    /// <summary>
    /// Every shipped level file (Assets/CardSlot/Content/Levels) must load, pass validation, and its stored
    /// solution must win on the current rules without boosters (Bước 5 rule: replay every level's verified
    /// solution). A rule change that breaks a shipped level fails here by name.
    /// </summary>
    public class ShippedLevelTests
    {
        static string Dir => RepoLayout.Path("Assets", "CardSlot", "Content", "Levels");
        static string[] Files => Directory.Exists(Dir) ? Directory.GetFiles(Dir, "level_*.json").OrderBy(f => f).ToArray() : new string[0];

        [Test]
        public void There_are_exactly_30_levels_numbered_1_to_30()
        {
            var names = Files.Select(Path.GetFileNameWithoutExtension).ToArray();
            Assert.That(names, Is.EqualTo(Enumerable.Range(1, 30).Select(i => $"level_{i:000}").ToArray()));
        }

        [TestCaseSource(nameof(Files))]
        public void Level_loads_and_its_solution_wins(string file)
        {
            var json = File.ReadAllText(file);
            Assert.That(LevelJson.TryRead(json, out var level, out var error), Is.True, error);
            Assert.That(level.Id, Is.EqualTo(Path.GetFileNameWithoutExtension(file)));
            var solution = LevelJson.ReadSolution(json);
            Assert.That(solution, Is.Not.Empty, "a shipped level carries its verified solution");
            Assert.That(LevelSolver.Replay(level, solution), Is.True, "stored solution no longer wins");
        }

        [TestCaseSource(nameof(Files))]
        public void Hard_flag_is_on_every_fifth_level_only(string file)
        {
            LevelJson.TryRead(File.ReadAllText(file), out var level, out _);
            int n = int.Parse(level.Id.Substring("level_".Length));
            Assert.That(level.Hard, Is.EqualTo(n % 5 == 0));
        }

        [Test]
        public void Ftue_levels_follow_level_design_6()
        {
            LevelJson.TryRead(File.ReadAllText(Path.Combine(Dir, "level_001.json")), out var l1, out _);
            Assert.That((l1.Ftue, l1.Slots, l1.Stacks.Count), Is.EqualTo(("ftue.l1", 2, 4)));
            Assert.That(l1.Stacks.All(s => s.Layer == 0 && s.Cards.Distinct().Count() == 1), "level 1: one run per stack, nothing covered");
            Assert.That(LevelSolver.BotWinRate(l1, new ClassicSpins.PrototypeFramework.Domain.Pcg32(1), 100), Is.EqualTo(1.0), "level 1: every order wins");
            LevelJson.TryRead(File.ReadAllText(Path.Combine(Dir, "level_002.json")), out var l2, out _);
            int ignored;
            Assert.That(LevelSolver.Solve(l2, 0, LevelSolver.DefaultStateLimit, new System.Collections.Generic.List<int>(), out ignored),
                Is.Not.EqualTo(SolveStatus.Solved), "level 2 must make the player use the holding area");
        }
    }
}
