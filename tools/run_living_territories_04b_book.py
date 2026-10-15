#!/usr/bin/env python3
"""Run actual engine-free territory ledger NUnit tests without Unity or native mocks."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
ROOT=Path(__file__).resolve().parents[1]

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet',type=Path,required=True)
    parser.add_argument('--nunit',type=Path,required=True)
    parser.add_argument('--report',type=Path,required=True)
    args=parser.parse_args();dotnet=args.dotnet.resolve()
    csc=sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    refs=sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))+[args.nunit.resolve()]
    relative=['Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Territories/TerritoryData.cs',
        'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Territories/TerritoryBook.cs',
        'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Quests/QuestJson.cs',
        'Assets/NeonSyndicate/Tests/EditMode/TerritoryBookTests.cs','tools/living_territories_04b_harness/BookProgram.cs']
    with tempfile.TemporaryDirectory(prefix='04b-book-tests-') as directory:
        scratch=Path(directory);dll=scratch/'BookTests.dll';rsp=scratch/'compile.rsp'
        options=['-nologo','-target:exe','-langversion:9','-out:'+str(dll)]+['-r:'+str(p) for p in refs]+[str(ROOT/p) for p in relative]
        rsp.write_text('\n'.join('"'+s+'"' for s in options));subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],check=True)
        shutil.copyfile(args.nunit,scratch/args.nunit.name)
        dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
        run=subprocess.run([str(dotnet),str(dll)],cwd=ROOT,text=True,capture_output=True)
        if run.returncode not in (0,1):raise RuntimeError(run.stderr)
        report=json.loads(run.stdout);report['source_sha256']={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in relative}
        report['catalog_sha256']=hashlib.sha256((ROOT/'Assets/NeonSyndicate/Resources/LivingTerritories04B/territories_catalog.json').read_bytes()).hexdigest()
        args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n')
        print(json.dumps({'executed':len(report['tests']),'failures':report['failures'],'unityEditMode':'NOT RUN'},indent=2))
        return run.returncode

if __name__=='__main__':raise SystemExit(main())
