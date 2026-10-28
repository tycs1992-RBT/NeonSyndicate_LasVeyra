/* Independent static environment FBX geometry and original cliff-surface correspondence, not Unity acceptance. */
#include "ufbx.h"
#include <float.h>
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
typedef struct { double x,y,z; } V;
static V v(double x,double y,double z){V a={x,y,z};return a;}
static V sub(V a,V b){return v(a.x-b.x,a.y-b.y,a.z-b.z);}
static V add(V a,V b){return v(a.x+b.x,a.y+b.y,a.z+b.z);}
static V mul(V a,double n){return v(a.x*n,a.y*n,a.z*n);}
static double dot(V a,V b){return a.x*b.x+a.y*b.y+a.z*b.z;}
static double distance(V p,V a,V b,V c){
 V ab=sub(b,a),ac=sub(c,a),ap=sub(p,a);double d1=dot(ab,ap),d2=dot(ac,ap);V q;
 if(d1<=0&&d2<=0)return sqrt(dot(ap,ap));V bp=sub(p,b);double d3=dot(ab,bp),d4=dot(ac,bp);if(d3>=0&&d4<=d3)return sqrt(dot(bp,bp));double vc=d1*d4-d3*d2;
 if(vc<=0&&d1>=0&&d3<=0){q=sub(p,add(a,mul(ab,d1/(d1-d3))));return sqrt(dot(q,q));}V cp=sub(p,c);double d5=dot(ab,cp),d6=dot(ac,cp);if(d6>=0&&d5<=d6)return sqrt(dot(cp,cp));double vb=d5*d2-d1*d6;
 if(vb<=0&&d2>=0&&d6<=0){q=sub(p,add(a,mul(ac,d2/(d2-d6))));return sqrt(dot(q,q));}double va=d3*d6-d5*d4;
 if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0){q=sub(p,add(b,mul(sub(c,b),(d4-d3)/((d4-d3)+(d5-d6)))));return sqrt(dot(q,q));}
 double sum=va+vb+vc;if(fabs(sum)<1e-18)return DBL_MAX;q=sub(p,add(a,add(mul(ab,vb/sum),mul(ac,vc/sum))));return sqrt(dot(q,q));
}
static int unit(ufbx_vec3 a){double l=sqrt(a.x*a.x+a.y*a.y+a.z*a.z);return isfinite(l)&&l>.98&&l<1.02;}
int main(int argc,char**argv){
 if(argc!=2)return 2;ufbx_load_opts options={0};options.strict=true;options.target_unit_meters=1;options.target_axes=ufbx_axes_left_handed_y_up;ufbx_error error;ufbx_scene*s=ufbx_load_file(argv[1],&options,&error);
 if(!s){char text[1024];ufbx_format_error(text,sizeof(text),&error);fprintf(stderr,"%s",text);return 1;}
 int pass=s->meshes.count>0&&s->bones.count==0&&s->skin_deformers.count==0&&s->anim_stacks.count==0&&s->lights.count==0&&s->cameras.count==0;size_t triangles=0,vertices=0,bad_uv=0,bad_normal=0,bad_tangent=0;double mn[3]={DBL_MAX,DBL_MAX,DBL_MAX},mx[3]={-DBL_MAX,-DBL_MAX,-DBL_MAX},max_deviation=0;
 V original[4096];int original_count=0,faces[4096][3],face_count=0,seed=0;char*tag=strstr(argv[1],"ErodedCliff");
 if(tag){seed=atoi(tag+11);char name[256];snprintf(name,sizeof(name),"ArtSource/HeartWR02/Cliff%d.source.obj",seed);FILE*f=fopen(name,"r");if(!f){fprintf(stderr,"Original physical source unavailable: %s",name);ufbx_free_scene(s);return 1;}char line[1024];while(fgets(line,sizeof(line),f)){double x,y,z;int a,b,c;if(sscanf(line,"v %lf %lf %lf",&x,&y,&z)==3&&original_count<4096)original[original_count++]=v(x,y,z);if(sscanf(line,"f %d %d %d",&a,&b,&c)==3&&face_count<4096){faces[face_count][0]=a-1;faces[face_count][1]=b-1;faces[face_count++][2]=c-1;}}fclose(f);if(!face_count)pass=0;}
 for(size_t i=0;i<s->meshes.count;i++){
  ufbx_mesh*m=s->meshes.data[i];triangles+=m->num_triangles;vertices+=m->num_vertices;pass&=m->num_faces==m->num_triangles&&m->vertex_normal.exists&&m->vertex_tangent.exists&&m->vertex_uv.exists&&m->instances.count==1;
  if(!m->vertex_uv.exists||m->instances.count!=1){pass=0;continue;}
  for(size_t n=0;n<m->vertex_normal.values.count;n++)bad_normal+=!unit(m->vertex_normal.values.data[n]);for(size_t n=0;n<m->vertex_tangent.values.count;n++)bad_tangent+=!unit(m->vertex_tangent.values.data[n]);
  for(size_t f=0;f<m->faces.count;f++){
   ufbx_face face=m->faces.data[f];if(face.num_indices!=3){pass=0;continue;}ufbx_vec2 a=ufbx_get_vertex_vec2(&m->vertex_uv,face.index_begin),b=ufbx_get_vertex_vec2(&m->vertex_uv,face.index_begin+1),c=ufbx_get_vertex_vec2(&m->vertex_uv,face.index_begin+2);double area=fabs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x));bad_uv+=!isfinite(area)||area<1e-13;
  }
  for(size_t j=0;j<m->num_vertices;j++){
   ufbx_vec3 p=ufbx_transform_position(&m->instances.data[0]->geometry_to_world,m->vertices.data[j]);double a[3]={p.x,p.y,p.z};for(int axis=0;axis<3;axis++){pass&=isfinite(a[axis]);mn[axis]=fmin(mn[axis],a[axis]);mx[axis]=fmax(mx[axis],a[axis]);}
   if(face_count){double closest=DBL_MAX;for(int f=0;f<face_count;f++){int a=faces[f][0],b=faces[f][1],c=faces[f][2];if(a<0||b<0||c<0||a>=original_count||b>=original_count||c>=original_count){pass=0;continue;}closest=fmin(closest,distance(v(p.x,p.y,p.z),original[a],original[b],original[c]));}max_deviation=fmax(max_deviation,closest);}
  }
 }
 pass&=triangles>=100&&triangles<=100000&&!bad_normal&&!bad_tangent&&!bad_uv&&max_deviation<.035;
 if(strstr(argv[1],"SanctuaryCedar")||strstr(argv[1],"AzureElder")){double lo[3]={-.5,0,-.5},hi[3]={.5,1,.5};for(int i=0;i<3;i++)pass&=fabs(mn[i]-lo[i])<.035&&fabs(mx[i]-hi[i])<.035;}
 fprintf(stderr,"triangles=%zu normals=%zu tangents=%zu UV=%zu deviation=%.9g\n",triangles,bad_normal,bad_tangent,bad_uv,max_deviation);
 printf("{\"scope\":\"Independent strict static FBX parse. No native Unity claim.\",\"meshes\":%zu,\"vertices\":%zu,\"triangles\":%zu,\"badNormals\":%zu,\"badTangents\":%zu,\"badUV\":%zu,\"sourceCliffSeed\":%d,\"maximumOriginalSurfaceDeviationMetres\":%.12g,\"minimum\":[%.9g,%.9g,%.9g],\"maximum\":[%.9g,%.9g,%.9g],\"passed\":%s,\"nativeUnity\":\"NOT RUN\"}\n",s->meshes.count,vertices,triangles,bad_normal,bad_tangent,bad_uv,seed,max_deviation,mn[0],mn[1],mn[2],mx[0],mx[1],mx[2],pass?"true":"false");ufbx_free_scene(s);return pass?0:1;
}
