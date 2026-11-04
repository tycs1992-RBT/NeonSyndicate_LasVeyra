using System;

namespace NeonSyndicate
{
    /// <summary>
    /// QuestBook, H8S venue offers: the venue request point asks NextVenueOffer, which applies the one shared rule
    /// (VenueQuestRulesH8S.FirstVenueOffer) with this book's own ledger: not completed, the venue available, and the quest
    /// accepted or unlocked. The campaign director's giver state calls the same rule, so the person's marker and the
    /// request point always name the same quest. The predicates are created once, so asking every frame allocates no
    /// closures (the progress snapshot is the same one the earlier loop took).
    /// </summary>
    public sealed partial class QuestBook
    {
        private Func<QuestDef, bool> _venueOfferDoneH8S, _venueOfferEligibleH8S;
        private QuestProgress _venueOfferProgressH8S;

        private QuestDef NextVenueOfferH8S(string venueId)
        {
            if (_venueOfferDoneH8S == null)
            {
                _venueOfferDoneH8S = q => _completed.Contains(q.Id);
                _venueOfferEligibleH8S = q => Game.Inhabited != null && Game.Inhabited.QuestVenueProblem(q) == null
                    && (q.Id == _active || WhyLocked(q, _venueOfferProgressH8S) == null);
            }
            QuestProgress p = Progress();
            _venueOfferProgressH8S = p;
            try { return VenueQuestRulesH8S.FirstVenueOffer(Quests, venueId, p.WorldRevision, _venueOfferDoneH8S, _venueOfferEligibleH8S); }
            finally { _venueOfferProgressH8S = null; }
        }
    }
}
