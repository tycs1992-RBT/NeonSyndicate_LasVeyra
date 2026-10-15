// Offline CPU geometry/coverage recorder only. These adapters do not emulate Unity rendering, physics or imports.
using System;
namespace UnityEngine
{
 public struct Vector2 {
  public float x,y; public Vector2(float a,float b){x=a;y=b;} public static Vector2 zero=>new Vector2(); public static Vector2 right=>new Vector2(1,0);public static Vector2 up=>new Vector2(0,1);public static Vector2 one=>new Vector2(1,1); public float magnitude=>(float)Math.Sqrt(x*x+y*y); public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);
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
 }
}
