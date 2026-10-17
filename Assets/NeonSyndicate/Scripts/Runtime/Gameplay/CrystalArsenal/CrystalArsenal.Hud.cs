using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Reward presentation: a non-pausing HUD card after a fight clears (the boss gone, its summons gone, a short beat
    /// after the banner) with a key prompt that opens the offer and equip screen, a matching focus trophy hovering where
    /// a boss fell (a celebration only, never the record of ownership), and the practice lane panel. 04E: the card sits
    /// in the shared top-centre encounter slot (HudLayout04E.RewardCard04D) and waits while a 04E encounter holds it.
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

        /// <summary>
        /// The fight is over enough for a card: no boss alive, its summons gone, a beat after the kill, and the shared
        /// encounter slot free (the card lives there now, HudLayout04E.RewardCard04D).
        /// </summary>
        private static bool FightCleared(RewardCard c)
        {
            if (Time.time - c.EarnedAt < 2.5f) return false;
            CreatureDirector cd = Game.Creatures;
            if (cd != null && cd.ActiveBoss != null) return false;
            if (cd != null && !string.IsNullOrEmpty(c.BossId) && cd.AliveWithTag(CreatureDirector.BossAddsTag(c.BossId)) > 0) return false;
            return !EncounterSlotBusy;
        }

        /// <summary>04E: an Underheart boss fight, a Choir mechanism or an ecology skirmish holds the top-centre encounter slot.</summary>
        private static bool EncounterSlotBusy
        {
            get
            {
                UnderheartBosses b = Game.UnderBosses;
                if (b != null && (b.FightActive || (b.ActiveMechanism != null && b.ActiveMechanism.Running))) return true;
                UnderheartEcology e = Game.Ecology04E;
                return e != null && e.AnySkirmishActive;
            }
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
            bool slotBusy = EncounterSlotBusy;
            // 04E: a card hidden behind a 04E encounter panel keeps its time (and its key) until the slot is free again.
            if (cur != null && slotBusy) cur.ShownAt += Time.deltaTime;
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
            if (cur != null && !slotBusy && p != null && !p.IsDead && !p.InVehicle && p.Focus == null && p.EnterCandidate == null && (Act.InteractKey || Act.PadUse))
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
            // The card shares the top-centre encounter slot: it waits (out of sight) while a 04E encounter holds it.
            if (c != null && !EncounterSlotBusy) DrawRewardCard(w, h, c, p);
            if (_inLane) DrawLanePanel(w, h, p);
        }

        private string _cardFitToken;
        private float _cardFitWidth = -1f;
        private bool _cardShowFire = true;

        /// <summary>
        /// The reward card in the shared encounter slot under the banners (HudLayout04E.RewardCard04D): the title and the
        /// focus name on the left, its firing line, where it went and the key prompt on the right. It used to sit in the
        /// left column over the rewards chip, the ward panel and the companion messages. On a narrow card (a large HUD
        /// scale) the firing line is left out when it would need more than two rows; ARSENAL shows it in full.
        /// </summary>
        private void DrawRewardCard(float w, float h, RewardCard c, PlayerController p)
        {
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(_entitlements, c.Token);
            if (e == null) return;
            LootUniqueDef u = LootUniques.Get(e.Unique);
            CrystalProfile prof = CrystalFocusDb.Get(CrystalFocusDb.BehaviorOfUnique(e.Unique));
            Rect r = HudLayout04E.RewardCard04D(w, h);
            Color rc = GearSystem.RarityColor(u != null ? u.Rarity : Rarity.Legendary);
            UIKit.Rect(r, new Color(0.03f, 0.04f, 0.06f, 0.9f));
            UIKit.Rect(new Rect(r.x, r.y, r.width, 4f), rc);
            UIKit.Frame(r, new Color(rc.r, rc.g, rc.b, 0.7f), 2f);
            float leftW = Mathf.Min(300f, r.width * 0.38f);
            float lx = r.x + 14f, lw = leftW - 14f;
            UIKit.Text(new Rect(lx, r.y + 10f, lw, 22f), c.Boss ? "SIGNATURE EARNED" : "REWARD EARNED", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(lx, r.y + 32f, lw, 76f), u != null ? u.Name : "Crystal focus", 24, rc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            float x = r.x + leftW + 10f, tw = r.xMax - 14f - x, y = r.y + 10f;
            if (prof != null && (_cardFitToken != c.Token || _cardFitWidth != tw))
            {
                _cardFitToken = c.Token;
                _cardFitWidth = tw;
                _cardShowFire = tw >= 420f && UIKit.Measure(prof.FireLine, 14).x <= tw * 2f - 20f;
            }
            if (prof != null && _cardShowFire)
            {
                UIKit.Text(new Rect(x, y, tw, 42f), prof.FireLine, 14, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 44f;
            }
            bool granted = CrystalEntitlementRules.IsGranted(e);
            string where = "Reward waiting: free a slot or collect at the case or the Forge.";
            if (granted)
            {
                LootGun owned = Game.Loot != null ? Game.Loot.Find(e.Uid) : null;
                int slot = owned != null ? Game.Loot.SlotOf(owned) : -1;
                where = owned == null ? "Granted." : (slot >= 0 ? "Equipped in loot slot " + (slot + 1) + "." : "It is in your backpack.");
            }
            // A narrow card lets these two lines wrap onto a second row (the firing line is left out there).
            float rowH = tw < 420f ? 40f : 22f;
            UIKit.Text(new Rect(x, y, tw, rowH), where, 14,
                granted ? ProgressionUI.Good : new Color(1f, 0.75f, 0.35f), TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            y += rowH + 2f;
            bool keyFree = p.Focus == null && p.EnterCandidate == null && !p.InVehicle;
            string prompt = keyFree ? GInput.Prompt("Press E to equip or compare it now") : "Open ARSENAL from the pause menu to equip or compare it";
            UIKit.Text(new Rect(x, y, tw, rowH), prompt, 14, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
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
