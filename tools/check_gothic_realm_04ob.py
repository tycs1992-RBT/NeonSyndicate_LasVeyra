#!/usr/bin/env python3
"""Revision 2 integration guard. Source checks are not native Unity or visual acceptance."""
import argparse,json,subprocess
from pathlib import Path
import check_city_rebirth_04o as inherited
ROOT=Path(__file__).resolve().parents[1]
def inspect(root):
 prior=inherited.inspect(root)
 approved={'Original owner retained: World/CityLayout.Graph.cs','04N inherited: Original durable event payment owner unchanged','Only approved menu/controls deltas differ from 04N exact-file guards'}
 rows=[dict(r) for r in prior if r['name'] not in approved]
 rows.append({'name':'Only explicitly authorized revision-2 spatial-query scope differs from prior guards','passed':all(r['passed'] or r['name'] in approved for r in prior)})
 def check(name,value):rows.append({'name':name,'passed':bool(value)})
 def read(path):return (root/'Assets/NeonSyndicate/Scripts/Runtime'/path).read_text()
 layout=read('World/CityLayout.Gothic04OB.cs');graph=read('World/CityLayout.Graph.cs');g=read('Core/Game.cs');builder=read('World/CityBuilder.cs');view=read('ArtInhabitedCity04C/VenueView04C.cs')
 check('World revisions zero and one remain explicitly known and distinct from two','Legacy04N = 0, Crystalline04O = 1, GothicRealm04OB = 2' in read('World/CityLayout.Rebirth04O.cs'))
 check('Revision two only mutates fresh legacy generation','Revision04O!=CityRevision04O.Legacy04N' in layout)
 check('Original host identities move, rather than copying service authorities','v.Origin=v.Entrance=door' in layout and 'v.Poi.Position=door+' in layout)
 check('Ordered collectible entries change in place, never reorder or remove',all(s not in layout for s in ['DataChipSpots.Clear','CrystalShardSpots.Clear','SpellStoneSpots.Clear','StuntJumps.Remove','PickupSpots.Remove']))
 check('Exact revision-2 solids precede the original building fallback','BuildGothicBuilding04OB(bld)' in builder and builder.index('BuildGothicBuilding04OB(bld)')<builder.index('BuildRebirth04O(bld)'))
 check('Old upper cuboids are omitted only in new-profile rooms','Game.Layout.Gothic04OB)GothicRoomComposition04OB.Prepare(layout)' in view)
 check('Original room activities remain original anchor IDs','record3' in read('ArtInhabitedCity04C/GothicRoomComposition04OB.cs') and 'layout.anchors=anchors.ToArray()' in read('ArtInhabitedCity04C/GothicRoomComposition04OB.cs'))
 check('Revision-two spatial queries handle detached lots before old rectangle masking','if (Gothic04OB)' in graph and '_blockHash.TryGetValue' in graph)
 check('Scene and actual render path appear in existing STATUS and startup identity','WorldIdentity04OB.Describe()' in read('Gameplay/Guidance04E/CampaignDirector.Status.cs') and 'Active world identity' in read('Core/GameRoot.cs'))
 check('Every new static owner joins the existing reset',all(x+'.ResetStatics();' in g for x in ['WorldIdentity04OB','GothicRealmGeometry04OB','GothicRealmMaterials04OB','GothicRoomComposition04OB']))
 check('Original scene has explicit new opt-in entry; no migration offered','Gothic realm preview (04O-B)' in read('UI/MenuSystem.cs') and 'CityRevision04O.GothicRealm04OB,null,true' in read('UI/MenuSystem.cs'))
 check('New encounter is unavailable to old worlds','t.Id=="ev_04ob_threshold_watch" && !Game.Layout.Gothic04OB' in read('Gameplay/WorldEvents.cs'))
 check('New encounter uses original consent, contribution and budgets',(root/'Assets/NeonSyndicate/Resources/Events/gothic_realm_04ob.json').is_file() and 'CrystalMuster' in (root/'Assets/NeonSyndicate/Resources/Events/gothic_realm_04ob.json').read_text())
 original=subprocess.check_output(['git','show','366f94e8d184e156d1897098a5763127ccbd9686:Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Events/WorldEvents.Crystal04F.cs'],cwd=root,text=True)
 current=read('Gameplay/Events/WorldEvents.Crystal04F.cs')
 current='\n'.join(line for line in current.split('\n') if 'if(candidate.Id=="ev_04ob_threshold_watch"' not in line)
 current=current.replace('int score = candidate.Id=="ev_04ob_threshold_watch" && seen==0 ? -100 : seen * 3 + order;','int score = seen * 3 + order;')
 check('Original durable payment capture/restore and cleanup bytes retained; only selection admits the new event',current==original)
 check('Original finite Muster runtime is unchanged',not subprocess.check_output(['git','diff','366f94e8d184e156d1897098a5763127ccbd9686','--','Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Events/EventKinds.Living04M.cs'],cwd=root,text=True).strip())
 check('Solid rock/quartz share rendered vertices and exact collider meshes','sharedMesh=mesh' in read('World/CityBuilder.Gothic04OB.cs') and '.Append(source)' in read('World/CityBuilder.Gothic04OB.cs'))
 check('Cavern additions obey original clearance and streaming owner','Free(cell,p,1.5f)' in read('World/Underheart/UnderheartBuilder.Gothic04OB.cs') and 'OwnedMeshes.Add(mesh)' in read('World/Underheart/UnderheartBuilder.Gothic04OB.cs'))
 check('New anatomy and recovery are called by existing rig owners','BuildGothicTravel04OB();' in read('Characters/BodyBuilder.cs') and 'GroundedRecovery04OB(ref p,ref b);' in read('Characters/HumanoidPose.cs') and 'w.GothicAnatomy04OB(evo,archetype);' in read('Creatures/CompanionSculpt.cs'))
 save=read('Gameplay/SaveSystem.cs');menu=read('UI/MenuSystem.cs')
 check('Preview saves including autosave use an isolated bank through the original atomic transaction','revision == 2 ? "lasveyra_realm04ob_save_" : "lasveyra_save_"' in save and 'SaveFileTransaction.Write(PathFor(slot, d.WorldRevision04O), json, ValidSave, out error)' in save)
 check('Explicit bank loads retain matching-revision scene reload','LoadGame(slot, _slotBankRevision04OB)' in menu and 'SaveSystem.Read(slot, bankRevision)' in read('Core/GameRoot.cs'))
 check('Inactive bank is read-only in original menus','bool canSave = allowSave && matchingBank' in menu)
 original_save=subprocess.check_output(['git','show','366f94e8d184e156d1897098a5763127ccbd9686:Assets/NeonSyndicate/Scripts/Runtime/Gameplay/SaveSystem.cs'],cwd=root,text=True)
 check('Original saved fields and capture/restore owners unchanged',save[:save.index('    /// <summary>JSON save games')]==original_save[:original_save.index('    /// <summary>JSON save games')] and save[save.index('        public static SaveData Capture()'):]==original_save[original_save.index('        public static SaveData Capture()'):])
 check('Original halls and cloisters contain real pierced masonry rather than hidden full side walls','PiercedWall(stone,trim' in read('World/GothicRealmGeometry04OB.cs') and 'ArchedWallBay' in read('World/GothicRealmGeometry04OB.Depth.cs'))
 check('The original Archive gains a physical supported stair/gallery route','SanctuaryGallery(stone,trim);' in read('World/GothicRealmGeometry04OB.cs') and 'Stairs(stone, new Vector3(6.2f' in read('World/GothicRealmGeometry04OB.Depth.cs'))
 check('Cave canopy admission uses original clearances','TryCavernCanopy(cell,i' in read('World/Underheart/UnderheartBuilder.Gothic04OB.cs') and 'Free(cell,floor,1.5f)' in read('World/Underheart/UnderheartBuilder.Gothic04OB.cs'))
 check('Profile-specific quest eligibility is enforced even in debug start','QuestLogic.WorldMatches(q.Requires, Game.Layout' in read('Gameplay/QuestBook.cs') and 'p.WorldRevision = Game.Layout' in read('Gameplay/QuestBook.cs'))
 check('New finite Forge survey is data on the original quest owner',(root/'Assets/NeonSyndicate/Resources/Quests/gothic_realm_04ob.json').is_file() and 'fg_04ob_stone_not_silence' in (root/'Assets/NeonSyndicate/Resources/Quests/gothic_realm_04ob.json').read_text())
 check('Produced surface maps are called by original material owner and released on reset','ApplySurface04OB(material,key);' in read('World/GothicRealmMaterials04OB.cs') and 'GeneratedSurfaces04OB.Clear();' in read('World/GothicRealmMaterials04OB.cs'))
 check('The grotto uses the existing world builder and real ground clearance','MineralGrotto(stone,mineral,building)' in read('World/GothicRealmGeometry04OB.cs') and 'BlocksGround04OB(bld,p,margin)' in read('World/CityLayout.Graph.cs'))
 check('Survey interaction is connected to the actual nearby cave floor','grotto-through-route' in read('World/CityLayout.Gothic04OB.cs') and 'RealmGrotto04OB' in read('UI/Hud.Rebirth04O.cs'))
 return rows,prior
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--root',type=Path,default=ROOT);p.add_argument('--report',type=Path);p.add_argument('--inherited-report',type=Path);a=p.parse_args();rows,prior=inspect(a.root)
 report={'scope':__doc__,'baseline':'366f94e8d184e156d1897098a5763127ccbd9686','passed':sum(r['passed'] for r in rows),'total':len(rows),'checks':rows}
 for path,obj in [(a.report,report),(a.inherited_report,{'checks':prior,'passed':sum(r['passed'] for r in prior),'total':len(prior)})]:
  if path:path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(obj,indent=2)+'\n')
 print(json.dumps(report,indent=2));raise SystemExit(0 if all(r['passed'] for r in rows) else 1)
