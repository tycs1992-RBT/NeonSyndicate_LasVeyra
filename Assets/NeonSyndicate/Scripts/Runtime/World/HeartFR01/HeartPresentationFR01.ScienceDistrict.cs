using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HeartPresentationFR01
    {
        private sealed class SciencePair
        {
            public RewritePairWR01 Visual;
            public GameObject OriginalSolid, CandidateSolid;
            public readonly List<Renderer> Warm = new List<Renderer>(), Cool = new List<Renderer>();
            public readonly List<Bounds> TransitionZones = new List<Bounds>();
        }
        private readonly Dictionary<string, SciencePair> _sciencePairs = new Dictionary<string, SciencePair>();
        private float _scienceReviewAt;

        /// <summary>One transaction prepares the entire exterior and its exact physics before source suppression.</summary>
        internal bool StageScienceDistrict(string id, Transform parent, int style, Dictionary<string, MeshBuilder> originalNear,
            Dictionary<string, MeshBuilder> originalFar, MeshBuilder originalCollision, ScienceDistrictArt art, bool service)
        {
            VisualRebirth04ABatch original = null;
            GameObject candidate = null, originalSolid = null, candidateSolid = null;
            List<Mesh> temporary = new List<Mesh>();
            string key = "revision:10/science-district/" + id;
            if (_sciencePairs.ContainsKey(key)) return false;
            try
            {
                original = new VisualRebirth04ABatch("SF01 retained physical source / " + id, parent, Vector3.zero, style);
                foreach (KeyValuePair<string, MeshBuilder> part in originalNear)
                {
                    Mesh near = part.Value.ToMesh("SF01 original " + id + " / " + part.Key);
                    Mesh far = originalFar[part.Key].ToMesh("SF01 original far " + id + " / " + part.Key);
                    temporary.Add(near); temporary.Add(far);
                    original.PartMeshes(Settings.Graphics == GraphicsPreset.Low ? far : near, far, AscensionArtH2.Surface(part.Key),Vector3.zero,Vector3.one,Quaternion.identity);
                }
                original.Finish();
                SciencePair pair = new SciencePair();
                candidate = new GameObject("SF01 sculpted crystal architecture / " + id);
                candidate.SetActive(false); candidate.transform.SetParent(parent,false);
                List<Renderer> visible = new List<Renderer>();
                foreach (KeyValuePair<string, MeshBuilder> part in art.Surfaces)
                {
                    if (part.Value.VertexCount == 0) continue;
                    Material material = AscensionArtH2.Surface(part.Key == "warm" ? "azure" : part.Key == "earth" ? "basalt" : part.Key);
                    if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null ||
                        material.GetTexture("_BumpMap") == null || material.GetTexture("_MetallicGlossMap") == null)
                        throw new InvalidOperationException("Science district requires intact H2 texture maps and supported shader: " + part.Key);
                    GameObject node = new GameObject(id + " / " + part.Key); node.transform.SetParent(candidate.transform,false);
                    Mesh mesh = OwnedMeshes.Track(node,part.Value.ToMesh("SF01 closed physical surface / " + part.Key));
                    mesh.RecalculateTangents(); node.AddComponent<MeshFilter>().sharedMesh = mesh;
                    MeshRenderer renderer = node.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                    renderer.receiveShadows = true;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    visible.Add(renderer);
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    if (part.Key == "masonry") block.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",new Color(.77f,.72f,.61f));
                    if (part.Key == "slate") block.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",new Color(.46f,.57f,.55f));
                    if (part.Key == "earth") block.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",new Color(.43f,.35f,.24f));
                    if (part.Key == "warm") pair.Warm.Add(renderer);
                    if (part.Key == "azure") pair.Cool.Add(renderer);
                    renderer.SetPropertyBlock(block);
                }
                // One bounded envelope at all distances: a visible roof never disappears from its physical owner.
                LODGroup lod = candidate.AddComponent<LODGroup>(); lod.SetLODs(new[] {new LOD(.006f,visible.ToArray())}); lod.RecalculateBounds();
                originalSolid = ScienceSolid(parent,"SF01 ORIGINAL physical owner / " + id,originalCollision,true);
                candidateSolid = ScienceSolid(parent,"SF01 CANDIDATE physical owner / " + id,art.Solid(),false);
                string reason;
                if (!RegisterRewriteVisual(key,"ScienceDistrictSF01",original.Transform.gameObject,candidate,
                    "Explicit temporary preview. Entire H3/H5 exterior and its physical owner switch together. Occupied rooms and anchors remain original.",out reason))
                    throw new InvalidOperationException(reason);
                pair.Visual = _rewritePairsWR01[_rewritePairsWR01.Count-1];
                pair.OriginalSolid = originalSolid; pair.CandidateSolid = candidateSolid;
                pair.TransitionZones.AddRange(HeartPlanFR01.ScienceChangedZones(originalCollision,art.Solid()));
                pair.Visual.Record.sourceOwner = "VenueView04C.GothicHeartShellH3 or CityBuilder.BuildGothicHeartH3 / " + id;
                pair.Visual.Record.collisionOwner = originalSolid.name + " <-> " + candidateSolid.name;
                pair.Visual.Record.collisionPolicy = "ATOMIC_VISIBLE_AND_PHYSICAL_PAIR_EXPLICIT_PREVIEW";
                _sciencePairs.Add(key,pair);
                ScienceEmission(pair);
                return true;
            }
            catch (Exception error)
            {
                if (original != null) original.Abort();
                foreach(GameObject obj in new[]{candidate,originalSolid,candidateSolid}) if(obj!=null){obj.SetActive(false);UnityEngine.Object.Destroy(obj);}
                RefuseRewriteVisual(key,"ScienceDistrictSF01","Original emitter retained: " + error.Message);
                return false;
            }
            finally { foreach(Mesh mesh in temporary) ArtInhabitedCity04C.Release(mesh); }
        }

        private static GameObject ScienceSolid(Transform parent,string name,MeshBuilder source,bool active)
        {
            GameObject solid = new GameObject(name); solid.SetActive(false); solid.transform.SetParent(parent,false);
            solid.layer = Layers.World; solid.isStatic = true;
            solid.AddComponent<MeshCollider>().sharedMesh = OwnedMeshes.Track(solid,source.ToMesh(name));
            solid.SetActive(active); return solid;
        }

        private bool ApplySciencePair(RewritePairWR01 visual,bool requested)
        {
            SciencePair pair;
            if (!_sciencePairs.TryGetValue(visual.Record.stableId,out pair)) return false;
            string reason = "";
            bool valid = visual.Record.admitted && PassiveRewriteWR01(visual.Legacy,out reason) &&
                PassiveRewriteWR01(visual.Candidate,out reason) && ValidPreparedModelV1(visual.Candidate,out reason) && ScienceSolidValid(pair.OriginalSolid) && ScienceSolidValid(pair.CandidateSolid);
            bool next = requested && valid;
            bool current = visual.Candidate != null && visual.Candidate.activeSelf;
            ScienceDistrictSelection selection=HeartPlanFR01.ScienceSelection(requested,valid,current,next!=current && valid && ScienceTransitionOccupied(pair));
            if (selection == ScienceDistrictSelection.WaitForClearance)
            {
                visual.Record.reason = "Move actors clear of the affected gallery, entrance or roof. Visual and physical pair remains together until clear.";
                visual.Record.state = "WAITING_FOR_CLEARANCE";
                return true;
            }
            next = selection == ScienceDistrictSelection.Prepared;
            // All four objects switch synchronously before the next physics step. No visual-only toggle.
            if (visual.Candidate != null) visual.Candidate.SetActive(next);
            if (pair.CandidateSolid != null) pair.CandidateSolid.SetActive(next);
            if (visual.Legacy != null) visual.Legacy.SetActive(!next);
            if (pair.OriginalSolid != null) pair.OriginalSolid.SetActive(!next);
            if (current != next) Physics.SyncTransforms();
            visual.Record.active = next && visual.Candidate.activeInHierarchy;
            visual.Record.legacySuppressed = next;
            visual.Record.state = !valid ? "REFUSED" : next ? "ACTIVE_PHYSICAL_PREVIEW" : "ADMITTED_INACTIVE";
            visual.Record.reason = !valid ? "Science district restored the complete source pair: " + reason :
                "Matched exterior and collision selected together. Native playtest remains required.";
            if (!valid) visual.Record.admitted = false;
            ScienceEmission(pair);
            return true;
        }

        private static bool ScienceSolidValid(GameObject root)
        {
            if (root == null) return false;
            MeshCollider collider = root.GetComponent<MeshCollider>();
            return collider != null && collider.enabled && !collider.isTrigger && collider.sharedMesh != null && collider.sharedMesh.vertexCount > 2;
        }

        private static bool ScienceTransitionOccupied(SciencePair pair)
        {
            if(pair.TransitionZones.Count==0)return false;
            Transform frame = pair.CandidateSolid.transform;
            Bounds total=pair.TransitionZones[0];
            foreach(Bounds zone in pair.TransitionZones)total.Encapsulate(zone);
            // One broad-phase engine query per requested switch. Unchanged room surfaces do not block it.
            foreach(Collider hit in Physics.OverlapBox(frame.TransformPoint(total.center),Vector3.Scale(total.extents,frame.lossyScale),frame.rotation,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.GetComponentInParent<CharacterBase>()==null && hit.GetComponentInParent<Vehicle>()==null)continue;
                Bounds world=hit.bounds;
                Bounds local=new Bounds(frame.InverseTransformPoint(world.center),Vector3.zero);
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    local.Encapsulate(frame.InverseTransformPoint(world.center+Vector3.Scale(world.extents,new Vector3(x,y,z))));
                foreach(Bounds zone in pair.TransitionZones)if(zone.Intersects(local))return true;
            }
            return false;
        }

        private static void ScienceEmission(SciencePair pair)
        {
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0;
            bool eye = Game.Abilities != null && Game.Abilities.ThirdEyeActive;
            for(int warm=0;warm<2;warm++)foreach(Renderer renderer in warm==1?pair.Warm:pair.Cool)
            {
                if(renderer==null)continue;
                MaterialPropertyBlock block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                Color tint = warm==1?new Color(1,.47f,.14f):new Color(.13f,.72f,.78f);
                block.SetColor("_EmissionColor",tint*HeartPlanFR01.ScienceGlow(night,eye,warm==1));
                if(warm==1)
                {
                    block.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",new Color(1,.76f,.4f));
                    Texture neutral=AscensionArtH2.Surface("masonry").mainTexture;
                    block.SetTexture(RenderPath.IsURP?"_BaseMap":"_MainTex",neutral);block.SetTexture("_EmissionMap",neutral);
                }
                renderer.SetPropertyBlock(block);
            }
        }

        /// <summary>Existing Heart heartbeat supplies lifetime, preview policy, atmosphere and earned Third Eye.</summary>
        public void ReviewScienceDistrict()
        {
            if(Time.unscaledTime<_scienceReviewAt)return;_scienceReviewAt=Time.unscaledTime+.35f;
            foreach(SciencePair pair in _sciencePairs.Values)
                ApplySciencePair(pair.Visual,WorldRewriteSelectedWR01 && !_compare && !_suspended);
        }
    }
}
