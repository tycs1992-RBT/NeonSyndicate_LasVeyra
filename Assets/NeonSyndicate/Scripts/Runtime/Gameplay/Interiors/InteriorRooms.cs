using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a room piece is made of (CityInteriors picks the material).</summary>
    public enum InteriorSurface { Floor, Wall, Ceiling, Trim, Wood, DarkWood, Metal, DarkMetal, Fabric, Paper, Glass, Glow, Accent, Stone, Leather }

    /// <summary>
    /// One box or cylinder of a room, in the room frame (x right, y up, z from the door into the room). A box's Size is its
    /// full size and Yaw turns it about the vertical; a cylinder's Size.y is half its height (it spans Center.y plus or
    /// minus Size.y) and a cylinder with a Yaw lies on its side, turned that far about the room's z axis.
    /// </summary>
    public struct InteriorPiece
    {
        public string Name;
        public bool Cylinder;
        public Vector3 Center, Size;
        public float Yaw;
        public InteriorSurface Surface;
        public Color Tint;
        public bool Collider;
    }

    /// <summary>A light of a room, in the room frame.</summary>
    public struct InteriorLight
    {
        public Vector3 Position;
        public Color Color;
        public float Range, Intensity;
    }

    /// <summary>A person's place in a room: where they stand and which way they face (room yaw, 0 faces into the room).</summary>
    public struct InteriorSpot
    {
        public Vector3 Position;
        public float Yaw;
    }

    /// <summary>A complete room: shell, furniture, lights, the entry and exit points, the counter and the people.</summary>
    public sealed class InteriorPlan
    {
        public Vector3 Size;
        public readonly List<InteriorPiece> Pieces = new List<InteriorPiece>();
        public readonly List<InteriorLight> Lights = new List<InteriorLight>();
        /// <summary>Where the player arrives (just inside the door, facing in) and where the exit prompt sits.</summary>
        public Vector3 Entry, Exit;
        /// <summary>Where the counter prompt sits (the service or the keeper's line).</summary>
        public Vector3 Counter;
        public InteriorSpot Keeper;
        public readonly List<InteriorSpot> Patrons = new List<InteriorSpot>();
    }

    /// <summary>
    /// Generated interiors, pure room plans: a closed shell with a door gap in the front wall, then each kind's furniture
    /// from a seeded random, its lights and its people. Everything stays at least DoorClear metres from the door so the
    /// way out is never blocked, and inside the walls with a margin. No engine calls.
    /// </summary>
    public static class InteriorRooms
    {
        public const float Wall = 0.2f, DoorWidth = 1.8f, DoorHeight = 2.6f, DoorClear = 2.2f;

        public static InteriorPlan Plan(InteriorKind kind, int seed)
        {
            System.Random r = new System.Random(seed * 31 + (int)kind);
            InteriorPlan p = new InteriorPlan { Size = InteriorRules.RoomSize(kind) };
            Color accent = Accent(kind, r);
            Shell(p, accent);
            p.Entry = new Vector3(0f, 0.05f, 1.2f);
            p.Exit = new Vector3(0f, 0f, 0.6f);
            switch (kind)
            {
                case InteriorKind.NoodleBar: NoodleBar(p, r, accent); break;
                case InteriorKind.TeaHouse: TeaHouse(p, r, accent); break;
                case InteriorKind.Boutique: Boutique(p, r, accent); break;
                case InteriorKind.Pawnshop: Pawnshop(p, r, accent); break;
                case InteriorKind.Gym: Gym(p, r, accent); break;
                case InteriorKind.Bookshop: Bookshop(p, r, accent); break;
                case InteriorKind.Chapel: Chapel(p, r, accent); break;
                default: Arcade(p, r, accent); break;
            }
            CeilingLights(p, accent);
            return p;
        }

        private static Color Accent(InteriorKind kind, System.Random r)
        {
            Color[] palette =
            {
                new Color(0.95f, 0.45f, 0.18f), new Color(0.35f, 0.8f, 0.55f), new Color(0.85f, 0.35f, 0.75f), new Color(0.9f, 0.75f, 0.25f),
                new Color(0.25f, 0.6f, 0.95f), new Color(0.75f, 0.55f, 0.3f), new Color(0.6f, 0.45f, 0.95f), new Color(0.2f, 0.95f, 0.9f)
            };
            Color c = palette[(int)kind];
            float v = 0.9f + (float)r.NextDouble() * 0.2f;
            return new Color(Mathf.Clamp01(c.r * v), Mathf.Clamp01(c.g * v), Mathf.Clamp01(c.b * v));
        }

        private static void Add(InteriorPlan p, string name, Vector3 center, Vector3 size, InteriorSurface surface, bool collider, Color tint = default(Color), float yaw = 0f, bool cylinder = false)
        {
            p.Pieces.Add(new InteriorPiece { Name = name, Center = center, Size = size, Surface = surface, Collider = collider, Tint = tint, Yaw = yaw, Cylinder = cylinder });
        }

        /// <summary>Floor, ceiling, three closed walls and the front wall with the door gap, plus skirting and the door slab outline.</summary>
        private static void Shell(InteriorPlan p, Color accent)
        {
            float w = p.Size.x, h = p.Size.y, d = p.Size.z, t = Wall;
            Add(p, "Floor", new Vector3(0f, -t * 0.5f, d * 0.5f), new Vector3(w + 2f * t, t, d + 2f * t), InteriorSurface.Floor, true);
            Add(p, "Ceiling", new Vector3(0f, h + t * 0.5f, d * 0.5f), new Vector3(w + 2f * t, t, d + 2f * t), InteriorSurface.Ceiling, true);
            Add(p, "Back wall", new Vector3(0f, h * 0.5f, d + t * 0.5f), new Vector3(w + 2f * t, h, t), InteriorSurface.Wall, true);
            Add(p, "Left wall", new Vector3(-w * 0.5f - t * 0.5f, h * 0.5f, d * 0.5f), new Vector3(t, h, d), InteriorSurface.Wall, true);
            Add(p, "Right wall", new Vector3(w * 0.5f + t * 0.5f, h * 0.5f, d * 0.5f), new Vector3(t, h, d), InteriorSurface.Wall, true);
            float side = (w - DoorWidth) * 0.5f;
            Add(p, "Front wall left", new Vector3(-DoorWidth * 0.5f - side * 0.5f, h * 0.5f, -t * 0.5f), new Vector3(side, h, t), InteriorSurface.Wall, true);
            Add(p, "Front wall right", new Vector3(DoorWidth * 0.5f + side * 0.5f, h * 0.5f, -t * 0.5f), new Vector3(side, h, t), InteriorSurface.Wall, true);
            Add(p, "Front wall over door", new Vector3(0f, (h + DoorHeight) * 0.5f, -t * 0.5f), new Vector3(DoorWidth, h - DoorHeight, t), InteriorSurface.Wall, true);
            // The street door seen from inside: a dark slab filling the gap (it closes the room) and a lit frame.
            Add(p, "Door", new Vector3(0f, DoorHeight * 0.5f, -t), new Vector3(DoorWidth, DoorHeight, 0.08f), InteriorSurface.DarkWood, true);
            Add(p, "Door frame top", new Vector3(0f, DoorHeight + 0.06f, -0.02f), new Vector3(DoorWidth + 0.24f, 0.12f, 0.06f), InteriorSurface.Glow, false, accent);
            Add(p, "Door frame left", new Vector3(-DoorWidth * 0.5f - 0.06f, DoorHeight * 0.5f, -0.02f), new Vector3(0.12f, DoorHeight, 0.06f), InteriorSurface.Glow, false, accent);
            Add(p, "Door frame right", new Vector3(DoorWidth * 0.5f + 0.06f, DoorHeight * 0.5f, -0.02f), new Vector3(0.12f, DoorHeight, 0.06f), InteriorSurface.Glow, false, accent);
            Add(p, "Skirting back", new Vector3(0f, 0.06f, d - 0.03f), new Vector3(w, 0.12f, 0.06f), InteriorSurface.Trim, false);
            Add(p, "Skirting left", new Vector3(-w * 0.5f + 0.03f, 0.06f, d * 0.5f), new Vector3(0.06f, 0.12f, d), InteriorSurface.Trim, false);
            Add(p, "Skirting right", new Vector3(w * 0.5f - 0.03f, 0.06f, d * 0.5f), new Vector3(0.06f, 0.12f, d), InteriorSurface.Trim, false);
            Add(p, "Accent band", new Vector3(0f, h - 0.35f, d - 0.02f), new Vector3(w * 0.9f, 0.05f, 0.03f), InteriorSurface.Glow, false, accent);
        }

        private static void CeilingLights(InteriorPlan p, Color accent)
        {
            float w = p.Size.x, h = p.Size.y, d = p.Size.z;
            int rows = Mathf.Max(1, Mathf.RoundToInt(d / 6f)), cols = w > 11f ? 2 : 1;
            Color warm = Color.Lerp(new Color(1f, 0.85f, 0.65f), accent, 0.25f);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    float x = cols == 1 ? 0f : (j == 0 ? -w * 0.25f : w * 0.25f);
                    float z = d * (i + 0.5f) / rows;
                    Add(p, "Ceiling lamp", new Vector3(x, h - 0.05f, z), new Vector3(1.1f, 0.06f, 0.5f), InteriorSurface.Glow, false, warm);
                    p.Lights.Add(new InteriorLight { Position = new Vector3(x, h - 0.4f, z), Color = warm, Range = Mathf.Max(7f, h * 2.2f), Intensity = 1.6f });
                }
        }

        private static float Rand(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

        private static void Counter(InteriorPlan p, float x, float z, float length, bool alongX, InteriorSurface top, Color accent)
        {
            Vector3 size = alongX ? new Vector3(length, 1.05f, 0.7f) : new Vector3(0.7f, 1.05f, length);
            Add(p, "Counter", new Vector3(x, 0.525f, z), size, InteriorSurface.DarkWood, true);
            Add(p, "Counter top", new Vector3(x, 1.07f, z), new Vector3(size.x + 0.08f, 0.05f, size.z + 0.08f), top, false);
            Add(p, "Counter light", new Vector3(x, 0.15f, z + (alongX ? -0.37f : 0f)), alongX ? new Vector3(length * 0.9f, 0.04f, 0.02f) : new Vector3(0.02f, 0.04f, length * 0.9f), InteriorSurface.Glow, false, accent);
        }

        private static void Stool(InteriorPlan p, Vector3 at, Color seat)
        {
            Add(p, "Stool leg", new Vector3(at.x, 0.35f, at.z), new Vector3(0.08f, 0.35f, 0.08f), InteriorSurface.Metal, false, default(Color), 0f, true);
            Add(p, "Stool seat", new Vector3(at.x, 0.72f, at.z), new Vector3(0.38f, 0.04f, 0.38f), InteriorSurface.Leather, true, seat, 0f, true);
        }

        private static void Table(InteriorPlan p, Vector3 at, float size, float height, InteriorSurface top)
        {
            Add(p, "Table top", new Vector3(at.x, height, at.z), new Vector3(size, 0.05f, size), top, true);
            Add(p, "Table leg", new Vector3(at.x, height * 0.5f, at.z), new Vector3(0.1f, height * 0.5f, 0.1f), InteriorSurface.DarkMetal, false, default(Color), 0f, true);
        }

        private static void Shelf(InteriorPlan p, System.Random r, Vector3 at, float width, float height, bool alongX, int items, Color accent)
        {
            Vector3 size = alongX ? new Vector3(width, height, 0.4f) : new Vector3(0.4f, height, width);
            Add(p, "Shelf back", at + Vector3.up * height * 0.5f, size, InteriorSurface.Wood, true);
            int levels = Mathf.Max(2, Mathf.FloorToInt(height / 0.45f));
            for (int l = 1; l < levels; l++)
            {
                float y = l * height / levels;
                for (int i = 0; i < items; i++)
                {
                    float along = -width * 0.45f + width * 0.9f * (i + 0.5f) / items + Rand(r, -0.05f, 0.05f);
                    Vector3 item = new Vector3(Rand(r, 0.12f, 0.3f), Rand(r, 0.15f, 0.35f), Rand(r, 0.15f, 0.25f));
                    Vector3 pos = at + (alongX ? new Vector3(along, y + item.y * 0.5f, -0.25f) : new Vector3(-0.25f, y + item.y * 0.5f, along));
                    Color tint = Color.Lerp(accent, new Color(Rand(r, 0.2f, 0.9f), Rand(r, 0.2f, 0.9f), Rand(r, 0.2f, 0.9f)), 0.6f);
                    Add(p, "Shelf item", pos, item, InteriorSurface.Accent, false, tint);
                }
            }
        }

        private static void NoodleBar(InteriorPlan p, System.Random r, Color accent)
        {
            float w = p.Size.x, d = p.Size.z;
            Counter(p, 0f, d - 2.4f, w * 0.7f, true, InteriorSurface.Stone, accent);
            for (int i = 0; i < 5; i++) Stool(p, new Vector3(-w * 0.3f + w * 0.6f * i / 4f, 0f, d - 3.2f), accent);
            Shelf(p, r, new Vector3(0f, 0f, d - 0.25f), w * 0.8f, 2.2f, true, 7, accent);
            Add(p, "Steam pot", new Vector3(w * 0.25f, 1.35f, d - 2.3f), new Vector3(0.5f, 0.25f, 0.5f), InteriorSurface.Metal, false, default(Color), 0f, true);
            for (int i = 0; i < 2; i++)
            {
                Vector3 t = new Vector3(i == 0 ? -w * 0.28f : w * 0.28f, 0f, d * 0.42f);
                Table(p, t, 0.9f, 0.75f, InteriorSurface.Wood);
                Stool(p, t + new Vector3(-0.7f, 0f, 0f), accent);
                Stool(p, t + new Vector3(0.7f, 0f, 0f), accent);
                p.Patrons.Add(new InteriorSpot { Position = t + new Vector3(0.7f, 0f, -0.6f), Yaw = 300f });
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 at = new Vector3(-w * 0.3f + w * 0.6f * i / 3f, p.Size.y - 0.9f, d * 0.5f);
                Add(p, "Lantern", at, new Vector3(0.35f, 0.45f, 0.35f), InteriorSurface.Glow, false, accent, 0f, true);
            }
            p.Counter = new Vector3(0f, 0f, d - 3.3f);
            p.Keeper = new InteriorSpot { Position = new Vector3(0f, 0f, d - 1.5f), Yaw = 180f };
            p.Patrons.Add(new InteriorSpot { Position = new Vector3(-w * 0.15f, 0f, d - 3.6f), Yaw = 0f });
        }

        private static void TeaHouse(InteriorPlan p, System.Random r, Color accent)
        {
            float w = p.Size.x, d = p.Size.z;
            Counter(p, w * 0.5f - 0.9f, d * 0.6f, d * 0.5f, false, InteriorSurface.Wood, accent);
            Shelf(p, r, new Vector3(w * 0.5f - 0.25f, 0f, d * 0.6f), d * 0.45f, 2.4f, false, 6, accent);
            for (int i = 0; i < 4; i++)
            {
                Vector3 t = new Vector3(-w * 0.22f + (i % 2) * w * 0.32f - w * 0.08f, 0f, d * 0.32f + (i / 2) * d * 0.33f);
                Table(p, t, 1.1f, 0.4f, InteriorSurface.DarkWood);
                for (int c = 0; c < 4; c++)
                {
                    Vector3 off = c == 0 ? new Vector3(0.85f, 0f, 0f) : c == 1 ? new Vector3(-0.85f, 0f, 0f) : c == 2 ? new Vector3(0f, 0f, 0.85f) : new Vector3(0f, 0f, -0.85f);
                    Add(p, "Cushion", t + off + Vector3.up * 0.08f, new Vector3(0.6f, 0.08f, 0.6f), InteriorSurface.Fabric, false, Color.Lerp(accent, Color.white, 0.4f), 0f, true);
                }
                if (i % 2 == 0) p.Patrons.Add(new InteriorSpot { Position = t + new Vector3(-0.9f, 0f, -0.9f), Yaw = 45f });
            }
            for (int i = 0; i < 3; i++)
                Add(p, "Paper screen", new Vector3(-w * 0.5f + 0.12f, 1.4f, d * (i + 1) / 4f), new Vector3(0.06f, 2.2f, 1.6f), InteriorSurface.Paper, false);
            Add(p, "Plant", new Vector3(-w * 0.5f + 0.6f, 0.6f, 2.8f), new Vector3(0.6f, 0.6f, 0.6f), InteriorSurface.Accent, true, new Color(0.25f, 0.5f, 0.25f), 0f, true);
            p.Counter = new Vector3(w * 0.5f - 1.8f, 0f, d * 0.6f);
            p.Keeper = new InteriorSpot { Position = new Vector3(w * 0.5f - 0.5f, 0f, d * 0.6f + 0.3f), Yaw = 270f };
        }

        private static void Boutique(InteriorPlan p, System.Random r, Color accent)
        {
            float w = p.Size.x, d = p.Size.z;
            for (int i = 0; i < 3; i++)
            {
                float z = d * 0.35f + i * d * 0.2f;
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = side * w * 0.25f;
                    Add(p, "Rack bar", new Vector3(x, 1.5f, z), new Vector3(2.2f, 0.04f, 0.04f), InteriorSurface.Metal, false);
                    Add(p, "Rack leg", new Vector3(x - 1.05f, 0.75f, z), new Vector3(0.04f, 1.5f, 0.04f), InteriorSurface.Metal, true);
                    Add(p, "Rack leg", new Vector3(x + 1.05f, 0.75f, z), new Vector3(0.04f, 1.5f, 0.04f), InteriorSurface.Metal, true);
                    for (int g = 0; g < 7; g++)
                    {
                        Color cloth = Color.HSVToRGB((float)r.NextDouble(), Rand(r, 0.3f, 0.7f), Rand(r, 0.35f, 0.85f));
                        Add(p, "Garment", new Vector3(x - 0.9f + g * 0.3f, 1.05f, z), new Vector3(0.06f, 0.85f, 0.5f), InteriorSurface.Fabric, false, cloth);
                    }
                }
            }
            for (int i = 0; i < 3; i++)
            {
                float x = -w * 0.3f + i * w * 0.3f;
                Add(p, "Mannequin", new Vector3(x, 1f, d - 1.2f), new Vector3(0.35f, 0.75f, 0.35f), InteriorSurface.Fabric, true, Color.HSVToRGB((float)r.NextDouble(), 0.5f, 0.6f), 0f, true);
                Add(p, "Mannequin head", new Vector3(x, 1.95f, d - 1.2f), new Vector3(0.22f, 0.22f, 0.22f), InteriorSurface.Trim, false, default(Color), 0f, true);
                Add(p, "Plinth", new Vector3(x, 0.12f, d - 1.2f), new Vector3(0.9f, 0.24f, 0.9f), InteriorSurface.Stone, true);
            }
            Add(p, "Mirror", new Vector3(-w * 0.5f + 0.05f, 1.4f, d * 0.5f), new Vector3(0.04f, 2f, 1.2f), InteriorSurface.Glass, false);
            Counter(p, w * 0.32f, 2.9f, 2f, true, InteriorSurface.Stone, accent);
            p.Counter = new Vector3(w * 0.32f, 0f, 2.2f);
            p.Keeper = new InteriorSpot { Position = new Vector3(w * 0.32f, 0f, 3.6f), Yaw = 180f };
            p.Patrons.Add(new InteriorSpot { Position = new Vector3(-w * 0.25f, 0f, d * 0.45f), Yaw = 0f });
            p.Patrons.Add(new InteriorSpot { Position = new Vector3(w * 0.1f, 0f, d * 0.7f), Yaw = 200f });
        }

        private static void Pawnshop(InteriorPlan p, System.Random r, Color accent)
        {
            float w = p.Size.x, d = p.Size.z;
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -w * 0.3f : w * 0.3f;
                Add(p, "Display case", new Vector3(x, 0.45f, d * 0.45f), new Vector3(1f, 0.9f, 3f), InteriorSurface.DarkWood, true);
                Add(p, "Case glass", new Vector3(x, 1.05f, d * 0.45f), new Vector3(1f, 0.3f, 3f), InteriorSurface.Glass, false);
                for (int k = 0; k < 5; k++)
                    Add(p, "Trinket", new Vector3(x + Rand(r, -0.35f, 0.35f), 0.95f, d * 0.45f - 1.2f + k * 0.6f), new Vector3(0.15f, 0.08f, 0.2f), InteriorSurface.Accent, false, Color.Lerp(accent, new Color(0.8f, 0.7f, 0.4f), 0.5f));
            }
            Counter(p, 0f, d - 2f, w * 0.75f, true, InteriorSurface.Glass, accent);
            Add(p, "Cage", new Vector3(0f, 1.95f, d - 2f), new Vector3(w * 0.75f, 1.6f, 0.03f), InteriorSurface.DarkMetal, false);
            Shelf(p, r, new Vector3(-w * 0.5f + 0.25f, 0f, d * 0.5f), d * 0.6f, 2.6f, false, 5, accent);
            Shelf(p, r, new Vector3(0f, 0f, d - 0.25f), w * 0.8f, 2.6f, true, 6, accent);
            p.Counter = new Vector3(0f, 0f, d - 2.9f);
            p.Keeper = new InteriorSpot { Position = new Vector3(0.6f, 0f, d - 1.2f), Yaw = 180f };
            p.Patrons.Add(new InteriorSpot { Position = new Vector3(w * 0.3f, 0f, d * 0.25f), Yaw = 20f });
        }

        private static void Gym(InteriorPlan p, System.Random r, Color accent)
        {
            float w = p.Size.x, d = p.Size.z;
            Vector3 ring = new Vector3(w * 0.15f, 0f, d * 0.55f);
            Add(p, "Ring floor", ring + Vector3.up * 0.3f, new Vector3(5f, 0.6f, 5f), InteriorSurface.Fabric, true, Color.Lerp(accent, Color.gray, 0.5f));
            for (int k = 0; k < 4; k++)
            {
                Vector3 corner = ring + new Vector3(k % 2 == 0 ? -2.4f : 2.4f, 1.1f, k < 2 ? -2.4f : 2.4f);
                Add(p, "Ring post", corner, new Vector3(0.12f, 1.0f, 0.12f), InteriorSurface.Metal, true, default(Color), 0f, true);
            }
            for (int l = 0; l < 3; l++)
            {
                float y = 1.0f + l * 0.35f;
                Add(p, "Rope", ring + new Vector3(0f, y, -2.4f), new Vector3(4.8f, 0.03f, 0.03f), InteriorSurface.Accent, false, accent);
                Add(p, "Rope", ring + new Vector3(0f, y, 2.4f), new Vector3(4.8f, 0.03f, 0.03f), InteriorSurface.Accent, false, accent);
                Add(p, "Rope", ring + new Vector3(-2.4f, y, 0f), new Vector3(0.03f, 0.03f, 4.8f), InteriorSurface.Accent, false, accent);
                Add(p, "Rope", ring + new Vector3(2.4f, y, 0f), new Vector3(0.03f, 0.03f, 4.8f), InteriorSurface.Accent, false, accent);
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 bag = new Vector3(-w * 0.35f, 0f, d * 0.3f + i * 2.4f);
                Add(p, "Bag chain", bag + Vector3.up * (p.Size.y - 0.6f), new Vector3(0.03f, 0.6f, 0.03f), InteriorSurface.Metal, false, default(Color), 0f, true);
                Add(p, "Punching bag", bag + Vector3.up * 1.45f, new Vector3(0.45f, 0.6f, 0.45f), InteriorSurface.Leather, true, new Color(0.35f, 0.08f, 0.06f), 0f, true);
            }
            for (int i = 0; i < 2; i++)
            {
                Vector3 bench = new Vector3(w * 0.32f, 0f, 2.8f + i * 1.8f);
                Add(p, "Bench", bench + Vector3.up * 0.45f, new Vector3(1.6f, 0.12f, 0.4f), InteriorSurface.Leather, true, new Color(0.1f, 0.1f, 0.12f));
                Add(p, "Barbell", bench + Vector3.up * 1.05f, new Vector3(0.04f, 0.9f, 0.04f), InteriorSurface.Metal, false, default(Color), 90f, true);
            }
            Add(p, "Mirror wall", new Vector3(w * 0.5f - 0.04f, 1.5f, d * 0.4f), new Vector3(0.04f, 2.2f, d * 0.5f), InteriorSurface.Glass, false);
            Counter(p, -w * 0.3f, 2.6f, 1.8f, true, InteriorSurface.Wood, accent);
            p.Counter = new Vector3(-w * 0.3f, 0f, 1.9f);
            p.Keeper = new InteriorSpot { Position = new Vector3(-w * 0.3f, 0f, 3.3f), Yaw = 180f };
            p.Patrons.Add(new InteriorSpot { Position = new Vector3(-w * 0.35f + 0.8f, 0f, d * 0.3f), Yaw = 270f });
            p.Patrons.Add(new InteriorSpot { Position = ring + new Vector3(-0.8f, 0.6f, 0f), Yaw = 90f });
            p.Patrons.Add(new InteriorSpot { Position = ring + new Vector3(0.8f, 0.6f, 0f), Yaw = 270f });
        }

        private static void Bookshop(InteriorPlan p, System.Random r, Color accent)
        {
            float w = p.Size.x, d = p.Size.z;
            for (int row = 0; row < 3; row++)
            {
                float z = d * 0.4f + row * 2.4f;
                Shelf(p, r, new Vector3(-w * 0.22f, 0f, z), w * 0.32f, 2.3f, true, 9, accent);
                Shelf(p, r, new Vector3(w * 0.22f, 0f, z), w * 0.32f, 2.3f, true, 9, accent);
            }
            Shelf(p, r, new Vector3(-w * 0.5f + 0.25f, 0f, d * 0.55f), d * 0.7f, 3.2f, false, 10, accent);
            Table(p, new Vector3(w * 0.3f, 0f, 3.2f), 1.4f, 0.78f, InteriorSurface.DarkWood);
            Add(p, "Reading lamp", new Vector3(w * 0.3f, 1.05f, 3.2f), new Vector3(0.25f, 0.25f, 0.25f), InteriorSurface.Glow, false, new Color(1f, 0.8f, 0.5f), 0f, true);
            Counter(p, -w * 0.25f, 2.6f, 1.8f, true, InteriorSurface.Wood, accent);
            p.Lights.Add(new InteriorLight { Position = new Vector3(w * 0.3f, 1.4f, 3.2f), Color = new Color(1f, 0.8f, 0.5f), Range = 4f, Intensity = 1.2f });
            p.Counter = new Vector3(-w * 0.25f, 0f, 1.9f);
            p.Keeper = new InteriorSpot { Position = new Vector3(-w * 0.25f, 0f, 3.3f), Yaw = 180f };
            p.Patrons.Add(new InteriorSpot { Position = new Vector3(0f, 0f, d * 0.5f), Yaw = 90f });
            p.Patrons.Add(new InteriorSpot { Position = new Vector3(w * 0.3f, 0f, 2.4f), Yaw = 0f });
        }

        private static void Chapel(InteriorPlan p, System.Random r, Color accent)
        {
            float w = p.Size.x, d = p.Size.z, h = p.Size.y;
            for (int row = 0; row < 5; row++)
            {
                float z = 4f + row * 1.8f;
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 at = new Vector3(side * w * 0.27f, 0f, z);
                    Add(p, "Pew seat", at + Vector3.up * 0.45f, new Vector3(w * 0.36f, 0.08f, 0.45f), InteriorSurface.DarkWood, true);
                    Add(p, "Pew back", at + new Vector3(0f, 0.8f, 0.22f), new Vector3(w * 0.36f, 0.7f, 0.06f), InteriorSurface.DarkWood, true);
                }
            }
            Add(p, "Altar step", new Vector3(0f, 0.15f, d - 2f), new Vector3(w * 0.6f, 0.3f, 2.4f), InteriorSurface.Stone, true);
            Add(p, "Altar", new Vector3(0f, 0.8f, d - 2f), new Vector3(2.4f, 1f, 0.9f), InteriorSurface.Stone, true);
            Add(p, "Window", new Vector3(0f, h * 0.62f, d - 0.03f), new Vector3(2.4f, h * 0.5f, 0.04f), InteriorSurface.Glow, false, Color.Lerp(accent, new Color(0.4f, 0.6f, 1f), 0.4f));
            for (int i = 0; i < 9; i++)
            {
                Vector3 c = new Vector3(-1f + i * 0.25f, 1.38f, d - 2f + Rand(r, -0.25f, 0.25f));
                Add(p, "Candle", c, new Vector3(0.05f, Rand(r, 0.08f, 0.16f), 0.05f), InteriorSurface.Glow, false, new Color(1f, 0.75f, 0.4f), 0f, true);
            }
            Add(p, "Runner", new Vector3(0f, 0.01f, d * 0.5f), new Vector3(1.4f, 0.02f, d - 3f), InteriorSurface.Fabric, false, Color.Lerp(accent, new Color(0.5f, 0.1f, 0.1f), 0.5f));
            p.Lights.Add(new InteriorLight { Position = new Vector3(0f, 2f, d - 2.6f), Color = new Color(1f, 0.75f, 0.45f), Range = 7f, Intensity = 1.8f });
            p.Counter = new Vector3(0f, 0f, d - 3.6f);
            p.Keeper = new InteriorSpot { Position = new Vector3(0f, 0.3f, d - 1.2f), Yaw = 180f };
            p.Patrons.Add(new InteriorSpot { Position = new Vector3(-w * 0.27f, 0f, 5.2f), Yaw = 0f });
            p.Patrons.Add(new InteriorSpot { Position = new Vector3(w * 0.27f, 0f, 8.8f), Yaw = 0f });
        }

        private static void Arcade(InteriorPlan p, System.Random r, Color accent)
        {
            float w = p.Size.x, d = p.Size.z;
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 5; i++)
                {
                    float x = -w * 0.5f + 0.6f + row * (w - 1.2f);
                    float z = 3.2f + i * (d - 4.5f) / 4f;
                    float yaw = row == 0 ? 90f : 270f;
                    Add(p, "Cabinet", new Vector3(x, 0.95f, z), new Vector3(0.8f, 1.9f, 0.75f), InteriorSurface.DarkMetal, true);
                    Color screen = Color.HSVToRGB((float)r.NextDouble(), 0.7f, 1f);
                    Add(p, "Cabinet screen", new Vector3(x + (row == 0 ? 0.41f : -0.41f), 1.35f, z), new Vector3(0.02f, 0.5f, 0.6f), InteriorSurface.Glow, false, screen);
                    Add(p, "Cabinet marquee", new Vector3(x + (row == 0 ? 0.41f : -0.41f), 1.8f, z), new Vector3(0.02f, 0.18f, 0.7f), InteriorSurface.Glow, false, accent);
                    if (i == 1 || i == 3) p.Patrons.Add(new InteriorSpot { Position = new Vector3(x + (row == 0 ? 0.9f : -0.9f), 0f, z), Yaw = yaw + 180f });
                }
            for (int i = 0; i < 2; i++)
                Add(p, "Floor strip", new Vector3(i == 0 ? -1.2f : 1.2f, 0.01f, d * 0.55f), new Vector3(0.08f, 0.02f, d - 4f), InteriorSurface.Glow, false, accent);
            Counter(p, 0f, d - 1.8f, w * 0.4f, true, InteriorSurface.Glass, accent);
            Shelf(p, r, new Vector3(0f, 0f, d - 0.25f), w * 0.5f, 2.2f, true, 8, accent);
            p.Counter = new Vector3(0f, 0f, d - 2.6f);
            p.Keeper = new InteriorSpot { Position = new Vector3(0f, 0f, d - 1.0f), Yaw = 180f };
        }
    }
}
