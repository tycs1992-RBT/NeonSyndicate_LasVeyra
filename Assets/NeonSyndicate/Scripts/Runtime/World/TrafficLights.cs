using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum SignalPhase
    {
        NSGreen = 0,
        NSYellow = 1,
        AllRedA = 2,
        EWGreen = 3,
        EWYellow = 4,
        AllRedB = 5
    }

    /// <summary>
    /// Runs every signalized intersection on a deterministic clock (so traffic AI and pedestrians always agree)
    /// and swaps the lamp materials of nearby signals.
    /// </summary>
    public sealed class TrafficLights : MonoBehaviour
    {
        public const float YellowTime = 3f;
        public const float AllRedTime = 1.2f;

        private CityLayout _layout;
        private float[] _offset;
        private float[] _greenNS;
        private float[] _greenEW;
        private int[] _shownPhase;
        private readonly Dictionary<int, MeshRenderer> _renderers = new Dictionary<int, MeshRenderer>();
        private float _refresh;
        private readonly Material[] _matBuffer = new Material[6];

        public void Init(CityLayout layout, Dictionary<int, MeshRenderer> renderers)
        {
            _layout = layout;
            int n = layout.Nodes.Count;
            _offset = new float[n];
            _greenNS = new float[n];
            _greenEW = new float[n];
            _shownPhase = new int[n];
            for (int i = 0; i < n; i++)
            {
                RoadNode node = layout.Nodes[i];
                float ns = 0f, ew = 0f;
                foreach (int eid in node.Edges)
                {
                    RoadEdge e = layout.Edges[eid];
                    if (e.AlongX) ew = Mathf.Max(ew, e.Width); else ns = Mathf.Max(ns, e.Width);
                }
                _greenNS[i] = 11f + (ns > ew ? 5f : 0f) + (ns > 20f ? 3f : 0f);
                _greenEW[i] = 11f + (ew > ns ? 5f : 0f) + (ew > 20f ? 3f : 0f);
                // Green-wave flavour: offsets follow position along avenues.
                _offset[i] = Mathf.Repeat(node.Pos.x * 0.031f + node.Pos.z * 0.017f + (node.SignalOffsetSeed % 7), Cycle(i));
                _shownPhase[i] = -1;
            }
            foreach (var kv in renderers) _renderers[kv.Key] = kv.Value;
        }

        private float Cycle(int node)
        {
            return _greenNS[node] + _greenEW[node] + 2f * (YellowTime + AllRedTime);
        }

        public SignalPhase Phase(int node, out float remaining)
        {
            remaining = 999f;
            if (_layout == null || node < 0 || node >= _offset.Length) return SignalPhase.NSGreen;
            float cycle = Cycle(node);
            float t = Mathf.Repeat(Time.time + _offset[node], cycle);
            float g1 = _greenNS[node];
            if (t < g1) { remaining = g1 - t; return SignalPhase.NSGreen; }
            t -= g1;
            if (t < YellowTime) { remaining = YellowTime - t; return SignalPhase.NSYellow; }
            t -= YellowTime;
            if (t < AllRedTime) { remaining = AllRedTime - t; return SignalPhase.AllRedA; }
            t -= AllRedTime;
            float g2 = _greenEW[node];
            if (t < g2) { remaining = g2 - t; return SignalPhase.EWGreen; }
            t -= g2;
            if (t < YellowTime) { remaining = YellowTime - t; return SignalPhase.EWYellow; }
            t -= YellowTime;
            remaining = AllRedTime - t;
            return SignalPhase.AllRedB;
        }

        public bool IsSignal(int node)
        {
            return _layout != null && node >= 0 && node < _layout.Nodes.Count && _layout.Nodes[node].Signal;
        }

        /// <summary>True if a vehicle approaching on the given axis may enter the intersection now.</summary>
        public bool MayEnter(int node, bool axisNS, float secondsToStopLine)
        {
            if (!IsSignal(node)) return true;
            float rem;
            SignalPhase p = Phase(node, out rem);
            if (axisNS)
            {
                if (p == SignalPhase.NSGreen) return true;
                if (p == SignalPhase.NSYellow) return secondsToStopLine < 1.2f;
                return false;
            }
            if (p == SignalPhase.EWGreen) return true;
            if (p == SignalPhase.EWYellow) return secondsToStopLine < 1.2f;
            return false;
        }

        /// <summary>Pedestrians crossing a north-south road need north-south traffic stopped (east-west green).</summary>
        public bool PedestrianMayCross(int node, bool crossesNSRoad)
        {
            if (!IsSignal(node)) return true;
            float rem;
            SignalPhase p = Phase(node, out rem);
            if (crossesNSRoad) return p == SignalPhase.EWGreen && rem > 5f;
            return p == SignalPhase.NSGreen && rem > 5f;
        }

        private void Update()
        {
            if (_layout == null) return;
            _refresh -= Time.deltaTime;
            if (_refresh > 0f) return;
            _refresh = 0.2f;
            Camera cam = Camera.main;
            Vector3 focus = cam != null ? cam.transform.position : Vector3.zero;
            foreach (var kv in _renderers)
            {
                int node = kv.Key;
                MeshRenderer mr = kv.Value;
                if (mr == null) continue;
                if (MathUtil.FlatSqrDistance(_layout.Nodes[node].Pos, focus) > 520f * 520f) continue;
                float rem;
                int p = (int)Phase(node, out rem);
                if (p == _shownPhase[node]) continue;
                _shownPhase[node] = p;
                bool nsRed = p >= 2;
                bool nsYellow = p == 1;
                bool nsGreen = p == 0;
                bool ewRed = p < 3 || p == 5;
                bool ewYellow = p == 4;
                bool ewGreen = p == 3;
                _matBuffer[0] = Mats.SignalLamp(0, nsRed);
                _matBuffer[1] = Mats.SignalLamp(1, nsYellow);
                _matBuffer[2] = Mats.SignalLamp(2, nsGreen);
                _matBuffer[3] = Mats.SignalLamp(0, ewRed);
                _matBuffer[4] = Mats.SignalLamp(1, ewYellow);
                _matBuffer[5] = Mats.SignalLamp(2, ewGreen);
                mr.sharedMaterials = _matBuffer;
            }
        }
    }
}
