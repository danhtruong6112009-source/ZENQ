import sys, io, zipfile, xml.etree.ElementTree as ET
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

docx_path = r'C:\Users\Danh\Downloads\Bài 1.ND GHI.docx'
with zipfile.ZipFile(docx_path, 'r') as z:
    doc_tree = ET.fromstring(z.read('word/document.xml'))

namespaces = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
body = doc_tree.find('w:body', namespaces)

for i in [1, 2, 3, 4]:
    p = body.findall('w:p', namespaces)[i]
    print(f'=== P {i} RAW XML ===')
    print(ET.tostring(p, encoding='unicode'))
