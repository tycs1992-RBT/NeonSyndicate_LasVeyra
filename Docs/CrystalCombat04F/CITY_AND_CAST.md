# Crystal combat and living city 04F

Implementation status: source implemented, native Unity placement, rendering, combat and performance acceptance NOT RUN.

## Playable city route

Normal Free Roam gives the existing WorldEvents director its first 04F placement attempt after 22 seconds of active play. The first eligible 04F incident is Wardens and Riftbound. It uses existing Frost Wardens and Hollow Cult faction identities and includes a handler-owned quartz drakeling. The director selects an outdoor plaza or an open street candidate 38 to 95 metres away. If geometry, another encounter, a wanted pursuit, a story sequence, an open phone or an underground visit prevents safe placement, it retries later. It does not force actors into a bad location.

Every participant belongs to the same EventInstance as its prompt, props, map blip, reward receipt and cleanup. This is an extension of WorldEvents, not another timed director. Neither faction acquires the player just for approaching, watching, aiming nearby or generating unrelated noise. Actual attacks may provoke retaliation and retain the existing crime semantics. Wardens remain lawful targets: deliberately hurting a warden can create wanted heat.

The neutral seal is five metres to the side of the combat court. Interact there to accept the optional objective. Remain within 3.5 metres for three seconds to record the evidence or open a safe route. For passage and rescue events this provides a nonlethal resolution. In a cache event the guild must still secure the supplies. In a drake rescue the drakeling starts at the neutral seal, separated from its handler. Opening the route releases its actual movement goal; success requires the living handler and body to physically reunite within three metres. A handler death is checked before every success and collection decision. Return to the seal after resolution and interact to claim. Watching or cleanup never awards the event reward.

## Event roster and limits

| Event | Existing factions | Roles and objective | Finite lifecycle |
| --- | --- | --- | --- |
| Wardens and Riftbound | Frost Wardens, Hollow Cult | Blade guard and focus weaver on each side, Warden handler and NPC-owned drakeling. Broker passage or let the factions clash. | Eight second warning, up to 80 seconds of combat, then a ceasefire or a defeated side. No reinforcements. |
| A Handler Without Her Drake | Frequency Order, Hollow Cult | Handler and drakeling face riftbound fighters. Open a safe crossing for three seconds or protect the pair. | Same finite combat window. Handler death withdraws its drake and fails the optional rescue. |
| The Contested Forge Cache | Forgewrights, Ember Court | Opposing melee and ranged crews. Record the seal, help the Forgewrights secure it, collect the cache. | Same finite combat window. A rival victory removes the resource without an observer penalty. |

Each shipped template has two humanoids per side. Low caps any supported template at two per side; other presets cap it at three. There is at most one active 04F city incident inside WorldEvents' existing cap of two total events. A city incident reserves the next ambient selection until at least 130 seconds after its start. Resolution also requests at least 40 quiet seconds. Template cooldowns are 420 seconds of play. The special 04F selector leaves 240 seconds between successful selections. Other eligible events still use WorldEvents' existing selection rules.

Surviving fighters lower weapons and release shared attack tokens when combat ends. A reward collection window lasts at most 30 seconds. Done actors disappear through their event's normal cleanup after eight seconds or when the player leaves. Going far away, entering the Veil, starting a mission, loading a save or disabling WorldEvents retires only tagged owned actors. The native first-court placement and escape paths still require playtesting.

## Crystal roles and attack ownership

| Role | Existing weapon identity | Distinct behavior |
| --- | --- | --- |
| Blade Warden | VoltKatana | Normal telegraphed NPC blade moves, poise, token-limited attacks against the player. |
| Ward Sentinel | CrystalMaul | Heavy melee and 95 armour. A frontal ward reduces ordinary damage to 45 percent while active. The blade guard reduces damage to 65 percent. Guards hold for three seconds, then leave a 1.5 second opening. Heavy or unblockable blows break it for 2.7 seconds. |
| Focus Weaver | VectorSidearm, LongWatch at higher severity | Actual visible swept crystal bolts. A 0.45 second tell for a normal focus, 0.95 second tell for a heavy focus. Aim is committed before release. |
| Crystal Marshal | LongWatch | Command focus with a slower charged tell. At most three pulses, 14 seconds apart, restore 12 poise to existing nearby officers. No support spawns. |
| Rift Duelist (faction counterpart) | ArcGauntlets | Close crystal strikes through the existing light/heavy melee moves and animation solver. |

NPC foci use their actor-local state. They never read the player's equipped loot item, spend the player's charge, trigger the player's camera or execute a silent conventional hitscan fallback. Bolts travel at 25 metres per second, or 34 for a heavy focus. Individual damage is clamped to 15 to 44 before the actor's damage scale. Three casts trigger a 3.2 second recovery; normal casts otherwise wait 1.15 seconds and heavy casts 2.1 seconds. Bolts have a global cap of 12 on Low and 24 on higher presets.

