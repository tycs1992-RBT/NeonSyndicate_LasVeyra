#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LiveCastSetup, the named campaign cast on the study bodies (owner direction in chat 2026-10-23: swap NPCs and enemies
    /// for realistic bodies). Every cast member of CastIds04E except Dante (his own H9 body) and the companions receives a
    /// row bound to their exact cast id, the way Dante's is: the study body of their sex (the crowd prefab, bones, sockets
    /// and proof of the study), the identity of their in-game name, the head, torso and waist costume layers hidden (never
    /// the held tool), and the CastStudy flag that makes the live adapter dress them from their procedural costume colours
    /// with their own hair and headwear. A drop-in folder named after the person still wins (it comes first).
    /// </summary>
    public static partial class LiveCastSetup
    {
        /// <summary>Cast members who keep their own bodies: Dante (the H9 body) and Ion (a story companion who also rides along as a creature-mode companion).</summary>
        private static readonly string[] CastKeepsOwnBody = { CastIds04E.Dante, CastIds04E.Ion };

        /// <summary>The origin of a cast study row.</summary>
        private static string CastOrigin(string castId)
        {
            return "builtin:cast-" + castId;
        }

        /// <summary>
        /// Adds a study-body row for every eligible cast member whose identity no drop-in already serves. Needs the two
        /// study rows in rows (a study that failed to prepare leaves its cast members procedural, which the study's own
        /// warning already explains).
        /// </summary>
        private static void AddCastStudies(List<LiveCastRow> rows, List<string> labels, HashSet<string> identities, List<string> problems)
        {
            LiveCastRow men = FindOriginIn(rows, CourtDefenderStudy.Origin), women = FindOriginIn(rows, GuildArtisanStudy.Origin);
            foreach (string castId in CastIds04E.All)
            {
                if (Array.IndexOf(CastKeepsOwnBody, castId) >= 0) continue;
                string identity = LiveCastRules.NpcPrefix + CastIds04E.Name(castId);
                if (identities.Contains(identity)) continue;
                Appearance look = CastRoster04E.Look(castId);
                LiveCastRow study = look.Female ? women : men;
                if (study == null) continue;
                try
                {
                    LiveCastRow row = CastStudyRow(study, castId, identity, look.Female);
                    string reason;
                    if (!LiveCastRules.CanGoLive(row, out reason)) throw new InvalidOperationException(reason);
                    identities.Add(identity);
                    rows.Add(row);
                    labels.Add(row.Label);
                }
                catch (Exception e) { problems.Add(CastIds04E.Name(castId) + " on the study body: " + e.Message); }
            }
        }

        private static LiveCastRow FindOriginIn(List<LiveCastRow> rows, string origin)
        {
            foreach (LiveCastRow row in rows)
                if (row != null && row.Origin == origin && row.Model != null && row.Model.Prefab != null) return row;
            return null;
        }

        /// <summary>The cast row: the study's visual row under the person's identity, bound to the cast id, costume layers hidden, the tool kept.</summary>
        private static LiveCastRow CastStudyRow(LiveCastRow study, string castId, string identity, bool female)
        {
            ImportedModel04OD source = study.Model;
            ImportedModel04OD model = new ImportedModel04OD
            {
                Identity = identity, Enabled = true, Prefab = source.Prefab, Scale = source.Scale, AlignBindAxesH3 = source.AlignBindAxesH3,
                PreserveInfluencesH5 = source.PreserveInfluencesH5, Bones = source.Bones, PassiveBonePaths = source.PassiveBonePaths,
                Sockets = source.Sockets, ArtProof = source.ArtProof, AnimationAuthority = source.AnimationAuthority
            };
            string[] hidden = new string[LiveCastRules.DropInHiddenCostume.Count];
            LiveCastRules.DropInHiddenCostume.CopyTo(hidden, 0);
            return new LiveCastRow
            {
                Model = model, CastId = castId, HiddenCostume = hidden, Label = CastIds04E.Name(castId) + " (" + (female ? "Guild Artisan" : "Court Defender") + " study)",
                Origin = CastOrigin(castId), Stamp = study.Stamp, CrowdRoles = 0, CrowdFemale = female, CrowdArmourPaths = study.CrowdArmourPaths,
                CrowdHair = true, CastStudy = true
            };
        }
    }
}
#endif
