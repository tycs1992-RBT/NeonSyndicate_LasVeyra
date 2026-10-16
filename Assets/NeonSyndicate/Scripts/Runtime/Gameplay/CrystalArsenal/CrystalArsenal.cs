using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The crystal arsenal's save block (owner: arsenal and rewards). Append only, like every save block.</summary>
    [Serializable]
    public sealed class CrystalArsenalSave
    {
        public int Version = 1;
    }

    /// <summary>
    /// The 04D crystal arsenal (Game.Arsenal, owner: arsenal and rewards): the guaranteed starter offer, practice lane,
    /// introductory foci, durable reward entitlements (pending claims survive a full backpack, save and reload), boss
    /// first-win signatures and retroactive claims for saves that already beat a boss.
    /// </summary>
    public sealed partial class CrystalArsenal : MonoBehaviour, IWorldSystem
    {
        public void Init(CityLayout layout)
        {
            WorldSystems.Register(this);
        }

        public void ResetAll() { }
        public void Capture(SaveData d) { }
        public void Restore(SaveData d) { }

        /// <summary>A boss died to the player (CreatureDirector.OnBossDied). Grants its first-win signature once.</summary>
        public void OnBossVictory(string bossId, Vector3 at) { }

        /// <summary>
        /// Grants a durable reward entitlement once per token (encounters call this on a real victory). Returns true when
        /// a new entitlement was created (granted now or left pending for a full backpack).
        /// </summary>
        public bool GrantReward(string token, int unique, string source)
        {
            return false;
        }
    }

    /// <summary>True while the player fires at the crystal practice targets: the shots are not combat noise.</summary>
    public static class CrystalPractice
    {
        public static bool SuppressNoise { get; set; }

        public static void ResetStatics()
        {
            SuppressNoise = false;
        }
    }

    /// <summary>One reset hub for every static the arsenal and rewards pass adds (called from Game.ResetStatics).</summary>
    public static class CrystalArsenalStatics
    {
        public static void ResetStatics()
        {
            CrystalPractice.ResetStatics();
        }
    }
}
