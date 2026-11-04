#!/usr/bin/env python3
"""Write an original skinned FBX calibration fixture from the actual source rig. Not a production hero."""
import argparse,hashlib,json,shutil,subprocess,math
from pathlib import Path
CODE='''using System;using System.Linq;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public static class Probe{public static void Main(){RigLayout rig=RigLayout.From(Appearance.Malik());Console.WriteLine(JsonSerializer.Serialize(new{bones=Enumerable.Range(0,RigLayout.BoneCount).Select(i=>new{name=RigLayout.Names[i],parent=RigLayout.Parent[i],local=new[]{rig.Local[i].x,rig.Local[i].y,rig.Local[i].z},world=new[]{rig.World[i].x,rig.World[i].y,rig.World[i].z}})}));}}'''
def main():
 p=argparse.ArgumentParser(description=__doc__)
 for arg in ['dotnet','assembly','unity-references','output','fixture']:p.add_argument('--'+arg,type=Path,required=True)
 a=p.parse_args();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True);sdk=a.dotnet.resolve().parent
 csc=sorted(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1];refs=list(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))+list(a.unity_references.resolve().glob('*.dll'))+[a.assembly.resolve()]
 src=out/'RigProbe.cs';src.write_text(CODE);exe=out/'RigProbe.dll'
 rsp=out/'probe.rsp';rsp.write_text('\n'.join('"'+str(v)+'"'for v in ['-nologo','-target:exe','-langversion:9','-out:'+str(exe)]+['-r:'+str(r)for r in refs]+[src]))
 subprocess.run([str(a.dotnet),str(csc),'@'+str(rsp)],check=True)
 for ref in refs:
  if ref.parent==a.unity_references.resolve() or ref==a.assembly.resolve():shutil.copyfile(ref,out/ref.name)
 exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
 r=subprocess.run([str(a.dotnet),str(exe)],capture_output=True,text=True,check=True);rig=json.loads(r.stdout);(out/'rig.json').write_text(json.dumps(rig,indent=2)+'\n')
 write_fixture(rig,a.fixture)
