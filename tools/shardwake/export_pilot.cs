// Offline harness. Pose and skin the production actor definitions for the software camera.
using System;
using System.IO;
using System.Reflection;
using NeonSyndicate;
using UnityEngine;

internal class ValeExport
{
    private static void V(BinaryWriter w,Vector3 p){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
    private static void Actor(string path,Appearance look,int detail)
    {
        RigLayout rig=RigLayout.From(look);BodyData body=BodyBuilder.Build(look,rig,detail);
        HumanoidPose pose=new HumanoidPose(rig);PoseInput input=PoseInput.Idle();
        for(int i=0;i<45;i++)pose.Solve(ref input,1/60f);
        Vector3[] bone=new Vector3[RigLayout.BoneCount];Quaternion[] rot=new Quaternion[RigLayout.BoneCount];
        for(int i=0;i<RigLayout.BoneCount;i++)
        {
            int parent=RigLayout.Parent[i];
            bone[i]=parent<0?pose.HipsPosition:bone[parent]+rot[parent]*rig.Local[i];
            rot[i]=parent<0?pose.Local[i]:rot[parent]*pose.Local[i];
        }
        using(BinaryWriter w=new BinaryWriter(File.Create(path)))
        {
            w.Write(body.Vertices.Count);
            for(int i=0;i<body.Vertices.Count;i++)
            {
                BoneWeight b=body.Weights[i];Vector3 p=Vector3.zero,n=Vector3.zero;
                int[] ids={b.boneIndex0,b.boneIndex1,b.boneIndex2,b.boneIndex3};float[] weights={b.weight0,b.weight1,b.weight2,b.weight3};
                for(int k=0;k<4;k++)
                {
                    int j=ids[k];p+=weights[k]*(bone[j]+rot[j]*(body.Vertices[i]-rig.World[j]));n+=weights[k]*(rot[j]*body.Normals[i]);
                }
                V(w,p);V(w,n.normalized);Vector2 uv=body.Palette[i];Color color=CharacterPaletteData.Albedo((int)(uv.y*16),(int)(uv.x*16));
                V(w,new Vector3(color.r,color.g,color.b)*255);
            }
            w.Write(body.Triangles.Count);foreach(int i in body.Triangles)w.Write(i);
        }
    }
    private static void Main(string[] args)
    {
        ValeCommission.Read(File.ReadAllText("Assets/ShardwakeVale/HearthCommission.json"));
        Actor(Path.Combine(args[0],"Malik.bin"),Appearance.Malik(),2);
        Actor(Path.Combine(args[0],"Dante.bin"),QuestWorld.Look("Dante",new System.Random(612)),1);
        if(args.Length<2)return;
        Type type=typeof(ShardwakeJourney).Assembly.GetType("NeonSyndicate.Tests.ShardwakeValeTests",true);
        object fixture=Activator.CreateInstance(type);int count=0;
        foreach(MethodInfo test in type.GetMethods())
            if(test.IsDefined(typeof(NUnit.Framework.TestAttribute),false))
            {test.Invoke(fixture,null);Console.WriteLine("PASS "+test.Name);count++;}
        if(count<9)throw new InvalidDataException("Missing pilot tests");
    }
}
