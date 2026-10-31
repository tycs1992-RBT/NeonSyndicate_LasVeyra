#!/usr/bin/env python3
"""External-only Tripo v3 adapter. No Unity dependency, no credential serialization, no automatic paid submission."""
import argparse,contextlib,datetime,decimal,hashlib,ipaddress,json,os,re,socket,time,urllib.error,urllib.parse,urllib.request
from pathlib import Path
D=decimal.Decimal
BASES={'ov':'https://openapi.tripo3d.ai/v3','cn':'https://openapi.tripo3d.com/v3'}
PRICING_DATE='2026-10-28';PRICING_SOURCE='https://developers.tripo3d.ai/en/pricing'
class FoundryError(Exception):pass
class NoRedirect(urllib.request.HTTPRedirectHandler):
 def redirect_request(self,*args,**kwargs):raise FoundryError('Redirect refused. Credentials are never forwarded.')
def stamp():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def canonical(value):return json.dumps(value,sort_keys=True,separators=(',',':')).encode()
def digest(value):return hashlib.sha256(canonical(value)).hexdigest()
def request_for(brief):
 kind=brief['generation']['kind'];g=brief['generation'];p=dict(g['parameters'])
 if kind not in ('text','image'):raise FoundryError('Only explicit text or image generation supported.')
 allowed={'prompt','negative_prompt','input','model','model_seed','texture_seed','image_seed','face_limit','texture','pbr','texture_quality','geometry_quality'}
 if set(p)-allowed:raise FoundryError('Unquoted generation parameters rejected.')
 if p.get('model')!='v3.1-20260211':raise FoundryError('Unquoted model rejected. Review pricing first.')
 if kind=='text' and (not isinstance(p.get('prompt'),str)or not 1<=len(p['prompt'])<=1024):raise FoundryError('Text prompt missing or too long.')
 if kind=='image' and (not brief.get('referenceRightsApproved')or not re.fullmatch(r'file_[A-Za-z0-9_-]+',p.get('input',''))):raise FoundryError('Image input requires an explicitly approved source and uploaded file token. No automatic image upload.')
 if p.get('texture_quality','standard')not in ('standard','detailed','extreme')or p.get('geometry_quality','standard')not in ('standard','detailed'):raise FoundryError('Unquoted quality rejected.')
 if p.get('face_limit',50000)>100000 or p.get('face_limit',50000)<500:raise FoundryError('Production face limit out of range.')
 return '/generation/'+('text-to-model'if kind=='text'else'image-to-model'),p
def quote(brief):
 endpoint,p=request_for(brief);textured=p.get('pbr',True)or p.get('texture',True);cost=D(10 if brief['generation']['kind']=='text'else 20)
 if textured:cost+=10
 if textured:cost+=D({'standard':0,'detailed':10,'extreme':20}[p.get('texture_quality','standard')])
 if p.get('geometry_quality','standard')=='detailed':cost+=20
 return {'endpoint':endpoint,'parameters':p,'requestHash':digest({'endpoint':endpoint,'parameters':p}),'estimatedCredits':str(cost),'estimatedUSD':str(cost*D('.01')),'pricingDate':PRICING_DATE,'pricingSource':PRICING_SOURCE,'providerHardCapAvailable':False,'note':'Local admission estimate, not a provider-enforced price ceiling. No live quote or billing verification has occurred.'}
def approved(q,approval):
 now=datetime.datetime.now(datetime.timezone.utc);expiry=datetime.datetime.fromisoformat(approval['expiresUtc'])
 if expiry.tzinfo is None or expiry<=now:raise FoundryError('Approval expired or lacks timezone.')
 if approval.get('ownerApproved')is not True or approval.get('requestHash')!=q['requestHash']or approval.get('pricingDate')!=PRICING_DATE:raise FoundryError('Exact owner approval and reviewed pricing required.')
 if approval.get('acknowledgeEstimateNotProviderCap')is not True:raise FoundryError('Approval must acknowledge the provider has no request-level hard cap.')
 limit=D(str(approval.get('maxCredits','0')))
 if not limit.is_finite()or limit<=0 or D(q['estimatedCredits'])>limit:raise FoundryError('Local credit ceiling exceeded.')
 if (now.date()-datetime.date.fromisoformat(PRICING_DATE)).days>1:raise FoundryError('Pricing snapshot expired. Recheck official pricing before paid use.')
 return limit
