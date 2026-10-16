using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The contract board at the resonance case: start "Resonance Breach" through the encounter interface (its
    /// availability reason and live status are shown as the encounters system reports them), and "Track the Foreman"
    /// (reveals its quarry arena and sets a GPS waypoint). One line explains the Djinn's night and the Queen's Veil.
    /// </summary>
    public sealed partial class CrystalOfferUi
    {
        private void DrawBoard(CrystalArsenal a, Rect r)
        {
            float x = r.x, y = r.y, w = r.width;
            UIKit.Text(new Rect(x, y, w, 30f), "CONTRACT BOARD", 26, new Color(1f, 0.6f, 0.2f), TextAnchor.UpperLeft, FontStyle.Bold);
            y += 40f;
            y = DrawBreach(a, x, y, w);
            y += 20f;
            y = DrawForeman(a, x, y, w);
            y += 20f;
            UIKit.Text(new Rect(x, y, w, 50f), "The Mirage Djinn appears only at night on the Mirage Flats. The Mirror Wraith Queen waits at Crystal Manor inside the Veil (hold the Third Eye to shift).",
                16, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Italic, true, true);
            y += 56f;
            UIKit.Text(new Rect(x, y, w, 22f), "SIGNATURES", 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 26f;
            string[] bosses = CrystalEntitlementRules.SignatureBosses;
            for (int i = 0; i < bosses.Length; i++)
            {
                string boss = bosses[i];
                CrystalEntitlementSave e = CrystalEntitlementRules.Find(a.EntitlementList, CrystalEntitlementRules.BossToken(boss));
                bool defeated = Game.Creatures != null && Game.Creatures.IsBossDefeated(boss);
                LootUniqueDef u = LootUniques.Get(CrystalFocusDb.SignatureFor(boss));
                CreatureSpecies sp = CreatureSpeciesDb.Get(boss);
                string state = e == null ? (defeated ? "defeated, claim on the next load" : "not yet defeated") : (CrystalEntitlementRules.IsGranted(e) ? "earned" : "earned, waiting for room");
                UIKit.Text(new Rect(x, y, w, 22f), (u != null ? u.Name : boss) + "   from " + (sp != null ? sp.Name : boss) + ": " + state, 16,
                    e != null ? ProgressionUI.Good : Color.white, TextAnchor.UpperLeft);
                y += 24f;
            }
        }

        private float DrawBreach(CrystalArsenal a, float x, float y, float w)
        {
            const string id = CrystalEncounters.ResonanceBreach;
            Rect box = new Rect(x, y, w, 150f);
            UIKit.Rect(box, new Color(1f, 1f, 1f, 0.05f));
            UIKit.Text(new Rect(x + 12f, y + 8f, w - 260f, 28f), "Resonance Breach", 22, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x + 12f, y + 38f, w - 260f, 44f), "A finite skirmish a short walk from the Lofts: crystal-touched foes with readable roles and one advertised reinforcement. First victory: Gyre Crescent.",
                15, new Color(0.85f, 0.86f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            CrystalEncounters enc = Game.Encounters;
            string status = "";
            string reason = "";
            bool available = false;
            if (enc == null) reason = "Contracts are not available.";
            else
            {
                try
                {
                    status = enc.ContractStatus(id);
                    available = enc.ContractAvailable(id, out reason);
                }
                catch (System.Exception e)
                {
                    Log.Exception("Contract board status", e);
                    reason = "The contract board could not read this contract.";
                }
            }
            CrystalEntitlementSave reward = CrystalEntitlementRules.Find(a.EntitlementList, CrystalEntitlementRules.ContractToken(id));
            string rewardLine = reward == null ? "Reward: Gyre Crescent (first victory)" : (CrystalEntitlementRules.IsGranted(reward) ? "Gyre Crescent earned" : "Gyre Crescent waiting for room");
            UIKit.Text(new Rect(x + 12f, y + 86f, w - 260f, 24f), (string.IsNullOrEmpty(status) ? "" : status + "   ") + rewardLine, 15, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            if (!available && !string.IsNullOrEmpty(reason))
                UIKit.Text(new Rect(x + 12f, y + 112f, w - 260f, 34f), reason, 15, new Color(1f, 0.75f, 0.4f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (ProgressionUI.Button(new Rect(box.xMax - 230f, y + 50f, 210f, 48f), "START", false, 20, available))
            {
                if (enc == null) Status("Contracts are not available.", false);
                else if (!available) Status(string.IsNullOrEmpty(reason) ? "Not available right now." : reason, false);
                else
                {
                    // The case screen pauses the game; the contract starts the moment the screen has closed and play resumed.
                    _pendingContract = id;
                    _closeRequested = true;
                }
            }
            return y + box.height;
        }

        private float DrawForeman(CrystalArsenal a, float x, float y, float w)
        {
            const string boss = "golem_foreman";
            Rect box = new Rect(x, y, w, 118f);
            UIKit.Rect(box, new Color(1f, 1f, 1f, 0.05f));
            CreatureSpecies sp = CreatureSpeciesDb.Get(boss);
            CreatureDirector cd = Game.Creatures;
            BossArena arena = cd != null ? cd.ArenaFor(boss) : null;
            UIKit.Text(new Rect(x + 12f, y + 8f, w - 260f, 28f), "Track the Foreman", 22, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            string where = arena != null ? arena.Name : "the quarry";
            UIKit.Text(new Rect(x + 12f, y + 38f, w - 260f, 44f), (sp != null ? sp.Name : "The Foreman") + " holds " + where + ". First victory: Sunderheart, its gravity-orb signature.",
                15, new Color(0.85f, 0.86f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            bool defeated = cd != null && cd.IsBossDefeated(boss);
            UIKit.Text(new Rect(x + 12f, y + 86f, w - 260f, 24f), defeated ? "Defeated. Its arena stays on your map." : "Not yet defeated.", 15, defeated ? ProgressionUI.Good : UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            if (ProgressionUI.Button(new Rect(box.xMax - 230f, y + 34f, 210f, 48f), "TRACK", false, 20, cd != null && arena != null))
            {
                if (cd == null || arena == null) Status("The Foreman's arena is not available.", false);
                else
                {
                    cd.RevealArena(boss);
                    if (Game.Gps != null) Game.Gps.SetWaypoint(arena.Center);
                    Status("GPS set to " + arena.Name + ". The fight starts when you walk into the arena.", true);
                }
            }
            return y + box.height;
        }
    }
}
