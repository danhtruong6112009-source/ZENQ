import zipfile, xml.etree.ElementTree as ET

pptx_path = r'C:\Users\Danh\Downloads\Lesson 1 - GETTING STARTED.pptx'

with zipfile.ZipFile(pptx_path, 'r') as z:
    # Check slide 1 image dimensions
    s1_xml = z.read('ppt/slides/slide1.xml').decode('utf-8')
    root1 = ET.fromstring(s1_xml)
    pic = root1.find('.//{http://schemas.openxmlformats.org/presentationml/2006/main}pic')
    # If we look in xfrm
    xfrm = pic.find('.//{http://schemas.openxmlformats.org/drawingml/2006/main}xfrm')
    off = xfrm.find('{http://schemas.openxmlformats.org/drawingml/2006/main}off')
    ext = xfrm.find('{http://schemas.openxmlformats.org/drawingml/2006/main}ext')
    print('Slide 1 pic in xfrm: off=', off.attrib, 'ext=', ext.attrib)
    w_px = (int(ext.attrib['cx']) / 9525.0) * 2.0
    h_px = (int(ext.attrib['cy']) / 9525.0) * 2.0
    print(f'Slide 1 pic scaled size: {w_px} x {h_px} (Canvas is 1920 x 1080)')
    
    # Check slide 18 table
    s18_xml = z.read('ppt/slides/slide18.xml').decode('utf-8')
    root18 = ET.fromstring(s18_xml)
    tbl = root18.find('.//{http://schemas.openxmlformats.org/drawingml/2006/table}tbl')
    trs = tbl.findall('{http://schemas.openxmlformats.org/drawingml/2006/table}tr')
    print('Slide 18 table rows:', len(trs))
    for r_i, tr in enumerate(trs):
        tcs = tr.findall('{http://schemas.openxmlformats.org/drawingml/2006/table}tc')
        row_txt = []
        for tc in tcs:
            txts = [t.text for t in tc.findall('.//{http://schemas.openxmlformats.org/drawingml/2006/main}t') if t.text]
            row_txt.append(''.join(txts).strip())
        print(f'  Row {r_i} ({len(tcs)} cols):', ' | '.join(row_txt[:4]))
