from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import sys,math
src=Path(sys.argv[1]); out=Path(sys.argv[2]); pages=sorted(src.glob('page-*.png'),key=lambda p:int(p.stem.split('-')[1]))
f=ImageFont.truetype(r'C:\Windows\Fonts\arialbd.ttf',22)
for batch in range(math.ceil(len(pages)/6)):
    subset=pages[batch*6:(batch+1)*6]; sheet=Image.new('RGB',(1800,2200),'#777'); d=ImageDraw.Draw(sheet)
    for i,p in enumerate(subset):
        im=Image.open(p); im.thumbnail((560,1010)); x=25+(i%3)*590; y=55+(i//3)*1070
        sheet.paste(im,(x,y)); d.text((x,y-32),p.stem,font=f,fill='white')
    sheet.save(out/f'sheet-{batch+1}.jpg',quality=88)
