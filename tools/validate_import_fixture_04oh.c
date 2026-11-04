/* Standalone validation with the separately obtained open-source FBX parser.
   This is NOT Unity import, deformation rendering, or professional-art acceptance. */
#include "ufbx.h"
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
static int require(int condition,const char *reason) { if(!condition){fprintf(stderr,"FAILED: %s\n",reason);return 0;}return 1; }
static double distance(ufbx_vec3 a,ufbx_vec3 b) { double x=a.x-b.x,y=a.y-b.y,z=a.z-b.z;return sqrt(x*x+y*y+z*z); }
int main(int argc,char **argv) {
 if(argc!=2)return 2;
 ufbx_load_opts options={0};options.strict=true;ufbx_error error;
 ufbx_scene *scene=ufbx_load_file(argv[1],&options,&error);
 if(!scene){char text[2048];ufbx_format_error(text,sizeof(text),&error);fprintf(stderr,"%s\n",text);return 1;}
 int ok=require(scene->meshes.count==1 && scene->bones.count==47 && scene->skin_deformers.count==1 && scene->anim_stacks.count==1,"expected skinned calibration hierarchy and clip");
 ok &= require(fabs(scene->settings.unit_meters-1)<1e-9,"metre units");
 ufbx_mesh *mesh=scene->meshes.data[0];ufbx_skin_deformer *skin=scene->skin_deformers.data[0];
 ok &= require(mesh->num_vertices==188 && mesh->num_triangles==188 && skin->clusters.count==47,"mesh/cluster topology");
 double bind_error=0;
 for(size_t i=0;i<mesh->num_vertices;i++) {
  ufbx_skin_vertex vertex=skin->vertices.data[i];double sum=0;
  for(size_t w=0;w<vertex.num_weights;w++)sum+=skin->weights.data[vertex.weight_begin+w].weight;
  ok &= require(vertex.num_weights==1 && fabs(sum-1)<1e-8,"complete finite normalized skin weights");
  ufbx_matrix matrix=ufbx_get_skin_vertex_matrix(skin,i,NULL);ufbx_vec3 point=ufbx_transform_position(&matrix,mesh->vertices.data[i]);
  double delta=distance(point,mesh->vertices.data[i]);if(delta>bind_error)bind_error=delta;
 }
 ok &= require(bind_error<1e-6,"bind matrices preserve actual mesh positions");
 ufbx_scene *middle=ufbx_evaluate_scene(scene,scene->anim_stacks.data[0]->anim,.5,NULL,&error);
 ufbx_scene *end=ufbx_evaluate_scene(scene,scene->anim_stacks.data[0]->anim,1,NULL,&error);
 ok &= require(middle!=NULL && end!=NULL,"clip evaluation");
 double motion=0,recovery=0;
 if(middle && end)for(size_t i=0;i<mesh->num_vertices;i++) {
  ufbx_matrix m=ufbx_get_skin_vertex_matrix(middle->skin_deformers.data[0],i,NULL),e=ufbx_get_skin_vertex_matrix(end->skin_deformers.data[0],i,NULL);
  ufbx_vec3 mp=ufbx_transform_position(&m,mesh->vertices.data[i]),ep=ufbx_transform_position(&e,mesh->vertices.data[i]);
  double movement=distance(mp,mesh->vertices.data[i]),release=distance(ep,mesh->vertices.data[i]);
  if(movement>motion)motion=movement;if(release>recovery)recovery=release;
 }
 ok &= require(motion>.01 && recovery<1e-6,"clip deforms real skin and recovers to bind");
 printf("{\"scope\":\"Independent strict FBX parse and skin/clip math, NOT Unity\",\"passed\":%s,\"bones\":%zu,\"vertices\":%zu,\"triangles\":%zu,\"skin_clusters\":%zu,\"clips\":%zu,\"unit_meters\":%.9g,\"maximum_bind_error\":%.9g,\"maximum_clip_motion\":%.9g,\"maximum_recovery_error\":%.9g,\"native_unity\":\"NOT RUN\"}\n",ok?"true":"false",scene->bones.count,mesh->num_vertices,mesh->num_triangles,skin->clusters.count,scene->anim_stacks.count,scene->settings.unit_meters,bind_error,motion,recovery);
 ufbx_free_scene(middle);ufbx_free_scene(end);ufbx_free_scene(scene);return ok?0:1;
}
