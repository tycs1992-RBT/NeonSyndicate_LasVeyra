using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The receipt line ending rules (why the Blender Malik and Dante stayed procedural on Windows): a text file checked out
    /// with CRLF endings hashes like Git's LF copy again, binary art is never touched. Engine-free.
    /// </summary>
    public sealed class ReceiptLineEndingsTests
    {
        private static string Sha(byte[] data)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }

        private static byte[] ToCrLf(byte[] lf)
        {
            MemoryStream result = new MemoryStream();
            foreach (byte b in lf)
            {
                if (b == 10) result.WriteByte(13);
                result.WriteByte(b);
            }
            return result.ToArray();
        }

        [Test]
        public void CrLfPairsTurnBackIntoLf()
        {
            byte[] crlf = Encoding.ASCII.GetBytes("{\r\n  \"a\": 1\r\n}\r\n");
            Assert.AreEqual("{\n  \"a\": 1\n}\n", Encoding.ASCII.GetString(ReceiptLineEndings.ToLf(crlf, crlf.Length)));
            byte[] lone = Encoding.ASCII.GetBytes("a\rb\r\nc\r");
            Assert.AreEqual("a\rb\nc\r", Encoding.ASCII.GetString(ReceiptLineEndings.ToLf(lone, lone.Length)));
            Assert.IsTrue(ReceiptLineEndings.HasCrLf(crlf, crlf.Length));
            Assert.IsFalse(ReceiptLineEndings.HasCrLf(Encoding.ASCII.GetBytes("a\nb\rc"), 5));
            Assert.AreEqual(0, ReceiptLineEndings.ToLf(null, 0).Length);
        }

        [Test]
        public void BinaryArtIsNeverText()
        {
            byte[] png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 };
            Assert.IsFalse(ReceiptLineEndings.LooksText(png, png.Length), "a PNG carries a CR LF pair but also zero bytes");
            byte[] fbx = Encoding.ASCII.GetBytes("Kaydara FBX Binary  \0\x1a\0");
            Assert.IsFalse(ReceiptLineEndings.LooksText(fbx, fbx.Length));
            Assert.IsTrue(ReceiptLineEndings.LooksText(Encoding.ASCII.GetBytes("{ \"ok\": true }\r\n"), 16));
            byte[] late = new byte[ReceiptLineEndings.TextProbe + 10];
            for (int i = 0; i < late.Length; i++) late[i] = 65;
            late[ReceiptLineEndings.TextProbe + 5] = 0;
            Assert.IsTrue(ReceiptLineEndings.LooksText(late, late.Length), "Git only probes the first 8000 bytes");
        }

        [Test]
        public void WindowsCheckoutsOfTheLiveReceiptsMatchAgain()
        {
            // The exact files that refused Malik H9 and Dante H9 on a Windows checkout, against their recorded receipt hashes.
            string[] receipts = { "Assets/NeonSyndicate/Art/ProductionH9/candidate.json", "Assets/NeonSyndicate/Art/ProductionH9/NPC/candidate.json" };
            int checkedFiles = 0;
            foreach (string receipt in receipts)
            {
                if (!File.Exists(receipt)) Assert.Ignore("Run from the project folder: " + receipt);
                string json = File.ReadAllText(receipt);
                int at = 0;
                while ((at = json.IndexOf("\"path\"", at, StringComparison.Ordinal)) >= 0)
                {
                    int p0 = json.IndexOf('"', json.IndexOf(':', at) + 1) + 1;
                    string path = json.Substring(p0, json.IndexOf('"', p0) - p0);
                    int s = json.IndexOf("\"sha256\"", p0, StringComparison.Ordinal);
                    int s0 = json.IndexOf('"', json.IndexOf(':', s) + 1) + 1;
                    string sha = json.Substring(s0, json.IndexOf('"', s0) - s0);
                    at = s0;
                    if (!path.EndsWith(".json", StringComparison.Ordinal) || !File.Exists(path)) continue;
                    byte[] lf = File.ReadAllBytes(path);
                    if (ReceiptLineEndings.HasCrLf(lf, lf.Length)) lf = ReceiptLineEndings.ToLf(lf, lf.Length);
                    Assert.AreEqual(sha, Sha(lf), path + " (LF, as Git stores it)");
                    byte[] windows = ToCrLf(lf);
                    Assert.AreNotEqual(sha, Sha(windows), path + " really differs byte for byte on Windows");
                    Assert.IsTrue(ReceiptLineEndings.LooksText(windows, windows.Length), path);
                    Assert.AreEqual(sha, Sha(ReceiptLineEndings.ToLf(windows, windows.Length)), path + " (Windows CRLF turned back)");
                    checkedFiles++;
                }
            }
            Assert.AreEqual(3, checkedFiles, "MalikFullBodyH9.json and both export-stats.json files");
        }
    }
}
