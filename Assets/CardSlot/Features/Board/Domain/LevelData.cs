using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>One target in a level's queue: a card colour (glossary <c>color_0..5</c> → 0..5) and how
    /// many cards fill it (GDD §2.1).</summary>
    public sealed class TargetSpec
    {
        public int Color;
        public int Capacity;

        public TargetSpec() { }
        public TargetSpec(int color, int capacity) { Color = color; Capacity = capacity; }
    }

    /// <summary>One stack on the board. <see cref="Cards"/>[0] is the top card. The rectangle is in world
    /// units inside the tray and is what the covering rule (R-3) and R-1b test.</summary>
    public sealed class StackSpec
    {
        public string Id;
        public int X, Y, W, H, Layer;
        public int[] Cards;

        public StackSpec() { }
        public StackSpec(string id, int x, int y, int w, int h, int layer, params int[] cards)
        {
            Id = id; X = x; Y = y; W = w; H = h; Layer = layer; Cards = cards;
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
        /// <summary>CR-008: the most cards one tap takes from the top run (0 = the whole run). Shipped levels: 6 —
        /// a run of 12 same-colour cards needs two taps.</summary>
        public int MaxRun;

        public const int ColorCount = 6;
    }
}
