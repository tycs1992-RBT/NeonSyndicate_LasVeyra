using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonSyndicate
{
    /// <summary>At most three LOD renderers per installed kind in a 160 m chunk. No per-piece object or Update.</summary>
    public sealed class LiveWildsBatchL2
    {
        private sealed class Group {public LiveWildsKitL2 Kit;public readonly List<CombineInstance>[] Instances={new List<CombineInstance>(),new List<CombineInstance>(),new List<CombineInstance>()};}
        private readonly Dictionary<WildPieceKind,Group> _groups=new Dictionary<WildPieceKind,Group>();
        public readonly LiveWildsMetricsL2 Metrics=new LiveWildsMetricsL2();
        public bool Append(WildPiece p,MeshBuilder legacy)
        {
            LiveWildsRegistryL2 registry=LiveWildsRegistryL2.Loaded;LiveWildsKitL2 kit=registry!=null?registry.Find(p.Kind):null;
            if(kit==null){Metrics.Reason=registry!=null?registry.Status:"Prepared kit missing. Stop Play > Live World > Prepare Redspire wild art.";return false;}
            int index=LiveWildsRulesL2.Variant(p,kit.Variants.Length);Vector3 center,size;
            if(index<0 || !LiveWildsRulesL2.Frame(p,legacy.Vertices,out center,out size)){Metrics.Reason="Invalid piece/frame, procedural fallback retained.";return false;}
            Group group;if(!_groups.TryGetValue(p.Kind,out group)){group=new Group{Kit=kit};_groups.Add(p.Kind,group);}
            Matrix4x4 matrix=Matrix4x4.TRS(center,Quaternion.Euler(0,p.Yaw,0),size);
            for(int lod=0;lod<3;lod++)group.Instances[lod].Add(new CombineInstance{mesh=kit.Variants[index].Lods[lod],subMeshIndex=0,transform=matrix});
            if(p.Kind==WildPieceKind.Boulder)Metrics.Boulders++;else if(p.Kind==WildPieceKind.RockSpire)Metrics.Spires++;else Metrics.Trees++;
            return true;
        }
        public void Emit(Transform parent,WildsBuilder.Result result)
        {
            foreach(KeyValuePair<WildPieceKind,Group> pair in _groups)
            {
                GameObject root=new GameObject("Redspire "+pair.Key+" chunk batch");root.transform.SetParent(parent,false);root.layer=2;root.isStatic=true;
                LOD[] lods=new LOD[3];float[] distances={.32f,.12f,.012f};
                for(int i=0;i<3;i++)
                {
                    Mesh mesh=new Mesh{name="Redspire "+pair.Key+" LOD"+i,indexFormat=IndexFormat.UInt32};
                    try{mesh.CombineMeshes(pair.Value.Instances[i].ToArray(),true,true);mesh.RecalculateBounds();}
                    catch{UnityEngine.Object.Destroy(mesh);throw;}
                    GameObject child=new GameObject("LOD"+i);child.transform.SetParent(root.transform,false);child.layer=2;child.isStatic=true;child.AddComponent<MeshFilter>().sharedMesh=mesh;
                    MeshRenderer renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=pair.Value.Kit.Material;renderer.receiveShadows=true;renderer.shadowCastingMode=i==2?ShadowCastingMode.Off:ShadowCastingMode.On;
                    lods[i]=new LOD(distances[i],new Renderer[]{renderer});Metrics.Batches++;Metrics.Vertices+=mesh.vertexCount;result.Renderers++;result.Vertices+=mesh.vertexCount;
                }
                LODGroup group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
            }
        }
    }
}
