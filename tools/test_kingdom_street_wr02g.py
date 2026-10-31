"""Run the actual supported-route and stone planner against checked-in routes, not Unity physics."""
import argparse
import re
import subprocess
import tempfile
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
WORLD = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World'
PLAN = WORLD / 'HeartFR01/HeartPlanFR01.StreetWR02D.cs'
DETAIL = WORLD / 'HeartFR01/HeartPlanFR01.KingdomStreetWR02G.cs'
HARNESS = ROOT / 'tools/kingdom_street_wr02g_harness.cs'
IDS = {'home-court', 'court-crossing', 'sanctuary-approach', 'forge-market', 'home-dispatch',
       'dispatch-clinic', 'shardwood-threshold', 'grotto-through-route', 'grotto-return'}


def fixtures():
    text = (WORLD / 'CityLayout.Gothic04OB.cs').read_text()
    fractured = (WORLD / 'CityLayout.Fractured04OC.cs').read_text()
    for declaration in ('Fractured04OC ? new Vector3(-108f,CurbHeight,28f)',
                        'Fractured04OC ? new Vector3(-110f,CurbHeight,74f)'):
        if declaration not in text:
            raise ValueError('Revision-10 realm anchor changed: ' + declaration)
    # Revision 10 follows the fractured route replacement, so the two obsolete Gothic grotto rows are excluded.
    entries = []
    expression = r'AddRealmRoute04OB\("([^"]+)",\s*([\d.]+)f,([^;]+)\);'
    token = r'new Vector3\(\s*([-\d.]+)\s*,\s*[-\d.]+\s*,\s*([-\d.]+)\s*\)|(RealmThreshold04OB|RealmGrotto04OB)'
    for source, is_fractured in ((text, False), (fractured, True)):
        for name, width, args in re.findall(expression, source):
            if name not in IDS or (not is_fractured and name in {'grotto-through-route', 'grotto-return'}):
                continue
            points = []
            for match in re.finditer(token, args):
                x, z, anchor = match.groups()
                points.append({'RealmThreshold04OB': (-108, 28), 'RealmGrotto04OB': (-110, 74)}[anchor]
                              if anchor else (float(x), float(z)))
            if len(points) < 2:
                raise ValueError('Cannot read real route ' + name)
            entries.append((name, width, points))
    if {row[0] for row in entries} != IDS or len(entries) != len(IDS):
        raise ValueError('Required revision-10 source routes absent or duplicated.')
    return '\n'.join(name + ';' + width + ';' + '|'.join(str(x) + ',' + str(z) for x, z in points)
                     for name, width, points in entries) + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet')
    parser.add_argument('--mono')
    parser.add_argument('--csc')
    args = parser.parse_args()
    data = fixtures()
    with tempfile.TemporaryDirectory(prefix='wr02g-street-') as tmp:
        directory = Path(tmp)
        route_file = directory / 'routes.txt'
        route_file.write_text(data)
        if args.dotnet:
            project = directory / 'Street.csproj'
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
                               '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
                               '</PropertyGroup><ItemGroup>' + ''.join('<Compile Include="' + escape(str(p)) + '" />'
                               for p in (PLAN, DETAIL, HARNESS)) + '</ItemGroup></Project>')
            command = [args.dotnet, 'run', '--project', str(project), '--', str(route_file)]
        elif args.mono and args.csc:
            exe = directory / 'Street.exe'
            subprocess.run([args.mono, args.csc, '-nologo', '-langversion:8.0', '-out:' + str(exe),
                            str(PLAN), str(DETAIL), str(HARNESS)], check=True, cwd=directory, timeout=90)
            command = [args.mono, str(exe), str(route_file)]
        else:
            print('Nine actual revision-10 routes read. Executable planner and Unity: NOT RUN.')
            return
        subprocess.run(command, check=True, cwd=directory, timeout=180)


if __name__ == '__main__':
    main()
