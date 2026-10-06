using System;
using System.Collections.Generic;
using Game.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Infrastructure
{
    /// <summary>
    /// Reads and writes the level file format of <c>docs/design/level-design.md</c> §2 (snake_case keys,
    /// colours as glossary ids <c>"color_0"</c>…). Engine-free apart from Newtonsoft, so the headless tests
    /// and the level tool compile this exact file (named <c>Compile Include</c>, not a glob). Malformed
    /// data comes back as an error message, never an exception.
    /// </summary>
    public static class LevelJson
    {
        public static bool TryRead(string json, out LevelData level, out string error)
        {
            level = null; error = null;
            try
            {
                var o = JObject.Parse(json);
                var l = new LevelData
                {
                    Id = (string)o["id"],
                    Revision = (int?)o["revision"] ?? 1,
                    Seed = (long?)o["seed"] ?? 0,
                    Hard = (bool?)o["hard"] ?? false,
                    Slots = (int?)o["n_slots"] ?? 3,
                    BufferCapacity = (int?)o["buffer_capacity"] ?? 12,
                    Ftue = (string)o["ftue"],
                };
                foreach (var t in (JArray)o["targets"] ?? new JArray())
                    l.Targets.Add(new TargetSpec(Color((string)t["color"]), (int)t["capacity"]));
                foreach (var s in (JArray)o["stacks"] ?? new JArray())
                {
                    var cards = new List<int>();
                    foreach (var c in (JArray)s["cards"]) cards.Add(Color((string)c));
                    l.Stacks.Add(new StackSpec((string)s["id"], (int)s["x"], (int)s["y"], (int)s["w"], (int)s["h"], (int)s["layer"], cards.ToArray()));
                }
                var problems = LevelValidator.Validate(l);
                if (problems.Count > 0) { error = string.Join("; ", problems); return false; }
                level = l;
                return true;
            }
            catch (Exception e) when (e is JsonException || e is FormatException || e is InvalidCastException || e is ArgumentException || e is NullReferenceException)
            {
                error = e.Message;
                return false;
            }
        }

        /// <param name="solution">Optional verified solution (stack ids) stored beside the level so tests can replay it.</param>
        public static string Write(LevelData l, IReadOnlyList<string> solution = null)
        {
            var o = new JObject
            {
                ["id"] = l.Id,
                ["revision"] = l.Revision,
                ["seed"] = l.Seed,
                ["hard"] = l.Hard,
                ["n_slots"] = l.Slots,
                ["buffer_capacity"] = l.BufferCapacity,
            };
            var targets = new JArray();
            foreach (var t in l.Targets) targets.Add(new JObject { ["color"] = ColorId(t.Color), ["capacity"] = t.Capacity });
            o["targets"] = targets;
            var stacks = new JArray();
            foreach (var s in l.Stacks)
            {
                var cards = new JArray();
                foreach (var c in s.Cards) cards.Add(ColorId(c));
                var so = new JObject { ["id"] = s.Id, ["x"] = s.X, ["y"] = s.Y, ["w"] = s.W, ["h"] = s.H, ["layer"] = s.Layer };
                so["cards"] = cards;
                stacks.Add(so);
            }
            o["stacks"] = stacks;
            o["ftue"] = l.Ftue;
            if (solution != null) o["solution"] = new JArray(solution);
            return o.ToString(Formatting.Indented);
        }

        /// <summary>The verified solution stored in a level file, or an empty list.</summary>
        public static List<string> ReadSolution(string json)
        {
            var list = new List<string>();
            var arr = JObject.Parse(json)["solution"] as JArray;
            if (arr != null) foreach (var s in arr) list.Add((string)s);
            return list;
        }

        public static string ColorId(int color) => "color_" + color;

        private static int Color(string id)
        {
            if (id == null || !id.StartsWith("color_", StringComparison.Ordinal) || !int.TryParse(id.Substring(6), out var c))
                throw new FormatException($"'{id}' is not a colour id (expected color_0..color_5)");
            return c;
        }
    }
}
