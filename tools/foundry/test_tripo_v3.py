"""Offline network-contract and spend-gate regressions. No live provider operation."""
import copy,datetime,json,tempfile,unittest,urllib.error
from pathlib import Path
from tripo_v3 import Client,FoundryError,approved,quote,PRICING_DATE
class FakeResponse:
 def __init__(self,data):self.data=json.dumps({'code':0,'data':data}).encode()
 def __enter__(self):return self
 def __exit__(self,*args):pass
 def read(self,*args):return self.data
class FakeOpener:
 def __init__(self):self.calls=[]
 def open(self,request,**kwargs):self.calls.append(request);return FakeResponse({'task_id':'task_mock123'})
class AdapterTests(unittest.TestCase):
 def brief(self):return {'generation':{'kind':'text','parameters':{'model':'v3.1-20260211','prompt':'Original sanctuary','face_limit':50000,'texture':True,'pbr':True,'texture_quality':'detailed'}}}
 def approval(self,q):return {'ownerApproved':True,'requestHash':q['requestHash'],'pricingDate':PRICING_DATE,'maxCredits':'30.00','acknowledgeEstimateNotProviderCap':True,'expiresUtc':(datetime.datetime.now(datetime.timezone.utc)+datetime.timedelta(hours=1)).isoformat()}
 def test_quote_is_not_a_connection_or_a_spending_grant(self):
  q=quote(self.brief());self.assertEqual(q['estimatedCredits'],'30');self.assertFalse(q['providerHardCapAvailable'])
 def test_missing_approval_rejects_before_network(self):
  opener=FakeOpener();client=Client('ov','test-secret-never-logged',opener)
  with tempfile.TemporaryDirectory()as temp:
   with self.assertRaises((FoundryError,KeyError)):client.submit(self.brief(),{},temp)
  self.assertEqual(opener.calls,[])
 def test_exact_request_and_cap_required(self):
  q=quote(self.brief());approval=self.approval(q);approval['maxCredits']='29.99'
  with self.assertRaises(FoundryError):approved(q,approval)
  approval=self.approval(q);approval['requestHash']='other'
  with self.assertRaises(FoundryError):approved(q,approval)
 def test_no_automatic_duplicate_submission(self):
  opener=FakeOpener();client=Client('ov','test-secret-never-logged',opener);brief=self.brief();approval=self.approval(quote(brief))
  with tempfile.TemporaryDirectory()as temp:
   self.assertEqual(client.submit(brief,approval,temp),'task_mock123')
   with self.assertRaises(FoundryError):client.submit(brief,approval,temp)
   self.assertNotIn('test-secret-never-logged',''.join(p.read_text()for p in Path(temp).glob('*')))
  self.assertEqual(len(opener.calls),1)
 def test_post_timeout_preserves_reservation_and_never_retries(self):
  class TimeoutOpener(FakeOpener):
   def open(self,request,**kwargs):self.calls.append(request);raise urllib.error.URLError('Private response details')
  opener=TimeoutOpener();client=Client('ov','test-secret-never-logged',opener);brief=self.brief()
  with tempfile.TemporaryDirectory()as temp:
   with self.assertRaisesRegex(FoundryError,'outcome unknown'):client.submit(brief,self.approval(quote(brief)),temp)
   self.assertEqual(len(list(Path(temp).glob('*.json'))),1)
  self.assertEqual(len(opener.calls),1)
 def test_unknown_model_or_parameter_has_no_quote(self):
  brief=self.brief();brief['generation']['parameters']['model']='future'
  with self.assertRaises(FoundryError):quote(brief)
  brief=self.brief();brief['generation']['parameters']['ultraMode']=True
  with self.assertRaises(FoundryError):quote(brief)
 def test_image_requires_authorized_file_token(self):
  brief=self.brief();brief['generation']['kind']='image';brief['generation']['parameters'].pop('prompt');brief['generation']['parameters']['input']='file_mock';brief['referenceRightsApproved']=False
  with self.assertRaises(FoundryError):quote(brief)
  brief['referenceRightsApproved']=True;self.assertEqual(quote(brief)['estimatedCredits'],'40')
 def test_region_and_download_hosts_are_never_guessed(self):
  with self.assertRaises(FoundryError):Client('invalid','test-secret')
  client=Client('ov','test-secret',FakeOpener())
  with self.assertRaises(FoundryError):client.download('http://127.0.0.1/private','not-written.glb')
  with self.assertRaises(FoundryError):client.download('https://unapproved.example/model.glb','not-written.glb')
 def test_expired_or_unacknowledged_approval_rejected(self):
  q=quote(self.brief());a=self.approval(q);a['expiresUtc']='2020-01-01T00:00:00+00:00'
  with self.assertRaises(FoundryError):approved(q,a)
  a=self.approval(q);a['acknowledgeEstimateNotProviderCap']=False
  with self.assertRaises(FoundryError):approved(q,a)
if __name__=='__main__':unittest.main()
