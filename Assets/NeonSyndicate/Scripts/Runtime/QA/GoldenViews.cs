using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One fixed golden-screenshot camera: a stable id, a plain description, where it stands and what it looks at.</summary>
    public struct GoldenView
    {
        /// <summary>Stable name used in the file names, so captures from different days line up.</summary>
        public string Id;
        public string Title;
        public Vector3 Position;
        public Vector3 LookAt;
        /// <summary>Vertical field of view in degrees.</summary>
        public float Fov;
        /// <summary>Taken inside the Veil: the capture shifts into the Inner World for this view and back out after it.</summary>
        public bool Inner;
        /// <summary>The Frostline cold and the Veil's growths follow this camera instead of the parked player (zone looks).</summary>
        public bool FollowCamera;

        public GoldenView(string id, string title, Vector3 position, Vector3 lookAt, float fov)
        {
            Id = id;
            Title = title;
            Position = position;
            LookAt = lookAt;
            Fov = fov;
            Inner = false;
            FollowCamera = false;
        }
    }

    /// <summary>
    /// The fourteen golden-screenshot cameras: a mix of districts, street level, the desert, the Crystal Manor ruins and
    /// an aerial view of the whole city, then the two Phase 3 looks appended at the end (so older captures still line
    /// up): the Frost Wardens Outpost in the Frostline snow and Veyra Avenue inside the Veil. They stand over the city's
    /// fixed anchor streets (grid lines CityLayout never jitters: x = -520, 0, 380 and 800, and the canal at z = -560),
    /// in Echo Park, high over the desert, beside the desert sites or by the outpost's fixed centre, so they frame the
    /// same scene every time for the default seed. Pure data (no engine calls), so the offline tests can check that no
    /// camera starts inside a building.
    /// </summary>
    public static class GoldenViews
    {
        /// <summary>Hours of the day every view is captured at: noon and 22:00.</summary>
        public static readonly float[] Hours = { 12f, 22f };

        /// <summary>Eye height for street-level shots, in meters above the road.</summary>
        public const float EyeHeight = 1.7f;

        /// <summary>The views in capture order. The layout is only used for the desert site positions.</summary>
        public static List<GoldenView> Build(CityLayout layout)
        {
            float strip = CityLayout.StripX;
            float canal = CityLayout.CanalZ;
            Vector3 motel = SitePosition(layout, "StarliteMotel", new Vector3(1290f, 0f, -350f));
            Vector3 manor = SitePosition(layout, "CrystalManor", new Vector3(470f, 0f, 1470f));
            List<GoldenView> v = new List<GoldenView>(14);
            v.Add(new GoldenView("city_aerial", "Aerial view of the whole city from the south-west desert",
                new Vector3(-620f, 380f, -960f), new Vector3(80f, 0f, 40f), 55f));
            v.Add(new GoldenView("core_street", "Eye level on Veyra Avenue in Las Veyra Core, looking north to Glass Heights",
                new Vector3(-3.6f, EyeHeight, -270f), new Vector3(-2f, 20f, 300f), 55f));
            v.Add(new GoldenView("strip_street", "Eye level on Mirage Strip Boulevard beside the monorail",
                new Vector3(strip - 8f, EyeHeight + 0.1f, -280f), new Vector3(strip - 6f, 12f, 220f), 55f));
            v.Add(new GoldenView("strip_aerial", "Above the Sunken Row Canal, looking up the Mirage Strip",
                new Vector3(strip + 70f, 120f, canal), new Vector3(strip, 30f, 100f), 55f));
            v.Add(new GoldenView("glass_heights", "Glass Heights towers seen from Echo Park",
                new Vector3(0f, 30f, 700f), new Vector3(0f, 70f, 300f), 60f));
            v.Add(new GoldenView("old_silver", "Eye level on an Old Silver avenue, looking north",
                new Vector3(-523.6f, EyeHeight, -250f), new Vector3(-523.6f, 6f, 150f), 55f));
            v.Add(new GoldenView("sunken_row_canal", "Along the Sunken Row Canal and its bridges",
                new Vector3(-200f, 6f, canal), new Vector3(150f, 2f, canal), 55f));
            v.Add(new GoldenView("ash_railyard", "Battery Rail Yard in Ash Underpass, from above the canal",
                new Vector3(-430f, 45f, canal), new Vector3(-660f, 0f, -740f), 55f));
            v.Add(new GoldenView("grave_frequency", "Grave Frequency cemetery and radio masts",
                new Vector3(-520f, 28f, 470f), new Vector3(-600f, 0f, 680f), 55f));
            v.Add(new GoldenView("skyrail_orbit", "Skyrail Orbit rooftops from above the east edge avenue, the Strip and Glass Heights behind",
                new Vector3(CityLayout.CoreHalf, 80f, 60f), new Vector3(560f, 20f, 450f), 55f));
            v.Add(new GoldenView("desert_motel", "Starlite Motel out in the Quartz Badlands",
                motel + new Vector3(-50f, 12f, 40f), motel + new Vector3(20f, 2f, -10f), 55f));
            v.Add(new GoldenView("crystal_manor", "The Crystal Manor ruins with the canyon walls behind",
                manor + new Vector3(30f, 10f, -40f), manor + new Vector3(-2f, 1f, 4f), 55f));
            // Phase 3 looks, appended so the first twelve keep their order. The Frostline cold and the Veil follow these
            // cameras (the player waits in the desert); the Veil view is taken inside the Inner World.
            Vector3 outpost = CityLayout.WardenOutpostCenter;
            v.Add(new GoldenView("frostline_outpost", "The Frost Wardens Outpost in the Frostline snow, Rimeglass Lake beyond",
                outpost + new Vector3(130f, 22f, 30f), outpost + new Vector3(-10f, 6f, 15f), 55f) { FollowCamera = true });
            v.Add(new GoldenView("veil_core_street", "Eye level on Veyra Avenue inside the Veil: crystal growths, violet lamps and the great spires",
                new Vector3(-3.6f, EyeHeight, -270f), new Vector3(-2f, 20f, 300f), 55f) { Inner = true, FollowCamera = true });
            if(layout!=null && layout.Gothic04OB)
            {
                v.Add(new GoldenView("04oc_home", "Home approach and quiet crystal growth",new Vector3(0,1.85f,-27.25f),new Vector3(-24,3,-5),60));
                v.Add(new GoldenView("04oc_sanctuary", "Original Archive vaulted sanctuary",new Vector3(-6,1.85f,115),new Vector3(-6,8,152),60));
                v.Add(new GoldenView("04oc_forge", "Original Forge and cooled scar",new Vector3(75,1.85f,94),new Vector3(128,5,132),60));
                v.Add(new GoldenView("04oc_threshold", "Natural Shardwood border",new Vector3(-76,1.85f,30),new Vector3(-115,4,65),60));
                v.Add(new GoldenView("04oc_grotto", "Two-mouth mineral grotto",new Vector3(-110,1.85f,58),new Vector3(-110,4,78),60));
                v.Add(new GoldenView("04oc_azure", "Cyan glade and storm prowler habitat",new Vector3(-294,1.85f,158),new Vector3(-264,4,158),60));
                v.Add(new GoldenView("04oc_skyline", "Finite settlement from wilds",new Vector3(-310,34,10),new Vector3(0,10,50),60));
                v.Add(new GoldenView("04oc_flight", "Compact heart and connected wildlands from flight",new Vector3(-200,150,-160),new Vector3(-40,0,60),60));
            }
            if(layout!=null && layout.Realms04OD)
            {
                Vector3 dominion=layout.RealmChallenge04OC,azure=layout.AzureGlade04OC;
                v.Add(new GoldenView("04od_home","Matched home courtyard",new Vector3(0,1.85f,-27.25f),new Vector3(-24,3,-5),60));
                v.Add(new GoldenView("04od_archive","Matched sanctuary",new Vector3(-6,1.85f,115),new Vector3(-6,8,152),60));
                v.Add(new GoldenView("04od_dominion","Dominion vault and mineral landmark",dominion+new Vector3(52,1.7f,0),dominion+new Vector3(0,7,-13),60));
                v.Add(new GoldenView("04od_castellan","Castellan normal eye-level court",dominion+new Vector3(7,1.7f,-8),dominion+new Vector3(-1,4,0),60));
                v.Add(new GoldenView("04od_shardwood","Storm grove normal eye-level approach",azure+new Vector3(0,1.7f,-34),azure+new Vector3(0,7,18),60));
                v.Add(new GoldenView("04od_sovereign","Stormroot arena and return branch",azure+new Vector3(-15,1.7f,-8),azure+Vector3.up*5,60));
                v.Add(new GoldenView("04od_grotto","Matched original two-mouth grotto",new Vector3(-110,1.85f,58),new Vector3(-110,4,78),60));
                v.Add(new GoldenView("04od_aerial","Matched settlement and wildland composition",new Vector3(-200,150,-160),new Vector3(-40,0,60),60));
            }
            if(layout!=null && layout.Unbound04OE)
            {
                v.Add(new GoldenView("04of_oath_gallery","Existing raised Oath gallery, source-matched framing",layout.OathTerrace04OE+new Vector3(-8,1.7f,0),layout.OathTerrace04OE+new Vector3(0,3,0),60));
                v.Add(new GoldenView("04of_storm_ledge","Rooted overlook, source-matched framing",layout.StormLedge04OE+new Vector3(-8,1.7f,0),layout.StormLedge04OE+new Vector3(0,3,0),60));
                v.Add(new GoldenView("04of_storm_cavern","Existing cavern bore, source-matched framing",layout.StormCavern04OE+new Vector3(0,1.7f,-12),layout.StormCavern04OE+new Vector3(0,3,0),60));
            }
            if(layout!=null && layout.Frontier04OG)
            {
                foreach(RealmRoute04OB route in layout.RealmRoutes04OB)
                {
                    if(route.Id!="04og/oath-loop" && route.Id!="04og/storm-loop" && route.Id!="04og/oath-witness" && route.Id!="04og/storm-cavern")continue;
                    int segment=route.Id.EndsWith("loop")?1:0;Vector3 a=route.Points[segment],b=route.Points[segment+1];
                    Vector3 middle=(a+b)*.5f,forward=MathUtil.Flat(b-a).normalized;
                    v.Add(new GoldenView(route.Id.Replace("/","_"),"04OG actual frontier route "+route.Id,middle-forward*5+Vector3.up*1.7f,middle+forward*4+Vector3.up*2,60));
                }
            }
            return v;
        }

        /// <summary>File name for one capture, for example "strip_street_2200.png".</summary>
        public static string FileName(GoldenView view, float hour)
        {
            return view.Id + "_" + ClockStamp(hour) + ".png";
        }

        /// <summary>An hour of the day as four digits: 12 gives "1200", 21.5 gives "2130".</summary>
        public static string ClockStamp(float hour)
        {
            int minutes = Mathf.Clamp(Mathf.RoundToInt(hour * 60f), 0, 24 * 60 - 1);
            int hhmm = minutes / 60 * 100 + minutes % 60;
            return hhmm.ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static Vector3 SitePosition(CityLayout layout, string key, Vector3 fallback)
        {
            SpecialSite s = layout != null ? layout.Site(key) : null;
            return s != null ? s.Position : fallback;
        }
    }
}
