#!/usr/bin/env python3
"""Render actual old WildsShapes exports and delivered source variants at matched bounds and camera. Not Unity."""
from pathlib import Path
import bpy,json,math
from mathutils import Vector
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1];out=root/'Docs/LiveWildL2';bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/RedspireL2/RedspireWildernessL2.blend'),load_ui=False,use_scripts=False);scene=bpy.context.scene;originals={o.name:o for o in bpy.data.objects if o.type=='MESH'}
for o in originals.values():o.hide_render=True
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.cycles.max_bounces=4;scene.render.threads_mode='FIXED';scene.render.threads=2;scene.render.resolution_x=768;scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX';scene.world=bpy.data.worlds.new('Neutral source comparison');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.24,.27,.31,1);scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.location.z=-.25;mat=bpy.data.materials.new('Source stage neutral ground');mat.diffuse_color=(.19,.17,.14,1);floor.data.materials.append(mat)
for name,pos,power,size in [('day key',(-6,-9,12),1800,8),('sky fill',(7,4,10),1200,10)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,2))-o.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Matched source camera');camera=bpy.data.objects.new('Matched source camera',data);scene.collection.objects.link(camera);scene.camera=camera;data.type='ORTHO';records=[];panels=[]
def before(name):
 verts=[];faces=[];slots=[];slot=0
 for line in (out/'SourceGeometry'/('Before_'+name+'.obj')).read_text().splitlines():
  w=line.split()
  if not w:continue
  if w[0]=='v':verts.append(tuple(float(x)for x in w[1:4]))
  elif w[0]=='g':slot=int(w[1][3:])
  elif w[0]=='f':faces.append(tuple(int(x)-1 for x in w[1:]));slots.append(slot)
 mesh=bpy.data.meshes.new('Actual prior '+name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(mesh.name,mesh);scene.collection.objects.link(o)
 colors={0:(.48,.24,.14,1),1:(.72,.61,.44,1),2:(.15,.13,.12,1),7:(.23,.18,.13,1)}
 for index in range(30):
  m=bpy.data.materials.new('Prior source slot '+str(index));m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=colors.get(index,(.4,.35,.3,1));p.inputs['Roughness'].default_value=.85;mesh.materials.append(m)
 for face,slot in zip(mesh.polygons,slots):face.material_index=slot
 return o
for kind in ['Boulder','RockSpire','DeadTree']:
 legacy=[before(kind+'_'+v)for v in ['A','B']];authored=[];width=0;height=0
 for i,v in enumerate(['A','B']):
  old=legacy[i];new=originals[kind+'_'+v].copy();new.data=originals[kind+'_'+v].data.copy();scene.collection.objects.link(new);new.hide_render=True;authored.append(new)
  oldmin=Vector(tuple(min(p.co[k]for p in old.data.vertices)for k in range(3)));oldmax=Vector(tuple(max(p.co[k]for p in old.data.vertices)for k in range(3)));oldsize=oldmax-oldmin;lo=Vector(tuple(min(p.co[k]for p in new.data.vertices)for k in range(3)));hi=Vector(tuple(max(p.co[k]for p in new.data.vertices)for k in range(3)));size=hi-lo
  for p in new.data.vertices:p.co=oldmin+Vector(tuple((p.co[k]-lo[k])/size[k]*oldsize[k]for k in range(3)))
  width=max(width,oldsize.x);height=max(height,oldsize.z);old.hide_render=True
 offset=width*.72
 for i in range(2):
  shift=(-offset if i==0 else offset);legacy[i].location.x=shift;authored[i].location.x=shift
 target=Vector((0,0,height*.48));camera.location=target+Vector((7,-12,7));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=max(width*3.5,height*1.55)
 for label,objects in [('BEFORE',legacy),('AFTER',authored)]:
  for o in objects:o.hide_render=False
  file=out/(kind+'_'+label+'.png');scene.render.filepath=str(file);bpy.ops.render.render(write_still=True)
  for o in objects:o.hide_render=True
  with Image.open(file)as im:im.load();panel=im.convert('RGB')
  draw=ImageDraw.Draw(panel);draw.rectangle((0,0,768,77),fill=(20,24,28));draw.text((18,12),kind+' | '+label+' | variants A and B',fill='white',font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',22));draw.text((18,45),'SOURCE 3D RENDER. Same bounds/camera/light. Unity NOT RUN.',fill=(235,205,130),font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',15));panel.save(file);panels.append(panel)
  records.append({'kind':kind,'state':label,'file':file.name,'camera':list(camera.location),'target':list(target),'ortho_scale':data.ortho_scale,'variants':['A','B'],'source_geometry':'actual unchanged WildsShapes C# OBJ export'if label=='BEFORE'else'actual delivered editable Blender source, fitted to the same prior bounds'})
 for o in legacy+authored:bpy.data.objects.remove(o,do_unlink=True)
canvas=Image.new('RGB',(1536,1800))
for i,panel in enumerate(panels):canvas.paste(panel,((i%2)*768,(i//2)*600))
canvas.save(out/'Redspire_Source_Comparison.png');(out/'render-manifest.json').write_text(json.dumps({'scope':__doc__,'renderer':'Blender Cycles CPU, 16 samples, neutral daylight','native_unity':'NOT RUN','resolution_per_panel':[768,600],'notes':'Before shows actual previous geometry with neutral palette approximations, not Unity procedural shader reproduction. After shows delivered original maps. No gameplay, FPS or native collision acceptance implied.','views':records},indent=2)+'\n');print('Rendered labeled matched source comparison',flush=True)
