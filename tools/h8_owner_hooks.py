"""Undo only two literal H8 calls for frozen predecessor comparisons. All original bytes remain checked."""
def restore_owner_h8(path,content):
 pairs={
 'Assets/NeonSyndicate/Scripts/Runtime/Characters/HumanoidPose.cs':('            AuthoredBodyH8(ref p,ref b,dt);\n',''),
 'Assets/NeonSyndicate/Scripts/Runtime/Characters/HumanoidAnimator.cs':('#if UNITY_EDITOR\n            PrepareFullBodyH8();\n#endif\n','')}
 if path not in pairs:return content
 text=content.decode('utf-8');before,after=pairs[path]
 if text.count(before)!=1:raise ValueError('H8 owner changed beyond its exact reviewed pose hook: '+path)
 return text.replace(before,after).encode()