def write_fixture(rig,path):
 bones=rig['bones'];n=len(bones);lines=['; FBX 7.4.0 project format','; Original diagnostic joint markers, NOT Malik production art.',
 'FBXHeaderExtension: { FBXHeaderVersion: 1003\n FBXVersion: 7400\n Creator: "Crystal Manor import calibration 04OH"\n}',
 'GlobalSettings: { Version: 1000\n Properties70: {\n P: "UpAxis", "int", "Integer", "",1\n P: "UpAxisSign", "int", "Integer", "",1\n P: "FrontAxis", "int", "Integer", "",2\n P: "FrontAxisSign", "int", "Integer", "",-1\n P: "CoordAxis", "int", "Integer", "",0\n P: "CoordAxisSign", "int", "Integer", "",1\n P: "UnitScaleFactor", "double", "Number", "",100\n P: "TimeMode", "enum", "", "",6\n }\n}',
 'Documents: { Count: 1\n Document: 1, "Scene", "Scene" { RootNode: 0 }\n}',
 'Definitions: { Version: 100\n Count: 0\n}', 'Objects: {']
 def array(name,values):return name+': *'+str(len(values))+' { a: '+','.join(format(v,'.9g')if isinstance(v,float)else str(v)for v in values)+' }'
 def matrix(pos):return [1,0,0,0,0,1,0,0,0,0,1,0,*pos,1]
 vertices=[];indices=[];uv=[];normals=[];uvindex=[]
 offsets=[(.018,.018,.018),(-.018,-.018,.018),(-.018,.018,-.018),(.018,-.018,-.018)];faces=[(0,2,1),(0,1,3),(0,3,2),(1,2,3)]
 for i,bone in enumerate(bones):
  for offset in offsets:vertices.extend(bone['world'][k]+offset[k]for k in range(3))
  for face in faces:
   indices.extend([i*4+face[0],i*4+face[1],-(i*4+face[2]+1)])
   a,b,c=[offsets[v]for v in face];u=[b[k]-a[k]for k in range(3)];v=[c[k]-a[k]for k in range(3)]
   normal=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];length=math.sqrt(sum(x*x for x in normal));normal=[x/length for x in normal]
   for j in range(3):normals.extend(normal);uvindex.append(len(uv)//2);uv.extend([(0,.5,1)[j],(0,1,0)[j]])
 lines+=['Geometry: 100, "Geometry::CalibrationJointMarkers", "Mesh" {',array('Vertices',vertices),array('PolygonVertexIndex',indices),
 'LayerElementNormal: 0 { Version: 101\n Name: ""\n MappingInformationType: "ByPolygonVertex"\n ReferenceInformationType: "Direct"\n'+array('Normals',normals)+'\n}',
 'LayerElementUV: 0 { Version: 101\n Name: "CalibrationUV"\n MappingInformationType: "ByPolygonVertex"\n ReferenceInformationType: "IndexToDirect"\n'+array('UV',uv)+'\n'+array('UVIndex',uvindex)+'\n}',
 'LayerElementMaterial: 0 { Version: 101\n Name: ""\n MappingInformationType: "AllSame"\n ReferenceInformationType: "IndexToDirect"\n Materials: *1 { a: 0 }\n}',
 'Layer: 0 { Version: 100\n LayerElement: { Type: "LayerElementNormal"\n TypedIndex: 0 }\n LayerElement: { Type: "LayerElementUV"\n TypedIndex: 0 }\n LayerElement: { Type: "LayerElementMaterial"\n TypedIndex: 0 }\n}', '}',
 'Model: 101, "Model::CalibrationSkin", "Mesh" { Version: 232\n Properties70: { P: "Lcl Scaling", "Lcl Scaling", "", "A",1,1,1 }\n}',
 'Model: 102, "Model::FixtureRoot04OH", "Null" { Version: 232\n}',
 'Material: 103, "Material::DiagnosticCyan", "" { Version: 102\n ShadingModel: "lambert"\n Properties70: { P: "DiffuseColor", "Color", "", "A",0.12,0.65,0.72 }\n}',
 'Deformer: 104, "Deformer::CalibrationSkin", "Skin" { Version: 101\n Link_DeformAcuracy: 50\n}']
 for i,bone in enumerate(bones):
  lines+=['Model: '+str(1000+i)+', "Model::'+bone['name']+'", "LimbNode" { Version: 232\n Properties70: { P: "Lcl Translation", "Lcl Translation", "", "A",'+','.join(format(v,'.9g')for v in bone['local'])+'\n P: "Lcl Scaling", "Lcl Scaling", "", "A",1,1,1 }\n}',
   'NodeAttribute: '+str(2000+i)+', "NodeAttribute::'+bone['name']+'", "LimbNode" { TypeFlags: "Skeleton"\n}',
   'Deformer: '+str(3000+i)+', "SubDeformer::'+bone['name']+'", "Cluster" { Version: 100\n'+array('Indexes',[i*4+j for j in range(4)])+'\n'+array('Weights',[1,1,1,1])+'\n'+array('Transform',matrix([-v for v in bone['world']]))+'\n'+array('TransformLink',matrix(bone['world']))+'\n}']
 lines+=['Pose: 105, "Pose::BindPose", "BindPose" { Type: "BindPose"\n Version: 100\n NbPoseNodes: '+str(n+1)]
 for node,pos in [(101,[0,0,0])]+[(1000+i,b['world'])for i,b in enumerate(bones)]:lines+=['PoseNode: { Node: '+str(node)+'\n'+array('Matrix',matrix(pos))+'\n}']
 lines+=['}', 'AnimationStack: 5000, "AnimStack::CalibrationArmSweep", "" { Properties70: { P: "LocalStop", "KTime", "Time", "",46186158000 }\n}',
 'AnimationLayer: 5001, "AnimLayer::Base", "" { }',
 'AnimationCurveNode: 5002, "AnimCurveNode::R", "" { Properties70: { P: "d|X", "Number", "", "A",0\n P: "d|Y", "Number", "", "A",0\n P: "d|Z", "Number", "", "A",0 }\n}',
 'AnimationCurve: 5003, "AnimCurve::ArmZ", "" { Default: 0\n KeyVer: 4008\n'+array('KeyTime',[0,23093079000,46186158000])+'\n'+array('KeyValueFloat',[0,25,0])+'\n'+array('KeyAttrFlags',[24836]*3)+'\n'+array('KeyAttrDataFloat',[0]*12)+'\n'+array('KeyAttrRefCount',[1]*3)+'\n}',
 '}', 'Connections: { C: "OO",101,102\n C: "OO",102,0\n C: "OO",100,101\n C: "OO",103,101\n C: "OO",104,100']
 for i,bone in enumerate(bones):lines+=['C: "OO",'+str(1000+i)+','+str(102 if bone['parent']<0 else 1000+bone['parent']), 'C: "OO",'+str(2000+i)+','+str(1000+i),'C: "OO",'+str(3000+i)+',104','C: "OO",'+str(1000+i)+','+str(3000+i)]
 arm=next(i for i,b in enumerate(bones)if b['name']=='UpperArm.R')
 lines+=['C: "OO",5001,5000','C: "OO",5002,5001','C: "OP",5002,'+str(1000+arm)+',"Lcl Rotation"','C: "OP",5003,5002,"d|Z"','}', 'Takes: { Current: "CalibrationArmSweep"\n}']
 path.parent.mkdir(parents=True,exist_ok=True);path.write_text('\n'.join(lines)+'\n')
 manifest={'asset_id':'FIXTURE04OH','classification':'ORIGINAL_DIAGNOSTIC_NOT_PRODUCTION_HERO','creator':'Project-authored import calibration','license':'CC0-1.0','source':'tools/generate_import_fixture_04oh.py and existing RigLayout','export_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'source_sha256':{name:hashlib.sha256((Path(__file__).resolve().parents[1]/name).read_bytes()).hexdigest()for name in ['tools/generate_import_fixture_04oh.py','Assets/NeonSyndicate/Scripts/Runtime/Characters/RigLayout.cs','Assets/NeonSyndicate/Scripts/Runtime/Characters/Appearance.cs']},'bounds_min':[min(vertices[k::3])for k in range(3)],'bounds_max':[max(vertices[k::3])for k in range(3)],'material_slots':1,'texture_maps':[],'units':'metres','up_axis':'+Y','forward_axis':'+Z','bones':bones,'vertices':n*4,'triangles':n*4,'clips':['CalibrationArmSweep'],'production_candidate':False,'native_import':'NOT RUN','registry_enabled':False}
 path.with_suffix('.fixture.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps({k:v for k,v in manifest.items()if k!='bones'}))
if __name__=='__main__':main()
