"""Matched complete-context DCC source views and actual FBX/structure checks. Never Unity evidence."""
from pathlib import Path
# Reuse the corrected retained CMH1/context decoder, its original gate probes and FR01E assembly.
# It consumes --context, --baseline-root and --output exactly as before.
recipe=Path(__file__).with_name('review_heart_fr01e.py').read_text().split('# Neutral reference ground')[0]
exec(compile(recipe,str(Path(__file__).with_name('review_heart_fr01e.py')),'exec'))
from mathutils.bvhtree import BVHTree
new_manifest=json.loads((ROOT/'Assets/NeonSyndicate/Art/HeartFR02A/manifest.json').read_text())
with bpy.data.libraries.load(str(ROOT/'ArtSource/HeartFR02A/LivingGothicStreets.blend'),link=False)as (available,requested):
 requested.objects=[n for n in available.objects if n.startswith(('Street','Court'))]
kit={o.name.split('.')[0]:o for o in requested.objects if o is not None}
new_checks=[];support=[]
specs=[(7,9,7),(8,10,11.4),(9,10,7.3),(11,10,5.6),(9,10,8),(14,8,7.6),(8,10,5.2),(8,10,8.4),(11,10,8)]
families=['StreetTownhouse','StreetResidence','StreetMerchant','StreetWorkshop','StreetGuild','StreetCourtRow','StreetRuin','StreetCorner','StreetGallery']
for row in new_manifest['modules']:
 prior=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(ROOT/row['path']),use_anim=False);objects=[o for o in bpy.data.objects if o not in prior];meshes=[o for o in objects if o.type=='MESH'];vs=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 bounds=[[min(v[i]for v in vs)for i in range(3)],[max(v[i]for v in vs)for i in range(3)]]
 ok=bool(meshes) and all(o.data.uv_layers and o.data.vertices for o in meshes) and max(abs(bounds[j][i]-row['source_bounds'][j][i])for j in range(2)for i in range(3))<.015
 uv_ok=all(all(math.isfinite(v)for v in loop.uv)for o in meshes for layer in o.data.uv_layers for loop in layer.data)
 new_checks.append({'path':row['path'],'sha256':hashlib.sha256((ROOT/row['path']).read_bytes()).hexdigest(),'passed':bool(ok and uv_ok),'bounds':bounds,'triangles':sum(len(o.data.polygons)for o in meshes)})
 if row['name']in families:
  vertices=[];faces=[]
  for o in meshes:
   start=len(vertices);vertices.extend(o.matrix_world@v.co for v in o.data.vertices);faces.extend(tuple(i+start for i in f.vertices)for f in o.data.polygons)
  tree=BVHTree.FromPolygons(vertices,faces,all_triangles=True);w,d,h=specs[families.index(row['name'])];hits=[]
  for fraction in [.1,.35,.65,.9]:
   hit=tree.ray_cast(Vector((-w/2-2,0,h*fraction)),Vector((1,0,0)),3)
   hits.append(hit[0]is not None and hit[3]<2.7)
  support.append({'name':row['name'],'lod':row['lod'],'side_wall_samples':hits,'passed':all(hits)})
 for o in objects:bpy.data.objects.remove(o,do_unlink=True)
