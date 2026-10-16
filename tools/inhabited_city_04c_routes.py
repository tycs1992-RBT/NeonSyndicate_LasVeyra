"""Conservative room-route connectivity over actual exported navigation surfaces.

This is a numerical source graph, not the Unity player controller. Dynamic people,
vehicles and the existing runtime Forge prop are outside this static art export.
"""
from collections import deque
import numpy as np
from check_visual_rebirth_04a import visual_surface_at


def connections(expanded, collisions, anchors, required, occupied):
    navigation = [entry['triangles'] for entry in collisions if entry['id'] == 'interior_floor' or
                  entry['id'].startswith(('upper_floor_', 'upper_landing', 'stair_tread_'))]
    triangles = np.concatenate(navigation)
    ground = next(n for n in expanded['nodes'] if n['id'] == 'interior_floor')
    floor = ground['position'][1] + ground['scale'][1]*.5
    levels = [(floor, np.array([0, floor, 1.0]))]
    if expanded['twoStoreys']: levels.append((4.2, np.array([-5.7,4.2,14.3])))
    result=[]
    for height, start in levels:
        targets = {name: np.array(anchors[name]['position'], dtype=float) for name in required
                   if name != 'exit' and abs(anchors[name]['position'][1]-height) < .04}
        xs, zs = np.arange(-7.25,7.26,.5), np.arange(.75,17.26,.5)
        def clear(a,b=None):
            if b is None:b=a
            return not occupied(collisions,np.minimum(a,b)+[-.4,.15,-.4],np.maximum(a,b)+[.4,2.1,.4])
        nodes={}
        for i,x in enumerate(xs):
            for j,z in enumerate(zs):
                p=np.array([x,height,z])
                if clear(p) and visual_surface_at(triangles,x,height,z):nodes[(i,j)]=p
        nearby=sorted(nodes,key=lambda key:float(np.linalg.norm(nodes[key]-start)))
        first=next((key for key in nearby if np.linalg.norm(nodes[key]-start)<.8 and clear(nodes[key],start)),None)
        seen=set();edges={}
        if first is not None:
            queue=deque([first]);seen.add(first)
            while queue:
                key=queue.popleft()
                for dx,dz in ((-1,0),(1,0),(0,-1),(0,1)):
                    next_key=(key[0]+dx,key[1]+dz)
                    if next_key not in nodes or next_key in seen:continue
                    pair=tuple(sorted((key,next_key)))
                    if pair not in edges:edges[pair]=clear(nodes[key],nodes[next_key])
                    if edges[pair]:seen.add(next_key);queue.append(next_key)
        unreachable=[]
        for name,target in targets.items():
            # The final 0.8m connector is checked as a complete body envelope, not an unchecked graph snap.
            reachable=any(np.linalg.norm(nodes[key]-target)<.8 and clear(nodes[key],target) for key in seen)
            if not reachable:unreachable.append(name)
        result.append({'floor_y':height,'spacing_m':.5,'body_radius_m':.4,'headroom_m':2.1,
                       'clear_cells':len(nodes),'connected_cells':len(seen),'checked_targets':sorted(targets),
                       'unreachable_targets':unreachable,'entry_or_landing_connected':first is not None})
    return result
