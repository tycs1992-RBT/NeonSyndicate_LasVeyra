using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// crown_valve: defend a valve operation against finite waves. The player starts the operation at the great
    /// wheel (E, or Y on a pad, through the ordinary interact prompt); it opens only while the player stays within 14 m,
    /// three declared waves arrive at 0, 35 and 70 percent (announced), and attackers standing at the valve wear its
    /// integrity down. A seized valve resets the operation and dismisses that attempt's attackers (start it again; the
    /// quest goes on). Done when the valve is fully open and every attacker that came is confirmed dead.
    /// </summary>
    public sealed class ChoirValve : ChoirMechanism
    {
        private ValveOperation04E _op;
        private MachineRig _rig;
        private Transform _wheel;
        private MeshRenderer _wheelR;
        private Interactable _use;
        private float _spin;
        private int _loop;
        private Vector3 _valve;

        protected override bool Build(out string reason)
        {
            reason = "";
            _op = new ValveOperation04E(Diff);
            _valve = Local(0f, 0f);
            _rig = UnderBossMachine.Prop(UnderBossShapes04E.ValveMechanism(), transform, gameObject, _valve, Yaw, true);
            UnderBossMachine.Untag(_rig);
            _wheel = _rig.Get("Wheel");
            _wheelR = _rig.Renderer("Wheel");
            if (_wheel == null)
            {
                reason = "the valve did not build";
                return false;
            }
            Vector3 front = _valve + Quaternion.Euler(0f, Yaw, 0f) * new Vector3(0f, 0f, 1.6f);
            _use = Interactable.Create("Choir valve wheel", front, 2.6f, "Press E to open the Choir valve", StartOperation, transform);
            _use.Condition = () => Running && _op != null && !_op.Started;
            Handle.Objective = "Open the Choir valve at its wheel, then defend it: three announced waves.";
            return true;
        }

        private void StartOperation()
        {
            if (!Running || _op.Started) return;
            if (_op.Resets > 0) NewSquad();
            _op.Start();
            Underheart04ESounds.Play(UhSfx.ValveTurn, _valve + Vector3.up * 1.6f, 1f, 0.9f);
            if (_loop == 0) _loop = StartLoop(UhLoop.Steam, _valve + Vector3.up * 1.6f, 0.5f);
            if (Game.Hud != null) Game.Hud.Notify("The valve groans open. Stay close and keep the attackers off it.", 4f);
        }

        protected override void Tick(float dt, float now)
        {
            bool near = PlayerNear(_valve, ValveOperation04E.NearRadius);
            int threats = Squad.CountNear(_valve, ValveOperation04E.ThreatRadius);
            int w = _op.Tick(dt, near, threats);
            if (w >= 0) SpawnWave(w, _valve, _valve + Vector3.up);
            else if (w == -2)
            {
                Squad.Dismiss(true);
                if (Game.Fx != null) Game.Fx.Sparks(_valve + Vector3.up * 1.6f, Vector3.up * 3f, 30);
                Underheart04ESounds.Play(UhSfx.SteamBurst, _valve + Vector3.up, 1f, 0.7f);
                if (Game.Hud != null) Game.Hud.Notify("The valve seized: the attackers broke the operation. Start it again at the wheel.", 5f);
                Log.Info("UBOSS04E mechanism crown_valve seized (attempt " + Attempt + ")");
            }
            _spin += (_op.Started ? (near ? 80f : 15f) : 0f) * dt;
            if (_wheel != null) _wheel.localRotation = Quaternion.Euler(0f, 0f, _spin);
            UnderBossFx.Emission(_wheelR, new Color(1f, 0.68f, 0.28f) * (0.6f + 3f * _op.Progress));
            if (!_op.Started) Handle.Objective = _op.Resets > 0 ? "The valve seized. Start the operation again at the wheel." : "Open the Choir valve at its wheel, then defend it: three announced waves.";
            else if (!_op.FullyOpen) Handle.Objective = near ? "Hold near the valve while it opens. Attackers at the valve wear it down." : "Get back to the valve: it only opens while you are near.";
            else Handle.Objective = "The valve is open. Clear the last attackers.";
            if (_op.Done(Squad.Living)) End(true, "The Choir valve is open and held.");
        }

        public override float Progress01 { get { return _op != null ? _op.Progress : 0f; } }

        public override string ExtraLine
        {
            get
            {
                if (_op == null) return "";
                return "Integrity " + Mathf.RoundToInt(_op.Integrity) + "%   Waves " + _op.WavesDue + " of " + ValveOperation04E.WaveMarks.Length + "   Attackers " + Squad.Living;
            }
        }

        public override void Targets(List<Vector3> into, List<string> labels)
        {
            base.Targets(into, labels);
            if (_op == null || (_op.Started && PlayerNear(_valve, ValveOperation04E.NearRadius))) return;
            into.Add(_valve + Vector3.up * 2.4f);
            labels.Add("VALVE");
        }

        protected override void Aftermath(float dt, float now)
        {
            _spin += 20f * dt;
            if (_wheel != null) _wheel.localRotation = Quaternion.Euler(0f, 0f, _spin);
        }
    }
}
