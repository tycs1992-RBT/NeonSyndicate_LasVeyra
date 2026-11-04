/* Actual triangle ray tests for corrected wall support. Independent of Blender and Unity. */
#include "ufbx.h"
#include <math.h>
#include <stdio.h>
#include <string.h>
static ufbx_vec3 sub(ufbx_vec3 a,ufbx_vec3 b){ufbx_vec3 v={a.x-b.x,a.y-b.y,a.z-b.z};return v;}
static ufbx_vec3 cross(ufbx_vec3 a,ufbx_vec3 b){ufbx_vec3 v={a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x};return v;}
static double dot(ufbx_vec3 a,ufbx_vec3 b){return a.x*b.x+a.y*b.y+a.z*b.z;}
static int ray(ufbx_vec3 origin,ufbx_vec3 direction,ufbx_vec3 a,ufbx_vec3 b,ufbx_vec3 c,double limit)
{
 ufbx_vec3 e=sub(b,a),f=sub(c,a),h=cross(direction,f);double det=dot(e,h);if(fabs(det)<1e-10)return 0;
 ufbx_vec3 s=sub(origin,a),q=cross(s,e);double u=dot(s,h)/det,v=dot(direction,q)/det,t=dot(f,q)/det;
 return u>=-1e-7&&v>=-1e-7&&u+v<=1.0000001&&t>=0&&t<limit;
}
static int hit(ufbx_scene *s,ufbx_vec3 origin,ufbx_vec3 direction,double limit)
{
 for(size_t j=0;j<s->meshes.count;j++){
  ufbx_mesh *m=s->meshes.data[j];for(size_t face=0;face<m->faces.count;face++){
   ufbx_face f=m->faces.data[face];if(f.num_indices!=3)continue;ufbx_vec3 p[3];
   for(size_t i=0;i<3;i++){p[i]=ufbx_get_vertex_vec3(&m->vertex_position,f.index_begin+i);if(m->instances.count)p[i]=ufbx_transform_position(&m->instances.data[0]->geometry_to_world,p[i]);}
   if(ray(origin,direction,p[0],p[1],p[2],limit))return 1;
  }
 }
 return 0;
}
int main(int argc,char **argv)
{
 if(argc!=2&&argc!=3)return 2;int legacy=argc==3&&strcmp(argv[2],"--legacy")==0;if(argc==3&&!legacy)return 2;
 int gate=strstr(argv[1],"HeartGate_")!=NULL,house=strstr(argv[1],"HeartHouse")!=NULL;if(!gate&&!house)return 2;
 ufbx_load_opts opts={0};opts.strict=true;opts.target_unit_meters=1;opts.target_axes=ufbx_axes_right_handed_y_up;
 ufbx_error error;ufbx_scene *s=ufbx_load_file(argv[1],&opts,&error);if(!s)return 2;
 double heights[]={.9,4.1,7.9,8.3,10.1,11.8};int total=gate?6:4,passed=0;
 for(int i=0;i<total;i++){
  ufbx_vec3 origin={gate?(legacy?18:9):0,heights[i],gate?-10:0},direction={gate?0:1,0,gate?1:0};int valid=hit(s,origin,direction,gate?14:6.2);passed+=valid;
 }
 printf("{\"gate_center_x\":%d,\"wall_probes_passed\":%d,\"wall_probes_total\":%d,\"passed\":%s}\n",legacy?18:9,passed,total,passed==total?"true":"false");
 ufbx_free_scene(s);return passed==total?0:1;
}
