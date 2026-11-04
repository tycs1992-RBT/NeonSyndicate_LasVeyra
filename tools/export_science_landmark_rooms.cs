// Optional camera context only. Read the real venue definition and expand its retained rooms,
// using the same local transforms as VenueView04C. Progress displays and native characters are omitted.
using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using NeonSyndicate;
using UnityEngine;

partial class Export
{
    static partial void AppendLandmarkRooms(List<VenueBinding04C> venues,Dictionary<string,MeshBuilder> context)
    {
        const string resources="Assets/NeonSyndicate/Resources/";
        // Select occurred before the profile moved the bindings; reuse the original bound objects.
        foreach(VenueBinding04C binding in venues)
        {
            string id=binding.Definition.Id;
            if(id!="lofts" && id!="forge" && id!="archive")continue;
            string json=File.ReadAllText(resources+"InhabitedCity04C/Layouts/"+id+".json");
            VenueLayout04C layout=new JavaScriptSerializer().Deserialize<VenueLayout04C>(json);
            VenueExpanded04C room=InhabitedCity04CComposition.Expand(layout,binding.Width,binding.Depth,binding.Building.Height);
            GothicRoomComposition04OB.Prepare(room);GothicHeartH3.PrepareRoom(room,id=="archive");
            Dictionary<string,MeshBuilder> parts=new Dictionary<string,MeshBuilder>();
            foreach(VenueNode04C node in room.nodes)
            {
                if(node.progress>0)continue;
                Vector3 at=V(node.position),scale=V(node.scale),angles=V(node.euler);
                Quaternion rotation=QMath.Euler(angles.x,angles.y,angles.z);
                MeshBuilder mesh;
                if(node.kind=="box")
                {
                    mesh=new MeshBuilder(1);mesh.Tile=2;mesh.Box(0,Vector3.zero,scale,UVMode.World);scale=Vector3.one;
                }
                else if(node.kind=="mesh")
                {
                    string owner=node.source=="04A"?"VisualRebirth04A":node.source=="04B"?"LivingTerritories04B":"InhabitedCity04C";
                    mesh=AscensionArtH2.Decode(File.ReadAllBytes(resources+owner+"/Meshes/"+node.model+"_high.bytes"));
                }
                else throw new InvalidDataException("Unexpected retained node "+node.kind);
                for(int i=0;i<mesh.VertexCount;i++)
                {
                    mesh.Vertices[i]=at+rotation*Vector3.Scale(mesh.Vertices[i],scale);
                    Vector3 n=mesh.Normals[i];
                    mesh.Normals[i]=rotation*new Vector3(n.x/scale.x,n.y/scale.y,n.z/scale.z).normalized;
                }
                string role=node.role=="metal"?"iron":node.role=="crystal"?"azure":node.role=="lamp"?"warm":
                    node.role=="timber" || node.role=="bark"?"root":"masonry";
                if(!parts.ContainsKey(role))parts[role]=new MeshBuilder(1);
                parts[role].Append(mesh);
            }
            Append(context,parts,binding.Origin,binding.Yaw);
            Console.WriteLine("Camera retained room "+id+" nodes "+room.nodes.Length);
        }
    }

    static Vector3 V(float[] a) { return new Vector3(a[0],a[1],a[2]); }
}
