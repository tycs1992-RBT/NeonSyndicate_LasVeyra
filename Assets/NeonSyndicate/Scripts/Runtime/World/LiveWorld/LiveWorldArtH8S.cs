using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S live world art: puts the real modeled environment pieces (the six H8R modules, the three H8S Forge props and
    /// anything Tyler drops into Assets/NeonSyndicate/Art/LiveWorld) into the revision-10 world (Gothic Civic H5) during
    /// play, from the Editor-prepared list (LiveWorldCatalogH8S) and each piece's placement file. Pieces are visual only:
    /// no collider, rigidbody or script, on the IgnoreRaycast layer, culled by distance through an LODGroup. Every spot
    /// passes the planner's safety rules (LiveWorldSafetyH8S) and the physical probes (floor, slope, solid geometry,
    /// trigger volumes); a replaced Forge anvil fits inside the station's own collider. Anything missing or unsafe is
    /// skipped with one LIVE_WORLD warning and the world keeps its original look there. The Forge station, its E or
    /// gamepad Y use, every collider, quest, save and reward stay with their original owners. F2 > Live world lists
    /// what was placed and can hide it or place it again.
    /// </summary>
    public sealed partial class LiveWorldArtH8S : MonoBehaviour
    {
        /// <summary>The STATUS page row id.</summary>
        public const string ModuleId = "live_world_h8s";
        /// <summary>The STATUS page row title.</summary>
        public const string ModuleTitle = "Live world art (H8S)";
        /// <summary>Seconds after the world finishes loading before placing starts (stations and quest givers appear first).</summary>
        private const float SettleSeconds = 1.5f;
        /// <summary>A placement line whose prerequisite (a Forge station, a venue) never appears is dropped after this many seconds.</summary>
        private const float GiveUpSeconds = 45f;
        /// <summary>At most this many copies in all, so a runaway placement file cannot hurt the frame rate.</summary>
        private const int MaxCopies = 64;

        private sealed class Piece
        {
            public LiveWorldEntryH8S Entry;
            public LiveWorldSpecH8S Spec;
            public int Placed;
            public bool Measured;
            public Vector3 Min, Max;
        }

        private sealed class Job
        {
            public Piece Piece;
            public LiveWorldOrderH8S Order;
            public int Rank;
        }

        private CityLayout _layout;
        private LiveWorldCatalogH8S _catalog;
        private Transform _root;
        private readonly List<Piece> _pieces = new List<Piece>();
        private readonly List<Job> _jobs = new List<Job>();
        private readonly List<PlanRectH8S> _placed = new List<PlanRectH8S>();
        private readonly List<string> _report = new List<string>();
        private int _next, _copies, _skipped;
        private float _placementSeconds;
        private bool _started, _finished, _hidden;
        private string _state = "Starting.";

        /// <summary>True while placed live art is showing (the Editor previews refuse to double it).</summary>
        public bool ShowingArt { get { return !_hidden && _copies > 0; } }

        /// <summary>Copies placed so far.</summary>
        public int Copies { get { return _copies; } }

        /// <summary>Called once by the boot after the campaign cast exists. Never throws.</summary>
        public void Init(CityLayout layout)
        {
            _layout = layout;
            DebugPages.Register(new MenuPage { Title = "Live world", Order = 58, Draw = DrawDebug });
            if (layout == null || !layout.GothicCivicH5)
            {
                _state = LiveWorldMessagesH8S.WrongWorld(WorldIdentity04OB.Describe());
                Log.Info(_state);
                ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Disabled, "Live world art belongs to Gothic Civic H5 (world revision 10).");
                enabled = false;
                return;
            }
            _catalog = Resources.Load<LiveWorldCatalogH8S>(LiveWorldCatalogH8S.ResourcePath);
            if (_catalog == null || _catalog.Entries == null)
            {
                _state = LiveWorldMessagesH8S.CatalogMissing();
                Log.Warn(_state);
                LiveCastNotice.AssetProblem("World art", "The generated world-art catalog is missing.", "Stop Play, then Neon Syndicate > Live World > Prepare live world art now. Player builds need rebuilding after preparation.");
                ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Partial, "The prepared art list is missing.", "In the Unity Editor: Neon Syndicate > Live World > Prepare live world art now.");
                enabled = false;
                return;
            }
            _root = new GameObject("Live World Art H8S").transform;
            _root.SetParent(transform, false);
            LoadPieces();
            ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Loaded, _pieces.Count + " pieces prepared; placing them once the world has loaded.");
            _state = "Waiting for the world to finish loading.";
        }

        private void LoadPieces()
        {
            _pieces.Clear();
            _jobs.Clear();
            for (int i = 0; i < _catalog.Entries.Count; i++)
            {
                LiveWorldEntryH8S entry = _catalog.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.Name)) continue;
                Piece piece = new Piece { Entry = entry, Spec = LiveWorldPlanH8S.Parse(entry.Name, PlacementText(entry)) };
                _pieces.Add(piece);
                for (int n = 0; n < entry.Notes.Count; n++) Log.Info(LiveWorldMessagesH8S.Note(entry.Name, entry.Notes[n]));
                for (int n = 0; n < piece.Spec.Notes.Count; n++) Log.Warn(LiveWorldMessagesH8S.Note(entry.Name, piece.Spec.Notes[n]));
                if (!string.IsNullOrEmpty(entry.Problem))
                {
                    Skip(piece, entry.Problem);
                    continue;
                }
                if (piece.Spec.Skip)
                {
                    Line(entry.Name + ": switched off by its placement file" + (piece.Spec.SkipReason.Length > 0 ? " (" + piece.Spec.SkipReason + ")" : "") + ".");
                    continue;
                }
                for (int o = 0; o < piece.Spec.Orders.Count; o++)
                {
                    LiveWorldOrderH8S order = piece.Spec.Orders[o];
                    _jobs.Add(new Job { Piece = piece, Order = order, Rank = Rank(order.Kind) });
                }
            }
            // Fitted replacements first, then rooms, then outdoor pieces, then the route dressing (it fills what is left).
            _jobs.Sort((a, b) =>
            {
                int c = a.Rank.CompareTo(b.Rank);
                if (c != 0) return c;
                c = string.CompareOrdinal(a.Piece.Entry.Name, b.Piece.Entry.Name);
                return c != 0 ? c : a.Order.Line.CompareTo(b.Order.Line);
            });
        }

        /// <summary>The placement text: in the Editor the file on disk (so edits apply on the next Play or Place again), in builds the prepared copy.</summary>
        private static string PlacementText(LiveWorldEntryH8S entry)
        {
#if UNITY_EDITOR
            try
            {
                string path = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, entry.Folder, LiveWorldPlanH8S.FileName);
                if (!string.IsNullOrEmpty(entry.Folder) && System.IO.File.Exists(path)) return System.IO.File.ReadAllText(path);
            }
            catch (Exception) { }
