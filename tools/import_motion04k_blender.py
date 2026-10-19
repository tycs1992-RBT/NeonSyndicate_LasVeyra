"""Blender: --background --python tools/import_motion04k_blender.py -- skin.json output.blend

Builds an editable armature and the original palette-colored, weighted mesh. This
is a DCC review/export bridge; FBX import into Unity still requires native validation.
"""
import json, sys
from pathlib import Path
import bpy
from mathutils import Vector

def main():
    args=sys.argv[sys.argv.index('--')+1:];source,out=map(Path,args)
    d=json.loads(source.read_text());assert d['format']==1
    # Unity Y-up/+Z-forward to Blender Z-up/-Y-forward, with a proper rotation (no winding inversion).
    def point(v):return Vector((v[0],-v[2],v[1]))
    mesh=bpy.data.meshes.new(source.stem);mesh.from_pydata([point(v) for v in d['vertices']],[],[d['triangles'][i:i+3] for i in range(0,len(d['triangles']),3)]);mesh.update()
    obj=bpy.data.objects.new(source.stem,mesh);bpy.context.collection.objects.link(obj)
    for name,values in [('Palette',d['uv']),('SurfaceDetail',d['detail'])]:
        uv=mesh.uv_layers.new(name=name)
        for loop in mesh.loops:uv.data[loop.index].uv=values[loop.vertex_index]
    colors=mesh.color_attributes.new(name='PaletteColor',type='FLOAT_COLOR',domain='POINT')
    for i,c in enumerate(d['colors']):colors.data[i].color=c
    mat=bpy.data.materials.new('Original palette');mat.use_nodes=True
    nodes=mat.node_tree.nodes;attribute=nodes.new('ShaderNodeVertexColor');attribute.layer_name='PaletteColor'
    mat.node_tree.links.new(attribute.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color']);obj.data.materials.append(mat)
    rig=bpy.data.armatures.new(source.stem+' rig');arm=bpy.data.objects.new(source.stem+' rig',rig);bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    bones=[]
    for i,name in enumerate(d['names']):
        bone=rig.edit_bones.new(name);bone.head=point(d['world'][i]);bone.tail=bone.head+Vector((0,0,.045))
        if d['parents'][i]>=0:bone.parent=bones[d['parents'][i]]
        bones.append(bone)
    bpy.ops.object.mode_set(mode='OBJECT')
    groups=[obj.vertex_groups.new(name=n) for n in d['names']]
    for i,(joints,weights) in enumerate(zip(d['joints'],d['weights'])):
        for joint,weight in zip(joints,weights):
            if weight>0:groups[joint].add([i],weight,'REPLACE')
    modifier=obj.modifiers.new('Original skin','ARMATURE');modifier.object=arm;obj.parent=arm
    bpy.ops.wm.save_as_mainfile(filepath=str(out.resolve()))

if __name__=='__main__':main()
