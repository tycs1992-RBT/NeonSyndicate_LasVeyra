using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Makes the city's points of interest usable: gun shops, hospitals and Mara's clinic, drive-in resprays,
    /// Jax Calder Customs, diners, bars, food carts and vending machines, convenience stores you can shop at or rob,
    /// ATMs, payphones, safehouses, clothing stores, casino slot machines and purchasable properties.
    /// </summary>
    public sealed class WorldServices : MonoBehaviour
    {
        private sealed class StoreState
        {
            public Poi Poi;
            public NpcController Clerk;
            public float RobbedUntil;
            public float Progress;
        }

        private readonly List<StoreState> _stores = new List<StoreState>();
        private readonly List<WorldMarker> _markers = new List<WorldMarker>();
        private float _storeTimer;
        private float _respraySeenCheck;

        public void Init(CityLayout layout)
        {
            foreach (Poi poi in layout.Pois)
            {
                switch (poi.Type)
                {
                    case PoiType.GunShop: Add(poi, 1.8f, "Press E to browse " + poi.Name, () => Game.Menus.OpenShop(ShopKind.Guns, poi.Name), true, new Color(1f, 0.55f, 0.15f)); break;
                    case PoiType.Hospital: Add(poi, 2f, "Press E for treatment ($250)", () => Treat(250, poi.Name), true, new Color(1f, 0.25f, 0.25f)); break;
                    case PoiType.Clinic:
                    {
                        Interactable clinic = Add(poi, 2f, "", () => Treat(ClinicFree ? 0 : 100, poi.Name), true, new Color(0.35f, 1f, 0.7f));
                        clinic.DynamicPrompt = () => ClinicFree ? "Press E for free treatment at Mara's clinic" : "Press E for treatment at Mara's clinic ($100)";
                        break;
                    }
                    case PoiType.Respray: AddDriveIn(poi, "Respray", () => Respray(poi.Name), new Color(0.3f, 0.8f, 1f)); break;
                    case PoiType.Garage:
                    {
                        Interactable garage = AddDriveIn(poi, "Jax Calder Customs", () => Game.Menus.OpenShop(ShopKind.Garage, poi.Name), new Color(0.95f, 0.85f, 0.25f));
                        // The shop stays shut during missions (deliveries to Jax would otherwise pop it open).
                        garage.Condition = () => Game.Missions == null || Game.Missions.Active == null;
                    }
                        break;
                    case PoiType.Diner: Add(poi, 1.8f, "Press E to eat at " + poi.Name + " ($40)", () => Eat(40, 80f, "You feel much better."), false, Color.white); break;
                    case PoiType.Bar: Add(poi, 1.8f, "Press E for a drink at " + poi.Name + " ($20)", () => Drink(), false, Color.white); break;
                    case PoiType.FoodCart: Add(poi, 1.6f, "Press E for street food ($10)", () => Eat(10, 30f, "Tasty."), false, Color.white); break;
                    case PoiType.Vending: Add(poi, 1.3f, "Press E for a can of Glow Cola ($5)", () => Eat(5, 15f, "Refreshing."), false, Color.white); break;
                    case PoiType.ConvenienceStore:
                        Add(poi, 1.8f, "Press E to buy snacks ($15)", () => Eat(15, 35f, "Snacks restore a little health."), false, Color.white);
                        _stores.Add(new StoreState { Poi = poi });
                        break;
                    case PoiType.ATM: Add(poi, 1.3f, "Press E to check your balance", () => Atm(), false, Color.white); break;
                    case PoiType.Payphone:
                    {
                        Interactable it = Add(poi, 1.4f, "Payphone", () => { if (Game.Activities != null) Game.Activities.AnswerPayphone(poi); }, false, Color.white);
                        it.DynamicPrompt = () => Game.Activities != null && Game.Activities.PayphoneRinging(poi) ? "Press E to answer the ringing payphone" : "Payphone (no messages)";
                        break;
                    }
                    case PoiType.Safehouse:
                    case PoiType.Motel:
                        if (poi.Type == PoiType.Motel) break;
                        Add(poi, 2f, "Press E to use your safehouse (save, sleep, stash)", () =>
                        {
                            if (Game.Root != null) Game.Root.MarkSafehouse(poi);
                            Game.Menus.OpenSafehouse(poi.Name);
                        }, true, new Color(0.3f, 1f, 0.45f));
                        break;
                    case PoiType.Property:
                    {
                        PropertyDef def = Economy.FindProperty(poi.Name);
                        if (def == null) break;
                        Interactable it = Add(poi, 2f, "", () => { if (Game.Economy != null) Game.Economy.BuyProperty(poi.Name); }, true, new Color(0.3f, 1f, 0.5f));
                        it.DynamicPrompt = () => Game.Economy != null && Game.Economy.OwnedProperties.Contains(poi.Name)
                            ? poi.Name + " (owned) earns " + MathUtil.Money(def.DailyIncome) + " a day"
                            : "Press E to buy " + poi.Name + " for " + MathUtil.Money(def.Price) + " (" + MathUtil.Money(def.DailyIncome) + "/day)";
                        break;
                    }
                    case PoiType.Clothing: Add(poi, 1.8f, "Press E to buy a new outfit at " + poi.Name + " ($150)", () => Game.Menus.OpenShop(ShopKind.Clothing, poi.Name), true, new Color(1f, 0.45f, 0.8f)); break;
                    case PoiType.Slots: Add(poi, 1.6f, "Press E to play the slots ($100 a spin)", () => Game.Menus.OpenSlots(poi.Name), true, new Color(1f, 0.8f, 0.2f)); break;
                    case PoiType.Casino: Add(poi, 2f, poi.Name + " (slots inside the lobby)", () => { }, false, Color.white); break;
                }
                AddServiceBlip(poi);
            }
        }

        /// <summary>Map and minimap icons for the places players look for (letters match the pause-map legend).</summary>
        private void AddServiceBlip(Poi poi)
        {
            string letter = null;
            Color c = Color.white;
            switch (poi.Type)
            {
                case PoiType.GunShop: letter = "G"; c = new Color(1f, 0.5f, 0.19f); break;
                case PoiType.Hospital: letter = "H"; c = new Color(1f, 0.31f, 0.31f); break;
                case PoiType.Clinic: letter = "+"; c = new Color(0.38f, 1f, 0.7f); break;
                case PoiType.Respray: letter = "R"; c = new Color(0.31f, 0.88f, 1f); break;
                case PoiType.Garage: letter = "J"; c = new Color(1f, 0.88f, 0.38f); break;
                case PoiType.Safehouse: letter = "S"; c = new Color(0.38f, 1f, 0.5f); break;
                case PoiType.Clothing: letter = "T"; c = new Color(1f, 0.44f, 0.82f); break;
                case PoiType.Slots: letter = "7"; c = new Color(1f, 0.82f, 0.25f); break;
                case PoiType.Police: letter = "*"; c = new Color(0.35f, 0.55f, 1f); break;
                case PoiType.Property:
                    if (Economy.FindProperty(poi.Name) == null) return;
                    letter = "$";
                    c = new Color(0.38f, 1f, 0.5f);
                    break;
            }
            if (letter == null) return;
            Blip b = Blip.Add(BlipKind.Service, null, PedManager.Ground(poi.Position), c, letter, poi.Name, false);
            b.Size = 0.95f;
            _blips.Add(b);
            if (poi.Type == PoiType.Property) _propertyBlips[b] = poi.Name;
        }

        private readonly List<Blip> _blips = new List<Blip>();
        private readonly Dictionary<Blip, string> _propertyBlips = new Dictionary<Blip, string>();
        private float _blipTimer;

        /// <summary>Owned properties show with a filled label so players can tell what they already bought.</summary>
        private void RefreshPropertyBlips()
        {
            if (Game.Economy == null) return;
            foreach (KeyValuePair<Blip, string> kv in _propertyBlips)
            {
                bool owned = Game.Economy.OwnedProperties.Contains(kv.Value);
                kv.Key.Label = kv.Value + (owned ? " (owned)" : " (for sale)");
                kv.Key.Color = owned ? new Color(0.25f, 0.75f, 0.35f) : new Color(0.38f, 1f, 0.5f);
            }
        }

        private Interactable Add(Poi poi, float radius, string prompt, System.Action use, bool marker, Color color)
        {
            Vector3 pos = PedManager.Ground(poi.Position);
            Interactable it = Interactable.Create(poi.Type + " " + poi.Name, pos, radius, prompt, use, transform);
            if (marker)
            {
                WorldMarker m = WorldMarker.Create(pos, color, 0.6f, it.transform, false);
                _markers.Add(m);
            }
            return it;
        }

        private static bool ClinicFree
        {
            get { return (Game.Root != null && Game.Root.FreeRoam) || (Game.Missions != null && Game.Missions.StoryProgress >= 1); }
        }

        /// <summary>Forgets store robbery cooldowns (new game, load).</summary>
        public void ResetAll()
        {
            foreach (StoreState st in _stores)
            {
                st.RobbedUntil = 0f;
                st.Progress = 0f;
            }
        }

        private Interactable AddDriveIn(Poi poi, string name, System.Action use, Color color)
        {
            Vector3 pos = PedManager.Ground(poi.Position + poi.Facing * -2f);
            Interactable it = Interactable.Create(name + " " + poi.Name, pos, 4.2f, name, use, transform);
            it.OnFoot = false;
            it.InVehicle = true;
            it.AutoTrigger = true;
            it.HeightTolerance = 3f;
            WorldMarker m = WorldMarker.Create(pos, color, 2.6f, it.transform, true);
            _markers.Add(m);
            // On foot hint.
            Interactable hint = Interactable.Create(name + " hint", pos, 4f, name + ": drive a vehicle in here", () => { }, transform);
            hint.OnFoot = true;
            return it;
        }

        // ------------------------------------------------------------------ services

        private void Treat(int price, string where)
        {
            PlayerController p = Game.Player;
            if (p.Health.Health >= p.Health.MaxHealth - 1f && p.Health.Burning <= 0f)
            {
                Game.Hud.Notify("You're already in good health.", 2f);
                return;
            }
            if (price > 0 && !Game.Economy.Spend(price, "Treatment")) return;
            p.Health.Heal(p.Health.MaxHealth);
            p.Health.Burning = 0f;
            Game.Audio.Play2D(SfxId.HealthPickup, 0.7f);
            Game.Hud.Notify(where + ": fully healed" + (price > 0 ? " (" + MathUtil.Money(price) + ")" : ", on the house") + ".", 3f);
        }

        private void Eat(int price, float heal, string msg)
        {
            PlayerController p = Game.Player;
            if (!Game.Economy.Spend(price, "Food")) return;
            p.Health.Heal(heal);
            Game.Audio.Play2D(SfxId.ItemPickup, 0.5f);
            Game.Hud.Notify(msg + " (+" + Mathf.RoundToInt(heal) + " health)", 2.5f);
        }

        private void Drink()
        {
            if (!Game.Economy.Spend(20, "Drink")) return;
            Game.Player.Health.Heal(10f);
            Game.Hud.Notify("The Rook's special. Everything feels slightly sideways.", 3f);
            if (Game.Cam != null) Game.Cam.Shake(0.2f);
        }

        private void Atm()
        {
            Economy e = Game.Economy;
            Game.Hud.Notify("Las Veyra Credit Union. Cash: " + MathUtil.Money(e.Money) + ". Property income: " + MathUtil.Money(e.DailyIncome) + "/day.", 4f);
            Game.Audio.Play2D(SfxId.Beep, 0.5f);
        }

        private void Respray(string where)
        {
            PlayerController p = Game.Player;
            if (p == null || !p.InVehicle || p.Seat != 0) return;
            Vehicle v = p.CurrentVehicle;
            if (v.IsPolice)
            {
                Game.Hud.Notify("We don't touch police cars. Get lost.", 3f);
                return;
            }
            int price = 500;
            bool wanted = Game.Wanted != null && Game.Wanted.Stars > 0;
            if (wanted && Game.Wanted.PoliceSeePlayer)
            {
                Game.Hud.Notify("The cops are watching. Lose them first.", 3f);
                return;
            }
            if (!Game.Economy.Spend(price, "Respray")) return;
            Color c = VehicleDb.All[0].Paints.Length > 0 ? Mats.CarPaints[Random.Range(0, Mats.CarPaints.Length)] : Color.gray;
            for (int i = 0; i < 6 && ColorClose(c, v.PaintColor); i++) c = Mats.CarPaints[Random.Range(0, Mats.CarPaints.Length)];
            v.Respray(c);
            v.Repair();
            if (wanted) Game.Wanted.ClearAll();
            if (Game.Stats != null) Game.Stats.Add(StatId.CarsResprayed, 1);
            Game.Audio.Play2D(SfxId.WeaponSwitch, 0.8f, 0.7f);
            Game.Hud.Banner("NEON RESPRAY", "New paint, fresh plates" + (wanted ? " and no more heat." : ".") + "  -" + MathUtil.Money(price), new Color(0.3f, 0.8f, 1f), 3f);
        }

        private static bool ColorClose(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.3f;
        }

        // ------------------------------------------------------------------ store robberies

        private void Update()
        {
            _blipTimer -= Time.unscaledDeltaTime;
            if (_blipTimer <= 0f)
            {
                _blipTimer = 1f;
                RefreshPropertyBlips();
            }
            if (Game.Player == null) return;
            _storeTimer -= Time.deltaTime;
            Vector3 pp = Game.PlayerPosition;
            if (_storeTimer <= 0f)
            {
                _storeTimer = 1f;
                foreach (StoreState s in _stores)
                {
                    float d = MathUtil.FlatDistance(s.Poi.Position, pp);
                    if (d < 45f && s.Clerk == null && Time.time > s.RobbedUntil)
                    {
                        Vector3 pos = PedManager.Ground(s.Poi.Position - s.Poi.Facing * 1.2f);
                        s.Clerk = NpcController.Spawn(pos, MathUtil.YawOf(s.Poi.Facing), NpcRole.Clerk, Appearance.Random(new System.Random(s.Poi.Id * 7 + 3)), -1);
                        s.Clerk.Persistent = true;
                        s.Clerk.GuardAt(pos);
                        s.Clerk.DropsLoot = false;
                        s.Clerk.transform.SetParent(transform, true);
                    }
                    else if (d > 80f && s.Clerk != null)
                    {
                        Destroy(s.Clerk.gameObject);
                        s.Clerk = null;
                        s.Progress = 0f;
                    }
                }
            }
            PlayerController p = Game.Player;
            foreach (StoreState s in _stores)
            {
                if (s.Clerk == null || s.Clerk.IsDead || Time.time < s.RobbedUntil) continue;
                bool aimingAtClerk = p.Aiming && p.Weapons.Current.IsGun && p.AimTarget == s.Clerk && MathUtil.FlatDistance(pp, s.Clerk.transform.position) < 9f;
                if (aimingAtClerk)
                {
                    if (s.Progress <= 0f)
                    {
                        Game.Hud.Notify("Keep your gun on the clerk while they empty the register.", 3f);
                        if (Game.Wanted != null) Game.Wanted.ReportCrime(CrimeType.Robbery, s.Poi.Position, s.Clerk.gameObject);
                    }
                    s.Progress += Time.deltaTime / 6f;
                    s.Clerk.SetState(NpcState.HandsUp);
                    Game.Hud.SetProgress("Robbing " + s.Poi.Name, s.Progress);
                    if (s.Progress >= 1f)
                    {
                        int cash = Random.Range(900, 2400);
                        Game.Pickups.SpawnCash(s.Clerk.transform.position + s.Poi.Facing * 1.2f, cash);
                        s.RobbedUntil = Time.time + 600f;
                        s.Progress = 0f;
                        if (Game.Stats != null) Game.Stats.Add(StatId.StoresRobbed, 1);
                        Game.Hud.Notify("Grab the cash and get out of there!", 3f);
                        if (Game.Wanted != null && Game.Wanted.Stars < 2) Game.Wanted.SetStars(2);
                    }
                }
                else if (s.Progress > 0f)
                {
                    s.Progress = Mathf.Max(0f, s.Progress - Time.deltaTime * 0.25f);
                    if (s.Progress <= 0f) s.Clerk.SetState(NpcState.Idle);
                }
            }
        }
    }
}
