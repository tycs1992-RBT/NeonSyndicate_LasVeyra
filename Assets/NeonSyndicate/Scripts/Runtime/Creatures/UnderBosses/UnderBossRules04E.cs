using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a hit volume of an Underheart boss is (the damage it takes depends on it).</summary>
    public enum UBossPart
    {
        /// <summary>Plain hull or hide.</summary>
        Body = 0,
        /// <summary>The Matron's heat shell: tough until a quench cracks it.</summary>
        Shell = 1,
        /// <summary>A visible weak point (the Guardian's valve wheel, the Matron's ember heart).</summary>
        Weak = 2,
        /// <summary>The Vaultbreaker's glowing flank and rear seams (fracture builds there).</summary>
        Seam = 3,
        /// <summary>The Vaultbreaker's furnace core behind its chest hatch.</summary>
        Core = 4,
        /// <summary>Legs, arms and tails: they soak most of a hit.</summary>
        Limb = 5,
        /// <summary>The Regent's crown emitter (open while it vents after a disruption pulse).</summary>
        Crown = 6,
        /// <summary>The Regent's regulator, caged until the finale.</summary>
        Regulator = 7,
        /// <summary>The Vaultbreaker's front armour: flank it instead.</summary>
        FrontPlate = 8
    }

    /// <summary>How an Underheart boss fight or Choir mechanism ended.</summary>
    public enum UBossEnd
    {
        None = 0,
        /// <summary>The boss was confirmed dead (the only ending that pays rewards).</summary>
        Victory = 1,
        PlayerDied = 2,
        /// <summary>The player left the arena boundary for too long (or fell or flew far out of it).</summary>
        PlayerLeft = 3,
        /// <summary>The player held against the arena seal to retreat on purpose.</summary>
        Withdrew = 4,
        /// <summary>The owner (a quest cleanup, the debug page) aborted it.</summary>
        Aborted = 5,
        /// <summary>A new game or a loaded save.</summary>
        SessionReset = 6,
        /// <summary>The game went back to the title screen.</summary>
        Title = 7,
        /// <summary>The arena or the body could not be built.</summary>
        BuildFailed = 8
    }

    /// <summary>Every move of the four Underheart bosses.</summary>
    public enum UBossMove
    {
        None = 0,
        // Valve Guardian
        VgSteamJet, VgStomp, VgScald, VgSteamLance,
        // Cinder Matron
        MaCharge, MaBreath, MaTailSweep, MaEmberRain, MaEmberVolley, MaCinderNova,
        // Vaultbreaker
        VbHammer, VbSweep, VbFurnaceShot, VbShardFan, VbOverheat,
        // Kiln Regent
        RgDisruption, RgSlam, RgSweep, RgEmberVolley, RgFurnaces, RgBeamLanes, RgRegulatorLanes
    }

    /// <summary>What a boss's move chooser may know: positions, line of sight and its own state. Never player input.</summary>
    public struct UBossView
    {
        /// <summary>0 based phase index.</summary>
        public int Phase;
        /// <summary>Flat metres from the boss to the player.</summary>
        public float Distance;
        /// <summary>The player is high above the floor (ground rings cannot reach).</summary>
        public bool Airborne;
        /// <summary>Moves since the signature move.</summary>
        public int SinceSignature;
        /// <summary>The player stands outside the boss's frontal arc (Vaultbreaker).</summary>
        public bool PlayerFlanking;
        /// <summary>Vaultbreaker furnace heat, 0 to 100.</summary>
        public float Heat;
        /// <summary>Matron: at least one cooled plate is ready to quench her.</summary>
        public bool PlateReady;
        /// <summary>Regent: 0 means the furnace cycle comes next, 1 the beam lanes.</summary>
        public int CycleNext;
        /// <summary>Regent: a furnace or beam cycle is still running.</summary>
        public bool CycleBusy;
        /// <summary>Regent: the player's companion is fused with a vehicle right now.</summary>
        public bool Fused;
    }

    /// <summary>One finite batch of announced reinforcements: which phase opens it, which species and how many.</summary>
    public struct UBossAddBatch
    {
        public int Phase;
        public string Species;
        public int Count;
        public string Announce;
    }

    /// <summary>
    /// One Underheart boss: health and damage at Balanced, phase thresholds, its finite reinforcement batches, the
    /// lesson and counter the HUD and docs show, its element interactions and the places it uses.
    /// </summary>
    public sealed class UnderBossProfile
    {
        public string Id;
        public string Title;
        public bool Principal;
        public float BaseHealth;
        /// <summary>Raw hit damage before the difficulty's per-hit cap (like a creature species' Damage).</summary>
        public float BaseDamage;
        /// <summary>Health fractions that open later phases, highest first. Phases = thresholds + 1.</summary>
        public float[] PhaseThresholds;
        public string[] PhaseNames;
        public UBossAddBatch[] Batches;
        public int MaxAddsAlive;
        public Element Weakness;
        public Element Resists;
        public string Lesson;
        public string Counter;
        /// <summary>What the first-win reward expresses (docs and the victory banner).</summary>
        public string RewardNote;
        /// <summary>Underheart anchors: the arena, the entrance gate the seal closes (empty for none), the cleared room id.</summary>
        public string ArenaAnchor;
        public string GateAnchor;
        public string RoomId;
        /// <summary>The theme colour of its telegraphs and glow.</summary>
        public Color Theme;

        public int PhaseCount { get { return (PhaseThresholds != null ? PhaseThresholds.Length : 0) + 1; } }

        /// <summary>Total reinforcements this fight may ever bring (the declared finite budget).</summary>
        public int DeclaredAdds
        {
            get
            {
                int n = 0;
                if (Batches != null) for (int i = 0; i < Batches.Length; i++) n += Mathf.Max(0, Batches[i].Count);
                return n;
            }
        }
    }

    /// <summary>
    /// The four Underheart boss profiles (owner: bosses) and the plain maths every fight shares: health per difficulty,
    /// the move choosers, damage per hit volume, element interactions and the Vaultbreaker's heat. Engine-free.
    /// </summary>
    public static class UnderBossProfiles
    {
        public const float PhaseTwoAt = 0.66f;
        public const float PhaseThreeAt = 0.33f;
        public const float MinimumMultiplier = 0.15f;
        /// <summary>Metres above the floor past which ground rings cannot reach the player.</summary>
        public const float AirHeight = 3.2f;

        private static UnderBossProfile[] _all;

        public static void ResetStatics()
        {
            _all = null;
        }

        public static UnderBossProfile[] All
        {
            get
            {
                if (_all == null) _all = Build();
                return _all;
            }
        }

        public static UnderBossProfile For(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            UnderBossProfile[] a = All;
            for (int i = 0; i < a.Length; i++) if (a[i].Id == id) return a[i];
            return null;
        }

        private static UnderBossProfile[] Build()
        {
            return new[]
            {
                new UnderBossProfile
                {
                    Id = UnderBossIds.ValveGuardian, Title = UnderBossIds.Title(UnderBossIds.ValveGuardian), Principal = false,
                    BaseHealth = 1300f, BaseDamage = 16f, PhaseThresholds = new[] { 0.5f },
                    PhaseNames = new[] { "PHASE 1 / 2: PRESSURE", "PHASE 2 / 2: OVERPRESSURE" },
                    Batches = new[] { new UBossAddBatch { Phase = 1, Species = EnemyIds04E.EmberScavenger, Count = 2, Announce = "The pipes burst: 2 ember scavengers pour out" } },
                    MaxAddsAlive = 2, Weakness = Element.Frost, Resists = Element.Ember,
                    Lesson = "A frontal steam jet you can step aside from or catch on your ward; the valve wheel on its back vents after every jet.",
                    Counter = "Sidestep or ward the jet, then strike the glowing valve wheel while it vents.",
                    RewardNote = "A guaranteed progression piece for your build.",
                    ArenaAnchor = "canals.valve_room", GateAnchor = "", RoomId = "canals.valve_room",
                    Theme = new Color(0.75f, 0.92f, 1f)
                },
                new UnderBossProfile
                {
                    Id = UnderBossIds.CinderMatron, Title = UnderBossIds.Title(UnderBossIds.CinderMatron), Principal = true,
                    BaseHealth = 2800f, BaseDamage = 22f, PhaseThresholds = new[] { PhaseTwoAt, PhaseThreeAt },
                    PhaseNames = new[] { "PHASE 1 / 3: LURED", "PHASE 2 / 3: BROOD FIRE", "PHASE 3 / 3: MOLTEN" },
                    Batches = new[] { new UBossAddBatch { Phase = 1, Species = EnemyIds04E.EmberScavenger, Count = 3, Announce = "Her brood answers: 3 ember scavengers" } },
                    MaxAddsAlive = 3, Weakness = Element.Frost, Resists = Element.Ember,
                    Lesson = "Extraction lures drive her; her charge cracks on a cooled crystal plate; her breath is a cone a ward can catch.",
                    Counter = "Shut the lures down, bait the charge across a frosted plate, ward the breath, stand on a cooled plate when the nova fills.",
                    RewardNote = "The Ember Mantle ward aspect (heat pressure absorbed) and a high quality gear choice.",
                    ArenaAnchor = "gardens.matron_arena", GateAnchor = "gardens.matron_gate", RoomId = "gardens.matron_arena",
                    Theme = new Color(1f, 0.45f, 0.12f)
                },
                new UnderBossProfile
                {
                    Id = UnderBossIds.Vaultbreaker, Title = UnderBossIds.Title(UnderBossIds.Vaultbreaker), Principal = true,
                    BaseHealth = 3200f, BaseDamage = 24f, PhaseThresholds = new[] { PhaseTwoAt, PhaseThreeAt },
                    PhaseNames = new[] { "PHASE 1 / 3: ARMOURED", "PHASE 2 / 3: MENDED", "PHASE 3 / 3: CRITICAL" },
                    Batches = new[]
                    {
                        new UBossAddBatch { Phase = 1, Species = EnemyIds04E.RepairConstruct, Count = 2, Announce = "2 repair constructs arrive to mend its seams" },
                        new UBossAddBatch { Phase = 2, Species = EnemyIds04E.AshBurrower, Count = 2, Announce = "The furnace floor cracks: 2 ash burrowers" }
                    },
                    MaxAddsAlive = 3, Weakness = Element.Volt, Resists = Element.None,
                    Lesson = "Front plates shrug off hits and it turns slowly; the seams on its flanks fracture; when it overheats it vents, then opens its core.",
                    Counter = "Flank to the glowing seams, break one to expose the core, get clear when it overheats, then punish the open core.",
                    RewardNote = "The Fracture Lattice focus module (fracture shards on your hits) and a finite vault core.",
                    ArenaAnchor = "workshops.vault_arena", GateAnchor = "workshops.vault_gate", RoomId = "workshops.vault_arena",
                    Theme = new Color(1f, 0.62f, 0.2f)
                },
                new UnderBossProfile
                {
                    Id = UnderBossIds.KilnRegent, Title = UnderBossIds.Title(UnderBossIds.KilnRegent), Principal = true,
                    BaseHealth = 3800f, BaseDamage = 26f, PhaseThresholds = new[] { PhaseTwoAt, PhaseThreeAt },
                    PhaseNames = new[] { "PHASE 1 / 3: DISRUPTION", "PHASE 2 / 3: FURNACES AND BEAMS", "PHASE 3 / 3: REGULATOR EXPOSED" },
                    Batches = new[]
                    {
                        new UBossAddBatch { Phase = 1, Species = EnemyIds04E.ExtractionSentinel, Count = 2, Announce = "2 extraction sentinels march in" },
                        new UBossAddBatch { Phase = 2, Species = EnemyIds04E.FurnaceCaster, Count = 2, Announce = "2 furnace casters take the galleries" }
                    },
                    MaxAddsAlive = 3, Weakness = Element.Frost, Resists = Element.Ember,
                    Lesson = "It breaks a fusion safely, feeds on alternating furnaces, cuts beam lanes, and finally bares its regulator.",
                    Counter = "Quench the burning furnace vents, stand in the gaps between beam lanes, strike the crown when it vents and the regulator when it opens.",
                    RewardNote = "A pinnacle focus or ward choice and the final bond evolution.",
                    ArenaAnchor = "crown.regent_arena", GateAnchor = "crown.regent_gate", RoomId = "crown.regent_arena",
                    Theme = new Color(1f, 0.32f, 0.12f)
                }
            };
        }

        /// <summary>The fight's health for a difficulty (Story is gentler, Veteran tougher; never below a third of Balanced).</summary>
        public static float HealthFor(UnderBossProfile p, DifficultyLevel d)
        {
            if (p == null) return 1f;
            float k = d == DifficultyLevel.Story ? 0.75f : (d == DifficultyLevel.Veteran ? 1.3f : 1f);
            return Mathf.Max(p.BaseHealth / 3f, p.BaseHealth * k);
        }

        /// <summary>The phase index for a health fraction with the profile's thresholds (no latch: the read-only view).</summary>
        public static int PhaseOf(UnderBossProfile p, float healthFraction)
        {
            if (p == null || p.PhaseThresholds == null) return 0;
            int phase = 0;
            for (int i = 0; i < p.PhaseThresholds.Length; i++) if (healthFraction < p.PhaseThresholds[i]) phase = i + 1;
            return phase;
        }

        /// <summary>Damage multiplier of an element against the boss: its weakness 1.35, its resistance 0.65, else 1.</summary>
        public static float ElementMultiplier(UnderBossProfile p, Element e)
        {
            if (p == null || e == Element.None) return 1f;
            if (e == p.Weakness) return 1.35f;
            if (e == p.Resists) return 0.65f;
            return 1f;
        }

        // ================================================================== damage per hit volume

        /// <summary>
        /// The multiplier for a hit on a part. Every result is at least MinimumMultiplier, so nothing in a fight is an
        /// invulnerable sponge; the exposed windows and broken parts raise it. windowFocus is the part the open punish
        /// window names (its own multiplier windowMul), other parts get a smaller share while a window is open.
        /// </summary>
        public static float PartMultiplier(string bossId, UBossPart part, UBossDamageState s)
        {
            float m = 1f;
            switch (bossId)
            {
                case UnderBossIds.ValveGuardian:
                    switch (part)
                    {
                        case UBossPart.Weak: m = 1.5f; break;
                        case UBossPart.Limb: m = 0.6f; break;
                        default: m = 0.9f; break;
                    }
                    break;
                case UnderBossIds.CinderMatron:
                    switch (part)
                    {
                        case UBossPart.Weak: m = 1.8f; break;
                        case UBossPart.Shell: m = s.ShellCracked ? 1.4f : 0.75f; break;
                        case UBossPart.Limb: m = 0.6f; break;
                        default: m = 0.85f; break;
                    }
                    if (s.Lured) m *= 0.6f;
                    break;
                case UnderBossIds.Vaultbreaker:
                    switch (part)
                    {
                        case UBossPart.FrontPlate: m = 0.3f; break;
                        case UBossPart.Seam: m = s.PartBroken ? 1.8f : 1.5f; break;
                        case UBossPart.Core: m = s.CoreOpen ? 2.4f : 0.25f; break;
                        case UBossPart.Limb: m = 0.55f; break;
                        default: m = 0.8f; break;
                    }
                    break;
                case UnderBossIds.KilnRegent:
                    switch (part)
                    {
                        case UBossPart.Crown: m = 0.8f; break;
                        case UBossPart.Regulator: m = s.CoreOpen ? 2.4f : 0.4f; break;
                        case UBossPart.Limb: m = 0.55f; break;
                        default: m = 0.7f; break;
                    }
                    if (s.FurnaceShield) m *= 0.45f;
                    break;
            }
            if (s.WindowOpen)
            {
                if (part == s.WindowFocus && s.WindowMul > 0f) m = Mathf.Max(m, s.WindowMul);
                else m *= 1.25f;
            }
            return Mathf.Max(MinimumMultiplier, m);
        }

        // ================================================================== move choosers

        /// <summary>Picks a boss's next move from what it can see (roll is a 0 to 1 random draw).</summary>
        public static UBossMove Choose(string bossId, UBossView v, float roll)
        {
            switch (bossId)
            {
                case UnderBossIds.ValveGuardian: return ChooseGuardian(v, roll);
                case UnderBossIds.CinderMatron: return ChooseMatron(v, roll);
                case UnderBossIds.Vaultbreaker: return ChooseVault(v, roll);
                case UnderBossIds.KilnRegent: return ChooseRegent(v, roll);
                default: return UBossMove.None;
            }
        }

        private static UBossMove ChooseGuardian(UBossView v, float roll)
        {
            if (v.Airborne) return UBossMove.VgSteamLance;
            if (v.SinceSignature >= 2) return UBossMove.VgSteamJet;
            if (v.Distance < 5.5f && roll < 0.7f) return UBossMove.VgStomp;
            if (roll < (v.Phase >= 1 ? 0.55f : 0.35f)) return UBossMove.VgScald;
            return UBossMove.VgSteamJet;
        }

        private static UBossMove ChooseMatron(UBossView v, float roll)
        {
            if (v.Phase >= 2 && v.SinceSignature >= 3) return UBossMove.MaCinderNova;
            if (v.Airborne) return UBossMove.MaEmberVolley;
            if (v.Distance < 6f && roll < 0.6f) return UBossMove.MaTailSweep;
            if (v.Distance > 7f && (roll < 0.45f || (v.PlateReady && v.SinceSignature >= 2))) return UBossMove.MaCharge;
            if (v.Phase >= 1 && roll > 0.72f) return UBossMove.MaEmberRain;
            if (v.Distance < 14f) return UBossMove.MaBreath;
            return UBossMove.MaCharge;
        }

        private static UBossMove ChooseVault(UBossView v, float roll)
        {
            if (v.Heat >= VaultHeat.Max) return UBossMove.VbOverheat;
            if (v.Airborne) return UBossMove.VbFurnaceShot;
            if (v.Distance < 6.5f) return roll < 0.6f || v.PlayerFlanking ? UBossMove.VbSweep : UBossMove.VbHammer;
            if (v.Distance < 14f)
            {
                if (v.Phase >= 1 && roll < 0.4f) return UBossMove.VbShardFan;
                return UBossMove.VbHammer;
            }
            return v.Phase >= 1 && roll < 0.5f ? UBossMove.VbShardFan : UBossMove.VbFurnaceShot;
        }

        private static UBossMove ChooseRegent(UBossView v, float roll)
        {
            if (v.Fused) return UBossMove.RgDisruption;
            if (v.Phase >= 1 && !v.CycleBusy && v.SinceSignature >= 2) return v.CycleNext == 0 ? UBossMove.RgFurnaces : UBossMove.RgBeamLanes;
            if (v.Airborne) return UBossMove.RgEmberVolley;
            if (v.Phase >= 2 && roll < 0.3f) return UBossMove.RgRegulatorLanes;
            if (v.Distance < 8.5f && roll < 0.65f) return UBossMove.RgSweep;
            return roll < 0.55f ? UBossMove.RgSlam : UBossMove.RgEmberVolley;
        }

        /// <summary>Every move a boss can choose (docs, the debug page and tests).</summary>
        public static UBossMove[] MovesOf(string bossId)
        {
            switch (bossId)
            {
                case UnderBossIds.ValveGuardian: return new[] { UBossMove.VgSteamJet, UBossMove.VgStomp, UBossMove.VgScald, UBossMove.VgSteamLance };
                case UnderBossIds.CinderMatron: return new[] { UBossMove.MaCharge, UBossMove.MaBreath, UBossMove.MaTailSweep, UBossMove.MaEmberRain, UBossMove.MaEmberVolley, UBossMove.MaCinderNova };
                case UnderBossIds.Vaultbreaker: return new[] { UBossMove.VbHammer, UBossMove.VbSweep, UBossMove.VbFurnaceShot, UBossMove.VbShardFan, UBossMove.VbOverheat };
                case UnderBossIds.KilnRegent: return new[] { UBossMove.RgDisruption, UBossMove.RgSlam, UBossMove.RgSweep, UBossMove.RgEmberVolley, UBossMove.RgFurnaces, UBossMove.RgBeamLanes, UBossMove.RgRegulatorLanes };
                default: return new UBossMove[0];
            }
        }

        /// <summary>
        /// The punish windows a boss opens outside its moves' own recoveries: the Matron's quench on a cooled plate, the
        /// Vaultbreaker's fractured seam, the Regent's starved furnaces (Balanced seconds each).
        /// </summary>
        public static float[] SpecialWindows(string bossId)
        {
            switch (bossId)
            {
                case UnderBossIds.CinderMatron: return new[] { UBossTiming.QuenchWindow };
                case UnderBossIds.Vaultbreaker: return new[] { UBossTiming.SeamWindow };
                case UnderBossIds.KilnRegent: return new[] { UBossTiming.StarvedWindow };
                default: return new float[0];
            }
        }

        /// <summary>How many distinct punish windows a boss offers (moves that leave one plus the special openings).</summary>
        public static int PunishWindowCount(string bossId)
        {
            int n = SpecialWindows(bossId).Length;
            UBossMove[] moves = MovesOf(bossId);
            for (int i = 0; i < moves.Length; i++) if (UBossTiming.BaseRecovery(moves[i]) > 0f) n++;
            return n;
        }

        /// <summary>The signature lesson moves (they leave the long windows and reset SinceSignature).</summary>
        public static bool IsSignature(UBossMove m)
        {
            switch (m)
            {
                case UBossMove.VgSteamJet:
                case UBossMove.MaCharge:
                case UBossMove.MaCinderNova:
                case UBossMove.VbOverheat:
                case UBossMove.RgFurnaces:
                case UBossMove.RgBeamLanes:
                case UBossMove.RgDisruption:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Moves aimed in three dimensions (they reach a flying player through visible aim lines).</summary>
        public static bool ReachesAir(UBossMove m)
        {
            switch (m)
            {
                case UBossMove.VgSteamLance:
                case UBossMove.MaEmberVolley:
                case UBossMove.VbFurnaceShot:
                case UBossMove.RgEmberVolley:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>The HUD line while a move winds up (shape words, never colour alone).</summary>
        public static string MoveLabel(UBossMove m)
        {
            switch (m)
            {
                case UBossMove.VgSteamJet: return "STEAM JET: STEP OUT OF THE LANE OR WARD IT";
                case UBossMove.VgStomp: return "PRESSURE STOMP: LEAVE THE RING";
                case UBossMove.VgScald: return "SCALDING SPRAY: WATCH THE RINGS";
                case UBossMove.VgSteamLance: return "STEAM LANCE: IT AIMS AT THE AIR, MOVE";
                case UBossMove.MaCharge: return "EMBER CHARGE: LURE IT ACROSS A COOLED PLATE";
                case UBossMove.MaBreath: return "FIRE BREATH: WARD IT OR LEAVE THE CONE";
                case UBossMove.MaTailSweep: return "TAIL SWEEP: LEAVE THE RING";
                case UBossMove.MaEmberRain: return "EMBER RAIN: BURNING GROUND";
                case UBossMove.MaEmberVolley: return "EMBER VOLLEY: SHE AIMS AT YOU";
                case UBossMove.MaCinderNova: return "CINDER NOVA: STAND ON A COOLED PLATE";
                case UBossMove.VbHammer: return "HAMMER: STEP OUT OF THE LANE";
                case UBossMove.VbSweep: return "ARM SWEEP: LEAVE THE RING";
                case UBossMove.VbFurnaceShot: return "FURNACE SHOT: MOVE OFF THE MARK";
                case UBossMove.VbShardFan: return "SHARD FAN: STAND BETWEEN THE LANES";
                case UBossMove.VbOverheat: return "OVERHEATING: GET CLEAR";
                case UBossMove.RgDisruption: return "RESONANCE DISRUPTION";
                case UBossMove.RgSlam: return "KILN HAMMER: STEP OUT OF THE LANE";
                case UBossMove.RgSweep: return "PEDESTAL SWEEP: LEAVE THE RING";
                case UBossMove.RgEmberVolley: return "EMBER VOLLEY: IT AIMS AT YOU";
                case UBossMove.RgFurnaces: return "FURNACES IGNITE: QUENCH THE VENTS";
                case UBossMove.RgBeamLanes: return "BEAM LANES: STAND IN A GAP";
                case UBossMove.RgRegulatorLanes: return "REGULATOR BEAMS: STAND IN A GAP";
                default: return "";
            }
        }

        // ================================================================== Matron cooled plates

        /// <summary>
        /// True when a charge from a to b passes over a plate (centre c, radius r): the closest point of the flat
        /// segment lies inside the plate. A quench needs the charge to cross the plate itself.
        /// </summary>
        public static bool ChargeCrossesPlate(Vector3 a, Vector3 b, Vector3 c, float r)
        {
            Vector3 fa = new Vector3(a.x, 0f, a.z), fb = new Vector3(b.x, 0f, b.z), fc = new Vector3(c.x, 0f, c.z);
            return CreatureFightRules.NearShotLine(fa, fb, fc, Mathf.Max(0f, r));
        }

        /// <summary>True when a flat point stands on a plate (safe from the cinder nova).</summary>
        public static bool OnPlate(Vector3 p, Vector3 plate, float r, float maxHeight)
        {
            float dx = p.x - plate.x, dz = p.z - plate.z;
            return dx * dx + dz * dz <= r * r && Mathf.Abs(p.y - plate.y) <= maxHeight;
        }
    }

    /// <summary>What decides a hit's multiplier right now (built by the fight every hit, no allocation).</summary>
    public struct UBossDamageState
    {
        public bool WindowOpen;
        public UBossPart WindowFocus;
        public float WindowMul;
        public bool Lured;
        public bool ShellCracked;
        public bool CoreOpen;
        public bool FurnaceShield;
        /// <summary>The struck part itself is already broken (a fractured seam).</summary>
        public bool PartBroken;
    }

    /// <summary>The Vaultbreaker's furnace heat: every move adds heat, the overheat empties it. Deterministic.</summary>
    public static class VaultHeat
    {
        public const float Max = 100f;

        /// <summary>Heat a move adds (phase 3 runs hotter, so the overheat comes sooner).</summary>
        public static float Add(UBossMove m, int phase)
        {
            float h;
            switch (m)
            {
                case UBossMove.VbHammer: h = 22f; break;
                case UBossMove.VbSweep: h = 18f; break;
                case UBossMove.VbFurnaceShot: h = 26f; break;
                case UBossMove.VbShardFan: h = 30f; break;
                default: h = 0f; break;
            }
            return h * (phase >= 2 ? 1.35f : 1f);
        }

        /// <summary>Seam fracture a seam can take before it breaks (it opens the core).</summary>
        public static float SeamThreshold(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 200f : (d == DifficultyLevel.Veteran ? 330f : 260f);
        }

        /// <summary>Fracture a living repair construct mends per second on every unbroken seam.</summary>
        public const float RepairPerSecond = 20f;
    }

    /// <summary>
    /// Wind-up (telegraph) and recovery (punish window) seconds per move and difficulty. Balanced and Veteran keep
    /// dangerous wind-ups at 0.75 s or longer; Story is longer on both. Signature moves leave the long windows.
    /// </summary>
    public static class UBossTiming
    {
        public const float MinTell = 0.75f;
        public const float MaxTell = 1.8f;
        public const float StoryMaxTell = 2.4f;
        public const float MinWindow = 1f;
        public const float MaxWindow = 3.6f;
        public const float StoryMaxWindow = 4.5f;

        public static void ResetStatics() { }

        public static float BaseTell(UBossMove m)
        {
            switch (m)
            {
                case UBossMove.VgSteamJet: return 1.1f;
                case UBossMove.VgStomp: return 0.95f;
                case UBossMove.VgScald: return 1f;
                case UBossMove.VgSteamLance: return 1.1f;
                case UBossMove.MaCharge: return 1.2f;
                case UBossMove.MaBreath: return 1.2f;
                case UBossMove.MaTailSweep: return 0.9f;
                case UBossMove.MaEmberRain: return 1f;
                case UBossMove.MaEmberVolley: return 1f;
                case UBossMove.MaCinderNova: return 1.8f;
                case UBossMove.VbHammer: return 1.05f;
                case UBossMove.VbSweep: return 0.95f;
                case UBossMove.VbFurnaceShot: return 1f;
                case UBossMove.VbShardFan: return 1.1f;
                case UBossMove.VbOverheat: return 1.4f;
                case UBossMove.RgDisruption: return 1.6f;
                case UBossMove.RgSlam: return 1.1f;
                case UBossMove.RgSweep: return 1f;
                case UBossMove.RgEmberVolley: return 1f;
                case UBossMove.RgFurnaces: return 1.4f;
                case UBossMove.RgBeamLanes: return 1.4f;
                case UBossMove.RgRegulatorLanes: return 1.2f;
                default: return 1f;
            }
        }

        /// <summary>The Balanced punish window after a move (0 means the move leaves no window of its own).</summary>
        public static float BaseRecovery(UBossMove m)
        {
            switch (m)
            {
                case UBossMove.VgSteamJet: return 2.2f;
                case UBossMove.VgSteamLance: return 1.4f;
                case UBossMove.MaBreath: return 1.6f;
                case UBossMove.MaCharge: return 1.2f;
                case UBossMove.MaCinderNova: return 3.5f;
                case UBossMove.VbHammer: return 1.4f;
                case UBossMove.VbShardFan: return 1.5f;
                case UBossMove.VbOverheat: return 3f;
                case UBossMove.RgDisruption: return 2.4f;
                case UBossMove.RgBeamLanes: return 2f;
                case UBossMove.RgRegulatorLanes: return 1.6f;
                default: return 0f;
            }
        }

        /// <summary>Seconds a move winds up for a difficulty (never under MinTell).</summary>
        public static float Telegraph(UBossMove m, DifficultyLevel d)
        {
            float b = BaseTell(m);
            switch (d)
            {
                case DifficultyLevel.Story: return Mathf.Clamp(b * 1.3f, MinTell, StoryMaxTell);
                case DifficultyLevel.Veteran: return Mathf.Clamp(b * 0.85f, MinTell, MaxTell);
                default: return Mathf.Clamp(b, MinTell, MaxTell);
            }
        }

        /// <summary>Seconds a punish window stays open for a difficulty (0 when the move leaves none).</summary>
        public static float Window(float baseSeconds, DifficultyLevel d)
        {
            if (!(baseSeconds > 0f)) return 0f;
            switch (d)
            {
                case DifficultyLevel.Story: return Mathf.Clamp(baseSeconds * 1.3f, MinWindow, StoryMaxWindow);
                case DifficultyLevel.Veteran: return Mathf.Clamp(baseSeconds * 0.8f, MinWindow, MaxWindow);
                default: return Mathf.Clamp(baseSeconds, MinWindow, MaxWindow);
            }
        }

        public static float Recovery(UBossMove m, DifficultyLevel d)
        {
            return Window(BaseRecovery(m), d);
        }

        /// <summary>Seconds of plain fighting between moves (shorter in later phases, never under 1.1 s).</summary>
        public static float MoveGap(int phase, DifficultyLevel d, bool pressured)
        {
            float g = 2.3f - 0.35f * Mathf.Clamp(phase, 0, 2);
            if (pressured) g *= 0.8f;
            if (d == DifficultyLevel.Story) g *= 1.35f;
            else if (d == DifficultyLevel.Veteran) g *= 0.85f;
            return Mathf.Max(1.1f, g);
        }

        /// <summary>Base windows of the special openings (Balanced seconds).</summary>
        public const float QuenchWindow = 3.2f;
        public const float SeamWindow = 2.6f;
        public const float StarvedWindow = 3f;
    }

    /// <summary>
    /// A punish window: opens at a time for some seconds with a damage multiplier on a named part. Opening it again
    /// while open only ever extends it (a window can never be shortened by a second trigger). Pure.
    /// </summary>
    public sealed class PunishWindow04E
    {
        public float OpenedAt { get; private set; }
        public float Until { get; private set; }
        public float Multiplier { get; private set; }
        public UBossPart Focus { get; private set; }
        public string Label { get; private set; } = "";
        /// <summary>How many windows were opened over the fight (debug page and tests).</summary>
        public int Count { get; private set; }

        public void Open(float now, float seconds, float multiplier, UBossPart focus, string label)
        {
            if (!(seconds > 0f)) return;
            float until = now + seconds;
            if (IsOpen(now))
            {
                if (until > Until) Until = until;
                if (multiplier > Multiplier) Multiplier = multiplier;
                Focus = focus;
                Label = label ?? Label;
                return;
            }
            OpenedAt = now;
            Until = until;
            Multiplier = multiplier;
            Focus = focus;
            Label = label ?? "";
            Count++;
        }

        public bool IsOpen(float now)
        {
            return now < Until && now >= OpenedAt;
        }

        /// <summary>1 when just opened, falling to 0 as it closes.</summary>
        public float Remaining01(float now)
        {
            float len = Until - OpenedAt;
            if (len <= 0f || !IsOpen(now)) return 0f;
            return Mathf.Clamp01((Until - now) / len);
        }

        public void Close()
        {
            Until = OpenedAt;
        }
    }

    /// <summary>
    /// The declared finite reinforcement budget of one fight or mechanism: never more than Declared in total and
    /// MaxAlive at once, each batch fires once, a death never refunds a slot and a closed ledger admits nothing.
    /// </summary>
    public sealed class AddLedger04E
    {
        public readonly int Declared;
        public readonly int MaxAlive;
        public int Spawned { get; private set; }
        public int Dead { get; private set; }
        public bool Closed { get; private set; }
        private readonly bool[] _fired;

        public AddLedger04E(int declared, int maxAlive, int batches)
        {
            Declared = Mathf.Max(0, declared);
            MaxAlive = Mathf.Max(0, maxAlive);
            _fired = new bool[Mathf.Max(0, batches)];
        }

        public int Alive { get { return Spawned - Dead; } }
        public int Remaining { get { return Declared - Spawned; } }

        /// <summary>Marks a batch as fired; true only the first time (a phase reopening cannot fire it twice).</summary>
        public bool TryFireBatch(int index)
        {
            if (Closed || index < 0 || index >= _fired.Length || _fired[index]) return false;
            _fired[index] = true;
            return true;
        }

        public bool BatchFired(int index)
        {
            return index >= 0 && index < _fired.Length && _fired[index];
        }

        /// <summary>How many of requested may spawn now.</summary>
        public int Allow(int requested)
        {
            if (Closed || requested <= 0) return 0;
            int n = Mathf.Min(requested, Declared - Spawned);
            n = Mathf.Min(n, MaxAlive - Alive);
            return Mathf.Max(0, n);
        }

        public void NoteSpawned(int n)
        {
            if (n > 0) Spawned = Mathf.Min(Declared, Spawned + n);
        }

        /// <summary>Records confirmed deaths (never more than spawned; a despawn is not a death).</summary>
        public void NoteDead(int n)
        {
            if (n > 0) Dead = Mathf.Min(Spawned, Dead + n);
        }

        public void Close()
        {
            Closed = true;
        }
    }

    /// <summary>A one-shot latch: the victory (and its rewards) commits exactly once, however many callbacks arrive.</summary>
    public sealed class VictoryLatch04E
    {
        public bool Committed { get; private set; }

        public bool TryCommit()
        {
            if (Committed) return false;
            Committed = true;
            return true;
        }
    }

    /// <summary>Where durable rewards go (the Builds reward ledger at runtime, a fake in tests).</summary>
    public interface IBossRewardSink04E
    {
        bool Has(string token);
        bool Grant(string rewardId, string token, string source);
    }

    /// <summary>One first-win entitlement: the reward definition and its once-only token.</summary>
    public struct UBossGrant
    {
        public string Reward;
        public string Token;
    }

    /// <summary>
    /// First-win rewards exactly as the 04E interface states: valve_guardian grants uh_descent_piece; cinder_matron
    /// uh_matron_aspect and (token + ":gear") uh_matron_gear_choice; vaultbreaker uh_vault_module; kiln_regent
    /// uh_regent_pinnacle and (token + ":bond") uh_bond_awaken. Quest Claim steps use the same tokens, so a token that
    /// already exists is never granted twice. Pure.
    /// </summary>
    public static class UnderBossRewards
    {
        public const int MaxGrants = 2;

        /// <summary>Fills into (length 2 or more) with the boss's grants and returns how many.</summary>
        public static int GrantsFor(string bossId, UBossGrant[] into)
        {
            if (into == null || into.Length < MaxGrants || string.IsNullOrEmpty(bossId)) return 0;
            string t = UnderBossIds.Token(bossId);
            switch (bossId)
            {
                case UnderBossIds.ValveGuardian:
                    into[0] = new UBossGrant { Reward = "uh_descent_piece", Token = t };
                    return 1;
                case UnderBossIds.CinderMatron:
                    into[0] = new UBossGrant { Reward = "uh_matron_aspect", Token = t };
                    into[1] = new UBossGrant { Reward = "uh_matron_gear_choice", Token = t + ":gear" };
                    return 2;
                case UnderBossIds.Vaultbreaker:
                    into[0] = new UBossGrant { Reward = "uh_vault_module", Token = t };
                    return 1;
                case UnderBossIds.KilnRegent:
                    into[0] = new UBossGrant { Reward = "uh_regent_pinnacle", Token = t };
                    into[1] = new UBossGrant { Reward = "uh_bond_awaken", Token = t + ":bond" };
                    return 2;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Grants every first-win entitlement the sink does not hold yet. Returns how many were newly granted; failed
        /// counts tokens the sink neither held nor accepted (they stay retryable). Existing tokens are left untouched.
        /// </summary>
        public static int GrantFirstWin(string bossId, IBossRewardSink04E sink, string source, out int failed)
        {
            failed = 0;
            if (sink == null) return 0;
            UBossGrant[] g = new UBossGrant[MaxGrants];
            int n = GrantsFor(bossId, g);
            int fresh = 0;
            for (int i = 0; i < n; i++)
            {
                if (sink.Has(g[i].Token)) continue;
                if (sink.Grant(g[i].Reward, g[i].Token, source)) fresh++;
                else failed++;
            }
            return fresh;
        }

        /// <summary>True once the boss's main first-win token exists.</summary>
        public static bool EverDefeated(string bossId, IBossRewardSink04E sink)
        {
            return sink != null && UnderBossIds.Has(bossId) && sink.Has(UnderBossIds.Token(bossId));
        }
    }

    /// <summary>What the arena boundary decided this frame.</summary>
    public enum ArenaVerdict
    {
        Inside = 0,
        /// <summary>Beyond the boundary: the HUD counts down.</summary>
        Warning = 1,
        /// <summary>Out for too long, or far out at once: the fight resets.</summary>
        Left = 2,
        /// <summary>Held against the seal long enough: a deliberate retreat.</summary>
        Withdraw = 3
    }

    /// <summary>
    /// The arena boundary rules: a short warning count when the player crosses the boundary, a reset after
    /// LeaveSeconds outside (or at once far outside or far above or below), and a deliberate withdrawal by holding
    /// against the closed seal for WithdrawSeconds. Pure.
    /// </summary>
    public sealed class ArenaWatch04E
    {
        public const float LeaveMargin = 3f;
        public const float LeaveSeconds = 6f;
        public const float HardLeaveMargin = 40f;
        public const float VerticalLimit = 25f;
        public const float WithdrawSeconds = 2.5f;
        public const float WithdrawRadius = 2.4f;
        /// <summary>The player must be this far inside the boundary for the fight to start.</summary>
        public const float StartVertical = 8f;

        public float OutsideFor { get; private set; }
        public float WithdrawFor { get; private set; }

        /// <summary>True when the player stands inside the boundary at about the arena's floor height.</summary>
        public static bool InsideToStart(float flatDistance, float dy, float radius)
        {
            return flatDistance <= radius && Mathf.Abs(dy) < StartVertical;
        }

        public ArenaVerdict Tick(float dt, float flatDistance, float dy, float radius, bool pressingSeal)
        {
            float step = dt > 0f && !float.IsNaN(dt) ? dt : 0f;
            if (pressingSeal)
            {
                WithdrawFor += step;
                if (WithdrawFor >= WithdrawSeconds) return ArenaVerdict.Withdraw;
            }
            else WithdrawFor = 0f;
            bool far = flatDistance > radius + HardLeaveMargin || Mathf.Abs(dy) > VerticalLimit;
            if (far) return ArenaVerdict.Left;
            if (flatDistance > radius + LeaveMargin)
            {
                OutsideFor += step;
                return OutsideFor >= LeaveSeconds ? ArenaVerdict.Left : ArenaVerdict.Warning;
            }
            OutsideFor = 0f;
            return ArenaVerdict.Inside;
        }

        /// <summary>Seconds left before a reset while warning.</summary>
        public float WarningLeft { get { return Mathf.Max(0f, LeaveSeconds - OutsideFor); } }

        public void Reset()
        {
            OutsideFor = 0f;
            WithdrawFor = 0f;
        }
    }
}
