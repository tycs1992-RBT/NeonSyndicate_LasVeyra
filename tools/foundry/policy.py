"""Engine-free import/collider admission policy. Candidate declarations never authorize live physics."""
import math

def collider_candidates(brief,bounds):
 boxes=brief.get('colliderCandidateBoxes',[]);policy=brief.get('colliderPolicy')
 if policy=='ORIGINAL_AUTHORITY_ONLY':
  if boxes:raise ValueError('Original-owner-only assets cannot add collider candidates.')
  return []
 if policy!='EXPLICIT_CANDIDATES_ONLY':raise ValueError('Unknown collider policy.')
 result=[]
 for box in boxes:
  center=box.get('center',[]);size=box.get('size',[])
  if len(center)!=3 or len(size)!=3 or not box.get('sourceOwner')or not all(isinstance(x,(float,int))and math.isfinite(x)for x in center+size)or min(size)<=0:raise ValueError('Invalid explicit collider declaration.')
  lo=[center[i]-size[i]/2 for i in range(3)];hi=[center[i]+size[i]/2 for i in range(3)]
  if any(lo[i]<bounds[0][i]-.001 or hi[i]>bounds[1][i]+.001 for i in range(3)):raise ValueError('Collider candidate outside declared source geometry.')
  for portal in brief.get('requiredPortalClearances',[]):
   if all(lo[i]<portal['max'][i]and hi[i]>portal['min'][i]for i in range(3)):raise ValueError('Collider candidate blocks required portal clearance.')
  result.append(dict(box,nativeAccepted=False,enabledInUnity=False))
 return result
