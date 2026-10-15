using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;
using NeonSyndicate;
public static class PlacementProgram
{
    private static float[] Point(Vector3 p) { return new[] { p.x, p.y, p.z }; }
    public static int Main(string[] args)
    {
        JsonElement catalog = JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
        JsonElement forts = JsonDocument.Parse(File.ReadAllText(args[1])).RootElement;
        List<object> seeds = new List<object>();
        List<string> failures = new List<string>();
        foreach (int seed in new[] { 61724, 1, 2016, 77231, 92017 })
        {
            CityLayout layout = new CityLayout(seed); layout.Generate();
            List<Vector3> reserved = new List<Vector3>();
            foreach (JsonElement fort in forts.GetProperty("sites").EnumerateArray())
            {
                float[] p = fort.GetProperty("anchor").EnumerateArray().Select(n => n.GetSingle()).ToArray();
                reserved.Add(new Vector3(p[0], p[1], p[2]));
            }
            List<object> sites = new List<object>();
            foreach (JsonElement site in catalog.GetProperty("sites").EnumerateArray())
            {
                string id = site.GetProperty("id").GetString();
                float[] anchor = site.GetProperty("anchor").EnumerateArray().Select(n => n.GetSingle()).ToArray();
                List<Vector3> candidates = TerritoryPlacement.Candidates(anchor[0], anchor[2]);
                HashSet<string> unique = new HashSet<string>();
                List<float[]> viable = new List<float[]>();
                int reservedRejected = 0, layoutRejected = 0;
                foreach (Vector3 p in candidates)
                {
                    string key = p.x.ToString("R") + "/" + p.z.ToString("R");
                    if (!unique.Add(key)) failures.Add(id + ": duplicate candidate");
                    if (!StrongholdPlacement.RegionAllowed(p)) failures.Add(id + ": unsafe region candidate");
                    if (Mathf.Abs(p.x-anchor[0]) > 224f || Mathf.Abs(p.z-anchor[2]) > 224f) failures.Add(id+": unbounded search");
                    if (!TerritoryPlacement.CanReserve(p, reserved)) { reservedRejected++; continue; }
                    try
                    {
                        Vector3 ground;
                        bool result = TerritoryPlacement.Probe(layout, p, reserved, out ground);
                        if (result) failures.Add(id+": native placement was fabricated by offline host");
                        layoutRejected++;
                    }
                    catch (NativeSurveyRequired) { viable.Add(Point(p)); }
                }
                Vector3? first = viable.Count == 0 ? (Vector3?)null : new Vector3(viable[0][0],0f,viable[0][2]);
                if (first.HasValue) reserved.Add(first.Value);
                sites.Add(new { id, preferred = anchor, candidates = candidates.Count, reservedRejected,
                    layoutRejected, candidatesRequiringNativeSurvey = viable.Count,
                    provisionalSourceCandidate = first.HasValue ? Point(first.Value) : null,
                    status = first.HasValue ? "SOURCE EXCLUSIONS PASS; NATIVE SURVEY NOT RUN" : "NO SOURCE-SAFE CANDIDATE",
                    note = "Provisional reservations use first source-safe candidate and original preferred fort anchors. Actual native site/court placement may differ." });
            }
            seeds.Add(new { seed, sites });
        }
        Vector3 center = new Vector3(1120f,0f,-740f);
        List<Vector3> occupied = new List<Vector3> { center };
        if (TerritoryPlacement.CanReserve(center+Vector3.right*87.99f,occupied)) failures.Add("Reservation accepted less than 88m");
        if (!TerritoryPlacement.CanReserve(center+Vector3.right*88f,occupied)) failures.Add("Reservation rejected exact 88m boundary");
        if (TerritoryPlacement.CanReserve(center+Vector3.up*100f,occupied)) failures.Add("Height bypassed reservation");
        if (TerritoryPlacement.Candidates(float.NaN,0f).Count!=0) failures.Add("Nonfinite candidate accepted");
        if (TerritoryPlacement.CanReserve(center,null)) failures.Add("Null reservation accepted");
        Console.Write(JsonSerializer.Serialize(new { scope = "Unchanged CityLayout, StrongholdPlacement and TerritoryPlacement executed with managed math and an explicit native boundary that throws. No physics, live Interactable inventory, AI navigation or live installed counts are simulated.",
            seeds, failures, unityPhysics = "NOT RUN", liveSiteCoordinates = "NOT VERIFIED" }, new JsonSerializerOptions { WriteIndented=true }));
        return failures.Count == 0 ? 0 : 1;
    }
}
