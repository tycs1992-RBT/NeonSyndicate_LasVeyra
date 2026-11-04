/* Independent civic-crown and skyline geometry inspection. Not Unity import. */
#include "ufbx.h"
#include <float.h>
#include <math.h>
#include <stdio.h>
#include <string.h>
static int unit(ufbx_vec3 v){double n=sqrt(v.x*v.x+v.y*v.y+v.z*v.z);return isfinite(n)&&n>.98&&n<1.02;}
int main(int argc,char **argv){
 if(argc!=2)return 2;
 int archive=strstr(argv[1],"WRArchiveCrown_LOD")!=NULL,office=strstr(argv[1],"WROfficeCrown_LOD")!=NULL;
 int skyline=strstr(argv[1],"WRSkyline809556_LOD")?809556:strstr(argv[1],"WRSkyline32874_LOD")?32874:strstr(argv[1],"WRSkyline53496_LOD")?53496:0;
 if(!archive&&!office&&!skyline)return 2;
 double width=skyline==809556?45.001105:skyline==32874?50.74799:40,depth=skyline==809556?21.18362:skyline==32874?20.03610:34.000003,radius=fmin(width,depth)*.11;
 ufbx_load_opts options={0};options.strict=true;options.target_unit_meters=1;options.target_axes=ufbx_axes_left_handed_y_up;
 ufbx_error error;ufbx_scene *s=ufbx_load_file(argv[1],&options,&error);
 if(!s){char message[2048];ufbx_format_error(message,sizeof(message),&error);fprintf(stderr,"%s",message);return 1;}
 int valid=s->meshes.count==1&&s->bones.count==0&&s->anim_stacks.count==0;double mn[3]={DBL_MAX,DBL_MAX,DBL_MAX},mx[3]={-DBL_MAX,-DBL_MAX,-DBL_MAX};size_t vertices=0,triangles=0,materials=0;
 for(size_t j=0;j<s->meshes.count;j++){
  ufbx_mesh *m=s->meshes.data[j];vertices+=m->num_vertices;triangles+=m->num_triangles;materials+=m->materials.count;
  valid&=m->vertex_uv.exists&&m->vertex_normal.exists&&m->vertex_tangent.exists&&m->vertex_bitangent.exists&&m->skin_deformers.count==0&&m->num_faces==m->num_triangles&&m->instances.count==1;
  for(size_t i=0;i<m->num_vertices;i++){
   ufbx_vec3 p=ufbx_transform_position(&m->instances.data[0]->geometry_to_world,m->vertices.data[i]);double a[3]={p.x,p.y,p.z};
   for(int axis=0;axis<3;axis++){valid&=isfinite(a[axis]);if(a[axis]<mn[axis])mn[axis]=a[axis];if(a[axis]>mx[axis])mx[axis]=a[axis];}
   valid&=skyline?p.y>=-.02&&fabs(p.x)<=26:p.y>=8.30&&fabs(p.x)<=15.10;
   if(skyline&&p.y<8.30){int near=0;for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){double dx=p.x-(x*width*.34-x*.06*p.y),dz=p.z-z*depth*.34;near|=dx*dx+dz*dz<(radius*1.5+.2)*(radius*1.5+.2);}valid&=near;}
  }
  for(size_t i=0;i<m->vertex_normal.values.count;i++)valid&=unit(m->vertex_normal.values.data[i]);
  for(size_t i=0;i<m->vertex_tangent.values.count;i++)valid&=unit(m->vertex_tangent.values.data[i]);
  for(size_t i=0;i<m->vertex_bitangent.values.count;i++)valid&=unit(m->vertex_bitangent.values.data[i]);
  for(size_t i=0;i<m->vertex_uv.values.count;i++)valid&=isfinite(m->vertex_uv.values.data[i].x)&&isfinite(m->vertex_uv.values.data[i].y);
  for(size_t i=0;i<m->materials.count;i++){const char*n=m->materials.data[i]->name.data;valid&=strcmp(n,"Masonry")==0||strcmp(n,"CarvedTrim")==0||strcmp(n,"SlateTimber")==0||strcmp(n,"AmethystBronze")==0;}
 }
 double emn[3]={-15.0200014,8.329999,archive?-.725000:-.950000},emx[3]={15.0200014,archive?35.0999985:33.2000008,archive?48.4799995:30.4299984};
 if(skyline){emn[0]=skyline==809556?-22.7505531:skyline==32874?-25.6239948:-20.25;emn[1]=-.006;emn[2]=skyline==809556?-10.5099745:skyline==32874?-9.947689:-16.7900028;emx[0]=-emn[0];emx[1]=skyline==809556?166.274826:skyline==32874?132.568369:166.466705;emx[2]=-emn[2];}
 for(int axis=0;axis<3;axis++){valid&=fabs(mn[axis]-emn[axis])<.01;valid&=fabs(mx[axis]-emx[axis])<.01;}
 valid&=vertices>=3&&triangles>=1000&&triangles<=40000&&materials==4;
 printf("{\"vertices\":%zu,\"triangles\":%zu,\"materials\":%zu,\"axes\":\"left_handed_y_up\",\"minimum\":[%.9g,%.9g,%.9g],\"maximum\":[%.9g,%.9g,%.9g],\"passed\":%s}\n",vertices,triangles,materials,mn[0],mn[1],mn[2],mx[0],mx[1],mx[2],valid?"true":"false");ufbx_free_scene(s);return valid?0:1;
}
