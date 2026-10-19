using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class MissionContext
    {
        // This context already owns the coroutine, actors, HUD and cleanup. No extra singleton/coroutine runner.
        private readonly List<CharacterBase> _performers04L = new List<CharacterBase>();
        private CharacterBase _speaker04L, _partner04L;
        private bool _directing04L, _ownsCamera04L, _controlsBefore04L;
        private Vector3 _sceneOrigin04L, _axis04L;
        private float _healthBefore04L, _shotStart04L, _safetyAt04L;
        private SagaShot04L _shot04L;

        private CharacterBase Actor04L(string name)
        {
            if (SagaDirection04L.Remote(name)) return null;
            if (name == "Malik") return Player;
            CharacterBase best = null;
            float nearest = 144f;
            // Mission duplicates take precedence over ambient cast; never pull someone out of another room.
            for (int pass = 0; pass < 2 && best == null; pass++)
            {
                IList<NpcController> list = pass == 0 ? (IList<NpcController>)Npcs : NpcController.All;
                for (int i = 0; i < list.Count; i++)
                {
                    NpcController n = list[i];
                    if (n == null || n.IsDead || n.IsRagdolled || n.DisplayName != name) continue;
                    Vector3 delta = n.transform.position - Game.PlayerPosition;
                    float d = delta.sqrMagnitude;
                    if (Mathf.Abs(delta.y) > 2.8f || d >= nearest) continue;
                    nearest = d; best = n;
                }
            }
            return best;
        }
        private bool SafePerformance04L()
        {
            if (Player == null || Player.IsDead || Player.IsRagdolled || Player.InVehicle || Player.Entering
                || Player.Climbing || !Player.IsGroundedPublic || Player.Velocity.sqrMagnitude > 1f) return false;
            if (Player.Anim != null && (Player.Anim.AttackT >= 0f || Player.Anim.MeleeT >= 0f
                || Player.Anim.Guard > .1f || Player.Anim.Aiming || Player.Anim.CrystalCast04G > .01f || Player.Anim.DodgeT >= 0f || Player.Anim.Travel != TravelPose.None)) return false;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n != null && !n.IsDead && n.State == NpcState.Fight && !n.Pacifist
                    && (n.transform.position - Game.PlayerPosition).sqrMagnitude < 900f) return false;
            }
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c != null && c.IsEngaged && (c.transform.position - Game.PlayerPosition).sqrMagnitude < 1600f) return false;
            }
            return true;
        }
        private void BeginPerformance04L(string[] pairs)
        {
            EndPerformance04L();
            if (pairs == null || Player == null || Game.Cam == null || GameTime.Paused || !SafePerformance04L()) return;
            if (Game.State != GameState.Playing && Game.State != GameState.Cutscene) return;
            if (Game.State == GameState.Playing && !Player.ControlsEnabled) return;
            StageStoryContacts04L(pairs);
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                CharacterBase actor = Actor04L(pairs[i]);
                if (actor != null && !_performers04L.Contains(actor)) _performers04L.Add(actor);
                if (_partner04L == null && actor != null && !actor.IsPlayer) _partner04L = actor;
            }
            if (_partner04L == null) { _performers04L.Clear(); return; }
            if (!_performers04L.Contains(Player)) _performers04L.Add(Player);
            _sceneOrigin04L = Player.transform.position;
            int chapter04L = SagaDirection04L.Chapter(Def != null ? Def.Id : null);
            if (chapter04L == 0 || chapter04L == 8 || chapter04L == 11)
                Underheart04ESounds.Play(chapter04L == 8 ? UhSfx.ReceiverPing : UhSfx.BondChime,
                    _partner04L.transform.position, .24f, chapter04L == 11 ? .8f : 1.05f);
            _axis04L = _partner04L.transform.position - _sceneOrigin04L;
            _healthBefore04L = Player.Health != null ? Player.Health.Health : 0f;
            _controlsBefore04L = Player.ControlsEnabled;
            _ownsCamera04L = !Game.Cam.InCinematic && Game.State == GameState.Playing;
            _directing04L = true;
            _safetyAt04L = 0f;
            if (_ownsCamera04L)
            {
                Player.ControlsEnabled = false;
                Game.State = GameState.Cutscene;
                if (M != null) M.CinematicHint = true;
            }
        }
        private void LinePerformance04L(string name, string line, int index)
        {
            if (!_directing04L) return;
            _speaker04L = Actor04L(name);
            _shot04L = SagaDirection04L.Shot(SagaDirection04L.Chapter(Def != null ? Def.Id : null), index);
            _shotStart04L = Time.time;
            for (int i = 0; i < _performers04L.Count; i++)
            {
                CharacterBase a = _performers04L[i];
                if (a == null || a.Anim == null) continue;
                CharacterBase focus = a == _speaker04L ? (a.IsPlayer ? _partner04L : Player) : _speaker04L;
                if (focus == null) focus = a.IsPlayer ? _partner04L : Player;
                if (focus == null) continue;
                a.Anim.Direct04L(this, SagaDirection04L.Emotion(a.IsPlayer ? "Malik" : a.DisplayName,
                    SagaDirection04L.Chapter(Def != null ? Def.Id : null)), a == _speaker04L,
                    a.DisplayName == "Luma", focus.HeadPosition, SagaDirection04L.ReadSeconds(line) + .5f);
            }
            TickPerformance04L();
        }
        private void TickPerformance04L()
        {
            if (!_directing04L || GameTime.Paused) return;
            if (Time.time >= _safetyAt04L)
            {
                _safetyAt04L = Time.time + .2f;
                bool harmed = Player != null && Player.Health != null && Player.Health.Health < _healthBefore04L;
                if (!SafePerformance04L() || harmed || _partner04L == null || _partner04L.IsDead
                    || (Player.transform.position - _sceneOrigin04L).sqrMagnitude > 4f
                    || (_partner04L.transform.position - Player.transform.position).sqrMagnitude > 196f)
                { _skipRest = true; EndPerformance04L(); return; }
            }
            // Explicit mission shots (including M00's Manor reveal) retain camera authority.
            // Their actors can perform, but Talk must never replace a camera it did not acquire.
            if (!_ownsCamera04L) return;
            CharacterBase focus = _speaker04L != null && !_speaker04L.IsPlayer ? _speaker04L : _partner04L;
            if (focus == null || Player == null || Game.Cam == null) return;
            Vector3 pos, look; float fov;
            bool reduced = !Settings.CameraShake || Game.CrystalCombat != null && Game.CrystalCombat.ReducedEffects;
            SagaDirection04L.Frame(Player.HeadPosition, focus.HeadPosition, _axis04L, _shot04L,
                Time.time - _shotStart04L, reduced, out pos, out look, out fov, _speaker04L == Player);
            Game.Cam.SetCinematic(pos, look, fov, false);
        }
        public void EndPerformance04L()
        {
            foreach (CharacterBase a in _performers04L) if (a != null && a.Anim != null) a.Anim.Release04L(this);
            _performers04L.Clear();
            _partner04L = _speaker04L = null;
            _directing04L = false;
            if (!_ownsCamera04L) return;
            _ownsCamera04L = false;
            if (Game.Cam != null) Game.Cam.ClearCinematic();
            if (Player != null) Player.ControlsEnabled = _controlsBefore04L;
            if (Game.State == GameState.Cutscene) Game.State = GameState.Playing;
            if (M != null) M.CinematicHint = false;
        }
    }
}
