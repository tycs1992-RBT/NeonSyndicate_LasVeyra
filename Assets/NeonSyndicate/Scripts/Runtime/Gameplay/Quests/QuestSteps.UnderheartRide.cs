using System;
using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart and Bonds 04E riding steps: Park (the player's own personal chassis stopped in a zone), Fuse (a fused
    /// form, optionally a given form or inside a zone; or a safe separation) and Course (a fusion handling course with
    /// ability uses and a controlled stop). The steps only watch and explain: Game.Fusion and Game.Garage own the
    /// transformation, its validation, rollback and the no-refill rules.
    /// </summary>
    public static partial class QuestSteps
    {
        private static string CompanionName()
        {
            CompanionRecord a = Game.Bonds != null ? Game.Bonds.Active : null;
            if (a == null) return "your companion";
            return string.IsNullOrEmpty(a.Name) ? BondIds.DefaultName(a.Archetype) : a.Name;
        }

        private static float VehicleSpeed()
        {
            PlayerController p = Game.Player;
            return p != null && p.InVehicle && p.CurrentVehicle != null ? p.CurrentVehicle.Speed : 0f;
        }

        // ------------------------------------------------------------------ Park

        private static IEnumerator Park(QuestRun r, QuestStep s)
        {
            if (Game.Garage == null)
            {
                FailQuest(r, "The personal garage did not start (see STATUS). Restart this chapter once it is available.");
                yield break;
            }
            Vector3 c;
            float radius;
            bool exact = true;
            if (string.IsNullOrEmpty(s.At))
            {
                if (Game.Fusion == null || !Game.Fusion.TryBay(out c, out radius))
                {
                    FailQuest(r, "Jax's bay is not available in this build. Restart this chapter once it is.");
                    yield break;
                }
            }
            else
            {
                c = r.Place(s.At, s.OffsetX, s.OffsetZ);
                exact = r.LastExact;
                radius = s.Radius > 0f ? s.Radius : 4f;
            }
            string objective = Obj(s, "Ride your own bike into the bay and stop. Phone > Bring my bike if it is not with you.");
            WorldMarker m = exact ? ExactMarker(r, c, MarkerColor, Mathf.Min(radius, 4f)) : r.C.Marker(c, MarkerColor, Mathf.Min(radius, 4f));
            Blip b = r.C.BlipAt(c, MarkerColor, "Garage bay");
            r.C.Target(c);
            float t = 0f;
            while (true)
            {
                PlayerController p = Game.Player;
                bool inVehicle = p != null && p.InVehicle && p.CurrentVehicle != null;
                bool personal = inVehicle && Game.Garage != null && Game.Garage.IsPersonal(p.CurrentVehicle);
                bool near = MathUtil.FlatDistance(Game.PlayerPosition, c) < radius && Mathf.Abs(Game.PlayerPosition.y - c.y) < ExactHeightTolerance;
                if (personal && near && VehicleSpeed() < 1f) break;
                t -= Time.deltaTime;
                if (t <= 0f)
                {
                    t = 0.4f;
                    string o;
                    if (!inVehicle) o = objective;
                    else if (!personal) o = "This is not your personal chassis: borrowed, police and mission vehicles cannot be fused. Phone > Bring my bike.";
                    else if (!near) o = "Ride into the marked bay.";
                    else o = "Stop inside the bay.";
                    if (r.C.ObjectiveText != o) r.C.Objective(o);
                }
                yield return null;
            }
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
            r.C.RemoveBlip(b);
            r.C.ClearTarget();
            if (!string.IsNullOrEmpty(s.Text) && Game.Hud != null) Game.Hud.Notify(s.Text, 4f);
        }

        // ------------------------------------------------------------------ Fuse

        private static IEnumerator Fuse(QuestRun r, QuestStep s)
        {
            FusionSystem f = Game.Fusion;
            if (f == null)
            {
                FailQuest(r, "Creature-vehicle fusion did not start (see STATUS). Your bike and companion are untouched; restart later.");
                yield break;
            }
            float t = 0f;
            if (s.Text == "separate")
            {
                while (f.IsFused || f.State != FusionState.Separate)
                {
                    t -= Time.deltaTime;
                    if (t <= 0f)
                    {
                        t = 0.4f;
                        string o = f.IsFused ? Obj(s, "Stop somewhere open and hold N to separate safely.") : "Separating...";
                        if (r.C.ObjectiveText != o) r.C.Objective(o);
                    }
                    yield return null;
                }
                if (Game.Hud != null) Game.Hud.Notify("Separated cleanly: " + CompanionName() + " and your bike, both as they were.", 4f);
                yield break;
            }
            bool zone = !string.IsNullOrEmpty(s.At);
            Vector3 c = Vector3.zero;
            float radius = s.Radius > 0f ? s.Radius : 6f;
            WorldMarker m = null;
            Blip b = null;
            if (zone)
            {
                c = r.Place(s.At, s.OffsetX, s.OffsetZ);
                m = r.LastExact ? ExactMarker(r, c, MarkerColor, Mathf.Min(radius, 4f)) : r.C.Marker(c, MarkerColor, Mathf.Min(radius, 4f));
                b = r.C.BlipAt(c, MarkerColor, "Fuse here");
                r.C.Target(c);
            }
            string formTitle = string.IsNullOrEmpty(s.Form) ? "" : FusionIds.Title(s.Form);
            while (true)
            {
                bool near = !zone || (MathUtil.FlatDistance(Game.PlayerPosition, c) < radius && Mathf.Abs(Game.PlayerPosition.y - c.y) < ExactHeightTolerance);
                bool rightForm = string.IsNullOrEmpty(s.Form) || f.ActiveForm == s.Form;
                if (f.IsFused && rightForm && near) break;
                t -= Time.deltaTime;
                if (t <= 0f)
                {
                    t = 0.4f;
                    string o;
                    if (f.IsFused && !rightForm) o = "Separate (hold N), then fuse with the companion that makes the " + formTitle + ".";
                    else if (f.IsFused && !near) o = "Bring the fused form back to the marked spot.";
                    else if (zone && !near) o = Obj(s, "Bring your bike to the marked spot.");
                    else if (f.State != FusionState.Separate) o = "Fusing...";
                    else
                    {
                        string why;
                        bool can = false;
                        try { can = f.CanFuse(out why); }
                        catch (Exception e) { why = "fusion check failed"; Log.Exception("Fusion check", e); }
                        o = can ? Obj(s, "Hold N to fuse with " + CompanionName() + (formTitle.Length > 0 ? " (" + formTitle + ")" : "") + ".") : "Fusion not ready: " + why;
                    }
                    if (r.C.ObjectiveText != o) r.C.Objective(o);
                }
                yield return null;
            }
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
            if (b != null) r.C.RemoveBlip(b);
            r.C.ClearTarget();
            if (Game.Hud != null) Game.Hud.Banner("FUSED", (string.IsNullOrEmpty(f.ActiveForm) ? "Fused form" : FusionIds.Title(f.ActiveForm)) + ": " + CompanionName() + " and your bike.", CastColor, 4f);
        }

        // ------------------------------------------------------------------ Course

        private static IEnumerator Course(QuestRun r, QuestStep s)
        {
            FusionSystem f = Game.Fusion;
            if (f == null)
            {
                FailQuest(r, "Creature-vehicle fusion did not start (see STATUS). Restart this chapter later.");
                yield break;
            }
            FusionCourse course = null;
            bool found = false;
            try { found = f.TryCourse(s.Course, out course); }
            catch (Exception e) { Log.Exception("Fusion course " + s.Course, e); }
            if (!found || course == null || course.Points == null || course.Points.Length == 0)
            {
                FailQuest(r, "The handling course is not set up in this build (" + s.Course + "). Restart this chapter once it is.");
                yield break;
            }
            int need = s.Count > 0 ? s.Count : Mathf.Max(0, course.AbilityUses);
            int uses = 0;
            Action<string> onUse = form => uses++;
            f.AbilityUsed += onUse;
            r.Cleanups.Add(() => { if (f != null) f.AbilityUsed -= onUse; });
            float radius = course.Radius > 0f ? course.Radius : 5f;
            int n = course.Points.Length;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = course.Points[i];
                bool last = i == n - 1;
                WorldMarker m = ExactMarker(r, p, MarkerColor, Mathf.Min(radius, 4f));
                Blip b = r.C.BlipAt(p, MarkerColor, "Course " + (i + 1) + "/" + n);
                r.C.Target(p);
                float t = 0f;
                while (true)
                {
                    bool near = MathUtil.FlatDistance(Game.PlayerPosition, p) < radius && Mathf.Abs(Game.PlayerPosition.y - p.y) < 8f;
                    bool fused = f.IsFused;
                    if (fused && near && !last) break;
                    if (fused && near && last && uses >= need && VehicleSpeed() < 3f) break;
                    t -= Time.deltaTime;
                    if (t <= 0f)
                    {
                        t = 0.3f;
                        string o;
                        if (!fused) o = "Fuse again to continue the course (hold N on your bike).";
                        else if (last && near && uses < need) o = "Use the form's ability (U) " + (need - uses) + " more time" + (need - uses == 1 ? "" : "s") + ", then stop at the last marker.";
                        else if (last && near) o = "Brake to a controlled stop at the last marker.";
                        else o = (!string.IsNullOrEmpty(course.Text) ? course.Text : Obj(s, "Ride the handling course.")) + " (" + (i + 1) + " of " + n + ", ability " + Mathf.Min(uses, need) + "/" + need + ")";
                        if (r.C.ObjectiveText != o) r.C.Objective(o);
                    }
                    yield return null;
                }
                if (m != null) UnityEngine.Object.Destroy(m.gameObject);
                r.C.RemoveBlip(b);
            }
            f.AbilityUsed -= onUse;
            r.C.ClearTarget();
            if (Game.Hud != null) Game.Hud.Notify("Course complete: braking, the form's ability and a controlled stop.", 4f);
            yield return SayLines(r, s.Lines);
        }
    }
}
