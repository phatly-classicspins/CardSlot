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
        /// <summary>Cards one tap may take (0 = the whole run); generation in groups uses 1 (CR-008).</summary>
        public int MaxRun;
    }

    /// <summary>
    /// Builds a random level that satisfies R-1/R-1b from <see cref="GenParams"/>. Whether it is winnable
    /// is the solver's call, not the generator's. Deterministic for a given seed (rule #14).
    /// </summary>
    public static class LevelGenerator
    {
        // A stack's footprint is one card, 150 × 206 world units. The grid leaves a small gap between cards; the tray
        // is sized to the layout by the view (CR-011), so only the column / row counts are capped.
        public const int CardW = 150, CardH = 206, GridPitchX = 176, GridPitchY = 232, LayerDX = 42, LayerDY = 52;
        private const int MaxColumns = 5, MaxRows = 3;

        public static LevelData Generate(string id, GenParams p, IRandom random)
        {
            var level = new LevelData
            {
                Id = id, Seed = random.Seed, Slots = p.Slots, BufferCapacity = p.BufferCapacity, MaxRun = p.MaxRun,
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

            // CR-011 (the reference, video IMG_3750): stacks sit on an even grid; a higher layer sits a little down
            // and to the right of the stack below it, so the lower one shows at the top-left. Layer counts shrink
            // upward and every higher stack rests on a stack of the layer below.
            int layers = System.Math.Min(p.MaxLayer, stacks.Count - 1) + 1;
            var counts = new int[layers];
            int left = stacks.Count;
            for (int layer = 0; layer < layers; layer++)
            {
                int share = layer == layers - 1 ? left : System.Math.Max(1, (int)System.Math.Ceiling(left * (layers == 1 ? 1.0 : 0.55)));
                if (layer > 0) share = System.Math.Min(share, counts[layer - 1]);
                if (layer == layers - 1 && share < left) return null;          // too many stacks for the layers — caller retries
                counts[layer] = share;
                left -= share;
            }
            int n0 = counts[0];
            int cols = System.Math.Min(MaxColumns, (int)System.Math.Ceiling(System.Math.Sqrt(n0)));
            int rows = (n0 + cols - 1) / cols;
            if (rows > MaxRows) return null;
            var spots = new List<(int x, int y, int layer)>();
            var below = new List<(int x, int y)>();
            for (int i = 0; i < n0; i++)
            {
                int row = i / cols, col = i % cols;
                int inRow = System.Math.Min(cols, n0 - row * cols);
                int x = (int)((cols - inRow) * GridPitchX / 2) + col * GridPitchX;   // a short last row is centred
                below.Add((x, row * GridPitchY));
            }
            foreach (var b in below) spots.Add((b.x, b.y, 0));
            for (int layer = 1; layer < layers; layer++)
            {
                random.Shuffle(below);
                var next = new List<(int x, int y)>();
                for (int i = 0; i < counts[layer]; i++) next.Add((below[i].x + LayerDX, below[i].y + LayerDY));
                foreach (var s in next) spots.Add((s.x, s.y, layer));
                below = next;
            }

            for (int i = 0; i < stacks.Count; i++)
            {
                var spot = spots[i];
                level.Stacks.Add(new StackSpec($"s{i}", spot.x, spot.y, CardW, CardH, spot.layer, stacks[i].ToArray()));
            }
            // stable, readable order: by layer, then row, then column
            level.Stacks.Sort((a, b) => a.Layer != b.Layer ? a.Layer.CompareTo(b.Layer) : a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            for (int i = 0; i < level.Stacks.Count; i++) level.Stacks[i].Id = $"s{i}";
            return LevelValidator.Validate(level).Count == 0 ? level : null;
        }

        /// <summary>
        /// CR-006: turn a level generated in groups into the shipped card level — every card becomes
        /// <paramref name="cardsPerGroup"/> cards of its colour, every target needs that many more, and the
        /// holding area holds <paramref name="holdCards"/> single cards (one per groove, as in the reference).
        /// Taps move whole runs (R-6), so a solution of the group level solves the card level tap for tap,
        /// provided <paramref name="holdCards"/> overflows on the same group: ⌊hold / group⌋ = the group buffer.
        /// Stack rectangles keep the group layout.
        /// </summary>
        public static LevelData ExpandToCards(LevelData groups, int cardsPerGroup, int holdCards)
        {
            var level = new LevelData
            {
                Id = groups.Id, Seed = groups.Seed, Slots = groups.Slots, BufferCapacity = holdCards,
                Ftue = groups.Ftue, Revision = groups.Revision,
                MaxRun = groups.MaxRun > 0 ? groups.MaxRun * cardsPerGroup : 0,   // one group a tap ⇒ 6 cards a tap
            };
            foreach (var t in groups.Targets) level.Targets.Add(new TargetSpec(t.Color, t.Capacity * cardsPerGroup));
            foreach (var s in groups.Stacks)
            {
                var cards = new int[s.Cards.Length * cardsPerGroup];
                for (int i = 0; i < cards.Length; i++) cards[i] = s.Cards[i / cardsPerGroup];
                level.Stacks.Add(new StackSpec(s.Id, s.X, s.Y, s.W, s.H, s.Layer, cards));
            }
            return level;
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
