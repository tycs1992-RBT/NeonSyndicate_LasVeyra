using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Reward presentation: a non-pausing HUD card after a fight clears (the boss gone, its summons gone, a short beat
    /// after the banner) with a key prompt that opens the offer and equip screen, a matching focus trophy hovering where
    /// a boss fell (a celebration only, never the record of ownership), and the practice lane panel.
    /// </summary>
    public sealed partial class CrystalArsenal
    {
        private const float CardSeconds = 25f;
        private const float TrophySeconds = 240f;

        private sealed class RewardCard
        {
            public string Token;
            public string BossId;
            public Vector3 At;
            public bool Boss;
            public float EarnedAt;
            public float ShownAt = -1f;
            public bool Done;
        }

        private readonly List<RewardCard> _cards = new List<RewardCard>();
        private GameObject _trophy;
        private Interactable _trophyUse;
        private string _trophyToken;
        private float _trophyUntil;

        private void QueueRewardCard(string token, string bossId, Vector3 at, bool boss)
        {
            _cards.Add(new RewardCard { Token = token, BossId = bossId, At = at, Boss = boss, EarnedAt = Time.time });
        }

        /// <summary>The fight is over enough for a card: no boss alive, its summons gone, and a beat after the kill.</summary>
        private static bool FightCleared(RewardCard c)
        {
            if (Time.time - c.EarnedAt < 2.5f) return false;
            CreatureDirector cd = Game.Creatures;
            if (cd != null && cd.ActiveBoss != null) return false;
            if (cd != null && !string.IsNullOrEmpty(c.BossId) && cd.AliveWithTag(CreatureDirector.BossAddsTag(c.BossId)) > 0) return false;
            return true;
        }

        private RewardCard CurrentCard
        {
            get
            {
                for (int i = 0; i < _cards.Count; i++) if (!_cards[i].Done && _cards[i].ShownAt >= 0f) return _cards[i];
                return null;
            }
        }

        private void UpdateRewards()
        {
            if (!Game.IsPlaying) return;
            RewardCard cur = CurrentCard;
            if (cur != null && Time.time - cur.ShownAt > CardSeconds) { cur.Done = true; cur = null; }
            if (cur == null)
            {
                for (int i = 0; i < _cards.Count; i++)
                {
                    RewardCard c = _cards[i];
                    if (c.Done || c.ShownAt >= 0f || !FightCleared(c)) continue;
                    c.ShownAt = Time.time;
                    cur = c;
                    if (c.Boss) SpawnTrophy(c);
                    CrystalSounds.PlayReward(Game.PlayerPosition);
                    break;
                }
            }
            _cards.RemoveAll(c => c.Done);
            // The card's key: E or pad Y, only when nothing else would take that press (no service, drop or vehicle nearby).
            PlayerController p = Game.Player;
            if (cur != null && p != null && !p.IsDead && !p.InVehicle && p.Focus == null && p.EnterCandidate == null && (Act.InteractKey || Act.PadUse))
            {
                cur.Done = true;
                if (_ui != null) _ui.Open(CrystalOfferUi.Mode.Claim, cur.Token);
            }
            if (_trophy != null && (Time.time > _trophyUntil || p == null || MathUtil.FlatSqrDistance(p.transform.position, _trophy.transform.position) > 200f * 200f)) ClearTrophy();
            if (_trophy != null) _trophy.transform.GetChild(0).localRotation = Quaternion.Euler(0f, Time.time * 40f, 0f);
        }

        private void SpawnTrophy(RewardCard c)
        {
            ClearTrophy();
            LootUniqueDef u = null;
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(_entitlements, c.Token);
            if (e != null) u = LootUniques.Get(e.Unique);
            CrystalBehavior b = e != null ? CrystalFocusDb.BehaviorOfUnique(e.Unique) : CrystalBehavior.None;
            if (b == CrystalBehavior.None) return;
            Vector3 ground = GameRoot.FindGround(c.At);
            _trophy = new GameObject("Signature Trophy");
            _trophy.transform.SetParent(transform, false);
            _trophy.transform.position = ground;
            GameObject spin = new GameObject("Spin");
            spin.transform.SetParent(_trophy.transform, false);
            spin.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            try
            {
                GameObject model = CrystalFocusModels.CreatePickup(b, spin.transform);
                if (model != null) model.transform.localPosition = Vector3.zero;
            }
            catch (System.Exception ex) { Log.Exception("Signature trophy model", ex); }
            WorldMarker.Create(ground, new Color(1f, 0.82f, 0.3f), 0.8f, _trophy.transform, true);
            _trophyToken = c.Token;
            _trophyUntil = Time.time + TrophySeconds;
            _trophyUse = _trophy.AddComponent<Interactable>();
            _trophyUse.Radius = 2.6f;
            _trophyUse.HeightTolerance = 3f;
            string name = u != null ? u.Name : "the signature";
            _trophyUse.DynamicPrompt = () => "Press E to take up " + name;
            _trophyUse.Condition = () => Game.IsPlaying && Game.Player != null && !Game.Player.InVehicle;
            _trophyUse.Use = () =>
            {
                string token = _trophyToken;
                ClearTrophy();
                if (_ui != null) _ui.Open(CrystalOfferUi.Mode.Claim, token);
            };
        }

        private void ClearTrophy()
        {
            if (_trophyUse != null) _trophyUse.enabled = false;
            if (_trophy != null) Destroy(_trophy);
            _trophy = null;
            _trophyUse = null;
            _trophyToken = null;
        }

        /// <summary>Session-only presentation (cards, trophy, lane counters). The case and lane geometry stay for the boot.</summary>
        private void ClearSessionObjects()
        {
            _cards.Clear();
            ClearTrophy();
            _laneHits = 0;
            _inLane = false;
            for (int i = 0; i < _targets.Length; i++) if (_targets[i] != null) _targets[i].ResetSession();
        }

        // ------------------------------------------------------------------ drawing

        private void DrawHud(float w, float h)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || !Game.IsPlaying) return;
            RewardCard c = CurrentCard;
            if (c != null) DrawRewardCard(w, h, c, p);
            if (_inLane) DrawLanePanel(w, h, p);
        }

        private void DrawRewardCard(float w, float h, RewardCard c, PlayerController p)
        {
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(_entitlements, c.Token);
            if (e == null) return;
            LootUniqueDef u = LootUniques.Get(e.Unique);
            CrystalProfile prof = CrystalFocusDb.Get(CrystalFocusDb.BehaviorOfUnique(e.Unique));
            Rect r = new Rect(40f, h * 0.30f, 520f, 200f);
            Color rc = GearSystem.RarityColor(u != null ? u.Rarity : Rarity.Legendary);
            UIKit.Rect(r, new Color(0.03f, 0.04f, 0.06f, 0.9f));
            UIKit.Rect(new Rect(r.x, r.y, r.width, 4f), rc);
            UIKit.Frame(r, new Color(rc.r, rc.g, rc.b, 0.7f), 2f);
            float x = r.x + 16f, y = r.y + 10f, tw = r.width - 32f;
            UIKit.Text(new Rect(x, y, tw, 24f), c.Boss ? "SIGNATURE EARNED" : "REWARD EARNED", 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 24f;
            UIKit.Text(new Rect(x, y, tw, 32f), u != null ? u.Name : "Crystal focus", 26, rc, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            if (prof != null)
            {
                UIKit.Text(new Rect(x, y, tw, 44f), prof.FireLine, 15, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 46f;
            }
            bool granted = CrystalEntitlementRules.IsGranted(e);
            string where = "Reward waiting: free a slot or collect at the case or the Forge.";
            if (granted)
            {
                LootGun owned = Game.Loot != null ? Game.Loot.Find(e.Uid) : null;
                int slot = owned != null ? Game.Loot.SlotOf(owned) : -1;
                where = owned == null ? "Granted." : (slot >= 0 ? "Equipped in loot slot " + (slot + 1) + "." : "It is in your backpack.");
            }
            UIKit.Text(new Rect(x, y, tw, 22f), where, 15,
                granted ? ProgressionUI.Good : new Color(1f, 0.75f, 0.35f), TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            y += 24f;
            bool keyFree = p.Focus == null && p.EnterCandidate == null && !p.InVehicle;
            string prompt = keyFree ? GInput.Prompt("Press E to equip or compare it now") : "Open ARSENAL from the pause menu to equip or compare it";
            UIKit.Text(new Rect(x, y, tw, 22f), prompt, 15, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
        }

        private void DrawLanePanel(float w, float h, PlayerController p)
        {
            CrystalBehavior b = WeaponController.BehaviorOf(p.Weapons.Current);
            Rect r = new Rect(w * 0.5f - 300f, h - 250f, 600f, 104f);
            UIKit.Rect(r, new Color(0.02f, 0.05f, 0.08f, 0.8f));
            UIKit.Frame(r, new Color(0.35f, 0.85f, 1f, 0.6f), 2f);
            float x = r.x + 14f, y = r.y + 8f, tw = r.width - 28f;
            int calibrated = CrystalPracticeRules.Count(_calibrationHits);
            string head = _calibrationDone ? "PRACTICE LANE   calibration complete" : "PRACTICE LANE   calibration " + calibrated + " / " + CrystalPracticeRules.TargetCount + " targets";
            UIKit.Text(new Rect(x, y, tw, 22f), head + "   hits " + _laneHits, 16, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 24f;
            string tip;
            CrystalProfile prof = CrystalFocusDb.Get(b);
            if (prof == null) tip = "Equip a crystal focus (ARSENAL) and hit each target. No rewards; shots here are not reported as crimes.";
            else if (b == CrystalBehavior.PrismThread) tip = "Hold fire for the beam; aim to narrow it and keep contact to ramp it. " + prof.HeatLine;
            else if (prof.Speed > 0f) tip = "Shots travel at " + Mathf.RoundToInt(prof.Speed) + " m/s: lead moving targets. " + prof.HeatLine;
            else tip = prof.FireLine;
            UIKit.Text(new Rect(x, y, tw, 46f), tip, 14, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 46f;
            UIKit.Text(new Rect(x, y, tw, 20f), "Tip: mark a foe with a power, then hit it with a gun for a Resonance Burst (practice targets are not foes).", 13,
                new Color(0.75f, 0.8f, 0.85f), TextAnchor.UpperLeft, FontStyle.Italic, true, true);
        }
    }
}
