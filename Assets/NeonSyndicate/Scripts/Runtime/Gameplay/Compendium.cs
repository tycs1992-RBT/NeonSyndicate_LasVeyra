using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The bestiary (Game.Compendium): which creatures the player has seen, scanned with the Third Eye and killed.
    /// A creature on screen within 40 m with a clear line of sight is seen (a new entry pops a toast). Holding the Third
    /// Eye on one within 35 m, near the middle of the screen, for 1.2 s scans it: the full entry opens, the first scan of
    /// each species pays Lumen, and its weak point stays marked for 20 s. Kills are counted per species. The BESTIARY
    /// pause page (BestiaryPage) shows it all; saved through IWorldSystem in SaveData.Creatures.
    /// </summary>
    public sealed class Compendium : MonoBehaviour, IWorldSystem
    {
        public event Action<string> Discovered;

        public const float SightRange = 40f;
        public const float ScanRange = 35f;
        public const float ScanSeconds = 1.2f;
        public const float HighlightSeconds = 20f;

        private readonly HashSet<string> _seen = new HashSet<string>();
        private readonly HashSet<string> _scanned = new HashSet<string>();
        private readonly Dictionary<string, int> _kills = new Dictionary<string, int>();
        private readonly HashSet<string> _partsBroken = new HashSet<string>();
        private readonly List<string> _toasts = new List<string>();
        private readonly List<float> _toastTimes = new List<float>();
        private float _sightTimer;
        private CharacterBase _scanTarget;
        private float _scanT;
        private float _losTimer;
        private bool _losOk;

        /// <summary>The creature the Third Eye is scanning right now (null when none) and how far along, 0 to 1.</summary>
        public CharacterBase ScanTarget { get { return _scanTarget; } }
        public float ScanProgress { get { return _scanTarget != null ? Mathf.Clamp01(_scanT / ScanSeconds) : 0f; } }

        public void Init()
        {
            WorldSystems.Register(this);
            BestiaryPage.Register();
            HudOverlays.Register("bestiary", DrawOverlay);
        }

        public bool IsDiscovered(string speciesId) { return !string.IsNullOrEmpty(speciesId) && _seen.Contains(speciesId); }
        public bool IsScanned(string speciesId) { return !string.IsNullOrEmpty(speciesId) && _scanned.Contains(speciesId); }

        public int Kills(string speciesId)
        {
            int n;
            return !string.IsNullOrEmpty(speciesId) && _kills.TryGetValue(speciesId, out n) ? n : 0;
        }

        /// <summary>Species seen so far (for the page header).</summary>
        public int DiscoveredCount { get { return _seen.Count; } }
        public int ScannedCount { get { return _scanned.Count; } }

        public void RaiseDiscovered(string speciesId)
        {
            if (Discovered == null) return;
            foreach (Delegate d in Discovered.GetInvocationList())
            {
                try { ((Action<string>)d)(speciesId); }
                catch (Exception e) { Log.Exception("Bestiary discovered handler", e); }
            }
        }

        /// <summary>A species was seen: a new entry (and a toast) the first time.</summary>
        public void MarkSeen(string speciesId)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(speciesId);
            if (sp == null || !_seen.Add(speciesId)) return;
            Toast("New bestiary entry: " + sp.Name);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.45f, 1.25f);
            RaiseDiscovered(speciesId);
        }

        /// <summary>A full entry: weak point, resistances and drops. award pays the species' scan Lumen the first time.</summary>
        public void MarkScanned(string speciesId, bool award)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(speciesId);
            if (sp == null) return;
            MarkSeen(speciesId);
            if (!_scanned.Add(speciesId)) return;
            if (!award) return;
            int lumen = ScanLumen(sp);
            if (lumen > 0 && Game.Progress != null)
            {
                Vector3 at = _scanTarget != null ? _scanTarget.ChestPosition : Game.PlayerPosition + Vector3.up;
                Game.Progress.GrantLumen(at, lumen);
            }
        }

        /// <summary>Lumen for the first scan of a species (half its kill value, between 15 and 300).</summary>
        public static int ScanLumen(CreatureSpecies sp)
        {
            return sp == null ? 0 : Mathf.Clamp(sp.Lumen / 2, 15, 300);
        }

        /// <summary>True once the player has broken this part of this species (the bestiary then lists it).</summary>
        public bool IsPartBroken(string speciesId, string partId)
        {
            return _partsBroken.Contains(CreaturePartsDb.Key(speciesId, partId));
        }

        /// <summary>A breakable part was broken for the first time: the bestiary learns it (and says so).</summary>
        public void MarkPartBroken(string speciesId, string partId)
        {
            PartDef def = CreaturePartsDb.Find(speciesId, partId);
            if (def == null || !_partsBroken.Add(CreaturePartsDb.Key(speciesId, partId))) return;
            MarkSeen(speciesId);
            Toast("Bestiary: " + def.Name + " can be broken");
        }

        public void RecordKill(string speciesId)
        {
            if (CreatureSpeciesDb.Get(speciesId) == null) return;
            MarkSeen(speciesId);
            int n;
            _kills.TryGetValue(speciesId, out n);
            _kills[speciesId] = n + 1;
        }

        /// <summary>Debug: every species seen and scanned (no Lumen).</summary>
        public void RevealAll()
        {
            foreach (CreatureSpecies sp in CreatureSpeciesDb.All)
            {
                _seen.Add(sp.Id);
                _scanned.Add(sp.Id);
            }
            Toast("Bestiary filled");
        }

        private void Toast(string text)
        {
            _toasts.Add(text);
            _toastTimes.Add(4f);
            if (_toasts.Count > 3)
            {
                _toasts.RemoveAt(0);
                _toastTimes.RemoveAt(0);
            }
        }

        // ================================================================== sightings and scans

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _toastTimes.Count - 1; i >= 0; i--)
            {
                _toastTimes[i] -= Time.unscaledDeltaTime;
                if (_toastTimes[i] <= 0f)
                {
                    _toastTimes.RemoveAt(i);
                    _toasts.RemoveAt(i);
                }
            }
            if (!Game.IsPlaying || Game.Player == null || Game.Player.IsDead || Game.Creatures == null)
            {
                _scanTarget = null;
                return;
            }
            _sightTimer -= dt;
            if (_sightTimer <= 0f)
            {
                _sightTimer = 0.3f;
                CheckSightings();
            }
            UpdateScan(dt);
        }

        private void CheckSightings()
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null) return;
            Vector3 eye = cam.transform.position;
            IList<CharacterBase> live = Game.Creatures.Live;
            for (int i = 0; i < live.Count; i++)
            {
                CharacterBase b = live[i];
                if (b == null || b.IsDead) continue;
                CreatureSpecies sp = CreatureDirector.SpeciesOf(b);
                if (sp == null || _seen.Contains(sp.Id) || !Visible(b)) continue;
                if (MathUtil.FlatDistance(b.transform.position, Game.PlayerPosition) > SightRange) continue;
                Vector3 v = cam.WorldToViewportPoint(b.ChestPosition);
                if (v.z <= 0f || v.x < 0.03f || v.x > 0.97f || v.y < 0.03f || v.y > 0.97f) continue;
                if (Physics.Linecast(eye, b.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                MarkSeen(sp.Id);
            }
        }

        /// <summary>Can be seen at all: built, above ground, not a stealthed shade (the Third Eye shows those).</summary>
        private static bool Visible(CharacterBase b)
        {
            Creature c = b as Creature;
            if (c != null) return c.Ready && !c.Burrowed && c.Visibility > 0.5f;
            NpcController n = b as NpcController;
            return n != null && !n.CreatureMirage;
        }

        private void UpdateScan(float dt)
        {
            Abilities ab = Game.Abilities;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (ab == null || !ab.ThirdEyeActive || cam == null || Game.Player.InVehicle)
            {
                _scanTarget = null;
                _scanT = 0f;
                return;
            }
            CharacterBase best = PickScanTarget(cam);
            if (best != _scanTarget)
            {
                _scanTarget = best;
                _scanT = 0f;
                _losTimer = 0f;
            }
            if (_scanTarget == null) return;
            _losTimer -= dt;
            if (_losTimer <= 0f)
            {
                _losTimer = 0.15f;
                _losOk = !Physics.Linecast(cam.transform.position, _scanTarget.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            }
            if (!_losOk)
            {
                _scanT = Mathf.Max(0f, _scanT - dt * 2f);
                return;
            }
            float before = _scanT;
            _scanT += dt;
            if (before < ScanSeconds && _scanT >= ScanSeconds) CompleteScan(_scanTarget);
        }

        /// <summary>The living creature within scan range nearest to the middle of the screen (shades included: the Eye sees them).</summary>
        private CharacterBase PickScanTarget(Camera cam)
        {
            IList<CharacterBase> live = Game.Creatures.Live;
            CharacterBase best = null;
            float bestScore = 0.09f;
            Vector3 pp = Game.PlayerPosition;
            for (int i = 0; i < live.Count; i++)
            {
                CharacterBase b = live[i];
                if (b == null || b.IsDead) continue;
                Creature c = b as Creature;
                if (c != null && (!c.Ready || c.Burrowed)) continue;
                NpcController n = b as NpcController;
                if (n != null && n.CreatureMirage) continue;
                if (MathUtil.FlatDistance(b.transform.position, pp) > ScanRange) continue;
                Vector3 v = cam.WorldToViewportPoint(b.ChestPosition);
                if (v.z <= 0f) continue;
                float dx = v.x - 0.5f, dy = v.y - 0.5f;
                float score = dx * dx + dy * dy;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = b;
                }
            }
            return best;
        }

        private void CompleteScan(CharacterBase b)
        {
            CreatureSpecies sp = CreatureDirector.SpeciesOf(b);
            if (sp == null) return;
            bool first = !_scanned.Contains(sp.Id);
            MarkScanned(sp.Id, true);
            float until = Time.time + HighlightSeconds;
            Creature c = b as Creature;
            if (c != null) c.HighlightUntil = until;
            NpcController n = b as NpcController;
            if (n != null) n.CreatureHighlightUntil = until;
            Toast(first ? "Scanned: " + sp.Name + ". Weak point revealed (+" + ScanLumen(sp) + " Lumen)" : sp.Name + ": weak point marked");
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.6f, 0.9f);
            if (Game.Fx != null) Game.Fx.Flash(b.ChestPosition, new Color(0.3f, 0.95f, 1f), 3f, 6f, 0.3f);
        }

        // ================================================================== HUD

        private void DrawOverlay(float W, float H)
        {
            DrawHighlights(W, H);
            DrawScan(W, H);
            DrawToasts(W, H);
        }

        private static bool ToGui(Vector3 world, out Vector2 gui)
        {
            gui = Vector2.zero;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0.1f) return false;
            float s = UIKit.Scale;
            gui = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return true;
        }

        /// <summary>Weak points of scanned creatures pulse gold for 20 s.</summary>
        private void DrawHighlights(float W, float H)
        {
            if (Game.Creatures == null) return;
            IList<CharacterBase> live = Game.Creatures.Live;
            float now = Time.time;
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 7f);
            for (int i = 0; i < live.Count; i++)
            {
                CharacterBase b = live[i];
                if (b == null || b.IsDead) continue;
                Creature c = b as Creature;
                if (c != null)
                {
                    if (!c.Ready || now > c.HighlightUntil || c.CRig == null) continue;
                    int[] weak = c.CRig.WeakVolumes;
                    for (int k = 0; k < weak.Length; k++) Marker(c.CRig.VolumeCenter(weak[k]), pulse, k == 0);
                    continue;
                }
                NpcController n = b as NpcController;
                if (n == null || now > n.CreatureHighlightUntil) continue;
                Marker(n.CreatureWeakPoint, pulse, true);
            }
        }

        private static void Marker(Vector3 world, float pulse, bool label)
        {
            Vector2 g;
            if (!ToGui(world, out g)) return;
            float s = 26f * pulse;
            UIKit.Tex(new Rect(g.x - s * 0.5f, g.y - s * 0.5f, s, s), UIKit.Ring, new Color(1f, 0.85f, 0.3f, 0.9f));
            UIKit.Tex(new Rect(g.x - 4f, g.y - 4f, 8f, 8f), UIKit.Circle, new Color(1f, 0.95f, 0.6f, 0.95f));
            if (label) UIKit.Text(new Rect(g.x - 60f, g.y - s * 0.5f - 22f, 120f, 20f), "WEAK POINT", 14, new Color(1f, 0.85f, 0.35f, 0.9f), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void DrawScan(float W, float H)
        {
            if (_scanTarget == null || _scanTarget.IsDead) return;
            Vector2 g;
            if (!ToGui(_scanTarget.ChestPosition, out g)) return;
            float k = ScanProgress;
            float size = 70f + 30f * (1f - k);
            Color c = _losOk ? new Color(0.3f, 0.95f, 1f, 0.85f) : new Color(0.5f, 0.6f, 0.7f, 0.6f);
            UIKit.Tex(new Rect(g.x - size * 0.5f, g.y - size * 0.5f, size, size), UIKit.Ring, c);
            UIKit.Bar(new Rect(g.x - 50f, g.y + size * 0.5f + 6f, 100f, 6f), k, new Color(0.3f, 0.95f, 1f), new Color(0f, 0f, 0f, 0.6f));
            CreatureSpecies sp = CreatureDirector.SpeciesOf(_scanTarget);
            string label = k >= 1f ? (sp != null ? sp.Name : "Scanned") : (_losOk ? "SCANNING" : "NO LINE OF SIGHT");
            UIKit.Text(new Rect(g.x - 150f, g.y + size * 0.5f + 14f, 300f, 22f), label, 16, c, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void DrawToasts(float W, float H)
        {
            float y = 210f;
            for (int i = 0; i < _toasts.Count; i++)
            {
                float a = Mathf.Clamp01(_toastTimes[i] * 1.5f);
                float w = Mathf.Max(360f, UIKit.Measure(_toasts[i], 20, FontStyle.Bold).x + 50f);
                Rect r = new Rect(W - w - 40f, y, w, 40f);
                UIKit.Rect(r, new Color(0.02f, 0.05f, 0.08f, 0.78f * a));
                UIKit.Rect(new Rect(r.x, r.y, 4f, r.height), new Color(0.3f, 0.95f, 1f, a));
                UIKit.Text(new Rect(r.x + 18f, r.y, r.width - 26f, r.height), _toasts[i], 20, new Color(0.85f, 0.97f, 1f, a), TextAnchor.MiddleLeft, FontStyle.Bold);
                y += 46f;
            }
        }

        // ================================================================== saves

        public void ResetAll()
        {
            _seen.Clear();
            _scanned.Clear();
            _kills.Clear();
            _partsBroken.Clear();
            _toasts.Clear();
            _toastTimes.Clear();
            _scanTarget = null;
            _scanT = 0f;
        }

        public void Capture(SaveData d)
        {
            if (d.Creatures == null) d.Creatures = new CreatureSave();
            CreatureSave s = d.Creatures;
            s.Seen = new List<string>(_seen);
            s.Scanned = new List<string>(_scanned);
            s.KillIds = new List<string>();
            s.KillCounts = new List<int>();
            foreach (KeyValuePair<string, int> kv in _kills)
            {
                s.KillIds.Add(kv.Key);
                s.KillCounts.Add(kv.Value);
            }
            s.PartsBroken = new List<string>(_partsBroken);
        }

        public void Restore(SaveData d)
        {
            _seen.Clear();
            _scanned.Clear();
            _kills.Clear();
            _partsBroken.Clear();
            _scanTarget = null;
            CreatureSave s = d != null ? d.Creatures : null;
            if (s == null) return;
            if (s.Seen != null) foreach (string id in s.Seen) if (!string.IsNullOrEmpty(id)) _seen.Add(id);
            if (s.Scanned != null) foreach (string id in s.Scanned) if (!string.IsNullOrEmpty(id)) _scanned.Add(id);
            if (s.KillIds != null && s.KillCounts != null)
            {
                int n = Mathf.Min(s.KillIds.Count, s.KillCounts.Count);
                for (int i = 0; i < n; i++) if (!string.IsNullOrEmpty(s.KillIds[i])) _kills[s.KillIds[i]] = Mathf.Max(0, s.KillCounts[i]);
            }
            if (s.PartsBroken != null) foreach (string k in s.PartsBroken) if (!string.IsNullOrEmpty(k)) _partsBroken.Add(k);
        }
    }
}
