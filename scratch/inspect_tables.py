
import sys, io, zipfile, xml.etree.ElementTree as ET
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

with zipfile.ZipFile(r'C:\Users\Danh\Downloads\Bài 1.ND GHI.docx') as z:
    root = ET.fromstring(z.read('word/document.xml'))

ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
body = root.find('w:body', ns)

tables = root.findall('.//w:tbl', ns)
print('Total tables found:', len(tables))
for t_idx, tbl in enumerate(tables):
    tblPr = tbl.find('w:tblPr', ns)
    tblW = tblPr.find('w:tblW', ns) if tblPr is not None else None
    rows = tbl.findall('w:tr', ns)
    print(f'=== TABLE {t_idx} ===')
    print('  tblW:', tblW.attrib if tblW is not None else None)
    print('  Rows count:', len(rows))
    for r_idx, r in enumerate(rows):
        trPr = r.find('w:trPr', ns)
        trHeight = trPr.find('w:trHeight', ns) if trPr is not None else None
        cells = r.findall('w:tc', ns)
        print(f'    Row {r_idx}: trHeight={trHeight.attrib if trHeight is not None else None}, cells={len(cells)}')
        for c_idx, c in enumerate(cells):
            tcPr = c.find('w:tcPr', ns)
            tcW = tcPr.find('w:tcW', ns) if tcPr is not None else None
            ps = c.findall('w:p', ns)
            text = ' // '.join([''.join(p.itertext()).strip() for p in ps if ''.join(p.itertext()).strip()])
            print(f'      Cell {c_idx}: tcW={tcW.attrib if tcW is not None else None}, ps={len(ps)}, text={text[:60]}')
