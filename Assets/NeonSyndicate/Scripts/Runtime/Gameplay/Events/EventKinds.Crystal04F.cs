using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Three finite autonomous city stories, owned by the existing event lifecycle and its tagged cleanup.</summary>
    public static partial class EventKinds
    {
        private sealed class CityWork04F
        {
            public bool Accepted;
            public bool Mediated;
            public bool Claimed;
            public bool Recording;
            public float Channel;
            public Vector3 RecordAt;
        }

        private static IEnumerator CrystalCity04F(EventInstance e)
        {
            e.Woken = true;
            EventTemplate t = e.T;
            bool rescue = t.Kind == "DrakeRescue";
            bool cache = t.Kind == "CrystalCache";
            List<NpcController> wardens = new List<NpcController>();
            List<NpcController> rivals = new List<NpcController>();
            CityWork04F work = new CityWork04F();
            int count = Mathf.Clamp(t.Count > 0 ? t.Count : 2, 1, CityCombatRules04F.FightersPerSide(Settings.Graphics == GraphicsPreset.Low));
            for (int i = 0; i < count; i++)
            {
                NpcController first = CityFighter04F(e, t.FactionA, 0, i, count, rivals);
                NpcController second = CityFighter04F(e, t.FactionB, 1, i, count, wardens);
                if (first == null || second == null)
                {
                    e.Miss("This street has no safe space for a dispute. The crews moved on.");
                    yield break;
                }
                wardens.Add(first);
                rivals.Add(second);
                yield return null;
            }
            NpcController handler = wardens.Count > 0 ? wardens[wardens.Count - 1] : null;
            NpcBondedDrake04F bonded = null;
            if (!string.IsNullOrEmpty(t.Prop)) e.AddProp(t.Prop, e.Pos, e.Yaw, Accent(e));
            Vector3 neutral;
            if (!CitySafeGround04F(e.Pos + Vector3.back * 5f, e.Pos, out neutral))
            { e.Miss("The neutral passage is obstructed. The crews moved on."); yield break; }
            work.RecordAt = neutral;
            if (!cache && handler != null)
            {
                bonded = e.Root.gameObject.AddComponent<NpcBondedDrake04F>();
                bonded.Init(e, handler, rescue, neutral);
                e.CleanupHooks.Add(() => { if (bonded != null) bonded.Retire(); });
                handler.DisplayName = QuestWorld.FactionName(t.FactionA) + " Drake Handler";
            }
            Interactable accept = e.AddInteract(neutral, rescue ? "Use E / interact to open a safe route for the handler" : cache ? "Use E / interact to record the cache seal" : "Use E / interact to broker cargo passage", () =>
            {
                if (e.Done || work.Accepted) return;
                work.Accepted = true;
                work.Recording = true;
                work.Channel = 0f;
            }, false);
            accept.Condition = () => !e.Done && !work.Accepted;
            accept.Priority = 2;
            WorldMarker mark = WorldMarker.Create(neutral, new Color(0.4f, 0.95f, 0.8f), 1.4f, e.Root, false);
            e.Objects.Add(mark.gameObject);
            if (Game.Hud != null && e.DistToPlayer < 150f)
                Game.Hud.Notify(t.Callout + " Watching is safe. No reinforcements will arrive.", 6f);
            bool concluded = false;
            bool alliedVictory = false;
            float limit = Mathf.Clamp(t.Seconds > 0f ? t.Seconds : CityCombatRules04F.EventSeconds, 40f, CityCombatRules04F.EventSeconds);
            float fightEnds = e.Age + limit;
            while (!e.Done && e.Age < fightEnds)
            {
                int a = EventInstance.Alive(wardens), b = EventInstance.Alive(rivals);
                // Handler survival is authoritative before mediation, victory, reunion and reward decisions.
                if (rescue && (handler == null || handler.IsDead || bonded != null && !bonded.Healthy))
                { e.Fail("The handler or drake fell. The crew withdrew; the player's bond was never changed."); yield break; }
                if (work.Recording)
                {
                    bool near = Vector3.Distance(Game.PlayerPosition, work.RecordAt) < 3.5f && !Game.Player.IsDead;
                    work.Channel = near ? work.Channel + Time.deltaTime : 0f;
                    if (work.Channel >= 3f)
                    {
                        work.Recording = false;
                        if (!cache) work.Mediated = true;
                    }
                }
                if (rescue && bonded != null && (work.Mediated || b == 0)) bonded.OpenRoute();
                if (work.Mediated && !rescue)
                {
                    concluded = true;
                    alliedVictory = true;
                    break;
                }
                if (rescue && bonded != null && CityCombatRules04F.RescueReady(handler != null && !handler.IsDead, bonded.Reunited, work.Mediated, b == 0))
                {
                    concluded = true;
                    alliedVictory = true;
                    break;
                }
                if (!rescue && (a == 0 || b == 0))
                {
                    concluded = true;
                    alliedVictory = a > 0;
                    break;
                }
                if (e.StatusDue())
                {
                    string phase = e.Age < CityCombatRules04F.SetupSeconds ? "Warning, weapons lowered" : "Autonomous clash";
                    e.Status = rescue && bonded != null && bonded.RouteOpen ? "Safe route open. Keep the handler alive until the drakeling physically reaches her" : work.Recording ? "Remain at the neutral seal   " + Mathf.FloorToInt(work.Channel) + "/3 s" : phase + "   " + a + " vs " + b + "   " + Mathf.CeilToInt(Mathf.Max(0f, fightEnds - e.Age)) + " s   Observe, assist, mediate, or leave";
                    if (bonded != null && bonded.Vitality != null) e.Status += "   Drake " + Mathf.CeilToInt(bonded.Vitality.Health) + "/90" + (" (" + bonded.Intent04H + ")");
                }
                yield return null;
            }
            if (e.Done) yield break;
            StopCityFighters04F(wardens);
            StopCityFighters04F(rivals);
            if (!concluded)
            {
                e.Miss("The crews called a ceasefire and withdrew. No further wave is coming.");
                yield break;
            }
            if (!alliedVictory)
            {
                e.Miss("The rival crew secured the passage and moved on. No observer penalty was applied.");
                yield break;
            }
            if (!work.Accepted)
            {
                e.Miss("The crews settled the incident. Observers receive no combat reward.");
                yield break;
            }
            if (rescue && handler != null && !handler.IsDead) handler.GuardAt(e.Pos);
            // The player must physically return and claim. A death or corpse cleanup can never award this reward.
            Interactable claim = e.AddInteract(neutral, "Use E / interact to collect the sealed crystal supplies", () => work.Claimed = true, false);
            claim.Condition = () => !e.Done && !work.Claimed;
            claim.Priority = 2;
            float collectUntil = e.Age + 30f;
            while (!work.Claimed && !e.Done && e.Age < collectUntil)
            {
                if (rescue && (handler == null || handler.IsDead || bonded == null || !bonded.Reunited))
                { e.Fail("The handler and drake were separated again before collection. No rescue reward was granted."); yield break; }
                if (e.StatusDue()) e.Status = "Incident ended. Return to the neutral seal to collect earned supplies   " + Mathf.CeilToInt(collectUntil - e.Age) + " s";
                yield return null;
            }
            if (rescue && (handler == null || handler.IsDead || bonded == null || !bonded.Reunited))
            { e.Fail("The handler fell before the rescue was confirmed. No reward was granted."); yield break; }
            if (CityCombatRules04F.Earned(work.Accepted, work.Claimed, e.Done))
                e.Succeed(e.QuestOwned04I?new QuestReward():t.Reward, 1f, work.Mediated ? "Passage secured without further casualties. The crews are standing down." : null);
            else if (!e.Done) e.Miss("The unclaimed supplies left with their owners.");
        }

        private static void StopCityFighters04F(List<NpcController> fighters)
        {
            foreach (NpcController actor in fighters)
            {
                if (actor == null || actor.IsDead) continue;
                if (actor.CityActor04F != null) actor.CityActor04F.StoodDown = true;
                actor.Target = null;
                actor.Hostile = false;
                actor.Weapons.CancelActions();
                actor.GuardAt(actor.transform.position);
                CombatDirector.ReleaseMeleeToken(actor);
                CombatDirector.ReleaseShotToken(actor);
            }
        }

        private static NpcController CityFighter04F(EventInstance e, string factionId, int side, int index, int count, List<NpcController> opponents)
        {
            FactionDef faction = FactionDb.Get(factionId);
            if (faction == null) return null;
            Vector3 at;
            Vector3 preferred = e.Pos + new Vector3(side == 0 ? -6f : 6f, 0f, (index - (count - 1) * 0.5f) * 3.2f);
            if (!CitySafeGround04F(preferred, e.Pos, out at)) return null;
            CrystalNpcRole04F role = index == 0 ? (side == 0 ? (e.T.Kind == "CrystalCache" ? CrystalNpcRole04F.WardSentinel : CrystalNpcRole04F.BladeWarden) : CrystalNpcRole04F.RiftDuelist)
                : index == 1 ? CrystalNpcRole04F.FocusWeaver : CrystalNpcRole04F.WardSentinel;
            Appearance look = side == 0 ? BodyFactory.PoliceLook() : Appearance.Random(e.Rand);
            look.SuitStyle = 3;
            look.ShellStyle = role == CrystalNpcRole04F.WardSentinel ? 3 : role == CrystalNpcRole04F.BladeWarden ? 2 : 1;
            look.GauntletStyle = role == CrystalNpcRole04F.RiftDuelist ? 3 : 1;
            look.VisorStyle = role == CrystalNpcRole04F.FocusWeaver ? 1 : role == CrystalNpcRole04F.WardSentinel ? 2 : 0;
            look.ArmorColor = side == 0 ? 9 : 4;
            look.Jacket = role == CrystalNpcRole04F.FocusWeaver;
            NpcRole npcRole = faction.Id == "frost_wardens" ? NpcRole.Police : NpcRole.Enemy;
            NpcController actor = NpcController.Spawn(at, side == 0 ? 90f : -90f, npcRole, look, faction.GangId);
            actor.Persistent = true;
            actor.MissionTag = e.Tag;
            actor.DropsLoot = false;
            actor.CashDrop = 0;
            actor.Courage = 1f;
            actor.SetCrystalRole04F(role, true);
            actor.DisplayName = faction.Name + " " + (role == CrystalNpcRole04F.FocusWeaver ? "Focus Weaver" : role == CrystalNpcRole04F.WardSentinel ? "Ward Sentinel" : "Blade Guard");
            actor.Health.MaxHealth = actor.Health.Health = 140f;
            actor.transform.SetParent(e.Root, true);
            actor.GuardAt(at);
            NpcCityActor04F member = actor.gameObject.AddComponent<NpcCityActor04F>();
            member.Bind(e, actor, side, opponents, faction.Color);
            e.Npcs.Add(actor);
            return actor;
        }

        /// <summary>Floor and body clearance on the street. Never uses indoor room floors or an occupied doorway.</summary>
        private static bool CitySafeGround04F(Vector3 preferred, Vector3 center, out Vector3 grounded)
        {
            grounded = preferred;
            for (int i = 0; i < 9; i++)
            {
                Vector3 candidate = preferred + new Vector3(Mathf.Cos(i * 2.4f), 0f, Mathf.Sin(i * 2.4f)) * (i == 0 ? 0f : 0.6f + i * 0.45f);
                if (MathUtil.FlatDistance(candidate, center) > 16f || PedManager.ExcludedVenue04C(candidate)) continue;
                RaycastHit hit;
                if (!Physics.Raycast(candidate + Vector3.up * 4f, Vector3.down, out hit, 8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (hit.normal.y < 0.93f || Mathf.Abs(hit.point.y - center.y) > 1.2f || WorldBounds.IsUnderground(hit.point)) continue;
                if (Game.Layout.IsInsideBuilding(hit.point, 1.5f)) continue;
                if (Physics.CheckCapsule(hit.point + Vector3.up * 0.65f, hit.point + Vector3.up * 2f, 0.5f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                bool occupied = false;
                foreach (NpcController resident in NpcController.All)
                    if (resident != null && !resident.IsDead && MathUtil.FlatDistance(resident.transform.position, hit.point) < 1.4f) { occupied = true; break; }
                if (occupied || !TrafficManager.Clear(hit.point, 2f)) continue;
                grounded = hit.point;
                return true;
            }
            return false;
        }
    }
}
