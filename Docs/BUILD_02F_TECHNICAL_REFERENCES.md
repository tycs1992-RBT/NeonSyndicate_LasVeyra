# Technical references checked for 02F

Primary documentation consulted for integration, not evidence of successful local Unity execution:

- Unity 6000.3 Mesh.CombineMeshes: readable source requirements and material/matrix batching.
  https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/mesh/combinemeshes
- Unity 6000.3 Renderer.SetPropertyBlock: per-renderer overrides. The final sprint does not require new property blocks; it uses shared materials and a few local lights.
  https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/renderer/setpropertyblock

Project API integration was checked against the provided game sources: CityBuilder, DistrictArchitecture, Mats, CityTypes, CityLayout.Graph, Game, Atmosphere, OwnedMeshes and Blip. Project code, not a generic engine tutorial, determined the actual boot, material and marker calls.
