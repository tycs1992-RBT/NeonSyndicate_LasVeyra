using System;
using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class QuestSteps
    {
        /// <summary>Player-requested finite city incident. No debug force, no extra reward owner and no passive consent.</summary>
        private static IEnumerator CityEvent04I(QuestRun r,QuestStep s)
        {
            if(r.Book.Flag(s.Key)) yield break;
            Vector3 point=r.Place(s.At,s.OffsetX,s.OffsetZ);
            yield return ExactPressE(r,point,"Press E to request the handler's safe passage",
                Obj(s,"Request a safe passage at the dispatch door, then help the handler and drake."),null,true);
            WorldEvents director=Game.Events;
            if(director==null) {FailQuest(r,"City events are unavailable. Retry this checkpoint when they recover.");yield break;}
            Action clear=()=> {if(director!=null) director.ReleaseQuestCity04I(r.C);r.C.ClearTarget();};
            r.Cleanups.Add(clear);
            EventInstance incident=null;
            try
            {
                while(incident==null)
                {
                    string why;
                    if(director.TryQuestCity04I(r,s.Name,out incident,out why)) break;
                    r.C.Objective("Safe passage request: "+why+" Existing cooldowns and street budgets apply.");
                    yield return r.C.Wait(2f);
                }
                Blip route=r.C.BlipAt(incident.Pos,MarkerColor,"Handler's safe passage");
                r.C.Target(incident.Pos);
                r.C.Objective("Reach the neutral seal. Reunite the handler and drake, then collect the sealed report.");
                while(!incident.Done) yield return null;
                r.C.RemoveBlip(route);r.C.ClearTarget();
                if(incident.Outcome!=EventOutcome.Success)
                {FailQuest(r,"The passage was not confirmed. Retry this checkpoint; no chapter reward was paid.");yield break;}
                r.Book.SetFlag(s.Key);
            }
            finally {clear();}
        }
    }
}
