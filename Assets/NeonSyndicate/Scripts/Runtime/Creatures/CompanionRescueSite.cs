using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A live rescue (BondSystem.BeginRescue, the quest Rescue step): the trapped creature on its rescue body, held by
    /// extraction clamps on bolted plates with glowing tethers to its legs. Releasing a clamp (interact) makes it
    /// flare: a warning ring on the ground, a rearing roar and a bright pulse; while it warns, the player must keep
    /// their distance or the warning does not end. Once free it settles as the player walks in calmly, then a short
    /// tuning sequence (interact as the ring closes, three times) and the naming card. Attacking it never hurts it:
    /// the approach resets. The rules live in RescueRules; this component is the presentation and the input.
    /// </summary>
    public sealed class CompanionRescueSite : MonoBehaviour
    {
        public RescueHandle Handle;
        public RescueRules Rules;
        public CompanionProfile Profile;
        public CompanionController Body { get; private set; }
        public Vector3 Site { get; private set; }
        public float SiteYaw { get; private set; }
        /// <summary>Raised once when the tuning is done and a name is needed (the bond system opens the naming card).</summary>
        public Action<CompanionRescueSite> NamingNeeded;

        private Transform[] _clamps = new Transform[0];
        private Transform[] _tethers = new Transform[0];
        private Transform[] _jawsL = new Transform[0];
        private Transform[] _jawsR = new Transform[0];
        private MeshRenderer[] _cores = new MeshRenderer[0];
        private Interactable[] _prompts = new Interactable[0];
        private Interactable _tunePrompt;
        private CompanionBodyFactory.Job _job;
        private bool _namingRaised;
        private bool _closed;
        private float _struggle;
        private float _warnCue;
        private float _sparks;
        private float[] _jawT = new float[0];
        private GroundRing _ring;
        private bool _flaringBefore;

        /// <summary>Builds the site: ground-snapped clamps (generated around the creature when no points are given) and the body order.</summary>
        public static CompanionRescueSite Begin(Transform parent, string archetype, Vector3 site, float yaw, int clamps, Vector3[] clampPoints, RescueHandle handle, int seed, int detail)
        {
            GameObject go = new GameObject("Rescue Site - " + BondIds.Title(archetype));
            go.layer = Layers.IgnoreRaycast;
            if (parent != null) go.transform.SetParent(parent, false);
            CompanionRescueSite s = go.AddComponent<CompanionRescueSite>();
            s.Setup(archetype, site, yaw, clamps, clampPoints, handle, seed, detail);
            return s;
        }

        private void Setup(string archetype, Vector3 site, float yaw, int clamps, Vector3[] clampPoints, RescueHandle handle, int seed, int detail)
        {
            Profile = CompanionArchetypes.Get(archetype) ?? CompanionArchetypes.QuartzGuide;
            Handle = handle;
            Site = Snap(site);
            SiteYaw = yaw;
            transform.position = Site;
            int n = Mathf.Clamp(clamps, 0, 6);
            Rules = new RescueRules(n);
            _clamps = new Transform[n];
            _tethers = new Transform[n];
            _jawsL = new Transform[n];
            _jawsR = new Transform[n];
            _cores = new MeshRenderer[n];
            _jawT = new float[n];
            _prompts = new Interactable[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 p;
                if (clampPoints != null && i < clampPoints.Length && MathUtil.IsFinite(clampPoints[i])) p = clampPoints[i];
                else
                {
                    float a = yaw + 60f + i * (360f / Mathf.Max(1, n));
                    p = Site + Quaternion.Euler(0f, a, 0f) * Vector3.forward * (1.6f + Profile.BodyRadius);
                }
                p = Snap(p);
                Vector3 toSite = Site - p;
                float cy = toSite.sqrMagnitude > 0.01f ? MathUtil.YawOf(new Vector3(toSite.x, 0f, toSite.z)) : yaw;
                _clamps[i] = CompanionArt.BuildClamp(transform, p, cy);
                _tethers[i] = _clamps[i].Find("Tether");
                _jawsL[i] = _clamps[i].Find("Jaw L");
                _jawsR[i] = _clamps[i].Find("Jaw R");
                Transform coreT = _clamps[i].Find("Core");
                _cores[i] = coreT != null ? coreT.GetComponent<MeshRenderer>() : null;
                int index = i;
                GameObject promptGo = new GameObject("Clamp Prompt");
                promptGo.layer = Layers.IgnoreRaycast;
                promptGo.transform.SetParent(_clamps[i], false);
                promptGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                Interactable it = promptGo.AddComponent<Interactable>();
                it.Radius = 1.9f;
                it.HeightTolerance = 2.2f;
                it.Condition = () => Rules != null && Rules.CanWorkClamp && !Rules.ClampOpen[index];
                it.Prompt = "Press E to release the extraction clamp";
                it.DynamicPrompt = () => PromptFor("release the extraction clamp");
                it.Use = () => ReleaseClamp(index);
                _prompts[i] = it;
            }
            GameObject tune = new GameObject("Tuning Prompt");
            tune.layer = Layers.IgnoreRaycast;
            tune.transform.SetParent(transform, false);
            tune.transform.position = Site + Vector3.up * 0.4f;
            _tunePrompt = tune.AddComponent<Interactable>();
            _tunePrompt.Radius = RescueRules.TuneRadius + 1.2f;
            _tunePrompt.HeightTolerance = 2.4f;
            _tunePrompt.Condition = () => Rules != null && Rules.Phase == RescuePhase.Tuning;
            _tunePrompt.DynamicPrompt = () => PromptFor("tune as the ring closes (" + Rules.Beats + " of " + RescueRules.BeatsNeeded + ")");
            _tunePrompt.Use = PressTune;
            _job = CompanionBodyFactory.Request(Profile.Archetype, seed, 0, "", detail);
            UpdateHandle();
        }

        private static string PromptFor(string what)
        {
            string key = Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.North) : "E";
            return "Press " + key + " to " + what;
        }

        private static Vector3 Snap(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
            return p;
        }

        // ================================================================== actions

        private void ReleaseClamp(int i)
        {
            if (Rules == null || !Rules.OpenClamp(i)) return;
            Transform c = _clamps[i];
            Underheart04ESounds.Play(UhSfx.ClampBreak, c.position + Vector3.up * 0.4f, 1f, 1f);
            if (Game.Fx != null) Game.Fx.Sparks(c.position + Vector3.up * 0.5f, Vector3.up * 2.5f, 16);
            if (_cores[i] != null) _cores[i].sharedMaterial = CompanionArt.CoreOff;
            if (_tethers[i] != null) _tethers[i].gameObject.SetActive(false);
            OnFlare(false);
            UpdateHandle();
        }

        private void PressTune()
        {
            if (Rules == null || Rules.Phase != RescuePhase.Tuning) return;
            bool hit = Rules.PressTune();
            Vector3 at = Body != null ? Body.ChestPosition : Site + Vector3.up * 0.5f;
            if (hit)
            {
                Underheart04ESounds.Play(UhSfx.CareChime, at, 0.8f, 1f + 0.12f * Rules.Beats);
                if (Game.Fx != null) Game.Fx.CrystalBurst(at, Profile.Accent, 8, 0.35f);
                if (Body != null) Body.Glow(1.4f);
            }
            else
            {
                Underheart04ESounds.Play(UhSfx.UiDenied, at, 0.35f, 0.9f);
                if (Game.Hud != null) Game.Hud.CombatText("Not quite: wait for the ring to close", new Color(0.8f, 0.85f, 1f));
            }
            UpdateHandle();
        }

        /// <summary>The trapped creature was hit by the player: the approach resets (it panics), nothing else happens.</summary>
        public void OnAttacked()
        {
            if (Rules == null) return;
            RescuePhase before = Rules.Phase;
            Rules.Attacked();
            if (Rules.Phase == RescuePhase.Naming || Rules.Phase == RescuePhase.Done) return;
            OnFlare(true);
            if (Game.Hud != null)
                Game.Hud.Notify("It panicked. Attacking only resets the approach: back off, let it settle and come in calmly.", 4f);
            if (before == RescuePhase.Tuning && Body != null) Body.ClearAction();
            UpdateHandle();
        }

        private void OnFlare(bool panic)
        {
            Underheart04ESounds.Play(UhSfx.DrakeWarn, Site + Vector3.up * 0.5f, 1f, panic ? 0.85f : 1f);
            if (_ring != null) _ring.Release();
            _ring = GroundRing.Show(Site, RescueRules.KeepDistance, Mathf.Max(1f, Rules.FlareLeft), new Color(1f, 0.55f, 0.85f));
            if (Body != null)
            {
                Body.PlayAction(CreatureAction.Roar, 1.3f, 0.3f, 0.65f, null);
                Body.Glow(1.6f);
                if (Game.Fx != null) Game.Fx.Flash(Body.ChestPosition, Profile.Accent, 2.4f, 6f, 0.3f);
            }
            _warnCue = 1.6f;
        }

        // ================================================================== frame

        private void Update()
        {
            if (_closed) return;
            float dt = Time.deltaTime;
            if (Body == null && _job != null && _job.Finished) MaterializeBody();
            if (dt <= 0f || Rules == null) return;
            PlayerController pl = Game.Player;
            float dist = 999f;
            bool rushing = false;
            if (pl != null && !pl.IsDead)
            {
                dist = MathUtil.FlatDistance(pl.transform.position, Site);
                if (Mathf.Abs(pl.transform.position.y - Site.y) > 4f) dist = 999f;
                Vector3 v = pl.Velocity;
                rushing = new Vector3(v.x, 0f, v.z).magnitude > 4.6f;
            }
            Rules.Tick(dt, dist, rushing);
            bool flaring = Rules.Flaring;
            if (flaring && !_flaringBefore && Rules.Phase == RescuePhase.Approach) OnFlare(false);
            _flaringBefore = flaring;
            if (flaring && Rules.TooClose)
            {
                _warnCue -= dt;
                if (_warnCue <= 0f) OnFlare(false);
            }
            if (!flaring && _ring != null)
            {
                _ring.Burst();
                _ring = null;
            }
            Animate(dt, pl, dist);
            UpdateTethers();
            if (Rules.Phase == RescuePhase.Naming && !_namingRaised)
            {
                _namingRaised = true;
                if (Body != null)
                {
                    Body.PlayAction(CreatureAction.Howl, 1.6f, 0.3f, 0.7f, null);
                    Body.Glow(2f);
                }
                Underheart04ESounds.Play(UhSfx.BondChime, Site + Vector3.up * 0.6f, 1f, 1f);
                Action<CompanionRescueSite> cb = NamingNeeded;
                if (cb != null)
                {
                    try { cb(this); }
                    catch (Exception e) { Log.Exception("Rescue naming", e); }
                }
            }
            for (int i = 0; i < _clamps.Length; i++)
            {
                if (!Rules.ClampOpen[i] || _jawT[i] >= 1f) continue;
                _jawT[i] = Mathf.Min(1f, _jawT[i] + dt * 1.6f);
                OpenJaws(_jawsL[i], _jawsR[i], _jawT[i]);
            }
            UpdateHandle();
        }

        private void MaterializeBody()
        {
            CompanionBodyFactory.Job job = _job;
            _job = null;
            if (job.Failed || job.Body == null)
            {
                Log.Warn("Rescue body could not be built: " + job.Error);
                if (Handle != null)
                {
                    Handle.Failed = true;
                    Handle.Objective = "The creature could not be built (" + job.Error + "). Leave the area and retry the quest.";
                }
                return;
            }
            try
            {
                Body = CompanionController.Create(job.Body, Profile, Site, SiteYaw, transform);
                Body.DisplayName = BondIds.DefaultName(Profile.Archetype);
                Body.BuiltEvolution = 0;
                Body.BuiltSpec = "";
                Body.Mode = CompanionMode.Rescue;
                Body.SetRescueMode(true, OnAttacked);
                Body.SetAlert(1f);
                CompanionBudget.Rescues = 1;
            }
            catch (Exception e)
            {
                Log.Exception("Rescue body", e);
                if (Handle != null)
                {
                    Handle.Failed = true;
                    Handle.Objective = "The creature could not be placed. Leave the area and retry the quest.";
                }
            }
        }

        private void Animate(float dt, PlayerController pl, float dist)
        {
            if (Body == null) return;
            if (pl != null && dist < 25f) Body.SetLook(pl.HeadPosition);
            else Body.ClearLook();
            switch (Rules.Phase)
            {
                case RescuePhase.Clamped:
                    Body.SetAlert(1f);
                    _struggle -= dt;
                    if (_struggle <= 0f && !Body.ActionPlaying)
                    {
                        _struggle = UnityEngine.Random.Range(1.4f, 2.6f);
                        Body.Flinch(0.7f, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Vector3.forward);
                        Underheart04ESounds.Play(UhSfx.RescueClamp, Site + Vector3.up * 0.4f, 0.5f, UnityEngine.Random.Range(0.9f, 1.1f));
                    }
                    _sparks -= dt;
                    if (_sparks <= 0f && Game.Fx != null)
                    {
                        _sparks = UnityEngine.Random.Range(0.8f, 1.6f);
                        for (int i = 0; i < _clamps.Length; i++)
                            if (!Rules.ClampOpen[i]) Game.Fx.Sparks(_clamps[i].position + Vector3.up * 0.6f, Vector3.up, 3);
                    }
                    break;
                case RescuePhase.Approach:
                    Body.SetAlert(Rules.Flaring ? 1f : 1f - Rules.Calm * 0.9f);
                    break;
                case RescuePhase.Tuning:
                    Body.SetAlert(0f);
                    Body.Glow(Mathf.Lerp(0.2f, 1.2f, 1f - Rules.Ring));
                    break;
                default:
                    Body.SetAlert(0f);
                    break;
            }
        }

        private void UpdateTethers()
        {
            CreatureRig rig = Body != null ? Body.Rig : null;
            LegChain[] legs = rig != null ? rig.Body.Skeleton.Legs : null;
            for (int i = 0; i < _clamps.Length; i++)
            {
                Transform c = _clamps[i];
                Transform tether = _tethers[i];
                if (tether == null) continue;
                bool on = rig != null && !Rules.ClampOpen[i];
                if (tether.gameObject.activeSelf != on) tether.gameObject.SetActive(on);
                if (!on) continue;
                Vector3 a = c.position + Vector3.up * 0.55f;
                Vector3 b = Body.ChestPosition;
                if (legs != null && legs.Length > 0)
                {
                    LegChain leg = legs[i % legs.Length];
                    int bone = leg.Foot >= 0 ? leg.Foot : leg.Lower;
                    if (bone >= 0 && bone < rig.Bones.Length) b = rig.Bones[bone].position;
                }
                Vector3 d = b - a;
                float len = d.magnitude;
                if (len < 0.05f) continue;
                tether.position = (a + b) * 0.5f;
                tether.rotation = Quaternion.LookRotation(d / len, Vector3.up);
                tether.localScale = new Vector3(0.035f, 0.035f, len);
            }
        }

        private static void OpenJaws(Transform l, Transform r, float t)
        {
            float a = Mathf.SmoothStep(0f, 1f, t) * 70f;
            if (l != null) l.localRotation = Quaternion.Euler(0f, -a, 0f);
            if (r != null) r.localRotation = Quaternion.Euler(0f, 180f + a, 0f);
        }

        private void UpdateHandle()
        {
            if (Handle == null || Rules == null) return;
            Handle.ClampsLeft = Rules.ClampsLeft;
            Handle.Position = Site;
            Handle.Progress = Rules.Progress;
            Handle.Resets = Rules.Resets;
            if (!Handle.Failed && !Handle.Done) Handle.Objective = Body == null && _job != null ? "Find the trapped " + Profile.Title.ToLowerInvariant() + "." : Rules.Objective(Profile.Title.ToLowerInvariant());
        }

        /// <summary>Hands the body over (it becomes the bonded companion) and lets the clamps fall open; the props clear shortly after.</summary>
        public CompanionController TakeBody()
        {
            CompanionController b = Body;
            Body = null;
            if (b != null)
            {
                b.SetRescueMode(false, null);
                b.transform.SetParent(null, true);
            }
            CompanionBudget.Rescues = 0;
            Rules.Named();
            Close(4f);
            return b;
        }

        /// <summary>Cancels the rescue: removes the body and the props now.</summary>
        public void Abort()
        {
            if (Rules != null) Rules.Abort();
            if (_job != null) _job.Cancelled = true;
            Close(0f);
        }

        private void Close(float delay)
        {
            if (_closed) return;
            _closed = true;
            if (_ring != null) _ring.Release();
            _ring = null;
            for (int i = 0; i < _prompts.Length; i++) if (_prompts[i] != null) _prompts[i].enabled = false;
            if (_tunePrompt != null) _tunePrompt.enabled = false;
            if (Body != null)
            {
                Destroy(Body.gameObject);
                Body = null;
            }
            CompanionBudget.Rescues = 0;
            Destroy(gameObject, Mathf.Max(0f, delay));
        }

        private void OnDestroy()
        {
            if (_ring != null) _ring.Release();
            _ring = null;
            if (Body != null) CompanionBudget.Rescues = 0;
        }
    }
}
