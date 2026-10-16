using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Regression coverage for saving after backup recovery. Uses temporary test files, never player saves.
    /// Portable cases call the actual fallback method directly so coverage does not depend on File.Replace support.
    /// </summary>
    public class SaveRecovery04DTests
    {
        private const string First = "{\"Version\":1,\"Step\":1}";
        private const string Second = "{\"Version\":1,\"Step\":2}";
        private const string Third = "{\"Version\":1,\"Step\":3}";
        private const string Broken = "{ truncated";
        private string _directory;
        private string _slot;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ns04d_recovery_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _slot = Path.Combine(_directory, "slot.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        private static bool Valid(string contents)
        { return contents == First || contents == Second || contents == Third; }

        private void Seed(string main, string backup)
        {
            if (main != null) File.WriteAllText(_slot, main);
            if (backup != null) File.WriteAllText(SaveFileTransaction.BackupPath(_slot), backup);
        }

        private void InstallSnapshot(bool portable, string contents, Func<string, bool> validator)
        {
            if (portable)
            {
                // The fallback consumes an already verified snapshot, as it does inside Write.
                Assert.That(validator == null || validator(contents), Is.True);
                File.WriteAllText(SaveFileTransaction.TempPath(_slot), contents);
                InvokePortable(validator);
            }
            else
            {
                string error;
                Assert.That(SaveFileTransaction.Write(_slot, contents, validator, out error),
                    Is.EqualTo(SaveFileTransaction.Result.Ok), error);
            }
            Assert.That(File.ReadAllText(_slot), Is.EqualTo(contents));
            Assert.That(File.Exists(SaveFileTransaction.TempPath(_slot)), Is.False);
        }

        private void InvokePortable(Func<string, bool> validator)
        {
            MethodInfo method = typeof(SaveFileTransaction).GetMethod("InstallPortable", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Exercise the real production fallback, not a reimplementation.");
            method.Invoke(null, new object[] { SaveFileTransaction.TempPath(_slot), _slot, validator });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SavingAfterRecoveryDoesNotBackUpTheCorruptPrimary(bool portable)
        {
            Seed(Broken, First);
            byte[] originalBackup = File.ReadAllBytes(SaveFileTransaction.BackupPath(_slot));
            bool recovered;
            Assert.That(SaveFileTransaction.ReadValidated(_slot, Valid, out recovered), Is.EqualTo(First));
            Assert.That(recovered, Is.True);

            InstallSnapshot(portable, Second, Valid);
            CollectionAssert.AreEqual(originalBackup, File.ReadAllBytes(SaveFileTransaction.BackupPath(_slot)));

            // A second damaged main must still be recoverable, not a pair of damaged files.
            File.WriteAllText(_slot, Broken);
            Assert.That(SaveFileTransaction.ReadValidated(_slot, Valid, out recovered), Is.EqualTo(First));
            Assert.That(recovered, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AHealthySaveAfterRecoveryResumesNormalRotation(bool portable)
        {
            Seed(Broken, First);
            InstallSnapshot(portable, Second, Valid);
            InstallSnapshot(portable, Third, Valid);
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(_slot)), Is.EqualTo(Second));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HealthyPrimaryReplacesAnOlderBackup(bool portable)
        {
            Seed(Second, First);
            InstallSnapshot(portable, Third, Valid);
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(_slot)), Is.EqualTo(Second));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingPrimaryKeepsAnExistingBackup(bool portable)
        {
            Seed(null, First);
            InstallSnapshot(portable, Second, Valid);
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(_slot)), Is.EqualTo(First));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidPrimaryDoesNotBecomeANewBackup(bool portable)
        {
            Seed(Broken, null);
            InstallSnapshot(portable, Second, Valid);
            Assert.That(File.Exists(SaveFileTransaction.BackupPath(_slot)), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ValidatorExceptionOnOldPrimaryKeepsRecoveryBackup(bool portable)
        {
            Seed(Broken, First);
            Func<string, bool> validator = delegate(string contents)
            {
                if (contents == Broken) throw new InvalidDataException("Deliberate fixture rejection.");
                return Valid(contents);
            };
            InstallSnapshot(portable, Second, validator);
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(_slot)), Is.EqualTo(First));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NullValidatorRetainsLegacyReadableTextBehavior(bool portable)
        {
            Seed("older readable text", First);
            InstallSnapshot(portable, "new readable text", null);
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(_slot)), Is.EqualTo("older readable text"));
        }

        [Test]
        public void RejectedNewSnapshotLeavesBothRecoveryFilesUnchanged()
        {
            Seed(Broken, First);
            string error;
            Assert.That(SaveFileTransaction.Write(_slot, "not a save", Valid, out error),
                Is.EqualTo(SaveFileTransaction.Result.VerifyFailed));
            Assert.That(error, Does.StartWith("Verify:"));
            Assert.That(File.ReadAllText(_slot), Is.EqualTo(Broken));
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(_slot)), Is.EqualTo(First));
            Assert.That(File.Exists(SaveFileTransaction.TempPath(_slot)), Is.False);
        }

        [Test]
        public void ThrowingNewSnapshotValidatorLeavesBothFilesUnchanged()
        {
            Seed(Second, First);
            string error;
            Func<string, bool> validator = delegate(string contents) { throw new InvalidDataException("Fixture rejection."); };
            Assert.That(SaveFileTransaction.Write(_slot, Third, validator, out error),
                Is.EqualTo(SaveFileTransaction.Result.VerifyFailed));
            Assert.That(File.ReadAllText(_slot), Is.EqualTo(Second));
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(_slot)), Is.EqualTo(First));
            Assert.That(File.Exists(SaveFileTransaction.TempPath(_slot)), Is.False);
        }

        [Test]
        public void BlockedInstallRetainsBackupAndVerifiedTemporaryFile()
        {
            Seed(null, First);
            Directory.CreateDirectory(_slot);
            string error;
            Assert.That(SaveFileTransaction.Write(_slot, Second, Valid, out error),
                Is.EqualTo(SaveFileTransaction.Result.InstallFailed));
            Assert.That(error, Does.StartWith("Install:"));
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(_slot)), Is.EqualTo(First));
            Assert.That(File.ReadAllText(SaveFileTransaction.TempPath(_slot)), Is.EqualTo(Second));
        }

        [Test]
        public void BlockedPortableInstallRetainsBackupAndVerifiedTemporaryFile()
        {
            Seed(null, First);
            Directory.CreateDirectory(_slot);
            File.WriteAllText(SaveFileTransaction.TempPath(_slot), Second);
            TargetInvocationException error = Assert.Throws<TargetInvocationException>(delegate { InvokePortable(Valid); });
            Assert.That(error.InnerException, Is.Not.Null);
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(_slot)), Is.EqualTo(First));
            Assert.That(File.ReadAllText(SaveFileTransaction.TempPath(_slot)), Is.EqualTo(Second));
        }
    }
}
