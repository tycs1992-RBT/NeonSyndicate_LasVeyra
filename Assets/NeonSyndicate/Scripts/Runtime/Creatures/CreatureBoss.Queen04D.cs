using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The upgraded Mirror Wraith Queen, the fight that teaches Mirrorwake. She raises visible mirror focus points round
    /// the hall (each a shootable glass facet with its own health); her signature reflects a beam off one or two of them.
    /// For a short acquire window the faint aim lines follow the player at a limited turn rate, then they lock: lanes
    /// appear on the floor along both legs and fill over the wind-up, and the beam fires exactly along the committed lines
    /// (each leg stopped by the first wall, never re-aimed). Shattering a focus breaks the beam through it; breaking every
    /// beam of a pattern, or letting a pattern fire, opens her core. At two thirds health she turns her mirrors once (a new
    /// arrangement); at one third her one copy batch splits the player's reflection (at most two). The Veil rule is the
    /// director's and is unchanged.
    /// </summary>
    public sealed partial class CreatureBoss
    {
        private sealed class Focus04D
        {
            public GameObject Go;
            public Transform Facet;
            public CharacterHealth Health;
            public Vector3 Pos;
            public bool Alive;
            public float Phase;
        }

        private sealed class QueenBeam04D
        {
            /// <summary>Index into the focus list, or -1 for a direct beam from the Queen.</summary>
            public int Focus;
            public BossBeamAim04D Aim;
            public Vector3 Source;
            public Vector3 A1, B1;
            public Vector3 A2, B2;
            public Beam04DLive Line1;
            public Beam04DLive Line2;
            public Lane04DLive Lane1;
            public Lane04DLive Lane2;
            public bool Broken;
        }

        private const float FocusHeight04D = 3f;
        private static readonly Color BeamColor04D = new Color(0.8f, 0.95f, 1f);
        private static readonly Color FocusMarkColor04D = new Color(0.55f, 0.95f, 1f);

        private readonly List<Focus04D> _foci = new List<Focus04D>();
        private readonly List<QueenBeam04D> _qbeams = new List<QueenBeam04D>();
        private float _lockAt;
        private float _beamLength = BossProfiles04D.BeamLength;
        private bool _beamsLocked;
        private bool _rearranged;
        private float _fociBase;

        private int LiveFoci04D
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _foci.Count; i++) if (_foci[i].Alive) n++;
                return n;
            }
        }

        private void EnterPhaseQueen04D(int phase)
        {
            if (phase == 1 && !_rearranged)
            {
                _rearranged = true;
                Callout("The Queen turns her mirrors: new focus points");
                RaiseFoci04D(true);
                Log.Info("Boss 04D: wraith_queen mirror arrangement changed (" + LiveFoci04D + " foci)");
            }
            if (phase >= 2) QueenCopies04D();
        }

        /// <summary>The Queen's one copy batch (at most two mirror copies sharing her guard), reserved before it spawns.</summary>
        private void QueenCopies04D()
        {
            if (_budget == null || _ledger == null || !_budget.TryReserveBatch()) return;
            int n = _budget.Allowance(BossProfiles04D.MaxSupportActors, AliveSupports04D);
            if (n <= 0) return;
            SpawnCopies("mirror_wraith", n);
            int made = 0;
            for (int i = 0; i < _copies.Count; i++)
            {
                if (_copies[i] == null || _copies[i].IsDead) continue;
                _ledger.Open(BossOwned04D.Illusion);
                made++;
            }
            _budget.NoteAdmitted(made);
            Callout("The Queen splits your reflection");
            Log.Info("Boss 04D: wraith_queen copy batch, " + made + " copies");
        }

        // ================================================================== focus points

        /// <summary>
        /// Raises the mirror focus points evenly round the arena (a new arrangement turns the old one by half a step),
        /// each in open air, three metres up and in the Queen's sight. Fewer may stand where the hall is crowded; with
        /// none she fires her beams straight.
        /// </summary>
        private void RaiseFoci04D(bool rearrange)
        {
            ClearFoci04D(rearrange);
            int n = BossProfiles04D.FociCount(Diff04D);
            float radius = Mathf.Clamp(ArenaRadius * 0.45f, 8f, 15f);
            float baseDeg = rearrange ? _fociBase + 180f / n : Random.value * 360f;
            _fociBase = baseDeg;
            Vector3 queen = Npc.ChestPosition;
            for (int i = 0; i < n; i++)
            {
                for (int tries = 0; tries < 4; tries++)
                {
                    float a = (baseDeg + i * 360f / n + (tries > 0 ? Random.Range(-25f, 25f) : 0f)) * Mathf.Deg2Rad;
                    float r = radius * (tries > 1 ? 0.75f : 1f);
                    Vector3 p = ArenaCenter + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r;
                    float g;
                    if (!CreatureDirector.GroundAt(p, 8f, out g)) continue;
                    p.y = g + FocusHeight04D;
                    if (Physics.CheckSphere(p, 0.9f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    if (Physics.Linecast(queen, p, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    Focus04D f = CreateFocus04D(p, i);
                    if (f != null) _foci.Add(f);
                    break;
                }
            }
        }

        /// <summary>One mirror focus: a glass facet round a bright prism, a shootable sphere on the shield layer (it blocks shots, never bodies or cars).</summary>
        private Focus04D CreateFocus04D(Vector3 pos, int index)
        {
            if (_ledger == null || !_ledger.Open(BossOwned04D.Focus)) return null;
            GameObject go = new GameObject("Queen Mirror Focus");
            go.layer = GadgetModels.ShieldLayer;
            go.transform.position = pos;
            SphereCollider col = go.AddComponent<SphereCollider>();
            col.radius = 0.85f;
            CharacterHealth h = go.AddComponent<CharacterHealth>();
            h.MaxHealth = BossProfiles04D.FocusHealth(Npc.Health.MaxHealth);
            h.Health = h.MaxHealth;
            h.Armor = 0f;
            h.HeadshotMultiplier = 1f;
            h.HeadHeight = 9999f;
            h.Surface = ImpactSurface.Glass;
            h.SurfaceTint = Species.HitColor;
            TeamTag tag = go.AddComponent<TeamTag>();
            tag.Team = Team.Creature;
            GameObject facet = Shapes.MeshObject("Facet", Shapes.CubeMesh, new[] { Mats.Emissive(new Color(0.78f, 0.9f, 1f), 1.6f) }, go.transform, false);
            facet.transform.localScale = new Vector3(1.1f, 1.5f, 0.07f);
            GameObject prism = Shapes.MeshObject("Prism", Shapes.CubeMesh, new[] { Mats.Emissive(Species.HitColor, 3.2f) }, facet.transform, false);
            prism.transform.localScale = new Vector3(0.24f, 0.18f, 4f);
            prism.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Focus04D f = new Focus04D();
            f.Go = go;
            f.Facet = facet.transform;
            f.Health = h;
            f.Pos = pos;
            f.Alive = true;
            f.Phase = index * 1.3f;
            h.Died += info => OnFocusDied04D(f, info);
            if (Game.Fx != null) Game.Fx.GlassBurst(pos, Species.HitColor, 14);
            return f;
        }

        private void OnFocusDied04D(Focus04D f, DamageInfo info)
        {
            if (f == null || !f.Alive) return;
            f.Alive = false;
            if (Game.Fx != null) Game.Fx.GlassBurst(f.Pos, Species.HitColor, 30);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.GlassBreak, f.Pos, 0.9f, 0.1f, 70f);
            if (info.FromPlayer) Callout("Focus shattered");
            if (_ledger != null) _ledger.Release(BossOwned04D.Focus);
            if (f.Go != null) Destroy(f.Go);
            f.Go = null;
        }

        private void UpdateFoci04D(float dt)
        {
            float now = Time.time;
            for (int i = 0; i < _foci.Count; i++)
            {
                Focus04D f = _foci[i];
                if (!f.Alive || f.Go == null) continue;
                f.Go.transform.position = f.Pos + Vector3.up * (0.12f * Mathf.Sin(now * 1.6f + f.Phase));
                if (f.Facet != null) f.Facet.localRotation = Quaternion.Euler(0f, now * 35f + f.Phase * 40f, 0f);
            }
        }

        /// <summary>Every focus goes (shattering when the arrangement changes). Returns how many stood.</summary>
        private int ClearFoci04D(bool shatter)
        {
            int n = 0;
            for (int i = 0; i < _foci.Count; i++)
            {
                Focus04D f = _foci[i];
                if (!f.Alive) continue;
                n++;
                f.Alive = false;
                if (shatter && Game.Fx != null) Game.Fx.GlassBurst(f.Pos, Species.HitColor, 20);
                if (_ledger != null) _ledger.Release(BossOwned04D.Focus);
                if (f.Go != null) Destroy(f.Go);
                f.Go = null;
            }
            _foci.Clear();
            return n;
        }

        private void DrawFoci04D(Camera cam, float w, float h)
        {
            Vector3 pp = Game.PlayerPosition;
            for (int i = 0; i < _foci.Count; i++)
            {
                Focus04D f = _foci[i];
                if (!f.Alive || f.Health == null) continue;
                if ((f.Pos - pp).sqrMagnitude > 80f * 80f) continue;
                Vector2 g;
                if (!ToGui04D(cam, f.Pos, w, h, out g)) continue;
                const float s = 26f;
                UIKit.Tex(new Rect(g.x - s * 0.5f, g.y - s * 0.5f, s, s), UIKit.Ring, FocusMarkColor04D);
                UIKit.Text(new Rect(g.x - 70f, g.y - s * 0.5f - 20f, 140f, 18f), "FOCUS", 13, FocusMarkColor04D, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Bar(new Rect(g.x - 22f, g.y + s * 0.5f + 2f, 44f, 3f), f.Health.Health / Mathf.Max(1f, f.Health.MaxHealth), FocusMarkColor04D, new Color(0f, 0f, 0f, 0.5f));
            }
        }

        // ================================================================== reflected beams

        /// <summary>
        /// Starts a pattern: one beam in phase one, two after, each off an intact focus she can see (a straight beam when
        /// none stands). The faint aim lines follow the player for the acquire window before they lock.
        /// </summary>
        private bool BeginBeams04D(float now)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return false;
            _qbeams.Clear();
            _beamsLocked = false;
            Vector3 queen = Npc.ChestPosition;
            Vector3 aim = PlayerAimPoint04D(p);
            _beamLength = BossProfiles04D.BeamLengthFor(Vector3.Distance(queen, aim));
            int want = BossProfiles04D.BeamsPerPattern(Phase);
            int start = Random.Range(0, Mathf.Max(1, _foci.Count));
            for (int k = 0; k < _foci.Count && _qbeams.Count < want; k++)
            {
                int i = (start + k) % _foci.Count;
                Focus04D f = _foci[i];
                if (!f.Alive) continue;
                if (Physics.Linecast(queen, f.Pos, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                QueenBeam04D b = new QueenBeam04D();
                b.Focus = i;
                b.Source = f.Pos;
                b.Aim = new BossBeamAim04D(aim - f.Pos);
                b.Line1 = AddBeam04D(queen, f.Pos, 0.07f, BeamColor04D, 0.35f, _moveSerial);
                b.Line2 = AddBeam04D(f.Pos, ClampBeam04D(f.Pos, b.Aim.Aim), 0.07f, BeamColor04D, 0.3f, _moveSerial);
                _qbeams.Add(b);
            }
            if (_qbeams.Count == 0)
            {
                QueenBeam04D b = new QueenBeam04D();
                b.Focus = -1;
                b.Source = queen;
                b.Aim = new BossBeamAim04D(aim - queen);
                b.Line2 = AddBeam04D(queen, ClampBeam04D(queen, b.Aim.Aim), 0.07f, BeamColor04D, 0.3f, _moveSerial);
                _qbeams.Add(b);
            }
            float acquire = BossTiming04D.AcquireSeconds(Diff04D);
            float tele = BossTiming04D.Telegraph(BossMove04D.QueenBeams, Diff04D);
            _lockAt = now + acquire;
            _beatUntil = _lockAt + tele;
            _beat = Beat04D.Telegraph;
            Cast(acquire + tele + 0.2f);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Shield, queen, 0.7f, 1.3f, 60f);
            return true;
        }

        /// <summary>Where a beam from from along dir stops: its full length (enough to reach the player) or the first wall.</summary>
        private Vector3 ClampBeam04D(Vector3 from, Vector3 dir)
        {
            Vector3 end = from + dir * _beamLength;
            RaycastHit hit;
            if (Physics.Linecast(from, end, out hit, Layers.WorldMask, QueryTriggerInteraction.Ignore)) end = hit.point;
            return end;
        }

        private void TickBeamsTelegraph04D(float now)
        {
            PlayerController p = Game.Player;
            Vector3 queen = Npc.ChestPosition;
            Vector3 aim = p != null ? PlayerAimPoint04D(p) : queen + Npc.transform.forward * 10f;
            float dt = Time.deltaTime;
            int live = 0;
            for (int i = 0; i < _qbeams.Count; i++)
            {
                QueenBeam04D b = _qbeams[i];
                if (b.Broken) continue;
                if (b.Focus >= 0 && (b.Focus >= _foci.Count || !_foci[b.Focus].Alive))
                {
                    BreakBeam04D(b);
                    continue;
                }
                live++;
                if (_beamsLocked) continue;
                if (b.Focus >= 0) SetBeam04D(b.Line1, queen, b.Source);
                else b.Source = queen;
                b.Aim.Track(aim - b.Source, BossTiming04D.AimTurnRate(Diff04D), dt);
                SetBeam04D(b.Line2, b.Source, ClampBeam04D(b.Source, b.Aim.Aim));
            }
            if (live == 0)
            {
                // The player broke every beam of the pattern: it fizzles and her core opens anyway.
                Callout("Her pattern breaks");
                ReleaseMoveLanes04D(_moveSerial);
                ReleaseMoveBeams04D(_moveSerial);
                _qbeams.Clear();
                BeginRecovery04D(now, BossMove04D.QueenBeams);
                return;
            }
            if (!_beamsLocked && now >= _lockAt) LockBeams04D(now);
        }

        /// <summary>
        /// The aim locks: both legs of every beam are fixed (the first wall stops each), lanes mark them on the floor and
        /// fill over the wind-up. A second beam is dropped if together they would leave no reachable safe spot.
        /// </summary>
        private void LockBeams04D(float now)
        {
            _beamsLocked = true;
            Vector3 queen = Npc.ChestPosition;
            Vector3 feet = Npc.transform.position;
            PlayerController p = Game.Player;
            Vector3 player = p != null ? p.transform.position : feet;
            Lane04D[] lanes = _laneScratch04D;
            float fill = Mathf.Max(BossTiming04D.TelegraphMin, _beatUntil - now);
            float y = GroundY04D(feet, 2f);
            int n = 0;
            for (int i = 0; i < _qbeams.Count; i++)
            {
                QueenBeam04D b = _qbeams[i];
                if (b.Broken) continue;
                b.Aim.Lock();
                int before = n;
                if (b.Focus >= 0)
                {
                    b.A1 = queen;
                    b.B1 = b.Source;
                    if (n < lanes.Length) lanes[n++] = Lane04D.Make(feet, b.Source - feet, MathUtil.FlatDistance(feet, b.Source), BossProfiles04D.BeamRadius);
                }
                b.A2 = b.Source;
                b.B2 = ClampBeam04D(b.Source, b.Aim.Aim);
                Vector3 ground = new Vector3(b.A2.x, GroundY04D(b.A2, 1f), b.A2.z);
                if (n < lanes.Length) lanes[n++] = Lane04D.Make(ground, b.B2 - b.A2, Mathf.Max(2f, MathUtil.FlatDistance(b.A2, b.B2)), BossProfiles04D.BeamRadius);
                Vector3 spot;
                if (before > 0 && !BossLanes04D.FindSafeSpot(lanes, n, player, 0f, player, BossLanes04D.SafeReach, ArenaCenter, ArenaRadius + 20f, out spot))
                {
                    n = before;
                    BreakBeam04D(b);
                    continue;
                }
                // Locked lines read a little brighter than the lines still following the player.
                if (b.Line1 != null) b.Line1.Alpha = 0.45f;
                if (b.Line2 != null) b.Line2.Alpha = 0.45f;
                if (b.Line1 != null) SetBeam04D(b.Line1, b.A1, b.B1);
                SetBeam04D(b.Line2, b.A2, b.B2);
                if (b.Focus >= 0) b.Lane1 = AddLane04D(lanes[before], y, 0f, fill, 0f, DamageFlags.Unblockable, BeamColor04D, _moveSerial);
                b.Lane2 = AddLane04D(lanes[n - 1], y, 0f, fill, 0f, DamageFlags.Unblockable, BeamColor04D, _moveSerial);
            }
        }

        private void BreakBeam04D(QueenBeam04D b)
        {
            if (b.Broken) return;
            b.Broken = true;
            ReleaseBeam04D(b.Line1);
            ReleaseBeam04D(b.Line2);
            RemoveLane04D(b.Lane1);
            RemoveLane04D(b.Lane2);
            b.Line1 = null;
            b.Line2 = null;
            b.Lane1 = null;
            b.Lane2 = null;
        }

        /// <summary>
        /// The beams fire along their committed lines: a bright flash on both legs and a hit on a player within the beam's
        /// radius of either leg (shins, chest or head; the car when driving). The floor lanes flash out with them.
        /// </summary>
        private void CommitBeams04D(float now)
        {
            PlayerController p = Game.Player;
            bool fired = false;
            for (int i = 0; i < _qbeams.Count; i++)
            {
                QueenBeam04D b = _qbeams[i];
                if (b.Broken) continue;
                if (b.Focus >= 0 && (b.Focus >= _foci.Count || !_foci[b.Focus].Alive))
                {
                    BreakBeam04D(b);
                    continue;
                }
                fired = true;
                FlashBeam04D(b.Line1, 0.3f, 0.35f);
                FlashBeam04D(b.Line2, 0.3f, 0.35f);
                if (Game.Fx != null) Game.Fx.GlassBurst(b.B2, Species.HitColor, 12);
                if (p == null || p.IsDead) continue;
                bool hit;
                if (p.InVehicle && p.CurrentVehicle != null)
                {
                    Vector3 c = p.CurrentVehicle.transform.position + Vector3.up;
                    hit = CreatureFightRules.NearShotLine(b.A2, b.B2, c, BossProfiles04D.BeamRadius + 1.2f) || (b.Focus >= 0 && CreatureFightRules.NearShotLine(b.A1, b.B1, c, BossProfiles04D.BeamRadius + 1.2f));
                }
                else
                {
                    Vector3 feet = p.transform.position;
                    float tall = Mathf.Max(1.6f, p.BodyHeight);
                    hit = BossLanes04D.BeamHitsBody(b.A2, b.B2, BossProfiles04D.BeamRadius, feet, tall) || (b.Focus >= 0 && BossLanes04D.BeamHitsBody(b.A1, b.B1, BossProfiles04D.BeamRadius, feet, tall));
                }
                if (hit) HitPlayer04D(0.9f, b.Source, DamageType.Melee, DamageFlags.Unblockable, 4f);
            }
            if (fired && Game.Audio != null) Game.Audio.PlayVar(SfxId.GlassBreak, Npc.ChestPosition, 0.9f, 0.1f, 80f);
            if (fired && Game.Cam != null) Game.Cam.Shake(0.2f);
            // The marker lanes flash out now (they carry no damage of their own).
            for (int i = 0; i < _lanes04D.Count; i++)
            {
                Lane04DLive l = _lanes04D[i];
                if (l.Serial != _moveSerial || l.Burst) continue;
                l.Burst = true;
                l.BurstT = 0f;
            }
            _qbeams.Clear();
            _beat = Beat04D.Active;
            _beatUntil = now + 0.35f;
        }
    }
}
