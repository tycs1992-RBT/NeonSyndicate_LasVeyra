#if UNITY_EDITOR
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>
    /// Editor-only presence driver for the Dante living cast review. Each LateUpdate (after the original animator, before
    /// the adapter's bone copy) it asks LivingCastPresenceH8S whether the reviewed visual or the procedural body should
    /// show, and ends the review if the actor stops being the exact canonical Dante. It writes no bones, input, AI, damage,
    /// rewards or saves, and is removed by the adapter on every exit path.
    /// </summary>
    [DefaultExecutionOrder(1990)]
    public sealed class LivingCastReviewH8S : MonoBehaviour
    {
        /// <summary>Seconds between post refreshes from the cast owner (the post rarely changes).</summary>
        private const float PostRefresh = 0.5f;

        private readonly LivingCastPresenceH8S _presence = new LivingCastPresenceH8S();
        private NpcController _npc;
        private ImportedVisual04OD _adapter;
        private int _settleFrame;
        private float _postTimer;
        private bool _postBound;
        private Vector3 _post;
        private LivingCastPresenceReasonH8S _logged;

        /// <summary>The latest presence decision (read by the Editor menu report).</summary>
        public LivingCastPresenceReasonH8S Reason { get { return _presence.Reason; } }

        /// <summary>Called once by the adapter right after a successful bind. The review starts shown.</summary>
        public void Bind(NpcController npc, ImportedVisual04OD adapter)
        {
            _npc = npc;
            _adapter = adapter;
            // Start() of a freshly added adapter runs before its first frame; resting before then could let it rebuild.
            _settleFrame = Time.frameCount + 2;
            _postTimer = 0f;
            _presence.Reset(true);
            _logged = LivingCastPresenceReasonH8S.Shown;
        }

        private void LateUpdate()
        {
            if (_npc == null || _adapter == null || !_adapter.NpcReviewLiveH8S)
            {
                Destroy(this);
                return;
            }
            CampaignDirector campaign = Game.Campaign;
            if (campaign == null || campaign.CastActor(CastIds04E.Dante) != _npc)
            {
                Log.Warn("H8S Dante living cast review ended: the actor is no longer the live canonical Dante Rook.");
                ImportedVisual04OD.EndNpcReviewH8S(_npc);
                return;
            }
            if (_adapter.SkinQualityLostH8S)
            {
                // Shown or resting: complete weights cannot silently degrade, and the session must not keep its cadence flag.
                Log.Warn("H8S Dante living cast review ended because live skin quality changed. Complete weights cannot silently degrade.");
                ImportedVisual04OD.EndNpcReviewH8S(_npc);
                return;
            }
            if (GameTime.Paused) return;
            _postTimer -= Time.unscaledDeltaTime;
            if (_postTimer <= 0f)
            {
                _postTimer = PostRefresh;
                float yaw;
                _postBound = campaign.CastPost(CastIds04E.Dante, out _post, out yaw);
            }
            PlayerCamera rig = Game.Cam;
            bool hasCamera = rig != null && rig.Cam != null;
            Vector3 camera = hasCamera ? rig.Cam.transform.position : Vector3.zero;
            bool hasPlayer = Game.Player != null;
            Vector3 player = hasPlayer ? Game.PlayerPosition : Vector3.zero;
            bool live = !_npc.IsDead && !_npc.IsRagdolled && _npc.isActiveAndEnabled;
            // Real time, so slow motion does not stretch the debounce.
            bool show = _presence.Step(live, _adapter.OwnerShowsBodyH8S, _postBound, _post, _npc.transform.position,
                hasCamera, camera, hasPlayer, player, Time.unscaledDeltaTime);
            if (!show && Time.frameCount < _settleFrame) return;
            if (!_adapter.ShowNpcH8S(show) && show) _presence.Reset(false);
            if (_presence.Reason != _logged && _presence.Reason != LivingCastPresenceReasonH8S.Holding)
            {
                _logged = _presence.Reason;
                Log.Info("H8S Dante living cast review: " + (_presence.Showing ? "supplied visual shown" : "procedural body shown (" + _logged + ")"));
            }
        }
    }
}
#endif
