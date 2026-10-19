#!/usr/bin/env python3
"""Export actual 04L facial deltas, Malik/Luma skins and canonical drake skins. No Unity render or FBX claim."""
import export_motion04k_art as pipeline
pipeline.PROGRAM=pipeline.PROGRAM.replace('vertices=d.Vertices.Select(V),normals=',
 'face_shapes=d.FaceShapes04L.ToDictionary(k=>k.Key,v=>v.Value.Select(V)),vertices=d.Vertices.Select(V),normals=')
pipeline.PROGRAM=pipeline.PROGRAM.replace('  foreach(string id in BondIds.Archetypes)',
 '''  look=Appearance.Luma();rig=RigLayout.From(look);
  File.WriteAllText(Path.Combine(dir,"luma.json"),JsonSerializer.Serialize(Skin(BodyBuilder.Build(look,rig,2),RigLayout.Names,RigLayout.Parent,rig.World)));
  foreach(string id in BondIds.Archetypes)''')
if __name__=='__main__':pipeline.main()
