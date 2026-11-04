using System.Globalization;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The exact words the live world art writes to the Console and the F2 page, kept in one engine-free place so
    /// Docs/LIVE_WORLD_ART.md can quote them and the tests can hold them steady. Every line starts with LIVE_WORLD.
    /// </summary>
    public static class LiveWorldMessagesH8S
    {
        /// <summary>The tag at the start of every live world Console line.</summary>
        public const string Tag = "LIVE_WORLD";

        /// <summary>One copy placed.</summary>
        public static string Placed(string piece, string how, Vector3 at)
        {
            return Tag + " placed " + piece + " " + how + " at " + Point(at) + ".";
        }

        /// <summary>A piece (or one of its placement lines) skipped, with the reason. The world keeps its old look there.</summary>
        public static string Skipped(string piece, string reason)
        {
            return Tag + " skipped " + piece + ": " + reason + " The world keeps its original look there.";
        }

        /// <summary>A note about a placement file line that was ignored or corrected.</summary>
        public static string Note(string piece, string note)
        {
            return Tag + " note for " + piece + ": " + note + ".";
        }

        /// <summary>The prepared list is missing from this build or Editor session.</summary>
        public static string CatalogMissing()
        {
            return Tag + " the prepared art list is missing, so no live world art was placed. In the Unity Editor choose Neon Syndicate > Live World > Prepare live world art now (it also runs by itself when the project opens, before Play and before every build).";
        }

        /// <summary>This world revision does not take live world art.</summary>
        public static string WrongWorld(string identity)
        {
            return Tag + " live world art belongs to Gothic Civic H5 (world revision 10); this session is " + identity + ", so nothing was placed.";
        }

        /// <summary>The one-line summary after every placement line was tried.</summary>
        public static string Summary(int pieces, int copies, int skipped)
        {
            return Tag + " finished: " + copies + " cop" + (copies == 1 ? "y" : "ies") + " of " + pieces + " piece" + (pieces == 1 ? "" : "s") + " placed, " + skipped + " placement line(s) skipped.";
        }

        /// <summary>A readable point such as (-210.0, 0.2, 16.0).</summary>
        public static string Point(Vector3 p)
        {
            return "(" + p.x.ToString("0.0", CultureInfo.InvariantCulture) + ", " + p.y.ToString("0.0", CultureInfo.InvariantCulture) + ", " + p.z.ToString("0.0", CultureInfo.InvariantCulture) + ")";
        }
    }
}
