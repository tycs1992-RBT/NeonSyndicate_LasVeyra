using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>Where and when the director is looking for an event (filled from the live game, or by hand in tests).</summary>
    public struct EventSituation
    {
        public District District;
        public bool Inner;
        public float Hours;
        public Weather Weather;
        public int Story;
        public bool FreeRoam;
        public bool ThirdEye;
        /// <summary>The director's clock in seconds (cooldowns are measured on it).</summary>
        public float Now;
    }

    /// <summary>
    /// Which world events may appear right now, and a seeded weighted pick among them. Plain C# with no engine calls,
    /// so the same rules run in the game, the EditMode tests and the offline harness.
    /// </summary>
    public static class EventSelector
    {
        /// <summary>True when hours lies in the window from min to max (wrapping past midnight). Both 0 means any time.</summary>
        public static bool HourInWindow(float hours, float min, float max)
        {
            if (min == 0f && max == 0f) return true;
            hours = ((hours % 24f) + 24f) % 24f;
            if (min <= max) return hours >= min && hours < max;
            return hours >= min || hours < max;
        }

        /// <summary>True when the template may appear in the district (empty list means every city district).</summary>
        public static bool DistrictAllowed(EventTemplate t, District d)
        {
            bool wild = CityLayout.IsWilderness(d);
            if (t.Districts == null || t.Districts.Count == 0) return !wild;
            foreach (string s in t.Districts)
            {
                if (s == "Any") return true;
                if (s == "City" && !wild) return true;
                if (s == "Wild" && wild) return true;
                if (s == d.ToString()) return true;
            }
            return false;
        }

        /// <summary>Outer-only (the default), Inner-only, or Any.</summary>
        public static bool VeilAllowed(EventTemplate t, bool inner)
        {
            if (t.Veil == "Any") return true;
            if (t.Veil == "Inner") return inner;
            return !inner;
        }

        public static bool WeatherAllowed(EventTemplate t, Weather w)
        {
            if (t.Weathers == null || t.Weathers.Count == 0) return true;
            string name = w.ToString();
            foreach (string s in t.Weathers) if (s == name) return true;
            return false;
        }

        public static float CooldownOf(EventTemplate t)
        {
            return t.Cooldown > 0f ? t.Cooldown : QuestDefaults.EventCooldown;
        }

        public static float WeightOf(EventTemplate t)
        {
            return t.Weight > 0f ? t.Weight : 1f;
        }

        /// <summary>
        /// Null when the template may appear in this situation, otherwise the first reason it may not.
        /// lastFired gives the director-clock time a template last appeared (negative: never); active holds the ids
        /// running now (a template never runs twice at once).
        /// </summary>
        public static string WhyNot(EventTemplate t, EventSituation s, Func<string, float> lastFired, ICollection<string> active)
        {
            if (t == null) return "missing";
            if (t.ManualOnly) return "start at its territory terminal";
            if (!VeilAllowed(t, s.Inner)) return s.Inner ? "outside the Inner World only" : "inside the Inner World only";
            if (!DistrictAllowed(t, s.District)) return "not in " + CityLayout.DistrictName(s.District);
            if (!HourInWindow(s.Hours, t.HourMin, t.HourMax)) return "wrong time of day";
            if (!WeatherAllowed(t, s.Weather)) return "wrong weather";
            if (t.Story > 0 && !s.FreeRoam && s.Story < t.Story) return "later in the story";
            if (t.ThirdEye && !s.ThirdEye) return "needs the Third Eye";
            if (active != null && active.Contains(t.Id)) return "already running";
            if (lastFired != null)
            {
                float last = lastFired(t.Id);
                if (last >= 0f && s.Now - last < CooldownOf(t)) return "cooling down";
            }
            return null;
        }

        /// <summary>Fills into with the templates that may appear now (in pool order).</summary>
        public static void Eligible(IList<EventTemplate> pool, EventSituation s, Func<string, float> lastFired, ICollection<string> active, List<EventTemplate> into)
        {
            into.Clear();
            if (pool == null) return;
            for (int i = 0; i < pool.Count; i++) if (WhyNot(pool[i], s, lastFired, active) == null) into.Add(pool[i]);
        }

        /// <summary>A weighted random pick from a list (null when it is empty). The same seed always gives the same pick.</summary>
        public static EventTemplate PickWeighted(IList<EventTemplate> list, GRand r)
        {
            if (list == null || list.Count == 0) return null;
            float total = 0f;
            for (int i = 0; i < list.Count; i++) total += WeightOf(list[i]);
            float x = r.Value() * total;
            for (int i = 0; i < list.Count; i++)
            {
                x -= WeightOf(list[i]);
                if (x < 0f) return list[i];
            }
            return list[list.Count - 1];
        }

        /// <summary>Eligible plus PickWeighted: the template the director should try next, or null.</summary>
        public static EventTemplate Pick(IList<EventTemplate> pool, EventSituation s, Func<string, float> lastFired, ICollection<string> active, GRand r, List<EventTemplate> scratch)
        {
            Eligible(pool, s, lastFired, active, scratch);
            return PickWeighted(scratch, r);
        }
    }
}
