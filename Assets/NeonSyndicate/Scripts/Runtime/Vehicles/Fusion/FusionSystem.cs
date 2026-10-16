using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The single authoritative fusion state (Docs/Underheart04E/BONDS_AND_FUSION.md).</summary>
    public enum FusionState
    {
        Separate = 0,
        FusionRequested = 1,
        Validating = 2,
        TransitionIn = 3,
        Fused = 4,
        SeparationRequested = 5,
        TransitionOut = 6,
        Recovery = 7
    }

    /// <summary>A handling course for the quest Course step: ordered checkpoints and the ability uses it asks for.</summary>
    public sealed class FusionCourse
    {
        public string Id = "";
        public Vector3[] Points = new Vector3[0];
        public float Radius = 5f;
        public int AbilityUses;
        public string Text = "";
    }

    /// <summary>
    /// Game.Fusion: joins the active bonded companion with the player's owned personal chassis into a fused form and
    /// back, with validation, swept clearance, rollback on any failed step and the no-refill invariants; owns the form
    /// handling, abilities and HUD. FOUNDATION STUB: the FUSION work stream replaces the bodies and keeps every signature.
    /// </summary>
    public sealed partial class FusionSystem : MonoBehaviour
    {
        public event Action<string> Fused;
        public event Action Separated;
        public event Action<string> AbilityUsed;

        public void Init() { }

        public FusionState State { get { return FusionState.Separate; } }
        public bool IsFused { get { return State == FusionState.Fused; } }
        /// <summary>The active form id ("" while separate).</summary>
        public string ActiveForm { get { return ""; } }
        /// <summary>Form ability uses since the last fusion (the Course step counts them).</summary>
        public int AbilityUses { get { return 0; } }

        public bool CanFuse(out string reason)
        {
            reason = "Fusion is not implemented in this build.";
            return false;
        }

        /// <summary>Starts a fusion (validation first; nothing changes unless every check passes).</summary>
        public bool RequestFuse(out string reason)
        {
            reason = "Fusion is not implemented in this build.";
            return false;
        }

        /// <summary>Starts a safe separation (verifies support and clearance for the returned companion).</summary>
        public bool RequestSeparate(out string reason)
        {
            reason = "Fusion is not implemented in this build.";
            return false;
        }

        /// <summary>A handling course by id (FusionIds.Courses), resolved in the live world.</summary>
        public bool TryCourse(string id, out FusionCourse course)
        {
            course = null;
            return false;
        }

        /// <summary>The live bay at Jax's garage (centre and radius) where the first fusion must happen.</summary>
        public bool TryBay(out Vector3 center, out float radius)
        {
            center = Vector3.zero;
            radius = 0f;
            return false;
        }

        private void RaiseFused(string form)
        {
            if (Fused == null) return;
            foreach (Delegate h in Fused.GetInvocationList())
            {
                try { ((Action<string>)h)(form); }
                catch (Exception e) { Log.Exception("Fused listener", e); }
            }
        }

        private void RaiseSeparated()
        {
            if (Separated == null) return;
            foreach (Delegate h in Separated.GetInvocationList())
            {
                try { ((Action)h)(); }
                catch (Exception e) { Log.Exception("Separated listener", e); }
            }
        }

        private void RaiseAbilityUsed(string form)
        {
            if (AbilityUsed == null) return;
            foreach (Delegate h in AbilityUsed.GetInvocationList())
            {
                try { ((Action<string>)h)(form); }
                catch (Exception e) { Log.Exception("AbilityUsed listener", e); }
            }
        }
    }

    /// <summary>Reset hub for every static the FUSION work stream adds (garage and fusion).</summary>
    public static class FusionStatics
    {
        public static void ResetStatics() { }
    }
}
