using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>Original reusable geometry. No imported OBJ or mutable static mesh cache is required.</summary>
    public static class CitadelGeometry03A2
    {
        public static string[] ModelIds()
        {
            return new[] { "arch", "column", "ring", "crystal", "root", "canopy", "fern", "flower", "banner", "fin", "plinth", "lancet" };
        }

        public static Mesh Build(string id, bool low)
        {
            Builder b = new Builder();
            switch (id)
            {
                case "arch": Arch(b, low); break;
                case "column": Column(b, low); break;
                case "ring": Ring(b, low ? 20 : 48, low ? 4 : 8); break;
                case "crystal": Crystal(b, low ? 5 : 7); break;
                case "root": Tree(b, low, false); break;
                case "canopy": Tree(b, low, true); break;
                case "fern": Fern(b, low); break;
                case "flower": Flower(b, low); break;
                case "banner": Banner(b, low); break;
                case "fin": Fin(b, low); break;
                case "plinth": Plinth(b, low); break;
                case "lancet": Lancet(b); break;
                default: throw new ArgumentException("Unknown citadel model: " + id, "id");
            }
            return b.Finish("Citadel 03A2 / " + id + (low ? " / distant" : " / detail"));
        }

        private static Vector3 Polar(float a, float r, float y)
        {
            return new Vector3((float)Math.Cos(a) * r, y, (float)Math.Sin(a) * r);
        }

        // Two circular arcs meet at a true pointed apex. The base is y=0, the opening is 2 m wide.
        private static void Arch(Builder b, bool low)
        {
            int steps = low ? 6 : 12;
            List<Vector3> points = new List<Vector3>();
            points.Add(new Vector3(-1f, 0f, 0f));
            for (int i = 0; i <= steps; i++)
            {
                double a = Math.PI - Math.PI / 3.0 * i / steps;
                points.Add(new Vector3(1f + 2f * (float)Math.Cos(a), 1f + 2f * (float)Math.Sin(a), 0f));
            }
            for (int i = 1; i <= steps; i++)
            {
                double a = Math.PI / 3.0 * (1.0 - i / (double)steps);
                points.Add(new Vector3(-1f + 2f * (float)Math.Cos(a), 1f + 2f * (float)Math.Sin(a), 0f));
            }
            points.Add(new Vector3(1f, 0f, 0f));
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = points[i], c = points[i + 1];
                // Separate stone voussoirs have tiny mortar gaps, not one flat painted arch.
                Vector3 gap = (c - a) * .012f;
                b.Beam(a + gap, c - gap, .19f, .30f);
            }
        }

        private static void Column(Builder b, bool low)
        {
            float[] y = { 0f, .06f, .12f, .18f, .23f, .30f, 1.65f, 1.73f, 1.80f, 1.9f, 2f };
            float[] r = { .30f, .30f, .25f, .25f, .20f, .17f, .14f, .18f, .21f, .27f, .27f };
            b.Lathe(y, r, low ? 8 : 24, low ? 0f : .04f);
        }

        private static void Ring(Builder b, int segments, int sides)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = i * (float)Math.PI * 2f / segments, c = (i + 1) * (float)Math.PI * 2f / segments;
                for (int j = 0; j < sides; j++)
                {
                    float u = j * (float)Math.PI * 2f / sides, v = (j + 1) * (float)Math.PI * 2f / sides;
                    Vector3 p = Polar(a, 1f + .045f * (float)Math.Cos(u), .045f * (float)Math.Sin(u));
                    Vector3 q = Polar(c, 1f + .045f * (float)Math.Cos(u), .045f * (float)Math.Sin(u));
                    Vector3 r = Polar(c, 1f + .045f * (float)Math.Cos(v), .045f * (float)Math.Sin(v));
                    Vector3 s = Polar(a, 1f + .045f * (float)Math.Cos(v), .045f * (float)Math.Sin(v));
                    Vector3 normal = Polar((a + c) * .5f, (float)Math.Cos((u + v) * .5f), (float)Math.Sin((u + v) * .5f));
                    b.Quad(p, q, r, s, normal);
                }
            }
        }

        private static void Crystal(Builder b, int sides)
        {
            Vector3[] centers = { Vector3.zero, new Vector3(-.025f,.13f,0f), new Vector3(.035f,.75f,.012f), new Vector3(.08f,1f,-.03f) };
            float[] radii = { 0f, .22f, .19f, 0f };
            for (int layer = 0; layer < 3; layer++)
                for (int i = 0; i < sides; i++)
                {
                    float a = i * (float)Math.PI * 2f / sides, c = (i + 1) * (float)Math.PI * 2f / sides;
                    Vector3 p = centers[layer] + Polar(a, radii[layer], 0f);
                    Vector3 q = centers[layer] + Polar(c, radii[layer], 0f);
                    Vector3 r = centers[layer + 1] + Polar(c, radii[layer + 1], 0f);
                    Vector3 s = centers[layer + 1] + Polar(a, radii[layer + 1], 0f);
                    b.Quad(p, q, r, s, Polar((a + c) * .5f, 1f, layer == 0 ? -.7f : (layer == 2 ? .5f : 0f)));
                }
        }

        private static void Tree(Builder b, bool low, bool leaves)
        {
            int branches = low ? 5 : 9;
            if (!leaves)
            {
                Vector3 last = Vector3.zero;
                int segments = low ? 5 : 9;
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 next = new Vector3(.12f * (float)Math.Sin(t * 4f), 3.6f * t, .08f * t);
                    b.Rod(last, next, .24f * (1f - (i - 1f) / segments * .8f), .24f * (1f - t * .8f), low ? 5 : 9);
                    last = next;
                }
                for (int i = 0; i < 5; i++) b.Rod(Polar(i * 1.256637f, .8f, .025f), Vector3.up * .7f, .025f, .18f, low ? 4 : 7);
            }
            for (int i = 0; i < branches; i++)
            {
                float a = i * 2.399963f;
                float rise = 2.45f + (i % 3) * .4f;
                Vector3 start = new Vector3(0f, rise, 0f);
                Vector3 elbow = Polar(a, .7f, rise + .6f), tip = Polar(a + .2f, 1.65f, rise + .9f);
                if (!leaves)
                {
                    b.Rod(start, elbow, .095f, .045f, low ? 4 : 7);
                    b.Rod(elbow, tip, .045f, .01f, low ? 4 : 7);
                }
                else
                {
                    int count = low ? 3 : 6;
                    for (int j = 0; j < count; j++)
                    {
                        float t = (j + 1f) / count;
                        Vector3 p = Vector3.Lerp(elbow, tip, t);
                        float side = (j % 2 == 0 ? .62f : -.62f);
                        b.Leaf(p, p + Polar(a + side, .83f, .12f - t * .24f), .27f);
                    }
                }
            }
        }

        private static void Fern(Builder b, bool low)
        {
            int fronds = low ? 4 : 7, steps = low ? 3 : 6;
            for (int i = 0; i < fronds; i++)
            {
                float a = i * 6.283185f / fronds;
                Vector3 side = Polar(a + 1.570796f, 1f, 0f), last = Vector3.zero;
                for (int j = 1; j <= steps; j++)
                {
                    float t = j / (float)steps;
                    Vector3 p = Polar(a, t * .85f, .12f + (float)Math.Sin(t * 2.4f) * .75f);
                    b.Rod(last, p, .015f, .008f, 4);
                    float span = .30f * (1f - t * .7f);
                    b.Leaf(p, p + side * span + Polar(a, .2f, -.08f), .09f);
                    b.Leaf(p, p - side * span + Polar(a, .2f, -.08f), .09f);
                    last = p;
                }
            }
        }

        private static void Flower(Builder b, bool low)
        {
            int petals = low ? 5 : 9;
            for (int i = 0; i < petals; i++)
            {
                float a = i * 6.283185f / petals;
                b.Leaf(Polar(a,.055f,.15f), Polar(a,.65f,.38f), .22f);
                b.Leaf(Polar(a + .3f,.06f,.17f), Polar(a + .3f,.34f,.72f), .16f);
            }
        }

        private static void Banner(Builder b, bool low)
        {
            b.ClothUV = true;
            int nx = low ? 4 : 10, ny = low ? 4 : 12;
            for (int y = 0; y < ny; y++)
                for (int x = 0; x < nx; x++)
                {
                    Vector3 p = Cloth(x/(float)nx,y/(float)ny), q = Cloth((x+1f)/nx,y/(float)ny);
                    Vector3 r = Cloth((x+1f)/nx,(y+1f)/ny), s = Cloth(x/(float)nx,(y+1f)/ny);
                    b.Quad(p,q,r,s,Vector3.forward); b.Quad(p,s,r,q,Vector3.back);
                }
        }
        private static Vector3 Cloth(float u, float v)
        {
            float hem = 1.8f - .32f * (1f - Math.Abs(u * 2f - 1f));
            return new Vector3(u - .5f, -v * hem, .07f * (float)Math.Sin(u*12f + v*3f) * v);
        }

        private static void Fin(Builder b, bool low)
        {
            int steps = low ? 5 : 12;
            for (int i = 0; i < steps; i++)
            {
                float a = i / (float)steps, c = (i + 1) / (float)steps;
                Vector3 p = new Vector3(-1.1f*(1f-a)*(1f-a), a*2.7f, 0f);
                Vector3 q = new Vector3(-1.1f*(1f-c)*(1f-c), c*2.7f, 0f);
                b.Beam(p,q,.12f,.20f);
            }
        }
        private static void Plinth(Builder b, bool low)
        {
            b.Lathe(new[] { 0f,.12f,.16f,.25f,.31f,.45f,.50f }, new[] { .8f,.8f,.67f,.67f,.57f,.57f,.62f }, low ? 8 : 20, 0f);
        }
        private static void Lancet(Builder b)
        {
            Vector3 a=new Vector3(0f,0f,0f), l=new Vector3(-.30f,.85f,0f), t=new Vector3(0f,1.6f,0f), r=new Vector3(.30f,.85f,0f);
            Vector3 front=new Vector3(0f,.85f,.09f), back=new Vector3(0f,.85f,-.09f);
            Vector3[] edge={a,l,t,r};
            for(int i=0;i<4;i++){b.Tri(edge[i],edge[(i+1)%4],front,Vector3.forward);b.Tri(edge[(i+1)%4],edge[i],back,Vector3.back);}
        }

        private sealed class Builder
        {
            public bool ClothUV;
            private readonly List<Vector3> _v = new List<Vector3>();
            private readonly List<Vector3> _n = new List<Vector3>();
            private readonly List<Vector2> _uv = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();
            public void Tri(Vector3 a, Vector3 c, Vector3 d, Vector3 outward)
            {
                Vector3 n = Vector3.Cross(c-a,d-a);
                if(n.sqrMagnitude < 1e-12f) return;
                if(Vector3.Dot(n,outward)<0f){Vector3 tmp=c;c=d;d=tmp;n=-n;}
                n.Normalize(); int first=_v.Count;
                Vertex(a,n);Vertex(c,n);Vertex(d,n);
                _triangles.Add(first);_triangles.Add(first+1);_triangles.Add(first+2);
            }
            public void Quad(Vector3 a,Vector3 c,Vector3 d,Vector3 e,Vector3 outward){Tri(a,c,d,outward);Tri(a,d,e,outward);}
            private void Vertex(Vector3 p,Vector3 n)
            {
                _v.Add(p);_n.Add(n);
                float x=Math.Abs(n.x),y=Math.Abs(n.y),z=Math.Abs(n.z);
                _uv.Add(ClothUV?new Vector2(p.x+.5f,-p.y/1.8f):(y>x&&y>z?new Vector2(p.x,p.z):(x>z?new Vector2(p.z,p.y):new Vector2(p.x,p.y))));
            }
            public void Rod(Vector3 a, Vector3 c,float r0,float r1,int sides)
            {
                Vector3 axis=(c-a).normalized;
                Vector3 right=Vector3.Cross(axis,Math.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized;
                Vector3 up=Vector3.Cross(axis,right).normalized;
                for(int i=0;i<sides;i++)
                {
                    float t=i*6.283185f/sides,u=(i+1)*6.283185f/sides;
                    Vector3 n0=right*(float)Math.Cos(t)+up*(float)Math.Sin(t),n1=right*(float)Math.Cos(u)+up*(float)Math.Sin(u);
                    Vector3 p=a+n0*r0,q=a+n1*r0,r=c+n1*r1,s=c+n0*r1;
                    Quad(p,q,r,s,n0+n1);Tri(a,q,p,-axis);Tri(c,s,r,axis);
                }
            }
            public void Beam(Vector3 a,Vector3 c,float width,float depth)
            {
                Vector3 direction=(c-a).normalized;
                Vector3 side=new Vector3(-direction.y,direction.x,0f)*width*.5f;
                Vector3 z=Vector3.forward*depth*.5f;
                Vector3 p=a-side-z,q=a+side-z,r=c+side-z,s=c-side-z;
                Quad(p,s,r,q,Vector3.back);Quad(p+2f*z,q+2f*z,r+2f*z,s+2f*z,Vector3.forward);
                Quad(p,q,q+2f*z,p+2f*z,-direction);Quad(s,s+2f*z,r+2f*z,r,direction);
                Quad(p,p+2f*z,s+2f*z,s,-side);Quad(q,r,r+2f*z,q+2f*z,side);
            }
            public void Lathe(float[] heights,float[] radii,int sides,float flutes)
            {
                for(int j=0;j<heights.Length-1;j++)for(int i=0;i<sides;i++)
                {
                    float a=i*6.283185f/sides,c=(i+1)*6.283185f/sides;
                    float fa=1f+flutes*(float)Math.Sin(a*8f),fc=1f+flutes*(float)Math.Sin(c*8f);
                    Vector3 p=Polar(a,radii[j]*fa,heights[j]),q=Polar(c,radii[j]*fc,heights[j]);
                    Vector3 r=Polar(c,radii[j+1]*fc,heights[j+1]),s=Polar(a,radii[j+1]*fa,heights[j+1]);
                    Quad(p,q,r,s,Polar((a+c)*.5f,1f,0f));
                    if(j==0)Tri(Vector3.up*heights[0],q,p,Vector3.down);
                    if(j==heights.Length-2)Tri(Vector3.up*heights[j+1],s,r,Vector3.up);
                }
            }
            public void Leaf(Vector3 start,Vector3 end,float width)
            {
                Vector3 axis=end-start,side=Vector3.Cross(axis,Math.Abs(axis.normalized.y)>.9f?Vector3.right:Vector3.up).normalized*width;
                Vector3 normal=Vector3.Cross(axis,side).normalized,center=Vector3.Lerp(start,end,.43f);
                Vector3 l=center-side,r=center+side,fold=center+normal*width*.18f;
                Vector3[] edge={start,l,end,r};
                for(int i=0;i<4;i++){Tri(edge[i],edge[(i+1)%4],fold,normal);Tri(edge[(i+1)%4],edge[i],fold,-normal);}
            }
            public Mesh Finish(string name)
            {
                Mesh mesh = new Mesh { name=name,indexFormat=_v.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
                mesh.SetVertices(_v);mesh.SetNormals(_n);mesh.SetUVs(0,_uv);mesh.SetTriangles(_triangles,0);
                mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
            }
        }
    }
}
