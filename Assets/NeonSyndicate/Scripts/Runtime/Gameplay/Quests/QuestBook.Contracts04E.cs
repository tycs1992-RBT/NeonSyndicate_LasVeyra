using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Lantern Refuge contract board (04E): a physical board at Under:refuge.map_board that opens once The First
    /// Descent is done and posts three repeatable jobs from the eight campaign grammars (four once the campaign is over:
    /// the endgame tier). A finished job is replaced; every slot is reposted after 20 minutes of active play. Jobs pay
    /// bounded cash, Lumen and materials, never a unique or a boss entitlement. Slots are rebuilt from saved serials.
    /// The stored matron_fate and regent_end decisions post outcome variants (CampaignContracts04E.Variants) and change
    /// Ilse's line at the top of the endgame board.
    /// </summary>
    public sealed partial class QuestBook
    {
        private const string BoardOpener = "uh05_04_descent";
        private const string EndgameQuest = "uh05_16_answers";

        private readonly ContractOffer[] _uhOffers = new ContractOffer[CampaignContracts04E.EndgameSlots];
        private readonly CampaignBoardContext _uhCtx = new CampaignBoardContext();
        private QuestProp _uhBoard;
        private Interactable _uhBoardUse;
        private Blip _uhBoardBlip;
        private Vector3 _uhBoardPos;
        private bool _uhBoardPlaced;
        private float _uhBoardRetry;
        private int _uhBlipCount = -1;

        /// <summary>True once the board exists in the world (the Underheart built its anchor).</summary>
        public bool CampaignBoardPlaced { get { return _uhBoardPlaced; } }
        /// <summary>Where the board stands (valid when CampaignBoardPlaced).</summary>
        public Vector3 CampaignBoardPosition { get { return _uhBoardPos; } }
        /// <summary>True when the board takes jobs (after The First Descent).</summary>
        public bool CampaignBoardOpen { get { return _completed.Contains(BoardOpener); } }

        /// <summary>The jobs posted now (null slots are empty), for the journal and the debug page.</summary>
        public IList<ContractOffer> CampaignBoardOffers
        {
            get
            {
                EnsureCampaignOffers();
                return _uhOffers;
            }
        }

        private string CampaignId
        {
            get { return Campaigns.Count > 0 ? Campaigns[0].Id : QuestIds04E.Campaign; }
        }

        private int CampaignSlots
        {
            get { return _completed.Contains(EndgameQuest) ? CampaignContracts04E.EndgameSlots : CampaignContracts04E.Slots; }
        }

        private CampaignBoardContext BoardContext()
        {
            _uhCtx.Completed.Clear();
            _uhCtx.Completed.UnionWith(_completed);
            _uhCtx.Endgame = _completed.Contains(EndgameQuest);
            // 04E: the stored decisions bend the board (CampaignContracts04E.Variants: matron_fate and regent_end).
            if (_uhCtx.Choice == null) _uhCtx.Choice = Choice;
            return _uhCtx;
        }

        private ContractDef TemplateById(string id)
        {
            foreach (ContractDef t in ContractTemplates) if (t.Id == id) return t;
            return null;
        }

        // ------------------------------------------------------------------ world

        private void UpdateCampaignBoard(float dt)
        {
            if (!_uhBoardPlaced)
            {
                _uhBoardRetry -= dt;
                if (_uhBoardRetry <= 0f)
                {
                    _uhBoardRetry = 2f;
                    TryPlaceCampaignBoard();
                }
            }
            bool busy = Game.Missions != null && Game.Missions.Active != null;
            bool open = CampaignBoardOpen;
            if (_uhBoard != null && _uhBoard.gameObject.activeSelf != open) _uhBoard.gameObject.SetActive(open);
            if (_uhBoardBlip != null)
            {
                _uhBoardBlip.Visible = open && !busy;
                if (open)
                {
                    int count = CountCampaignOffers();
                    if (count != _uhBlipCount)
                    {
                        _uhBlipCount = count;
                        _uhBoardBlip.Label = CampaignContracts04E.BoardName + " (" + count + " jobs)";
                    }
                }
            }
            if (!open || !Game.IsPlaying) return;
            // Replenishes over active play only (never real-world time): every slot is reposted after the interval.
            _campaign.BoardPlaySeconds += dt;
            if (_campaign.BoardPlaySeconds >= CampaignContracts04E.RefreshSeconds)
            {
                _campaign.BoardPlaySeconds = 0f;
                RepostCampaignBoard(false);
            }
        }

        private void TryPlaceCampaignBoard()
        {
            if (Game.Underheart == null || !Game.Underheart.Ready) return;
            Vector3 p;
            float yaw;
            if (!Game.Underheart.TryAnchor("refuge.map_board", out p, out yaw)) return;
            _uhBoardPos = p;
            Color c = CampaignBoardColor();
            GameObject root = new GameObject("Lantern Refuge Board");
            root.transform.SetParent(transform, false);
            Transform parent = root.transform;
            _uhBoard = QuestProps.Build("Board", p, yaw, parent, c);
            _uhBoardUse = Interactable.Create("Lantern Refuge board", p, 2.2f, "Press E to read the " + CampaignContracts04E.BoardName, ShowCampaignBoard, parent);
            _uhBoardUse.HeightTolerance = 2f;
            _uhBoardUse.Condition = () => CampaignBoardOpen && (Game.Missions == null || Game.Missions.Active == null);
            _uhBoardBlip = Blip.Add(BlipKind.Activity, null, p, c, "C", CampaignContracts04E.BoardName, false);
            _uhBoardPlaced = true;
            Log.Info("Campaign: the Lantern Refuge board stands at the refuge map board.");
        }

        private Color CampaignBoardColor()
        {
            CampaignDef def;
            float r, g, b;
            if (CampaignRegistry04E.TryGet(CampaignId, out def) && CampaignRules04E.ParseColor(def.Color, out r, out g, out b)) return new Color(r, g, b);
            return UIKit.Gold;
        }

        // ------------------------------------------------------------------ offers

        private int CountCampaignOffers()
        {
            EnsureCampaignOffers();
            int n = 0;
            for (int i = 0; i < CampaignSlots; i++) if (_uhOffers[i] != null) n++;
            return n;
        }

        /// <summary>Fills the board's slots from their serials (a new serial for an empty or unplaceable slot).</summary>
        private void EnsureCampaignOffers()
        {
            if (!CampaignBoardOpen) return;
            while (_campaign.BoardSerials.Count < CampaignContracts04E.EndgameSlots) _campaign.BoardSerials.Add(0);
            if (_campaign.BoardNextSerial < 1) _campaign.BoardNextSerial = 1;
            CampaignBoardContext ctx = null;
            string campaign = CampaignId;
            for (int k = 0; k < CampaignSlots; k++)
            {
                if (_uhOffers[k] != null && _uhOffers[k].Serial == _campaign.BoardSerials[k]) continue;
                // The context is rebuilt only when a slot actually needs a posting (not every frame).
                if (ctx == null) ctx = BoardContext();
                _uhOffers[k] = null;
                for (int attempt = 0; attempt < 4 && _uhOffers[k] == null; attempt++)
                {
                    if (_campaign.BoardSerials[k] == 0 || attempt > 0) _campaign.BoardSerials[k] = _campaign.BoardNextSerial++;
                    int serial = _campaign.BoardSerials[k];
                    ContractDef t = CampaignContracts04E.PickTemplate(ContractTemplates, campaign, ctx, CampaignContracts04E.SlotSeed(serial));
                    if (t == null) break;
                    try { _uhOffers[k] = CampaignContracts04E.Make(t, serial, ctx, _campaign.ContractsDone); }
                    catch (Exception e) { Log.Exception("Campaign contract " + t.Id, e); }
                }
            }
        }

        /// <summary>Reposts every slot (the running job keeps its slot unless all is true).</summary>
        private void RepostCampaignBoard(bool all)
        {
            for (int k = 0; k < _uhOffers.Length; k++)
            {
                if (!all && _run != null && _run.Contract && _uhOffers[k] != null && _uhOffers[k].Quest == _run.Quest) continue;
                if (k < _campaign.BoardSerials.Count) _campaign.BoardSerials[k] = 0;
                _uhOffers[k] = null;
            }
        }

        private void ResetCampaignBoard()
        {
            for (int k = 0; k < _uhOffers.Length; k++) _uhOffers[k] = null;
        }

        /// <summary>The pausing board screen: up to four jobs with their place, risk and pay, and a button to take one.</summary>
        public void ShowCampaignBoard()
        {
            if (!CampaignBoardOpen || !CanStartNow(true)) return;
            EnsureCampaignOffers();
            StringBuilder sb = new StringBuilder();
            List<string> labels = new List<string>();
            List<ContractOffer> picks = new List<ContractOffer>();
            bool endgame = _completed.Contains(EndgameQuest);
            sb.Append(CampaignContracts04E.BoardGreeting(endgame, Choice(ChoiceEcology04E.RegentKey))).Append("\n\n");
            for (int k = 0; k < CampaignSlots; k++)
            {
                ContractOffer o = _uhOffers[k];
                if (o == null) continue;
                picks.Add(o);
                labels.Add("TAKE " + picks.Count);
                sb.Append(picks.Count).Append(". ").Append(CampaignContracts04E.BoardLine(o, TemplateById(o.TemplateId), endgame)).Append('\n');
            }
            if (picks.Count == 0) sb.Append("Nothing posted right now. The board reposts after a while of play, or when a job is done.");
            labels.Add("LEAVE");
            if (Game.Menus == null) return;
            Game.Menus.ShowChoice(CampaignContracts04E.BoardName.ToUpperInvariant(), sb.ToString(), labels.ToArray(), i =>
            {
                if (i >= 0 && i < picks.Count) _pendingContract = picks[i];
            });
        }

        /// <summary>A finished campaign job leaves the board; a fresh one takes its slot.</summary>
        private void CampaignContractFinished(QuestDef q)
        {
            _campaign.ContractsDone++;
            for (int k = 0; k < _uhOffers.Length; k++)
            {
                if (_uhOffers[k] == null || _uhOffers[k].Quest != q) continue;
                _uhOffers[k] = null;
                if (k < _campaign.BoardSerials.Count) _campaign.BoardSerials[k] = _campaign.BoardNextSerial++;
            }
        }

        /// <summary>True for a running or finished job that came from the Lantern Refuge board.</summary>
        private bool IsCampaignContract(QuestDef q)
        {
            return q != null && CampaignRegistry04E.Has(q.Arc) && q.Id.StartsWith("contract_", StringComparison.Ordinal);
        }
    }
}
