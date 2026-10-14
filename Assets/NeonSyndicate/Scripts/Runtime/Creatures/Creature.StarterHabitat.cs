using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Local sanctuary behavior on the existing creature brain and pose solver, never a second combat AI.</summary>
    public sealed partial class Creature
    {
        private CharacterBase _habitatObserver;
        private float _habitatObserveUntil;
        public bool PracticeDefeated { get; private set; }
        public bool PracticePlayerDefeated { get; private set; }

        /// <summary>Remove a trial's pending strike and telegraph before its deferred director despawn.</summary>
        public void StopHabitatPractice()
        {
            if (Options == null || !Options.PracticeTarget) return;
            Target = null;
            CancelAttack();
            ReleaseTokens();
            SetMode(CreatureMode.Idle);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// The one practice jackal's bite/pounce damage runs through normal dodge and armor, with a final nonlethal
        /// hook only for this synchronous hit. The player's usual last-stand callback is restored in every case.
        /// </summary>
        private bool DealPracticeHit(CharacterBase target, DamageInfo info)
        {
            if (Options == null || !Options.PracticeTarget || !target.IsPlayer) return false;
            if (PracticeDefeated || PracticePlayerDefeated) return true;
            CharacterHealth health = target.Health;
            System.Func<DamageInfo, bool> previous = health.OnLethal;
            bool defeated = false;
            info.Amount = Mathf.Min(info.Amount, Mathf.Max(0f, health.Health - 1f));
            health.OnLethal = (DamageInfo hit) => { defeated = true; return true; };
            try { health.TakeDamage(info); }
            finally { health.OnLethal = previous; }
            if (!health.LastBlocked && (defeated || health.Health <= 1.001f))
            {
                PracticePlayerDefeated = true;
                Target = null;
                CancelAttack();
                ReleaseTokens();
                SetMode(CreatureMode.Idle);
            }
            return true;
        }

        private void LimitPracticeDamage(ref DamageInfo info)
        {
            if (Options == null || !Options.PracticeTarget || Health == null) return;
            float scale = Mathf.Max(0f, Health.DamageScale);
            if (info.Amount * scale < Health.Health) return;
            // Burning applies DamageScale after the filter and bypasses OnLethal. Include that multiplier here.
            info.Amount = Mathf.Min(info.Amount, Mathf.Max(0f, Health.Health - 1f) / Mathf.Max(0.0001f, scale));
            SurvivePracticeHit(info);
        }

        // CharacterHealth invokes this after resonance and all damage multipliers. The burn path is covered above.
        private bool SurvivePracticeHit(DamageInfo info)
        {
            PracticeDefeated = true;
            Target = null;
            CancelAttack();
            ReleaseTokens();
            SetMode(CreatureMode.Idle);
            return true;
        }

        public bool CalmHabitatResident
        {
            get { return Options != null && Options.CalmUntilProvoked && !Provoked; }
        }

        /// <summary>Acknowledge the observer with the existing head-look solver for eight seconds.</summary>
        public void ObserveInHabitat(CharacterBase observer)
        {
            if (!CalmHabitatResident || !Ready || _dead || observer == null) return;
            _habitatObserver = observer;
            _habitatObserveUntil = Time.time + 8f;
            _hasGoal = false;
            _wanderTimer = 8f;
        }

        private bool ThinkHabitat(float dt)
        {
            if (!CalmHabitatResident) return false;
            Target = null;
            CancelAttack();
            SetMode(CreatureMode.Wander);
            _wanderTimer -= dt;
            if (_wanderTimer > 0f) return true;
            _wanderTimer = UnityEngine.Random.Range(5f, 9f);
            _hasGoal = false;
            // Compact ground-level habitat; residents cannot patrol across roads or wander away from the tutorial.
            for (int attempt = 0; attempt < 4; attempt++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.25f;
                Vector3 goal = Home + new Vector3(offset.x, 0f, offset.y);
                float margin = Mathf.Clamp(Radius * 0.35f, 0.7f, 1.4f);
                if (Game.Layout != null && (Game.Layout.IsInsideBuilding(goal, margin) || Game.Layout.RoadAt(goal, margin) != null)) continue;
                float ground;
                if (!CreatureDirector.GroundAt(goal, 2f, out ground) || Mathf.Abs(ground - Home.y) > 0.65f) continue;
                goal.y = ground;
                if (Physics.CheckCapsule(goal + Vector3.up * (margin + 0.1f), goal + Vector3.up * (margin + 1.2f),
                    margin, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                SetGoal(goal, _sp != null ? _sp.WalkSpeed * 0.45f : 0.65f);
                break;
            }
            return true;
        }

        private void ApplyHabitatLook(ref CreaturePoseInput pose)
        {
            if (_dead || !CalmHabitatResident || _habitatObserver == null || Time.time > _habitatObserveUntil) return;
            pose.HasLookTarget = true;
            pose.LookTarget = _habitatObserver.ChestPosition;
        }
    }
}
