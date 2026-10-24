/* Independent actual authored FBX action inspection. No Unity or gameplay execution. */
#include "ufbx.h"
#include <stdio.h>
#include <math.h>
#include <string.h>
int main(int argc,char **argv) {
 if(argc!=2)return 2;ufbx_load_opts opts={0};opts.strict=true;opts.target_unit_meters=1;ufbx_error error;ufbx_scene *s=ufbx_load_file(argv[1],&opts,&error);
 if(!s){char text[2048];ufbx_format_error(text,sizeof(text),&error);fprintf(stderr,"%s",text);return 1;}
 const char *names[]={"H7_IdleBreath","H7_Walk","H7_Run","H7_SwordReady","H7_OathCut"};int seen[5]={0};int pass=s->bones.count==53 && s->meshes.count==0 && s->anim_stacks.count==5;size_t samples=0;double max_root=0;
 ufbx_node *root=ufbx_find_node(s,"root");if(!root)pass=0;
 printf("{\"scope\":\"Actual FBX action sampling, NOT native Unity playback\",\"bones\":%zu,\"clips\":[",s->bones.count);
 for(size_t a=0;a<s->anim_stacks.count;a++) {
  ufbx_anim_stack *stack=s->anim_stacks.data[a];int found=-1;for(int i=0;i<5;i++)if(strstr(stack->name.data,names[i]))found=i;
  if(found<0)pass=0;else seen[found]++;
  double duration=stack->time_end-stack->time_begin;if(!(duration>.2 && duration<=4.01))pass=0;
  if(a)putchar(',');printf("{\"name\":\"%s\",\"duration\":%.9g,\"frames_sampled\":%d}",stack->name.data,duration,(int)floor(duration*30+.5)+1);
  ufbx_vec3 initial={0};if(root)initial=ufbx_evaluate_transform(stack->anim,root,stack->time_begin).translation;
  for(int frame=0;frame<=(int)floor(duration*30+.5);frame++) {
   double t=stack->time_begin+frame/30.0;
   for(size_t b=0;b<s->nodes.count;b++) {
    ufbx_node *n=s->nodes.data[b];if(!n->bone)continue;ufbx_transform v=ufbx_evaluate_transform(stack->anim,n,t);samples++;
    if(!isfinite(v.rotation.x+v.rotation.y+v.rotation.z+v.rotation.w+v.translation.x+v.translation.y+v.translation.z))pass=0;
    if(n==root) {double x=v.translation.x-initial.x,y=v.translation.y-initial.y,z=v.translation.z-initial.z;double d=sqrt(x*x+y*y+z*z);if(d>max_root)max_root=d;}
   }
  }
 }
 for(int i=0;i<5;i++)if(seen[i]!=1)pass=0;if(max_root>.00001)pass=0;
 printf("],\"bone_samples\":%zu,\"maximum_root_translation_metres\":%.9g,\"passed\":%s,\"native_unity\":\"NOT RUN\"}\n",samples,max_root,pass?"true":"false");ufbx_free_scene(s);return pass?0:1;
}
