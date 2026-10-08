using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClassicSpins.PrototypeFramework.Domain;
using Game.Domain;
using Game.Infrastructure;
using NUnit.Framework;

namespace CardSlot.SkuHeadlessTests.Levels
{
    /// <summary>One row of the difficulty curve (level-design.md §4).</summary>
    sealed class Row
    {
        public int Level, Colors, MaxLayer, Buffer, Slots = 3, TargetsPerColor = 2;
        public string Role;
        /// <summary>Wanted difficulty = 1 − random-bot win rate (a ranking proxy, G5).</summary>
        public double TargetD;
        public float RunBias;
        public bool SingleRun;
        public int MinBufferAtLeast; // FTUE level 2 must make the player use the buffer
        public string Ftue;
    }

    /// <summary>
    /// The level tool (Bước 5, D-023), run as an opt-in [Slow] sweep because this machine blocks new
    /// executables (Application Control) while allowing the test host. It generates candidates with the
    /// SKU's own LevelGenerator, proves each with LevelSolver, picks 30 along level-design.md §4 and writes
    /// Assets/CardSlot/Content/Levels/*.json and docs/design/level-report.md:
    ///   PF_RUN_SLOW=1 dotnet test SkuHeadlessTests --filter FullyQualifiedName~LevelGenSweep
    /// </summary>
    public class LevelGenSweep
    {
        const int CandidatesPerLevel = 40, MaxAttemptsPerLevel = 600, BotPlayouts = 300;
        // CR-006: levels are generated in groups and shipped in cards — 6 cards a group, a target needs 18,
        // and the holding area has 26 grooves like the reference, which overflows on the 5th group: 4 groups.
        const int CardsPerGroup = 6, HoldCards = 26, GroupBuffer = HoldCards / CardsPerGroup;

        [Test, Slow]  // deterministic: re-running rewrites byte-identical files
        public void Generate_all_levels()
        {
            string root = RepoLayout.RepoRoot;
            string outDir = Path.Combine(root, "Assets", "CardSlot", "Content", "Levels");
            Directory.CreateDirectory(outDir);
            var rows = Curve();
            var report = new List<(Row row, LevelData level, SolveReport solve, double d, int candidates)>();
            foreach (var row in rows)
            {
                var (groups, _, d, n) = Pick(row);
                if (groups == null) Assert.Fail($"level {row.Level}: no solvable candidate");
                var level = LevelGenerator.ExpandToCards(groups, CardsPerGroup, HoldCards);
                level.Revision = 2;   // CR-006 changed every shipped level
                var solve = LevelSolver.Analyze(level, new Pcg32(level.Seed), BotPlayouts);
                if (solve.Solution.Count == 0) Assert.Fail($"level {row.Level}: the card level does not solve");
                File.WriteAllText(Path.Combine(outDir, $"{level.Id}.json"), LevelJson.Write(level, solve.Solution) + "\n");
                report.Add((row, level, solve, d, n));
                TestContext.Progress.WriteLine($"{level.Id} seed {level.Seed} D {d:0.00} (want {row.TargetD:0.00}) minBuffer {solve.MinBufferCapacity}/{level.BufferCapacity} from {n} candidates");
            }
            File.WriteAllText(Path.Combine(root, "docs", "design", "level-report.md"), Report(report), new UTF8Encoding(false));
        }

        static (LevelData, SolveReport, double, int) Pick(Row row)
        {
            var p = new GenParams
            {
                Colors = row.Colors, TargetsPerColor = row.TargetsPerColor, MaxLayer = row.MaxLayer, BufferCapacity = GroupBuffer,
                Slots = row.Slots, RunBias = row.RunBias, SingleRunStacks = row.SingleRun,
                MinCardsPerStack = row.SingleRun ? 3 : 2, MaxCardsPerStack = row.SingleRun ? 3 : 5,
                MaxRun = 1,   // CR-008: a tap takes one group (6 cards once expanded)
            };
            LevelData best = null; double bestD = 0, bestScore = double.MaxValue; int found = 0;
            for (int attempt = 0; attempt < MaxAttemptsPerLevel && found < CandidatesPerLevel; attempt++)
            {
                long seed = row.Level * 100_000L + attempt;   // logged in the file (rule #14)
                var level = LevelGenerator.Generate($"level_{row.Level:000}", p, new Pcg32(seed));
                if (level == null) continue;
                level.Ftue = row.Ftue;
                var solution = new List<int>();
                if (LevelSolver.Solve(level, level.BufferCapacity, LevelSolver.DefaultStateLimit, solution, out _) != SolveStatus.Solved) continue;
                double d = 1 - LevelSolver.BotWinRate(level, new Pcg32(seed), BotPlayouts);
                double score = Math.Abs(d - row.TargetD);
                if (row.MinBufferAtLeast > 0)
                {
                    int ignored;
                    bool solvableWithLess = LevelSolver.Solve(level, row.MinBufferAtLeast - 1, LevelSolver.DefaultStateLimit, new List<int>(), out ignored) == SolveStatus.Solved;
                    if (solvableWithLess) continue;
                }
                found++;
                if (score < bestScore) { best = level; bestD = d; bestScore = score; }
            }
            if (best == null) return (null, null, 0, found);
            var report = LevelSolver.Analyze(best, new Pcg32(best.Seed), BotPlayouts);
            return (best, report, bestD, found);
        }

