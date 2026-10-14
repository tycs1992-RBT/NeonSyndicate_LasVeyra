using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Hover bikes and the faster story opening: the pure handling maths (lean from speed and steering, steering lock,
    /// grip-capped turning, the drive curve, top speed and acceleration against the cars), the bike roster, and the
    /// opening's walking distances and bike route on the default city.
    /// </summary>
    public sealed class BikeTests
    {
        private const float Wheelbase = 1.38f;
        private const float Grip = 14f;

        // ------------------------------------------------------------------ handling maths

        [Test]
        public void NoLeanWhenStoppedOrReversing()
        {
            Assert.That(BikeMath.TargetLean(0f, 25f, Wheelbase, Grip), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(BikeMath.TargetLean(-6f, 25f, Wheelbase, Grip), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(BikeMath.LeanForTurn(30f, 0f), Is.EqualTo(0f).Within(1e-4f), "straight ahead stays upright");
        }

        [Test]
        public void LeanFollowsTheSteeringAndIsSymmetric()
        {
            float right = BikeMath.TargetLean(15f, 8f, Wheelbase, Grip);
            float left = BikeMath.TargetLean(15f, -8f, Wheelbase, Grip);
            Assert.That(right, Is.GreaterThan(1f), "steering right leans right (positive)");
            Assert.That(left, Is.LessThan(-1f), "steering left leans left (negative, as HumanoidAnimator.RideLean expects)");
            Assert.That(left, Is.EqualTo(-right).Within(1e-4f));
        }

        [Test]
        public void LeanGrowsWithSpeedAndIsCapped()
        {
            float slow = BikeMath.TargetLean(5f, 6f, Wheelbase, Grip);
            float fast = BikeMath.TargetLean(12f, 6f, Wheelbase, Grip);
            Assert.That(fast, Is.GreaterThan(slow), "the same steering at more speed banks harder");
            for (float v = 0f; v <= 70f; v += 5f)
                for (float s = -32f; s <= 32f; s += 4f)
                {
                    float lean = BikeMath.TargetLean(v, s, Wheelbase, Grip * 3f);
                    Assert.That(Mathf.Abs(lean), Is.LessThanOrEqualTo(BikeMath.MaxLean + 1e-3f), "lean at " + v + " m/s, " + s + " degrees");
                    Assert.That(TestData.Finite(lean), Is.True);
                }
        }

        [Test]
        public void ShownLeanEasesInAtALimitedRate()
        {
            float lean = 0f;
            float dt = 1f / 60f;
            for (int i = 0; i < 6; i++)
            {
                float next = BikeMath.StepLean(lean, BikeMath.MaxLean, dt);
                Assert.That(next - lean, Is.LessThanOrEqualTo(BikeMath.LeanRate * dt + 1e-4f), "never snaps");
                Assert.That(next, Is.GreaterThan(lean), "moves toward the target");
                lean = next;
            }
            for (int i = 0; i < 120; i++) lean = BikeMath.StepLean(lean, BikeMath.MaxLean, dt);
            Assert.That(lean, Is.EqualTo(BikeMath.MaxLean).Within(0.5f), "settles within two seconds");
            Assert.That(BikeMath.StepLean(10f, 0f, 0f), Is.EqualTo(10f), "a zero time step changes nothing");
        }

        [Test]
        public void SteeringLockShrinksWithSpeed()
        {
            float stand = BikeMath.SteerLock(32f, 0f);
            float city = BikeMath.SteerLock(32f, 15f);
            float top = BikeMath.SteerLock(32f, 60f);
            Assert.That(stand, Is.EqualTo(32f).Within(1e-4f));
            Assert.That(city, Is.LessThan(stand));
            Assert.That(top, Is.LessThan(city));
            Assert.That(top, Is.GreaterThan(5f), "still steerable flat out");
            Assert.That(BikeMath.SteerLock(32f, -15f), Is.EqualTo(city).Within(1e-4f), "reverse uses the same lock");
        }

        [Test]
        public void TurningNeverAsksForMoreGripThanTheBikeHas()
        {
            for (float v = 1f; v <= 70f; v += 3f)
                for (float s = -32f; s <= 32f; s += 2f)
                {
                    float lockDeg = BikeMath.SteerLock(32f, v);
                    float yaw = BikeMath.YawRate(v, Mathf.Clamp(s, -lockDeg, lockDeg), Wheelbase, Grip);
                    Assert.That(Mathf.Abs(v * yaw), Is.LessThanOrEqualTo(Grip + 1e-3f), "sideways pull at " + v + " m/s");
                }
            Assert.That(BikeMath.YawRate(10f, 10f, Wheelbase, Grip), Is.GreaterThan(0f), "right steering turns right");
            Assert.That(BikeMath.YawRate(-3f, 10f, Wheelbase, Grip), Is.LessThan(0f), "pushed backwards the bike turns the other way");
            Assert.That(BikeMath.YawRate(0f, 30f, Wheelbase, Grip), Is.EqualTo(0f).Within(1e-5f), "no turning on the spot");
        }

        [Test]
        public void DriveForceFadesToNothingAtTopSpeed()
        {
            Assert.That(BikeMath.DriveForce(1f, 0f, 4000f, 60f), Is.EqualTo(4000f).Within(1e-2f));
            Assert.That(BikeMath.DriveForce(1f, 60f, 4000f, 60f), Is.EqualTo(0f).Within(1e-2f));
            Assert.That(BikeMath.DriveForce(0.5f, 10f, 4000f, 60f), Is.EqualTo(2000f).Within(1e-2f), "half throttle, half force");
            Assert.That(BikeMath.DriveForce(0f, 10f, 4000f, 60f), Is.EqualTo(0f));
            float last = float.MaxValue;
            for (float v = 0f; v <= 66f; v += 1f)
            {
                float f = BikeMath.DriveForce(1f, v, 4000f, 60f);
                Assert.That(f, Is.LessThanOrEqualTo(last + 1e-3f), "never grows with speed");
                last = f;
            }
        }

        // ------------------------------------------------------------------ the roster

        private static List<VehicleDef> Bikes()
        {
            List<VehicleDef> l = new List<VehicleDef>();
            foreach (VehicleDef d in VehicleDb.All) if (d.IsBike) l.Add(d);
            return l;
        }

        [Test]
        public void RosterHasTheFourBikes()
        {
            List<VehicleDef> bikes = Bikes();
            Assert.That(bikes.Count, Is.EqualTo(4));
            foreach (string n in new[] { VehicleDb.SportBikeName, VehicleDb.CruiserBikeName, VehicleDb.CourierBikeName, VehicleDb.PoliceBikeName })
            {
                VehicleDef d = VehicleDb.Find(n);
                Assert.That(d != null && d.Name == n && d.IsBike && d.Class == VehicleClass.Bike, Is.True, n);
                Assert.That(d.Seats, Is.GreaterThanOrEqualTo(1), n);
                Assert.That(d.WheelRadius, Is.GreaterThan(0.1f), n + " pod radius");
                Assert.That(d.Engine, Is.EqualTo(SfxId.EngineBike), n);
                Assert.That(d.Paints != null && d.Paints.Length > 0, Is.True, n);
            }
            VehicleDef cop = VehicleDb.PoliceBike;
            Assert.That(cop.Name, Is.EqualTo(VehicleDb.PoliceBikeName));
            Assert.That(cop.IsPolice && cop.Siren && cop.Emergency, Is.True, "the LVPD bike counts as police");
            Assert.That(cop.SpawnWeight, Is.EqualTo(0f), "never civilian traffic");
            Assert.That(VehicleDb.CourierBike.SpawnWeight, Is.GreaterThan(0f), "couriers ride in traffic");
            Assert.That(VehicleDb.Find(VehicleDb.SportBikeName).Seats, Is.EqualTo(2), "the story bike carries Luma");
        }

        [Test]
        public void BikesAreAppendedAfterEveryCar()
        {
            int lastCar = -1, firstBike = int.MaxValue;
            for (int i = 0; i < VehicleDb.All.Count; i++)
            {
                Assert.That(VehicleDb.All[i].Index, Is.EqualTo(i), "index matches position");
                if (VehicleDb.All[i].IsBike) firstBike = Mathf.Min(firstBike, i);
                else lastCar = i;
            }
            Assert.That(firstBike, Is.GreaterThan(lastCar), "older vehicle indices never move");
            Assert.That(VehicleDb.Get(lastCar).Name, Is.EqualTo("Anvil Sentinel Armored Truck"));
        }

        [Test]
        public void SportBikeTopSpeedIsFiftyFiveToSixtyTwo()
        {
            VehicleDef d = VehicleDb.Find(VehicleDb.SportBikeName);
            float top;
            BikeMath.RunTo(999f, d.Mass, d.DriveForce, d.TopSpeed, BikeMath.DragCoefficient(d.Width, d.Height), true, 40f, out top);
            Assert.That(top, Is.InRange(55f, 62f), "sport bike top speed " + top);
            foreach (VehicleDef b in Bikes())
            {
                float t;
                BikeMath.RunTo(999f, b.Mass, b.DriveForce, b.TopSpeed, BikeMath.DragCoefficient(b.Width, b.Height), true, 40f, out t);
                Assert.That(t, Is.InRange(42f, 62f), b.Name + " top speed " + t);
            }
        }

        [Test]
        public void BikesOutAccelerateTheCars()
        {
            // Time to 27 m/s (about 60 mph) from a standstill, everyone on the same drive curve and drag formula.
            const float target = 27f;
            List<float> cars = new List<float>();
            float bestCar = float.MaxValue;
            foreach (VehicleDef d in VehicleDb.All)
            {
                if (d.IsBike || d.TopSpeed <= target) continue;
                float top;
                float t = BikeMath.RunTo(target, d.Mass, d.DriveForce, d.TopSpeed, BikeMath.DragCoefficient(d.Width, d.Height), false, 30f, out top);
                cars.Add(t);
                bestCar = Mathf.Min(bestCar, t);
            }
            cars.Sort();
            float median = cars[cars.Count / 2];
            VehicleDef sport = VehicleDb.Find(VehicleDb.SportBikeName);
            float sportTop;
            float sportTime = BikeMath.RunTo(target, sport.Mass, sport.DriveForce, sport.TopSpeed, BikeMath.DragCoefficient(sport.Width, sport.Height), true, 30f, out sportTop);
            Assert.That(sportTime, Is.LessThan(bestCar), "the sport bike beats every car off the line (" + sportTime + " s vs " + bestCar + " s)");
            Assert.That(sportTime, Is.LessThan(3f), "60 mph in under three seconds");
            foreach (VehicleDef b in Bikes())
            {
                float top;
                float t = BikeMath.RunTo(target, b.Mass, b.DriveForce, b.TopSpeed, BikeMath.DragCoefficient(b.Width, b.Height), true, 30f, out top);
                Assert.That(t, Is.LessThan(median), b.Name + " is quicker than a typical car (" + t + " s vs " + median + " s)");
            }
        }

        // ------------------------------------------------------------------ the opening on the default city

        [Test]
        public void OpeningNeverAsksForALongWalk()
        {
            CityLayout L = TestData.City;
            SpecialSite site = L.Site("CrystalManor");
            Assert.That(site, Is.Not.Null, "Crystal Manor site");
            Vector3 manor = site.Position;
            Vector3 start = PrologueLayout.PlayerStart(manor);
            Vector3 bike = PrologueLayout.BikeSpot(manor);
            Assert.That(Flat(start, bike), Is.LessThan(8f), "the bike is right beside where Malik wakes");
            Assert.That(Flat(start, PrologueLayout.Crystal(manor)), Is.LessThanOrEqualTo(PrologueLayout.MaxWalk), "start to the crystal");
            Assert.That(Flat(bike, PrologueLayout.Crystal(manor)), Is.LessThanOrEqualTo(PrologueLayout.MaxWalk), "crystal back to the bike");
            Assert.That(Flat(bike, PrologueLayout.HunterCenter(manor)), Is.LessThanOrEqualTo(PrologueLayout.MaxWalk), "fight back to the bike");
            Assert.That(L.IsInsideBuilding(bike, 1.2f), Is.False, "the bike is not inside the ruins' walls");
            Assert.That(L.IsInsideBuilding(start, 0.4f), Is.False, "Malik does not wake inside a wall");
        }

        [Test]
        public void BikeRouteToTheBlockadeMissesTheCanyonWallsAndRuins()
        {
            CityLayout L = TestData.City;
            Vector3 manor = L.Site("CrystalManor").Position;
            SpecialSite canyon = L.Site("Canyon");
            SpecialSite blockade = L.Site("Blockade");
            Assert.That(canyon != null && blockade != null, Is.True, "canyon and blockade sites");
            Vector3 from = PrologueLayout.BikeSpot(manor);
            Vector3 to = PrologueLayout.BlockadeTrigger(blockade.Position);
            Assert.That(PrologueLayout.CrossesCanyonWall(canyon.Position, from, to, 3f), Is.False, "a straight ride clears the canyon rock walls");
            // The old opening's car spot sat behind the east wall from the manor: the check does catch that.
            Assert.That(PrologueLayout.CrossesCanyonWall(canyon.Position, manor, new Vector3(300f, 0f, 1450f), 0f), Is.True, "a line into the canyon is caught");
            float length = Flat(from, to);
            for (float d = 0f; d <= length; d += 2f)
            {
                Vector3 p = Vector3.Lerp(from, to, d / length);
                Assert.That(L.IsInsideBuilding(p, 1.5f), Is.False, "route point " + d.ToString("0") + " m along");
            }
            Assert.That(length, Is.LessThan(400f), "a short ride to the blockade (" + length.ToString("0") + " m)");
        }

        [Test]
        public void AfterThePrologueMalikStandsOutsideTheClinicWithHisBike()
        {
            CityLayout L = TestData.City;
            Poi clinic = L.FindPoi("Mara Solane Street Clinic");
            Poi jax = L.FindPoi("Jax Calder");
            Assert.That(clinic != null && jax != null, Is.True, "clinic and Jax Calder");
            Vector3 stand = PrologueLayout.ClinicStand(clinic.Position, clinic.Facing);
            Vector3 bike = PrologueLayout.ClinicBike(clinic.Position, clinic.Facing);
            Assert.That(L.IsInsideBuilding(stand, 0.3f), Is.False, "standing spot is outside");
            Assert.That(L.IsInsideBuilding(bike, 0.6f), Is.False, "bike spot is outside");
            Assert.That(Flat(stand, bike), Is.LessThan(4f), "the bike is right beside him");
            List<Vector3> route = L.FindRoute(stand, jax.Position);
            Assert.That(route != null && route.Count > 0, Is.True, "a road route to Jax Calder");
            float roadLength = Flat(stand, route[0]);
            for (int i = 1; i < route.Count; i++) roadLength += Flat(route[i - 1], route[i]);
            Assert.That(roadLength, Is.LessThan(1200f), "Jax is a short ride away (" + roadLength.ToString("0") + " m)");
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
