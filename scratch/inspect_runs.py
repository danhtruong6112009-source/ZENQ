import sys, io, zipfile, xml.etree.ElementTree as ET
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

docx_path = r'C:\Users\Danh\Downloads\Bài 1.ND GHI.docx'
with zipfile.ZipFile(docx_path, 'r') as z:
    doc_tree = ET.fromstring(z.read('word/document.xml'))

namespaces = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
body = doc_tree.find('w:body', namespaces)

for i, p in enumerate(body.findall('w:p', namespaces)[:8]):
    print(f'=== P {i} ===')
    pPr = p.find('w:pPr', namespaces)
    if pPr is not None:
        pStyle = pPr.find('w:pStyle', namespaces)
        print('  pStyle:', pStyle.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if pStyle is not None else 'None')
        rPr = pPr.find('w:rPr', namespaces)
        if rPr is not None:
            sz = rPr.find('w:sz', namespaces)
            b = rPr.find('w:b', namespaces)
            print('  pPr/rPr sz:', sz.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if sz is not None else 'None', 'b:', b is not None)
    for j, r in enumerate(p.findall('w:r', namespaces)):
        t = ''.join(r.itertext())
        rPr = r.find('w:rPr', namespaces)
        sz = rPr.find('w:sz', namespaces) if rPr is not None else None
        b = rPr.find('w:b', namespaces) if rPr is not None else None
        sz_val = sz.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if sz is not None else 'None'
        print(f'  run {j}: "{t}" sz={sz_val} b={b is not None}')
