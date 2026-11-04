/* Strict independent source inspection. This does not execute Unity import, rendering or collision. */
#include "ufbx.h"
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
typedef struct { double x,y,z; } point;
typedef struct { point a,b,c; } triangle;
static point sub(point a,point b){point p={a.x-b.x,a.y-b.y,a.z-b.z};return p;}
static point add(point a,point b){point p={a.x+b.x,a.y+b.y,a.z+b.z};return p;}
static point mul(point a,double s){point p={a.x*s,a.y*s,a.z*s};return p;}
static double dot(point a,point b){return a.x*b.x+a.y*b.y+a.z*b.z;}
static point cross(point a,point b){point p={a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x};return p;}
static double segment(point p,point a,point b){point d=sub(b,a);double n=dot(d,d),t=n>0?fmax(0,fmin(1,dot(sub(p,a),d)/n)):0;point v=sub(p,add(a,mul(d,t)));return sqrt(dot(v,v));}
static double distance_to(point p,triangle t){point ab=sub(t.b,t.a),ac=sub(t.c,t.a),n=cross(ab,ac),ap=sub(p,t.a);double nn=dot(n,n);if(nn<1e-20)return 1e9;double h=dot(ap,n)/nn;point q=sub(ap,mul(n,h));double d00=dot(ab,ab),d01=dot(ab,ac),d11=dot(ac,ac),d20=dot(q,ab),d21=dot(q,ac),den=d00*d11-d01*d01;double v=(d11*d20-d01*d21)/den,w=(d00*d21-d01*d20)/den;if(v>=-1e-7&&w>=-1e-7&&v+w<=1.0000001)return fabs(h)*sqrt(nn);return fmin(segment(p,t.a,t.b),fmin(segment(p,t.b,t.c),segment(p,t.c,t.a)));}
static double source_distance(point p,triangle *source,size_t count){double d=1e9;for(size_t i=0;i<count;i++)d=fmin(d,distance_to(p,source[i]));return d;}
int main(int argc,char **argv){
 if(argc<2||argc>3)return 2;triangle source[512];size_t source_count=0;int cliff=argc==3;
 if(cliff){FILE *f=fopen(argv[2],"r");if(!f)return 2;point v[4096];size_t nv=0;char line[1024];while(fgets(line,sizeof(line),f)){point p;unsigned a,b,c;if(sscanf(line,"v %lf %lf %lf",&p.x,&p.y,&p.z)==3){if(nv>=4096)return 2;v[nv++]=p;}else if(sscanf(line,"f %u %u %u",&a,&b,&c)==3){if(source_count>=512||a<1||b<1||c<1||a>nv||b>nv||c>nv)return 2;source[source_count++]=(triangle){v[a-1],v[b-1],v[c-1]};}}fclose(f);if(source_count!=36)return 2;}
 ufbx_load_opts options={0};options.strict=true;options.target_unit_meters=1;options.target_axes=ufbx_axes_left_handed_y_up;ufbx_error error;
 ufbx_scene *scene=ufbx_load_file(argv[1],&options,&error);if(!scene){char msg[2048];ufbx_format_error(msg,sizeof(msg),&error);fprintf(stderr,"%s",msg);return 1;}
 int valid=scene->meshes.count==1&&scene->bones.count==0&&scene->anim_stacks.count==0&&scene->lights.count==0&&scene->cameras.count==0;
 size_t vertices=0,triangles=0,uv_degenerate=0;double minimum[3]={1e9,1e9,1e9},maximum[3]={-1e9,-1e9,-1e9},max_error=0;
 for(size_t k=0;k<scene->meshes.count;k++){
  ufbx_mesh *m=scene->meshes.data[k];vertices+=m->num_vertices;triangles+=m->num_triangles;
  valid&=m->instances.count==1&&m->materials.count==1&&m->skin_deformers.count==0&&m->vertex_uv.exists&&m->vertex_normal.exists&&m->vertex_tangent.exists;
  if(m->materials.count)valid&=strcmp(m->materials.data[0]->name.data,cliff?"WeatheredStrata":"CanopyLeaf")==0;
  for(size_t i=0;i<m->num_vertices;i++){
   ufbx_vec3 v=ufbx_transform_position(&m->instances.data[0]->geometry_to_world,m->vertices.data[i]);double a[3]={v.x,v.y,v.z};point p={v.x,v.y,v.z};
   for(int axis=0;axis<3;axis++){valid&=isfinite(a[axis]);minimum[axis]=fmin(minimum[axis],a[axis]);maximum[axis]=fmax(maximum[axis],a[axis]);}
   if(cliff)max_error=fmax(max_error,source_distance(p,source,source_count));
  }
  for(size_t i=0;i<m->vertex_normal.values.count;i++){ufbx_vec3 n=m->vertex_normal.values.data[i];double length=sqrt(n.x*n.x+n.y*n.y+n.z*n.z);valid&=isfinite(length)&&length>.98&&length<1.02;}
  for(size_t i=0;i<m->vertex_tangent.values.count;i++){ufbx_vec3 n=m->vertex_tangent.values.data[i];double length=sqrt(n.x*n.x+n.y*n.y+n.z*n.z);valid&=isfinite(length)&&length>.98&&length<1.02;}
  for(size_t i=0;i<m->faces.count;i++){
   ufbx_face f=m->faces.data[i];if(f.num_indices!=3){valid=0;continue;}
   ufbx_vec2 uv[3];point p[3];for(size_t j=0;j<3;j++){uint32_t index=f.index_begin+(uint32_t)j;uv[j]=ufbx_get_vertex_vec2(&m->vertex_uv,index);ufbx_vec3 v=ufbx_transform_position(&m->instances.data[0]->geometry_to_world,ufbx_get_vertex_vec3(&m->vertex_position,index));p[j]=(point){v.x,v.y,v.z};valid&=isfinite(uv[j].x)&&isfinite(uv[j].y);}
   double area=fabs((uv[1].x-uv[0].x)*(uv[2].y-uv[0].y)-(uv[1].y-uv[0].y)*(uv[2].x-uv[0].x));if(area<1e-12)uv_degenerate++;
   if(cliff){max_error=fmax(max_error,source_distance(mul(add(add(p[0],p[1]),p[2]),1.0/3),source,source_count));for(int j=0;j<3;j++)max_error=fmax(max_error,source_distance(mul(add(p[j],p[(j+1)%3]),.5),source,source_count));}
  }
 }
 valid&=vertices>3&&triangles>0&&triangles<=30000&&uv_degenerate==0;
 if(cliff)valid&=max_error<.0001;else{double lo[3]={-.5,0,-.5},hi[3]={.5,1,.5};for(int i=0;i<3;i++)valid&=fabs(minimum[i]-lo[i])<.002&&fabs(maximum[i]-hi[i])<.002;}
 printf("{\"vertices\":%zu,\"triangles\":%zu,\"uvDegenerate\":%zu,\"sourceTrianglePlanes\":%zu,\"maximumSourceSurfaceErrorMetres\":%.12g,\"minimum\":[%.9g,%.9g,%.9g],\"maximum\":[%.9g,%.9g,%.9g],\"axes\":\"left_handed_y_up\",\"passed\":%s}\n",vertices,triangles,uv_degenerate,source_count,max_error,minimum[0],minimum[1],minimum[2],maximum[0],maximum[1],maximum[2],valid?"true":"false");ufbx_free_scene(scene);return valid?0:1;
}
