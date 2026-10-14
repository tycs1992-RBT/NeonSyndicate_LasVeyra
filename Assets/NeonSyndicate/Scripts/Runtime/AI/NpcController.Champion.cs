using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Champions: named enemies with one to three affixes (Armored, Shielded, Swift, Volatile, Vampiric,
    /// Regenerating), a purple aura, much more health than a Shard-Touched elite, and a guaranteed Epic or better gun plus
    /// a weapon core when they fall. The loot armory promotes them and drives UpdateChampion every frame.
    /// </summary>
    public sealed partial class NpcController
    {
        /// <summary>True for a champion (it also counts as Elite for every other rule).</summary>
        public bool IsChampion { get; private set; }
        /// <summary>Bit per ChampionAffix.</summary>
        public int ChampionAffixes { get; private set; }

        private GameObject _champAura;
        private Light _champLight;

        public bool HasAffix(ChampionAffix a)
        {
            return (ChampionAffixes & (1 << (int)a)) != 0;
        }

        /// <summary>Promotes this enemy to a champion with the given name and affixes.</summary>
        public void MakeChampion(string name, int affixMask)
        {
            if (IsChampion || IsDead || IsCreature) return;
            IsChampion = true;
            ChampionAffixes = affixMask;
            Elite = true;
            if (!string.IsNullOrEmpty(name)) DisplayName = name;
            Health.MaxHealth *= ChampionRules.HealthMultiplier;
            Health.Health = Health.MaxHealth;
            MaxPoise *= 3.5f;
            Poise = MaxPoise;
            MeleeDamageScale = Mathf.Max(MeleeDamageScale, 1.45f);
            Accuracy *= 0.8f;
            Courage = 1f;
            CashDrop = Mathf.Max(CashDrop, 40) * 6;
            if (Anim != null) Anim.EliteGlow = 1f;
            if (HasAffix(ChampionAffix.Armored))
            {
                Health.DamageScale *= 0.7f;
                Health.MaxArmor = 200f;
                Health.Armor = 200f;
            }
            if (HasAffix(ChampionAffix.Shielded))
            {
                Health.MaxArmor = Mathf.Max(Health.MaxArmor, 160f);
                Health.Armor = Health.MaxArmor;
            }
            if (HasAffix(ChampionAffix.Swift))
            {
                RunSpeed *= 1.35f;
                WalkSpeed *= 1.3f;
            }
            BuildChampionAura();
            RefreshArmor();
        }

        private void BuildChampionAura()
        {
            Color c = GearSystem.RarityColor(Rarity.Epic);
            _champAura = new GameObject("Champion Aura");
            _champAura.transform.SetParent(transform, false);
            GameObject ring = Shapes.MeshObject("Ring", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(c.r, c.g, c.b, 0.45f)) }, _champAura.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ring.transform.localScale = new Vector3(1.6f, 0.04f, 1.6f);
            GameObject column = Shapes.MeshObject("Column", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(c.r, c.g, c.b, 0.12f)) }, _champAura.transform, false);
            column.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            column.transform.localScale = new Vector3(1.1f, 2.2f, 1.1f);
            GameObject lgo = new GameObject("Champion Light");
            lgo.transform.SetParent(_champAura.transform, false);
            lgo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            _champLight = lgo.AddComponent<Light>();
            _champLight.type = LightType.Point;
            _champLight.color = c;
            _champLight.range = 5f;
            _champLight.intensity = 1.5f;
            _champLight.shadows = LightShadows.None;
        }

        /// <summary>Champion upkeep each frame: the aura pulse, the shield refill and regeneration.</summary>
        public void UpdateChampion(float dt)
        {
            if (!IsChampion || IsDead) return;
            if (_champLight != null) _champLight.intensity = 1.2f + Mathf.Sin(Time.time * 3f) * 0.5f;
            float quiet = Time.time - Health.LastDamageTime;
            if (HasAffix(ChampionAffix.Shielded) && quiet > 4f && Health.Armor < Health.MaxArmor)
                Health.Armor = Mathf.Min(Health.MaxArmor, Health.Armor + Health.MaxArmor * 0.25f * dt);
            if (HasAffix(ChampionAffix.Regenerating) && quiet > 3f && Health.Health < Health.MaxHealth)
                Health.Heal(Health.MaxHealth * 0.025f * dt);
        }

        /// <summary>Called from the death block: the aura fades and a Volatile champion blows up a moment later.</summary>
        private void OnChampionDied()
        {
            if (_champAura != null) Destroy(_champAura);
            _champAura = null;
            _champLight = null;
            if (HasAffix(ChampionAffix.Volatile)) StartCoroutine(VolatileBlast());
        }

        private IEnumerator VolatileBlast()
        {
            Vector3 at = transform.position + Vector3.up * 0.6f;
            for (int i = 0; i < 4; i++)
            {
                if (Game.Fx != null) Game.Fx.Flash(at, new Color(1f, 0.45f, 0.15f), 3f + i, 6f, 0.15f);
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Beep, at, 0.6f, 1f + i * 0.15f, 40f);
                yield return new WaitForSeconds(0.3f);
            }
            if (Game.Explosions != null) Game.Explosions.Explode(at, 4.5f, 70f, gameObject, ExplosionKind.Grenade);
        }
    }
}
