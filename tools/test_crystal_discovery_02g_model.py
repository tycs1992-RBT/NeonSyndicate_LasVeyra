#!/usr/bin/env python3
"""Independent Python reference tests of discovery rules. These do not execute the C# implementation."""
from copy import deepcopy
from itertools import permutations, product
from pathlib import Path
import json, random, sys, unittest
CAT=json.loads((Path(__file__).resolve().parents[1]/'Assets/NeonSyndicate/Resources/CrystalDiscovery02G/discovery_catalog.json').read_text())

class Book:
    def __init__(self):self.reset()
    def reset(self):
        self.state={d['id']:dict(seen=False,read=False,step=0,solved=False,mask=0,mistakes=0) for d in CAT['sites']};self.circuit=False;self.busy=False;self.tracked=''
    def discover(self,id):
        s=self.state.get(id)
        if s is None or s['seen']:return False
        s['seen']=True;return True
    def read(self,id):
        if id not in self.state:return False
        self.state[id].update(seen=True,read=True);self.tracked=id;return True
    def touch(self,id,rune):
        s=self.state.get(id)
        if s is None or rune not in range(3):return 'unavailable'
        if s['solved']:return 'already'
        if not s['read']:return 'need_read'
        order=next(d['order'] for d in CAT['sites'] if d['id']==id)
        if order[s['step']]!=rune:s['step']=0;s['mistakes']=min(999999,s['mistakes']+1);return 'wrong'
        s['step']+=1
        if s['step']==3:s['solved']=True;return 'solved'
        return 'correct'
    def claim(self,id,fn):
        s=self.state.get(id)
        if s is None or not s['solved'] or self.busy:return False
        self.busy=True
        try:
            for bit in (1,2):
                if s['mask']&bit:continue
                s['mask']|=bit;ok=False
                try:ok=fn(bit)
                finally:
                    if not ok:s['mask']&=~bit
            return s['mask']==3
        finally:self.busy=False
    @property
    def claimed(self):return sum(s['solved'] and s['mask']==3 for s in self.state.values())
    def circuit_claim(self,fn):
        if self.busy or self.circuit or self.claimed!=3:return False
        self.busy=True;self.circuit=True;ok=False
        try:ok=fn(4);return ok
        finally:
            if not ok:self.circuit=False
            self.busy=False
    def capture(self):return deepcopy(dict(entries=self.state,circuit=self.circuit,tracked=self.tracked))
    def restore(self,save):
        self.reset()
        if not save:return
        for id,r in (save.get('entries') or {}).items():
            if id not in self.state:continue
            s=self.state[id];s.update(seen=bool(r.get('seen')),read=bool(r.get('read')),solved=bool(r.get('solved')) or bool(r.get('mask',0)&3),mask=r.get('mask',0)&3,mistakes=max(0,min(999999,r.get('mistakes',0))),step=max(0,min(2,r.get('step',0))))
            if s['solved']:s.update(seen=True,read=True,step=3)
            elif s['read']:s['seen']=True
            else:s['step']=0
        self.circuit=bool(save.get('circuit')) and self.claimed==3
        self.tracked=save.get('tracked') if save.get('tracked') in self.state else ''
    def solve(self,d):self.read(d['id']);[self.touch(d['id'],r) for r in d['order']]

