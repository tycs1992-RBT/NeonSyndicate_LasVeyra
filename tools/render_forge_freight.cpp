// Reuse the district depth-buffer camera, with rigid production joint transforms sampled in Mono.
#define main district_camera_main
#include "render_science_circuit.cpp"
#undef main
struct Piece {int joint; V color; std::vector<V> p,n;std::vector<int> triangles;};
struct Pose {V p,x,y,z;V rotate(V v)const{return x*v.x+y*v.y+z*v.z;}};
static Vertex vertex(V p,V n){return {p,n,0,0};}
static Texture solid(V color){
    Texture t;t.w=t.h=1;t.average=color;t.rgb={(unsigned char)color.x,(unsigned char)color.y,(unsigned char)color.z};return t;
}
static void flat(Camera&cam,V a,V b,V c,V d,V color) {
    Texture t=solid(color);V n=unit(cross(b-a,c-a));if(n.y<0)n=n*-1;
    cam.triangle(vertex(a,n),vertex(b,n),vertex(c,n),t,{1,1,1});
    cam.triangle(vertex(a,n),vertex(c,n),vertex(d,n),t,{1,1,1});
}
int main(int argc,char**argv){
    if(argc!=3)return 2;
    std::string dir=argv[1];std::ifstream model(dir+"/team.bin",std::ios::binary),motion(dir+"/motion.bin",std::ios::binary);
    std::vector<Piece> pieces(read<int>(model));
    for(auto& part:pieces){
        part.joint=read<int>(model);part.color=read<V>(model)*255;int n=read<int>(model);
        for(int i=0;i<n;i++){part.p.push_back(read<V>(model));part.n.push_back(read<V>(model));}
        part.triangles.resize(read<int>(model));model.read((char*)part.triangles.data(),part.triangles.size()*4);
    }
    int frames=read<int>(motion),bones=read<int>(motion);std::vector<Pose> poses(bones);
    for(int frame=0;frame<frames;frame++){
        V animal=read<V>(motion),axle=read<V>(motion),crossing=read<V>(motion);
        for(auto& pose:poses)pose=read<Pose>(motion);
        V track=animal*.55f+axle*.45f;
        Camera cam(track+V{2.5f,2.08f,-6.45f},track+V{0,1.28f,0});
        cam.sun={.27f,.84f,-.68f};
        flat(cam,{-100,-.10f,-190},{170,-.10f,-190},{170,-.10f,-117},{-100,-.10f,-117},{143,141,121});
        flat(cam,{-100,-.03f,-156.1f},{170,-.03f,-156.1f},{170,-.03f,-143.9f},{-100,-.03f,-143.9f},{114,111,103});
        for(int k=-70;k<160;k+=5)
            flat(cam,{float(k),0,-150.04f},{float(k+2),0,-150.04f},{float(k+2),0,-149.96f},{float(k),0,-149.96f},{178,167,144});
        // Project a daylight contact shadow onto the simple evidence road.
        Texture shade=solid({72,69,62});
        for(const auto& part:pieces){
            const Pose& pose=poses[part.joint];std::vector<Vertex> shadow;
            for(V v:part.p){
                V p=pose.p+pose.rotate(v);p=p-cam.sun*(p.y/cam.sun.y);p.y=.006f;
                shadow.push_back(vertex(p,{0,1,0}));
            }
            for(size_t i=0;i<part.triangles.size();i+=3)
                cam.triangle(shadow[part.triangles[i]],shadow[part.triangles[i+1]],shadow[part.triangles[i+2]],shade,{1,1,1});
        }
        for(const auto& part:pieces){
            Pose pose=poses[part.joint];Texture tex=solid(part.color);std::vector<Vertex> nodes(part.p.size());
            for(size_t i=0;i<nodes.size();i++){
                V n=part.n[i];V normal=unit(pose.x*(n.x/dot(pose.x,pose.x))+pose.y*(n.y/dot(pose.y,pose.y))+pose.z*(n.z/dot(pose.z,pose.z)));
                nodes[i]=vertex(pose.p+pose.rotate(part.p[i]),normal);
            }
            for(size_t i=0;i<part.triangles.size();i+=3){
                Vertex a=nodes[part.triangles[i]],b=nodes[part.triangles[i+1]],c=nodes[part.triangles[i+2]];
                if(dot(cross(b.p-a.p,c.p-a.p),cam.eye-a.p)>0)cam.triangle(a,b,c,tex,{1,1,1});
            }
        }
        if(crossing.y>0){
            // A copper ground disc locates the injected obstacle; it is not a rendered game character.
            Texture t=solid({218,150,84});V centre{crossing.x,.016f,crossing.z},n{0,1,0};
            for(int j=0;j<32;j++){
                float a=j*.19634954f,b=(j+1)*.19634954f;
                cam.triangle(vertex(centre,n),vertex(centre+V{.37f*std::cos(a),0,.37f*std::sin(a)},n),vertex(centre+V{.37f*std::cos(b),0,.37f*std::sin(b)},n),t,{1,1,1});
            }
        }
        char name[32];std::snprintf(name,sizeof(name),"/frame-%03d.ppm",frame);
        std::ofstream out(std::string(argv[2])+name,std::ios::binary);out<<"P6\n"<<cam.w<<" "<<cam.h<<"\n255\n";out.write((char*)cam.rgb.data(),cam.rgb.size());
    }
}
