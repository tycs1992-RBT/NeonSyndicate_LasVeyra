// Sample the production model and production drive with a declared, synthetic lane occupant.
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using NeonSyndicate;
using UnityEngine;

class FreightExport
{
    static void V(BinaryWriter w,Vector3 p){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
    static void Main(string[] args)
    {
        string output=args[0];
        CityLayout world=new CityLayout(61724);world.Generate();
        List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,NeonSyndicate.Tests.InhabitedCityBindings04CTests.ReadCatalog());
        world.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues);
        ForgeFreightRun run=ForgeFreightRun.Find(world);
        if(run==null)throw new Exception("Canonical freight run is missing");
        ForgeFreightModel model=new ForgeFreightModel();
        using(BinaryWriter b=new BinaryWriter(File.Create(Path.Combine(output,"team.bin"))))
        {
            b.Write(model.Pieces.Count);
            foreach(ForgeFreightModel.Piece piece in model.Pieces)
            {
                b.Write(piece.Joint);Color c=ForgeFreightModel.Tint(piece.Surface);b.Write(c.r);b.Write(c.g);b.Write(c.b);
                MeshBuilder m=piece.Mesh;b.Write(m.VertexCount);
                for(int i=0;i<m.VertexCount;i++){V(b,m.Vertices[i]);V(b,m.Normals[i]);}
                IReadOnlyList<int> tri=m.Triangles(0);b.Write(tri.Count);foreach(int i in tri)b.Write(i);
            }
        }
        bool occupied=false;
        Vector3 crossing=run.Point(6.1f,out Vector3 fwd)+Vector3.up*.9f;
        ForgeFreightDrive drive=new ForgeFreightDrive(run,run.Start(),f=>!occupied ||
            (!ForgeFreightEnvelope.Animal(f).Contains(crossing,.35f,.9f) && !ForgeFreightEnvelope.Wagon(f).Contains(crossing,.35f,.9f)),
            (ref ForgeFreightFrame f)=>true);
        ForgeFreightModel.JointPose[] poses=new ForgeFreightModel.JointPose[ForgeFreightModel.Count];
        using(BinaryWriter b=new BinaryWriter(File.Create(Path.Combine(output,"motion.bin"))))
        using(StreamWriter csv=new StreamWriter(Path.Combine(output,"motion.csv")))
        {
            const int frames=240;b.Write(frames);b.Write(poses.Length);
            csv.WriteLine("frame,seconds,metres,speed,waiting,occupant,yaw_offset");
            for(int i=0;i<frames;i++)
            {
                float clock=i/16f;occupied=clock>=3.75f && clock<8.15f;
                for(int k=0;k<3;k++)drive.Step(1f/48,true);
                ForgeFreightFrame f=drive.Frame;ForgeFreightModel.Pose(run,f,drive.Effort,drive.Clock,poses);
                V(b,f.Animal);V(b,f.Axle);V(b,occupied?crossing:new Vector3(0,-99,0));
                foreach(ForgeFreightModel.JointPose p in poses)
                {
                    V(b,p.Position);V(b,p.Rotation*Vector3.right*p.Scale.x);
                    V(b,p.Rotation*Vector3.up*p.Scale.y);V(b,p.Rotation*Vector3.forward*p.Scale.z);
                }
                csv.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:F3},{2:F4},{3:F3},{4},{5},{6:F3}",
                    i,clock,f.Distance,drive.Speed,drive.Waiting,occupied,Vector3.Angle(f.Forward,f.WagonForward)));
            }
        }
        Console.WriteLine("Lane "+run.Lane.Id+", "+run.Length+" m, "+model.Pieces.Count+" material/joint meshes and "+poses.Length+" joints.");
        if(args.Length>1)
        {
            Type t=typeof(ForgeFreightDrive).Assembly.GetType("NeonSyndicate.Tests.ForgeFreightTests",true);
            object tests=Activator.CreateInstance(t);int count=0;
            foreach(MethodInfo test in t.GetMethods())if(test.IsDefined(typeof(NUnit.Framework.TestAttribute),false))
            {test.Invoke(tests,null);Console.WriteLine("PASS "+test.Name);count++;}
            if(count<8)throw new Exception("Freight tests were not discovered");
        }
    }
}
