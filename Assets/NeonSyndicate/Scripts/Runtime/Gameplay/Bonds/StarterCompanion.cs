using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Owner direction 2026-10-24: the mechanics must show in ordinary play. Before this, the first companion came only
    /// after the prologue, two side quests and the Underheart rescue, so most sessions never saw one. Now the Quartz
    /// Drakeling (Glint) bonds with the player and follows at once in Free Roam, and right after the prologue in the story.
    /// It is granted once (an owned drakeling is left alone, and the later rescue quest already says "already bonded").
    /// </summary>
    public sealed class StarterCompanion : MonoBehaviour
    {
        private float _check = 3f;

        /// <summary>True when the player may receive the starter companion now (pure, tested).</summary>
        public static bool Due(bool playing, bool freeRoam, int storyProgress, bool missionRunning, bool owned, bool playerAlive)
        {
            if (!playing || missionRunning || owned || !playerAlive) return false;
            return freeRoam || storyProgress >= 1;
        }

        private void Update()
        {
            _check -= Time.unscaledDeltaTime;
            if (_check > 0f) return;
            _check = 2f;
            BondSystem bonds = Game.Bonds;
            PlayerController p = Game.Player;
            if (bonds == null || p == null || Game.Root == null) return;
            bool mission = Game.Missions != null && Game.Missions.Active != null;
            int story = Game.Missions != null ? Game.Missions.StoryProgress : 0;
            if (!Due(Game.State == GameState.Playing, Game.Root.FreeRoam, story, mission, bonds.Owns(BondIds.QuartzGuide), !p.IsDead)) return;
            CompanionRecord r = bonds.Grant(BondIds.QuartzGuide, BondIds.DefaultName(BondIds.QuartzGuide), "starter");
            if (r == null) return;
            string message;
            bonds.Summon(r.Uid, out message);
            if (Game.Hud != null) Game.Hud.Banner("A NEW BOND", r.Name + " the quartz drakeling follows you now. It reveals routes and marks targets for a Resonance Burst.", UIKit.Cyan, 6f);
            Log.Info("Starter companion: " + r.Name + " bonded and summoned (" + message + ").");
        }
    }
}
