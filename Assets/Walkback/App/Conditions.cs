// Written with Claude Code (AI), directed and tested by team MARSXR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// The CURRENT CONDITIONS card (ficha C): reads Site/conditions.json from session B
    /// (contract §22.7: rems, donki, season, daylight, radiation, each with its date).
    /// Field names inside each block are not fixed yet, so it matches them loosely.
    /// No file, or a file it cannot read: it says so and shows no numbers.
    /// </summary>
    public static class Conditions
    {
        public const string FileName = "conditions.json";
        public const string WaitingText = "Conditions: waiting for data.";

        public class View
        {
            public bool HasData;
            public string Text;
        }

        public static View Load()
        {
            byte[] bytes = null;
            try { bytes = SiteLoader.ReadStreamingAsset(SiteLoader.SiteFolder + "/" + FileName); }
            catch (Exception e) { WalkbackLog.Warn("conditions.json could not be read: " + e.Message); }
            if (bytes == null) return new View { HasData = false, Text = WaitingText };
            return Describe(Encoding.UTF8.GetString(bytes));
        }

        /// <summary>The card text from the JSON. Public so the test can feed it hand-made files.</summary>
        public static View Describe(string json)
        {
            Dictionary<string, object> root;
            try { root = MiniJson.Parse(json) as Dictionary<string, object>; }
            catch (Exception e)
            {
                WalkbackLog.Warn("conditions.json is not valid JSON: " + e.Message);
                return new View { HasData = false, Text = "Conditions: conditions.json could not be read." };
            }
            if (root == null) return new View { HasData = false, Text = "Conditions: conditions.json could not be read." };

            var lines = new List<string>();
            object rems = Field(root, "rems", "weather", "rem");
            if (rems is Dictionary<string, object>) lines.AddRange(RemsLines(Flatten((Dictionary<string, object>)rems)));
            foreach (var key in new[] { "season", "daylight", "radiation" })
            {
                object value = Field(root, key);
                string text = Describe(value);
                if (!string.IsNullOrEmpty(text)) lines.Add(char.ToUpperInvariant(key[0]) + key.Substring(1) + ": " + text);
            }
            object donki = Field(root, "donki", "spaceweather", "space_weather");
            if (donki is Dictionary<string, object>)
            {
                string line = DonkiLine((Dictionary<string, object>)donki);
                if (!string.IsNullOrEmpty(line)) lines.Add(line);
            }

            if (lines.Count == 0) return new View { HasData = false, Text = WaitingText };
            return new View { HasData = true, Text = string.Join("\n", lines.ToArray()) };
        }

        static IEnumerable<string> RemsLines(Dictionary<string, object> flat)
        {
            var lines = new List<string>();
            string sol = Pick(flat, k => k == "sol" || k.EndsWith("sol") && !k.Contains("solar"));
            string date = Pick(flat, k => k.Contains("earth") || k.Contains("terrestrial") || k.Contains("fecha") || k == "date" || k.EndsWith("utc"));
            string source = Pick(flat, k => k.Contains("source") || k.Contains("fuente") || k.Contains("credit"));
            string head = "Latest REMS report";
            if (sol != null) head += ": sol " + sol;
            if (date != null) head += (sol != null ? " (Earth date " + date + ")" : ": Earth date " + date);
            if (source != null) head += " · " + source;
            if (sol != null || date != null) lines.Add(head);

            double? airMin = Number(flat, k => (k.Contains("air") || k.Contains("aire")) && k.Contains("min"));
            double? airMax = Number(flat, k => (k.Contains("air") || k.Contains("aire")) && k.Contains("max"));
            double? groundMin = Number(flat, k => (k.Contains("ground") || k.Contains("soil") || k.Contains("suelo")) && k.Contains("min"));
            double? groundMax = Number(flat, k => (k.Contains("ground") || k.Contains("soil") || k.Contains("suelo")) && k.Contains("max"));
            double? pressure = Number(flat, k => k.Contains("pressure") || k.Contains("presion") || k.Contains("presión"));

            // REMS publishes °C and Pa. Values above 100 for every temperature can only be kelvin; under 50 for pressure, hPa.
            bool kelvin = true;
            foreach (var t in new[] { airMin, airMax, groundMin, groundMax }) if (t.HasValue && t.Value < 100) kelvin = false;
            Func<double?, double?> c = t => t.HasValue && kelvin ? t.Value - 273.15 : t;
            var parts = new List<string>();
            string air = Range(c(airMin), c(airMax));
            if (air != null) parts.Add("Air " + air + " °C");
            string ground = Range(c(groundMin), c(groundMax));
            if (ground != null) parts.Add("Ground " + ground + " °C");
            if (pressure.HasValue) parts.Add("Pressure " + (pressure.Value < 50 ? pressure.Value * 100 : pressure.Value).ToString("0", CultureInfo.InvariantCulture) + " Pa");
            if (parts.Count > 0) lines.Add(string.Join(" · ", parts.ToArray()));
            return lines;
        }

        static string DonkiLine(Dictionary<string, object> donki)
        {
            var flat = Flatten(donki);
            string from = Pick(flat, k => k.Contains("from") || k.Contains("start") || k.Contains("desde"));
            string to = Pick(flat, k => k == "to" || k.EndsWith(".to") || k.Contains("end") || k.Contains("until") || k.Contains("hasta"));
            string checkedOn = Pick(flat, k => k.Contains("checked") || k.Contains("consult") || k.Contains("queried") || k.Contains("retrieved") || k.Contains("accessed"));
            object events = Field(donki, "events", "eventos");
            string count = null;
            var types = new List<string>();
            if (events is List<object>)
            {
                var list = (List<object>)events;
                count = list.Count + (list.Count == 1 ? " event" : " events");
                foreach (var e in list)
                {
                    var d = e as Dictionary<string, object>;
                    if (d == null) continue;
                    foreach (var pair in d)
                    {
                        if (!Normalize(pair.Key).Contains("type") || !(pair.Value is string)) continue;
                        if (!types.Contains((string)pair.Value)) types.Add((string)pair.Value);
                        break;
                    }
                }
            }
            else if (events is double)
            {
                double n = (double)events;
                count = n.ToString("0", CultureInfo.InvariantCulture) + (n == 1 ? " event" : " events");
            }
            else if (events is string) count = (string)events;
            if (count == null && from == null) return null;

            string line = "Space weather (NASA DONKI";
            if (from != null && to != null) line += ", " + from + " to " + to;
            line += "): " + (count ?? "no events listed");
            if (types.Count > 0) line += " (" + string.Join(", ", types.ToArray()) + ")";
            if (checkedOn != null) line += " · checked " + checkedOn;
            return line;
        }

        static string Range(double? min, double? max)
        {
            if (min.HasValue && max.HasValue) return Fmt(min.Value) + " / " + Fmt(max.Value);
            if (min.HasValue) return "min " + Fmt(min.Value);
            if (max.HasValue) return "max " + Fmt(max.Value);
            return null;
        }

        static string Fmt(double v)
        {
            return Math.Round(v).ToString("0", CultureInfo.InvariantCulture);
        }

        /// <summary>A string, a number, or an object such as {value, date, source}, as one short text.</summary>
        static string Describe(object value)
        {
            if (value == null) return null;
            if (value is string) return (string)value;
            if (value is double) return ((double)value).ToString("0.###", CultureInfo.InvariantCulture);
            if (value is bool) return (bool)value ? "yes" : "no";
            var d = value as Dictionary<string, object>;
            if (d == null) return null;
            var flat = Flatten(d);
            string main = Pick(flat, k => k == "value" || k == "text" || k == "description" || k == "label" || k == "summary" || k == "valor");
            if (main == null)
            {
                // No obvious main field: join every short value except dates and sources.
                var parts = new List<string>();
                foreach (var pair in flat)
                {
                    if (pair.Key.Contains("date") || pair.Key.Contains("source") || pair.Key.Contains("fecha") || pair.Key.Contains("fuente")) continue;
                    string s = Describe(pair.Value);
                    if (!string.IsNullOrEmpty(s)) parts.Add(s);
                }
                main = parts.Count > 0 ? string.Join(" · ", parts.ToArray()) : null;
            }
            if (main == null) return null;
            string unit = Pick(flat, k => k == "unit" || k == "units");
            if (unit != null && !main.Contains(unit)) main += " " + unit;
            string date = Pick(flat, k => k.Contains("date") || k.Contains("fecha") || k.Contains("asof"));
            string source = Pick(flat, k => k.Contains("source") || k.Contains("fuente") || k.Contains("credit"));
            if (date != null || source != null)
                main += " (" + (date ?? "") + (date != null && source != null ? ", " : "") + (source ?? "") + ")";
            return main;
        }

        static object Field(Dictionary<string, object> d, params string[] names)
        {
            foreach (var pair in d)
            {
                string k = Normalize(pair.Key);
                foreach (var n in names) if (k == Normalize(n)) return pair.Value;
            }
            return null;
        }

        /// <summary>Nested keys as "air.min"-style paths, lower case, without spaces, dashes or underscores.</summary>
        static Dictionary<string, object> Flatten(Dictionary<string, object> d)
        {
            var flat = new Dictionary<string, object>();
            FlattenInto(flat, "", d);
            return flat;
        }

        static void FlattenInto(Dictionary<string, object> flat, string prefix, Dictionary<string, object> d)
        {
            foreach (var pair in d)
            {
                string key = (prefix.Length > 0 ? prefix + "." : "") + Normalize(pair.Key);
                if (pair.Value is Dictionary<string, object>) FlattenInto(flat, key, (Dictionary<string, object>)pair.Value);
                else if (!flat.ContainsKey(key)) flat.Add(key, pair.Value);
            }
        }

        static string Normalize(string key)
        {
            var sb = new StringBuilder();
            foreach (char ch in key.ToLowerInvariant()) if (ch != '_' && ch != '-' && ch != ' ') sb.Append(ch);
            return sb.ToString();
        }

        static string Pick(Dictionary<string, object> flat, Func<string, bool> test)
        {
            foreach (var pair in flat)
            {
                if (!test(pair.Key) || pair.Value == null || pair.Value is List<object>) continue;
                string s = Describe(pair.Value);
                if (!string.IsNullOrEmpty(s)) return s;
            }
            return null;
        }

        static double? Number(Dictionary<string, object> flat, Func<string, bool> test)
        {
            foreach (var pair in flat)
            {
                if (!test(pair.Key)) continue;
                if (pair.Value is double) return (double)pair.Value;
                double parsed;
                if (pair.Value is string && double.TryParse(((string)pair.Value).Replace("°C", "").Replace("Pa", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) return parsed;
            }
            return null;
        }
    }

    /// <summary>
    /// A small JSON reader for files whose field names are not fixed: objects become
    /// Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;, numbers double.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            int i = 0;
            object value = ReadValue(json, ref i);
            SkipSpace(json, ref i);
            if (i < json.Length) throw new FormatException("extra text after the JSON value at " + i);
            return value;
        }

        static object ReadValue(string s, ref int i)
        {
            SkipSpace(s, ref i);
            if (i >= s.Length) throw new FormatException("unexpected end of JSON");
            char c = s[i];
            if (c == '{') return ReadObject(s, ref i);
            if (c == '[') return ReadArray(s, ref i);
            if (c == '"') return ReadString(s, ref i);
            if (c == 't' && Match(s, ref i, "true")) return true;
            if (c == 'f' && Match(s, ref i, "false")) return false;
            if (c == 'n' && Match(s, ref i, "null")) return null;
            return ReadNumber(s, ref i);
        }

        static Dictionary<string, object> ReadObject(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++;
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (true)
            {
                SkipSpace(s, ref i);
                string key = ReadString(s, ref i);
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException("expected ':' at " + i);
                i++;
                d[key] = ReadValue(s, ref i);
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return d; }
                throw new FormatException("expected ',' or '}' at " + i);
            }
        }

        static List<object> ReadArray(string s, ref int i)
        {
            var list = new List<object>();
            i++;
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }
            while (true)
            {
                list.Add(ReadValue(s, ref i));
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return list; }
                throw new FormatException("expected ',' or ']' at " + i);
            }
        }

        static string ReadString(string s, ref int i)
        {
            if (i >= s.Length || s[i] != '"') throw new FormatException("expected a string at " + i);
            i++;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("bad \\u escape");
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: sb.Append(e); break;
                }
            }
            throw new FormatException("unterminated string");
        }

        static double ReadNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (i == start) throw new FormatException("unexpected character '" + s[i] + "' at " + i);
            return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static bool Match(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw new FormatException("unexpected token at " + i);
            i += word.Length;
            return true;
        }

        static void SkipSpace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }
    }
}
