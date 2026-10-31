#!/usr/bin/env python3
"""L2 original-owner, asset and bounded-lane preservation. Source evidence, not native Unity acceptance."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE='a8f21d507876b36630f4d0421c2e55401e36ab15'
def main():
 p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);a=p.parse_args();rows=[]
 def check(n,v):rows.append({'name':n,'passed':bool(v)})
 runtime='Assets/NeonSyndicate/Scripts/Runtime/'
 protected=['Core/Game.cs','Core/GameRoot.cs','Core/CityRebirthBoot04O.cs','Gameplay/SaveSystem.cs','Gameplay/StoryMissions.cs','Gameplay/MissionManager.cs','Gameplay/Bonds/BondSystem.cs','Gameplay/CraftingSystem.cs','Characters/PlayerController.cs','Characters/PlayerCombat.cs','Characters/PlayerCamera.cs','Characters/LiveCastRegistry.cs','Characters/LiveCastVisual.cs','Characters/LiveCrowd.cs','AI/PedManager.cs','Creatures/CreatureDirector.cs','World/Wilds/WildsLayout.cs','World/Wilds/WildsSites.cs','World/Wilds/WildsShapes.cs','World/Wilds/WildsTypes.cs','World/Wilds/WildsSystem.cs','World/Wilds/WildsSystem.LandmarksL1.cs','World/LiveWorld/LiveWorldArtH8S.WildL1.cs']
 protected=[runtime+x for x in protected]+['ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset','Packages/manifest.json','Packages/packages-lock.json']
 for path in protected:check('Frozen L1 owner '+path,subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT)==(ROOT/path).read_bytes())
 paths=subprocess.check_output(['git','ls-tree','-r','--name-only',BASE],cwd=ROOT,text=True).splitlines()
 for prefix in ['Assets/NeonSyndicate/Resources/Quests/','Assets/NeonSyndicate/Resources/Events/','Assets/NeonSyndicate/Art/LiveCast/','Assets/NeonSyndicate/Art/LiveWorld/OasisPavilion/','Assets/NeonSyndicate/Tests/EditMode/']:
  files=[x for x in paths if x.startswith(prefix) and (prefix.endswith('EditMode/') and x.endswith('.cs') or not prefix.endswith('EditMode/'))]
  def content(path):
   expected=subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT);actual=(ROOT/path).read_bytes()
   if expected.startswith(b'version https://git-lfs.github.com/spec/v1'):
    digest=next(l[11:].decode()for l in expected.splitlines()if l.startswith(b'oid sha256:'));return hashlib.sha256(actual).hexdigest()==digest
   return actual==expected
  check('Prior content and tests retained '+prefix,all(content(x)for x in files))
 manifest=json.loads((ROOT/'Docs/LiveWildL2/ASSET_MANIFEST_L2.json').read_text());check('Original rights, six actual variants and eighteen LOD meshes',manifest['license']=='CC0-1.0' and manifest['external_inputs']==[] and len(manifest['lods'])==18)
 for row in manifest['files']:
  path=ROOT/row['path'];check('Delivered bytes '+row['path'],path.is_file() and path.stat().st_size==row['bytes'] and hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'])
 text=lambda p:(ROOT/runtime/p).read_text()
 check('No per-piece object or Update component', 'CombineMeshes('in text('World/LiveWilds/LiveWildsBatchL2.cs') and 'void Update('not in text('World/LiveWilds/LiveWildsBatchL2.cs'))
 check('Original collision kept active during appearance switch',all(s in text('World/Wilds/WildsChunkL1.cs')for s in ['if (!collision && previous != null)','solid.SetParent(stage, false)','stage.gameObject.SetActive(true)','Release(previous)']) and 'Rebuild(new WildsBuilder.Result(), false)' in text('World/Wilds/WildsChunkL1.ArtL2.cs'))
 check('Existing 160 m owner, scoped exclusions and missing-art fallback retained',all(s in text('World/Wilds/WildsBuilder.cs')for s in ['ChunkSize = 160f','excluded.Contains(index)','if(!replaced)','c.Collider']))
 check('Explicit default-off preference, no new progression block','PlayerPrefs.GetInt(ArtPreferenceL2,0)'in text('World/Wilds/WildsSystem.ArtL2.cs') and 'CompareArtL2' in text('UI/VisibleRealityV0.cs'))
 check('Native save/reset/restore comparison cleanup uses existing WorldSystems','ComparisonLifetimeL2' in text('UI/VisibleRealityV0.WildsL2.cs') and 'WorldSystems.Register(new ComparisonLifetimeL2(this))'in text('UI/VisibleRealityV0.cs'))
 check('Original EXPLORE/GPS with no L2 teleport','Route: Redspire art walk'in text('UI/VisibleRealityV0.WildsL2.cs') and 'Game.Gps.SetWaypoint(point)'in text('UI/VisibleRealityV0.WildsL2.cs') and 'Teleport('not in text('UI/VisibleRealityV0.WildsL2.cs'))
 check('Mutable registry resets through original WildsMaterials hub','LiveWildsRegistryL2.ResetStatics();'in text('World/Wilds/WildsMaterials.cs') and 'WildsMaterials.ResetStatics();'in text('Core/Game.cs'))
 result={'scope':__doc__,'baseline':BASE,'passed':sum(x['passed']for x in rows),'total':len(rows),'checks':rows,'native_unity':'NOT RUN'};a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'passed':result['passed'],'total':len(rows),'failed':[x['name']for x in rows if not x['passed']]}));return int(result['passed']!=len(rows))
if __name__=='__main__':raise SystemExit(main())
