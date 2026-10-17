using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// crown_reflectors: rotate visible reflectors to bend a beam into a receptor. The emitter, four reflector stands
    /// (A to D), a pillar and the receptor stand where ReflectorPuzzle04E.CrownLayout puts them in the anchor's frame
    /// (scaled down when the room is too small for the solution's beam path). Each reflector turns 45 degrees per use
    /// (the interact prompt); the beam is re-traced with the same maths the tests check and drawn segment by segment,
    /// stopping at the first wall. Touching the beam stings. The receptor must stay lit for two seconds to finish.
    /// </summary>
    public sealed class ChoirReflectors : ChoirMechanism
    {
        private ReflectorPuzzle04E _puzzle;
        private float _scale = 1f;
        private float _height;
        private MachineRig _emitter, _receptor;
        private MeshRenderer _receptorLens, _emitterLens;
        private readonly List<MachineRig> _stands = new List<MachineRig>();
        private readonly List<Transform> _panels = new List<Transform>();
        private readonly List<float> _panelYaw = new List<float>();
        private readonly List<Interactable> _uses = new List<Interactable>();
        private readonly List<Vector2> _path = new List<Vector2>();
        private readonly List<Vector3> _world = new List<Vector3>();
        private readonly List<UBossBeamVis> _beams = new List<UBossBeamVis>();
        private BeamEnd _end;
        private int _lastMirror = -1;
        private bool _wallCut;
        private float _charge;
        private float _retraceAt;
        private float _stingAt;

        protected override bool Build(out string reason)
        {
            reason = "";
            _puzzle = ReflectorPuzzle04E.CrownLayout();
            _scale = FitScale();
            _height = 1.6f;
            Vector3 e = World(_puzzle.EmitterPos);
            _emitter = UnderBossMachine.Prop(UnderBossShapes04E.BeamPost(false), transform, gameObject, e, Yaw + MathUtil.YawOf(new Vector3(_puzzle.EmitterDir.x, 0f, _puzzle.EmitterDir.y)), true);
            UnderBossMachine.Untag(_emitter);
            _emitterLens = _emitter.Renderer("Lens");
            Vector3 r = World(_puzzle.ReceptorPos);
            _receptor = UnderBossMachine.Prop(UnderBossShapes04E.BeamPost(true), transform, gameObject, r, Yaw, true);
            UnderBossMachine.Untag(_receptor);
            _receptorLens = _receptor.Renderer("Lens");
            for (int i = 0; i < _puzzle.Blockers.Count; i++)
            {
                if (i == 1) continue;
                Vector3 b = World(_puzzle.Blockers[i]);
                GameObject pillar = Shapes.Box("Choir pillar", transform, Vector3.zero, new Vector3(1.6f, 4f, 1.6f), UnderBossFx.MachineMaterials[UnderBossFx.SubIron], true);
                pillar.transform.position = b + Vector3.up * 2f;
                pillar.layer = Layers.Debris;
            }
            for (int i = 0; i < _puzzle.Mirrors.Count; i++)
            {
                Vector3 m = World(_puzzle.Mirrors[i]);
                MachineRig stand = UnderBossMachine.Prop(UnderBossShapes04E.ReflectorStand(), transform, gameObject, m, Yaw, true);
                UnderBossMachine.Untag(stand);
                _stands.Add(stand);
                Transform panel = stand.Get("Panel");
                _panels.Add(panel);
                float target = PanelYaw(i);
                _panelYaw.Add(target);
                if (panel != null) panel.rotation = Quaternion.Euler(0f, target, 0f);
                int index = i;
                string name = _puzzle.Names[i];
                Interactable use = Interactable.Create("Choir reflector " + name, m, 2.4f, "Press E to turn reflector " + name + " (45 degrees)", () => Turn(index), stand.Parts[0].parent);
                use.Condition = () => Running;
                _uses.Add(use);
            }
            Retrace();
            Handle.Objective = "Turn the reflectors (each turn is 45 degrees) to bend the beam into the receptor.";
            return true;
        }

        /// <summary>
        /// The largest layout scale (1, 0.8, 0.65) whose solved beam path is clear of walls at beam height in this room;
        /// 0.65 if none is (the HUD then says the room blocks the beam).
        /// </summary>
        private float FitScale()
        {
            float[] tries = { 1f, 0.8f, 0.65f };
            int[] keep = (int[])_puzzle.Orientation.Clone();
            _puzzle.Orientation = (int[])_puzzle.Solution.Clone();
            List<Vector2> path = new List<Vector2>();
            int last;
            _puzzle.Trace(path, out last);
            _puzzle.Orientation = keep;
            for (int t = 0; t < tries.Length; t++)
            {
                _scale = tries[t];
                bool clear = true;
                for (int i = 0; i + 1 < path.Count && clear; i++)
                {
                    Vector3 a = World(path[i]) + Vector3.up * 1.6f, b = World(path[i + 1]) + Vector3.up * 1.6f;
                    if (Physics.Linecast(a, b, Layers.WorldMask, QueryTriggerInteraction.Ignore)) clear = false;
                }
                if (clear) return tries[t];
            }
            Log.Warn("UBOSS04E crown_reflectors: the room blocks the solved beam path even at 0.65 scale");
            return tries[tries.Length - 1];
        }

        private Vector3 World(Vector2 local)
        {
            return Local(local.x * _scale, local.y * _scale);
        }

        /// <summary>The panel's world yaw for a reflector's orientation (the mirror line sits at orientation times 45 degrees from +x).</summary>
        private float PanelYaw(int i)
        {
            int k = i < _puzzle.Orientation.Length ? _puzzle.Orientation[i] : 0;
            return Yaw - k * 45f;
        }

        private void Turn(int i)
        {
            if (!Running || !_puzzle.Rotate(i)) return;
            _panelYaw[i] = PanelYaw(i);
            Underheart04ESounds.Play(UhSfx.ValveTurn, World(_puzzle.Mirrors[i]) + Vector3.up * 1.6f, 0.9f, 1.3f);
            Retrace();
        }

        private void Retrace()
        {
            _end = _puzzle.Trace(_path, out _lastMirror);
            _world.Clear();
            _wallCut = false;
            for (int i = 0; i < _path.Count; i++) _world.Add(World(_path[i]) + Vector3.up * _height);
            // The real room may cut the beam short: it stops at the first wall.
            for (int i = 0; i + 1 < _world.Count; i++)
            {
                RaycastHit hit;
                if (!Physics.Linecast(_world[i], _world[i + 1], out hit, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                _world[i + 1] = hit.point;
                _world.RemoveRange(i + 2, _world.Count - (i + 2));
                _wallCut = true;
                break;
            }
            while (_beams.Count < _world.Count - 1) _beams.Add(UnderBossFx.TakeBeam());
            while (_beams.Count > Mathf.Max(0, _world.Count - 1))
            {
                UnderBossFx.FreeBeam(_beams[_beams.Count - 1]);
                _beams.RemoveAt(_beams.Count - 1);
            }
            _retraceAt = Time.time + 0.5f;
        }

        private bool Lit { get { return _end == BeamEnd.Receptor && !_wallCut; } }

        protected override void Tick(float dt, float now)
        {
            if (now >= _retraceAt) Retrace();
            for (int i = 0; i < _panels.Count; i++)
            {
                Transform p = _panels[i];
                if (p == null) continue;
                p.rotation = Quaternion.RotateTowards(p.rotation, Quaternion.Euler(0f, _panelYaw[i], 0f), 180f * dt);
            }
            float flicker = 0.85f + 0.15f * Mathf.Sin(now * 30f);
            Color beam = new Color(0.6f, 0.95f, 1f, 1f);
            for (int i = 0; i < _beams.Count; i++) UnderBossFx.PlaceBeam(_beams[i], _world[i], _world[i + 1], 0.16f, beam, 1.2f * flicker);
            UnderBossFx.Emission(_emitterLens, new Color(0.6f, 0.95f, 1f) * 3f);
            _charge = Lit ? _charge + dt : Mathf.Max(0f, _charge - dt * 2f);
            UnderBossFx.Emission(_receptorLens, new Color(0.6f, 0.95f, 1f) * (0.4f + 4f * Mathf.Clamp01(_charge / ChoirRules04E.ReceptorCharge)));
            if (now >= _stingAt)
            {
                _stingAt = now + 0.25f;
                Vector3 feet = Game.Player != null ? Game.Player.transform.position : Game.PlayerPosition;
                for (int i = 0; i + 1 < _world.Count; i++)
                {
                    if (!BossLanes04D.BeamHitsBody(_world[i], _world[i + 1], 0.45f, feet, 1.8f)) continue;
                    Sting(_world[i], 0.25f);
                    break;
                }
            }
            if (_charge >= ChoirRules04E.ReceptorCharge)
            {
                if (Game.Fx != null) Game.Fx.Flash(World(_puzzle.ReceptorPos) + Vector3.up * 2.2f, new Color(0.6f, 0.95f, 1f), 6f, 20f, 0.6f);
                End(true, "The beam holds in the receptor: the reflector mechanism is down.");
                return;
            }
            Handle.Objective = Lit ? "The receptor is charging: keep the beam on it." : "Turn the reflectors (each turn is 45 degrees) to bend the beam into the receptor.";
        }

        public override float Progress01 { get { return Mathf.Clamp01(_charge / ChoirRules04E.ReceptorCharge); } }

        public override string ExtraLine
        {
            get
            {
                if (_wallCut) return _end == BeamEnd.Receptor ? "The room's wall blocks the beam before the receptor." : "Beam: stopped by the room's wall.";
                switch (_end)
                {
                    case BeamEnd.Receptor: return "Beam: reaches the receptor.";
                    case BeamEnd.Blocked: return "Beam: stopped by a pillar or the emitter housing.";
                    case BeamEnd.Looped: return "Beam: bouncing between reflectors.";
                    default: return _lastMirror >= 0 ? "Beam: leaves the room after reflector " + _puzzle.Names[_lastMirror] + "." : "Beam: misses every reflector.";
                }
            }
        }

        public override void Targets(List<Vector3> into, List<string> labels)
        {
            base.Targets(into, labels);
            for (int i = 0; i < _puzzle.Mirrors.Count; i++)
            {
                into.Add(World(_puzzle.Mirrors[i]) + Vector3.up * 2.5f);
                labels.Add(_puzzle.Names[i]);
            }
            into.Add(World(_puzzle.ReceptorPos) + Vector3.up * 3.2f);
            labels.Add("RECEPTOR");
        }

        /// <summary>F2: sets the solution (the beam must still pass the real room).</summary>
        public void DebugSolve()
        {
            if (!Running) return;
            for (int i = 0; i < _puzzle.Orientation.Length; i++)
            {
                _puzzle.Orientation[i] = _puzzle.Solution[i];
                _panelYaw[i] = PanelYaw(i);
            }
            Retrace();
        }

        protected override void Aftermath(float dt, float now)
        {
            float flicker = 0.85f + 0.15f * Mathf.Sin(now * 30f);
            for (int i = 0; i < _beams.Count && i + 1 < _world.Count; i++) UnderBossFx.PlaceBeam(_beams[i], _world[i], _world[i + 1], 0.12f, new Color(0.6f, 0.95f, 1f, 1f), 0.8f * flicker);
        }

        protected override void OnCleanup()
        {
            for (int i = 0; i < _beams.Count; i++) UnderBossFx.FreeBeam(_beams[i]);
            _beams.Clear();
        }
    }
}
