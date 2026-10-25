using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class WildsChunkL1
    {
        private bool _authoredL2;
        public LiveWildsMetricsL2 MetricsL2 {get;private set;}
        public void ExclusionsL2(System.Collections.Generic.HashSet<int> target){target.UnionWith(_excluded);}
        /// <summary>Rebuild appearance only; the original collider stays active and keeps the same mesh instance.</summary>
        public void SetArtL2(bool authored)
        {
            if(_authoredL2==authored)return;
            _authoredL2=authored;if(MetricsL2==null || MetricsL2.Eligible==0)return;
            Transform solid=_geometry!=null?_geometry.Find("Collision"):null;
            Mesh solidMesh=solid!=null?solid.GetComponent<MeshCollider>().sharedMesh:null;
            if(solid!=null){solid.SetParent(transform,false);_owned.Remove(solidMesh);}
            _authoredL2=authored;
            try{Rebuild(new WildsBuilder.Result(),false);}
            finally{if(solid!=null && _geometry!=null){solid.SetParent(_geometry,false);if(solidMesh!=null)_owned.Add(solidMesh);}}
        }
    }
}
