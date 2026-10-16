#!/usr/bin/env python3
"""Check final offline source frame provenance and before/after camera pairing.

Passing verifies recorded files/inputs and disclosure, not Unity rendering,
native appearance, collision, live residents or performance.
"""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Docs/InhabitedCity04C/Previews'
REQUIRED={'lofts_before','lofts_frontage','lofts_threshold','lofts_interior','lofts_stairs','lofts_upper',
          'clinic_interior','forge_interior','inn_interior','archive_interior','observatory_upper','observatory_stairs',
          'garage_interior','depot_interior','lofts_frontage_night','neighborhood_before','neighborhood_after'}


def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    data=json.loads((OUT/'preview_manifest.json').read_text());checks=[]
    def check(name,passed,detail=None):checks.append({'name':name,'passed':bool(passed),'detail':detail})
    check('Preview explicitly discloses offline scope',data['scope']=='OFFLINE SOURCE PREVIEW, NOT UNITY GAMEPLAY' and data['unity']=='NOT RUN')
    check('All destination/stair/street/night views recorded',REQUIRED.issubset(data['views']),sorted(REQUIRED-set(data['views'])))
    for name,view in data['views'].items():
        path=OUT/view['file'];check(name+': artifact hash matches stamped frame',path.is_file() and digest(path)==view.get('artifact_sha256'))
        if path.exists():
            with Image.open(path) as image:
                check(name+': actual PNG matches recorded resolution',image.format=='PNG' and list(image.size)==view['resolution'])
        source=Path(view['source']);source=source if source.is_absolute() else ROOT/source
        check(name+': room source hash matches',source.is_file() and digest(source)==view['source_sha256'])
        check(name+': standing player-height camera',1.65 <= (view['camera_world'][1]-view['binding']['origin'][1] if view['venue']!='neighborhood' else view['camera_world'][1]-.15) <= 1.75 or 'upper' in name)
    mappings=[('geometry',data['geometry_and_texture_sha256']),('render helpers',data['render_source_sha256']),
              ('geometry export inputs',data['source_export']['source_sha256'])]
    actor=json.loads((OUT/'ActorSource/source_provenance.json').read_text());mappings.append(('resident source builders',actor['source_sha256']))
    for category,entries in mappings:
        bad=[]
        for name,sha in entries.items():
            path=Path(name);path=path if path.is_absolute() else ROOT/path
            if not path.is_file() or digest(path)!=sha:bad.append(name)
        check(category+': every recorded input remains current',not bad,bad)
    for first,last in [('lofts_before','lofts_frontage'),('neighborhood_before','neighborhood_after')]:
        a,b=data['views'].get(first,{}),data['views'].get(last,{})
        check(first+' > '+last+': matched camera and settings',bool(a) and bool(b) and all(a.get(k)==b.get(k) for k in ['camera_world','target_world','lens_mm','samples','resolution']))
    check('Resident pose snapshot explicitly excludes live AI',actor.get('native_residents')=='NOT RUN' and bool(data.get('residents')))
    report={'scope':__doc__.strip(),'views':len(data['views']),'checks':len(checks),'passed':sum(c['passed'] for c in checks),
            'failed':[c for c in checks if not c['passed']],'details':checks,'native_visual_acceptance':'NOT RUN'}
    path=ROOT/'Docs/InhabitedCity04C/Validation/previews.json';path.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items() if k!='details'},indent=2))
    return 1 if report['failed'] else 0


if __name__=='__main__':raise SystemExit(main())
