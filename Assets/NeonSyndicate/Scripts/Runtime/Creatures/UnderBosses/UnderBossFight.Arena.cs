using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The readable arena: a ring of short pylons with glowing caps on the boundary (cyan while the boss waits, amber
    /// during the fight, green after victory) joined by a low translucent band, and, where the arena has a known gate
    /// anchor close to it, a seal across the entrance that closes once the player is inside and opens on victory. The
    /// seal never traps anyone: holding against it for a few seconds withdraws (the fight resets), and every ending
    /// removes it. After a victory a green marker stands at the exit (the gate, or where the player came in).
    /// </summary>
    public abstract partial class UnderBossFight
    {
        public const float PostSpacing = 6f;
        public const float BandHeight = 1.15f;
        public const float SealWidth = 7f;
        public const float SealHeight = 5f;

        private static readonly Color DormantCap = new Color(0.3f, 0.9f, 1f);
        private static readonly Color FightCap = new Color(1f, 0.55f, 0.15f);
        private static readonly Color VictoryCap = new Color(0.35f, 1f, 0.5f);

        private Transform _arenaRoot;
        private MeshRenderer _postR, _bandR;
        private GameObject _seal;
        private BoxCollider _sealCol;
        private MeshRenderer _sealR;
        private bool _sealClosed;
        private Vector3 _sealNormal;
        private Vector3 _entry;
        private WorldMarker _exitMarker;
        private float _bandFade = 1f;
        private int _posts;

        /// <summary>The live gate point (Vector3.zero when the arena has no usable gate anchor).</summary>
        protected Vector3 GatePoint { get; private set; }
        public bool SealClosed { get { return _sealClosed; } }
        public int PostCount { get { return _posts; } }

        protected Transform ArenaRoot
        {
            get
            {
                if (_arenaRoot == null)
                {
                    GameObject go = new GameObject("Arena");
                    go.transform.SetParent(transform, false);
                    _arenaRoot = go.transform;
                }
                return _arenaRoot;
            }
        }

        private void BuildArena()
        {
            ResolveGate();
            BuildBoundary();
            if (GatePoint != Vector3.zero) BuildSeal();
        }

        /// <summary>The profile's gate anchor when the Underheart knows it and it sits by this arena (within 1.6 radii).</summary>
        private void ResolveGate()
        {
            GatePoint = Vector3.zero;
            if (Profile == null || string.IsNullOrEmpty(Profile.GateAnchor) || Game.Underheart == null) return;
            Vector3 pos;
            float yaw;
            bool ok;
            try { ok = Game.Underheart.TryAnchor(Profile.GateAnchor, out pos, out yaw); }
            catch (System.Exception e)
            {
                Log.Exception("Underheart boss gate anchor " + Profile.GateAnchor, e);
                return;
            }
            if (!ok || !MathUtil.IsFinite(pos)) return;
            if (MathUtil.FlatDistance(pos, ArenaCenter) > ArenaRadius * 1.6f || Mathf.Abs(pos.y - ArenaCenter.y) > 6f) return;
            GatePoint = pos;
        }

        /// <summary>The boundary posts (one mesh, one renderer) and the band between them (one mesh, both faces).</summary>
        private void BuildBoundary()
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(2f * Mathf.PI * ArenaRadius / PostSpacing), 10, 28);
            MeshBuilder posts = new MeshBuilder(2);
            posts.FixedUV = new Vector2(0.5f, 0.5f);
            MeshBuilder band = new MeshBuilder(1);
            Vector3[] base0 = new Vector3[n];
            bool[] ok = new bool[n];
            Vector3 origin = ArenaCenter;
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n;
                Vector3 p = ArenaCenter + MathUtil.DirFromYaw(a) * ArenaRadius;
                float y;
                if (!UnderBossFx.Floor(new Vector3(p.x, ArenaCenter.y, p.z), 2.5f, 5f, out y) || Mathf.Abs(y - ArenaCenter.y) > 3.5f) continue;
                p.y = y;
                if (Physics.CheckSphere(p + Vector3.up * 1.1f, 0.22f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                ok[i] = true;
                base0[i] = p;
                Vector3 local = p - origin;
                Quaternion r = Quaternion.Euler(0f, a, 0f);
                posts.Box(0, local + Vector3.up * 0.9f, new Vector3(0.26f, 1.8f, 0.26f), r, UVMode.Fixed);
                posts.Box(0, local + Vector3.up * 0.06f, new Vector3(0.6f, 0.12f, 0.6f), r, UVMode.Fixed);
                posts.Box(1, local + Vector3.up * 1.92f, new Vector3(0.34f, 0.24f, 0.34f), r, UVMode.Fixed);
                _posts++;
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                if (!ok[i] || !ok[j]) continue;
                Vector3 a = base0[i] - origin, b = base0[j] - origin;
                Vector3 au = a + Vector3.up * BandHeight, bu = b + Vector3.up * BandHeight;
                Vector3 nrm = MathUtil.Flat(-(a + b)).normalized;
                band.Quad(0, a, au, bu, b, nrm, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
                band.Quad(0, b, bu, au, a, -nrm, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
            }
            Material[] mm = UnderBossFx.MachineMaterials;
            if (_posts > 0)
            {
                Mesh pm = OwnedMeshes.Track(gameObject, posts.ToMesh("Underheart Arena Posts"));
                GameObject pg = Shapes.MeshObject("Boundary posts", pm, new[] { mm[UnderBossFx.SubIron], mm[UnderBossFx.SubHeat] }, ArenaRoot, false);
                pg.transform.SetPositionAndRotation(origin, Quaternion.identity);
                _postR = pg.GetComponent<MeshRenderer>();
                Mesh bm = OwnedMeshes.Track(gameObject, band.ToMesh("Underheart Arena Band"));
                GameObject bg = Shapes.MeshObject("Boundary band", bm, new[] { UnderBossFx.BandMat }, ArenaRoot, false);
                bg.transform.SetPositionAndRotation(origin, Quaternion.identity);
                _bandR = bg.GetComponent<MeshRenderer>();
                _bandR.receiveShadows = false;
                _bandR.shadowCastingMode = ShadowCastingMode.Off;
            }
            SetBoundaryColor(DormantCap, 0.35f);
        }

        private void SetBoundaryColor(Color cap, float bandAlpha)
        {
            if (_postR != null) UnderBossFx.Emission(_postR, cap * 2.4f);
            if (_bandR != null) UnderBossFx.Tint(_bandR, new Color(cap.r, cap.g, cap.b, 1f) * bandAlpha);
        }

        /// <summary>The seal across the gate: a translucent curtain facing the arena with a Debris-layer collider (open at first).</summary>
        private void BuildSeal()
        {
            Vector3 n = MathUtil.Flat(ArenaCenter - GatePoint);
            if (n.sqrMagnitude < 0.5f) return;
            n.Normalize();
            _sealNormal = n;
            _seal = new GameObject("Arena seal");
            _seal.transform.SetParent(ArenaRoot, false);
            _seal.transform.SetPositionAndRotation(GatePoint, Quaternion.LookRotation(n, Vector3.up));
            _seal.layer = Layers.Debris;
            _sealCol = _seal.AddComponent<BoxCollider>();
            _sealCol.center = new Vector3(0f, SealHeight * 0.5f, 0f);
            _sealCol.size = new Vector3(SealWidth, SealHeight, 0.4f);
            _sealCol.enabled = false;
            MeshBuilder b = new MeshBuilder(1);
            float hw = SealWidth * 0.5f;
            Vector3 a0 = new Vector3(-hw, 0f, 0f), a1 = new Vector3(-hw, SealHeight, 0f), b1 = new Vector3(hw, SealHeight, 0f), b0 = new Vector3(hw, 0f, 0f);
            b.Quad(0, a0, a1, b1, b0, Vector3.back, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
            b.Quad(0, b0, b1, a1, a0, Vector3.forward, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
            Mesh m = OwnedMeshes.Track(gameObject, b.ToMesh("Underheart Arena Seal"));
            GameObject vis = Shapes.MeshObject("Seal glow", m, new[] { UnderBossFx.BandMat }, _seal.transform, false);
            _sealR = vis.GetComponent<MeshRenderer>();
            _sealR.receiveShadows = false;
            _sealR.shadowCastingMode = ShadowCastingMode.Off;
            // Two pylons frame the seal so the entrance reads as a door even while open.
            Material[] mm = UnderBossFx.MachineMaterials;
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject py = Shapes.Box("Seal pylon", _seal.transform, new Vector3(s * (hw + 0.25f), SealHeight * 0.5f, 0f), new Vector3(0.45f, SealHeight, 0.45f), mm[UnderBossFx.SubIron], false);
                py.layer = Layers.Debris;
                Shapes.Box("Seal lamp", py.transform, new Vector3(0f, 0.42f, 0f), new Vector3(1.15f, 0.06f, 1.15f), mm[UnderBossFx.SubHeat], false);
            }
            _sealR.enabled = false;
        }

        /// <summary>Closes the seal when the player is inside, clear of it on the arena side (never on top of anyone).</summary>
        protected void TryCloseSeal()
        {
            if (_seal == null || _sealClosed || State != FightState.Fighting) return;
            Vector3 pp = PlayerFeet;
            Vector3 rel = MathUtil.Flat(pp - GatePoint);
            if (rel.magnitude < 4f || Vector3.Dot(rel, _sealNormal) < 2f) return;
            _sealClosed = true;
            _sealCol.enabled = true;
            _sealR.enabled = true;
            UnderBossFx.Tint(_sealR, new Color(1f, 0.4f, 0.12f, 1f) * 0.55f);
            Underheart04ESounds.Play(UhSfx.GateOpen, GatePoint, 0.8f, 0.7f);
            Callout("THE ARENA SEALS: HOLD AGAINST THE SEAL TO WITHDRAW");
        }

        private void OpenSeal(bool announce)
        {
            if (_seal == null || !_sealClosed) return;
            _sealClosed = false;
            _sealCol.enabled = false;
            _sealR.enabled = false;
            if (announce) Underheart04ESounds.Play(UhSfx.GateOpen, GatePoint, 1f, 1f);
        }

        /// <summary>True while the player leans on the closed seal from the arena side.</summary>
        private bool PressingSeal(Vector3 pp)
        {
            if (!_sealClosed || _seal == null) return false;
            Vector3 rel = MathUtil.Flat(pp - GatePoint);
            float across = Vector3.Dot(rel, _sealNormal);
            float along = Mathf.Abs(Vector3.Dot(rel, MathUtil.RightOf(_sealNormal)));
            return across > 0f && across < ArenaWatch04E.WithdrawRadius && along < SealWidth * 0.5f + 0.5f;
        }

        /// <summary>Where the player crossed the boundary (the exit when the arena has no gate).</summary>
        private void NoteEntry(Vector3 pp)
        {
            Vector3 d = MathUtil.Flat(pp - ArenaCenter);
            if (d.sqrMagnitude < 0.5f) d = MathUtil.DirFromYaw(ArenaYaw + 180f);
            _entry = ArenaCenter + d.normalized * ArenaRadius;
            float y;
            _entry.y = UnderBossFx.Floor(new Vector3(_entry.x, ArenaCenter.y, _entry.z), 2.5f, 5f, out y) ? y : ArenaCenter.y;
        }

        private void UpdateArena(float dt, float now)
        {
            switch (State)
            {
                case FightState.Dormant:
                    SetBoundaryColor(DormantCap * (0.8f + 0.2f * Mathf.Sin(now * 2.2f)), 0.3f);
                    break;
                case FightState.Intro:
                    SetBoundaryColor(Color.Lerp(DormantCap, FightCap, Mathf.PingPong(now * 3f, 1f)), 0.45f);
                    break;
                case FightState.Fighting:
                    TryCloseSeal();
                    float warn = Watch.OutsideFor > 0f ? 0.4f * Mathf.Abs(Mathf.Sin(now * 8f)) : 0f;
                    SetBoundaryColor(FightCap * (0.85f + 0.15f * Mathf.Sin(now * 3f)), 0.45f + warn);
                    if (_sealClosed) UnderBossFx.Tint(_sealR, new Color(1f, 0.4f, 0.12f, 1f) * (0.45f + 0.15f * Mathf.Sin(now * 4f) + Watch.WithdrawFor * 0.25f));
                    break;
                case FightState.Victory:
                    _bandFade = Mathf.MoveTowards(_bandFade, 0f, dt * 0.25f);
                    SetBoundaryColor(VictoryCap, 0.4f * _bandFade);
                    if (_bandR != null && _bandFade <= 0f && _bandR.enabled) _bandR.enabled = false;
                    break;
            }
        }

        /// <summary>Victory: the seal opens, the boundary turns green and a marker stands at the exit.</summary>
        private void OpenArenaOnVictory()
        {
            OpenSeal(true);
            Vector3 exit = GatePoint != Vector3.zero ? GatePoint : _entry;
            if (exit == Vector3.zero) exit = ArenaCenter + MathUtil.DirFromYaw(ArenaYaw + 180f) * ArenaRadius;
            try { _exitMarker = WorldMarker.Create(exit, VictoryCap, 1.4f, ArenaRoot, true); }
            catch (System.Exception e) { Log.Exception("Underheart boss exit marker", e); }
            Callout("EXIT OPEN: THE ARENA FALLS QUIET");
        }

        /// <summary>The player left after the victory (or a few minutes passed): the marker and the boundary go, the husk stays a while.</summary>
        private void TidyAftermath()
        {
            if (_exitMarker != null) Destroy(_exitMarker.gameObject);
            _exitMarker = null;
            if (_postR != null) _postR.gameObject.SetActive(false);
            if (_bandR != null) _bandR.gameObject.SetActive(false);
            if (_seal != null) _seal.SetActive(false);
        }

        /// <summary>The exit point after a victory (the HUD arrow), Vector3.zero before.</summary>
        public Vector3 ExitPoint { get { return State == FightState.Victory && _exitMarker != null ? _exitMarker.transform.position : Vector3.zero; } }

        /// <summary>True while the player stands beyond the boundary during the fight (the HUD counts down).</summary>
        public bool LeavingWarning { get { return State == FightState.Fighting && Watch.OutsideFor > 0f; } }
        public float LeavingSecondsLeft { get { return Watch.WarningLeft; } }
        public float WithdrawProgress { get { return Mathf.Clamp01(Watch.WithdrawFor / ArenaWatch04E.WithdrawSeconds); } }
    }
}
