import zipfile, xml.etree.ElementTree as ET

pptx_path = r'C:\Users\Danh\Downloads\Lesson 1 - GETTING STARTED.pptx'
with zipfile.ZipFile(pptx_path, 'r') as z:
    for s_idx in range(1, 6):
        xml_str = z.read(f'ppt/slides/slide{s_idx}.xml').decode('utf-8')
        root = ET.fromstring(xml_str)
        sptree = root.find('.//{http://schemas.openxmlformats.org/presentationml/2006/main}spTree')
        print(f'=== SLIDE {s_idx} ===')
        for child in sptree:
            tag = child.tag.split('}')[-1]
            if tag in ['nvGrpSpPr', 'grpSpPr']: continue
            cnvpr = child.find('.//{http://schemas.openxmlformats.org/presentationml/2006/main}cNvPr')
            if cnvpr is None: cnvpr = child.find('.//{http://schemas.openxmlformats.org/drawingml/2006/main}cNvPr')
            c_id = cnvpr.get('id', '') if cnvpr is not None else ''
            c_name = cnvpr.get('name', '') if cnvpr is not None else ''
            
            # Find off and ext inside xfrm
            xfrm = child.find('.//{http://schemas.openxmlformats.org/drawingml/2006/main}xfrm')
            off = xfrm.find('{http://schemas.openxmlformats.org/drawingml/2006/main}off') if xfrm is not None else None
            ext = xfrm.find('{http://schemas.openxmlformats.org/drawingml/2006/main}ext') if xfrm is not None else None
            
            texts = [t.text for t in child.findall('.//{http://schemas.openxmlformats.org/drawingml/2006/main}t') if t.text]
            t_str = ' '.join(texts)[:40]
            ox = off.get('x') if off is not None else ''
            oy = off.get('y') if off is not None else ''
            cx = ext.get('cx') if ext is not None else ''
            cy = ext.get('cy') if ext is not None else ''
            print(f'  [{tag}] id={c_id} name="{c_name}" off=({ox},{oy}) ext=({cx},{cy}) text="{t_str}"')
