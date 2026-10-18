using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Optional first-session routes attached to the existing guidance and giver system.</summary>
    public sealed partial class CampaignDirector
    {
        private void OnCityEncounterCompleted04F(string eventId)
        {
            if (_save.CityChoicesOffered04F || !_sessionFreeRoam) return;
            if (eventId != "ev_04f_warden_passage" && eventId != "ev_04f_street_drake" && eventId != "ev_04f_crystal_cache") return;
            QueueNextChoices("city04f:" + eventId);
        }

        /// <summary>The next real step toward the first rescue. No quest is accepted and no companion is granted.</summary>
        public string CompanionLeadId04F()
        {
            QuestBook book = Game.Quests;
            return book == null ? "" : GuidanceRules04F.CompanionLead(book.Find, book.IsDone);
        }

        public bool TrackCompanionLead04F()
        {
            string id = CompanionLeadId04F();
            if (id.Length == 0)
            {
                Notify("No uncompleted rescue lead is available. BONDS and STATUS show the current state.", 5f);
                return false;
            }
            return TrackQuestLead(id, true);
        }

        private NextOption04E? NearbyContractOption04F()
        {
            QuestBook book = Game.Quests;
            if (book == null) return null;
            QuestDef best = null;
            float distance = float.MaxValue;
            foreach (QuestDef q in book.Quests)
            {
                if (!GuidanceRules04F.IsInteriorContract(q.Id) || book.IsDone(q.Id) || book.WhyLocked(q, null) != null) continue;
                CastSlot giver = Slot(QuestMeta04E.CastGiver(q));
                if (giver == null) continue;
                EnsureBound(giver);
                if (!giver.PostBound) continue;
                float away = GuidanceRules04E.LayerDistance(Game.PlayerPosition, giver.Post);
                if (away >= distance) continue;
                distance = away;
                best = q;
            }
            if (best != null)
                return new NextOption04E { Kind = 1, Button = "CONTRACT", QuestId = best.Id,
                    Detail = "Optional indoor contract, \"" + best.Title + "\" with " + GiverWhere(best) };
            NextOption04E? side = SideOption();
            if (side.HasValue)
            {
                NextOption04E option = side.Value;
                option.Button = "CONTRACT";
                return option;
            }
            return null;
        }

        private NextOption04E? RechargeOption04F()
        {
            Vector3 target;
            bool bound = VenuePoint(VenueIds04E.Dispatch, "fitting", out target);
            return new NextOption04E
            {
                Kind = 2, Button = "RECHARGE", HasTarget = bound, Target = target,
                Detail = bound ? "Forge / Recharging: Cinder Dispatch's indoor fitting bench. Use the charge station there, then compare builds (K or pause > BUILD)."
                    : "Forge / Recharging: the Cinder Dispatch bench is unavailable. See STATUS; melee still works without ranged charge."
            };
        }

        private NextOption04E? CompanionOption04F()
        {
            BondSystem bonds = Game.Bonds;
            if (bonds != null && bonds.Roster.Count > 0)
            {
                Vector3 point;
                bool bound = VenuePoint(VenueIds04E.Bondkeeper, "care_station", out point);
                return new NextOption04E { Kind = 3, Button = "COMPANION", Target = point, HasTarget = bound,
                    Detail = "Companion Lead: care and training at Bondkeeper's Refuge. BONDS shows your saved roster and the summon / assist controls." };
            }
            string id = CompanionLeadId04F();
            QuestDef q = Game.Quests != null ? Game.Quests.Find(id) : null;
            return new NextOption04E
            {
                Kind = 3, Button = "COMPANION", QuestId = q != null ? q.Id : "",
                Detail = q != null ? "Companion Lead: NOT BONDED YET. Next prerequisite: \"" + q.Title + "\" with " + GiverWhere(q)
                    + ". The quartz drakeling joins only through A Creature Is Not a Battery."
                    : "Companion Lead: no unfinished rescue route loaded. See BONDS and STATUS; nothing is granted automatically."
            };
        }
    }
}
