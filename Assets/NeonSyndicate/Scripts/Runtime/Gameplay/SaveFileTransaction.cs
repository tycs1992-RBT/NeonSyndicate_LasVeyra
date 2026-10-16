using System;
using System.IO;
using System.Text;

namespace NeonSyndicate
{
    /// <summary>
    /// Engine-free save file transaction. A new snapshot is written to a temporary file, flushed to disk, read back and
    /// validated, and only then installed over the old save. A readable, valid previous snapshot becomes the ".bak" copy.
    /// An invalid main file never replaces an existing recovery backup. The old
    /// save is never deleted before a complete validated replacement exists. Reading falls back to the backup when the
    /// main file is missing, unreadable or fails validation. This is durable after a successful write; it does not make
    /// a single-player local save immune to deliberate rollback or to a disk that lies about flushing.
    /// </summary>
    public static class SaveFileTransaction
    {
        public enum Result
        {
            /// <summary>The new snapshot is installed; a valid previous main is backed up, otherwise the backup is retained.</summary>
            Ok = 0,
            /// <summary>The temporary file could not be written. The previous save is untouched.</summary>
            WriteFailed = 1,
            /// <summary>The written bytes did not read back identically or failed validation. The previous save is untouched.</summary>
            VerifyFailed = 2,
            /// <summary>The validated file could not be installed. The previous save (or its backup) is still on disk.</summary>
            InstallFailed = 3
        }

        public static void ResetStatics() { }

        public static string TempPath(string path) { return path + ".tmp"; }
        public static string BackupPath(string path) { return path + ".bak"; }

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>Writes contents to path through the verified temporary-file transaction. error explains any failure.</summary>
        public static Result Write(string path, string contents, Func<string, bool> validate, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(path) || contents == null)
            {
                error = "No path or no contents.";
                return Result.WriteFailed;
            }
            string tmp = TempPath(path);
            try
            {
                byte[] bytes = Utf8.GetBytes(contents);
                using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(true);
                }
            }
            catch (Exception e)
            {
                TryDelete(tmp);
                error = "Write: " + e.Message;
                return Result.WriteFailed;
            }
            try
            {
                string back = File.ReadAllText(tmp, Utf8);
                if (!string.Equals(back, contents, StringComparison.Ordinal))
                {
                    TryDelete(tmp);
                    error = "Verify: the written file does not match the snapshot.";
                    return Result.VerifyFailed;
                }
                if (validate != null && !validate(back))
                {
                    TryDelete(tmp);
                    error = "Verify: the snapshot failed validation.";
                    return Result.VerifyFailed;
                }
            }
            catch (Exception e)
            {
                TryDelete(tmp);
                error = "Verify: " + e.Message;
                return Result.VerifyFailed;
            }
            try
            {
                Install(tmp, path, validate);
                return Result.Ok;
            }
            catch (Exception e)
            {
                error = "Install: " + e.Message;
                return Result.InstallFailed;
            }
        }

        /// <summary>
        /// Installs the verified temporary file. Only a main snapshot accepted by the same validator may replace the
        /// backup. In particular, saving after recovery must not rotate the rejected main into the good backup.
        /// Calls are serialized by the game's main-thread save flow, not a cross-process writer lock.
        /// </summary>
        private static void Install(string tmp, string path, Func<string, bool> validate)
        {
            if (!File.Exists(path))
            {
                File.Move(tmp, path);
                return;
            }
            string bak = BackupForValidMain(path, validate);
            try
            {
                // A null backup argument replaces the main without touching an existing .bak.
                File.Replace(tmp, path, bak, true);
                return;
            }
            catch (PlatformNotSupportedException) { }
            catch (NotSupportedException) { }
            catch (IOException) { if (!File.Exists(tmp)) throw; }
            catch (UnauthorizedAccessException) { if (!File.Exists(tmp)) throw; }
            InstallPortable(tmp, path, validate);
        }

        private static string BackupForValidMain(string path, Func<string, bool> validate)
        {
            string main = TryRead(path);
            return main != null && (validate == null || SafeValidate(validate, main)) ? BackupPath(path) : null;
        }

        /// <summary>
        /// Non-atomic replacement for filesystems without File.Replace. Recheck the backup policy after a failed
        /// replace; never copy rejected main bytes over a good backup. Keep the verified temporary file on failure.
        /// </summary>
        private static void InstallPortable(string tmp, string path, Func<string, bool> validate)
        {
            string bak = BackupForValidMain(path, validate);
            if (bak != null) File.Copy(path, bak, true);
            File.Copy(tmp, path, true);
            TryDelete(tmp);
        }

        /// <summary>
        /// Reads the save at path, or its backup when the main file is missing, unreadable or invalid. Returns null when
        /// neither is usable. fromBackup reports which file supplied the text.
        /// </summary>
        public static string ReadValidated(string path, Func<string, bool> validate, out bool fromBackup)
        {
            fromBackup = false;
            string main = TryRead(path);
            if (main != null && (validate == null || SafeValidate(validate, main))) return main;
            string bak = TryRead(BackupPath(path));
            if (bak != null && (validate == null || SafeValidate(validate, bak)))
            {
                fromBackup = true;
                return bak;
            }
            return null;
        }

        public static bool AnyExists(string path)
        {
            try { return File.Exists(path) || File.Exists(BackupPath(path)); }
            catch (Exception) { return false; }
        }

        private static bool SafeValidate(Func<string, bool> validate, string text)
        {
            try { return validate(text); }
            catch (Exception) { return false; }
        }

        private static string TryRead(string p)
        {
            try { return File.Exists(p) ? File.ReadAllText(p, Utf8) : null; }
            catch (Exception) { return null; }
        }

        private static void TryDelete(string p)
        {
            try { if (File.Exists(p)) File.Delete(p); }
            catch (Exception) { }
        }
    }
}
