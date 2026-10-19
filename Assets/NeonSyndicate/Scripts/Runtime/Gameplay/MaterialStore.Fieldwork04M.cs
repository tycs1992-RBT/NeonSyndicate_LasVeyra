using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class MaterialStore
    {
        private sealed class FieldNode04M
        {
            public FieldSource04M Source;public Transform Root,Crystal;public Interactable Use;
        }
        private readonly List<FieldNode04M> _fieldNodes04M=new List<FieldNode04M>();
        private float _fieldAt04M;
        private int _fieldPage04M;
        private void Update()
        {
            if(!Game.IsPlaying || GameTime.Paused || Game.Player==null || Game.Layout==null || Game.Quests==null)return;
            bool under=WorldBounds.IsUnderground(Game.PlayerPosition);
            foreach(FieldNode04M node in _fieldNodes04M)
            {
                if(node.Root==null)continue;
                bool active=node.Source.Underground==under && (Game.Inner==null || !Game.Inner.IsInner) && !Game.Quests.Flag(node.Source.Receipt);
                node.Root.gameObject.SetActive(active);
                if(active && node.Crystal!=null)node.Crystal.localRotation=QMath.AY(Time.time*18f);
            }
            if(Time.time<_fieldAt04M)return;_fieldAt04M=Time.time+1f;
            foreach(FieldSource04M source in FieldSources04M.All)
            {
                if(source.Underground!=under || Game.Quests.Flag(source.Receipt))continue;
                bool exists=false;foreach(FieldNode04M node in _fieldNodes04M)if(node.Source==source){exists=true;break;}
                Vector3 expected;
                if(exists || !ResolveField04M(source,out expected) || (expected-Game.PlayerPosition).sqrMagnitude>160f*160f)continue;
                Vector3 point;
                if(!FieldPosition04M(source,expected,out point))continue;
                BuildField04M(source,point);
                break; // At most one small geometry/interaction allocation per update slice.
            }
        }
        private static bool ResolveField04M(FieldSource04M source,out Vector3 point)
        {
            point=Vector3.zero;if(Game.Layout==null)return false;
            if(!QuestPlaces.Resolve(Game.Layout,source.At,source.Offset.x,source.Offset.z,Vector3.zero,Vector3.zero,false,Game.PlayerPosition,61724,out point))return false;
            if(!source.Underground && source.At.StartsWith("Site:"))point=QuestPlaces.PushOutOfArenas(Game.Layout,point);
            return true;
        }
        private static bool FieldPosition04M(FieldSource04M source,Vector3 expected,out Vector3 point)
        {
            point=expected;
            for(int i=0;i<8;i++)
            {
                Vector3 test=expected+MathUtil.DirFromYaw(i*45f)*(i==0?0f:1.5f);
                RaycastHit ground;
                if(!Physics.Raycast(test+Vector3.up*1.2f,Vector3.down,out ground,2.6f,Layers.WorldMask,QueryTriggerInteraction.Ignore))continue;
                test.y=ground.point.y;
                if(WorldBounds.IsUnderground(test)!=source.Underground || Mathf.Abs(test.y-expected.y)>1.4f)continue;
                if(Game.Layout!=null && !source.Underground && Game.Layout.RoadAt(test,1.2f)!=null)continue;
                if(Physics.CheckCapsule(test+Vector3.up*.45f,test+Vector3.up*1.6f,.35f,Layers.WorldMask|1<<Layers.Vehicles,QueryTriggerInteraction.Ignore))continue;
                bool doorway=false;
                foreach(Interactable existing in Interactable.All)
                    if(existing!=null && (existing.transform.position-test).sqrMagnitude<1.5f*1.5f){doorway=true;break;}
                if(doorway)continue;
                point=test;return true;
            }
            return false;
        }
        private void BuildField04M(FieldSource04M source,Vector3 point)
        {
            GameObject root=new GameObject(source.Title);root.transform.SetParent(transform,false);root.transform.position=point;
            MeshBuilder mesh=new MeshBuilder(1);
            mesh.Cylinder(0,new Vector3(0,.26f,0),.30f,.20f,.45f,6,QMath.Identity,true,UVMode.World);
            mesh.Box(0,new Vector3(0,.62f,0),new Vector3(.22f,.22f,.22f),QMath.AZ(45f),UVMode.World);
            GameObject crystal=new GameObject("Recoverable mineral");crystal.transform.SetParent(root.transform,false);
            crystal.AddComponent<MeshFilter>().sharedMesh=OwnedMeshes.Track(crystal,mesh.ToMesh("04M recoverable mineral"));
            crystal.AddComponent<MeshRenderer>().sharedMaterial=Mats.Emissive(source.Underground?new Color(.55f,.75f,.85f):new Color(.35f,.7f,.65f),.7f);
            FieldNode04M node=new FieldNode04M{Source=source,Root=root.transform,Crystal=crystal.transform};
            node.Use=Interactable.Create(source.Title,point,2.2f,"Press E to recover "+source.Count+" "+MaterialDb.NameOf(source.Material)+" (finite deposit)",()=>RecoverField04M(node),root.transform);
            node.Use.HeightTolerance=1.4f;node.Use.Condition=()=>FieldAllowed04M(node);
            node.Use.DynamicPrompt=()=>Count(source.Material)>9999-source.Count?"Material storage is full. Spend or sell some first.":
                "Press E to recover "+source.Count+" "+MaterialDb.NameOf(source.Material)+" (once per save)";
            _fieldNodes04M.Add(node);
        }
        private bool FieldAllowed04M(FieldNode04M node)
        {
            if(Game.Player==null || Game.Quests==null || node.Root==null)return false;
            Vector3 delta=Game.PlayerPosition-node.Root.position;
            bool safe=!CombatDirector.InCombat && (Game.Wanted==null || Game.Wanted.Stars==0)
                && (Game.Inner==null || !Game.Inner.IsInner) && !Physics.Linecast(Game.Player.HeadPosition,node.Root.position+Vector3.up,Layers.WorldMask,QueryTriggerInteraction.Ignore);
            return FieldSources04M.CanRecover(Game.Quests.Flag(node.Source.Receipt),!Game.Player.IsDead && !Game.Player.IsRagdolled,
                !Game.Player.InVehicle && Game.Player.IsGroundedPublic && !Game.Player.Climbing,
                node.Source.Underground==WorldBounds.IsUnderground(Game.PlayerPosition),Game.IsPlaying,safe,MathUtil.Flat(delta).magnitude,delta.y);
        }
        private void RecoverField04M(FieldNode04M node)
        {
            if(!FieldAllowed04M(node) || Count(node.Source.Material)>9999-node.Source.Count)return;
            // Both receipt and materials are captured by the existing world-save transaction in the same frame.
            if(!Game.Quests.SetFlag(node.Source.Receipt))return;
            Add(node.Source.Material,node.Source.Count,true);node.Root.gameObject.SetActive(false);
            if(Game.Root!=null && (Game.Missions==null || Game.Missions.Active==null))Game.Root.Autosave();
        }
        private void ClearField04M()
        {
            foreach(FieldNode04M node in _fieldNodes04M)if(node.Root!=null)Destroy(node.Root.gameObject);
            _fieldNodes04M.Clear();_fieldAt04M=0f;
        }
        private void OnDisable(){ClearField04M();}
        private void DrawFieldwork04M(Rect rect)
        {
            UIKit.Rect(rect,UIKit.Panel);
            UIKit.Text(new Rect(rect.x+20,rect.y+12,rect.width-40,45),"FIELD RECOVERIES",30,UIKit.Gold,TextAnchor.UpperLeft,FontStyle.Bold);
            UIKit.Text(new Rect(rect.x+20,rect.y+58,rect.width-40,48),"Six finite deposits. Follow public doors, service stairs and marked roads. Recovery persists in your save; deposits do not respawn.",18,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
            int rows=FieldSources04M.PageSize(rect.height),pages=(FieldSources04M.All.Length+rows-1)/rows;
            _fieldPage04M=Mathf.Clamp(_fieldPage04M,0,pages-1);
            Rect area=new Rect(rect.x+12,rect.y+112,rect.width-24,rect.height-170);
            float y=area.y;
            int first=_fieldPage04M*rows;
            for(int item=first;item<Mathf.Min(FieldSources04M.All.Length,first+rows);item++)
            {
                FieldSource04M source=FieldSources04M.All[item];
                bool claimed=Game.Quests!=null && Game.Quests.Flag(source.Receipt);
                Vector3 point;bool resolved=ResolveField04M(source,out point);
                UIKit.Text(new Rect(area.x+12,y,area.width-220,28),source.Title+" / "+(claimed?"depleted":"recoverable"),21,Color.white,TextAnchor.UpperLeft);
                UIKit.Text(new Rect(area.x+12,y+30,area.width-220,44),(source.Underground?"Underheart":"Surface route")+"; "+source.Count+" "+MaterialDb.NameOf(source.Material)+". "+(resolved?"Follow its entrance or road.":"World location unavailable."),16,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,false,true);
                if(!claimed && resolved && UIKit.Button(new Rect(area.xMax-170,y+12,150,44),"TRACK",false,19) && Game.Gps!=null)
                    Game.Gps.SetWaypoint(point); // Mission GPS keeps its existing higher priority.
                y+=90;
            }
            if(_fieldPage04M>0 && UIKit.Button(new Rect(rect.x+20,rect.yMax-52,150,40),"PREVIOUS",false,18))_fieldPage04M--;
            UIKit.Text(new Rect(rect.x+190,rect.yMax-48,200,30),"Page "+(_fieldPage04M+1)+" / "+pages,18,Color.white,TextAnchor.MiddleCenter);
            if(_fieldPage04M+1<pages && UIKit.Button(new Rect(rect.xMax-170,rect.yMax-52,150,40),"NEXT",false,18))_fieldPage04M++;
        }
    }
}
