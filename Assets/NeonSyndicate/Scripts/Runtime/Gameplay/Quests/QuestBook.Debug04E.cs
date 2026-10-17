using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 debug tab "Campaign" (04E): every campaign quest with its state, committed checkpoint and the reason it is
    /// locked; start (teleports to where it would start), complete, reset and clear-checkpoint buttons; the stored choices
    /// and flags; the Lantern Refuge board; and an in-game run of the campaign data check. For testing only.
    /// </summary>
    public sealed partial class QuestBook
    {
        private const int CampaignDebugRows = 8;
        private int _campaignDebugPage;
        private string _campaignDebugStatus = "";
        private QuestDef _campaignDebugStart;
        private int _campaignDebugStartFrame;

        private void RegisterCampaignDebug()
        {
            DebugPages.Register(new MenuPage { Title = "Campaign", Order = 63, Draw = DrawCampaignDebug });
        }

        private List<QuestDef> CampaignQuests()
        {
            List<QuestDef> list = new List<QuestDef>();
            foreach (QuestDef q in Quests) if (IsCampaign(q)) list.Add(q);
            list.Sort((a, b) => a.Order.CompareTo(b.Order));
            return list;
        }

        private void DrawCampaignDebug(Rect r)
        {
            float x = r.x, y = r.y;
            if (UIKit.Button(new Rect(x, y, 250f, 42f), "Clear choices and flags", false, 17))
            {
                _campaign.Choices.Clear();
                _campaign.Flags.Clear();
                _campaignDebugStatus = "Every campaign choice and flag is cleared.";
            }
            if (UIKit.Button(new Rect(x + 260f, y, 250f, 42f), "Clear all checkpoints", false, 17))
            {
                _campaign.Checkpoints.Clear();
                _campaignDebugStatus = "Every campaign checkpoint is cleared.";
            }
            if (UIKit.Button(new Rect(x + 520f, y, 250f, 42f), "Repost Refuge board", false, 17))
            {
                RepostCampaignBoard(true);
                _campaignDebugStatus = "The Lantern Refuge board reposted every slot.";
            }
            if (UIKit.Button(new Rect(x + 780f, y, 250f, 42f), "Check campaign data", false, 17)) _campaignDebugStatus = CheckCampaignDataInGame();
            y += 50f;
            StringBuilder head = new StringBuilder();
            head.Append("Choices: ").Append(_campaign.Choices.Count == 0 ? "none" : string.Join(", ", _campaign.Choices.ToArray()));
            head.Append("   Flags: ").Append(_campaign.Flags.Count == 0 ? "none" : string.Join(", ", _campaign.Flags.ToArray()));
            UIKit.Text(new Rect(x, y, r.width, 24f), head.ToString(), 16, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            y += 26f;
            string board = !CampaignBoardOpen ? "closed (opens after The First Descent)" : (_uhBoardPlaced ? CountCampaignOffers() + " jobs posted" : "open, waiting for the Underheart anchor");
            UIKit.Text(new Rect(x, y, r.width, 24f), "Lantern Refuge board: " + board + "   Contracts done: " + _campaign.ContractsDone + "   Pending ledger grants: " + _campaign.PendingGrants.Count, 16, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            y += 26f;
            UIKit.Text(new Rect(x, y, r.width, 24f), _campaignDebugStatus, 16, UIKit.Gold, TextAnchor.UpperLeft);
            y += 30f;
            List<QuestDef> list = CampaignQuests();
            int pages = Mathf.Max(1, (list.Count + CampaignDebugRows - 1) / CampaignDebugRows);
            _campaignDebugPage = Mathf.Clamp(_campaignDebugPage, 0, pages - 1);
            QuestProgress p = Progress();
            for (int i = _campaignDebugPage * CampaignDebugRows; i < Mathf.Min(list.Count, (_campaignDebugPage + 1) * CampaignDebugRows); i++)
            {
                QuestDef q = list[i];
                bool done = _completed.Contains(q.Id);
                string cp = CheckpointOf(q.Id);
                string why = done ? null : WhyLocked(q, p);
                string state = done ? "<color=#60ff80>done</color>" : (q.Id == _active ? "<color=#ffd24d>accepted</color>" : (why == null ? "<color=#ffd24d>available</color>" : "<color=#999999>" + why + "</color>"));
                string line = (q.Kind == "Main" ? "M" : "S") + q.Order + "  " + q.Title + "   " + state + (cp.Length > 0 ? "   <color=#73f2ff>checkpoint " + cp + "</color>" : "");
                UIKit.Text(new Rect(x, y + 10f, 820f, 30f), line, 16, Color.white, TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(x + 830f, y, 120f, 40f), "Start", false, 16))
                {
                    if (Game.Menus != null) Game.Menus.Close();
                    if (Game.Missions != null && Game.Missions.Active != null) Game.Missions.Fail("Restarted from the debug menu.");
                    Vector3 start = StartPosition(q);
                    if (Game.Root != null) Game.Root.TeleportPlayer(start);
                    _campaignDebugStart = q;
                    _campaignDebugStartFrame = Time.frameCount + 2;
                    return;
                }
                if (UIKit.Button(new Rect(x + 960f, y, 120f, 40f), "Complete", false, 16) && !done)
                {
                    if (_run != null && _run.Quest == q && Game.Missions != null && Game.Missions.Active == _run.C) Game.Missions.Pass();
                    else
                    {
                        QuestWorld.Grant(q.Reward, q.Title, true);
                        _completed.Add(q.Id);
                        if (_active == q.Id) _active = "";
                        CampaignRules04E.ClearCheckpoint(_campaign.Checkpoints, q.Id);
                        RaiseCompleted(q.Id);
                    }
                    _campaignDebugStatus = "Completed " + q.Title + " (Claim rewards are not granted by this button).";
                }
                if (UIKit.Button(new Rect(x + 1090f, y, 120f, 40f), "Reset", false, 16))
                {
                    _completed.Remove(q.Id);
                    _announced.Remove(q.Id);
                    if (_active == q.Id) _active = "";
                    CampaignRules04E.ClearCheckpoint(_campaign.Checkpoints, q.Id);
                    _campaignDebugStatus = "Reset " + q.Title + ".";
                }
                if (cp.Length > 0 && UIKit.Button(new Rect(x + 1220f, y, 150f, 40f), "Clear checkpoint", false, 15))
                {
                    CampaignRules04E.ClearCheckpoint(_campaign.Checkpoints, q.Id);
                    _campaignDebugStatus = "Checkpoint cleared for " + q.Title + ".";
                }
                y += 46f;
            }
            if (pages > 1)
            {
                if (UIKit.Button(new Rect(x, y + 6f, 200f, 40f), "Prev page", false, 16)) _campaignDebugPage = (_campaignDebugPage + pages - 1) % pages;
                if (UIKit.Button(new Rect(x + 210f, y + 6f, 200f, 40f), "Next page", false, 16)) _campaignDebugPage = (_campaignDebugPage + 1) % pages;
                UIKit.Text(new Rect(x + 430f, y + 14f, 300f, 30f), "Page " + (_campaignDebugPage + 1) + " of " + pages, 16, Color.white, TextAnchor.UpperLeft);
            }
        }

        /// <summary>Debug starts wait until the menu closed and the teleport landed (two frames).</summary>
        private void UpdateCampaignDebugStart()
        {
            if (_campaignDebugStart == null || Time.frameCount < _campaignDebugStartFrame || !Game.IsPlaying) return;
            QuestDef q = _campaignDebugStart;
            _campaignDebugStart = null;
            if (_completed.Contains(q.Id)) _completed.Remove(q.Id);
            StartQuest(q, true);
        }

        /// <summary>The full validator over the shipped files, run inside the game (the same check as the EditMode tests).</summary>
        private string CheckCampaignDataInGame()
        {
            try
            {
                List<string> errors = new List<string>();
                QuestJsonTrace trace = new QuestJsonTrace();
                List<KeyValuePair<string, QuestFile>> qf = new List<KeyValuePair<string, QuestFile>>();
                foreach (TextAsset ta in Resources.LoadAll<TextAsset>("Quests")) qf.Add(new KeyValuePair<string, QuestFile>(ta.name + ".json", QuestJson.Read<QuestFile>(ta.text, ta.name + ".json", errors, trace)));
                QuestReport rep = new QuestReport();
                QuestValidator.ValidateQuests(qf, QuestRefs.Build(Game.Layout), rep, trace);
                rep.Errors.InsertRange(0, errors);
                string details = rep.Details();
                if (details.Length > 0) Log.Warn("Campaign data check:\n" + details);
                return rep.Summary() + (rep.Ok ? "" : " (details in the Console)");
            }
            catch (Exception e)
            {
                Log.Exception("Campaign data check", e);
                return "The check failed: " + e.Message;
            }
        }
    }
}
