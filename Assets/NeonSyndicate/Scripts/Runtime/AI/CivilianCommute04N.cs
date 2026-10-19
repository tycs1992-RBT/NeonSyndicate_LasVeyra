using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public static class CivilianCommute04N
    {
        /// <summary>Bounded graph search. Returns first original link, -2 at destination, -1 when no admitted route.</summary>
        public static int FirstLink(IList<WalkNode> nodes,IList<WalkLink> links,int from,int to,int budget=128,float radius=180)
        {
            if(nodes==null || links==null || from<0 || to<0 || from>=nodes.Count || to>=nodes.Count)return -1;
            if(from==to)return -2;if((nodes[to].Pos-nodes[from].Pos).sqrMagnitude>radius*radius)return -1;
            var queue=new Queue<int>();var first=new Dictionary<int,int>();queue.Enqueue(from);first[from]=-1;
            while(queue.Count>0 && first.Count<=budget)
            {
                int node=queue.Dequeue();
                foreach(int index in nodes[node].Links)
                {
                    if(index<0 || index>=links.Count)continue;WalkLink edge=links[index];
                    if(edge.A!=node && edge.B!=node)continue;int next=edge.A==node?edge.B:edge.A;
                    if(next<0 || next>=nodes.Count || first.ContainsKey(next) || Mathf.Abs(nodes[next].Pos.y-nodes[node].Pos.y)>.35f ||
                        (nodes[next].Pos-nodes[from].Pos).sqrMagnitude>radius*radius)continue;
                    int initial=node==from?index:first[node];if(next==to)return initial;
                    if(first.Count>=budget)continue;first[next]=initial;queue.Enqueue(next);
                }
            }
            return -1;
        }
        public static bool GoHome(float hour,int seed,bool wet,bool localDanger)
        {var shift=LivingWorldRules04M.Shift(hour,seed,wet);return localDanger || shift==ResidentShift04M.Rest || shift==ResidentShift04M.Shelter;}
    }
    public sealed partial class NpcController
    {
        private int _commuteHome04N=-1,_commuteWork04N=-1;private bool _commuteTried04N;
        private static float _nextCommuteAssign04N;
        private int CommuteLink04N(CityLayout layout)
        {
            if(Role!=NpcRole.Civilian || Persistent || IsCreature || !string.IsNullOrEmpty(MissionTag) ||
                MathUtil.FlatSqrDistance(transform.position,Game.PlayerPosition)>85f*85f)return -1;
            if(!_commuteTried04N)
            {
                if(Time.time<_nextCommuteAssign04N)return -1;_nextCommuteAssign04N=Time.time+.3f;_commuteTried04N=true;
                float nearest=180f*180f;Lot home=null;
                foreach(Block block in layout.Blocks)foreach(Lot lot in block.Lots)
                {
                    if(lot.Use!=LotUse.House && lot.Use!=LotUse.Projects)continue;
                    float d=MathUtil.FlatSqrDistance(lot.Entrance,Home);if(d<nearest){nearest=d;home=lot;}
                }
                if(home!=null)
                {
                    int h=layout.NearestWalkNode(home.Entrance,12f),w=layout.NearestWalkNode(Home,20f);
                    if(CivilianCommute04N.FirstLink(layout.WalkNodes,layout.WalkLinks,w,h)>=0){_commuteHome04N=h;_commuteWork04N=w;}
                }
            }
            if(_commuteHome04N<0 || _node<0)return -1;
            bool wet=Game.Atmo!=null && (Game.Atmo.Current==Weather.Storm || Game.Atmo.Current==Weather.Rain);
            bool danger=MathUtil.FlatSqrDistance(transform.position,Game.PlayerPosition)<30f*30f &&
                (CombatDirector.InCombat || Game.Wanted!=null && Game.Wanted.Stars>0);
            int target=CivilianCommute04N.GoHome(Game.Atmo!=null?Game.Atmo.Hours:10f,_commuteHome04N,wet,danger)?_commuteHome04N:_commuteWork04N;
            return CivilianCommute04N.FirstLink(layout.WalkNodes,layout.WalkLinks,_node,target);
        }
    }
}
