using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A running Underheart boss fight. Done on a confirmed victory; Failed when aborted or the arena could not be built.</summary>
    public sealed class BossFightHandle
    {
        public string BossId = "";
        public bool Done;
        public bool Failed;
        public string Status = "";
        public float Health01 = 1f;
        public int Phase;
        public Action Abort = () => { };
    }

    /// <summary>A mechanism of Break the Extraction Choir (valve defense, reflector beam, maintenance escort).</summary>
    public sealed class MechanismHandle
    {
        public string Id = "";
        public bool Done;
        public bool Failed;
        public string Objective = "";
        public Action Abort = () => { };
    }

    /// <summary>
    /// Game.UnderBosses: the Valve Guardian, the Cinder Matron, the Vaultbreaker and the Kiln Regent (three readable
    /// phases, punish windows, finite adds, a quiet aftermath, first-win rewards through the reward ledger), plus the
    /// Choir mechanisms. FOUNDATION STUB: the BOSSES work stream replaces the bodies and keeps every signature.
    /// </summary>
    public sealed partial class UnderheartBosses : MonoBehaviour
    {
        /// <summary>Raised on a confirmed victory (boss id).</summary>
        public event Action<string> Defeated;

        public void Init() { }

        public BossFightHandle Begin(string bossId, Vector3 arena, float yaw, float radius, out string reason)
        {
            reason = "Underheart bosses are not implemented in this build.";
            return new BossFightHandle { BossId = bossId ?? "", Failed = true, Status = reason };
        }

        /// <summary>True once the boss has been beaten at least once (its first-win token exists).</summary>
        public bool EverDefeated(string bossId) { return false; }

        /// <summary>True while any Underheart boss fight runs.</summary>
        public bool FightActive { get { return false; } }

        public MechanismHandle BeginMechanism(string mechanismId, Vector3 anchor, float yaw, out string reason)
        {
            reason = "Underheart mechanisms are not implemented in this build.";
            return new MechanismHandle { Id = mechanismId ?? "", Failed = true, Objective = reason };
        }

        private void RaiseDefeated(string id)
        {
            if (Defeated == null) return;
            foreach (Delegate h in Defeated.GetInvocationList())
            {
                try { ((Action<string>)h)(id); }
                catch (Exception e) { Log.Exception("UnderBosses Defeated listener", e); }
            }
        }
    }

    /// <summary>Reset hub for every static the BOSSES work stream adds.</summary>
    public static class UnderBossStatics
    {
        public static void ResetStatics() { }
    }
}
