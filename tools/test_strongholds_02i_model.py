#!/usr/bin/env python3
"""Independent Python model tests of the proposed fort rules. NOT execution of the C# runtime."""
from copy import deepcopy
from itertools import product
import math, random, unittest, json, sys
from pathlib import Path
class Book:
 def __init__(self):
  self.s={i:dict(owned=False,echo=False,theme=0,slots=[(1 if x%2==0 else 3,0) for x in range(6)]) for i in ('hearthspire','galehook','sunscar')};self.cancel()
 def cancel(self):self.raid=None;self.dead=set();self.progress=0.
 def claim(self):
  if self.s['hearthspire']['owned'] or self.raid:return False
  self.s['hearthspire']['owned']=True;return True
 def begin(self,id,n=2):
  if not self.s['hearthspire']['owned'] or id not in ('galehook','sunscar') or self.s[id]['owned'] or self.raid or n!=2:return False
  self.cancel();self.raid=id;return True
 def defeat(self,id,index):
  if not self.raid or id!=self.raid or index not in (0,1) or index in self.dead:return False
  self.dead.add(index);return True
 def tick(self,id,dt,near=True,safe=True):
  if not self.raid or id!=self.raid or len(self.dead)!=2 or not near or not safe or not math.isfinite(dt) or dt<=0:
   if self.raid==id:self.progress=0.
   return False
  self.progress+=min(.1,dt)
  if self.progress+.0001<5:return False
  self.s[id]['owned']=True;self.cancel();return True
 def infuse(self):
  if self.raid or not self.s['hearthspire']['owned']:return 0
  n=0
  for id in ('galehook','sunscar'):
   if self.s[id]['owned'] and not self.s[id]['echo']:self.s[id]['echo']=True;n+=1
  return n
 def configure(self,id,slot,typ,rot):
  if self.raid or id not in self.s or not self.s[id]['owned'] or slot not in range(6) or typ not in range(5) or rot not in range(4):return False
  self.s[id]['slots'][slot]=(typ,rot);return True
 @property
 def resonance(self):return sum(v['owned'] and v['echo'] for k,v in self.s.items() if k!='hearthspire')
 def save(self):return deepcopy(self.s)
 def load(self,s):self.s=deepcopy(s);self.cancel()
 def win(self,id):
  assert self.begin(id);assert self.defeat(id,0);assert self.defeat(id,1)
  for i in range(49):assert not self.tick(id,.1)
  assert self.tick(id,.1)
