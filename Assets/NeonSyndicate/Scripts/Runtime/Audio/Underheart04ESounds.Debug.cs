using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 debug page "Audio 04E": bank progress, the zone the ambience is in and why (request, region, weights), loop
    /// and voice use, rate-limit counts, and buttons to audition every cue, every loop, every zone and the encounter score
    /// in a native session (a forced zone or score lasts until "Follow the game" or the next load).
    /// </summary>
    public static partial class Underheart04ESounds
    {
        private static readonly int[] _debugLoopKeys = new int[UhAudioBank.LoopCount];
        /// <summary>Debug page: -1 normal, 0 forces the score off, 1 the skirmish pulse, 2 the boss drive.</summary>
        private static int _debugMusic = -1;

        private static void ResetDebugStatics()
        {
            for (int i = 0; i < _debugLoopKeys.Length; i++) _debugLoopKeys[i] = 0;
            _debugMusic = -1;
        }

        private static void DrawDebugPage(Rect r)
        {
            float x = r.x, y = r.y;
            Color dim = new Color(0.8f, 0.85f, 0.9f);
            UIKit.Text(new Rect(x, y, r.width, 28f), "UNDERHEART AUDIO (04E)", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 32f;
            Bank bank = _bank;
            string bankLine = bank == null ? "Sound bank: not started." :
                "Sound bank: " + bank.Uploaded + " of " + UhAudioBank.Total + " ready" + (bank.FailedCount > 0 ? ", " + bank.FailedCount + " failed (silent)" : "")
                + (bank.WorkerStarted ? (bank.WorkerDone ? ", built in the background" : ", building in the background") : ", building on the main thread");
            int busy = 0;
            if (_voices != null) for (int i = 0; i < _voices.Length; i++) if (_voices[i].Key != 0) busy++;
            UIKit.Text(new Rect(x, y, r.width, 24f), bankLine + "     Loops alive " + _loops.Count + ", voices busy " + busy + " of " + LoopVoices
                + "     One-shots played " + _played + ", dropped by the rate limit " + _limited, 16, Color.white, TextAnchor.UpperLeft);
            y += 26f;
            string region = Game.Underheart != null ? (string.IsNullOrEmpty(Game.Underheart.PlayerRegion) ? "surface" : Game.Underheart.PlayerRegion) : "no Underheart";
            UIKit.Text(new Rect(x, y, r.width, 24f), "Zone now: " + _zone + (_debugZone >= 0 ? " (forced)" : "") + "     requested " + _requested + "     region " + region
                + "     street level " + SurfaceGain.ToString("0.00") + "     score " + _musicLevel.ToString("0.00") + " / drive " + _driveLevel.ToString("0.00"), 16, dim, TextAnchor.UpperLeft);
            y += 26f;
            float bw = (r.width - 7f * 8f) / 8f;
            for (int z = 0; z < UhAudioRules.ZoneCount; z++)
            {
                Rect b = new Rect(x + z * (bw + 8f), y, bw, 22f);
                UIKit.Bar(b, ZoneWeight((UhZone)z), UIKit.Cyan, new Color(0f, 0f, 0f, 0.5f));
                UIKit.Text(b, ((UhZone)z).ToString(), 13, Color.white, TextAnchor.MiddleCenter);
            }
            y += 28f;
            if (UIKit.Button(new Rect(x, y, bw, 30f), "Follow the game", _debugZone < 0, 14)) _debugZone = -1;
            for (int z = 0; z < UhAudioRules.ZoneCount; z++)
            {
                if (z == 0) continue;
                if (UIKit.Button(new Rect(x + z * (bw + 8f), y, bw, 30f), "Force " + (UhZone)z, _debugZone == z, 14)) _debugZone = z;
            }
            y += 38f;
            if (UIKit.Button(new Rect(x, y, bw, 30f), "Score: follow game", _debugMusic < 0, 14)) _debugMusic = -1;
            if (UIKit.Button(new Rect(x + (bw + 8f), y, bw, 30f), "Score: pulse", _debugMusic == 1, 14)) _debugMusic = 1;
            if (UIKit.Button(new Rect(x + 2f * (bw + 8f), y, bw, 30f), "Score: boss drive", _debugMusic == 2, 14)) _debugMusic = 2;
            if (UIKit.Button(new Rect(x + 3f * (bw + 8f), y, bw, 30f), "Stop all loops", false, 14))
            {
                StopAll();
                for (int i = 0; i < _debugLoopKeys.Length; i++) _debugLoopKeys[i] = 0;
            }
            y += 38f;

            UIKit.Text(new Rect(x, y, r.width, 22f), "Loops (toggle at the player):", 15, UIKit.Gold, TextAnchor.UpperLeft);
            y += 22f;
            Vector3 at = Game.PlayerPosition + Vector3.up;
            for (int i = 0; i < UhAudioBank.LoopCount; i++)
            {
                bool on = _debugLoopKeys[i] != 0 && LoopAlive(_debugLoopKeys[i]);
                if (UIKit.Button(new Rect(x + i * (bw + 8f), y, bw, 28f), ((UhLoop)i).ToString(), on, 13))
                {
                    if (on)
                    {
                        StopLoop(_debugLoopKeys[i]);
                        _debugLoopKeys[i] = 0;
                    }
                    else _debugLoopKeys[i] = StartLoop((UhLoop)i, at, 1f);
                }
            }
            y += 34f;

            UIKit.Text(new Rect(x, y, r.width, 22f), "Cues (play at the player; interface cues play in 2D):", 15, UIKit.Gold, TextAnchor.UpperLeft);
            y += 22f;
            int cols = 8;
            float cw = (r.width - (cols - 1) * 6f) / cols;
            for (int i = 0; i < UhAudioBank.SfxCount; i++)
            {
                int row = i / cols, col = i % cols;
                Rect b = new Rect(x + col * (cw + 6f), y + row * 30f, cw, 26f);
                if (!UIKit.Button(b, ((UhSfx)i).ToString(), false, 12)) continue;
                UhSfx id = (UhSfx)i;
                if (UhAudioRules.KindOf(id) == UhAudioCueKind.Ui) Play2D(id);
                else Play(id, at + (Game.Cam != null ? Game.Cam.transform.forward * 3f : Vector3.forward * 3f));
            }
            y += ((UhAudioBank.SfxCount + cols - 1) / cols) * 30f + 6f;
            UIKit.Text(new Rect(x, y, r.width, 40f), "A forced zone or score lasts until you pick Follow the game or load a save. Street ambience (city, wind, rain) fades only away from the surface.",
                14, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }
    }
}
