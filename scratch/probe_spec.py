import zipfile
import os
import sys
import xml.etree.ElementTree as ET

sys.stdout.reconfigure(encoding='utf-8')

def probe_smartart_doc1():
    print("=== PROBING DOC1.DOCX SMARTART ===")
    with zipfile.ZipFile(r'C:\Users\Danh\Desktop\Doc1.docx', 'r') as z:
        print("\n--- word/document.xml snippet referencing diagrams ---")
        doc_xml = z.read('word/document.xml').decode('utf-8')
        # print occurrences of drawing / graphicData
        tree = ET.fromstring(doc_xml)
        for elem in tree.iter():
            if elem.tag.endswith('graphicData'):
                print(f"graphicData URI: {elem.attrib.get('uri')}")
                for child in elem:
                    print(f"  Child tag: {child.tag}, attrib: {child.attrib}")
            elif elem.tag.endswith('drawing'):
                # inspect inline vs anchor
                for child in elem:
                    print(f"drawing child: {child.tag}")
                    for sub in child:
                        if sub.tag.endswith('extent'):
                            print(f"  extent: {sub.attrib}")
                        elif sub.tag.endswith('docPr'):
                            print(f"  docPr: {sub.attrib}")

        print("\n--- word/_rels/document.xml.rels diagram relationships ---")
        rels_xml = z.read('word/_rels/document.xml.rels').decode('utf-8')
        rtree = ET.fromstring(rels_xml)
        for r in rtree:
            print(f"Rel Id={r.attrib.get('Id')}, Type={r.attrib.get('Type')}, Target={r.attrib.get('Target')}")

        for i in [1, 2, 3]:
            print(f"\n--- DIAGRAM {i} DETAILS ---")
            layout_name = f'word/diagrams/layout{i}.xml'
            data_name = f'word/diagrams/data{i}.xml'
            drawing_name = f'word/diagrams/drawing{i}.xml'
            colors_name = f'word/diagrams/colors{i}.xml'
            style_name = f'word/diagrams/quickStyle{i}.xml'
            
            if layout_name in z.namelist():
                ltree = ET.fromstring(z.read(layout_name))
                print(f"Layout root: {ltree.tag}, attrib: {ltree.attrib}")
                title = ltree.attrib.get('title', '')
                uniqueId = ltree.attrib.get('uniqueId', '')
                print(f"Layout title: {title}, uniqueId: {uniqueId}")

            if data_name in z.namelist():
                dtree = ET.fromstring(z.read(data_name))
                ptLst = dtree.find('.//{http://schemas.openxmlformats.org/drawingml/2006/diagram}ptLst')
                cxnLst = dtree.find('.//{http://schemas.openxmlformats.org/drawingml/2006/diagram}cxnLst')
                pts = ptLst.findall('{http://schemas.openxmlformats.org/drawingml/2006/diagram}pt') if ptLst is not None else []
                cxns = cxnLst.findall('{http://schemas.openxmlformats.org/drawingml/2006/diagram}cxn') if cxnLst is not None else []
                print(f"Data points count: {len(pts)}, Connections count: {len(cxns)}")
                for pt in pts[:5]:
                    modelId = pt.attrib.get('modelId')
                    ptType = pt.attrib.get('type')
                    # check text
                    t_elems = pt.findall('.//{http://schemas.openxmlformats.org/drawingml/2006/main}t')
                    texts = [t.text for t in t_elems if t.text]
                    print(f"  pt modelId={modelId}, type={ptType}, texts={texts}")
                for cxn in cxns[:5]:
                    print(f"  cxn: {cxn.attrib}")

            if drawing_name in z.namelist():
                drtree = ET.fromstring(z.read(drawing_name))
                shapes = drtree.findall('.//{http://schemas.openxmlformats.org/drawingml/2006/diagram}sp')
                print(f"Drawing shapes count in drawing{i}.xml: {len(shapes)}")
                for sp in shapes[:3]:
                    modelId = sp.attrib.get('modelId')
                    spPr = sp.find('{http://schemas.openxmlformats.org/drawingml/2006/diagram}spPr')
                    txBody = sp.find('{http://schemas.openxmlformats.org/drawingml/2006/diagram}txBody')
                    geom = spPr.find('{http://schemas.openxmlformats.org/drawingml/2006/main}prstGeom') if spPr is not None else None
                    prst = geom.attrib.get('prst') if geom is not None else 'unknown'
                    xfrm = spPr.find('{http://schemas.openxmlformats.org/drawingml/2006/main}xfrm') if spPr is not None else None
                    off = xfrm.find('{http://schemas.openxmlformats.org/drawingml/2006/main}off') if xfrm is not None else None
                    ext = xfrm.find('{http://schemas.openxmlformats.org/drawingml/2006/main}ext') if xfrm is not None else None
                    off_val = (off.attrib.get('x'), off.attrib.get('y')) if off is not None else None
                    ext_val = (ext.attrib.get('cx'), ext.attrib.get('cy')) if ext is not None else None
                    print(f"  sp modelId={modelId}, prst={prst}, off={off_val}, ext={ext_val}")

