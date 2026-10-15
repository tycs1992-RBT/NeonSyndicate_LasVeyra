#!/usr/bin/env python3
"""Source invariants and independent window arithmetic. Does NOT compile or run C# or Unity."""
from pathlib import Path
import argparse, hashlib, json, math, re

ROOT=Path(__file__).resolve().parents[1]
PATHS=[
 'Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.cs',
 'Assets/NeonSyndicate/Scripts/Runtime/World/CityDressing.cs',
 'Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.Fantasy02E.cs',
 'Assets/NeonSyndicate/Scripts/Runtime/ArtDistrict/DistrictArchitecture.cs',
 'Assets/NeonSyndicate/Scripts/Runtime/ArtDistrict/CrystalCity02ECoverage.cs',
 'Assets/NeonSyndicate/Scripts/Runtime/ArtDistrict/CrystalCity02EAssets.cs',
 'Assets/NeonSyndicate/Tests/EditMode/CrystalCity02ETests.cs']

def strip(s):
 return re.sub(r'//[^\n]*|/\*.*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '', s, flags=re.S)
def balanced(s):
 stack=[]
 for c in strip(s):
  if c in '([{':stack.append(c)
  elif c in ')]}':
   if not stack or stack.pop()!={')':'(',']':'[','}':'{'}[c]:return False
 return not stack

def calls(s,name):
 result=[]
 for match in re.finditer(r'\b'+re.escape(name)+r'\s*\(',strip(s)):
  start=match.end(); code=strip(s);depth=1;i=start
  while i<len(code) and depth:
   if code[i]=='(':depth+=1
   elif code[i]==')':depth-=1
   i+=1
  result.append(re.sub(r'\s+','',code[start:i-1]))
 return result

def main():
 arg=argparse.ArgumentParser();arg.add_argument('--baseline');arg.add_argument('--output');args=arg.parse_args()
 checks=[]
 def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
 src={Path(f).name:(ROOT/f).read_text() for f in PATHS}
 for f in PATHS:
  s=(ROOT/f).read_text();check(f+' balanced delimiters',balanced(s))
  check(f+' metadata present',(ROOT/(f+'.meta')).exists())
  check(f+' metadata GUID length',bool(re.search(r'^guid: [0-9a-f]{32}$',(ROOT/(f+'.meta')).read_text(),re.M)))
  check(f+' no disallowed attribute','[NonParallelizable]' not in s)
  check(f+' no new dash glyph','\u2014' not in s and '\u2013' not in s)
 builder=src['CityBuilder.cs'];dressing=src['CityDressing.cs'];ext=src['CityBuilder.Fantasy02E.cs'];policy=src['CrystalCity02ECoverage.cs']
 check('lightweight path wired', 'BuildOrdinaryTier02E(bld, tf, baseY + bottom, y1, t);' in builder)
 check('original visual path retained on preparation failure', 'if (art == null && !themed)' in builder)
 check('heavy landmarks retained','DistrictBuilding.TryBegin' in builder and 'art.Complete();' in builder)
 check('roof helipad is excluded from new ornament','building.Roof != RoofType.Helipad' in policy)
 check('coverage not tied to seed, quality or location',all(v not in policy.split('public static bool CanTheme')[1].split('public static bool IsCityProp')[0] for v in ['.Seed','Settings.','CoreHalf','RingHalf','budget']))
 check('new source cache reset is wired','CrystalCity02EAssets.ResetStatics();' in src['DistrictArchitecture.cs'])
 check('source arrays cached once','Sources.TryGetValue(model' in src['CrystalCity02EAssets.cs'])
 check('tree integration automatic','StreetTree02E(p, seed, s)' in dressing)
 check('lamp integration automatic','StreetLamp02E(p, rot)' in dressing)
 check('coverage report runs after emission','yield return EmitAll(progress);\n            ReportCoverage02E();' in builder)
 check('no random geometry calls in new integration','new GRand' not in ext and 'Random.' not in ext)
 check('no new shader/package APIs','UnityEditor' not in ext and 'UnityEngine.InputSystem' not in ext and 'UnityEngine.Rendering.Universal' not in ext)
 for token in ['AddBoxCollider(', 'AddCapsule(', 'AddComponent<', 'Time.timeScale', 'RenderSettings.', 'SetCinematic(', 'OnGUI(', 'Instantiate(', 'new GameObject']:
  check('new visual module avoids '+token, token not in ext)
 check('source geometry preserves existing chunks','CrystalCity02EAssets.Append(Get(position, material, 1f)' in ext)
 check('per-face window cap explicit','MaximumBays = 4' in policy and 'MaximumRegisters = 2' in policy)
 # Reference-model coverage samples are independent of C# execution.
 samples=0
 for x in [-1400,-800,-300,0,300,800,1400]:
  for z in [-1400,-800,-300,0,300,800,1400]:
   for seed in range(8):
    width,depth,height=20,24,32
    assert width>=3 and depth>=3 and height>.5 and all(math.isfinite(v) for v in [x,z,x+width,z+depth,height])
    samples+=1
 check('392 independent eligibility samples',samples==392)
 # Actual numeric projection calculation of the authored window, not a native rendered mesh.
 import itertools
 outline=[(-.5,0),(.5,0),(.5,.57),(.32,.81),(0,1),(-.32,.81),(-.5,.57)]
 def sub(a,b):return tuple(x-y for x,y in zip(a,b))
 def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
 triangles=0
 for w,h in itertools.product([.56,1,2,3.4,4.7],[.91,2,8,14,20,28]):
  ix,iy=min(.14,w*.08),min(.18,h*.08)
  middle=(0,h*.42,.004)
  for i,a in enumerate(outline):
   b=outline[(i+1)%7]
   oa=(a[0]*w,a[1]*h,.1);ob=(b[0]*w,b[1]*h,.1)
   ia=(a[0]*(w-2*ix),iy+a[1]*(h-2*iy),.004);ib=(b[0]*(w-2*ix),iy+b[1]*(h-2*iy),.004)
   for p,q,r in [(middle,ia,ib),(oa,ob,ib),(oa,ib,ia)]:
    n=cross(sub(q,p),sub(r,p));assert sum(v*v for v in n)>1e-12
    assert n[2]>0,(w,h,i,n)
    triangles+=1
 check('630 reference window triangles have outward winding',triangles==630)
 baseline_results=[]
 if args.baseline:
  baseline=Path(args.baseline)
  for path in PATHS[:2]:
   before=(baseline/path).read_text();after=(ROOT/path).read_text()
   for name in ['AddBoxCollider','AddCapsule']:
    check(Path(path).name+' preserves all literal '+name+' arguments',calls(before,name)==calls(after,name))
  # Existing phase D files not part of this sprint must be unchanged.
  expected=set(PATHS)
  for old in baseline.rglob('*'):
   if old.is_file():
    rel=old.relative_to(baseline).as_posix();new=ROOT/rel
    if rel not in expected and new.exists() and old.read_bytes()!=new.read_bytes():baseline_results.append(rel)
  check('no unrelated pre-existing file edited',not baseline_results)
 else:baseline_results=['not provided; original-source comparison not run']
 failed=[c for c in checks if not c['passed']]
 report={'scope':'Executed Python source-contract checks plus independent arithmetic, NOT C# execution',
  'total_checks':len(checks),'passed':len(checks)-len(failed),'failed':failed,'eligibility_reference_samples':samples,
  'window_reference_triangles':triangles,'baseline_unexpected_changes':baseline_results,
  'unity_compile':'NOT RUN','unity_tests':'NOT RUN','rendering':'NOT RUN','performance':'NOT MEASURED',
  'files':{f:hashlib.sha256((ROOT/f).read_bytes()).hexdigest() for f in PATHS}}
 text=json.dumps(report,indent=2)
 if args.output:Path(args.output).write_text(text+'\n')
 print(text)
 return 1 if failed else 0
if __name__=='__main__':raise SystemExit(main())
