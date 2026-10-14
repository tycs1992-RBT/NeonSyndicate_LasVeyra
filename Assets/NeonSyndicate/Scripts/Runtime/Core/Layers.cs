using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Physics/render layers by index. Unity allows using unnamed layer indices, so the project needs no TagManager edits.
    /// </summary>
    public static class Layers
    {
        public const int World = 0;
        public const int IgnoreRaycast = 2;
        public const int Vehicles = 8;
        public const int Characters = 9;
        public const int Triggers = 11;
        public const int Debris = 12;
        /// <summary>Kinematic copies of vehicle bodies that block the player's CharacterController without stopping cars.</summary>
        public const int VehicleShell = 13;
        /// <summary>Active ragdoll bones (collide with world, vehicles, debris and other ragdolls, never with walking characters).</summary>
        public const int Ragdoll = 14;
        public const int Minimap = 30;

        /// <summary>What bullets and melee can hit.</summary>
        public static int ShootMask
        {
            get { return ~((1 << Triggers) | (1 << Minimap) | (1 << IgnoreRaycast) | (1 << VehicleShell)); }
        }

        /// <summary>What the camera collides with (world and vehicles, never characters).</summary>
        public static int CameraMask
        {
            get { return (1 << World) | (1 << Vehicles); }
        }

        /// <summary>What vehicle wheels stand on (static world only; props and debris are pushed, not driven over).</summary>
        public static int WheelMask
        {
            get { return 1 << World; }
        }

        /// <summary>What characters' ground probes stand on.</summary>
        public static int GroundMask
        {
            get { return (1 << World) | (1 << Vehicles) | (1 << Debris); }
        }

        /// <summary>Static world geometry only (line of sight, ground probes).</summary>
        public static int WorldMask
        {
            get { return 1 << World; }
        }

        public static int SightMask
        {
            get { return (1 << World) | (1 << Vehicles); }
        }

        /// <summary>Configures the physics collision matrix for the layers above. Called once at boot.</summary>
        public static void ConfigureCollisions()
        {
            Physics.IgnoreLayerCollision(Vehicles, Debris, true);
            Physics.IgnoreLayerCollision(Vehicles, Characters, true);
            Physics.IgnoreLayerCollision(VehicleShell, Vehicles, true);
            Physics.IgnoreLayerCollision(VehicleShell, VehicleShell, true);
            Physics.IgnoreLayerCollision(VehicleShell, Debris, true);
            Physics.IgnoreLayerCollision(VehicleShell, Ragdoll, true);
            Physics.IgnoreLayerCollision(VehicleShell, World, true);
            Physics.IgnoreLayerCollision(Ragdoll, Characters, true);
            Physics.IgnoreLayerCollision(Triggers, Triggers, true);
            Physics.IgnoreLayerCollision(Triggers, World, true);
            Physics.IgnoreLayerCollision(Triggers, Debris, true);
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(Minimap, i, true);
        }

        public static void SetRecursive(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++) SetRecursive(t.GetChild(i).gameObject, layer);
        }
    }
}
