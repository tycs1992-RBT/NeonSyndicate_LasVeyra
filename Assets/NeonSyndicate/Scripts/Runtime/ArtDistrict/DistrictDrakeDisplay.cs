using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A non-combat visual exhibit for the demonstration district. Uses the upgraded gameplay species mesh and pose
    /// solver without registering an enemy, changing quests, adding navigation obstacles or drawing any UI.
    /// Place its root on a broad existing plinth and call Initialize after the district materials are available.
    /// </summary>
    public sealed class DistrictDrakeDisplay : MonoBehaviour
    {
        public CreatureRig Rig { get; private set; }
        private float _clock;
        private float _pending;

        /// <summary>Place one cosmetic drake. This creates no companion ownership, combat AI or map marker.</summary>
        public static DistrictDrakeDisplay Attach(Transform parent, Vector3 worldPosition, float yaw)
        {
            GameObject root = new GameObject("Quartz Drake - Crystal Court Exhibit");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0f, yaw, 0f));
            DistrictDrakeDisplay display = root.AddComponent<DistrictDrakeDisplay>();
            try
            {
                display.Initialize(Game.Seed, Settings.Graphics >= GraphicsPreset.High ? 2 : 1);
                return display;
            }
            catch
            {
                Object.Destroy(root);
                throw;
            }
        }

        private void Initialize(int seed, int detail)
        {
            if (Rig != null) return;
            CreatureLook look = CreatureLooks.For("quartz_drake", seed);
            CreatureBody body = CreatureBuilder.Build(look, Mathf.Clamp(detail, 0, 2));
            Rig = CreatureRig.Build(gameObject, body, null, detail > 0);
            foreach (Collider collider in Rig.Colliders)
                if (collider != null) collider.enabled = false;
            CreaturePoseInput pose = RestInput();
            Rig.Pose.Solve(ref pose, 1f / 60f);
            Rig.ApplyPose();
        }

        private CreaturePoseInput RestInput()
        {
            return new CreaturePoseInput
            {
                RootPosition = transform.position,
                Yaw = transform.eulerAngles.y,
                Grounded = true,
                Flying = false,
                Action = CreatureAction.None,
                ActionT = -1f,
                Windup = 0.35f,
                Strike = 0.65f,
                Time = _clock
            };
        }

        private void LateUpdate()
        {
            if (Rig == null || Rig.Renderer == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Transform viewer = Game.Cam != null ? Game.Cam.transform : null;
            float distanceSq = viewer != null ? (viewer.position - transform.position).sqrMagnitude : 0f;
            Rig.Renderer.enabled = distanceSq < 140f * 140f;
            if (!Rig.Renderer.enabled) return;
            _clock += dt;
            _pending += dt;
            // Near inspection stays fluid. The distant sculpture updates its bones only ten times per second.
            float interval = distanceSq > 45f * 45f ? 0.1f : (distanceSq > 22f * 22f ? 1f / 30f : 0f);
            if (_pending < interval) return;
            float step = Mathf.Min(0.1f, _pending);
            _pending = 0f;
            CreaturePoseInput pose = RestInput();
            float cycle = Mathf.Repeat(_clock, 26f);
            if (cycle > 11f && cycle < 15f)
            {
                // This is a pose demonstration only: no Creature component, combat damage, roar audio or loot.
                pose.Action = CreatureAction.Roar;
                pose.ActionT = (cycle - 11f) / 4f;
            }
            else if (viewer != null && distanceSq < 18f * 18f)
            {
                pose.HasLookTarget = true;
                pose.LookTarget = viewer.position;
            }
            Rig.Pose.Solve(ref pose, step);
            Rig.ApplyPose();
        }
    }
}
