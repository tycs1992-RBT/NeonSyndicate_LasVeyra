using System;
using System.IO;

namespace NeonSyndicate
{
    /// <summary>
    /// One unified game (owner decision, 2026-10-23): the boot, New Game, Start After the Prologue, Continue, Load Game and
    /// Free Roam all play the newest world, Gothic Civic H5 (revision 10, see OneGameH8S). The scene reload that changes
    /// worlds stays the existing CityRebirthBoot04O handoff; nothing here writes a save.
    /// </summary>
    public sealed partial class GameRoot
    {
        /// <summary>This boot had no world request (no save, no menu), so it chose the unified world itself.</summary>
        private bool _defaultBootH8S;

        /// <summary>
        /// The unified world could not be planned at the default boot (its profile refused before touching the city), so
        /// the original city runs this session and the title buttons play it instead of reloading in a loop.
        /// </summary>
        private bool _oneGameFallbackH8S;

        /// <summary>True when a title button must reload the scene to reach the unified world.</summary>
        public bool NeedsOneGameReloadH8S
        {
            get { return !_oneGameFallbackH8S && Game.Layout != null && OneGameH8S.NeedsReload(Game.Layout.Revision04O); }
        }

        /// <summary>The bank Continue and the title's Load list read: the unified world's (10), or the running city's after a fallback.</summary>
        public int ContinueBankH8S
        {
            get { return _oneGameFallbackH8S ? SaveSystem.ActiveBankRevision04OB : OneGameH8S.Bank; }
        }

        /// <summary>
        /// Default boot only: when the unified world's profile refused (returned false, so the city was not changed), keep
        /// the original city for this session instead of stopping on an error screen. Explicit requests keep the old rule.
        /// </summary>
        private bool UseOriginalCityH8S(CityLayout layout)
        {
            if (!_defaultBootH8S || layout == null || layout.Revision04O != CityRevision04O.Legacy04N) return false;
            _revision04O = CityRevision04O.Legacy04N;
            _oneGameFallbackH8S = true;
            Log.Warn("One game: the Gothic Civic world could not be planned for this city, so the original city runs this session. Saves are unchanged.");
            ModuleStatus.Report("one_game_h8s", "Current world", ModuleState.Partial, "The Gothic Civic world could not be planned; the original city runs this session.", "Restart the game. If it repeats, send the Console log.");
            return true;
        }

        /// <summary>The most recently written slot of one bank (a verified backup counts), or -1. Reads file times only.</summary>
        private static int MostRecentSlotH8S(int bank)
        {
            int best = -1;
            DateTime bestTime = DateTime.MinValue;
            for (int i = 0; i <= SaveSystem.Slots; i++)
            {
                if (!SaveSystem.Exists(i, bank)) continue;
                string main = SaveSystem.PathFor(i, bank);
                string path = File.Exists(main) ? main : SaveFileTransaction.BackupPath(main);
                DateTime t = File.GetLastWriteTimeUtc(path);
                if (t > bestTime)
                {
                    bestTime = t;
                    best = i;
                }
            }
            return best;
        }
    }
}
