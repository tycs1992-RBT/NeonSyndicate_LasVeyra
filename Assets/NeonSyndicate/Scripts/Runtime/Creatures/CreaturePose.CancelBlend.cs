using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Visual-only crossfade for an action that is cut short (Phase 5 of the visual upgrade). Every creature action
    /// eases itself in and out (Phases.W), so an action that runs to its end never snaps; one that is cancelled part
    /// way (a stagger, a brain decision, a boss phase change) used to drop from its full pose to the gait in a single
    /// frame. When the action identity changes or restarts while the previous one still carried weight, the last output
    /// pose is kept and the new output is blended from it over CancelBlendSeconds, by the weight the cut action had.
    /// Only LocalRotations and LocalPositions (what the rig copies to its bones) are blended; the solver's own state,
    /// the action timers the runtime reads for hit frames and ModelPosition are untouched. Reset clears it.
    /// </summary>
    public sealed partial class CreaturePose
    {
        /// <summary>How long a cut action takes to fade into whatever follows it.</summary>
        public const float CancelBlendSeconds = 0.12f;
        /// <summary>An action lighter than this when cut is not worth a fade.</summary>
        private const float CancelMinWeight = 0.05f;

        private Quaternion[] _cutRot;
        private Vector3[] _cutPos;
        private float _cutWeight;
        private float _cutT;
        private bool _prevActionValid;
        private CreatureAction _prevAction;
        private float _prevActionT;
        private float _prevActionW;

        /// <summary>Remaining strength of the current cancel fade, 0 when none is running (diagnostics and tests).</summary>
        public float CancelBlendWeight
        {
            get
            {
                if (_cutWeight <= 0f) return 0f;
                return _cutWeight * (1f - E3(_cutT / CancelBlendSeconds));
            }
        }

        private void CancelBlendReset()
        {
            _cutWeight = 0f;
            _cutT = 0f;
            _prevActionValid = false;
        }

        /// <summary>Before the solve: notices an action that was cut and keeps last frame's output to fade from.</summary>
        private void CancelBlendBegin(ref CreaturePoseInput input)
        {
            bool active = input.Action != CreatureAction.None && input.ActionT >= 0f;
            float w = active ? PhasesOf(ref input).W : 0f;
            if (_prevActionValid && _prevActionW > CancelMinWeight && _prevAction != CreatureAction.Death)
            {
                bool changed = input.Action != _prevAction;
                bool restarted = !changed && active && input.ActionT < _prevActionT - 0.1f;
                if (changed || restarted)
                {
                    if (_cutRot == null)
                    {
                        _cutRot = new Quaternion[_n];
                        _cutPos = new Vector3[_n];
                    }
                    // Keep what is on screen now, whether that is a pose or an earlier fade still in flight.
                    for (int i = 0; i < _n; i++)
                    {
                        _cutRot[i] = LocalRotations[i];
                        _cutPos[i] = LocalPositions[i];
                    }
                    _cutWeight = Mathf.Max(_prevActionW, CancelBlendWeight);
                    _cutT = 0f;
                }
            }
            _prevActionValid = true;
            _prevAction = active ? input.Action : CreatureAction.None;
            _prevActionT = active ? input.ActionT : -1f;
            _prevActionW = w;
        }

        /// <summary>After WriteLocals: blends the written output from the kept pose while the fade runs.</summary>
        private void CancelBlendApply(float dt)
        {
            if (_cutWeight <= 0f) return;
            _cutT += dt;
            float k = CancelBlendWeight;
            if (k <= 0.001f || _cutT >= CancelBlendSeconds)
            {
                _cutWeight = 0f;
                return;
            }
            for (int i = 0; i < _n; i++)
            {
                LocalRotations[i] = QMath.Slerp(LocalRotations[i], _cutRot[i], k);
                LocalPositions[i] = Vector3.Lerp(LocalPositions[i], _cutPos[i], k);
            }
        }
    }
}
