using System;
using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class QuestSteps
    {
        /// <summary>Physical resonance diagnosis. Three quiet beats, optional one-seal companion help, recoverable interruption.</summary>
        private static IEnumerator Attune04H(QuestRun r,QuestStep s)
        {
            Vector3 point=r.Place(s.At,s.OffsetX,s.OffsetZ);
            float elapsed=0f,lastDamage=-1f;
            int seals=0,lastCycle=-1;
            bool assisted=false,healthCaptured=false;
            QuestProp node=QuestProps.Build("Crystal",point+Vector3.up*.7f,0f,r.C.M.transform,Accent(r));
            node.enabled=false;
            foreach(Collider collider in node.GetComponentsInChildren<Collider>()) collider.enabled=false;
            node.transform.localScale*=.3f;
            Vector3 size=node.transform.localScale;
            r.C.Objects.Add(node.gameObject);
            WorldMarker marker=ExactMarker(r,point,MarkerColor,.6f);
            Blip blip=r.C.BlipAt(point,MarkerColor,"Resonance diagnosis");
            PlayerController player=Game.Player;
            BondSystem bonds=Game.Bonds;
            Action<Vector3> support=p=>
            {
                if(assisted || !Game.IsPlaying || GameTime.Paused || !AtExact(point,2.4f) ||
                    Vector3.Distance(p,point)>3f || Game.Player==null || Game.Player.InVehicle) return;
                assisted=true;seals=Mathf.Min(3,seals+1);
                if(Game.Hud!=null) Game.Hud.Notify("Your companion steadied one seal. Finish the quiet beats.",3f);
            };
            if(bonds!=null) bonds.ExplorationAssist+=support;
            Action clear=()=> { if(bonds!=null) bonds.ExplorationAssist-=support; if(Game.Hud!=null) Game.Hud.ClearCounter(); };
            r.Cleanups.Add(clear);
            Interactable prompt=QuestPrompt("Tune resonance",point,2.2f,"Interact on the quiet beat",()=>
            {
                if(!Game.IsPlaying || GameTime.Paused || Game.Player==null || Game.Player.IsDead || Game.Player.InVehicle ||
                    !AtExact(point,2.4f) || Physics.Linecast(Game.Player.ChestPosition,point+Vector3.up*.6f,Layers.WorldMask,QueryTriggerInteraction.Ignore)) return;
                int cycle=Attunement04H.Cycle(elapsed);
                if(cycle==lastCycle) return;
                if(Attunement04H.Quiet(elapsed))
                {
                    seals++;lastCycle=cycle;
                    if(Game.Audio!=null) Game.Audio.PlayAt(SfxId.CrystalChime,point,.4f,.8f+seals*.15f,18f);
                }
                else if(Game.Hud!=null) Game.Hud.Notify("Listen for the quiet beat; no progress was lost.",2f);
            },r.C.M.transform);
            prompt.HeightTolerance=1.4f;
            prompt.DynamicPrompt=()=>Attunement04H.Cycle(elapsed)==lastCycle?"Seal aligned. Listen for the next beat."
                :Attunement04H.Quiet(elapsed)?"QUIET BEAT: Press E to align a seal":"Listen... align when QUIET BEAT appears";
            r.C.Objects.Add(prompt.gameObject);
            r.C.Target(point);
            r.C.Objective(Obj(s,"Align three quiet beats at the marker. A companion assist can steady one seal."));
            try
            {
                while(seals<3)
                {
                    player=Game.Player;
                    if(player==null || player.IsDead || !Game.IsPlaying || GameTime.Paused) {yield return null;continue;}
                    if(!healthCaptured) {lastDamage=player.Health.LastDamageTime;healthCaptured=true;}
                    if(player.Health.LastDamageTime>lastDamage)
                    {
                        seals=0;lastCycle=-1;lastDamage=player.Health.LastDamageTime;
                        if(Game.Hud!=null) Game.Hud.Notify("The strike disturbed this node. Re-align its seals when safe.",3f);
                    }
                    bool nearby=!player.InVehicle && AtExact(point,2.4f);
                    // Rhythm pauses on leaving this work point. No hidden timeout or loss of completed earlier nodes.
                    if(nearby) elapsed+=Time.deltaTime;
                    if(node==null) throw new InvalidOperationException("Resonance node was lost. Resume this checkpoint.");
                    bool reduced=Game.CrystalCombat!=null && Game.CrystalCombat.ReducedEffects;
                    node.transform.localScale=size*(1f+(reduced?.015f:.065f)*Mathf.Sin(elapsed*Mathf.PI*2f/Attunement04H.Period));
                    if(Game.Hud!=null && nearby) Game.Hud.SetCounter("ALIGNED SEALS",Mathf.Min(seals,3),3);
                    yield return null;
                }
            }
            finally
            {
                clear();
                if(node!=null) UnityEngine.Object.Destroy(node.gameObject);
                if(prompt!=null) UnityEngine.Object.Destroy(prompt.gameObject);
                if(marker!=null) UnityEngine.Object.Destroy(marker.gameObject);
                r.C.RemoveBlip(blip);r.C.ClearTarget();
                if(Game.Hud!=null) Game.Hud.ClearCounter();
            }
        }
    }

    public static class Attunement04H
    {
        public const float Period=1.8f;
        public static void ResetStatics() { }
        public static int Cycle(float elapsed)
        { return elapsed>=0f && !float.IsInfinity(elapsed)?Mathf.FloorToInt(elapsed/Period):-1; }
        public static bool Quiet(float elapsed)
        {
            if(Cycle(elapsed)<0) return false;
            float beat=elapsed%Period;
            return beat>=.75f && beat<=1.65f;
        }
    }
}
