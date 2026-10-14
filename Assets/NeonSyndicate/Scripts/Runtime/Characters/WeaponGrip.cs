using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Where a held weapon sits in the hand. Weapon models have their origin at the grip with +Z along the barrel and
    /// +Y up; two-handed models carry a child named Foregrip where the support hand holds them. WeaponController calls
    /// Attach whenever it parents a model to a hand, so the hand pose and the weapon placement live in one place. The
    /// placement table and the hand frame it assumes are in WeaponGrip.Table.cs (pure, shared with the pose solver).
    /// </summary>
    public static partial class WeaponGrip
    {
        /// <summary>Name of the child transform on two-handed weapon models where the support hand goes.</summary>
        public const string ForegripName = "Foregrip";

        /// <summary>Places a weapon model, already parented to the hand bone, in the palm (see the attach table).</summary>
        public static void Attach(Transform model, float rigScale, HoldType hold, bool leftHand)
        {
            if (model == null) return;
            float s = rigScale > 0f ? rigScale : 1f;
            if (leftHand)
            {
                model.localPosition = GripPositionLeft(hold, s);
                model.localRotation = GripRotationLeft(hold);
                model.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                model.localPosition = GripPosition(hold, s);
                model.localRotation = GripRotation(hold);
                model.localScale = Vector3.one;
            }
        }

        /// <summary>The model's foregrip point, or null when the model has none (one-handed weapons).</summary>
        public static Transform FindForegrip(Transform model)
        {
            return model != null ? model.Find(ForegripName) : null;
        }
    }
}
