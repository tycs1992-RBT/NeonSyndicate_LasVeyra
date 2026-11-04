/* Strict independent parsing of Living Streets geometry, including exported tangent frames. Not Unity. */
#include "ufbx.h"
#include <math.h>
#include <stdio.h>
static int unit(ufbx_vec3 v){double length=sqrt(v.x*v.x+v.y*v.y+v.z*v.z);return isfinite(length)&&length>.98&&length<1.02;}
int main(int argc,char **argv){
    if(argc!=2)return 2;ufbx_load_opts options={0};options.strict=true;options.target_unit_meters=1;
    ufbx_error error;ufbx_scene *scene=ufbx_load_file(argv[1],&options,&error);
    if(!scene){char message[2048];ufbx_format_error(message,sizeof(message),&error);fprintf(stderr,"%s",message);return 1;}
    int valid=scene->meshes.count>0&&scene->bones.count==0&&scene->anim_stacks.count==0;
    size_t vertices=0,triangles=0;double radius=0;
    for(size_t j=0;j<scene->meshes.count;j++){
        ufbx_mesh *mesh=scene->meshes.data[j];vertices+=mesh->num_vertices;triangles+=mesh->num_triangles;
        valid&=mesh->vertex_uv.exists&&mesh->vertex_normal.exists&&mesh->vertex_tangent.exists&&mesh->vertex_bitangent.exists&&mesh->skin_deformers.count==0&&mesh->num_faces==mesh->num_triangles;
        for(size_t i=0;i<mesh->num_vertices;i++){ufbx_vec3 p=mesh->vertices.data[i];if(mesh->instances.count)p=ufbx_transform_position(&mesh->instances.data[0]->geometry_to_world,p);double d=sqrt(p.x*p.x+p.y*p.y+p.z*p.z);valid&=isfinite(d);if(d>radius)radius=d;}
        for(size_t i=0;i<mesh->vertex_normal.values.count;i++)valid&=unit(mesh->vertex_normal.values.data[i]);
        for(size_t i=0;i<mesh->vertex_tangent.values.count;i++)valid&=unit(mesh->vertex_tangent.values.data[i]);
        for(size_t i=0;i<mesh->vertex_bitangent.values.count;i++)valid&=unit(mesh->vertex_bitangent.values.data[i]);
        for(size_t i=0;i<mesh->vertex_uv.values.count;i++)valid&=isfinite(mesh->vertex_uv.values.data[i].x)&&isfinite(mesh->vertex_uv.values.data[i].y);
    }
    valid&=vertices>=3&&triangles>=1&&radius<45;
    printf("{\"vertices\":%zu,\"triangles\":%zu,\"maximum_radius_m\":%.9g,\"meshes\":%zu,\"bones\":%zu,\"passed\":%s}\n",vertices,triangles,radius,scene->meshes.count,scene->bones.count,valid?"true":"false");
    ufbx_free_scene(scene);return valid?0:1;
}