assert all(r['passed']for r in new_checks),'New FBX reimport/UV mismatch'
assert all(r['passed']for r in support),'New building has a wall gap'
(a.output/'dcc-import-fr02a.json').write_text(json.dumps({'scope':'Actual Blender FBX reimports and UV checks, not Unity','passed':sum(r['passed']for r in new_checks),'total':len(new_checks),'rows':new_checks,'native_unity':'NOT RUN'},indent=2)+'\n')
(a.output/'structure-fr02a.json').write_text(json.dumps({'scope':'BVH rays through four side-wall heights in all nine families and all three actual reimported LODs','passed':sum(r['passed']for r in support),'total':len(support),'rows':support,'native_unity':'NOT RUN'},indent=2)+'\n')
before=list(new);after=list(new);ledger=[];installed=[];houses=0;props=0
for row in plan['living']:
 ok=row['source_clear_alone'];reason=row['alone_reason'];area=row['area']
 if ok and any(area[0]<other['area'][2] and area[2]>other['area'][0] and area[1]<other['area'][3] and area[3]>other['area'][1]for other in installed):ok=False;reason='Another static-source-admitted module.'
 if ok and (houses>=18 if row['house']else props>=18):ok=False;reason='Bounded module limit.'
 if ok and solid(np.asarray(row['position'])+[0,row['size'][1]*.5+.15,0],[row['size'][0]*.5,row['size'][1]*.5-.1,row['size'][2]*.5],row['yaw']):ok=False;reason='Retained H3/H5 low-mesh or structural triangles.'
 ledger.append({**row,'static_source_admitted':bool(ok),'static_reason':reason})
 if not ok:continue
 original=kit[row['module']+'_LOD0'];coords=np.array([v.co[:]for v in original.data.vertices]);lo=coords.min(axis=0);hi=coords.max(axis=0);mid=(lo+hi)*.5;offset=np.array([mid[0],lo[2],-mid[1]]);at=np.asarray(row['position'])-world(offset,[0,0,0],row['yaw'],[1,1,1])
 after.append(place(original.data,'FR02A source / '+row['key'],at,row['yaw']));installed.append(row)
 if row['house']:houses+=1
 else:props+=1
(a.output/'dcc-static-fr02a.json').write_text(json.dumps({'scope':'Source geometry and reservation simulation. Original managed source admission plus retained CMH1/structure triangle probes. Does not simulate native ground raycasts, other owners, actors, triggers, camera or gameplay. Not runtime counts.','source_houses':plan['source_houses'],'source_props':plan['source_props'],'static_source_houses':houses,'static_source_props':props,'ledger':ledger,'native_unity':'NOT RUN'},indent=2)+'\n')
# Original geometry stays in both frames. Only the reversible visual groups differ.
for o in bpy.data.objects:o.hide_render=True
bpy.ops.mesh.primitive_plane_add(size=1500);floor=bpy.context.object;floor.data.materials.append(bpy.data.materials['Masonry'])
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=True;scene.render.resolution_x=1000;scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.35,.42,.52,1)
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.rotation_euler=(.45,-.35,-.7)
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.lens=24;cam.data.clip_end=1500
bpy.ops.object.text_add();label=bpy.context.object;label.parent=cam;label.location=(-.72,.42,-1.2);label.data.size=.022;label.data.body='DCC SOURCE REVIEW / NOT UNITY'
mat=bpy.data.materials.new('FR02A evidence label');mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();em=nodes.new('ShaderNodeEmission');em.inputs['Color'].default_value=(1,.8,.3,1);out=nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(em.outputs[0],out.inputs[0]);label.data.materials.append(mat)
views=[('Home',(-8,-18,1.8),(0,60,7),False),('Gateway',(12,55,1.8),(12,92,6),False),('Street',(30,63,1.8),(75,70,7),False),('Residential',(-24,-24,1.8),(-50,-64,7),False),('Market',(36,60,1.8),(55,75,3),False),('Archive',(15,95,1.8),(-6,145,14),False),('Skyline',(0,-100,45),(0,60,8),False),('Evening',(-24,-24,1.8),(-50,-64,7),True)]
for name,eye,target,night in views:
 for mode,active in [('Before',before),('After',after)]:
  for o in set(before+after+legacy+old):o.hide_render=True
  for o in active+common:o.hide_render=False
  floor.hide_render=False;label.hide_render=False;sun.hide_render=False
  cam.location=(eye[0],-eye[1],eye[2]);aim=Vector((target[0],-target[1],target[2]));cam.rotation_euler=(aim-cam.location).to_track_quat('-Z','Y').to_euler();sun.data.energy=.3 if night else 3;scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.1 if night else .7
  scene.render.filepath=str(a.output/(name+'_'+mode+'_DCC.png'));bpy.ops.render.render(write_still=True)
(a.output/'dcc-views-fr02a.json').write_text(json.dumps({'scope':'Matched FR01E versus proposed FR02A DCC source compositions. Real retained CMH1 geometry, approximate five unrefined service envelopes. No native people, shader/post, ground raycast, trigger or performance simulation.','retained_placements':len(plan['art_parts']),'retained_geometry_files':len(plan['geometry']),'approximate_services':approximate_services,'views':views,'static_source_houses':houses,'static_source_props':props,'runtime_installed':'NOT RUN','native_unity':'NOT RUN'},indent=2)+'\n')
