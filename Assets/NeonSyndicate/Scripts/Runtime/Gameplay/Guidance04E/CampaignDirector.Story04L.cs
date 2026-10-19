namespace NeonSyndicate
{
    public sealed partial class CampaignDirector
    {
        /// <summary>The existing cast owner yields its body before the existing story owner stages that identity.</summary>
        public void YieldStoryContact04L(MissionContext context,string name)
        {
            if(context==null || context.Def==null || !context.Def.Story || Game.Missions==null || Game.Missions.Active!=context)return;
            foreach(CastSlot slot in _slots)
                if(slot.Def.StoryIdentity && slot.Def.Name==name && slot.Present)Despawn(slot);
        }
    }
}
