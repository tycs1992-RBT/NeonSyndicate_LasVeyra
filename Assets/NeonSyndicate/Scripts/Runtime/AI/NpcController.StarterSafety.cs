namespace NeonSyndicate
{
    public sealed partial class NpcController
    {
        /// <summary>Authored missions, quests and events retain explicit aggression regardless of welcome bounds.</summary>
        private bool IsAmbientGang
        {
            get { return Role == NpcRole.Gang && !Persistent && string.IsNullOrEmpty(MissionTag) && !IsCreature; }
        }

        private bool GangHostileToPlayer()
        {
            float heat = GangId >= 0 && GangId < Gangs.Heat.Length ? Gangs.Heat[GangId] : 0f;
            bool protectedArea = IsAmbientGang && StarterSafety.BlockAmbientAggression(Game.PlayerPosition);
            return StarterSafetyRules.GangHostile(!IsAmbientGang, protectedArea, Hostile,
                Target != null && Target.IsPlayer, heat);
        }

        private bool IgnoreWelcomeThreat(ThreatEvent threat)
        {
            if (!IsAmbientGang || !StarterSafety.BlockAmbientAggression(Game.PlayerPosition)) return false;
            // Practice noise never recruits ambient crews. Actual hits still use OnAttackedBy and AlertGang.
            return threat.Kind == ThreatKind.Aim || StarterHabitat.SuppressPracticeNoise;
        }

        private void StopPassiveGangPursuit()
        {
            if (!IsAmbientGang || Hostile || Target == null || !Target.IsPlayer ||
                !StarterSafety.BlockAmbientAggression(Game.PlayerPosition)) return;
            // Heat-only acquisition outside a refuge must not keep firing after its hostility query turns false.
            // Actual retaliation sets Hostile, so entering a welcome area does not cancel a fight the player chose.
            Target = null;
            CancelAttack();
            CombatDirector.ReleaseShotToken(this);
            if (State == NpcState.Fight || State == NpcState.Chase) SetState(NpcState.Idle);
        }

        /// <summary>Humanoid ecology uses the same welcome policy as non-humanoid wildlife.</summary>
        private bool QuietAmbientCreature
        {
            get
            {
                if (!IsCreature) return false;
                return StarterSafetyRules.QuietAmbientCreature(CreatureAmbient, Persistent, CreatureProvoked,
                    CreatureOptions != null && CreatureOptions.Hostile, !string.IsNullOrEmpty(MissionTag),
                    StarterSafety.BlockAmbientAggression(Game.PlayerPosition));
            }
        }
    }
}
