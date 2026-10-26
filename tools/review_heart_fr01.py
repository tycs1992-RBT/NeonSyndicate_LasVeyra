"""Reopen editable source, validate actual FBX reimports and render labeled DCC route compositions."""
import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Assets/NeonSyndicate/Art/HeartFR01';SRC=ROOT/'ArtSource/HeartFR01';DOC=ROOT/'Docs/FantasyRenaissance/FR01';m=json.loads((ART/'manifest.json').read_text());plan=json.loads((DOC/'source-plan.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(SRC/'GothicHeartFR01.blend'));source={o.name:o for o in bpy.data.objects if o.type=='MESH'}
for mat in bpy.data.materials:
 if mat.name not in ['Masonry','CarvedTrim','SlateTimber','MossRoot','AmethystBronze']:continue
 nodes=mat.node_tree.nodes;links=mat.node_tree.links;p=nodes.get('Principled BSDF')
 for ch in ['BaseColor','Normal','MetallicSmoothness']:
  image=bpy.data.images.load(str(ART/(mat.name+'_'+ch+'.png')),check_existing=True);image.filepath=bpy.path.relpath(image.filepath,start=str(SRC));tex=nodes.new('ShaderNodeTexImage');tex.image=image
  if ch=='BaseColor':links.new(tex.outputs['Color'],p.inputs['Base Color'])
  elif ch=='Normal':image.colorspace_settings.name='Non-Color';normal=nodes.new('ShaderNodeNormalMap');links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],p.inputs['Normal'])
  else:
   image.colorspace_settings.name='Non-Color';separate=nodes.new('ShaderNodeSeparateColor');links.new(tex.outputs['Color'],separate.inputs['Color']);links.new(separate.outputs['Red'],p.inputs['Metallic']);invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1;links.new(tex.outputs['Alpha'],invert.inputs[1]);links.new(invert.outputs[0],p.inputs['Roughness'])
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'GothicHeartFR01.blend'))
checks=[]
for row in m['modules']:
 prior=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(ROOT/row['path']),use_anim=False);objects=[o for o in bpy.data.objects if o not in prior];meshes=[o for o in objects if o.type=='MESH'];v=[o.matrix_world@x.co for o in meshes for x in o.data.vertices];bounds=[[min(p[i]for p in v)for i in range(3)],[max(p[i]for p in v)for i in range(3)]]
 ok=len(meshes)>0 and all(len(o.data.uv_layers)>0 and len(o.data.vertices)>0 for o in meshes) and max(abs(bounds[j][i]-row['source_bounds'][j][i])for j in range(2)for i in range(3))<.015
 checks.append({'model':row['name'],'lod':row['lod'],'passed':ok,'imported_bounds':bounds,'source_bounds':row['source_bounds']})
 for o in objects:bpy.data.objects.remove(o,do_unlink=True)
(DOC/'dcc-import-validation.json').write_text(json.dumps({'scope':'Actual Blender 4.5.3 FBX reimports, not Unity','passed':sum(x['passed']for x in checks),'total':len(checks),'checks':checks,'native_unity':'NOT RUN'},indent=2)+'\n')
# Before uses unchanged H3/H5 source mesh placements; it is a DCC composition, not a Unity screenshot.
for o in list(bpy.data.objects):o.hide_render=True;o.hide_set(True)
old=[];new=[];shared=[];cache={}
mat=bpy.data.materials.get('Masonry')
def objmesh(path):
 if path in cache:return cache[path]
 vs=[];fs=[]
 for line in path.read_text().splitlines():
  if line.startswith('v '):x,y,z=map(float,line.split()[1:4]);vs.append((x,-z,y))
  elif line.startswith('f '):fs.append(tuple(int(x.split('/')[0])-1 for x in line.split()[1:]))
 data=bpy.data.meshes.new(path.stem);data.from_pydata(vs,[],fs);data.materials.append(bpy.data.materials.get('SlateTimber')if 'roof'in path.stem else mat);cache[path]=data;return data
def place(data,name,position,yaw=0,scale=(1,1,1)):
 o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.location=(position[0],-position[2],position[1]);o.rotation_euler[2]=math.radians(yaw);o.scale=(scale[0],scale[2],scale[1]);return o
for r in plan['art_parts']:
 p=ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/Models'/(r['model']+'.obj')
 if p.exists():shared.append(place(objmesh(p),r['owner']+' '+r['model'],r['position'],r['yaw'],r['scale']))
