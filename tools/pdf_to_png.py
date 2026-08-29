import sys
from pathlib import Path
import pypdfium2 as pdfium
pdf=pdfium.PdfDocument(sys.argv[1]); out=Path(sys.argv[2]); out.mkdir(parents=True,exist_ok=True)
for i,p in enumerate(pdf):
    p.render(scale=1.7).to_pil().convert('RGB').save(out/f'page-{i+1}.png')
print(len(pdf))
