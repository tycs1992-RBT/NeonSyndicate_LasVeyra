#!/usr/bin/env python3
"""Non-destructive binary FBX linear-skin derivation. Originals, bind/mesh data and failed raw gates are retained."""
import argparse,copy,hashlib,json,math,struct,zlib
from dataclasses import dataclass
from pathlib import Path

@dataclass
class Property:
    kind: str
    value: object
    raw: bytes
    changed: bool=False

@dataclass
class Node:
    name: str
    props: list
    children: list
    terminated: bool

class Document:
    def __init__(self,data):
        if data[:23]!=b'Kaydara FBX Binary  \x00\x1a\x00':raise ValueError('Not binary FBX')
        self.header=data[:27];self.version=struct.unpack_from('<I',data,23)[0]
        self.wide=self.version>=7500;self.fmt='<QQQB'if self.wide else'<IIIB';self.hsize=25 if self.wide else 13
        self.nodes=[];at=27
        while data[at:at+self.hsize]!=bytes(self.hsize):
            node,at=self.read(data,at);self.nodes.append(node)
        self.footer=data[at:]
    def read(self,data,at):
        end,count,length,n=struct.unpack_from(self.fmt,data,at);at+=self.hsize
        name=data[at:at+n].decode();at+=n;props=[];pstart=at
        scalar={'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}
        arrays={'f':'f','d':'d','l':'q','i':'i','b':'?','c':'B'}
        for unused in range(count):
            begin=at;kind=chr(data[at]);at+=1
            if kind in scalar:
                form='<'+scalar[kind];value=struct.unpack_from(form,data,at)[0];at+=struct.calcsize(form)
            elif kind in ['S','R']:
                size=struct.unpack_from('<I',data,at)[0];at+=4;value=data[at:at+size];at+=size
            elif kind in arrays:
                size,encoding,packed=struct.unpack_from('<III',data,at);at+=12;raw=data[at:at+packed];at+=packed
                if encoding not in [0,1]:raise ValueError('Unsupported FBX array encoding')
                if encoding:raw=zlib.decompress(raw)
                value=list(struct.unpack('<'+str(size)+arrays[kind],raw))
            else:raise ValueError('Unknown FBX property '+kind)
            props.append(Property(kind,value,data[begin:at]))
        if at-pstart!=length:raise ValueError('FBX property length mismatch')
        children=[];terminated=False
        while at<end:
            if data[at:at+self.hsize]==bytes(self.hsize):at+=self.hsize;terminated=True;break
            child,at=self.read(data,at);children.append(child)
        if at!=end:raise ValueError('FBX node boundary mismatch '+name)
        return Node(name,props,children,terminated),at
    def encode(self):
        def prop(p):
            if not p.changed:return p.raw
            form={'d':'d','f':'f','i':'i','l':'q'}[p.kind];raw=struct.pack('<'+str(len(p.value))+form,*p.value);packed=zlib.compress(raw,9)
            return p.kind.encode()+struct.pack('<III',len(p.value),1,len(packed))+packed
        def node(n,offset):
            name=n.name.encode();properties=b''.join(prop(p)for p in n.props);begin=offset+self.hsize+len(name)+len(properties);children=b''
            for child in n.children:children+=node(child,begin+len(children))
            if n.terminated:children+=bytes(self.hsize)
            return struct.pack(self.fmt,begin+len(children),len(n.props),len(properties),len(name))+name+properties+children
        data=self.header
        for n in self.nodes:data+=node(n,len(data))
        # The FBX version field in the footer follows zero padding to a 16-byte boundary.
        prefix=self.footer[:self.hsize+20];mark=struct.pack('<I',self.version);index=self.footer.find(mark,self.hsize+20,self.hsize+52)
        if index<0 or any(self.footer[self.hsize+16:index]):raise ValueError('Unsupported FBX footer layout')
        return data+prefix+bytes((-(len(data)+len(prefix)))%16)+self.footer[index:]

def child(node,name):
    found=[n for n in node.children if n.name==name]
    if len(found)!=1:raise ValueError('Missing/ambiguous FBX field '+name)
    return found[0]

def derive(data,limit=4):
    if limit not in [4,8]:raise ValueError('Only explicit four/eight influence derivations supported')
    doc=Document(data);objects=next(n for n in doc.nodes if n.name=='Objects');connections=next(n for n in doc.nodes if n.name=='Connections')
    ids={n.props[0].value:n for n in objects.children};parents={}
    for c in connections.children:
        if len(c.props)>=3 and c.props[0].value==b'OO':parents.setdefault(c.props[1].value,[]).append(c.props[2].value)
    groups={};reports=[]
    for node in objects.children:
        if node.name!='Deformer'or len(node.props)<3 or node.props[2].value!=b'Cluster':continue
        parent=parents.get(node.props[0].value,[])
        if len(parent)!=1:raise ValueError('Ambiguous cluster skin owner')
        groups.setdefault(parent[0],[]).append(node)
    if not groups:raise ValueError('No skin clusters')
    for sid,clusters in groups.items():
        skin=ids[sid];geometry_ids=parents.get(sid,[])
        if len(geometry_ids)!=1:raise ValueError('Shared/ambiguous skin geometry')
        geom=ids[geometry_ids[0]];count=len(child(geom,'Vertices').props[0].value)//3
        if geom.name!='Geometry':raise ValueError('Skin owner is not a geometry')
        mode=[n for n in skin.children if n.name=='SkinningType']
        if mode and mode[0].props[0].value not in [b'Linear',b'Rigid']:raise ValueError('Nonlinear skin requires an authored repair')
        vertices=[[]for unused in range(count)]
        for c in clusters:
            link=[n for n in c.children if n.name=='Mode']
            if link and link[0].props[0].value!=b'Normalize':raise ValueError('Additive/total-one cluster cannot be normalized blindly')
            if not any(n.name in ['Indexes','Weights']for n in c.children):continue
            indices=child(c,'Indexes').props[0].value;weights=child(c,'Weights').props[0].value
            if len(indices)!=len(weights)or len(indices)!=len(set(indices)):raise ValueError('Mismatched/duplicate cluster data')
            for index,weight in zip(indices,weights):
                if index<0 or index>=count or not math.isfinite(weight)or weight<0:raise ValueError('Invalid skin influence')
                if weight>0:vertices[index].append((c.props[0].value,weight))
        replacement={c.props[0].value:[]for c in clusters};maximum_error=0.;maximum_loss=0.;pruned=0;maximum_before=0
        for index,influences in enumerate(vertices):
            total=math.fsum(w for bone,w in influences)
            if total<=1e-12:raise ValueError('Zero-total weighted vertex')
            maximum_before=max(maximum_before,len(influences));maximum_error=max(maximum_error,abs(total-1))
            ranked=sorted(influences,key=lambda p:(-p[1],p[0]));retained=ranked[:limit];kept=math.fsum(w for bone,w in retained)
            loss=1-kept/total;maximum_loss=max(maximum_loss,loss);pruned+=len(ranked)-len(retained)
            for bone,weight in retained:replacement[bone].append((index,weight/kept))
        for c in clusters:
            values=replacement[c.props[0].value]
            if not any(n.name in ['Indexes','Weights']for n in c.children):
                if values:raise ValueError('Unexpected weights on an empty source cluster')
                continue
            for field,values2 in [('Indexes',[v[0]for v in values]),('Weights',[v[1]for v in values])]:
                p=child(c,field).props[0];p.value=values2;p.changed=True
        reports.append({'geometry_id':geometry_ids[0],'geometry_name':geom.props[1].value.decode(errors='replace').split('\x00')[0],'vertices':count,'raw_maximum_sum_error':maximum_error,'raw_maximum_influences':maximum_before,'maximum_discarded_normalized_mass':maximum_loss,'pruned_influences':pruned,'derived_limit':limit})
    derived=doc.encode()
    # Every property except actual skin Indexes/Weights bytes stays identical, including all original geometry/UVs/bind matrices.
    original=Document(data);again=Document(derived)
    def fingerprint(nodes,parent='',skin_cluster=False):
        out=[]
        for n in nodes:
            identity=n.props[0].value if n.props and n.props[0].kind=='L'else'';path=parent+'/'+n.name+str(identity)
            if not(skin_cluster and n.name in ['Indexes','Weights']):out.append((path,[p.raw.hex()for p in n.props]))
            is_cluster=n.name=='Deformer'and len(n.props)>=3 and n.props[2].value==b'Cluster'
            out+=fingerprint(n.children,path,is_cluster)
        return out
    if fingerprint(original.nodes)!=fingerprint(again.nodes):raise ValueError('Unexpected non-skin transformation')
    return derived,{'scope':'Deterministic linear-skin derivation, not native import, appearance approval or motion acceptance','ranking':'weight descending, source cluster ID ascending; preserve top N; divide each retained positive influence by their exact sum','original_sha256':hashlib.sha256(data).hexdigest(),'derived_sha256':hashlib.sha256(derived).hexdigest(),'non_skin_properties_unchanged':True,'meshes':reports,'native_unity':'NOT RUN','owner_art_approved':False}

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);p.add_argument('--output',type=Path,required=True);p.add_argument('--influences',type=int,choices=[4,8],default=4);args=p.parse_args()
    source=args.root/'Assets/NeonSyndicate/Art/CandidatesH3';manifest=json.loads((source/'candidate.json').read_text());reports=[];args.output.mkdir(parents=True,exist_ok=True)
    for row in manifest['files']:
        if not row['path'].endswith('.fbx'):continue
        path=args.root/row['path'];data=path.read_bytes()
        if hashlib.sha256(data).hexdigest()!=row['sha256']:raise ValueError('Original source fingerprint changed')
        derived,report=derive(data,args.influences);target=args.output/path.name;target.write_bytes(derived);report.update(source=row['path'],output=str(target));reports.append(report)
    (args.output/'skin-lineage.json').write_text(json.dumps({'creator':'Quaternius','license':'CC0-1.0','original_manifest':'Assets/NeonSyndicate/Art/CandidatesH3/candidate.json','state':'DERIVED TECHNICAL REFERENCE, NOT FINAL MALIK','owner_appearance_approved':False,'registry_enabled':False,'native_import':'NOT RUN','models':reports},indent=2)+'\n')
    print(json.dumps({'models':len(reports),'max_pruned_mass':max(m['maximum_discarded_normalized_mass']for r in reports for m in r['meshes']),'originals_unchanged':True}))
if __name__=='__main__':main()
