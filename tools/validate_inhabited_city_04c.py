#!/usr/bin/env python3
"""Reproduce the available 04C source checks. Native Unity results remain separate.

Requires the retained .NET 8 SDK, genuine older Unity reference assemblies,
NUnit, numpy and Pillow. It never substitutes source checks for native tests.
"""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
REPORT = ROOT / 'Docs/InhabitedCity04C/Validation'


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--dotnet',type=Path,default=ROOT.parent/'.tools/dotnet/dotnet')
    p.add_argument('--nunit',type=Path,default=ROOT.parent/'.tools/nunit/lib/netstandard2.0/nunit.framework.dll')
    p.add_argument('--unity-references',type=Path,default=ROOT.parent/'.tools/unity-references/lib/netstandard2.0')
    args=p.parse_args(); REPORT.mkdir(parents=True,exist_ok=True)
    results=[]
    def run(name, command, expected=0):
        process=subprocess.run(command,cwd=ROOT,capture_output=True,text=True)
        (REPORT/(name+'.txt')).write_text(process.stdout+process.stderr)
        results.append({'check':name,'exit_code':process.returncode,'passed':process.returncode==expected})
        print(name+': '+('PASS' if process.returncode==expected else 'FAIL'),flush=True)
        return process.returncode==expected
    python='python3'; sdk=['--dotnet',str(args.dotnet),'--nunit',str(args.nunit)]
    source_ok=run('source_book_run',[python,'tools/run_inhabited_city_04c_source.py',*sdk,'--report',str(REPORT/'source_book_bindings.json')])
    run('quest_run',[python,'tools/run_inhabited_city_04c_quests.py',*sdk,'--report',str(REPORT/'quest_validation.json')])
    run('residents_run',[python,'tools/check_inhabited_residents_04c.py','--dotnet',str(args.dotnet),'--output',str(REPORT/'residents.json')])
    geometry=ROOT/'Docs/InhabitedCity04C/Previews/SourceGeometry'
    if source_ok:
        export_ok=run('geometry_export_run',[python,'tools/export_inhabited_city_04c.py','--dotnet',str(args.dotnet),'--venues','lofts,clinic,forge,inn,archive,observatory,garage,depot'])
        if export_ok:
            run('geometry_run',[python,'tools/check_inhabited_city_04c_geometry.py','--resolved-dir',str(geometry),'--report',str(REPORT/'assets_geometry.json')])
            run('resident_routes_run',[python,'tools/check_inhabited_resident_routes_04c.py'])
    run('metadata_run',[python,'tools/check_unity_metadata_04c.py','--report',str(REPORT/'metadata_current.json')])
    run('preservation_run',[python,'tools/check_inhabited_city_04c_preservation.py','--report',str(REPORT/'preservation.json')])
    run('syntax_run',[python,'tools/check_visual_rebirth_04a_syntax.py','--dotnet',str(args.dotnet),'--report',str(REPORT/'syntax_configurations.json')])
    run('retained_events_run',[python,'tools/run_living_territories_04b_events.py',*sdk,'--report',str(REPORT/'retained_04b_events.json')])
    run('retained_ecology_run',[python,'tools/check_territory_ecology_04b.py','--dotnet',str(args.dotnet),'--report',str(REPORT/'retained_04b_ecology.json')])
    run('retained_assets_run',[python,'tools/check_living_territories_04b_assets.py','--report',str(REPORT/'retained_04b_assets.json')])
    with tempfile.TemporaryDirectory(prefix='04c-diagnostics-') as directory:
        run('supplemental_compile_run',[python,'tools/compile_visual_rebirth_04a.py',*sdk,'--unity-references',str(args.unity_references),
            '--baseline','c49144150cfeb2f105f628f7f9d6ed85c1ccb218','--output',directory])
        for source,target in [('diagnostic_comparison.json','final_diagnostic_comparison.json'),('baseline.log','supplemental_baseline.txt'),('working.log','supplemental_working.txt')]:
            file=Path(directory)/source
            if file.exists():shutil.copyfile(file,REPORT/target)
    # The repository runner reports exit 3 when no Editor is present; do not turn that into a passing native test.
    native=subprocess.run(['bash','tools/test.sh','--filter','04C'],cwd=ROOT,capture_output=True,text=True)
    (REPORT/'native_attempt.txt').write_text(native.stdout+native.stderr)
    native_status='NOT RUN' if native.returncode==3 and 'TESTS NOT RUN' in native.stdout else 'PASSED' if native.returncode==0 else 'FAILED'
    summary={'scope':__doc__.strip(),'checks':results,'all_source_checks_passed':all(row['passed'] for row in results),
        'native_Unity_EditMode':native_status,'native_runner_exit':native.returncode,'native_gameplay':'NOT RUN','native_Windows_build':'NOT RUN','native_performance':'NOT RUN',
        'note':'Supplemental compile deliberately retains 35 baseline older-Unity API errors. Syntax checks are not native configuration builds. Source render validation runs separately after image generation.'}
    (REPORT/'validation_summary.json').write_text(json.dumps(summary,indent=2)+'\n')
    print(json.dumps({key:value for key,value in summary.items() if key!='checks'},indent=2))
    return 0 if summary['all_source_checks_passed'] and native_status!='FAILED' else 1


if __name__=='__main__':raise SystemExit(main())
