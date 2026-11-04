#!/usr/bin/env python3
"""Fit short strand grooming directly to the owner-selected facial surface. No face replacement."""
import bpy,random,math,bisect,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parents[1];path=root/'ArtSource/AscensionH8R/MalikVeyrH8R.blend';bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False);body=bpy.data.objects['MalikBodyH8R'];rig=bpy.data.objects['MalikRig']
for o in list(bpy.data.objects):
 if o.type=='MESH'and o.name.startswith(('H8R Fitted short hair','H8R Short parted hair','H8R Groomed beard','H8R Surface hair','H8R Surface beard')):bpy.data.objects.remove(o,do_unlink=True)
points=[v.co.copy()for v in body.data.vertices];surface=BVHTree.FromPolygons(points,[list(f.vertices)for f in body.data.polygons]);tris=[]
for f in body.data.polygons:
 pp=[points[i]for i in f.vertices]
 for j in range(1,len(pp)-1):tris.append((pp[0],pp[j],pp[j+1]))
material=bpy.data.materials.new('H6 Short groom');material.use_nodes=True;bs=material.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.006,.0035,.0025,1);bs.inputs['Roughness'].default_value=.87;bs.inputs['Specular IOR Level'].default_value=.12
rows=[]
for kind,count in [('hair',4500),('beard',6500)]:
 selected=[];areas=[];total=0;rng=random.Random(40880 if kind=='hair'else 40881)
 for tri in tris:
  c=sum(tri,Vector())/3
  eligible=(c.z>1.80 or c.z>1.735 and c.y>-.025)and abs(c.x)<.10 if kind=='hair'else 1.638<c.z<1.737 and abs(c.x)<.073 and c.y<-.060 and not(abs(c.x)<.027 and 1.666<c.z<1.701)
  if eligible:
   area=(tri[1]-tri[0]).cross(tri[2]-tri[0]).length*.5
   if area>1e-10:total+=area;areas.append(total);selected.append(tri)
 vs=[];faces=[];levels=5 if kind=='hair'else 2
 for unused in range(count):
  aa,bb,cc=selected[bisect.bisect_left(areas,rng.random()*total)];u=rng.random();v=rng.random()
  if u+v>1:u=1-u;v=1-v
  anchor=aa+(bb-aa)*u+(cc-aa)*v;normal=(bb-aa).cross(cc-aa).normalized()
  if normal.dot(anchor-Vector((0,0,1.74)))<0:normal=-normal
  if kind=='hair'and anchor.y<-.065 and anchor.z<1.804+.004*math.sin(anchor.x*22):continue
  length=(.010+.007*rng.random())if kind=='hair'else .0014+.0014*rng.random();flow=Vector((-.1,1,-.1))if kind=='hair'else Vector((0,.15,-1));flow=(flow-normal*flow.dot(normal)).normalized();across=normal.cross(flow).normalized();width=.00028 if kind=='hair'else .00012;start=len(vs)
  for j in range(levels+1):
   t=j/levels;point,n,idx,d=surface.find_nearest(anchor+flow*length*t)
   if point is None:point=anchor;n=normal
   center=point+n*(.00028+(math.sin(math.pi*t)*.0018 if kind=='hair'else .0003*t));w=width*(1-t*.90);vs.extend([tuple(center-across*w),tuple(center+across*w)])
  for j in range(levels):k=start+2*j;faces.append((k,k+1,k+3,k+2))
 data=bpy.data.meshes.new('Selected surface '+kind);data.from_pydata(vs,[],faces);data.update();o=bpy.data.objects.new('H8R Surface '+kind,data);bpy.context.collection.objects.link(o);o.data.materials.append(material);o.parent=rig;g=o.vertex_groups.new(name='Head');g.add(list(range(len(vs))),1,'REPLACE');m=o.modifiers.new('Existing head skin','ARMATURE');m.object=rig;o['h7_strand_vertices']=2*(levels+1)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.005);bpy.ops.object.mode_set(mode='OBJECT');rows.append({'kind':kind,'vertices':len(vs),'strands':len(vs)//(2*(levels+1))})
for i in range(3):bpy.ops.outliner.orphans_purge(do_recursive=True)
material.name='H6 Short groom';bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True,relative_remap=False)
(root/'ArtSource/AscensionH8R/groom-fit.json').write_text(json.dumps({'selected':'Original strand grooming fitted to selected actual face','parts':rows,'rejected_donor_groom':'Technically compatible CC0 parts read as a solid stylized beard in actual DCC review, not retained on Malik','license':'CC0-1.0','native_unity':'NOT RUN','owner_approval':'NOT GIVEN'},indent=2)+'\n')
print(json.dumps(rows),flush=True)
