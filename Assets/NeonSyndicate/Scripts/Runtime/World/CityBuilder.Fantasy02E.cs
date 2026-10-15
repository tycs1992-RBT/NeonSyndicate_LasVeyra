using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// City-wide visual coverage for buildings outside the expensive landmark selection.
    /// All ornament is appended to existing 160m material chunks; physics and layout are not generated here.
    /// </summary>
    public sealed partial class CityBuilder
    {
        private int _fantasyEligible02E;
        private int _fantasyDetailed02E;
        private int _fantasyLight02E;
        private int _fantasyFallback02E;
        private int _fantasyProtected02E;
        private int _fantasyTrees02E;
        private int _fantasyLamps02E;
        private int _fantasyFurniture02E;
        private int _fantasyPalmCrowns02E;
        private int _fantasyFailures02E;

        private void ResetCoverage02E()
        {
            _fantasyEligible02E = _fantasyDetailed02E = _fantasyLight02E = _fantasyFallback02E = 0;
            _fantasyProtected02E = _fantasyTrees02E = _fantasyLamps02E = _fantasyFurniture02E = 0;
            _fantasyPalmCrowns02E = _fantasyFailures02E = 0;
        }

        private void ReportCoverage02E()
        {
            Log.Info("Crystal City 02E | Eligible buildings " + _fantasyEligible02E +
                ", detailed " + _fantasyDetailed02E + ", lightweight " + _fantasyLight02E +
                ", original fallback " + _fantasyFallback02E + ", protected/invalid " + _fantasyProtected02E +
                ". Street trees " + _fantasyTrees02E + ", lamps " + _fantasyLamps02E +
                ", furniture " + _fantasyFurniture02E + ", palm crowns " + _fantasyPalmCrowns02E +
                ", art exceptions " + _fantasyFailures02E + ".");
            if (_fantasyEligible02E != _fantasyDetailed02E + _fantasyLight02E + _fantasyFallback02E)
                Log.Warn("02E coverage accounting mismatch. Please retain this Console message for the art audit.");
        }

        private bool PrepareBuilding02E(Building b, Lot lot)
        {
            if (!CrystalCity02ECoverage.CanTheme(b, lot)) { _fantasyProtected02E++; return false; }
            _fantasyEligible02E++;
            try
            {
                Vector3 at = b.Footprint.Center(0f);
                // Resolve resources before any default facade is suppressed.
                return CrystalCity02EAssets.Surface(at, "wall") != null &&
                    CrystalCity02EAssets.Surface(at, "pane") != null &&
                    CrystalCity02EAssets.Surface(at, "trim") != null &&
                    CrystalCity02EAssets.Surface(at, "roof") != null;
            }
            catch (Exception error)
            {
                _fantasyFailures02E++;
                Log.Exception("02E material preparation (original facade retained)", error);
                return false;
            }
        }

        private Material Surface02E(Vector3 position, string role, Material original)
        {
            if (!CrystalCity02ECoverage.IsCityProp(position)) return original;
            try
            {
                Material result = CrystalCity02EAssets.Surface(position, role) ?? original;
                if (role == "paving") _noShadow.Add(result);
                return result;
            }
            catch (Exception error)
            {
                _fantasyFailures02E++;
                if (_fantasyFailures02E == 1) Log.Exception("02E optional structure material (original retained)", error);
                return original;
            }
        }

        private void Stamp02E(string model, Material material, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            CrystalCity02EAssets.Append(Get(position, material, 1f), model, position, rotation, scale);
        }

        private void BuildOrdinaryTier02E(Building b, RectF f, float bottom, float top, int tier)
        {
            if (top <= bottom) return;
            Vector3 center = f.Center(0f);
            Material stone = CrystalCity02EAssets.Surface(center, "wall");
            Material roof = CrystalCity02EAssets.Surface(center, "roof");
            Material trim = CrystalCity02EAssets.Surface(center, "trim");
            Material pane = CrystalCity02EAssets.Surface(center, "pane");
            // Original footprint and full-height opaque shell, including shop floor. No facade-sheet apartment texture.
            Get(center, stone, 3f).Box(0, f.Center((bottom + top) * .5f),
                new Vector3(f.Width, top - bottom, f.Depth), Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            Get(center, roof, 3f).FlatRect(0, f.X0, f.Z0, f.X1, f.Z1, top, UVMode.World);
            // A slim projecting cornice changes the profile without replacing the physical tier envelope.
            Get(center, trim, 1f).Box(0, f.Center(top - .11f), new Vector3(f.Width + .12f, .16f, f.Depth + .12f),
                Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            CrystalCityStyle style = CrystalCity02DMaterials.StyleAt(center);
            for (int side = 0; side < 4; side++)
            {
                Quaternion rot = Quaternion.Euler(0f, side * 90f, 0f);
                Vector3 face = side == 0 ? new Vector3(f.CenterX,bottom,f.Z1) :
                    (side == 1 ? new Vector3(f.X1,bottom,f.CenterZ) :
                    (side == 2 ? new Vector3(f.CenterX,bottom,f.Z0) : new Vector3(f.X0,bottom,f.CenterZ)));
                float width = side % 2 == 0 ? f.Width : f.Depth;
                float height = top - bottom;
                int bays = CrystalCity02ECoverage.Bays(width, style);
                int rows = CrystalCity02ECoverage.Registers(height);
                if (bays == 0 || rows == 0) continue;
                float bayWidth = width / bays, rowHeight = height / rows;
                for (int row = 0; row < rows; row++)
                    for (int bay = 0; bay < bays; bay++)
                    {
                        float h = Mathf.Min(rowHeight - 1.4f, style == CrystalCityStyle.VerdantReach ? 20f : 28f);
                        float w = Mathf.Min(bayWidth * .65f, style == CrystalCityStyle.DawnstoneCourt ? 3.4f : 4.7f);
                        if (h <= .9f || w <= .55f) continue;
                        Vector3 at = face + rot * new Vector3((bay + .5f) * bayWidth - width * .5f,
                            row * rowHeight + .85f, .028f);
                        Lancet02E(at, rot, w, h, pane, trim);
                    }
                // Edge buttresses do not stand in the street. Trim only projects 10cm beyond the shell.
                for (int edge = -1; edge <= 1; edge += 2)
                {
                    Vector3 p = face + rot * new Vector3(edge * (width * .5f - .18f), height * .5f, .045f);
                    Get(center, roof, 2f).Box(0, p, new Vector3(.25f,height,.10f),rot,UVMode.World);
                }
                // Housing-scale portico, still at the existing building boundary.
                if (tier == 0 && width > 5f && height > 4f)
                {
                    Vector3 threshold = face + rot * new Vector3(0f,3.35f,.10f);
                    Get(center, trim, 1f).Box(0, threshold, new Vector3(2.2f,.14f,.26f),rot,UVMode.World);
                }
            }
        }

        private void Lancet02E(Vector3 at, Quaternion rot, float width, float height, Material glass, Material border)
        {
            // A seven-sided pointed window with an extruded bevel. No per-window GameObject or texture atlas dependency.
            Vector2[] outline =
            {
                new Vector2(-.5f,0f), new Vector2(.5f,0f), new Vector2(.5f,.57f),
                new Vector2(.32f,.81f), new Vector2(0f,1f), new Vector2(-.32f,.81f), new Vector2(-.5f,.57f)
            };
            MeshBuilder pane = Get(at, glass, 1f), frame = Get(at, border, 1f);
            Vector3 normal = rot * Vector3.forward;
            Vector3 middle = at + rot * new Vector3(0f, height * .42f, .004f);
            float insetX = Mathf.Min(.14f, width * .08f), insetY = Mathf.Min(.18f, height * .08f);
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % outline.Length];
                Vector3 oa = at + rot * new Vector3(a.x * width, a.y * height, CrystalCity02ECoverage.FrameProjection);
                Vector3 ob = at + rot * new Vector3(b.x * width, b.y * height, CrystalCity02ECoverage.FrameProjection);
                Vector3 ia = at + rot * new Vector3(a.x * (width - insetX * 2f), insetY + a.y * (height - insetY * 2f), .004f);
                Vector3 ib = at + rot * new Vector3(b.x * (width - insetX * 2f), insetY + b.y * (height - insetY * 2f), .004f);
                // The outline is counter-clockwise in local XY: winding and normals match the +Z front.
                pane.Triangle(0, middle, ia, ib, normal, new Vector2(.5f,.42f),new Vector2(a.x+.5f,a.y),new Vector2(b.x+.5f,b.y));
                Vector3 bevelNormal = Vector3.Cross(ob - oa, ib - oa).normalized;
                frame.Quad(0, oa, ob, ib, ia, bevelNormal,
                    new Vector2(0f,0f),new Vector2(1f,0f),new Vector2(1f,1f),new Vector2(0f,1f));
            }
        }

        private void BuildOrdinaryRoof02E(Building b, RectF roof, float y)
        {
            if (!CrystalCity02ECoverage.AllowRoofOrnament(b, roof)) return;
            Vector3 center = roof.Center(y + .03f);
            Material stone = CrystalCity02EAssets.Surface(center, "roof");
            Material crystal = CrystalCity02EAssets.Surface(center, "crystal");
            Material trim = CrystalCity02EAssets.Surface(center, "trim");
            float height = Mathf.Clamp(b.Height * .085f, 2f, 6f);
            // Perimeter finials leave the roof centre and existing rooftop equipment intact.
            float dx = roof.Width * .5f - .85f, dz = roof.Depth * .5f - .85f;
            for (int i = 0; i < 2; i++)
            {
                Vector3 foot = center + new Vector3(i == 0 ? -dx : dx, 0f, i == 0 ? -dz : dz);
                Get(foot, stone, 1f).Cylinder(0,foot,.42f,.32f,.50f,6,Quaternion.identity,true,UVMode.World);
                Stamp02E("shard",crystal,foot+Vector3.up*.42f,new Vector3(.85f,height,.85f),Quaternion.Euler(0f,i*115f,0f));
            }
            if (b.Height > 20f)
            {
                // A small crest on the parapet, not a large obstacle above an accessible roof.
                Vector3 front = center + new Vector3(0f,.45f,-dz);
                Stamp02E("orbit",trim,front+Vector3.up*.6f,Vector3.one*.7f,Quaternion.Euler(90f,0f,0f));
            }
        }

        private bool StreetTree02E(Vector3 p, int seed, float s)
        {
            if (!CrystalCity02ECoverage.IsCityProp(p)) return false;
            try
            {
                Material bark = CrystalCity02EAssets.Surface(p,"bark");
                Material leaf = CrystalCity02EAssets.Surface(p,"leaf");
                Quaternion yaw = Quaternion.Euler(0f,seed % 360,0f);
                // Keep the original trunk base/radius/height. Branches and leaves start above pedestrians.
                Get(p,bark,1f).Cylinder(0,p,.2f*s,.13f*s,3.2f*s,6,Quaternion.identity,false);
                for (int i=0;i<3;i++)
                {
                    Vector3 a=p+Vector3.up*(2.65f+.18f*i)*s;
                    Vector3 b=p+yaw*new Vector3(Mathf.Cos(i*2.0944f)*1.3f,4.1f,Mathf.Sin(i*2.0944f)*1.3f)*s;
                    Vector3 delta=b-a;
                    Get(p,bark,1f).Cylinder(0,a,.08f*s,.025f*s,delta.magnitude,5,Quaternion.FromToRotation(Vector3.up,delta.normalized),false);
                }
                Stamp02E("leaves",leaf,p+Vector3.up*.35f*s,Vector3.one*s,yaw);
                _fantasyTrees02E++;
                return true;
            }
            catch(Exception error) { _fantasyFailures02E++; Log.Exception("02E street tree (original canopy retained)",error); return false; }
        }

        private bool StreetLamp02E(PropSpot p, Quaternion rot)
        {
            if (!CrystalCity02ECoverage.IsCityProp(p.Pos)) return false;
            try
            {
                Material stone=CrystalCity02EAssets.Surface(p.Pos,"roof"), trim=CrystalCity02EAssets.Surface(p.Pos,"trim");
                // Stem follows the original thin lamppost collision. The crystal sits at the old lamp-head site.
                Get(p.Pos,stone,1f).Cylinder(0,p.Pos,.12f,.08f,7.4f,6,Quaternion.identity,false);
                Vector3 forward=rot*Vector3.forward;
                Vector3 head=p.Pos+Vector3.up*7.18f+forward*2.15f;
                Get(head,trim,1f).Box(0,p.Pos+Vector3.up*7.3f+forward*1.1f,new Vector3(.10f,.10f,2.3f),rot,UVMode.Unit);
                Stamp02E("lantern",trim,head-Vector3.up*.38f,Vector3.one*.6f,rot);
                Get(head,Mats.LampHead,1f).Cylinder(0,head-Vector3.up*.20f,.15f,.02f,.52f,6,Quaternion.identity,true);
                _fantasyLamps02E++;
                return true;
            }
            catch(Exception error) { _fantasyFailures02E++; Log.Exception("02E street lamp (original retained)",error); return false; }
        }

        private void PalmCrown02E(Vector3 top, int seed, float scale)
        {
            if (!CrystalCity02ECoverage.IsCityProp(top)) return;
            try
            {
                Stamp02E("bloom",CrystalCity02EAssets.Surface(top,"crystal"),top+Vector3.up*.12f,
                    new Vector3(scale*1.3f,scale*.8f,scale*1.3f),Quaternion.Euler(0f,seed%360,0f));
                _fantasyPalmCrowns02E++;
            }
            catch(Exception error) { _fantasyFailures02E++; Log.Exception("02E palm crown (original palm retained)",error); }
        }

        private void PlanterPlant02E(Vector3 p, Quaternion rot)
        {
            Stamp02E("fern",CrystalCity02EAssets.Surface(p,"leaf"),p+Vector3.up*.82f,Vector3.one*.6f,rot);
            Stamp02E("bloom",CrystalCity02EAssets.Surface(p,"crystal"),p+Vector3.up*.88f,Vector3.one*.4f,rot);
            _fantasyFurniture02E++;
        }

        private void FountainCrown02E(Vector3 p, float radius)
        {
            // All extra geometry lies over the existing centre/basin, not its surrounding footpath.
            Material crystal=CrystalCity02EAssets.Surface(p,"crystal"), trim=CrystalCity02EAssets.Surface(p,"trim");
            Stamp02E("orbit",trim,p+Vector3.up*2.05f,Vector3.one*Mathf.Min(.85f,radius*.3f),Quaternion.Euler(72f,0f,0f));
            Stamp02E("shard",crystal,p+Vector3.up*1.55f,new Vector3(.6f,1.0f,.6f),Quaternion.identity);
            _fantasyFurniture02E++;
        }

        private void CanopyCrown02E(Vector3 canopy)
        {
            Material trim=CrystalCity02EAssets.Surface(canopy,"trim"), crystal=CrystalCity02EAssets.Surface(canopy,"crystal");
            Stamp02E("crown",trim,canopy+Vector3.up*.33f,new Vector3(2.1f,1.4f,2.1f),Quaternion.identity);
            Stamp02E("shard",crystal,canopy+Vector3.up*.55f,new Vector3(1.6f,2.3f,1.6f),Quaternion.Euler(0f,30f,0f));
        }
    }
}
