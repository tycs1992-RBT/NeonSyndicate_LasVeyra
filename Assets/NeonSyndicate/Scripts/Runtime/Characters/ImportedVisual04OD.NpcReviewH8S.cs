#if UNITY_EDITOR
using System;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>
    /// Explicit Editor review of Dante Rook's supplied visual on the exact canonical cast actor, through this same adapter.
    /// The actor keeps its identity, brain, dialogue, quest giver role, faction, schedule, inventory, navigation, damage and
    /// Forge service; only presentation changes. While the visual shows, the duplicated procedural apron and tool belt are
    /// hidden (the hammer stays); while the review runs the original animator keeps its full pose cadence. Every exit
    /// restores the procedural body, those costume layers and the cadence. No registry, save or player state is written.
    /// </summary>
    public sealed partial class ImportedVisual04OD
    {
        private NpcController _npcH8S;
        private LivingCastReviewH8S _driverH8S;
        private bool _npcShownH8S;
        private MeshRenderer[] _costumeH8S;
        private bool[] _costumeEnabledH8S;
        private bool _costumeHiddenH8S;
        private bool _cadenceOwnedH8S, _priorAlwaysAnimateH8S;

        /// <summary>
        /// Binds model to the live canonical Dante actor through Initialize. Refuses unless in Play mode in the Editor, the
        /// actor is exactly Game.Campaign.CastActor(CastIds04E.Dante), alive, upright, not in creature mode, built with the
        /// complete 47-bone rig, and DanteReviewH8S.CanReview passes. cleanup runs once on every exit, including a refusal
        /// raised inside Initialize. reason explains a refusal.
        /// </summary>
        public static bool BeginNpcReviewH8S(NpcController npc, ImportedModel04OD model, Action cleanup, out string reason)
        {
            reason = "";
            if (!Application.isEditor || !Application.isPlaying) { reason = "Start Play mode and meet Dante Rook at his Forge before the review."; return false; }
            CampaignDirector campaign = Game.Campaign;
            if (npc == null || campaign == null || campaign.CastActor(CastIds04E.Dante) != npc)
            { reason = "Only the live canonical Dante Rook from the campaign cast can be reviewed. No substitute is spawned."; return false; }
            if (npc.IsDead || npc.IsRagdolled || !npc.isActiveAndEnabled) { reason = "Dante Rook must be alive, upright and present."; return false; }
            if (npc.CreatureSpecies != null || npc.Rig == null || npc.Rig.Look.Creature != HumanoidCreature.None)
            { reason = "Creature mode is not a cast review target."; return false; }
            if (npc.Rig.Bones == null || npc.Rig.Bones.Length != RigLayout.BoneCount || npc.Rig.Layout == null || npc.Rig.Body == null || npc.Anim == null)
            { reason = "Dante Rook has the reduced 17-bone body (Low graphics preset). Choose Medium or higher and let him respawn, then retry."; return false; }
            if ("npc:" + npc.DisplayName != DanteReviewH8S.Identity) { reason = "The live actor's display name no longer derives " + DanteReviewH8S.Identity + "."; return false; }
            if (!DanteReviewH8S.CanReview(model, true, out reason)) return false;
            if (ImportedModelRegistry04OD.Find(model.Identity) != null) { reason = "An approved production visual already owns Dante Rook. Review does not displace it."; return false; }
            ImportedVisual04OD adapter = npc.GetComponent<ImportedVisual04OD>();
            if (adapter == null) adapter = npc.gameObject.AddComponent<ImportedVisual04OD>();
            adapter.Restore();
            adapter._owner = npc;
            adapter._companion = null;
            adapter._npcH8S = npc;
            adapter._reviewH6 = model;
            adapter._reviewCleanupH6 = () =>
            {
                adapter.ReleaseNpcH8S();
                if (cleanup != null) cleanup();
            };
            adapter.enabled = true;
            adapter.Initialize();
            if (!adapter._ready)
            {
                reason = "Native visual validation refused the candidate. The procedural body stays; inspect the first Console warning.";
                return false;
            }
            adapter._npcShownH8S = true;
            adapter.CaptureCostumeH8S();
            adapter.HideCostumeH8S(true);
            adapter.OwnCadenceH8S(true);
            adapter._driverH8S = npc.gameObject.AddComponent<LivingCastReviewH8S>();
            adapter._driverH8S.Bind(npc, adapter);
            return true;
        }

        /// <summary>Ends the review on this actor: the visual is destroyed and the body, costume and cadence are restored.</summary>
        public static void EndNpcReviewH8S(NpcController npc)
        {
            ImportedVisual04OD adapter = npc != null ? npc.GetComponent<ImportedVisual04OD>() : null;
            if (adapter != null && adapter._npcH8S != null)
            {
                adapter.Restore();
                Destroy(adapter);
            }
        }

        /// <summary>True while a Dante review binding exists on this actor (shown or resting on the fallback).</summary>
        public static bool IsNpcReviewActiveH8S(NpcController npc)
        {
            ImportedVisual04OD adapter = npc != null ? npc.GetComponent<ImportedVisual04OD>() : null;
            return adapter != null && adapter.NpcReviewLiveH8S;
        }

        /// <summary>True while the reviewed visual is the one being drawn (false while the presence policy shows the body).</summary>
        public static bool IsNpcReviewShownH8S(NpcController npc)
        {
            ImportedVisual04OD adapter = npc != null ? npc.GetComponent<ImportedVisual04OD>() : null;
            return adapter != null && adapter.NpcReviewLiveH8S && adapter._npcShownH8S;
        }

        /// <summary>How many duplicated procedural costume layers the review hides while the visual shows (Dante: 2).</summary>
        public static int NpcReviewCostumeLayersH8S(NpcController npc)
        {
            ImportedVisual04OD adapter = npc != null ? npc.GetComponent<ImportedVisual04OD>() : null;
            return adapter != null && adapter.NpcReviewLiveH8S ? adapter.CostumeLayersH8S : 0;
        }

        /// <summary>The binding is intact: a review row, its actor and the visual instance all exist.</summary>
        internal bool NpcReviewLiveH8S
        {
            get { return _npcH8S != null && _reviewH6 != null && _visual != null && _fallback != null; }
        }

        /// <summary>Whether the original owner shows the procedural body (only observable while the fallback is drawn).</summary>
        internal bool OwnerShowsBodyH8S
        {
            get { return _npcShownH8S || (_fallback != null && _fallback.enabled); }
        }

        /// <summary>
        /// The presence switch. Shown: the adapter copies the pose each LateUpdate and the duplicated costume is hidden.
        /// Resting: the adapter rests (no bone writes), the visual is inactive and the procedural body and costume are exactly
        /// as the original owners left them. The cadence flag stays owned for the whole session (see OwnCadenceH8S), so a
        /// later nested owner of the same flag unwinds in order. Returns false when the binding cannot honour the request.
        /// </summary>
        internal bool ShowNpcH8S(bool show)
        {
            if (!NpcReviewLiveH8S) return false;
            if (show == _npcShownH8S) return true;
            if (show)
            {
                if (_reviewH6.PreserveInfluencesH5 && QualitySettings.skinWeights != SkinWeights.Unlimited)
                {
                    // Complete weights cannot silently degrade while resting either: end the review, keep the body.
                    Log.Warn("H8S Dante review ended because live skin quality changed. Complete weights cannot silently degrade.");
                    Restore();
                    enabled = false;
                    return false;
                }
                if (_npcH8S.IsDead || _npcH8S.IsRagdolled || !_fallback.enabled) return false;
                _originalEnabled = _fallback.enabled;
                _visual.SetActive(true);
                _fallback.enabled = false;
                HideCostumeH8S(true);
                _npcShownH8S = true;
                _ready = true;
            }
            else
            {
                // A resting adapter skips LateUpdate entirely, so the original body is never re-hidden behind its back.
                _ready = false;
                _npcShownH8S = false;
                _visual.SetActive(false);
                _fallback.enabled = _originalEnabled;
                HideCostumeH8S(false);
            }
            return true;
        }

        /// <summary>Runs once from ReleaseReviewH6, before the caller's cleanup, on every exit path.</summary>
        private void ReleaseNpcH8S()
        {
            // The quiet listening loop joined this review last, so it leaves first (its cadence owner unwinds before ours).
            if (_npcH8S != null && _npcH8S.Anim != null) _npcH8S.Anim.EndQuietReviewH8S();
            HideCostumeH8S(false);
            OwnCadenceH8S(false);
            if (_driverH8S != null) Destroy(_driverH8S);
            _driverH8S = null;
            _npcH8S = null;
            _npcShownH8S = false;
            _costumeH8S = null;
            _costumeEnabledH8S = null;
        }

        /// <summary>
        /// Finds the procedural layers that duplicate the supplied apron and belt. CastCostumeBuilder04E parents each layer
        /// directly under its bone with a fixed name; the held tool on the left hand is deliberately not collected.
        /// </summary>
        private void CaptureCostumeH8S()
        {
            HumanoidRig rig = _npcH8S != null ? _npcH8S.Rig : null;
            MeshRenderer torso = rig != null ? CostumeLayerH8S(rig.Chest, DanteReviewH8S.CostumeTorso) : null;
            MeshRenderer waist = rig != null ? CostumeLayerH8S(rig.Hips, DanteReviewH8S.CostumeWaist) : null;
            int count = (torso != null ? 1 : 0) + (waist != null ? 1 : 0);
            _costumeH8S = new MeshRenderer[count];
            _costumeEnabledH8S = new bool[count];
            int at = 0;
            if (torso != null) _costumeH8S[at++] = torso;
            if (waist != null) _costumeH8S[at] = waist;
            _costumeHiddenH8S = false;
        }

        private static MeshRenderer CostumeLayerH8S(Transform bone, string name)
        {
            if (bone == null) return null;
            for (int i = 0; i < bone.childCount; i++)
            {
                Transform child = bone.GetChild(i);
                if (child.name == name) return child.GetComponent<MeshRenderer>();
            }
            return null;
        }

        /// <summary>The number of duplicated costume layers this review hides while the visual shows (Dante: torso and waist).</summary>
        internal int CostumeLayersH8S
        {
            get { return _costumeH8S != null ? _costumeH8S.Length : 0; }
        }

        /// <summary>Hides the duplicated layers, recording each exact enabled state, or restores those states.</summary>
        private void HideCostumeH8S(bool hide)
        {
            if (_costumeH8S == null || hide == _costumeHiddenH8S) return;
            for (int i = 0; i < _costumeH8S.Length; i++)
            {
                MeshRenderer layer = _costumeH8S[i];
                if (layer == null) continue;
                if (hide)
                {
                    _costumeEnabledH8S[i] = layer.enabled;
                    layer.enabled = false;
                }
                else layer.enabled = _costumeEnabledH8S[i];
            }
            _costumeHiddenH8S = hide;
        }

        /// <summary>
        /// While the procedural body is hidden its renderer reports unseen, and the original animator would then refresh the
        /// pose only every eighth frame. The review keeps the original cadence owner at full rate from the bind until the
        /// release (not toggled by presence, so any nested owner of the flag unwinds last in, first out) and then restores
        /// the prior flag.
        /// </summary>
        private void OwnCadenceH8S(bool own)
        {
            HumanoidAnimator anim = _npcH8S != null ? _npcH8S.Anim : null;
            if (own)
            {
                if (_cadenceOwnedH8S || anim == null) return;
                _priorAlwaysAnimateH8S = anim.AlwaysAnimate;
                anim.AlwaysAnimate = true;
                _cadenceOwnedH8S = true;
            }
            else if (_cadenceOwnedH8S)
            {
                if (anim != null) anim.AlwaysAnimate = _priorAlwaysAnimateH8S;
                _cadenceOwnedH8S = false;
            }
        }
    }
}
#endif
