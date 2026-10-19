using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class MissionContext
    {
        private readonly List<NpcController> _stagedContacts04L = new List<NpcController>();
        private void StageStoryContacts04L(string[] pairs)
        {
            if(Def==null || !Def.Story || SagaDirection04L.Chapter(Def.Id)<0 || M==null || Player==null)return;
            for(int i=0;i+1<pairs.Length;i+=2)
            {
                string name=pairs[i];if(!StoryMissions.HasContact04L(name))continue;
                bool already=false;
                foreach(NpcController n in Npcs)if(n!=null && !n.IsDead && n.DisplayName==name){already=true;break;}
                if(already)continue;
                Poi post=Game.Layout!=null?Game.Layout.FindPoi(name):null;
                // Only people physically near their original story meeting point. Radio, final boss and Luma are never fabricated.
                if(post==null || MathUtil.FlatDistance(Game.PlayerPosition,post.Position)>12f)continue;
                Vector3 point=Vector3.zero;bool found=false;
                for(int side=0;side<8;side++)
                {
                    Vector3 candidate=PedManager.Ground(Game.PlayerPosition+MathUtil.DirFromYaw(side*45f)*2.2f);
                    if(Mathf.Abs(candidate.y-Game.PlayerPosition.y)>1.4f)continue;
                    if(Physics.CheckCapsule(candidate+Vector3.up*.35f,candidate+Vector3.up*1.6f,.32f,
                        Layers.WorldMask|1<<Layers.Vehicles,QueryTriggerInteraction.Ignore))continue;
                    if(Physics.Linecast(Player.HeadPosition,candidate+Vector3.up*1.4f,Layers.WorldMask,QueryTriggerInteraction.Ignore))continue;
                    point=candidate;found=true;break;
                }
                if(!found)continue; // Keep the subtitle and player camera when there is no legitimate staging space.
                if(Game.Campaign!=null)Game.Campaign.YieldStoryContact04L(this,name);
                NpcController actor=Civilian(point,QuestWorld.YawToward(point,Game.PlayerPosition),StoryMissions.ContactLook04L(name));
                actor.DisplayName=name;actor.Pacifist=true;actor.DropsLoot=false;actor.CashDrop=0;
                if(actor.Health!=null)actor.Health.Invulnerable=true;
                if(Settings.Graphics!=GraphicsPreset.Low && actor.Rig.Detail<2)
                {actor.Rig.Detail=2;actor.Rig.ChangeLook(StoryMissions.ContactLook04L(name));}
                foreach(CastDef04E def in CastRoster04E.All)if(def.Name==name){CastCostumeBuilder04E.Dress(actor.Rig,def.Costume);break;}
                _stagedContacts04L.Add(actor);
            }
        }
        public void ReleaseStagedContact04L(string name)
        {
            for(int i=_stagedContacts04L.Count-1;i>=0;i--)
            {
                NpcController n=_stagedContacts04L[i];if(n==null){_stagedContacts04L.RemoveAt(i);continue;}
                if(n.DisplayName!=name)continue;
                Npcs.Remove(n);_stagedContacts04L.RemoveAt(i);n.gameObject.SetActive(false);Object.Destroy(n.gameObject);
            }
        }
    }
    public static partial class StoryMissions
    {
        public static bool HasContact04L(string name)
        {
            return name=="Mara Solane"||name=="Ion Solane"||name=="Jax Calder"||name=="Dante Rook"
                ||name=="Nico Ash"||name=="June Pyre"||name=="Iris Moon"||name=="Officer Valez"||name=="Sable Knox";
        }
        public static Appearance ContactLook04L(string name)
        {
            foreach(CastDef04E def in CastRoster04E.All)if(def.Name==name)return CastRoster04E.Look(def.Id);
            if(name=="Sable Knox")return NamedLook(901,4,0,true); // Exact original betrayal appearance.
            if(name=="Officer Valez"){Appearance a=Appearance.Police(new System.Random(907));a.Headwear=Headwear.None;return a;}
            Appearance n=NamedLook(902,4,2,false);n.Headwear=Headwear.Cap;n.Jacket=true;n.JacketRow=3;n.JacketColor=2;return n;
        }
    }
}
