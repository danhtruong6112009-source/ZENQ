import sys, io, zipfile, xml.etree.ElementTree as ET
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

docx_path = r'C:\Users\Danh\Downloads\Bài 1.ND GHI.docx'
with zipfile.ZipFile(docx_path, 'r') as z:
    doc_tree = ET.fromstring(z.read('word/document.xml'))

namespaces = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
body = doc_tree.find('w:body', namespaces)

for i, p in enumerate(body.findall('w:p', namespaces)[:25]):
    text = ''.join(p.itertext())[:40].replace('\n', ' ')
    pPr = p.find('w:pPr', namespaces)
    if pPr is not None:
        pStyle = pPr.find('w:pStyle', namespaces)
        ind = pPr.find('w:ind', namespaces)
        numPr = pPr.find('w:numPr', namespaces)
        jc = pPr.find('w:jc', namespaces)
        pStyle_str = pStyle.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if pStyle is not None else 'None'
        ind_str = ET.tostring(ind, encoding='unicode').strip() if ind is not None else 'None'
        num_str = ET.tostring(numPr, encoding='unicode').strip() if numPr is not None else 'None'
        jc_str = jc.get('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val') if jc is not None else 'None'
        print(f'P{i}: style={pStyle_str} text="{text}"')
        print(f'   ind: {ind_str}')
        print(f'   num: {num_str}')
        print(f'   jc: {jc_str}')
