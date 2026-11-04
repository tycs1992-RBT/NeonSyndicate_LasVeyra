using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One saved review camera: where it stands, where it looks and its vertical field of view.</summary>
    public struct ValeReviewShot
    {
        public string Name;
        public Vector3 Eye, Forward, Up;
        public float Fov;
    }

    /// <summary>
    /// Reads the Vale's saved review cameras (Docs/ShardwakeVale/SplitCrown/cameras.json and review-shots.json, the
    /// same files the offline previews use) so a native capture frames exactly the same views. Pure text parsing.
    /// </summary>
    public static class ValeReviewShots
    {
        /// <summary>The camera files, relative to the project folder.</summary>
        public static readonly string[] Files =
        {
            "Docs/ShardwakeVale/SplitCrown/cameras.json",
            "Docs/ShardwakeVale/SplitCrown/review-shots.json",
        };

        private const string Number = @"[-+0-9.eE]+";

        /// <summary>Every shot in one JSON text, in file order. Malformed entries are skipped.</summary>
        public static List<ValeReviewShot> Parse(string json)
        {
            List<ValeReviewShot> shots = new List<ValeReviewShot>();
            if (string.IsNullOrEmpty(json)) return shots;
            string vector = @"\[\s*(" + Number + @")\s*,\s*(" + Number + @")\s*,\s*(" + Number + @")\s*\]";
            Regex entry = new Regex("\"(\\w+)\"\\s*:\\s*\\{\\s*\"eye\"\\s*:\\s*" + vector + "\\s*,\\s*\"forward\"\\s*:\\s*" + vector +
                                    "\\s*,\\s*\"up\"\\s*:\\s*" + vector + "\\s*,\\s*\"fov\"\\s*:\\s*(" + Number + ")");
            foreach (Match m in entry.Matches(json))
            {
                ValeReviewShot shot = new ValeReviewShot
                {
                    Name = m.Groups[1].Value,
                    Eye = Read(m, 2),
                    Forward = Read(m, 5),
                    Up = Read(m, 8),
                    Fov = F(m.Groups[11].Value),
                };
                if (shot.Forward.sqrMagnitude < 1e-8f || shot.Fov <= 0f) continue;
                shots.Add(shot);
            }
            return shots;
        }

        private static Vector3 Read(Match m, int first)
        {
            return new Vector3(F(m.Groups[first].Value), F(m.Groups[first + 1].Value), F(m.Groups[first + 2].Value));
        }

        private static float F(string s)
        {
            return float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
