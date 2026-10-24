using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The live character adapter. It draws an owner-approved imported humanoid from LiveCastRegistry (Tyler's Blender
    /// Malik for the player, Dante Rook at the Forge, any dropped-in character) in place of the procedural body, in
    /// ordinary play. The original actor keeps every role: identity, controller, procedural pose, weapons, collision,
    /// damage, AI, dialogue and saves. Each frame, after every pose writer, it copies the original bones onto the imported
    /// skeleton (the same bind rules as ImportedVisual04OD), mirrors the procedural body's own hidden and shadow-only
    /// states (first person, hidden people, shadowless bosses), copies the hit flash and glow, and keeps following through
    /// ragdoll and death. The procedural body is only forced off (its own enabled flag, which its owners write, is never
    /// touched), and it is restored exactly on any failure, on disable and on destroy. It yields completely while an
    /// Editor review owns the same actor and re-binds if the rig is rebuilt. Nothing is allocated per frame.
    /// </summary>
    [DefaultExecutionOrder(2010)]
    public sealed partial class LiveCastVisual : MonoBehaviour
    {
        private CharacterBase _owner;
        private NpcController _npc;
        private LiveCastRow _row;
        private string _castHint = "";
        private string _identity = "";
        private HumanoidRig _rig;
        private Transform[] _source;
        private SkinnedMeshRenderer _body;
        private bool _bodyForcedPrior;
        private GameObject _visual;
        private Transform[] _target;
        private Quaternion[] _offset;
        private int[] _order;
        private Renderer[] _renderers;
        private int[] _materialStart;
        private int[] _materialCount;
        private Color[] _emission;
        private MaterialPropertyBlock _block;
        private int _glowId, _flashId, _emissionId;
        private float _sentGlow = -1f, _sentFlash = -1f;
        private MeshRenderer[] _costume;
        private bool[] _costumeForcedPrior;
        private CadenceLoanH8S _cadence;
        private bool _ready, _reinit, _failed, _complete, _hidingBody, _needPose, _applied;
        private bool _appliedShow;
        private ShadowCastingMode _appliedShadows;

        /// <summary>
        /// Called once from the original body builder (CharacterBase.BuildBody). Adds the adapter only when the generated
        /// registry has a row that could serve this actor: the player row, an NPC row bound by display name, or a crowd row
        /// (a body that serves ordinary people by role, LiveCrowdRules; the adapter then decides per person in Start).
        /// Cast-bound rows attach from the cast owner instead (AttachCast).
        /// </summary>
        public static void Attach(CharacterBase owner)
        {
            if (owner == null || owner is Creature) return;
            LiveCastRegistry registry = LiveCastRegistry.Loaded;
            if (owner.IsPlayer) LiveCastNotice.CheckPlayer(registry);
            LiveCrowdPage.EnsureRegistered();
            if (registry == null) return;
            bool wanted = owner.IsPlayer ? registry.HasPlayerRow : (registry.HasOpenNpcRow || registry.HasCrowdRow);
            LiveCastVisual existing;
            if (wanted && !owner.TryGetComponent(out existing)) owner.gameObject.AddComponent<LiveCastVisual>();
        }

        /// <summary>
        /// Called once by the campaign cast owner right after it spawns a named cast actor. Adds the adapter when a live row is
        /// bound to that cast id; the adapter still checks that the actor is exactly Game.Campaign.CastActor(castId).
        /// </summary>
        public static void AttachCast(NpcController npc, string castId)
        {
            if (npc == null || string.IsNullOrEmpty(castId)) return;
            LiveCastRegistry registry = LiveCastRegistry.Loaded;
            if (registry == null || !registry.HasCastRow(castId)) return;
            LiveCastVisual live;
            if (!npc.TryGetComponent(out live)) live = npc.gameObject.AddComponent<LiveCastVisual>();
            live._castHint = castId;
        }

        /// <summary>
        /// Whether anyone can see this actor: the procedural body is visible, or its live visual is shown and visible. The
        /// original "return while nobody is looking" checks use this, because a live actor's forced-off body never reports
        /// visible.
        /// </summary>
        public static bool Seen(CharacterBase actor)
        {
            if (actor == null) return false;
            HumanoidRig rig = actor.Rig;
            if (rig != null && rig.Body != null && rig.Body.isVisible) return true;
            LiveCastVisual live;
            if (!actor.TryGetComponent(out live) || !live._ready || live._renderers == null) return false;
            for (int i = 0; i < live._renderers.Length; i++)
            {
                Renderer r = live._renderers[i];
                if (r != null && r.enabled && r.isVisible) return true;
            }
            return false;
        }

        /// <summary>True while a live visual draws this actor (for diagnostics and QA).</summary>
        public static bool IsLive(CharacterBase actor)
        {
            LiveCastVisual live;
            return actor != null && actor.TryGetComponent(out live) && live._ready && live._hidingBody;
        }

        private string Label
        {
            get { return _row != null && !string.IsNullOrEmpty(_row.Label) ? _row.Label : _identity; }
        }

        private void Start()
        {
            if (_row != null) return;
            _owner = GetComponent<CharacterBase>();
            _npc = _owner as NpcController;
            if (!Resolve())
            {
                Destroy(this);
                return;
            }
            _reinit = true;
        }

        /// <summary>Finds this actor's row by identity and, for a cast-bound row, by the exact live cast actor.</summary>
        private bool Resolve()
        {
            LiveCastRegistry registry = LiveCastRegistry.Loaded;
            if (registry == null || _owner == null) return false;
            bool creature = _owner is Creature || (_npc != null && _npc.CreatureSpecies != null);
            _identity = LiveCastRules.Identity(_owner.IsPlayer, creature, _owner.DisplayName);
            string castId = _npc != null ? CastIdOf(_npc) : "";
            _row = registry.Find(_identity, castId);
            if (_row != null) return true;
            // No named row: an ordinary person may still receive a crowd body (by role and sex, within the budget).
            return ResolveCrowd(registry, creature, castId);
        }

        /// <summary>The campaign cast id whose live actor is exactly npc, or "".</summary>
        private string CastIdOf(NpcController npc)
        {
            CampaignDirector campaign = Game.Campaign;
            if (campaign == null) return "";
            if (!string.IsNullOrEmpty(_castHint) && campaign.CastActor(_castHint) == npc) return _castHint;
            foreach (string id in CastIds04E.All)
                if (campaign.CastActor(id) == npc) return id;
            return "";
        }

        private void OnEnable()
        {
            if (_row != null && !_failed && !_ready) _reinit = true;
        }

        private void OnDisable()
        {
            Teardown();
            if (_row != null && !_failed) _reinit = true;
        }

        private void OnDestroy()
        {
            Teardown();
            if (_crowd) LiveCrowd.Unregister(this);
        }

        private void LateUpdate()
        {
            if (_failed || _row == null) return;
            if (_owner == null)
            {
                Teardown();
                return;
            }
            if (_crowd)
            {
                // A crowd body belongs to the budget: only the people nearest the camera hold one. Ungranted people cost
                // nothing more than this check; when the budget moves on, the body goes back and the person keeps waiting.
                LiveCrowd.TickIfDue();
                if (!_crowdGranted)
                {
                    if (_ready)
                    {
                        Teardown();
                        _reinit = true;
                    }
                    return;
                }
            }
            else if (_row.CastStudy && !LiveCrowd.Active)
            {
                // A cast member on a study body follows the Realistic people switch like the crowd (Dante's own body does not).
                if (_ready)
                {
                    Teardown();
                    _reinit = true;
                }
                return;
            }
            bool review = ReviewOwnsActor();
            if (_reinit)
            {
                // Wait while an Editor review owns this actor; nothing of the live visual exists meanwhile.
                if (review) return;
                _reinit = false;
                if (!Initialize()) return;
            }
            if (!_ready) return;
            if (_owner.Rig != _rig || _rig == null || _rig.Bones != _source)
            {
                // The rig was rebuilt: bind again from scratch next frame (the procedural body shows meanwhile).
                Teardown();
                _reinit = true;
                return;
            }
            if (_rig.Body != _body) Retarget();
            if (_complete && !review && !KeepSkinWeights()) return;
            ApplyPresentation(LiveCastRules.Present(review, _body != null && _body.enabled, _body != null ? _body.shadowCastingMode : ShadowCastingMode.On));
            if (!_appliedShow || (GameTime.Paused && !_needPose)) return;
            if (!CopyPose()) return;
            _needPose = false;
            CopyProperties();
        }

        /// <summary>Re-applies Unlimited skin weights if something reset them; fails closed when that does not stick.</summary>
        private bool KeepSkinWeights()
        {
            if (LiveCastRules.SkinWeightStep(true, LiveCastQuality.IsUnlimited, false) == LiveCastSkinStep.Keep) return true;
            if (LiveCastRules.SkinWeightStep(true, LiveCastQuality.Ensure(), true) == LiveCastSkinStep.Keep) return true;
            Fail(SkinWeightsRefused);
            return false;
        }

        private const string SkinWeightsRefused = "complete skin weights need Unlimited skin weights and the current quality level refused them.";

        /// <summary>The procedural body was rebuilt by its owner (HumanoidRig.ChangeLook): hide the new one instead.</summary>
        private void Retarget()
        {
            _body = _rig.Body;
            _bodyForcedPrior = _body != null && _body.forceRenderingOff;
            if (_hidingBody && _body != null) _body.forceRenderingOff = true;
            _sentGlow = -1f;
            _sentFlash = -1f;
        }

        /// <summary>Applies one presentation, writing renderer state only when it changes.</summary>
        private void ApplyPresentation(LiveCastPresentation look)
        {
            if (look.HideBody != _hidingBody)
            {
                SetBodyForced(look.HideBody);
                SetCostumeForced(look.HideBody);
                _hidingBody = look.HideBody;
                _needPose = true;
            }
            if (_applied && look.ShowVisual == _appliedShow && look.Shadows == _appliedShadows) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;
                r.enabled = look.ShowVisual && (_partShown == null || _partShown[i]);
                r.shadowCastingMode = look.Shadows;
            }
            if (look.ShowVisual && !_appliedShow) _needPose = true;
            _applied = true;
            _appliedShow = look.ShowVisual;
            _appliedShadows = look.Shadows;
        }

        /// <summary>Copies the original bones (world space, parents first) onto the imported skeleton. False when a bone vanished.</summary>
        private bool CopyPose()
        {
            for (int k = 0; k < _order.Length; k++)
            {
                int i = _order[k];
                Transform source = _source[i], target = _target[i];
                if (source == null || target == null)
                {
                    Teardown();
                    _reinit = true;
                    return false;
                }
                Vector3 position;
                Quaternion rotation;
                source.GetPositionAndRotation(out position, out rotation);
                target.SetPositionAndRotation(position, rotation * _offset[i]);
            }
            return true;
        }

        /// <summary>
        /// The original rig owns flashes and glow pulses in the procedural body's property block. When they change, each
        /// imported material receives its authored emission scaled by the glow plus the hit flash; at rest (glow one, no
        /// flash) the per-material blocks are cleared so the materials batch normally again. A crowd body also carries the
        /// person's own colours (skin, hair, coat, leather, trim) in the same block, so it is never cleared: the tint and
        /// the flash ride one block and neither erases the other.
        /// </summary>
        private void CopyProperties()
        {
            if (_body == null) return;
            _body.GetPropertyBlock(_block);
            float glow = _block.GetFloat(_glowId), flash = _block.GetFloat(_flashId);
            if (Mathf.Abs(glow - _sentGlow) < 0.005f && Mathf.Abs(flash - _sentFlash) < 0.005f) return;
            _sentGlow = glow;
            _sentFlash = flash;
            bool rest = Mathf.Abs(glow - 1f) < 0.005f && flash < 0.005f;
            bool tinted = _tints != null;
            for (int r = 0; r < _renderers.Length; r++)
            {
                Renderer renderer = _renderers[r];
                if (renderer == null) continue;
                int start = _materialStart[r], count = _materialCount[r];
                for (int m = 0; m < count; m++)
                {
                    if (rest && !tinted)
                    {
                        renderer.SetPropertyBlock(null, m);
                        continue;
                    }
                    if (tinted)
                    {
                        _block.SetColor(_colorId, _tints[start + m]);
                        _block.SetColor(_baseColorId, _tints[start + m]);
                    }
                    _block.SetColor(_emissionId, MalikReviewH6.SurfaceEmission(_emission[start + m], glow, flash));
                    renderer.SetPropertyBlock(_block, m);
                }
            }
        }

        private void SetBodyForced(bool hide)
        {
            if (_body == null) return;
            _body.forceRenderingOff = hide || _bodyForcedPrior;
        }

        private void SetCostumeForced(bool hide)
        {
            if (_costume == null) return;
            for (int i = 0; i < _costume.Length; i++)
                if (_costume[i] != null) _costume[i].forceRenderingOff = hide || _costumeForcedPrior[i];
        }

        /// <summary>Refuses once with one Console warning, restores the procedural body and removes the adapter.</summary>
        private void Fail(string reason)
        {
            if (!_failed && _crowd) LiveCrowd.Fallback(Label, reason);
            else if (!_failed)
            {
                Log.Warn("Live character fallback: " + Label + ": " + reason + " The procedural character stays. Neon Syndicate > Characters > Rebuild live characters prepares the art again.");
                LiveCastNotice.Fallback(Label, reason);
            }
            _failed = true;
            Teardown();
            Destroy(this);
        }

        /// <summary>Restores the procedural body, the costume layers and the animation cadence, and removes the visual.</summary>
        private void Teardown()
        {
            if (_hidingBody)
            {
                SetBodyForced(false);
                SetCostumeForced(false);
                _hidingBody = false;
            }
            ReleaseCadence();
            _ready = false;
            _applied = false;
            if (_crowd) ReleaseCrowdBody();
            else if (_visual != null)
            {
                _visual.SetActive(false);
                Destroy(_visual);
                _visual = null;
            }
            _target = null;
            _offset = null;
            _order = null;
            _renderers = null;
            _costume = null;
            _costumeForcedPrior = null;
            _body = null;
        }
    }
}
