"""Run the real vertical quest protection source on .NET stubs; Unity physics is not simulated."""
import argparse
import subprocess
import tempfile
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
SAFETY = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldSafetyH8S.cs'
HARNESS = ROOT / 'tools/safety_wr02d_harness.cs'
PLACEMENT = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldArtH8S.Place.cs'

# The runtime lowers the holder after the physics probes; do not let that step bypass the quest guard.
ground = PLACEMENT.read_text(encoding='utf-8').split('private string Ground(', 1)[1].split('private void Settle(', 1)[0]
settled = ground.split('at = new Vector3(want.x, low - Mathf.Max', 1)[1]
recompute = settled.index('WorldRouteH8S.YawedBounds(')
recheck = settled.index('LiveWorldSafetyH8S.Conflict(min, max, low, rules)')
refuse = settled.index('if (conflict != null) return "inside the protected " + conflict;')
assert recompute < recheck < refuse < settled.index('return null;'), 'Settled bounds must pass the protected-volume check.'
print('Settled-bounds clearance source contract: PASS.', flush=True)

parser = argparse.ArgumentParser()
parser.add_argument('--dotnet')
args = parser.parse_args()
if not args.dotnet:
    print('Executable placement safety: NOT RUN (dotnet unavailable). Unity physics: NOT RUN.')
else:
    with tempfile.TemporaryDirectory(prefix='wr02d-safety-') as tmp:
        project = Path(tmp) / 'Safety.csproj'
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
                           '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
                           '</PropertyGroup><ItemGroup><Compile Include="' + escape(str(SAFETY)) + '" />'
                           '<Compile Include="' + escape(str(HARNESS)) + '" /></ItemGroup></Project>', encoding='utf-8')
        subprocess.run([args.dotnet, 'run', '--project', str(project)], cwd=tmp, check=True, timeout=120)
