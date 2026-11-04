using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One existing civic emitter and the physical opening that its art must preserve.</summary>
    public sealed class HeartRewritePlanWR01
    {
        public string StableId, Family, Service, CollisionOwner, FitPolicy, FallbackPolicy, PortalContract;
        public string CollisionPolicy = "NATIVE_COLLISION_REVIEW";
        public int Seed;
        public Vector3 Origin, PortalCenter, PortalSize;
        public float Yaw;
        public Bounds OriginalEnvelope;
    }

    public static partial class HeartPlanFR01
    {
        public static bool RewriteSelectedWR01(bool preview, bool proofComparison, bool proofLegacy)
        { return preview && (!proofComparison || !proofLegacy); }

        /// <summary>Imported camera candidacy cannot be established by a retained witness or an invalid imported root.</summary>
        public static bool RewriteImportedEvidenceWR01(bool present, bool active, bool valid, int enabledRenderers, int frustumCandidates)
        { return present && active && valid && enabledRenderers > 0 && frustumCandidates > 0; }

        /// <summary>The three source-identified dominant receipt frames. Their original landing and physics never move.</summary>
        public static bool TryRewriteBuildingPlanWR01(CityLayout layout, Building building,
            out HeartRewritePlanWR01 plan, out string reason)
        {
            plan = null; reason = "Not one of the three source-audited revision-10 skyline frames.";
            if (layout == null || !layout.GothicCivicH5 || building == null || building.GothicFamily04OB != 9) return false;
            RectF expected; float height;
            switch (building.Seed)
            {
                case 809556: expected = new RectF(68.09897f, 367.45792f, 113.100075f, 388.64154f); height = 162.37482f; break;
                case 32874: expected = new RectF(-232.5f, 367.13513f, -181.75201f, 387.17123f); height = 128.66837f; break;
                case 53496: expected = new RectF(281.06775f, 38.778107f, 321.06775f, 72.77811f); height = 162.56671f; break;
                default: return false;
            }
            reason = "Audited skyline footprint or supported receipt height changed. Preserve this original frame.";
            RectF actual = building.Footprint;
            if (!Finite(new Vector3(actual.X0, building.Height, actual.X1)) || !Finite(new Vector3(actual.Z0, 0, actual.Z1)) ||
                Mathf.Abs(actual.X0 - expected.X0) > .02f || Mathf.Abs(actual.X1 - expected.X1) > .02f ||
                Mathf.Abs(actual.Z0 - expected.Z0) > .02f || Mathf.Abs(actual.Z1 - expected.Z1) > .02f || Mathf.Abs(building.Height - height) > .02f) return false;
            plan = new HeartRewritePlanWR01
            {
                StableId = "revision:10/seed:" + building.Seed + "/skyline", Family = "WRSkyline" + building.Seed,
                Service = "", Seed = building.Seed, Origin = actual.Center(CityLayout.CurbHeight), Yaw = 0,
                OriginalEnvelope = new Bounds(Vector3.up * building.Height * .5f, new Vector3(actual.Width, building.Height, actual.Depth)),
                CollisionOwner = "CityBuilder.Gothic04OB / 04O-B exact compound solid",
                PortalContract = "Open four-pier frame. No doorway is replaced. Every original support, roof landing and ordered receipt remains under its original owner.",
                FitPolicy = "Exact audited seed, footprint and receipt height. Authored local metres at footprint centre, unit scale. Original full source geometry remains as physical witness. New upper belfry and roof volumes are nonauthoritative visuals without new collision; native flight and contact review required before acceptance.",
                FallbackPolicy = "Restore every captured source append on any preparation or transaction failure."
            };
            reason = "Existing four-pier skyline visual can be isolated before merging. All collision and roof receipts remain.";
            return true;
        }

        /// <summary>Only these decorative crown emits are separated. Doors and supporting interiors remain visible.</summary>
        public static bool CrownPartWR01(string model)
        { return model == "slate_roof" || model == "mineral_pier"; }

        /// <summary>Fixed authored metres and frame, never stretch a generic tower to fit a different service.</summary>
        public static bool RewriteArtBoundsWR01(string family, Bounds bounds, out string reason)
        {
            reason = "Unknown civic crown family or invalid authored bounds.";
            if (!Finite(bounds.center) || !Finite(bounds.size) || bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0) return false;
            Vector3 min, max;
            if (family == "WRArchiveCrown")
            { min = new Vector3(-15.020001f, 8.329999f, -.725f); max = new Vector3(15.020001f, 35.099998f, 48.479999f); }
            else if (family == "WROfficeCrown")
            { min = new Vector3(-15.020001f, 8.329999f, -.95f); max = new Vector3(15.020001f, 33.200001f, 30.429998f); }
            else if (family == "WRSkyline809556")
            { min = new Vector3(-22.750553f, -.006f, -10.509974f); max = new Vector3(22.750553f, 166.274826f, 10.509974f); }
            else if (family == "WRSkyline32874")
            { min = new Vector3(-25.623995f, -.006f, -9.947689f); max = new Vector3(25.623995f, 132.568369f, 9.947689f); }
            else if (family == "WRSkyline53496")
            { min = new Vector3(-20.25f, -.006f, -16.790003f); max = new Vector3(20.25f, 166.466705f, 16.790003f); }
            else return false;
            for (int axis = 0; axis < 3; axis++)
                if (Mathf.Abs(bounds.min[axis] - min[axis]) > .03f || Mathf.Abs(bounds.max[axis] - max[axis]) > .03f)
                { reason = "Authored crown frame or dimensions changed. Original portal and physical correspondence require review."; return false; }
            reason = family.StartsWith("WRSkyline", StringComparison.Ordinal) ?
                "Exact authored skyline metres verified. Original pier and receipt surfaces remain visible; native contact review remains required." :
                "Authored crown metres verified; every new surface is above the original ground portals.";
            return true;
        }

        /// <summary>Read the existing service binding. Unknown geometry or a receipt roof is never guessed compatible.</summary>
        public static bool TryRewritePlanWR01(CityLayout layout, VenueBinding04C binding,
            out HeartRewritePlanWR01 plan, out string reason)
        {
            plan = null; reason = "Requires the existing revision-10 civic service binding.";
            if (layout == null || !layout.GothicCivicH5 || binding == null || binding.Definition == null || binding.Building == null) return false;
            string service = binding.Definition.Id;
            reason = "Service is outside the two authored civic crown families.";
            if (service != "archive" && service != "office") return false;
            float depth = service == "archive" ? 48f : 30f;
            reason = "Original service dimensions, transform or receipt roof require native collision review.";
            if (!Finite(new Vector3(binding.Width, binding.Building.Height, binding.Depth)) || binding.Building.Height <= 0 ||
                Mathf.Abs(binding.Width - 30f) > .01f || Mathf.Abs(binding.Depth - depth) > .01f ||
                binding.Building.RoofPad04OB || !Finite(binding.Origin) || float.IsNaN(binding.Yaw) || float.IsInfinity(binding.Yaw)) return false;
            plan = new HeartRewritePlanWR01
            {
                StableId = "revision:10/venue:" + service + "/seed:" + binding.Building.Seed + "/crown",
                Family = service == "archive" ? "WRArchiveCrown" : "WROfficeCrown",
                Service = service, Seed = binding.Building.Seed, Origin = binding.Origin, Yaw = binding.Yaw,
                OriginalEnvelope = new Bounds(new Vector3(0, binding.Building.Height * .5f, depth * .5f),
                    new Vector3(binding.Width, binding.Building.Height, depth)),
                PortalCenter = new Vector3(0, 1.6f, .15f), PortalSize = new Vector3(2.8f, 3.2f, .3f),
                PortalContract = "Required canonical 04C Front opening (halfDoor 1.4, lintel 3.2, front plane .15, depth .3). Retained H5 doorway unchanged; native opening not measured.",
                CollisionOwner = "VenueView04C / H3 fixed-asset service shell / " + service,
                FitPolicy = "Authored local frame, unit scale. Original low roof and mineral surfaces retained as visible physical witnesses. New crown volume is presentation only; native roof, camera and flight contact review required.",
                FallbackPolicy = "Keep the isolated original crown on every missing asset, invalid candidate or failed transaction."
            };
            reason = "Only slate_roof and mineral_pier visuals may switch. All original collision, portal, structure and service owners remain.";
            return true;
        }
    }

    [Serializable]
    public sealed class FantasyRewriteRecordWR01
    {
        public string stableId = "", family = "", state = "", reason = "", collisionOwner = "", fitPolicy = "", sourceOwner = "UNSPECIFIED";
        public string assetVariant = "Original WR01";
        public string boundsFrame = "UNSPECIFIED", portalContract = "UNSPECIFIED", collisionPolicy = "NATIVE_COLLISION_REVIEW";
        public Vector3 origin, originalBoundsCenter, originalBoundsSize, portalCenter, portalSize;
        public float yaw;
        public int seed;
        public bool admitted, active, legacySuppressed, portalKnown, importedCameraCandidate;
    }

    [Serializable]
    public sealed class FantasyRewriteProofWR01
    {
        public bool selected, prepared, policyActive;
        public int planned, admitted, active, legacyGroupsSuppressed, refused;
        public int activeRenderers, nearPlayerRenderers, inCameraFrustumRenderers;
        public int importedCrownRenderers, importedCrownFrustumRenderers, activeCivicCrowns, activeUndergroundCells;
        public int importedSkylineRenderers, importedSkylineFrustumRenderers, activeSkylineMasses;
        public string status = "World rewrite not prepared.";
        public FantasyRewriteRecordWR01[] records = new FantasyRewriteRecordWR01[0];
    }
}
