"""Transactional Blender GLB/FBX production. Run with Blender -b -Y --python ... -- --brief <json>."""
import argparse,hashlib,json,math,re,shutil,struct,sys,tempfile,subprocess
from pathlib import Path
import bpy,bmesh
from mathutils import Vector,Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
from policy import collider_candidates
ROOT=Path(__file__).resolve().parents[2]
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def inspect_glb(path):
 raw=path.read_bytes()
 if len(raw)>128*1024*1024 or len(raw)<20 or raw[:4]!=b'glTF':raise ValueError('Invalid or oversized GLB.')
 magic,version,size=struct.unpack_from('<III',raw);length,kind=struct.unpack_from('<II',raw,12)
 if version!=2 or size!=len(raw)or kind!=0x4e4f534a or length>8*1024*1024:raise ValueError('Invalid GLB header.')
 data=json.loads(raw[20:20+length])
 for row in data.get('images',[])+data.get('buffers',[]):
  uri=row.get('uri')
  if uri and not uri.startswith('data:'):raise ValueError('GLB must embed resources. External or remote URI rejected.')
 return data
def bounds(objects):
 points=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
 if not points or not all(math.isfinite(x)and abs(x)<100000 for p in points for x in p):raise ValueError('Absent or nonfinite mesh vertices.')
 return [[min(p[i]for p in points)for i in range(3)],[max(p[i]for p in points)for i in range(3)]]
def inspect(objects,skinned):
 result={'meshes':len(objects),'vertices':0,'triangles':0,'degenerateFaces':0,'missingUV':0,'unweightedVertices':0,'nonUnitWeightVertices':0,'nonmanifoldEdges':0}
 for o in objects:
  m=o.data;result['vertices']+=len(m.vertices);result['triangles']+=sum(len(p.vertices)-2 for p in m.polygons);result['degenerateFaces']+=sum(p.area<1e-12 for p in m.polygons);result['missingUV']+=int(not m.uv_layers)
  bm=bmesh.new();bm.from_mesh(m);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);result['nonmanifoldEdges']+=sum(not e.is_manifold for e in bm.edges);bm.free()
  if skinned:
   for v in m.vertices:
    weights=[g.weight for g in v.groups if g.weight>1e-8];result['unweightedVertices']+=int(not weights);result['nonUnitWeightVertices']+=int(bool(weights)and(abs(sum(weights)-1)>.002 or len(weights)>4))
  if m.uv_layers and not all(math.isfinite(c)for uv in m.uv_layers[0].data for c in uv.uv):raise ValueError('Nonfinite UV.')
 result['boundsBlender']=bounds(objects)
 if result['degenerateFaces']or result['missingUV']or result['unweightedVertices']or result['nonUnitWeightVertices']or(skinned and result['nonmanifoldEdges']):raise ValueError('Mesh contract refused: '+str(result))
 return result
def bind_materials(objects,brief):
 materials={};receipts=[]
 for name,paths in brief['materials'].items():
  files={key:ROOT/value for key,value in paths.items()}
  if set(files)!={'baseColor','normal','metallicSmoothness'}or any(not p.is_file()for p in files.values()):raise ValueError('Incomplete PBR material family '+name)
  mat=bpy.data.materials.new(name);mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();shader=nodes.new('ShaderNodeBsdfPrincipled');out=nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(shader.outputs[0],out.inputs[0])
  for key,path in files.items():
   node=nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(path),check_existing=True)
   if key=='baseColor':mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
   elif key=='normal':
    node.image.colorspace_settings.name='Non-Color';normal=nodes.new('ShaderNodeNormalMap');mat.node_tree.links.new(node.outputs['Color'],normal.inputs['Color']);mat.node_tree.links.new(normal.outputs['Normal'],shader.inputs['Normal'])
   else:
    node.image.colorspace_settings.name='Non-Color';separate=nodes.new('ShaderNodeSeparateColor');mat.node_tree.links.new(node.outputs['Color'],separate.inputs['Color']);mat.node_tree.links.new(separate.outputs['Red'],shader.inputs['Metallic']);invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1;mat.node_tree.links.new(node.outputs['Alpha'],invert.inputs[1]);mat.node_tree.links.new(invert.outputs[0],shader.inputs['Roughness'])
   receipts.append({'material':name,'channel':key,'path':path.relative_to(ROOT).as_posix(),'sha256':sha(path)})
  materials[name]=mat
 for o in objects:
  for i,old in enumerate(o.data.materials):
   name=re.sub(r'\.\d{3}$','',old.name if old else '')
   if name not in materials:raise ValueError('Unknown semantic surface '+name)
   o.data.materials[i]=materials[name]
 return receipts
