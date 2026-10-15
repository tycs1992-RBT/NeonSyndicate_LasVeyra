"""Offline lexical/source checks only. This program is not a C# compiler or Unity Test Runner."""
from pathlib import Path
import hashlib, json, re
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Strongholds'
TEST=ROOT/'Assets/NeonSyndicate/Tests/EditMode/CitadelArt03A2Tests.cs'
FILES=[SRC/name for name in ('CitadelGeometry03A2.cs','CitadelKit03A2.cs','CitadelLayout03A2.cs','StrongholdView.Citadel03A2.cs','StrongholdView.cs')]+[TEST]
checks=[]
def check(label,result):
    checks.append({'name':label,'passed':bool(result)})
def stripped(s):
    return re.sub(r'//[^\n]*|/\*.*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '',s,flags=re.S)
def balanced(s):
    stack=[]
    for c in stripped(s):
        if c in '([{': stack.append(c)
        elif c in ')]}':
            if not stack or stack.pop()!={')':'(',']':'[','}':'{'}[c]:return False
    return not stack
for f in FILES:
    s=f.read_text()
    check(f.name+' balanced lexical delimiters',balanced(s))
    check(f.name+' ASCII project convention',s.isascii())
    check(f.name+' no conflict markers',not re.search(r'(?m)^([<=>])\1{6}',s))
    check(f.name+' no unavailable NUnit attribute','NonParallelizable' not in s)
    check(f.name+' explicit types convention',not re.search(r'\bvar\s+\w+\s*=',stripped(s)))
expected='c6e2466f4ef633a64f8f419fc6dd9322746fb0c4'
b=(SRC/'StrongholdView.cs').read_text()
reverse=b.replace('public sealed partial class StrongholdView : IDisposable','public sealed class StrongholdView : IDisposable').replace('                TryDecorateCitadel(staged.transform,state);\n','').replace('                    if(_citadelActive)_label.text+="\\nCITADEL RENAISSANCE 03A2";\n','').replace('            _citadelKit.Dispose();\n','')
a=reverse
baseline=a.encode('utf-8')
check('reverse patch recovers exact uploaded baseline blob',hashlib.sha1(b'blob '+str(len(baseline)).encode()+b'\0'+baseline).hexdigest()==expected)
check('four patch anchors present',all(x in b for x in ('sealed partial class StrongholdView','TryDecorateCitadel(staged.transform,state);','if(_citadelActive)_label.text','_citadelKit.Dispose();')))
check('hook occurs once',b.count('TryDecorateCitadel(staged.transform,state);')==1)
check('hook precedes atomic swap',b.index('TryDecorateCitadel(staged.transform,state);')<b.index('GameObject previous = _art'))
check('collision calls unchanged',re.findall(r'(?:Box|Cylinder)\(staged\.transform[^;]*;',a)==re.findall(r'(?:Box|Cylinder)\(staged\.transform[^;]*;',b))
new='\n'.join(f.read_text() for f in FILES if f.name not in ('StrongholdView.cs','CitadelArt03A2Tests.cs'))
for token in ['AddComponent<BoxCollider>','AddComponent<Rigidbody>','AddComponent<MeshCollider>','Time.timeScale','PlayerPrefs','File.Write','Physics.','Network','OnGUI','Input.Get','Game.Player.','SaveData','Resources.Load','UnityEditor']:
    check('new visual code avoids '+token,token not in new)
check('no mutable static caches',not re.search(r'\bstatic\s+(?:readonly\s+)?(?:Dictionary|List|HashSet)<[^;\n]+>\s+\w+\s*(?:=|;)',new))
partial=(SRC/'StrongholdView.Citadel03A2.cs').read_text()
for token in ['OwnedMeshes.Track','mesh.UploadMeshData(true)','new LOD(.095f','new LOD(.008f','catch(Exception error)','decoration.SetActive(false)','TryDecorateCitadel','_citadelFailureReported']:
    check('lifecycle '+token,token in partial)
check('source meshes remain readable','UploadMeshData(true)' not in (SRC/'CitadelGeometry03A2.cs').read_text())
check('all layout part model names exist',set(re.findall(r'Add\([^,]+,"(\w+)"',(SRC/'CitadelLayout03A2.cs').read_text()))<=set(re.findall(r'case "(\w+)"',(SRC/'CitadelGeometry03A2.cs').read_text())))
check('local single-player state remains read only',not re.search(r'\bstate\.\w+\s*=(?!=)',new))
check('low quality omits fine details','part.Fine&&Settings.Graphics==GraphicsPreset.Low' in partial)
check('source normal area guard','n.sqrMagnitude < 1e-12f' in new)
check('runtime normal map is linear','kind=="normal")' in new)
check('material clones not shared mutation','material=new Material(source)' in new)
check('no extra art error pause spam','Log.Exception' not in partial and 'Log.Warn' in partial)
# Reference checks of the geometric envelope formula, not execution of C#.
import math
check('gate aperture wider than six metres',2*(3.7-.30*1.8)>6)
check('gate base ornamental arch above head height',2.42>2.2)
check('rear arch apex under original probe ceiling',2.15+(1+math.sqrt(3))*4*1.12<14.95)
check('tree canopy approximate envelope within plot',16.85+(1.65+.83)*2.2<23)
for rot in range(4):
    x,z=1.5,2.25
    for _ in range(rot):x,z=z,-x
    check('module rotation '+str(rot)+' preserves radius',abs(x*x+z*z-(1.5**2+2.25**2))<1e-8)
result={'status':'SOURCE_AUDIT_PASSED' if all(c['passed'] for c in checks) else 'FAILED','baseline_commit':'4a4c26443b8fda3dbfd30f09e612989f9bbd9f5e','scope':'Lexical/source preservation checks and independent envelope arithmetic. Not a compiler or native Unity run.','checks':checks,'passed':sum(c['passed'] for c in checks),'total':len(checks),'unity_compile':'NOT RUN','unity_tests':'INCLUDED, NOT RUN','unity_visuals':'NOT RENDERED','performance':'NOT MEASURED','source_sha256':{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in sorted(FILES)}}
# Print results; do not modify the user's repository or prior reports.
print(json.dumps({k:v for k,v in result.items() if k not in ('checks','source_sha256')},indent=2))
for c in checks:
    if not c['passed']:print('FAIL:',c['name'])
raise SystemExit(0 if result['status']=='SOURCE_AUDIT_PASSED' else 1)
