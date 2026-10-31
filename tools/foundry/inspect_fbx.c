/* Independent strict asset/skin/motion parse. Never Unity import, motion or combat acceptance. */
#include "ufbx.h"
#include "creature_contract.h"
#include <float.h>
#include <math.h>
#include <stdio.h>
#include <string.h>
static int finite_unit(ufbx_vec3 v){double n=sqrt(v.x*v.x+v.y*v.y+v.z*v.z);return isfinite(n)&&n>.98&&n<1.02;}
int main(int argc,char **argv){
 if(argc!=2)return 2;int creature=strstr(argv[1],"CrystalBasiliskCandidate")!=NULL;
 ufbx_load_opts opt={0};opt.strict=true;opt.target_unit_meters=1;opt.target_axes=ufbx_axes_left_handed_y_up;ufbx_error error;
 ufbx_scene *s=ufbx_load_file(argv[1],&opt,&error);if(!s){char message[1024];ufbx_format_error(message,sizeof(message),&error);fprintf(stderr,"%s",message);return 1;}
 int pass=s->meshes.count>0&&s->lights.count==0&&s->cameras.count==0;size_t vertices=0,triangles=0,unweighted=0,too_many=0,uv_bad=0;double bind_error=0,mn[3]={DBL_MAX,DBL_MAX,DBL_MAX},mx[3]={-DBL_MAX,-DBL_MAX,-DBL_MAX};
 for(size_t i=0;i<s->meshes.count;i++){
  ufbx_mesh *m=s->meshes.data[i];fprintf(stderr,"mesh %s faces=%zu triangles=%zu normal=%d tangent=%d uv=%d instances=%zu\n",m->name.data,m->num_faces,m->num_triangles,m->vertex_normal.exists,m->vertex_tangent.exists,m->vertex_uv.exists,m->instances.count);vertices+=m->num_vertices;triangles+=m->num_triangles;pass&=m->num_faces==m->num_triangles&&m->vertex_uv.exists&&m->vertex_normal.exists&&m->vertex_tangent.exists&&m->instances.count==1;
  for(size_t v=0;v<m->num_vertices;v++){
   ufbx_vec3 p=ufbx_transform_position(&m->instances.data[0]->geometry_to_world,m->vertices.data[v]);double q[3]={p.x,p.y,p.z};for(int axis=0;axis<3;axis++){pass&=isfinite(q[axis]);mn[axis]=fmin(mn[axis],q[axis]);mx[axis]=fmax(mx[axis],q[axis]);}
  }
  for(size_t v=0;v<m->vertex_normal.values.count;v++){if(!finite_unit(m->vertex_normal.values.data[v]))fprintf(stderr,"bad normal\n");pass&=finite_unit(m->vertex_normal.values.data[v]);}
  for(size_t v=0;v<m->vertex_tangent.values.count;v++){if(!finite_unit(m->vertex_tangent.values.data[v]))fprintf(stderr,"bad tangent %s %.9g %.9g %.9g\n",m->name.data,m->vertex_tangent.values.data[v].x,m->vertex_tangent.values.data[v].y,m->vertex_tangent.values.data[v].z);pass&=finite_unit(m->vertex_tangent.values.data[v]);}
  for(size_t v=0;v<m->num_indices;v++) {ufbx_vec2 uv=ufbx_get_vertex_vec2(&m->vertex_uv,v);pass&=isfinite(uv.x)&&isfinite(uv.y);}
  for(size_t f=0;f<m->faces.count;f++){ufbx_face face=m->faces.data[f];ufbx_vec2 a=ufbx_get_vertex_vec2(&m->vertex_uv,face.index_begin),b=ufbx_get_vertex_vec2(&m->vertex_uv,face.index_begin+1),c=ufbx_get_vertex_vec2(&m->vertex_uv,face.index_begin+2);double area=fabs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x));uv_bad+=area<1e-13;}
  if(creature){
   pass&=m->skin_deformers.count==1;
   for(size_t d=0;d<m->skin_deformers.count;d++){
    ufbx_skin_deformer *skin=m->skin_deformers.data[d];
    for(size_t v=0;v<skin->vertices.count;v++){ufbx_skin_vertex sv=skin->vertices.data[v];double sum=0;size_t nonzero=0;for(size_t j=0;j<sv.num_weights;j++){double w=skin->weights.data[sv.weight_begin+j].weight;pass&=isfinite(w)&&w>=0;sum+=w;nonzero+=w>1e-8;}unweighted+=sum<.001;too_many+=nonzero>4;pass&=fabs(sum-1)<.003;}
   }
  }else pass&=m->skin_deformers.count==0;
 }
 pass&=triangles>1000&&triangles<=(creature?20000:70000)&&uv_bad==0&&unweighted==0&&too_many==0;
 if(creature){
  for(int i=0;i<32;i++){ufbx_node *node=ufbx_find_node(s,bone_names[i]);if(!node){pass=0;continue;}ufbx_vec3 p=node->node_to_world.cols[3];int bound=0;for(size_t pi=0;pi<s->poses.count;pi++){ufbx_pose *pose=s->poses.data[pi];if(!pose->is_bind_pose)continue;for(size_t bi=0;bi<pose->bone_poses.count;bi++)if(pose->bone_poses.data[bi].bone_node==node){p=pose->bone_poses.data[bi].bone_to_world.cols[3];bound=1;}}pass&=bound;double q[3]={p.x,p.y,p.z};for(int j=0;j<3;j++)bind_error=fmax(bind_error,fabs(q[j]-bone_positions[i][j]));}
  pass&=bind_error<.002&&s->anim_stacks.count==5;
 }else{
  double lo[3]={-15.02,8.33,-.725},hi[3]={15.02,35.1,48.48};for(int i=0;i<3;i++)pass&=fabs(mn[i]-lo[i])<.03&&fabs(mx[i]-hi[i])<.03;
  pass&=s->bones.count==0&&s->anim_stacks.count==0;
 }
 printf("{\"scope\":\"Independent strict FBX parse, not native Unity\",\"meshes\":%zu,\"vertices\":%zu,\"triangles\":%zu,\"uvDegenerate\":%zu,\"unweighted\":%zu,\"overFourInfluences\":%zu,\"bones\":%zu,\"bindErrorMetres\":%.12g,\"minimum\":[%.9g,%.9g,%.9g],\"maximum\":[%.9g,%.9g,%.9g],\"clips\":[",s->meshes.count,vertices,triangles,uv_bad,unweighted,too_many,s->bones.count,bind_error,mn[0],mn[1],mn[2],mx[0],mx[1],mx[2]);
 for(size_t a=0;a<s->anim_stacks.count;a++){
  ufbx_anim_stack *stack=s->anim_stacks.data[a];if(a)putchar(',');printf("{\"name\":\"%s\",\"duration\":%.9g,\"samples\":[",stack->name.data,stack->time_end-stack->time_begin);
  int frame_count=(int)floor((stack->time_end-stack->time_begin)*30+.5)+1;for(int f=0;f<frame_count;f++){
   double t=fmin(stack->time_end,stack->time_begin+f/30.0);ufbx_evaluate_opts ev={0};ufbx_scene *scene=ufbx_evaluate_scene(s,stack->anim,t,&ev,&error);if(!scene){pass=0;continue;}
   if(f)putchar(',');printf("{\"frame\":%d,\"positions\":[",(int)floor((t-stack->time_begin)*30+.5)+1);
   for(int b=0;b<32;b++){ufbx_node *node=ufbx_find_node(scene,bone_names[b]);if(b)putchar(',');if(!node){printf("null");pass=0;continue;}ufbx_vec3 v=node->node_to_world.cols[3];pass&=isfinite(v.x+v.y+v.z);printf("[%.9g,%.9g,%.9g]",v.x,v.y,v.z);}printf("]}");ufbx_free_scene(scene);
  }printf("]}");
 }printf("],\"passed\":%s,\"nativeUnity\":\"NOT RUN\"}\n",pass?"true":"false");ufbx_free_scene(s);return pass?0:1;
}
