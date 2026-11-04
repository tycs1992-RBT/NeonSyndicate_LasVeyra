/* Independent licensed-candidate inspection. Does not run Unity or approve character likeness. */
#include "ufbx.h"
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
static void string_json(ufbx_string s) { putchar('"');for(size_t i=0;i<s.length;i++){unsigned char c=s.data[i];if(c=='"'||c=='\\')putchar('\\');if(c>=32)putchar(c);}putchar('"'); }
static void path_json(ufbx_node *node) {
 ufbx_node *parents[128];size_t count=0;while(node && !node->is_root && count<128){parents[count++]=node;node=node->parent;}
 putchar('"');for(size_t i=count;i>0;i--){if(i!=count)putchar('/');ufbx_string s=parents[i-1]->name;for(size_t j=0;j<s.length;j++){char c=s.data[j];if(c=='"'||c=='\\')putchar('\\');if(c>=32)putchar(c);}}putchar('"');
}
int main(int argc,char **argv) {
 if(argc<2)return 2;ufbx_load_opts opts={0};opts.strict=true;opts.target_unit_meters=1;ufbx_error error;
 ufbx_scene *scene=ufbx_load_file(argv[1],&opts,&error);if(!scene){char text[2048];ufbx_format_error(text,sizeof(text),&error);fprintf(stderr,"%s\n",text);return 1;}
 size_t vertices=0,triangles=0,unweighted=0;double max_sum_error=0;int valid=1;size_t max_weights=0;
 printf("{\"scope\":\"Strict independent candidate FBX/skin inspection, NOT Unity import or appearance approval\",\"bones\":%zu,\"clips\":%zu,\"source_unit_meters\":%.9g,\"meshes\":[",scene->bones.count,scene->anim_stacks.count,scene->settings.original_unit_meters);
 for(size_t m=0;m<scene->meshes.count;m++) {
  ufbx_mesh *mesh=scene->meshes.data[m];if(m)putchar(',');printf("{\"name\":");string_json(mesh->name);printf(",\"vertices\":%zu,\"triangles\":%zu,\"uv\":%s,\"normals\":%s,\"skin_deformers\":%zu}",mesh->num_vertices,mesh->num_triangles,mesh->vertex_uv.exists?"true":"false",mesh->vertex_normal.exists?"true":"false",mesh->skin_deformers.count);
  vertices+=mesh->num_vertices;triangles+=mesh->num_triangles;valid&=mesh->vertex_uv.exists && mesh->vertex_normal.exists && mesh->skin_deformers.count>0;
  if(mesh->skin_deformers.count) {
   ufbx_skin_deformer *skin=mesh->skin_deformers.data[0];
   for(size_t v=0;v<mesh->num_vertices;v++) {
    ufbx_skin_vertex vertex=skin->vertices.data[v];double sum=0;if(vertex.num_weights>max_weights)max_weights=vertex.num_weights;
    if(!vertex.num_weights)unweighted++;for(size_t w=0;w<vertex.num_weights;w++){double value=skin->weights.data[vertex.weight_begin+w].weight;sum+=value;valid&=isfinite(value)&&value>=0;}
    double err=fabs(sum-1);if(err>max_sum_error)max_sum_error=err;ufbx_vec3 p=mesh->vertices.data[v];valid&=isfinite(p.x)&&isfinite(p.y)&&isfinite(p.z);
   }
  }
 }
 printf("],\"materials\":[");for(size_t i=0;i<scene->materials.count;i++){if(i)putchar(',');string_json(scene->materials.data[i]->name);}
 printf("],\"nodes\":[");int comma=0;for(size_t i=0;i<scene->nodes.count;i++){ufbx_node *n=scene->nodes.data[i];if(!n->bone)continue;if(comma++)putchar(',');printf("{\"name\":");string_json(n->name);printf(",\"path\":");path_json(n);printf(",\"bind_position\":[%.9g,%.9g,%.9g]}",n->node_to_world.cols[3].x,n->node_to_world.cols[3].y,n->node_to_world.cols[3].z);}
 valid&=unweighted==0 && max_sum_error<.001 && scene->bones.count>20;
 printf("],\"vertices\":%zu,\"triangles\":%zu,\"unweighted\":%zu,\"maximum_weight_sum_error\":%.9g,\"maximum_influences\":%zu,\"passed\":%s,\"native_unity\":\"NOT RUN\"}\n",vertices,triangles,unweighted,max_sum_error,max_weights,valid?"true":"false");
 ufbx_free_scene(scene);return valid?0:1;
}
