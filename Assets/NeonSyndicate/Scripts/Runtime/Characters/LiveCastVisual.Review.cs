using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// LiveCastVisual, review yield. The explicit Editor reviews (the Malik art reviews through ImportedVisual04OD.BeginReviewH6
    /// and the Dante living cast review through BeginNpcReviewH8S) draw their own visual on the same actor, so the live visual
    /// yields completely while one owns the actor: it hides, and gives the procedural body, costume layers and their
    /// states back to the review. The quiet listening review only adds pose blending to the procedural animator, which
    /// the live visual then shows, so nothing is drawn twice and it needs no yield. Player builds have no reviews.
    /// </summary>
    public sealed partial class LiveCastVisual
    {
        /// <summary>True while an Editor review binding owns this actor (always false in player builds).</summary>
        private bool ReviewOwnsActor()
        {
#if UNITY_EDITOR
            // TryGetComponent first: in the Editor a failed GetComponent allocates its placeholder every call.
            ImportedVisual04OD review;
            if (_owner == null || !_owner.TryGetComponent(out review)) return false;
            return ImportedVisual04OD.IsReviewActiveH6(_owner) || (_npc != null && ImportedVisual04OD.IsNpcReviewActiveH8S(_npc));
#else
            return false;
#endif
        }
    }
}
