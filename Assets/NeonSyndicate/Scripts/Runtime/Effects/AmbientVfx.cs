using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One place in the world that wants an ambient particle volume when the viewer is near.</summary>
    public sealed class AmbientVfxSpot
    {
        public AmbientVfxKind Kind;
        /// <summary>World position, or a position local to Anchor when one is set.</summary>
        public Vector3 Local;
        /// <summary>The transform the spot lives under (an Underheart region, a venue); the spot dies with it.</summary>
        public Transform Anchor;
        private bool _anchored;
        /// <summary>Half size of the emission box.</summary>
        public Vector3 Extents = new Vector3(1f, 1f, 1f);
        public Color Tint = Color.white;
        /// <summary>Only between dusk and dawn (street steam).</summary>
        public bool NightOnly;
        /// <summary>Only while the viewer is in the Underheart (set for spots registered there).</summary>
        public bool Underground;
        /// <summary>Multiplies the family's rate (0.5 for a small lamp, 2 for a hall).</summary>
        public float Intensity = 1f;
        public bool Enabled = true;

        public AmbientVfxSpot Anchored(Transform anchor)
        {
            Anchor = anchor;
            _anchored = anchor != null;
            return this;
        }

        /// <summary>False once an anchored spot's transform was destroyed.</summary>
        public bool Alive { get { return !_anchored || Anchor != null; } }

        public Vector3 WorldPosition { get { return _anchored && Anchor != null ? Anchor.TransformPoint(Local) : Local; } }
    }

    /// <summary>
    /// The registry of ambient particle spots (dust motes, heat haze, embers, snow motes, steam, spores). Builders
    /// register spots; the runner (created on first use) keeps the nearest few alive within the preset's budget and
    /// never runs anything on Low. Registration is free: a spot costs nothing until it is chosen.
    /// </summary>
    public static class AmbientVfx
    {
        private static readonly List<AmbientVfxSpot> _spots = new List<AmbientVfxSpot>();
        private static AmbientVfxRunner _runner;
        private static bool _cityRegistered;
        private static CityLayout _cityLayout;

        public static IList<AmbientVfxSpot> Spots { get { return _spots; } }

        public static void ResetStatics()
        {
            _spots.Clear();
            _runner = null;
            _cityRegistered = false;
            _cityLayout = null;
        }

        public static AmbientVfxSpot Register(AmbientVfxKind kind, Vector3 position, Vector3 extents, Color tint, float intensity)
        {
            AmbientVfxSpot s = new AmbientVfxSpot();
            s.Kind = kind;
            s.Local = position;
            s.Extents = extents;
            s.Tint = tint;
            s.Intensity = intensity;
            _spots.Add(s);
            Ensure();
            return s;
        }

        /// <summary>Registers a spot under a transform (position local to it); it is dropped when the transform goes.</summary>
        public static AmbientVfxSpot RegisterUnder(Transform anchor, AmbientVfxKind kind, Vector3 local, Vector3 extents, Color tint, float intensity)
        {
            AmbientVfxSpot s = Register(kind, local, extents, tint, intensity).Anchored(anchor);
            return s;
        }

        /// <summary>Drops a spot.</summary>
        public static void Unregister(AmbientVfxSpot s)
        {
            if (s != null) _spots.Remove(s);
        }

        /// <summary>Removes the spots whose anchor was destroyed.</summary>
        public static void Prune()
        {
            for (int i = _spots.Count - 1; i >= 0; i--) if (!_spots[i].Alive) _spots.RemoveAt(i);
        }

        /// <summary>The runner that drives the spots (one per scene, created on demand).</summary>
        public static AmbientVfxRunner Ensure()
        {
            if (_runner != null) return _runner;
            GameObject go = new GameObject("Ambient VFX");
            _runner = go.AddComponent<AmbientVfxRunner>();
            return _runner;
        }

        /// <summary>
        /// City-side spots once the city is built: night steam at a share of the curb drains in the older districts.
        /// Venue interiors are picked up by the runner when the inhabited city reports operational.
        /// </summary>
        public static void RegisterCity(CityLayout layout)
        {
            if (_cityRegistered || layout == null) return;
            _cityLayout = layout;
            // On Low the plan is never made; the runner calls again once the preset allows the spots.
            if (!DressingRules.Enabled(Settings.Graphics)) { Ensure(); return; }
            _cityRegistered = true;
            CityBuilder city = Game.City;
            if (city == null) return;
            int n = 0;
            List<DressItem> items = city.Dressing.Items;
            for (int i = 0; i < items.Count; i++)
            {
                DressItem it = items[i];
                if (it.Kind != DressKind.DrainStain) continue;
                District d = CityLayout.DistrictAt(it.Pos);
                bool steamy = d == District.LasVeyraCore || d == District.OldSilver || d == District.SunkenRow || d == District.AshUnderpass || d == District.BrassProjects;
                if (!steamy || (it.Seed % 3) != 0) continue;
                AmbientVfxSpot s = Register(AmbientVfxKind.Steam, it.Pos + Vector3.up * 0.05f, new Vector3(0.25f, 0.05f, 0.25f), new Color(0.85f, 0.88f, 0.95f, 0.22f), 1f);
                s.NightOnly = true;
                n++;
            }
            Ensure();
            Log.Info("AMBIENT_VFX_P4 city spots: " + n + " drain steam vents");
        }

        /// <summary>Registers the deferred city spots once the preset allows them (called by the runner).</summary>
        public static void RegisterCityIfReady()
        {
            if (_cityRegistered || _cityLayout == null) return;
            RegisterCity(_cityLayout);
        }
    }
}
