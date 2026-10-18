using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Shared authored-module and masonry composition for ordinary chunks and the capped district LODs.
    /// Large piers, recessed halls, sheltered shop fronts and perimeter roof pavilions replace flat office skins.
    /// All placement stays inside the original parcel, except the established twelve-centimetre facade relief.
    /// </summary>
    public static partial class VisualRebirth04AArchitecture
    {
        private static bool _checked;
        private static bool _ready;
        public static void ResetStatics() { _checked = _ready = false; }

        public static bool Ready()
        {
            if (_checked) return _ready;
            _checked = true;
            try
            {
                // Resolve every source and surface before suppressing legacy facades or appending to shared
                // chunks. A missing leaf, high LOD or palette restores the complete previous architecture.
                string[] required = { "arcade", "roof", "spire", "tower", "column", "crystal_cluster" };
                for (int i = 0; i < required.Length; i++)
                {
                    if (VisualRebirth04AAssets.Mesh(required[i], true) == null ||
                        VisualRebirth04AAssets.Mesh(required[i], false) == null) return false;
                }
                string[] low = { "banner", "lantern", "vine", "willow_trunk", "willow_canopy" };
                for (int i = 0; i < low.Length; i++)
                    if (VisualRebirth04AAssets.Mesh(low[i], true) == null) return false;
                string[] roles = { "stone", "trim", "roof", "metal", "glass", "crystal", "cloth", "bark", "leaf", "paving" };
                for (int style = 0; style < 4; style++)
                    for (int role = 0; role < roles.Length; role++)
                        if (VisualRebirth04AAssets.Material(style, roles[role]) == null) return false;
                _ready = true;
            }
            catch (Exception error) { Log.Warn("04A city kit preflight failed. Existing city architecture retained: " + error.Message); }
            return _ready;
        }

        private static Material Surface(int style, string role) { return VisualRebirth04AAssets.Material(style, role); }

        private static void Part(Func<Material, MeshBuilder> batch, int style, string role, string model,
            Vector3 at, Quaternion rotation, Vector3 scale, bool detailed)
        {
            Material material = Surface(style, role);
            if (material == null) return;
            VisualRebirth04AAssets.Append(batch(material), model, at, rotation, scale, !detailed);
        }

        public static void Tier(Building building, RectF f, float bottom, float top, int tier, int style,
            bool detailed, Func<Material, MeshBuilder> batch)
        {
            if (top <= bottom || !f.Valid) return;
            int variant = VisualRebirth04ACityPlan.Variant(building.Seed, building.Style);
            if (tier == 0) RenaissanceFrontage04H(building, f, bottom, top, style, detailed, batch);
            float height = top - bottom;
            MeshBuilder stone = batch(Surface(style, "stone"));
            MeshBuilder trim = batch(Surface(style, "trim"));
            MeshBuilder roof = batch(Surface(style, "roof"));
            stone.Tile = 2.4f;
            // Recessed mineral backing gives windows and structural piers actual depth. The original solid
            // tier collider is still generated once by CityBuilder, with exactly the previous dimensions.
            stone.Box(0, f.Center((bottom + top) * .5f), new Vector3(f.Width - .40f, height, f.Depth - .40f),
                Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            roof.FlatRect(0, f.X0, f.Z0, f.X1, f.Z1, top, UVMode.World);
            trim.Box(0, f.Center(bottom + .22f), new Vector3(f.Width, Mathf.Min(.44f, height), f.Depth),
                Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            trim.Box(0, f.Center(top - .22f), new Vector3(f.Width + .12f, .28f, f.Depth + .12f),
                Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            roof.Box(0, f.Center(top - .42f), new Vector3(f.Width -.07f, .13f, f.Depth -.07f),
                Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            for (int side = 0; side < 4; side++)
                Face(building, f, bottom, top, tier, side, style, variant, detailed, batch);
        }

        private static Vector3 FaceCenter(RectF f, float bottom, int side)
        {
            if (side == 0) return new Vector3(f.CenterX, bottom, f.Z1);
            if (side == 1) return new Vector3(f.X1, bottom, f.CenterZ);
            if (side == 2) return new Vector3(f.CenterX, bottom, f.Z0);
            return new Vector3(f.X0, bottom, f.CenterZ);
        }

        private static void Face(Building building, RectF footprint, float bottom, float top, int tier,
            int side, int style, int variant, bool detailed, Func<Material, MeshBuilder> batch)
        {
            float height = top - bottom;
            float width = side % 2 == 0 ? footprint.Width : footprint.Depth;
            int bays = VisualRebirth04ACityPlan.Bays(width, building.Style, detailed);
            int rows = VisualRebirth04ACityPlan.Registers(height, detailed);
            if (bays == 0 || rows == 0) return;
            Quaternion rotation = Quaternion.Euler(0f, side * 90f, 0f);
            Vector3 origin = FaceCenter(footprint, bottom, side);
            float spacing = width / bays;
            float rowHeight = height / rows;
            MeshBuilder trim = batch(Surface(style, "trim"));
            MeshBuilder roof = batch(Surface(style, "roof"));
            MeshBuilder metal = batch(Surface(style, "metal"));
            for (int row = 0; row < rows; row++)
            {
                float start = row * rowHeight + .65f;
                float apertureHeight = Mathf.Min(6f, rowHeight - 1.45f);
                if (apertureHeight < .6f) continue;
                for (int bay = 0; bay < bays; bay++)
                {
                    float x = (bay + .5f) * spacing - width * .5f;
                    float aspect = variant == 1 ? .58f : (variant == 4 ? .36f : .48f);
                    float windowWidth = Mathf.Min(spacing * aspect, style == 1 ? 4.6f : 4.2f);
                    Vector3 foot = origin + rotation * new Vector3(x, start, -.11f);
                    // Human-scaled glazing sits in a tall carved mineral niche. Opaque stone remains the
                    // dominant surface, including at high-rise scale; no window is stretched into an office skin.
                    roof.Box(0, foot + rotation * new Vector3(0f,apertureHeight*.5f,-.065f),
                        new Vector3(windowWidth+.38f,apertureHeight+.32f,.05f),rotation,UVMode.World);
                    Window(batch, style, foot, rotation, windowWidth, apertureHeight, variant == 4);
                    if (rowHeight > 12f)
                    {
                        float upperHeight = Mathf.Min(3.6f,rowHeight*.18f);
                        Vector3 upper = origin + rotation * new Vector3(x,row*rowHeight+rowHeight*.64f,-.11f);
                        Window(batch,style,upper,rotation,windowWidth*.56f,upperHeight,false);
                        trim.Box(0,upper+rotation*new Vector3(0f,-.24f,-.18f),
                            new Vector3(windowWidth*.72f,.35f,.75f),rotation,UVMode.World);
                    }
                    // A few broad arcades create an inhabited temple scale, not another window-grid overlay.
                    // Their depth is inset into the original facade; outer edge is at most +.12m.
                    if (row == 0 && height > 5f && (detailed || bay == bays / 2))
                    {
                        if (detailed && bay == bays / 2)
                            Part(batch, style, "trim", "arcade", origin + rotation * new Vector3(x, .4f, -.31f),
                                rotation, new Vector3(Mathf.Min(spacing * .94f, 10f) / 6f,
                                    Mathf.Min(rowHeight - .75f, 15f) / 6f, .78f), true);
                        else MasonryArch(trim, origin + rotation * new Vector3(x,.4f,-.1f),rotation,
                            Mathf.Min(spacing*.84f,10f),Mathf.Min(rowHeight-.75f,15f));
                    }
                    if ((variant == 2 || style == 2) && row > 0 && apertureHeight > 3f)
                    {
                        // Narrow planted loggia recessed into the wall, above pedestrian and camera height.
                        Vector3 ledge = origin + rotation * new Vector3(x, start + .3f, -.31f);
                        trim.Box(0, ledge, new Vector3(windowWidth + .45f, .18f, .80f), rotation, UVMode.World);
                        metal.Box(0, ledge + rotation * new Vector3(0f, .62f, .34f),
                            new Vector3(windowWidth + .4f, .08f, .07f), rotation, UVMode.World);
                        for (int rail = 0; rail < 4; rail++)
                            metal.Box(0, ledge + rotation * new Vector3((rail / 3f - .5f) * windowWidth, .32f, .34f),
                                new Vector3(.07f, .6f, .07f), rotation, UVMode.World);
                        if (detailed && VisualRebirth04ASettings.Gardens)
                            Part(batch, style, "leaf", "vine", ledge + rotation * new Vector3(0f, .45f, .2f),
                                rotation, Vector3.one * .50f, false);
                    }
                    if (tier == 0 && row == 0 && bay == bays / 2 && building.GroundShops && height > 5f)
                        Threshold(batch, style, origin + rotation * new Vector3(x, 0f, 0f), rotation, variant, detailed);
                }
                if (row > 0)
                    roof.Box(0, origin + rotation * new Vector3(0f, row * rowHeight - .12f, -.06f),
                        new Vector3(width - .5f, .25f, .30f), rotation, UVMode.World);
            }
            // Layered wall shoulders break the vertical slab into occupied floors without multiplying windows.
            int shoulders=Mathf.Clamp(Mathf.FloorToInt(height/9f),1,4);
            for(int shoulder=1;shoulder<=shoulders;shoulder++)
            {
                float y=height*shoulder/(shoulders+1f);
                trim.Box(0,origin+rotation*new Vector3(0f,y,-.07f),new Vector3(width-.36f,.24f,.36f),rotation,UVMode.World);
                roof.Box(0,origin+rotation*new Vector3(0f,y-.22f,-.11f),new Vector3(width-.56f,.14f,.30f),rotation,UVMode.World);
            }
            // Broad load-bearing ribs turn the box edges into mineral buttresses. Upper shoulders are
            // deliberately asymmetric per seed, and all mass remains inside the old building envelope.
            for (int pier = 0; pier <= bays; pier++)
            {
                float x = Mathf.Clamp(pier * spacing - width * .5f, -width * .5f + 1.15f, width * .5f - 1.15f);
                Vector3 at = origin + rotation * new Vector3(x, .45f, -.35f);
                float pierWidth = Mathf.Min(2.2f, spacing * .24f);
                trim.Box(0, at + Vector3.up * (height - .7f) * .5f,
                    new Vector3(pierWidth * .70f, height - .7f, .64f), rotation, UVMode.World);
                trim.Box(0, at + Vector3.up * .5f, new Vector3(pierWidth, 1f, .90f), rotation, UVMode.World);
                float shoulder = height * (variant == 3 ? .67f : .79f);
                roof.Box(0, at + Vector3.up * shoulder,
                    new Vector3(pierWidth * .85f, Mathf.Min(1.2f, height * .13f), .85f), rotation, UVMode.World);
                if (detailed && side % 2 == 0 && (pier == 0 || pier == bays) && height > 5f)
                    Part(batch, style, "stone", "column", at, rotation,
                        new Vector3(pierWidth * .7f, (height - .6f) / 6f, .65f), true);
            }
        }

        private static void MasonryArch(MeshBuilder mesh, Vector3 at, Quaternion rotation, float width, float height)
        {
            // The city budget uses seven extruded voussoir segments. Hero LODs use the authored arcade.
            Vector2[] path = { new Vector2(-.5f,0f),new Vector2(-.5f,.58f),new Vector2(-.36f,.81f),
                new Vector2(0f,1f),new Vector2(.36f,.81f),new Vector2(.5f,.58f),new Vector2(.5f,0f) };
            float thickness = Mathf.Min(.95f,width*.13f);
            for (int segment=0;segment<path.Length-1;segment++)
            {
                Vector3 a=at+rotation*new Vector3(path[segment].x*width,path[segment].y*height,0f);
                Vector3 b=at+rotation*new Vector3(path[segment+1].x*width,path[segment+1].y*height,0f);
                Vector3 delta=b-a;
                // All segments lie in the facade plane. The third dimension remains its outward normal.
                Vector3 normal=rotation*Vector3.forward;
                Vector3 across=Vector3.Cross(delta.normalized,normal).normalized;
                Vector3 halfAcross=across*(thickness*.5f),halfDepth=normal*.20f;
                Vector3 a0=a-halfAcross-halfDepth,a1=a+halfAcross-halfDepth;
                Vector3 b0=b-halfAcross-halfDepth,b1=b+halfAcross-halfDepth;
                Vector3 a2=a-halfAcross+halfDepth,a3=a+halfAcross+halfDepth;
                Vector3 b2=b-halfAcross+halfDepth,b3=b+halfAcross+halfDepth;
                mesh.Quad(0,a2,a3,b3,b2,normal,Vector2.zero,Vector2.right,Vector2.one,Vector2.up);
                mesh.Quad(0,a1,a0,b0,b1,-normal,Vector2.zero,Vector2.right,Vector2.one,Vector2.up);
                mesh.Quad(0,a3,a1,b1,b3,across,Vector2.zero,Vector2.right,Vector2.one,Vector2.up);
                mesh.Quad(0,a0,a2,b2,b0,-across,Vector2.zero,Vector2.right,Vector2.one,Vector2.up);
            }
        }

        private static void Threshold(Func<Material, MeshBuilder> batch, int style, Vector3 at,
            Quaternion rotation, int variant, bool detailed)
        {
            MeshBuilder metal = batch(Surface(style, "metal"));
            MeshBuilder dark = batch(Surface(style, "roof"));
            // An explicit door inset remains flush with the old wall. Existing POI triggers are outside it.
            dark.Box(0, at + rotation * new Vector3(0f, 1.6f, -.06f), new Vector3(1.85f, 3f, .18f), rotation, UVMode.World);
            metal.Box(0, at + rotation * new Vector3(0f, 1.6f, .045f), new Vector3(.07f, 3f, .07f), rotation, UVMode.World);
            metal.Box(0, at + rotation * new Vector3(0f, 3.18f, -.02f), new Vector3(2.2f, .16f, .25f), rotation, UVMode.World);
            if (detailed)
            {
                // Recessed fabric and lamp fixtures keep all roads and sidewalks at their original clearance.
                Part(batch, style, "cloth", "banner", at + rotation * new Vector3(2f, 3.5f, -.06f),
                    rotation, new Vector3(.55f, .7f, .1f), false);
                Part(batch, style, "metal", "lantern", at + rotation * new Vector3(-1.5f, 3.5f, -.10f),
                    rotation, new Vector3(.25f,.4f,.15f), false);
            }
        }

        private static void Window(Func<Material, MeshBuilder> batch, int style, Vector3 at,
            Quaternion rotation, float width, float height, bool split)
        {
            MeshBuilder pane = batch(Surface(style, "glass"));
            MeshBuilder border = batch(Surface(style, "metal"));
            // A thick, seven-segment carved reveal surrounds a deep stained-glass recess.
            Vector2[] outline = {
                new Vector2(-.5f,0f), new Vector2(.5f,0f), new Vector2(.5f,.63f),
                new Vector2(.33f,.84f), new Vector2(0f,1f), new Vector2(-.33f,.84f), new Vector2(-.5f,.63f) };
            Vector3 center = at + rotation * new Vector3(0f, height * .43f, .002f);
            Vector3 normal = rotation * Vector3.forward;
            float inset = Mathf.Min(.17f, width * .1f);
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % outline.Length];
                Vector3 innerA = at + rotation * new Vector3(a.x * (width - inset * 2f), inset + a.y * (height - inset * 2f), 0f);
                Vector3 innerB = at + rotation * new Vector3(b.x * (width - inset * 2f), inset + b.y * (height - inset * 2f), 0f);
                Vector3 outerA = at + rotation * new Vector3(a.x * width, a.y * height, .22f);
                Vector3 outerB = at + rotation * new Vector3(b.x * width, b.y * height, .22f);
                pane.Triangle(0, center, innerA, innerB, normal, new Vector2(.5f,.43f),
                    new Vector2(a.x + .5f,a.y), new Vector2(b.x + .5f,b.y));
                Vector3 bevel = Vector3.Cross(outerB - outerA, innerB - outerA).normalized;
                border.Quad(0, outerA, outerB, innerB, innerA, bevel,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            // Distinct leadwork with a restrained emission texture, rather than glowing wall panels.
            border.Box(0, at + rotation * new Vector3(0f, height * .35f, .025f),
                new Vector3(split ? .14f : .07f, height * .65f, .06f), rotation, UVMode.World);
            border.Box(0, at + rotation * new Vector3(0f, height * .40f, .025f),
                new Vector3(width * .86f, .075f, .06f), rotation, UVMode.World);
        }

        private static void RoofWings(Func<Material,MeshBuilder> batch,int style,RectF roof,float top,
            float radius,float height,int variant,bool detailed)
        {
            // Broad inhabited-looking pavilion shoulders use only perimeter space. A continuous central
            // four-metre landing corridor is retained. These roofs remain explicitly decorative.
            float wingWidth=Mathf.Min(radius*1.25f,7.5f);
            float wingLength=Mathf.Max(4f,roof.Depth-radius*2.2f);
            float wallHeight=Mathf.Clamp(height*.38f,1.6f,4.2f);
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 foot=roof.Center(top)+new Vector3(side*(roof.Width*.5f-wingWidth*.5f-.25f),0f,0f);
                float rise=wallHeight*(variant==4 && side<0?.65f:1f);
                MeshBuilder stone=batch(Surface(style,"stone"));
                MeshBuilder trim=batch(Surface(style,"trim"));
                stone.Box(0,foot+Vector3.up*(rise*.5f),new Vector3(wingWidth,rise,wingLength),Quaternion.identity,UVMode.World);
                trim.Box(0,foot+Vector3.up*(rise-.18f),new Vector3(wingWidth+.12f,.28f,wingLength+.12f),Quaternion.identity,UVMode.World);
                Part(batch,style,"roof","roof",foot+Vector3.up*rise,Quaternion.identity,
                    new Vector3(wingWidth/7.2f,Mathf.Clamp(height*.30f,1.7f,3.6f)/4.3f,wingLength/7.2f),detailed);
                if (style==2 && VisualRebirth04ASettings.Gardens)
                    Part(batch,style,"leaf","vine",foot+new Vector3(-side*wingWidth*.49f,rise*.7f,0f),
                        Quaternion.Euler(0f,side*90f,0f),Vector3.one * .65f,false);
            }
        }

        public static void Crown(Building building, RectF roof, float top, int style, bool detailed,
            Func<Material, MeshBuilder> batch)
        {
            float radius = VisualRebirth04ACityPlan.PerimeterRadius(building, roof);
            if (radius <= 0f) return;
            int variant = VisualRebirth04ACityPlan.Variant(building.Seed, building.Style);
            float height = VisualRebirth04ACityPlan.CrownHeight(building, radius, style);
            // Two opposing perimeter pavilions leave a continuous central landing and roof equipment route.
            // These are decorative silhouettes, never platforms or implicit new collision.
            int count = detailed && roof.Width > 22f && roof.Depth > 22f ? 4 : 2;
            if (roof.Width >= 18f && roof.Depth >= 18f)
                RoofWings(batch,style,roof,top,radius,height,variant,detailed);
            for (int corner = 0; corner < count; corner++)
            {
                float x = (corner == 0 || corner == 3 ? -1f : 1f) * (roof.Width * .5f - radius - .25f);
                float z = (corner == 0 || corner == 2 ? -1f : 1f) * (roof.Depth * .5f - radius - .25f);
                if (count == 2)
                {
                    if (variant == 1) z = -roof.Depth * .5f + radius + .25f;
                    else if (variant == 2) z = 0f;
                    else if (variant == 3) z = roof.Depth * .5f - radius - .25f;
                }
                float localHeight = Mathf.Min(12f, height * (variant == 4 ? (corner % 2 == 0 ? 1.35f : .67f) :
                    (variant == 1 ? .78f : 1f)));
                Vector3 foot = roof.Center(top) + new Vector3(x,0f,z);
                float rotationAngle = variant * 18f;
                Quaternion rotation = Quaternion.Euler(0f, rotationAngle, 0f);
                MeshBuilder stone = batch(Surface(style,"stone"));
                float drumHeight=Mathf.Clamp(localHeight*.48f,1.5f,5.8f);
                stone.Cylinder(0, foot, radius, radius * .88f, drumHeight,
                    8, rotation, true, UVMode.World);
                batch(Surface(style,"trim")).Cylinder(0,foot+Vector3.up*(drumHeight-.25f),
                    radius*.91f,radius*.91f,.35f,8,rotation,true,UVMode.World);
                string model = variant == 1 ? "roof" : (variant == 2 ? "tower" : "spire");
                if (style == 1 && variant != 3) model = corner % 2 == 0 || variant == 1 ? "roof" : "tower";
                if (style == 2) model = variant == 0 || variant == 2 ? "tower" : "roof";
                if (variant == 4 && corner % 2 == 1) model = "roof";
                float sourceWidth = model == "roof" ? 7f : (model == "tower" ? 6f : 4f);
                float sourceHeight = model == "roof" ? 4f : (model == "tower" ? 12f : 14f);
                // A rotated square roof uses its diagonal envelope, keeping even its eaves inside the parcel.
                float horizontal = radius * 1.35f / sourceWidth;
                Part(batch, style, style == 2 ? "stone" : "roof", model,
                    foot + Vector3.up * drumHeight, rotation,
                    new Vector3(horizontal, localHeight * .78f / sourceHeight, horizontal), detailed);
                if (style == 0 || style == 3)
                    Part(batch, style, "crystal", "crystal_cluster", foot + Vector3.up * (drumHeight + localHeight * .68f),
                        rotation, new Vector3(radius * .13f, localHeight * .07f, radius * .13f), false);
            }
            if (detailed && VisualRebirth04ASettings.Gardens && style == 2 && roof.Width > 18f && roof.Depth > 18f)
            {
                // One small rooftop canopy along the far edge, never across the centre landing strip.
                Vector3 tree = roof.Center(top) + new Vector3(0f,0f,roof.Depth * .5f - 2.2f);
                Part(batch, style, "bark", "willow_trunk", tree, Quaternion.identity, new Vector3(.28f,.42f,.28f), false);
                Part(batch, style, "leaf", "willow_canopy", tree, Quaternion.identity, new Vector3(.28f,.42f,.28f), false);
            }
        }
    }
}