class Client:
 def __init__(self,region=None,key=None,opener=None):
  region=region or os.environ.get('TRIPO_REGION');self.key=key or os.environ.get('TRIPO_API_KEY')
  if region not in BASES:raise FoundryError('Set TRIPO_REGION explicitly to ov or cn. Account region is never inferred.')
  if not self.key:raise FoundryError('TRIPO_API_KEY is not configured.')
  self.base=BASES[region];self.opener=opener or urllib.request.build_opener(NoRedirect())
 def api(self,path,payload=None):
  if not re.fullmatch(r'/(?:account/balance|tasks/task_[A-Za-z0-9_-]+|generation/(?:text|image)-to-model)',path):raise FoundryError('Unapproved API path.')
  req=urllib.request.Request(self.base+path,data=canonical(payload)if payload is not None else None,headers={'Authorization':'Bearer '+self.key,'Content-Type':'application/json'})
  for attempt in range(3 if payload is None else 1):
   try:
    with self.opener.open(req,timeout=30)as response:raw=response.read(2*1024*1024)
    value=json.loads(raw)
    if value.get('code')!=0:raise FoundryError('Provider rejected operation; code '+str(value.get('code'))+'. Raw provider messages are not logged.')
    return value['data']
   except(urllib.error.URLError,TimeoutError,ValueError)as error:
    if payload is not None:raise FoundryError('Submission outcome unknown. Do not retry automatically; reconcile the task in the provider console.')from None
    if attempt==2:raise FoundryError('Read operation failed after bounded retries. No credentials or response bodies logged.')from None
    time.sleep(2**attempt)
 def submit(self,brief,approval,state):
  q=quote(brief);approved(q,approval);state=Path(state);state.mkdir(parents=True,exist_ok=True);record=state/(q['requestHash']+'.json')
  # Exclusive reservation survives a timeout or crash. Never resubmit an ambiguous paid request.
  try:
   with record.open('x')as stream:json.dump({'requestHash':q['requestHash'],'state':'RESERVED','estimatedCredits':q['estimatedCredits'],'createdUtc':stamp()},stream)
  except FileExistsError:raise FoundryError('This request already has a reservation. Inspect it instead of paying twice.')from None
  try:
   task=self.api(q['endpoint'],q['parameters']);task_id=task.get('task_id','')
   if not re.fullmatch(r'task_[A-Za-z0-9_-]+',task_id):raise FoundryError('Provider response lacks a valid task id. Reservation retained.')
   record.write_text(json.dumps({'requestHash':q['requestHash'],'taskId':task_id,'state':'SUBMITTED','estimatedCredits':q['estimatedCredits'],'createdUtc':stamp()},indent=2)+'\n');return task_id
  except FoundryError:raise
 def poll(self,task_id,timeout=180,interval=2):
  deadline=time.monotonic()+timeout
  while time.monotonic()<deadline:
   task=self.api('/tasks/'+task_id);status=task.get('status')
   if status=='success':return task
   if status in ('failed','cancelled'):raise FoundryError('Task ended '+status+'; provider details omitted from logs.')
   if status not in ('queued','running'):raise FoundryError('Unexpected provider task status.')
   time.sleep(max(2,min(interval,30)))
  raise FoundryError('Polling deadline reached. Resume with the same task id, never resubmit.')
 def download(self,url,target,allowed_hosts=()):
  parsed=urllib.parse.urlsplit(url);host=parsed.hostname or ''
  if parsed.scheme!='https'or parsed.username or parsed.password or parsed.port not in (None,443):raise FoundryError('Unsafe model URL.')
  if not(host.endswith('.tripo3d.ai')or host.endswith('.tripo3d.com')or host in allowed_hosts):raise FoundryError('Model CDN host requires explicit local allowlisting.')
  addresses=socket.getaddrinfo(host,443,type=socket.SOCK_STREAM)
  if not addresses or any(not ipaddress.ip_address(a[4][0]).is_global for a in addresses):raise FoundryError('Private or unresolved download host refused.')
  target=Path(target)
  if target.exists():raise FoundryError('Refuse overwriting an existing download.')
  part=target.with_name(target.name+'.part');target.parent.mkdir(parents=True,exist_ok=True);size=0;sha=hashlib.sha256()
  # Deliberately separate opener/request: no bearer header is sent to CDN downloads.
  try:
   with self.opener.open(urllib.request.Request(url),timeout=60)as response,part.open('xb')as stream:
    while True:
     chunk=response.read(1024*1024)
     if not chunk:break
     size+=len(chunk)
     if size>128*1024*1024:raise FoundryError('Model exceeds bounded download budget.')
     stream.write(chunk);sha.update(chunk)
   if part.read_bytes()[:4]!=b'glTF':raise FoundryError('Downloaded payload is not a GLB model. No automatic import.')
   part.rename(target);return {'bytes':size,'sha256':sha.hexdigest(),'downloadUrlStored':False,'sourceRights':'REVIEW REQUIRED'}
  except Exception:
   if part.exists():part.unlink()
   raise FoundryError('Model download refused or failed. Credential and signed URL details omitted.')from None
def main():
 p=argparse.ArgumentParser();p.add_argument('operation',choices=['doctor','quote','submit','fetch']);p.add_argument('--brief',type=Path);p.add_argument('--approval',type=Path);p.add_argument('--state',type=Path,default=Path('.foundry-local/tasks'));p.add_argument('--task');p.add_argument('--output',type=Path);p.add_argument('--allow-download-host',action='append',default=[]);a=p.parse_args()
 try:
  if a.operation=='doctor':
   if not os.environ.get('TRIPO_API_KEY'):print(json.dumps({'credentialConfigured':False,'connection':'NOT ESTABLISHED','paidGeneration':'DISABLED'}));return 0
   client=Client();client.api('/account/balance');print(json.dumps({'credentialConfigured':True,'connection':'READ-ONLY BALANCE VERIFIED','paidGeneration':'REQUIRES EXACT OWNER APPROVAL'}));return 0
  if a.operation=='quote':print(json.dumps(quote(json.loads(a.brief.read_text())),indent=2));return 0
  client=Client()
  if a.operation=='submit':
   if a.approval is None:raise FoundryError('No paid generation without an exact owner approval file.')
   print(json.dumps({'taskId':client.submit(json.loads(a.brief.read_text()),json.loads(a.approval.read_text()),a.state)}));return 0
  if not a.task or a.output is None:raise FoundryError('Existing task id and new local output required.')
  task=client.poll(a.task);receipt=client.download(task['output']['model_url'],a.output,a.allow_download_host);receipt.update(taskId=a.task,creditsConsumed=str(task.get('credits_consumed','UNSPECIFIED')),nativeUnity='NOT RUN');a.output.with_suffix('.receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt));return 0
 except(FoundryError,KeyError,ValueError,TypeError,OSError)as error:
  # Never print an arbitrary exception that could contain request headers, response bodies or signed URLs.
  print(json.dumps({'error':str(error)if isinstance(error,FoundryError)else'Local configuration rejected.','connection':'UNVERIFIED'}));return 2
if __name__=='__main__':raise SystemExit(main())
