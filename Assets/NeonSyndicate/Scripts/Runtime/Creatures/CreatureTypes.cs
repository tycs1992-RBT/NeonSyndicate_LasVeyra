using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    // ============================================================================================================
    // The shared creature contract. CreatureBuilder (meshes, skeletons, hit volumes) and CreaturePose (procedural
    // animation) are pure C# so they run on worker threads and in the offline preview; the runtime side (CreatureRig,
    // Creature, the AI and the ecology spawner) only talks to them through the types in this file.
    //
    // Conventions: model space is metres, +Z forward, +Y up, +X right, origin on the ground under the body centre.
    // Every bone binds with an identity local rotation; LocalPositions are offsets from the parent bone. The pose
    // solver writes local rotations (relative to bind) and local positions per bone each frame.
    //
    // Humanoid creatures (Hollows, wraiths, golems, the Djinn) are not in this file: they reuse the humanoid rig and
    // the NPC brain, with their looks chosen by Appearance.Creature.
    // ============================================================================================================

    // API implemented by the builder and pose files (pure C#, no engine calls that fail off the main thread):
    //   public static class CreatureBuilder { public static CreatureBody Build(CreatureLook look, int detail); }
    //       detail: 0 crowd, 1 standard, 2 hero (same meaning as for people).
    //   public static class CreatureLooks { public static CreatureLook For(string speciesId, int seed); }
    //       speciesId: "neon_jackal", "frost_stalker", "crystal_basilisk", "violet_wyvern", "rime_griffin",
    //       "sun_griffin", "quartz_drake", "dune_wyrm", "hollow_shade", "glass_mite".
    //   public sealed class CreaturePose
    //   {
    //       public CreaturePose(CreatureBody body);
    //       public readonly Quaternion[] LocalRotations;   // per bone, relative to bind (identity at rest)
    //       public readonly Vector3[] LocalPositions;      // per bone, bind offsets plus any procedural offset
    //       public int FootPlantMask;                      // bit i set on the frame leg i plants (footsteps)
    //       public void Reset(Vector3 rootPosition, float yaw);
    //       public void Solve(ref CreaturePoseInput input, float dt);
    //   }

    /// <summary>
    /// Humanoid creatures: built on the people rig (so they walk, fight, stagger and fall like people) with their own
    /// bodies. Chosen with Appearance.Creature; Appearance.CreatureScale sizes the golems and the bosses.
    /// </summary>
    public enum HumanoidCreature
    {
        None = 0,
        /// <summary>Shadow stalker from the Inner World: smoky black body, violet seams, burning eyes.</summary>
        Hollow = 1,
        /// <summary>A Hollow that froze in the Frostline: frost-white with icy cracks and blue light.</summary>
        SnowHollow = 2,
        /// <summary>A glassy negative copy of someone (it borrows the source look's shape).</summary>
        MirrorWraith = 3,
        /// <summary>Badlands apex: a towering spirit whose legs dissolve into a sand-and-smoke tail.</summary>
        MirageDjinn = 4,
        /// <summary>Construction-site golem of dark stone and cyan crystal.</summary>
        ShardGolem = 5,
        /// <summary>Badlands golem of sandstone blocks and amber light.</summary>
        SandGolem = 6,
        /// <summary>Frostline boss: a giant of blue ice slabs.</summary>
        GlacierTitan = 7,
        /// <summary>Boss: a bigger Shard Golem with a crystal crown and hazard-striped plates.</summary>
        GolemForeman = 8,
        /// <summary>Boss: a tall crowned Mirror Wraith.</summary>
        WraithQueen = 9
    }

    /// <summary>The non-humanoid body plans the builder and the pose solver know.</summary>
    public enum CreaturePlan
    {
        /// <summary>Four legs, a neck, a head and a tail (Neon Jackal, Frost Stalker, Crystal Basilisk).</summary>
        Quadruped,
        /// <summary>A flyer: wings plus hind legs, with forelegs when Forelegs is set (wyvern, griffins, drake).</summary>
        Winged,
        /// <summary>A long segmented body with no legs that can burrow (Dune Wyrm).</summary>
        Serpent,
        /// <summary>A floating core with trailing tendrils (Hollow Shade).</summary>
        Wisp,
        /// <summary>A tiny rigid crystal insect drawn in swarms (Glass Mite).</summary>
        Mite
    }

    /// <summary>Actions the pose solver animates. The AI picks one and drives ActionT from 0 to 1.</summary>
    public enum CreatureAction
    {
        None,
        Bite,
        Claw,
        Pounce,
        TailSwipe,
        Slam,
        Breath,
        Roar,
        Dive,
        Burrow,
        Erupt,
        Stagger,
        Death,
        Spit,
        Charge,
        Howl
    }

    /// <summary>Visual parameters for one creature. Plain data; CreatureLooks holds the species presets.</summary>
    public sealed class CreatureLook
    {
        public CreaturePlan Plan;
        public int Seed;
        /// <summary>Overall size multiplier applied to every length below.</summary>
        public float Scale = 1f;
        /// <summary>Nose to rump at Scale 1, metres (serpent: total length).</summary>
        public float BodyLength = 1.2f;
        /// <summary>Ground to the top of the shoulders at rest, metres.</summary>
        public float ShoulderHeight = 0.7f;
        /// <summary>Girth multiplier (1 = lean predator).</summary>
        public float Bulk = 1f;
        public float NeckLength = 0.35f;
        public float HeadSize = 1f;
        public float TailLength = 0.8f;
        public int TailSegments = 4;
        /// <summary>Leg length multiplier.</summary>
        public float LegLength = 1f;
        /// <summary>Winged plan: has forelegs as well as wings (griffin, drake) or wings for arms (wyvern).</summary>
        public bool Forelegs = true;
        /// <summary>Tip to tip at Scale 1, metres; 0 means no wings.</summary>
        public float WingSpan;
        /// <summary>0 none, 1 swept back, 2 crown of crystal points, 3 ram curls.</summary>
        public int Horns;
        /// <summary>Number of crystal spines down the back.</summary>
        public int Spines;
        public bool Mane;
        public bool Beak;
        public bool Feathers;
        /// <summary>Serpent body segments (spine bones).</summary>
        public int Segments = 14;
        /// <summary>Wisp tendrils.</summary>
        public int Tendrils = 5;
        // Palette cells in CharacterPaletteData (x = column, y = row).
        public Vector2Int Hide;
        public Vector2Int Belly;
        public Vector2Int Crystal;
        public Vector2Int Glow;
        public Vector2Int Claw;
        public float GlowStrength = 1f;
        /// <summary>Head shape: 0 long canine muzzle, 1 short feline face, 2 flat lizard head, 3 horned dragon head,
        /// 4 hooked beak (griffin), 5 ring maw of crystal teeth (wyrm), 6 none (wisp, mite).</summary>
        public int HeadStyle;
        /// <summary>0 none, 1 tall pointed ears, 2 small rounded ears, 3 feathered tufts.</summary>
        public int EarStyle;
        /// <summary>0 plain taper, 1 bushy brush, 2 tufted tip (lion), 3 crystal blade at the tip.</summary>
        public int TailStyle;
        /// <summary>Surface: 0 fur, 1 scales, 2 feathers (the griffin's front), 3 armour rings, 4 smoke, 5 crystal.
        /// Drives the detail-map tiling and the small surface pieces (ruffs, scutes, ring plates).</summary>
        public int Coat;
        /// <summary>Crystal armour: 0 none, 1 shoulder plates, 2 armoured back, 3 quartz plates along the back and flanks.</summary>
        public int Plates;
        /// <summary>Quartz Drake visual refinement. Adds mesh decoration and quiet secondary motion without changing its rig or combat.</summary>
        public bool CrystalRegalia;
        public bool StormProwler04OF;
        /// <summary>A crest of feathers (or crystal fins) on the head.</summary>
        public bool Crest;
        /// <summary>0 legs straight under the body (mammals), 1 lizard sprawl (elbows and knees out to the sides).</summary>
        public float Sprawl;
        /// <summary>Secondary coat cell: markings, the griffin's lion body, the flesh between the wyrm's armour rings.</summary>
        public Vector2Int Accent;
        /// <summary>Wing membranes or flight feathers.</summary>
        public Vector2Int Membrane;
        /// <summary>Inside of the mouth (a glow cell makes it burn, like the wyrm's ember throat).</summary>
        public Vector2Int Mouth;

        public CreatureLook Clone()
        {
            return (CreatureLook)MemberwiseClone();
        }
    }

    /// <summary>One leg as bone indices, upper to toe.</summary>
    public struct LegChain
    {
        public int Upper, Lower, Foot, Toe;
        /// <summary>True for forelegs.</summary>
        public bool Front;
        /// <summary>-1 left, +1 right.</summary>
        public int Side;
        /// <summary>Hip or shoulder to sole at rest, metres (after Scale).</summary>
        public float Length;
    }

    /// <summary>One wing as bone indices, shoulder to tip.</summary>
    public struct WingChain
    {
        public int Arm, Fore, Hand;
        public int Side;
    }

    /// <summary>A collision and damage volume carried by a bone: a capsule along Axis (a sphere when Length is 0).</summary>
    public struct CreatureHitVolume
    {
        public int Bone;
        /// <summary>Centre in the bone's local space at bind.</summary>
        public Vector3 Center;
        /// <summary>Capsule axis in the bone's local space (unit).</summary>
        public Vector3 Axis;
        public float Radius;
        /// <summary>Total capsule length including the end caps (0 = sphere).</summary>
        public float Length;
        /// <summary>A glowing crystal core: hits here count as critical (like a headshot).</summary>
        public bool WeakPoint;
    }

    /// <summary>The bone hierarchy of a creature, built by CreatureBuilder.</summary>
    public sealed class CreatureSkeleton
    {
        public readonly List<string> Names = new List<string>();
        public readonly List<int> Parents = new List<int>();
        /// <summary>Bind offsets from the parent bone (identity rotations at bind).</summary>
        public readonly List<Vector3> LocalPositions = new List<Vector3>();

        /// <summary>The bone everything hangs from (directly under the creature's transform).</summary>
        public int Root = -1;
        public int Pelvis = -1, Chest = -1, Head = -1, Jaw = -1;
        public int[] Spine = new int[0];
        public int[] Neck = new int[0];
        public int[] Tail = new int[0];
        public int[] Tendrils = new int[0];
        public LegChain[] Legs = new LegChain[0];
        public WingChain[] Wings = new WingChain[0];

        public int Count { get { return Names.Count; } }

        public int Add(string name, int parent, Vector3 localPosition)
        {
            Names.Add(name);
            Parents.Add(parent);
            LocalPositions.Add(localPosition);
            return Names.Count - 1;
        }

        /// <summary>Bind position of a bone in model space.</summary>
        public Vector3 ModelPosition(int bone)
        {
            Vector3 p = Vector3.zero;
            for (int b = bone; b >= 0; b = Parents[b]) p += LocalPositions[b];
            return p;
        }
    }

    /// <summary>Everything CreatureBuilder produces for one creature.</summary>
    public sealed class CreatureBody
    {
        public CreatureLook Look;
        public CreatureSkeleton Skeleton;
        /// <summary>Skinned mesh in model space at bind, palette UVs in UV0 and fabric detail in UV1 (same as people).</summary>
        public BodyData Mesh;
        public readonly List<CreatureHitVolume> HitVolumes = new List<CreatureHitVolume>();
        /// <summary>Model-space bounds of the bind pose.</summary>
        public Bounds Bounds;
        /// <summary>Horizontal radius of the body from its centre, metres (used for melee reach and spacing).</summary>
        public float Radius;
        /// <summary>Height of the top of the head or back above the ground at rest.</summary>
        public float Height;
        /// <summary>Model-space point the AI and the lock-on aim at (the chest).</summary>
        public Vector3 AimPoint;
        /// <summary>Model-space point the Third Eye scan and the head-height crits use.</summary>
        public Vector3 HeadPoint;
        /// <summary>Model-space mouth position at bind (breath and spit attacks start here).</summary>
        public Vector3 MouthPoint;
        /// <summary>Model-space box that holds the body in every pose CreaturePose makes (wings raised, serpent coils,
        /// pounces, death sprawl); use it as the skinned renderer's local bounds so posed creatures are never culled.</summary>
        public Bounds PoseBounds;
    }

    /// <summary>Per-frame input to CreaturePose.</summary>
    public struct CreaturePoseInput
    {
        /// <summary>World position of the creature's transform (ground under the body centre) and its heading.</summary>
        public Vector3 RootPosition;
        public float Yaw;
        /// <summary>Heading change rate in degrees per second (lean and bank into turns).</summary>
        public float YawRate;
        /// <summary>World velocity.</summary>
        public Vector3 Velocity;
        public bool Grounded;
        /// <summary>In flight (wings working); Altitude is height above the ground under the body.</summary>
        public bool Flying;
        public float Altitude;
        /// <summary>Serpent: how far below the surface the head is (0 = surfaced). The body follows the head's trail.</summary>
        public float Depth;
        public CreatureAction Action;
        /// <summary>0..1 through the action, or negative when idle.</summary>
        public float ActionT;
        /// <summary>Fraction of ActionT where the wind-up ends and the strike begins.</summary>
        public float Windup;
        /// <summary>Fraction of ActionT where the strike ends and the recovery begins.</summary>
        public float Strike;
        /// <summary>0 relaxed, 1 hackles up and ready to fight.</summary>
        public float Alert;
        public float Forage04M, Rest04M;
        public int RegionalArt04OG;
        /// <summary>Flinch from a hit, 1 on impact and decaying.</summary>
        public float Hurt;
        /// <summary>World direction the last hit came from (for the flinch).</summary>
        public Vector3 HurtDirection;
        public bool HasLookTarget;
        public Vector3 LookTarget;
        /// <summary>World ground height at a position; null means flat ground at RootPosition.y.</summary>
        public Func<float, float, float> GroundHeight;
        /// <summary>Running time in seconds (breathing, idle variation).</summary>
        public float Time;

        public static CreaturePoseInput Idle(Vector3 root, float yaw)
        {
            CreaturePoseInput p = new CreaturePoseInput();
            p.RootPosition = root;
            p.Yaw = yaw;
            p.Grounded = true;
            p.ActionT = -1f;
            p.Windup = 0.4f;
            p.Strike = 0.6f;
            p.HurtDirection = Vector3.forward;
            return p;
        }
    }
}