#endif
            return entry.Placement ?? "";
        }

        private static int Rank(LiveWorldOrderKindH8S kind)
        {
            switch (kind)
            {
                case LiveWorldOrderKindH8S.Replace: return 0;
                case LiveWorldOrderKindH8S.Room: return 1;
                case LiveWorldOrderKindH8S.Near: return 2;
                default: return 3;
            }
        }

        private void Update()
        {
            if (_finished || _catalog == null) return;
            // Cast actors do not exist on the title screen. Title or pause time must never consume their placement wait.
            if (Game.State != GameState.Playing || Game.Player == null) return;
            _placementSeconds += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            float waited = _placementSeconds;
            if (waited < SettleSeconds) return;
            if (!_started)
            {
                _started = true;
                try { BuildContext(); }
                catch (Exception e) { Log.Exception("Live world context", e); }
                _state = "Placing pieces.";
            }
            if (_next >= _jobs.Count)
            {
                Finish();
                return;
            }
            Job job = _jobs[_next];
            bool done;
            try { done = Run(job, waited); }
            catch (Exception e)
            {
                Log.Exception("Live world " + job.Piece.Entry.Name, e);
                Skip(job.Piece, "line " + job.Order.Line + " stopped with an error (" + e.Message + ").");
                done = true;
            }
            if (done) _next++;
        }

        /// <summary>Tries one placement line; false when it must wait for something that is not built yet.</summary>
        private bool Run(Job job, float waited)
        {
            if (_copies >= MaxCopies)
            {
                Skip(job.Piece, "line " + job.Order.Line + " was not placed because the world already holds " + MaxCopies + " live pieces.");
                return true;
            }
            switch (job.Order.Kind)
            {
                case LiveWorldOrderKindH8S.Replace: return RunReplace(job, waited);
                case LiveWorldOrderKindH8S.Room: return RunRoom(job, waited);
                case LiveWorldOrderKindH8S.Near: RunNear(job); return true;
                default: RunRoute(job); return true;
            }
        }

        private void Finish()
        {
            _finished = true;
            enabled = false;
            _state = LiveWorldMessagesH8S.Summary(_pieces.Count, _copies, _skipped);
            Log.Info(_state);
            ModuleStatus.Report(ModuleId, ModuleTitle, _skipped == 0 ? ModuleState.Loaded : ModuleState.Partial,
                _skipped == 0 ? "" : _skipped + " placement line(s) skipped; F2 > Live world lists why.");
            ModuleStatus.SetCount(ModuleId, _copies, "live pieces placed");
        }

        private void Skip(Piece piece, string reason)
        {
            _skipped++;
            string message = LiveWorldMessagesH8S.Skipped(piece.Entry.Name, reason);
            Log.Warn(message);
            LiveCastNotice.AssetProblem("World art", reason, "F2 > Live world lists the piece and location. Stop Play, pull Git LFS if requested, then Neon Syndicate > Live World > Prepare live world art now.");
            Line(piece.Entry.Name + ": skipped, " + reason);
        }

        private void Line(string text)
        {
            if (_report.Count < 80) _report.Add(text);
        }

        private void OnDestroy()
        {
            RestoreReplaced();
            ReleaseSurfaces();
        }
    }
}
