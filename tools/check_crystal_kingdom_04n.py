#!/usr/bin/env python3
"""04N source preservation/integration receipts. No native Unity, player save, screenshot or performance acceptance."""
import argparse,json,re,subprocess
from pathlib import Path
BASE='f9655c2c4373b6ef6efffee21f49f364e70ea765'
ROOT=Path(__file__).resolve().parents[1]
def inspect(root):
 def git(*args):return subprocess.check_output(['git',*args],cwd=root,text=True)
 def before(path):return git('show',BASE+':'+path)
 rows=[]
 def check(name,value):rows.append({'name':name,'passed':bool(value)})
 original=set(git('ls-tree','-r','--name-only',BASE).splitlines())
 changed=[p for p in git('diff','--name-only',BASE,'--').splitlines() if p in original]
 check('No original file removed',not git('diff','--name-only','--diff-filter=D',BASE,'--').strip())
 check('Original project settings/packages retained',not any(p.startswith(('ProjectSettings/','Packages/')) for p in changed))
 check('Original GUIDs/scenes/binary art and prefabs retained',not any(p.endswith(('.meta','.unity','.prefab','.asset','.fbx','.obj','.png','.jpg','.jpeg','.wav','.ogg','.mat','.controller','.anim')) for p in changed))
 check('Every original quest/event resource retained',not any(p.startswith(('Assets/NeonSyndicate/Resources/Quests/','Assets/NeonSyndicate/Resources/Events/')) for p in changed))
 prefix='Assets/NeonSyndicate/Scripts/Runtime/'
 for name in ['Characters/PlayerCamera.cs','Core/GInput.cs','UI/MenuSystem.cs','Gameplay/MissionManager.cs','Gameplay/SagaDirection04L.cs','Gameplay/MissionContext.Performance04L.cs','Gameplay/MissionContext.Cast04L.cs','Gameplay/StoryMissions.cs','World/CityLayout.cs','Gameplay/Bonds/BondData.cs','Gameplay/Bonds/BondRoster.cs','Gameplay/Bonds/BondRules.cs','Gameplay/CrystalArsenal/CrystalFocusDb.cs']:
  path=prefix+name;check('Protected original owner '+name,(root/path).read_text()==before(path))
 check('SaveData and old control declarations retained',not any(Path(p).name in ('SaveData.cs','CONTROLS.md') for p in changed))
 def fields(text,kind):
  match=re.search(r'class '+kind+r'\s*\{(.*?)\n    \}',text,re.S)
  return re.findall(r'public\s+[^;\n]+;',match.group(1)) if match else []
 for name,kinds in [('Gameplay/MaterialStore.cs',['MaterialSave']),('Gameplay/CraftingSystem.cs',['CraftSave']),('Gameplay/FactionSystem.cs',['FactionSave']),('Gameplay/LootArmory.cs',['LootGunSave','LootSave']),('Combat/CrystalCombat04F.cs',['CrystalCombatSave04F'])]:
  p=prefix+name
  for kind in kinds:
   a=fields(before(p),kind);b=fields((root/p).read_text(),kind)
   check('Saved fields retained in original order '+kind,bool(a) and b[:len(a)]==a)
 def read(p):return (root/(prefix+p)).read_text()
 check('City and hero geometry called by original builders','KingdomFrontage04N(building' in read('ArtRebirth04A/VisualRebirth04AArchitecture.cs') and 'BuildKingdom04N();' in read('Characters/BodyBuilder.cs'))
 muster=read('Gameplay/Events/EventKinds.Living04M.cs')
 check('Legitimate finite muster caster with capped admission','Cantor04N=slot==1' in muster and 'AllocatedBodyCost04M' in muster and 'AllocatedGroups04N+1>capGroups' in muster and 'NoRewards=true' in muster)
 check('Observer consent and damage contribution both retained','if(contributed && e.PlayerAccepted)' in muster and 'e.PlayerAccepted=true' not in muster)
 check('Finite retreat and timeout cleanup retained','away>=5f' in muster and 'e.Age>=battleEnd' in muster and 'confirmed==count' in muster)
 ecology=read('Creatures/CreatureDirector.Ecology.cs')
 check('Ambient admissions reserve persistent/event bodies','CreatureAdmission04N.Room(capBodies,bodies,AllocatedBodyCost04M)' in ecology and 'groups=AllocatedGroups04N' in ecology)
 check('Covered-pavement probe keeps ground policy and bounded buffer','n==_streetGround04N.Length' in ecology and 'GroundOk(spot, sp, c)' in ecology and 'CityRiftGround04N.Eligible' in ecology)
 check('Original durable event payment owner unchanged',read('Gameplay/Events/WorldEvents.Crystal04F.cs')==before(prefix+'Gameplay/Events/WorldEvents.Crystal04F.cs'))
 strike=read('Combat/CombatDirector.cs')
 check('Physical contact resolves through original once-per-swing damage owner','physicalBlade04N.Value.Contact' in strike and 'alreadyHit.Contains(c)' in strike and 'c.Health.TakeDamage(info)' in strike)
 check('Contact resolved after the actual player pose','Game.Player.Combat.ResolveBladeAfterPose04N()' in read('Characters/HumanoidAnimator.cs') and 'BladeSweep04N.HistoryValid' in read('Characters/PlayerCombat.Blade04N.cs'))
 check('Existing eight paid Forge treatments remain exact',read('Gameplay/ForgeTreatments04M.cs')==before(prefix+'Gameplay/ForgeTreatments04M.cs'))
 check('Gear fittings read applied timers without granting items','forge.BuffLeft' in read('Gameplay/Gear.Fittings04N.cs') and 'TickFittings04N();' in read('Gameplay/Gear.cs'))
 loot=read('Gameplay/LootArmory.cs');shop=read('Gameplay/LootArmory.Shop.cs')
 check('Stock capture/restore use the original armory save owner','Stock04N=_merchant04N.Capture()' in loot and '_merchant04N.Restore(s!=null?s.Stock04N:null' in loot and 'TakeStock04N("gun"+i)' in shop)
 check('Stock time is gameplay time with bounded inventory and prices','TickStock04N(dt)' in loot and 'BackpackFull' in shop and 'LootEconomy.SellValue(g)+1' in shop)
 refuge=read('Gameplay/Guidance04E/CampaignDirector.Refuge.cs')
 check('Refuge stock/overflow admission occurs before payment',refuge.index('StockAvailable04N("refuge:"+o.Material)<=0')<refuge.index('Game.Economy.Spend(o.Price') and refuge.index('>9999-o.Count')<refuge.index('Game.Economy.Spend(o.Price'))
 check('Original all-eight treatment group/expiration owner preserved',read('Gameplay/CraftingSystem.cs')==before(prefix+'Gameplay/CraftingSystem.cs'))
 resonance=read('Gameplay/Bonds/BondSystem.Resonance04N.cs')
 check('Owned partner, focus UID and real resource validation','PartnerEligible' in resonance and 'Game.Loot.Find(chains.FocusUid)' in resonance and 'Game.CrystalCombat.Spend(cost)' in resonance)
 check('Synergy terminal cleanup uses existing assist owner','CancelSynergy04N()' in read('Gameplay/Bonds/BondSystem.Assist.cs') and 'CancelSynergy04N();' in read('Gameplay/Bonds/BondSystem.cs') and '_synergy04N.Resolve(out confirmed)' in resonance)
 check('No cinematic time or camera authority replacement','Time.timeScale' not in resonance and 'SetCinematic' not in resonance and '.Shake(' in resonance and 'GameTime.Paused' in resonance)
 puzzle=read('World/Underheart/UnderheartKingdom04N.cs')
 check('Optional garden reward is receipted before ordinary materials',puzzle.index('SetFlag(GardenReceipt04N)')<puzzle.index('Game.Materials.Add("seed_core"') and 'CreateGardenCircuit04N();' in read('World/Underheart/UnderheartWorld.cs'))
 contracts=json.loads((root/'Assets/NeonSyndicate/Resources/Quests/crystal_kingdom_contracts_04n.json').read_text())['Contracts']
 check('Four distinct finite physical guild requests',len(contracts)==4 and len({c['Id'] for c in contracts})==4 and {c['Guild'] for c in contracts}=={'menders','frequency_order','forgewrights','frost_wardens'} and {c['Kind'] for c in contracts}=={'Escort','Rift','Courier','Hunt'})
 check('Commutes use original sidewalk/crosswalk navigation','CommuteLink04N(L)' in read('AI/NpcController.cs') and 'if (chosen.Crosswalk)' in read('AI/NpcController.cs') and 'Persistent || IsCreature' in read('AI/CivilianCommute04N.cs'))
 for p in ['Creatures/CompanionController.Motion.cs','Creatures/CompanionController.Life04G.cs']:
  check('Shared companion collision policy '+p,'KingdomCombatRules04N.CompanionObstacleMask' in read(p))
 check('No new focus/bond/fusion grants in 04N runtime additions',not any(re.search(r'\.Grant\(|Inventory\.Give\(|GrantSignature\(',p.read_text()) for p in (root/prefix).rglob('*04N.cs')))
 return rows
if __name__=='__main__':
 a=argparse.ArgumentParser(description=__doc__);a.add_argument('--root',type=Path,default=ROOT);a.add_argument('--report',type=Path);x=a.parse_args();rows=inspect(x.root)
 report={'scope':__doc__,'baseline':BASE,'passed':sum(r['passed'] for r in rows),'total':len(rows),'checks':rows}
 if x.report:x.report.parent.mkdir(parents=True,exist_ok=True);x.report.write_text(json.dumps(report,indent=2)+'\n')
 print(json.dumps(report,indent=2));raise SystemExit(0 if all(r['passed'] for r in rows) else 1)
