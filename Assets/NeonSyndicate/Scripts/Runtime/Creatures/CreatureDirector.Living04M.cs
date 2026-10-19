namespace NeonSyndicate
{
    public sealed partial class CreatureDirector
    {
        /// <summary>Live bodies, worker reservations and external bonded bodies, counted once for opt-in world encounters.</summary>
        public float AllocatedBodyCost04M
        {
            get
            {
                float cost=CompanionBudget.BodyCost;
                foreach(CharacterBase body in _live)
                    if(body!=null && !body.IsDead)cost+=CreatureEcology.BodyCost(SpeciesOf(body));
                foreach(GroupPlan plan in _plans)cost+=plan.Count*CreatureEcology.BodyCost(plan.Species);
                return cost;
            }
        }
    }
}
