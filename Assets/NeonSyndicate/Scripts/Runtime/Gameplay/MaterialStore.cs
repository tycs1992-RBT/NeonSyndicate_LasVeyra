using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One crafting material: what creatures drop, rifts leave behind and the Forge turns into gear.</summary>
    public sealed class MaterialDef
    {
        public string Id;
        public string Name;
        /// <summary>0 common, 1 uncommon, 2 rare, 3 mythic (boss hearts and cores).</summary>
        public int Rarity;
        public Element Element;
        /// <summary>What a Forgewright pays for one (cash).</summary>
        public int Value;
        public string Description;

        public Color RarityColor
        {
            get
            {
                switch (Rarity)
                {
                    case 1: return new Color(0.35f, 0.85f, 1f);
                    case 2: return new Color(0.75f, 0.45f, 1f);
                    case 3: return new Color(1f, 0.7f, 0.25f);
                    default: return new Color(0.85f, 0.85f, 0.85f);
                }
            }
        }

        public string RarityName
        {
            get
            {
                switch (Rarity)
                {
                    case 1: return "Uncommon";
                    case 2: return "Rare";
                    case 3: return "Mythic";
                    default: return "Common";
                }
            }
        }
    }

    /// <summary>The material catalog. Ids are stable (saves, loot tables and recipes refer to them).</summary>
    public static class MaterialDb
    {
        public static readonly MaterialDef[] All =
        {
            M("glass_chitin", "Glass Chitin", 0, Element.Volt, 18, "Brittle shell plates from Glass Mites. They hum when struck."),
            M("mite_dust", "Mite Dust", 0, Element.None, 10, "Glittering powder a mite swarm leaves behind. Binds crystal to steel."),
            M("jackal_fang", "Jackal Fang", 0, Element.None, 22, "A Neon Jackal's fang, glowing faintly at the root."),
            M("neon_hide", "Neon Hide", 1, Element.Volt, 60, "Jackal hide threaded with living light. Prized by street tailors."),
            M("shard_chunk", "Shard Chunk", 0, Element.None, 25, "A fist-sized lump of golem crystal, still warm."),
            M("golem_core", "Golem Core", 2, Element.Volt, 220, "The beating crystal heart of a golem. It keeps beating."),
            M("shade_essence", "Shade Essence", 1, Element.Void, 70, "Bottled darkness from a Hollow Shade. Cold to the touch."),
            M("hollow_essence", "Hollow Essence", 0, Element.Void, 30, "What is left when a Hollow comes apart: smoke that will not rise."),
            M("void_thread", "Void Thread", 1, Element.Void, 80, "A strand of the Inner World's fabric. It pulls toward the Veil."),
            M("mirror_glass", "Mirror Glass", 2, Element.Void, 200, "A sliver of a Mirror Wraith. Your reflection in it is a moment late."),
            M("basilisk_scale", "Basilisk Scale", 1, Element.None, 75, "An armoured scale from a Crystal Basilisk's back."),
            M("crown_gem", "Basilisk Crown Gem", 2, Element.Void, 260, "The gem from a basilisk's crown. Do not stare into it for long."),
            M("wyvern_membrane", "Wyvern Membrane", 1, Element.Void, 90, "Violet wing skin veined with light. Light as silk, tough as leather."),
            M("frost_fang", "Frost Fang", 1, Element.Frost, 70, "A Frost Stalker's fang. Frost spreads from it across your palm."),
            M("rime_pelt", "Rime Pelt", 0, Element.Frost, 35, "Pale, ice-crusted pelt from the snowfields."),
            M("rime_feather", "Rime Feather", 1, Element.Frost, 85, "A flight feather from a Rime Griffin. It never melts."),
            M("griffin_heart", "Griffin Heart", 2, Element.None, 280, "A griffin's heart crystal. Wardens say it remembers the sky."),
            M("frost_essence", "Frost Essence", 0, Element.Frost, 32, "A Snow Hollow's cold, condensed into blue droplets."),
            M("glacier_shard", "Glacier Shard", 2, Element.Frost, 240, "Ancient ice cut from the Glacier Titan. It cools forge fires."),
            M("titan_heart", "Titan Heart", 3, Element.Frost, 1200, "The frozen heart of the Glacier Titan. Mythic forging core."),
            M("wyrm_scale", "Wyrm Scale", 1, Element.Ember, 95, "A ring plate from a Dune Wyrm, scoured smooth by sand."),
            M("ember_gland", "Ember Gland", 2, Element.Ember, 260, "The furnace gland from a wyrm's throat. Handle with tongs."),
            M("drake_plate", "Drake Plate", 2, Element.Ember, 300, "Quartz armour plate from a Quartz Drake."),
            M("quartz_heart", "Quartz Heart", 3, Element.Ember, 1400, "A Quartz Drake's heart: rose quartz holding a captive sunset."),
            M("sun_feather", "Sun Feather", 1, Element.Ember, 85, "A gold feather from a Sun Griffin, warm as noon."),
            M("sand_core", "Sand Core", 1, Element.None, 110, "The packed, glowing core of a Sand Golem."),
            M("mirage_heart", "Mirage Heart", 3, Element.Void, 1500, "The Mirage Djinn's heart. It shows each holder a different thing."),
            M("foreman_core", "Foreman Core", 3, Element.Volt, 1300, "The Shard Golem Foreman's crowned core. It still gives orders."),
            M("queen_mirror", "Queen's Mirror", 3, Element.Void, 1600, "The Mirror Wraith Queen's heart-glass. Nothing reflects in it at all."),
            M("rift_shard", "Rift Shard", 1, Element.Void, 90, "A shard left where a rift closed. Forgewrights pay well for these."),
            // Weapon cores (loot guns): elemental cores infuse an element at the Forge's ARMORY; prism and apex cores raise rarity.
            M("core_volt", "Volt Core", 2, Element.Volt, 180, "A weapon core humming with stored lightning. Infuses a gun with Volt."),
            M("core_ember", "Ember Core", 2, Element.Ember, 180, "A weapon core that never cools. Infuses a gun with Ember."),
            M("core_frost", "Frost Core", 2, Element.Frost, 180, "A weapon core rimed with frost. Infuses a gun with Frost."),
            M("core_void", "Void Core", 2, Element.Void, 180, "A weapon core that swallows light. Infuses a gun with Void."),
            M("core_prism", "Prism Core", 2, Element.None, 450, "A many-faced weapon core. The Forge uses it to raise a gun's rarity."),
            M("core_apex", "Apex Core", 3, Element.None, 1800, "The rarest weapon core, dropped by bosses and now and then by champions. Raises guns to Legendary and beyond.")
        };

        private static Dictionary<string, MaterialDef> _byId;

        /// <summary>Drops the lookup cache (rebuilt on the next Get), for Enter Play Mode without domain reload.</summary>
        public static void ResetStatics()
        {
            _byId = null;
        }

        private static MaterialDef M(string id, string name, int rarity, Element e, int value, string desc)
        {
            return new MaterialDef { Id = id, Name = name, Rarity = rarity, Element = e, Value = value, Description = desc };
        }

        public static MaterialDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null)
            {
                _byId = new Dictionary<string, MaterialDef>();
                foreach (MaterialDef d in All) _byId[d.Id] = d;
            }
            MaterialDef m;
            return _byId.TryGetValue(id, out m) ? m : null;
        }

        public static string NameOf(string id)
        {
            MaterialDef d = Get(id);
            return d != null ? d.Name : id;
        }
    }

    [Serializable]
    public sealed class MaterialSave
    {
        public List<string> Ids = new List<string>();
        public List<int> Counts = new List<int>();
    }

    /// <summary>The player's material satchel (Game.Materials). Saves with the game.</summary>
    public sealed class MaterialStore : MonoBehaviour, IWorldSystem
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly List<KeyValuePair<MaterialDef, int>> _held = new List<KeyValuePair<MaterialDef, int>>();
        private bool _heldDirty = true;

        /// <summary>Raised after any change: material id and the new count.</summary>
        public event Action<string, int> Changed;

        public void Init()
        {
            WorldSystems.Register(this);
        }

        public int Count(string id)
        {
            int n;
            return id != null && _counts.TryGetValue(id, out n) ? n : 0;
        }

        public bool Has(string id, int amount)
        {
            return Count(id) >= amount;
        }

        /// <summary>
        /// Every material currently held, in catalog order. The list is kept and rebuilt only when the satchel changes, so
        /// menus can ask every frame without allocating: read it or copy it, but do not change it or hold on to it.
        /// </summary>
        public List<KeyValuePair<MaterialDef, int>> Held()
        {
            if (!_heldDirty) return _held;
            _heldDirty = false;
            _held.Clear();
            foreach (MaterialDef d in MaterialDb.All)
            {
                int n = Count(d.Id);
                if (n > 0) _held.Add(new KeyValuePair<MaterialDef, int>(d, n));
            }
            return _held;
        }

        public void Add(string id, int amount, bool notify)
        {
            if (amount <= 0 || MaterialDb.Get(id) == null) return;
            int n = Count(id) + amount;
            _counts[id] = Mathf.Min(n, 9999);
            if (notify && Game.Hud != null)
            {
                MaterialDef d = MaterialDb.Get(id);
                Game.Hud.CombatText("+" + amount + " " + d.Name, d.RarityColor);
            }
            Raise(id);
        }

        /// <summary>Spends a set of materials only if all of them are available. Returns false and spends nothing otherwise.</summary>
        public bool Spend(IList<KeyValuePair<string, int>> cost)
        {
            if (cost == null) return true;
            foreach (KeyValuePair<string, int> c in cost) if (Count(c.Key) < c.Value) return false;
            foreach (KeyValuePair<string, int> c in cost)
            {
                _counts[c.Key] = Count(c.Key) - c.Value;
                Raise(c.Key);
            }
            return true;
        }

        public bool Remove(string id, int amount)
        {
            if (Count(id) < amount) return false;
            _counts[id] = Count(id) - amount;
            Raise(id);
            return true;
        }

        private void Raise(string id)
        {
            _heldDirty = true;
            if (Changed == null) return;
            try { Changed(id, Count(id)); }
            catch (Exception e) { Log.Exception("Material changed handler", e); }
        }

        public void ResetAll()
        {
            _counts.Clear();
            _heldDirty = true;
        }

        public void Capture(SaveData d)
        {
            d.Materials = new MaterialSave();
            foreach (KeyValuePair<string, int> kv in _counts)
            {
                if (kv.Value <= 0) continue;
                d.Materials.Ids.Add(kv.Key);
                d.Materials.Counts.Add(kv.Value);
            }
        }

        public void Restore(SaveData d)
        {
            _counts.Clear();
            _heldDirty = true;
            if (d.Materials == null) return;
            int n = Mathf.Min(d.Materials.Ids.Count, d.Materials.Counts.Count);
            for (int i = 0; i < n; i++)
            {
                if (MaterialDb.Get(d.Materials.Ids[i]) != null && d.Materials.Counts[i] > 0) _counts[d.Materials.Ids[i]] = d.Materials.Counts[i];
            }
        }
    }
}
