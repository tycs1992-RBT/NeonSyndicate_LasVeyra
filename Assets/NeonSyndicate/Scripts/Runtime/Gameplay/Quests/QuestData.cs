using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    // ============================================================================================================
    // Quest data: plain serializable classes with public fields only, so the same JSON loads with JsonUtility in the
    // game, with QuestJson (the engine-free reader the EditMode tests use) and with System.Text.Json (the offline
    // validator). No properties, no dictionaries, no polymorphism: a step is one flat class whose Type picks the
    // behaviour and whose other fields are the parameters. Numbers left at 0 mean "use the default" (see
    // QuestDefaults), so a file only needs the fields it actually uses. Docs/QUESTS_AND_EVENTS.md is the reference.
    // ============================================================================================================

    /// <summary>One quest data file (Resources/Quests/*.json). A file may hold guild givers, quests and contract templates.</summary>
    [Serializable]
    public sealed class QuestFile
    {
        /// <summary>Schema version of the file. Always 1 for now.</summary>
        public int Format = 1;
        public List<GiverDef> Givers = new List<GiverDef>();
        public List<QuestDef> Quests = new List<QuestDef>();
        public List<ContractDef> Contracts = new List<ContractDef>();
        // ---- appended for Underheart and Bonds 04E
        /// <summary>Story campaigns declared by this file (a quest's Arc may name one instead of a guild faction).</summary>
        public List<CampaignDef> Campaigns = new List<CampaignDef>();
    }

    /// <summary>A story campaign (04E): an arc that is not a guild. Quests set Arc and Campaign to its Id.</summary>
    [Serializable]
    public sealed class CampaignDef
    {
        /// <summary>Lower case id, for example "underheart".</summary>
        public string Id = "";
        /// <summary>Shown wherever a guild name would be (the tracker, the journal, the offer card).</summary>
        public string Title = "";
        /// <summary>Marker and tracker colour as "#RRGGBB".</summary>
        public string Color = "";
    }

    /// <summary>One line of a finite encounter roster in a quest Skirmish step (or a campaign contract).</summary>
    [Serializable]
    public sealed class RosterEntry
    {
        public string Species = "";
        public int Count;
        public bool Elite;
    }

    /// <summary>Where a guild hands out its quests and posts its contract board.</summary>
    [Serializable]
    public sealed class GiverDef
    {
        /// <summary>Guild faction id (FactionDb), for example "frost_wardens".</summary>
        public string Guild = "";
        /// <summary>POI name or site key the giver stands at.</summary>
        public string At = "";
        /// <summary>World-axis offset in metres from that place. Both 0: a small automatic step to the side of the POI.</summary>
        public float OffsetX;
        public float OffsetZ;
        /// <summary>Who the player talks to (shown on the marker, the board and the journal).</summary>
        public string Name = "";
        /// <summary>Title of the guild's contract board.</summary>
        public string Board = "";
        /// <summary>What the giver says when the player reads the board.</summary>
        public string Greeting = "";
    }

    /// <summary>One quest: who gives it, what unlocks it, its phases of steps, how it can fail and what it pays.</summary>
    [Serializable]
    public sealed class QuestDef
    {
        /// <summary>Unique id (lower case, underscores). Saves and prerequisites refer to it; never rename a shipped id.</summary>
        public string Id = "";
        public string Title = "";
        /// <summary>The guild arc it belongs to (a guild faction id).</summary>
        public string Arc = "";
        /// <summary>Position in the arc (1, 2, 3...), used for sorting in the journal.</summary>
        public int Order;
        /// <summary>Empty: the arc's giver. Otherwise a POI name or site key.</summary>
        public string Giver = "";
        /// <summary>Optional named presenter for a quest with its own giver.</summary>
        public string Presenter = "";
        /// <summary>Optional inhabited venue whose resident offers this quest instead of an outdoor giver marker.</summary>
        public string VenueGiver = "";
        public float GiverOffsetX;
        public float GiverOffsetZ;
        /// <summary>One or two sentences shown when offered and in the journal.</summary>
        public string Summary = "";
        /// <summary>Shown in the journal once completed.</summary>
        public string Done = "";
        /// <summary>The police ignore crimes while the quest runs (creature hunts in the city).</summary>
        public bool IgnoreCrimes;
        /// <summary>The ambient creature ecology is switched off while the quest runs (set-piece fights).</summary>
        public bool NoAmbient;
        public QuestRequirements Requires = new QuestRequirements();
        public List<QuestPhase> Phases = new List<QuestPhase>();
        public List<QuestFailCondition> FailIf = new List<QuestFailCondition>();
        public QuestReward Reward = new QuestReward();
        // ---- appended for Underheart and Bonds 04E (legacy quests leave them empty)
        /// <summary>"Main" or "Side" for campaign quests; "" for guild and legacy quests.</summary>
        public string Kind = "";
        /// <summary>The campaign id (a CampaignDef in some file); campaign quests set Arc to the same id.</summary>
        public string Campaign = "";
        /// <summary>The campaign act (1 to 4) a main quest belongs to.</summary>
        public int Act;
        /// <summary>Suggested difficulty, 1 (gentle) to 5 (hardest), shown on the offer and map card.</summary>
        public int Difficulty;
        /// <summary>The region the quest mostly takes place in: an UnderheartIds region id or "surface".</summary>
        public string Region = "";
        /// <summary>The floor or depth in plain words ("Ground floor", "Lantern Refuge, about 18 m below").</summary>
        public string Floor = "";
        /// <summary>The entrance to use in plain words ("Pressure House door on Veyra Avenue").</summary>
        public string Entrance = "";
        /// <summary>RewardIds04E ids the quest's Claim steps grant, for display on the offer and the journal.</summary>
        public List<string> Claims = new List<string>();
    }

    /// <summary>What must be true before a quest is offered. Every field left empty or 0 is not checked.</summary>
    [Serializable]
    public sealed class QuestRequirements
    {
        /// <summary>Quest ids that must be completed first.</summary>
        public List<string> Quests = new List<string>();
        /// <summary>Story missions passed (MissionManager.StoryProgress). Free roam counts as all of them.</summary>
        public int Story;
        /// <summary>0 accepts every layout. Positive values require that opt-in world revision; old data defaults to 0.</summary>
        public int WorldRevision;
        /// <summary>Optional explicit allowed layouts, intersected with WorldRevision when both are supplied.</summary>
        public List<int> WorldRevisions = new List<int>();
        /// <summary>Faction whose reputation is checked against Rep.</summary>
        public string Faction = "";
        public int Rep;
        public bool ThirdEye;
        /// <summary>The Veil must be open (InnerWorld.Unlocked).</summary>
        public bool InnerWorld;
        /// <summary>Site key the player must have reached at least once (for example "WardenOutpost").</summary>
        public string Visited = "";
    }

    /// <summary>A named group of steps. Phases run in file order unless Next or a Choice step says otherwise.</summary>
    [Serializable]
    public sealed class QuestPhase
    {
        /// <summary>Unique within the quest. Choices, Next and fail conditions refer to it.</summary>
        public string Id = "";
        /// <summary>Shown on the HUD tracker while the phase runs.</summary>
        public string Title = "";
        /// <summary>Empty: the following phase (or the end after the last). A phase id, or "END" to complete the quest.</summary>
        public string Next = "";
        public List<QuestStep> Steps = new List<QuestStep>();
        // ---- appended for Underheart and Bonds 04E
        /// <summary>Entering this phase commits a checkpoint (campaign quests): a restart or retry resumes here.</summary>
        public bool Checkpoint;
        /// <summary>Where a resumed or retried quest starts when this is the committed checkpoint (a place word).</summary>
        public string ResumeAt = "";
        // ---- appended for the 04E choice consequences (the descent window)
        /// <summary>
        /// The Underheart descent window this phase holds the Ash Reliquary in while it runs: "early" (cooler heat columns,
        /// lamps out), "late" (hotter heat columns, lit columns) or "choice" (the stored descent_window decision). Empty:
        /// none. The Underheart reads it from the running quest, so finishing, failing, abandoning or loading clears it on
        /// its own. Campaign quests only.
        /// </summary>
        public string Window = "";
    }

    /// <summary>
    /// One step. Type picks the behaviour (GoTo, Talk, Interact, Kill, Boss, Collect, Survive, Defend, Escort, Hunt,
    /// CloseRift, Scan, Shift, Wait) or an instant action (Spawn, Ally, Prop, SetTime, SetWeather, Give, GiveGun,
    /// AddRep, Message, Cinematic, Choice). QuestValidator knows which fields each type reads and rejects the rest.
    /// </summary>
    [Serializable]
    public sealed class QuestStep
    {
        public string Type = "";
        /// <summary>HUD objective text. Empty: a sensible default for the type.</summary>
        public string Objective = "";
        /// <summary>Venue steps resolve authored indoor points without outdoor ground projection.</summary>
        public string VenueId = "";
        public string ActionId = "";

        // ---- where: At is a POI name, a site key, "Giver", "Player", "Previous" (also the empty default),
        // "District:Name" (a sidewalk in that city district) or "World" (the map origin, so offsets are coordinates).
        public string At = "";
        public float OffsetX;
        public float OffsetZ;
        public float Radius;
        /// <summary>Second place: escort and hunt destinations, the cinematic camera's target.</summary>
        public string To = "";
        public float ToOffsetX;
        public float ToOffsetZ;
        public float ToRadius;
        /// <summary>"Inner": the step happens inside the Inner World (the player is asked to shift first). "Outer": outside.</summary>
        public string Veil = "";
        /// <summary>Inside the Inner World the guild holds the Veil open: no crystal-energy drain during this step.</summary>
        public bool HoldVeil;

        // ---- who and what
        public string Species = "";
        public int Count;
        /// <summary>Kill and CloseRift: at most this many alive at once (more arrive as they fall).</summary>
        public int MaxAlive;
        public int Waves;
        public float Seconds;
        /// <summary>Display name of the spawned character or beast, the banner title of a Message, the title of a Choice.</summary>
        public string Name = "";
        /// <summary>Look of a spawned person (QuestLooks names: Civilian, Patient, Warden, Mender, Mara...).</summary>
        public string Look = "";
        /// <summary>Defend: people to protect inside the zone.</summary>
        public int Allies;
        public bool Elite;
        public float HealthScale;
        public float DamageScale;
        /// <summary>Health of an escorted person or an ally, or of a rift core.</summary>
        public float Health;
        /// <summary>Boss: reinforcements that arrive at two thirds and one third health. CloseRift: a second species the tear spits out.</summary>
        public string AddSpecies = "";
        public int AddCount;
        public bool Pacifist;
        public bool OnFoot;
        public int Clues;
        /// <summary>Set dressing placed at the spot (QuestProps names: Crate, Brazier, Shrine, Jammer, Scaffold...).</summary>
        public string Prop = "";
        /// <summary>Interact prompt. Empty: "Press E to ..." built from the objective.</summary>
        public string Prompt = "";

        // ---- materials, rewards and the world
        public string Material = "";
        /// <summary>Collect: count what the player holds (not only what they gather during the step).</summary>
        public bool Have;
        /// <summary>Collect: hand the materials over when the step completes.</summary>
        public bool Spend;
        /// <summary>Collect: glowing caches to scatter around the spot (each gives its share).</summary>
        public int Caches;
        public string Faction = "";
        public int Amount;
        public int Cash;
        public int Lumen;
        /// <summary>SetTime: hour of the day, 0 to 24.</summary>
        public float Hour;
        public string Weather = "";
        /// <summary>Give: a gear piece for this slot (Visor, Shell, Gauntlets, Striders, Implant, Core), rolled by the gear system.</summary>
        public string Gear = "";
        /// <summary>Give: the gear piece's rarity (Common, Uncommon, Rare, Epic, Legendary, Mythic). Empty: Rare. GiveGun: the loot gun's rarity.</summary>
        public string Rarity = "";
        /// <summary>GiveGun: the loot gun type (Pistol, HandCannon, Smg, AssaultRifle, Shotgun, Sniper, Launcher, CrystalCaster). Empty: any.</summary>
        public string Archetype = "";
        /// <summary>GiveGun: the name of a named unique gun (for example "Skyline Requiem"); it sets the rarity and type itself.</summary>
        public string Unique = "";
        /// <summary>Shift: true to enter the Inner World, false to leave it.</summary>
        public bool Inner;
        /// <summary>Shift: move the player through the Veil at once instead of waiting for them to do it.</summary>
        public bool Force;
        /// <summary>Cinematic: camera height above the ground in metres.</summary>
        public float Height;
        public string Text = "";
        public bool Banner;
        public List<DialogueLine> Lines = new List<DialogueLine>();
        public List<ChoiceOption> Options = new List<ChoiceOption>();

        // ---- appended for Underheart and Bonds 04E (Docs/Underheart04E/CAMPAIGN.md lists which step reads which)
        /// <summary>Skirmish: the declared finite roster.</summary>
        public List<RosterEntry> Roster = new List<RosterEntry>();
        /// <summary>Skirmish: announced reinforcements from the declared budget (0 for none).</summary>
        public int Reinforce;
        /// <summary>Skirmish: the room id marked cleared on victory (an Underheart anchor id).</summary>
        public string Room = "";
        /// <summary>Claim: a RewardIds04E id.</summary>
        public string Reward = "";
        /// <summary>Claim: the once-per-token entitlement token (empty: "quest:questId:rewardId").</summary>
        public string Token = "";
        /// <summary>Course: a FusionIds course id.</summary>
        public string Course = "";
        /// <summary>Fuse: a FusionIds form the fusion must be in (empty: any form).</summary>
        public string Form = "";
        /// <summary>Rescue and BondRank: a BondIds archetype.</summary>
        public string Bond = "";
        /// <summary>BondRank: the rank to raise the companion to at least (1 Bonded to 4 Awakened).</summary>
        public int Rank;
        /// <summary>Decide: the campaign choice key. Flag: the flag name.</summary>
        public string Key = "";
        /// <summary>Decide without options: the value to record.</summary>
        public string Value = "";
        /// <summary>Rescue: the clamp places (place words).</summary>
        public List<string> Points = new List<string>();
        /// <summary>Mechanism: an UnderBossIds mechanism id.</summary>
        public string Mechanism = "";
        /// <summary>Shortcut: an UnderheartIds shortcut id.</summary>
        public string Shortcut = "";
        /// <summary>UBoss: an UnderBossIds boss id.</summary>
        public string Boss = "";
        /// <summary>Conduit: true lights the arrival conduit, false dims it.</summary>
        public bool Lit;
        /// <summary>Equip: also finish the practice-lane calibration with the focus (the 04D lane).</summary>
        public bool Calibrate;
        // ---- appended by the 04E integration fixes
        /// <summary>
        /// Decide with Options: ask again on every attempt and let the new answer overwrite the stored one. For approach
        /// choices (fight or bypass) a player who failed one way may try the other; consequence choices leave it false.
        /// </summary>
        public bool Reask;
    }

    /// <summary>One line of dialogue.</summary>
    [Serializable]
    public sealed class DialogueLine
    {
        public string Speaker = "";
        public string Text = "";
    }

    /// <summary>One answer of a Choice step: its button label and the phase it leads to.</summary>
    [Serializable]
    public sealed class ChoiceOption
    {
        public string Label = "";
        public string Phase = "";
        // ---- appended for Underheart and Bonds 04E
        /// <summary>Decide: the value recorded for the choice key when this option is picked.</summary>
        public string Value = "";
        /// <summary>
        /// Branch: the condition that sends the quest to Phase (CampaignRules04E grammar, "else" always matches).
        /// Choice and Decide: the option is offered only while the condition holds (empty: always).
        /// </summary>
        public string When = "";
    }

    /// <summary>
    /// A way to fail a quest besides dying or being arrested. Types: AllyDies, TimeLimit, LeaveArea, LeaveVeil,
    /// Wanted, Hour, Vehicle. Phases limits it to some phases (empty: the whole quest).
    /// </summary>
    [Serializable]
    public sealed class QuestFailCondition
    {
        public string Type = "";
        public List<string> Phases = new List<string>();
        /// <summary>TimeLimit: the limit. LeaveArea and LeaveVeil: the grace period before failing.</summary>
        public float Seconds;
        public string At = "";
        public float OffsetX;
        public float OffsetZ;
        public float Radius;
        /// <summary>Wanted: fail at this many stars or more (0 means 1).</summary>
        public int Stars;
        /// <summary>Hour: fail when the clock is inside HourMin to HourMax (wraps past midnight).</summary>
        public float HourMin;
        public float HourMax;
        public string Message = "";
    }

    /// <summary>What a quest, contract or world event pays.</summary>
    [Serializable]
    public sealed class QuestReward
    {
        public int Cash;
        public int Lumen;
        /// <summary>Granted through Game.Skills.Grant(int) when that exists, otherwise kept as unclaimed points (QuestBook).</summary>
        public int SkillPoints;
        public List<MaterialAmount> Materials = new List<MaterialAmount>();
        public List<RepAmount> Rep = new List<RepAmount>();
    }

    [Serializable]
    public sealed class MaterialAmount
    {
        public string Id = "";
        public int Count;
    }

    [Serializable]
    public sealed class RepAmount
    {
        public string Faction = "";
        public int Amount;
    }

    /// <summary>
    /// A repeatable contract template. Boards turn templates into concrete jobs: a species picked from the list and a
    /// place in its habitat (Hunt, Rift), or a destination from the list (Escort, Courier), with rewards scaled by
    /// danger, count, distance and how many contracts the player has done for the guild.
    /// Title and Brief may use {species}, {count}, {place} and {dest}.
    /// </summary>
    [Serializable]
    public sealed class ContractDef
    {
        public string Id = "";
        public string Guild = "";
        /// <summary>Hunt, Rift, Escort or Courier.</summary>
        public string Kind = "";
        public string Title = "";
        public string Brief = "";
        /// <summary>How often the board posts it compared with the guild's other templates (0 means 1).</summary>
        public float Weight;
        /// <summary>Reputation with the guild before the board posts it.</summary>
        public int MinRep;
        public List<string> Species = new List<string>();
        public int CountMin;
        public int CountMax;
        /// <summary>Escort and Courier: POI names or site keys the job can lead to.</summary>
        public List<string> Destinations = new List<string>();
        /// <summary>Escort: who is escorted.</summary>
        public string Look = "";
        public string Name = "";
        /// <summary>Courier: time limit in seconds (0: none).</summary>
        public float Seconds;
        /// <summary>Base pay before scaling.</summary>
        public int Cash;
        public int Lumen;
        /// <summary>Reputation with the guild for finishing it.</summary>
        public int Rep;
        public List<MaterialAmount> Materials = new List<MaterialAmount>();
        // ---- appended for Underheart and Bonds 04E (campaign contract templates; Guild is then the campaign id)
        /// <summary>Region ids the posting may send the player to (each Destination's region must be listed).</summary>
        public List<string> Regions = new List<string>();
        /// <summary>Readable risk, 1 (low) to 5 (extreme); pay scales with it.</summary>
        public int Risk;
        /// <summary>A quest id that must be completed before the board posts this template ("" for none).</summary>
        public string After = "";
        /// <summary>The declared finite roster for the template's encounter (Skirmish grammars).</summary>
        public List<RosterEntry> Roster = new List<RosterEntry>();
        /// <summary>Announced reinforcements in the encounter (0 for none).</summary>
        public int Reinforce;
    }

    /// <summary>One world-event data file (Resources/Events/*.json).</summary>
    [Serializable]
    public sealed class EventFile
    {
        public int Format = 1;
        public List<EventTemplate> Events = new List<EventTemplate>();
    }

    /// <summary>
    /// A random world event. Kind picks the behaviour (Rescue, Loot, Talk, Brawl, Relight, Swarm, Rift, Escort,
    /// Rising, Pickup, Sighting, Impostor, Sabotage, Storm, Holdout); the director picks templates by place, time,
    /// weather, story progress, cooldown and weight, and spawns them 80 to 160 m from the player.
    /// </summary>
    [Serializable]
    public sealed class EventTemplate
    {
        public string Id = "";
        public string Title = "";
        public string Kind = "";
        /// <summary>Only an explicit physical territory interaction may start this event; never an ambient pick.</summary>
        public bool ManualOnly;
        /// <summary>Fieldwork: Gather, Aid, Observe, Resonance. Skirmish: Defense or Stabilize.</summary>
        public string Activity = "";
        /// <summary>The small HUD callout when the event appears.</summary>
        public string Callout = "";
        /// <summary>The status line while the player is close. Empty: a default for the kind.</summary>
        public string Objective = "";
        public string Success = "";
        public string Failure = "";
        /// <summary>Relative chance among eligible templates (0 means 1).</summary>
        public float Weight;
        /// <summary>District names, "City" (every city district), "Wild" (Badlands and Frostline) or "Any". Empty: City.</summary>
        public List<string> Districts = new List<string>();
        /// <summary>"Outer" (also the empty default), "Inner" (only inside the Inner World) or "Any".</summary>
        public string Veil = "";
        /// <summary>Hour window (wraps past midnight, so 20 to 5 is night). Both 0: any time.</summary>
        public float HourMin;
        public float HourMax;
        /// <summary>Weather names (Clear, Cloudy, Rain, Storm, Fog, Sandstorm). Empty: any.</summary>
        public List<string> Weathers = new List<string>();
        /// <summary>Story missions passed before it can appear.</summary>
        public int Story;
        public bool ThirdEye;
        /// <summary>Seconds before the same template can appear again (0: 600).</summary>
        public float Cooldown;
        /// <summary>Seconds it waits for the player before fading (0: 240).</summary>
        public float Duration;
        /// <summary>Where it spawns: Sidewalk, Road, Parking, Park, Plaza, Lot, Canal, Lamp, Desert, Snow, Any or "Site:Key".</summary>
        public string Anchor = "";
        public string Species = "";
        public int Count;
        public string Species2 = "";
        public int Count2;
        public bool Elite;
        public float HealthScale;
        /// <summary>Bystanders to save or protect.</summary>
        public int Civilians;
        public string Look = "";
        public string Name = "";
        /// <summary>Brawl: the two crews. Loot, Sabotage and Pickup: the crew guarding or contesting it.</summary>
        public string FactionA = "";
        public string FactionB = "";
        public int Guards;
        public string Prop = "";
        /// <summary>Things to use or collect: braziers to relight, crates to loot, motes to gather.</summary>
        public int Points;
        public float Seconds;
        public float Health;
        /// <summary>Escort destination: POI name or site key. Empty: the nearest clinic or hospital.</summary>
        public string To = "";
        /// <summary>Brawl: reputation won with the side you back (and lost with the other).</summary>
        public int Amount;
        public List<DialogueLine> Lines = new List<DialogueLine>();
        public QuestReward Reward = new QuestReward();
    }

    /// <summary>The values a 0 or empty field stands for, shared by the runtime, the validator and the docs.</summary>
    public static class QuestDefaults
    {
        public const float GoToRadius = 6f;
        public const float SpawnRadius = 14f;
        public const float DefendRadius = 18f;
        public const float EscortArrive = 7f;
        public const int KillMaxAlive = 6;
        public const int Waves = 3;
        public const int Clues = 3;
        public const float RiftInterval = 7f;
        public const float RiftCoreHealth = 650f;
        public const float AllyHealth = 300f;
        public const float ScanSeconds = 1.5f;
        public const float CinematicSeconds = 4f;
        public const float CinematicHeight = 4f;
        public const float MessageSeconds = 5f;
        public const float EventCooldown = 600f;
        public const float EventDuration = 240f;
        public const float LeaveGrace = 8f;
        public const string End = "END";

        /// <summary>Replaces nulls left by a loader with empty values so the runtime never meets a null list or string.</summary>
        public static void Normalize(QuestFile f)
        {
            if (f == null) return;
            if (f.Givers == null) f.Givers = new List<GiverDef>();
            if (f.Quests == null) f.Quests = new List<QuestDef>();
            if (f.Contracts == null) f.Contracts = new List<ContractDef>();
            if (f.Campaigns == null) f.Campaigns = new List<CampaignDef>();
            for (int i = 0; i < f.Campaigns.Count; i++)
            {
                if (f.Campaigns[i] == null) f.Campaigns[i] = new CampaignDef();
                f.Campaigns[i].Id = f.Campaigns[i].Id ?? "";
                f.Campaigns[i].Title = f.Campaigns[i].Title ?? "";
                f.Campaigns[i].Color = f.Campaigns[i].Color ?? "";
            }
            for (int i = 0; i < f.Givers.Count; i++) if (f.Givers[i] == null) f.Givers[i] = new GiverDef(); else Normalize(f.Givers[i]);
            for (int i = 0; i < f.Quests.Count; i++) if (f.Quests[i] == null) f.Quests[i] = new QuestDef(); else Normalize(f.Quests[i]);
            for (int i = 0; i < f.Contracts.Count; i++) if (f.Contracts[i] == null) f.Contracts[i] = new ContractDef(); else Normalize(f.Contracts[i]);
        }

        public static void Normalize(GiverDef g)
        {
            g.Guild = g.Guild ?? "";
            g.At = g.At ?? "";
            g.Name = g.Name ?? "";
            g.Board = g.Board ?? "";
            g.Greeting = g.Greeting ?? "";
        }

        public static void Normalize(QuestDef q)
        {
            q.Id = q.Id ?? "";
            q.Title = q.Title ?? "";
            q.Arc = q.Arc ?? "";
            q.Giver = q.Giver ?? "";
            q.Presenter = q.Presenter ?? "";
            q.VenueGiver = q.VenueGiver ?? "";
            q.Summary = q.Summary ?? "";
            q.Done = q.Done ?? "";
            q.Kind = q.Kind ?? "";
            q.Campaign = q.Campaign ?? "";
            q.Region = q.Region ?? "";
            q.Floor = q.Floor ?? "";
            q.Entrance = q.Entrance ?? "";
            if (q.Claims == null) q.Claims = new List<string>();
            for (int i = 0; i < q.Claims.Count; i++) q.Claims[i] = q.Claims[i] ?? "";
            if (q.Requires == null) q.Requires = new QuestRequirements();
            if (q.Requires.Quests == null) q.Requires.Quests = new List<string>();
            q.Requires.Faction = q.Requires.Faction ?? "";
            q.Requires.Visited = q.Requires.Visited ?? "";
            if (q.Phases == null) q.Phases = new List<QuestPhase>();
            for (int i = 0; i < q.Phases.Count; i++)
            {
                if (q.Phases[i] == null) q.Phases[i] = new QuestPhase();
                QuestPhase p = q.Phases[i];
                p.Id = p.Id ?? "";
                p.Title = p.Title ?? "";
                p.Next = p.Next ?? "";
                p.ResumeAt = p.ResumeAt ?? "";
                p.Window = p.Window ?? "";
                if (p.Steps == null) p.Steps = new List<QuestStep>();
                for (int s = 0; s < p.Steps.Count; s++)
                {
                    if (p.Steps[s] == null) p.Steps[s] = new QuestStep();
                    Normalize(p.Steps[s]);
                }
            }
            if (q.FailIf == null) q.FailIf = new List<QuestFailCondition>();
            for (int i = 0; i < q.FailIf.Count; i++)
            {
                if (q.FailIf[i] == null) q.FailIf[i] = new QuestFailCondition();
                QuestFailCondition c = q.FailIf[i];
                c.Type = c.Type ?? "";
                c.At = c.At ?? "";
                c.Message = c.Message ?? "";
                if (c.Phases == null) c.Phases = new List<string>();
            }
            q.Reward = Normalize(q.Reward);
        }

        public static void Normalize(QuestStep s)
        {
            s.Type = s.Type ?? "";
            s.Objective = s.Objective ?? "";
            s.VenueId = s.VenueId ?? "";
            s.ActionId = s.ActionId ?? "";
            s.At = s.At ?? "";
            s.To = s.To ?? "";
            s.Veil = s.Veil ?? "";
            s.Species = s.Species ?? "";
            s.Name = s.Name ?? "";
            s.Look = s.Look ?? "";
            s.AddSpecies = s.AddSpecies ?? "";
            s.Prop = s.Prop ?? "";
            s.Prompt = s.Prompt ?? "";
            s.Material = s.Material ?? "";
            s.Faction = s.Faction ?? "";
            s.Weather = s.Weather ?? "";
            s.Gear = s.Gear ?? "";
            s.Rarity = s.Rarity ?? "";
            s.Archetype = s.Archetype ?? "";
            s.Unique = s.Unique ?? "";
            s.Text = s.Text ?? "";
            if (s.Lines == null) s.Lines = new List<DialogueLine>();
            for (int i = 0; i < s.Lines.Count; i++)
            {
                if (s.Lines[i] == null) s.Lines[i] = new DialogueLine();
                s.Lines[i].Speaker = s.Lines[i].Speaker ?? "";
                s.Lines[i].Text = s.Lines[i].Text ?? "";
            }
            if (s.Options == null) s.Options = new List<ChoiceOption>();
            for (int i = 0; i < s.Options.Count; i++)
            {
                if (s.Options[i] == null) s.Options[i] = new ChoiceOption();
                s.Options[i].Label = s.Options[i].Label ?? "";
                s.Options[i].Phase = s.Options[i].Phase ?? "";
                s.Options[i].Value = s.Options[i].Value ?? "";
                s.Options[i].When = s.Options[i].When ?? "";
            }
            s.Room = s.Room ?? "";
            s.Reward = s.Reward ?? "";
            s.Token = s.Token ?? "";
            s.Course = s.Course ?? "";
            s.Form = s.Form ?? "";
            s.Bond = s.Bond ?? "";
            s.Key = s.Key ?? "";
            s.Value = s.Value ?? "";
            s.Mechanism = s.Mechanism ?? "";
            s.Shortcut = s.Shortcut ?? "";
            s.Boss = s.Boss ?? "";
            s.Roster = Normalize(s.Roster);
            if (s.Points == null) s.Points = new List<string>();
            for (int i = 0; i < s.Points.Count; i++) s.Points[i] = s.Points[i] ?? "";
        }

        /// <summary>A roster list with no nulls (missing species become "").</summary>
        public static List<RosterEntry> Normalize(List<RosterEntry> roster)
        {
            if (roster == null) roster = new List<RosterEntry>();
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i] == null) roster[i] = new RosterEntry();
                roster[i].Species = roster[i].Species ?? "";
            }
            return roster;
        }

        public static void Normalize(ContractDef c)
        {
            c.Id = c.Id ?? "";
            c.Guild = c.Guild ?? "";
            c.Kind = c.Kind ?? "";
            c.Title = c.Title ?? "";
            c.Brief = c.Brief ?? "";
            c.Look = c.Look ?? "";
            c.Name = c.Name ?? "";
            if (c.Species == null) c.Species = new List<string>();
            if (c.Destinations == null) c.Destinations = new List<string>();
            if (c.Materials == null) c.Materials = new List<MaterialAmount>();
            for (int i = 0; i < c.Materials.Count; i++)
            {
                if (c.Materials[i] == null) c.Materials[i] = new MaterialAmount();
                c.Materials[i].Id = c.Materials[i].Id ?? "";
            }
            if (c.Regions == null) c.Regions = new List<string>();
            for (int i = 0; i < c.Regions.Count; i++) c.Regions[i] = c.Regions[i] ?? "";
            for (int i = 0; i < c.Species.Count; i++) c.Species[i] = c.Species[i] ?? "";
            for (int i = 0; i < c.Destinations.Count; i++) c.Destinations[i] = c.Destinations[i] ?? "";
            c.After = c.After ?? "";
            c.Roster = Normalize(c.Roster);
        }

        public static QuestReward Normalize(QuestReward r)
        {
            if (r == null) r = new QuestReward();
            if (r.Materials == null) r.Materials = new List<MaterialAmount>();
            if (r.Rep == null) r.Rep = new List<RepAmount>();
            for (int i = 0; i < r.Materials.Count; i++)
            {
                if (r.Materials[i] == null) r.Materials[i] = new MaterialAmount();
                r.Materials[i].Id = r.Materials[i].Id ?? "";
            }
            for (int i = 0; i < r.Rep.Count; i++)
            {
                if (r.Rep[i] == null) r.Rep[i] = new RepAmount();
                r.Rep[i].Faction = r.Rep[i].Faction ?? "";
            }
            return r;
        }

        public static void Normalize(EventFile f)
        {
            if (f == null) return;
            if (f.Events == null) f.Events = new List<EventTemplate>();
            for (int i = 0; i < f.Events.Count; i++)
            {
                if (f.Events[i] == null) f.Events[i] = new EventTemplate();
                Normalize(f.Events[i]);
            }
        }

        public static void Normalize(EventTemplate t)
        {
            t.Id = t.Id ?? "";
            t.Title = t.Title ?? "";
            t.Kind = t.Kind ?? "";
            t.Callout = t.Callout ?? "";
            t.Objective = t.Objective ?? "";
            t.Success = t.Success ?? "";
            t.Failure = t.Failure ?? "";
            t.Veil = t.Veil ?? "";
            t.Anchor = t.Anchor ?? "";
            t.Species = t.Species ?? "";
            t.Species2 = t.Species2 ?? "";
            t.Look = t.Look ?? "";
            t.Name = t.Name ?? "";
            t.FactionA = t.FactionA ?? "";
            t.FactionB = t.FactionB ?? "";
            t.Prop = t.Prop ?? "";
            t.To = t.To ?? "";
            if (t.Districts == null) t.Districts = new List<string>();
            if (t.Weathers == null) t.Weathers = new List<string>();
            if (t.Lines == null) t.Lines = new List<DialogueLine>();
            for (int i = 0; i < t.Lines.Count; i++)
            {
                if (t.Lines[i] == null) t.Lines[i] = new DialogueLine();
                t.Lines[i].Speaker = t.Lines[i].Speaker ?? "";
                t.Lines[i].Text = t.Lines[i].Text ?? "";
            }
            t.Reward = Normalize(t.Reward);
        }
    }
}
