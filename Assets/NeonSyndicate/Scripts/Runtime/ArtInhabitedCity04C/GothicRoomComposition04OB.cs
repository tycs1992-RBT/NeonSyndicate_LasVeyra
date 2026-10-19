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
            }
        }
    }
}
