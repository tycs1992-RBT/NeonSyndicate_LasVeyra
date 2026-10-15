#!/usr/bin/env python3
"""Offline source/asset contracts and independent placement arithmetic. Does NOT execute C# or Unity."""
from pathlib import Path
import hashlib, json, math, re, struct, wave, sys
ROOT=Path(__file__).resolve().parents[1]
D=ROOT/'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Discovery'
RES=ROOT/'Assets/NeonSyndicate/Resources/CrystalDiscovery02G'
# The payload deliberately excludes unchanged game files. Run from an installed full project.
required = [ROOT/'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/MaterialStore.cs',
            ROOT/'Assets/NeonSyndicate/Scripts/Runtime/Core/GameRoot.cs',
            ROOT/'Assets/NeonSyndicate/Resources/CrystalCity02F/Models/terrace.obj']
if not all(p.is_file() for p in required):
    print('Run this checker from tools/ inside the installed full Unity project, not the update Payload folder.', file=sys.stderr)
    sys.exit(2)
checks=[]
def check(name,ok): checks.append({'check':name,'passed':bool(ok)})
def clean(s):
    # A lexer-level delimiter check; not a C# grammar parser or compiler.
    return re.sub(r'//[^\n]*|/\*.*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '',s,flags=re.S)
def balanced(s):
    stack=[]
    for c in clean(s):
        if c in '({[':stack.append(c)
        elif c in ')}]':
            if not stack or stack.pop()!={')':'(',']':'[','}':'{'}[c]:return False
    return not stack
sources={p.name:p.read_text() for p in sorted(D.glob('*.cs'))}
for name,s in sources.items():
    check(name+' balanced delimiters',balanced(s))
    check(name+' metadata',Path(str(D/name)+'.meta').is_file())
    check(name+' no unsupported test attribute','NonParallelizable' not in s)
    check(name+' no font resource lookup','GetBuiltinResource' not in s)
    check(name+' no dash glyphs','\u2014' not in s and '\u2013' not in s)
all_source='\n'.join(sources.values())
for token in ['Time.timeScale =','AddComponent<BoxCollider>','AddComponent<Rigidbody>','Physics.IgnoreLayerCollision','Input.GetKey','OnGUI(','SetCinematic(','RenderSettings.','PlayerPrefs.','SaveSystem.Save(','Camera.main']:
    check('no new mutation/API '+token,token not in all_source)
logic=sources['DiscoveryLogic.cs']
check('logic has no Unity dependency','UnityEngine' not in logic)
check('reservation precedes reward callback',logic.index('s.RewardMask |= bit;')<logic.index('applied = apply(kind, id, amount);'))
check('failed credit reservation restored','if (!applied) s.RewardMask &= ~bit;' in logic)
check('partial rewards tracked individually','Credit(s, 1,' in logic and 'Credit(s, 2,' in logic)
check('reentrant delivery blocked','apply == null || _delivering' in logic)
check('capture returns copied records','save.Entries.Add(Copy(' in logic)
check('old null block handled','save == null || save.Entries == null' in logic)
check('duplicate rows merged not replaced','s.RewardMask |= read.RewardMask & 3;' in logic)
check('circuit after all sites','ClaimedCount != Total' in logic)
check('site uses existing interaction','Interactable.Create' in sources['DiscoverySiteView.cs'])
check('gamepad bindings reused','Act.' not in all_source and 'Input.' not in all_source)
check('safe ground capsule probes','Physics.CheckCapsule' in sources['DiscoveryPlacement.cs'])
check('placement roads rejected','layout.RoadAt' in sources['DiscoveryPlacement.cs'])
check('placement buildings rejected','layout.IsInsideBuilding' in sources['DiscoveryPlacement.cs'])
check('walk approach validated','bool approach = false;' in sources['DiscoveryPlacement.cs'])
check('physical probe trigger ignored','QueryTriggerInteraction.Ignore' in sources['DiscoveryPlacement.cs'])
check('placement max240','points.Count > 80' in sources['DiscoveryPlacement.cs'] and 'parks.Count > 160' in sources['DiscoveryPlacement.cs'])
check('native probe work yielded','i % 6 == 5' in sources['CityDiscoverySystem.cs'])
check('world system registration','WorldSystems.Register(this)' in all_source)
check('world system cleanup','WorldSystems.All.Remove(this)' in all_source)
check('page cleanup','MenuPages.Pages.Remove(_page)' in all_source)
check('marker cleanup','Blip.Remove(MapMarker)' in all_source)
check('audio respects existing Sfx volume','Settings.SfxVolume' in sources['DiscoverySiteView.cs'])
check('no silent autosave', 'SaveSystem.Save(' not in all_source)
root_source=(ROOT/'Assets/NeonSyndicate/Scripts/Runtime/Core/GameRoot.cs').read_text()
check('boot integration exactly once',root_source.count('AddSystem<CityDiscoverySystem>')==1)
save=(ROOT/'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/SaveSystem.cs').read_text()
check('save field appended','public DiscoverySave Discovery = new DiscoverySave();' in save)
check('save frozen schema updated','"Discovery:DiscoverySave"' in (ROOT/'Assets/NeonSyndicate/Tests/EditMode/SaveDataTests.cs').read_text())
cat=json.loads((RES/'discovery_catalog.json').read_text()); check('version',cat['version']=='02G')
check('three unique IDs',len(cat['sites'])==len({d['id'] for d in cat['sites']})==3)
check('three existing landmarks',{d['landmark'] for d in cat['sites']}=={'aster_observatory','dawn_atrium','moonroot_garden'})
materials=(ROOT/'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/MaterialStore.cs').read_text()
for d in cat['sites']:
    check(d['id']+' order permutation',sorted(d['order'])==[0,1,2])
    check(d['id']+' reward exists','M("'+d['material']+'"' in materials)
    check(d['id']+' low reward bounds',1<=d['amount']<=5 and 1<=d['lumen']<=300)
    check(d['id']+' clue and memory',bool(d['inscription']) and bool(d['memory']))
check('circuit reward actual',cat['circuitMaterial']=='core_prism' and cat['circuitAmount']==1)
# Four original WAVs. Check PCM payload, peak range, and hard cut prevention.
for p in sorted((RES/'Audio').glob('*.wav')):
    with wave.open(str(p)) as w:
        check(p.name+' PCM format',w.getnchannels()==1 and w.getsampwidth()==2 and w.getframerate()==22050)
        n=w.getnframes();samples=struct.unpack('<%dh'%n,w.readframes(n))
    check(p.name+' duration',.5<n/22050<1.2)
    check(p.name+' peak/no clipping',1000<max(abs(s) for s in samples)<32760)
    check(p.name+' ends at silence',abs(samples[0])<10 and abs(samples[-1])<10)
# Compute physical footprint of the ACTUAL reused OBJ compositions, independent of Unity importer.
models=ROOT/'Assets/NeonSyndicate/Resources/CrystalCity02F/Models'
for core in (False,True):
    for name,scale,pos in [('terrace',(.13,.28,.13),(0,-.11,0)),('rune_disc',(.52,.1,.52),(0,.08,0)),('light_seed',(.12,.19,.12),(0,.46,0)),('crescent',((.22 if core else .17),)*3,(0,.10,.10))]:
        vs=[[float(x) for x in s.split()[1:4]] for s in (models/(name+'.obj')).read_text().splitlines() if s.startswith('v ')]
        converted=[[v[i]*scale[i]+pos[i] for i in range(3)] for v in vs]
        check(name+(' core' if core else ' rune')+' horizontal radius <= .6',all(abs(v[0])<=.6 and abs(v[2])<=.6 for v in converted))
        check(name+(' core' if core else ' rune')+' vertical bounds',min(v[1] for v in converted)>=-.12 and max(v[1] for v in converted)<1.2)
# Verify all newly supplied GUIDs are 32 hexadecimal characters and unique to their paths.
guid_paths={}
for p in list(D.glob('*.meta'))+[Path(str(D)+'.meta')]+list(RES.rglob('*.meta'))+[Path(str(RES)+'.meta'),ROOT/'Assets/NeonSyndicate/Tests/EditMode/Discovery02GTests.cs.meta']:
    guid=re.search(r'^guid: ([0-9a-f]+)$',p.read_text(),re.M)
    check(p.name+' GUID32',guid is not None and len(guid.group(1))==32)
    if guid:check(p.name+' GUID unique',guid.group(1) not in guid_paths);guid_paths[guid.group(1)]=str(p)
report={'scope':'Executed offline source and asset checks, not C# compilation, PowerShell execution or a Unity run',
'checks':len(checks),'passed':sum(c['passed'] for c in checks),'failed':[c for c in checks if not c['passed']],
'native_compilation':'NOT RUN','native_editmode_tests':'NOT RUN','live_playtest':'NOT RUN','performance':'NOT MEASURED',
'files_sha256':{n:hashlib.sha256(s.encode()).hexdigest() for n,s in sources.items()},'details':checks}
text=json.dumps(report,indent=2)+'\n';print(text)
if '--report' in sys.argv:Path(sys.argv[sys.argv.index('--report')+1]).write_text(text)
sys.exit(bool(report['failed']))
