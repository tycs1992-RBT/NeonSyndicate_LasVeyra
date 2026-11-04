"""Compile all twelve managed configurations with an installed editor's compiler and real package DLLs.

Use the response files from tools/check_unity_classic_wr02g.py on this same source tree. This does
not launch Unity, check shader compilation, import assets, execute the editor or produce a player.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import subprocess


def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ['editor-data','classic-output','package-assemblies','output']:
        p.add_argument('--'+name,type=Path,required=True)
    args=p.parse_args()
    ed,classic,packages,out=[v.resolve() for v in (args.editor_data,args.classic_output,args.package_assemblies,args.output)]
    out.mkdir(parents=True,exist_ok=True)
    sources=json.loads((classic/'report.json').read_text())['rows']
    package_refs=[packages/'Unity.InputSystem.dll',packages/'Unity.RenderPipelines.Core.Runtime.dll',
                  packages/'Unity.RenderPipelines.Universal.Runtime.dll']
    assert all(path.is_file() for path in package_refs),'Missing package reference assemblies'
    def compile_row(job):
        target,mode,pipeline=job;name='-'.join(job)
        base=[line.strip('"') for line in (classic/(target+'.rsp')).read_text().splitlines()]
        flags=next(x for x in base if x.startswith('-define:')).split(':')[1].split(',')
        flags.append('NS_INPUT_SYSTEM')
        if mode!='classic':flags.append('ENABLE_INPUT_SYSTEM')
        if mode=='new':flags.remove('ENABLE_LEGACY_INPUT_MANAGER')
        dependencies=[package_refs[0]]
        if pipeline=='urp':
            flags.append('NS_URP')
            dependencies+=sorted(packages.glob('Unity.RenderPipelines.*.dll'))+sorted(packages.glob('Unity.RenderPipeline.*.dll'))
            dependencies+=[packages/(x+'.dll') for x in ['Unity.Mathematics','Unity.Collections','Unity.Burst']]
        options=[x for x in base if not x.startswith(('-define:','-out:'))]
        options+=['-define:'+','.join(flags),'-out:'+str(out/(name+'.dll'))]
        options+=['-r:'+str(d) for d in dependencies]
        rsp=out/(name+'.rsp')
        rsp.write_text('\n'.join('"'+x+'"' for x in options))
        run=subprocess.run([ed/'NetCoreRuntime/dotnet',ed/'DotNetSdkRoslyn/csc.dll','@'+str(rsp)],capture_output=True,text=True)
        log=run.stdout+run.stderr;(out/(name+'.log')).write_text(log)
        row={'configuration':name,'exit':run.returncode,'errors':log.count(': error '),'warnings':log.count(': warning ')}
        print(name,run.returncode,flush=True)
        return row
    jobs=[(target,mode,pipe) for target in ['player','editor'] for mode in ['classic','both','new'] for pipe in ['builtin','urp']]
    with ThreadPoolExecutor(max_workers=2) as workers:
        results=list(workers.map(compile_row,jobs))
    checksums={path.name:hashlib.sha256(path.read_bytes()).hexdigest() for path in package_refs}
    data={'scope':'Managed/API compilation only, with the supplied Unity editor and template package references.',
          'package_assemblies':packages.parent.name,'package_sha256':checksums,
          'sources':{r['row']:r['source_sha256'] for r in sources},'configurations':results}
    (out/'matrix.json').write_text(json.dumps(data,indent=2)+'\n')
    return int(any(row['exit'] for row in results))

if __name__=='__main__':
    raise SystemExit(main())
