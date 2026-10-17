using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Keeps the dressing tiers in step with the graphics preset after boot: a lower preset hides tiers, a higher one
    /// builds the missing tier from the deterministic plan (spread over frames) and shows it. Attached to the city
    /// root by the city builder; nothing else needs to know it exists.
    /// </summary>
    public sealed class DressingLayers : MonoBehaviour
    {
        private CityBuilder _city;
        private GraphicsPreset _applied = (GraphicsPreset)(-1);
        private bool _building;

        /// <summary>Statics hub for the whole Phase 4 dressing pass (called from Game.ResetStatics).</summary>
        public static void ResetStatics()
        {
            DecalTex.ResetStatics();
            DecalMats.ResetStatics();
            CityBuilder.ResetClutterStatics();
            AmbientVfx.ResetStatics();
        }

        public static DressingLayers Attach(Transform cityRoot, CityBuilder city)
        {
            if (cityRoot == null || city == null) return null;
            DressingLayers d = cityRoot.GetComponent<DressingLayers>();
            if (d == null) d = cityRoot.gameObject.AddComponent<DressingLayers>();
            d._city = city;
            d._applied = Settings.Graphics;
            d.RegisterDebugPage();
            return d;
        }

        private bool _pageRegistered;
        private bool _forceHidden;

        /// <summary>F2 > Dressing: what each tier holds, what is alive, and a hide switch for before and after looks.</summary>
        private void RegisterDebugPage()
        {
            if (_pageRegistered) return;
            _pageRegistered = true;
            DebugPages.Register(new MenuPage { Title = "Dressing", Order = 56, Draw = DrawDebug });
        }

        private void DrawDebug(Rect r)
        {
            float x = r.x, y = r.y;
            Color white = Color.white;
            UIKit.Text(new Rect(x, y, r.width, 28f), "Phase 4 dressing: decals, clutter, roof gear, foliage cards and ambient particles. Preset: " + Settings.Graphics + (_building ? " (building a tier...)" : ""), 18, white, TextAnchor.MiddleLeft);
            y += 34f;
            if (UIKit.Button(new Rect(x, y, 300f, 40f), _forceHidden ? "Dressing hidden (show)" : "Dressing shown (hide)", _forceHidden, 18))
            {
                _forceHidden = !_forceHidden;
                if (_city != null) _city.SetDressingVisible(_forceHidden ? GraphicsPreset.Low : Settings.Graphics);
            }
            y += 50f;
            if (_city != null)
            {
                int planned = 0;
                try { planned = _city.Dressing.Items.Count; }
                catch (System.Exception) { planned = -1; }
                UIKit.Text(new Rect(x, y, r.width, 26f), "Planned items (all tiers): " + planned, 16, white, TextAnchor.MiddleLeft);
                y += 28f;
                foreach (GraphicsPreset tier in new[] { GraphicsPreset.Medium, GraphicsPreset.High, GraphicsPreset.Ultra })
                {
                    int n;
                    _city.DressingCounts.TryGetValue(tier, out n);
                    string state = _city.DressingBuilt(tier) ? (DressingRules.Shows(tier, Settings.Graphics) && !_forceHidden ? "shown" : "built, hidden") : "not built";
                    UIKit.Text(new Rect(x, y, r.width, 26f), "  " + tier + ": " + n + " items, " + state, 16, white, TextAnchor.MiddleLeft);
                    y += 28f;
                }
            }
            y += 10f;
            int spots = AmbientVfx.Spots.Count;
            AmbientVfxRunner runner = AmbientVfx.Ensure();
            UIKit.Text(new Rect(x, y, r.width, 26f), "Ambient particle spots: " + spots + " registered, " + (runner != null ? runner.ActiveCount : 0) + " alive (max " + AmbientVfxRules.MaxActive(Settings.Graphics) + ", cull " + AmbientVfxRules.CullDistance(Settings.Graphics).ToString("0") + " m)", 16, white, TextAnchor.MiddleLeft);
            y += 28f;
            UIKit.Text(new Rect(x, y, r.width, 26f), "Low shows none of this. Medium: the light share. High: the full set. Ultra: the dense set.", 16, new Color(0.8f, 0.8f, 0.85f), TextAnchor.MiddleLeft);
        }

        private void Update()
        {
            if (_city == null || _building) return;
            GraphicsPreset g = Settings.Graphics;
            if (g == _applied) return;
            StartCoroutine(Apply(g));
        }

        private IEnumerator Apply(GraphicsPreset g)
        {
            _building = true;
            try
            {
                for (GraphicsPreset tier = GraphicsPreset.Medium; tier <= g; tier++)
                {
                    if (_city.DressingBuilt(tier)) continue;
                    IEnumerator build = _city.BuildDressingLayer(tier);
                    while (true)
                    {
                        bool moved;
                        try { moved = build.MoveNext(); }
                        catch (System.Exception e)
                        {
                            Log.Exception("Dressing tier " + tier, e);
                            break;
                        }
                        if (!moved) break;
                        yield return build.Current;
                    }
                }
                _city.SetDressingVisible(_forceHidden ? GraphicsPreset.Low : g);
                _applied = g;
            }
            finally
            {
                _building = false;
            }
        }
    }
}
