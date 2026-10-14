using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed class PropertyDef
    {
        public string Name;
        public int Price;
        public int DailyIncome;
        public string Blurb;
    }

    /// <summary>Cash, spending, property purchases and the daily property payout (at 06:00 in-game).</summary>
    public sealed class Economy : MonoBehaviour
    {
        public int Money = 1500;
        public readonly HashSet<string> OwnedProperties = new HashSet<string>();
        public int PendingIncome;
        private float _displayMoney;

        public static readonly PropertyDef[] Properties =
        {
            new PropertyDef { Name = "Circuit Ward Night Market", Price = 60000, DailyIncome = 2200, Blurb = "Stalls, neon and no questions." },
            new PropertyDef { Name = "Old Mile Pawn and Loan", Price = 45000, DailyIncome = 1600, Blurb = "Everything has a price on the Old Mile." },
            new PropertyDef { Name = "Glass Pier Records", Price = 38000, DailyIncome = 1300, Blurb = "Vinyl by day, back room by night." },
            new PropertyDef { Name = "Rust Canal Boathouse", Price = 52000, DailyIncome = 1900, Blurb = "Docks for things that should not float." },
            new PropertyDef { Name = "Battery Yard Scrap Co.", Price = 30000, DailyIncome = 1100, Blurb = "Crushed cars, crushed evidence." },
            new PropertyDef { Name = "Signal Hill Radio Shack", Price = 42000, DailyIncome = 1500, Blurb = "Pirate radio and police scanners." },
            new PropertyDef { Name = "Lotus Row Noodle House", Price = 34000, DailyIncome = 1250, Blurb = "Best noodles in Brass Projects." },
            new PropertyDef { Name = "Neon Mesa Car Wash", Price = 48000, DailyIncome = 1800, Blurb = "Washes cars. Also money." },
            new PropertyDef { Name = "Sunset Exchange Lounge", Price = 95000, DailyIncome = 3400, Blurb = "High rollers, higher margins." },
            new PropertyDef { Name = "Palm Terminal Hangar 3", Price = 120000, DailyIncome = 4200, Blurb = "Private flights, private cargo." },
        };

        public static PropertyDef FindProperty(string name)
        {
            foreach (PropertyDef p in Properties) if (p.Name == name) return p;
            return null;
        }

        public float DisplayMoney { get { return _displayMoney; } }

        public void Earn(int amount, string reason)
        {
            if (amount <= 0) return;
            // Street Smarts and City Royalty (SkillMods.CashGain); not on the casino, cheats or Forge salvage.
            amount = ProgressionHooks.CashGain(amount, reason);
            Money += amount;
            if (Game.Stats != null) Game.Stats.Add(StatId.MoneyEarned, amount);
            if (Game.Hud != null) Game.Hud.MoneyPopup(amount);
        }

        public bool CanAfford(int amount)
        {
            return Money >= amount;
        }

        public bool Spend(int amount, string reason)
        {
            if (amount <= 0) return true;
            if (Money < amount)
            {
                if (Game.Hud != null) Game.Hud.Notify("You can't afford that (" + MathUtil.Money(amount) + ").", 2.5f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIBack, 0.5f);
                return false;
            }
            Money -= amount;
            if (Game.Stats != null) Game.Stats.Add(StatId.MoneySpent, amount);
            if (Game.Hud != null) Game.Hud.MoneyPopup(-amount);
            return true;
        }

        /// <summary>Takes up to 'amount' (hospital bills, bribes). Never goes negative.</summary>
        public int Charge(int amount)
        {
            int take = Mathf.Min(Money, Mathf.Max(0, amount));
            Money -= take;
            if (take > 0 && Game.Hud != null) Game.Hud.MoneyPopup(-take);
            return take;
        }

        public bool BuyProperty(string name)
        {
            PropertyDef p = FindProperty(name);
            if (p == null || OwnedProperties.Contains(name)) return false;
            if (!Spend(p.Price, "Property")) return false;
            OwnedProperties.Add(name);
            if (Game.Stats != null) Game.Stats.Set(StatId.PropertiesOwned, OwnedProperties.Count);
            if (Game.Hud != null) Game.Hud.Banner("PROPERTY PURCHASED", p.Name + " now pays " + MathUtil.Money(p.DailyIncome) + " a day.", new Color(0.3f, 1f, 0.5f), 4f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CashPickup, 0.8f);
            return true;
        }

        public int DailyIncome
        {
            get
            {
                int total = 0;
                foreach (string n in OwnedProperties)
                {
                    PropertyDef p = FindProperty(n);
                    if (p != null) total += p.DailyIncome;
                }
                return total;
            }
        }

        public void OnNewDay()
        {
            int income = DailyIncome;
            if (income <= 0) return;
            Earn(income, "Property income");
            if (Game.Hud != null) Game.Hud.Notify("Your properties earned " + MathUtil.Money(income) + " today.", 4f);
        }

        private void Update()
        {
            _displayMoney = Mathf.MoveTowards(_displayMoney, Money, Mathf.Max(30f, Mathf.Abs(Money - _displayMoney) * 4f) * Time.unscaledDeltaTime);
        }

        public void SnapDisplay()
        {
            _displayMoney = Money;
        }
    }
}
