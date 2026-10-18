using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Finite local crystal enforcement. Story wanted state and explicit mission spawns retain authority.</summary>
    public sealed partial class PoliceManager
    {
        private int _incidentSpent04F;
        private float _incidentStart04F = -1f;
        private float _containUntil04F;
        private float _quietUntil04F;
        public int IncidentDispatched04F { get { return _incidentSpent04F; } }
        public bool StoryPursuit04F
        {
            get
            {
                MissionContext mission = Game.Missions != null ? Game.Missions.Active : null;
                return CityCombatRules04F.StoryOwnsPursuit(Game.Wanted != null && Game.Wanted.Logic.Frozen,
                    mission != null && mission.Def != null, mission != null && mission.Def != null && mission.Def.Story,
                    mission != null && mission.Def != null && mission.Def.Quiet);
            }
        }
        public bool LocalContainment04F { get { return !StoryPursuit04F && Time.time < _containUntil04F; } }
        private bool MayDispatch04F
        {
            get { return CityCombatRules04F.LocalCanDispatch(StoryPursuit04F, LocalContainment04F,
                _incidentStart04F < 0f ? 0f : Time.time - _incidentStart04F,
                CityCombatRules04F.IncidentOfficers(Settings.Difficulty, Game.Wanted != null ? Game.Wanted.Stars : 1) - _incidentSpent04F); }
        }

        private static bool ProtectedCrew04F(Vehicle vehicle)
        {
            if (vehicle == null) return false;
            foreach (CharacterBase occupant in vehicle.Occupants)
            {
                NpcController actor = occupant as NpcController;
                if (actor != null && (actor.Persistent || !string.IsNullOrEmpty(actor.MissionTag))) return true;
            }
            return false;
        }

        private static bool OffCameraDispatch04F(Vector3 position)
        {
            Camera camera = Game.Cam != null ? Game.Cam.Cam : null;
            if (camera == null || !PedManager.IsVisible(camera, position + Vector3.up)) return true;
            return Physics.Linecast(camera.transform.position, position + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        private void BeginContainment04F()
        {
            if (StoryPursuit04F || LocalContainment04F) return;
            _containUntil04F = Time.time + CityCombatRules04F.ContainmentSeconds;
            _responseReady = false;
            if (Heli != null) Heli.Leave();
            if (Game.Wanted != null) Game.Wanted.SuppressSightings(CityCombatRules04F.ContainmentSeconds);
            foreach (NpcController officer in Officers)
            {
                if (officer == null || officer.IsDead || officer.Persistent || !string.IsNullOrEmpty(officer.MissionTag)) continue;
                officer.Target = null;
                officer.Hostile = false;
                officer.Weapons.CancelActions();
                CombatDirector.ReleaseMeleeToken(officer);
                CombatDirector.ReleaseShotToken(officer);
                officer.GuardAt(officer.transform.position);
            }
            if (Game.Hud != null) Game.Hud.Subtitle("Crystal Wardens", "Squad withdrawn to containment. No further local wave. Break sight and leave the marked search area.", 6f);
        }

        private CrystalNpcRole04F WardenRole04F(int severity, bool heavy, int ordinal)
        {
            int choice = (_incidentSpent04F + ordinal) % 4;
            return heavy && choice == 0 ? CrystalNpcRole04F.WardSentinel
                : choice == 1 ? CrystalNpcRole04F.FocusWeaver : choice == 2 ? CrystalNpcRole04F.WardSentinel
                : choice == 3 && severity >= 2 ? CrystalNpcRole04F.Marshal : CrystalNpcRole04F.BladeWarden;
        }

        private Appearance WardenLook04F(int severity, bool heavy, int ordinal)
        {
            CrystalNpcRole04F role = WardenRole04F(severity, heavy, ordinal);
            Appearance look = heavy ? BodyFactory.SwatLook() : BodyFactory.PoliceLook();
            look.SuitStyle = 3;
            look.ShellStyle = role == CrystalNpcRole04F.Marshal || role == CrystalNpcRole04F.WardSentinel ? 3 : role == CrystalNpcRole04F.BladeWarden ? 2 : 1;
            look.VisorStyle = role == CrystalNpcRole04F.Marshal ? 2 : role == CrystalNpcRole04F.FocusWeaver ? 1 : 0;
            look.GauntletStyle = role == CrystalNpcRole04F.Marshal ? 2 : 1;
            look.Jacket = role == CrystalNpcRole04F.Marshal || role == CrystalNpcRole04F.FocusWeaver;
            look.ArmorColor = role == CrystalNpcRole04F.Marshal ? 4 : 9;
            return look;
        }

        private void ArmWarden04F(NpcController actor, int severity, bool heavy, int ordinal)
        {
            if (actor == null) return;
            CrystalNpcRole04F role = WardenRole04F(severity, heavy, ordinal);
            actor.SetCrystalRole04F(role, true);
            if (role == CrystalNpcRole04F.FocusWeaver && severity >= 3) actor.Arm(WeaponId.LongWatch);
        }
    }
}
