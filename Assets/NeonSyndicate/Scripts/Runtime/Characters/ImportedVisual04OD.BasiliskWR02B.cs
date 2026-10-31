using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class ImportedVisual04OD
    {
        private ImportedModel04OD _basiliskModelWR02B;
        private Vector3[] _basiliskBindScalesWR02B;
        private bool _basiliskNativeReviewWR02B;

        public bool BasiliskNativeReviewActiveWR02B { get { return _basiliskNativeReviewWR02B && _ready && _visual != null && _visual.activeInHierarchy; } }

        /// <summary>Hash this actual original body, including its hit declarations. A review of another seed is insufficient.</summary>
        public static string BasiliskBodyFingerprintWR02B(CreatureBody body)
        {
            if (body == null || body.Skeleton == null) return "";
            StringBuilder text = new StringBuilder("WR02B original body contract 1");
            CreatureSkeleton skeleton = body.Skeleton;
            for (int i = 0; i < skeleton.Count; i++)
            {
                Vector3 p = skeleton.LocalPositions[i];
                text.Append('|').Append(skeleton.Names[i]).Append(':').Append(skeleton.Parents[i]).Append(':').Append(p.x.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(p.y.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(p.z.ToString("R", CultureInfo.InvariantCulture));
            }
            Func<Vector3, string> vector = point => point.x.ToString("R", CultureInfo.InvariantCulture) + "," + point.y.ToString("R", CultureInfo.InvariantCulture) + "," + point.z.ToString("R", CultureInfo.InvariantCulture);
            foreach (CreatureHitVolume hit in body.HitVolumes)
                text.Append('|').Append(hit.Bone).Append(':').Append(vector(hit.Center)).Append(':').Append(vector(hit.Axis)).Append(':').Append(hit.Radius.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(hit.Length.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(hit.WeakPoint);
            using (SHA256 sha = SHA256.Create())
            { byte[] value = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())); StringBuilder hex = new StringBuilder(); foreach (byte b in value) hex.Append(b.ToString("x2", CultureInfo.InvariantCulture)); return hex.ToString(); }
        }

#if UNITY_EDITOR
        /// <summary>Editor review only, attached to an existing legal creature after actual isolated native acceptance. Never spawns or enables a registry row.</summary>
        public static bool BeginBasiliskNativeReviewWR02B(Creature creature, ImportedModel04OD candidate, BasiliskNativeReviewWR02B receipt, string sourceFingerprint, string assetFingerprint, out string reason)
        {
            reason = "Native rig, full motion/contact and owner review of this exact body are required. Original creature retained.";
            if (creature == null || !creature.Ready || creature.Species == null || creature.Species.Id != BasiliskSkinRulesWR02B.SpeciesId || creature.CRig == null || creature.IsDead || creature.IsRagdolled || receipt == null || candidate == null || candidate.Enabled) return false;
            if (Game.Root == null || !Game.Root.FreeRoam || Game.State != GameState.Playing || Game.Layout == null || !Game.Layout.GothicCivicH5 || CityLayout.InSettlement04OC(creature.transform.position))
            { reason = "Review requires ordinary revision-10 Free Roam and an existing basilisk outside the protected settlement. No spawn or progression bypass is provided."; return false; }
            if (creature.Parts != null && creature.Parts.Book != null && creature.Parts.Book.BrokenCount > 0) { reason = "Original broken-part presentation must remain visible."; return false; }
            if (!receipt.AllowsReview(sourceFingerprint, assetFingerprint, BasiliskBodyFingerprintWR02B(creature.CRig.Body))) return false;
            ImportedVisual04OD adapter = creature.GetComponent<ImportedVisual04OD>();
            if (adapter != null && adapter._ready) { reason = "Another existing visual adapter already owns this creature."; return false; }
            if (adapter == null) adapter = creature.gameObject.AddComponent<ImportedVisual04OD>();
            adapter._owner = creature; adapter._companion = null; adapter._basiliskModelWR02B = candidate; adapter._basiliskNativeReviewWR02B = true;
            adapter.Initialize();
            reason = adapter._ready ? "Temporary native review uses the same creature, original pose, ground solver, root, hitboxes, damage, loot and part owner. Revert restores the original." : "Imported candidate refused; original restored. Read the first Imported model fallback warning.";
            return adapter._ready;
        }

        public static void EndBasiliskNativeReviewWR02B(Creature creature)
        {
            ImportedVisual04OD adapter = creature != null ? creature.GetComponent<ImportedVisual04OD>() : null;
            if (adapter == null || !adapter._basiliskNativeReviewWR02B) return;
            adapter.Restore(); UnityEngine.Object.Destroy(adapter);
        }
#endif

        private void ValidateBasiliskBindWR02B(Creature creature, ImportedModel04OD model, SkinnedMeshRenderer[] skins)
        {
            if (!_basiliskNativeReviewWR02B) return;
            if (creature == null || creature.CRig == null || model.Identity != "creature:" + BasiliskSkinRulesWR02B.SpeciesId || model.Scale != 1) throw new InvalidOperationException("Basilisk review cannot change species or root scale.");
            string[] names = new string[_target.Length]; int[] parents = new int[_target.Length];
            for (int i = 0; i < _target.Length; i++)
            {
                names[i] = _target[i].name; parents[i] = -1;
                for (Transform parent = _target[i].parent; parent != null && parent != _visual.transform; parent = parent.parent)
                { int found = Array.IndexOf(_target, parent); if (found >= 0) { parents[i] = found; break; } }
            }
            string reason;
            if (!BasiliskSkinRulesWR02B.Mapping(creature.Species.Id, creature.CRig.Body.Skeleton, model.Bones, names, parents, out reason)) throw new InvalidOperationException(reason);
            CreatureSkeleton canonical = CreatureBuilder.Build(CreatureLooks.For(BasiliskSkinRulesWR02B.SpeciesId, 61724), 0).Skeleton;
            _basiliskBindScalesWR02B = new Vector3[_target.Length];
            for (int i = 0; i < _target.Length; i++)
            {
                bool found = false; Matrix4x4 bind = Matrix4x4.identity;
                foreach (SkinnedMeshRenderer skin in skins)
                {
                    int bone = Array.IndexOf(skin.bones, _target[i]);
                    if (bone < 0) continue;
                    Matrix4x4 actual = skin.transform.localToWorldMatrix * skin.sharedMesh.bindposes[bone].inverse;
                    if (found && (bind.GetColumn(3) - actual.GetColumn(3)).sqrMagnitude > .000004f) throw new InvalidOperationException("Conflicting imported bind matrices.");
                    bind = actual; found = true;
                }
                if (!found || (_visual.transform.InverseTransformPoint(bind.GetColumn(3)) - canonical.ModelPosition(i)).sqrMagnitude > .000009f) throw new InvalidOperationException("Imported bind position differs from the authored canonical source at " + names[i]);
                _targetBind[i] = bind.rotation; _basiliskBindScalesWR02B[i] = _target[i].localScale;
            }
        }

        private bool BasiliskReviewLostWR02B()
        {
            if (!_basiliskNativeReviewWR02B) return false;
            Creature creature = _owner as Creature;
            CreatureParts parts = creature != null ? creature.Parts : null;
            // A real broken part must not remain intact on an unmasked imported mesh. Restore the original immediately.
            return !BasiliskSkinRulesWR02B.KeepCandidate(creature != null, creature != null && creature.CRig != null, creature != null && creature.IsRagdolled, false, parts != null ? parts.BrokenEffects : PartEffect.None, parts != null && parts.Book != null ? parts.Book.BrokenCount : 0);
        }

        private void CopyBasiliskScaleWR02B(int index)
        {
            if (_basiliskNativeReviewWR02B && _basiliskBindScalesWR02B != null)
                _target[index].localScale = BasiliskSkinRulesWR02B.LocalScale(_basiliskBindScalesWR02B[index], _source[index].localScale);
        }
    }
}
