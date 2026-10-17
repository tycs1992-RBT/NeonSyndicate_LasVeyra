using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Where resting companions live: bonded companions that are not out with the player rest at Bondkeeper's Refuge,
    /// one at the habitat enclosure and one on the perch, and are shown there (idle, watching the player) while the
    /// player is within about 45 m of the refuge. They are display bodies only: no assists, never hittable, counted in
    /// the companion body budget, removed when the player leaves or when one of them is brought along.
    /// </summary>
    public sealed partial class BondSystem
    {
        private sealed class HomeBody
        {
            public int Uid;
            public string Anchor;
            public Vector3 Position;
            public float Yaw;
            public CompanionController Body;
            public CompanionBodyFactory.Job Job;
        }

        private static readonly string[] HomeAnchors = { "habitat", "perch" };
        private readonly List<HomeBody> _home = new List<HomeBody>();
        private float _homeScan;

        private void TickHome()
        {
            for (int i = 0; i < _home.Count; i++) MaterializeHome(_home[i]);
            _homeScan -= Time.unscaledDeltaTime;
            if (_homeScan > 0f) return;
            _homeScan = 1f;
            PlayerController pl = Game.Player;
            VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(VenueIds04E.Bondkeeper) : null;
            bool near = Game.IsPlaying && pl != null && !QaTools.Busy && v != null && v.Available && v.View != null &&
                MathUtil.FlatDistance(pl.transform.position, v.Entrance) < 45f && WorldBounds.SameLayer(pl.transform.position, v.Entrance);
            if (!near)
            {
                ClearHome();
                return;
            }
            // Drop bodies whose companion is no longer resting at home.
            for (int i = _home.Count - 1; i >= 0; i--)
            {
                CompanionRecord r = Find(_home[i].Uid);
                if (r != null && r != Active && r.State == (int)CompanionState.Resting) continue;
                DropHome(i);
            }
            CompanionRecord active = Active;
            for (int i = 0; i < _data.Companions.Count && _home.Count < HomeAnchors.Length; i++)
            {
                CompanionRecord r = _data.Companions[i];
                if (r == active || !BondIds.Has(r.Archetype) || r.State != (int)CompanionState.Resting || HasHome(r.Uid)) continue;
                string anchor = FreeAnchor(r.Archetype == BondIds.StormSkimmer ? "perch" : "habitat");
                if (anchor == null) break;
                Vector3 p = v.View.WorldPoint(anchor);
                if ((p - v.View.transform.position).sqrMagnitude < 0.04f) continue;
                int evo = BondRules.EvolutionStage(r.Rank, r.Evolution);
                HomeBody h = new HomeBody { Uid = r.Uid, Anchor = anchor, Position = p, Yaw = v.View.AnchorYaw(anchor) };
                h.Job = CompanionBodyFactory.Request(r.Archetype, r.Seed, evo, r.Specialization ?? "", CompanionLooks.DetailFor(r.Archetype, GraphicsPreset.Low));
                _home.Add(h);
            }
            CompanionBudget.Homes = CountHomeBodies();
        }

        private void MaterializeHome(HomeBody h)
        {
            if (h.Body != null || h.Job == null || !h.Job.Finished) return;
            CompanionBodyFactory.Job job = h.Job;
            h.Job = null;
            if (job.Failed || job.Body == null) return;
            CompanionRecord r = Find(h.Uid);
            if (r == null) return;
            try
            {
                CompanionController c = CompanionController.Create(job.Body, ProfileOf(r), h.Position, h.Yaw, transform);
                c.Uid = -r.Uid;
                c.DisplayName = NameOf(r);
                c.WaitPoint = h.Position;
                c.Mode = CompanionMode.Wait;
                h.Body = c;
                CompanionBudget.Homes = CountHomeBodies();
            }
            catch (System.Exception e)
            {
                Log.Exception("Bonds home body", e);
            }
        }

        private bool HasHome(int uid)
        {
            for (int i = 0; i < _home.Count; i++) if (_home[i].Uid == uid) return true;
            return false;
        }

        private string FreeAnchor(string preferred)
        {
            bool prefTaken = false;
            for (int i = 0; i < _home.Count; i++) if (_home[i].Anchor == preferred) prefTaken = true;
            if (!prefTaken) return preferred;
            for (int a = 0; a < HomeAnchors.Length; a++)
            {
                bool taken = false;
                for (int i = 0; i < _home.Count; i++) if (_home[i].Anchor == HomeAnchors[a]) taken = true;
                if (!taken) return HomeAnchors[a];
            }
            return null;
        }

        private int CountHomeBodies()
        {
            int n = 0;
            for (int i = 0; i < _home.Count; i++) if (_home[i].Body != null) n++;
            return n;
        }

        /// <summary>Removes a companion's home body at once (it is being brought along: never two bodies of one companion).</summary>
        private void RemoveHome(int uid)
        {
            for (int i = _home.Count - 1; i >= 0; i--) if (_home[i].Uid == uid) DropHome(i);
            CompanionBudget.Homes = CountHomeBodies();
        }

        private void DropHome(int index)
        {
            HomeBody h = _home[index];
            if (h.Job != null) h.Job.Cancelled = true;
            if (h.Body != null) Destroy(h.Body.gameObject);
            _home.RemoveAt(index);
        }

        private void ClearHome()
        {
            for (int i = _home.Count - 1; i >= 0; i--) DropHome(i);
            CompanionBudget.Homes = 0;
        }
    }
}
