#!/usr/bin/env python3
"""Read-only bounded archive intake. Never runs bundled code or imports an entire package."""
import argparse, hashlib, json, stat, zipfile
from pathlib import Path, PurePosixPath

def inspect(path):
    rows=[]; licenses=[]; total=0
    with zipfile.ZipFile(path) as archive:
        for item in archive.infolist():
            name=PurePosixPath(item.filename.replace('\\','/')); total+=item.file_size
            if name.is_absolute() or '..' in name.parts or ':' in str(name) or stat.S_ISLNK(item.external_attr>>16):
                raise ValueError('Unsafe archive path: '+item.filename)
            if item.file_size>256*1024*1024 or (item.file_size>1024*1024 and item.file_size/max(1,item.compress_size)>200):
                raise ValueError('Unbounded archive member: '+item.filename)
            if total>2*1024*1024*1024: raise ValueError('Archive exceeds admitted staging budget')
            row={'path':item.filename,'bytes':item.file_size,'crc32':item.CRC,'executed':False}
            rows.append(row)
            if 'license' in item.filename.lower() and item.file_size<100000:
                data=archive.read(item); text=data.decode('utf8','strict')
                licenses.append({'path':item.filename,'sha256':hashlib.sha256(data).hexdigest(),'cc0_dedication_present':'CC0 1.0 Universal' in text and 'Public Domain Dedication' in text})
    return {'archive':path.name,'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'uncompressed_bytes':total,'entries':rows,'licenses':licenses,'scripts_executed':False,'scope':'Inventory only. Each selected file needs an explicit admission and dependency record.'}

if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('archives',nargs='+',type=Path); parser.add_argument('--output',type=Path,required=True); args=parser.parse_args()
    records=[inspect(p) for p in args.archives]
    args.output.parent.mkdir(parents=True,exist_ok=True); args.output.write_text(json.dumps({'schema':1,'archives':records,'native_unity':'NOT RUN'},indent=2)+'\n')
    print(json.dumps({'archives':len(records),'files':sum(len(r['entries']) for r in records),'unsafe_paths':0,'executed_bundled_scripts':0}))
