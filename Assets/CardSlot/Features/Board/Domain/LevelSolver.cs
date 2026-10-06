using System.Collections.Generic;
using ClassicSpins.PrototypeFramework.Domain;

namespace Game.Domain
{
    public enum SolveStatus { Solved, Unsolvable, SearchLimit }

    /// <summary>The solver's verdict on one level (level-design §5).</summary>
    public sealed class SolveReport
    {
        public SolveStatus Status;
        /// <summary>A winning tap sequence (stack ids) without boosters, when <see cref="Status"/> is Solved.</summary>
        public List<string> Solution = new List<string>();
        /// <summary>The smallest buffer capacity that still has a booster-free solution. Slack =
        /// level capacity − this; less slack = harder.</summary>
        public int MinBufferCapacity = -1;
        /// <summary>Share of random-tap playouts that win (a ranking proxy only — G5: not a player win rate).</summary>
        public double BotWinRate;
        public int StatesExplored;
    }

    /// <summary>
    /// Proves a level winnable without boosters (depth-first search over taps, memoised on the board
    /// state) and measures its difficulty. Engine-free; randomness only through <see cref="IRandom"/>.
    /// </summary>
    public static class LevelSolver
    {
        public const int DefaultStateLimit = 400_000;

        public static SolveReport Analyze(LevelData level, IRandom random, int botPlayouts = 200, int stateLimit = DefaultStateLimit)
        {
            var report = new SolveReport();
            var solution = new List<int>();
            int explored;
            var status = Solve(level, level.BufferCapacity, stateLimit, solution, out explored);
            report.Status = status;
            report.StatesExplored = explored;
            if (status != SolveStatus.Solved) return report;
            foreach (var s in solution) report.Solution.Add(level.Stacks[s].Id);
            // smallest capacity that still solves
            report.MinBufferCapacity = level.BufferCapacity;
            for (int cap = 0; cap < level.BufferCapacity; cap++)
            {
                int ignored;
                if (Solve(level, cap, stateLimit, new List<int>(), out ignored) == SolveStatus.Solved) { report.MinBufferCapacity = cap; break; }
            }
            report.BotWinRate = BotWinRate(level, random, botPlayouts);
            return report;
        }

        /// <summary>DFS with memo. Taps whose whole run fits a target are tried first.</summary>
        public static SolveStatus Solve(LevelData level, int bufferCapacity, int stateLimit, List<int> solution, out int explored)
        {
            var board = new BoardModel(level, null, bufferCapacity);
            var seen = new HashSet<string>();
            int count = 0;
            bool hitLimit = false;
            bool found = Dfs(board, seen, solution, stateLimit, ref count, ref hitLimit);
            explored = count;
            if (found) return SolveStatus.Solved;
            return hitLimit ? SolveStatus.SearchLimit : SolveStatus.Unsolvable;
        }

        private static bool Dfs(BoardModel board, HashSet<string> seen, List<int> path, int limit, ref int count, ref bool hitLimit)
        {
            if (board.Result == BoardResult.Won) return true;
            if (board.Result != BoardResult.Playing) return false;
            if (!seen.Add(board.StateKey())) return false;
            if (++count > limit) { hitLimit = true; return false; }
            var moves = new List<int>();
            for (int i = 0; i < board.StackCount; i++) if (board.CanTap(i)) moves.Add(i);
            moves.Sort((a, b) => (board.RunFitsTargets(b) ? 1 : 0).CompareTo(board.RunFitsTargets(a) ? 1 : 0));
            foreach (var m in moves)
            {
                board.Tap(m);
                path.Add(m);
                if (Dfs(board, seen, path, limit, ref count, ref hitLimit)) return true;
                path.RemoveAt(path.Count - 1);
                board.UndoForSearch();
                if (hitLimit) return false;
            }
            return false;
        }

        /// <summary>Plays <paramref name="playouts"/> games tapping a uniformly random open stack.</summary>
        public static double BotWinRate(LevelData level, IRandom random, int playouts)
        {
            if (playouts <= 0) return 0;
            int wins = 0;
            var open = new List<int>();
            for (int p = 0; p < playouts; p++)
            {
                var board = new BoardModel(level);
                while (board.Result == BoardResult.Playing)
                {
                    open.Clear();
                    for (int i = 0; i < board.StackCount; i++) if (board.CanTap(i)) open.Add(i);
                    if (open.Count == 0) break;
                    board.Tap(open[random.NextInt(0, open.Count)]);
                }
                if (board.Result == BoardResult.Won) wins++;
            }
            return (double)wins / playouts;
        }

        /// <summary>Replays a tap sequence (stack ids) on a fresh board; true when it ends in a win.</summary>
        public static bool Replay(LevelData level, IReadOnlyList<string> solution)
        {
            var board = new BoardModel(level);
            foreach (var id in solution)
            {
                int index = level.Stacks.FindIndex(s => s.Id == id);
                if (index < 0 || board.Tap(index).Count == 0) return false;
            }
            return board.Result == BoardResult.Won;
        }
    }
}
