using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>Optional visual layer on the working fort, not a new ownership, combat, input or save system.</summary>
    public sealed partial class StrongholdView
    {
        private readonly CitadelKit03A2 _citadelKit = new CitadelKit03A2();
        private bool _citadelActive, _citadelLogged, _citadelFailureReported;

        private void TryDecorateCitadel(Transform parent,StrongholdSiteSave state)
        {
            GameObject decoration = null;
            _citadelActive = false;
            try
            {
                int style = CitadelLayout03A2.Style(Definition.id,state);
                List<CitadelPart03A2> parts = CitadelLayout03A2.Build(style,state.Slots);
                decoration = new GameObject("Citadel Renaissance 03A2 / "+CitadelLayout03A2.Name(style));
                decoration.transform.SetParent(parent,false);
                decoration.SetActive(false);
                Dictionary<Material,List<CombineInstance>> near = new Dictionary<Material,List<CombineInstance>>();
                Dictionary<Material,List<CombineInstance>> far = new Dictionary<Material,List<CombineInstance>>();
                foreach(CitadelPart03A2 part in parts)
                {
                    if(part.Fine&&Settings.Graphics==GraphicsPreset.Low)continue;
                    Material material = _citadelKit.Surface(style,part.Role,_assets);
                    Matrix4x4 matrix = Matrix4x4.TRS(part.Position,Quaternion.Euler(part.Euler),part.Scale);
                    AddCitadelPart(near,material,_citadelKit.Model(part.Model,Settings.Graphics==GraphicsPreset.Low),matrix);
                    if(!part.Fine)AddCitadelPart(far,material,_citadelKit.Model(part.Model,true),matrix);
                }
                Renderer[] nearRenderers = EmitCitadel(near,decoration.transform,"detail");
                Renderer[] farRenderers = EmitCitadel(far,decoration.transform,"silhouette");
                LODGroup lod = decoration.AddComponent<LODGroup>();
                lod.fadeMode = LODFadeMode.None;
                lod.SetLODs(new[] { new LOD(.095f,nearRenderers),new LOD(.008f,farRenderers) });
                lod.RecalculateBounds();
                // Stage replacements first. Failure cannot partially retint the base before all new surfaces exist.
                Material stone = _citadelKit.Surface(style,"stone",_assets);
                Material cloth = _citadelKit.Surface(style,"cloth",_assets);
                MeshRenderer[] original = parent.GetComponentsInChildren<MeshRenderer>(true);
                foreach(MeshRenderer renderer in original)
                {
                    Material m=renderer.sharedMaterial;
                    if(m==null||!m.name.StartsWith("Strongholds 02I / ",StringComparison.Ordinal))continue;
                    if(m.name.EndsWith("/stone",StringComparison.Ordinal))renderer.sharedMaterial=stone;
                    else if(m.name.EndsWith("/cloth",StringComparison.Ordinal))renderer.sharedMaterial=cloth;
                }
                decoration.SetActive(true);_citadelActive=true;
                if(!_citadelLogged)
                {
                    _citadelLogged=true;
                    Log.Info("Citadel Renaissance 03A2 | "+Definition.title+" | "+CitadelLayout03A2.Name(style)+
                        " | "+parts.Count+" exterior pieces. Existing sockets, captures and collisions retained.");
                }
            }
            catch(Exception error)
            {
                if(decoration!=null){decoration.SetActive(false);UnityEngine.Object.Destroy(decoration);}
                if(!_citadelFailureReported)
                {
                    _citadelFailureReported=true;
                    Log.Warn("Citadel 03A2 optional art did not load; the working fort is retained. "+error.Message);
                }
            }
        }
        private static void AddCitadelPart(Dictionary<Material,List<CombineInstance>> map,Material mat,Mesh mesh,Matrix4x4 matrix)
        {
            if(mesh==null||!mesh.isReadable||mesh.vertexCount==0)throw new InvalidOperationException("Citadel source mesh is not readable.");
            List<CombineInstance> items;
            if(!map.TryGetValue(mat,out items)){items=new List<CombineInstance>();map.Add(mat,items);}
            items.Add(new CombineInstance{mesh=mesh,transform=matrix});
        }
        private static Renderer[] EmitCitadel(Dictionary<Material,List<CombineInstance>> groups,Transform parent,string detail)
        {
            List<Renderer> output = new List<Renderer>();
            foreach(KeyValuePair<Material,List<CombineInstance>> group in groups)
            {
                if(group.Value.Count==0)continue;
                GameObject node = new GameObject("03A2 "+detail+" / "+group.Key.name);
                node.transform.SetParent(parent,false);
                Mesh mesh=OwnedMeshes.Track(node,new Mesh{name="Citadel 03A2 "+detail,indexFormat=IndexFormat.UInt32});
                mesh.CombineMeshes(group.Value.ToArray(),true,true,false);
                mesh.RecalculateBounds();mesh.RecalculateTangents();
                node.AddComponent<MeshFilter>().sharedMesh=mesh;
                MeshRenderer renderer=node.AddComponent<MeshRenderer>();renderer.sharedMaterial=group.Key;
                renderer.shadowCastingMode=Settings.Graphics>=GraphicsPreset.High?ShadowCastingMode.On:ShadowCastingMode.Off;
                renderer.receiveShadows=true;renderer.lightProbeUsage=LightProbeUsage.Off;
                mesh.UploadMeshData(true);output.Add(renderer);
            }
            return output.ToArray();
        }
    }
}
