using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Transactional replacement of the optional 03A2 shell, retaining all original gameplay geometry.</summary>
    public sealed partial class StrongholdView
    {
        private bool _rebirth04AActive, _rebirth04ALogged, _rebirth04AFailureReported;

        private void TryDecorateRebirth04A(Transform parent,StrongholdSiteSave state)
        {
            _rebirth04AActive = false;
            if (!VisualRebirth04ASettings.Enabled || !VisualRebirth04ASettings.Strongholds) return;
            VisualRebirth04ABatch batch = null;
            GameObject previousShell = null;
            bool previousShellActive = false;
            List<KeyValuePair<MeshRenderer,Material>> replaced = new List<KeyValuePair<MeshRenderer,Material>>();
            try
            {
                TextAsset source = Resources.Load<TextAsset>("VisualRebirth04A/Layouts/" + Definition.id);
                if (source == null) throw new InvalidOperationException("The exact fort composition is unavailable.");
                StrongholdLayout04A layout = JsonUtility.FromJson<StrongholdLayout04A>(source.text);
                if (layout == null) throw new InvalidOperationException("The fort composition is empty.");
                layout.Validate(Definition.id);
                int style = CitadelLayout03A2.Style(Definition.id,state);
                batch = new VisualRebirth04ABatch("Visual Rebirth 04A / " + layout.title,parent,Vector3.zero,style);
                foreach (StrongholdPart04A part in layout.parts) AddRebirthPart(batch,part,style);
                List<StrongholdPart04A> socketParts = StrongholdLayout04A.SocketParts(state.Slots);
                foreach (StrongholdPart04A part in socketParts) AddRebirthPart(batch,part,style);
                foreach (StrongholdCollision04A collision in layout.collisions)
                {
                    GameObject node = new GameObject("04A physical route / " + collision.name);
                    node.transform.SetParent(batch.Transform,false);
                    node.transform.localPosition = StrongholdLayout04A.Vector(collision.position);
                    node.layer = Layers.World;
                    node.AddComponent<BoxCollider>().size = StrongholdLayout04A.Vector(collision.size);
                }
                // Resolve every replacement before mutating any existing renderer. Missing art
                // therefore leaves the fully working 03A2 and 02I visuals and colliders intact.
                Material stone = VisualRebirth04AAssets.Material(style,"stone");
                Material cloth = VisualRebirth04AAssets.Material(style,"cloth");
                Material crystal = VisualRebirth04AAssets.Material(style,"crystal");
                if (stone == null || cloth == null || crystal == null) throw new InvalidOperationException("04A material family is unavailable.");
                MeshRenderer[] originals = parent.GetComponentsInChildren<MeshRenderer>(true);
                batch.Finish();
                // Only the prior optional shell is hidden. Original 02I objects and physics
                // remain live; all construction modules still rebuild from saved slots.
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child.name.StartsWith("Citadel Renaissance 03A2 / ",StringComparison.Ordinal))
                    {
                        previousShell = child.gameObject;
                        previousShellActive = previousShell.activeSelf;
                        previousShell.SetActive(false);
                    }
                }
                foreach (MeshRenderer renderer in originals)
                {
                    Material material = renderer.sharedMaterial;
                    if (material == null) continue;
                    string name = material.name;
                    bool original = name.StartsWith("Strongholds 02I / ",StringComparison.Ordinal) ||
                        name.StartsWith("Citadel 03A2 / ",StringComparison.Ordinal);
                    if (!original) continue;
                    Material replacement = name.EndsWith("/stone",StringComparison.Ordinal) ? stone :
                        name.EndsWith("/cloth",StringComparison.Ordinal) ? cloth :
                        name.EndsWith("/crystal",StringComparison.Ordinal) ? crystal : null;
                    if (replacement == null) continue;
                    replaced.Add(new KeyValuePair<MeshRenderer,Material>(renderer,material));
                    renderer.sharedMaterial = replacement;
                }
                _rebirth04AActive = true;
                if (!_rebirth04ALogged)
                {
                    _rebirth04ALogged = true;
                    Log.Info("VISUAL REBIRTH 04A | " + Definition.title + " | " + (layout.parts.Length+socketParts.Count) +
                        " modeled parts | " + layout.collisions.Length + " bounded route colliders | 6 original sockets preserved.");
                }
                // Presentation is independently optional and cannot roll back successful fort art.
                try
                {
                    VisualRebirth04AAmbience ambience = VisualRebirth04AAmbience.Add(batch.Transform,(CrystalCityStyle)style);
                    ambience.AddMotes(batch.Transform,new Vector3(0f,2.2f,5f),(CrystalCityStyle)style,Game.Seed+419);
                    ambience.AddAccent(Origin+new Vector3(0f,3.2f,5f),(CrystalCityStyle)style,batch.Transform);
                    if (Definition.home)
                        ambience.AddPerchedDrake(batch.Transform,new Vector3(19.1f,4f,13f),210f,Game.Seed+421);
                }
                catch (Exception ambienceError) { Log.Warn("04A fort atmosphere skipped: " + ambienceError.Message); }
            }
            catch (Exception error)
            {
                if (batch != null) batch.Abort();
                _rebirth04AActive = false;
                if (previousShell != null) previousShell.SetActive(previousShellActive);
                foreach (KeyValuePair<MeshRenderer,Material> original in replaced)
                    if (original.Key != null) original.Key.sharedMaterial = original.Value;
                if (!_rebirth04AFailureReported)
                {
                    _rebirth04AFailureReported = true;
                    Log.Warn("Visual Rebirth 04A fort art unavailable; the working 03A2 fort is retained. " + error.Message);
                }
            }
        }

        private static void AddRebirthPart(VisualRebirth04ABatch batch,StrongholdPart04A part,int style)
        {
            if (part.fine && Settings.Graphics == GraphicsPreset.Low) return;
            Vector3 position = StrongholdLayout04A.Vector(part.position), scale = StrongholdLayout04A.Vector(part.scale);
            Quaternion rotation = Quaternion.Euler(StrongholdLayout04A.Vector(part.euler));
            if (part.model == "sky_ring" || part.model == "vine")
            {
                Mesh mesh = VisualRebirth04AAssets.Mesh(part.model,false);
                float radius = (mesh.bounds.extents.magnitude+mesh.bounds.center.magnitude)*Mathf.Max(scale.x,Mathf.Max(scale.y,scale.z));
                // A rotating ellipse near the plot edge could sweep outside the placement probe.
                // Animate only ornaments whose complete swept sphere remains inside the plot.
                if (Mathf.Abs(position.x)+radius<23.9f && Mathf.Abs(position.z)+radius<23.9f)
                {
                    VisualRebirth04AAmbience ambience = VisualRebirth04AAmbience.Add(batch.Transform,(CrystalCityStyle)style);
                    if (ambience.TryAddMovingPart(batch.Transform,part.model,part.role,position,scale,rotation,style)) return;
                }
            }
            batch.Part(part.model,part.role,position,scale,rotation,!part.fine);
        }
    }
}