for r in plan['buildings']:
 if r['seed']not in plan['eligible_buildings']:continue
 x0,z0,x1,z1=r['rect'];id='HeartHouseA'if r['seed']%2==0 else'HeartHouseB';o=source[id+'_LOD0'];size=[max(v.co[i]for v in o.data.vertices)-min(v.co[i]for v in o.data.vertices)for i in range(3)]
 new.append(place(o.data,id,[(x0+x1)/2,.15,(z0+z1)/2],0,[(x1-x0)/size[0],r['height']/size[2],(z1-z0)/size[1]]))
 # This block is only an explicitly approximate legacy silhouette; it is not exported as accepted art.
 bpy.ops.mesh.primitive_cube_add(size=1,location=((x0+x1)/2,-(z0+z1)/2,r['height']/2));b=bpy.context.object;b.scale=(x1-x0,z1-z0,r['height']);b.data.materials.append(mat);old.append(b)
gate=plan['gate'];assert gate['ready'],'No source-admitted gate; do not invent one.'
new.append(place(source['HeartGate_LOD0'].data,'DCC gate',gate['position'],gate['yaw'],(11/9,1,1)))
for r in plan['routes']:
 if r['id']not in ['home-court','court-crossing','forge-market','sanctuary-approach']:continue
 for a,b in zip(r['points'],r['points'][1:]):
  d=Vector(b)-Vector(a);length=math.hypot(d.x,d.z);n=math.ceil(length/4)
  for j in range(n):p=Vector(a)+d*((j+.5)/n);p.y+=.021;new.append(place(source['HeartStreet_LOD0'].data,'DCC processional paving',p,math.degrees(math.atan2(d.x,d.z)),(r['width']/4,1,length/n/4)))
for sign in [-1,1]:
 for i in range(3):
  p=[plan['court'][0]+sign*(34+i*3),.15,plan['court'][2]+(i-1)*20];new.append(place(source[('HeartRootCourt'if i%2==0 else'HeartIvyRuin')+'_LOD0'].data,'DCC proposed side growth',p,sign*20))
# Existing H8R editable geometry is reused in source review without duplicating delivered files.
with bpy.data.libraries.load(str(ROOT/'ArtSource/WorldFoundationH8R/WorldFoundationH8R.blend'),link=False)as (a,b):b.objects=[n for n in a.objects if 'Shardwood'in n]
for o in b.objects:
 if o is not None:bpy.context.collection.objects.link(o);o.location=(-38,-54,.15);o.hide_render=False;shared.append(o)
bpy.ops.mesh.primitive_plane_add(size=450);floor=bpy.context.object;floor.data.materials.append(mat);shared.append(floor)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=20;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=760;scene.render.resolution_percentage=100
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.35,.42,.52,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.7
bpy.ops.object.light_add(type='SUN',location=(0,0,100));sun=bpy.context.object;sun.rotation_euler=(.45,-.35,-.7);sun.data.energy=2
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.lens=26;cam.data.clip_end=1000
bpy.ops.object.text_add();label=bpy.context.object;label.parent=cam;label.location=(-.64,.37,-1.2);label.rotation_euler=(0,0,0);label.data.size=.022;label.data.body='DCC SOURCE REVIEW / NOT UNITY';labelmat=bpy.data.materials.new('Evidence label');labelmat.use_nodes=True;nodes=labelmat.node_tree.nodes;nodes.clear();em=nodes.new('ShaderNodeEmission');em.inputs['Color'].default_value=(1,.78,.34,1);out=nodes.new('ShaderNodeOutputMaterial');labelmat.node_tree.links.new(em.outputs[0],out.inputs[0]);label.data.materials.append(labelmat)
views=[('Approach_Before',(0,-28,2.1),(0,29,13),False,False),('Approach_After',(0,-28,2.1),(0,29,13),True,False),('Gateway_After',(0,2,2.1),(0,29,14),True,False),('Court_After',(1,45,2.2),(-6,122,14),True,False),('Sanctuary_After',(12,84,2.2),(-6,138,14),True,False),('Evening_After',(0,-28,2.1),(0,29,13),True,True)]
for name,eye,target,on,night in views:
 for o in new:o.hide_render=not on
 for o in old:o.hide_render=on
 cam.location=(eye[0],-eye[1],eye[2]);aim=Vector((target[0],-target[1],target[2]));cam.rotation_euler=(aim-cam.location).to_track_quat('-Z','Y').to_euler();sun.data.energy=.3 if night else 3;scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.1 if night else .7
 scene.render.filepath=str(DOC/(name+'.png'));bpy.ops.render.render(write_still=True)
(DOC/'dcc-views.json').write_text(json.dumps({'scope':'Blender DCC compositions from source plan. Proposed side dressing is illustrative until native safety probes. Legacy nonservice blocks are a palette/silhouette approximation. Not Unity screenshots or acceptance.','views':views,'native_unity':'NOT RUN'},indent=2)+'\n')
