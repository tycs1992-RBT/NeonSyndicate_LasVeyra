# GideoVamesAI: Crystal Manor Saga
## Visual Renaissance 01C, playtest corrections

This is the complete Visual Renaissance Unity source project with scoped combat, starter-area, handling, habitat and architecture changes. It continues 01B and Master Build 01. No blank scene or replacement gameplay project was created.

**Unity has not been run for this revision here.** Compilation, live driving feel, five-minute peaceful exploration, two-minute creature discovery, visual approval and desktop performance remain untested. The reports distinguish implemented source from observed results.

## First review session

1. Extract the ZIP to a new folder, keeping your tested build for comparison. Add the folder containing `Assets`, `Packages` and `ProjectSettings` in Unity Hub.
2. Open with the recorded **Unity 6000.3.16f1**. Let the existing setup and asset imports complete. Use the current default Built-in renderer first.
3. In Settings, select **Story** for the welcoming balance. Story is the default for fresh settings. Your previously saved difficulty choice is deliberately retained, so an existing profile may still show Balanced or Veteran.
4. Choose **Free Roam**, or use the existing **Start After the Prologue** option. The original story opening is preserved and remains a scripted sequence. The habitat waits until free exploration, then settles near the actual player start.
5. Walk around the home or clinic. Their existing nearby building frontages are selected for the stronger fantasy treatment. Asterfall Promenade also remains at Core Avenue, approximately x22 to x238 around z0.
6. Look for the nearby sanctuary, approach a calm creature and use the existing interact control shown by its prompt. Observation adds bestiary knowledge and an acknowledgement pose. The practice marker offers one optional creature encounter; no practice loot or experience is granted.
7. Test low-speed turns and recovery in a car and a motorcycle using the same controls as before. Keep an unchanged copy of the previous build openable for the matched tests below.

## Read the current reports

- [Exact changes and before/after behavior](Docs/Renaissance01C/EXACT_CHANGES.md)
- [Visual source comparison and reference targets](Docs/Renaissance01C/VISUAL_COMPARISON.html)
- [QA report and all seven acceptance tests](Docs/Renaissance01C/QA_REPORT.md)
- [Police spawning and wanted audit](Docs/Renaissance01C/POLICE_AUDIT.md)
- [Combat and aggression audit](Docs/Renaissance01C/COMBAT_AUDIT.md)
- [Random encounter and chest-guard audit](Docs/Renaissance01C/AMBIENT_DIRECTOR_AUDIT.md)
- [Vehicle handling audit and numerical comparisons](Docs/Renaissance01C/VEHICLE_AUDIT.md)
- [Current asset inventory](Docs/Renaissance01C/ASSET_INVENTORY.md)
- [Remaining production-quality art](Docs/Renaissance01C/REMAINING_PRODUCTION_ASSETS.md)

Protected starting areas quiet unsolicited aggression. They do not erase wanted levels, override a chosen challenge or stop scripted mission opponents. Deliberate aggression opens a local 60-second response window. Ordinary combat and crimes still have consequences.

The existing first-person/third-person controls, story positions, save schema, road layout, traffic routes and vehicle roster are retained. No new external Unity package was added. `Docs/ArtDistrict` and `Docs/ArtDistrict01B` are preserved historical reports; they are not current 01C playtest results.
