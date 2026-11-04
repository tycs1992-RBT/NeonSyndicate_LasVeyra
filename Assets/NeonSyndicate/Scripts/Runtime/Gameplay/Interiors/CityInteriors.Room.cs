using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// CityInteriors, the room: the plan's boxes and cylinders with the city's own materials and colliders, its lights,
    /// the keeper behind the counter and a few customers (ordinary civilians, so the live crowd bodies dress them), the
    /// way out and the counter (food and tea restore health for a few dollars, the gym's sparring round too; elsewhere
    /// the keeper says a line). Everything is destroyed when the player leaves.
    /// </summary>
    public sealed partial class CityInteriors
    {
        private GameObject _room;
        private InteriorPlan _plan;
        private readonly List<NpcController> _people = new List<NpcController>();

        private static Vector3 RoomPoint(InteriorSite site, Vector3 local)
        {
            return InhabitedCityBindings04C.LocalToWorld(site.RoomOrigin, site.Yaw, local);
        }

        private void BuildRoom(InteriorSite site)
        {
            ClearRoom();
            _plan = InteriorRooms.Plan(site.Kind, site.Seed);
            _room = new GameObject("Interior " + site.Name);
            _room.transform.SetParent(transform, false);
            _room.transform.SetPositionAndRotation(site.RoomOrigin, Quaternion.Euler(0f, site.Yaw, 0f));
            Transform t = _room.transform;
            Color wall = WallColor(site.Kind, site.Seed);
            foreach (InteriorPiece piece in _plan.Pieces)
            {
                Material m = MaterialFor(piece, wall);
                GameObject go;
                if (piece.Cylinder)
                {
                    go = Shapes.Cyl(piece.Name, t, piece.Center, new Vector3(piece.Size.x, piece.Size.y * 2f, piece.Size.z), m, piece.Collider);
                    if (Mathf.Abs(piece.Yaw) > 0.01f) go.transform.localRotation = Quaternion.Euler(0f, 0f, piece.Yaw);
                }
                else
                {
                    go = Shapes.Box(piece.Name, t, piece.Center, piece.Size, m, piece.Collider);
                    if (Mathf.Abs(piece.Yaw) > 0.01f) go.transform.localRotation = Quaternion.Euler(0f, piece.Yaw, 0f);
                }
                go.layer = Layers.World;
            }
            foreach (InteriorLight l in _plan.Lights)
            {
                Light light = new GameObject("Room light").AddComponent<Light>();
                light.transform.SetParent(t, false);
                light.transform.localPosition = l.Position;
                light.type = LightType.Point;
                light.color = l.Color;
                light.range = l.Range;
                light.intensity = l.Intensity;
                light.shadows = LightShadows.None;
            }
            InteriorSite s = site;
            Interactable exit = Interactable.Create("Interior exit", RoomPoint(site, _plan.Exit) + Vector3.up * 0.2f, 1.8f, "Press E to go back out to the street", Leave, t);
            exit.HeightTolerance = 3f;
            Interactable counter = Interactable.Create("Interior counter", RoomPoint(site, _plan.Counter) + Vector3.up * 0.2f, 1.8f, "", () => UseCounter(s), t);
            counter.DynamicPrompt = () => CounterPrompt(s);
            counter.HeightTolerance = 3f;
            Physics.SyncTransforms();
            SpawnPeople(site);
        }

        private void SpawnPeople(InteriorSite site)
        {
            System.Random r = new System.Random(site.Seed ^ 0x5f3a);
            try
            {
                NpcController keeper = NpcController.Spawn(RoomPoint(site, _plan.Keeper.Position) + Vector3.up * 0.05f, site.Yaw + _plan.Keeper.Yaw, NpcRole.Clerk, BodyFactory.CivilianLook(r), -1);
                Settle(keeper);
                foreach (InteriorSpot spot in _plan.Patrons)
                {
                    NpcController n = NpcController.Spawn(RoomPoint(site, spot.Position) + Vector3.up * 0.05f, site.Yaw + spot.Yaw, NpcRole.Civilian, BodyFactory.CivilianLook(r), -1);
                    Settle(n);
                }
            }
            catch (Exception e) { Log.Exception("Interior people " + site.Name, e); }
        }

        private void Settle(NpcController n)
        {
            if (n == null) return;
            n.State = NpcState.Idle;
            n.transform.SetParent(_room.transform, true);
            _people.Add(n);
        }

        private void ClearRoom()
        {
            foreach (NpcController n in _people) if (n != null) Destroy(n.gameObject);
            _people.Clear();
            if (_room != null) Destroy(_room);
            _room = null;
            _plan = null;
        }

        private static Color WallColor(InteriorKind kind, int seed)
        {
            Color[] walls =
            {
                new Color(0.42f, 0.22f, 0.16f), new Color(0.38f, 0.42f, 0.32f), new Color(0.82f, 0.8f, 0.76f), new Color(0.3f, 0.27f, 0.22f),
                new Color(0.3f, 0.32f, 0.36f), new Color(0.36f, 0.26f, 0.18f), new Color(0.62f, 0.58f, 0.52f), new Color(0.12f, 0.1f, 0.16f)
            };
            Color c = walls[(int)kind];
            float v = 0.92f + ((seed & 0xff) / 255f) * 0.16f;
            return new Color(c.r * v, c.g * v, c.b * v);
        }

        private static Material MaterialFor(InteriorPiece piece, Color wall)
        {
            bool tinted = piece.Tint.a > 0.01f;
            switch (piece.Surface)
            {
                case InteriorSurface.Floor: return Mats.Wood;
                case InteriorSurface.Wall: return Mats.Solid(wall, 0.15f);
                case InteriorSurface.Ceiling: return Mats.Solid(wall * 0.55f, 0.1f);
                case InteriorSurface.Trim: return Mats.Trim;
                case InteriorSurface.Wood: return Mats.Wood;
                case InteriorSurface.DarkWood: return Mats.Solid(new Color(0.17f, 0.1f, 0.06f), 0.35f);
                case InteriorSurface.Metal: return Mats.Metal;
                case InteriorSurface.DarkMetal: return Mats.DarkMetal;
                case InteriorSurface.Glass: return Mats.Glass;
                case InteriorSurface.Stone: return Mats.Concrete;
                case InteriorSurface.Paper: return Mats.Emissive(new Color(0.95f, 0.9f, 0.78f), 0.35f);
                case InteriorSurface.Glow: return Mats.Emissive(tinted ? piece.Tint : Color.white, 1.8f);
                case InteriorSurface.Fabric: return Mats.Solid(tinted ? piece.Tint : new Color(0.4f, 0.3f, 0.3f), 0.08f);
                case InteriorSurface.Leather: return Mats.Solid(tinted ? piece.Tint : new Color(0.2f, 0.12f, 0.08f), 0.3f);
                default: return Mats.Solid(tinted ? piece.Tint : new Color(0.5f, 0.5f, 0.5f), 0.3f);
            }
        }

        private string CounterPrompt(InteriorSite site)
        {
            string what; int price; float heal;
            if (InteriorRules.Service(site.Kind, out what, out price, out heal)) return "Press E to buy " + what + " ($" + price + ")";
            return "Press E to talk to the keeper";
        }

        private void UseCounter(InteriorSite site)
        {
            string what; int price; float heal;
            if (!InteriorRules.Service(site.Kind, out what, out price, out heal))
            {
                if (Game.Hud != null) Game.Hud.Notify(KeeperLine(site), 4f);
                return;
            }
            PlayerController p = Game.Player;
            if (p == null || p.Health == null || Game.Economy == null) return;
            if (!Game.Economy.Spend(price, site.Name))
            {
                if (Game.Hud != null) Game.Hud.Notify("Not enough money for " + what + ".", 2.5f);
                return;
            }
            p.Health.Heal(p.Health.MaxHealth * heal);
            if (Game.Hud != null) Game.Hud.Notify("You have " + what + ". You feel better.", 3f);
        }

        /// <summary>A line from the keeper, by kind, varied by the building.</summary>
        private static string KeeperLine(InteriorSite site)
        {
            string[] lines;
            switch (site.Kind)
            {
                case InteriorKind.Boutique: lines = new[] { "Veilcut is in this season. Everything shimmers a little.", "The Syndicates buy plain. The rich buy loud." }; break;
                case InteriorKind.Pawnshop: lines = new[] { "Shards, rings, old guns. I ask no questions.", "Someone pawned a Warden badge yesterday. Make of that what you will." }; break;
                case InteriorKind.Bookshop: lines = new[] { "Half these books were printed before the Manor rose.", "Looking for the Resonance histories? Top shelf, gloves on." }; break;
                case InteriorKind.Chapel: lines = new[] { "Sit a while. The bell only rings for the lost.", "We light a candle for everyone the Veil kept." }; break;
                case InteriorKind.Arcade: lines = new[] { "High score on the third cabinet has stood for nine years.", "Tokens at the counter. No crystal play on the cabinets." }; break;
                default: lines = new[] { "Welcome in.", "Stay as long as you like." }; break;
            }
            return site.Name + ": \"" + lines[Mathf.Abs(site.Seed) % lines.Length] + "\"";
        }
    }
}
