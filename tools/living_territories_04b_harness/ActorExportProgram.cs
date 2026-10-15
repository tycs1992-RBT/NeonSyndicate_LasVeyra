// Records the actual game builders and procedural pose solvers. No Unity runtime or live AI is emulated.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NeonSyndicate;
using UnityEngine;

public sealed class ActorRequest
{
    public string id, kind, species, region, siteId;
    public int seed, style, detail = 1;
    public int worldSeed = 61724, slotIndex, staffIndex;
    public bool hearth;
    public bool flying;
    public float time = 2f;
}

public static class ActorExportProgram
{
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
    private static float[] V(Vector3 p) { return new[] { p.x, p.y, p.z }; }
    private static float[] Q(Quaternion q) { return new[] { q.x, q.y, q.z, q.w }; }
    private static float[] C(Color c) { return new[] { c.r, c.g, c.b, c.a }; }

    public static void Main(string[] args)
    {
        ActorRequest[] requests = JsonSerializer.Deserialize<ActorRequest[]>(File.ReadAllText(args[0]), Json);
        Directory.CreateDirectory(args[1]);
        List<object> actors = new List<object>();
        foreach (ActorRequest request in requests)
        {
            if (!string.IsNullOrEmpty(request.siteId)) request.seed = TerritoryEcologyRules04B.Seed(request.worldSeed, request.siteId, request.slotIndex);
            BodyData mesh;
            string[] names;
            int[] parents;
            Vector3[] bind, local;
            Quaternion[] rotation;
            bool regalia = false;
            object appearance;
            if (request.kind == "human")
            {
                Appearance look = string.IsNullOrEmpty(request.region) ? Appearance.Random(new System.Random(request.seed)) : TerritoryEcologyRules04B.StaffLook(request.region, request.seed);
                RigLayout rig = RigLayout.From(look);
                mesh = BodyBuilder.Build(look, rig, request.detail);
                HumanoidPose pose = new HumanoidPose(rig);
                PoseInput input = PoseInput.Idle();
                for (int f = 0; f < Math.Max(1, request.time * 60); f++) pose.Solve(ref input, 1f / 60f);
                names = RigLayout.Names; parents = RigLayout.Parent; bind = rig.World;
                local = (Vector3[])rig.Local.Clone(); local[RigLayout.Hips] = pose.HipsPosition;
                rotation = pose.Local; appearance = look;
            }
            else
            {
                if (Array.IndexOf(CreatureLooks.Species, request.species) < 0)
                    throw new InvalidDataException("Unknown source species: " + request.species);
                CreatureLook look = CreatureLooks.For(request.species, request.seed);
                CreatureBody body = CreatureBuilder.Build(look, request.detail);
                CreaturePose pose = new CreaturePose(body);
                CreaturePoseInput input = CreaturePoseInput.Idle(Vector3.zero, 0f);
                input.Flying = request.flying; input.Grounded = !request.flying;
                input.Altitude = request.flying ? 12f : 0f;
                for (int f = 0; f < Math.Max(1, request.time * 60); f++)
                {
                    input.Time = f / 60f;
                    pose.Solve(ref input, 1f / 60f);
                }
                mesh = body.Mesh; names = body.Skeleton.Names.ToArray(); parents = body.Skeleton.Parents.ToArray();
                bind = Enumerable.Range(0, names.Length).Select(body.Skeleton.ModelPosition).ToArray();
                local = pose.LocalPositions; rotation = pose.LocalRotations; regalia = look.CrystalRegalia; appearance = look;
            }
            Vector3[] positions = new Vector3[names.Length];
            Quaternion[] rotations = new Quaternion[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int parent = parents[i];
                positions[i] = parent < 0 ? local[i] : positions[parent] + rotations[parent] * local[i];
                rotations[i] = parent < 0 ? rotation[i] : rotations[parent] * rotation[i];
            }
            Vector3[] vertices = new Vector3[mesh.Vertices.Count];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector3 low = Vector3.one * float.MaxValue, high = Vector3.one * float.MinValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                BoneWeight w = mesh.Weights[i];
                int[] ids = { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 };
                float[] weights = { w.weight0, w.weight1, w.weight2, w.weight3 };
                for (int j = 0; j < 4; j++)
                {
                    if (weights[j] <= 0f) continue;
                    int b = ids[j];
                    vertices[i] += (positions[b] + rotations[b] * (mesh.Vertices[i] - bind[b])) * weights[j];
                    normals[i] += rotations[b] * mesh.Normals[i] * weights[j];
                }
                normals[i] = normals[i].normalized;
                if (!float.IsFinite(vertices[i].x + vertices[i].y + vertices[i].z + normals[i].sqrMagnitude))
                    throw new InvalidDataException("Non-finite source actor geometry: " + request.id);
                low = Vector3.Min(low, vertices[i]); high = Vector3.Max(high, vertices[i]);
            }
            string file = request.id + ".bytes";
            using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(args[1], file))))
            {
                writer.Write(new byte[] { 67, 77, 72, 49 }); writer.Write(mesh.Triangles.Count);
                foreach (int index in mesh.Triangles)
                {
                    Vector3 p = vertices[index], n = normals[index]; Vector2 uv = mesh.Palette[index];
                    foreach (float value in new[] { p.x, p.y, p.z, n.x, n.y, n.z, uv.x, uv.y }) writer.Write(value);
                }
            }
            actors.Add(new { request, file, vertices = vertices.Length, triangles = mesh.Triangles.Count / 3,
                bounds_min = V(low), bounds_max = V(high), palette = regalia ? "quartz_regalia" : "character", appearance,
                staffOffset = V(TerritoryEcologyRules04B.StaffOffset(request.staffIndex, request.hearth)),
                staffYaw = TerritoryEcologyRules04B.StaffYaw(request.staffIndex),
                bones = Enumerable.Range(0, names.Length).Select(i => new { name = names[i], parent = parents[i], bind = V(bind[i]), localPosition = V(local[i]), localRotation = Q(rotation[i]) }).ToArray(),
                pose = "Static CPU-skinned frame from actual procedural pose solver, not live gameplay." });
        }
        File.WriteAllText(Path.Combine(args[1], "actors.json"), JsonSerializer.Serialize(new { actors }, Json));
        foreach (bool drake in new[] { false, true })
        {
            Color[] albedo = new Color[256], properties = new Color[256], emission = new Color[256];
            for (int row = 0; row < 16; row++) for (int column = 0; column < 16; column++)
            {
                int i = row * 16 + column;
                albedo[i] = CharacterPaletteData.Albedo(row, column);
                properties[i] = CharacterPaletteData.MetalDetailSmooth(row, column);
                emission[i] = CharacterPaletteData.Emission(row, column);
            }
            if (drake)
            {
                DRAKE_CELLS
            }
            File.WriteAllText(Path.Combine(args[1], drake ? "quartz_regalia_palette.json" : "character_palette.json"),
                JsonSerializer.Serialize(new { albedo = albedo.Select(C).ToArray(), properties = properties.Select(C).ToArray(), emission = emission.Select(C).ToArray() }, Json));
        }
        Console.WriteLine("Exported " + actors.Count + " actual source actors as static posed meshes.");
    }

    private static void Cell(Color[] albedo, Color[] properties, Color[] emission, int row, int column, Color colour, Color surface, Color glow)
    {
        int i = row * CharacterPaletteData.Size + column;
        albedo[i] = colour; properties[i] = surface; emission[i] = glow;
    }
}