Every bolt performs a sphere sweep over the distance it travels that frame and selects the nearest valid physical hit. The owner's own body and occupied vehicle are excluded. Walls, allies and protected actors stop the projectile; a protected actor takes no damage. There is no NPC explosion or area-of-effect splash. Hits attribute damage to the attacking NPC. Ordinary actors can hurt only their explicit current target. An event actor can hurt only the opposite side of the same event, or the player after a real direct attack. The same permission check protects normal NPC melee sweeps. Player attacks continue through the established crystal and melee systems.

## Local enforcement

PoliceManager retains witnessed crime reporting, warnings, surrender for the existing minor-incident conditions, sight-based evasion and mission pursuit. Standing in a district is not misconduct.

A local incident lasts at most 100 active seconds before the squad withdraws into a 75 second containment period. Total automatically dispatched officers for that incident are 3 + severity on Story, 4 + severity on Balanced and 6 + severity on Veteran. Active unit caps still come from PoliceResponseRules. Recruited traffic officers, vehicle crews and foot responders all spend the same lifetime count. Losing officers never replenishes that lifetime budget. The normal shared CombatDirector melee and ranged token caps remain in force for every attacker targeting the player.

Local incidents do not dispatch helicopters or roadblocks. Those remain available to legitimate story pursuit. After a local incident, the manager stops replacing units, releases owned combat targets and suppresses sightings long enough for the normal unseen-wanted logic to resolve the search. It never calls Wanted.ClearAll, changes story progress, clears Frozen story pursuit, deletes mission-tagged officers or cleans up another encounter's actors. Frozen wanted state or an active non-quiet original story mission exempts story pursuit from local containment. A resolved local pursuit schedules a 40 second quiet dispatch interval before a new incident's automatic reinforcements.

## NPC drake semantics

NpcBondedDrake04F requests the established quartz guide mesh through CompanionBodyFactory, builds the shared CompanionController rig, and steers Goal/Wait modes beside its actual handler. A timed breath pose releases a real owned crystal bolt at the handler's valid target with a 5.5 second recovery and 22 metre reach. It never registers a BondSystem record, auto-grants a rescue, replaces the player's companion, changes an existing companion UID or enters the fusion garage. Its UID is zero and its display name is explicitly an NPC street drakeling.

Its event owns its body, worker request, animation callback and cleanup. Handler death or event retirement cancels pending work and removes the body. The rescue variant waits at a surveyed separation point until the route is opened or the hostile crew is defeated, then physically crosses to its handler. It currently uses the companion system's non-targetable body semantics rather than adding a separate creature health pool. Independent NPC-drake injuries, autonomous retreat choices and targeted handler-to-drake rescue animation remain partial. This is not a second player-companion unlock route.

## Rewards and save safety

The three events award a modest 45 Lumen, three existing glass_chitin materials and two reputation with their supporting faction. Only an explicit accepted and completed objective followed by physical collection grants the reward. The event has no ordinary corpse or NPC cash drops. A receipt keyed by template ID and occurrence is installed before grant. EventSave appends receipt and remaining cooldown lists, leaving existing IDs and fields unchanged. Restore reads receipts without granting anything, discards transient actors and resumes cooldowns. Successful collection requests the normal autosave. A failed autosave is logged accurately and does not pretend remote or disk persistence succeeded.

## Native acceptance still required

1. Open the pinned Unity version, start Free Roam, wait for the first safe Warden dispute and verify the drake and the two opposing loadouts are visible from normal play.
2. Watch without attacking. Verify no wanted increase and no stray melee or projectile damages the player, Nessa, traders or the player's companion.
3. Watch a complete unattended fight. Verify a side falls or the bounded ceasefire ends combat and no reinforcements arrive.
4. Approach the neutral seal, test keyboard and gamepad interaction, finish the three second objective, physically collect once, save and reload. Confirm no duplicate reward or actors.
5. Start a minor crime. Confirm squad totals, clear warning, surrender under the existing conditions, retreat and containment. Separately test a scripted Frozen pursuit remains active.
6. Test shots against a wall, through an ally, in front of a shopkeeper and against a live rival. Confirm nearest-hit occlusion, protected-target blocking, visible windups and correct NPC damage attribution.
7. Enter the Underheart during a dispute and return or leave for long enough that it expires. Confirm no upstairs combat actor leaks into the campaign or its companion rescue.
8. Record active counts, frame times and memory on Low, Medium, High and Ultra. No measured frame rate is claimed by this source delivery.
