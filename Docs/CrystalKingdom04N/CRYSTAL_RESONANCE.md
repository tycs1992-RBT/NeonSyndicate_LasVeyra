# Crystal Resonance — implemented offline foundation

This extends the existing ResonanceSystem mark/burst loop, CrystalCombat04F reservoir, CombatDirector damage and BONDS assists. It does not replace them. Source integration and external tests are complete for the described foundation; native animation, input, physics, save and cinematic acceptance are NOT RUN.

## Available chains

Actions mean actual qualified hostile hits, not raw button presses. Fixed crystal sidearms/rifles count as Gun; a legitimately owned signature focus (unique17–22) counts as Focus. Riposte hits count as Counter. Heavy is a distinct category but has no dedicated initial recipe.

| Recipe | Successful actions | Result |
|---|---|---|
| Twin Fang | Gun → Gun → Melee | Six bounded Resonance charge; surviving target Glassbound through original prime owner;5s partner-art opportunity |
| Riftstorm Waltz | Melee → Gun → Melee | Same shared loop with a different valid order |
| Prism Volley | Gun → Gun → earned Focus → Gun | Owned focus snapshot is retained for a partner modifier |
| Phantom Reversal | Counter → Melee → earned Focus | Riposte-based focus chain; no meter for merely holding guard |

Maximum gap3.2s between steps. Wrong order, zero damage, protected/neutral targets and invalid times do not qualify. Per-action serials deduplicate pellets, volley bodies, targets and continuous beam ticks; one action contributes once. Readied chains are consumed once and expire. At most two completed chains credit a target life. The session holds bounded attack/life receipts and refuses new entries if saturated rather than evicting paid receipts. Death/session/load resets transient chains.

A chain can still prime/charge with no companion. Ordinary melee, guns, focus behavior and existing burst rewards remain useful.

## Three earned partner arts

Lock or aim at the living hostile that completed the chain. Press the original assist binding, **U / LB+Up**. Existing BONDS validation still requires the active legitimately owned, visible, capable body, Bonded rank, same layer/range/LOS, no vehicle/fusion/downed state and available assist cooldown. Invalid synergy eligibility falls back to the original ordinary assist request; no wild/NPC ally is granted.

A committed art costs40 from the original100-cap Resonance reservoir and uses at least20s or the original longer combat-assist cooldown. It does not have a second replenishable energy bar. During the clear partner tell, repeat assist between0.12–0.95s (1.3s generous mode) to confirm. RESONANCE menu offers automatic confirmation and generous timing through local accessibility preferences. The prompt uses Pad.Tag for the active controller family and the existing keyboard binding. Pause/menu/dodge/movement bindings are retained.

| Partner | Current art/performance | Confirmed base damage before original defenses | Other outcome |
|---|---|---|---|
| Quartz guide | Cataclysm Wing: checked elevated positioning where the existing profile permits flight, breath action, Malik's offhand cast, directional impact |34 | Offensive single-target impact; focus can add a bounded secondary effect |
| Ash warden | Aegis Breaker: checked brace position, charge action, original directional brace hook |22 | At most40 protection capacity, up to3s and cleared with assist cleanup; this is no global invulnerability field |
| Storm skimmer | Prismatic Snare: higher positioning, spit action, directed contact |28 | Original bounded disruption; bosses use original resistance rules |

The guide art does not implement the proposed launch/juggle sequence. These performances reuse existing creature/cast poses with actual synchronized strike callbacks. No new recorded animation clips, bespoke cuts, crossed-human-slash art or cinematic actor lock was produced. Camera feedback is local settings-aware shake; dramatic framed shots remain a future production task. No global time freeze is added.

A missed confirmation resolves a40%-strength ordinary strike when target/body/LOS remain valid. A confirmed Eclipse variant scales damage by1.25. Boss multiplier0.65 limits both. Target immunity/defense/damage/kill credit still pass through original CharacterHealth. Companion origin prevents recursive chain credit. Hits do not duplicate ordinary weapon damage or emit repeated callback damage. Dodging, real damage, lost/invalid target/body, range/layer loss, death, title/load/cutscene transition or disable cancel. Cancellation after commitment retains cost/cooldown. Pause freezes the input window; scene/death cleanup occurs even if delta time is zero.

## Earned focus modifiers

A focus must have actually hit during the chain and its original UID must still be owned with matching canonical behavior at commitment. No new focus/unlock is granted.

| Focus | Confirmed synergy modifier |
|---|---|
| Shardwake | Up to two LOS-checked nearby hostile fragments at25% strike damage |
| Prism Thread | Glassbound precision mark for the original next-gun burst loop |
| Gyre Crescent | Short nonboss knockdown with bounded lift/push |
| Sunderheart | Heavy flag and additional poise pressure through original defense rules |
| Mirage Choir | Short resisted disruption; no new decoy actor is spawned |
| Mirrorwake | Recover10 stamina, capped at current maximum |

These are six modest tactical modifiers, not18 bespoke authored cinematics or an overhaul of all ordinary focus attacks.

## Per-companion bond tree

The BONDS record's existing Milestones list stores up to three `res04n_fang_N` and three `res04n_volley_N` successes. Successful confirmed damage is required; miss/cancel/immune targets do not progress mastery. Fang includes Twin Fang/Riftstorm, Volley includes Prism Volley/Phantom Reversal. The original roster copy/normalization preserves unknown nonempty unique milestones. Original rank/trust/source/ownership/fusion fields and gates remain unchanged.

Three successes on both paths, legitimately earned Harmonized rank, and a still-owned focus enable Eclipse Convergence:55 charge and1.25 base damage. No rank, trust, item, focus or bond is granted by mastery. The RESONANCE page displays progress. Existing save/autosave owner captures costs and milestone records; actual player save/reload remains unverified.

## Tests and native limits

ResonanceChains04NTests executes18 engine-independent cases covering recognition/order/gaps, invalid hits, attack deduplication, finite life credit, owned partner states, focus snapshot/reset, cost reservoir, prompt pause/miss/automatic/cancel/duplicate impact, bounded partner damage and mastery. Four cases explicitly cover scene/death cancellation before zero-delta pause handling. Full suite results and exact source snapshot are in TEST_LEDGER.md. Native target acquisition, rig timing, device prompts, Pause/skip, real save persistence and visual spectacle are not certified by those tests.

## Future online contract

The repository has no completed network/lobby/session/player authority stack. No online gameplay is claimed. ResonanceChains04N is an instance-based pure recognizer, currently exposed through the original single-local-player ResonanceSystem facade. A future server must own one recognizer/resource/cooldown ledger per authoritative participant; the current static local facade is not a multiplayer authority.

Before two/four-player arts ship: establish authenticated sessions and stable actor/target IDs; server-validate action nonce, timestamp/window, hit, focus entitlement, partner/participant state, range, resource and cooldown; reserve/consume resources atomically; issue one impact/reward receipt; reject replayed inputs; bound latency tolerance; cancel on death/disconnect/scene/range loss; reconcile client prediction. Presentation/camera leases must be participant-local through 04L protections, with no global slow/freeze. Competitive timing/accessibility rules require an explicit authoritative policy. Temporary human ally eligibility needs an original quest/party owner before that art is implemented.
