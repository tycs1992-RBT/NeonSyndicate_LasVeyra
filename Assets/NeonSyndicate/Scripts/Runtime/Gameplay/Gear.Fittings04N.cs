using UnityEngine;
namespace NeonSyndicate
{
    public static class GearFittings04N
    {
        public static int Treatment(CraftingSystem forge)
        {if(forge==null)return -1;for(int i=0;i<ForgeTreatments04M.Ids.Length;i++)if(forge.BuffLeft(ForgeTreatments04M.Ids[i])>0)return i;return -1;}
        /// <summary>Eight distinct physical fittings, all within the forearm envelope. Pure geometry, no effects/stat grants.</summary>
        public static void Bracer(MeshBuilder b,int treatment)
        {
            if(treatment<0 || treatment>=ForgeTreatments04M.Ids.Length)return;
            b.Box(0,new Vector3(0,-.14f,-.046f),new Vector3(.09f,.18f,.02f),UVMode.Unit);
            if(treatment==11)
            {
                for(int side=-1;side<=1;side+=2)b.Box(0,new Vector3(side*.052f,-.14f,0),new Vector3(.022f,.24f,.12f),QMath.AZ(side*10f),UVMode.Unit);
                GothicRealmGeometry04OB.Crystal(b,new Vector3(0,-.08f,-.08f),new Vector3(.07f,.15f,.04f),0,11);
            }
            int count=2+treatment%3;
            for(int i=0;i<count;i++)
            {
                float y=-.20f+i*.12f/(count-1);
                b.Box(0,new Vector3(0,y,-.065f),new Vector3(.095f,.013f,.024f),QMath.AZ((treatment/3)*12f),UVMode.Unit);
                b.Box(1,new Vector3((i%2==0?-1:1)*.024f,y,-.082f),new Vector3(.022f,.025f,.012f),QMath.AZ(treatment*11f),UVMode.Unit);
            }
        }
        public static void Shell(MeshBuilder b,int upgrade)
        {
            int n=Mathf.Clamp(upgrade,1,5);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<n;i++)
            {b.Box(0,new Vector3(side*.13f,-.025f-i*.026f,.12f),new Vector3(.12f,.022f,.026f),QMath.AZ(side*12f),UVMode.Unit);}
        }
        public static void Collar(MeshBuilder b,int treatment)
        {
            if(treatment<0||treatment>=ForgeTreatments04M.Ids.Length)return;
            if(treatment==10 || treatment==12)
            {
                for(int i=0;i<5;i++)b.Box(1,new Vector3((treatment==12?.014f*i:0),0,.25f+i*.085f),new Vector3(.030f,.013f,.07f),QMath.AY(treatment==12?i*5f:0),UVMode.Unit);
            }
            for(int side=-1;side<=1;side+=2)
            {b.Box(0,new Vector3(side*.026f,0,.16f),new Vector3(.020f,.035f,.080f),UVMode.Unit);
             b.Box(1,new Vector3(side*.040f,0,.17f),new Vector3(.009f,.020f,.035f+treatment*.004f),QMath.AY(side*15f),UVMode.Unit);}
        }
    }
    public sealed partial class GearSystem
    {
        private GameObject _treatmentFitting04N,_shellFitting04N,_weaponFitting04N;
        private HumanoidRig _fittingRig04N;private Transform _fittingModel04N;private int _fittingKey04N=int.MinValue;private float _fittingPoll04N;
        private void TickFittings04N()
        {
            if(GameTime.Paused)return;_fittingPoll04N-=Time.deltaTime;if(_fittingPoll04N>0)return;_fittingPoll04N=.25f;
            PlayerController p=Game.Player;
            if(p==null || p.Rig==null || Game.State==GameState.Title || Game.State==GameState.Loading){ClearFittings04N();return;}
            int treatment=GearFittings04N.Treatment(Game.Crafting);GearItem shell=Equipped[(int)GearSlot.Shell];
            int upgrade=shell!=null?shell.Upgrade:0;
            Transform model=p.Weapons!=null && p.Weapons.Current.Fire==FireKind.Melee?p.Weapons.HeldModel04N:null;
            int key=(treatment+2)*97+upgrade*7+(shell!=null?shell.Uid:0)*397;
            if(_fittingKey04N==key && _fittingRig04N==p.Rig && _fittingModel04N==model)return;
            ClearFittings04N();_fittingKey04N=key;_fittingRig04N=p.Rig;_fittingModel04N=model;
            if(treatment>=0)
            {
                var b=new MeshBuilder(2);GearFittings04N.Bracer(b,treatment);
                _treatmentFitting04N=Fitting04N(b,p.Rig.ForearmL,"Paid Forge treatment fitting");
                if(model!=null && p.Weapons.Current.Id!=WeaponId.Fists && p.Weapons.Current.Id!=WeaponId.ArcGauntlets)
                {var w=new MeshBuilder(2);GearFittings04N.Collar(w,treatment);_weaponFitting04N=Fitting04N(w,model,"Tempered grip fitting");}
            }
            if(upgrade>0){var b=new MeshBuilder(1);GearFittings04N.Shell(b,upgrade);_shellFitting04N=Fitting04N(b,p.Rig.Chest,"Upgraded shell lamellae");}
        }
        private GameObject Fitting04N(MeshBuilder b,Transform anchor,string label)
        {
            GameObject go=new GameObject(label);go.transform.SetParent(anchor,false);go.layer=Layers.IgnoreRaycast;
            var f=go.AddComponent<MeshFilter>();f.sharedMesh=OwnedMeshes.Track(go,b.ToMesh(label));
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=f.sharedMesh.subMeshCount==1?new[]{Mats.Metal}:new[]{Mats.Metal,GearFittings04N.Treatment(Game.Crafting)>=8?GothicRealmMaterials04OB.For((GearFittings04N.Treatment(Game.Crafting)==9 || GearFittings04N.Treatment(Game.Crafting)>=12)?"teal":"crystal"):Mats.Glass};
            return go;
        }
        private void ClearFittings04N()
        {
            if(_treatmentFitting04N!=null)Destroy(_treatmentFitting04N);if(_shellFitting04N!=null)Destroy(_shellFitting04N);if(_weaponFitting04N!=null)Destroy(_weaponFitting04N);
            _treatmentFitting04N=_shellFitting04N=_weaponFitting04N=null;_fittingRig04N=null;_fittingModel04N=null;_fittingKey04N=int.MinValue;
        }
        private void OnDestroy(){ClearFittings04N();}
    }
}
