"""Matched 1.67 m eye-height reviews with production UVs and H2/H3/H5 albedo. Software camera, no Unity."""
import argparse, subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('before',type=Path);p.add_argument('after',type=Path);p.add_argument('output',type=Path);a=p.parse_args();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
tex=out/'albedo';tex.mkdir(exist_ok=True)
for file in (ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/Textures').glob('*_Albedo.png'):
    Image.open(file).convert('RGB').resize((256,256),Image.Resampling.LANCZOS).save(tex/(file.name.split('_Albedo')[0]+'.ppm'))
# The civic profile uses the original H3 masonry and basalt and H5 iron in preference to H2.
for source in ['AscensionH3/Textures/masonry_Albedo.jpg','AscensionH3/Textures/basalt_Albedo.jpg','AscensionH5/Textures/iron_Albedo.jpg']:
    file=ROOT/'Assets/NeonSyndicate/Resources'/source
    Image.open(file).convert('RGB').resize((256,256),Image.Resampling.LANCZOS).save(tex/(file.name.split('_Albedo')[0]+'.ppm'))
exe=out/'raster';subprocess.run(['c++','-O2','-std=c++17',ROOT/'tools/render_science_circuit.cpp','-o',exe],check=True)
views={'return':[('Seam station from the settlement return',(-85,1.82,30),(-86,2.85,42)),('South cut toward the seam station',(-109,1.82,59),(-91,3,36)),('Threshold toward the existing grotto',(-108,1.82,29),(-110,3.2,66)),('Archive promenade toward woodland',(9,1.82,92),(-25,2,91))],
       'services':[('Lofts entrance',(-.4,1.82,-18),(0,3.5,-34)),('Forge entrance',(69,1.82,84),(76,3.7,102)),('Archive portico',(-5,1.82,101),(-6,3.8,124)),('West courtyard gallery',(-3,1.82,24),(-25,2.7,27.5))]}
font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',19);small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',16)
for sheet,cameras in views.items():
    img=Image.new('RGB',(1920,56+len(cameras)*580+44),(224,228,221));d=ImageDraw.Draw(img);d.text((15,14),'CRYSTAL DISTRICT  |  '+sheet.upper()+'  |  MATCHED SOFTWARE CAMERAS',font=font,fill='#182b2b')
    for row,(label,eye,look) in enumerate(cameras):
        for side,folder in enumerate([a.before,a.after]):
            ppm=out/('view_%s_%s_%s.ppm'%(sheet,row,side));subprocess.run([exe,folder/'circuit.bin',tex,ppm,*map(str,(*eye,*look))],check=True)
            y=56+row*580;img.paste(Image.open(ppm),(side*960,y+35));d.text((side*960+16,y+5),('BEFORE' if side==0 else 'AFTER')+'   '+label,font=small,fill='#182b2b')
    d.text((16,img.height-34),'Actual exported meshes and H2/H3/H5 albedo sampling. Simplified ground, tints, directional light and haze. No Unity Game View, normal maps, live actors or native shadows.',font=small,fill='#182b2b');img.save(out/('circuit_'+sheet+'.jpg'),quality=87,optimize=True)

    # A compact ordinary-Git review image preserves the documented source camera and colour labels.
    compact=img.resize((1440,round(img.height*.75)),Image.Resampling.LANCZOS)
    compact.quantize(colors=256,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).save(out/('circuit_'+sheet+'.png'),optimize=True)
