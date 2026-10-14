using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Breakable parts on a non-humanoid creature (CreatureParts holds them): what a broken part changes in the fight.
    /// Claws and fangs halve melee reach and damage, torn wings ground a flyer for good, an exposed core makes weak-point
    /// criticals hit harder, a broken gland or crown gem stops breath, gaze and spit, cracked ring plates slow it.
    /// </summary>
    public sealed partial class Creature
    {
        private CreatureParts _parts;

        /// <summary>This creature's breakable parts (null before it is built, or for a species without any).</summary>
        public CreatureParts Parts { get { return _parts; } }

        private bool PartBroken(PartEffect e)
        {
            return _parts != null && _parts.Has(e);
        }

        /// <summary>Melee reach and damage scale from broken claws, fangs or talons.</summary>
        private float PartMelee { get { return _parts != null ? CreaturePartRules.MeleeScale(_parts.BrokenEffects) : 1f; } }

        /// <summary>Movement scale from cracked legs or ring plates.</summary>
        private float PartSpeed { get { return _parts != null ? CreaturePartRules.SpeedScale(_parts.BrokenEffects) : 1f; } }

        /// <summary>True for melee shapes (bites, claws, pounces, dives): the ones broken claws weaken.</summary>
        private static bool IsMeleeShape(CreatureAttack a)
        {
            return a.Shape == AttackShape.Arc || a.Shape == AttackShape.Leap || a.Shape == AttackShape.Line;
        }

        /// <summary>A broken gland or crown gem stops breath, gaze and thrown attacks.</summary>
        private bool PartBlocks(CreatureAttack a)
        {
            if (!PartBroken(PartEffect.NoRanged)) return false;
            return a.Shape == AttackShape.Cone || a.Shape == AttackShape.Projectile;
        }

        /// <summary>Called by CreatureParts when one of its parts breaks: the stagger, and torn wings bring it down.</summary>
        public void OnPartBroken(PartDef def)
        {
            if (_dead || def == null) return;
            Stagger(IsBossLike ? CreaturePartRules.BossBreakStagger : CreaturePartRules.BreakStagger);
            if ((def.Effects & PartEffect.Grounded) != 0) ForceLanding(true);
        }
    }
}
