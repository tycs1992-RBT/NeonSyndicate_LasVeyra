using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Classic open-world side content: Nico Ash's street races, rampages, Iris Moon's payphone contracts and vehicle jobs
    /// started with J (taxi fares in a cab, vigilante in a police vehicle, paramedic in an ambulance). Also keeps
    /// service vehicles parked outside hospitals, police stations and casinos so those jobs are easy to find.
    /// Everything runs through the MissionManager as side missions, so fail checks, retries and cleanup are shared.
    /// </summary>
    public sealed class Activities : MonoBehaviour
    {
        public const int RaceUnlock = 3;
        public const int ContractUnlock = 4;
        public const int RampageUnlock = 6;

        /// <summary>Ids of finished one-time activities (first race wins, rampages, contracts, job milestones). Saved.</summary>
        public readonly HashSet<string> Completed = new HashSet<string>();

        private sealed class RaceInfo
        {
            public string Id;
            public string Name;
            public Vector3 Start;
            public float Yaw;
            public Vector3 TriggerPos;
            public Vector3[] Way;
            public string[] Rivals;
            public int Prize;
            public string Intro;
            public Interactable Trigger;
            public Interactable Hint;
            public WorldMarker Marker;
            public Blip Blip;
            public float LockUntil;
        }

        private sealed class RampageInfo
        {
            public string Id;
            public string Name;
            public Vector3 Pos;
            public int Gang;
            public WeaponId Weapon;
            public int Kills;
            public float Seconds;
            public int Prize;
            public GameObject Token;
            public Blip Blip;
            public float LockUntil;
        }

        private sealed class ContractInfo
        {
            public string Id;
            public string Title;
            public string Brief;
            public int Reward;
            public Func<MissionContext, IEnumerator> Script;
        }

        private sealed class ServiceSpot
        {
            public VehicleDef Def;
            public Vector3 Pos;
            public float Yaw;
            public Vehicle Current;
        }

        /// <summary>Per-job bookkeeping shared by the job loops (iterators cannot take ref parameters).</summary>
        private sealed class JobState
        {
            public float Outside;
            public int Count;
            public int Earned;
            public int Level;
        }

        private readonly List<RaceInfo> _races = new List<RaceInfo>();
        private readonly List<RampageInfo> _rampages = new List<RampageInfo>();
        private readonly List<ContractInfo> _contracts = new List<ContractInfo>();
        private readonly List<ServiceSpot> _spots = new List<ServiceSpot>();
        private readonly List<Poi> _payphones = new List<Poi>();
        private readonly List<int> _tmp = new List<int>();
        private Poi _ringing;
        private float _ringUntil;
        private float _nextRing = 75f;
        private Blip _ringBlip;
        private GameObject _ringObject;
        private float _refreshTimer;
        private float _spotTimer;
        private MissionDef _jobDef;
        private bool _jobQuit;
        private Vehicle _hintVehicle;
        private MissionDef _activeActivity;
        private readonly System.Random _rand = new System.Random(4242);

        private static readonly Color RaceColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color RampageColor = new Color(1f, 0.32f, 0.18f);
        private static readonly Color PhoneColor = new Color(0.35f, 1f, 0.5f);

        // ================================================================== setup

        public void Init(CityLayout L)
        {
            if (L == null) return;
            foreach (Poi p in L.Pois) if (p.Type == PoiType.Payphone) _payphones.Add(p);
            try { BuildRaces(L); } catch (Exception e) { Log.Exception("Activities races", e); }
            try { BuildRampages(L); } catch (Exception e) { Log.Exception("Activities rampages", e); }
            try { BuildContracts(); } catch (Exception e) { Log.Exception("Activities contracts", e); }
            try { BuildServiceSpots(L); } catch (Exception e) { Log.Exception("Activities service vehicles", e); }
            Refresh();
        }

        /// <summary>Forgets all progress (new game).</summary>
        public void ResetAll()
        {
            Completed.Clear();
            StopRinging();
            _nextRing = Time.time + 75f;
            _jobQuit = false;
            _jobDef = null;
            _activeActivity = null;
            foreach (RaceInfo r in _races) r.LockUntil = 0f;
            foreach (RampageInfo r in _rampages) r.LockUntil = 0f;
            Refresh();
        }

        /// <summary>Called after a save is applied so markers match the loaded progress.</summary>
        public void OnLoaded()
        {
            StopRinging();
            _nextRing = Time.time + 60f;
            Refresh();
        }

        private static bool FreeRoam { get { return Game.Root != null && Game.Root.FreeRoam; } }
        private static int Progress { get { return Game.Missions != null ? Game.Missions.StoryProgress : 0; } }

        public static bool IsUnlocked(int storyNeeded)
        {
            return FreeRoam || Progress >= storyNeeded;
        }

        private static bool NoMission { get { return Game.Missions != null && Game.Missions.Active == null; } }

        private bool CanStart(bool explain)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || !Game.IsPlaying || Game.Missions == null) return false;
            if (Game.Missions.Active != null) return false;
            if (Game.Wanted != null && Game.Wanted.Stars > 0)
            {
                if (explain && Game.Hud != null) Game.Hud.Notify("Lose the police first.", 2.5f);
                return false;
            }
            return true;
        }

        // ================================================================== update

        private void Update()
        {
            if (Game.Layout == null || Game.Player == null) return;
            float dt = Time.deltaTime;
            _refreshTimer -= dt;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 1f;
                Refresh();
            }
            _spotTimer -= dt;
            if (_spotTimer <= 0f)
            {
                _spotTimer = 2f;
                UpdateServiceSpots();
            }
            if (!Game.IsPlaying) return;
            UpdatePayphones();
            // Re-arm activity markers a little after an activity ends, so finishing on the start line doesn't restart it.
            if (_activeActivity != null && (Game.Missions == null || Game.Missions.Active == null || Game.Missions.Active.Def != _activeActivity))
            {
                foreach (RaceInfo r in _races) if (r.LockUntil > Time.time + 1000f) r.LockUntil = Time.time + 20f;
                foreach (RampageInfo r in _rampages) if (r.LockUntil > Time.time + 1000f) r.LockUntil = Time.time + 20f;
                _activeActivity = null;
            }
            // Job hint when getting into a taxi, police vehicle or ambulance.
            Vehicle pv = Game.PlayerVehicle;
            if (pv != _hintVehicle)
            {
                _hintVehicle = pv;
                string kind = JobKindFor(pv);
                if (kind != null && NoMission && Game.Player.Seat == 0 && Game.Hud != null)
                    Game.Hud.Notify("Press J to start " + JobLabel(kind) + " missions.", 4f);
            }
        }

        /// <summary>Shows or hides markers and blips to match story progress and what's already done.</summary>
        private void Refresh()
        {
            bool racesOn = IsUnlocked(RaceUnlock);
            bool rampOn = IsUnlocked(RampageUnlock);
            bool idle = NoMission;
            foreach (RaceInfo r in _races)
            {
                if (r.Trigger != null) r.Trigger.gameObject.SetActive(racesOn);
                if (r.Hint != null) r.Hint.gameObject.SetActive(racesOn);
                if (r.Marker != null) r.Marker.gameObject.SetActive(racesOn && idle);
                if (r.Blip != null)
                {
                    r.Blip.Visible = racesOn;
                    r.Blip.Label = "Street race: " + r.Name + (Completed.Contains(r.Id) ? " (won, rerun for " + MathUtil.Money(r.Prize / 3) + ")" : " (" + MathUtil.Money(r.Prize) + ")");
                }
            }
            foreach (RampageInfo r in _rampages)
            {
                bool show = rampOn && !Completed.Contains(r.Id);
                if (r.Token != null) r.Token.SetActive(show && idle);
                if (r.Blip != null) r.Blip.Visible = show;
            }
        }

        // ================================================================== street races

        private void BuildRaces(CityLayout L)
        {
            // The three city races come from one table (CityRacesH8S): the original routes in the original city, and real-road
            // routes in revision 10, which has no downtown street grid. Ids, names, rivals and prizes are the same in both.
            foreach (CityRaceRouteH8S race in CityRacesH8S.StreetRaces(L != null && L.GothicCivicH5))
                AddRace(L, race.Id, race.Name, race.Near, race.Way, race.Rivals, race.Prize, race.Intro);
            Vector3 motel = MissionManager.SitePos("StarliteMotel", new Vector3(1290f, 0f, -350f));
            AddRace(L, "race_badlands", "Badlands Rally", motel + new Vector3(-70f, 0f, 0f),
                new[]
                {
                    MissionManager.SitePos("Airstrip", new Vector3(1340f, 0f, 750f)),
                    new Vector3(300f, 0f, 1150f),
                    MissionManager.SitePos("Quarry", new Vector3(-1340f, 0f, 700f)),
                    MissionManager.SitePos("TrailerPark", new Vector3(-1290f, 0f, -700f))
                },
                new[] { "Anvil Ridge", "Marrow Coyote", "Harbor Ranger" }, 6000,
                "Out past the ring road. Sand, rocks and nobody around to hear you crash.");
        }

        private void AddRace(CityLayout L, string id, string name, Vector3 near, Vector3[] way, string[] rivals, int prize, string intro)
        {
            RaceInfo r = new RaceInfo { Id = id, Name = name, Rivals = rivals, Prize = prize, Intro = intro };
            r.Way = new Vector3[way.Length];
            for (int i = 0; i < way.Length; i++) r.Way[i] = MissionManager.RoadNear(way[i]);
            float yaw;
            r.Start = RaceStartPoint(L, near, r.Way[0], out yaw);
            r.Yaw = yaw;
            // The marker sits a little behind the start line so finishing a loop race doesn't retrigger it.
            Vector3 back = r.Start - MathUtil.DirFromYaw(yaw) * 32f;
            float along;
            int lane = L.NearestLane(back, 12f, out along, true);
            r.TriggerPos = PedManager.Ground(lane >= 0 ? L.Lanes[lane].PointAt(along) : back);
            RaceInfo captured = r;
            r.Trigger = Interactable.Create("Race " + name, r.TriggerPos, 5f, name, () => StartRace(captured), transform);
            r.Trigger.OnFoot = false;
            r.Trigger.InVehicle = true;
            r.Trigger.AutoTrigger = true;
            r.Trigger.HeightTolerance = 3.5f;
            r.Trigger.Condition = () => Time.time > captured.LockUntil && CanStart(false) && Game.Player.Seat == 0
                                        && Game.Player.CurrentVehicle != null && Game.Player.CurrentVehicle.Speed < 9f;
            r.Hint = Interactable.Create("Race hint " + name, r.TriggerPos, 5f, "Street race: " + name + ". Drive a car into the marker to start.", () => { }, transform);
            r.Hint.OnFoot = true;
            r.Marker = WorldMarker.Create(r.TriggerPos, RaceColor, 3.2f, transform, true);
            r.Blip = Blip.Add(BlipKind.Activity, null, r.TriggerPos, RaceColor, "F", "Street race: " + name, false);
            _races.Add(r);
        }

        /// <summary>Picks a straight, multi-lane spot near a point, heading toward the first checkpoint.</summary>
        private Vector3 RaceStartPoint(CityLayout L, Vector3 near, Vector3 toward, out float yaw)
        {
            L.LanesNear(near, 140f, _tmp);
            int best = -1;
            float bestScore = float.MinValue;
            float bestAlong = 0f;
            for (int k = 0; k < _tmp.Count; k++)
            {
                LanePath lane = L.Lanes[_tmp[k]];
                if (lane.IsConnector || lane.Length < 70f) continue;
                float along = Mathf.Clamp(lane.Project(near), 32f, lane.Length - 32f);
                Vector3 p = lane.PointAt(along);
                float d = MathUtil.FlatDistance(p, near);
                if (d > 140f) continue;
                Vector3 dir = lane.DirectionAt(along);
                Vector3 to = MathUtil.Flat(toward - p);
                float align = to.sqrMagnitude > 1f ? Vector3.Dot(dir, to.normalized) : 0f;
                float score = -d + align * 70f + (lane.LanesInDir >= 2 ? 40f : 0f) + (lane.LeftNeighbor >= 0 || lane.RightNeighbor >= 0 ? 20f : 0f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = _tmp[k];
                    bestAlong = along;
                }
            }
            if (best < 0)
            {
                Vector3 rp = MissionManager.RoadNear(near);
                yaw = MissionManager.RoadYawNear(rp);
                return rp;
            }
            LanePath bl = L.Lanes[best];
            yaw = MathUtil.YawOf(bl.DirectionAt(bestAlong));
            return bl.PointAt(bestAlong);
        }

        private void StartRace(RaceInfo r)
        {
            if (!CanStart(true)) return;
            bool first = !Completed.Contains(r.Id);
            MissionDef def = new MissionDef
            {
                Id = r.Id,
                Title = r.Name,
                Chapter = "Street Race",
                Contact = "Nico Ash",
                ContactLetter = "F",
                Summary = "Nico Ash's " + r.Name + ". First across the line takes the pot.",
                Reward = first ? r.Prize : r.Prize / 3,
                Story = false,
                StartPosition = () => r.TriggerPos
            };
            def.Script = c => RaceScript(c, r, first);
            def.OnPass = () =>
            {
                if (Completed.Add(r.Id) && Game.Stats != null) Game.Stats.Add(StatId.RacesWon, 1);
            };
            r.LockUntil = float.MaxValue;
            _activeActivity = def;
            Game.Missions.StartMission(def);
        }

        private IEnumerator RaceScript(MissionContext c, RaceInfo r, bool first)
        {
            yield return c.Fade(true, 0.35f);
            if (Game.Traffic != null) Game.Traffic.ClearAround(r.Start, 70f);
            PlayerController p = c.Player;
            if (!p.InVehicle)
            {
                VehicleDef d = VehicleDb.Find(r.Rivals[r.Rivals.Length - 1]) ?? VehicleDb.All[0];
                Vehicle loaner = c.Car(d, r.Start, r.Yaw, null);
                p.EnterVehicleImmediate(loaner, 0);
                if (Game.Hud != null) Game.Hud.Notify("Nico lends you a " + d.Name + ".", 3f);
            }
            else p.Teleport(PedManager.Ground(r.Start), r.Yaw);
            if (Game.Cam != null) Game.Cam.SnapBehindPlayer();
            yield return c.Fade(false, 0.5f);
            yield return c.Say("Nico Ash", r.Intro);
            List<Vector3> cps = StoryMissions.RaceCheckpoints(r.Start, r.Way, 160f);
            VehicleDef[] rivals = new VehicleDef[r.Rivals.Length];
            for (int i = 0; i < rivals.Length; i++) rivals[i] = VehicleDb.Find(r.Rivals[i]) ?? VehicleDb.All[0];
            yield return StoryMissions.RunRace(c, cps, rivals, r.Name.ToUpperInvariant());
            yield return c.Say("Nico Ash", first
                ? "First time out and you took the " + r.Name + ". People are going to start saying your name like a warning."
                : "Still the fastest thing in Las Veyra. Here's your cut.");
        }

        // ================================================================== rampages

        private void BuildRampages(CityLayout L)
        {
            AddRampage(L, "rampage_ember", "Ember Court Rampage", new Vector3(-520f, 0f, -430f), 0, WeaponId.MicroSpray, 25, 120f, 6000);
            AddRampage(L, "rampage_brass", "Brass Tower Rampage", new Vector3(700f, 0f, -260f), 3, WeaponId.GlassShot, 20, 120f, 7000);
            AddRampage(L, "rampage_static", "Static Saints Rampage", new Vector3(-560f, 0f, 600f), 4, WeaponId.ArcCarbine, 30, 150f, 8000);
        }

        private void AddRampage(CityLayout L, string id, string name, Vector3 near, int gang, WeaponId weapon, int kills, float seconds, int prize)
        {
            int node = L.NearestWalkNode(near, 160f);
            Vector3 pos = PedManager.Ground(node >= 0 ? L.WalkNodes[node].Pos : near);
            RampageInfo r = new RampageInfo { Id = id, Name = name, Pos = pos, Gang = gang, Weapon = weapon, Kills = kills, Seconds = seconds, Prize = prize };
            GameObject token = new GameObject("Rampage " + name);
            token.transform.position = pos;
            token.transform.SetParent(transform, true);
            GameObject model = new GameObject("Model");
            model.transform.SetParent(token.transform, false);
            model.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            Material glow = Mats.Emissive(RampageColor, 2.2f);
            Material dark = Mats.Solid(new Color(0.05f, 0.03f, 0.03f), 0.3f);
            Shapes.Ball("Skull", model.transform, Vector3.zero, new Vector3(0.5f, 0.55f, 0.5f), glow, false);
            Shapes.Box("Jaw", model.transform, new Vector3(0f, -0.28f, 0.04f), new Vector3(0.3f, 0.16f, 0.3f), glow, false);
            Shapes.Box("Eye L", model.transform, new Vector3(-0.11f, 0.03f, 0.23f), new Vector3(0.12f, 0.12f, 0.06f), dark, false);
            Shapes.Box("Eye R", model.transform, new Vector3(0.11f, 0.03f, 0.23f), new Vector3(0.12f, 0.12f, 0.06f), dark, false);
            Spinner spin = token.AddComponent<Spinner>();
            spin.Target = model.transform;
            spin.Speed = 90f;
            WorldMarker.Create(pos, RampageColor, 0.9f, token.transform, false);
            RampageInfo captured = r;
            Interactable it = Interactable.Create("Rampage trigger", pos, 1.5f, name, () => StartRampage(captured), token.transform);
            it.AutoTrigger = true;
            it.Condition = () => Time.time > captured.LockUntil && CanStart(true);
            r.Token = token;
            r.Blip = Blip.Add(BlipKind.Activity, null, pos, RampageColor, "!", name, false);
            _rampages.Add(r);
        }

        private void StartRampage(RampageInfo r)
        {
            if (!CanStart(true) || Completed.Contains(r.Id)) return;
            GangDef g = Gangs.Get(r.Gang);
            MissionDef def = new MissionDef
            {
                Id = r.Id,
                Title = r.Name,
                Chapter = "Rampage",
                Contact = "",
                Summary = "Kill " + r.Kills + " " + (g != null ? g.Name : "gang") + " members in " + Mathf.RoundToInt(r.Seconds) + " seconds.",
                Reward = r.Prize,
                Story = false,
                StartPosition = () => r.Pos + new Vector3(2f, 0f, 0f)
            };
            def.Script = c => RampageScript(c, r);
            def.OnPass = () =>
            {
                if (Completed.Add(r.Id) && Game.Stats != null) Game.Stats.Add(StatId.Rampages, 1);
            };
            r.LockUntil = float.MaxValue;
            _activeActivity = def;
            Game.Missions.StartMission(def);
        }

        private IEnumerator RampageScript(MissionContext c, RampageInfo r)
        {
            PlayerController p = c.Player;
            GangDef g = Gangs.Get(r.Gang) ?? Gangs.All[0];
            WeaponDef wd = Weapons.Get(r.Weapon);
            p.Weapons.Inventory.Give(r.Weapon, Mathf.Max(60, wd.ClipSize * 8));
            p.Weapons.Inventory.Select(r.Weapon);
            p.Weapons.RefreshModel();
            if (Game.Hud != null) Game.Hud.BigMessage("RAMPAGE", "Kill " + r.Kills + " " + g.Name + " in " + Mathf.RoundToInt(r.Seconds) + " seconds", RampageColor, 3.5f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Heartbeat, 0.8f);
            c.Timer(r.Seconds, "RAMPAGE", "Out of time.");
            c.Objective("Kill " + g.Name + " members with the " + wd.Name + ".");
            int killed = 0;
            int spawned = 0;
            float spawnTimer = 1f;
            List<NpcController> live = new List<NpcController>();
            System.Random rand = new System.Random(r.Id.Length * 977 + (int)(Time.time * 10f));
            while (killed < r.Kills)
            {
                for (int i = live.Count - 1; i >= 0; i--)
                {
                    NpcController n = live[i];
                    if (n == null) { live.RemoveAt(i); continue; }
                    if (n.IsDead) { live.RemoveAt(i); killed++; continue; }
                    // Stragglers that got left far behind are recycled closer to the action.
                    if (MathUtil.FlatDistance(n.transform.position, Game.PlayerPosition) > 90f)
                    {
                        live.RemoveAt(i);
                        c.Npcs.Remove(n);
                        Destroy(n.gameObject);
                        spawned--;
                    }
                }
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f && live.Count < 7 && spawned < r.Kills + 14)
                {
                    spawnTimer = 0.7f;
                    Vector3 pos;
                    if (FindWalkPoint(Game.PlayerPosition, 20f, 45f, Vector3.zero, out pos))
                    {
                        WeaponId w = g.Arsenal[rand.Next(g.Arsenal.Length)];
                        NpcController n = c.Enemy(pos, w, Gangs.Look(g, rand), 70f);
                        n.GangId = g.Id;
                        n.DisplayName = g.Name;
                        n.CashDrop = 25;
                        n.Accuracy = 0.55f;
                        n.Attack(p);
                        live.Add(n);
                        spawned++;
                    }
                }
                if (Game.Hud != null) Game.Hud.SetCounter("RAMPAGE KILLS", Mathf.Min(killed, r.Kills), r.Kills);
                yield return null;
            }
            c.ClearTimer();
            if (Game.Hud != null) Game.Hud.ClearCounter();
            // Survivors drop the fight and scatter instead of vanishing at cleanup.
            foreach (NpcController n in live)
            {
                if (n == null || n.IsDead) continue;
                c.Npcs.Remove(n);
                n.Role = NpcRole.Gang;
                n.Hostile = false;
                n.Target = null;
                n.Persistent = false;
                n.MissionTag = null;
                n.Flee(Game.PlayerPosition, 20f);
            }
            yield return c.Wait(1f);
        }

        // ================================================================== payphone contracts (Iris Moon)

        private void BuildContracts()
        {
            AddContract("contract_0", "Loose Lips", 3000,
                "The city sells secrets by the minute. Tobias Wren sold mine. He's eating at a diner downtown with one bodyguard. Close his mouth for good.",
                ContractLooseLips);
            AddContract("contract_1", "Audit Season", 4000,
                "Cameras blink; people remember. Handle both. A crooked auditor is walking his ledger through Glass Heights with three guards. I want the ledger.",
                ContractAuditSeason);
            AddContract("contract_2", "Expensive and Guilty", 3500,
                "That engine sounds expensive and guilty. A gold Bishop Vektor is cruising the Strip with a blackmail drive in the glovebox. Turn it into scrap.",
                ContractExpensive);
            AddContract("contract_3", "Dead Air", 5000,
                "The Static Saints' pirate DJ keeps reading my clients' names on air. Silence him at the Grave Frequency masts, then lose the eye in the sky before you celebrate.",
                ContractDeadAir);
            AddContract("contract_4", "Keys and Silence", 4500,
                "You bring the keys, I bring the silence. The Brass Tower Union parked a van full of evidence in the projects. Get it to the crusher at Battery Yard.",
                ContractKeys);
            AddContract("contract_5", "Patrol Wake", 8000,
                "Captain Harlan Voss runs the dirtiest precinct in Old Silver. Take him off the board. Every patrol car in the grid will wake up, so have an exit.",
                ContractPatrolWake);
        }

        private void AddContract(string id, string title, int reward, string brief, Func<MissionContext, IEnumerator> script)
        {
            _contracts.Add(new ContractInfo { Id = id, Title = title, Reward = reward, Brief = brief, Script = script });
        }

        private ContractInfo NextContract()
        {
            foreach (ContractInfo k in _contracts) if (!Completed.Contains(k.Id)) return k;
            return null;
        }

        public bool PayphoneRinging(Poi poi)
        {
            return poi != null && _ringing == poi;
        }

        public void AnswerPayphone(Poi poi)
        {
            if (poi == null || _ringing != poi)
            {
                if (Game.Hud != null) Game.Hud.Notify("Dial tone. Nobody's calling this one.", 2f);
                return;
            }
            if (!CanStart(true)) return;
            ContractInfo k = NextContract();
            StopRinging();
            if (k == null) return;
            Vector3 phonePos = poi.Position;
            MissionDef def = new MissionDef
            {
                Id = k.Id,
                Title = k.Title,
                Chapter = "Iris Moon Contract",
                Contact = "Iris Moon",
                ContactLetter = "I",
                Summary = k.Brief,
                Reward = k.Reward,
                Story = false,
                StartPosition = () => phonePos + poi.Facing * 2f
            };
            ContractInfo captured = k;
            def.Script = c => ContractWrapper(c, captured);
            def.OnPass = () =>
            {
                if (Completed.Add(captured.Id) && Game.Stats != null) Game.Stats.Add(StatId.ContractsDone, 1);
                if (NextContract() == null && Game.Hud != null) Game.Hud.Notify("Iris Moon: That's every name on my list. You've earned your quiet.", 6f);
            };
            Game.Missions.StartMission(def);
        }

        /// <summary>Iris calls a payphone near the player (phone contact). Returns false when there's no work.</summary>
        public bool RequestContract()
        {
            if (!IsUnlocked(ContractUnlock) || NextContract() == null || !NoMission) return false;
            if (_ringing != null) return true;
            Poi best = null;
            float bd = float.MaxValue;
            Vector3 p = Game.PlayerPosition;
            foreach (Poi phone in _payphones)
            {
                float d = MathUtil.FlatDistance(phone.Position, p);
                if (d < 15f || d >= bd) continue;
                bd = d;
                best = phone;
            }
            if (best == null) return false;
            Ring(best, 240f, false);
            return true;
        }

        private void UpdatePayphones()
        {
            bool can = IsUnlocked(ContractUnlock) && NoMission && NextContract() != null;
            if (_ringing != null)
            {
                if (!can) { StopRinging(); return; }
                if (Time.time > _ringUntil)
                {
                    StopRinging();
                    _nextRing = Time.time + 180f;
                }
                return;
            }
            if (!can || Time.time < _nextRing || (Game.Wanted != null && Game.Wanted.Stars > 0)) return;
            _nextRing = Time.time + 45f;
            Vector3 p = Game.PlayerPosition;
            List<Poi> cands = new List<Poi>();
            foreach (Poi phone in _payphones)
            {
                float d = MathUtil.FlatDistance(phone.Position, p);
                if (d > 60f && d < 380f) cands.Add(phone);
            }
            if (cands.Count == 0) return;
            Ring(cands[_rand.Next(cands.Count)], 150f, true);
        }

        private void Ring(Poi phone, float seconds, bool announce)
        {
            StopRinging();
            _ringing = phone;
            _ringUntil = Time.time + seconds;
            Vector3 pos = PedManager.Ground(phone.Position);
            _ringBlip = Blip.Add(BlipKind.Activity, null, pos, PhoneColor, "P", "Ringing payphone (Iris Moon contract)", true);
            _ringBlip.Flash = true;
            _ringBlip.Size = 1.3f;
            _ringObject = new GameObject("Ringing Payphone");
            _ringObject.transform.position = pos + Vector3.up * 1.4f;
            _ringObject.transform.SetParent(transform, true);
            if (Game.Audio != null && Game.Audio.Ready) Game.Audio.CreateLoop(_ringObject.transform, SfxId.PhoneRing, 0.7f, 45f);
            WorldMarker.Create(pos, PhoneColor, 0.7f, _ringObject.transform, true);
            if (announce && Game.Hud != null) Game.Hud.Notify("A payphone is ringing nearby. Iris Moon has work (green P on the map).", 5f);
        }

        private void StopRinging()
        {
            _ringing = null;
            if (_ringBlip != null) Blip.Remove(_ringBlip);
            _ringBlip = null;
            if (_ringObject != null) Destroy(_ringObject);
            _ringObject = null;
        }

        private IEnumerator ContractWrapper(MissionContext c, ContractInfo k)
        {
            yield return c.Say("Iris Moon", k.Brief);
            yield return k.Script(c);
        }

        private static Vector3 PoiNear(Vector3 p, PoiType type, out Vector3 facing)
        {
            facing = Vector3.forward;
            if (Game.Layout == null) return p;
            Poi x = Game.Layout.NearestPoi(p, type);
            if (x == null) return p;
            facing = x.Facing.sqrMagnitude > 0.01f ? x.Facing : Vector3.forward;
            return x.Position;
        }

        private static Appearance Look(int seed)
        {
            return Appearance.Random(new System.Random(seed));
        }

        private static Vector3 Ring8(Vector3 c, float radius, int i, int n)
        {
            float a = (i / (float)Mathf.Max(1, n)) * Mathf.PI * 2f;
            return c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
        }

        /// <summary>Fails the mission when the target stays far away for too long. Returns true once failed.</summary>
        private static bool EscapeCheck(MissionContext c, Vector3 targetPos, float maxDist, ref float timer, float seconds, string reason)
        {
            if (MathUtil.FlatDistance(Game.PlayerPosition, targetPos) > maxDist) timer += Time.deltaTime;
            else timer = 0f;
            if (Game.Hud != null && timer > 0.5f) Game.Hud.SetProgress("The target is getting away!", 1f - timer / seconds);
            if (timer > seconds)
            {
                c.M.Fail(reason);
                return true;
            }
            return false;
        }

        private sealed class Escape
        {
            public float Timer;
        }

        private IEnumerator AfterHit(MissionContext c, string line)
        {
            c.ClearTarget();
            if (Game.Wanted != null && Game.Wanted.Stars > 0) yield return c.LoseWanted("Lose the police.");
            yield return c.Say("Iris Moon", line);
        }

        // ---------------------------------------------------------------- 1. Loose Lips

        private IEnumerator ContractLooseLips(MissionContext c)
        {
            Vector3 facing;
            Vector3 diner = PoiNear(new Vector3(-60f, 0f, -120f), PoiType.Diner, out facing);
            Vector3 stand = PedManager.Ground(diner + facing * 2f);
            NpcController target = c.Civilian(stand, MathUtil.YawOf(-facing), Look(901));
            target.DisplayName = "Tobias Wren";
            target.Health.MaxHealth = target.Health.Health = 90f;
            NpcController guard = c.Enemy(stand + MathUtil.RightOf(facing) * 1.6f, WeaponId.VectorSidearm, Look(902), 130f);
            guard.Target = null;
            guard.Hostile = false;
            guard.GuardAt(guard.transform.position);
            Vector3 carPos = MissionManager.RoadNear(stand + facing * 7f);
            Vehicle car = c.Car(VehicleDb.Find("Horizon Vektor") ?? VehicleDb.All[0], carPos, MissionManager.RoadYawNear(carPos), new Color(0.08f, 0.08f, 0.1f));
            Blip tb = c.BlipOn(target.transform, new Color(1f, 0.2f, 0.2f), "Tobias Wren");
            c.Objective("Kill Tobias Wren, the informant eating at the diner.");
            bool alerted = false;
            bool inCar = false;
            float runTime = 0f;
            Escape esc = new Escape();
            while (target != null && !target.IsDead)
            {
                Vector3 tp = target.transform.position;
                if (!alerted)
                {
                    c.Target(tp);
                    bool hurt = target.Health.Health < target.Health.MaxHealth || guard == null || guard.IsDead || guard.Health.Health < guard.Health.MaxHealth;
                    if (MathUtil.FlatDistance(Game.PlayerPosition, tp) < 20f || hurt)
                    {
                        alerted = true;
                        c.ClearTarget();
                        if (guard != null && !guard.IsDead) guard.Attack(Game.Player);
                        if (car != null && !car.IsDestroyed && car.Driver == null)
                        {
                            target.MoveTo(car.DoorPoint(0), true, null);
                            c.Objective("He's running for his car! Don't let him get away.");
                        }
                        else
                        {
                            target.IgnoreThreats = false;
                            target.Flee(Game.PlayerPosition, 40f);
                        }
                    }
                }
                else if (!inCar && car != null && !car.IsDestroyed && !target.IsRagdolled)
                {
                    runTime += Time.deltaTime;
                    if (MathUtil.FlatDistance(tp, car.DoorPoint(0)) < 2.6f && car.Driver == null)
                    {
                        inCar = true;
                        c.PutInDriverSeat(target, car);
                        TrafficDriver ai = car.GetComponent<TrafficDriver>();
                        if (ai != null)
                        {
                            ai.Mode = DriveMode.Flee;
                            ai.IgnoreLights = true;
                            ai.SpeedScale = 0.95f;
                        }
                        c.Objective("Tobias is fleeing in his car. Stop him!");
                    }
                    else if (runTime > 9f)
                    {
                        target.IgnoreThreats = false;
                        target.Flee(Game.PlayerPosition, 40f);
                        runTime = -999f;
                    }
                }
                if (alerted && EscapeCheck(c, tp, 330f, ref esc.Timer, 7f, "Tobias Wren got away.")) yield break;
                yield return null;
            }
            c.RemoveBlip(tb);
            yield return AfterHit(c, "Quiet as a closed book. The money's in your account.");
        }

        // ---------------------------------------------------------------- 2. Audit Season

        private IEnumerator ContractAuditSeason(MissionContext c)
        {
            Vector3 center = MissionManager.WalkPointIn(District.GlassHeights, 17);
            if (center == Vector3.zero) center = new Vector3(0f, 0f, 420f);
            NpcController auditor = c.Civilian(center, 0f, Look(911));
            auditor.DisplayName = "Auditor Pell";
            List<NpcController> crew = new List<NpcController> { auditor };
            WeaponId[] guns = { WeaponId.CoilBurst, WeaponId.VectorSidearm, WeaponId.VectorSidearm };
            for (int i = 0; i < 3; i++)
            {
                NpcController gd = c.Enemy(Ring8(center, 3.5f, i, 3), guns[i], Look(912 + i), 120f);
                gd.Target = null;
                gd.Hostile = false;
                gd.GuardAt(gd.transform.position);
                crew.Add(gd);
            }
            c.Target(center);
            yield return c.GoTo(center, 45f, "Find the crooked auditor in Glass Heights.", false);
            foreach (NpcController n in crew) if (n != null && !n.IsDead && n != auditor) n.Attack(Game.Player);
            auditor.IgnoreThreats = false;
            auditor.Flee(Game.PlayerPosition, 30f);
            Vector3 lastPos = auditor.transform.position;
            Blip ab = c.BlipOn(auditor.transform, new Color(1f, 0.2f, 0.2f), "Auditor Pell");
            c.Objective("Kill Auditor Pell. His guards are armed.");
            Escape esc = new Escape();
            while (auditor != null && !auditor.IsDead)
            {
                lastPos = auditor.transform.position;
                if (EscapeCheck(c, lastPos, 260f, ref esc.Timer, 7f, "The auditor escaped with the ledger.")) yield break;
                yield return null;
            }
            c.RemoveBlip(ab);
            if (auditor != null) lastPos = auditor.Ragdoll != null && auditor.IsRagdolled ? auditor.Ragdoll.HipsPosition : auditor.transform.position;
            yield return c.PressE(lastPos, "Press E to take the ledger", "Grab the ledger from the auditor's body.");
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.7f);
            if (Game.Wanted != null && Game.Wanted.Stars > 0) yield return c.LoseWanted("Lose the police before the drop.");
            Vector3 dropFacing;
            Vector3 drop = PoiNear(new Vector3(0f, 0f, 500f), PoiType.Payphone, out dropFacing);
            yield return c.GoTo(drop + dropFacing * 1.5f, 3f, "Leave the ledger at Iris Moon's dead drop (the payphone).", false);
            yield return c.Say("Iris Moon", "Every page of it. Pell's partners will be sweating for a month. Good.");
        }

        // ---------------------------------------------------------------- 3. Expensive and Guilty

        private IEnumerator ContractExpensive(MissionContext c)
        {
            Vector3 spawn = MissionManager.RoadNear(new Vector3(CityLayout.StripX, 0f, 220f));
            Vehicle car = c.Car(VehicleDb.Find("Bishop Vektor") ?? VehicleDb.All[0], spawn, MissionManager.RoadYawNear(spawn), new Color(0.95f, 0.75f, 0.12f));
            car.Parked = false;
            NpcController driver = c.Civilian(spawn, 0f, Look(921));
            driver.DisplayName = "Rich kid";
            c.PutInDriverSeat(driver, car);
            Blip vb = c.BlipOn(car.transform, new Color(1f, 0.2f, 0.2f), "Gold Bishop Vektor", BlipKind.VehicleTarget);
            c.Objective("Destroy the gold Bishop Vektor cruising the Mirage Strip.");
            c.Timer(240f, "CONTRACT", "The Vektor reached its private garage.");
            Escape esc = new Escape();
            float targetTimer = 0f;
            while (car != null && !car.IsDestroyed)
            {
                targetTimer -= Time.deltaTime;
                if (targetTimer <= 0f)
                {
                    targetTimer = 1f;
                    c.Target(car.transform.position);
                }
                if (EscapeCheck(c, car.transform.position, 450f, ref esc.Timer, 8f, "The Vektor got away.")) yield break;
                yield return null;
            }
            c.ClearTimer();
            c.RemoveBlip(vb);
            yield return AfterHit(c, "That drive is ash now. A lot of important people just slept better and don't know why.");
        }

        // ---------------------------------------------------------------- 4. Dead Air

        private IEnumerator ContractDeadAir(MissionContext c)
        {
            CityLayout L = c.L;
            Vector3 mast = new Vector3(-560f, 0f, 620f);
            float best = float.MaxValue;
            foreach (Vector3 m in L.RadioMastSpots)
            {
                if (CityLayout.DistrictAt(m) != District.GraveFrequency) continue;
                float d = MathUtil.FlatDistance(m, mast);
                if (d < best) { best = d; mast = m; }
            }
            int node = L.NearestWalkNode(mast, 120f);
            Vector3 pos = node >= 0 ? L.WalkNodes[node].Pos : mast;
            GangDef saints = Gangs.Get(4) ?? Gangs.All[0];
            System.Random rand = new System.Random(931);
            NpcController dj = c.Enemy(pos, WeaponId.BrassHammer, Gangs.Look(saints, rand), 160f);
            dj.DisplayName = "DJ Hollow";
            dj.Target = null;
            dj.Hostile = false;
            dj.GuardAt(pos);
            List<NpcController> guards = new List<NpcController>();
            for (int i = 0; i < 3; i++)
            {
                NpcController g = c.Enemy(Ring8(pos, 4f, i, 3), WeaponId.CoilBurst, Gangs.Look(saints, rand), 110f);
                g.Target = null;
                g.Hostile = false;
                g.GuardAt(g.transform.position);
                guards.Add(g);
            }
            yield return c.GoTo(pos, 40f, "Go to the Grave Frequency radio masts.", false);
            dj.Attack(Game.Player);
            foreach (NpcController g in guards) if (g != null && !g.IsDead) g.Attack(Game.Player);
            yield return c.KillAll(new List<NpcController> { dj }, "Kill DJ Hollow before he gets back on the air.", null);
            if (Game.Wanted != null)
            {
                Game.Wanted.SetStars(Mathf.Max(Game.Wanted.Stars, 3));
                if (Game.Hud != null) Game.Hud.Notify("The Saints called it in. There's a helicopter on its way.", 4f);
            }
            yield return c.LoseWanted("Lose the eye in the sky before you celebrate.");
            yield return c.Say("Iris Moon", "Dead air at last. Listen to that. Lovely.");
        }

        // ---------------------------------------------------------------- 5. Keys and Silence

        private IEnumerator ContractKeys(MissionContext c)
        {
            Vector3 lot = MissionManager.WalkPointIn(District.BrassProjects, 23);
            if (lot == Vector3.zero) lot = new Vector3(700f, 0f, -300f);
            Vector3 vanPos = MissionManager.RoadNear(lot);
            Vehicle van = c.Car(VehicleDb.Find("Mistral Solace") ?? VehicleDb.All[0], vanPos, MissionManager.RoadYawNear(vanPos), new Color(0.2f, 0.22f, 0.25f));
            van.Locked = false;
            bool crushed = false;
            c.FailIf(() => !crushed && (van == null || van.IsDestroyed), "The evidence van was destroyed.");
            GangDef union = Gangs.Get(3) ?? Gangs.All[0];
            System.Random rand = new System.Random(941);
            for (int i = 0; i < 2; i++)
            {
                NpcController g = c.Enemy(vanPos + van.transform.right * (i == 0 ? 3.5f : -3.5f), WeaponId.GlassShot, Gangs.Look(union, rand), 120f);
                g.Target = null;
                g.Hostile = false;
                g.GuardAt(g.transform.position);
                StartCoroutine(GuardWatch(g, van));
            }
            yield return c.EnterVehicle(van, "Steal the Brass Tower Union's evidence van.");
            Vector3 crusher = MissionManager.PoiPos("Battery Yard Scrap Co.", MissionManager.SitePos("Quarry", new Vector3(-1340f, 0f, 700f)));
            Vector3 dropPoint = MissionManager.RoadNear(crusher);
            if (Game.Wanted != null && Game.Wanted.Stars > 0) yield return c.LoseWanted("Lose the police. Nobody follows the van to the crusher.");
            yield return c.DriveTo(dropPoint, 7f, "Take the van to the crusher at Battery Yard Scrap Co.", van);
            c.Objective("Get out and let the crusher do its job.");
            while (c.Player.InVehicle && c.Player.CurrentVehicle == van) yield return null;
            yield return c.Wait(1.2f);
            if (van != null)
            {
                Vector3 vp = van.transform.position;
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrashHeavy, vp, 1f, 0.6f);
                if (Game.Fx != null) Game.Fx.Dust(vp + Vector3.up, Vector3.up * 2f, 4f);
                crushed = true;
                c.Vehicles.Remove(van);
                foreach (CharacterBase occ in van.Occupants) if (occ != null && !occ.IsPlayer) Destroy(occ.gameObject);
                Destroy(van.gameObject);
            }
            yield return c.Say("Iris Moon", "Crushed into a cube the size of a suitcase. Evidence is such a fragile thing.");
        }

        /// <summary>Union guards open fire once the player messes with their van.</summary>
        private IEnumerator GuardWatch(NpcController guard, Vehicle van)
        {
            while (guard != null && !guard.IsDead && van != null)
            {
                bool provoked = guard.Health.Health < guard.Health.MaxHealth || (Game.Player != null && (van.HasPlayer || MathUtil.FlatDistance(Game.PlayerPosition, van.transform.position) < 6f));
                if (provoked)
                {
                    guard.Attack(Game.Player);
                    yield break;
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ---------------------------------------------------------------- 6. Patrol Wake

        private IEnumerator ContractPatrolWake(MissionContext c)
        {
            Vector3 spawn = MissionManager.RoadNear(new Vector3(-520f, 0f, 20f));
            Vehicle cruiser = c.Car(VehicleDb.FirstOfClass(VehicleClass.Police), spawn, MissionManager.RoadYawNear(spawn), null);
            cruiser.Parked = false;
            NpcController captain = NpcController.Spawn(PedManager.Ground(spawn) + Vector3.up, 0f, NpcRole.Police, Appearance.Police(), -1);
            captain.Persistent = true;
            captain.MissionTag = c.Def.Id;
            captain.DisplayName = "Captain Voss";
            captain.Arm(WeaponId.BrassHammer);
            captain.Health.MaxHealth = captain.Health.Health = 180f;
            captain.transform.SetParent(c.M.transform, true);
            c.Npcs.Add(captain);
            c.PutInDriverSeat(captain, cruiser);
            NpcController partner = NpcController.Spawn(PedManager.Ground(spawn) + Vector3.up, 0f, NpcRole.Police, Appearance.Police(), -1);
            partner.Persistent = true;
            partner.MissionTag = c.Def.Id;
            partner.Arm(WeaponId.VectorSidearm);
            partner.transform.SetParent(c.M.transform, true);
            c.Npcs.Add(partner);
            partner.EnterVehicleImmediate(cruiser, 1);
            Blip cb = c.BlipOn(captain.transform, new Color(1f, 0.2f, 0.2f), "Captain Voss");
            c.Objective("Take out Captain Voss. He patrols Old Silver in his cruiser.");
            Escape esc = new Escape();
            float targetTimer = 0f;
            while (captain != null && !captain.IsDead)
            {
                Vector3 tp = captain.transform.position;
                targetTimer -= Time.deltaTime;
                if (targetTimer <= 0f)
                {
                    targetTimer = 1f;
                    c.Target(tp);
                }
                if (EscapeCheck(c, tp, 520f, ref esc.Timer, 10f, "Captain Voss slipped away.")) yield break;
                yield return null;
            }
            c.RemoveBlip(cb);
            c.ClearTarget();
            if (Game.Wanted != null)
            {
                Game.Wanted.SetStars(Mathf.Max(Game.Wanted.Stars, 4));
                if (Game.Hud != null) Game.Hud.Notify("Every patrol car in the grid just woke up.", 4f);
            }
            yield return c.LoseWanted("Lose the police. All of them.");
            yield return c.Say("Iris Moon", "The precinct's already fighting over his chair. That's the thing about kings, Malik. There's always a line.");
        }

        // ================================================================== vehicle jobs (J)

        private static string JobKindFor(Vehicle v)
        {
            if (v == null || v.Def == null) return null;
            switch (v.Def.Class)
            {
                case VehicleClass.Taxi: return "Taxi";
                case VehicleClass.Police:
                case VehicleClass.PoliceSUV:
                case VehicleClass.Enforcer: return "Vigilante";
                case VehicleClass.Ambulance: return "Paramedic";
            }
            return null;
        }

        private static string JobLabel(string kind)
        {
            return kind == "Taxi" ? "taxi" : (kind == "Vigilante" ? "vigilante" : "paramedic");
        }

        public bool JobActive { get { return _jobDef != null && Game.Missions != null && Game.Missions.Active != null && Game.Missions.Active.Def == _jobDef; } }

        public void ToggleVehicleJob(Vehicle v)
        {
            MissionManager m = Game.Missions;
            if (m == null || v == null || Game.Player == null) return;
            if (m.Active != null)
            {
                if (JobActive) _jobQuit = true;
                else if (Game.Hud != null) Game.Hud.Notify("Finish \"" + m.Active.Def.Title + "\" first.", 2.5f);
                return;
            }
            string kind = JobKindFor(v);
            if (kind == null)
            {
                if (Game.Hud != null) Game.Hud.Notify("J starts taxi work in a cab, vigilante work in a police vehicle and paramedic work in an ambulance.", 4f);
                return;
            }
            if (Game.Player.Seat != 0)
            {
                if (Game.Hud != null) Game.Hud.Notify("You need to be the one driving.", 2.5f);
                return;
            }
            if (Game.Wanted != null && Game.Wanted.Stars > 0)
            {
                if (Game.Hud != null) Game.Hud.Notify(kind == "Vigilante" ? "Lose your own wanted level before playing cop." : "Lose the police first.", 3f);
                return;
            }
            _jobQuit = false;
            MissionDef def = new MissionDef
            {
                Id = "job_" + JobLabel(kind),
                Title = kind + " Missions",
                Chapter = kind,
                Contact = "Dispatch",
                Summary = kind == "Taxi" ? "Pick up fares and drive them where they're going. Fast driving earns tips." :
                          (kind == "Vigilante" ? "Hunt down armed suspects. Each level sends tougher crews." : "Pick up injured people and rush them to a hospital."),
                Reward = 0,
                Story = false,
                Quiet = true
            };
            Vehicle veh = v;
            JobState st = new JobState();
            switch (kind)
            {
                case "Taxi": def.Script = c => TaxiJob(c, veh, st); break;
                case "Vigilante": def.Script = c => VigilanteJob(c, veh, st); break;
                default: def.Script = c => ParamedicJob(c, veh, st); break;
            }
            _jobDef = def;
            m.StartMission(def);
            if (kind != "Taxi") veh.SirenOn = true;
        }

        /// <summary>Shared per-frame job checks: quit key, wrecked vehicle, leaving the vehicle. False ends the job.</summary>
        private bool JobTick(MissionContext c, Vehicle v, JobState st, string vehicleName)
        {
            if (_jobQuit)
            {
                _jobQuit = false;
                c.M.Pass();
                return false;
            }
            if (v == null || v.IsDestroyed)
            {
                c.M.Fail("Your " + vehicleName + " was wrecked.");
                return false;
            }
            bool inside = c.Player.InVehicle && c.Player.CurrentVehicle == v;
            if (!inside)
            {
                st.Outside += Time.deltaTime;
                if (Game.Hud != null) Game.Hud.SetProgress("Get back in your " + vehicleName, 1f - st.Outside / 12f);
                if (st.Outside > 12f)
                {
                    c.M.Fail("You left your " + vehicleName + ".");
                    return false;
                }
            }
            else st.Outside = 0f;
            return true;
        }

        private void Summarize(MissionContext c, JobState st, string what)
        {
            c.PassMessage = st.Count + " " + what + ", " + MathUtil.Money(st.Earned) + " earned";
        }

        private bool FindWalkPoint(Vector3 from, float minD, float maxD, Vector3 preferDir, out Vector3 pos)
        {
            pos = Vector3.zero;
            CityLayout L = Game.Layout;
            if (L == null) return false;
            L.WalkNodesNear(from, maxD, _tmp);
            int best = -1;
            float bestScore = float.MinValue;
            for (int k = 0; k < _tmp.Count; k++)
            {
                WalkNode n = L.WalkNodes[_tmp[k]];
                float d = MathUtil.FlatDistance(n.Pos, from);
                if (d < minD || d > maxD) continue;
                if (Mathf.Abs(n.Pos.y - from.y) > 8f) continue;
                float score = (float)_rand.NextDouble() * 60f;
                if (preferDir != Vector3.zero) score += Vector3.Dot(MathUtil.Flat(n.Pos - from).normalized, preferDir) * 50f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = _tmp[k];
                }
            }
            if (best < 0) return false;
            pos = PedManager.Ground(L.WalkNodes[best].Pos);
            return true;
        }

        private bool FindLanePoint(Vector3 from, float minD, float maxD, out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero;
            yaw = 0f;
            CityLayout L = Game.Layout;
            if (L == null) return false;
            L.LanesNear(from, maxD, _tmp);
            for (int attempt = 0; attempt < 24 && _tmp.Count > 0; attempt++)
            {
                LanePath lane = L.Lanes[_tmp[_rand.Next(_tmp.Count)]];
                if (lane.IsConnector || lane.Length < 30f) continue;
                float s = (float)_rand.NextDouble() * (lane.Length - 16f) + 8f;
                Vector3 p = lane.PointAt(s);
                float d = MathUtil.FlatDistance(p, from);
                if (d < minD || d > maxD) continue;
                if (!TrafficManager.Clear(p, 6f)) continue;
                pos = p;
                yaw = MathUtil.YawOf(lane.DirectionAt(s));
                return true;
            }
            return false;
        }

        private static Poi PickDestination(Vector3 from, float minD, float maxD, System.Random r)
        {
            CityLayout L = Game.Layout;
            List<Poi> cands = new List<Poi>();
            foreach (Poi p in L.Pois)
            {
                switch (p.Type)
                {
                    case PoiType.Diner:
                    case PoiType.Bar:
                    case PoiType.Casino:
                    case PoiType.Hospital:
                    case PoiType.Motel:
                    case PoiType.GasStation:
                    case PoiType.Clothing:
                    case PoiType.ConvenienceStore:
                    case PoiType.Property:
                    case PoiType.Police:
                        float d = MathUtil.FlatDistance(p.Position, from);
                        if (d >= minD && d <= maxD) cands.Add(p);
                        break;
                }
            }
            if (cands.Count == 0) return null;
            return cands[r.Next(cands.Count)];
        }

        private static readonly string[] FareLines =
        {
            "Take me to {0}. And step on it, I'm late.",
            "{0}, please. Don't talk to me about the weather.",
            "I need to get to {0} before my ex does.",
            "{0}. If you get me there fast there's a tip in it.",
            "Hey, you're that guy from the news! Anyway. {0}, please.",
            "{0}. Avoid the Strip, the traffic's murder tonight."
        };

        // ---------------------------------------------------------------- taxi

        private IEnumerator TaxiJob(MissionContext c, Vehicle cab, JobState st)
        {
            System.Random rand = new System.Random(cab.GetInstanceID());
            Summarize(c, st, "fares");
            while (true)
            {
                Vector3 pick = Vector3.zero;
                bool found = false;
                float retry = 0f;
                while (!found)
                {
                    if (!JobTick(c, cab, st, "taxi")) yield break;
                    retry -= Time.deltaTime;
                    if (retry <= 0f)
                    {
                        retry = 1f;
                        found = FindWalkPoint(Game.PlayerPosition, 60f, 220f, cab.transform.forward, out pick);
                        if (!found) c.Objective("No fares out here. Head into the city.");
                    }
                    if (!found) yield return null;
                }
                NpcController fare = c.Civilian(pick, 0f, Appearance.Random(rand));
                fare.DisplayName = "Fare";
                fare.Anim.Phone = rand.NextDouble() < 0.3;
                Blip fb = c.BlipOn(fare.transform, new Color(0.3f, 0.7f, 1f), "Taxi fare", BlipKind.Objective);
                c.Objective("Pick up the fare (blue). Stop right next to them.");
                bool boarded = false;
                float targetTimer = 0f;
                while (!boarded)
                {
                    if (!JobTick(c, cab, st, "taxi")) yield break;
                    if (fare == null || fare.IsDead || fare.IsRagdolled)
                    {
                        if (Game.Hud != null) Game.Hud.Notify("That fare's not going anywhere now. Finding another...", 3f);
                        break;
                    }
                    targetTimer -= Time.deltaTime;
                    if (targetTimer <= 0f) { targetTimer = 1f; c.Target(fare.transform.position); }
                    float d = MathUtil.FlatDistance(fare.transform.position, cab.transform.position);
                    if (d < 8f + cab.Def.Width * 0.5f && cab.Speed < 2.5f && c.Player.CurrentVehicle == cab)
                    {
                        int seat = cab.Def.Seats > 2 ? (cab.Occupants[2] == null ? 2 : cab.FreeSeat(false)) : cab.FreeSeat(false);
                        if (seat > 0)
                        {
                            fare.Anim.Phone = false;
                            fare.EnterVehicleImmediate(cab, seat);
                            boarded = true;
                            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.DoorClose, cab.transform.position, 0.6f);
                        }
                    }
                    yield return null;
                }
                c.RemoveBlip(fb);
                c.ClearTarget();
                if (!boarded) continue;

                Poi dest = PickDestination(cab.transform.position, 260f, 1100f, rand);
                Vector3 dpos = dest != null ? MissionManager.RoadNear(dest.Position) : MissionManager.RoadNear(cab.transform.position + cab.transform.forward * 400f);
                string destName = dest != null ? dest.Name : CityLayout.DistrictName(CityLayout.DistrictAt(dpos));
                float dist = MathUtil.FlatDistance(cab.transform.position, dpos);
                float allowed = Mathf.Clamp(dist * 1.35f / 13f + 18f, 35f, 170f);
                c.Timer(allowed, "FARE", "The fare got out and walked.");
                yield return c.Say("Fare", string.Format(FareLines[rand.Next(FareLines.Length)], destName));
                Blip db = c.BlipAt(dpos, UIKit.Gold, destName);
                WorldMarker mk = c.Marker(dpos, UIKit.Gold, 4f);
                float hp0 = cab.Health;
                c.Objective("Drive the fare to " + destName + ".");
                bool arrived = false;
                while (!arrived)
                {
                    if (!JobTick(c, cab, st, "taxi")) yield break;
                    if (fare == null || fare.IsDead || fare.CurrentVehicle != cab)
                    {
                        if (Game.Hud != null) Game.Hud.Notify("You lost your fare.", 3f);
                        break;
                    }
                    c.Target(dpos);
                    if (MathUtil.FlatDistance(cab.transform.position, dpos) < 9f && cab.Speed < 3f) arrived = true;
                    yield return null;
                }
                float left = c.TimeLeft;
                c.ClearTimer();
                c.ClearTarget();
                c.RemoveBlip(db);
                if (mk != null) Destroy(mk.gameObject);
                if (!arrived)
                {
                    if (fare != null)
                    {
                        if (fare.InVehicle) fare.ExitVehicleImmediate(false);
                        c.Npcs.Remove(fare);
                        fare.Persistent = false;
                        fare.MissionTag = null;
                    }
                    continue;
                }
                int pay = Mathf.RoundToInt(20f + dist * 0.09f);
                int tip = Mathf.RoundToInt(Mathf.Max(0f, left) * 1.4f);
                bool rough = cab.Health < hp0 - 140f;
                if (rough) tip = 0;
                fare.ExitVehicleImmediate(false);
                c.Npcs.Remove(fare);
                fare.Persistent = false;
                fare.IgnoreThreats = false;
                fare.MissionTag = null;
                fare.SetState(NpcState.Wander);
                st.Count++;
                st.Earned += pay + tip;
                if (Game.Economy != null) Game.Economy.Earn(pay + tip, "Taxi fare");
                if (Game.Stats != null) Game.Stats.Add(StatId.TaxiFares, 1);
                if (Game.Hud != null)
                    Game.Hud.Notify(rough ? "Fare: " + MathUtil.Money(pay) + ". \"You drive like a lunatic. No tip.\"" : "Fare: " + MathUtil.Money(pay) + "  Tip: " + MathUtil.Money(tip), 3.5f);
                if (st.Count % 5 == 0)
                {
                    int bonus = st.Count * 100;
                    st.Earned += bonus;
                    if (Game.Economy != null) Game.Economy.Earn(bonus, "Taxi streak");
                    if (Game.Hud != null) Game.Hud.Banner("FARE STREAK", st.Count + " fares in a row. Bonus " + MathUtil.Money(bonus), UIKit.Gold, 3.5f);
                }
                if (st.Count >= 20 && Completed.Add("job_taxi_20"))
                {
                    if (Game.Economy != null) Game.Economy.Earn(5000, "Taxi milestone");
                    if (Game.Hud != null) Game.Hud.Banner("TAXI LEGEND", "20 fares in one shift. Bonus $5,000.", UIKit.Gold, 5f);
                }
                Summarize(c, st, "fares");
                yield return c.Wait(1.5f);
            }
        }

        // ---------------------------------------------------------------- vigilante

        private static readonly string[] CrookCars = { "Torque Falcon", "Kestrel Marauder", "Harbor Solace", "Orion Comet", "Anvil Ridge", "Horizon Vektor", "Bishop Coyote" };

        private IEnumerator VigilanteJob(MissionContext c, Vehicle cop, JobState st)
        {
            System.Random rand = new System.Random(cop.GetInstanceID() + 7);
            Summarize(c, st, "suspects");
            while (true)
            {
                Vector3 spawn;
                float yaw;
                if (!FindLanePoint(Game.PlayerPosition, 150f, 300f, out spawn, out yaw))
                {
                    if (!JobTick(c, cop, st, "cruiser")) yield break;
                    yield return null;
                    continue;
                }
                st.Level++;
                int level = st.Level;
                VehicleDef def = VehicleDb.Find(CrookCars[rand.Next(CrookCars.Length)]) ?? VehicleDb.All[0];
                Vehicle car = c.Car(def, spawn, yaw, null);
                car.Parked = false;
                int count = Mathf.Clamp(1 + (level - 1) / 2, 1, Mathf.Max(1, def.Seats));
                GangDef g = Gangs.All[rand.Next(Gangs.All.Length)];
                List<NpcController> crooks = new List<NpcController>();
                for (int i = 0; i < count; i++)
                {
                    WeaponId w = level >= 6 && i == 0 ? WeaponId.ArcCarbine : g.Arsenal[rand.Next(g.Arsenal.Length)];
                    NpcController n = c.Enemy(spawn + Vector3.up, w, Gangs.Look(g, rand), 70f + level * 6f);
                    n.Target = null;
                    n.DisplayName = "Suspect";
                    n.CashDrop = 40 + level * 10;
                    if (i == 0) c.PutInDriverSeat(n, car);
                    else n.EnterVehicleImmediate(car, i);
                    crooks.Add(n);
                }
                TrafficDriver ai = car.GetComponent<TrafficDriver>();
                if (ai != null)
                {
                    ai.Mode = DriveMode.Flee;
                    ai.IgnoreLights = true;
                    ai.SpeedScale = Mathf.Clamp(0.8f + level * 0.03f, 0.8f, 1.05f);
                }
                List<Blip> blips = new List<Blip>();
                foreach (NpcController n in crooks) blips.Add(c.BlipOn(n.transform, new Color(1f, 0.2f, 0.2f), "Suspect", BlipKind.Objective));
                c.Timer(100f + level * 8f, "VIGILANTE", "The suspects got away.");
                if (Game.Hud != null) Game.Hud.Banner("VIGILANTE LEVEL " + level, count + (count == 1 ? " suspect" : " suspects") + " in a " + def.Name, new Color(0.4f, 0.6f, 1f), 3f);
                c.Objective("Take out the suspects (red).");
                Escape esc = new Escape();
                float stopped = 0f;
                float targetTimer = 0f;
                bool bailed = false;
                while (true)
                {
                    if (!JobTick(c, cop, st, "police vehicle")) yield break;
                    int alive = 0;
                    Vector3 nearest = Vector3.zero;
                    float nd = float.MaxValue;
                    foreach (NpcController n in crooks)
                    {
                        if (n == null || n.IsDead) continue;
                        alive++;
                        float d = MathUtil.FlatDistance(n.transform.position, Game.PlayerPosition);
                        if (d < nd) { nd = d; nearest = n.transform.position; }
                    }
                    if (Game.Hud != null) Game.Hud.SetCounter("SUSPECTS", count - alive, count);
                    if (alive == 0) break;
                    targetTimer -= Time.deltaTime;
                    if (targetTimer <= 0f) { targetTimer = 1f; c.Target(nearest); }
                    if (EscapeCheck(c, nearest, 420f, ref esc.Timer, 7f, "The suspects got away.")) yield break;
                    // A stalled getaway car means everyone piles out shooting.
                    if (!bailed && car != null)
                    {
                        bool nearPlayer = MathUtil.FlatDistance(car.transform.position, Game.PlayerPosition) < 45f;
                        stopped = car.Speed < 1.2f && nearPlayer ? stopped + Time.deltaTime : 0f;
                        if (stopped > 3f || car.IsDestroyed || car.OnFire)
                        {
                            bailed = true;
                            foreach (NpcController n in crooks)
                            {
                                if (n == null || n.IsDead) continue;
                                if (n.InVehicle) n.ExitVehicleImmediate(false);
                                n.Attack(Game.Player);
                            }
                        }
                    }
                    yield return null;
                }
                foreach (Blip b in blips) c.RemoveBlip(b);
                c.ClearTimer();
                c.ClearTarget();
                if (Game.Hud != null) Game.Hud.ClearCounter();
                int reward = level * 350;
                st.Count += count;
                st.Earned += reward;
                if (Game.Economy != null) Game.Economy.Earn(reward, "Vigilante");
                if (Game.Stats != null) Game.Stats.Add(StatId.VigilanteKills, count);
                if (Game.Hud != null) Game.Hud.Banner("SUSPECTS NEUTRALIZED", "Level " + level + " reward " + MathUtil.Money(reward), new Color(0.4f, 0.6f, 1f), 3f);
                if (level >= 10 && Completed.Add("job_vigilante_10"))
                {
                    if (Game.Economy != null) Game.Economy.Earn(5000, "Vigilante milestone");
                    if (Game.Player != null) Game.Player.Health.MaxArmor = Mathf.Max(Game.Player.Health.MaxArmor, 150f);
                    if (Game.Hud != null) Game.Hud.Banner("TEN LEVELS CLEAN", "Bonus $5,000 and max armor raised to 150.", UIKit.Gold, 5f);
                }
                Summarize(c, st, "suspects");
                yield return c.Wait(2.5f);
            }
        }

        // ---------------------------------------------------------------- paramedic

        private IEnumerator ParamedicJob(MissionContext c, Vehicle amb, JobState st)
        {
            System.Random rand = new System.Random(amb.GetInstanceID() + 11);
            Summarize(c, st, "patients saved");
            while (true)
            {
                st.Level++;
                int level = st.Level;
                int want = Mathf.Min(level, Mathf.Max(1, amb.Def.Seats - 1));
                List<NpcController> patients = new List<NpcController>();
                List<Blip> blips = new List<Blip>();
                Vector3 from = amb.transform.position;
                float total = 25f;
                for (int i = 0; i < want; i++)
                {
                    Vector3 pos;
                    if (!FindWalkPoint(from, 60f, 280f, Vector3.zero, out pos)) continue;
                    NpcController n = c.Civilian(pos, (float)rand.NextDouble() * 360f, Appearance.Random(rand));
                    n.DisplayName = "Patient";
                    n.SetState(NpcState.Cower);
                    patients.Add(n);
                    blips.Add(c.BlipOn(n.transform, new Color(0.3f, 0.7f, 1f), "Patient", BlipKind.Objective));
                    total += MathUtil.FlatDistance(from, pos) * 1.3f / 10f + 12f;
                    from = pos;
                }
                if (patients.Count == 0)
                {
                    st.Level--;
                    c.Objective("No emergencies out here. Head into the city.");
                    float wait = 1f;
                    while (wait > 0f)
                    {
                        if (!JobTick(c, amb, st, "ambulance")) yield break;
                        wait -= Time.deltaTime;
                        yield return null;
                    }
                    continue;
                }
                Poi hospital = NearestHospital(from);
                Vector3 hpos = hospital != null ? MissionManager.RoadNear(hospital.Position) : MissionManager.RoadNear(Vector3.zero);
                total += MathUtil.FlatDistance(from, hpos) * 1.3f / 10f;
                c.Timer(Mathf.Clamp(total, 45f, 300f), "PARAMEDIC", "The patients didn't make it in time.");
                if (Game.Hud != null) Game.Hud.Banner("PARAMEDIC LEVEL " + level, patients.Count + (patients.Count == 1 ? " patient" : " patients") + " waiting", new Color(1f, 0.35f, 0.35f), 3f);
                int aboard = 0;
                int lost = 0;
                float targetTimer = 0f;
                while (aboard + lost < patients.Count)
                {
                    if (!JobTick(c, amb, st, "ambulance")) yield break;
                    c.Objective("Pick up the injured (blue). " + aboard + " aboard.");
                    Vector3 nearest = Vector3.zero;
                    float nd = float.MaxValue;
                    for (int i = 0; i < patients.Count; i++)
                    {
                        NpcController n = patients[i];
                        if (n == null || n.IsDead)
                        {
                            if (blips[i] != null) { c.RemoveBlip(blips[i]); blips[i] = null; lost++; }
                            continue;
                        }
                        if (n.InVehicle) continue;
                        float d = MathUtil.FlatDistance(n.transform.position, amb.transform.position);
                        if (d < nd) { nd = d; nearest = n.transform.position; }
                        if (d < 8f + amb.Def.Width * 0.5f && amb.Speed < 2.5f && c.Player.CurrentVehicle == amb)
                        {
                            int seat = amb.FreeSeat(false);
                            if (seat > 0)
                            {
                                n.EnterVehicleImmediate(amb, seat);
                                aboard++;
                                if (blips[i] != null) { c.RemoveBlip(blips[i]); blips[i] = null; }
                                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.DoorClose, amb.transform.position, 0.6f);
                            }
                        }
                    }
                    targetTimer -= Time.deltaTime;
                    if (nd < float.MaxValue && targetTimer <= 0f) { targetTimer = 1f; c.Target(nearest); }
                    yield return null;
                }
                c.ClearTarget();
                if (aboard == 0)
                {
                    c.M.Fail("No patients survived.");
                    yield break;
                }
                Blip hb = c.BlipAt(hpos, new Color(1f, 0.3f, 0.3f), hospital != null ? hospital.Name : "Hospital");
                WorldMarker hm = c.Marker(hpos, new Color(1f, 0.3f, 0.3f), 4f);
                c.Objective("Rush the patients to " + (hospital != null ? hospital.Name : "the hospital") + ".");
                bool arrived = false;
                while (!arrived)
                {
                    if (!JobTick(c, amb, st, "ambulance")) yield break;
                    c.Target(hpos);
                    if (MathUtil.FlatDistance(amb.transform.position, hpos) < 10f && amb.Speed < 3f) arrived = true;
                    yield return null;
                }
                c.RemoveBlip(hb);
                if (hm != null) Destroy(hm.gameObject);
                c.ClearTarget();
                c.ClearTimer();
                int saved = 0;
                foreach (NpcController n in patients)
                {
                    if (n == null || n.IsDead || n.CurrentVehicle != amb) continue;
                    n.ExitVehicleImmediate(false);
                    c.Npcs.Remove(n);
                    n.Persistent = false;
                    n.IgnoreThreats = false;
                    n.MissionTag = null;
                    n.SetState(NpcState.Wander);
                    saved++;
                }
                int reward = level * 200 + saved * 100;
                st.Count += saved;
                st.Earned += reward;
                if (Game.Economy != null) Game.Economy.Earn(reward, "Paramedic");
                if (Game.Stats != null) Game.Stats.Add(StatId.PatientsSaved, saved);
                if (Game.Player != null) Game.Player.Health.Heal(20f);
                if (Game.Hud != null) Game.Hud.Banner("PATIENTS DELIVERED", saved + " saved. Level " + level + " reward " + MathUtil.Money(reward), new Color(1f, 0.35f, 0.35f), 3f);
                if (level >= 10 && Completed.Add(Progression.ParamedicMilestone))
                {
                    if (Game.Economy != null) Game.Economy.Earn(5000, "Paramedic milestone");
                    if (Game.Player != null)
                    {
                        // Progression builds max health from every source and now counts the milestone, so the bonus
                        // survives level-ups, gear changes, skills, brews and loads.
                        if (Game.Progress != null) Game.Progress.ApplyToPlayer(false);
                        else Game.Player.Health.MaxHealth += Progression.ParamedicHealthBonus;
                        Game.Player.Health.Heal(Game.Player.Health.MaxHealth);
                    }
                    if (Game.Hud != null) Game.Hud.Banner("SAINT LUMEN'S FINEST", "Bonus $5,000 and +" + Mathf.RoundToInt(Progression.ParamedicHealthBonus) + " max health for good.", UIKit.Gold, 5f);
                }
                Summarize(c, st, "patients saved");
                yield return c.Wait(2f);
            }
        }

        private static Poi NearestHospital(Vector3 p)
        {
            CityLayout L = Game.Layout;
            Poi best = null;
            float bd = float.MaxValue;
            foreach (Poi x in L.Pois)
            {
                if (x.Type != PoiType.Hospital && x.Type != PoiType.Clinic) continue;
                float d = MathUtil.FlatDistance(x.Position, p);
                if (d < bd) { bd = d; best = x; }
            }
            return best;
        }

        // ================================================================== service vehicles

        private void BuildServiceSpots(CityLayout L)
        {
            VehicleDef amb = VehicleDb.FirstOfClass(VehicleClass.Ambulance);
            VehicleDef cop = VehicleDb.FirstOfClass(VehicleClass.Police);
            VehicleDef cab = VehicleDb.FirstOfClass(VehicleClass.Taxi);
            foreach (Poi p in L.Pois)
            {
                VehicleDef def = null;
                if (p.Type == PoiType.Hospital || p.Type == PoiType.Clinic) def = amb;
                else if (p.Type == PoiType.Police) def = cop;
                else if (p.Type == PoiType.Casino || p.Type == PoiType.Motel) def = cab;
                if (def == null) continue;
                int best = -1;
                float bd = 70f;
                for (int i = 0; i < L.ParkingSpots.Count; i++)
                {
                    float d = MathUtil.FlatDistance(L.ParkingSpots[i], p.Position);
                    if (d < bd) { bd = d; best = i; }
                }
                if (best < 0) continue;
                _spots.Add(new ServiceSpot { Def = def, Pos = L.ParkingSpots[best], Yaw = L.ParkingYaws[best] });
            }
        }

        private void UpdateServiceSpots()
        {
            if (Game.Traffic == null) return;
            Vector3 p = Game.PlayerPosition;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            foreach (ServiceSpot s in _spots)
            {
                if (s.Current != null)
                {
                    if (!s.Current.IsDestroyed && MathUtil.FlatDistance(s.Current.transform.position, s.Pos) < 15f) continue;
                    s.Current = null;
                }
                float d = MathUtil.FlatDistance(p, s.Pos);
                if (d > 160f || d < 35f) continue;
                if (cam != null && d < 110f && PedManager.IsVisible(cam, s.Pos)) continue;
                if (!TrafficManager.Clear(s.Pos, 4.5f)) continue;
                try
                {
                    Vehicle v = Game.Traffic.SpawnParkedAt(s.Def, s.Pos, s.Yaw, null);
                    v.Locked = false;
                    s.Current = v;
                }
                catch (Exception e)
                {
                    Log.Exception("Service vehicle", e);
                }
            }
        }

        // ================================================================== summary (pause menu brief, phone jobs app)

        public string Summary()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int racesWon = 0;
            foreach (RaceInfo r in _races) if (Completed.Contains(r.Id)) racesWon++;
            sb.Append("<b>STREET RACES</b>  ").Append(racesWon).Append("/").Append(_races.Count).Append(" won");
            if (!IsUnlocked(RaceUnlock)) sb.Append("  (unlocks after Ghost Lanes)");
            else sb.Append("  (white F on the map, drive into the marker)");
            sb.Append('\n');
            int ramp = 0;
            foreach (RampageInfo r in _rampages) if (Completed.Contains(r.Id)) ramp++;
            sb.Append("<b>RAMPAGES</b>  ").Append(ramp).Append("/").Append(_rampages.Count);
            if (!IsUnlocked(RampageUnlock)) sb.Append("  (unlocks after Ashline Treaty)");
            else sb.Append("  (red ! on the map)");
            sb.Append('\n');
            int contracts = 0;
            foreach (ContractInfo k in _contracts) if (Completed.Contains(k.Id)) contracts++;
            sb.Append("<b>IRIS MOON CONTRACTS</b>  ").Append(contracts).Append("/").Append(_contracts.Count);
            if (!IsUnlocked(ContractUnlock)) sb.Append("  (unlocks after False Kings)");
            else if (contracts < _contracts.Count) sb.Append(_ringing != null ? "  (a payphone is ringing: green P)" : "  (answer ringing payphones)");
            sb.Append('\n');
            Stats s = Game.Stats;
            if (s != null)
            {
                sb.Append("<b>VEHICLE JOBS</b> (press J)  Taxi fares ").Append(Mathf.RoundToInt(s.Get(StatId.TaxiFares)))
                  .Append("   Suspects stopped ").Append(Mathf.RoundToInt(s.Get(StatId.VigilanteKills)))
                  .Append("   Patients saved ").Append(Mathf.RoundToInt(s.Get(StatId.PatientsSaved))).Append('\n');
                sb.Append("<b>STORES ROBBED</b>  ").Append(Mathf.RoundToInt(s.Get(StatId.StoresRobbed)))
                  .Append("   <b>STUNT JUMPS</b>  ").Append(Mathf.RoundToInt(s.Get(StatId.StuntJumps)))
                  .Append("   <b>PROPERTIES</b>  ").Append(Game.Economy != null ? Game.Economy.OwnedProperties.Count : 0).Append("/").Append(Economy.Properties.Length);
            }
            if (JobActive) sb.Append("\n\nOn the clock: ").Append(_jobDef.Title).Append(". Press J again to clock out.");
            return sb.ToString();
        }
    }
}
