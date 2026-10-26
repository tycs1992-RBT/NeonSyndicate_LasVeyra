using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class HeartPresentationFR01
    {
        private readonly HeartCatalogFR01 _livingCatalog;
        private readonly List<GameObject> _livingObjects=new List<GameObject>();
        private readonly List<HeartLivingFR02A> _livingInstalled=new List<HeartLivingFR02A>();
        private bool _livingSelected,_livingLast;
        private int _livingSourceRefused,_livingNativeRefused,_livingHouses,_livingProps;
        private string _livingReason="Living Streets not prepared.";
        public bool LivingSelected {get{return _livingSelected;}}
        public bool LivingActive {get{return _livingLast;}}
        /// <summary>Explicit owner action, using the existing two switches and their admission checks. Defaults stay OFF.</summary>
        public bool EnableDeliveredLook(out string reason)
        {
            if(_catalog==null || !_catalog.Ready || _livingCatalog==null || !_livingCatalog.LivingReady || _livingHouses==0)
            {reason="The delivered district is unavailable. "+Status+" "+LivingStatus;return false;}
            if(!_selected)Toggle();
            if(!_livingSelected)ToggleLiving();
            reason=Status+" "+LivingStatus;
            return _selected && _livingSelected;
        }
        public string LivingStatus {get{return "Living Streets "+(_livingSelected?"ON":"OFF")+"; staged "+_livingHouses+" houses, "+_livingProps+" court pieces; active "+(_livingLast?_livingObjects.Count:0)+"; refused source "+_livingSourceRefused+", native "+_livingNativeRefused+". "+_livingReason;}}
        /// <summary>Run once in the original art lifetime. Never edits a lot, service, route graph or legacy mesh.</summary>
        private void BuildLiving()
        {
            if(_livingCatalog==null || !_livingCatalog.LivingReady){_livingReason="Prepared FR02A catalog missing. Stop Play, then Live World > Prepare Living Gothic Streets.";return;}
            try
            {
                // Court furnishings get their contextual spaces first. Houses cannot swallow the existing market approach.
                List<HeartLivingFR02A> proposals=HeartPlanFR01.LivingProposals(_layout);
                foreach(bool house in new[]{false,true})foreach(HeartLivingFR02A proposal in proposals)
                {
                    if(proposal.House!=house || (house?_livingHouses>=HeartPlanFR01.LivingHouseLimit:_livingProps>=HeartPlanFR01.LivingPropLimit))continue;
                    string reason;
                    if(!HeartPlanFR01.LivingClear(_layout,proposal,_livingInstalled,out reason)){_livingSourceRefused++;continue;}
                    RaycastHit ground;
                    if(!Physics.Raycast(proposal.Position+Vector3.up*1.5f,Vector3.down,out ground,3f,Layers.WorldMask,QueryTriggerInteraction.Ignore) || ground.normal.y<.85f || Mathf.Abs(ground.point.y-proposal.Position.y)>.5f)
                    {_livingNativeRefused++;Log.Info("FR02A refused "+proposal.Key+": original flat supporting ground not found.");continue;}
                    proposal.Position=new Vector3(proposal.Position.x,ground.point.y,proposal.Position.z);
                    if(!LivingSpace(proposal,out reason)){_livingNativeRefused++;Log.Info("FR02A refused "+proposal.Key+": "+reason);continue;}
                    HeartModuleFR01 module=_livingCatalog.Find(proposal.Module);
                    if(module==null || module.Size.x>proposal.Size.x || module.Size.y>proposal.Size.y || module.Size.z>proposal.Size.z)
                    {_livingNativeRefused++;Log.Warn("FR02A bounds refused "+proposal.Module+"; original world retained.");continue;}
                    GameObject copy=null;
                    try
                    {
                        // Center the actual asymmetric bounds and put their bottom on the original route's ground level.
                        Vector3 offset=new Vector3(module.Center.x,module.Center.y-module.Size.y*.5f,module.Center.z);
                        copy=Model(proposal.Module,proposal.Position-Quaternion.Euler(0,proposal.Yaw,0)*offset,proposal.Yaw,Vector3.one,_livingCatalog);
                        if(house)LivingProxy(copy,module.Center,module.Size);
                        else if(proposal.Module=="CourtMarket")
                        {
                            // Match the authored counter and four posts, leaving the spaces under the shelter open.
                            LivingProxy(copy,new Vector3(0,.7f,.792f),new Vector3(4.675f,1.4f,.9f));
                            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})LivingProxy(copy,new Vector3(x*2.75f,1.8f,z*1.8f),new Vector3(.15f,3.6f,.15f));
                        }
                        else if(proposal.Module=="CourtLantern")LivingProxy(copy,new Vector3(0,1.75f,0),new Vector3(.28f,3.5f,.28f));
                        else LivingProxy(copy,new Vector3(module.Center.x,.6f,module.Center.z),new Vector3(module.Size.x,Mathf.Min(1.2f,module.Size.y),module.Size.z));
                        _livingObjects.Add(copy);_livingInstalled.Add(proposal);if(house)_livingHouses++;else _livingProps++;
                        Log.Info("FR02A staged "+proposal.Key+" / "+proposal.Module+" at "+proposal.Position.ToString("F1")+". Original owners unchanged.");
                    }
                    catch(Exception e){if(copy!=null)UnityEngine.Object.Destroy(copy);_livingNativeRefused++;Log.Warn("FR02A piece retained original space: "+e.Message);}
                }
                _livingSelected=PlayerPrefs.GetInt("NSLV_heart_fr02a",0)==1;
                _livingReason=_livingHouses>0?"Closed house fronts add a bounded neighborhood. Original services, two FR01 substitutions and all legacy collision remain. Native appearance acceptance pending.":"FAILED: no neighborhood house admitted. Original world retained; F2 and FR02A Console lines identify refusals.";
            }
            catch(Exception e)
            {
                foreach(GameObject copy in _livingObjects)if(copy!=null)UnityEngine.Object.Destroy(copy);
                _livingObjects.Clear();_livingInstalled.Clear();_livingHouses=0;_livingProps=0;_livingSelected=false;_livingReason="Rolled back: "+e.Message;
            }
            Log.Info(LivingStatus);
        }
        private static void LivingProxy(GameObject parent,Vector3 center,Vector3 size)
        {GameObject proxy=new GameObject("FR02A new bounded structure proxy");proxy.transform.SetParent(parent.transform,false);proxy.layer=Layers.World;BoxCollider collider=proxy.AddComponent<BoxCollider>();collider.center=center;collider.size=size;}
        private static bool LivingSpace(HeartLivingFR02A p,out string reason)
        {
            // Full height, roof overhangs and every trigger; exclude only the existing supporting ground below .25m.
            Vector3 center=p.Position+Vector3.up*(p.Size.y*.5f+.15f);
            return Probe(center,new Vector3(p.Size.x*.5f,p.Size.y*.5f-.1f,p.Size.z*.5f),Quaternion.Euler(0,p.Yaw,0),out reason);
        }
        public bool LivingRoute(out Vector3 point)
        {
            point=Vector3.zero;if(_livingHouses==0)return false;
            foreach(RealmRoute04OB r in _layout.RealmRoutes04OB)if(r.Id=="office" && r.Points.Count>0){point=r.Points[0];return true;}
            return false;
        }
        private bool LivingActivation(out string reason)
        {
            foreach(HeartLivingFR02A p in _livingInstalled)
            {
                if(!LivingSpace(p,out reason))return false;
                foreach(Collider hit in Physics.OverlapBox(p.Position+Vector3.up*p.Size.y*.5f,p.Size*.5f,Quaternion.Euler(0,p.Yaw,0),~0,QueryTriggerInteraction.Collide))
                    if(hit.GetComponentInParent<CharacterBase>()!=null || hit.GetComponentInParent<Vehicle>()!=null){reason="Move people and vehicles clear of "+p.Key+" before enabling.";return false;}
            }
            reason="";return true;
        }
        public void ToggleLiving()
        {
            if(_livingCatalog==null || !_livingCatalog.LivingReady || _livingHouses==0){Log.Warn(LivingStatus);return;}
            string reason;if(!_livingSelected && !LivingActivation(out reason)){_livingReason="Activation refused: "+reason;Log.Warn(LivingStatus);return;}
            _livingSelected=!_livingSelected;PlayerPrefs.SetInt("NSLV_heart_fr02a",_livingSelected?1:0);PlayerPrefs.Save();ApplyLiving();Log.Info(LivingStatus);
        }
        private void ApplyLiving()
        {
            bool active=HeartPlanFR01.Active(_livingSelected,_compare,_suspended,_livingCatalog!=null && _livingCatalog.LivingReady && _livingHouses>0);
            if(active==_livingLast)return;
            string reason;if(active && !LivingActivation(out reason)){_livingReason="Appearance refused: "+reason;Log.Warn(LivingStatus);return;}
            foreach(GameObject copy in _livingObjects)if(copy!=null)copy.SetActive(active);_livingLast=active;
        }
    }
}
