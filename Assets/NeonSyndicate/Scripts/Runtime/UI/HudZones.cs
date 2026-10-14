using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Zone and Veil HUD: the banner that greets the player crossing into the Frostline or out of the city into the Quartz
    /// Badlands, the VEIL tag beside the crystal energy bar, the ring of crystal dots that fills while the Third Eye is held
    /// toward a Veil shift, and the violet edge of the screen inside the Inner World.
    /// </summary>
    public sealed partial class Hud
    {
        /// <summary>Seconds before the same zone banner can show again (no repeats while driving along a border).</summary>
        private const float ZoneBannerCooldown = 45f;
        /// <summary>A jump bigger than this between two frames is a teleport, a respawn or a load: no banner then.</summary>
        private const float ZoneJump = 60f;
        private const int VeilRingDots = 24;
        private const float VeilRingRadius = 40f;
        private static readonly Color VeilViolet = new Color(0.72f, 0.38f, 1f);

        private float _frostBannerAt = -999f;
        private float _badlandsBannerAt = -999f;
        private Vector3 _zonePos;
        private bool _zoneSeen;
        /// <summary>The VEIL tag's text, rebuilt only when its seconds change (-1: no seconds shown).</summary>
        private string _veilTag = "VEIL";
        private int _veilTagSeconds = -1;

        /// <summary>
        /// Called every playing frame before the district popup updates (so _lastDistrict is still the previous district):
        /// shows the Frostline banner when the player walks or drives into it from anywhere, and the Quartz Badlands banner
        /// when they leave the city for the open desert. Never on the first district after a load and never over another banner.
        /// </summary>
        private void WatchZones(District d)
        {
            Vector3 pos = Game.PlayerPosition;
            bool jumped = !_zoneSeen || MathUtil.FlatSqrDistance(pos, _zonePos) > ZoneJump * ZoneJump;
            _zonePos = pos;
            _zoneSeen = true;
            District from = _lastDistrict;
            if (d == from || jumped || (int)from < 0) return;
            if (_bannerTime > 0.5f) return;
            float now = Time.unscaledTime;
            if (d == District.Frostline)
            {
                if (now - _frostBannerAt < ZoneBannerCooldown) return;
                _frostBannerAt = now;
                Banner("THE FROSTLINE", "Frozen wilds past the northwest ridge. Ice on the roads and no one out here to help.\n<color=#ff7a66>Danger: high</color>", new Color(0.62f, 0.88f, 1f), 5f);
            }
            else if (d == District.QuartzBadlands && !CityLayout.IsWilderness(from))
            {
                if (now - _badlandsBannerAt < ZoneBannerCooldown) return;
                _badlandsBannerAt = now;
                Banner("QUARTZ BADLANDS", "Open desert past the city limits. Few patrols and fewer witnesses.\n<color=#ffc060>Danger: moderate</color>", new Color(1f, 0.76f, 0.4f), 5f);
            }
        }

        /// <summary>A soft violet edge while inside the Veil and a bright violet flash at each shift.</summary>
        private void DrawVeilVignette(float W, float H)
        {
            InnerWorld iw = Game.Inner;
            if (iw == null) return;
            float a = Mathf.Max(iw.Pulse * 0.85f, iw.Blend * 0.22f);
            if (a > 0.01f) UIKit.Tex(new Rect(0f, 0f, W, H), UIKit.Vignette, new Color(0.62f, 0.16f, 1f, a));
        }

        /// <summary>
        /// The VEIL tag beside the crystal energy bar, with the seconds of energy the Veil has left to feed on at the real
        /// drain (Veil skills and perks, plus the Third Eye's bullet time while it is open).
        /// </summary>
        private void DrawVeilTag(float x, float y, Abilities ab)
        {
            InnerWorld iw = Game.Inner;
            if (iw == null || !iw.IsInner || ab == null) return;
            float drain = iw.DrainPaused ? 0f : iw.DrainNow(ab);
            int seconds = drain > 0.001f ? Mathf.CeilToInt(ab.Energy / drain) : -1;
            if (seconds != _veilTagSeconds)
            {
                _veilTagSeconds = seconds;
                _veilTag = seconds >= 0 ? "VEIL  " + seconds + "s" : "VEIL";
            }
            float glow = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f);
            UIKit.Text(new Rect(x, y - 6f, 160f, 22f), _veilTag, 17, new Color(VeilViolet.r, VeilViolet.g, VeilViolet.b, glow), TextAnchor.UpperLeft, FontStyle.Bold);
        }

        /// <summary>
        /// A ring of crystal dots around the screen centre that fills while the Third Eye is held toward a Veil shift
        /// (Abilities.VeilHold). It turns red when there is not enough crystal energy to open the Veil.
        /// </summary>
        private void DrawVeilHold(float W, float H)
        {
            Abilities ab = Game.Abilities;
            if (ab == null || ab.VeilHold <= 0f) return;
            bool inside = Game.Inner != null && Game.Inner.IsInner;
            float cost = InnerWorld.ShiftCost;
            bool canPay = inside || ab.Energy >= cost;
            Color on = canPay ? VeilViolet : new Color(1f, 0.35f, 0.3f);
            Vector2 c = new Vector2(W * 0.5f, H * 0.5f);
            // The ring texture's band sits at 85% of its half size: scale it so the band runs under the dots.
            float half = VeilRingRadius / 0.85f;
            UIKit.Tex(new Rect(c.x - half, c.y - half, half * 2f, half * 2f), UIKit.Ring, new Color(0f, 0f, 0f, 0.4f));
            float fill = ab.VeilHold * VeilRingDots;
            for (int i = 0; i < VeilRingDots; i++)
            {
                float ang = (i + 0.5f) / VeilRingDots * Mathf.PI * 2f;
                Vector2 p = c + new Vector2(Mathf.Sin(ang), -Mathf.Cos(ang)) * VeilRingRadius;
                float lit = Mathf.Clamp01(fill - i);
                float s = 5f + lit * 3f;
                Color col = lit > 0f ? new Color(on.r, on.g, on.b, 0.35f + 0.65f * lit) : new Color(1f, 1f, 1f, 0.2f);
                UIKit.Tex(new Rect(p.x - s * 0.5f, p.y - s * 0.5f, s, s), UIKit.Circle, col);
            }
            string label = inside ? "Leaving the Veil" : (canPay ? "Entering the Veil" : "Need " + Mathf.CeilToInt(cost) + " crystal energy");
            UIKit.Text(new Rect(c.x - 200f, c.y + VeilRingRadius + 14f, 400f, 24f), label, 17, new Color(on.r, on.g, on.b, 0.9f), TextAnchor.UpperCenter, FontStyle.Bold);
        }
    }
}
