#!/usr/bin/env python3
"""Execute actual city and placement source until native physics is required.

Uses the disclosed 04A managed math adapter. Never substitutes flat terrain or
claims native site success. Pending runtime Interactable/court locations remain
unverified. Requires .NET 8; no Unity installation is implied.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path
import subprocess
import tempfile

ROOT=Path(__file__).resolve().parents[1]
RUNTIME=ROOT/'Assets/NeonSyndicate/Scripts/Runtime'

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet',type=Path,required=True)
    parser.add_argument('--catalog',type=Path,required=True)
    parser.add_argument('--forts',type=Path,default=ROOT/'Assets/NeonSyndicate/Resources/CrystalStrongholds02I/strongholds_catalog.json')
    parser.add_argument('--report',type=Path,required=True)
    args=parser.parse_args();dotnet=args.dotnet.resolve()
    csc=sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    refs=sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    hashes={}
    def read(p):hashes[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest();return p.read_text()
    with tempfile.TemporaryDirectory(prefix='territory04b-survey-') as directory:
        scratch=Path(directory);sources=[]
        for p in sorted((RUNTIME/'World').glob('CityLayout*.cs')):
            text=read(p)
            if p.name=='CityLayout.Frost.cs':
                end=text.index('    public static class SnowMesher');text=text[:text.rfind('    /// <summary>',0,end)]+'}\n'
            target=scratch/p.name;target.write_text(text);sources.append(target)
        text=read(RUNTIME/'Core/Util.cs');target=scratch/'UtilPure.cs';target.write_text(text[:text.index('    public static class Log')]+'}\n');sources.append(target)
        text=read(RUNTIME/'Core/Mats.cs');start=text.index('    public enum FacadeStyle');end=text.index('    /// <summary>',start)
        target=scratch/'Enums.cs';target.write_text('namespace NeonSyndicate {\n'+text[start:end]+'}\n');sources.append(target)
        paths=[RUNTIME/'World/CityTypes.cs',RUNTIME/'Gameplay/Strongholds/StrongholdData.cs',RUNTIME/'Gameplay/Strongholds/StrongholdPlacement.cs',
            RUNTIME/'Gameplay/Territories/TerritoryPlacement.cs',ROOT/'tools/visual_rebirth_04a_harness/MathAdapter.cs',
            ROOT/'tools/living_territories_04b_harness/PlacementHost.cs',ROOT/'tools/living_territories_04b_harness/PlacementProgram.cs']
        for p in paths:read(p);sources.append(p)
        dll=scratch/'Survey.dll';rsp=scratch/'compile.rsp'
        options=['-nologo','-target:exe','-langversion:9','-out:'+str(dll)]+['-r:'+str(p) for p in refs]+[str(p) for p in sources]
        rsp.write_text('\n'.join('"'+s+'"' for s in options));subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],check=True)
        dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
        catalog=json.loads(args.catalog.read_text())
        runtime_path=RUNTIME/'Gameplay/Territories/TerritorySystem.cs'
        runtime=read(runtime_path)
        id_block=re.search(r'string\[\] habitatIds = \{([^}]+)\};',runtime).group(1)
        habitat_ids=re.findall(r'"([a-z_]+)"',id_block)
        anchor_block=re.search(r'Vector3\[\] anchors = \{([^}]+)\};',runtime).group(1)
        habitat_anchors=[[float(n.strip().rstrip('f')) for n in group.split(',')] for group in re.findall(r'new Vector3\(([^)]+)\)',anchor_block)]
        if len(habitat_ids)!=4 or len(habitat_anchors)!=4:raise RuntimeError('Runtime habitat anchor extraction changed; review the source contract.')
        catalog['sites'] += [{'id':i,'anchor':a} for i,a in zip(habitat_ids,habitat_anchors)]
        combined=scratch/'survey_sites.json';combined.write_text(json.dumps(catalog))
        run=subprocess.run([str(dotnet),str(dll),str(combined),str(args.forts.resolve())],capture_output=True,text=True)
        if run.returncode not in (0,1):raise RuntimeError(run.stderr)
        report=json.loads(run.stdout);report['source_sha256']=hashes
        report['habitat_anchor_source']='Exact habitatIds and anchors arrays extracted from TerritorySystem.cs. These remain preferred, not live coordinates.'
        report['catalog_sha256']=hashlib.sha256(args.catalog.read_bytes()).hexdigest()
        report['fort_catalog_sha256']=hashlib.sha256(args.forts.read_bytes()).hexdigest()
        args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n')
        print(json.dumps({'seeds':len(report['seeds']),'failures':report['failures'],'unityPhysics':report['unityPhysics']},indent=2))
        return run.returncode

if __name__=='__main__':raise SystemExit(main())
