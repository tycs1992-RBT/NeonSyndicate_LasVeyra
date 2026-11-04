using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VenueView04C
    {
        private bool StageScienceService(VenueBinding04C binding)
        {
            if (Game.Layout == null || !Game.Layout.GothicCivicH5 || !HeartPlanFR01.ScienceServiceFrame(binding.Definition.Id,binding.Width,binding.Depth)) return false;
            try
            {
                MeshBuilder structure=new MeshBuilder(1),collision=new MeshBuilder(1),porch=new MeshBuilder(1);
                bool civic=binding.Definition.Id=="archive";
                MeshBuilder mineral=new MeshBuilder(1);
                if(civic)GothicCivicH5.Structure(structure,binding,mineral);else GothicHeartH3.Structure(structure,binding);
                collision.Append(structure);collision.Append(mineral);
                Dictionary<string,MeshBuilder> near=new Dictionary<string,MeshBuilder>(),far=new Dictionary<string,MeshBuilder>();
                near.Add("masonry",new MeshBuilder(1));far.Add("masonry",new MeshBuilder(1));near["masonry"].Append(structure);far["masonry"].Append(structure);
                if(mineral.VertexCount>0){near.Add("amethyst",new MeshBuilder(1));far.Add("amethyst",new MeshBuilder(1));near["amethyst"].Append(mineral);far["amethyst"].Append(mineral);}
                foreach(ArtPlacementH2 part in civic?GothicCivicH5.Plan(binding):GothicHeartH3.ServicePlan(binding))
                {
                    string role=AscensionArtH2.Role(part.Model);
                    if(!near.ContainsKey(role)){near.Add(role,new MeshBuilder(1));far.Add(role,new MeshBuilder(1));}
                    AscensionArtH2.Append(near[role],AscensionArtH2.Source(part.Model,false),part.Position,part.Yaw,part.Scale);
                    AscensionArtH2.Append(far[role],AscensionArtH2.Source(part.Model,true),part.Position,part.Yaw,part.Scale);
                    if(AscensionArtH2.HasCollision(part.Model))AscensionArtH2.Append(collision,AscensionArtH2.Source(part.Model,true),part.Position,part.Yaw,part.Scale);
                    if(part.Model=="processional_stair" || part.Model=="gallery_rail")
                        AscensionArtH2.Append(porch,AscensionArtH2.Source(part.Model,true),part.Position,part.Yaw,part.Scale);
                }
                ScienceDistrictArt art=HeartPlanFR01.ScienceService(binding.Definition.Id,structure,porch);
                if(mineral.VertexCount>0)art.Surface("amethyst").Append(mineral);
                if(Game.Layout.HeartArtFR01==null) Game.Layout.HeartArtFR01=new HeartPresentationFR01(Game.Layout,transform.parent);
                return Game.Layout.HeartArtFR01.StageScienceDistrict(binding.Definition.Id,transform,_layout.style,near,far,collision,
                    art,true);
            }
            catch(Exception error){Log.Warn("Science service retained original: "+error.Message);return false;}
        }
    }

    public sealed partial class CityBuilder
    {
        private bool StageScienceCourt()
        {
            if(!L.GothicCivicH5)return false;
            try
            {
                if(L.HeartArtFR01==null)L.HeartArtFR01=new HeartPresentationFR01(L,_meshRoot);
                Dictionary<string,MeshBuilder> near=new Dictionary<string,MeshBuilder>(),far=new Dictionary<string,MeshBuilder>();
                MeshBuilder collision=new MeshBuilder(1);
                foreach(ArtPlacementH2 part in GothicHeartH3.CourtPlan(L))
                {
                    string role=AscensionArtH2.Role(part.Model);
                    if(!near.ContainsKey(role)){near.Add(role,new MeshBuilder(1));far.Add(role,new MeshBuilder(1));}
                    AscensionArtH2.Append(near[role],AscensionArtH2.Source(part.Model,false),part.Position,part.Yaw,part.Scale);
                    AscensionArtH2.Append(far[role],AscensionArtH2.Source(part.Model,true),part.Position,part.Yaw,part.Scale);
                    if(AscensionArtH2.HasCollision(part.Model))AscensionArtH2.Append(collision,AscensionArtH2.Source(part.Model,true),part.Position,part.Yaw,part.Scale);
                }
                ScienceDistrictArt art=HeartPlanFR01.ScienceCourt(L);
                HeartPlanFR01.ScienceApproach(art,L);
                return L.HeartArtFR01.StageScienceDistrict("home-court",_meshRoot,0,near,far,collision,art,false);
            }
            catch(Exception error){Log.Warn("Science court retained original: "+error.Message);return false;}
        }
    }
}
