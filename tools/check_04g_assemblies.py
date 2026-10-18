#!/usr/bin/env python3
"""Check Runtime, Editor and EditMode assembly boundaries using a captured genuine-metadata matrix case.
This still does not execute the Unity importer or its assembly-definition graph.
"""
import argparse,json,subprocess
from pathlib import Path


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--matrix',type=Path,required=True);p.add_argument('--dotnet',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
    lines=[line.strip('"') for line in (a.matrix/'editor-both-builtin.rsp').read_text().splitlines()]
    base=[line for line in lines if not line.endswith('.cs') and not line.startswith('-out:')]
    csc=sorted(a.dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    rows=[];runtime=out/'NeonSyndicate.Runtime.dll'
    for kind,folder,name in [('runtime','Scripts/Runtime','NeonSyndicate.Runtime'),('editor','Scripts/Editor','NeonSyndicate.Editor'),('tests','Tests/EditMode','NeonSyndicate.Tests.EditMode')]:
        files=[line for line in lines if line.endswith('.cs') and '/Assets/NeonSyndicate/'+folder+'/' in line]
        opts=base+['-out:'+str(out/(name+'.dll'))]+(['-r:'+str(runtime)] if kind!='runtime' else [])+files
        rsp=out/(kind+'.rsp');rsp.write_text('\n'.join('"'+line+'"' for line in opts))
        run=subprocess.run([str(a.dotnet),str(csc),'@'+str(rsp)],capture_output=True,text=True)
        log=run.stdout+run.stderr;(out/(kind+'.log')).write_text(log)
        rows.append({'assembly':name,'source_files':len(files),'exit_code':run.returncode,'errors':[s for s in log.splitlines() if ': error ' in s]})
    report={'scope':__doc__.strip(),'configuration':'Editor, both input systems, Built-in','source_sha256':json.loads((a.matrix/'report.json').read_text())['source_sha256'],'assemblies':rows,'invocation':__import__('sys').argv}
    (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(rows,indent=2));return int(any(row['exit_code'] for row in rows))
if __name__=='__main__':raise SystemExit(main())
