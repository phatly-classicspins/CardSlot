using System.Collections.Generic;
using ClassicSpins.PrototypeFramework.Domain;

namespace Game.Domain
{
    /// <summary>Knobs for one generated level (level-design §3/§4).</summary>
    public sealed class GenParams
    {
        public int Colors = 3;
        /// <summary>Targets per colour; every target holds <see cref="TargetCapacity"/> cards.</summary>
        public int TargetsPerColor = 2;
        public int TargetCapacity = 3;
        /// <summary>Highest layer index used (0 = no covering).</summary>
        public int MaxLayer = 1;
        public int BufferCapacity = 12;
        public int Slots = 3;
        public int MinCardsPerStack = 2;
        public int MaxCardsPerStack = 5;
        /// <summary>Chance (0..1) that the next card in a stack repeats the previous colour — long runs are easier.</summary>
        public float RunBias = 0.5f;
        /// <summary>Each stack holds exactly one run of one colour (FTUE level 1).</summary>
        public bool SingleRunStacks;
        public bool Hard;
    }

    /// <summary>
    /// Builds a random level that satisfies R-1/R-1b from <see cref="GenParams"/>. Whether it is winnable
    /// is the solver's call, not the generator's. Deterministic for a given seed (rule #14).
    /// </summary>
    public static class LevelGenerator
    {
        // Tray inner area is 956×606 world units; a card is 150 wide, 206 tall, +16 per card below it.
        public const int CardW = 150, CardH = 206, CardStep = 16, ColumnPitch = 220, LayerShift = 55;
        private static readonly int[] RowY = { 40, 330 };

        public static LevelData Generate(string id, GenParams p, IRandom random)
        {
            var level = new LevelData
            {
                Id = id, Seed = random.Seed, Hard = p.Hard, Slots = p.Slots, BufferCapacity = p.BufferCapacity,
            };
            // colours: pick p.Colors of the 6
            var palette = new List<int> { 0, 1, 2, 3, 4, 5 };
            random.Shuffle(palette);
            var colours = palette.GetRange(0, p.Colors);

            // target queue
            for (int c = 0; c < p.Colors; c++)
                for (int k = 0; k < p.TargetsPerColor; k++)
                    level.Targets.Add(new TargetSpec(colours[c], p.TargetCapacity));
            random.Shuffle(level.Targets);

            // the card multiset, then cut into stacks
            var pool = new List<int>();
            foreach (var t in level.Targets) for (int i = 0; i < t.Capacity; i++) pool.Add(t.Color);
            var stacks = p.SingleRunStacks ? SingleRuns(level.Targets) : CutIntoStacks(pool, p, random);

            // positions: layer 0 first, upper layers shifted right so they cover their neighbours
            var spots = new List<(int x, int y, int layer)>();
            for (int layer = 0; layer <= p.MaxLayer; layer++)
                for (int row = 0; row < RowY.Length; row++)
                    for (int col = 0; col < 4; col++)
                    {
                        int x = 40 + (layer % 5) * LayerShift + col * ColumnPitch;
                        if (x + CardW > 956 - 20) continue;
                        spots.Add((x, RowY[row], layer));
                    }
            // keep layer 0 full enough and spread the rest at random across layers
            var lower = spots.FindAll(s => s.layer == 0);
            var upper = spots.FindAll(s => s.layer > 0);
            random.Shuffle(lower); random.Shuffle(upper);
            var chosen = new List<(int x, int y, int layer)>();
            int upperShare = p.MaxLayer == 0 ? 0 : System.Math.Min(upper.Count, stacks.Count / 2);
            chosen.AddRange(upper.GetRange(0, upperShare));
            int needLower = stacks.Count - upperShare;
            if (needLower > lower.Count) { chosen.AddRange(upper.GetRange(upperShare, System.Math.Min(upper.Count - upperShare, needLower - lower.Count))); needLower = lower.Count; }
            chosen.AddRange(lower.GetRange(0, needLower));
            if (chosen.Count < stacks.Count) return null;   // not enough room — caller retries with another seed

            for (int i = 0; i < stacks.Count; i++)
            {
                var spot = chosen[i];
                var cards = stacks[i];
                level.Stacks.Add(new StackSpec($"s{i}", spot.x, spot.y, CardW, CardH + CardStep * (cards.Count - 1), spot.layer, cards.ToArray()));
            }
            // stable, readable order: by layer, then row, then column
            level.Stacks.Sort((a, b) => a.Layer != b.Layer ? a.Layer.CompareTo(b.Layer) : a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            for (int i = 0; i < level.Stacks.Count; i++) level.Stacks[i].Id = $"s{i}";
            return LevelValidator.Validate(level).Count == 0 ? level : null;
        }

        private static List<List<int>> SingleRuns(List<TargetSpec> targets)
        {
            var result = new List<List<int>>();
            foreach (var t in targets)
            {
                var s = new List<int>();
                for (int i = 0; i < t.Capacity; i++) s.Add(t.Color);
                result.Add(s);
            }
            return result;
        }

        private static List<List<int>> CutIntoStacks(List<int> pool, GenParams p, IRandom random)
        {
            // order the pool with a run bias: repeatedly pick the previous colour with probability RunBias
            random.Shuffle(pool);
            var ordered = new List<int>();
            int prev = -1;
            while (pool.Count > 0)
            {
                int pick = -1;
                if (prev >= 0 && random.NextFloat() < p.RunBias) pick = pool.IndexOf(prev);
                if (pick < 0) pick = random.NextInt(0, pool.Count);
                prev = pool[pick];
                ordered.Add(prev);
                pool.RemoveAt(pick);
            }
            var stacks = new List<List<int>>();
            int at = 0;
            while (at < ordered.Count)
            {
                int left = ordered.Count - at;
                int n = random.NextInt(p.MinCardsPerStack, p.MaxCardsPerStack + 1);
                if (left - n > 0 && left - n < p.MinCardsPerStack) n = left - p.MinCardsPerStack;
                if (n > left) n = left;
                if (n < 1) n = 1;
                stacks.Add(ordered.GetRange(at, n));
                at += n;
            }
            return stacks;
        }
    }
}
