// Source geometry recorder only. No Unity import, rendering, physics or gameplay is emulated.
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using NeonSyndicate;
using UnityEngine;

public static class InhabitedGeometryExportProgram
{
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
    private static readonly FieldInfo Indices = typeof(MeshBuilder).GetField("_tris", BindingFlags.NonPublic | BindingFlags.Instance);
    private static float[] V(Vector3 value) { return new[] { value.x, value.y, value.z }; }
    private static float[] Point(JsonElement value) { return value.EnumerateArray().Select(n => n.GetSingle()).ToArray(); }
    private static void Write(string path, MeshBuilder mesh)
    {
        List<int> indices = ((List<int>[])Indices.GetValue(mesh))[0];
        using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(new byte[] { 67, 77, 72, 49 }); writer.Write(indices.Count);
            foreach (int index in indices)
            {
                Vector3 p = mesh.Vertices[index], n = mesh.Normals[index]; Vector2 uv = mesh.UVs[index];
                foreach (float value in new[] { p.x, p.y, p.z, n.x, n.y, n.z, uv.x, uv.y }) writer.Write(value);
            }
        }
    }
    private static Dictionary<Material, MeshBuilder> Upper(Building original, VenueExpanded04C expanded, bool detailed)
    {
        float width = expanded.width, depth = expanded.depth, bottom = expanded.occupiedHeight, top = original.Height;
        RectF footprint = new RectF(-width * .5f, 0f, width * .5f, depth);
        Building local = new Building { Footprint = footprint, Height = original.Height, Seed = original.Seed, Style = original.Style,
            GroundShops = original.GroundShops, Roof = original.Roof, Tiers = original.Tiers, NeonColor = original.NeonColor };
        if (local.Tiers != 1 || local.Roof == RoofType.Helipad) throw new InvalidDataException("Protected or tiered venue binding.");
        Dictionary<Material, MeshBuilder> output = new Dictionary<Material, MeshBuilder>();
        Func<Material, MeshBuilder> batch = material =>
        {
            MeshBuilder mesh;
            if (!output.TryGetValue(material, out mesh)) { mesh = new MeshBuilder(1); output.Add(material, mesh); }
            return mesh;
        };
        VisualRebirth04AArchitecture.Tier(local, footprint, bottom, top, 0, expanded.style, detailed, batch);
        VisualRebirth04AArchitecture.Crown(local, footprint, top, expanded.style, detailed, batch);
        return output;
    }
    private static object BuildingData(Building b)
    {
        return new { b.Seed, b.Height, b.Tiers, b.GroundShops, style = b.Style.ToString(), roof = b.Roof.ToString(),
            footprint = new[] { b.Footprint.X0, b.Footprint.Z0, b.Footprint.X1, b.Footprint.Z1 } };
    }
    public static void Main(string[] args)
    {
        VisualRebirth04AAssets.Folder = args[0]; string layouts = args[1], folder = args[3]; Directory.CreateDirectory(folder);
        JsonElement report = JsonDocument.Parse(File.ReadAllText(args[2])).RootElement;
        JsonElement seed = report.GetProperty("seeds").EnumerateArray().First(s => s.GetProperty("seed").GetInt32() == 61724);
        HashSet<string> selected = new HashSet<string>(args[4].Split(','));
        CityLayout city = new CityLayout(61724); city.Generate();
        List<Building> buildings = city.Blocks.SelectMany(block => block.Lots).Concat(city.DesertLots).SelectMany(lot => lot.Buildings).ToList();
        List<object> written = new List<object>();
        foreach (JsonElement binding in seed.GetProperty("venues").EnumerateArray())
        {
            string id = binding.GetProperty("id").GetString(); if (!selected.Contains(id)) continue;
            Building building = buildings.Single(b => b.Seed == binding.GetProperty("buildingSeed").GetInt32());
            float[] fp = Point(binding.GetProperty("footprint"));
            if (Math.Abs(fp[0] - building.Footprint.X0) > .001f || Math.Abs(fp[3] - building.Footprint.Z1) > .001f)
                throw new InvalidDataException("Source binding does not match generated parcel: " + id);
            VenueLayout04C layout = JsonSerializer.Deserialize<VenueLayout04C>(File.ReadAllText(Path.Combine(layouts, id + ".json")), Json);
            VenueExpanded04C expanded = InhabitedCity04CComposition.Expand(layout, binding.GetProperty("width").GetSingle(), binding.GetProperty("depth").GetSingle(), building.Height);
            List<object> parts = new List<object>(), collisions = new List<object>();
            foreach (VenueNode04C node in expanded.nodes)
            {
                if (node.kind == "upper04A")
                {
                    foreach (bool low in new[] { false, true })
                        foreach (KeyValuePair<Material, MeshBuilder> pair in Upper(building, expanded, !low))
                        {
                            string file = id + "_upper_" + pair.Key.Role + (low ? "_low" : "_high") + ".bytes";
                            Write(Path.Combine(folder, file), pair.Value);
                            parts.Add(new { nodeId = node.id, kind = "upper04A", file, role = pair.Key.Role, style = pair.Key.Style,
                                position = new float[3], scale = new[] { 1f, 1f, 1f }, euler = new float[3], low, solid = false, node.interior, progress = 0 });
                        }
                    collisions.Add(new { nodeId = node.id, kind = "box", position = new[] { 0f, (expanded.occupiedHeight + building.Height) * .5f, expanded.depth * .5f },
                        size = new[] { expanded.width, building.Height - expanded.occupiedHeight, expanded.depth }, euler = new float[3] });
                }
                else if (node.kind == "box")
                {
                    MeshBuilder box = new MeshBuilder(1); box.Tile = 2f;
                    box.Box(0, Vector3.zero, new Vector3(node.scale[0], node.scale[1], node.scale[2]), UVMode.World);
                    string file = id + "_" + node.id + ".bytes"; Write(Path.Combine(folder, file), box);
                    parts.Add(new { nodeId = node.id, node.kind, file, node.role, style = expanded.style, node.position,
                        scale = new[] { 1f, 1f, 1f }, node.euler, low = false, node.solid, node.interior, node.progress });
                    if (node.solid) collisions.Add(new { nodeId = node.id, kind = "box", node.position, size = node.scale, node.euler });
                }
                else if (node.kind == "mesh")
                {
                    parts.Add(new { nodeId = node.id, node.kind, node.source, node.model, node.role, style = expanded.style, node.position, node.scale, node.euler,
                        low = false, node.solid, node.interior, node.progress });
                    if (node.solid) collisions.Add(new { nodeId = node.id, kind = "mesh", node.source, node.model, low = true, node.position, node.scale, node.euler });
                }
            }
            List<object> baseline = new List<object>();
            foreach (KeyValuePair<Material, MeshBuilder> pair in GeometryProgram.Build(building, false))
            {
                string file = id + "_baseline_" + pair.Key.Role + ".bytes"; Write(Path.Combine(folder, file), pair.Value);
                baseline.Add(new { file, role = pair.Key.Role, style = pair.Key.Style, position = new float[3], scale = new[] { 1f, 1f, 1f }, euler = new float[3] });
            }
            object result = new { id, scope = "Exact production Composition.Expand and MeshBuilder source; inherited upper Tier/Crown composition. Native Unity execution NOT RUN.",
                binding, building = BuildingData(building), expanded, parts, collisions, baseline };
            File.WriteAllText(Path.Combine(folder, id + ".json"), JsonSerializer.Serialize(result, Json));
            written.Add(new { id, nodes = expanded.nodes.Length, parts = parts.Count, collisions = collisions.Count });
        }
        ExportNeighborhood(city, seed, folder);
        Console.WriteLine(JsonSerializer.Serialize(new { exported = written }, Json));
    }

    private static void ExportNeighborhood(CityLayout city, JsonElement seed, string folder)
    {
        HashSet<int> selected = new HashSet<int>(seed.GetProperty("venues").EnumerateArray().Where(v => v.TryGetProperty("buildingSeed", out JsonElement value) && value.ValueKind == JsonValueKind.Number).Select(v => v.GetProperty("buildingSeed").GetInt32()));
        Vector3 home = city.FindPoi("Veyra Lofts").Position;
        List<object> parts = new List<object>();
        int count = 0;
        foreach (Block block in city.Blocks)
        {
            if (Math.Abs(block.Curb.CenterX - home.x) > 180f || Math.Abs(block.Curb.CenterZ - home.z) > 180f) continue;
            // Context paving follows exact generated curb rectangles. Detailed production road dressing is omitted.
            MeshBuilder ground = new MeshBuilder(1);
            ground.Box(0, block.Curb.Center(CityLayout.CurbHeight * .5f), new Vector3(block.Curb.Width, CityLayout.CurbHeight, block.Curb.Depth), UVMode.World);
            string pavement = "context_curb_" + block.Id + ".bytes"; Write(Path.Combine(folder, pavement), ground);
            parts.Add(new { file = pavement, role = "paving", style = 0, position = new float[3], scale = new[] { 1f, 1f, 1f }, euler = new float[3] });
            foreach (Lot lot in block.Lots)
            foreach (Building building in lot.Buildings)
            {
                if (lot.CustomBuilt || selected.Contains(building.Seed) || !VisualRebirth04ACityPlan.CanTheme(building, lot)) continue;
                count++;
                foreach (KeyValuePair<Material, MeshBuilder> pair in GeometryProgram.Build(building, false))
                {
                    string file = "context_" + building.Seed + "_" + pair.Key.Role + ".bytes"; Write(Path.Combine(folder, file), pair.Value);
                    parts.Add(new { file, role = pair.Key.Role, style = pair.Key.Style, position = new float[3], scale = new[] { 1f, 1f, 1f }, euler = new float[3] });
                }
            }
        }
        File.WriteAllText(Path.Combine(folder, "neighborhood_context.json"), JsonSerializer.Serialize(new
        {
            scope = "Unchanged ordinary 04A Tier/Crown source at actual generated neighboring parcels, with exact curb rectangles. Road surfaces, signs, props, traffic and native lighting are omitted; context is not a full Unity scene.",
            home = V(home), unchangedBuildings = count, parts
        }, Json));
    }
}
