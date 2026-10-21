/* Actual independent skin/matrix/pose comparison and optional model export. Never Unity/native or art approval. */
#include "ufbx.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
static const ufbx_matrix ID={.cols={{1,0,0},{0,1,0},{0,0,1},{0,0,0}}};
static void string_json(FILE *f,ufbx_string s){fputc('"',f);for(size_t i=0;i<s.length;i++){char c=s.data[i];if(c=='"'||c=='\\')fputc('\\',f);if(c>=32)fputc(c,f);}fputc('"',f);}
static ufbx_scene *load(const char *path){ufbx_load_opts o={0};o.strict=true;o.target_unit_meters=1;ufbx_error e;ufbx_scene*s=ufbx_load_file(path,&o,&e);if(!s){char b[2048];ufbx_format_error(b,sizeof(b),&e);fprintf(stderr,"%s\n",b);}return s;}
static double angle(ufbx_string name,int pose){const char*n=name.data;int arm=strstr(n,"upperarm_")!=NULL,elbow=strstr(n,"lowerarm_")!=NULL,knee=strstr(n,"calf_")!=NULL,thigh=strstr(n,"thigh_")!=NULL,finger=strstr(n,"_01_")||strstr(n,"_02_")||strstr(n,"_03_");
 if(pose==1 && arm)return strstr(n,"_l")?60:-60;
 if(pose==2 && elbow)return 95;if(pose==3 && thigh)return 45;if(pose==3 && knee)return -85;
 if(pose==4 && strcmp(n,"spine_03")==0)return 30;if(pose==5 && finger)return 45;
 if(pose==6 && arm)return strstr(n,"_l")?90:-90;if(pose==7 && strcmp(n,"Head")==0)return 40;return 0;}
static ufbx_matrix delta(ufbx_node *bone,int pose){ufbx_node *chain[160];size_t count=0;while(bone && !bone->is_root && count<160){chain[count++]=bone;bone=bone->parent;}ufbx_matrix result=ID;
 for(size_t i=count;i>0;i--){ufbx_node*n=chain[i-1];double a=angle(n->name,pose)*3.14159265358979323846/180.;if(!a)continue;ufbx_vec3 p=n->node_to_world.cols[3];ufbx_matrix r=ID;r.cols[0].x=cos(a);r.cols[0].y=sin(a);r.cols[1].x=-sin(a);r.cols[1].y=cos(a);ufbx_vec3 rp=ufbx_transform_direction(&r,p);r.cols[3]=(ufbx_vec3){p.x-rp.x,p.y-rp.y,p.z-rp.z};result=ufbx_matrix_mul(&result,&r);}
 return result;}
static ufbx_vec3 point(ufbx_mesh*m,size_t v,int pose){ufbx_skin_deformer*s=m->skin_deformers.data[0];ufbx_skin_vertex sv=s->vertices.data[v];ufbx_vec3 out={0};double sum=0;
 for(size_t i=0;i<sv.num_weights;i++){ufbx_skin_weight w=s->weights.data[sv.weight_begin+i];ufbx_skin_cluster*c=s->clusters.data[w.cluster_index];ufbx_matrix d=delta(c->bone_node,pose);ufbx_matrix a=ufbx_matrix_mul(&d,&c->geometry_to_world);ufbx_vec3 p=ufbx_transform_position(&a,m->vertices.data[v]);out.x+=w.weight*p.x;out.y+=w.weight*p.y;out.z+=w.weight*p.z;sum+=w.weight;}
 if(sum<=0)return (ufbx_vec3){NAN,NAN,NAN};out.x/=sum;out.y/=sum;out.z/=sum;return out;}
