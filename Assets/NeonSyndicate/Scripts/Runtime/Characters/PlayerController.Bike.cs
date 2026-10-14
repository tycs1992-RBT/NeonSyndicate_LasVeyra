using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Getting on a hover bike: step to whichever side is nearer, shove the rider off if someone is riding it, and hop
    /// onto the seat in about half a second (bikes have no doors). Getting off is one press (UpdateInVehicle); the
    /// vehicle code keeps the rider's animator fields (Ride, RideRoot, RideLean) up to date while riding.
    /// </summary>
    public sealed partial class PlayerController
    {
        /// <summary>Seconds the hop from the ground onto the seat takes.</summary>
        private const float BikeHopTime = 0.3f;

        private int _bikeSide = -1;
        private Vector3 _hopFrom;
        private float _hopFromYaw;

        private void BeginEnterBike(Vehicle v)
        {
            _enterTarget = v;
            _enterSeat = 0;
            Vector3 local = v.transform.InverseTransformPoint(transform.position);
            _bikeSide = local.x < 0f ? -1 : 1;
            _enter = EnterPhase.Approach;
            _enterTimer = 0f;
            Aiming = false;
            Crouched = false;
        }

        private void UpdateEnteringBike(Vehicle v, float dt, bool canControl)
        {
            switch (_enter)
            {
                case EnterPhase.Approach:
                {
                    if (canControl && (Act.VehicleKey || Act.PadUse || GInput.Move.sqrMagnitude > 0.5f && _enterTimer > 0.35f))
                    {
                        CancelEnter();
                        return;
                    }
                    Vector3 to = MathUtil.Flat(v.MountPoint(_enterSeat, _bikeSide) - transform.position);
                    float dist = to.magnitude;
                    if (dist < 0.4f || _enterTimer > 1.2f)
                    {
                        CharacterBase rider = v.Occupants[_enterSeat];
                        Controller.enabled = false;
                        _hopFrom = transform.position;
                        _hopFromYaw = _yaw;
                        _enterTimer = 0f;
                        if (rider != null && rider != this)
                        {
                            KnockOffBikeRider(v, rider);
                            _enter = EnterPhase.Jack;
                        }
                        else _enter = EnterPhase.Sit;
                        return;
                    }
                    Vector3 step = to.normalized * Mathf.Min(dist, 5f * dt);
                    _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(to), 900f * dt);
                    transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                    _vy = Controller.isGrounded ? -2f : _vy - 19.6f * dt;
                    Controller.Move(step + Vector3.up * _vy * dt);
                    _vel = to.normalized * 5f;
                    Anim.Velocity = _vel;
                    Anim.Grounded = true;
                    Anim.Aiming = false;
                    break;
                }
                case EnterPhase.Jack:
                {
                    // A quick shove (the rider is already flying), then the hop.
                    Anim.Velocity = Vector3.zero;
                    if (_enterTimer > 0.15f)
                    {
                        _enter = EnterPhase.Sit;
                        _enterTimer = 0f;
                        _hopFrom = transform.position;
                        _hopFromYaw = _yaw;
                    }
                    break;
                }
                default:
                {
                    if (v.Occupants[_enterSeat] != null && v.Occupants[_enterSeat] != this)
                    {
                        CancelEnter();
                        return;
                    }
                    // The hop: swing up and over onto the seat, turning to face along the bike.
                    float t = Mathf.Clamp01(_enterTimer / BikeHopTime);
                    Vector3 seat = v.SeatTransform(_enterSeat).position;
                    transform.position = Vector3.Lerp(_hopFrom, seat, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.3f;
                    _yaw = Mathf.LerpAngle(_hopFromYaw, v.transform.eulerAngles.y, t);
                    transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                    Anim.Velocity = Vector3.zero;
                    if (t < 1f) break;
                    if (!v.StolenByPlayer) EvictBikePassengers(v);
                    EnterVehicleImmediate(v, _enterSeat);
                    _enter = EnterPhase.None;
                    _enterTarget = null;
                    if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, seat, 0.35f, 1.6f, 20f);
                    break;
                }
            }
        }

        /// <summary>Knocks a rider off the far side of the bike (a carjacking, reported like one).</summary>
        private void KnockOffBikeRider(Vehicle v, CharacterBase rider)
        {
            rider.ExitVehicleImmediate(false);
            Vector3 away = v.transform.right * -_bikeSide;
            rider.transform.position = v.MountPoint(_enterSeat, -_bikeSide) + Vector3.up * 0.05f;
            rider.Knockdown(away * 3.4f + Vector3.up * 1.6f, rider.ChestPosition, 1.4f);
            NpcController npc = rider as NpcController;
            if (npc != null) npc.OnCarjacked(this);
            if (Game.Wanted != null) Game.Wanted.ReportCrime(rider.Team != null && rider.Team.Team == NeonSyndicate.Team.Police ? CrimeType.AttackCop : CrimeType.Carjack, v.transform.position, rider.gameObject);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Punch, rider.ChestPosition, 0.6f, 0.1f, 25f);
        }

        /// <summary>The pillion of a jacked bike gets off: cops and crews fight, civilians run. Companions and mission passengers stay.</summary>
        private void EvictBikePassengers(Vehicle v)
        {
            for (int i = 1; i < v.Occupants.Length; i++)
            {
                NpcController other = v.Occupants[i] as NpcController;
                if (other == null || other.Role == NpcRole.Companion) continue;
                bool hostile = other.Role == NpcRole.Police || other.Role == NpcRole.Swat || other.Role == NpcRole.Gang || other.Role == NpcRole.Enemy;
                if (!hostile && other.MissionTag != null) continue;
                other.ExitVehicleImmediate(false);
                if (hostile) other.Attack(this);
                else other.Flee(transform.position, 12f);
            }
        }
    }
}
