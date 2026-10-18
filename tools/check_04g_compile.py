#!/usr/bin/env python3
"""Managed API matrix using an exact installed Editor's genuine assemblies.
This does not run Unity, import a project, render, or produce a native player.
"""
import argparse, concurrent.futures, hashlib, json, subprocess
from pathlib import Path


def main():
    a=argparse.ArgumentParser(description=__doc__)
    a.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1])
    a.add_argument('--dotnet',type=Path,required=True)
    a.add_argument('--editor-data',type=Path,required=True)
    a.add_argument('--packages',type=Path,required=True,help='Genuine compiled template package assemblies from this Editor')
    a.add_argument('--nunit',type=Path,required=True)
    a.add_argument('--input-system',type=Path,required=True,help='Verified project-pinned Input System 1.17.0 assembly')
    a.add_argument('--output',type=Path,required=True)
    args=a.parse_args(); root=args.root.resolve(); out=args.output.resolve(); out.mkdir(parents=True,exist_ok=True)
    csc=sorted(args.dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    bcl=list(args.dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    engine=args.editor_data/'Managed/UnityEngine'
    player=args.editor_data/'PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed'
    assert (engine/'UnityEditor.CoreModule.dll').is_file() and (player/'UnityEngine.CoreModule.dll').is_file()
    groups={}; digest=hashlib.sha256()
    for kind,base in [('runtime','Assets/NeonSyndicate/Scripts/Runtime'),('editor','Assets/NeonSyndicate/Scripts/Editor'),('tests','Assets/NeonSyndicate/Tests/EditMode')]:
        groups[kind]=[]
        for p in sorted((root/base).rglob('*.cs')):
            rel=p.relative_to(root); data=p.read_bytes(); digest.update(rel.as_posix().encode()+b'\0'+data+b'\0')
            dst=out/'sources'/rel; dst.parent.mkdir(parents=True,exist_ok=True); dst.write_bytes(data); groups[kind].append(dst)
    def compile_case(case):
        editor,mode,urp=case; label=('editor' if editor else 'player')+'-'+mode+'-'+('urp' if urp else 'builtin')
        refs={p.name:p for p in bcl}; refs.update({p.name:p for p in (engine if editor else player).glob('*.dll') if p.name.startswith(('UnityEngine','UnityEditor'))})
        refs['nunit.framework.dll']=args.nunit
        if editor: refs.update({p.name:p for p in (args.editor_data/'Managed').glob('Unity*.dll') if p.name not in ('UnityEngine.dll','UnityEditor.dll')})
        if mode!='classic': refs['Unity.InputSystem.dll']=args.input_system
        if urp:
            refs.update({p.name:p for p in args.packages.glob('*.dll') if p.name.startswith(('Unity.Render','Unity.Mathematics','Unity.Collections','Unity.Burst','Unity.ShaderGraph'))})
        missing=[str(p) for p in refs.values() if not p.is_file()]
        if missing:return {'name':label,'status':'BLOCKED','missing':missing}
        symbols=['UNITY_STANDALONE_WIN','UNITY_64','NS_INPUT_SYSTEM']
        if editor: symbols+=['UNITY_EDITOR','UNITY_EDITOR_WIN','UNITY_INCLUDE_TESTS']
        if mode!='input-only':symbols+=['ENABLE_LEGACY_INPUT_MANAGER']
        if mode!='classic':symbols+=['ENABLE_INPUT_SYSTEM']
        if urp:symbols+=['NS_URP']
        files=groups['runtime']+groups['tests']+(groups['editor'] if editor else [])
        rsp=out/(label+'.rsp'); dll=out/(label+'.dll')
        opts=['-nologo','-target:library','-langversion:9','-define:'+','.join(symbols),'-out:'+str(dll)]
        opts+=['-r:'+str(p) for p in refs.values()]+[str(p) for p in files]
        rsp.write_text('\n'.join('"'+v+'"' for v in opts))
        run=subprocess.run([str(args.dotnet),str(csc),'@'+str(rsp)],capture_output=True,text=True)
        log=run.stdout+run.stderr; (out/(label+'.log')).write_text(log)
        return {'name':label,'status':'PASS' if run.returncode==0 else 'FAIL','exit_code':run.returncode,'source_files':len(files),
            'errors':[v.replace(str(out/'sources')+'/', '') for v in log.splitlines() if ': error ' in v],
            'warning_count':sum(': warning ' in v for v in log.splitlines()),
            'reference_hashes':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in refs.values() if p.name.startswith(('Unity','nunit'))}}
    cases=[(e,m,u) for e in (False,True) for m in ('classic','both','input-only') for u in (False,True)]
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool: rows=list(pool.map(compile_case,cases))
    report={'scope':__doc__.strip(),'source_sha256':digest.hexdigest(),'baseline_unity':(root/'ProjectSettings/ProjectVersion.txt').read_text().strip(),'configurations':rows,'invocation':__import__('sys').argv,'native_unity':'NOT EXECUTED by this managed API checker; see separate native environment evidence'}
    (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
    for row in rows:
        print(row['name'],row['status'])
        if row['status']=='FAIL':print('\n'.join(row['errors'][:8]));print('Error count:',len(row['errors']))
    return 0 if all(r['status']=='PASS' for r in rows) else 1
if __name__=='__main__':raise SystemExit(main())
