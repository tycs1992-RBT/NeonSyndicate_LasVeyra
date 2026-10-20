import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
from pathlib import Path
import numpy as np,json
import argparse
a=argparse.ArgumentParser(description='Render actual exported meshes for offline technical review, not Unity screenshots.')
a.add_argument('--input',type=Path,required=True);a.add_argument('--output',type=Path,required=True)
x=a.parse_args();src=x.input;out=x.output;out.mkdir(parents=True,exist_ok=True)
names=['Civic mineral hall','Terraced residence','Forge cathedral','Canal compound','Signal ruin','Tower garden','Fringe remains','Roof reliquary']
fig=plt.figure(figsize=(16,9),facecolor='#111724')
for i in range(1,9):
 v=[];faces=[]
 for line in (src/f'family-{i}-near.obj').read_text().splitlines():
  if line.startswith('v '):v.append([float(t) for t in line.split()[1:4]])
  if line.startswith('f '):faces.append([int(t)-1 for t in line.split()[1:4]])
 v=np.array(v);ax=fig.add_subplot(2,4,i,projection='3d',facecolor='#111724')
 verts=[v[f][:,[0,2,1]] for f in faces]
 colors=[]
 for face in verts:
  n=np.cross(face[1]-face[0],face[2]-face[0]);n=n/(np.linalg.norm(n)+1e-6)
  light=.38+.45*abs(np.dot(n,np.array([.4,-.4,.8])))
  colors.append((light*.7,light*.83,light))
 mesh=Poly3DCollection(verts,facecolors=colors,edgecolors=(.10,.15,.23,.16),linewidths=.12)
 ax.add_collection3d(mesh);ax.set_xlim(v[:,0].min()-2,v[:,0].max()+2);ax.set_ylim(v[:,2].min()-2,v[:,2].max()+2);ax.set_zlim(0,v[:,1].max()+2)
 ax.view_init(elev=23,azim=-52);ax.set_box_aspect((1,1,1.1));ax.set_axis_off();ax.set_title(names[i-1],color='#cfe5ff',pad=3,fontsize=12)
fig.suptitle('04O | COMPILED GEOMETRY REVIEW',color='white',fontsize=23,x=.04,ha='left',y=.97)
fig.text(.04,.91,'Actual exported generator meshes. Neutral review lighting. This is NOT a Unity screenshot or visual acceptance.',color='#b5c6dd',fontsize=12)
fig.text(.04,.035,'Six structural families plus low fringe remains and retained collectible roofs. Runtime material, collision and camera review is still required.',color='#b5c6dd',fontsize=11)
fig.subplots_adjust(top=.86,bottom=.09,wspace=.02,hspace=.15)
fig.savefig(out/'compiled-geometry-review.png',dpi=150,facecolor=fig.get_facecolor());plt.close(fig)
