using System;
using System.IO;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The build manifest the build step writes to Resources/BuildManifest04E.json for a packaged player (and removes
    /// again afterwards). It holds the source revision and dirty state at build time, never usernames, credentials or
    /// paths. Append fields only.
    /// </summary>
    [Serializable]
    public sealed class BuildManifest04E
    {
        public int Version = 1;
        public string Label = "";
        /// <summary>The full commit id the player was built from ("" when the build machine had no Git metadata).</summary>
        public string Revision = "";
        public string Branch = "";
        /// <summary>True when the working tree had uncommitted tracked changes at build time.</summary>
        public bool Dirty;
        /// <summary>False when the dirty check could not run (no Git, timeout).</summary>
        public bool DirtyKnown;
        public string BuiltUtc = "";
        public string Unity = "";
        public string Target = "";
    }

    /// <summary>Where the STATUS page's revision came from.</summary>
    public enum RevisionSource04E
    {
        Unavailable = 0,
        /// <summary>Read from the project's .git folder by the editor (play mode in the editor).</summary>
        EditorGit = 1,
        /// <summary>From the packaged player's build manifest.</summary>
        Manifest = 2
    }

    /// <summary>The source revision as the STATUS page and the session chip show it.</summary>
    public struct RevisionInfo04E
    {
        public RevisionSource04E Source;
        public string Revision;
        public string Branch;
        /// <summary>"clean", "local changes" or "" when not known.</summary>
        public string State;
        public string BuiltUtc;
        /// <summary>Why the revision is unavailable or partial, in plain words.</summary>
        public string Note;

        public string Short { get { return string.IsNullOrEmpty(Revision) ? "" : Revision.Substring(0, Math.Min(7, Revision.Length)); } }
    }

    /// <summary>
    /// Pure parsing of a Git directory's HEAD, loose refs, packed-refs and worktree pointer files. No process is
    /// started and nothing is written. Engine-free, tested in Guidance04ETests.
    /// </summary>
    public static class GitRefReader04E
    {
        /// <summary>True for a 40 (SHA-1) or 64 (SHA-256) character lowercase or uppercase hexadecimal id.</summary>
        public static bool IsSha(string s)
        {
            if (string.IsNullOrEmpty(s) || (s.Length != 40 && s.Length != 64)) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
            return true;
        }

        /// <summary>Reads HEAD: "ref: refs/heads/x" gives refName, a bare id (detached head) gives sha.</summary>
        public static bool ParseHead(string headText, out string refName, out string sha)
        {
            refName = "";
            sha = "";
            if (string.IsNullOrEmpty(headText)) return false;
            string t = headText.Trim();
            if (t.StartsWith("ref:", StringComparison.Ordinal))
            {
                refName = t.Substring(4).Trim();
                return refName.StartsWith("refs/", StringComparison.Ordinal);
            }
            if (IsSha(t))
            {
                sha = t.ToLowerInvariant();
                return true;
            }
            return false;
        }

        /// <summary>A loose ref file's content gives its id ("" when it is not a plain id).</summary>
        public static string ParseLoose(string looseText)
        {
            if (string.IsNullOrEmpty(looseText)) return "";
            string t = looseText.Trim();
            return IsSha(t) ? t.ToLowerInvariant() : "";
        }

        /// <summary>Finds a ref in packed-refs text ("id refs/heads/x" lines; comments and peeled "^" lines skipped).</summary>
        public static string FindPacked(string packedText, string refName)
        {
            if (string.IsNullOrEmpty(packedText) || string.IsNullOrEmpty(refName)) return "";
            string[] lines = packedText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == '^') continue;
                int sp = line.IndexOf(' ');
                if (sp <= 0) continue;
                string id = line.Substring(0, sp);
                string name = line.Substring(sp + 1).Trim();
                if (name == refName && IsSha(id)) return id.ToLowerInvariant();
            }
            return "";
        }

        /// <summary>"refs/heads/feature/x" gives "feature/x"; other refs are returned unchanged.</summary>
        public static string BranchName(string refName)
        {
            if (string.IsNullOrEmpty(refName)) return "";
            const string heads = "refs/heads/";
            return refName.StartsWith(heads, StringComparison.Ordinal) ? refName.Substring(heads.Length) : refName;
        }

        /// <summary>A worktree or submodule ".git" file's "gitdir: path" line gives the path ("" otherwise).</summary>
        public static string ParseGitDirFile(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string t = text.Trim();
            return t.StartsWith("gitdir:", StringComparison.Ordinal) ? t.Substring(7).Trim() : "";
        }

        /// <summary>The dirty state line for `git status --porcelain -uno` output: any non-empty line means local changes.</summary>
        public static bool PorcelainIsDirty(string output)
        {
            if (string.IsNullOrEmpty(output)) return false;
            string[] lines = output.Split('\n');
            for (int i = 0; i < lines.Length; i++) if (lines[i].Trim().Length > 0) return true;
            return false;
        }

        /// <summary>
        /// Resolves the revision from a project root by reading files only (.git as a folder or a worktree pointer file,
        /// HEAD, the loose ref, then packed-refs in the folder or its common directory). Returns false with a plain
        /// reason when there is no readable Git metadata. Never throws.
        /// </summary>
        public static bool TryRead(string projectRoot, out string sha, out string branch, out string reason)
        {
            sha = "";
            branch = "";
            reason = "";
            try
            {
                if (string.IsNullOrEmpty(projectRoot)) { reason = "project folder unknown"; return false; }
                string dotGit = Path.Combine(projectRoot, ".git");
                string gitDir = dotGit;
                if (File.Exists(dotGit))
                {
                    string pointer = ParseGitDirFile(File.ReadAllText(dotGit));
                    if (pointer.Length == 0) { reason = "the .git file has no gitdir line"; return false; }
                    gitDir = Path.IsPathRooted(pointer) ? pointer : Path.GetFullPath(Path.Combine(projectRoot, pointer));
                }
                else if (!Directory.Exists(dotGit)) { reason = "no Git metadata in this folder (a source archive)"; return false; }
                string headPath = Path.Combine(gitDir, "HEAD");
                if (!File.Exists(headPath)) { reason = "HEAD is missing"; return false; }
                string refName, direct;
                if (!ParseHead(File.ReadAllText(headPath), out refName, out direct)) { reason = "HEAD could not be read"; return false; }
                if (direct.Length > 0)
                {
                    sha = direct;
                    branch = "detached";
                    return true;
                }
                branch = BranchName(refName);
                string common = gitDir;
                string commonFile = Path.Combine(gitDir, "commondir");
                if (File.Exists(commonFile))
                {
                    string c = File.ReadAllText(commonFile).Trim();
                    if (c.Length > 0) common = Path.IsPathRooted(c) ? c : Path.GetFullPath(Path.Combine(gitDir, c));
                }
                string[] dirs = { gitDir, common };
                foreach (string dir in dirs)
                {
                    string loose = Path.Combine(dir, refName.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(loose))
                    {
                        sha = ParseLoose(File.ReadAllText(loose));
                        if (sha.Length > 0) return true;
                    }
                }
                foreach (string dir in dirs)
                {
                    string packed = Path.Combine(dir, "packed-refs");
                    if (!File.Exists(packed)) continue;
                    sha = FindPacked(File.ReadAllText(packed), refName);
                    if (sha.Length > 0) return true;
                }
                reason = "the branch " + branch + " has no commit yet";
                return false;
            }
            catch (Exception e)
            {
                reason = "Git metadata could not be read (" + e.GetType().Name + ")";
                sha = "";
                return false;
            }
        }
    }

    /// <summary>
    /// The source revision for the STATUS page and the session chip. In the editor it reads the project's .git folder
    /// once per session (files only) and takes the dirty state from the one bounded background `git status` the editor
    /// ran at load (BuildInfo04EEditor pushes it). A packaged player reads Resources/BuildManifest04E.json written by
    /// the build step. Without either it says "revision unavailable" instead of inventing one. Nothing here starts a
    /// process or runs per frame.
    /// </summary>
    public static class BuildInfo04E
    {
        public const string ManifestResource = "BuildManifest04E";

        private static bool _resolved;
        private static RevisionInfo04E _info;
        /// <summary>Dirty state pushed by the editor ("clean", "local changes", or "" while unknown) and why it is unknown.</summary>
        private static string _editorState = "";
        private static string _editorNote = "";

        public static void ResetStatics()
        {
            _resolved = false;
            _info = new RevisionInfo04E();
            // The editor pushes its cached dirty state again when play mode starts (BuildInfo04EEditor).
            _editorState = "";
            _editorNote = "";
        }

        /// <summary>Called by the editor with the result of its bounded `git status` (state "clean", "local changes" or "").</summary>
        public static void SetEditorState(string state, string note)
        {
            _editorState = state ?? "";
            _editorNote = note ?? "";
            if (_resolved && _info.Source == RevisionSource04E.EditorGit)
            {
                _info.State = _editorState;
                if (_editorState.Length == 0 && _editorNote.Length > 0) _info.Note = _editorNote;
            }
        }

        /// <summary>The revision (resolved once, then cached for the session).</summary>
        public static RevisionInfo04E Current
        {
            get
            {
                if (!_resolved)
                {
                    _resolved = true;
                    try { _info = Resolve(); }
                    catch (Exception e)
                    {
                        _info = new RevisionInfo04E { Source = RevisionSource04E.Unavailable, Note = "revision check failed (" + e.GetType().Name + ")" };
                    }
                }
                return _info;
            }
        }

        /// <summary>One line: "revision abc1234 (feature/x), local changes" or "revision unavailable: reason".</summary>
        public static string Describe()
        {
            RevisionInfo04E r = Current;
            if (r.Source == RevisionSource04E.Unavailable || string.IsNullOrEmpty(r.Revision))
                return "revision unavailable" + (string.IsNullOrEmpty(r.Note) ? "" : ": " + r.Note);
            string s = "revision " + r.Short + (string.IsNullOrEmpty(r.Branch) ? "" : " (" + r.Branch + ")");
            if (!string.IsNullOrEmpty(r.State)) s += ", " + r.State;
            else s += ", dirty state unknown";
            if (r.Source == RevisionSource04E.Manifest) s += ", packaged build" + (string.IsNullOrEmpty(r.BuiltUtc) ? "" : " " + r.BuiltUtc);
            return s;
        }

        private static RevisionInfo04E Resolve()
        {
#if UNITY_EDITOR
            string root = Path.GetDirectoryName(Application.dataPath);
            string sha, branch, reason;
            if (GitRefReader04E.TryRead(root, out sha, out branch, out reason))
                return new RevisionInfo04E { Source = RevisionSource04E.EditorGit, Revision = sha, Branch = branch, State = _editorState, Note = _editorState.Length == 0 ? _editorNote : "" };
            return new RevisionInfo04E { Source = RevisionSource04E.Unavailable, Note = reason };
#else
            TextAsset ta = Resources.Load<TextAsset>(ManifestResource);
            if (ta == null) return new RevisionInfo04E { Source = RevisionSource04E.Unavailable, Note = "this build has no manifest (built without the Neon Syndicate build step)" };
            BuildManifest04E m = JsonUtility.FromJson<BuildManifest04E>(ta.text);
            if (m == null || !GitRefReader04E.IsSha(m.Revision))
                return new RevisionInfo04E { Source = RevisionSource04E.Unavailable, Note = "the build machine had no Git metadata", BuiltUtc = m != null ? m.BuiltUtc : "" };
            return new RevisionInfo04E
            {
                Source = RevisionSource04E.Manifest, Revision = m.Revision.ToLowerInvariant(), Branch = m.Branch ?? "", BuiltUtc = m.BuiltUtc ?? "",
                State = m.DirtyKnown ? (m.Dirty ? "local changes at build time" : "clean") : ""
            };
#endif
        }
    }
}
