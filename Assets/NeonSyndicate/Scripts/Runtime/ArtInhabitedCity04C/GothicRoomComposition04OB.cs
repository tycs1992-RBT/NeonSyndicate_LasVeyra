using System.Collections.Generic;
namespace NeonSyndicate
{
    /// <summary>Revision-2-only room adaptation. Existing source and saved anchor names stay intact.</summary>
    public static class GothicRoomComposition04OB
    {
        public static void ResetStatics() { }
        public static void Prepare(VenueExpanded04C layout)
        {
            List<VenueNode04C> retained=new List<VenueNode04C>();
            foreach(VenueNode04C node in layout.nodes)
            {
                if(node.kind=="upper04A" || node.id=="interior_ceiling" || node.id.EndsWith("_original_mass"))continue;
                if(layout.id=="archive" && node.id=="rear_room_wall")continue;
                retained.Add(node);
            }
            layout.nodes=retained.ToArray();
            if(layout.id=="archive")
            {
                layout.roomDepth=layout.depth;layout.occupiedHeight=layout.originalHeight-.6f;
                List<VenueAnchor04C> anchors=new List<VenueAnchor04C>();
                foreach(VenueAnchor04C a in layout.anchors)
                {
                    // Stable original record IDs now lead through the real extended sanctuary, ending on its stepped dais.
                    float[] p=(float[])a.position.Clone();
                    if(a.id=="record2"){p=new[]{-5f,0f,28f};}
                    if(a.id=="record3"){p=new[]{0f,1.2f,43f};}
                    anchors.Add(new VenueAnchor04C{id=a.id,position=p,yaw=a.yaw});
                }
                layout.anchors=anchors.ToArray();
                List<VenueLight04C> lights=new List<VenueLight04C>(layout.lights);
                List<VenueNode04C> fittings=new List<VenueNode04C>(layout.nodes);
                for(int i=0;i<2 && lights.Count<6;i++)
                {
                    string id="gothic_depth_"+i;float z=28f+i*15f;
                    lights.Add(new VenueLight04C{id=id,position=new[]{5.8f,3.3f,z},color=new[]{1f,.65f,.3f},range=10f,intensity=1.15f});
                    fittings.Add(new VenueNode04C{id=id+"_fixture",kind="box",role="lamp",position=new[]{5.8f,3f,z},scale=new[]{.24f,.6f,.28f},euler=new float[3],solid=false,far=false,interior=true});
                }
                layout.lights=lights.ToArray();layout.nodes=fittings.ToArray();
            }
        }
    }
}
