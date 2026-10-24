/* Independently inspect actual static FBX geometry. This does not execute Unity or prove navigation. */
#include "ufbx.h"
#include <math.h>
#include <stdio.h>
int main(int argc,char **argv) {
 if(argc!=2)return 2;ufbx_load_opts o={0};o.strict=true;o.target_unit_meters=1;ufbx_error e;ufbx_scene *s=ufbx_load_file(argv[1],&o,&e);
 if(!s){char message[2048];ufbx_format_error(message,sizeof(message),&e);fprintf(stderr,"%s",message);return 1;}
 int valid=s->meshes.count>0 && s->bones.count==0 && s->anim_stacks.count==0;size_t vertices=0,triangles=0;double largest=0;
 for(size_t j=0;j<s->meshes.count;j++) {
  ufbx_mesh *m=s->meshes.data[j];vertices+=m->num_vertices;triangles+=m->num_triangles;valid&=m->vertex_uv.exists && m->vertex_normal.exists && m->skin_deformers.count==0;
  for(size_t i=0;i<m->num_vertices;i++) {ufbx_vec3 p=m->vertices.data[i];if(m->instances.count)p=ufbx_transform_position(&m->instances.data[0]->geometry_to_world,p);double r=sqrt(p.x*p.x+p.y*p.y+p.z*p.z);valid&=isfinite(r);if(r>largest)largest=r;}
 }
 valid&=vertices>100 && triangles>100 && largest<20;
 printf("{\"scope\":\"Independent actual static FBX, not native collision or appearance\",\"meshes\":%zu,\"vertices\":%zu,\"triangles\":%zu,\"bones\":%zu,\"clips\":%zu,\"maximum_world_radius_metres\":%.9g,\"passed\":%s,\"native_unity\":\"NOT RUN\"}\n",s->meshes.count,vertices,triangles,s->bones.count,s->anim_stacks.count,largest,valid?"true":"false");ufbx_free_scene(s);return valid?0:1;
}
