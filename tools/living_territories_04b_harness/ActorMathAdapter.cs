// Offline source-geometry recorder only. These arithmetic adapters do not emulate Unity, AI, physics or imports.
using System;
namespace UnityEngine
{
 public struct Vector2 {
  public float x,y; public Vector2(float a,float b){x=a;y=b;} public static Vector2 zero=>new Vector2(); public static Vector2 right=>new Vector2(1,0);public static Vector2 up=>new Vector2(0,1);public static Vector2 one=>new Vector2(1,1); public float magnitude=>(float)Math.Sqrt(x*x+y*y); public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);
  public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
  public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
  public static Vector2 operator /(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);
  public static bool operator ==(Vector2 a,Vector2 b)=>(a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)<9.99999944e-11f;
  public static bool operator !=(Vector2 a,Vector2 b)=>!(a==b);
  public override bool Equals(object o)=>o is Vector2 v&&x==v.x&&y==v.y;
  public override int GetHashCode()=>HashCode.Combine(x,y);
 }
 public struct Vector2Int { public int x,y; public Vector2Int(int a,int b){x=a;y=b;} public static bool operator ==(Vector2Int a,Vector2Int b)=>a.x==b.x&&a.y==b.y;public static bool operator !=(Vector2Int a,Vector2Int b)=>!(a==b);public override bool Equals(object o)=>o is Vector2Int v&&this==v;public override int GetHashCode()=>HashCode.Combine(x,y); }
 public static class Random {public static int Range(int a,int b)=>throw new InvalidOperationException("Native Unity random is outside this source recorder; use actual seeded source factories.");}
 public struct Vector4 {public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}}
 public struct BoneWeight {public int boneIndex0,boneIndex1,boneIndex2,boneIndex3;public float weight0,weight1,weight2,weight3;}
 public struct Color {public float r,g,b,a;public Color(float x,float y,float z,float w=1){r=x;g=y;b=z;a=w;}public static Color black=>new Color(0,0,0);public static Color operator *(Color c,float k)=>new Color(c.r*k,c.g*k,c.b*k,c.a*k);}
 public struct Bounds {
  public Vector3 center,size; public Bounds(Vector3 c,Vector3 s){center=c;size=s;}
  public Vector3 extents{get=>size*.5f;set=>size=value*2;}
  public Vector3 min{get=>center-extents;set{Vector3 hi=max;SetMinMax(value,hi);}}
  public Vector3 max{get=>center+extents;set{Vector3 lo=min;SetMinMax(lo,value);}}
  public void SetMinMax(Vector3 lo,Vector3 hi){center=(lo+hi)*.5f;size=hi-lo;}
  public void Encapsulate(Vector3 p){SetMinMax(Vector3.Min(min,p),Vector3.Max(max,p));}
  public void Expand(float amount){size+=Vector3.one*amount;}
  public void Expand(Vector3 amount){size+=amount;}
 }
 public struct Quaternion {
  public float x,y,z,w;public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
  public static Quaternion identity=>new Quaternion(0,0,0,1);
  public static Quaternion operator *(Quaternion a,Quaternion b)=>new Quaternion(a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,a.w*b.y+a.y*b.w+a.z*b.x-a.x*b.z,a.w*b.z+a.z*b.w+a.x*b.y-a.y*b.x,a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z);
  public static Vector3 operator *(Quaternion q,Vector3 v){Vector3 u=new Vector3(q.x,q.y,q.z);return v+2*q.w*Vector3.Cross(u,v)+2*Vector3.Cross(u,Vector3.Cross(u,v));}
 }
 public struct Vector3 {
  public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 zero=>new Vector3(); public static Vector3 one=>new Vector3(1,1,1);public static Vector3 down=>new Vector3(0,-1,0); public static Vector3 up=>new Vector3(0,1,0);
  public static Vector3 forward=>new Vector3(0,0,1); public static Vector3 back=>new Vector3(0,0,-1);
  public static Vector3 left=>new Vector3(-1,0,0); public static Vector3 right=>new Vector3(1,0,0);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
  public Vector3 normalized {get {float n=magnitude;return n>1e-5f?this/n:zero;}}
  public void Normalize(){this=normalized;}
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator -(Vector3 a)=>a*-1;
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
  public static Vector3 operator *(float b,Vector3 a)=>a*b;
  public static Vector3 operator /(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);
  public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
  public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
  public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
  public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
  public static Vector3 Min(Vector3 a,Vector3 b)=>new Vector3(Math.Min(a.x,b.x),Math.Min(a.y,b.y),Math.Min(a.z,b.z));
  public static Vector3 Max(Vector3 a,Vector3 b)=>new Vector3(Math.Max(a.x,b.x),Math.Max(a.y,b.y),Math.Max(a.z,b.z));
  public static Vector3 Normalize(Vector3 a)=>a.normalized;
  public static bool operator ==(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<9.99999944e-11f;
  public static bool operator !=(Vector3 a,Vector3 b)=>!(a==b);
  public override bool Equals(object o)=>o is Vector3 v&&x==v.x&&y==v.y&&z==v.z;
  public override int GetHashCode()=>HashCode.Combine(x,y,z);
 }
 public static class Mathf {
  public const float PI=(float)Math.PI; public const float Deg2Rad=PI/180f;public const float Rad2Deg=180f/PI;
  public static float Abs(float x)=>Math.Abs(x);public static int Abs(int x)=>Math.Abs(x);
  public static float Sin(float x)=>(float)Math.Sin(x);public static float Cos(float x)=>(float)Math.Cos(x);
  public static float Atan2(float a,float b)=>(float)Math.Atan2(a,b);public static float Sqrt(float x)=>(float)Math.Sqrt(x);
  public static float Exp(float x)=>(float)Math.Exp(x);public static float Min(float a,float b)=>Math.Min(a,b);
  public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);
  public static int Max(int a,int b)=>Math.Max(a,b);public static float Clamp(float x,float a,float b)=>Math.Min(b,Math.Max(a,x));
  public static int Clamp(int x,int a,int b)=>Math.Min(b,Math.Max(a,x));public static float Clamp01(float x)=>Clamp(x,0,1);
  public static int RoundToInt(float x)=>(int)Math.Round(x,MidpointRounding.ToEven);
  public static int FloorToInt(float x)=>(int)Math.Floor(x);public static int CeilToInt(float x)=>(int)Math.Ceiling(x);
  public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t); public static float Sign(float x)=>x>=0?1:-1;
  public static float Repeat(float t,float length)=>Clamp(t-(float)Math.Floor(t/length)*length,0,length);
  public static float DeltaAngle(float a,float b){float d=Repeat(b-a,360);return d>180?d-360:d;}
  public static float Floor(float x)=>(float)Math.Floor(x);
  public static float Pow(float a,float b)=>(float)Math.Pow(a,b);
  public static float Acos(float x)=>(float)Math.Acos(x);
  public static float Asin(float x)=>(float)Math.Asin(x);
  public static float InverseLerp(float a,float b,float x)=>a==b?0:Clamp01((x-a)/(b-a));
  public static float LerpAngle(float a,float b,float t)=>a+DeltaAngle(a,b)*Clamp01(t);
  public static float MoveTowards(float a,float b,float delta)=>Math.Abs(b-a)<=delta?b:a+Sign(b-a)*delta;
  public static bool Approximately(float a,float b)=>Math.Abs(b-a)<Math.Max(1e-6f*Math.Max(Math.Abs(a),Math.Abs(b)),1.121039e-44f);
 }
}
