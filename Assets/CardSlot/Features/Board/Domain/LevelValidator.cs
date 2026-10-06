using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>
    /// Rejects malformed level data before a board is built from it: R-1 (cards per colour equal target
    /// capacity per colour), R-1b (no two stacks on the same layer overlap) and the valid ranges in
    /// <c>level-design.md</c> §2. Returns the problems as data; an empty list means valid.
    /// </summary>
    public static class LevelValidator
    {
        public static List<string> Validate(LevelData level)
        {
            var errors = new List<string>();
            if (level == null) { errors.Add("level is null"); return errors; }
            if (string.IsNullOrEmpty(level.Id)) errors.Add("id is empty");
            Range(errors, "slots", level.Slots, 2, 4);
            Range(errors, "buffer_capacity", level.BufferCapacity, 6, 20);
            if (level.Targets == null || level.Targets.Count == 0) errors.Add("no targets");
            if (level.Stacks == null || level.Stacks.Count == 0) errors.Add("no stacks");
            if (errors.Count > 0 && (level.Targets == null || level.Stacks == null)) return errors;
            Range(errors, "stack count", level.Stacks.Count, 2, 24);

            var cardsPerColor = new int[LevelData.ColorCount];
            var targetPerColor = new int[LevelData.ColorCount];
            var ids = new HashSet<string>();
            foreach (var t in level.Targets)
            {
                if (t.Color < 0 || t.Color >= LevelData.ColorCount) { errors.Add($"target colour {t.Color} out of range"); continue; }
                Range(errors, "target capacity", t.Capacity, 2, 6);
                targetPerColor[t.Color] += t.Capacity;
            }
            foreach (var s in level.Stacks)
            {
                if (string.IsNullOrEmpty(s.Id) || !ids.Add(s.Id)) errors.Add($"stack id '{s.Id}' empty or duplicated");
                if (s.Cards == null || s.Cards.Length == 0) { errors.Add($"stack {s.Id} has no cards"); continue; }
                Range(errors, $"stack {s.Id} cards", s.Cards.Length, 1, 8);
                Range(errors, $"stack {s.Id} layer", s.Layer, 0, 4);
                if (s.W <= 0 || s.H <= 0) errors.Add($"stack {s.Id} has an empty rectangle");
                foreach (var c in s.Cards)
                {
                    if (c < 0 || c >= LevelData.ColorCount) errors.Add($"stack {s.Id} card colour {c} out of range");
                    else cardsPerColor[c]++;
                }
            }
            int colours = 0;
            for (int c = 0; c < LevelData.ColorCount; c++)
            {
                if (cardsPerColor[c] != targetPerColor[c])
                    errors.Add($"R-1: colour {c} has {cardsPerColor[c]} cards but targets take {targetPerColor[c]}");
                if (targetPerColor[c] > 0) colours++;
            }
            Range(errors, "colour count", colours, 2, 6);
            for (int i = 0; i < level.Stacks.Count; i++)
                for (int j = i + 1; j < level.Stacks.Count; j++)
                {
                    var a = level.Stacks[i]; var b = level.Stacks[j];
                    if (a.Layer == b.Layer && a.Overlaps(b))
                        errors.Add($"R-1b: stacks {a.Id} and {b.Id} overlap on layer {a.Layer}");
                }
            return errors;
        }

        private static void Range(List<string> errors, string what, int value, int min, int max)
        {
            if (value < min || value > max) errors.Add($"{what} = {value}, expected {min}..{max}");
        }
    }
}
