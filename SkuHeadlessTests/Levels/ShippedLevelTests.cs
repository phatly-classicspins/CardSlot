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

        [Test]
        public void First_levels_are_easy_like_the_reference()
        {
            // CR-011: one target per colour and every peg in view up to level 4; the back row from level 5
            for (int n = 1; n <= 5; n++)
            {
                LevelJson.TryRead(File.ReadAllText(Path.Combine(Dir, $"level_{n:000}.json")), out var l, out _);
                int colours = l.Targets.Select(t => t.Color).Distinct().Count();
                Assert.That(l.Targets.Count, Is.EqualTo(colours), $"level {n}: one target per colour");
                Assert.That(l.Targets.Count > l.Slots, Is.EqualTo(n == 5), $"level {n}: waiting pegs only from level 5");
            }
            LevelJson.TryRead(File.ReadAllText(Path.Combine(Dir, "level_001.json")), out var l1, out _);
            Assert.That((l1.Slots, l1.Stacks.All(s => s.Layer == 0)), Is.EqualTo((2, true)), "level 1: two pegs, nothing covered");
        }
    }
}
