using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>City-wide 04A coverage with explicit fallback reporting and shared chunk geometry.</summary>
    public sealed partial class CityBuilder
    {
        private sealed class Coverage04A
        {
            public int Seen;
            public int Eligible;
            public int Detailed;
            public int Ordinary;
            public int Fallback;
            public int Excluded;
            public int HelipadRoofs;
            public int Variants;
        }

        private readonly Dictionary<string, Coverage04A> _coverage04A = new Dictionary<string, Coverage04A>();
        private readonly Dictionary<string, int> _exclusions04A = new Dictionary<string, int>();
        private bool _assetsChecked04A;
        private bool _assetsReady04A;
        private int _trees04A;
        private int _failures04A;

        private void ResetCoverage04A()
        {
            _coverage04A.Clear();
            _exclusions04A.Clear();
            _assetsChecked04A = _assetsReady04A = false;
            _trees04A = _failures04A = 0;
        }

        private bool PrepareBuilding04A(Building building, Lot lot)
        {
            if (!VisualRebirth04ASettings.Enabled || !VisualRebirth04ASettings.City ||
                !VisualRebirth04ACityPlan.CanTheme(building, lot)) return false;
            if (_assetsChecked04A) return _assetsReady04A;
            _assetsChecked04A = true;
            try { _assetsReady04A = VisualRebirth04AArchitecture.Ready(); }
            catch (Exception error)
            {
                _failures04A++;
                Log.Exception("04A architecture preparation, 02E fallback retained", error);
            }
            if (!_assetsReady04A) Log.Warn("04A architecture kit unavailable. Existing 02D and 02E city art remains active.");
            return _assetsReady04A;
        }

        private void BuildOrdinaryTier04A(Building building, RectF footprint, float bottom, float top, int tier)
        {
            Vector3 at = footprint.Center(0f);
            int style = VisualRebirth04ACityPlan.StyleAt(at.x, at.z);
            VisualRebirth04AArchitecture.Tier(building, footprint, bottom, top, tier, style, false,
                delegate(Material material) { return Get(at, material, 2f); });
        }

        private void BuildOrdinaryRoof04A(Building building, RectF roof, float top)
        {
            Vector3 at = roof.Center(0f);
            int style = VisualRebirth04ACityPlan.StyleAt(at.x, at.z);
            VisualRebirth04AArchitecture.Crown(building, roof, top, style, false,
                delegate(Material material) { return Get(at, material, 2f); });
        }

        private void RecordBuilding04A(Building building, Lot lot, bool applied, bool detailed)
        {
            Vector3 at = building == null ? Vector3.zero : building.Footprint.Center(0f);
            string key = CityLayout.DistrictAt(at) + "/" + (building == null ? "Missing" : building.Style.ToString());
            Coverage04A record;
            if (!_coverage04A.TryGetValue(key, out record))
            {
                record = new Coverage04A();
                _coverage04A.Add(key, record);
            }
            record.Seen++;
            string exclusion = VisualRebirth04ACityPlan.Exclusion(building, lot);
            if (exclusion.Length > 0)
            {
                record.Excluded++;
                int count;
                _exclusions04A.TryGetValue(exclusion, out count);
                _exclusions04A[exclusion] = count + 1;
                return;
            }
            record.Eligible++;
            if (building.Roof == RoofType.Helipad) record.HelipadRoofs++;
            if (applied)
            {
                if (detailed) record.Detailed++;
                else record.Ordinary++;
                record.Variants |= 1 << VisualRebirth04ACityPlan.Variant(building.Seed, building.Style);
            }
            else record.Fallback++;
        }

        private void ReportCoverage04A()
        {
            int seen = 0, eligible = 0, applied = 0, fallback = 0;
            List<string> keys = new List<string>(_coverage04A.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (string key in keys)
            {
                Coverage04A record = _coverage04A[key];
                seen += record.Seen;
                eligible += record.Eligible;
                applied += record.Detailed + record.Ordinary;
                fallback += record.Fallback;
                // Stable one-line JSON is available in the Unity player log for automated Windows QA.
                // Enum names and fixed labels are the only string inputs; no user data is serialized.
                Log.Info("VISUAL04A_COVERAGE {\"group\":\"" + key + "\",\"seen\":" + record.Seen +
                    ",\"eligible\":" + record.Eligible + ",\"detailed\":" + record.Detailed +
                    ",\"ordinary\":" + record.Ordinary + ",\"fallback\":" + record.Fallback +
                    ",\"excluded\":" + record.Excluded + ",\"protectedHelipadRoofs\":" + record.HelipadRoofs +
                    ",\"variantMask\":" + record.Variants + "}");
            }
            StringBuilder exclusions = new StringBuilder();
            foreach (KeyValuePair<string,int> pair in _exclusions04A)
                exclusions.Append(pair.Key).Append('=').Append(pair.Value).Append(' ');
            Log.Info("Visual Rebirth 04A | buildings " + seen + ", eligible " + eligible + ", applied " + applied +
                ", legacy fallback " + fallback + ", authored trees " + _trees04A + ", art errors " + _failures04A +
                ". Exclusions: " + exclusions + ". Enabled=" + VisualRebirth04ASettings.Enabled +
                ", City=" + VisualRebirth04ASettings.City + ". Rooftop ornament excludes helipads; their facades remain eligible.");
            if (eligible != applied + fallback) Log.Warn("04A coverage accounting mismatch. Retain the log for review.");
        }

        private Material Surface04A(Vector3 position, string role)
        {
            if (!VisualRebirth04ASettings.Enabled || !VisualRebirth04ASettings.City || !VisualRebirth04AArchitecture.Ready()) return null;
            if (role == "wall") role = "stone";
            else if (role == "pane") role = "glass";
            else if (role != "roof" && role != "trim" && role != "paving" && role != "crystal" &&
                role != "bark" && role != "leaf") return null;
            return VisualRebirth04AAssets.Material(VisualRebirth04ACityPlan.StyleAt(position.x,position.z),role);
        }

        private bool StreetTree04A(Vector3 p, int seed, float scale)
        {
            if (!VisualRebirth04ASettings.Enabled || !VisualRebirth04ASettings.Gardens ||
                !CrystalCity02ECoverage.IsCityProp(p)) return false;
            try
            {
                int style = VisualRebirth04ACityPlan.StyleAt(p.x, p.z);
                string model = style == 1 || (seed & 3) == 0 ? "cypress_canopy" : "willow_canopy";
                if (VisualRebirth04AAssets.Mesh(model, true) == null) return false;
                Material bark = VisualRebirth04AAssets.Material(style,"bark");
                Material leaf = VisualRebirth04AAssets.Material(style,"leaf");
                if (bark == null || leaf == null) return false;
                Quaternion rotation = Quaternion.Euler(0f, seed % 360, 0f);
                // Preserve the exact old trunk dimensions and capsule. Leaf geometry starts overhead;
                // every tree shares palette materials and existing city chunks, without new GameObjects.
                MeshBuilder trunk = Get(p,bark,1f);
                trunk.Cylinder(0,p,.20f*scale,.13f*scale,3.2f*scale,7,Quaternion.identity,false,UVMode.World);
                for (int branch = 0; branch < 3; branch++)
                {
                    Vector3 a = p + Vector3.up * (2.75f + .12f * branch) * scale;
                    Vector3 b = p + rotation * new Vector3(Mathf.Cos(branch*2.0944f)*.9f,4.25f,Mathf.Sin(branch*2.0944f)*.9f) * scale;
                    Vector3 delta = b-a;
                    trunk.Cylinder(0,a,.085f*scale,.025f*scale,delta.magnitude,5,
                        Quaternion.FromToRotation(Vector3.up,delta.normalized),false,UVMode.World);
                }
                Vector3 size = model == "cypress_canopy" ? new Vector3(.80f,.40f,.80f) : new Vector3(.40f,.48f,.40f);
                VisualRebirth04AAssets.Append(Get(p,leaf,1f),model,p + Vector3.up*1.3f*scale,rotation,size*scale,true);
                _trees04A++;
                return true;
            }
            catch (Exception error)
            {
                _failures04A++;
                if (_failures04A == 1) Log.Exception("04A tree art, previous foliage retained",error);
                return false;
            }
        }
    }
}
