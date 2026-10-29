"""Run the real street-plan source against actual authored routes, without claiming Unity evidence."""
import argparse
import json
import re
import subprocess
import tempfile
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World/CityLayout.Gothic04OB.cs'
PLAN = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.StreetWR02D.cs'
HARNESS = ROOT / 'tools/street_wr02d_harness.cs'
IDS = {'home-court', 'court-crossing', 'sanctuary-approach', 'forge-market'}


def routes():
    text = SOURCE.read_text(encoding='utf-8')
    out = []
    for name, width, args in re.findall(r'AddRealmRoute04OB\("([^"]+)",\s*([\d.]+)f,([^;]+)\);', text):
        if name not in IDS:
            continue
        pts = re.findall(r'new Vector3\(\s*([-\d.]+)\s*,\s*[-\d.]+\s*,\s*([-\d.]+)\s*\)', args)
        if len(pts) < 2:
            raise ValueError('Cannot read actual source route ' + name)
        out.append({'id': name, 'width': float(width), 'points': [[float(x), float(z)] for x, z in pts]})
    if {r['id'] for r in out} != IDS or len(out) != len(IDS):
        raise ValueError('The four required routes are absent or duplicated.')
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dotnet')
    args = parser.parse_args()
    fixture = routes()
    print('Read %d actual source routes and %d route segments.' % (len(fixture), sum(len(r['points']) - 1 for r in fixture)), flush=True)
    if not args.dotnet:
        print('Executable street planner: NOT RUN (dotnet unavailable). Unity: NOT RUN.')
        return
    with tempfile.TemporaryDirectory(prefix='wr02d-street-') as tmp:
        project, data = Path(tmp) / 'Street.csproj', Path(tmp) / 'routes.json'
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
                           '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
                           '</PropertyGroup><ItemGroup><Compile Include="' + escape(str(PLAN)) + '" />'
                           '<Compile Include="' + escape(str(HARNESS)) + '" /></ItemGroup></Project>', encoding='utf-8')
        data.write_text(json.dumps(fixture), encoding='utf-8')
        subprocess.run([args.dotnet, 'run', '--project', str(project), '--', str(data)], cwd=tmp, check=True, timeout=120)


if __name__ == '__main__':
    main()
