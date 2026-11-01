using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum ScienceDistrictSelection { Retained, Prepared, WaitForClearance }

    public static partial class HeartPlanFR01
    {
        public static ScienceDistrictSelection ScienceSelection(bool requested,bool valid,bool current,bool occupied)
        {
            if(!valid)return ScienceDistrictSelection.Retained;
            if(requested!=current && occupied)return ScienceDistrictSelection.WaitForClearance;
            return requested?ScienceDistrictSelection.Prepared:ScienceDistrictSelection.Retained;
        }

        private struct ScienceTriangle : IEquatable<ScienceTriangle>
        {
            public Vector3 A,B,C;
            public bool Equals(ScienceTriangle other){return A.Equals(other.A)&&B.Equals(other.B)&&C.Equals(other.C);}
            public override bool Equals(object other){return other is ScienceTriangle && Equals((ScienceTriangle)other);}
            public override int GetHashCode(){unchecked{return A.GetHashCode()*397^B.GetHashCode()*31^C.GetHashCode();}}
        }

        /// <summary>All replaced surfaces, including the actual old and new roofs. Exact retained triangles are excluded.</summary>
        public static List<Bounds> ScienceChangedZones(MeshBuilder original,MeshBuilder candidate)
        {
            HashSet<ScienceTriangle> before=ScienceTriangles(original),after=ScienceTriangles(candidate);
            List<Bounds> zones=new List<Bounds>();
            foreach(ScienceTriangle triangle in before)if(!after.Contains(triangle))ScienceZone(zones,triangle);
            foreach(ScienceTriangle triangle in after)if(!before.Contains(triangle))ScienceZone(zones,triangle);
            return zones;
        }

        private static HashSet<ScienceTriangle> ScienceTriangles(MeshBuilder mesh)
        {
            HashSet<ScienceTriangle> triangles=new HashSet<ScienceTriangle>();IReadOnlyList<int> indices=mesh.Triangles(0);
            for(int i=0;i<indices.Count;i+=3)triangles.Add(new ScienceTriangle{A=mesh.Vertices[indices[i]],B=mesh.Vertices[indices[i+1]],C=mesh.Vertices[indices[i+2]]});
            return triangles;
        }

        private static void ScienceZone(List<Bounds> zones,ScienceTriangle triangle)
        {
            Bounds bounds=new Bounds(triangle.A,Vector3.zero);bounds.Encapsulate(triangle.B);bounds.Encapsulate(triangle.C);
            // Include feet resting just above the surface and bodies near the next collision envelope.
            bounds.Expand(.7f);zones.Add(bounds);
        }
    }
}
