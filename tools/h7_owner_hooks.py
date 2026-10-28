def restore_selected_h8r(source):
 before='model.ArtProof.AssetId==MalikReviewH7.AssetId || model.ArtProof.AssetId==MalikReviewH8R.AssetId);'
 if before in source:
  if source.count(before)!=1:raise ValueError('Selected human emission identity hook is ambiguous')
  source=source.replace(before,'model.ArtProof.AssetId==MalikReviewH7.AssetId);')
 return source

"""Reverse only the three literal H7 visual additions before frozen H6/H2 owner comparison."""
def restore_h6_visual(source):
 from pathlib import Path
 from composition_lineage_wr02b import canonical_composition_bytes
 root=Path(__file__).resolve().parents[1]
 source=canonical_composition_bytes(root,'Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedVisual04OD.cs',source.encode()).decode()
 source=restore_selected_h8r(source)
 pairs=[
 ('_surfaceH6=model.ArtProof!=null && (model.ArtProof.AssetId==MalikReviewH6.AssetId || model.ArtProof.AssetId==MalikReviewH7.AssetId);','_surfaceH6=model.ArtProof!=null && model.ArtProof.AssetId==MalikReviewH6.AssetId;'),
 ('#if UNITY_EDITOR\n            PrepareMotionH7();\n#endif\n',''),
 ('                Quaternion pose=ImportedModelRegistry04OD.BindRotation04OF(_source[i].rotation,_sourceBind[i],_targetBind[i]);\n#if UNITY_EDITOR\n                pose=ComposeMotionH7(i,pose);\n#endif\n                _target[i].rotation=pose;','                _target[i].rotation=ImportedModelRegistry04OD.BindRotation04OF(_source[i].rotation,_sourceBind[i],_targetBind[i]);')]
 if 'PrepareMotionH7();'not in source:return source
 for before,after in pairs:
  if source.count(before)!=1:raise ValueError('H7 importer hook changed beyond the reviewed literal scope')
  source=source.replace(before,after)
 return source
