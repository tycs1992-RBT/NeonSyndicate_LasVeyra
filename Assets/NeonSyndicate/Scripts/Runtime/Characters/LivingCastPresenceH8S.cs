using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Why the living cast presence policy chose the reviewed visual or the procedural fallback on a step.</summary>
    public enum LivingCastPresenceReasonH8S
    {
        /// <summary>The reviewed visual shows.</summary>
        Shown = 0,
        /// <summary>The exact actor is missing, dead, knocked down or inactive.</summary>
        NoActor = 1,
        /// <summary>The cast owner has no bound post for the actor.</summary>
        PostUnbound = 2,
        /// <summary>The original visibility owner hides the procedural body, so nothing replaces it.</summary>
        HiddenByOwner = 3,
        /// <summary>The actor is away from the post.</summary>
        AwayFromPost = 4,
        /// <summary>Neither the camera nor the player is within review distance.</summary>
        OutOfRange = 5,
        /// <summary>A change is wanted but has not yet held for MinHold seconds.</summary>
        Holding = 6
    }

    /// <summary>
    /// Engine-free presence policy for one reviewed living cast visual (Dante Rook first). The supplied visual may show only
    /// while the exact actor stands at the bound post and the camera or the player is within review distance; otherwise the
    /// procedural body shows. Separate enter and exit distances and a short debounce keep it from flickering. Losing the
    /// actor, the owner hiding the body or the actor leaving the post switch to the fallback at once, because the original
    /// cast owner returns a displaced actor to the post only while the procedural body is unseen, and that test must see
    /// the real body again before the actor drifts 3 m away.
    /// </summary>
    public sealed class LivingCastPresenceH8S
    {
        /// <summary>Flat metres from the post: enter at or below PostEnter, leave above PostExit (below the cast owner's 3 m return).</summary>
        public const float PostEnter = 1.6f, PostExit = 2.6f;
        /// <summary>Vertical metres from the post: enter at or below RiseEnter, leave above RiseExit (below the owner's 1.5 m).</summary>
        public const float RiseEnter = 0.8f, RiseExit = 1.2f;
        /// <summary>Metres from the nearer of the camera and the player: enter at or below ViewEnter, leave above ViewExit.</summary>
        public const float ViewEnter = 24f, ViewExit = 30f;
        /// <summary>Seconds a range change must persist before it takes effect.</summary>
        public const float MinHold = 0.5f;
        /// <summary>The longest step counted, so one long hitch cannot satisfy the debounce on its own.</summary>
        public const float MaxStep = 0.25f;

        private float _pending;

        /// <summary>True while the reviewed visual should show.</summary>
        public bool Showing { get; private set; }

        /// <summary>The reason for the latest decision.</summary>
        public LivingCastPresenceReasonH8S Reason { get; private set; }

        /// <summary>Starts in a known state (the Editor review starts shown, after checking the enter conditions itself).</summary>
        public void Reset(bool showing)
        {
            Showing = showing;
            _pending = 0f;
            Reason = showing ? LivingCastPresenceReasonH8S.Shown : LivingCastPresenceReasonH8S.OutOfRange;
        }

        /// <summary>True when the enter conditions hold right now (the Editor checks this before it starts a review).</summary>
        public static bool CanEnter(bool postBound, Vector3 post, Vector3 actor, bool hasCamera, Vector3 camera, bool hasPlayer, Vector3 player)
        {
            return postBound && AtPost(post, actor, PostEnter, RiseEnter) && Nearest(actor, hasCamera, camera, hasPlayer, player) <= ViewEnter;
        }

        /// <summary>
        /// One decision. actorLive: the exact actor exists, is alive, upright and active. ownerShowsBody: the original owner
        /// currently shows the procedural body (pass true while the visual shows, when the adapter hides the body itself).
        /// Positions are world positions; dt is the unpaused frame time. Returns Showing.
        /// </summary>
        public bool Step(bool actorLive, bool ownerShowsBody, bool postBound, Vector3 post, Vector3 actor,
            bool hasCamera, Vector3 camera, bool hasPlayer, Vector3 player, float dt)
        {
            if (!(dt > 0f)) dt = 0f;
            if (dt > MaxStep) dt = MaxStep;
            if (!actorLive) return Hide(LivingCastPresenceReasonH8S.NoActor);
            if (!postBound) return Hide(LivingCastPresenceReasonH8S.PostUnbound);
            if (!ownerShowsBody) return Hide(LivingCastPresenceReasonH8S.HiddenByOwner);
            bool atPost = Showing ? AtPost(post, actor, PostExit, RiseExit) : AtPost(post, actor, PostEnter, RiseEnter);
            if (!atPost) return Hide(LivingCastPresenceReasonH8S.AwayFromPost);
            float view = Nearest(actor, hasCamera, camera, hasPlayer, player);
            bool want = Showing ? view <= ViewExit : view <= ViewEnter;
            if (want == Showing)
            {
                _pending = 0f;
                Reason = Showing ? LivingCastPresenceReasonH8S.Shown : LivingCastPresenceReasonH8S.OutOfRange;
                return Showing;
            }
            _pending += dt;
            if (_pending < MinHold)
            {
                Reason = LivingCastPresenceReasonH8S.Holding;
                return Showing;
            }
            _pending = 0f;
            Showing = want;
            Reason = want ? LivingCastPresenceReasonH8S.Shown : LivingCastPresenceReasonH8S.OutOfRange;
            return Showing;
        }

        private bool Hide(LivingCastPresenceReasonH8S why)
        {
            Showing = false;
            _pending = 0f;
            Reason = why;
            return false;
        }

        /// <summary>Within flat and vertical limits of the post. Non-finite input never counts as present.</summary>
        private static bool AtPost(Vector3 post, Vector3 actor, float flat, float rise)
        {
            float dx = actor.x - post.x, dz = actor.z - post.z, dy = actor.y - post.y;
            float flatSqr = dx * dx + dz * dz;
            return flatSqr <= flat * flat && Mathf.Abs(dy) <= rise;
        }

        /// <summary>Distance from the actor to the nearer available viewer, or infinity when there is none.</summary>
        private static float Nearest(Vector3 actor, bool hasCamera, Vector3 camera, bool hasPlayer, Vector3 player)
        {
            float best = float.PositiveInfinity;
            if (hasCamera)
            {
                float d = Vector3.Distance(actor, camera);
                if (d < best) best = d;
            }
            if (hasPlayer)
            {
                float d = Vector3.Distance(actor, player);
                if (d < best) best = d;
            }
            return best;
        }
    }
}
