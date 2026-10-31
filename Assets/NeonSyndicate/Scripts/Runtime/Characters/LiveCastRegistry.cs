using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// One live character: an owner-approved imported humanoid visual in the original ImportedModel04OD row format (so the
    /// same pure binding rules apply) plus how it binds. Rows are written by the Editor generator (LiveCastSetup) on each
    /// machine and are never committed or edited by hand.
    /// </summary>
    [Serializable]
    public sealed class LiveCastRow
    {
        /// <summary>The visual row: identity, prefab, bone mapping, passive bones, sockets and art proof.</summary>
        public ImportedModel04OD Model = new ImportedModel04OD();
        /// <summary>When set, only the exact live campaign cast actor with this id receives the visual (Dante: "dante").</summary>
        public string CastId = "";
        /// <summary>Procedural costume layer names hidden while the visual shows (never the held tool).</summary>
        public string[] HiddenCostume = new string[0];
        /// <summary>A short name for the Console ("Malik Veyr H9", "Dante Rook H9", "drop-in Corin Pell").</summary>
        public string Label = "";
        /// <summary>Where the row came from ("builtin:malik-h9", "builtin:dante-h9", "dropin:Corin Pell").</summary>
        public string Origin = "";
        /// <summary>The generator's fingerprint of the recipe, the render pipeline, the source files and their import
        /// settings. A different fingerprint makes the generator prepare the art again.</summary>
        public string Stamp = "";
        /// <summary>Crowd rows only (LiveCrowdRules): the NpcRole bits this body serves instead of one display name; 0 for a named row.</summary>
        public int CrowdRoles;
        /// <summary>Crowd rows only: the sex of the body, matched against Appearance.Female so a man never wears a woman's body or the other way round.</summary>
        public bool CrowdFemale;
        /// <summary>Crowd rows only: the age class of the body (livecast.json "age"), matched by LiveCrowdRules.AgeFits.</summary>
        public AgeClass CrowdAge;
        /// <summary>Crowd rows only: transform paths (below the prefab root) of the armour renderers that hide on plain roles.</summary>
        public string[] CrowdArmourPaths = new string[0];
        /// <summary>Study bodies: the live adapter measures the skull and fits procedural hair, beards and headwear (false for a body that brings its own hair).</summary>
        public bool CrowdHair;
        /// <summary>A named cast member on a study body (CastId set): the cast palette, hidden armour, the person's own hair and headwear, the held tool kept.</summary>
        public bool CastStudy;

        /// <summary>Per-skin weighted-bone tables of the prefab, measured once by the first live instance (not saved).</summary>
        [NonSerialized] internal bool[][] ReferencedCache;
        /// <summary>True once a live instance inspected the prefab's transforms and components (crowd rows inspect once per session, not per pooled body).</summary>
        [NonSerialized] internal bool PrefabChecked;
        /// <summary>True once the Console line for a crowd row was written this session (one line per body, not one per person).</summary>
        [NonSerialized] internal bool Announced;
        /// <summary>The cached result of LiveCastRules.CanGoLive for this row (not saved).</summary>
        [NonSerialized] internal int ContractState;
        /// <summary>The cached refusal reason when ContractState is negative (not saved).</summary>
        [NonSerialized] internal string ContractReason;
    }

    /// <summary>
    /// The live character registry, generated at Resources/LiveCast/LiveCastRegistry.asset by LiveCastSetup (git-ignored,
    /// rebuilt on every machine on the first editor load). Owner-approved imported humanoids replace only the procedural
    /// body's appearance; identity, saves, weapons, collision, AI, dialogue and the procedural pose stay authoritative.
    /// </summary>
    public sealed class LiveCastRegistry : ScriptableObject
    {
        /// <summary>The Resources path the runtime loads.</summary>
        public const string ResourcePath = "LiveCast/LiveCastRegistry";
        /// <summary>The project path the Editor generator writes.</summary>
        public const string AssetPath = "Assets/NeonSyndicate/Resources/LiveCast/LiveCastRegistry.asset";

        /// <summary>Master switch (the generator always writes true; set false in the Inspector to test the procedural cast).</summary>
        public bool Enabled = true;
        /// <summary>True when the materials were generated for URP (a pipeline switch makes the generator rebuild them).</summary>
        public bool BuiltForUrp;
        /// <summary>The generator recipe version the rows were written with.</summary>
        public string Recipe = "";
        /// <summary>The live rows, drop-in folders first (a drop-in replaces a built-in with the same identity).</summary>
        public LiveCastRow[] Rows = new LiveCastRow[0];

        private static LiveCastRegistry _loaded;
        private static bool _attempted;

        /// <summary>Forgets the cached registry (Enter Play Mode without a domain reload starts clean).</summary>
        public static void ResetStatics()
        {
            _loaded = null;
            _attempted = false;
        }

        /// <summary>The generated registry, loaded once per play session, or null when none was generated.</summary>
        public static LiveCastRegistry Loaded
        {
            get
            {
                if (!_attempted)
                {
                    _attempted = true;
                    try { _loaded = Resources.Load<LiveCastRegistry>(ResourcePath); }
                    catch (Exception e)
                    {
                        _loaded = null;
                        Log.Warn("Live character registry could not be read, the procedural characters stay: " + e.Message);
                    }
                    // ScriptableObject assets survive when domain reload is disabled. Their nonserialized measurements
                    // and verdicts belong to one Play session, never to the next one after art or settings changed.
                    if (_loaded != null && _loaded.Rows != null)
                        foreach (LiveCastRow row in _loaded.Rows)
                        {
                            if (row == null) continue;
                            row.ReferencedCache = null;
                            row.PrefabChecked = false;
                            row.Announced = false;
                            row.ContractState = 0;
                            row.ContractReason = null;
                        }
                }
                return _loaded != null && _loaded.Enabled && _loaded.Rows != null ? _loaded : null;
            }
        }

        /// <summary>True when an enabled row serves the player.</summary>
        public bool HasPlayerRow
        {
            get { return LiveCastRules.Find(Rows, LiveCastRules.PlayerIdentity, "") >= 0; }
        }

        /// <summary>True when an enabled NPC row without a cast id exists (it can match any NPC by display name).</summary>
        public bool HasOpenNpcRow
        {
            get
            {
                foreach (LiveCastRow row in Rows)
                    if (row != null && row.Model != null && row.Model.Enabled && string.IsNullOrEmpty(row.CastId)
                        && row.Model.Identity != null && row.Model.Identity.StartsWith(LiveCastRules.NpcPrefix, StringComparison.Ordinal)) return true;
                return false;
            }
        }

        /// <summary>True when an enabled crowd row exists (a body that serves NPCs by role, LiveCrowdRules).</summary>
        public bool HasCrowdRow
        {
            get
            {
                foreach (LiveCastRow row in Rows)
                    if (row != null && row.Model != null && row.Model.Enabled && row.CrowdRoles != 0 && string.IsNullOrEmpty(row.CastId)) return true;
                return false;
            }
        }

        /// <summary>The first enabled crowd row that serves an NPC of this role, sex and build (LiveCrowdRules.FindCrowd), or null.</summary>
        public LiveCastRow FindCrowd(NpcRole role, bool female, bool creature, bool castActor, float height, float bulk)
        {
            return FindCrowd(role, female, AgeClass.Adult, creature, castActor, height, bulk);
        }

        /// <summary>FindCrowd for a person of an age class (a child takes only a child body, an elder an elder or a grown-up one).</summary>
        public LiveCastRow FindCrowd(NpcRole role, bool female, AgeClass age, bool creature, bool castActor, float height, float bulk)
        {
            int index = LiveCrowdRules.FindCrowd(Rows, role, female, age, creature, castActor, height, bulk);
            return index >= 0 ? Rows[index] : null;
        }

        /// <summary>True when an enabled row is bound to this campaign cast id.</summary>
        public bool HasCastRow(string castId)
        {
            if (string.IsNullOrEmpty(castId)) return false;
            foreach (LiveCastRow row in Rows)
                if (row != null && row.Model != null && row.Model.Enabled && row.CastId == castId) return true;
            return false;
        }

        /// <summary>The row that serves an actor (LiveCastRules.Find), or null.</summary>
        public LiveCastRow Find(string identity, string actorCastId)
        {
            int index = LiveCastRules.Find(Rows, identity, actorCastId);
            return index >= 0 ? Rows[index] : null;
        }

        /// <summary>LiveCastRules.CanGoLive for a row, evaluated once per loaded registry.</summary>
        public static bool Contract(LiveCastRow row, out string reason)
        {
            reason = "";
            if (row == null) { reason = "The live row is empty."; return false; }
            if (row.ContractState == 0)
            {
                string why;
                row.ContractState = LiveCastRules.CanGoLive(row, out why) ? 1 : -1;
                row.ContractReason = why;
            }
            reason = row.ContractReason ?? "";
            return row.ContractState > 0;
        }
    }
}
