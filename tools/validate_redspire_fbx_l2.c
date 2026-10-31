/* Independent strict parser evidence. No Unity import or native gameplay. */
#include "ufbx.h"
#include <math.h>
#include <stdio.h>
int main(int argc,char **argv){
 if(argc!=2)return 2;ufbx_load_opts options={0};options.strict=true;options.target_unit_meters=1;ufbx_error error;ufbx_scene *s=ufbx_load_file(argv[1],&options,&error);
 if(!s){char text[2048];ufbx_format_error(text,sizeof(text),&error);fprintf(stderr,"%s\n",text);return 1;}
 int valid=s->meshes.count==1 && s->bones.count==0 && s->anim_stacks.count==0 && s->cameras.count==0 && s->lights.count==0 && s->materials.count==1;size_t triangles=0,vertices=0;
 for(size_t j=0;j<s->meshes.count;j++){ufbx_mesh *m=s->meshes.data[j];triangles+=m->num_triangles;vertices+=m->num_vertices;valid &= m->vertex_uv.exists && m->vertex_normal.exists && m->skin_deformers.count==0 && m->num_triangles>0 && m->num_triangles<=3000;
  for(size_t i=0;i<m->num_vertices;i++){ufbx_vec3 p=m->vertices.data[i];valid &= isfinite(p.x)&&isfinite(p.y)&&isfinite(p.z);}}
 printf("{\"triangles\":%zu,\"vertices\":%zu,\"materials\":%zu,\"passed\":%s,\"native_unity\":\"NOT RUN\"}\n",triangles,vertices,s->materials.count,valid?"true":"false");ufbx_free_scene(s);return valid?0:1;
}
