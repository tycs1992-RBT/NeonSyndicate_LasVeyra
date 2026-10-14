using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum VehicleClass
    {
        Compact,
        Sedan,
        Muscle,
        Sports,
        Super,
        SUV,
        Pickup,
        Van,
        BoxTruck,
        Bus,
        Taxi,
        Police,
        PoliceSUV,
        Enforcer,
        Ambulance,
        Armored,
        /// <summary>Hover bikes (appended: values are stable). The LVPD bike is a Bike with VehicleDef.PoliceUnit set.</summary>
        Bike
    }

    public enum BodyStyle
    {
        Hatch,
        Sedan,
        Coupe,
        Wedge,
        SUV,
        Pickup,
        Van,
        BoxTruck,
        Bus,
        /// <summary>A hover bike: one long hull on two glowing thruster pods, ridden astride (appended).</summary>
        Bike
    }

    public enum DriveType
    {
        FWD,
        RWD,
        AWD
    }

    /// <summary>Static description of a vehicle model. Names use the Neon Syndicate manufacturer roster from the original vehicle database.</summary>
    public sealed class VehicleDef
    {
        public int Index;
        public string Name;
        public string Make;
        public VehicleClass Class;
        public BodyStyle Body;
        public DriveType Drive;
        public float Length;
        public float Width;
        public float Height;
        public float Mass;
        public float DriveForce;
        public float TopSpeed;
        public float Grip;
        public float SteerAngle = 34f;
        public float WheelRadius;
        public float Clearance;
        public int Seats;
        public SfxId Engine;
        public float EnginePitch = 1f;
        public float Health = 1000f;
        public int Price;
        public float SpawnWeight;
        public bool Emergency;
        public bool Siren;
        public Color[] Paints;
        public string Blurb;
        /// <summary>Police vehicles outside the police classes (the LVPD hover bike): counts as police everywhere.</summary>
        public bool PoliceUnit;

        public float Wheelbase { get { return Length * (Body == BodyStyle.Bus ? 0.62f : (Body == BodyStyle.BoxTruck ? 0.58f : (Body == BodyStyle.Bike ? 0.64f : 0.6f))); } }
        public float Track { get { return Width - 0.32f; } }
        public string FullName { get { return Name; } }
        public bool IsPolice { get { return Class == VehicleClass.Police || Class == VehicleClass.PoliceSUV || Class == VehicleClass.Enforcer || PoliceUnit; } }
        /// <summary>A hover bike: ridden astride with the rider in view, leans into turns, throws its rider in a hard crash.</summary>
        public bool IsBike { get { return Body == BodyStyle.Bike; } }
        public float TopSpeedMph { get { return TopSpeed * 2.23694f; } }
    }

    public static class VehicleDb
    {
        // Paint palettes come first: static fields initialize in order, and Build() below reads them.
        private static readonly Color[] Civilian =
        {
            new Color(0.75f, 0.75f, 0.77f), new Color(0.08f, 0.08f, 0.09f), new Color(0.92f, 0.92f, 0.9f), new Color(0.55f, 0.06f, 0.06f),
            new Color(0.08f, 0.16f, 0.4f), new Color(0.2f, 0.32f, 0.22f), new Color(0.42f, 0.38f, 0.32f), new Color(0.3f, 0.3f, 0.32f),
            new Color(0.6f, 0.52f, 0.36f), new Color(0.12f, 0.3f, 0.45f)
        };

        private static readonly Color[] Flashy =
        {
            new Color(0.9f, 0.12f, 0.08f), new Color(0.95f, 0.75f, 0.05f), new Color(0.05f, 0.55f, 0.85f), new Color(0.98f, 0.98f, 0.98f),
            new Color(0.05f, 0.05f, 0.06f), new Color(0.95f, 0.35f, 0.05f), new Color(0.15f, 0.7f, 0.25f), new Color(0.55f, 0.1f, 0.75f),
            new Color(0.9f, 0.2f, 0.55f)
        };

        /// <summary>The hover bike roster (exact names, used by the story, the courier and the police).</summary>
        public const string SportBikeName = "Vanta Stilettra";
        public const string CruiserBikeName = "Anvil Duskhauler";
        public const string CourierBikeName = "Kestrel Skiff";
        public const string PoliceBikeName = "Metro Response Courser";

        public static readonly List<VehicleDef> All = Build();
        private static readonly Dictionary<string, VehicleDef> _byName = new Dictionary<string, VehicleDef>();

        public static VehicleDef Get(int index)
        {
            return index >= 0 && index < All.Count ? All[index] : All[0];
        }

        public static VehicleDef Find(string name)
        {
            if (_byName.Count == 0) foreach (VehicleDef d in All) _byName[d.Name.ToLowerInvariant()] = d;
            VehicleDef r;
            if (name != null && _byName.TryGetValue(name.ToLowerInvariant(), out r)) return r;
            foreach (VehicleDef d in All) if (name != null && d.Name.ToLowerInvariant().Contains(name.ToLowerInvariant())) return d;
            return null;
        }

        /// <summary>The LVPD hover bike (falls back to the patrol cruiser if the roster ever loses it).</summary>
        public static VehicleDef PoliceBike { get { return Find(PoliceBikeName) ?? FirstOfClass(VehicleClass.Police); } }

        /// <summary>The cheap courier bike that Bring my bike delivers when the player has no bike of their own yet.</summary>
        public static VehicleDef CourierBike { get { return Find(CourierBikeName) ?? All[0]; } }

        public static VehicleDef FirstOfClass(VehicleClass c)
        {
            foreach (VehicleDef d in All) if (d.Class == c) return d;
            return All[0];
        }

        private static List<VehicleDef> Build()
        {
            List<VehicleDef> l = new List<VehicleDef>();
            // Compacts
            Add(l, "Kestrel Courier", "Kestrel", VehicleClass.Compact, BodyStyle.Hatch, DriveType.FWD, 3.9f, 1.75f, 1.48f, 1150f, 6800f, 46f, 1.05f, 0.31f, 4, SfxId.EngineSmall, 1.15f, 9000, 1.3f, Civilian, "Cheap, nimble and everywhere.");
            Add(l, "Marrow Ghost", "Marrow", VehicleClass.Compact, BodyStyle.Hatch, DriveType.FWD, 3.7f, 1.7f, 1.5f, 1050f, 6200f, 44f, 1.05f, 0.3f, 2, SfxId.EngineSmall, 1.25f, 7500, 1.0f, Flashy, "A city box with a big attitude.");
            Add(l, "Northline Arrow", "Northline", VehicleClass.Compact, BodyStyle.Hatch, DriveType.FWD, 4.0f, 1.78f, 1.45f, 1180f, 7600f, 50f, 1.1f, 0.32f, 4, SfxId.EngineSmall, 1.1f, 14000, 0.8f, Flashy, "Hot hatch with a turbo whistle.");
            // Sedans
            Add(l, "Kestrel Metro Starter", "Kestrel", VehicleClass.Sedan, BodyStyle.Sedan, DriveType.FWD, 4.6f, 1.82f, 1.45f, 1400f, 7800f, 50f, 1.05f, 0.34f, 4, SfxId.EngineSmall, 1f, 11894, 1.4f, Civilian, "The first car in the original database. Reliable to a fault.");
            Add(l, "Harbor Solace", "Harbor", VehicleClass.Sedan, BodyStyle.Sedan, DriveType.RWD, 4.9f, 1.88f, 1.45f, 1550f, 8600f, 52f, 1.0f, 0.35f, 4, SfxId.EngineV8, 1.15f, 18000, 1.2f, Civilian, "Full-size comfort for Glass Heights commuters.");
            Add(l, "Horizon Vektor", "Horizon", VehicleClass.Sedan, BodyStyle.Sedan, DriveType.AWD, 4.8f, 1.86f, 1.42f, 1520f, 9800f, 56f, 1.1f, 0.35f, 4, SfxId.EngineSport, 0.85f, 26000, 0.9f, Civilian, "Executive sedan with a sneaky AWD launch.");
            Add(l, "Orion Comet", "Orion", VehicleClass.Sedan, BodyStyle.Sedan, DriveType.FWD, 4.5f, 1.8f, 1.46f, 1380f, 7400f, 49f, 1.05f, 0.33f, 4, SfxId.EngineSmall, 1.05f, 12000, 1.1f, Civilian, "Practical, forgettable, perfect getaway camouflage.");
            // Muscle
            Add(l, "Torque Falcon", "Torque", VehicleClass.Muscle, BodyStyle.Coupe, DriveType.RWD, 4.95f, 1.95f, 1.34f, 1650f, 12000f, 60f, 0.95f, 0.36f, 2, SfxId.EngineV8, 0.9f, 32000, 0.7f, Flashy, "Big block, no apologies. Loves to go sideways.");
            Add(l, "Kestrel Marauder", "Kestrel", VehicleClass.Muscle, BodyStyle.Coupe, DriveType.RWD, 4.85f, 1.92f, 1.36f, 1620f, 11500f, 58f, 0.95f, 0.36f, 2, SfxId.EngineV8, 0.85f, 28000, 0.7f, Flashy, "Old Silver street-race royalty.");
            Add(l, "Bishop Barrage", "Bishop", VehicleClass.Muscle, BodyStyle.Coupe, DriveType.RWD, 5.0f, 1.96f, 1.33f, 1700f, 13000f, 62f, 0.97f, 0.37f, 2, SfxId.EngineV8, 0.8f, 36000, 0.5f, Flashy, "Supercharged and loud enough to set off car alarms.");
            // Sports
            Add(l, "Vanta Arrow", "Vanta", VehicleClass.Sports, BodyStyle.Coupe, DriveType.RWD, 4.45f, 1.95f, 1.26f, 1400f, 12500f, 68f, 1.2f, 0.35f, 2, SfxId.EngineSport, 1f, 85000, 0.45f, Flashy, "Mid-engine precision for Mirage Strip cruising.");
            Add(l, "Mistral Comet", "Mistral", VehicleClass.Sports, BodyStyle.Coupe, DriveType.AWD, 4.5f, 1.94f, 1.28f, 1450f, 13500f, 70f, 1.22f, 0.35f, 2, SfxId.EngineSport, 0.95f, 95000, 0.4f, Flashy, "All-wheel-drive grip that forgives a lot.");
            Add(l, "Orion Vista", "Orion", VehicleClass.Sports, BodyStyle.Coupe, DriveType.RWD, 4.4f, 1.9f, 1.27f, 1380f, 12000f, 66f, 1.18f, 0.34f, 2, SfxId.EngineSport, 1.05f, 72000, 0.45f, Flashy, "A roadster with neon-lit dreams.");
            // Super
            Add(l, "Bishop Vektor", "Bishop", VehicleClass.Super, BodyStyle.Wedge, DriveType.AWD, 4.65f, 2.02f, 1.12f, 1350f, 15500f, 82f, 1.32f, 0.36f, 2, SfxId.EngineSport, 1.15f, 480000, 0.12f, Flashy, "Carbon wedge, quad turbos, zero patience.");
            Add(l, "Torque Surge", "Torque", VehicleClass.Super, BodyStyle.Wedge, DriveType.RWD, 4.6f, 2.0f, 1.14f, 1320f, 15000f, 80f, 1.26f, 0.36f, 2, SfxId.EngineSport, 1.25f, 420000, 0.12f, Flashy, "The Strip's favorite way to lose a license.");
            // SUVs
            Add(l, "Anvil Ridge", "Anvil", VehicleClass.SUV, BodyStyle.SUV, DriveType.AWD, 4.9f, 2.0f, 1.85f, 2100f, 11000f, 50f, 1.0f, 0.42f, 4, SfxId.EngineV8, 0.95f, 42000, 1.0f, Civilian, "Built for the Quartz Badlands, bought for school runs.");
            Add(l, "Bishop Coyote", "Bishop", VehicleClass.SUV, BodyStyle.SUV, DriveType.AWD, 4.7f, 1.96f, 1.8f, 1950f, 10500f, 49f, 1.0f, 0.41f, 4, SfxId.EngineV8, 1f, 38000, 0.9f, Civilian, "Luxury on stilts.");
            Add(l, "Harbor Ranger", "Harbor", VehicleClass.SUV, BodyStyle.SUV, DriveType.AWD, 4.6f, 1.95f, 1.82f, 2000f, 9800f, 47f, 1.0f, 0.42f, 4, SfxId.EngineSmall, 0.8f, 30000, 0.8f, Civilian, "A boxy off-roader that refuses to die.");
            // Pickups
            Add(l, "Anvil Ranger", "Anvil", VehicleClass.Pickup, BodyStyle.Pickup, DriveType.RWD, 5.4f, 2.0f, 1.85f, 2200f, 11500f, 48f, 0.95f, 0.43f, 2, SfxId.EngineTruck, 1.3f, 26000, 0.9f, Civilian, "Desert workhorse with a dented tailgate.");
            Add(l, "Marrow Coyote", "Marrow", VehicleClass.Pickup, BodyStyle.Pickup, DriveType.AWD, 5.2f, 1.96f, 1.82f, 2100f, 11000f, 47f, 0.98f, 0.42f, 2, SfxId.EngineTruck, 1.4f, 22000, 0.8f, Civilian, "Lifted, loud and covered in Badlands dust.");
            // Vans
            Add(l, "Harbor Vista", "Harbor", VehicleClass.Van, BodyStyle.Van, DriveType.FWD, 5.1f, 1.98f, 2.0f, 2100f, 9000f, 44f, 0.95f, 0.36f, 4, SfxId.EngineSmall, 0.85f, 21000, 0.9f, Civilian, "Family minivan, surprisingly good at ramming.");
            Add(l, "Mistral Solace", "Mistral", VehicleClass.Van, BodyStyle.Van, DriveType.RWD, 5.4f, 2.05f, 2.3f, 2500f, 9500f, 40f, 0.92f, 0.38f, 2, SfxId.EngineTruck, 1.5f, 19000, 0.8f, Civilian, "Delivery van. Nobody asks what is in the back.");
            // Heavy
            Add(l, "Vanta Sentinel Box Truck", "Vanta", VehicleClass.BoxTruck, BodyStyle.BoxTruck, DriveType.RWD, 7.6f, 2.4f, 3.2f, 6500f, 22000f, 33f, 0.9f, 0.5f, 2, SfxId.EngineTruck, 1f, 45000, 0.5f, Civilian, "Moves furniture and getaway crews.");
            Add(l, "Metro Transit Coach", "Metro", VehicleClass.Bus, BodyStyle.Bus, DriveType.RWD, 11f, 2.55f, 3.2f, 11000f, 32000f, 29f, 0.9f, 0.52f, 2, SfxId.EngineTruck, 0.8f, 90000, 0.25f, new[] { new Color(0.12f, 0.45f, 0.65f) }, "Las Veyra public transit. Exact change only.");
            // Services
            Add(l, "Vanta Metro Cab", "Vanta", VehicleClass.Taxi, BodyStyle.Sedan, DriveType.FWD, 4.7f, 1.84f, 1.46f, 1450f, 8200f, 50f, 1.05f, 0.34f, 4, SfxId.EngineSmall, 1f, 15000, 0.7f, new[] { new Color(0.95f, 0.75f, 0.1f) }, "Yellow, battered, everywhere. Press J for taxi work.");
            Add(l, "Harbor Lifeline Ambulance", "Harbor", VehicleClass.Ambulance, BodyStyle.Van, DriveType.RWD, 5.8f, 2.15f, 2.6f, 3200f, 12000f, 44f, 0.95f, 0.4f, 4, SfxId.EngineTruck, 1.2f, 60000, 0.12f, new[] { new Color(0.95f, 0.95f, 0.95f) }, "Lights, siren and a stretcher in the back.");
            Add(l, "Metro Response Cruiser", "Metro", VehicleClass.Police, BodyStyle.Sedan, DriveType.RWD, 4.95f, 1.9f, 1.45f, 1700f, 12500f, 60f, 1.12f, 0.36f, 4, SfxId.EngineV8, 1f, 0, 0f, new[] { new Color(0.06f, 0.06f, 0.08f) }, "LVPD patrol cruiser. Stealing one is a two-star decision.");
            Add(l, "Metro Response Interceptor", "Metro", VehicleClass.PoliceSUV, BodyStyle.SUV, DriveType.AWD, 4.95f, 2.0f, 1.8f, 2150f, 13000f, 55f, 1.05f, 0.42f, 4, SfxId.EngineV8, 0.9f, 0, 0f, new[] { new Color(0.06f, 0.06f, 0.08f) }, "LVPD pursuit SUV.");
            Add(l, "Bishop Enforcer", "Bishop", VehicleClass.Enforcer, BodyStyle.Van, DriveType.AWD, 5.6f, 2.2f, 2.5f, 4200f, 16000f, 45f, 0.95f, 0.44f, 4, SfxId.EngineTruck, 0.9f, 0, 0f, new[] { new Color(0.08f, 0.08f, 0.1f) }, "Armored SWAT transport.");
            Add(l, "Anvil Sentinel Armored Truck", "Anvil", VehicleClass.Armored, BodyStyle.BoxTruck, DriveType.AWD, 6.6f, 2.35f, 2.9f, 7000f, 24000f, 36f, 0.92f, 0.5f, 2, SfxId.EngineTruck, 0.85f, 0, 0.05f, new[] { new Color(0.32f, 0.36f, 0.3f) }, "Cash-in-transit. Somebody always tries.");
            // Hover bikes (appended after the cars so every older index stays the same). Width and height include the
            // rider; the wheel radius is the thruster pod radius. Bikes out-accelerate every car (see BikeMath).
            Add(l, SportBikeName, "Vanta", VehicleClass.Bike, BodyStyle.Bike, DriveType.RWD, 2.15f, 0.78f, 1.3f, 300f, 4600f, 62f, 1.35f, 0.2f, 2, SfxId.EngineBike, 1.15f, 42000, 0.45f, Flashy, "A needle of carbon and light. Nothing in Las Veyra leaves a green light faster.");
            Add(l, CruiserBikeName, "Anvil", VehicleClass.Bike, BodyStyle.Bike, DriveType.RWD, 2.45f, 0.95f, 1.35f, 400f, 4800f, 54f, 1.15f, 0.24f, 2, SfxId.EngineBike, 0.8f, 30000, 0.35f, Civilian, "Long, low and heavy. It does not corner so much as negotiate.");
            Add(l, CourierBikeName, "Kestrel", VehicleClass.Bike, BodyStyle.Bike, DriveType.RWD, 1.95f, 0.72f, 1.3f, 250f, 3100f, 50f, 1.25f, 0.18f, 2, SfxId.EngineBike, 1.35f, 9000, 0.8f, Flashy, "Cheap, light and everywhere. The city's couriers live on these.");
            Add(l, PoliceBikeName, "Metro", VehicleClass.Bike, BodyStyle.Bike, DriveType.RWD, 2.2f, 0.82f, 1.32f, 330f, 4700f, 60f, 1.3f, 0.2f, 1, SfxId.EngineBike, 1.05f, 0, 0f, new[] { new Color(0.06f, 0.07f, 0.12f) }, "LVPD pursuit bike. Quick, loud and very blue.");
            l[l.Count - 1].PoliceUnit = true;

            foreach (VehicleDef d in l)
            {
                d.Emergency = d.IsPolice || d.Class == VehicleClass.Ambulance;
                d.Siren = d.Emergency;
                if (d.IsBike) d.Health = d.Mass >= 380f ? 800f : (d.PoliceUnit ? 750f : (d.Mass <= 260f ? 500f : 600f));
                else if (d.Class == VehicleClass.Enforcer) d.Health = 2600f;
                else if (d.Class == VehicleClass.Armored) d.Health = 4000f;
                else if (d.Class == VehicleClass.Bus || d.Class == VehicleClass.BoxTruck) d.Health = 2200f;
                else if (d.Class == VehicleClass.SUV || d.Class == VehicleClass.PoliceSUV || d.Class == VehicleClass.Pickup) d.Health = 1300f;
                d.Clearance = d.Body == BodyStyle.Wedge ? 0.14f : (d.Body == BodyStyle.Coupe ? 0.16f : (d.Body == BodyStyle.SUV || d.Body == BodyStyle.Pickup ? 0.3f : 0.2f));
                if (d.Body == BodyStyle.BoxTruck || d.Body == BodyStyle.Bus) d.Clearance = 0.32f;
                d.SteerAngle = d.Body == BodyStyle.Bus || d.Body == BodyStyle.BoxTruck ? 38f : (d.IsBike ? 32f : 34f);
            }
            return l;
        }

        private static void Add(List<VehicleDef> l, string name, string make, VehicleClass cls, BodyStyle body, DriveType drive, float length, float width, float height,
            float mass, float force, float top, float grip, float wheelRadius, int seats, SfxId engine, float pitch, int price, float weight, Color[] paints, string blurb)
        {
            l.Add(new VehicleDef
            {
                Index = l.Count,
                Name = name,
                Make = make,
                Class = cls,
                Body = body,
                Drive = drive,
                Length = length,
                Width = width,
                Height = height,
                Mass = mass,
                DriveForce = force,
                TopSpeed = top,
                Grip = grip,
                WheelRadius = wheelRadius,
                Seats = seats,
                Engine = engine,
                EnginePitch = pitch,
                Price = price,
                SpawnWeight = weight,
                Paints = paints,
                Blurb = blurb
            });
        }

        /// <summary>Random civilian model weighted by spawn weight, flavored by district (Strip and Glass Heights get flashier cars).</summary>
        public static VehicleDef RandomCivilian(District district, System.Random r)
        {
            float total = 0f;
            foreach (VehicleDef d in All) total += Weight(d, district);
            double pick = r.NextDouble() * total;
            foreach (VehicleDef d in All)
            {
                pick -= Weight(d, district);
                if (pick <= 0) return d;
            }
            return All[0];
        }

        private static float Weight(VehicleDef d, District district)
        {
            if (d.IsPolice || d.Class == VehicleClass.Ambulance || d.Class == VehicleClass.Armored) return 0f;
            float w = d.SpawnWeight;
            bool rich = district == District.MirageStrip || district == District.GlassHeights || district == District.SkyrailOrbit;
            bool rough = district == District.AshUnderpass || district == District.BrassProjects || district == District.SunkenRow;
            if (rich && (d.Class == VehicleClass.Sports || d.Class == VehicleClass.Super || d.Class == VehicleClass.Muscle)) w *= 3f;
            if (rough && (d.Class == VehicleClass.Super || d.Class == VehicleClass.Sports)) w *= 0.3f;
            if (rough && (d.Class == VehicleClass.Compact || d.Class == VehicleClass.Pickup || d.Class == VehicleClass.Van)) w *= 1.6f;
            if (district == District.QuartzBadlands && (d.Class == VehicleClass.Pickup || d.Class == VehicleClass.SUV || d.Class == VehicleClass.BoxTruck)) w *= 3f;
            if (district == District.LasVeyraCore && d.Class == VehicleClass.Taxi) w *= 3f;
            if (district == District.QuartzBadlands && d.Class == VehicleClass.Bus) w = 0f;
            if (d.IsBike && district == District.QuartzBadlands) w *= 0.5f;
            if (d.IsBike && rough && d.Name == CourierBikeName) w *= 1.5f;
            return w;
        }
    }
}