def probe_more():
    print("=== INSPECTING DOC1 DATA3 & LAYOUT3 (cycle6) ===")
    with zipfile.ZipFile(r'C:\Users\Danh\Desktop\Doc1.docx', 'r') as z:
        data3 = z.read('word/diagrams/data3.xml').decode('utf-8')
        layout3 = z.read('word/diagrams/layout3.xml').decode('utf-8')
        print("Data3 snippet (first 1000 chars):")
        print(data3[:1000])
        print("\nLayout3 snippet (first 1500 chars):")
        print(layout3[:1500])

    print("\n=== PROBING TEST_MATH_DOC.DOCX ===")
    with zipfile.ZipFile(r'c:\Users\Danh\Desktop\genofficepy\scratch\test_math_doc.docx', 'r') as z:
        doc = z.read('word/document.xml').decode('utf-8')
        tree = ET.fromstring(doc)
        math_tags = collections.Counter()
        for elem in tree.iter():
            tag = elem.tag
            if 'math' in tag.lower() or tag.startswith('{http://schemas.openxmlformats.org/officeDocument/2006/math}'):
                short_tag = tag.split('}')[-1]
                math_tags[short_tag] += 1
        print("Math tags count:", math_tags.most_common(30))

        # Sample math expressions
        math_nodes = tree.findall('.//{http://schemas.openxmlformats.org/officeDocument/2006/math}oMath')
        print(f"Total oMath elements: {len(math_nodes)}")
        for i, om in enumerate(math_nodes[:5]):
            subtags = [e.tag.split('}')[-1] for e in om.iter()]
            print(f"  oMath #{i+1} tags: {subtags}")

def probe_tables_and_shapes():
    print("\n=== PROBING TABLES & BORDERS ACROSS DOCX FILES ===")
    files = [
        r'C:\Users\Danh\Desktop\Ngan_hang_cau_hoi_Cong_nghe_12_Bai_1.docx',
        r'c:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoTongKet_2026.docx',
        r'c:\Users\Danh\Desktop\genofficepy\scratch\test_smartart_roundtrip.docx'
    ]
    for fp in files:
        if not os.path.exists(fp):
            continue
        print(f"\n--- File: {fp} ---")
        with zipfile.ZipFile(fp, 'r') as z:
            doc = z.read('word/document.xml').decode('utf-8', errors='ignore')
            tree = ET.fromstring(doc)
            tbls = tree.findall('.//{http://schemas.openxmlformats.org/wordprocessingml/2006/main}tbl')
            print(f"Total tables: {len(tbls)}")
            borders = tree.findall('.//{http://schemas.openxmlformats.org/wordprocessingml/2006/main}tblBorders')
            tcBorders = tree.findall('.//{http://schemas.openxmlformats.org/wordprocessingml/2006/main}tcBorders')
            print(f"tblBorders count: {len(borders)}, tcBorders count: {len(tcBorders)}")
            for b in borders[:3]:
                print("  tblBorders elements:")
                for child in b:
                    print(f"    {child.tag.split('}')[-1]}: {child.attrib}")
            for b in tcBorders[:3]:
                print("  tcBorders elements:")
                for child in b:
                    print(f"    {child.tag.split('}')[-1]}: {child.attrib}")

probe_smartart_doc1()
import collections
probe_more()
probe_tables_and_shapes()

