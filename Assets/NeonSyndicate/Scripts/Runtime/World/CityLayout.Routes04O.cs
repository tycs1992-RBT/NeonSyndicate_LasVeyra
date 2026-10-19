using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CityLayout
    {
        /// <summary>Shortest admitted pedestrian graph path, including the new ground courts. Empty means no safe route.</summary>
        public List<Vector3> FindWalkRoute04O(Vector3 from,Vector3 to)
        {
            List<Vector3> result=new List<Vector3>();
            if(Mathf.Abs(from.y-CurbHeight)>2f || Mathf.Abs(to.y-CurbHeight)>2f) return result;
            int start=ReachableWalk04O(from),end=ReachableWalk04O(to);
            if(start<0 || end<0) return result;
            float[] distance=new float[WalkNodes.Count];int[] previous=new int[WalkNodes.Count];
            for(int i=0;i<distance.Length;i++){distance[i]=float.MaxValue;previous[i]=-1;}
            SortedSet<(float Cost,int Node)> open=new SortedSet<(float Cost,int Node)>();
            distance[start]=0f;open.Add((0f,start));
            int visited=0;
            while(open.Count>0 && visited++<WalkNodes.Count*4)
            {
                (float Cost,int Node) item=open.Min;open.Remove(item);
                if(item.Cost>distance[item.Node])continue;
                if(item.Node==end)break;
                foreach(int linkId in WalkNodes[item.Node].Links)
                {
                    WalkLink link=WalkLinks[linkId];int next=link.A==item.Node?link.B:link.A;
                    float candidate=item.Cost+Vector3.Distance(WalkNodes[item.Node].Pos,WalkNodes[next].Pos);
                    if(candidate>=distance[next])continue;
                    distance[next]=candidate;previous[next]=item.Node;open.Add((candidate,next));
                }
            }
            if(distance[end]==float.MaxValue)return result;
            int cursor=end;
            for(int i=0;i<=WalkNodes.Count;i++)
            {
                result.Add(WalkNodes[cursor].Pos);if(cursor==start)break;
                cursor=previous[cursor];if(cursor<0){result.Clear();return result;}
            }
            result.Reverse();result.Insert(0,from);result.Add(to);return result;
        }
        private int ReachableWalk04O(Vector3 point)
        {
            // Check a small closest set before the expensive solid-clearance samples.
            List<int> candidates=new List<int>();
            foreach(WalkNode node in WalkNodes) if(MathUtil.FlatSqrDistance(point,node.Pos)<50f*50f)candidates.Add(node.Id);
            candidates.Sort((a,b)=>MathUtil.FlatSqrDistance(point,WalkNodes[a].Pos).CompareTo(MathUtil.FlatSqrDistance(point,WalkNodes[b].Pos)));
            for(int i=0;i<Mathf.Min(12,candidates.Count);i++) if(ClearPassage04O(point,WalkNodes[candidates[i]].Pos,.28f))return candidates[i];
            return -1;
        }
    }
}
