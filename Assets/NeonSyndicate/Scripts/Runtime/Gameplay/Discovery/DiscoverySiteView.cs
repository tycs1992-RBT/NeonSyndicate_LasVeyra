using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>Four small collision-free pedestals, animated glyphs and contextual labels under one site owner.</summary>
    internal sealed class DiscoverySiteView
    {
        public readonly DiscoveryDefinition Definition;
        public readonly GameObject Root;
        public readonly Vector3[] Points;
        public readonly Blip MapMarker;
        private readonly Transform[] _jewels = new Transform[4];
        private readonly Renderer[] _jewelRenderers = new Renderer[4];
        private readonly TextMesh[] _labels = new TextMesh[4];
        private readonly List<Interactable> _interactions = new List<Interactable>();
        private readonly CrystalCityStyle _style;
        private readonly AudioSource _audio;
        private readonly AudioClip[] _tones = new AudioClip[4];
        private float _pulse;
        private bool _wrong;
        private int _lastMask = -1;

        public DiscoverySiteView(CityDiscoverySystem owner, DiscoveryDefinition def, Vector3[] points, Quaternion yaw)
        {
            Definition = def; Points = points;
            _style = CrystalCity02DMaterials.StyleAt(points[0]);
            Root = new GameObject("Crystal Discovery 02G | " + def.title);
            Root.transform.SetParent(owner.transform, false); Root.transform.position = points[0];
            Root.SetActive(false);
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    int captured = i;
                    string title = i == 0 ? "MEMORY CORE" : DiscoveryBook.RuneName(i - 1);
                    CrystalCity02FComposer composer = new CrystalCity02FComposer("Discovery pedestal " + title, Root.transform, points[i], _style, null);
                    composer.Append(Pedestal(i == 0), points[i], yaw, 1f, false); composer.Finish(false);
                    GameObject jewel = Shapes.MeshObject("Resonance " + title,
                        CrystalCity02DGeometry.Get(i == 0 ? "shard" : (i == 2 ? "bloom" : "rosette")),
                        new[] { CrystalCity02DMaterials.Get(_style, "crystal") }, Root.transform, false);
                    jewel.transform.position = points[i] + Vector3.up * (i == 0 ? 1.05f : .95f);
                    jewel.transform.rotation = yaw * (i == 0 || i == 2 ? Quaternion.identity : Quaternion.Euler(0f, 0f, 0f));
                    jewel.transform.localScale = Vector3.one * (i == 0 ? .65f : .42f);
                    _jewels[i] = jewel.transform; _jewelRenderers[i] = jewel.GetComponent<Renderer>();
                    GameObject label = new GameObject("Label " + title); label.transform.SetParent(Root.transform, false);
                    label.transform.position = points[i] + Vector3.up * (i == 0 ? 1.95f : 1.8f);
                    TextMesh text = label.AddComponent<TextMesh>(); text.font = Mats.UIFont;
                    text.text = title; text.fontSize = 48; text.characterSize = .035f;
                    text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = Color.white;
                    MeshRenderer renderer = label.GetComponent<MeshRenderer>();
                    if (text.font != null) renderer.sharedMaterial = text.font.material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    _labels[i] = text;
                    Interactable it = Interactable.Create("02G " + title, points[i], 1.35f, "Press E to resonate", null, Root.transform);
                    it.HeightTolerance = 1.45f;
                    it.DynamicPrompt = () => owner.Prompt(def.id, captured);
                    it.Condition = () => owner.CanApproach(points[captured]);
                    it.Use = () => owner.Use(this, captured);
                    _interactions.Add(it);
                }
                _audio = Root.AddComponent<AudioSource>(); _audio.playOnAwake = false;
                _audio.spatialBlend = 1f; _audio.minDistance = 2f; _audio.maxDistance = 24f;
                _audio.dopplerLevel = 0f; _audio.rolloffMode = AudioRolloffMode.Logarithmic;
                for (int i = 0; i < _tones.Length; i++) _tones[i] = Resources.Load<AudioClip>("CrystalDiscovery02G/Audio/chime_" + i);
                MapMarker = Blip.Add(BlipKind.Activity, Root.transform, points[0], UIKit.Cyan, "D", def.title + " (ground shrine)", false);
                MapMarker.ShowOnMap = true; MapMarker.ShowOnMinimap = true;
                Root.SetActive(true);
            }
            catch { Destroy(); throw; }
        }

        private static CrystalCity02FBlueprint Pedestal(bool core)
        {
            return new CrystalCity02FBlueprint
            {
                id = "discovery_pedestal", title = "Resonance pedestal", radius = .55f, minY = -.10f, maxY = 1.7f,
                nodes = new[]
                {
                    Node("terrace", "stone", new Vector3(0f,-.11f,0f), new Vector3(.13f,.28f,.13f)),
                    Node("rune_disc", "gilt", new Vector3(0f,.08f,0f), new Vector3(.52f,.10f,.52f)),
                    Node("light_seed", "crystal", new Vector3(0f,.46f,0f), new Vector3(.12f,.19f,.12f)),
                    Node("crescent", "gilt", new Vector3(0f,.10f,.10f), Vector3.one * (core ? .22f : .17f))
                }
            };
        }
        private static CrystalCity02FNode Node(string model, string material, Vector3 pos, Vector3 scale)
        {
            return new CrystalCity02FNode { model = model, material = material,
                position = new[] { pos.x,pos.y,pos.z }, scale = new[] { scale.x,scale.y,scale.z }, euler = new[] { 0f,0f,0f } };
        }
        public void Feedback(int rune, bool wrong)
        {
            _pulse = 1.0f; _wrong = wrong;
            if (_audio == null) return;
            _audio.volume = Mathf.Clamp01(Settings.SfxVolume) * .38f;
            // AudioManager already owns AudioListener.volume; do not multiply MasterVolume a second time.
            _audio.pitch = wrong ? .65f : 1f;
            AudioClip clip = _tones[Mathf.Clamp(rune,0,3)];
            if (clip != null) _audio.PlayOneShot(clip);
        }
        public void Tick(DiscoveryEntrySave status, Transform viewer, bool inCity, float delta)
        {
            if (Root == null || status == null) return;
            Root.SetActive(inCity);
            if (MapMarker != null)
            {
                MapMarker.Visible = inCity;
                MapMarker.Color = status.RewardMask == 3 ? UIKit.Gold : (status.Seen ? UIKit.Cyan : Color.white);
                MapMarker.Label = Definition.title + (status.RewardMask == 3 ? " (attuned)" : " (ground shrine)");
            }
            if (!inCity) { if (_audio != null && _audio.isPlaying) _audio.Stop(); return; }
            _pulse = Mathf.Max(0f, _pulse - delta * 1.8f);
            float distance = viewer == null ? 1000f : Vector3.Distance(viewer.position, Points[0]);
            int mask = status.Solved ? 15 : 0;
            for (int j = 0; j < Mathf.Min(3,status.Step); j++) mask |= 1 << (Definition.order[j] + 1);
            for (int i = 0; i < 4; i++)
            {
                if (_labels[i] != null)
                {
                    _labels[i].gameObject.SetActive(distance < 18f && viewer != null);
                    if (distance < 18f && viewer != null) _labels[i].transform.rotation = viewer.rotation;
                    _labels[i].color = (mask & (1 << i)) != 0 ? UIKit.Gold : Color.white;
                }
                if (_jewels[i] == null) continue;
                if (_lastMask != mask)
                    _jewelRenderers[i].sharedMaterial = DistrictMaterials.Get((mask & (1 << i)) != 0 ? "crystalWarm" :
                        (_style == CrystalCityStyle.AmethystWard ? "crystalViolet" : "crystal"));
                if (distance < 65f && delta > 0f)
                {
                    float lift = Mathf.Sin(Time.time * 1.6f + i) * .05f + _pulse * (_wrong ? -.035f : .18f);
                    _jewels[i].position = Points[i] + Vector3.up * ((i == 0 ? 1.05f : .95f) + lift);
                    if (i == 0) _jewels[i].Rotate(Vector3.up, delta * 18f, Space.Self);
                }
            }
            _lastMask = mask;
        }
        public void Destroy()
        {
            Blip.Remove(MapMarker);
            foreach (Interactable it in _interactions) if (it != null) it.enabled = false;
            _interactions.Clear();
            if (Root != null) { Root.SetActive(false); UnityEngine.Object.Destroy(Root); }
        }
    }
}
