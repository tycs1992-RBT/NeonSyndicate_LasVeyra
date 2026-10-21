using UnityEngine;
using System.Collections.Generic;
namespace NeonSyndicate
{
    /// <summary>Two finite regional formations, using original species, AI, attack tokens, shots and kill authority.</summary>
    public static class FracturedWildsCombat04OC
    {
        public static void ResetStatics() { }
        public static string RewardToken(string eventId,int occurrence,int revision)
        {return (revision==3 || revision==4 || revision==5 || revision==6 || revision==7 || revision==8 || revision==9 || revision==10) && (eventId=="ev_04ob_threshold_watch"||eventId=="ev_04oc_azure_watch")?eventId+":first-win-04oc":eventId+":"+occurrence;}
        public static bool PreviouslyPaid04OF(IEnumerable<string> tokens,string eventId,int revision)
        {
            if(revision<3 || revision>10 || (eventId!="ev_04ob_threshold_watch" && eventId!="ev_04oc_azure_watch") || tokens==null)return false;
            string prefix=eventId+":";
            foreach(string token in tokens)
            {
                if(token==null || !token.StartsWith(prefix,System.StringComparison.Ordinal))continue;
                string suffix=token.Substring(prefix.Length);int occurrence;
                if(suffix=="first-win-04oc" || (int.TryParse(suffix,out occurrence) && occurrence>=0))return true;
            }
            return false;
        }
        public static bool Azure(string eventId){return eventId=="ev_04oc_azure_watch";}
        public static string Species(string eventId,int slot)
        {return Azure(eventId)?(slot==1 || slot>=3?"glass_mite":"neon_jackal"):LivingEcology04M.MusterSpecies(slot);}
        public static bool Caster(string eventId,int slot){return !Azure(eventId) && slot==1;}
        public static CreatureAttack[] AzureAttacks()
        {
            return new[]{new CreatureAttack{Name="Shardwood: storm pounce",Action=CreatureAction.Pounce,Shape=AttackShape.Leap,
                Windup=1.1f,Active=.35f,Recovery=1.3f,Cooldown=5.2f,Speed=12f,MinRange=2.5f,MaxRange=9f,Reach=3f,Arc=65f,
                Damage=.75f,Type=DamageType.Power,Element=Element.Volt,Flags=DamageFlags.Parryable},
                new CreatureAttack{Name="Shardwood: warning bite",Action=CreatureAction.Bite,Shape=AttackShape.Arc,
                Windup=.65f,Active=.15f,Recovery=.95f,Cooldown=2.6f,MaxRange=2.7f,Reach=1.7f,Arc=75f,Damage=.65f,Flags=DamageFlags.Parryable}};
        }
        public static CreatureLook AzureLook(CreatureLook source)
        {
            CreatureLook look=source.Clone();look.Crystal=new Vector2Int(0,9);look.Glow=new Vector2Int(0,10);
            look.Horns=1;look.Spines=11;look.Plates=1;look.TailLength*=1.3f;look.HeadStyle=2;
            look.StormProwler04OF=true;look.BodyLength*=1.08f;look.ShoulderHeight*=.94f;look.HeadSize*=.92f;look.TailStyle=3;look.Sprawl=.15f;return look;
        }
        public static Color Affinity(int region){return region==1?new Color(.15f,.85f,.78f):new Color(.60f,.24f,.86f);}
    }
}