        // level-design.md §4, with the wanted difficulty as a sawtooth: rising inside each group of five,
        // (no hard levels since Phat: the game has no difficulty tiers — the curve only orders the levels)
        static List<Row> Curve()
        {
            var rows = new List<Row>();
            void R(int level, string role, int colors, int layer, int buffer, double d, float runBias, int perColor = 2)
                => rows.Add(new Row { Level = level, Role = role, Colors = colors, MaxLayer = layer, Buffer = buffer, TargetD = d, RunBias = runBias,
                                      TargetsPerColor = perColor });
            // CR-011: the first levels as easy as the reference (video IMG_3750) — one target per colour, every peg in
            // view; the back row of waiting pegs only from level 5
            rows.Add(new Row { Level = 1, Role = "mở đầu", Colors = 2, MaxLayer = 0, Buffer = 16, Slots = 2, TargetsPerColor = 1, TargetD = 0, RunBias = 0.8f, Ftue = "ftue.l1" });
            rows.Add(new Row { Level = 2, Role = "dễ", Colors = 3, MaxLayer = 0, Buffer = 14, Slots = 3, TargetsPerColor = 1, TargetD = 0.05, RunBias = 0.7f, Ftue = "ftue.l2" });
            rows.Add(new Row { Level = 3, Role = "dễ", Colors = 3, MaxLayer = 1, Buffer = 14, Slots = 3, TargetsPerColor = 1, TargetD = 0.08, RunBias = 0.6f });
            rows.Add(new Row { Level = 4, Role = "dễ · che phủ", Colors = 3, MaxLayer = 1, Buffer = 14, Slots = 3, TargetsPerColor = 1, TargetD = 0.12, RunBias = 0.5f });
            rows.Add(new Row { Level = 5, Role = "hàng cọc chờ", Colors = 4, MaxLayer = 1, Buffer = 14, Slots = 2, TargetsPerColor = 1, TargetD = 0.15, RunBias = 0.5f });
            R(6, "nghỉ", 3, 1, 14, 0.15, 0.4f, 4);
            R(7, "trung bình", 4, 1, 12, 0.25, 0.45f, 3);
            R(8, "trung bình", 4, 2, 12, 0.30, 0.4f, 3);
            R(9, "trung bình", 4, 2, 12, 0.35, 0.45f, 3);
            R(10, "khó", 4, 2, 10, 0.55, 0.3f, 3);
            R(11, "nghỉ", 4, 1, 12, 0.25, 0.45f, 3);
            R(12, "trung bình · run ngắn", 5, 2, 11, 0.35, 0.35f);
            R(13, "trung bình · run ngắn", 5, 2, 11, 0.40, 0.35f);
            R(14, "trung bình · run ngắn", 5, 2, 11, 0.45, 0.35f);
            R(15, "khó", 5, 3, 10, 0.60, 0.3f);
            R(16, "nghỉ", 4, 2, 12, 0.30, 0.4f, 3);
            R(17, "trung bình", 5, 3, 10, 0.45, 0.4f);
            R(18, "trung bình", 5, 3, 10, 0.50, 0.4f);
            R(19, "trung bình", 5, 3, 10, 0.55, 0.4f);
            R(20, "khó", 5, 3, 9, 0.65, 0.3f);
            R(21, "nghỉ", 5, 2, 11, 0.40, 0.4f, 2);
            R(22, "khó", 6, 3, 10, 0.55, 0.35f);
            R(23, "khó", 6, 3, 10, 0.60, 0.35f);
            R(24, "khó", 6, 3, 10, 0.62, 0.35f);
            R(25, "khó", 6, 4, 9, 0.72, 0.3f);
            R(26, "nghỉ", 5, 3, 11, 0.45, 0.45f);
            R(27, "khó", 6, 4, 9, 0.65, 0.3f);
            R(28, "khó", 6, 4, 9, 0.68, 0.3f);
            R(29, "khó", 6, 4, 9, 0.70, 0.3f);
            R(30, "khó (cuối)", 6, 4, 8, 0.80, 0.25f);
            rows.Sort((a, b) => a.Level.CompareTo(b.Level));
            return rows;
        }

