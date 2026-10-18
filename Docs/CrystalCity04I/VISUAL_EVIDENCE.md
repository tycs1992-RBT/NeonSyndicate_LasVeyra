# 04I visual implementation evidence

No current Unity screenshot or gameplay render was captured. Editor startup did not reach a valid project import. Historical screenshots in the supplied reference archive are not 04I after images. The following measurements execute the real C# builders on genuine managed modules; vertex counts establish changed geometry and budgets, not visual quality or frame-time gains.

| Deterministic character | Detail | 04H vertices | 04I vertices |
|---|---:|---:|---:|
| Malik | 2 | 11616 | 11938 |
| Malik | 1 | 6729 | 6961 |
| Blade Warden | 1 | 5914 | 6274 |
| Heavy Sentinel | 2 | 12317 | 12917 |
| Focus Weaver | 1 | 6711 | 7191 |
| Rift duelist | 1 | 6710 | 7070 |
| Warden crowd | 0 | 3036 | 3036 |

Malik gains a longer split rear coat silhouette, fitted cuffs and changed skin planes. Corin's service harness adds 120 vertices; Tamsin's curved bib/pocket layer adds 208. Existing 04H hair and 04F fingers are preserved, not counted as new 04I work.

| Companion, seed 61724, detail 1, evolution 0 | 04H vertices | 04I vertices | Skeleton bones unchanged |
|---|---:|---:|---:|
| Quartz Drakeling | 11383 | 11727 | 48 |
| Ashshell Warden | 5898 | 6378 | 32 |
| Stormskimmer | 5084 | 5484 | 39 |

All three evolution stages are included in the JSON evidence. Event drakes share the Quartz build path. Scutes/frills/sails are geometry, not additional lights or particles.

Ventwing high/low grows3348/1628 to4188/1884 vertices; Kilnback2192/1264 to3032/1520; Coilrunner2948/1700 to3788/1956. The three original chassis classes keep their part counts and rider/leg spaces.

For each Lofts/Dispatch/Refuge venue, the added far geometry is896 vertices; Medium near2320, High3312, Ultra4080. Pressure House adds1016 far and2440/3432/4200 near because it has a third overhead service run. Runtime Low selects far, not the probe's near candidate. These additions use existing material batches and no new collision. Vault peaks stay below4.67m; portal crossing geometry stays above the original door route. External tests check x/z envelopes, floor clearances and finite normals. Actual player/companion collision remains a native check.

## Matched native captures to produce

Use separate 04H and 04I checkouts, seed 61724, 1920x1080, clear weather,15:30 and identical Low/Medium/High/Ultra settings. Record commit, pipeline, actor seed, evolution and camera transform. Repeat22:00, interior and Underheart lighting. Use the existing PlayerCamera.SetCinematic and QA golden capture tools only in a real Play Mode run.

| View | Reproducible camera recipe | Compare |
|---|---|---|
| Arrival | New Free Roam, original Veyra Lofts spawn, Mid view, FootPitch 7.5, unchanged player yaw | Coat silhouette and Lofts/Dispatch portals |
| Malik close | position=feet+forward*1.4+up*1.6; look=feet+up*1.55; FOV 50 | Orbital/chin planes, clothing boundaries; idle and guard |
| Malik side/back | position=feet+right*2+up*1.2; look=feet+up*1.1; FOV 50 | Split coat during walk, stop, turn, dodge, stairs and riding |
| Dispatch/Pressure portal | local position=(0,1.65,-8), look=(0,2.6,0); FOV 60, transformed by actual venue binding | Supported carved portal and clear entrance |
| Dispatch/Pressure room | local position=(0,1.65,2), look=(0,3.2,12); FOV 65 | Full vault, service runs, material response and navigation |
| Corin/Tamsin | same real giver post, camera3m in front at1.5m height; FOV 50 | Harness/bib, held tool, inspection and dialogue gestures |
| Companion | same species/seed/evolution, mid third-person then3m side view | New jaw/breast scutes, frills/sails through flight, land and assist |
| Fusion | same owned chassis/class, parked and moving on same route | New fairing depth, original wing/coil/brace transitions and rider clearance |

The source-to-render call chains are in ART_AND_RIG_PIPELINE.md. No reference-fidelity, cinematic-quality or whole-city overhaul acceptance is asserted without these native images.
