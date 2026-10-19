#!/usr/bin/env python3
"""Inspect actual shipping generator meshes under neutral offline light. This does not render Unity materials or gameplay."""
import argparse
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
import numpy as np

def read(path):
 v=[];t=[]
 for line in path.read_text().splitlines():
  if line.startswith('v '):v.append([float(x) for x in line.split()[1:4]])
  elif line.startswith('f '):t.append([int(x)-1 for x in line.split()[1:4]])
 return np.asarray(v)[:,[0,2,1]],np.asarray(t)
def panel(ax,path,title,elev=23,azim=-58):
 v,t=read(path);tri=v[t];normal=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);normal/=np.maximum(1e-8,np.linalg.norm(normal,axis=1))[:,None]
 light=.24+.6*np.maximum(0,normal@np.array([.4,-.5,.76]))
 colors=np.column_stack((light*.81,light*.86,light*.97,np.ones(len(light))))
 shape=Poly3DCollection(tri,facecolors=colors,edgecolors=(.10,.13,.22,.11),linewidths=.12)
 ax.add_collection3d(shape);lo=v.min(axis=0);hi=v.max(axis=0);span=np.maximum(1,hi-lo);pad=span*.04
 ax.set_xlim(lo[0]-pad[0],hi[0]+pad[0]);ax.set_ylim(lo[1]-pad[1],hi[1]+pad[1]);ax.set_zlim(min(0,lo[2]),hi[2]+pad[2]);ax.set_box_aspect(span)
 ax.view_init(elev=elev,azim=azim);ax.set_axis_off();ax.set_title(title,color='#d7e6f6',fontsize=12,pad=0)
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--input',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
 fig=plt.figure(figsize=(16,10),facecolor='#101827')
 names=['Cloister residence','Pointed vaulted hall','Forge and mineral furnace','Raised canal compound','Mineral-backed dwelling','Fractured gate ruin']
 for i,name in enumerate(names):
  ax=fig.add_subplot(2,4,i+1,projection='3d',facecolor='#101827');panel(ax,a.input/f'gothic-family-{i+1}-near.obj',name)
 panel(fig.add_subplot(2,4,7,projection='3d',facecolor='#101827'),a.input/'venue-archive.obj','Deep Archive sanctuary',elev=12,azim=-80)
 panel(fig.add_subplot(2,4,8,projection='3d',facecolor='#101827'),a.input/'compact-shipping-structures.obj','330m authoring crop, not the whole world',elev=36,azim=-65)
 fig.suptitle('04O-B | ACTUAL COMPILED GEOMETRY',color='white',x=.035,ha='left',y=.975,fontsize=24)
 fig.text(.035,.925,'NOT UNITY. Neutral source-mesh inspection only. Materials, game-camera views, performance and gameplay remain unverified.',color='#d3bd8d',fontsize=12)
 fig.text(.035,.04,'Original generators are called by CityBuilder and the existing venue owner. The crop omits the surrounding retained realm and source furniture.',color='#b2c3d7',fontsize=11)
 fig.subplots_adjust(top=.86,bottom=.10,wspace=.08,hspace=.19);fig.savefig(a.output/'04ob-compiled-geometry-review.png',dpi=150,facecolor=fig.get_facecolor());plt.close(fig)
if __name__=='__main__':main()
