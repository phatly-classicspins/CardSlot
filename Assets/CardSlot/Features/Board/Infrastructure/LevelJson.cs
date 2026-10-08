using System;
using System.Collections.Generic;
using Game.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Infrastructure
{
    /// <summary>
    /// Reads and writes the level file format of <c>docs/design/level-design.md</c> §2 (snake_case keys,
    /// a stack is one colour and a count (R-1c, level format v3), colours as glossary ids <c>"color_0"</c>…). Engine-free apart from Newtonsoft, so the headless tests
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
                    Slots = (int?)o["n_slots"] ?? 3,
                    BufferCapacity = (int?)o["buffer_capacity"] ?? 12,
                    Ftue = (string)o["ftue"],
                };
                foreach (var t in (JArray)o["targets"] ?? new JArray())
                    l.Targets.Add(new TargetSpec(Color((string)t["color"]), (int)t["capacity"]));
                foreach (var s in (JArray)o["stacks"] ?? new JArray())
                    l.Stacks.Add(new StackSpec((string)s["id"], (int)s["x"], (int)s["y"], (int)s["w"], (int)s["h"], (int)s["layer"],
                        Color((string)s["color"]), (int)s["count"]) { Fan = (bool?)s["fan"] ?? false, Spread = (bool?)s["spread"] ?? false, OnStack = (string)s["on_stack"], SpreadDirection = (int?)s["spread_direction"] ?? 0, SpreadAngle = (int?)s["spread_angle"] ?? 0 });
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
                ["n_slots"] = l.Slots,
                ["buffer_capacity"] = l.BufferCapacity,
            };
            var targets = new JArray();
            foreach (var t in l.Targets) targets.Add(new JObject { ["color"] = ColorId(t.Color), ["capacity"] = t.Capacity });
            o["targets"] = targets;
            var stacks = new JArray();
            foreach (var s in l.Stacks)
                stacks.Add(new JObject
                {
                    ["id"] = s.Id, ["x"] = s.X, ["y"] = s.Y, ["w"] = s.W, ["h"] = s.H, ["layer"] = s.Layer,
                    ["color"] = ColorId(s.Color), ["count"] = s.Count, ["fan"] = s.Fan, ["spread"] = s.Spread, ["on_stack"] = s.OnStack, ["spread_direction"] = s.SpreadDirection, ["spread_angle"] = s.SpreadAngle,
                });
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
                throw new FormatException($"'{id}' is not a colour id (expected color_0..color_7)");
            return c;
        }
    }
}