class ModelTests(unittest.TestCase):
 def test_home_single_claim(self):
  b=Book();self.assertTrue(b.claim());self.assertFalse(b.claim())
 def test_no_home_no_raids(self):self.assertFalse(Book().begin('galehook'))
 def test_no_home_no_construction(self):self.assertFalse(Book().configure('hearthspire',0,1,0))
 def test_no_wins_while_guards_live(self):
  b=Book();b.claim();b.begin('galehook')
  for i in range(500):self.assertFalse(b.tick('galehook',.1))
  self.assertFalse(b.s['galehook']['owned'])
 def test_dead_guard_index_once(self):
  b=Book();b.claim();b.begin('galehook');self.assertTrue(b.defeat('galehook',0));self.assertFalse(b.defeat('galehook',0));self.assertFalse(b.defeat('sunscar',1));self.assertFalse(b.defeat('galehook',2))
 def test_second_raid_refused(self):
  b=Book();b.claim();b.begin('galehook');self.assertFalse(b.begin('sunscar'))
 def test_wrong_roster_refused(self):
  b=Book();b.claim();self.assertFalse(b.begin('sunscar',1))
 def test_no_reward_from_cancel(self):
  b=Book();b.claim();b.begin('galehook');b.defeat('galehook',0);b.cancel();self.assertEqual(b.infuse(),0)
 def test_captured_before_infused_no_boost(self):
  b=Book();b.claim();b.win('galehook');self.assertEqual(b.resonance,0);self.assertEqual(b.infuse(),1);self.assertEqual(b.resonance,1)
 def test_unique_echoes_only(self):
  b=Book();b.claim();b.win('galehook');b.infuse();b.win('sunscar');b.infuse()
  for _ in range(1000):self.assertEqual(b.infuse(),0);self.assertFalse(b.begin('galehook'))
  self.assertEqual(b.resonance,2);self.assertAlmostEqual(1+.04*b.resonance,1.08);self.assertAlmostEqual(1+.03*b.resonance,1.06)
 def test_infuse_not_allowed_mid_raid(self):
  b=Book();b.claim();b.win('galehook');b.begin('sunscar');self.assertEqual(b.infuse(),0);b.cancel();self.assertEqual(b.infuse(),1)
 def test_requires_five_seconds(self):
  b=Book();b.claim();b.win('galehook');self.assertTrue(b.s['galehook']['owned'])
 def test_bad_time_values(self):
  for dt in (float('nan'),float('inf'),-1,0):
   b=Book();b.claim();b.begin('galehook');b.defeat('galehook',0);b.defeat('galehook',1);self.assertFalse(b.tick('galehook',dt));self.assertEqual(b.progress,0)
 def test_huge_delta_capped(self):
  b=Book();b.claim();b.begin('galehook');b.defeat('galehook',0);b.defeat('galehook',1);self.assertFalse(b.tick('galehook',1e9));self.assertEqual(b.progress,.1)
 def test_damage_interrupts(self):
  b=Book();b.claim();b.begin('galehook');b.defeat('galehook',0);b.defeat('galehook',1)
  for _ in range(20):b.tick('galehook',.1)
  b.tick('galehook',.1,safe=False);self.assertEqual(b.progress,0)
 def test_leaving_core_interrupts(self):
  b=Book();b.claim();b.begin('galehook');b.defeat('galehook',0);b.defeat('galehook',1);b.tick('galehook',.1);b.tick('galehook',.1,near=False);self.assertEqual(b.progress,0)
 def test_all_120_socket_commands(self):
  b=Book();b.claim()
  for slot,typ,rot in product(range(6),range(5),range(4)):
   self.assertTrue(b.configure('hearthspire',slot,typ,rot));self.assertEqual(b.s['hearthspire']['slots'][slot],(typ,rot))
 def test_invalid_socket_commands(self):
  b=Book();b.claim()
  for a in [(-1,1,0),(6,1,0),(0,-1,0),(0,5,0),(0,1,4),(0,1,-1)]:self.assertFalse(b.configure('hearthspire',*a))
 def test_cannot_build_at_rival(self):
  b=Book();b.claim();self.assertFalse(b.configure('galehook',0,1,0));b.win('galehook');self.assertTrue(b.configure('galehook',0,1,0))
 def test_cannot_build_mid_raid(self):
  b=Book();b.claim();b.begin('galehook');self.assertFalse(b.configure('hearthspire',0,1,0))
 def test_save_restores_echo_and_module(self):
  b=Book();b.claim();b.win('galehook');b.infuse();b.configure('hearthspire',4,4,3);s=b.save();b.load(s);self.assertEqual(b.resonance,1);self.assertEqual(b.s['hearthspire']['slots'][4],(4,3))
 def test_copy_isolation(self):
  b=Book();b.claim();s=b.save();s['hearthspire']['owned']=False;self.assertTrue(b.s['hearthspire']['owned']);b.load(s);s['hearthspire']['owned']=True;self.assertFalse(b.s['hearthspire']['owned'])
 def test_raid_not_serialized(self):
  b=Book();b.claim();b.begin('sunscar');b.defeat('sunscar',0);s=b.save();b.load(s);self.assertIsNone(b.raid);self.assertFalse(b.s['sunscar']['owned']);self.assertEqual(b.progress,0)
 def test_10000_random_commands_invariants(self):
  b=Book();b.claim();r=random.Random(2199)
  for _ in range(10000):
   id=r.choice(['galehook','sunscar']);op=r.randrange(6)
   if op==0:b.begin(id)
   elif op==1:b.defeat(id,r.randrange(-1,3))
   elif op==2:b.tick(id,r.random()*.3,near=r.random()>.2,safe=r.random()>.1)
   elif op==3:b.infuse()
   elif op==4:b.cancel()
   else:b.load(b.save())
   self.assertIn(b.resonance,range(3));self.assertGreaterEqual(b.progress,0);self.assertLess(b.progress,5.001)
   for v in b.s.values():self.assertEqual(len(v['slots']),6)
if __name__=='__main__':
 result=unittest.TextTestRunner(verbosity=1).run(unittest.defaultTestLoader.loadTestsFromTestCase(ModelTests))
 report={'suite':'Independent Python fort-rule model, not C# execution','tests':result.testsRun,'failures':len(result.failures),'errors':len(result.errors),'unity':'NOT RUN'}
 if '--report' in sys.argv:Path(sys.argv[sys.argv.index('--report')+1]).write_text(json.dumps(report,indent=2)+'\n')
 sys.exit(not result.wasSuccessful())
