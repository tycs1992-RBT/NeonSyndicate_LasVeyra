// Execute the actual selection and changed-surface planner with small math stubs. Not Unity physics.
using System;
using System.Collections.Generic;
using NeonSyndicate;
using UnityEngine;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero {get{return new Vector3();}}
        public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
        public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
        public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}
    }
    public struct Bounds
    {
        public Vector3 center,size;
        public Bounds(Vector3 c,Vector3 s){center=c;size=s;}
        public Vector3 min {get{return center-size*.5f;}}
        public Vector3 max {get{return center+size*.5f;}}
        public void Encapsulate(Vector3 p)
        {
            Vector3 a=min,b=max;
            a=new Vector3(Math.Min(a.x,p.x),Math.Min(a.y,p.y),Math.Min(a.z,p.z));
            b=new Vector3(Math.Max(b.x,p.x),Math.Max(b.y,p.y),Math.Max(b.z,p.z));center=(a+b)*.5f;size=b-a;
        }
        public void Expand(float amount){size+=new Vector3(amount,amount,amount);}
    }
}
namespace NeonSyndicate
{
    public sealed class MeshBuilder
    {
        public readonly List<Vector3> Vertices=new List<Vector3>();
        private readonly List<int> indices=new List<int>();
        public IReadOnlyList<int> Triangles(int sub){return indices;}
        public void Face(Vector3 a,Vector3 b,Vector3 c){int n=Vertices.Count;Vertices.Add(a);Vertices.Add(b);Vertices.Add(c);indices.Add(n);indices.Add(n+1);indices.Add(n+2);}
    }
}
class ScienceClearanceHarness
{
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static bool Contains(List<Bounds> zones,Vector3 p)
    {
        foreach(Bounds b in zones)if(p.x>=b.min.x && p.y>=b.min.y && p.z>=b.min.z && p.x<=b.max.x && p.y<=b.max.y && p.z<=b.max.z)return true;
        return false;
    }
    static int Main()
    {
        foreach(bool request in new[]{false,true})foreach(bool valid in new[]{false,true})foreach(bool current in new[]{false,true})foreach(bool occupied in new[]{false,true})
        {
            ScienceDistrictSelection choice=HeartPlanFR01.ScienceSelection(request,valid,current,occupied);
            if(!valid)Check(choice==ScienceDistrictSelection.Retained,"Invalid prepared geometry must restore the source pair.");
            else if(request!=current && occupied)Check(choice==ScienceDistrictSelection.WaitForClearance,"Occupied changes must retain both present owners.");
            else Check((choice==ScienceDistrictSelection.Prepared)==request && choice!=ScienceDistrictSelection.WaitForClearance,"Clear or unchanged selection must match explicit intent.");
        }
        MeshBuilder before=new MeshBuilder(),after=new MeshBuilder();
        before.Face(new Vector3(-2,0,0),new Vector3(2,0,0),new Vector3(0,0,2));
        after.Face(new Vector3(-2,0,0),new Vector3(2,0,0),new Vector3(0,0,2));
        Check(HeartPlanFR01.ScienceChangedZones(before,after).Count==0,"Identical retained floor is excluded.");
        before.Face(new Vector3(-4,37,12),new Vector3(4,37,12),new Vector3(0,37,16));
        after.Face(new Vector3(-4,15,12),new Vector3(4,15,12),new Vector3(0,15,16));
        List<Bounds> zones=HeartPlanFR01.ScienceChangedZones(before,after);
        Check(zones.Count==2,"Both removed and introduced surfaces must be checked.");
        Check(Contains(zones,new Vector3(0,37.3f,13)),"Old roof feet are covered above the old fixed clearance height.");
        Check(Contains(zones,new Vector3(0,15.3f,13)),"New roof feet are covered on restoration.");
        Check(!Contains(zones,new Vector3(0,1,1)),"Retained occupied room must not block activation.");
        Console.WriteLine("Science selection and clearance: "+checks+" checks passed. Math stubs, not Unity or physical gameplay.");return 0;
    }
}
