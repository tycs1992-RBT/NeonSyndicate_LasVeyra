using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Authored actions for legged creatures as offsets layered over the locomotion: every action has a wind-up that
    /// reads as a silhouette from across a fight (coiling back, a paw raised, a head drawn up), a fast strike and a clear
    /// recovery, timed by input.Windup and input.Strike. Also the alert posture, the flinch from a hit, the stagger and the
    /// death collapse.
    /// </summary>
    public sealed partial class CreaturePose
    {
        /// <summary>Offsets for one frame (degrees and metres; legs carry their own overrides).</summary>
        private struct Overlay
        {
            public Vector3 Root;
            public float Pitch, Roll, Yaw, Bend, Arch, Crouch, CrouchRear, CrouchFront, ChestPitch;
            public float NeckPitch, NeckYaw, HeadPitch, HeadYaw, HeadRoll, LookW, Free, Snap;
            public float Jaw, EarsBack, TailYaw, TailPitch, TailFree, TailSnap, ToeCurl, Dead;
            /// <summary>Extra pitch for the first tail bone only (the tail's carriage without curling it).</summary>
            public float TailBase;
            /// <summary>Flight: wings forced open (0..1), extra wing beat strength.</summary>
            public float WingSpread, WingBeat;

            public void Reset()
            {
                this = new Overlay();
                LookW = 1f;
            }
        }

        private void ClearLegOverrides(float dt)
        {
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                l.OverW = Mathf.MoveTowards(l.OverW, 0f, dt * 6f);
                l.OverFlex = Mathf.Lerp(l.OverFlex, 0f, QMath.Damp(10f, dt));
            }
        }

        /// <summary>Drives a leg override toward weight w and model-space target.</summary>
        private void OverrideLeg(Leg l, float w, Vector3 target, float flex)
        {
            float L = Mathf.Max(0.05f, _legLen);
            // Letting go of a paw that is already down at ground level: it touches down right where it is (the gait or
            // a settling step moves it later), instead of being dragged back across the ground to its old stance.
            if (w < l.OverW - 1e-4f && target.y - l.Neutral.y < 0.08f * L && l.OverW > 0f)
            {
                l.OverW = 0f;
                l.Over = target;
                return;
            }
            if (w <= l.OverW) l.OverW = Mathf.Max(w, l.OverW - 0.2f);
            else l.OverW = w;
            l.Over = target;
            l.OverFlex = flex;
        }

        private Leg LegOf(bool front, int side)
        {
            for (int i = 0; i < _legRig.Length; i++)
                if (_legRig[i].Front == front && _legRig[i].Side == side && !_legRig[i].Wing) return _legRig[i];
            for (int i = 0; i < _legRig.Length; i++)
                if (_legRig[i].Front == front && _legRig[i].Side == side) return _legRig[i];
            return null;
        }

        /// <summary>Which side the target is on (+1 right, -1 left); the right side when there is no target.</summary>
        private int TargetSide()
        {
            if (!_hasLook) return 1;
            return _lookDir.x >= 0f ? 1 : -1;
        }

        private void ActionsLegged(ref CreaturePoseInput p, ref Overlay o, float dt)
        {
            ClearLegOverrides(dt);
            if (p.ActionT < 0f) { _deathHold = 0f; return; }
            Phases ph = PhasesOf(ref p);
            float L = Mathf.Max(0.05f, _legLen);
            float W = ph.W;
            switch (p.Action)
            {
                case CreatureAction.Bite: Bite(ref o, ph, L); break;
                case CreatureAction.Claw: ClawSwipe(ref o, ph, L); break;
                case CreatureAction.Pounce:
                case CreatureAction.Dive: Pounce(ref p, ref o, ph, L); break;
                case CreatureAction.TailSwipe: TailSwipe(ref o, ph, L); break;
                case CreatureAction.Roar: Roar(ref o, ph, L, false); break;
                case CreatureAction.Howl: Roar(ref o, ph, L, true); break;
                case CreatureAction.Charge: Charge(ref o, ph, L); break;
                case CreatureAction.Breath:
                case CreatureAction.Spit: Breath(ref o, ph, L); break;
                case CreatureAction.Slam: Slam(ref o, ph, L); break;
                case CreatureAction.Stagger: StaggerPose(ref o, ph, L); break;
                case CreatureAction.Death: DeathPose(ref p, ref o, dt, L); break;
            }
        }

        // ------------------------------------------------------------------ attacks

        private void Bite(ref Overlay o, Phases ph, float L)
        {
            float W = ph.W;
            // Coil back and low with the head drawn up and the jaws parting, lunge forward and snap shut.
            o.Root.z += W * Key3(ph, -0.1f * L, 0.22f * L);
            o.Crouch += W * Key3(ph, 0.28f, 0.08f);
            o.CrouchFront += W * Key3(ph, 0.1f, 0.35f);
            o.NeckPitch += W * Key3(ph, -26f, 12f);
            o.HeadPitch += W * Key3(ph, -12f, -2f);
            o.ChestPitch += W * Key3(ph, -5f, 4f);
            o.EarsBack = Mathf.Max(o.EarsBack, W);
            float jaw;
            if (ph.Rec > 0f) jaw = Mathf.Lerp(0.05f, 0f, ph.Rec);
            else if (ph.Hit > 0f) jaw = ph.Hit < 0.5f ? Mathf.Lerp(0.6f, 1f, ph.Hit / 0.5f) : Mathf.Lerp(1f, 0.05f, EOut((ph.Hit - 0.5f) / 0.5f));
            else jaw = 0.6f * E3(ph.Wind);
            o.Jaw = Mathf.Max(o.Jaw, jaw * W);
            o.LookW = Mathf.Lerp(1f, 0.6f, ph.Hit);
            o.Snap = Mathf.Max(o.Snap, W);
            o.TailPitch += W * Key3(ph, 8f, -6f);
        }

        private void ClawSwipe(ref Overlay o, Phases ph, float L)
        {
            int side = TargetSide();
            Leg leg = LegOf(true, side);
            float W = ph.W;
            // Weight rocks onto the other three legs, the paw rises cocked beside the head, then rakes down across.
            o.Root.x += W * Key3(ph, -side * 0.05f * L, side * 0.03f * L);
            o.Root.z += W * Key3(ph, -0.06f * L, 0.14f * L);
            o.Roll += W * Key3(ph, -side * 7f, side * 4f);
            o.Pitch += W * Key3(ph, -7f, 6f);
            o.CrouchRear += W * Key3(ph, 0.2f, 0.05f);
            o.NeckYaw += W * Key3(ph, -side * 12f, side * 6f);
            o.NeckPitch += W * Key3(ph, -8f, 10f);
            o.Jaw = Mathf.Max(o.Jaw, W * Key3(ph, 0.35f, 0.5f));
            o.EarsBack = Mathf.Max(o.EarsBack, W);
            o.Snap = Mathf.Max(o.Snap, W);
            if (leg == null || leg.Wing) return;
            Vector3 n = leg.Neutral;
            Vector3 raised = n + new Vector3(side * 0.12f * L, 0.95f * L, 0.18f * L);
            Vector3 struck = n + new Vector3(-side * 0.1f * L, 0.12f * L, 0.75f * L);
            Vector3 target = Key3(ph, raised, struck, n);
            if (ph.Rec > 0f) target.y += 0.18f * L * Mathf.Sin(Mathf.PI * Mathf.Min(1f, ph.Rec * 1.15f));
            float lift = ph.Rec > 0f ? 1f - E3(Mathf.Clamp01((ph.Rec - 0.7f) / 0.3f)) : 1f;
            float w = W * Mathf.Min(1f, ph.Wind * 3f) * lift;
            OverrideLeg(leg, w, target, Key3(ph, 70f, -25f));
            o.ToeCurl = W * Key3(ph, 30f, -20f);
        }

        private void Pounce(ref CreaturePoseInput p, ref Overlay o, Phases ph, float L)
        {
            float W = ph.W;
            // Crouch with the rear wiggling and the head locked low on the prey.
            float wiggle = ph.Hit <= 0f ? Mathf.Sin(ph.Wind * 26f) * E3(ph.Wind) : 0f;
            o.Crouch += W * Key3(ph, 0.55f, -0.15f);
            o.CrouchFront += W * Key3(ph, 0.15f, 0f);
            o.Bend += W * wiggle * 6f;
            o.TailYaw += W * wiggle * 22f;
            o.NeckPitch += W * Key3(ph, 14f, -10f);
            o.HeadPitch += W * Key3(ph, -16f, 4f);
            o.EarsBack = Mathf.Max(o.EarsBack, W * Key3(ph, 0f, 1f));
            o.Snap = Mathf.Max(o.Snap, W);
            o.LookW = 1f;
            // Launch: the body stretches out and up, the forelegs reach, the hind legs drive back.
            float air = ph.Hit > 0f && ph.Rec <= 0f ? Mathf.Sin(Mathf.PI * ph.Hit) : 0f;
            float hop = p.Grounded ? 0.55f * L * air : 0f;
            o.Root.y += W * hop;
            o.Root.z += W * Key3(ph, -0.08f * L, 0.3f * L);
            o.Pitch += W * (ph.Hit > 0f && ph.Rec <= 0f ? Mathf.Lerp(-16f, 12f, ph.Hit) : Key3(ph, 4f, 6f));
            o.Arch += W * Key3(ph, 6f, -12f);
            o.Jaw = Mathf.Max(o.Jaw, W * Key3(ph, 0.2f, 0.9f));
            o.TailPitch += W * Key3(ph, -10f, 28f);
            float release = ph.Hit > 0f ? (ph.Rec > 0f ? 1f - E3(ph.Rec * 2f) : SS(0f, 0.15f, ph.Hit)) : 0f;
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                if (l.Wing) continue;
                Vector3 n = l.Neutral;
                Vector3 reach = l.Front ? n + new Vector3(0f, 0.42f * L, 0.55f * L) : n + new Vector3(0f, 0.3f * L, -0.5f * L);
                // At the end of the flight the forelegs come down first to catch the landing.
                if (l.Front && ph.Hit > 0.65f) reach = Vector3.Lerp(reach, n + new Vector3(0f, 0.05f * L, 0.3f * L), SS(0.65f, 1f, ph.Hit));
                OverrideLeg(l, W * release, reach, l.Front ? -15f : 20f);
            }
        }

        private void TailSwipe(ref Overlay o, Phases ph, float L)
        {
            int side = TargetSide();
            float W = ph.W;
            // Turn the hindquarters toward the target, then whip round: the tail lags and cracks across.
            o.Yaw += W * Key3(ph, -side * 26f, side * 30f);
            o.Bend += W * Key3(ph, -side * 12f, side * 16f);
            o.TailYaw += W * Key3(ph, side * 55f, -side * 70f);
            o.TailPitch += W * Key3(ph, 18f, 28f);
            o.TailSnap = Mathf.Max(o.TailSnap, W * (ph.Hit > 0f ? 1f : 0.3f));
            o.NeckYaw += W * Key3(ph, side * 30f, side * 10f);
            o.CrouchFront += W * 0.15f;
            o.Snap = Mathf.Max(o.Snap, W);
            o.LookW = 0.4f;
        }

        private void Roar(ref Overlay o, Phases ph, float L, bool howl)
        {
            float W = ph.W;
            if (howl)
            {
                // Sit back on the haunches, the muzzle rising to the sky, a long trembling call.
                o.CrouchRear += W * Key3(ph, 0.3f, 0.42f);
                o.Pitch += W * Key3(ph, -10f, -16f);
                o.NeckPitch += W * Key3(ph, -40f, -55f);
                o.HeadPitch += W * Key3(ph, -20f, -32f);
                float trem = ph.Hit > 0f && ph.Rec <= 0f ? 0.06f * Mathf.Sin(ph.Hit * 60f) : 0f;
                o.Jaw = Mathf.Max(o.Jaw, W * (Key3(ph, 0.3f, 0.55f) + trem));
                o.LookW = 0f;
                o.EarsBack = Mathf.Max(o.EarsBack, 0.3f * W);
                o.TailPitch += W * -10f;
                return;
            }
            // Draw the head up and back with the chest rising, then thrust it at the enemy, jaws wide, body shaking.
            float shake = ph.Hit > 0f && ph.Rec <= 0f ? Mathf.Sin(ph.Hit * 90f) * (1f - ph.Hit * 0.5f) : 0f;
            o.NeckPitch += W * Key3(ph, -28f, 16f);
            o.HeadPitch += W * Key3(ph, -12f, -14f);
            o.ChestPitch += W * Key3(ph, -8f, 4f);
            o.Pitch += W * Key3(ph, -5f, 3f);
            o.Root.z += W * Key3(ph, -0.06f * L, 0.08f * L);
            o.Crouch += W * Key3(ph, 0.05f, 0.18f);
            o.Jaw = Mathf.Max(o.Jaw, W * Key3(ph, 0.25f, 1f));
            o.HeadYaw += W * shake * 3f;
            o.Roll += W * shake * 1.5f;
            o.EarsBack = Mathf.Max(o.EarsBack, W);
            o.LookW = Mathf.Lerp(1f, 0.7f, ph.Hit);
            o.TailPitch += W * Key3(ph, 8f, 14f);
            o.Snap = Mathf.Max(o.Snap, W * 0.6f);
            // Flyers throw their wings open: a threat display that doubles their size.
            o.WingSpread = Mathf.Max(o.WingSpread, W * Key3(ph, 0.7f, 1f));
        }

        private void Charge(ref Overlay o, Phases ph, float L)
        {
            float W = ph.W;
            // Head down and horns (or jaws) forward, body low; a foreleg paws the ground in the wind-up.
            o.NeckPitch += W * Key3(ph, 22f, 30f);
            o.HeadPitch += W * Key3(ph, -12f, -6f);
            o.Crouch += W * Key3(ph, 0.2f, 0.12f);
            o.CrouchFront += W * Key3(ph, 0.15f, 0.25f);
            o.Root.z += W * Key3(ph, -0.05f * L, 0.15f * L);
            o.EarsBack = Mathf.Max(o.EarsBack, W);
            o.TailPitch += W * 22f;
            o.Jaw = Mathf.Max(o.Jaw, W * Key3(ph, 0.1f, 0.4f));
            o.Snap = Mathf.Max(o.Snap, W);
            Leg leg = LegOf(true, 1);
            if (leg != null && !leg.Wing && ph.Hit <= 0f)
            {
                float scrape = Frac(ph.Wind * 2f);
                Vector3 n = leg.Neutral;
                Vector3 t;
                if (scrape < 0.6f) t = n + new Vector3(0f, 0.012f * L, Mathf.Lerp(0.22f, -0.22f, scrape / 0.6f) * L);
                else
                {
                    float u = (scrape - 0.6f) / 0.4f;
                    t = n + new Vector3(0f, 0.012f * L + 0.14f * L * Mathf.Sin(Mathf.PI * u), Mathf.Lerp(-0.22f, 0.22f, E3(u)) * L);
                }
                OverrideLeg(leg, W * SS(0.05f, 0.2f, ph.Wind) * (1f - SS(0.85f, 1f, ph.Wind)), t, 20f);
            }
        }

        private void Breath(ref Overlay o, Phases ph, float L)
        {
            float W = ph.W;
            // Draw the head back and inflate the chest, then extend the neck at the target and pour it out, sweeping.
            float sweep = ph.Hit > 0f && ph.Rec <= 0f ? Mathf.Sin(ph.Hit * Mathf.PI * 2f) * 10f : 0f;
            o.NeckPitch += W * Key3(ph, -22f, 10f);
            o.HeadPitch += W * Key3(ph, -10f, -2f);
            o.ChestPitch += W * Key3(ph, -9f, 2f);
            o.Root.z += W * Key3(ph, -0.08f * L, 0.06f * L);
            o.Crouch += W * Key3(ph, 0.12f, 0.22f);
            o.Jaw = Mathf.Max(o.Jaw, W * Key3(ph, 0.3f, 0.95f));
            o.HeadYaw += W * sweep;
            o.LookW = 1f;
            o.EarsBack = Mathf.Max(o.EarsBack, W);
            o.TailPitch += W * Key3(ph, 6f, -8f);
            o.WingSpread = Mathf.Max(o.WingSpread, W * 0.4f);
        }

        private void Slam(ref Overlay o, Phases ph, float L)
        {
            float W = ph.W;
            // Rear up with the forelegs raised, then crash down with both paws.
            o.Pitch += W * Key3(ph, -32f, 10f);
            o.Root.y += W * Key3(ph, 0.45f * L, -0.08f * L);
            o.CrouchRear += W * Key3(ph, 0.3f, 0.1f);
            o.NeckPitch += W * Key3(ph, -10f, 22f);
            o.HeadPitch += W * Key3(ph, -8f, 6f);
            o.Jaw = Mathf.Max(o.Jaw, W * Key3(ph, 0.5f, 0.8f));
            o.Snap = Mathf.Max(o.Snap, W);
            o.WingSpread = Mathf.Max(o.WingSpread, W * Key3(ph, 1f, 0.6f));
            for (int s = -1; s <= 1; s += 2)
            {
                Leg leg = LegOf(true, s);
                if (leg == null || leg.Wing) continue;
                Vector3 n = leg.Neutral;
                Vector3 up = n + new Vector3(s * 0.05f * L, 0.8f * L, 0.3f * L);
                Vector3 down = n + new Vector3(0f, 0f, 0.4f * L);
                Vector3 target = Key3(ph, up, down, n);
                if (ph.Hit > 0f && ph.Rec <= 0f)
                {
                    // Forward travel finishes first so the paws come straight down onto the ground.
                    float hz = EOut(Mathf.Min(1f, ph.Hit * 1.6f)), hy = EOut(ph.Hit);
                    target = new Vector3(Mathf.Lerp(up.x, down.x, hz), Mathf.Lerp(up.y, down.y, hy), Mathf.Lerp(up.z, down.z, hz));
                }
                float w = W * (ph.Rec > 0f ? 1f - E3(ph.Rec) : 1f);
                OverrideLeg(leg, w, target, Key3(ph, 50f, -10f));
            }
        }

        // ------------------------------------------------------------------ states

        private void StaggerPose(ref Overlay o, Phases ph, float L)
        {
            float t = ph.T;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * (1f - 0.3f * t);
            Vector3 away = -_hurtDir;
            // Thrown back off balance, head swinging, legs splayed to catch the weight.
            o.Root += new Vector3(away.x, 0f, away.z) * (0.16f * L * env);
            o.Roll += -away.x * 14f * env;
            o.Pitch += away.z * -10f * env;
            o.Crouch += 0.25f * env;
            o.HeadYaw += Mathf.Sin(t * 38f) * 16f * env;
            o.NeckPitch += 14f * env;
            o.Jaw = Mathf.Max(o.Jaw, 0.35f * env);
            o.Free = Mathf.Max(o.Free, env);
            o.LookW = 1f - env;
            o.TailYaw += Mathf.Sin(t * 20f) * 20f * env;
        }

        private void DeathPose(ref CreaturePoseInput p, ref Overlay o, float dt, float L)
        {
            // Collapse over the first part of the action, then hold the final pose for as long as the action lasts.
            float t = Mathf.Max(p.ActionT, _deathHold);
            _deathHold = Mathf.Max(_deathHold, Mathf.Min(1f, p.ActionT));
            float k = E3(Mathf.Clamp01(t / 0.72f));
            float buckle = E3(Mathf.Clamp01(t / 0.3f));
            int side = _hurtDir.x >= 0f ? -1 : 1;
            float lying = Mathf.Max(0.04f, Body.Bounds.size.x * 0.32f);
            o.Dead = k;
            o.Root.y += -(_rootH - lying) * k - 0.1f * L * buckle * (1f - k);
            o.Roll += side * 84f * E3(Mathf.Clamp01((t - 0.15f) / 0.6f));
            o.Pitch += 6f * buckle * (1f - k);
            o.NeckPitch += 34f * k;
            o.NeckYaw += side * 20f * k;
            o.HeadPitch += 10f * k;
            o.HeadRoll += side * 30f * k;
            o.Jaw = Mathf.Max(o.Jaw, 0.3f * k);
            o.Free = 1f;
            o.LookW = 0f;
            o.TailFree = 1f;
            o.TailPitch += -18f * k;
            o.EarsBack = Mathf.Max(o.EarsBack, 0.8f * k);
            o.WingSpread = Mathf.Max(o.WingSpread, 0.7f * k);
            for (int i = 0; i < _legRig.Length; i++) _legRig[i].Released = true;
        }

        private void AlertPose(ref Overlay o, float a)
        {
            // Hackles up: front lowered, back arched, head low with the eyes up on the target, ears pinned.
            o.CrouchFront += 0.22f * a;
            o.Arch += -5f * a;
            o.NeckPitch += 16f * a;
            o.HeadPitch += -14f * a;
            o.EarsBack = Mathf.Max(o.EarsBack, 0.6f * a);
            if (_look.HeadStyle == 1) o.TailYaw += Mathf.Sin(_clock * 5.5f) * 18f * a;
            o.TailPitch += (_look.HeadStyle == 0 ? 24f : (_look.HeadStyle == 1 ? -6f : 10f)) * a;
            o.Jaw = Mathf.Max(o.Jaw, 0.12f * a * (0.5f + 0.5f * Mathf.Sin(_clock * 3f)));
        }

        private void HurtPose(ref Overlay o, float h)
        {
            float L = Mathf.Max(0.05f, _legLen);
            Vector3 away = -_hurtDir;
            o.Root += new Vector3(away.x, 0f, away.z) * (0.07f * L * h);
            o.Roll += -away.x * 7f * h;
            o.Pitch += away.z * -5f * h;
            o.HeadYaw += away.x * 18f * h;
            o.NeckPitch += 6f * h;
            o.Jaw = Mathf.Max(o.Jaw, 0.25f * h);
            o.EarsBack = Mathf.Max(o.EarsBack, h);
            o.Free = Mathf.Max(o.Free, 0.5f * h);
        }
    }
}
