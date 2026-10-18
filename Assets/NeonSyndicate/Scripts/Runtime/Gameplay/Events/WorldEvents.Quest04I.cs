using UnityEngine;

namespace NeonSyndicate
{
    public static class CityIncidentRules04I
    {
        public static void ResetStatics() { }
        public static bool Authorized(string quest,string incident)
        {return quest=="uh05_04i_02_passage" && incident=="ev_04f_street_drake";}
    }

    public sealed partial class WorldEvents
    {
        private MissionContext _questOwner04I;
        private string _questId04I="";
        private EventInstance _questCity04I;
        private bool QuestLease04I
        {
            get
            {
                QuestRun run=Game.Quests!=null?Game.Quests.Running:null;
                return _questOwner04I!=null && Game.Missions!=null && Game.Missions.Active==_questOwner04I &&
                    run!=null && run.C==_questOwner04I && run.Quest.Id==_questId04I;
            }
        }

        /// <summary>Explicit quest request through the existing director. Normal cooldowns, safe placement and actor budgets still apply.</summary>
        public bool TryQuestCity04I(QuestRun run,string id,out EventInstance instance,out string reason)
        {
            instance=null;reason="The current quest does not own this request.";
            if(run==null || Game.Quests==null || Game.Quests.Running!=run || Game.Missions==null || Game.Missions.Active!=run.C ||
                !CityIncidentRules04I.Authorized(run.Quest.Id,id)) return false;
            EventTemplate template=Find(id);
            if(template==null || !CityCombatRules04F.IsCityKind(template.Kind)) {reason="Unknown crystal city incident.";return false;}
            if(_questOwner04I!=null && _questOwner04I!=run.C) {reason="Another activity owns the incident.";return false;}
            _questOwner04I=run.C;_questId04I=run.Quest.Id;
            if(_questCity04I!=null) {instance=_questCity04I;reason="";return true;}
            if(Game.Player==null || Game.Player.IsDead || Game.Player.InVehicle || PlayerUnderground() ||
                Game.State==GameState.Cutscene || Game.Cam!=null && Game.Cam.InCinematic)
            {reason="Step outside on foot when safe.";return false;}
            if(Game.Inner!=null && Game.Inner.IsInner) {reason="Return from the Veil.";return false;}
            if(Game.Wanted!=null && Game.Wanted.Stars>0) {reason="Wait until local enforcement stands down.";return false;}
            if(CountRunning()>0 || TerritoryEventActive || Game.Territories!=null && Game.Territories.HasActiveEncounter ||
                Game.Strongholds!=null && Game.Strongholds.Book!=null && Game.Strongholds.Book.ActiveRaid!=null)
            {reason="Another encounter still holds the district budget.";return false;}
            reason=WhyNot(template);
            if(reason!=null) return false;
            if(!Spawn(template,false)) {reason="No safe street court nearby. Walk farther from doors and try again.";return false;}
            foreach(EventInstance e in _active)
                if(!e.Done && e.T==template)
                {
                    _questCity04I=e;e.QuestOwned04I=true;
                    // The chapter's ordinary once-only reward is the sole payout. No overlapping event transaction.
                    e.RewardHandler=reward=>true;
                    instance=e;reason="";
                    _cityNextAttempt04F=Mathf.Max(_cityNextAttempt04F,_clock+CityCombatRules04F.EventCooldown);
                    _nextSpawn=Mathf.Max(_nextSpawn,_clock+130f);
                    return true;
                }
            reason="The admitted incident was lost. Retry this checkpoint.";return false;
        }

        public void ReleaseQuestCity04I(MissionContext owner)
        {
            if(_questOwner04I!=owner) return;
            EventInstance e=_questCity04I;
            _questOwner04I=null;_questId04I="";_questCity04I=null;
            if(e==null) return;
            if(!e.Done) e.Miss("The requested route was called off. Retry from the chapter checkpoint.");
            if(_reported.Add(e)) Report(e);
            e.Cleanup();_active.Remove(e);_forced.Remove(e);_reported.Remove(e);
            if(_callout==e) _callout=null;
        }
    }
}
