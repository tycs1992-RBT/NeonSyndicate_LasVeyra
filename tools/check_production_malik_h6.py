#!/usr/bin/env python3
"""Audit actual H6 art, fingerprints and protected ownership. Never native import or appearance approval."""
import argparse,hashlib,json,subprocess
from h8_owner_hooks import restore_owner_h8
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASE='0f18b4fb21832a18a59268d37d056200b8cfabb4'
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);a=p.parse_args();checks=[]
 def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
 def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
 def text(path):return(ROOT/path).read_text()
 art=ROOT/'Assets/NeonSyndicate/Art/ProductionH6';r=json.loads((art/'candidate.json').read_text());s=json.loads((art/'Models/authoring-stats.json').read_text())
 check('Actual candidate, disabled and appearance unapproved',r['assetId']=='MALIK-H6-REVIEW-01' and r['enabled']is False and r['ownerApproved']is False and r['NATIVE_UNITY_IMPORT']=='NOT RUN')
 check('Explicit lawful rights and complete influences',r['license']=='CC0-1.0' and r['commercialUse']is True and r['publicSourceApproved']is True and r['preserveInfluences']is True)
 for row in r['files']:
  path=ROOT/row['path'];check('Actual received/authored bytes / '+row['path'],path.is_file() and path.stat().st_size==row['bytes'] and sha(path)==row['sha256'])
 for row in s['files']:
  path=(ROOT/'ArtSource/AscensionH6' if row['path'].endswith('.blend') else art/'Models')/row['path'];check('Editable/export lineage / '+row['path'],path.is_file() and sha(path)==row['sha256'])
 check('Real binary FBX and separate artist source',(ROOT/r['sourceFbx']).read_bytes().startswith(b'Kaydara FBX Binary') and (ROOT/r['authoringSource']).read_bytes().startswith(b'\x28\xb5\x2f\xfd'))
 check('53 source bones, 39 real skin batches, three decreasing LODs',s['bone_count']==53 and s['runtime_skins']==39 and len(s['lods'])==39 and all(sum(x['triangles']for x in s['lods']if x['lod']=='LOD'+str(i))>sum(x['triangles']for x in s['lods']if x['lod']=='LOD'+str(i+1))for i in [0,1]))
 check('No claimed authored clips or production acceptance',s['authored_animation']=='ASSET REQUIRED' and r['PRODUCTION_CHARACTER_ACCEPTED']=='NO')
 views=json.loads((ROOT/'Docs/Ascension04OH/ArtReviewH6/render-manifest.json').read_text())
 check('Eight actual source views tied to current artist source',len(views['views'])==8 and views['source_sha256']==sha(ROOT/r['authoringSource']) and views['native_unity']=='NOT RUN')
 check('All source views exist',all((ROOT/'Docs/Ascension04OH/ArtReviewH6'/x['file']).is_file()for x in views['views']))
 check('No prepared copy or enabled registry shipped',not(art/'Prepared').exists() and not(ROOT/'Assets/NeonSyndicate/Resources/ImportedModels04OD.asset').exists())
 editor=text('Assets/NeonSyndicate/Scripts/Editor/ProductionMalikH6.cs');runtime=text('Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedVisual04OD.ReviewH6.cs');policy=text('Assets/NeonSyndicate/Scripts/Runtime/Characters/MalikReviewH6.cs')
 check('Explicit Editor scope, fallback restoration and original adapter',all(x in runtime for x in ['#if UNITY_EDITOR','!Application.isEditor','owner.IsDead','MalikReviewH6.CanReview','ImportedModelRegistry04OD.Find','adapter.Initialize()','adapter.Restore()','ReleaseReviewH6']))
 check('Full weights, original shader, sockets and safe LOD preparation',all(x in editor for x in ['maxBonesPerVertex=r.preserveInfluences?8:4','minBoneWeight=0','CalculateMikk','Mats.StandardShader','LODGroup','socket_','ImportedBindH3.CandidateBone','SkinWeightsH5.Referenced']))
 check('Scoped quality is restored, no persistent quality/save writes','QualitySettings.skinWeights=_priorWeights' in editor and all(x not in editor for x in ['PlayerPrefs.','SaveSystem.','SetQualityLevel','SetDirty(QualitySettings']))
 check('Review does not grant approval or replace animation authority','OwnerApproved=false' in editor and 'Enabled=false' in editor and 'ImportedAnimationAuthority04OH.ExistingPose' in editor and 'model.Enabled' in policy)
 check('New pure statics reset by original hub','MalikReviewH6.ResetStatics();'in text('Assets/NeonSyndicate/Scripts/Runtime/Core/Game.cs'))
 protected=['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset']
 protected+=['Assets/NeonSyndicate/Scripts/Runtime/'+x for x in ['Characters/BodyBuilder.cs','Characters/PlayerController.cs','Characters/HumanoidRig.cs','Characters/HumanoidAnimator.cs','Characters/HumanoidPose.cs','Characters/ImportedArt04OH.cs','Characters/ImportedBindH3.cs','Combat/WeaponController.cs','Gameplay/Bonds/BondSystem.Resonance04N.cs','Gameplay/RegionalArsenal04OE.cs','Gameplay/ForgeTreatments04M.cs','Gameplay/SaveSystem.cs','World/CityLayout.Rebirth04O.cs','Core/CityRebirthBoot04O.cs','UI/MenuSystem.cs']]
 # Owner decision 2026-10-23 (in chat): one unified game. The title, boot world and story start move to revision 10;
 # MenuSystem.cs is intentionally edited for that and is no longer byte-frozen here.
 protected=[x for x in protected if x!='Assets/NeonSyndicate/Scripts/Runtime/UI/MenuSystem.cs']
 for path in protected:check('Original gameplay/default/save owner / '+path,restore_owner_h8(path,(ROOT/path).read_bytes())==subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT))
 report={'scope':__doc__,'baseline':BASE,'passed':sum(x['passed']for x in checks),'total':len(checks),'checks':checks,'native_unity':'NOT RUN','owner_appearance':'NOT GIVEN','production_acceptance':'NOT ACHIEVED'}
 a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:report[k]for k in ['passed','total']}));print('Failed:',[x['name']for x in checks if not x['passed']]);return int(report['passed']!=report['total'])
if __name__=='__main__':raise SystemExit(main())