static void vec(FILE*f,ufbx_vec3 p){fprintf(f,"[%.9g,%.9g,%.9g]",p.x,p.y,p.z);}
static int export_model(ufbx_scene*s,const char*name){FILE*f=fopen(name,"w");if(!f)return 0;fputs("{\"scope\":\"Actual received/derived FBX geometry, offline review only\",\"meshes\":[",f);
 for(size_t k=0;k<s->meshes.count;k++){ufbx_mesh*m=s->meshes.data[k];if(k)fputc(',',f);fputs("{\"name\":",f);string_json(f,m->name);fputs(",\"positions\":[",f);for(size_t v=0;v<m->num_vertices;v++){if(v)fputc(',',f);vec(f,point(m,v,0));}fputs("],\"triangles\":[",f);int comma=0;
 for(size_t a=0;a<m->faces.count;a++){ufbx_face face=m->faces.data[a];uint32_t*indices=malloc((face.num_indices+1)*3*sizeof(uint32_t));size_t n=ufbx_triangulate_face(indices,(face.num_indices+1)*3,m,face);for(size_t t=0;t<n;t++){if(comma++)fputc(',',f);fputs("{\"i\":[",f);for(int q=0;q<3;q++){if(q)fputc(',',f);fprintf(f,"%u",m->vertex_indices.data[indices[t*3+q]]);}fputs("],\"uv\":[",f);for(int q=0;q<3;q++){if(q)fputc(',',f);ufbx_vec2 uv=ufbx_get_vertex_vec2(&m->vertex_uv,indices[t*3+q]);fprintf(f,"[%.9g,%.9g]",uv.x,uv.y);}fputs("],\"normals\":[",f);for(int q=0;q<3;q++){if(q)fputc(',',f);ufbx_vec3 normal=ufbx_get_vertex_vec3(&m->vertex_normal,indices[t*3+q]);if(m->instances.count)normal=ufbx_transform_direction(&m->instances.data[0]->geometry_to_world,normal);double length=sqrt(normal.x*normal.x+normal.y*normal.y+normal.z*normal.z);if(length>0){normal.x/=length;normal.y/=length;normal.z/=length;}vec(f,normal);}fputs("],\"material\":",f);size_t mat=m->face_material.count?m->face_material.data[a]:0;if(mat<m->materials.count)string_json(f,m->materials.data[mat]->name);else fputs("\"\"",f);fputc('}',f);}free(indices);}fputs("]}",f);}
 fputs("],\"bones\":[",f);int comma=0;for(size_t i=0;i<s->nodes.count;i++){ufbx_node*n=s->nodes.data[i];if(!n->bone)continue;if(comma++)fputc(',',f);fputs("{\"name\":",f);string_json(f,n->name);fputs(",\"position\":",f);vec(f,n->node_to_world.cols[3]);fputc('}',f);}fputs("]}\n",f);fclose(f);return 1;}
int main(int argc,char**argv){if(argc<3)return 2;ufbx_scene*a=load(argv[1]),*b=load(argv[2]);if(!a||!b)return 1;if(a->meshes.count!=b->meshes.count||a->bones.count!=b->bones.count)return 1;
 double maximum=0;size_t samples=0;int unchanged=1;printf("{\"scope\":\"Independent source skin, actual bind matrices and eight deterministic joint-stress poses. Not authored animation, Unity import or owner appearance approval\",\"meshes\":[");
 for(size_t m=0;m<a->meshes.count;m++){ufbx_mesh*x=a->meshes.data[m],*y=b->meshes.data[m];if(m)putchar(',');if(x->num_vertices!=y->num_vertices||x->num_triangles!=y->num_triangles)return 1;ufbx_skin_deformer*s=x->skin_deformers.data[0],*t=y->skin_deformers.data[0];if(s->skinning_method!=t->skinning_method||s->skinning_method!=UFBX_SKINNING_METHOD_LINEAR||s->clusters.count!=t->clusters.count)return 1;
 for(size_t i=0;i<s->clusters.count;i++){ufbx_skin_cluster*c=s->clusters.data[i],*d=t->clusters.data[i];if(strcmp(c->bone_node->name.data,d->bone_node->name.data)||memcmp(&c->geometry_to_bone,&d->geometry_to_bone,sizeof(ufbx_matrix)))unchanged=0;}
 double error=0,squares=0;size_t count=0;for(int pose=0;pose<8;pose++)for(size_t v=0;v<x->num_vertices;v++){ufbx_vec3 p=point(x,v,pose),q=point(y,v,pose);double e=sqrt((p.x-q.x)*(p.x-q.x)+(p.y-q.y)*(p.y-q.y)+(p.z-q.z)*(p.z-q.z));if(!isfinite(e))return 1;if(e>error)error=e;squares+=e*e;count++;}
 if(error>maximum)maximum=error;samples+=count;printf("{\"mesh\":");string_json(stdout,x->name);printf(",\"samples\":%zu,\"maximum_error_metres\":%.12g,\"rms_error_metres\":%.12g}",count,error,sqrt(squares/count));}
 int passed=unchanged&&maximum<=.005;printf("],\"samples\":%zu,\"binds_and_bone_names_unchanged\":%s,\"maximum_error_metres\":%.12g,\"acceptance_bound_metres\":0.005,\"passed\":%s,\"native_unity\":\"NOT RUN\"}\n",samples,unchanged?"true":"false",maximum,passed?"true":"false");
 if(argc>3&&!export_model(b,argv[3]))return 1;ufbx_free_scene(a);ufbx_free_scene(b);return passed?0:1;}
