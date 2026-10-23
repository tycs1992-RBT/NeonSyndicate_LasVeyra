using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What one line of a live world placement file asks for.</summary>
    public enum LiveWorldOrderKindH8S
    {
        /// <summary>One copy near a named place: a Realm anchor, a Venue, Cast or Under token, a site key or a POI name.</summary>
        Near,
        /// <summary>Copies beside the stops of the story route through revision 10 (the Hearth Witness route).</summary>
        Route,
        /// <summary>Furnishings standing against a wall inside a venue room.</summary>
        Room,
        /// <summary>The look of an existing procedural prop type (today: forge-anvil).</summary>
        Replace
    }

    /// <summary>The colour family used for pieces that have no textures of their own (stone, bark, leaves and crystals).</summary>
    public enum LiveWorldPaletteH8S
    {
        /// <summary>Chosen from the place the piece stands near.</summary>
        Auto,
        /// <summary>Gothic Heart: charcoal and weathered stone, tarnished bronze, subtle violet mineral accents.</summary>
        Gothic,
        /// <summary>Amethyst Dominion: charcoal stone, deep violet crystal glow.</summary>
        Amethyst,
        /// <summary>Azure Shardwood: wet stone, roots, moss and turquoise mineral clusters.</summary>
        Azure
    }

    /// <summary>One placement line of a piece: where and how one or more copies go.</summary>
    public sealed class LiveWorldOrderH8S
    {
        /// <summary>The kind of placement.</summary>
        public LiveWorldOrderKindH8S Kind;
        /// <summary>Near: the place; Room: the venue id; Replace: the prop type; Route: empty.</summary>
        public string Place = "";
        /// <summary>Preferred offset from the place in metres (x east, y up, z north).</summary>
        public Vector3 Offset;
        /// <summary>Degrees added to the facing, clockwise seen from above.</summary>
        public float Turn;
        /// <summary>Uniform scale, already clamped to the allowed range.</summary>
        public float Scale = 1f;
        /// <summary>How many copies a route or room line asks for.</summary>
        public int Count = 1;
        /// <summary>The colour family for pieces without their own textures.</summary>
        public LiveWorldPaletteH8S Palette;
        /// <summary>Line number in the placement file (1 based), for messages.</summary>
        public int Line;
    }

    /// <summary>A parsed placement file: the model to use, whether the piece is switched off, its placements and the notes about anything skipped.</summary>
    public sealed class LiveWorldSpecH8S
    {
        /// <summary>The piece's name (its folder name).</summary>
        public string Name = "";
        /// <summary>The model named by a model line ("" means the model inside the piece's own folder).</summary>
        public string Model = "";
        /// <summary>True when the file says skip: the piece is kept but placed nowhere.</summary>
        public bool Skip;
        /// <summary>The words after skip, shown in the report.</summary>
        public string SkipReason = "";
        /// <summary>The placements, in file order.</summary>
        public readonly List<LiveWorldOrderH8S> Orders = new List<LiveWorldOrderH8S>();
        /// <summary>Readable notes about lines that were ignored or corrected.</summary>
        public readonly List<string> Notes = new List<string>();
    }

    /// <summary>
    /// Engine-free reader for the tiny placement file (placement.txt) of a live world piece. One instruction per line,
    /// words are not case sensitive, # starts a note, commas and equals signs count as spaces. Lines:
    /// near PLACE [offset X Y Z] [turn DEGREES] [scale S] [palette gothic|amethyst|azure];
    /// route [COUNT] [scale S] [palette ...]; room VENUE [COUNT] [turn DEGREES]; replace forge-anvil; model PATH; skip [why].
    /// It never throws: anything it cannot use becomes a note and the rest of the file still counts.
    /// </summary>
    public static class LiveWorldPlanH8S
    {
        /// <summary>The placement file's name inside a piece folder.</summary>
        public const string FileName = "placement.txt";
        /// <summary>At most this many placement lines are read per piece.</summary>
        public const int MaxOrders = 16;
        /// <summary>The smallest allowed scale.</summary>
        public const float MinScale = 0.25f;
        /// <summary>The largest allowed scale.</summary>
        public const float MaxScale = 4f;
        /// <summary>Route lines place at most this many copies (the route preview's own limit).</summary>
        public const int MaxRouteCount = WorldRouteH8S.MaxDressing;
        /// <summary>Room lines place at most this many copies.</summary>
        public const int MaxRoomCount = 4;
        /// <summary>A requested offset is kept within this many metres of its place on the plan.</summary>
        public const float MaxOffset = 80f;
        /// <summary>The prop type a replace line can give a new look today.</summary>
        public const string ForgeAnvil = "forge-anvil";

        /// <summary>Reads a placement file. name is the piece's folder name; text may be null or empty.</summary>
        public static LiveWorldSpecH8S Parse(string name, string text)
        {
            LiveWorldSpecH8S spec = new LiveWorldSpecH8S { Name = name ?? "" };
            if (string.IsNullOrEmpty(text))
            {
                spec.Notes.Add("the placement file is empty: add a near, route, room or replace line");
                return spec;
            }
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int ignoredOrders = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripNote(lines[i]).Trim();
                if (line.Length == 0) continue;
                List<string> words = Words(line);
                if (words.Count == 0) continue;
                string verb = words[0].ToLowerInvariant();
                int number = i + 1;
                switch (verb)
                {
                    case "model":
                        ReadModel(spec, line, number);
                        break;
                    case "skip":
                    case "off":
                        spec.Skip = true;
                        spec.SkipReason = words.Count > 1 ? line.Substring(line.IndexOf(words[0], StringComparison.Ordinal) + words[0].Length).Trim() : "";
                        break;
                    case "near":
                    case "at":
                    case "beside":
                    case "route":
                    case "along":
                    case "room":
                    case "inside":
                    case "replace":
                        if (spec.Orders.Count >= MaxOrders)
                        {
                            ignoredOrders++;
                            break;
                        }
                        LiveWorldOrderH8S order = ReadOrder(verb, words, number, spec.Notes);
                        if (order != null) spec.Orders.Add(order);
                        break;
                    default:
                        spec.Notes.Add("line " + number + ": \"" + words[0] + "\" is not an instruction; start the line with near, route, room, replace, model or skip");
                        break;
                }
            }
            if (ignoredOrders > 0) spec.Notes.Add(ignoredOrders + " placement line(s) after the first " + MaxOrders + " were ignored");
            if (!spec.Skip && spec.Orders.Count == 0) spec.Notes.Add("nothing to place: add a near, route, room or replace line");
            return spec;
        }

        /// <summary>The line without its note: everything from a # that is not inside quotes (and a leading //) is removed.</summary>
        public static string StripNote(string line)
        {
            if (string.IsNullOrEmpty(line)) return "";
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal)) return "";
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') quoted = !quoted;
                else if (c == '#' && !quoted) return line.Substring(0, i);
            }
            return line;
        }

        /// <summary>Splits a line into words on spaces, tabs, commas, semicolons and equals signs; text in double quotes stays one word.</summary>
        public static List<string> Words(string line)
        {
            List<string> words = new List<string>();
            if (string.IsNullOrEmpty(line)) return words;
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    quoted = !quoted;
                    continue;
                }
                bool separator = !quoted && (char.IsWhiteSpace(c) || c == ',' || c == ';' || c == '=');
                if (separator)
                {
                    if (current.Length > 0) words.Add(current.ToString());
                    current.Length = 0;
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0) words.Add(current.ToString());
            return words;
        }

        /// <summary>Reads a number written with a dot for decimals; a trailing unit such as m, deg or a percent sign is allowed.</summary>
        public static bool TryNumber(string word, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(word)) return false;
            string w = word.Trim();
            int end = w.Length;
            while (end > 0 && (char.IsLetter(w[end - 1]) || w[end - 1] == '%' || w[end - 1] == '°')) end--;
            if (end == 0) return false;
            if (!float.TryParse(w.Substring(0, end), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return false;
            if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            if (w.EndsWith("%", StringComparison.Ordinal)) value *= 0.01f;
            return true;
        }

        /// <summary>The palette named by a word, or false when the word is not one.</summary>
        public static bool TryPalette(string word, out LiveWorldPaletteH8S palette)
        {
            palette = LiveWorldPaletteH8S.Auto;
            switch ((word ?? "").ToLowerInvariant())
            {
                case "auto": palette = LiveWorldPaletteH8S.Auto; return true;
                case "gothic": case "heart": case "home": case "forge": palette = LiveWorldPaletteH8S.Gothic; return true;
                case "amethyst": case "violet": case "purple": palette = LiveWorldPaletteH8S.Amethyst; return true;
                case "azure": case "turquoise": case "teal": case "shardwood": palette = LiveWorldPaletteH8S.Azure; return true;
                default: return false;
            }
        }

        /// <summary>
        /// Null when a near place can be used, otherwise why not. Realm anchors must be known, 04E tokens (Venue:, Cast:,
        /// Under:, Arsenal:) must be valid; any other word is taken as a site key or POI name and checked in the live world.
        /// Places that move or are random (Player, Giver, Previous, World, District:) are refused.
        /// </summary>
        public static string PlaceProblem(string place)
        {
            if (string.IsNullOrEmpty(place)) return "no place was given after near";
            if (place.StartsWith("Realm:", StringComparison.Ordinal))
                return QuestPlaces.IsKnownPlace(null, place) ? null : "unknown Realm anchor \"" + place + "\"";
            if (CampaignRules04E.IsPlaceToken(place))
            {
                string why = CampaignRules04E.PlaceTokenError(place);
                return why == null ? null : why;
            }
            if (place == "Player" || place == "Giver" || place == "Previous" || place == "World" || place.StartsWith(QuestPlaces.DistrictPrefix, StringComparison.Ordinal))
                return "\"" + place + "\" moves or is chosen at random, so it cannot hold a piece; use a Realm, Venue, Cast or named place";
            return null;
        }

        private static void ReadModel(LiveWorldSpecH8S spec, string line, int number)
        {
            int at = line.IndexOf("model", StringComparison.OrdinalIgnoreCase);
            string path = at >= 0 ? line.Substring(at + 5).Trim().Trim('"').Trim() : "";
            if (path.StartsWith("=", StringComparison.Ordinal)) path = path.Substring(1).Trim().Trim('"').Trim();
            path = path.Replace('\\', '/');
            if (path.Length == 0)
            {
                spec.Notes.Add("line " + number + ": model needs a path, for example model Assets/NeonSyndicate/Art/LiveWorld/MyTower/MyTower.fbx");
                return;
            }
            if (spec.Model.Length > 0) spec.Notes.Add("line " + number + ": a second model line replaces the first");
            spec.Model = path;
        }

        private static LiveWorldOrderH8S ReadOrder(string verb, List<string> words, int number, List<string> notes)
        {
            LiveWorldOrderH8S order = new LiveWorldOrderH8S { Line = number };
            int i = 1;
            switch (verb)
            {
                case "near":
                case "at":
                case "beside":
                {
                    order.Kind = LiveWorldOrderKindH8S.Near;
                    StringBuilder place = new StringBuilder();
                    while (i < words.Count && !IsOption(words[i]))
                    {
                        if (place.Length > 0) place.Append(' ');
                        place.Append(words[i]);
                        i++;
                    }
                    order.Place = place.ToString();
                    string problem = PlaceProblem(order.Place);
                    if (problem != null)
                    {
                        notes.Add("line " + number + ": " + problem + "; the line was skipped");
                        return null;
                    }
                    break;
                }
                case "route":
                case "along":
                    order.Kind = LiveWorldOrderKindH8S.Route;
                    order.Count = 4;
                    break;
                case "room":
                case "inside":
                {
                    order.Kind = LiveWorldOrderKindH8S.Room;
                    if (i >= words.Count || IsOption(words[i]))
                    {
                        notes.Add("line " + number + ": room needs a venue, for example room forge; the line was skipped");
                        return null;
                    }
                    order.Place = words[i].ToLowerInvariant();
                    i++;
                    if (!CampaignRules04E.KnownVenue(order.Place))
                    {
                        notes.Add("line " + number + ": \"" + order.Place + "\" is not a venue (for example forge, lofts, archive, clinic, inn, office); the line was skipped");
                        return null;
                    }
                    break;
                }
                default:
                {
                    order.Kind = LiveWorldOrderKindH8S.Replace;
                    string type = i < words.Count ? words[i].ToLowerInvariant() : "";
                    i++;
                    if (type != ForgeAnvil)
                    {
                        notes.Add("line " + number + ": " + (type.Length == 0 ? "replace needs a prop type" : "\"" + type + "\" is not a prop type the game can replace") +
                            "; today you can replace " + ForgeAnvil + "; the line was skipped");
                        return null;
                    }
                    order.Place = type;
                    break;
                }
            }
            ReadOptions(order, words, i, number, notes);
            return order;
        }

        private static bool IsOption(string word)
        {
            switch ((word ?? "").ToLowerInvariant())
            {
                case "offset": case "move": case "turn": case "rotate": case "yaw": case "scale": case "size": case "count": case "copies":
                case "palette": case "look": case "colour": case "color": case "crystal":
                    return true;
                default:
                    return false;
            }
        }

        private static void ReadOptions(LiveWorldOrderH8S order, List<string> words, int i, int number, List<string> notes)
        {
            while (i < words.Count)
            {
                string word = words[i].ToLowerInvariant();
                float n;
                switch (word)
                {
                    case "offset":
                    case "move":
                    {
                        List<float> values = new List<float>();
                        int j = i + 1;
                        while (j < words.Count && values.Count < 3 && TryNumber(words[j], out n)) { values.Add(n); j++; }
                        if (values.Count == 3) order.Offset = new Vector3(values[0], values[1], values[2]);
                        else if (values.Count == 2) order.Offset = new Vector3(values[0], 0f, values[1]);
                        else notes.Add("line " + number + ": offset needs two numbers (east north) or three (east up north)");
                        i = j;
                        continue;
                    }
                    case "turn":
                    case "rotate":
                    case "yaw":
                        if (i + 1 < words.Count && TryNumber(words[i + 1], out n))
                        {
                            order.Turn = Mathf.Repeat(n, 360f);
                            i += 2;
                        }
                        else
                        {
                            notes.Add("line " + number + ": turn needs a number of degrees");
                            i++;
                        }
                        continue;
                    case "scale":
                    case "size":
                        if (i + 1 < words.Count && TryNumber(words[i + 1], out n) && n > 0f)
                        {
                            float clamped = Mathf.Clamp(n, MinScale, MaxScale);
                            if (!Mathf.Approximately(clamped, n)) notes.Add("line " + number + ": scale " + n.ToString("0.###", CultureInfo.InvariantCulture) + " was limited to " + clamped.ToString("0.##", CultureInfo.InvariantCulture));
                            order.Scale = clamped;
                            i += 2;
                        }
                        else
                        {
                            notes.Add("line " + number + ": scale needs a positive number, for example scale 1.5");
                            i++;
                        }
                        continue;
                    case "count":
                    case "copies":
                        if (i + 1 < words.Count && TryNumber(words[i + 1], out n))
                        {
                            SetCount(order, n, number, notes);
                            i += 2;
                        }
                        else
                        {
                            notes.Add("line " + number + ": count needs a number");
                            i++;
                        }
                        continue;
                    case "palette":
                    case "look":
                    case "colour":
                    case "color":
                    case "crystal":
                    {
                        LiveWorldPaletteH8S palette;
                        if (i + 1 < words.Count && TryPalette(words[i + 1], out palette))
                        {
                            order.Palette = palette;
                            i += 2;
                        }
                        else
                        {
                            notes.Add("line " + number + ": palette needs gothic, amethyst or azure");
                            i++;
                        }
                        continue;
                    }
                }
                // A bare number right after route or room is its count.
                if ((order.Kind == LiveWorldOrderKindH8S.Route || order.Kind == LiveWorldOrderKindH8S.Room) && TryNumber(words[i], out n))
                {
                    SetCount(order, n, number, notes);
                    i++;
                    continue;
                }
                notes.Add("line " + number + ": \"" + words[i] + "\" was not understood and was ignored");
                i++;
            }
            if (order.Kind == LiveWorldOrderKindH8S.Near)
            {
                Vector2 plan = new Vector2(order.Offset.x, order.Offset.z);
                if (plan.magnitude > MaxOffset)
                {
                    plan = plan.normalized * MaxOffset;
                    order.Offset = new Vector3(plan.x, order.Offset.y, plan.y);
                    notes.Add("line " + number + ": the offset was kept within " + MaxOffset.ToString("0", CultureInfo.InvariantCulture) + " m of the place");
                }
                order.Offset.y = Mathf.Clamp(order.Offset.y, -2f, 2f);
            }
            if (order.Kind == LiveWorldOrderKindH8S.Replace || order.Kind == LiveWorldOrderKindH8S.Room)
            {
                if (order.Kind == LiveWorldOrderKindH8S.Replace && !Mathf.Approximately(order.Scale, 1f))
                {
                    notes.Add("line " + number + ": a replaced prop is fitted inside the original's collider, so scale was ignored");
                    order.Scale = 1f;
                }
            }
        }

        private static void SetCount(LiveWorldOrderH8S order, float value, int number, List<string> notes)
        {
            int max = order.Kind == LiveWorldOrderKindH8S.Route ? MaxRouteCount : order.Kind == LiveWorldOrderKindH8S.Room ? MaxRoomCount : 1;
            int count = Mathf.Clamp(Mathf.RoundToInt(value), 1, max);
            if (count != Mathf.RoundToInt(value)) notes.Add("line " + number + ": count was limited to " + count);
            order.Count = count;
        }
    }
}
