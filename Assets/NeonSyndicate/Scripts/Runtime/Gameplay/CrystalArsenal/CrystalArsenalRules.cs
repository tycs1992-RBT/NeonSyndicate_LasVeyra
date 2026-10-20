using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The choices on the crystal offer card and the ARSENAL claim panel.</summary>
    public enum CrystalClaimAction
    {
        /// <summary>Equip into the first empty loot slot.</summary>
        EquipFree = 0,
        /// <summary>Equip into a chosen slot; its gun moves to the backpack.</summary>
        EquipReplace = 1,
        /// <summary>Put it in the backpack (or keep it there when it already is).</summary>
        ToBackpack = 2,
        /// <summary>Keep the reward as a pending claim (nothing has room).</summary>
        KeepPending = 3
    }

    /// <summary>The armory's room as the offer rules see it (built by the arsenal from LootArmory every time).</summary>
    public struct CrystalCapacity
    {
        /// <summary>First empty loot slot, or -1.</summary>
        public int FreeSlot;
        public bool[] SlotOccupied;
        public bool[] SlotFavorite;
        public string[] SlotNames;
        public bool BackpackFull;
        /// <summary>The reward's gun already sits in the backpack (a granted reward).</summary>
        public bool InBackpack;
        /// <summary>The reward's gun is already in a loot slot (its index), or -1.</summary>
        public int InSlot;
    }

    /// <summary>
    /// Engine-free offer and claim rules: which choice is possible with the armory's current room and why a refused one
    /// is refused. Nothing is ever deleted, a favourite is never replaced, and a replaced gun always has somewhere to go.
    /// </summary>
    public static class CrystalOfferRules
    {
        /// <summary>No free slot, a full backpack and the reward not yet inside: only a pending claim is possible.</summary>
        public static bool AllFull(CrystalCapacity c)
        {
            return c.FreeSlot < 0 && c.BackpackFull && !c.InBackpack && c.InSlot < 0;
        }

        public static bool Allowed(CrystalClaimAction a, int slot, CrystalCapacity c, out string reason)
        {
            reason = "";
            int slots = c.SlotOccupied != null ? c.SlotOccupied.Length : 0;
            switch (a)
            {
                case CrystalClaimAction.EquipFree:
                    if (c.InSlot >= 0) { reason = "Already equipped in loot slot " + (c.InSlot + 1) + "."; return false; }
                    if (c.FreeSlot < 0) { reason = "All four loot slots are full. Replace one, or send it to the backpack."; return false; }
                    return true;
                case CrystalClaimAction.EquipReplace:
                    if (slot < 0 || slot >= slots) { reason = "Pick a loot slot."; return false; }
                    if (c.InSlot == slot) { reason = "Already equipped in loot slot " + (slot + 1) + "."; return false; }
                    if (!c.SlotOccupied[slot]) return true;
                    if (c.SlotFavorite != null && slot < c.SlotFavorite.Length && c.SlotFavorite[slot])
                    {
                        reason = SlotName(c, slot) + " is a favourite and is never replaced. Unmark it in ARSENAL or pick another slot.";
                        return false;
                    }
                    // A gun moving between slots swaps places; a gun from the backpack frees its own space.
                    if (c.InSlot >= 0 || c.InBackpack || !c.BackpackFull) return true;
                    reason = "The backpack is full, so " + SlotName(c, slot) + " would have nowhere to go. Free a backpack space first.";
                    return false;
                case CrystalClaimAction.ToBackpack:
                    if (c.InBackpack) return true;
                    if (c.InSlot >= 0) { reason = "It is equipped in loot slot " + (c.InSlot + 1) + "; unequip it in ARSENAL."; return false; }
                    if (c.BackpackFull) { reason = "The backpack is full."; return false; }
                    return true;
                default:
                    if (c.InBackpack || c.InSlot >= 0) { reason = "It is already in your armory."; return false; }
                    return true;
            }
        }

        private static string SlotName(CrystalCapacity c, int slot)
        {
            string n = c.SlotNames != null && slot < c.SlotNames.Length ? c.SlotNames[slot] : null;
            return string.IsNullOrEmpty(n) ? "The gun in slot " + (slot + 1) : n;
        }
    }

    /// <summary>Calibration at the practice lane: every target hit at least once by a crystal focus.</summary>
    public static class CrystalPracticeRules
    {
        public const int TargetCount = 3;

        /// <summary>Records a hit. Returns true only on the hit that completes calibration (all three by a focus).</summary>
        public static bool RegisterHit(bool[] focusHits, int index, bool byFocus)
        {
            if (focusHits == null || index < 0 || index >= focusHits.Length || !byFocus) return false;
            bool before = Complete(focusHits);
            focusHits[index] = true;
            return !before && Complete(focusHits);
        }

        public static bool Complete(bool[] focusHits)
        {
            if (focusHits == null || focusHits.Length == 0) return false;
            for (int i = 0; i < focusHits.Length; i++) if (!focusHits[i]) return false;
            return true;
        }

        public static int Count(bool[] focusHits)
        {
            int n = 0;
            if (focusHits == null) return 0;
            for (int i = 0; i < focusHits.Length; i++) if (focusHits[i]) n++;
            return n;
        }
    }

    /// <summary>The practice lane's authored geometry from its shooting spot and direction (all flat, y from the spot).</summary>
    public struct CrystalLaneLayout
    {
        public Vector3 Shooter;
        public Vector3 Dir;
        public Vector3 Right;
        public Vector3[] Targets;
        public Vector3 WallCenter;
        /// <summary>Full wall size: width across the lane, height, thickness along it.</summary>
        public Vector3 WallSize;
        /// <summary>Footprint rectangle: starts here (centre of the near edge) and runs Length along Dir.</summary>
        public Vector3 FootStart;
        public float FootLength;
        public float FootHalfWidth;
    }

    /// <summary>
    /// Engine-free placement maths for the resonance case and the practice lane: ordered candidate spots along the Veyra
    /// Lofts frontage, the lane rectangle, point and segment tests against it, and the "player is in the lane" zone.
    /// The engine side runs the ground, building, road and physics checks on these candidates in this order.
    /// </summary>
    public static class CrystalCaseRules
    {
        public const float TargetDistance = 8f;
        public const float TargetSpacing = 1.7f;
        public const float WallDistance = 10.6f;
        public static readonly Vector3 WallSize = new Vector3(6.2f, 3.2f, 0.45f);
        public const float FootBehind = 1f;
        public const float FootHalfWidth = 3.4f;
        /// <summary>Closest a case may stand to the Lofts door, in metres.</summary>
        public const float MinDoorDistance = 4f;

        /// <summary>Nothing to clear: the class holds only constant data (kept so the reset hub covers every new static).</summary>
        public static void ResetStatics() { }

        public static Vector3 FlatDir(Vector3 v)
        {
            Vector3 f = new Vector3(v.x, 0f, v.z);
            return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
        }

        /// <summary>The horizontal right of a flat direction (y up).</summary>
        public static Vector3 RightOf(Vector3 dir)
        {
            Vector3 f = FlatDir(dir);
            return new Vector3(f.z, 0f, -f.x);
        }

        /// <summary>
        /// Case spots along the frontage of the building whose door is at door, facing out: beside the door (never in
        /// front of it), close to the facade first so the sidewalk stays open, nearest lateral offsets first.
        /// </summary>
        public static List<Vector3> CaseCandidates(Vector3 door, Vector3 facing)
        {
            Vector3 f = FlatDir(facing);
            Vector3 r = RightOf(f);
            float[] lateral = { 4.5f, 6f, 7.5f, 9f, 10.5f, 12f, 14f, 16f };
            float[] setback = { 1.4f, 2.2f, 3.0f };
            List<Vector3> l = new List<Vector3>();
            for (int i = 0; i < lateral.Length; i++)
            {
                for (int k = 0; k < setback.Length; k++)
                {
                    l.Add(door + f * setback[k] + r * lateral[i]);
                    l.Add(door + f * setback[k] - r * lateral[i]);
                }
            }
            return l;
        }

        /// <summary>Ring candidates around a point (a fallback when the frontage is fully occupied), nearest first.</summary>
        public static List<Vector3> RingCandidates(Vector3 center, float firstRadius, float step, int rings, int perRing)
        {
            List<Vector3> l = new List<Vector3>();
            for (int ring = 0; ring < rings; ring++)
            {
                float radius = firstRadius + ring * step;
                for (int i = 0; i < perRing; i++)
                {
                    float a = (i + (ring % 2) * 0.5f) * Mathf.PI * 2f / perRing;
                    l.Add(center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
                }
            }
            return l;
        }

        /// <summary>The four lane directions tried for a spot: along the frontage first, then out and in.</summary>
        public static Vector3[] LaneDirections(Vector3 facing)
        {
            Vector3 f = FlatDir(facing);
            Vector3 r = RightOf(f);
            return new[] { r, -r, -f, f };
        }

        public static CrystalLaneLayout Lane(Vector3 shooter, Vector3 dir)
        {
            Vector3 d = FlatDir(dir);
            Vector3 r = RightOf(d);
            CrystalLaneLayout L = new CrystalLaneLayout
            {
                Shooter = shooter,
                Dir = d,
                Right = r,
                Targets = new Vector3[CrystalPracticeRules.TargetCount],
                WallCenter = shooter + d * WallDistance,
                WallSize = WallSize,
                FootStart = shooter - d * FootBehind,
                FootLength = FootBehind + WallDistance + WallSize.z * 0.5f + 0.2f,
                FootHalfWidth = FootHalfWidth
            };
            for (int i = 0; i < L.Targets.Length; i++)
            {
                float side = (i - (L.Targets.Length - 1) * 0.5f) * TargetSpacing;
                float depth = TargetDistance + (i % 2 == 1 ? 1f : 0f);
                L.Targets[i] = shooter + d * depth + r * side;
            }
            return L;
        }

        /// <summary>Local (across, along) coordinates of a point relative to a rectangle start and direction.</summary>
        public static Vector2 Local(Vector3 p, Vector3 start, Vector3 dir)
        {
            Vector3 d = FlatDir(dir);
            Vector3 r = RightOf(d);
            Vector3 v = new Vector3(p.x - start.x, 0f, p.z - start.z);
            return new Vector2(Vector3.Dot(v, r), Vector3.Dot(v, d));
        }

        public static bool InRect(Vector3 p, Vector3 start, Vector3 dir, float length, float halfWidth, float margin)
        {
            Vector2 l = Local(p, start, dir);
            return l.y >= -margin && l.y <= length + margin && Mathf.Abs(l.x) <= halfWidth + margin;
        }

        public static bool InFootprint(CrystalLaneLayout L, Vector3 p, float margin)
        {
            return InRect(p, L.FootStart, L.Dir, L.FootLength, L.FootHalfWidth, margin);
        }

        /// <summary>The zone in which the player counts as practising: the lane plus room to stand behind and beside it.</summary>
        public static bool InLaneZone(CrystalLaneLayout L, Vector3 p)
        {
            if (Mathf.Abs(p.y - L.Shooter.y) > 3.5f) return false;
            Vector3 start = L.Shooter - L.Dir * 4f;
            return InRect(p, start, L.Dir, 4f + WallDistance, FootHalfWidth + 1.6f, 0f);
        }

        /// <summary>True when a flat segment crosses (or lies inside) the rectangle grown by margin.</summary>
        public static bool SegmentHitsRect(Vector3 a, Vector3 b, Vector3 start, Vector3 dir, float length, float halfWidth, float margin)
        {
            Vector2 p0 = Local(a, start, dir);
            Vector2 p1 = Local(b, start, dir);
            float minX = -halfWidth - margin, maxX = halfWidth + margin;
            float minY = -margin, maxY = length + margin;
            // Liang-Barsky clipping of the segment against the axis-aligned local box.
            float t0 = 0f, t1 = 1f;
            float dx = p1.x - p0.x, dy = p1.y - p0.y;
            if (!Clip(-dx, p0.x - minX, ref t0, ref t1)) return false;
            if (!Clip(dx, maxX - p0.x, ref t0, ref t1)) return false;
            if (!Clip(-dy, p0.y - minY, ref t0, ref t1)) return false;
            if (!Clip(dy, maxY - p0.y, ref t0, ref t1)) return false;
            return t0 <= t1;
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Abs(p) < 1e-7f) return q >= 0f;
            float t = q / p;
            if (p < 0f)
            {
                if (t > t1) return false;
                if (t > t0) t0 = t;
            }
            else
            {
                if (t < t0) return false;
                if (t < t1) t1 = t;
            }
            return true;
        }

        /// <summary>Sample points covering the footprint (every step metres, edges included) for ground and layout checks.</summary>
        public static List<Vector3> FootprintSamples(CrystalLaneLayout L, float step)
        {
            List<Vector3> l = new List<Vector3>();
            FootprintSamples(L, step, l);
            return l;
        }

        /// <summary>The same samples written into a reused list (the placement audit tries hundreds of lanes).</summary>
        public static void FootprintSamples(CrystalLaneLayout L, float step, List<Vector3> l)
        {
            l.Clear();
            int nx = Mathf.Max(1, Mathf.CeilToInt(L.FootHalfWidth * 2f / step));
            int ny = Mathf.Max(1, Mathf.CeilToInt(L.FootLength / step));
            for (int iy = 0; iy <= ny; iy++)
            {
                for (int ix = 0; ix <= nx; ix++)
                {
                    float across = -L.FootHalfWidth + ix * (L.FootHalfWidth * 2f / nx);
                    float along = iy * (L.FootLength / ny);
                    l.Add(L.FootStart + L.Right * across + L.Dir * along);
                }
            }
        }

        /// <summary>Flat distance from a point to a segment.</summary>
        public static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = new Vector3(b.x - a.x, 0f, b.z - a.z);
            Vector3 ap = new Vector3(p.x - a.x, 0f, p.z - a.z);
            float len2 = ab.sqrMagnitude;
            float t = len2 > 1e-6f ? Mathf.Clamp01(Vector3.Dot(ap, ab) / len2) : 0f;
            Vector3 c = ab * t;
            return (ap - c).magnitude;
        }
    }

    /// <summary>The Forge operations the focus rules judge.</summary>
    public enum CrystalForgeOp
    {
        Infuse = 0,
        Ascend = 1,
        RaiseLevel = 2,
        Scrap = 3,
        Sell = 4,
        AddTrait = 5
    }

    /// <summary>
    /// Engine-free Forge compatibility for crystal foci: element, level and rarity changes keep the behavior (it comes
    /// from the unique index); anything that would add a trait is refused before payment because a focus takes no traits;
    /// selling and scrapping ask for confirmation because a focus is granted once.
    /// </summary>
    public static class CrystalForgeRules
    {
        public const string GrantedOnce = "Granted once: it will not be granted again.";

        public static bool IsFocus(LootGun g)
        {
            return g != null && CrystalFocusDb.BehaviorOfUnique(g.Unique) != CrystalBehavior.None;
        }

        /// <summary>Allowed or refused (with the reason), plus a note the screen shows before payment.</summary>
        public static bool Check(LootGun g, CrystalForgeOp op, out string note)
        {
            note = "";
            if (RegionalArsenal04OE.IsWeapon(g))
            {
                if (op==CrystalForgeOp.Ascend || op==CrystalForgeOp.RaiseLevel) {note="Permanent blade power increases; original contact and move identity remain.";return true;}
                if (op==CrystalForgeOp.Sell || op==CrystalForgeOp.Scrap) {note=GrantedOnce;return true;}
                note="This blade retains its mineral identity. Use ascension, level upgrades or a temporary fitting.";return false;
            }
            if (!IsFocus(g)) return true;
            switch (op)
            {
                case CrystalForgeOp.Infuse:
                    note = "Behavior unchanged: the element rides on every crystal hit.";
                    return true;
                case CrystalForgeOp.Ascend:
                    note = "A crystal focus takes no traits, so ascension raises its power only. Behavior unchanged.";
                    return true;
                case CrystalForgeOp.RaiseLevel:
                    note = "Behavior unchanged; damage follows the new level.";
                    return true;
                case CrystalForgeOp.Scrap:
                case CrystalForgeOp.Sell:
                    note = GrantedOnce;
                    return true;
                default:
                    note = "Refused: a crystal focus takes no traits. Its fire behavior is its signature.";
                    return false;
            }
        }

        /// <summary>True when a Forge result kept the focus behavior and carries no trait (checked before payment).</summary>
        public static bool KeepsBehavior(LootGun before, LootGun after)
        {
            if (before == null || after == null) return false;
            if (RegionalArsenal04OE.IsWeapon(before)) return before.Unique==after.Unique && after.Traits.Count==0 && after.Element==Element.None;
            CrystalBehavior a = CrystalFocusDb.BehaviorOfUnique(before.Unique);
            CrystalBehavior b = CrystalFocusDb.BehaviorOfUnique(after.Unique);
            if (a != b) return false;
            return a == CrystalBehavior.None || after.Traits.Count == 0;
        }

        public static bool NeedsGrantedOnceConfirm(LootGun g)
        {
            return IsFocus(g) || RegionalArsenal04OE.IsWeapon(g);
        }
    }

    /// <summary>The focus lines of an item card, all derived from CrystalFocusDb (the beam never shows a per-tick figure).</summary>
    public struct CrystalCardLines
    {
        public bool Valid;
        public bool Beam;
        public string Name;
        public string FireLine;
        public string HitLine;
        public string DpsLine;
        public string HeatLine;
        public string RangeLine;
        public string Limitation;
        public float SustainedDps;
        public float Primary;
        public float Secondary;
        /// <summary>Shots (or beam seconds) before an overheat.</summary>
        public float BeforeOverheat;
    }

    /// <summary>Engine-free item card text for crystal foci.</summary>
    public static class CrystalCardRules
    {
        public static CrystalCardLines Lines(LootGun g)
        {
            CrystalCardLines c = new CrystalCardLines();
            if (g == null) return c;
            CrystalProfile p = CrystalFocusDb.Get(CrystalFocusDb.BehaviorOfUnique(g.Unique));
            if (p == null) return c;
            c.Valid = true;
            c.Beam = p.Behavior == CrystalBehavior.PrismThread;
            c.Name = p.Name;
            c.FireLine = p.FireLine;
            c.Limitation = p.Limitation;
            c.HeatLine = p.HeatLine;
            c.Primary = CrystalFocusDb.PrimaryDamage(p, g.Rarity, g.Level);
            c.Secondary = CrystalFocusDb.SecondaryDamage(p, g.Rarity, g.Level);
            c.SustainedDps = CrystalFocusDb.SustainedDps(p, g.Rarity, g.Level);
            c.BeforeOverheat = c.Beam ? 1f / Mathf.Max(0.01f, p.Heat) : Mathf.Ceil(1f / Mathf.Max(0.01f, p.Heat));
            c.RangeLine = "Range " + Mathf.RoundToInt(p.Range) + " m" + (p.Speed > 0f ? ", projectile " + Mathf.RoundToInt(p.Speed) + " m/s" : (c.Beam ? ", beam" : ", instant pulse"));
            c.DpsLine = c.Beam
                ? "Sustained " + Mathf.RoundToInt(c.SustainedDps) + " damage per second over a full heat cycle (beam on: " + Mathf.RoundToInt(c.Primary) + " per second, ramp excluded)"
                : "Sustained " + Mathf.RoundToInt(c.SustainedDps) + " damage per second over a full heat cycle (misses and movement excluded)";
            c.HitLine = HitText(p, c.Primary, c.Secondary);
            return c;
        }

        private static string HitText(CrystalProfile p, float primary, float secondary)
        {
            int a = Mathf.RoundToInt(primary), b = Mathf.RoundToInt(secondary);
            switch (p.Behavior)
            {
                case CrystalBehavior.Shardwake: return "Shard " + a + "; charged fan " + p.Count + " x " + b;
                case CrystalBehavior.PrismThread: return "Beam " + a + " per second, ramps to x" + CrystalFocusDb.BeamRampMax.ToString("0.00") + " on held aimed contact";
                case CrystalBehavior.GyreCrescent: return "Disc " + a + " out, " + b + " on the return, up to " + p.MaxVictims + " foes, " + p.Count + " banks";
                case CrystalBehavior.Sunderheart: return "Rupture " + a + " at the centre, " + b + " splash in " + p.Radius.ToString("0.0") + " m, up to " + p.MaxVictims + " foes";
                case CrystalBehavior.MirageChoir: return p.Count + " guided shards x " + a + ", up to " + CrystalFocusDb.MaxLocks + " locks";
                case CrystalBehavior.Mirrorwake: return "Pulse " + a + "; a planted focus bends one pulse to a foe";
                default: return "Hit " + a;
            }
        }
    }
}
