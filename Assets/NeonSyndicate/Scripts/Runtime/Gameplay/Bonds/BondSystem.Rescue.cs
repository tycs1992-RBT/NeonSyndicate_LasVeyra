using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The rescue sequence behind the quest Rescue step: BeginRescue builds the site (clamps, the trapped creature's
    /// body, the warning and tuning interactions) and returns a handle the step polls (Objective, ClampsLeft, Progress,
    /// Done, Failed, Result, Abort). After tuning, the naming card asks for a name (or keeps the default); the rescued
    /// creature joins the roster as Rescued. The first companion becomes active on the very body the player just freed
    /// and follows at once; a later rescue keeps a trusted (Bonded or better) or fused companion active and sends the
    /// newcomer home to rest, switchable on the BONDS page or at a care station (BondRules.RescueKeepsActive). An
    /// archetype already owned completes immediately with the existing record, so a replay never mints a second
    /// companion or any reward.
    /// </summary>
    public sealed partial class BondSystem
    {
        private CompanionRescueSite _rescue;
        private RescueHandle _rescueHandle;
        private int _rescueSeed;
        private float _separationGrace;

        /// <summary>True while a rescue site is active.</summary>
        public bool RescueActive { get { return _rescue != null; } }

        /// <summary>Starts a rescue at a site: the trapped creature, its clamps and its warning behavior.</summary>
        public RescueHandle BeginRescue(string archetype, Vector3 site, float yaw, int clamps, Vector3[] clampPoints)
        {
            RescueHandle h = new RescueHandle { Archetype = archetype ?? "", Position = site };
            if (!BondIds.Has(archetype))
            {
                h.Failed = true;
                h.Objective = "Unknown companion kind '" + archetype + "'.";
                return h;
            }
            CompanionRecord have = FindArchetype(archetype);
            if (have != null)
            {
                h.Done = true;
                h.Result = have;
                h.Progress = 1f;
                h.Objective = NameOf(have) + " is already bonded with you.";
                return h;
            }
            if (!MathUtil.IsFinite(site))
            {
                h.Failed = true;
                h.Objective = "The rescue site is not available.";
                return h;
            }
            AbortRescue();
            int predicted = Mathf.Max(1, _data.NextUid);
            while (Find(predicted) != null) predicted++;
            _rescueSeed = BondRoster.SeedFor(predicted, archetype);
            try
            {
                _rescue = CompanionRescueSite.Begin(transform, archetype, site, yaw, Mathf.Clamp(clamps, 0, 6), clampPoints, h, _rescueSeed, CompanionLooks.DetailFor(archetype, Settings.Graphics));
            }
            catch (System.Exception e)
            {
                Log.Exception("Bonds rescue", e);
                _rescue = null;
                h.Failed = true;
                h.Objective = "The rescue could not be set up (" + e.Message + ").";
                return h;
            }
            _rescue.NamingNeeded = OnRescueNaming;
            _rescueHandle = h;
            RescueHandle mine = h;
            h.Abort = () => { if (_rescueHandle == mine) AbortRescue(); };
            Log.Info("Bonds: rescue of a " + BondIds.Title(archetype) + " started (" + Mathf.Clamp(clamps, 0, 6) + " clamps).");
            return h;
        }

        private void OnRescueNaming(CompanionRescueSite site)
        {
            if (site == null || site != _rescue || _naming == null) return;
            string title = BondIds.Title(site.Profile.Archetype);
            string def = BondIds.DefaultName(site.Profile.Archetype);
            _naming.Request("You rescued a " + title, "It is calm now, and it answers to " + def + ". Give it a name of your own, or keep that one.", def, "", n => CompleteRescue(site, n));
        }

        private void CompleteRescue(CompanionRescueSite site, string name)
        {
            if (site == null || site != _rescue) return;
            RescueHandle h = _rescueHandle;
            string archetype = site.Profile.Archetype;
            CompanionRecord prev = Active;
            CompanionRecord r = GrantAt(archetype, name, "rescue", BondIds.RankStray);
            if (r == null)
            {
                if (h != null)
                {
                    h.Failed = true;
                    h.Objective = "The bond could not be recorded.";
                }
                AbortRescue();
                return;
            }
            r.Seed = _rescueSeed;
            r.State = (int)CompanionState.Following;
            bool hasPrev = prev != null && prev != r;
            bool prevFused = hasPrev && prev.State == (int)CompanionState.Fused;
            // A trusted (Bonded or better) or fused companion stays active; the rank 0 newcomer goes home to rest.
            bool keepPrev = BondRules.RescueKeepsActive(hasPrev, hasPrev ? prev.Rank : 0, prevFused);
            if (keepPrev)
            {
                r.State = (int)CompanionState.Resting;
                Say(NameOf(r) + " heads home to rest. " + NameOf(prev) + " stays with you: switch companions on the BONDS page or at a care station.", 6f, false);
            }
            else
            {
                if (hasPrev && prev.State != (int)CompanionState.Downed)
                {
                    prev.State = (int)CompanionState.Resting;
                    Say(NameOf(prev) + " heads home to rest. Switch companions on the BONDS page.", 4f, false);
                }
                DespawnBody();
                BondRoster.SetActive(_data, r.Uid);
            }
            CompanionController b = site.TakeBody();
            if (b != null)
            {
                if (keepPrev) Destroy(b.gameObject);
                else
                {
                    b.transform.SetParent(transform, true);
                    b.Uid = r.Uid;
                    b.BuiltEvolution = 0;
                    b.BuiltSpec = "";
                    b.Trail = _trail;
                    b.DisplayName = NameOf(r);
                    b.Mode = CompanionMode.Follow;
                    b.SetAlert(0f);
                    _body = b;
                }
            }
            _rescue = null;
            _rescueHandle = null;
            if (h != null)
            {
                h.Result = r;
                h.Done = true;
                h.Progress = 1f;
                h.ClampsLeft = 0;
                h.Objective = keepPrev ? NameOf(r) + " is bonded with you and rests at home until you bring it along." : NameOf(r) + " is bonded with you and follows you.";
            }
            if (keepPrev)
            {
                if (Game.Hud != null) Game.Hud.Banner("BONDED: " + NameOf(r).ToUpperInvariant(), BondIds.Title(archetype) + ". Resting at home: bring it along from the BONDS page or at a care station. " + NameOf(prev) + " stays at your side.", ProfileOf(r).Accent, 6f);
                Underheart04ESounds.Play2D(UhSfx.BondChime, 1f, 1f);
                ReportStatus();
                return;
            }
            string keys = Pad.Connected && Pad.LastUsed
                ? Pad.Tag(PadButton.LB) + "+D-pad down: follow or wait (hold to call). " + Pad.Tag(PadButton.LB) + "+D-pad up: assist."
                : "N: follow or wait (hold N to call). U: assist.";
            if (Game.Hud != null) Game.Hud.Banner("BONDED: " + NameOf(r).ToUpperInvariant(), BondIds.Title(archetype) + ". " + keys, ProfileOf(r).Accent, 6f);
            Underheart04ESounds.Play2D(UhSfx.BondChime, 1f, 1f);
            ShowControlHint(45f);
            ReportStatus();
        }

        /// <summary>Cancels the active rescue (the quest's cleanup calls it through the handle). Nothing is granted.</summary>
        private void AbortRescue()
        {
            if (_rescue != null)
            {
                try { _rescue.Abort(); }
                catch (System.Exception e) { Log.Exception("Bonds rescue abort", e); }
            }
            _rescue = null;
            if (_rescueHandle != null && !_rescueHandle.Done)
            {
                _rescueHandle.Failed = true;
                _rescueHandle.Objective = "Rescue cancelled.";
            }
            _rescueHandle = null;
            CompanionBudget.Rescues = 0;
        }

        private void TickRescue()
        {
            if (_rescue == null && _rescueHandle != null && !_rescueHandle.Done && !_rescueHandle.Failed)
            {
                // The site was destroyed from outside (a scene reset): the handle reports it instead of hanging.
                _rescueHandle.Failed = true;
                _rescueHandle.Objective = "Rescue interrupted.";
                _rescueHandle = null;
            }
        }
    }
}
