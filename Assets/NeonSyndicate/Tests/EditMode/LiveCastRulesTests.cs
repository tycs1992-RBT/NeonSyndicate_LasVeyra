using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The live character rules (owner decision 2026-10-23): which row an actor receives, which rows may go live, what shows
    /// on a frame (mirroring and review yield), the costume policy, the pipeline check, the skin weight policy and the
    /// allocation-free per-frame adapter code. Engine-free; native Unity is NOT RUN here.
    /// </summary>
    public sealed class LiveCastRulesTests
    {
        /// <summary>A verified review row promoted the way the generator promotes it (LiveCastSetup.Promote).</summary>
        internal static LiveCastRow Live(ImportedModel04OD review, string castId, string[] hidden)
        {
            review.Enabled = true;
            review.ArtProof.OwnerApproved = true;
            review.ArtProof.RightsRecordId = LiveCastRules.OwnerApprovalRecord + "; " + review.ArtProof.RightsRecordId;
            return new LiveCastRow { Model = review, CastId = castId, HiddenCostume = hidden, Label = "test", Origin = "test", Stamp = "s" };
        }

        internal static LiveCastRow Dante()
        {
            ImportedModel04OD review = DanteReviewH8STests.Row();
            review.ArtProof.ExportAssetPath = DanteReviewH9.ExportPath;
            return Live(review, CastIds04E.Dante, new[] { DanteReviewH8S.CostumeTorso, DanteReviewH8S.CostumeWaist });
        }

        internal static LiveCastRow Malik()
        {
            ImportedModel04OD review = DanteReviewH8STests.Row();
            review.Identity = LiveCastRules.PlayerIdentity;
            review.ArtProof.AssetId = MalikReviewH9.AssetId;
            review.ArtProof.ExportAssetPath = MalikReviewH9.ExportPath;
            review.ArtProof.RightsRecordId = "h9-selected-human-cc0-lineage";
            return Live(review, "", new string[0]);
        }

        [Test]
        public void IdentityFollowsTheOriginalAdapterForHumansOnly()
        {
            Assert.AreEqual("malik", LiveCastRules.Identity(true, false, "Malik Veyr"));
            Assert.AreEqual("npc:Dante Rook", LiveCastRules.Identity(false, false, "Dante Rook"));
            Assert.AreEqual("npc:Dante Rook", LiveCastRules.Identity(false, false, " Dante Rook "));
            Assert.AreEqual("", LiveCastRules.Identity(false, true, "Glass Mite"), "creatures and creature-mode people are not served by the humanoid adapter");
            Assert.AreEqual("", LiveCastRules.Identity(false, false, "  "));
            Assert.IsTrue(LiveCastRules.ValidIdentity("malik"));
            Assert.IsTrue(LiveCastRules.ValidIdentity("npc:Corin Pell"));
            foreach (string bad in new[] { "", "npc:", "npc: Corin", "npc:a/b", "npc:a:b", "creature:glass_mite", "companion:Drake", "Malik" })
                Assert.IsFalse(LiveCastRules.ValidIdentity(bad), bad);
        }

        [Test]
        public void CastBoundRowsServeOnlyTheExactCastActor()
        {
            Assert.IsTrue(LiveCastRules.Matches("npc:Dante Rook", true, "dante", "npc:Dante Rook", "dante"));
            Assert.IsFalse(LiveCastRules.Matches("npc:Dante Rook", true, "dante", "npc:Dante Rook", ""), "a lookalike resident named Dante Rook stays procedural");
            Assert.IsFalse(LiveCastRules.Matches("npc:Dante Rook", false, "dante", "npc:Dante Rook", "dante"), "disabled rows never match");
            Assert.IsFalse(LiveCastRules.Matches("npc:Dante Rook", true, "dante", "npc:Corin Pell", "dante"));
            Assert.IsTrue(LiveCastRules.Matches("npc:Corin Pell", true, "", "npc:Corin Pell", ""), "an open row matches by display name");
            Assert.IsFalse(LiveCastRules.Matches("malik", true, "", "", ""));
        }

        [Test]
        public void FindTakesTheFirstServingRowSoDropInsWin()
        {
            LiveCastRow dropIn = Malik();
            dropIn.Origin = "dropin:Malik";
            LiveCastRow builtin = Malik();
            builtin.Origin = "builtin:malik-h9";
            LiveCastRow dante = Dante();
            List<LiveCastRow> rows = new List<LiveCastRow> { null, dropIn, builtin, dante };
            Assert.AreEqual(1, LiveCastRules.Find(rows, "malik", ""));
            Assert.AreEqual(3, LiveCastRules.Find(rows, "npc:Dante Rook", "dante"));
            Assert.AreEqual(-1, LiveCastRules.Find(rows, "npc:Dante Rook", ""));
            Assert.AreEqual(-1, LiveCastRules.Find(null, "malik", ""));
            dropIn.Model.Enabled = false;
            Assert.AreEqual(2, LiveCastRules.Find(rows, "malik", ""), "a disabled drop-in leaves the built-in live");
        }

        [Test]
        public void DeliveredMalikAndDanteRowsGoLiveOnlyWithTheOwnerDecision()
        {
            string reason;
            Assert.IsTrue(LiveCastRules.CanGoLive(Malik(), out reason), reason);
            Assert.IsTrue(LiveCastRules.CanGoLive(Dante(), out reason), reason);
            StringAssert.StartsWith(LiveCastRules.OwnerApprovalRecord, Malik().Model.ArtProof.RightsRecordId);
            // The live row is not a review row: the review contracts still refuse an enabled, approved row.
            Assert.IsFalse(MalikReviewH9.CanReview(Malik().Model, true, out reason));
            Assert.IsFalse(DanteReviewH9.CanReview(Dante().Model, true, out reason));
            // And the delivered review rows (disabled, unapproved) never go live.
            ImportedModel04OD review = DanteReviewH8STests.Row();
            Assert.IsFalse(LiveCastRules.CanGoLive(new LiveCastRow { Model = review, CastId = CastIds04E.Dante }, out reason));
            review.Enabled = true;
            Assert.IsFalse(LiveCastRules.CanGoLive(new LiveCastRow { Model = review, CastId = CastIds04E.Dante }, out reason), "enabled without owner approval stays refused");
        }

        [Test]
        public void LiveContractRefusesEveryBrokenRow()
        {
            string reason;
            Assert.IsFalse(LiveCastRules.CanGoLive(null, out reason));
            Refused(r => r.Model.Enabled = false, "disabled");
            Refused(r => r.Model.ArtProof.OwnerApproved = false, "not approved");
            Refused(r => r.Model.ArtProof.Diagnostic = true, "diagnostic fixture");
            Refused(r => r.Model.ArtProof.ExportSha256 = "", "no export hash");
            Refused(r => r.Model.ArtProof.RightsRecordId = "", "no rights record");
            Refused(r => r.Model.ArtProof.CommercialUse = false, "no commercial use");
            Refused(r => r.Model.ArtProof = null, "no proof");
            Refused(r => r.Model.AnimationAuthority = ImportedAnimationAuthority04OH.AuthoredClips, "authored clip authority");
            Refused(r => r.Model.AlignBindAxesH3 = false, "no bind alignment");
            Refused(r => r.Model.Scale = 1.2f, "rescaled");
            Refused(r => r.Model.Identity = "creature:glass_mite", "creature identity");
            Refused(r => r.CastId = "corin", "unknown cast id");
            Refused(r => r.CastId = CastIds04E.Dispatcher, "cast id of another person");
            Refused(r => r.HiddenCostume = new[] { LiveCastRules.ToolLayer }, "hiding the held tool");
            Refused(r => r.HiddenCostume = new[] { "Body" }, "hiding something that is not a costume layer");
            Refused(r => r.Model.Sockets = Array.FindAll(r.Model.Sockets, s => s.Role != "muzzle"), "missing socket");
            Refused(r => r.Model.Bones[RigLayout.HandL].TargetPath = r.Model.Bones[RigLayout.HandL].TargetPath.Replace("hand_l", "palm_l"), "renamed mapped bone");
            Refused(r => r.Model.Bones = Array.FindAll(r.Model.Bones, b => b.SourceBone != RigLayout.Head), "unmapped pose bone");
            Refused(r => r.Model.PassiveBonePaths = new[] { r.Model.Bones[RigLayout.Hips].TargetPath.Replace("/pelvis", "") }, "weighted bone above the pelvis");
            Refused(r => r.Model.PassiveBonePaths = new[] { r.Model.Bones[RigLayout.Chest].TargetPath }, "mapped and passive at once");
        }

        private static void Refused(Action<LiveCastRow> breakIt, string what)
        {
            LiveCastRow row = Dante();
            breakIt(row);
            string reason;
            Assert.IsFalse(LiveCastRules.CanGoLive(row, out reason), what);
            Assert.IsNotEmpty(reason, what);
        }

        [Test]
        public void ContractIsEvaluatedOncePerRow()
        {
            LiveCastRow row = Dante();
            string reason;
            Assert.IsTrue(LiveCastRegistry.Contract(row, out reason), reason);
            row.Model.Enabled = false;
            Assert.IsTrue(LiveCastRegistry.Contract(row, out reason), "the cached verdict holds for the loaded row");
            LiveCastRow broken = Dante();
            broken.Model.ArtProof.OwnerApproved = false;
            Assert.IsFalse(LiveCastRegistry.Contract(broken, out reason));
            Assert.IsNotEmpty(reason);
            Assert.IsFalse(LiveCastRegistry.Contract(null, out reason));
        }

        [Test]
        public void PresentationMirrorsTheBodyAndYieldsToReviews()
        {
            LiveCastPresentation look = LiveCastRules.Present(false, true, ShadowCastingMode.On);
            Assert.IsTrue(look.ShowVisual);
            Assert.AreEqual(ShadowCastingMode.On, look.Shadows);
            Assert.IsTrue(look.HideBody);
            look = LiveCastRules.Present(false, true, ShadowCastingMode.ShadowsOnly);
            Assert.IsTrue(look.ShowVisual, "first person keeps the shadow");
            Assert.AreEqual(ShadowCastingMode.ShadowsOnly, look.Shadows);
            Assert.IsTrue(look.HideBody);
            look = LiveCastRules.Present(false, true, ShadowCastingMode.Off);
            Assert.AreEqual(ShadowCastingMode.Off, look.Shadows, "a shadowless boss body stays shadowless");
            look = LiveCastRules.Present(false, false, ShadowCastingMode.On);
            Assert.IsFalse(look.ShowVisual, "the owner hid the person");
            Assert.IsTrue(look.HideBody, "the hidden body stays hidden, never shown twice");
            foreach (bool owner in new[] { true, false })
                foreach (ShadowCastingMode mode in new[] { ShadowCastingMode.On, ShadowCastingMode.ShadowsOnly, ShadowCastingMode.Off })
                {
                    look = LiveCastRules.Present(true, owner, mode);
                    Assert.IsFalse(look.ShowVisual, "an Editor review draws its own visual");
                    Assert.IsFalse(look.HideBody, "the review gets the procedural body and costume back");
                }
        }

        [Test]
        public void CostumePolicyNeverHidesTheHeldTool()
        {
            Assert.IsTrue(LiveCastRules.HidesCostumeLayer(LiveCastRules.DanteHiddenCostume, "Costume Torso 04E"));
            Assert.IsTrue(LiveCastRules.HidesCostumeLayer(LiveCastRules.DanteHiddenCostume, "Costume Waist 04E"));
            Assert.IsFalse(LiveCastRules.HidesCostumeLayer(LiveCastRules.DanteHiddenCostume, "Costume Head 04E"), "Dante hides exactly what the Editor review hides");
            Assert.IsFalse(LiveCastRules.HidesCostumeLayer(LiveCastRules.DanteHiddenCostume, LiveCastRules.ToolLayer));
            Assert.IsTrue(LiveCastRules.HidesCostumeLayer(LiveCastRules.DropInHiddenCostume, "Costume Head 04E"));
            Assert.IsFalse(LiveCastRules.HidesCostumeLayer(new[] { LiveCastRules.ToolLayer }, LiveCastRules.ToolLayer), "even a list naming the tool keeps it");
            Assert.IsFalse(LiveCastRules.HidesCostumeLayer(null, "Costume Torso 04E"));
            CollectionAssert.DoesNotContain(LiveCastRules.DropInHiddenCostume, LiveCastRules.ToolLayer);
            Assert.AreEqual(DanteReviewH8S.CostumeTorso, LiveCastRules.DanteHiddenCostume[0]);
            Assert.AreEqual(DanteReviewH8S.CostumeWaist, LiveCastRules.DanteHiddenCostume[1]);
        }

        [Test]
        public void ShadersMustFitTheActivePipeline()
        {
            Assert.IsTrue(LiveCastRules.ShaderFits("Standard", true, false));
            Assert.IsFalse(LiveCastRules.ShaderFits("Standard", true, true), "a Built-in material under URP would be magenta: fail closed");
            Assert.IsTrue(LiveCastRules.ShaderFits("Universal Render Pipeline/Lit", true, true));
            Assert.IsFalse(LiveCastRules.ShaderFits("Universal Render Pipeline/Lit", true, false));
            Assert.IsTrue(LiveCastRules.ShaderFits(Mats.UrpCharacterShaderName, true, true));
            Assert.IsFalse(LiveCastRules.ShaderFits("Standard", false, false), "unsupported");
            Assert.IsFalse(LiveCastRules.ShaderFits("Hidden/InternalErrorShader", true, false));
            Assert.IsFalse(LiveCastRules.ShaderFits("", true, false));
            Assert.IsFalse(LiveCastRules.ShaderFits(null, true, true));
        }

        [Test]
        public void CompleteWeightsRaiseUnlimitedOrFailClosed()
        {
            Assert.AreEqual(LiveCastSkinStep.Keep, LiveCastRules.SkinWeightStep(false, false, false), "four-weight rows need nothing");
            Assert.AreEqual(LiveCastSkinStep.Keep, LiveCastRules.SkinWeightStep(true, true, false));
            Assert.AreEqual(LiveCastSkinStep.Raise, LiveCastRules.SkinWeightStep(true, false, false));
            Assert.AreEqual(LiveCastSkinStep.FailClosed, LiveCastRules.SkinWeightStep(true, false, true), "a raise that did not stick keeps the procedural body");
            Assert.AreEqual(LiveCastSkinStep.Keep, LiveCastRules.SkinWeightStep(true, true, true));
        }

        [Test]
        public void ComponentConstructionDoesNotCallNativeUnity()
        {
            ConstructorInfo constructor = typeof(LiveCastVisual).GetConstructor(Type.EmptyTypes);
            Assert.That(constructor, Is.Not.Null);
            List<string> native = new List<string>();
            foreach (MethodBase called in Calls(constructor))
                if (called.DeclaringType.Namespace == "UnityEngine" && called.DeclaringType != typeof(UnityEngine.MonoBehaviour)) native.Add(called.DeclaringType.Name + "." + called.Name);
            Assert.That(native, Is.Empty, "native presentation objects belong in the binding, never a field initializer");
        }

        [Test]
        public void PerFrameAdapterCodeAllocatesNothing()
        {
            foreach (string name in new[] { "LateUpdate", "ApplyPresentation", "CopyPose", "CopyProperties", "KeepSkinWeights", "Retarget", "SetBodyForced", "SetCostumeForced", "ReviewOwnsActor" })
            {
                MethodInfo method = typeof(LiveCastVisual).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null, name);
                foreach (OpCode code in Codes(method))
                    Assert.That(code != OpCodes.Newobj && code != OpCodes.Newarr && code != OpCodes.Box, name + " allocates (" + code.Name + ")");
                foreach (MethodBase called in Calls(method))
                    Assert.That(!(called.DeclaringType == typeof(string) && (called.Name == "Concat" || called.Name == "Format")), name + " builds a string every frame");
            }
        }

        private static readonly Dictionary<short, OpCode> OpCodeTable = BuildTable();

        private static Dictionary<short, OpCode> BuildTable()
        {
            Dictionary<short, OpCode> codes = new Dictionary<short, OpCode>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                OpCode code = (OpCode)field.GetValue(null);
                codes[code.Value] = code;
            }
            return codes;
        }

        /// <summary>Walks a method body's IL and returns each opcode with the offset of its operand.</summary>
        private static List<KeyValuePair<OpCode, int>> Walk(MethodBase method)
        {
            List<KeyValuePair<OpCode, int>> result = new List<KeyValuePair<OpCode, int>>();
            byte[] body = method.GetMethodBody().GetILAsByteArray();
            for (int at = 0; at < body.Length;)
            {
                short value = body[at++];
                if (value == 0xfe) value = (short)(0xfe00 | body[at++]);
                OpCode code = OpCodeTable[value];
                int bytes;
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: bytes = 0; break;
                    case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: bytes = 1; break;
                    case OperandType.InlineVar: bytes = 2; break;
                    case OperandType.InlineI8: case OperandType.InlineR: bytes = 8; break;
                    case OperandType.InlineSwitch: bytes = 4 + BitConverter.ToInt32(body, at) * 4; break;
                    default: bytes = 4; break;
                }
                result.Add(new KeyValuePair<OpCode, int>(code, at));
                at += bytes;
            }
            return result;
        }

        internal static IEnumerable<OpCode> Codes(MethodBase method)
        {
            foreach (KeyValuePair<OpCode, int> step in Walk(method)) yield return step.Key;
        }

        internal static IEnumerable<MethodBase> Calls(MethodBase method)
        {
            byte[] body = method.GetMethodBody().GetILAsByteArray();
            foreach (KeyValuePair<OpCode, int> step in Walk(method))
                if (step.Key.OperandType == OperandType.InlineMethod)
                    yield return method.Module.ResolveMethod(BitConverter.ToInt32(body, step.Value));
        }
    }
}
