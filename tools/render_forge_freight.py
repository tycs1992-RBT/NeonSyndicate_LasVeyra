"""Render Mono-exported freight poses into a labelled MP4, animated PNG and stride sheet."""
import argparse
import csv
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONT = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'


def label(row):
    t = float(row['seconds'])
    if t < 2.5:
        return 'Loading stop'
    if row['waiting'] == 'True':
        return 'Yield at crossing'
    if t < 8.15:
        return 'Pulling out'
    if float(row['speed']) < 1.5:
        return 'Clear lane / restart'
    return 'Eastbound haul'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for arg in ['export', 'work', 'evidence']:
        parser.add_argument(arg, type=Path)
    args = parser.parse_args()
    args.work.mkdir(parents=True, exist_ok=True)
    args.evidence.mkdir(parents=True, exist_ok=True)
    exe = args.work/'camera'
    subprocess.run(['c++','-O2','-std=c++17',ROOT/'tools/render_forge_freight.cpp','-o',exe],check=True)
    subprocess.run([exe,args.export,args.work],check=True)
    font = ImageFont.truetype(FONT, 21)
    small = ImageFont.truetype(FONT, 16)
    with (args.export/'motion.csv').open() as stream:
        rows = list(csv.DictReader(stream))
    animated, selected = [], {}
    keys = [18, 55, 108, 146, 186, 222]
    movie = args.work/'dante_freight.mp4'
    encoder = subprocess.Popen(['ffmpeg','-loglevel','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','960x540',
                                '-r','16','-i','-','-c:v','libx264','-crf','21','-pix_fmt','yuv420p',
                                '-movflags','+faststart',movie],stdin=subprocess.PIPE)
    try:
        for i, row in enumerate(rows):
            image = Image.open(args.work/f'frame-{i:03d}.ppm').convert('RGB')
            draw = ImageDraw.Draw(image)
            draw.rectangle((0,0,960,43),fill='#202c2d')
            draw.text((15,9),'DANTE FREIGHT    /    '+label(row).upper(),font=font,fill='#f6eddc')
            draw.rectangle((0,494,960,539),fill='#202c2d')
            draw.text((15,502),f"{float(row['seconds']):04.1f} s     {float(row['speed']):.2f} m/s     Hitch angle {float(row['yaw_offset']):.1f} deg",font=small,fill='#f6eddc')
            draw.text((516,502),'Exported poses  |  simulated crossing  |  software',font=small,fill='#d5dbbf')
            encoder.stdin.write(image.tobytes())
            if i%2==0:
                animated.append(image.resize((576,324),Image.Resampling.LANCZOS).quantize(128))
            if i in keys:
                selected[i] = image.resize((624,351),Image.Resampling.LANCZOS)
    finally:
        encoder.stdin.close()
        if encoder.wait()!=0:
            raise RuntimeError('Motion encoder failed')
    frames = [image.convert('RGB') for image in animated]
    atlas = Image.new('RGB',(288,162*8))
    for i, image in enumerate(frames[::15][:8]):
        atlas.paste(image.resize((288,162)),(0,162*i))
    palette = atlas.quantize(128)
    frames = [im.quantize(palette=palette,dither=Image.Dither.NONE) for im in frames]
    frames[0].save(args.evidence/'forge_freight_motion.png', save_all=True,
                   append_images=frames[1:], duration=125, loop=0, optimize=True)
    sheet = Image.new('RGB',(1248,3*351+55),'#e7e6da')
    draw = ImageDraw.Draw(sheet)
    draw.text((16,15),'SOUTHERN MINERAL ROAD   /   ACTUAL TEAM POSES',font=font,fill='#21302e')
    for k, frame in enumerate(keys):
        sheet.paste(selected[frame],((k%2)*624,55+(k//2)*351))
    sheet.quantize(192).save(args.evidence/'forge_freight_sequence.png',optimize=True)


if __name__ == '__main__':
    main()
