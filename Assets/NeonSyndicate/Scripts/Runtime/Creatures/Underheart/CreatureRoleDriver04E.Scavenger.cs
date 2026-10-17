using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Ember Scavenger's role (Harasser): it holds its darts back while the player looks straight at it (never longer
    /// than four seconds, so a staring contest does not stall the fight), runs off a few metres after every blow to circle
    /// in again with the pack, and breaks and runs once when badly hurt before coming back. Its attacks are the plain
    /// table (ember nip, darting slash); this is the readable rhythm around them.
    /// </summary>
    public sealed partial class CreatureRoleDriver04E
    {
        /// <summary>Degrees off the player's view inside which a scavenger feels watched.</summary>
        public const float WatchAngle = 35f;
        /// <summary>Metres within which being watched holds a scavenger back.</summary>
        public const float WatchRange = 12f;
        /// <summary>Longest a scavenger stays held back by being watched (seconds).</summary>
        public const float WatchPatience = 4f;

        private float _watchedFor;
        private float _nextChitter;

        private void ThinkScavenger(bool engaged)
        {
            if (!HuntingPlayer)
            {
                _watchedFor = 0f;
                return;
            }
            PlayerController p = Game.Player;
            if (p == null) return;
            // Hit and run: a blow just finished, so it darts back out to circle again.
            if (_wasAttacking && !Body.RoleAttacking && !Body.IsStaggered && LastAttackDamaging)
            {
                Body.RoleRetreat(PlayerGround, Random.Range(1.1f, 1.7f));
                _watchedFor = 0f;
                return;
            }
            if (!_retreated && HealthFraction < Def.RetreatHealth)
            {
                _retreated = true;
                Body.RoleRetreat(PlayerGround, Def.RetreatSeconds);
                Underheart04ESounds.Play(UhSfx.ScavengerChitter, Body.HeadPosition, 0.8f, 1.25f);
                return;
            }
            if (Game.Cam != null && !Body.RoleAttacking)
            {
                Vector3 to = MathUtil.Flat(Body.transform.position - p.transform.position);
                float d = to.magnitude;
                float angle = d > 0.01f ? Vector3.Angle(Game.Cam.FlatForward, to) : 0f;
                bool watched = UnderheartRoles04E.HarasserHesitates(angle, d, WatchAngle, WatchRange);
                if (watched && _watchedFor < WatchPatience)
                {
                    _watchedFor += _thinkDt;
                    Body.RoleDelayAttacks(0.45f);
                }
                else if (!watched) _watchedFor = Mathf.Max(0f, _watchedFor - _thinkDt * 2f);
            }
            if (Time.time >= _nextChitter && Random.value < 0.15f)
            {
                _nextChitter = Time.time + Random.Range(4f, 8f);
                Underheart04ESounds.Play(UhSfx.ScavengerChitter, Body.HeadPosition, 0.55f, Random.Range(0.9f, 1.15f));
            }
        }
    }
}
