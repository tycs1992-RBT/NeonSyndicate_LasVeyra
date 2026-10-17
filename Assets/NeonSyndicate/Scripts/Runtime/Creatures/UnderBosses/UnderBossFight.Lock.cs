using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// UnderBossFight, 04E integration with the shared combat feedback: the lock-on stand-in (UBossLockProxy04E) whose
    /// aim point follows the best part to strike (UBossLockRules04E), and floating damage numbers for every hit the
    /// player's side lands on the body (the player's own, a companion's and a fused vehicle's, all credited to the
    /// player). The body is not a character, so CharacterHealth never reported those numbers on its own. The same hook
    /// feeds the Resonance loop (UnderBossFight.Resonance) before the number is shown.
    /// </summary>
    public abstract partial class UnderBossFight
    {
        private UBossLockProxy04E _lockProxy;
        private Transform _lockAim;
        private Transform _lockHead;
        private UBossPartTag _lockTag;
        private float _lockEvalAt;
        private UBossPartTag _lastHitTag;
        private readonly List<UBossPart> _presentKinds = new List<UBossPart>(8);

        /// <summary>The lock-on stand-in of this fight (null before setup or after cleanup).</summary>
        public UBossLockProxy04E LockProxy { get { return _lockProxy; } }

        /// <summary>The part the lock point is on right now (null: the body's centre).</summary>
        public UBossPartTag LockPart { get { return _lockTag; } }

        /// <summary>Builds the stand-in under the body and hooks the damage numbers. Called once at the end of Setup.</summary>
        private void SetupCombatFeedback()
        {
            if (Body == null || Health == null) return;
            GameObject go = new GameObject("Lock point");
            go.transform.SetParent(Body, false);
            _lockProxy = go.AddComponent<UBossLockProxy04E>();
            _lockProxy.Fight = this;
            _lockProxy.Health = Health;
            _lockProxy.DisplayName = Profile != null ? Profile.Title : "Underheart boss";
            float reach, top;
            MeasureBody(out reach, out top);
            _lockProxy.BodyRadius = UBossLockRules04E.Radius(reach);
            _lockProxy.BodyHeight = UBossLockRules04E.Height(top);
            // The aim and head points live on the fight root so they can follow any part in world space.
            _lockAim = new GameObject("Lock aim").transform;
            _lockAim.SetParent(transform, false);
            _lockHead = new GameObject("Lock head").transform;
            _lockHead.SetParent(transform, false);
            _lockProxy.AimAnchor = _lockAim;
            _lockProxy.HeadAnchor = _lockHead;
            UpdateLockPoint(Time.time, true);
            Health.Damaged += OnBodyDamaged;
        }

        /// <summary>Unhooks the damage numbers (cleanup). The stand-in goes with the body.</summary>
        private void ReleaseCombatFeedback()
        {
            if (Health != null) Health.Damaged -= OnBodyDamaged;
            _lockTag = null;
            _lastHitTag = null;
        }

        /// <summary>The farthest flat reach and the top of the body's hit volumes, measured from its feet.</summary>
        private void MeasureBody(out float reach, out float top)
        {
            reach = 0f;
            top = 0f;
            Vector3 feet = Body.position;
            for (int i = 0; i < _parts.Count; i++)
            {
                UBossPartTag t = _parts[i];
                if (t == null || t.Collider == null) continue;
                Bounds b = t.Collider.bounds;
                Vector3 c = b.center;
                Vector3 e = b.extents;
                float flat = MathUtil.FlatDistance(c, feet) + Mathf.Max(e.x, e.z);
                if (flat > reach) reach = flat;
                float up = b.max.y - feet.y;
                if (up > top) top = up;
            }
        }

        /// <summary>Moves the lock aim to the best part to strike now (re-chosen a few times a second), the head above it.</summary>
        private void UpdateLockPoint(float now, bool force)
        {
            if (_lockProxy == null || _lockAim == null) return;
            if (force || now >= _lockEvalAt)
            {
                _lockEvalAt = now + UBossLockRules04E.Interval;
                ChooseLockPart(now);
            }
            bool tagOk = _lockTag != null && _lockTag.Collider != null && _lockTag.Collider.enabled && _lockTag.gameObject.activeInHierarchy;
            _lockAim.position = tagOk ? _lockTag.Collider.bounds.center : BodyCenter;
            _lockHead.position = HeadPoint + Vector3.up * 0.4f;
        }

        private void ChooseLockPart(float now)
        {
            _presentKinds.Clear();
            for (int i = 0; i < _parts.Count; i++)
            {
                UBossPartTag t = _parts[i];
                if (t == null || t.Collider == null || !t.Collider.enabled || !t.gameObject.activeInHierarchy) continue;
                if (!_presentKinds.Contains(t.Part)) _presentKinds.Add(t.Part);
            }
            UBossDamageState s = DamageState(null);
            if (Window.IsOpen(now))
            {
                s.WindowOpen = true;
                s.WindowFocus = Window.Focus;
                s.WindowMul = Window.Multiplier;
            }
            UBossPart kind = UBossLockRules04E.BestKind(Profile.Id, _presentKinds, s);
            if (kind == UBossPart.Body)
            {
                _lockTag = null;
                return;
            }
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            Vector3 eye = cam != null ? cam.transform.position : PlayerAim;
            Vector3 fwd = cam != null ? cam.transform.forward : Body.position - PlayerAim;
            UBossPartTag best = null;
            float bestAngle = float.MaxValue, currentAngle = float.MaxValue;
            for (int i = 0; i < _parts.Count; i++)
            {
                UBossPartTag t = _parts[i];
                if (t == null || t.Part != kind || t.Collider == null || !t.Collider.enabled || !t.gameObject.activeInHierarchy) continue;
                float a = Vector3.Angle(fwd, t.Collider.bounds.center - eye);
                if (t == _lockTag) currentAngle = a;
                if (a < bestAngle)
                {
                    bestAngle = a;
                    best = t;
                }
            }
            if (_lockTag != null && _lockTag.Part == kind && currentAngle < float.MaxValue && UBossLockRules04E.KeepCurrent(currentAngle, bestAngle)) return;
            _lockTag = best;
        }

        /// <summary>
        /// A hit landed on the body (after the fight's filter): a floating number where it struck, per part, yellow on a
        /// weak point or an open core. Only the player's side reaches here (the filter refuses everything else).
        /// </summary>
        private void OnBodyDamaged(DamageInfo info)
        {
            // 04E: the Resonance loop first (burst, crystal energy, marks), so it runs even if the number cannot show.
            ResonanceState burst = TakePendingBurst();
            if (info.FromPlayer && Health != null) ResonanceAfterHit(info, burst, Health.LastApplied, !Health.IsAlive);
            try
            {
                if (!info.FromPlayer || Health == null) return;
                float applied = Health.LastApplied;
                if (!(applied > 0f)) return;
                int id = _lastHitTag != null ? _lastHitTag.GetInstanceID() : GetInstanceID();
                Vector3 at = MathUtil.IsFinite(info.Point) && info.Point != Vector3.zero ? info.Point : BodyCenter;
                DamageNumbers.ReportAt(id, at + Vector3.up * 0.35f, info, applied, burst != ResonanceState.None);
            }
            catch (Exception e) { Log.Exception("Underheart boss damage number", e); }
        }
    }
}
