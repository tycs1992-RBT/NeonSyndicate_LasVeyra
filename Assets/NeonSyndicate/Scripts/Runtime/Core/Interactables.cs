using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Anything the player can use with a key press (shops, ATMs, payphones, safehouse doors, mission contacts) or
    /// trigger by walking or driving into it (drive-in respray, mission markers).
    /// </summary>
    public sealed class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public string Prompt = "Press E to interact";
        public float Radius = 1.8f;
        public bool OnFoot = true;
        public bool InVehicle;
        /// <summary>Fires automatically when the player enters the radius (re-arms after leaving).</summary>
        public bool AutoTrigger;
        public Func<bool> Condition;
        public Func<string> DynamicPrompt;
        public Action Use;
        public float HeightTolerance = 2.5f;
        private bool _inside;
        private float _cooldown;

        public static void ResetStatics()
        {
            All.Clear();
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        public bool Available(Vector3 playerPos, bool inVehicle)
        {
            if (!isActiveAndEnabled) return false;
            if (inVehicle && !InVehicle) return false;
            if (!inVehicle && !OnFoot) return false;
            if (Mathf.Abs(playerPos.y - transform.position.y) > HeightTolerance) return false;
            if (MathUtil.FlatSqrDistance(playerPos, transform.position) > Radius * Radius) return false;
            if (Condition != null)
            {
                try { if (!Condition()) return false; }
                catch (Exception e) { Log.Exception("Interactable condition " + name, e); return false; }
            }
            return true;
        }

        public string CurrentPrompt
        {
            get
            {
                if (DynamicPrompt != null)
                {
                    try { return DynamicPrompt(); }
                    catch (Exception e) { Log.Exception("Interactable prompt " + name, e); }
                }
                return Prompt;
            }
        }

        public void Activate()
        {
            if (_cooldown > 0f || Use == null) return;
            _cooldown = 0.4f;
            try { Use(); }
            catch (Exception e) { Log.Exception("Interactable " + name, e); }
        }

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.unscaledDeltaTime;
            if (!AutoTrigger || Game.Player == null || !Game.IsPlaying) return;
            bool now = Available(Game.PlayerPosition, Game.Player.InVehicle);
            if (now && !_inside) Activate();
            _inside = now;
        }

        /// <summary>Nearest key-press interactable around the player, or null.</summary>
        public static Interactable Nearest(Vector3 pos, bool inVehicle)
        {
            Interactable best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                Interactable it = All[i];
                if (it == null || it.AutoTrigger) continue;
                if (!it.Available(pos, inVehicle)) continue;
                float d = MathUtil.FlatSqrDistance(pos, it.transform.position);
                if (d < bd)
                {
                    bd = d;
                    best = it;
                }
            }
            return best;
        }

        public static Interactable Create(string name, Vector3 pos, float radius, string prompt, Action use, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent, true);
            Interactable it = go.AddComponent<Interactable>();
            it.Radius = radius;
            it.Prompt = prompt;
            it.Use = use;
            return it;
        }
    }

    /// <summary>Classic open-world glowing world marker (cylinder of light with a bobbing chevron).</summary>
    public sealed class WorldMarker : MonoBehaviour
    {
        private Transform _chevron;
        private Transform _ring;
        private float _phase;
        public Color Color;

        public static WorldMarker Create(Vector3 groundPos, Color color, float radius, Transform parent, bool chevron = true)
        {
            GameObject go = new GameObject("Marker");
            go.transform.position = groundPos;
            if (parent != null) go.transform.SetParent(parent, true);
            WorldMarker m = go.AddComponent<WorldMarker>();
            m.Color = color;
            // A light veil, not a wall: the player and the NPC inside must stay clearly visible.
            Color c = new Color(color.r, color.g, color.b, 0.18f);
            GameObject ring = Shapes.MeshObject("Glow", Shapes.CylinderMesh, new[] { Mats.Marker(c) }, go.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            ring.transform.localScale = new Vector3(radius * 2f, 1.2f, radius * 2f);
            m._ring = ring.transform;
            if (chevron)
            {
                GameObject chev = Shapes.MeshObject("Chevron", Shapes.ChevronMesh, new[] { Mats.Marker(new Color(color.r, color.g, color.b, 0.85f)) }, go.transform, false);
                chev.transform.localPosition = new Vector3(0f, 2.4f, 0f);
                m._chevron = chev.transform;
            }
            return m;
        }

        private void Update()
        {
            _phase += Time.deltaTime;
            if (_chevron != null)
            {
                _chevron.localPosition = new Vector3(0f, 2.4f + Mathf.Sin(_phase * 2.2f) * 0.18f, 0f);
                _chevron.localRotation = Quaternion.Euler(0f, _phase * 90f, 0f);
            }
            if (_ring != null)
            {
                float s = 1f + Mathf.Sin(_phase * 3f) * 0.03f;
                _ring.localScale = new Vector3(_ring.localScale.x, 1.2f * s, _ring.localScale.z);
            }
        }
    }
}
