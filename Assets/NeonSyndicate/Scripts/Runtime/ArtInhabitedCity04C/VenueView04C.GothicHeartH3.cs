using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class VenueView04C
    {
        private bool GothicHeartShellH3(VenueBinding04C binding,VisualRebirth04ABatch batch,List<Mesh> temporary)
        {
            if(Game.Layout==null || !Game.Layout.GothicHeartH3 || !(GothicHeartH3.Replaces(binding.Definition.Id) || Game.Layout.GothicCivicH5 && GothicCivicH5.Replaces(binding.Definition.Id)) || !AscensionArtH2.Ready())return false;
            Dictionary<string,MeshBuilder> high=new Dictionary<string,MeshBuilder>(),low=new Dictionary<string,MeshBuilder>();
            MeshBuilder collision=new MeshBuilder(1),structure=new MeshBuilder(1);bool civic=Game.Layout.GothicCivicH5 && GothicCivicH5.Replaces(binding.Definition.Id);
            MeshBuilder mineral=new MeshBuilder(1);if(civic)GothicCivicH5.Structure(structure,binding,mineral);else GothicHeartH3.Structure(structure,binding);
            high.Add("masonry",structure);low.Add("masonry",new MeshBuilder(1));low["masonry"].Append(structure);collision.Append(structure);
            if(mineral.VertexCount>0){high.Add("amethyst",mineral);low.Add("amethyst",new MeshBuilder(1));low["amethyst"].Append(mineral);collision.Append(mineral);}
            foreach(ArtPlacementH2 part in civic?GothicCivicH5.Plan(binding):GothicHeartH3.ServicePlan(binding))
            {
                if(!CaptureRewritePartWR01(binding,part))
                {
                    string role=AscensionArtH2.Role(part.Model);
                    if(!high.ContainsKey(role)){high.Add(role,new MeshBuilder(1));low.Add(role,new MeshBuilder(1));}
                    AscensionArtH2.Append(high[role],AscensionArtH2.Source(part.Model,Settings.Graphics==GraphicsPreset.Low),part.Position,part.Yaw,part.Scale);
                    AscensionArtH2.Append(low[role],AscensionArtH2.Source(part.Model,true),part.Position,part.Yaw,part.Scale);
                }
                if(AscensionArtH2.HasCollision(part.Model))AscensionArtH2.Append(collision,AscensionArtH2.Source(part.Model,true),part.Position,part.Yaw,part.Scale);
            }
            FinishRewriteSourceWR01(batch,temporary);
            if (!StageForgeCourtWR02I(binding, structure, high, low))
            foreach(KeyValuePair<string,MeshBuilder> group in high)
            {
                Mesh near=group.Value.ToMesh("H3 "+binding.Definition.Id+" / "+group.Key),far=low[group.Key].ToMesh("H3 bounded far / "+group.Key);
                temporary.Add(near);temporary.Add(far);
                if(!StageExteriorMasonryWR02D(group.Key,near,far))batch.PartMeshes(near,far,AscensionArtH2.Surface(group.Key),Vector3.zero,Vector3.one,Quaternion.identity);
            }
            GameObject solid=new GameObject("H3 fixed-asset service shell / "+binding.Definition.Id);solid.transform.SetParent(transform,false);solid.layer=Layers.World;solid.isStatic=true;
            solid.AddComponent<MeshCollider>().sharedMesh=OwnedMeshes.Track(solid,collision.ToMesh(solid.name));ColliderCount++;return true;
        }
    }
    public sealed partial class CityBuilder
    {
        private void BuildGothicHeartH3()
        {
            if(!L.GothicHeartH3 || !AscensionArtH2.Ready())return;
            List<ArtPlacementH2> plan=GothicHeartH3.CourtPlan(L);plan.AddRange(GothicHeartH3.RegionalPlan(L));
            foreach(ArtPlacementH2 part in plan)
            {
                MeshBuilder near=new MeshBuilder(1),solid=new MeshBuilder(1);
                AscensionArtH2.Append(near,AscensionArtH2.Source(part.Model,Settings.Graphics==GraphicsPreset.Low),part.Position,part.Yaw,part.Scale);
                if(AscensionArtH2.HasCollision(part.Model))AscensionArtH2.Append(solid,AscensionArtH2.Source(part.Model,true),part.Position,part.Yaw,part.Scale);
                EmitWildMesh04OC(part.Position,near,AscensionArtH2.Surface(AscensionArtH2.Role(part.Model)),false);
                // The existing static chunk collision owner flushes these with the terrain below.
                long key=ChunkKey(part.Position.x,part.Position.z);MeshBuilder prior;
                if(!_wildCollision04OC.TryGetValue(key,out prior)){prior=new MeshBuilder(1);_wildCollision04OC.Add(key,prior);}prior.Append(solid);
            }
        }
    }
}
