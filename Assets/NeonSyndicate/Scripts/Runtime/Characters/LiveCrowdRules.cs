using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What an imported material of a crowd body stands for (LiveCrowdRules.Surface).</summary>
    public enum LiveCrowdSurface
    {
        /// <summary>The skin and underlayer atlas of the study.</summary>
        Skin = 0,
        /// <summary>The charcoal wool coat and walking panels (shared cloth maps).</summary>
        Wool = 1,
        /// <summary>Collar, lapels, forearm fittings, trousers and boots (shared leather maps).</summary>
        Leather = 2,
        /// <summary>Coat fasteners and the bronze armour edges.</summary>
        Bronze = 3,
        /// <summary>Chest plates and shoulder armour.</summary>
        Steel = 4,
        /// <summary>Eyebrows (the study carries no hair mesh).</summary>
        Brows = 5,
        /// <summary>Eyes.</summary>
        Eyes = 6,
        /// <summary>A material the crowd pipeline does not know (the row is refused).</summary>
        Unknown = 7,
        /// <summary>The live hair's base cap and strand shells (LiveCastVisual.Hair; never a study material).</summary>
        Hair = 8,
        /// <summary>The live hair's tip strands (a gang member's dyed accent).</summary>
        HairTips = 9,
        /// <summary>The live beard and the shaved stubble of an undercut or a crest.</summary>
        Beard = 10,
        /// <summary>The live headwear's cloth or leather.</summary>
        HeadCloth = 11,
        /// <summary>The live headwear's metal.</summary>
        HeadMetal = 12,
        /// <summary>The live headwear's trim: bands, badges, rims.</summary>
        HeadTrim = 13,
        /// <summary>The live headwear's lit parts: lenses, stones, the visor slit, the microphone light.</summary>
        HeadGlow = 14
    }

    /// <summary>
    /// One person's colours on a crowd body. Skin, Cloth, Leather and Bronze multiply the shared textures (one is white),
    /// Hair is the brow colour itself (the brow material is white) and Glow is the emission of the bronze trim (black
    /// for everyone but a Shard-Touched elite). Deterministic from the NPC's own appearance, role and gang.
    /// </summary>
    public struct LiveCrowdTint
    {
        public Color Skin;
        public Color Hair;
        public Color Cloth;
        public Color Leather;
        public Color Bronze;
        public Color Glow;
    }

    /// <summary>
    /// Engine-free rules of the live crowd (owner direction in chat 2026-10-23: swap NPCs and enemies for realistic bodies):
    /// which crowd row serves an NPC (by role and sex, never a creature, companion or named cast actor, never extreme
    /// proportions), what each imported material is, which pieces are armour and hide on plain roles, the per-person tint
    /// from the canon palettes, and the camera budget with hysteresis. Pure string and number work, tested offline.
    /// </summary>
    public static class LiveCrowdRules
    {
        /// <summary>The identity prefix of a crowd row ("crowd:" plus a lowercase slug), never an actor identity.</summary>
        public const string Prefix = "crowd:";
        /// <summary>The recorded owner direction behind the crowd rows.</summary>
        public const string OwnerDirectionRecord = "owner direction in chat 2026-10-23: swap NPCs and enemies for realistic bodies";
        /// <summary>Crowd visuals drawn at once by default, and the hard cap of the F2 page.</summary>
        public const int DefaultBudget = 20, MaxBudget = 40;
        /// <summary>How far from the camera a person may receive a body, and the F2 limits.</summary>
        public const float DefaultRadius = 60f, MinRadius = 15f, MaxRadius = 120f;
        /// <summary>A person who has a body keeps it this much beyond the radius (hysteresis).</summary>
        public const float ReleaseMargin = 8f;
        /// <summary>When every slot is taken, a newcomer must be this much nearer than the farthest holder to swap in.</summary>
        public const float SwapMargin = 6f;
        /// <summary>How often the budget is re-evaluated.</summary>
        public const float TickSeconds = 0.4f;
        /// <summary>Candidates the budget can track at once.</summary>
        public const int MaxCandidates = 512;
        /// <summary>Procedural proportions a crowd body can wear without visible distortion (Appearance.Height and Bulk).</summary>
        public const float MinHeight = 0.88f, MaxHeight = 1.12f, MinBulk = 0.7f, MaxBulk = 1.35f;
        /// <summary>Tint multipliers stay inside this range so the shared textures never wash out or vanish.</summary>
        public const float MinTint = 0.3f, MaxTint = 5f;
        /// <summary>A study body is scaled to the person's height within this range (HeightScale).</summary>
        public const float MinHeightScale = 0.86f, MaxHeightScale = 1.16f;

        /// <summary>The bit of a role in a crowd role mask.</summary>
        public static int RoleBit(NpcRole role)
        {
            return 1 << (int)role;
        }

        /// <summary>Every role a crowd row may serve: civilians, clerks, police, sentinels, gang members and mission enemies.</summary>
        public static readonly int CrowdRoles = RoleBit(NpcRole.Civilian) | RoleBit(NpcRole.Clerk) | RoleBit(NpcRole.Police) | RoleBit(NpcRole.Swat) | RoleBit(NpcRole.Gang) | RoleBit(NpcRole.Enemy);
        /// <summary>Roles drawn without the study's armour pieces (a plain coat).</summary>
        public static readonly int PlainRoles = RoleBit(NpcRole.Civilian) | RoleBit(NpcRole.Clerk);

        // Canon palettes (the brief of 2026-10-23), as sRGB colours.
        private static readonly Color GothicHeart1 = Hex(0x31373f), GothicHeart2 = Hex(0x665b4e), GothicHeart0 = Hex(0x15191f);
        private static readonly Color Amethyst1 = Hex(0x322142), Amethyst2 = Hex(0x643f83), Amethyst3 = Hex(0x9d67c1);
        private static readonly Color Azure1 = Hex(0x135058);
        private static readonly Color Veil1 = Hex(0x1c2843), Veil2 = Hex(0x394e74);
        /// <summary>Coat colours a civilian may wear (index Appearance.Shirt modulo the count); the first keeps the charcoal wool as it is.</summary>
        private static readonly Color[] CivilianCloth = { Color.clear, GothicHeart1, GothicHeart2, Amethyst1, Amethyst2, Azure1, Veil1, Veil2 };
        /// <summary>Average sRGB colour of the shared cloth map, the skin atlases and the shared leather map (measured offline).</summary>
        private static readonly Color ClothBase = new Color(0.103f, 0.111f, 0.134f), SkinBase = new Color(0.67f, 0.46f, 0.32f), LeatherBase = new Color(0.151f, 0.112f, 0.088f);
        /// <summary>Skin multipliers per Appearance.Skin (0 light to 5 deep), chosen so the atlas lands near the procedural skin palette.</summary>
        private static readonly Color[] SkinTints =
        {
            new Color(1.30f, 1.45f, 1.60f), new Color(1.18f, 1.28f, 1.38f), new Color(1.08f, 1.12f, 1.16f),
            new Color(0.92f, 0.90f, 0.87f), new Color(0.68f, 0.63f, 0.57f), new Color(0.47f, 0.42f, 0.38f)
        };
        private static readonly Color[] LeatherTints = { Color.white, new Color(0.7f, 0.7f, 0.72f), new Color(1.25f, 1.0f, 0.8f), new Color(0.5f, 0.5f, 0.55f) };

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        /// <summary>True for a crowd identity: "crowd:" plus lowercase letters, digits and dashes (no spaces, paths or colons).</summary>
        public static bool ValidIdentity(string identity)
        {
            if (string.IsNullOrEmpty(identity) || !identity.StartsWith(Prefix, StringComparison.Ordinal) || identity.Length == Prefix.Length) return false;
            for (int i = Prefix.Length; i < identity.Length; i++)
            {
                char c = identity[i];
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-')) return false;
            }
            return true;
        }

        /// <summary>True for a nonempty role mask that names only crowd roles.</summary>
        public static bool ValidRoles(int mask)
        {
            return mask != 0 && (mask & ~CrowdRoles) == 0;
        }

        /// <summary>
        /// Reads the "roles" list of a drop-in livecast.json ("Civilian", "Clerk", "Police", "Swat", "Gang", "Enemy", any case;
        /// "all" or an empty list means every crowd role). False, with mask 0, for an unknown name or a non-crowd role.
        /// </summary>
        public static bool ParseRoles(string[] names, out int mask)
        {
            mask = 0;
            if (names == null || names.Length == 0)
            {
                mask = CrowdRoles;
                return true;
            }
            foreach (string name in names)
            {
                string text = (name ?? "").Trim();
                if (text.Length == 0) continue;
                if (string.Equals(text, "all", StringComparison.OrdinalIgnoreCase))
                {
                    mask |= CrowdRoles;
                    continue;
                }
                NpcRole role;
                if (!Enum.TryParse(text, true, out role) || (RoleBit(role) & CrowdRoles) == 0)
                {
                    mask = 0;
                    return false;
                }
                mask |= RoleBit(role);
            }
            if (mask == 0) mask = CrowdRoles;
            return true;
        }

        /// <summary>A short list of the roles in a mask for the F2 page and the Console ("civilians, police").</summary>
        public static string RoleList(int mask)
        {
            string text = "";
            if ((mask & RoleBit(NpcRole.Civilian)) != 0) text += "civilians, ";
            if ((mask & RoleBit(NpcRole.Clerk)) != 0) text += "clerks, ";
            if ((mask & RoleBit(NpcRole.Police)) != 0) text += "police, ";
            if ((mask & RoleBit(NpcRole.Swat)) != 0) text += "sentinels, ";
            if ((mask & RoleBit(NpcRole.Gang)) != 0) text += "gang members, ";
            if ((mask & RoleBit(NpcRole.Enemy)) != 0) text += "mission enemies, ";
            return text.Length > 2 ? text.Substring(0, text.Length - 2) : "nobody";
        }

        /// <summary>True when a procedural body's proportions sit inside the range a crowd body wears without distortion.</summary>
        public static bool ProportionsFit(float height, float bulk)
        {
            if (float.IsNaN(height) || float.IsNaN(bulk)) return false;
            if (height <= 0f) height = 1f;
            if (bulk <= 0f) bulk = 1f;
            return height >= MinHeight && height <= MaxHeight && bulk >= MinBulk && bulk <= MaxBulk;
        }

        /// <summary>
        /// Whether an enabled crowd row with these roles and this sex serves an NPC. Creatures (and people in creature mode),
        /// companions, named campaign cast actors and bodies outside the proportion range stay procedural; the row's sex must
        /// match the NPC's, so a female body never stands in for a man or the other way round.
        /// </summary>
        public static bool Serves(int rowRoles, bool rowFemale, bool rowEnabled, NpcRole role, bool female, bool creature, bool castActor, bool proportionsFit)
        {
            if (!rowEnabled || creature || castActor || !proportionsFit) return false;
            if (role == NpcRole.Companion || (rowRoles & RoleBit(role)) == 0 || (RoleBit(role) & CrowdRoles) == 0) return false;
            return rowFemale == female;
        }

        /// <summary>The index of the first enabled crowd row (rows are ordered drop-ins first) that serves the NPC, or -1.</summary>
        public static int FindCrowd(IList<LiveCastRow> rows, NpcRole role, bool female, bool creature, bool castActor, float height, float bulk)
        {
            if (rows == null) return -1;
            bool fits = ProportionsFit(height, bulk);
            for (int i = 0; i < rows.Count; i++)
            {
                LiveCastRow row = rows[i];
                if (row == null || row.Model == null || row.CrowdRoles == 0 || !string.IsNullOrEmpty(row.CastId)) continue;
                if (Serves(row.CrowdRoles, row.CrowdFemale, row.Model.Enabled, role, female, creature, castActor, fits)) return i;
            }
            return -1;
        }

        /// <summary>
        /// Which surface a material name is. The study's imported names ("CourtDefender Skin and underlayer", "H6 Charcoal
        /// wool", "H6 Worn leather", "H6 Aged bronze", "H6 Blackened steel", the pack's "MI Hair" brows and "MI Eyes"), the
        /// generator's prepared names ("CourtDefender Wool") and plain drop-in names ("Coat", "Boots", "Hair") are all read by
        /// their last word: skin, face or body; wool, cloth, coat or fabric; leather, boots or belt; bronze, brass, gold or
        /// trim; steel, iron, armour, armor or plate; brows, brow, hair or beard; eyes or eye. Underscores read as spaces; an
        /// instance suffix and a numeric duplicate suffix (".001") are ignored.
        /// </summary>
        public static LiveCrowdSurface Surface(string imported)
        {
            if (string.IsNullOrWhiteSpace(imported)) return LiveCrowdSurface.Unknown;
            string name = imported.Replace(" (Instance)", "").Replace('_', ' ').Trim();
            int dot = name.LastIndexOf('.');
            if (dot > 0 && dot < name.Length - 1)
            {
                bool digits = true;
                for (int i = dot + 1; i < name.Length; i++) if (name[i] < '0' || name[i] > '9') { digits = false; break; }
                if (digits) name = name.Substring(0, dot).TrimEnd();
            }
            string lower = name.ToLowerInvariant();
            if (lower.EndsWith("skin and underlayer", StringComparison.Ordinal)) return LiveCrowdSurface.Skin;
            if (lower.StartsWith("mi hair", StringComparison.Ordinal)) return LiveCrowdSurface.Brows;
            if (lower == "mi eyes") return LiveCrowdSurface.Eyes;
            int space = lower.LastIndexOf(' ');
            string last = space >= 0 ? lower.Substring(space + 1) : lower;
            switch (last)
            {
                case "skin": case "face": case "body": return LiveCrowdSurface.Skin;
                case "wool": case "cloth": case "coat": case "fabric": return LiveCrowdSurface.Wool;
                case "leather": case "boots": case "belt": return LiveCrowdSurface.Leather;
                case "bronze": case "brass": case "gold": case "trim": return LiveCrowdSurface.Bronze;
                case "steel": case "iron": case "armour": case "armor": case "plate": return LiveCrowdSurface.Steel;
                case "brows": case "brow": case "hair": case "beard": return LiveCrowdSurface.Brows;
                case "eyes": case "eye": return LiveCrowdSurface.Eyes;
                default: return LiveCrowdSurface.Unknown;
            }
        }

        /// <summary>
        /// True for a piece of the study's armour: every steel piece, and any piece named as armour, a plate or a pauldron
        /// (the bronze "front armor edge" strips). Coat fasteners, the coat, the collar and the body are never armour.
        /// </summary>
        public static bool ArmourPart(string nodeName, LiveCrowdSurface surface)
        {
            if (surface == LiveCrowdSurface.Steel) return true;
            if (string.IsNullOrEmpty(nodeName)) return false;
            string lower = nodeName.ToLowerInvariant();
            return lower.Contains("armor") || lower.Contains("armour") || lower.Contains("plate") || lower.Contains("pauldron");
        }

        /// <summary>Whether a renderer of a crowd body shows for a role: armour pieces hide on plain roles, everything else always shows.</summary>
        public static bool PartShown(bool armourPart, NpcRole role)
        {
            return !armourPart || (PlainRoles & RoleBit(role)) == 0;
        }

        /// <summary>
        /// The uniform scale a study body takes for one person: the procedural rig's head height over the study's own at
        /// rest, clamped. The pose copy pins every bone where the person's rig is, so without this a tall person's skin is
        /// stretched everywhere and a short person's compressed; with it, only the proportion differences remain (the
        /// studies' arms are shorter than the rig's). One for anything unmeasurable.
        /// </summary>
        public static float HeightScale(float rigHeadHeight, float studyHeadHeight)
        {
            if (float.IsNaN(rigHeadHeight) || float.IsNaN(studyHeadHeight) || rigHeadHeight <= 0.5f || studyHeadHeight <= 0.5f) return 1f;
            return Mathf.Clamp(rigHeadHeight / studyHeadHeight, MinHeightScale, MaxHeightScale);
        }

        /// <summary>The multiplier that lands the shared wool map on a target colour (hoods, caps and beanies cut from the coat's cloth).</summary>
        public static Color ClothMultiplier(Color target)
        {
            return Multiplier(target, ClothBase);
        }

        /// <summary>The multiplier that turns a texture's average colour into a target colour, clamped to the tint range.</summary>
        public static Color Multiplier(Color target, Color average)
        {
            return new Color(Clamp(target.r / Mathf.Max(0.01f, average.r)), Clamp(target.g / Mathf.Max(0.01f, average.g)), Clamp(target.b / Mathf.Max(0.01f, average.b)), 1f);
        }

        private static float Clamp(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return 1f;
            return Mathf.Clamp(v, MinTint, MaxTint);
        }

        private static int Wrap(int value, int count)
        {
            int m = value % count;
            return m < 0 ? m + count : m;
        }

        /// <summary>
        /// One person's colours. Skin follows Appearance.Skin (a realistic light to deep range), the brows follow the hair
        /// colour, civilians and clerks pick a coat colour from the canon palettes by Appearance.Shirt and a leather shade by
        /// Appearance.Pants, police wear Veil navy and sentinels near-black, gang members wear their gang colour folded into
        /// the charcoal with the gang colour on the bronze trim, mission enemies wear Gothic Heart or Amethyst, and a
        /// Shard-Touched elite gets a restrained amethyst glow on the trim.
        /// </summary>
        public static LiveCrowdTint Tint(NpcRole role, Appearance look, Color gangColor, bool hasGang, bool elite)
        {
            LiveCrowdTint t = new LiveCrowdTint();
            t.Skin = SkinTints[Wrap(look.Skin, SkinTints.Length)];
            // The brows take the natural hair colour the live hair uses (LiveHairRules), so brows and strands match.
            t.Hair = LiveHairRules.Naturals[LiveHairRules.NaturalIndex(look)];
            t.Hair.a = 1f;
            t.Leather = LeatherTints[Wrap(look.Pants, LeatherTints.Length)];
            t.Bronze = Color.white;
            t.Glow = Color.black;
            switch (role)
            {
                case NpcRole.Police:
                    t.Cloth = Multiplier(Veil1, ClothBase);
                    t.Leather = new Color(0.75f, 0.8f, 0.9f, 1f);
                    break;
                case NpcRole.Swat:
                    t.Cloth = Multiplier(GothicHeart0, ClothBase);
                    t.Leather = new Color(0.6f, 0.62f, 0.7f, 1f);
                    break;
                case NpcRole.Gang:
                {
                    Color gang = hasGang ? gangColor : Amethyst2;
                    gang.a = 1f;
                    t.Cloth = Multiplier(Color.Lerp(GothicHeart1, gang, 0.5f), ClothBase);
                    t.Bronze = Color.Lerp(Color.white, gang, 0.5f);
                    t.Bronze.a = 1f;
                    break;
                }
                case NpcRole.Enemy:
                    t.Cloth = Multiplier((look.Shirt & 1) == 0 ? GothicHeart1 : Amethyst1, ClothBase);
                    t.Leather = new Color(0.8f, 0.8f, 0.85f, 1f);
                    break;
                case NpcRole.Clerk:
                    t.Cloth = Multiplier((look.Shirt & 1) == 0 ? GothicHeart1 : GothicHeart2, ClothBase);
                    break;
                default:
                {
                    Color coat = CivilianCloth[Wrap(look.Shirt, CivilianCloth.Length)];
                    t.Cloth = coat.a <= 0f ? Color.white : Multiplier(coat, ClothBase);
                    break;
                }
            }
            if (elite)
            {
                t.Glow = Amethyst3 * 0.8f;
                t.Glow.a = 1f;
                t.Cloth = new Color(Clamp(t.Cloth.r), Clamp(t.Cloth.g * 0.92f), Clamp(t.Cloth.b * 1.08f), 1f);
            }
            return t;
        }

        /// <summary>
        /// A named cast member's colours on a study body, from their procedural costume: the coat folds the costume's main
        /// colour into the charcoal wool, the bronze trim takes the accent, the leather follows the look, no armour glow.
        /// Deterministic, so a cast member looks the same every visit.
        /// </summary>
        public static LiveCrowdTint CastTint(Appearance look, Color main, Color accent)
        {
            LiveCrowdTint t = Tint(NpcRole.Civilian, look, Color.black, false, false);
            main.a = 1f;
            accent.a = 1f;
            t.Cloth = Multiplier(Color.Lerp(GothicHeart1, main, 0.65f), ClothBase);
            t.Bronze = Color.Lerp(Color.white, accent, 0.6f);
            t.Bronze.a = 1f;
            t.Glow = Color.black;
            return t;
        }

        /// <summary>The sRGB skin colour a tint index lands near (documentation and tests).</summary>
        public static Color SkinResult(int skin)
        {
            Color m = SkinTints[Wrap(skin, SkinTints.Length)];
            return new Color(SkinBase.r * m.r, SkinBase.g * m.g, SkinBase.b * m.b, 1f);
        }

        /// <summary>The sRGB leather colour a leather tint index lands near (documentation and tests).</summary>
        public static Color LeatherResult(int pants)
        {
            Color m = LeatherTints[Wrap(pants, LeatherTints.Length)];
            return new Color(LeatherBase.r * m.r, LeatherBase.g * m.g, LeatherBase.b * m.b, 1f);
        }

        /// <summary>
        /// The budget: which of count candidates (distance from the camera, whether each holds a body now) receive a body.
        /// Holders keep theirs up to radius plus ReleaseMargin; when more hold than the budget allows the farthest give
        /// theirs up; free slots go to the nearest newcomers inside the radius; when every slot is taken, one newcomer per
        /// tick may replace the farthest holder if it is SwapMargin nearer. order and keys are scratch arrays and grant is
        /// the result, all at least count long; nothing is allocated. Returns how many are granted.
        /// </summary>
        public static int Select(int count, float[] distance, bool[] held, int budget, float radius, int[] order, float[] keys, bool[] grant)
        {
            if (count <= 0 || distance == null || held == null || order == null || keys == null || grant == null) return 0;
            if (count > distance.Length || count > held.Length || count > order.Length || count > keys.Length || count > grant.Length) return 0;
            if (budget < 0) budget = 0;
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
                float d = distance[i];
                keys[i] = float.IsNaN(d) || float.IsInfinity(d) || d < 0f ? float.MaxValue : d;
                grant[i] = false;
            }
            Array.Sort(keys, order, 0, count);
            int kept = 0;
            float keep = radius + ReleaseMargin;
            for (int k = 0; k < count; k++)
            {
                int i = order[k];
                if (held[i] && keys[k] <= keep)
                {
                    grant[i] = true;
                    kept++;
                }
            }
            for (int k = count - 1; k >= 0 && kept > budget; k--)
            {
                int i = order[k];
                if (grant[i])
                {
                    grant[i] = false;
                    kept--;
                }
            }
            for (int k = 0; k < count && kept < budget; k++)
            {
                if (keys[k] > radius) break;
                int i = order[k];
                if (!grant[i])
                {
                    grant[i] = true;
                    kept++;
                }
            }
            if (kept >= budget && budget > 0)
            {
                int near = -1, far = -1;
                for (int k = 0; k < count; k++)
                {
                    if (keys[k] > radius) break;
                    if (!grant[order[k]]) { near = k; break; }
                }
                for (int k = count - 1; k >= 0; k--)
                    if (grant[order[k]]) { far = k; break; }
                if (near >= 0 && far >= 0 && near < far && keys[near] + SwapMargin < keys[far])
                {
                    grant[order[far]] = false;
                    grant[order[near]] = true;
                }
            }
            return kept;
        }
    }
}
