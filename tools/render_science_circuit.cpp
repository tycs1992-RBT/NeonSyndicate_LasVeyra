// Offline perspective camera for exported production meshes. No Unity API, native lighting or physics.
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>
struct V {
    float x,y,z;
    V operator+(V b)const {
        return{x+b.x,y+b.y,z+b.z};
    }
    V operator-(V b)const {
        return{x-b.x,y-b.y,z-b.z};
    }
    V operator*(float s)const {
        return{x*s,y*s,z*s};
    }
};
static float dot(V a,V b) {
    return a.x*b.x+a.y*b.y+a.z*b.z;
}
static V cross(V a,V b) {
    return{a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x};
}
static V unit(V v) {
    return v*(1.f/std::max(.000001f,std::sqrt(dot(v,v))));
}
struct Vertex {
    V p,n;
    float u,v;
};
struct Projected {
    float x,y,z,u,v,light;
};
struct Texture {
    int w=0,h=0;
    std::vector<unsigned char> rgb;
    V average{0,0,0};
};
static Texture texture(std::string root,std::string name) {
    Texture t;
    std::ifstream f(root+"/"+name+".ppm",std::ios::binary);
    if(!f)return t;
    std::string magic;
    int maxval;
    f>>magic>>t.w>>t.h>>maxval;
    f.get();
    if(magic!="P6"||maxval!=255)throw std::runtime_error("Bad texture");
    t.rgb.resize(3*t.w*t.h);
    f.read((char*)t.rgb.data(),t.rgb.size());
    for(size_t i=0;i<t.rgb.size();i+=3)t.average=t.average+V{float(t.rgb[i]),float(t.rgb[i+1]),float(t.rgb[i+2])}*(1.f/(t.w*t.h));
    return t;
}
static V sample(const Texture&t,float u,float v,float footprint) {
    if(t.rgb.empty())return{172,170,159};
    u-=std::floor(u);
    v-=std::floor(v);
    int k=3*((int((1-v)*t.h)%t.h)*t.w+int(u*t.w)%t.w);
    V c{float(t.rgb[k]),float(t.rgb[k+1]),float(t.rgb[k+2])};
    float blend=std::min(.94f,footprint*.25f);
    return c*(1-blend)+t.average*blend;
}
template<class T>static T read(std::istream&f) {
    T value;
    f.read((char*)&value,sizeof(T));
    if(!f)throw std::runtime_error("Truncated mesh");
    return value;
}
static Vertex mix(Vertex a,Vertex b,float t) {
    return {a.p+(b.p-a.p)*t,a.n+(b.n-a.n)*t,a.u+(b.u-a.u)*t,a.v+(b.v-a.v)*t};
}
struct Camera {
    int w=960,h=540;
    V eye,forward,right,up;
    V sun{-.37f,.85f,.42f};
    float focal;
    std::vector<float> depth;
    std::vector<unsigned char> rgb;
    Camera(V from,V at):eye(from),forward(unit(at-from)),right(unit(cross(V{0,1,0},forward))),up(cross(forward,right)),focal(h*.5f/std::tan(.51f)),depth(w*h,1e9f),rgb(3*w*h) {
        for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
            float t=float(y)/h;
            int k=3*(y*w+x);
            rgb[k]=112+67*t;
            rgb[k+1]=143+42*t;
            rgb[k+2]=164+20*t;
        }
    }
    float z(Vertex v)const {
        return dot(v.p-eye,forward);
    }
    Projected project(Vertex v) {
        float d=z(v);
        return{w*.5f+focal*dot(v.p-eye,right)/d,h*.5f-focal*dot(v.p-eye,up)/d,d,v.u,v.v,.53f+.54f*std::max(0.f,dot(unit(v.n),unit(sun)))};
    }
    void triangle(Vertex a,Vertex b,Vertex c,const Texture&t,V tint) {
        Vertex p[3]={a,b,c};
        std::vector<Vertex> clipped;
        for(int i=0;i<3;i++) {
            Vertex prev=p[(i+2)%3],cur=p[i];
            float x=z(prev),y=z(cur);
            if((x>=.12f)!=(y>=.12f))clipped.push_back(mix(prev,cur,(.12f-x)/(y-x)));
            if(y>=.12f)clipped.push_back(cur);
        }
        for(size_t i=1;i+1<clipped.size();i++)raster(project(clipped[0]),project(clipped[i]),project(clipped[i+1]),t,tint);
    }
    void raster(Projected a,Projected b,Projected c,const Texture&t,V tint) {
        float den=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);
        if(std::abs(den)<1e-5f)return;
        int left=std::max(0,int(std::floor(std::min({a.x,b.x,c.x})))),rightX=std::min(w-1,int(std::ceil(std::max({a.x,b.x,c.x}))));
        int top=std::max(0,int(std::floor(std::min({a.y,b.y,c.y})))),bottom=std::min(h-1,int(std::ceil(std::max({a.y,b.y,c.y}))));
        float scale=std::sqrt(std::abs((b.u-a.u)*(c.v-a.v)-(b.v-a.v)*(c.u-a.u))/std::abs(den))*t.w;
        for(int y=top;y<=bottom;y++)for(int x=left;x<=rightX;x++) {
            float aa=((b.y-c.y)*(x+.5f-c.x)+(c.x-b.x)*(y+.5f-c.y))/den,bb=((c.y-a.y)*(x+.5f-c.x)+(a.x-c.x)*(y+.5f-c.y))/den,cc=1-aa-bb;
            if(std::min({aa,bb,cc})<0)continue;
            float inv=aa/a.z+bb/b.z+cc/c.z,d=1/inv;
            int k=y*w+x;
            if(d>=depth[k])continue;
            depth[k]=d;
            aa*=d/a.z;
            bb*=d/b.z;
            cc*=d/c.z;
            V color=sample(t,aa*a.u+bb*b.u+cc*c.u,aa*a.v+bb*b.v+cc*c.v,scale);
            float light=aa*a.light+bb*b.light+cc*c.light,fog=std::min(.54f,1-std::exp(-.003f*d));
            V out=V{color.x*std::sqrt(tint.x*light),color.y*std::sqrt(tint.y*light),color.z*std::sqrt(tint.z*light)}*(1-fog)+V{178,185,182}*fog;
            rgb[3*k]=std::clamp(out.x,0.f,255.f);
            rgb[3*k+1]=std::clamp(out.y,0.f,255.f);
            rgb[3*k+2]=std::clamp(out.z,0.f,255.f);
        }
    }
};
int main(int argc,char**argv) {
    if(argc!=10)return 2;
    std::ifstream f(argv[1],std::ios::binary);
    if(read<int>(f)!=0x53463035)return 3;
    Camera cam({std::stof(argv[4]),std::stof(argv[5]),std::stof(argv[6])},{std::stof(argv[7]),std::stof(argv[8]),std::stof(argv[9])});
    int groups=read<int>(f);
    for(int g=0;g<groups;g++) {
        int length=read<unsigned char>(f);
        std::string role(length,' ');
        f.read(&role[0],length);
        int n=read<int>(f);
        std::vector<Vertex> vs(n);
        f.read((char*)vs.data(),sizeof(Vertex)*n);
        int m=read<int>(f);
        std::vector<int> index(m);
        f.read((char*)index.data(),4*m);
        std::string asset=role;
        V tint{1,1,1};
        if(role=="earth"||role=="retained-rock"||role=="ground"||role=="terrain")asset="basalt";
        if(role=="paving"||role=="flagstone"||role=="warm"||role=="forge-lens")asset="masonry";
        if(role=="retained-mineral")asset="azure";
        if(role=="retained-metal")asset="iron";
        if(role=="masonry")tint={.77f,.72f,.61f};
        if(role=="slate")tint={.46f,.57f,.55f};
        if(role=="earth")tint={.55f,.43f,.30f};
        if(role=="ground"||role=="terrain")tint={.70f,.77f,.54f};
        if(role=="amber-glass"){asset="plain-glass";tint={1.23f,.78f,.42f};}
        if(role=="forge-lens")tint={.65f,.29f,.08f};
        if(role=="warm")tint={1.65f,1.05f,.46f};
        if(role=="ground")asset="neutral-ground";
        Texture tex=texture(argv[2],asset);
        for(int i=0;i<m;i+=3) {
            Vertex a=vs[index[i]],b=vs[index[i+1]],c=vs[index[i+2]];
            if(dot(cross(b.p-a.p,c.p-a.p),cam.eye-a.p)<=0)continue;
            if(std::max({cam.z(a),cam.z(b),cam.z(c)})<.12f)continue;
            cam.triangle(a,b,c,tex,tint);
        }
    }
    std::ofstream out(argv[3],std::ios::binary);
    out<<"P6\n"<<cam.w<<" "<<cam.h<<"\n255\n";
    out.write((char*)cam.rgb.data(),cam.rgb.size());
    return !out;
}
