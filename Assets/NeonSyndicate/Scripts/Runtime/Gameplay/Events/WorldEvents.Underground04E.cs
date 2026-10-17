namespace NeonSyndicate
{
    /// <summary>
    /// WorldEvents, 04E: surface events while the player is in the Underheart. New events already wait until the player
    /// is back on the street (MayStartSurfaceEvent); this part handles the ones that were running when the player went
    /// down. Each is held (its script does not step, so nothing spawns and no timer runs out; its age and leave timer
    /// stop; the HUD says nothing about it), then resumes on the way back up, or ends quietly as expired (no reward, no
    /// failure, its creatures leave at once) when holding is not safe. The decision is UndergroundEventRules04E.
    /// </summary>
    public sealed partial class WorldEvents
    {
        /// <summary>How many running events are held right now (the F2 Events page shows it).</summary>
        public int HeldCount
        {
            get
            {
                int n = 0;
                foreach (EventInstance e in _active) if (e.Held && !e.Done) n++;
                return n;
            }
        }

        /// <summary>
        /// Called from Tick for one event before its script steps. True when the event was held this frame or expired
        /// and was removed (the caller then skips the rest of that event's tick); false when it runs normally.
        /// </summary>
        private bool TickUnderground(EventInstance e, int index, bool underground, float dt)
        {
            if (e.Done || (!underground && !e.Held)) return false;
            bool territory = !string.IsNullOrEmpty(e.TerritoryKey);
            bool changed = e.Held && UndergroundEventRules04E.ActorsChanged(e.HeldAlive, e.AliveActors());
            bool live = e.Held ? e.HeldLive : UndergroundEventRules04E.IsLive(e.Engaged, e.Woken, e.Focus != null, e.AliveCreatures());
            UndergroundEventAction action = UndergroundEventRules04E.Decide(underground, e.Held, e.Done, territory, live, e.HeldSeconds, changed);
            switch (action)
            {
                case UndergroundEventAction.Hold:
                    if (!e.Held)
                    {
                        e.Held = true;
                        e.HeldSeconds = 0f;
                        e.HeldLive = live;
                        e.HeldAlive = e.AliveActors();
                        Log.Info("World events: " + e.T.Id + " held while the player is underground (" + (live ? "live, " + UndergroundEventRules04E.LiveGraceSeconds : "dormant, " + UndergroundEventRules04E.DormantHoldSeconds) + " s at most).");
                    }
                    else e.HeldSeconds += dt;
                    return true;
                case UndergroundEventAction.Resume:
                    e.Held = false;
                    e.HeldSeconds = 0f;
                    string line = UndergroundEventRules04E.ResumeLine(e.T.Title, e.Engaged);
                    if (Game.Hud != null && line.Length > 0) Game.Hud.Notify(line, 3.5f);
                    Log.Info("World events: " + e.T.Id + " resumes (the player is back on the surface).");
                    return false;
                case UndergroundEventAction.Expire:
                    ExpireUnderground(e, index, territory, changed);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Ends a held (or territory) event quietly as missed: no reward and no failure. Miss comes first so nothing that
        /// follows, the despawn included, can ever count as defeating its enemies. Recorded like any missed event.
        /// </summary>
        private void ExpireUnderground(EventInstance e, int index, bool territory, bool changed)
        {
            e.Miss(UndergroundEventRules04E.ExpireLine(e.T.Title, e.Engaged, territory));
            e.DespawnCreatures();
            e.Held = false;
            if (_reported.Add(e)) Report(e);
            e.Cleanup();
            _active.RemoveAt(index);
            _forced.Remove(e);
            _reported.Remove(e);
            if (_callout == e) _callout = null;
            Log.Info("World events: " + e.T.Id + " expired while the player was underground (" + (territory ? "territory activity" : changed ? "its actors changed during the hold" : "the hold ran out") + "); no reward, no failure.");
        }
    }
}