def main():
 p=argparse.ArgumentParser();p.add_argument('--brief',type=Path,required=True);p.add_argument('--replace-derived',action='store_true');p.add_argument('--inspector',type=Path,required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);brief=json.loads(a.brief.read_text());asset=brief['assetId'];source=(ROOT/brief['input']).resolve();out=(ROOT/brief['output']).resolve();editable=(ROOT/brief['editableSource']).resolve()
 if not source.is_relative_to(ROOT/'ArtSource/FoundryWR02A')or not out.is_relative_to(ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A')or not editable.is_relative_to(ROOT/'ArtSource/FoundryWR02A'):raise ValueError('Output/input outside authorized foundry directories.')
 receipt_path=out/'processed.json'
 if out.exists()and list(out.iterdir()):
  if not a.replace_derived or not receipt_path.exists():raise ValueError('Existing output retained. Explicit reviewed derived replacement required.')
  prior=json.loads(receipt_path.read_text())
  if prior['assetId']!=asset:raise ValueError('Different asset owns this output.')
  for row in prior['outputs']:
   if sha(ROOT/row['path'])!=row['sha256']:raise ValueError('Derived asset has unrelated local edits. Refuse overwriting.')
 license_id='CC0-1.0'
 if brief.get('sourceRights')!='ORIGINAL_PROJECT_CC0':
  rights=brief.get('sourceLicenseReview',{})
  if brief.get('sourceRights')!='OWNER_APPROVED_LICENSED_INPUT'or rights.get('ownerApproved')is not True or rights.get('sourceSha256')!=sha(source)or not rights.get('licenseId'):raise ValueError('Source rights need exact owner/license review before production.')
  license_id=rights['licenseId']
 if source.suffix.lower()=='.glb':inspect_glb(source)
 elif source.suffix.lower()!='.fbx':raise ValueError('Only GLB or FBX input supported.')
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.render.fps=30
 if source.suffix.lower()=='.glb':bpy.ops.import_scene.gltf(filepath=str(source))
 else:bpy.ops.import_scene.fbx(filepath=str(source),use_image_search=False,use_custom_props=True)
 arms=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];helpers={pb.custom_shape for arm in arms for pb in arm.pose.bones if pb.custom_shape is not None};objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and o not in helpers];skinned=brief['kind']=='creature'
 if not objects or(len(arms)!=1 if skinned else bool(arms)):raise ValueError('Asset kind and armature count disagree.')
 for arm in arms:
  if arm.animation_data:
   arm.animation_data.action=None
   for track in arm.animation_data.nla_tracks:track.mute=True
  for bone in arm.pose.bones:bone.matrix_basis=Matrix.Identity(4)
 bpy.context.view_layer.update()
 if any(o.type not in ('MESH','ARMATURE','EMPTY')for o in bpy.data.objects):raise ValueError('Input contains camera/light or unsupported scene authority.')
 if any(o.animation_data and o.animation_data.drivers for o in bpy.data.objects):raise ValueError('Animation driver rejected.')
 before=bounds(objects);scale=float(brief.get('uniformScale',1));pivot=brief.get('pivot','KEEP_AUTHORED_FRAME')
 if skinned and(scale!=1 or pivot!='KEEP_AUTHORED_FRAME'):raise ValueError('Automatic rig scaling/recentring rejected. Author a reviewed skeletal transform.')
 if not skinned:
  for o in objects:
   bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.select_set(False)
  offset=Vector((0,0,0))
  if pivot=='CENTER_BOTTOM':offset=Vector(((before[0][0]+before[1][0])/2,(before[0][1]+before[1][1])/2,before[0][2]))
  elif pivot!='KEEP_AUTHORED_FRAME':raise ValueError('Unknown pivot policy.')
  for o in objects:
   for v in o.data.vertices:v.co=(v.co-offset)*scale
 repairs=[]
 for o in objects:
  bm=bmesh.new();bm.from_mesh(o.data)
  if skinned:bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7)
  bad=[f for f in bm.faces if f.calc_area()<1e-12]
  if bad:
   if skinned:raise ValueError('Skinned degenerate topology rejected rather than altering weights silently.')
   bmesh.ops.delete(bm,geom=bad,context='FACES');repairs.append({'mesh':o.name,'removedDegenerateFaces':len(bad)})
  bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
  if not o.data.uv_layers:
   if skinned or not brief.get('allowStaticUVGeneration'):raise ValueError('Missing authored UV rejected.')
   bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False);repairs.append({'mesh':o.name,'generatedUV':'Smart projection, requires artist review'})
 textures=bind_materials(objects,brief);base=inspect(objects,skinned)
 if 'expectedUnityBounds'in brief:
  actual=[[v[0],v[2],v[1]]for v in base['boundsBlender']]
  if max(abs(actual[j][i]-brief['expectedUnityBounds'][j][i])for j in range(2)for i in range(3))>.03:raise ValueError('Asset does not fit its exact existing presentation contract.')
 levels=[objects];ratios=[1,.68,.38]
 for lod in [1,2]:
  copies=[]
  for original in objects:
   o=original.copy();o.data=original.data.copy();bpy.context.collection.objects.link(o);copies.append(o);o.name=original.name+'_LOD'+str(lod);bpy.context.view_layer.objects.active=o
   if skinned or not original.get('foundry_protect_silhouette',False):
    mod=o.modifiers.new('Reviewed LOD reduction','DECIMATE');mod.ratio=ratios[lod];mod.use_collapse_triangulate=True
    while list(o.modifiers).index(mod)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    if skinned:
     repaired=0
     for vertex in o.data.vertices:
      weights=sorted([(g.group,g.weight)for g in vertex.groups if g.weight>1e-8],key=lambda x:x[1],reverse=True)
      if weights and(len(weights)>4 or abs(sum(w for i,w in weights)-1)>.00001):
       repaired+=1;top=weights[:4];total=sum(w for i,w in top)
       for index,w in weights:o.vertex_groups[index].remove([vertex.index])
       for index,w in top:o.vertex_groups[index].add([vertex.index],w/total,'REPLACE')
     repairs.append({'mesh':o.name,'lod':lod,'verticesWeightsNormalizedOrPrunedToFour':repaired})
   o.hide_render=True
  info=inspect(copies,skinned)
  if max(abs(info['boundsBlender'][j][i]-base['boundsBlender'][j][i])for j in range(2)for i in range(3))>brief['maxLODBoundsDeviationMetres']:raise ValueError('LOD changes the accepted silhouette envelope. Artist revision required.')
  levels.append(copies)
 tempbase=ROOT/'.foundry-local/processing';tempbase.mkdir(parents=True,exist_ok=True);stage=Path(tempfile.mkdtemp(prefix=asset+'-',dir=tempbase));rows=[]
 try:
  for lod,meshes in enumerate(levels):
   bpy.ops.object.select_all(action='DESELECT')
   for o in meshes+arms:o.select_set(True)
   target=stage/(asset+'_LOD'+str(lod)+'.fbx');bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=not skinned,add_leaf_bones=False,use_tspace=True,bake_anim=skinned,bake_anim_use_all_actions=skinned,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP',use_custom_props=False)
   verified=subprocess.run([str(a.inspector.resolve()),str(target)],capture_output=True,text=True)
   if verified.returncode!=0:raise ValueError('Independent FBX inspection refused staged output; original derived files retained. '+verified.stderr[:300])
   inspection=json.loads(verified.stdout)
   if inspection.get('passed')is not True:raise ValueError('Independent inspector did not certify the staged payload.')
   row=inspect(meshes,skinned);row['independentInspection']=inspection;row.update(path=(out/target.name).relative_to(ROOT).as_posix(),sha256=sha(target),bytes=target.stat().st_size,lod=lod);rows.append(row)
  # No automatic convex hull: only explicit, reviewed semantic boxes can become passive collider candidates.
  declared_bounds=[[v[0],v[2],v[1]]for v in base['boundsBlender']];boxes=collider_candidates(brief,declared_bounds)
  collider={'policy':brief['colliderPolicy'],'nativeAccepted':False,'unityCollidersCreated':0,'candidateBoxes':boxes,'reason':'Existing authoritative physics retained. Candidates are declarations only until native correspondence acceptance.'}
  if skinned:collider['originalHitVolumes']=json.loads((ROOT/brief['creatureContract']).read_text())['hits']
  (stage/'colliders.json').write_text(json.dumps(collider,indent=2)+'\n')
  for im in bpy.data.images:
   if im.source=='FILE':im.pack()
  bpy.context.scene.render.fps=30;processed=stage/'processed.blend';bpy.ops.wm.save_as_mainfile(filepath=str(processed),compress=True)
  data={'schemaVersion':1,'assetId':asset,'kind':brief['kind'],'method':'Local original Blender source, processed through the same GLB intake used for optional remote output','input':brief['input'],'input_sha256':sha(source),'brief_sha256':sha(a.brief),'processor_sha256':sha(__file__),'license':license_id,'pivot':pivot,'uniformScale':scale,'beforeBoundsBlender':before,'repairs':repairs,'outputs':rows,'editableSource':editable.relative_to(ROOT).as_posix(),'editable_sha256':sha(processed),'textures':textures,'colliderManifest':(out/'colliders.json').relative_to(ROOT).as_posix(),'actions':[x.name for x in bpy.data.actions],'nativeUnity':'NOT RUN','candidateOnly':skinned}
  (stage/'processed.json').write_text(json.dumps(data,indent=2)+'\n');out.mkdir(parents=True,exist_ok=True);editable.parent.mkdir(parents=True,exist_ok=True)
  for name in [Path(x['path']).name for x in rows]+['colliders.json','processed.json']:shutil.move(stage/name,out/name)
  shutil.move(processed,editable);print(json.dumps({'assetId':asset,'lodTriangles':[x['triangles']for x in rows],'source':str(editable),'candidateOnly':skinned}),flush=True)
 finally:shutil.rmtree(stage)
if __name__=='__main__':main()