class ModelTests(unittest.TestCase):
    def test_all_permutations_only_authored_order_solves_in_three(self):
        for d in CAT['sites']:
            for order in permutations(range(3)):
                with self.subTest(id=d['id'],order=order):
                    b=Book();b.read(d['id']);[b.touch(d['id'],r) for r in order]
                    self.assertEqual(b.state[d['id']]['solved'],list(order)==d['order'])
    def test_all_27_three_press_sequences_only_authored_order_solves(self):
        for d in CAT['sites']:
            for order in product(range(3),repeat=3):
                b=Book();b.read(d['id']);[b.touch(d['id'],r) for r in order]
                self.assertEqual(b.state[d['id']]['solved'],list(order)==d['order'])
    def test_10000_generated_actions_preserve_invariants(self):
        randomizer=random.Random(1724);b=Book();credits={d['id']:0 for d in CAT['sites']}
        for n in range(10000):
            d=randomizer.choice(CAT['sites']);id=d['id'];kind=randomizer.randrange(4)
            if kind==0:b.read(id)
            elif kind==1:b.touch(id,randomizer.randrange(-1,4))
            elif kind==2:
                def fn(bit):credits[id]+=1;return True
                b.claim(id,fn)
            else:save=b.capture();b.restore(save)
            for id,s in b.state.items():
                self.assertIn(s['step'],range(4));self.assertIn(s['mask'],range(4));self.assertLessEqual(credits[id],2)
                if s['solved']:self.assertTrue(s['read']);self.assertEqual(s['step'],3)
    def test_discovery_idempotent(self):
        b=Book();self.assertTrue(b.discover('aster_oath'));self.assertFalse(b.discover('aster_oath'))
    def test_read_required(self):self.assertEqual(Book().touch('aster_oath',2),'need_read')
    def test_unknown_id(self):
        b=Book();self.assertFalse(b.read('bad'));self.assertEqual(b.touch('bad',2),'unavailable');self.assertFalse(b.claim('bad',lambda _:True))
    def test_wrong_rune_never_removes_rewards_at_other_site(self):
        b=Book();b.solve(CAT['sites'][0]);b.claim('aster_oath',lambda _:True);b.read('dawn_promise');b.touch('dawn_promise',2)
        self.assertEqual(b.state['aster_oath']['mask'],3)
    def test_wrong_rune_restores_zero_not_expensive_failure(self):
        b=Book();b.read('aster_oath');b.touch('aster_oath',2);self.assertEqual(b.touch('aster_oath',1),'wrong');self.assertEqual(b.state['aster_oath']['step'],0)
    def test_solved_never_advances_past_three(self):
        b=Book();b.solve(CAT['sites'][0]);[b.touch('aster_oath',0) for _ in range(1000)];self.assertEqual(b.state['aster_oath']['step'],3)
    def test_cannot_claim_without_solution(self):self.assertFalse(Book().claim('aster_oath',lambda _:self.fail('must not call')))
    def test_1000_repeated_claims_pay_two_credits(self):
        b=Book();b.solve(CAT['sites'][0]);calls=[]
        for _ in range(1000):b.claim('aster_oath',lambda bit: calls.append(bit) or True)
        self.assertEqual(calls,[1,2])
    def test_full_material_inventory_defers_only_material(self):
        b=Book();b.solve(CAT['sites'][0]);calls=[]
        self.assertFalse(b.claim('aster_oath',lambda bit: calls.append(bit) or bit==1));self.assertEqual(b.state['aster_oath']['mask'],1)
        b.claim('aster_oath',lambda bit:calls.append(bit) or True);self.assertEqual(calls,[1,2,2])
    def test_reward_reservation_visible_to_save_callback(self):
        b=Book();b.solve(CAT['sites'][0])
        def credit(bit):self.assertTrue(b.capture()['entries']['aster_oath']['mask']&bit);return True
        b.claim('aster_oath',credit)
    def test_exception_does_not_leave_reservation(self):
        b=Book();b.solve(CAT['sites'][0])
        def fail(_):raise ValueError('injected')
        with self.assertRaises(ValueError):b.claim('aster_oath',fail)
        self.assertEqual(b.state['aster_oath']['mask'],0);self.assertFalse(b.busy)
    def test_second_credit_failure_preserves_first(self):
        b=Book();b.solve(CAT['sites'][0])
        def fail(bit):
            if bit==2:raise ValueError('injected')
            return True
        with self.assertRaises(ValueError):b.claim('aster_oath',fail)
        self.assertEqual(b.state['aster_oath']['mask'],1)
    def test_reentrant_claim_blocked(self):
        b=Book();b.solve(CAT['sites'][0]);calls=[]
        def credit(bit):
            self.assertFalse(b.claim('aster_oath',lambda _:self.fail('reentrancy')));calls.append(bit);return True
        b.claim('aster_oath',credit);self.assertEqual(calls,[1,2])
    def test_circuit_requires_all_claimed(self):
        b=Book();[b.solve(d) for d in CAT['sites']];self.assertFalse(b.circuit_claim(lambda _:True))
    def test_circuit_once(self):
        b=Book()
        for d in CAT['sites']:b.solve(d);b.claim(d['id'],lambda _:True)
        self.assertTrue(b.circuit_claim(lambda _:True));self.assertFalse(b.circuit_claim(lambda _:True))
    def test_circuit_full_inventory_retry(self):
        b=Book()
        for d in CAT['sites']:b.solve(d);b.claim(d['id'],lambda _:True)
        self.assertFalse(b.circuit_claim(lambda _:False));self.assertFalse(b.circuit);self.assertTrue(b.circuit_claim(lambda _:True))
    def test_capture_detached(self):
        b=Book();b.read('aster_oath');s=b.capture();s['entries'].clear();self.assertTrue(b.state['aster_oath']['read'])
    def test_restore_partial_sequence(self):
        b=Book();b.read('aster_oath');b.touch('aster_oath',2);new=Book();new.restore(b.capture());self.assertEqual(new.state['aster_oath']['step'],1)
    def test_restore_partial_credit(self):
        b=Book();b.solve(CAT['sites'][0]);b.claim('aster_oath',lambda bit:bit==1);new=Book();new.restore(b.capture());calls=[]
        new.claim('aster_oath',lambda bit:calls.append(bit) or True);self.assertEqual(calls,[2])
    def test_restore_old_missing_block(self):
        b=Book();b.read('aster_oath');b.restore(None);self.assertFalse(b.state['aster_oath']['read'])
    def test_restore_corrupt_steps_clamped(self):
        b=Book();s=b.capture();s['entries']['aster_oath'].update(read=True,step=100,mistakes=-5);b.restore(s);self.assertEqual(b.state['aster_oath']['step'],2);self.assertEqual(b.state['aster_oath']['mistakes'],0)
    def test_restore_unknown_site_ignored(self):
        b=Book();s=b.capture();s['entries']['unknown']={};b.restore(s);self.assertEqual(len(b.state),3)
    def test_reset_new_playthrough(self):
        b=Book();b.solve(CAT['sites'][0]);b.claim('aster_oath',lambda _:True);b.reset();self.assertEqual(b.claimed,0)
    def test_exact_authored_reward_totals(self):
        self.assertEqual(sum(d['lumen'] for d in CAT['sites']),540);self.assertEqual([d['amount'] for d in CAT['sites']],[1,1,1]);self.assertEqual(CAT['circuitAmount'],1)

if __name__=='__main__':
    suite=unittest.defaultTestLoader.loadTestsFromTestCase(ModelTests);res=unittest.TextTestRunner(verbosity=2).run(suite)
    report={'scope':'Executed Python reference-model tests and generated action sequences, NOT execution of C# or Unity', 'tests_run':res.testsRun,'passed':res.wasSuccessful(),'failures':len(res.failures),'errors':len(res.errors),'random_actions':10000,'three_rune_sequences':81,'native_CSharp':'NOT RUN'}
    if '--report' in sys.argv:Path(sys.argv[sys.argv.index('--report')+1]).write_text(json.dumps(report,indent=2)+'\n')
    sys.exit(not res.wasSuccessful())
