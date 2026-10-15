# Build 02I verification

## Performed here

See the adjacent JSON reports for exact counts and results. Asset checks inspect actual CMH1 headers, indices, finite positions/normals/UVs, triangle areas/winding, matching OBJ geometry, textures, metadata uniqueness and content hashes. Source-contract checks inspect known integration calls and the five intended edits. They also compare the unchanged baseline files byte-for-byte.

Independent Python fort-ledger tests exercise the intended capture/ownership rules, invalid commands, cancellation, persistence, duplicated credits and bounded flight progression. These are reference-model tests, NOT execution of the C# code. The installer-policy tests use a Python simulation to check validation, refusal, restore and injected copy failure. They do NOT execute Windows PowerShell.

## Not performed here

Unity compilation, the newly included NUnit tests, native physics, model rendering inside Unity, controller input, a full live raid, actual save restoration while standing on a tower, domain-reload modes, Windows PowerShell execution and CPU/GPU frame profiling remain untested. No review score, AAA rating, 60 FPS promise or network certification is implied.

## Local acceptance

- Import in Unity 6000.3.16f1 with no new red errors. Do not change editor/package versions to suppress a compile error.
- Boot Free Story and check `Crystal Strongholds 02I | Ready. n/3 forts.` Open STRONGHOLDS. Confirm unavailable sites are clearly reported.
- Track K, enter the gate, claim home, open the pause page beside the core and test all four modules and Empty. Verify all rotations and themes.
- Walk the courtyard, fly onto the watchtower deck and flight dais, land, take off and inspect camera collision. Decorative trim has simplified physics.
- Try building from far away, at an unowned fort, during combat and over an occupied socket. These requests should be refused.
- Opt into a rival challenge. Verify two wardens appear, existing weapons/powers damage them, and no capture occurs when one is merely despawned.
- Defeat both, begin capture, step away, take damage, pause/resume and finish the five-second capture. Check only the appropriate actions reset or freeze the clock.
- Infuse at home; confirm one-time gain and a maximum two echoes. Measure flight without aiming, then while aiming. No unlocks or aim speed changes should occur.
- Save pending echo/module/theme changes using the normal save UI. Load that save, and also test an old save without Strongholds. Active raids should not resume half-won.
- Leave the raid area, enter a vehicle, die or return to title. Verify tagged defenders are cleaned up and previous owned bases stay owned in a saved game.
- Stop/start Play with domain reload enabled and disabled. Check no duplicate forts, stale map markers or lingering registered callbacks.
- Check Low and High graphics, day/night lights, original 02F landmarks, 02G Discovery, driving and ordinary missions for regressions.

## Known limitations

Some seeds may not provide all safe plots. The planner checks existing layout points and physics, not future dynamic spawn reservations. Unrelated ambient dangers are not disabled around a claimed base. The fort is an outdoor compound, not an enterable furnished home with relocated companions. Construction is six fixed sockets, not arbitrary walls/platform placement. Fort structures are not destructible. The guardians are existing creature species, not newly rigged characters.

## Rollback

The installer prints a dated backup path under _CrystalManorBackups next to the project. Close Unity before running its RESTORE_02I.cmd. The guarded restore refuses to overwrite files edited after installation. Preserve that backup and local work if it stops. Restoring older code does not restore or overwrite game save files; avoid saving over your only fort-enabled save from older code. No Git history or Library folders are deleted.
