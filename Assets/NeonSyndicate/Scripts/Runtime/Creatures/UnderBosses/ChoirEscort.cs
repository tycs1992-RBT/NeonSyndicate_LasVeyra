using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// crown_escort: protect a small maintenance construct along a short safe path. The construct walks from the
    /// start anchor to crown.escort_end (or about 28 m ahead when the Underheart has no such anchor) along a path
    /// checked against the room's walls, only while the player stays within 9 m (it waits and blinks otherwise). Two
    /// declared waves arrive ahead of it (announced); attackers next to it damage it. A broken construct reboots at
    /// the start after a moment and that attempt's attackers withdraw: the mechanism resets, the quest goes on.
    /// </summary>
    public sealed class ChoirEscort : ChoirMechanism
    {
        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<float> _cum = new List<float>();
        private EscortRun04E _run;
        private MachineRig _rig;
        private Transform _bot;
        private MeshRenderer _botR;
        private Vector3 _end;
        private float _rebootAt;
        private float _walk;
        private float _yaw;
        private UBossDiscVis _goal;

        protected override bool Build(out string reason)
        {
            reason = "";
            Vector3 start = Local(0f, 0f);
            _end = ResolveEnd(start);
            BuildPath(start, _end);
            float len = _cum[_cum.Count - 1];
            if (len < 4f)
            {
                reason = "the escort path is too short";
                return false;
            }
            _run = new EscortRun04E(len);
            _rig = UnderBossMachine.Prop(UnderBossShapes04E.EscortConstruct(), transform, gameObject, start, Yaw, true);
            UnderBossMachine.Untag(_rig);
            _bot = _rig.Parts[0].parent;
            _botR = _rig.Renderer("Body");
            if (_rig.Colliders[0] != null) _rig.Colliders[0].enabled = false;
            _goal = UnderBossFx.TakeDisc();
            Handle.Objective = "Escort the maintenance construct: stay close and keep the attackers off it.";
            return true;
        }

        /// <summary>The Underheart's escort end anchor when it is close and level enough, else a floor point ahead.</summary>
        private Vector3 ResolveEnd(Vector3 start)
        {
            Vector3 pos;
            float yaw;
            if (Game.Underheart != null && Game.Underheart.TryAnchor("crown.escort_end", out pos, out yaw)
                && MathUtil.FlatDistance(pos, start) > 8f && MathUtil.FlatDistance(pos, start) < 80f && Mathf.Abs(pos.y - start.y) < 10f)
                return pos;
            return Local(0f, 28f);
        }

        /// <summary>
        /// A short safe path: the Underheart's own walkable route when it offers one, else the straight line, else a dog-leg
        /// to either side that clears the walls (checked at knee and chest height).
        /// </summary>
        private void BuildPath(Vector3 a, Vector3 b)
        {
            _path.Clear();
            List<Vector3> route = new List<Vector3>();
            string label;
            bool ok = false;
            if (Game.Underheart != null)
            {
                try { ok = Game.Underheart.TryRoute(a, b, route, out label) && route.Count >= 2; }
                catch (System.Exception e) { Log.Exception("Choir escort route", e); }
            }
            if (ok && RouteLength(route) < MathUtil.FlatDistance(a, b) * 1.8f + 5f && Clear(route)) _path.AddRange(route);
            else if (Clear(a, b))
            {
                _path.Add(a);
                _path.Add(b);
            }
            else
            {
                Vector3 mid = (a + b) * 0.5f;
                Vector3 side = MathUtil.RightOf(MathUtil.Flat(b - a).normalized);
                bool found = false;
                for (int k = 0; k < 4 && !found; k++)
                {
                    float off = (k % 2 == 0 ? 1f : -1f) * (5f + 3f * (k / 2));
                    Vector3 m;
                    if (!UnderBossFx.StandSpot(mid + side * off, a.y, 3f, out m)) continue;
                    if (!Clear(a, m) || !Clear(m, b)) continue;
                    _path.Add(a);
                    _path.Add(m);
                    _path.Add(b);
                    found = true;
                }
                if (!found)
                {
                    _path.Add(a);
                    _path.Add(b);
                    Log.Warn("UBOSS04E crown_escort: no clear path found; using the straight line");
                }
            }
            _cum.Clear();
            float sum = 0f;
            _cum.Add(0f);
            for (int i = 1; i < _path.Count; i++)
            {
                sum += Vector3.Distance(_path[i - 1], _path[i]);
                _cum.Add(sum);
            }
        }

        private static float RouteLength(List<Vector3> r)
        {
            float s = 0f;
            for (int i = 1; i < r.Count; i++) s += Vector3.Distance(r[i - 1], r[i]);
            return s;
        }

        private static bool Clear(List<Vector3> r)
        {
            for (int i = 1; i < r.Count; i++) if (!Clear(r[i - 1], r[i])) return false;
            return true;
        }

        private static bool Clear(Vector3 a, Vector3 b)
        {
            return !Physics.Linecast(a + Vector3.up * 0.5f, b + Vector3.up * 0.5f, Layers.WorldMask, QueryTriggerInteraction.Ignore)
                && !Physics.Linecast(a + Vector3.up * 1.3f, b + Vector3.up * 1.3f, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        private Vector3 PointAt(float d, out Vector3 dir)
        {
            dir = Vector3.forward;
            for (int i = 1; i < _path.Count; i++)
            {
                if (d > _cum[i] && i < _path.Count - 1) continue;
                float seg = Mathf.Max(0.01f, _cum[i] - _cum[i - 1]);
                float t = Mathf.Clamp01((d - _cum[i - 1]) / seg);
                Vector3 f = MathUtil.Flat(_path[i] - _path[i - 1]);
                if (f.sqrMagnitude > 0.01f) dir = f.normalized;
                return Vector3.Lerp(_path[i - 1], _path[i], t);
            }
            return _path[_path.Count - 1];
        }

        private Vector3 BotPos { get { return _bot != null ? _bot.position : Anchor; } }

        protected override void Tick(float dt, float now)
        {
            if (_rebootAt > 0f)
            {
                if (now < _rebootAt) return;
                _rebootAt = 0f;
                NewSquad();
                _bot.gameObject.SetActive(true);
                if (Game.Fx != null) Game.Fx.Sparks(BotPos + Vector3.up * 0.6f, Vector3.up * 2f, 16);
            }
            float pd = MathUtil.FlatDistance(Game.PlayerPosition, BotPos);
            int threats = Squad.CountNear(BotPos, EscortRun04E.ThreatRadius);
            int w = _run.Tick(dt, pd, threats);
            if (w == -2)
            {
                if (Game.Fx != null)
                {
                    Game.Fx.Sparks(BotPos + Vector3.up * 0.6f, Vector3.up * 3f, 30);
                    Game.Fx.Smoke(BotPos + Vector3.up * 0.6f, Vector3.up * 2f, 1.2f, 0.5f);
                }
                Underheart04ESounds.Play(UhSfx.PlateCrack, BotPos, 1f, 1.3f);
                Squad.Dismiss(true);
                _bot.gameObject.SetActive(false);
                _rebootAt = now + 3f;
                Vector3 dir0;
                _bot.position = PointAt(0f, out dir0);
                if (Game.Hud != null) Game.Hud.Notify("The construct broke. It reboots at the start; the attackers withdraw.", 4.5f);
                Log.Info("UBOSS04E mechanism crown_escort construct broke (attempt " + Attempt + ")");
                return;
            }
            Vector3 dir;
            Vector3 p = PointAt(_run.Distance, out dir);
            float y;
            if (UnderBossFx.Floor(p, 1.5f, 3f, out y)) p.y = y;
            if (!_run.Waiting) _walk += EscortRun04E.Speed * dt * 4f;
            _bot.position = p;
            _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(dir), 180f * dt);
            _bot.rotation = Quaternion.Euler(0f, _yaw, 0f);
            Transform body = _rig.Parts[0];
            body.localPosition = new Vector3(0f, 0.55f + Mathf.Abs(Mathf.Sin(_walk)) * 0.04f, 0f);
            float blink = _run.Waiting ? (Mathf.Sin(now * 8f) > 0f ? 3f : 0.3f) : 2f;
            UnderBossFx.Emission(_botR, new Color(0.6f, 0.95f, 1f) * blink * (_run.Health / EscortRun04E.MaxHealth + 0.2f));
            UnderBossFx.PlaceDisc(_goal, _end, 2f, new Color(0.45f, 1f, 0.55f, 1f), 0.35f + 0.15f * Mathf.Sin(now * 3f));
            if (w >= 0)
            {
                Vector3 d2;
                Vector3 ahead = PointAt(Mathf.Min(_run.Length, _run.Distance + 12f), out d2);
                SpawnWave(w, ahead, BotPos + Vector3.up);
            }
            Handle.Objective = _run.Waiting ? "The construct waits for you: stay within 9 m." : "Escort the maintenance construct: keep the attackers off it.";
            if (_run.Arrived) End(true, "The maintenance construct reached the Choir: the escort is done.");
        }

        public override float Progress01 { get { return _run != null ? _run.Progress01 : 0f; } }

        public override string ExtraLine
        {
            get
            {
                if (_run == null) return "";
                if (_rebootAt > 0f) return "Rebooting at the start.";
                return "Construct " + Mathf.RoundToInt(_run.Health) + "%   Waves " + _run.WavesDue + " of " + EscortRun04E.WaveMarks.Length + "   Attackers " + Squad.Living + (_run.Resets > 0 ? "   Reboots " + _run.Resets : "");
            }
        }

        public override void Targets(List<Vector3> into, List<string> labels)
        {
            base.Targets(into, labels);
            into.Add(BotPos + Vector3.up * 1.4f);
            labels.Add("CONSTRUCT");
            into.Add(_end + Vector3.up * 1f);
            labels.Add("DESTINATION");
        }

        protected override void OnCleanup()
        {
            if (_goal != null) UnderBossFx.FreeDisc(_goal);
            _goal = null;
        }

        protected override void OnEnded(bool done)
        {
            if (done && _goal != null)
            {
                UnderBossFx.FreeDisc(_goal);
                _goal = null;
            }
        }
    }
}
