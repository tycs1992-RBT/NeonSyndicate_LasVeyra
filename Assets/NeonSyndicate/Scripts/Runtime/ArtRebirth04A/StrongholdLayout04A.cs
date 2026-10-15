using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Exact art composition shared by the live game and the offline source-asset renderer.</summary>
    [Serializable]
    public sealed class StrongholdLayout04A
    {
        public string version, site, title;
        public int previewStyle;
        public StrongholdPart04A[] parts;
        public StrongholdCollision04A[] collisions;
        public StrongholdWaypoint04A[] route;

        /// <summary>Engine-free safety validation. This never changes the existing fort or save data.</summary>
        public void Validate(string expectedSite)
        {
            if (version != "04A" || site != expectedSite ||
                (site != "hearthspire" && site != "galehook" && site != "sunscar") ||
                string.IsNullOrEmpty(title) || previewStyle < 0 || previewStyle > 3)
                throw new InvalidOperationException("04A stronghold layout identity is invalid.");
            if (parts == null || parts.Length < 60 || parts.Length > 1200 ||
                collisions == null || collisions.Length > 96 || route == null || route.Length < 3)
                throw new InvalidOperationException("04A stronghold layout exceeds its bounded content contract.");
            foreach (StrongholdPart04A part in parts)
            {
                if (part == null || !SafeKey(part.model) || !SafeKey(part.role))
                    throw new InvalidOperationException("04A stronghold model key is invalid.");
                Vector3 at = Vector(part.position), scale = Vector(part.scale);
                Vector(part.euler);
                if (Math.Abs(at.x) >= StrongholdPlacement.PlotHalf || Math.Abs(at.z) >= StrongholdPlacement.PlotHalf ||
                    at.y < -.5f || at.y > 40f || scale.x <= 0 || scale.y <= 0 || scale.z <= 0 ||
                    scale.x > 4f || scale.y > 4f || scale.z > 4f)
                    throw new InvalidOperationException("04A stronghold art transform is outside its contract: " + part.model);
            }
            foreach (StrongholdCollision04A collision in collisions)
            {
                if (collision == null || string.IsNullOrEmpty(collision.name))
                    throw new InvalidOperationException("04A stronghold collision is incomplete.");
                Vector3 at = Vector(collision.position), size = Vector(collision.size);
                if (size.x <= 0 || size.y <= 0 || size.z <= 0 || at.y-size.y*.5f < -.02f ||
                    Math.Abs(at.x)+size.x*.5f >= StrongholdPlacement.PlotHalf ||
                    Math.Abs(at.z)+size.z*.5f >= StrongholdPlacement.PlotHalf)
                    throw new InvalidOperationException("04A stronghold collision escapes its probed plot.");
                if (site != "hearthspire")
                    throw new InvalidOperationException("04A rival art cannot introduce raid blockers.");
                if (Intersects(at,size,0f,-17.5f,6.2f,13f) ||
                    Intersects(at,size,0f,5f,12.4f,13.5f))
                    throw new InvalidOperationException("04A collider blocks a protected approach or core sightline.");
                for (int i = 0; i < 6; i++)
                {
                    Vector3 socket = StrongholdPlacement.Socket(i);
                    if (Intersects(at,size,socket.x,socket.z,6.4f,6.4f))
                        throw new InvalidOperationException("04A collider enters an existing construction socket.");
                }
            }
            foreach (StrongholdWaypoint04A waypoint in route)
            {
                if (waypoint == null || string.IsNullOrEmpty(waypoint.name))
                    throw new InvalidOperationException("04A visitor route is incomplete.");
                Vector3 point = Vector(waypoint.position);
                if (Math.Abs(point.x) >= StrongholdPlacement.PlotHalf || Math.Abs(point.z) >= StrongholdPlacement.PlotHalf)
                    throw new InvalidOperationException("04A visitor route escapes its validated plot.");
            }
        }

        public static Vector3 Vector(float[] values)
        {
            if (values == null || values.Length != 3) throw new InvalidOperationException("04A transform requires three components.");
            foreach (float value in values)
                if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidOperationException("04A transform is not finite.");
            return new Vector3(values[0],values[1],values[2]);
        }

        /// <summary>Optional vegetation is independent of structural root arches and physical routes.</summary>
        public static bool IsVegetation(string model)
        {
            return model == "living_trunk" || model == "living_canopy" || model == "willow_trunk" ||
                model == "willow_canopy" || model == "cypress_trunk" || model == "cypress_canopy" ||
                model == "fern" || model == "mushroom" || model == "flower" || model == "reeds" || model == "vine";
        }

        /// <summary>Small physical-bound preserving ornaments follow saved module type and rotation.</summary>
        public static List<StrongholdPart04A> SocketParts(IList<FortSlotSave> slots)
        {
            if (slots == null || slots.Count != 6) throw new ArgumentException("The existing six sockets are required.","slots");
            List<StrongholdPart04A> result = new List<StrongholdPart04A>(64);
            for (int i = 0; i < slots.Count; i++)
            {
                FortSlotSave slot = slots[i];
                if (slot == null) continue;
                List<StrongholdPart04A> local = new List<StrongholdPart04A>();
                FortModule module = (FortModule)slot.Module;
                if (module == FortModule.Watchtower)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        local.Add(Part("railing","trim",0,6.01f,side*2.08f,.68f,.65f,.7f));
                        local.Add(Part("crystal_cluster","crystal",side*2.02f,6.9f,0,.2f,.5f,.2f));
                    }
                    local.Add(Part("window","glass",0,1.45f,-1.3f,.6f,.8f,.35f));
                }
                else if (module == FortModule.Conservatory)
                {
                    local.Add(Part("root_arch","bark",0,.1f,-1.7f,.53f,.58f,.4f));
                    local.Add(Part("root_arch","bark",0,.1f,1.7f,.53f,.58f,.4f));
                    for (int side = -1; side <= 1; side += 2)
                    {
                        local.Add(Part("fern","leaf",side*1.8f,.1f,.6f,.6f,.8f,.6f,true));
                        local.Add(Part("flower","flower",side*1.8f,.1f,-.8f,.65f,.65f,.65f,true));
                    }
                }
                else if (module == FortModule.Bulwark)
                {
                    local.Add(Part("window","glass",0,.35f,-.43f,1.1f,.58f,.24f));
                    for (int side = -1; side <= 1; side += 2)
                        local.Add(Part("roof_finial","metal",side*2.2f,3.0f,0,.32f,.55f,.3f));
                }
                else if (module == FortModule.FlightDais)
                {
                    local.Add(Part("sky_ring","metal",0,3.08f,0,.56f,.22f,.56f));
                    for (int side = -1; side <= 1; side += 2)
                        local.Add(Part("lantern","metal",side*2.15f,3.1f,2.15f,.35f,.35f,.35f,true));
                }
                int turns = ((slot.Rotation % 4) + 4) % 4;
                Vector3 origin = StrongholdPlacement.Socket(i);
                foreach (StrongholdPart04A part in local)
                {
                    Vector3 point = Vector(part.position);
                    for (int turn = 0; turn < turns; turn++) point = new Vector3(point.z,point.y,-point.x);
                    point += origin;
                    part.position = new[] {point.x,point.y,point.z};
                    part.euler = new[] {0f,turns*90f,0f};
                    result.Add(part);
                }
            }
            return result;
        }

        private static StrongholdPart04A Part(string model,string role,float x,float y,float z,
            float sx,float sy,float sz,bool fine=false)
        {
            return new StrongholdPart04A {model=model,role=role,position=new[]{x,y,z},
                scale=new[]{sx,sy,sz},euler=new[]{0f,0f,0f},fine=fine};
        }
        private static bool Intersects(Vector3 at,Vector3 size,float x,float z,float width,float depth)
        {
            return Math.Abs(at.x-x)<(size.x+width)*.5f && Math.Abs(at.z-z)<(size.z+depth)*.5f;
        }
        private static bool SafeKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length > 40) return false;
            foreach (char c in key) if (!(c>='a'&&c<='z') && c!='_' && !(c>='0'&&c<='9')) return false;
            return true;
        }
    }

    [Serializable]
    public sealed class StrongholdPart04A
    {
        public string model, role;
        public float[] position, scale, euler;
        public bool fine;
    }
    [Serializable]
    public sealed class StrongholdCollision04A
    {
        public string name;
        public float[] position, size;
    }
    [Serializable]
    public sealed class StrongholdWaypoint04A
    {
        public string name;
        public float[] position;
    }
}
