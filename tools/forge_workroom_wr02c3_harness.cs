// Executes the actual engine-free Forge plan with minimal Vector3 stubs, not Unity physics or rendering.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float b) { return new Vector3(a.x * b, a.y * b, a.z * b); }
    }
}

namespace NeonSyndicate
{
    public static class MathUtil
    {
        public static bool IsFinite(UnityEngine.Vector3 v) { return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z); }
    }
}

public static class ForgeWorkroomHarness
{
    private static int _checks;
    private static void Check(bool condition, string message)
    { _checks++; if (!condition) throw new InvalidOperationException(message); }

    private static string At(List<NeonSyndicate.ForgeSurfaceSectionWR02C3> sections, float x, float z)
    {
        foreach (NeonSyndicate.ForgeSurfaceSectionWR02C3 part in sections)
            if (Math.Abs(part.Center.x - x) < part.Size.x * .5f && Math.Abs(part.Center.z - z) < part.Size.z * .5f) return part.Surface;
        return "";
    }

    private static void VerifyBox(string id, UnityEngine.Vector3 size)
    {
        List<NeonSyndicate.ForgeSurfaceSectionWR02C3> sections = NeonSyndicate.HeartPlanFR01.ForgeSurfacePlanWR02C3(id, size);
        Check(sections.Count > 1, id + " has no material divisions");
        double volume = 0;
        for (int i = 0; i < sections.Count; i++)
        {
            NeonSyndicate.ForgeSurfaceSectionWR02C3 a = sections[i];
            Check(a.Size.x > 0 && a.Size.y > 0 && a.Size.z > 0, id + " has an empty section");
            Check(Math.Abs(a.Center.x) + a.Size.x * .5f <= size.x * .5f + .0001f &&
                Math.Abs(a.Center.y) + a.Size.y * .5f <= size.y * .5f + .0001f &&
                Math.Abs(a.Center.z) + a.Size.z * .5f <= size.z * .5f + .0001f, id + " extends outside its original solid");
            Check(a.Surface == "Masonry" || a.Surface == "CarvedTrim" || a.Surface == "SlateTimber", id + " uses an unknown surface");
            volume += a.Size.x * a.Size.y * a.Size.z;
            for (int j = 0; j < i; j++)
            {
                NeonSyndicate.ForgeSurfaceSectionWR02C3 b = sections[j];
                bool overlap = Math.Abs(a.Center.x - b.Center.x) < (a.Size.x + b.Size.x) * .5f - .0001f &&
                    Math.Abs(a.Center.y - b.Center.y) < (a.Size.y + b.Size.y) * .5f - .0001f &&
                    Math.Abs(a.Center.z - b.Center.z) < (a.Size.z + b.Size.z) * .5f - .0001f;
                Check(!overlap, id + " has overlapping interior volume");
            }
        }
        Check(Math.Abs(volume - size.x * size.y * size.z) < .001, id + " leaves a gap in its original box");
        Check(NeonSyndicate.HeartPlanFR01.ForgeSurfacePlanWR02C3(id, size + new UnityEngine.Vector3(1, 0, 0)).Count == 0, id + " accepted the wrong dimensions");
    }

    public static int Main(string[] args)
    {
        try
        {
            VerifyBox("interior_floor", new UnityEngine.Vector3(16, .212f, 18));
            VerifyBox("west_room_wall", new UnityEngine.Vector3(.3f, 5, 18));
            VerifyBox("east_room_wall", new UnityEngine.Vector3(.3f, 5, 18));
            VerifyBox("rear_room_wall", new UnityEngine.Vector3(16, 5, .3f));
            Check(NeonSyndicate.HeartPlanFR01.ForgeSurfacePlanWR02C3("front_door", new UnityEngine.Vector3(16, 5, .3f)).Count == 0, "A doorway was admitted");
            Check(!NeonSyndicate.HeartPlanFR01.ForgeSurfaceSourceWR02C3("interior_floor", new UnityEngine.Vector3(0, -.094f, 9), new UnityEngine.Vector3(16, .212f, 18), new UnityEngine.Vector3(0, 1, 0)), "A rotated source was admitted");
            Check(!NeonSyndicate.HeartPlanFR01.ForgeSurfaceSourceWR02C3("interior_floor", new UnityEngine.Vector3(float.NaN, -.094f, 9), new UnityEngine.Vector3(16, .212f, 18), UnityEngine.Vector3.zero), "A nonfinite source was admitted");
            using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(args[0])))
            {
                UnityEngine.Vector3 service = UnityEngine.Vector3.zero;
                foreach (JsonElement anchor in document.RootElement.GetProperty("anchors").EnumerateArray())
                    if (anchor.GetProperty("id").GetString() == "service")
                    {
                        JsonElement p = anchor.GetProperty("position");
                        service = new UnityEngine.Vector3(p[0].GetSingle(), p[1].GetSingle(), p[2].GetSingle());
                    }
                List<NeonSyndicate.ForgeSurfaceSectionWR02C3> floor = NeonSyndicate.HeartPlanFR01.ForgeSurfacePlanWR02C3("interior_floor", new UnityEngine.Vector3(16, .212f, 18));
                Check(At(floor, 0, 3 - 9) == "CarvedTrim", "The actual entrance approach is not on the stone path");
                Check(At(floor, service.x, service.z - 9) == "CarvedTrim", "The actual Forge service is not reached by the stone path");
                Check(At(floor, 4, service.z - 9) == "Masonry", "The workroom lost its contrasting floor");
            }
            Console.WriteLine("Forge plan: " + _checks + " independent geometry and existing-anchor checks passed. Unity: NOT RUN.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
}
