using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>One target (a pole) in a level's queue: a card colour (glossary <c>color_0..7</c> → 0..7) and how
    /// many cards fill it (GDD §2.1).</summary>
    public sealed class TargetSpec
    {
        public int Color;
        public int Capacity;

        public TargetSpec() { }
        public TargetSpec(int color, int capacity) { Color = color; Capacity = capacity; }
    }

    /// <summary>One stack on the board: <see cref="Count"/> cards of one <see cref="Color"/> (R-1c, GDD v2.0). The
    /// rectangle is in world units inside the tray and is what the covering rule (R-3) and R-1b test.</summary>
    public sealed class StackSpec
    {
        public string Id;
        public int X, Y, W, H, Layer;
        public int Color, Count;
        /// <summary>Presentation-only layout: spread the stack as a fan; rules still use its rectangle.</summary>
        public bool Fan;
        /// <summary>Use the wider reference spacing when drawing this stack.</summary>
        public bool Spread;
        /// <summary>-1 reverses spreading, +1 spreads forward, 0 selects a stable outward direction automatically.</summary>
        public int SpreadDirection;
        /// <summary>Axis of a straight spread in tray degrees: 0 horizontal, 90 downward. Cards remain upright.</summary>
        public int SpreadAngle;
        /// <summary>Continue the authored pose sequence of this lower stack.</summary>
        public string OnStack;

        public StackSpec() { }
        public StackSpec(string id, int x, int y, int w, int h, int layer, int color, int count)
        {
            Id = id; X = x; Y = y; W = w; H = h; Layer = layer; Color = color; Count = count;
        }

        /// <summary>True when the two rectangles share an area greater than zero (touching edges do not).</summary>
        public bool Overlaps(StackSpec o) =>
            X < o.X + o.W && o.X < X + W && Y < o.Y + o.H && o.Y < Y + H;
    }

    /// <summary>A level as authored data (G20, <c>docs/design/level-design.md</c> §2). Immutable by convention:
    /// the board model copies what it mutates.</summary>
    public sealed class LevelData
    {
        public string Id;
        public int Revision = 1;
        /// <summary>The generator seed that produced this level (logged, so it can be reproduced).</summary>
        public long Seed;
        public int Slots = 3;
        public int BufferCapacity = 12;
        public List<TargetSpec> Targets = new List<TargetSpec>();
        public List<StackSpec> Stacks = new List<StackSpec>();
        /// <summary>FTUE script id for this level, or null.</summary>
        public string Ftue;

        /// <summary>GDD v2.0: one list of 8 colours shared by cards and poles.</summary>
        public const int ColorCount = 8;
    }

    /// <summary>Stable card-sequence placement. Indices refer to the input level, never live card counts.</summary>
    public struct StackPlacement
    {
        public int Root, Support, Offset, Direction;
        public StackPlacement(int root, int support, int offset) { Root = root; Support = support; Offset = offset; Direction = 1; }
    }

    /// <summary>Global layout rule shared by authored and generated levels. Does not mutate level/rule rectangles.</summary>
    public static class StackPlacementRules
    {
        public static StackPlacement[] Resolve(LevelData level)
        {
            var stacks = level.Stacks;
            var order = new List<int>();
            for (int i = 0; i < stacks.Count; i++) order.Add(i);
            order.Sort((a, b) => stacks[a].Layer != stacks[b].Layer
                ? stacks[a].Layer.CompareTo(stacks[b].Layer)
                : string.CompareOrdinal(stacks[a].Id, stacks[b].Id));
            var result = new StackPlacement[stacks.Count];
            var next = new int[stacks.Count];
            foreach (int i in order)
            {
                int support = FindSupport(stacks, i);
                int root = support < 0 ? i : result[support].Root;
                // Multiple stacks on the same root reserve successive intervals, never coincident card poses.
                result[i] = new StackPlacement(root, support, next[root]);
                next[root] += stacks[i].Count;
            }
            int left = int.MaxValue, right = int.MinValue;
            foreach (int i in order) if (result[i].Root == i)
            {
                int centre = stacks[i].X * 2 + stacks[i].W;
                left = System.Math.Min(left, centre); right = System.Math.Max(right, centre);
            }
            for (int i = 0; i < result.Length; i++)
            {
                var root = stacks[result[i].Root];
                result[i].Direction = root.SpreadDirection != 0 ? root.SpreadDirection
                    : ((long)root.X * 4 + root.W * 2 < (long)left + right ? -1 : 1);
            }
            return result;
        }

        private static int FindSupport(List<StackSpec> stacks, int index)
        {
            var me = stacks[index];
            int best = -1;
            long bestArea = -1, bestDistance = long.MaxValue;
            for (int j = 0; j < stacks.Count; j++)
            {
                var candidate = stacks[j];
                if (candidate.Layer >= me.Layer || !candidate.Overlaps(me) || candidate.SpreadAngle != me.SpreadAngle) continue;
                // Optional old author hints remain readable; ordinary levels need no on_stack field.
                if (!string.IsNullOrEmpty(me.OnStack) && candidate.Id == me.OnStack) return j;
                long area = (long)(System.Math.Min(me.X + me.W, candidate.X + candidate.W) - System.Math.Max(me.X, candidate.X))
                    * (System.Math.Min(me.Y + me.H, candidate.Y + candidate.H) - System.Math.Max(me.Y, candidate.Y));
                long dx = (long)me.X * 2 + me.W - ((long)candidate.X * 2 + candidate.W);
                long dy = (long)me.Y * 2 + me.H - ((long)candidate.Y * 2 + candidate.H);
                long distance = dx * dx + dy * dy;
                if (best < 0 || candidate.Layer > stacks[best].Layer
                    || (candidate.Layer == stacks[best].Layer && (area > bestArea
                        || (area == bestArea && (distance < bestDistance
                            || (distance == bestDistance && string.CompareOrdinal(candidate.Id, stacks[best].Id) < 0))))))
                {
                    best = j; bestArea = area; bestDistance = distance;
                }
            }
            return best;
        }
    }

}
