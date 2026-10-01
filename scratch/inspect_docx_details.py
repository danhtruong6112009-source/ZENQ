import sys, io, zipfile, xml.etree.ElementTree as ET
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

docx_path = r'C:\Users\Danh\Downloads\Bài 1.ND GHI.docx'
with zipfile.ZipFile(docx_path, 'r') as z:
    num_tree = ET.fromstring(z.read('word/numbering.xml'))
    styles_tree = ET.fromstring(z.read('word/styles.xml'))

namespaces = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}

print('=== NUMBERING.XML ===')
for num in num_tree.findall('w:num', namespaces):
    numId = num.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}numId')
    abElem = num.find('w:abstractNumId', namespaces)
    abVal = abElem.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if abElem is not None else 'None'
    print(f'numId={numId} -> abstractNumId={abVal}')

for ab in num_tree.findall('w:abstractNum', namespaces):
    abId = ab.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}abstractNumId')
    print(f'\nabstractNumId={abId}:')
    for lvl in ab.findall('w:lvl', namespaces):
        ilvl = lvl.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}ilvl')
        start = lvl.find('w:start', namespaces)
        numFmt = lvl.find('w:numFmt', namespaces)
        lvlText = lvl.find('w:lvlText', namespaces)
        start_val = start.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if start is not None else '1'
        fmt_val = numFmt.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if numFmt is not None else 'decimal'
        txt_val = lvlText.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if lvlText is not None else ''
        print(f'  ilvl={ilvl}: start={start_val}, fmt={fmt_val}, lvlText={txt_val}')

print('\n=== STYLES.XML ===')
for style in styles_tree.findall('w:style', namespaces):
    sId = style.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}styleId')
    if sId in ['Normal', 'BodyText', 'ListParagraph', 'Heading1', 'Heading2']:
        name = style.find('w:name', namespaces)
        name_val = name.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if name is not None else ''
        rPr = style.find('w:rPr', namespaces)
        pPr = style.find('w:pPr', namespaces)
        print(f'Style: {sId} ({name_val})')
        if rPr is not None:
            sz = rPr.find('w:sz', namespaces)
            b = rPr.find('w:b', namespaces)
            sz_val = sz.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if sz is not None else 'default'
            print(f'  rPr: sz={sz_val}, b={b is not None}')
        if pPr is not None:
            jc = pPr.find('w:jc', namespaces)
            jc_val = jc.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if jc is not None else 'default'
            print(f'  pPr: jc={jc_val}')
