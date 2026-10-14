using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Breakable parts on a live creature (non-humanoid Creatures and humanoid creature NPCs alike): glowing crystal
    /// pieces on the part's bones, part health from CreaturePartsDb, and the health component's hook (OnHit) that finds
    /// the part a hit landed on and damages it (a Resonance Burst doubles it). A breaking part throws its crystal off
    /// with a shard burst and a crack, staggers the creature, drops its material, changes the fight (CreaturePartRules)
    /// and is written into the bestiary. Broken parts raise the loot-gun chance when the creature dies. The Third Eye
    /// marks intact parts (DrawOverlay).
    /// </summary>
    public sealed class CreatureParts : MonoBehaviour
    {
        /// <summary>Every live parts component (the overlay and the debug page walk it).</summary>
        public static readonly List<CreatureParts> All = new List<CreatureParts>();

        public CharacterBase Owner;
        public CreatureSpecies Species;
        public CreaturePartBook Book;

        private Creature _beast;
        private NpcController _npc;
        /// <summary>Per part: bones of the creature rig (indices) or humanoid bones (transforms) it covers.</summary>
        private readonly List<List<int>> _beastBones = new List<List<int>>();
        private readonly List<List<Transform>> _bones = new List<List<Transform>>();
        private readonly List<List<GameObject>> _pieces = new List<List<GameObject>>();
        private float _textAt;

        public static void ResetStatics()
        {
            All.Clear();
        }

        public PartEffect BrokenEffects { get { return Book != null ? Book.BrokenEffects : PartEffect.None; } }

        public bool Has(PartEffect e)
        {
            return Book != null && Book.Has(e);
        }

        // ================================================================== attaching

        /// <summary>Gives a built non-humanoid creature its species' parts (null when it has none).</summary>
        public static CreatureParts Attach(Creature c)
        {
            if (c == null || c.Species == null || c.CRig == null) return null;
            PartDef[] defs = CreaturePartsDb.For(c.Species.Id);
            if (defs.Length == 0) return null;
            CreatureParts p = c.gameObject.AddComponent<CreatureParts>();
            p.Owner = c;
            p._beast = c;
            p.Species = c.Species;
            p.Book = new CreaturePartBook(defs, c.Health.MaxHealth);
            for (int i = 0; i < defs.Length; i++)
            {
                List<int> bones = BeastBones(c.CRig.Body.Skeleton, defs[i].Anchor);
                p._beastBones.Add(bones);
                List<Transform> ts = new List<Transform>();
                for (int k = 0; k < bones.Count; k++) if (bones[k] >= 0 && bones[k] < c.CRig.Bones.Length) ts.Add(c.CRig.Bones[bones[k]]);
                p._bones.Add(ts);
            }
            c.Health.Parts = p;
            p.BuildPieces(Mathf.Clamp(c.Size, 0.15f, 8f));
            All.Add(p);
            return p;
        }

        /// <summary>Gives a humanoid creature NPC its species' parts (null when it has none).</summary>
        public static CreatureParts Attach(NpcController n)
        {
            if (n == null || n.CreatureSpecies == null || n.Rig == null) return null;
            PartDef[] defs = CreaturePartsDb.For(n.CreatureSpecies.Id);
            if (defs.Length == 0) return null;
            CreatureParts p = n.gameObject.AddComponent<CreatureParts>();
            p.Owner = n;
            p._npc = n;
            p.Species = n.CreatureSpecies;
            p.Book = new CreaturePartBook(defs, n.Health.MaxHealth);
            for (int i = 0; i < defs.Length; i++)
            {
                p._beastBones.Add(new List<int>());
                p._bones.Add(HumanBones(n.Rig, defs[i].Anchor));
            }
            n.Health.Parts = p;
            p.BuildPieces(Mathf.Max(0.5f, n.Rig.Scale));
            All.Add(p);
            return p;
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        /// <summary>The creature-rig bones an anchor covers; an anchor the body lacks falls back to the core.</summary>
        public static List<int> BeastBones(CreatureSkeleton sk, PartAnchor a)
        {
            List<int> b = new List<int>();
            switch (a)
            {
                case PartAnchor.Head:
                    b.Add(sk.Head);
                    b.Add(sk.Jaw);
                    if (sk.Neck.Length > 0) b.Add(sk.Neck[sk.Neck.Length - 1]);
                    break;
                case PartAnchor.Chest:
                    b.Add(sk.Chest);
                    if (sk.Neck.Length > 0) b.Add(sk.Neck[0]);
                    break;
                case PartAnchor.Back:
                    b.Add(sk.Root);
                    b.Add(sk.Pelvis);
                    b.Add(sk.Chest);
                    b.AddRange(sk.Spine);
                    b.AddRange(sk.Tail);
                    break;
                case PartAnchor.Tail:
                    b.AddRange(sk.Tail);
                    b.AddRange(sk.Tendrils);
                    break;
                case PartAnchor.ForeLimbs:
                case PartAnchor.HindLimbs:
                    for (int i = 0; i < sk.Legs.Length; i++)
                    {
                        LegChain l = sk.Legs[i];
                        if (l.Front != (a == PartAnchor.ForeLimbs)) continue;
                        b.Add(l.Upper);
                        b.Add(l.Lower);
                        b.Add(l.Foot);
                        b.Add(l.Toe);
                    }
                    break;
                case PartAnchor.Wings:
                    for (int i = 0; i < sk.Wings.Length; i++)
                    {
                        b.Add(sk.Wings[i].Arm);
                        b.Add(sk.Wings[i].Fore);
                        b.Add(sk.Wings[i].Hand);
                    }
                    break;
            }
            b.RemoveAll(x => x < 0);
            if (b.Count == 0)
            {
                if (sk.Chest >= 0) b.Add(sk.Chest);
                if (sk.Pelvis >= 0) b.Add(sk.Pelvis);
                if (sk.Root >= 0) b.Add(sk.Root);
            }
            return b;
        }

        /// <summary>The humanoid rig bones an anchor covers (arms for fore limbs, legs for hind limbs).</summary>
        private static List<Transform> HumanBones(HumanoidRig r, PartAnchor a)
        {
            List<Transform> b = new List<Transform>();
            switch (a)
            {
                case PartAnchor.Head: b.Add(r.Head); b.Add(r.Neck); break;
                case PartAnchor.Chest: b.Add(r.Chest); b.Add(r.Spine); break;
                case PartAnchor.Back: b.Add(r.Chest); b.Add(r.Spine); b.Add(r.Hips); break;
                case PartAnchor.ForeLimbs: b.Add(r.UpperArmL); b.Add(r.ForearmL); b.Add(r.HandL); b.Add(r.UpperArmR); b.Add(r.ForearmR); b.Add(r.HandR); break;
                case PartAnchor.HindLimbs: b.Add(r.ThighL); b.Add(r.ShinL); b.Add(r.FootL); b.Add(r.ThighR); b.Add(r.ShinR); b.Add(r.FootR); break;
                default: b.Add(r.Chest); break;
            }
            b.RemoveAll(x => x == null);
            return b;
        }

        // ================================================================== hits

        /// <summary>
        /// The health component's hook, after the creature's own damage rules: an intact shell cuts frontal hits,
        /// cracked armour adds damage, and the part under the hit takes its share. Returns the (scaled) damage.
        /// </summary>
        public float OnHit(DamageInfo info, float amount)
        {
            if (Book == null || Book.Count == 0 || amount <= 0f) return amount;
            float guard = Book.FrontGuard;
            if (guard < 1f && _beast != null && Frontal(info)) amount *= guard;
            amount *= CreaturePartRules.DamageTakenScale(Book.BrokenEffects);
            float partDamage = CreaturePartRules.PartDamage(amount, info.Type, info.Resonance);
            if (partDamage <= 0f) return amount;
            int part = PartAt(info.Point);
            if (part >= 0 && Book.Damage(part, partDamage)) Break(part, info);
            return amount;
        }

        private bool Frontal(DamageInfo info)
        {
            Vector3 from = info.Source != null ? info.Source.transform.position - transform.position : -info.Direction;
            from.y = 0f;
            if (from.sqrMagnitude < 0.01f) return true;
            return Vector3.Angle(transform.forward, from) < 70f;
        }

        /// <summary>The part a world point lies on (-1 when none): by the nearest hit volume (beasts) or bone (humanoids).</summary>
        public int PartAt(Vector3 point)
        {
            if (_beast != null)
            {
                if (_beast.CRig == null) return -1;
                int bone = _beast.CRig.NearestVolumeBone(point);
                if (bone < 0) return -1;
                for (int i = 0; i < _beastBones.Count; i++) if (!Book.Broken(i) && _beastBones[i].Contains(bone)) return i;
                return -1;
            }
            if (_npc == null || _npc.Rig == null) return -1;
            HumanoidRig r = _npc.Rig;
            // The nearest of the main bones decides; a hit nearest the hips or a bare limb lands on no part.
            Transform best = null;
            float bd = float.MaxValue;
            Transform[] bones = r.Bones;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                float d = (bones[i].position - point).sqrMagnitude;
                if (d < bd) { bd = d; best = bones[i]; }
            }
            if (best == null) return -1;
            for (int i = 0; i < _bones.Count; i++) if (!Book.Broken(i) && _bones[i].Contains(best)) return i;
            return -1;
        }

        /// <summary>World centre of a part (its first piece, else its first bone).</summary>
        public Vector3 PartCenter(int i)
        {
            List<GameObject> pcs = _pieces[i];
            for (int k = 0; k < pcs.Count; k++) if (pcs[k] != null) return pcs[k].transform.position;
            if (_bones[i].Count > 0 && _bones[i][0] != null) return _bones[i][0].position;
            return Owner != null ? Owner.ChestPosition : transform.position;
        }

        // ================================================================== breaking

        private void Break(int i, DamageInfo info)
        {
            PartDef def = Book.Defs[i];
            Vector3 at = PartCenter(i);
            List<GameObject> pcs = _pieces[i];
            for (int k = 0; k < pcs.Count; k++) if (pcs[k] != null) ArmorPlates.Fling(pcs[k], info.Direction);
            pcs.Clear();
            Color tint = Owner != null && Owner.Health != null ? Owner.Health.SurfaceTint : Color.cyan;
            float size = _beast != null ? _beast.Size : (_npc != null && _npc.Rig != null ? _npc.Rig.Scale : 1f);
            if (Game.Fx != null)
            {
                Game.Fx.Flash(at, tint, 4f, 5f + size, 0.25f);
                Game.Fx.CrystalBurst(at, tint, Mathf.Clamp(Mathf.RoundToInt(16f * size), 10, 50), Mathf.Clamp(size * 0.5f, 0.4f, 2.5f));
                if (Owner != null && Owner.Health != null && Owner.Health.Surface == ImpactSurface.Glass) Game.Fx.GlassBurst(at, tint, 24);
                else Game.Fx.RockBurst(at, tint, Mathf.Clamp(Mathf.RoundToInt(8f * size), 6, 30), Mathf.Clamp(size * 0.4f, 0.4f, 2f));
            }
            CombatSounds.Play(CombatSounds.Clip.PartBreak, at, 0.95f, Mathf.Clamp(1.25f - size * 0.08f, 0.7f, 1.3f), 0.8f, 120f);
            if (_beast != null)
            {
                ShrinkBones(def);
                _beast.OnPartBroken(def);
            }
            else if (_npc != null) _npc.OnCreaturePartBroken(def);
            if (Game.Cam != null && info.FromPlayer) Game.Cam.Shake(Mathf.Clamp(0.12f + size * 0.04f, 0.12f, 0.35f));
            if (!info.FromPlayer || Species == null) return;
            if (Game.Materials != null && !string.IsNullOrEmpty(def.Material)) Game.Materials.Add(def.Material, Mathf.Max(1, def.MaterialCount), true);
            if (Game.Compendium != null) Game.Compendium.MarkPartBroken(Species.Id, def.Id);
            if (Game.Hud != null && Time.unscaledTime - _textAt > 0.4f)
            {
                _textAt = Time.unscaledTime;
                Game.Hud.CombatText("Part broken: " + def.Name, new Color(1f, 0.8f, 0.35f));
            }
        }

        /// <summary>Torn wings and a snapped tail tip show on the body itself: the end bones shrink.</summary>
        private void ShrinkBones(PartDef def)
        {
            CreatureRig rig = _beast.CRig;
            if (rig == null) return;
            CreatureSkeleton sk = rig.Body.Skeleton;
            if (def.Anchor == PartAnchor.Wings)
            {
                for (int w = 0; w < sk.Wings.Length; w++)
                {
                    int h = sk.Wings[w].Hand;
                    if (h >= 0 && h < rig.Bones.Length) rig.Bones[h].localScale = new Vector3(0.55f, 0.55f, 0.55f);
                }
            }
            else if (def.Anchor == PartAnchor.Tail && sk.Tail.Length > 0)
            {
                int t = sk.Tail[sk.Tail.Length - 1];
                if (t >= 0 && t < rig.Bones.Length) rig.Bones[t].localScale = new Vector3(0.6f, 0.6f, 0.6f);
            }
        }

        /// <summary>Bonus loot when a creature with broken parts dies (CreatureDirector.ReportDeath calls this).</summary>
        public static void OnOwnerKilled(CharacterBase body, CreatureSpecies sp, Vector3 at)
        {
            if (body == null || sp == null || sp.Boss || body.Health == null || body.Health.Parts == null) return;
            CreaturePartBook book = body.Health.Parts.Book;
            if (book == null) return;
            float chance = CreaturePartRules.BonusLootChance(book.BrokenCount);
            if (chance <= 0f || UnityEngine.Random.value > chance) return;
            if (Game.Loot != null) Game.Loot.OnCreatureKilled(sp, true, at);
        }

        // ================================================================== looks

        private void BuildPieces(float size)
        {
            Color tint = Owner.Health != null ? Owner.Health.SurfaceTint : Color.cyan;
            Material crystal = Mats.Emissive(new Color(tint.r * 0.8f, tint.g * 0.8f, tint.b * 0.8f), 1.6f);
            for (int i = 0; i < Book.Count; i++)
            {
                List<GameObject> list = new List<GameObject>();
                List<Transform> show = DisplayBones(i, size);
                for (int k = 0; k < show.Count; k++) if (show[k] != null) list.Add(Shard(show[k], crystal, size, Book.Defs[i].Anchor, k));
                _pieces.Add(list);
            }
        }

        /// <summary>
        /// The bones that carry a part's crystal shards, chosen to read well and stay symmetric: the forearm and hand of
        /// each wing, the foot of each leg (claws, talons), the forearms or shins of a humanoid, else up to three spread
        /// along the part. Swarm-sized bodies carry a single shard so a swarm stays cheap to draw.
        /// </summary>
        private List<Transform> DisplayBones(int i, float size)
        {
            List<Transform> show = new List<Transform>();
            PartAnchor a = Book.Defs[i].Anchor;
            if (_beast != null && _beast.CRig != null)
            {
                CreatureSkeleton sk = _beast.CRig.Body.Skeleton;
                Transform[] bones = _beast.CRig.Bones;
                if (a == PartAnchor.Wings)
                {
                    for (int w = 0; w < sk.Wings.Length; w++)
                    {
                        AddBone(show, bones, sk.Wings[w].Fore);
                        AddBone(show, bones, sk.Wings[w].Hand);
                    }
                }
                else if (a == PartAnchor.ForeLimbs || a == PartAnchor.HindLimbs)
                {
                    for (int l = 0; l < sk.Legs.Length; l++) if (sk.Legs[l].Front == (a == PartAnchor.ForeLimbs)) AddBone(show, bones, sk.Legs[l].Foot);
                }
            }
            else if (_npc != null && _npc.Rig != null)
            {
                HumanoidRig r = _npc.Rig;
                if (a == PartAnchor.ForeLimbs) { show.Add(r.ForearmL); show.Add(r.ForearmR); }
                else if (a == PartAnchor.HindLimbs) { show.Add(r.ShinL); show.Add(r.ShinR); }
            }
            if (show.Count == 0)
            {
                List<Transform> all = _bones[i];
                int want = Mathf.Min(3, all.Count);
                for (int k = 0; k < want; k++) show.Add(all[Mathf.Min(all.Count - 1, k * all.Count / Mathf.Max(1, want))]);
            }
            show.RemoveAll(t => t == null);
            if (size < 0.4f && show.Count > 1) show.RemoveRange(1, show.Count - 1);
            return show;
        }

        private static void AddBone(List<Transform> into, Transform[] bones, int index)
        {
            if (index >= 0 && index < bones.Length && bones[index] != null) into.Add(bones[index]);
        }

        /// <summary>One glowing crystal shard on a bone (a rotated cube, sized to the body).</summary>
        private GameObject Shard(Transform bone, Material mat, float size, PartAnchor a, int n)
        {
            float s = _npc != null ? 0.11f * size : Mathf.Clamp(0.09f * size, 0.025f, 0.9f);
            GameObject root = new GameObject("Part Shard");
            root.layer = gameObject.layer;
            root.transform.SetParent(bone, false);
            Vector3 off;
            if (_npc != null)
            {
                switch (a)
                {
                    case PartAnchor.Head: off = new Vector3((n - 1) * 0.07f, 0.21f, 0f) * size; break;
                    case PartAnchor.Chest: off = new Vector3((n - 1) * 0.09f, 0.08f, 0.15f) * size; break;
                    case PartAnchor.HindLimbs: off = new Vector3(0f, -0.05f, 0.09f) * size; break;
                    default: off = new Vector3(0f, -0.1f, 0.06f) * size; break;
                }
            }
            else
            {
                float up = a == PartAnchor.HindLimbs || a == PartAnchor.ForeLimbs ? -0.02f : 0.05f;
                off = new Vector3(0f, up * size, (a == PartAnchor.Head ? 0.04f : 0f) * size);
            }
            root.transform.localPosition = off;
            root.transform.localRotation = Quaternion.Euler(35f + n * 20f, 45f + n * 30f, 15f);
            GameObject go = Shapes.MeshObject("Crystal", Shapes.CubeMesh, new[] { mat }, root.transform, false);
            go.layer = root.layer;
            go.transform.localScale = new Vector3(s * 0.6f, s * 1.5f, s * 0.6f);
            return root;
        }

        // ================================================================== Third Eye and debug

        /// <summary>HUD overlay: intact parts of scanned creatures (and of creatures near you while the Third Eye is open).</summary>
        public static void DrawOverlay(float w, float h)
        {
            if (All.Count == 0 || Game.Cam == null || Game.Cam.Cam == null || Game.Player == null) return;
            Camera cam = Game.Cam.Cam;
            bool eye = Game.Abilities != null && Game.Abilities.ThirdEyeActive;
            float now = Time.time;
            float k = UIKit.Scale;
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f);
            Vector3 pp = Game.PlayerPosition;
            for (int i = 0; i < All.Count; i++)
            {
                CreatureParts p = All[i];
                if (p == null || p.Owner == null || p.Owner.IsDead) continue;
                bool scanned = (p._beast != null && now < p._beast.HighlightUntil) || (p._npc != null && now < p._npc.CreatureHighlightUntil);
                if (!scanned && !(eye && (p.Owner.transform.position - pp).sqrMagnitude < 35f * 35f)) continue;
                for (int j = 0; j < p.Book.Count; j++)
                {
                    if (p.Book.Broken(j)) continue;
                    Vector3 sp = cam.WorldToScreenPoint(p.PartCenter(j));
                    if (sp.z <= 0.1f) continue;
                    Vector2 g = new Vector2(sp.x / k, (Screen.height - sp.y) / k);
                    if (g.x < -40f || g.x > w + 40f || g.y < -40f || g.y > h + 40f) continue;
                    float s = 18f * pulse;
                    Color c = new Color(0.45f, 0.95f, 1f, 0.9f);
                    UIKit.Tex(new Rect(g.x - s * 0.5f, g.y - s * 0.5f, s, s), UIKit.Ring, c);
                    float f = p.Book.Hp(j) / Mathf.Max(1f, p.Book.Max(j));
                    UIKit.Bar(new Rect(g.x - 24f, g.y + s * 0.5f + 2f, 48f, 3f), f, c, new Color(0f, 0f, 0f, 0.5f));
                    UIKit.Text(new Rect(g.x - 90f, g.y - s * 0.5f - 20f, 180f, 18f), p.Book.Defs[j].Name.ToUpper(), 13, c, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
            }
        }

        /// <summary>F2: breaks the first intact part of the nearest creature that has one.</summary>
        public static void DebugBreakNearest()
        {
            Vector3 pp = Game.PlayerPosition;
            CreatureParts best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                CreatureParts p = All[i];
                if (p == null || p.Owner == null || p.Owner.IsDead || p.Book.BrokenCount >= p.Book.Count) continue;
                float d = (p.Owner.transform.position - pp).sqrMagnitude;
                if (d < bd) { bd = d; best = p; }
            }
            if (best == null)
            {
                if (Game.Hud != null) Game.Hud.Notify("No creature with an intact part nearby.", 2f);
                return;
            }
            for (int j = 0; j < best.Book.Count; j++)
            {
                if (best.Book.Broken(j)) continue;
                DamageInfo info = DamageInfo.Make(0f, best.PartCenter(j), Vector3.up, Game.Player != null ? Game.Player.gameObject : null, DamageType.Bullet);
                best.Book.Damage(j, best.Book.Hp(j) + 1f);
                best.Break(j, info);
                return;
            }
        }
    }

    /// <summary>The bestiary's "Breakable parts" line for a species: the parts the player has broken, the rest hidden.</summary>
    public static class CreaturePartsUi
    {
        public static string BestiaryLine(CreatureSpecies sp, Compendium c)
        {
            if (sp == null) return "";
            PartDef[] defs = CreaturePartsDb.For(sp.Id);
            if (defs.Length == 0) return "None known.";
            string known = "";
            int n = 0;
            for (int i = 0; i < defs.Length; i++)
            {
                if (c == null || !c.IsPartBroken(sp.Id, defs[i].Id)) continue;
                n++;
                if (known.Length > 0) known += "   ";
                known += defs[i].Name + ": " + defs[i].Text;
            }
            string tail = n < defs.Length ? (defs.Length - n) + " more to find. The Third Eye marks intact parts; Resonance Bursts break them twice as fast." : "";
            if (n == 0) return "Break one in a fight to learn what it does (" + defs.Length + " to find). The Third Eye marks intact parts; Resonance Bursts break them twice as fast.";
            return known + (tail.Length > 0 ? "   " + tail : "");
        }
    }
}
