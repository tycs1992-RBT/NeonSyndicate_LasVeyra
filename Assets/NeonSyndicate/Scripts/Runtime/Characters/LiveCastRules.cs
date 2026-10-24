using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>What the live visual does on one frame (LiveCastRules.Present).</summary>
    public struct LiveCastPresentation
    {
        /// <summary>The imported visual's renderers are enabled.</summary>
        public bool ShowVisual;
        /// <summary>The shadow mode the visual's renderers use (copied from the procedural body).</summary>
        public ShadowCastingMode Shadows;
        /// <summary>The procedural body and its duplicated costume layers are forced off (they never draw twice).</summary>
        public bool HideBody;
    }

    /// <summary>What the skin weight policy asks for (LiveCastRules.SkinWeightStep).</summary>
    public enum LiveCastSkinStep
    {
        /// <summary>Nothing to do: the row keeps at most four weights or Unlimited is already active.</summary>
        Keep = 0,
        /// <summary>Select Unlimited skin weights for this play session (and restore the prior value when it ends).</summary>
        Raise = 1,
        /// <summary>Unlimited could not be selected: show the procedural body instead of a degraded skin.</summary>
        FailClosed = 2
    }

    /// <summary>
    /// Engine-free rules of the live character adapter (LiveCastVisual): which registry row an actor receives, which rows
    /// may go live at all (owner approval, rights, the H9 humanoid skeleton, sockets), what shows on a frame (mirroring the
    /// procedural body's hidden and shadow-only states and yielding to an Editor review), which procedural costume layers
    /// a live visual hides, whether a material's shader can draw under the active render pipeline and when complete skin
    /// weights need Unlimited. Pure string and number checks, so EditMode tests run them offline.
    /// </summary>
    public static class LiveCastRules
    {
        /// <summary>The identity of the player (the original adapter's "malik").</summary>
        public const string PlayerIdentity = "malik";
        /// <summary>The prefix of a named human NPC identity ("npc:" plus the display name).</summary>
        public const string NpcPrefix = "npc:";
        /// <summary>The recorded owner decision that lets the delivered H9 Malik and H9 Dante go live.</summary>
        public const string OwnerApprovalRecord = "owner approved in chat 2026-10-23";
        /// <summary>The held tool layer of a cast costume (the hammer). A live visual never hides it.</summary>
        public const string ToolLayer = "Costume Tool 04E";
        /// <summary>Every procedural cast costume layer name (CastCostumeBuilder04E).</summary>
        public static readonly ReadOnlyCollection<string> CostumeLayers = Array.AsReadOnly(new[] { "Costume Head 04E", "Costume Torso 04E", "Costume Waist 04E", ToolLayer });
        /// <summary>What a dropped-in, fully dressed character hides by default: headgear, torso and waist layers (not the tool).</summary>
        public static readonly ReadOnlyCollection<string> DropInHiddenCostume = Array.AsReadOnly(new[] { "Costume Head 04E", "Costume Torso 04E", "Costume Waist 04E" });
        /// <summary>What the H9 Dante hides: the apron bib and the apron skirt and belt (exactly the Editor review's policy).</summary>
        public static readonly ReadOnlyCollection<string> DanteHiddenCostume = Array.AsReadOnly(new[] { DanteReviewH8S.CostumeTorso, DanteReviewH8S.CostumeWaist });

        /// <summary>
        /// The identity the live registry knows an actor by: "malik" for the player, "npc:" plus the display name for a
        /// human NPC, and "" for anything the humanoid adapter cannot serve (creatures, people in creature mode, unnamed).
        /// </summary>
        public static string Identity(bool isPlayer, bool creature, string displayName)
        {
            if (isPlayer) return PlayerIdentity;
            if (creature || string.IsNullOrWhiteSpace(displayName)) return "";
            return NpcPrefix + displayName.Trim();
        }

        /// <summary>True for "malik", "npc:" followed by a plain name (no path, colon or control characters), or a crowd
        /// identity ("crowd:" plus a slug, LiveCrowdRules.ValidIdentity), which serves NPCs by role instead of by name.</summary>
        public static bool ValidIdentity(string identity)
        {
            if (identity == PlayerIdentity) return true;
            if (LiveCrowdRules.ValidIdentity(identity)) return true;
            if (string.IsNullOrEmpty(identity) || !identity.StartsWith(NpcPrefix, StringComparison.Ordinal)) return false;
            string name = identity.Substring(NpcPrefix.Length);
            if (name.Trim().Length == 0 || name != name.Trim()) return false;
            foreach (char c in name)
                if (c == '/' || c == '\\' || c == ':' || c < ' ') return false;
            return true;
        }

        /// <summary>
        /// True when an enabled row with rowIdentity serves an actor with identity. A row with a cast id serves only the
        /// exact live campaign cast actor with that id (actorCastId), never a lookalike with the same display name.
        /// </summary>
        public static bool Matches(string rowIdentity, bool rowEnabled, string rowCastId, string identity, string actorCastId)
        {
            if (!rowEnabled || string.IsNullOrEmpty(identity) || !string.Equals(rowIdentity, identity, StringComparison.Ordinal)) return false;
            return string.IsNullOrEmpty(rowCastId) || string.Equals(rowCastId, actorCastId, StringComparison.Ordinal);
        }

        /// <summary>The index of the first row that serves the actor (rows are ordered drop-ins first), or -1.</summary>
        public static int Find(IList<LiveCastRow> rows, string identity, string actorCastId)
        {
            if (rows == null) return -1;
            for (int i = 0; i < rows.Count; i++)
            {
                LiveCastRow row = rows[i];
                if (row == null || row.Model == null) continue;
                if (Matches(row.Model.Identity, row.Model.Enabled, row.CastId, identity, actorCastId)) return i;
            }
            return -1;
        }

        /// <summary>
        /// The live contract: an enabled, owner-approved row with complete rights (the original ImportedArt04OH policy), the
        /// original pose authority, bind-axis alignment at scale one, all 47 original pose bones mapped exactly once onto the
        /// H9 humanoid bone names (ImportedBindH3.CandidateBone), passive bones strictly below mapped bones, the ten sockets,
        /// a cast id that names the same person as the identity and hidden costume layers that never include the tool.
        /// </summary>
        public static bool CanGoLive(LiveCastRow row, out string reason)
        {
            reason = "";
            if (row == null || row.Model == null) { reason = "The live row is empty."; return false; }
            ImportedModel04OD m = row.Model;
            if (!m.Enabled) { reason = "The live row is disabled."; return false; }
            if (!ValidIdentity(m.Identity)) { reason = "Unsupported identity \"" + m.Identity + "\". Use malik or npc: plus the character's in-game name."; return false; }
            bool crowd = LiveCrowdRules.ValidIdentity(m.Identity);
            if (crowd != (row.CrowdRoles != 0)) { reason = crowd ? "A crowd identity needs the roles it serves." : "Only a crowd: identity may serve roles."; return false; }
            if (crowd && !LiveCrowdRules.ValidRoles(row.CrowdRoles)) { reason = "A crowd row may serve only civilians, clerks, police, sentinels, gang members and mission enemies."; return false; }
            if (crowd && !string.IsNullOrEmpty(row.CastId)) { reason = "A crowd row is never bound to a campaign cast actor."; return false; }
            if (!string.IsNullOrEmpty(row.CastId))
            {
                if (!CastIds04E.Has(row.CastId)) { reason = "Unknown cast id \"" + row.CastId + "\"."; return false; }
                if (m.Identity != NpcPrefix + CastIds04E.Name(row.CastId)) { reason = "Cast id \"" + row.CastId + "\" belongs to " + CastIds04E.Name(row.CastId) + ", not " + m.Identity + "."; return false; }
            }
            if (!m.AlignBindAxesH3) { reason = "Live humanoids need bind-axis alignment."; return false; }
            if (float.IsNaN(m.Scale) || Mathf.Abs(m.Scale - 1f) > 0.0001f) { reason = "Live humanoids are authored in metres at scale one."; return false; }
            if (m.AnimationAuthority != ImportedAnimationAuthority04OH.ExistingPose) { reason = "The original procedural pose stays the only animation authority."; return false; }
            ImportedArtRecord04OH p = m.ArtProof;
            if (p == null) { reason = "A live row needs its art proof (creator, license, rights record, export hash)."; return false; }
            if (!p.OwnerApproved) { reason = "The owner has not approved this appearance."; return false; }
            if (!ImportedArt04OH.CanActivate(p, m.AnimationAuthority, out reason)) return false;
            if (!ImportedModelRegistry04OD.ValidMapping(RigLayout.BoneCount, m.Bones, m.Scale, out reason)) return false;
            foreach (ImportedBone04OD bone in m.Bones)
            {
                string expected = ImportedBindH3.CandidateBone(RigLayout.Names[bone.SourceBone]);
                if (DanteReviewH8S.Leaf(bone.TargetPath) != expected)
                { reason = "The mapping differs from the H9 humanoid bone names at " + RigLayout.Names[bone.SourceBone] + " (expected " + expected + ")."; return false; }
            }
            if (!ImportedModelRegistry04OD.ValidPassivePaths(m.PassiveBonePaths)) { reason = "Invalid or duplicate passive bone paths."; return false; }
            if (!PassiveBelowMapped(m.Bones, m.PassiveBonePaths, out reason)) return false;
            if (!DanteReviewH8S.SocketsComplete(m.Sockets, out reason)) return false;
            if (row.HiddenCostume != null)
                foreach (string layer in row.HiddenCostume)
                    if (layer == ToolLayer || !CostumeLayers.Contains(layer)) { reason = "A live visual may hide only the head, torso and waist costume layers, never the held tool (" + layer + ")."; return false; }
            return true;
        }

        /// <summary>
        /// True for the original pose bones whose imported bone keeps its own bind length from its parent and takes only the
        /// rotation (owner report 2026-10-24: "their necks are messed up"). The procedural neck is longer than the imported
        /// bodies' necks, so copying the neck and head positions lifted every head about five centimetres and stretched the
        /// neck out of the collar; with the body's own neck length the head sits on its own shoulders.
        /// </summary>
        public static bool KeepsOwnLength(int bone)
        {
            return bone == RigLayout.Neck || bone == RigLayout.Head;
        }

        /// <summary>
        /// The original adapter's passive rule on paths: each passive path is not itself mapped and lies strictly below at
        /// least one mapped bone, so it keeps its bind-local offset under a driven parent and no skin weight is pruned.
        /// </summary>
        public static bool PassiveBelowMapped(IList<ImportedBone04OD> bones, IList<string> passive, out string reason)
        {
            reason = "";
            if (passive == null || passive.Count == 0) return true;
            if (bones == null) { reason = "Passive paths need the mapped bones they ride."; return false; }
            foreach (string path in passive)
            {
                bool below = false;
                foreach (ImportedBone04OD bone in bones)
                {
                    if (bone == null || string.IsNullOrEmpty(bone.TargetPath)) continue;
                    if (string.Equals(bone.TargetPath, path, StringComparison.Ordinal)) { reason = "A bone cannot be both mapped and passive: " + DanteReviewH8S.Leaf(path) + "."; return false; }
                    if (path.Length > bone.TargetPath.Length + 1 && path.StartsWith(bone.TargetPath, StringComparison.Ordinal) && path[bone.TargetPath.Length] == '/') below = true;
                }
                if (!below) { reason = "Weighted bone " + DanteReviewH8S.Leaf(path) + " is not below a mapped bone. Parent extra bones under the body skeleton."; return false; }
            }
            return true;
        }

        /// <summary>
        /// One frame's presentation. While an Editor review owns the same actor the live visual yields completely: it hides
        /// and gives the procedural body and costume back to their owners (the review shows its own visual). Otherwise the
        /// visual mirrors the procedural body's own state: hidden when the owner hides the body (ownerShowsBody false), and
        /// the body's exact shadow mode, so first person (shadows only) and shadowless bosses stay correct.
        /// </summary>
        public static LiveCastPresentation Present(bool reviewOwnsActor, bool ownerShowsBody, ShadowCastingMode ownerShadows)
        {
            LiveCastPresentation look = new LiveCastPresentation();
            if (reviewOwnsActor)
            {
                look.ShowVisual = false;
                look.Shadows = ShadowCastingMode.On;
                look.HideBody = false;
                return look;
            }
            look.ShowVisual = ownerShowsBody;
            look.Shadows = ownerShadows;
            look.HideBody = true;
            return look;
        }

        /// <summary>True when a live visual with this hidden list hides the named procedural costume layer (never the tool).</summary>
        public static bool HidesCostumeLayer(IList<string> hidden, string layerName)
        {
            if (hidden == null || string.IsNullOrEmpty(layerName) || layerName == ToolLayer) return false;
            for (int i = 0; i < hidden.Count; i++)
                if (string.Equals(hidden[i], layerName, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// Whether a material's shader can draw under the active pipeline. A shader that is not supported, hidden or the
        /// error shader never draws; under URP only URP shaders draw (a Built-in shader would render magenta); under the
        /// Built-in pipeline a URP-only shader is refused. The live adapter keeps the procedural body when this fails.
        /// </summary>
        public static bool ShaderFits(string shaderName, bool supported, bool urp)
        {
            if (!supported || string.IsNullOrEmpty(shaderName) || shaderName.StartsWith("Hidden/", StringComparison.Ordinal)) return false;
            bool urpOnly = shaderName.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal)
                || shaderName.StartsWith("Shader Graphs/", StringComparison.Ordinal)
                || shaderName == Mats.UrpCharacterShaderName;
            return urp ? urpOnly : !urpOnly;
        }

        /// <summary>
        /// The skin weight policy. A row with complete influences (up to eight per vertex, nothing pruned) needs Unlimited
        /// skin weights: raise them when they are not active, and fail closed (procedural body) when a raise did not stick.
        /// </summary>
        public static LiveCastSkinStep SkinWeightStep(bool completeInfluences, bool unlimitedNow, bool raiseAttempted)
        {
            if (!completeInfluences || unlimitedNow) return LiveCastSkinStep.Keep;
            return raiseAttempted ? LiveCastSkinStep.FailClosed : LiveCastSkinStep.Raise;
        }
    }
}
