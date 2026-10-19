using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public static partial class EventKinds
    {
        private static IEnumerator CrystalMuster04M(EventInstance e)
        {
            if(Game.Creatures==null || Game.Player==null || StarterSafety.BlockAmbientAggression(e.Pos))
            {e.Miss("The muster dispersed away from the protected streets.");yield break;}
            int count=Mathf.Clamp(e.T.Count,4,LivingEcology04M.MusterCount(Settings.Graphics==GraphicsPreset.Low));
            float cost=0f;
            for(int i=0;i<count;i++)cost+=CreatureEcology.BodyCost(CreatureSpeciesDb.Get(LivingEcology04M.MusterSpecies(i)));
            int groups,capGroups;float bodies,cap;
            Game.Creatures.AmbientStats(out groups,out bodies,out capGroups,out cap);
            // Pending ambient work also reserves space; conservative admission never expands a graphics preset's budget.
            if(cost+Game.Creatures.AllocatedBodyCost04M>cap || Game.Creatures.AllocatedGroups04N+1>capGroups){e.Miss("The local creature budget is occupied. The muster moved on.");yield break;}
            var pack=new List<CharacterBase>();
            bool accepted=false,contributed=false;
            for(int slot=0;slot<count;slot++)
            {
                Vector3 at;
                if(!CitySafeGround04F(e.Pos+QMath.AY(e.Yaw)*LivingEcology04M.MusterOffset(slot),e.Pos,out at)
                    || Physics.CheckSphere(at+Vector3.up*1.4f,slot==0?1.25f:.65f,Layers.WorldMask,QueryTriggerInteraction.Ignore))
                {e.Miss("The formation has no safe room here.");e.DespawnCreatures();yield break;}
                var options=new CreatureSpawnOptions{Tag=e.Tag,Persistent=true,Hostile=false,CalmUntilProvoked=true,NoRewards=true,
                    Cantor04N=slot==1,Elite=slot==0,Seed=e.Rand.Next(1,int.MaxValue),HealthScale=slot==0?.65f:.8f,DamageScale=.65f,
                    DisplayName=slot==0?"Rallying Shard Bulwark":slot==1?"Lattice Cantor":slot==2?"Muster Flanker":"Muster Mite"};
                CharacterBase creature=Game.Creatures.Spawn(LivingEcology04M.MusterSpecies(slot),at,e.Yaw,options);
                if(creature==null || creature.Health==null){e.Miss("The formation could not assemble.");e.DespawnCreatures();yield break;}
                creature.transform.SetParent(e.Root,true);e.Creatures.Add(creature);pack.Add(creature);
                NpcController observerSafe=creature as NpcController;
                if(observerSafe!=null){observerSafe.Pacifist=true;observerSafe.Hostile=false;observerSafe.GuardAt(at);}
                System.Action<DamageInfo> hit=info=>{if(info.FromPlayer)contributed=true;};
                creature.Health.Damaged+=hit;
                CharacterHealth health=creature.Health;e.CleanupHooks.Add(()=>{if(health!=null)health.Damaged-=hit;});
                yield return null;
            }
            Vector3 court;
            if(!CitySafeGround04F(e.Pos+Vector3.back*7,e.Pos,out court)){e.Miss("No safe observer approach.");yield break;}
            e.AddInteract(court,"Press E to record and challenge the finite crystal muster",()=>accepted=true,false);
            float until=e.Age+Mathf.Min(180f,e.T.Duration);
            while(!accepted && !contributed && e.Age<until && !e.Done)
            {
                if(e.StatusDue())e.Status="Crystal muster: use E to record the paid challenge, observe safely, or leave. No reinforcements.";
                yield return null;
            }
            if(e.Done)yield break;
            if(!accepted && !contributed){e.Miss("The unchallenged muster moved on.");yield break;}
            accepted=true;
            float rally=e.Age,started=Time.time;bool disrupted=false;
            e.AddProp("Totem",e.Pos,e.Yaw,new Color(.5f,.75f,1f));
            CharacterBase leader=pack[0];
            if(Game.Hud!=null)Game.Hud.Notify("The bulwark calls the formation. Strike it to interrupt the rally; the pack still obeys shared attack limits.",5f);
            while(!e.Done && e.Age-rally<2.25f)
            {
                if(LivingEcology04M.RallyInterrupted(leader!=null && !leader.IsDead,leader!=null && leader.Health!=null?leader.Health.LastDamageTime:-1f,started)){disrupted=true;break;}
                if(e.StatusDue())e.Status="Rally call: interrupt the bulwark or prepare for the flanks";
                yield return null;
            }
            if(e.Done)yield break;
            if(disrupted)
            {
                if(Game.Hud!=null)Game.Hud.Notify("Rally broken. The followers hesitate for three seconds.",3f);
                float hesitation=e.Age+3f;
                while(!e.Done && e.Age<hesitation)yield return null;
            }
            if(e.Done)yield break;
            foreach(CharacterBase member in pack)
            {
                Creature c=member as Creature;
                if(c!=null && !c.IsDead){c.Options.CalmUntilProvoked=false;c.JoinHunt(Game.Player);}
                NpcController n=member as NpcController;if(n!=null && !n.IsDead){n.Pacifist=false;n.CreatureOptions.CalmUntilProvoked=false;n.CreatureOptions.Hostile=true;n.Attack(Game.Player);}
            }
            float battleEnd=e.Age+Mathf.Min(95f,e.T.Seconds);
            float away=0f;bool supported04N=false,supportStarted04N=false;
            while(!e.Done)
            {
                // One finite support pulse, disrupted permanently if the caster is killed first.
                Creature cantor=pack.Count>1?pack[1] as Creature:null;
                if(!supported04N && cantor!=null && !cantor.IsDead && cantor.Ready && cantor.Health.Health<cantor.Health.MaxHealth*.5f && !cantor.IsStaggered)
                {
                    if(!supportStarted04N && cantor.RoleCry())
                    {supportStarted04N=true;if(Game.Hud!=null)Game.Hud.Notify("The Cantor calls a mending chord. Interrupt its tell before it lands.",3);}
                    else if(supportStarted04N && cantor.CurrentAttackName=="Cantor: mending chord" && cantor.AttackPhase==1)
                    {
                        supported04N=true;int aided=0;
                        foreach(var ally in pack)if(ally!=null && ally!=cantor && !ally.IsDead && MathUtil.FlatDistance(ally.transform.position,cantor.transform.position)<12f && aided<2)
                        {ally.Health.Heal(5f);aided++;}
                    }
                    else if(supportStarted04N && cantor.CurrentAttackName!="Cantor: mending chord")supported04N=true;
                }
                int alive=0,confirmed=0;
                foreach(CharacterBase member in pack){if(member!=null && member.IsDead)confirmed++;else alive++;}
                if(confirmed==count)
                {
                    if(contributed && e.PlayerAccepted)e.Succeed(e.T.Reward,1f,"The muster is broken. Your contribution was recorded.");
                    else e.Miss("The muster fell without a recorded challenge and contribution. No contract payment was claimed.");
                    yield break;
                }
                if(Game.Player==null || Game.Player.IsDead || e.Age>=battleEnd){e.Fail("The muster withdrew; its finite challenge ended.");yield break;}
                away=e.DistToPlayer>65f?away+Time.deltaTime:0f;
                if(away>=5f){e.Miss("You retreated. The muster dispersed.");yield break;}
                if(e.StatusDue())e.Status="Break the crystal muster: "+alive+" remain. Retreat beyond 65m to withdraw.";
                yield return null;
            }
        }
    }
}
