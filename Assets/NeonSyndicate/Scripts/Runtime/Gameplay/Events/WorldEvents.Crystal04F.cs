using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>04F selection, durable cooldowns and reward receipts inside the existing world event director.</summary>
    public sealed partial class WorldEvents
    {
        private readonly CityRewardLedger04F _cityRewardTokens04F = new CityRewardLedger04F();
        private float _cityNextAttempt04F = 22f;
        private bool _citySavePending04F;
        public bool CrystalCityRunning04F
        {
            get
            {
                foreach (EventInstance e in _active) if (!e.Done && CityCombatRules04F.IsCityKind(e.T.Kind)) return true;
                return false;
            }
        }

        /// <summary>A nearby, safe-to-observe dispute gets one deterministic early opportunity, then long quiet gaps.</summary>
        private bool TrySpawnCrystal04F()
        {
            if (_clock < _cityNextAttempt04F || CountRunning() > 0 || CrystalCityRunning04F || Blocked() || PlayerUnderground()) return false;
            if (Game.Root == null || !Game.Root.FreeRoam || Game.Player == null || Game.Player.IsDead || Game.Player.InVehicle) return false;
            if (Game.Inner != null && Game.Inner.IsInner) return false;
            if (Game.Wanted != null && Game.Wanted.Stars > 0) return false;
            if (Game.Phone != null && Game.Phone.IsOpen) return false;
            if (Game.Territories != null && Game.Territories.HasActiveEncounter) return false;
            if (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null) return false;
            if (QaTools.Busy) return false;
            _cityNextAttempt04F = _clock + 20f;
            EventTemplate chosen = null;
            int least = int.MaxValue;
            foreach (EventTemplate candidate in Templates)
            {
                if (!CityCombatRules04F.IsCityKind(candidate.Kind) || WhyNot(candidate) != null) continue;
                if(candidate.Id=="ev_04ob_threshold_watch" || candidate.Id=="ev_04oc_azure_watch")
                {
                    if(Game.Layout==null || !Game.Layout.Gothic04OB || candidate.Id=="ev_04oc_azure_watch" && !Game.Layout.Fractured04OC)continue;
                    Vector3 at=candidate.Id=="ev_04oc_azure_watch"?Game.Layout.AzureGlade04OC:Game.Layout.RealmChallenge04OC;
                    float distance=MathUtil.FlatDistance(Game.PlayerPosition,at);if(distance<35f || distance>95f)continue;
                }
                int seen = SeenCount(candidate.Id);
                int order = candidate.Kind == "CrystalDispute" ? 0 : candidate.Kind == "DrakeRescue" ? 1 : 2;
                int score = (candidate.Id=="ev_04ob_threshold_watch" || candidate.Id=="ev_04oc_azure_watch") && seen==0 ? -100 : seen * 3 + order;
                if (score >= least) continue;
                chosen = candidate;
                least = score;
            }
            if (chosen == null || !Spawn(chosen, false)) return false;
            _cityNextAttempt04F = _clock + CityCombatRules04F.EventCooldown;
            _nextSpawn = Mathf.Max(_nextSpawn, _clock + 130f);
            return true;
        }

        /// <summary>The observation court never sits on a named quest giver, shop entrance or populated doorstep.</summary>
        private static bool CrystalPlacementSafe04F(Vector3 candidate)
        {
            if (PedManager.ExcludedVenue04C(candidate) || WorldBounds.IsUnderground(candidate)) return false;
            if (LocalQuiet.Blocks(candidate)) return false;
            foreach (NpcController actor in NpcController.All)
            {
                if (actor == null || actor.IsDead) continue;
                if ((actor.Persistent || !string.IsNullOrEmpty(actor.MissionTag) || actor.Role == NpcRole.Clerk || actor.Role == NpcRole.Companion)
                    && MathUtil.FlatDistance(actor.transform.position, candidate) < 24f) return false;
            }
            foreach (Interactable doorway in Interactable.All)
            {
                if (doorway == null || !doorway.isActiveAndEnabled) continue;
                if (MathUtil.FlatDistance(doorway.transform.position, candidate) < 14f) return false;
            }
            return true;
        }

        private void RetryCrystalCandidates04F(EventTemplate template, Vector3 player, float min, float max, bool force)
        {
            QuestPlaces.AnchorCandidates(Game.Layout, "Any", player, min, max, _cands);
            for (int i = _cands.Count - 1; i >= 0; i--)
            {
                Vector3 candidate = _cands[i];
                bool invalid = Game.Layout.IsInsideBuilding(candidate, 0.5f) || !CrystalPlacementSafe04F(candidate)
                    || (!force && !EventSelector.DistrictAllowed(template, CityLayout.DistrictAt(candidate)));
                foreach (EventInstance other in _active)
                    if (MathUtil.FlatDistance(other.Pos, candidate) < 60f) { invalid = true; break; }
                if (invalid) _cands.RemoveAt(i);
            }
        }

        private void AttachCrystalReceipt04F(EventInstance instance, int occurrence)
        {
            string token = Game.Layout!=null && Game.Layout.Fractured04OC && (instance.T.Id=="ev_04oc_azure_watch" || instance.T.Id=="ev_04ob_threshold_watch")
                ? instance.T.Id + ":first-win-04oc" : instance.T.Id + ":" + occurrence;
            instance.RewardHandler = reward =>
            {
                if (!instance.PlayerAccepted || !_cityRewardTokens04F.TryClaim(token)) return false;
                // Install the receipt first. A failing grant may lose this payout, but cannot duplicate it on callback retry.
                QuestWorld.Grant(reward, instance.T.Title, true);
                _citySavePending04F = true;
                return true;
            };
        }

        private void CaptureCrystal04F(EventSave save)
        {
            foreach (string token in _cityRewardTokens04F.Tokens) save.CrystalRewardTokens04F.Add(token);
            foreach (EventTemplate template in Templates)
            {
                if (!CityCombatRules04F.IsCityKind(template.Kind)) continue;
                float last = LastFired(template.Id);
                float left = last < 0f ? 0f : Mathf.Max(0f, EventSelector.CooldownOf(template) - (_clock - last));
                if (left <= 0f) continue;
                save.CrystalCooldownIds04F.Add(template.Id);
                save.CrystalCooldownSeconds04F.Add(left);
            }
        }

        private void RestoreCrystal04F(EventSave save)
        {
            _cityRewardTokens04F.Restore(save.CrystalRewardTokens04F);
            if (save.CrystalCooldownIds04F == null || save.CrystalCooldownSeconds04F == null) return;
            int count = Mathf.Min(save.CrystalCooldownIds04F.Count, save.CrystalCooldownSeconds04F.Count);
            for (int i = 0; i < count; i++)
            {
                EventTemplate template = Find(save.CrystalCooldownIds04F[i]);
                if (template != null && CityCombatRules04F.IsCityKind(template.Kind)) _clock = Mathf.Max(_clock, EventSelector.CooldownOf(template));
            }
            for (int i = 0; i < count; i++)
            {
                EventTemplate template = Find(save.CrystalCooldownIds04F[i]);
                if (template == null || !CityCombatRules04F.IsCityKind(template.Kind)) continue;
                float left = Mathf.Clamp(save.CrystalCooldownSeconds04F[i], 0f, EventSelector.CooldownOf(template));
                // LastFired's negative value means never seen, so put the restored clock after the largest cooldown.
                _lastFired[template.Id] = _clock - (EventSelector.CooldownOf(template) - left);
            }
            _cityNextAttempt04F = _clock + 22f;
            _nextSpawn = Mathf.Max(_nextSpawn, _clock + FirstDelay);
        }
    }
}
