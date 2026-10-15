// Offline CPU geometry/coverage recorder only. These adapters do not emulate Unity rendering, physics or imports.
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine.Rendering { public enum IndexFormat { UInt16, UInt32 } }
namespace UnityEngine {
 public class Material { public int Style; public string Role; }
 public class Mesh { }
 public struct BoneWeight { public int boneIndex0; public float weight0; }
 public struct Quaternion {
  public float yaw;public static Quaternion identity=>new Quaternion();
  public static Quaternion Euler(float x,float y,float z) {if(x!=0||z!=0)throw new NotSupportedException("Recorder supports yaw only");return new Quaternion{yaw=y};}
  public static Vector3 operator *(Quaternion q,Vector3 p){double a=q.yaw*Math.PI/180;return new Vector3((float)(p.x*Math.Cos(a)+p.z*Math.Sin(a)),p.y,(float)(-p.x*Math.Sin(a)+p.z*Math.Cos(a)));}
  public static Quaternion FromToRotation(Vector3 a,Vector3 b){throw new NotSupportedException("Not used in source architecture extraction");}
 }
}
namespace NeonSyndicate {
 public static class VisualRebirth04ASettings { public static bool Gardens=true; }
 public static class Log { public static void Warn(string text) {Console.Error.WriteLine(text);} }
 public static class VisualRebirth04AAssets {
  public static string Folder;
  private static Dictionary<string,Material> mats=new Dictionary<string,Material>();
  private static Dictionary<string,float[]> sources=new Dictionary<string,float[]>();
  public static Material Material(int style,string role) {string k=style+"/"+role;if(!mats.ContainsKey(k))mats[k]=new Material{Style=style,Role=role};return mats[k];}
  public static Mesh Mesh(string model,bool low) {if(!File.Exists(Path.Combine(Folder,model+(low?"_low.bytes":"_high.bytes"))))throw new FileNotFoundException(model);return new Mesh();}
  public static void Append(MeshBuilder target,string model,Vector3 at,Quaternion rotation,Vector3 scale,bool low=true) {
   string key=model+(low?"_low":"_high");if(!sources.TryGetValue(key,out float[] data)) {
    using(BinaryReader reader=new BinaryReader(File.OpenRead(Path.Combine(Folder,key+".bytes")))) {reader.ReadBytes(4);int count=reader.ReadInt32();data=new float[count*8];for(int i=0;i<data.Length;i++)data[i]=reader.ReadSingle();}sources[key]=data;
   }
   for(int i=0;i<data.Length;i+=24) {
    Vector3[] p=new Vector3[3],n=new Vector3[3];Vector2[] uv=new Vector2[3];
    for(int j=0;j<3;j++){int k=i+j*8;p[j]=at+rotation*new Vector3(data[k]*scale.x,data[k+1]*scale.y,data[k+2]*scale.z);n[j]=(rotation*new Vector3(data[k+3]/scale.x,data[k+4]/scale.y,data[k+5]/scale.z)).normalized;uv[j]=new Vector2(data[k+6],data[k+7]);}
    int start=target.VertexCount;target.Triangle(0,p[0],p[1],p[2],n[0],uv[0],uv[1],uv[2]);target.Normals[start+1]=n[1];target.Normals[start+2]=n[2];
   }
  }
 }
}