        static string Report(List<(Row row, LevelData level, SolveReport solve, double d, int candidates)> items)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("# Báo cáo level — CardSlot");
            sb.AppendLine();
            sb.AppendLine("> Sinh tự động bởi `SkuHeadlessTests/Levels/LevelGenSweep.cs` (`PF_RUN_SLOW=1 dotnet test SkuHeadlessTests --filter FullyQualifiedName~LevelGenSweep`), không sửa tay (G12). Đường cong: `level-design.md` §4. Quyết định: D-023.");
            sb.AppendLine("> **Độ khó D = 1 − tỉ lệ thắng của bot chạm ngẫu nhiên** (300 ván). Đây chỉ là thước đo để xếp level (G5), không phải tỉ lệ thắng của người chơi. Chơi thử ở Bước 6/8 mới là bằng chứng.");
            sb.AppendLine("> **Ô tạm tối thiểu** = sức chứa nhỏ nhất mà level vẫn thắng được không cần booster; **dư** = sức chứa thật − tối thiểu (càng ít càng khó).");
            sb.AppendLine();
            sb.AppendLine("Mọi level dưới đây đã được solver chứng minh thắng được không cần booster. Lời giải nằm trong trường `solution` của file level và được test `ShippedLevelTests` chạy lại mỗi lần `dotnet test`.");
            sb.AppendLine();
            sb.AppendLine("| Level | Vai trò | Màu | Lá | Chồng | Layer | Đích hiện | Ô tạm | Tối thiểu | Dư | Chạm | D muốn | D đạt | Ứng viên | Seed |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var (row, l, s, d, n) in items)
            {
                int cards = l.Stacks.Sum(x => x.Cards.Length);
                int layers = l.Stacks.Max(x => x.Layer) + 1;
                string name = row.Level.ToString(ci);
                sb.AppendLine(string.Format(ci, "| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} | {9} | {10} | {11:0.00} | {12:0.00} | {13} | {14} |",
                    name, row.Role, row.Colors, cards, l.Stacks.Count, layers, l.Slots, l.BufferCapacity, s.MinBufferCapacity,
                    l.BufferCapacity - s.MinBufferCapacity, s.Solution.Count, row.TargetD, d, n, l.Seed));
            }
            sb.AppendLine();
            sb.AppendLine("## Đường cong (D đạt được)");
            sb.AppendLine();
            sb.AppendLine("```");
            for (int band = 9; band >= 0; band--)
            {
                sb.Append(band == 9 ? "1.0 │" : band == 0 ? "0.0 │" : "    │");
                foreach (var it in items) sb.Append(it.d * 10 >= band + 0.5 ? " █" : "  ");
                sb.AppendLine();
            }
            sb.Append("    └");
            foreach (var it in items) sb.Append("──");
            sb.AppendLine();
            sb.Append("     ");
            foreach (var it in items) sb.Append(it.row.Level % 5 == 0 ? (it.row.Level.ToString(ci).PadLeft(2)) : "  ");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine();
            var misses = items.Where(i => Math.Abs(i.d - i.row.TargetD) > 0.12).ToList();
            sb.AppendLine("## Lệch đường cong (|D đạt − D muốn| > 0.12)");
            sb.AppendLine();
            if (misses.Count == 0) sb.AppendLine("Không có.");
            foreach (var m in misses) sb.AppendLine(string.Format(ci, "- Level {0}: muốn {1:0.00}, đạt {2:0.00}.", m.row.Level, m.row.TargetD, m.d));
            return sb.ToString();
        }
    }
}
