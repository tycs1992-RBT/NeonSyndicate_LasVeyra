using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Quest field work: Collect, Escort, Hunt and Scan.</summary>
    public static partial class QuestSteps
    {
        private static readonly string[] ClueLines =
        {
            "Fresh tracks. Whatever made them is heavier than it looks.",
            "Claw marks, still glittering. It passed here minutes ago.",
            "The trail doubles back. It knows it is being followed.",
            "Blood on the ground. Not its own.",
            "A shed scale, warm to the touch. Close now.",
            "Everything around here has gone quiet. That is never good."
        };

        // ------------------------------------------------------------------ Collect

        private static IEnumerator Collect(QuestRun r, QuestStep s)
        {
            MaterialDef m = MaterialDb.Get(s.Material);
            if (m == null || Game.Materials == null) yield break;
            int need = Mathf.Max(1, s.Count);
            int start = Game.Materials.Count(m.Id);
            string objective = Obj(s, "Collect " + need + " " + m.Name + ".");
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            List<GameObject> caches = new List<GameObject>();
            List<Blip> blips = new List<Blip>();
            if (s.Caches > 0)
            {
                float radius = s.Radius > 0f ? s.Radius : 22f;
                int share = Mathf.CeilToInt(need / (float)s.Caches);
                System.Random rand = new System.Random(r.Salt + r.Step * 7);
                for (int i = 0; i < s.Caches; i++)
                {
                    Vector3 p = QuestWorld.OpenGround(QuestWorld.InRing(c, radius * 0.2f, radius, rand), c);
                    QuestProp prop = QuestProps.Build(!string.IsNullOrEmpty(s.Prop) ? s.Prop : "Cache", p, (float)rand.NextDouble() * 360f, r.C.M.transform, m.RarityColor);
                    r.C.Objects.Add(prop.gameObject);
                    caches.Add(prop.gameObject);
                    Blip b = r.C.BlipAt(p, m.RarityColor, m.Name);
                    b.Size = 0.8f;
                    blips.Add(b);
                    GameObject cacheObj = prop.gameObject;
                    Interactable it = Interactable.Create("Quest cache", p, 1.8f, "Gather " + m.Name, null, prop.transform);
                    it.AutoTrigger = true;
                    it.Use = () =>
                    {
                        if (cacheObj == null || !cacheObj.activeSelf) return;
                        Game.Materials.Add(m.Id, share, true);
                        if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.7f, 1.2f);
                        cacheObj.SetActive(false);
                    };
                }
            }
            r.C.Objective(objective);
            r.C.Target(c);
            float targetTimer = 0f;
            string counter = m.Name.ToUpperInvariant();
            while (true)
            {
                int have = Game.Materials.Count(m.Id);
                int progress = s.Have ? have : Mathf.Max(0, have - start);
                if (Game.Hud != null) Game.Hud.SetCounter(counter, Mathf.Min(progress, need), need);
                if (progress >= need) break;
                targetTimer -= Time.deltaTime;
                if (targetTimer <= 0f && caches.Count > 0)
                {
                    targetTimer = 0.5f;
                    float best = float.MaxValue;
                    for (int i = 0; i < caches.Count; i++)
                    {
                        if (caches[i] == null || !caches[i].activeSelf)
                        {
                            if (blips[i] != null) { r.C.RemoveBlip(blips[i]); blips[i] = null; }
                            continue;
                        }
                        float d = MathUtil.FlatDistance(caches[i].transform.position, Game.PlayerPosition);
                        if (d < best)
                        {
                            best = d;
                            r.C.Target(caches[i].transform.position);
                        }
                    }
                }
                yield return null;
            }
            foreach (Blip b in blips) if (b != null) r.C.RemoveBlip(b);
            if (s.Spend) Game.Materials.Remove(m.Id, need);
            if (Game.Hud != null) Game.Hud.ClearCounter();
            r.C.ClearTarget();
        }

        // ------------------------------------------------------------------ Escort

        private static IEnumerator Escort(QuestRun r, QuestStep s)
        {
            Vector3 from = r.Place(s.At, s.OffsetX, s.OffsetZ);
            Vector3 to = r.PlaceTo(s.To, s.ToOffsetX, s.ToOffsetZ);
            float arrive = s.ToRadius > 0f ? s.ToRadius : QuestDefaults.EscortArrive;
            string who = !string.IsNullOrEmpty(s.Name) ? s.Name : "your charge";
            string dest = PlaceName(to);
            Vector3 stand = QuestWorld.OpenGround(from + new Vector3(2f, 0f, 1.5f), from);
            NpcController ally = r.C.Ally(stand, QuestWorld.Look(!string.IsNullOrEmpty(s.Look) ? s.Look : "Civilian", new System.Random(r.Salt + 41)), who, s.Health > 0f ? s.Health : QuestDefaults.AllyHealth);
            ally.Pacifist = s.Pacifist;
            if (!s.Pacifist) ally.Arm(WeaponId.VectorSidearm);
            ally.SetState(NpcState.Idle);
            ally.GuardAt(ally.transform.position);
            // Meet them first, then they follow.
            r.C.Objective("Meet " + who + ".");
            Blip ab = r.C.BlipOn(ally.transform, new Color(0.35f, 0.75f, 1f), who, BlipKind.Ally);
            while (!ally.IsDead && MathUtil.FlatDistance(Game.PlayerPosition, ally.transform.position) > 5f)
            {
                r.C.Target(ally.transform.position);
                yield return null;
            }
            if (ally.IsDead)
            {
                r.C.M.Fail(who + " didn't make it.");
                yield break;
            }
            ally.SetState(NpcState.Follow);
            yield return SayLines(r, s.Lines);
            int waves = s.Count > 0 && !string.IsNullOrEmpty(s.Species) ? Mathf.Max(1, s.Waves) : 0;
            int waveDone = 0, pending = 0;
            float total = Mathf.Max(30f, MathUtil.FlatDistance(stand, to));
            Blip db = r.C.BlipAt(to, MarkerColor, dest);
            WorldMarker dm = r.C.Marker(to, MarkerColor, Mathf.Min(arrive, 4f));
            string objective = Obj(s, "Escort " + who + " to " + dest + ".");
            string behind = who + " fell behind. Go back.";
            bool pack = QuestLogic.SpawnsAsPack(CreatureSpeciesDb.Get(s.Species));
            Vector3 ahead = Vector3.zero;
            System.Random rand = new System.Random(r.Salt + 43);
            while (true)
            {
                if (ally == null || ally.IsDead)
                {
                    r.C.M.Fail(who + " didn't make it.");
                    yield break;
                }
                Vector3 ap = ally.InVehicle && ally.CurrentVehicle != null ? ally.CurrentVehicle.transform.position : ally.transform.position;
                float left = MathUtil.FlatDistance(ap, to);
                if (left < arrive) break;
                float progress = 1f - left / total;
                if (waveDone < waves && progress >= (waveDone + 1f) / (waves + 1f))
                {
                    waveDone++;
                    Vector3 dir = MathUtil.Flat(to - ap);
                    dir = dir.sqrMagnitude > 1f ? dir.normalized : Vector3.forward;
                    ahead = ap + dir * 30f;
                    if (pack) r.SpawnGroup(s.Species, QuestWorld.InRing(ahead, 4f, 12f, rand), s.Count, true, false, 1f, 1f, null);
                    else pending += s.Count;
                    if (Game.Hud != null) Game.Hud.Notify("Ambush! Keep " + who + " alive.", 3f);
                }
                if (pending > 0)
                {
                    // One a frame, so the ambush's bodies never build at once.
                    pending--;
                    Vector3 p = QuestWorld.InRing(ahead, 4f, 12f, rand);
                    r.Spawn(s.Species, p, QuestWorld.YawToward(p, ap), true, false, 1f, 1f, null);
                }
                if (MathUtil.FlatDistance(Game.PlayerPosition, ap) > 55f)
                {
                    if (r.C.ObjectiveText != behind) r.C.Objective(behind);
                    r.C.Target(ap);
                }
                else
                {
                    if (r.C.ObjectiveText != objective) r.C.Objective(objective);
                    r.C.Target(to);
                }
                yield return null;
            }
            r.C.RemoveBlip(ab);
            r.C.RemoveBlip(db);
            if (dm != null) Object.Destroy(dm.gameObject);
            r.C.ClearTarget();
            if (ally.InVehicle) ally.ExitVehicleImmediate(false);
            ally.Pacifist = true;
            ally.MoveTo(to, false, n => n.GuardAt(n.transform.position));
            if (Game.Hud != null) Game.Hud.Notify(who + " is safe.", 3f);
        }

        // ------------------------------------------------------------------ Hunt

        private static IEnumerator Hunt(QuestRun r, QuestStep s)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(s.Species);
            if (sp == null) yield break;
            Vector3 start = r.Place(s.At, s.OffsetX, s.OffsetZ);
            Vector3 lair = r.PlaceTo(s.To, s.ToOffsetX, s.ToOffsetZ);
            int clues = s.Clues > 0 ? s.Clues : QuestDefaults.Clues;
            string name = !string.IsNullOrEmpty(s.Name) ? s.Name : sp.Name;
            Vector3 dir = MathUtil.Flat(lair - start);
            Vector3 side = dir.sqrMagnitude > 1f ? MathUtil.RightOf(dir.normalized) : Vector3.right;
            System.Random rand = new System.Random(r.Salt + r.Step * 17);
            Color trail = QuestWorld.FactionColor(r.Quest.Arc);
            for (int i = 0; i < clues; i++)
            {
                float f = (i + 1f) / (clues + 1f);
                Vector3 p = Vector3.Lerp(start, lair, f) + side * (float)((rand.NextDouble() * 2.0 - 1.0) * 14.0);
                p = QuestWorld.OpenGround(p, lair);
                Vector3 next = Vector3.Lerp(start, lair, (i + 2f) / (clues + 1f));
                QuestProp tracks = QuestProps.Build("Tracks", p, QuestWorld.YawToward(p, next), r.C.M.transform, trail);
                r.C.Objects.Add(tracks.gameObject);
                yield return r.C.PressE(p, "Press E to read the trail", Obj(s, "Follow the " + name + "'s trail (" + (i + 1) + "/" + clues + ")."));
                if (Game.Hud != null) Game.Hud.Notify(ClueLines[(r.Salt + i) % ClueLines.Length], 4f);
            }
            yield return r.C.GoTo(lair, 30f, "Find the " + name + ".", false);
            CharacterBase quarry = r.Spawn(s.Species, QuestWorld.InRing(lair, 6f, 12f, rand), QuestWorld.YawToward(lair, Game.PlayerPosition), true, s.Elite, s.HealthScale, s.DamageScale, name);
            if (quarry == null) yield break;
            Blip qb = r.C.BlipOn(quarry.transform, EnemyColor, name, BlipKind.Enemy);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Heartbeat, 0.7f, 1f);
            yield return SayLines(r, s.Lines);
            string objective = "Kill the " + name + ".";
            string counter = name.ToUpperInvariant();
            r.C.Objective(objective);
            while (!QuestWorld.Gone(quarry))
            {
                if (VeilBlocked(r, s, objective)) { yield return null; continue; }
                r.C.Target(quarry.transform.position);
                float hp = quarry.Health != null ? quarry.Health.Health / Mathf.Max(1f, quarry.Health.MaxHealth) : 1f;
                if (Game.Hud != null) Game.Hud.SetCounter(counter, Mathf.CeilToInt(hp * 100f), 100);
                yield return null;
            }
            r.C.RemoveBlip(qb);
            if (Game.Hud != null) Game.Hud.ClearCounter();
            r.C.ClearTarget();
        }

        // ------------------------------------------------------------------ Scan

        /// <summary>Crystal energy the Third Eye needs to open (Abilities.SetThirdEye) and burns per second while open.</summary>
        private const float EyeOpenEnergy = 8f;
        private const float EyeDrainPerSecond = 12f;

        /// <summary>
        /// Inside the Inner World nothing refills the crystal bar, so a Scan step there could become impossible: while the
        /// Eye is closed and the bar is below what one reading needs, the guild tops it up (said once per step).
        /// </summary>
        private static bool KeepEyeEnergy(float need, bool told)
        {
            Abilities ab = Game.Abilities;
            if (ab == null || Game.Inner == null || !Game.Inner.IsInner || ab.ThirdEyeActive) return told;
            float floor = EyeOpenEnergy + EyeDrainPerSecond * (need + 1.5f);
            if (ab.Energy >= floor) return told;
            ab.AddEnergy(floor - ab.Energy);
            if (!told && Game.Hud != null) Game.Hud.Notify("Your crystal bar hums back to life: enough to open the Third Eye and read it.", 4f);
            return true;
        }

        private static IEnumerator Scan(QuestRun r, QuestStep s)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(s.Species);
            if (sp == null) yield break;
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            string name = !string.IsNullOrEmpty(s.Name) ? s.Name : sp.Name;
            float need = s.Seconds > 0f ? s.Seconds : QuestDefaults.ScanSeconds;
            bool knownBefore = Game.Compendium != null && Game.Compendium.IsScanned(sp.Id);
            string objective = Obj(s, "Scan the " + name + " with the Third Eye.");
            string reading = "Reading the " + name + "...";
            r.C.Objective(objective);
            if (Game.Hud != null) Game.Hud.Notify("Get close, keep it in view and open the Third Eye (tap Caps Lock or T, LB + RB on a controller).", 6f);
            CharacterBase target = null;
            Blip tb = null;
            float hold = 0f;
            bool toppedUp = false;
            while (true)
            {
                if (VeilBlocked(r, s, objective)) { yield return null; continue; }
                toppedUp = KeepEyeEnergy(need, toppedUp);
                if (QuestWorld.Gone(target))
                {
                    if (target != null && Game.Hud != null) Game.Hud.Notify("It died before the Eye could read it. Another one stirs nearby.", 4f);
                    r.C.RemoveBlip(tb);
                    Vector3 p = target != null ? QuestWorld.InRing(c, 10f, 20f, new System.Random(r.Salt + Time.frameCount)) : c;
                    target = r.Spawn(sp.Id, p, QuestWorld.YawToward(p, Game.PlayerPosition), false, s.Elite, s.HealthScale, 1f, name);
                    if (target == null) yield break;
                    tb = r.C.BlipOn(target.transform, MarkerColor, name, BlipKind.Objective);
                }
                r.C.Target(target.transform.position);
                if (!knownBefore && Game.Compendium != null && Game.Compendium.IsScanned(sp.Id)) break;
                bool eye = Game.Abilities != null && Game.Abilities.ThirdEyeActive;
                float d = MathUtil.FlatDistance(Game.PlayerPosition, target.transform.position);
                if (eye && d < 32f && OnScreen(target.transform.position + Vector3.up)) hold += Time.unscaledDeltaTime;
                else hold = Mathf.Max(0f, hold - Time.unscaledDeltaTime * 0.5f);
                if (hold > 0f && Game.Hud != null) Game.Hud.SetProgress(reading, hold / need);
                if (hold >= need) break;
                yield return null;
            }
            r.C.RemoveBlip(tb);
            r.C.ClearTarget();
            // A Kill step of the same species that follows fights this one first instead of spawning a twin.
            r.Scanned = target;
            r.ScannedSpecies = sp.Id;
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.8f, 1.3f);
            if (Game.Hud != null) Game.Hud.Notify("Scan complete: " + sp.Name + ". " + sp.Weak, 6f);
        }

        private static bool OnScreen(Vector3 p)
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null) return true;
            Vector3 v = cam.WorldToViewportPoint(p);
            return v.z > 0f && v.x > 0.12f && v.x < 0.88f && v.y > 0.1f && v.y < 0.9f;
        }
    }
}
