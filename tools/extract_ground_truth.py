#!/usr/bin/env python3
# tools/extract_ground_truth.py
# GenOffice Ground Truth OpenXML Extractor & Manifest Generator
# Unpacks DOCX parts (drawing1.xml, data1.xml, layout1.xml, theme1.xml, document.xml)
# and compiles tests/ground_truth_manifest.json indexing all extracted attributes.

import os
import sys
import json
import zipfile
from datetime import datetime
from xml.etree import ElementTree as ET

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.dirname(BASE_DIR)
SAMPLES_DIR = os.path.join(REPO_ROOT, "tests", "samples")
GROUND_TRUTH_DIR = os.path.join(REPO_ROOT, "tests", "ground_truth")
MANIFEST_PATH = os.path.join(REPO_ROOT, "tests", "ground_truth_manifest.json")

# Namespaces
NS = {
    'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main',
    'wp': 'http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing',
    'a': 'http://schemas.openxmlformats.org/drawingml/2006/main',
    'dgm': 'http://schemas.openxmlformats.org/drawingml/2006/diagram',
    'm': 'http://schemas.openxmlformats.org/officeDocument/2006/math',
    'wps': 'http://schemas.microsoft.com/office/word/2010/wordprocessingShape',
    'r': 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
}

EMU_PER_PT = 12700.0

def safe_int(val, default=0):
    try:
        return int(val)
    except (ValueError, TypeError):
        return default

def emu_to_pt(emu_val):
    return round(safe_int(emu_val) / EMU_PER_PT, 2)

def pretty_xml(xml_bytes):
    try:
        root = ET.fromstring(xml_bytes)
        ET.indent(root, space="  ", level=0)
        return ET.tostring(root, encoding="utf-8").decode("utf-8")
    except Exception:
        return xml_bytes.decode("utf-8", errors="replace")

def extract_smartart_drawing(xml_bytes):
    nodes = []
    connectors = []
    root = ET.fromstring(xml_bytes)

    for sp in root.findall('.//dgm:sp', NS):
        model_id = sp.get('modelId', '')
        
        # Transform & bounds
        xfrm = sp.find('.//a:xfrm', NS)
        bounds_emu = {}
        bounds_pt = {}
        aspect_ratio = 1.0
        is_circle_1_to_1 = False
        
        if xfrm is not None:
            off = xfrm.find('a:off', NS)
            ext = xfrm.find('a:ext', NS)
            if off is not None and ext is not None:
                x = safe_int(off.get('x', 0))
                y = safe_int(off.get('y', 0))
                cx = safe_int(ext.get('cx', 0))
                cy = safe_int(ext.get('cy', 0))
                bounds_emu = {'x': x, 'y': y, 'cx': cx, 'cy': cy}
                bounds_pt = {
                    'x': emu_to_pt(x),
                    'y': emu_to_pt(y),
                    'width': emu_to_pt(cx),
                    'height': emu_to_pt(cy)
                }
                if cy > 0:
                    aspect_ratio = round(cx / cy, 4)
                is_circle_1_to_1 = abs(cx - cy) <= 10

        # Preset geometry
        geom = "custom"
        prst_elem = sp.find('.//a:prstGeom', NS)
        if prst_elem is not None:
            geom = prst_elem.get('prst', 'custom')

        # Color fill
        fill = "none"
        solid_fill = sp.find('.//a:solidFill', NS)
        if solid_fill is not None:
            scheme = solid_fill.find('a:schemeClr', NS)
            srgb = solid_fill.find('a:srgbClr', NS)
            if scheme is not None:
                fill = f"scheme:{scheme.get('val', '')}"
            elif srgb is not None:
                fill = f"#{srgb.get('val', '')}"

        # Text
        text_runs = [t.text for t in sp.findall('.//a:t', NS) if t.text]
        text_content = ' '.join(text_runs).strip()

        nodes.append({
            'model_id': model_id,
            'geometry': geom,
            'bounds_emu': bounds_emu,
            'bounds_pt': bounds_pt,
            'aspect_ratio': aspect_ratio,
            'is_circle_1_to_1': is_circle_1_to_1,
            'fill': fill,
            'text': text_content
        })

    for cxn in root.findall('.//dgm:cxnSp', NS):
        model_id = cxn.get('modelId', '')
        connectors.append({'model_id': model_id})

    return {
        'total_nodes': len(nodes),
        'nodes': nodes,
        'connectors': connectors,
        'has_1_to_1_circles': any(n['is_circle_1_to_1'] for n in nodes)
    }

def extract_smartart_data(xml_bytes):
    pts = []
    cxns = []
    root = ET.fromstring(xml_bytes)

    for pt in root.findall('.//dgm:pt', NS):
        model_id = pt.get('modelId', '')
        pt_type = pt.get('type', 'node')
        text_runs = [t.text for t in pt.findall('.//a:t', NS) if t.text]
        pts.append({
            'model_id': model_id,
            'type': pt_type,
            'text': ' '.join(text_runs).strip()
        })

    for cxn in root.findall('.//dgm:cxn', NS):
        cxns.append({
            'model_id': cxn.get('modelId', ''),
            'type': cxn.get('type', ''),
            'src_id': cxn.get('srcId', ''),
            'dest_id': cxn.get('destId', '')
        })

    return {'points': pts, 'connections': cxns}

def extract_theme(xml_bytes):
    root = ET.fromstring(xml_bytes)
    colors = {}
    clr_scheme = root.find('.//a:clrScheme', NS)
    if clr_scheme is not None:
        for child in clr_scheme:
            tag = child.tag.split('}')[-1]
            srgb = child.find('a:srgbClr', NS)
            sys_clr = child.find('a:sysClr', NS)
            if srgb is not None:
                colors[tag] = f"#{srgb.get('val', '')}"
            elif sys_clr is not None:
                colors[tag] = f"#{sys_clr.get('lastClr', '000000')}"
    return colors

def extract_document_features(xml_bytes):
    root = ET.fromstring(xml_bytes)
    shapes = []
    tables = []
    math_equations = []

    # 1. Shapes (wps:wsp or wp:inline / wp:anchor)
    for drawing in root.findall('.//w:drawing', NS):
        # Extent
        extent = drawing.find('.//wp:extent', NS)
        doc_pr = drawing.find('.//wp:docPr', NS)
        name = doc_pr.get('name', '') if doc_pr is not None else ''

        bounds_pt = {}
        if extent is not None:
            cx = safe_int(extent.get('cx', 0))
            cy = safe_int(extent.get('cy', 0))
            bounds_pt = {
                'width': emu_to_pt(cx),
                'height': emu_to_pt(cy),
                'aspect_ratio': round(cx / cy, 4) if cy > 0 else 1.0,
                'is_circle_1_to_1': abs(cx - cy) <= 10
            }

        # Shape geometry & border
        geom = "unknown"
        border_info = {}
        effects = []

        sp_pr = drawing.find('.//wps:spPr', NS)
        if sp_pr is not None:
            prst_geom = sp_pr.find('a:prstGeom', NS)
            if prst_geom is not None:
                geom = prst_geom.get('prst', 'unknown')

            # Border line
            ln = sp_pr.find('a:ln', NS)
            if ln is not None:
                w_pt = emu_to_pt(ln.get('w', '0'))
                cmpd = ln.get('cmpd', 'sng')
                dash_elem = ln.find('a:prstDash', NS)
                dash = dash_elem.get('val', 'solid') if dash_elem is not None else 'solid'
                border_info = {
                    'width_pt': w_pt,
                    'compound_style': cmpd,
                    'dash_style': dash
                }

            # Visual effects
            if sp_pr.find('.//a:outerShdw', NS) is not None:
                effects.append('drop_shadow')
            if sp_pr.find('.//a:bevelT', NS) is not None:
                effects.append('bevel_3d')
            if sp_pr.find('.//a:gradFill', NS) is not None:
                effects.append('gradient_fill')

        # Text
        text_runs = [t.text for t in drawing.findall('.//w:t', NS) if t.text]

        shapes.append({
            'name': name,
            'geometry': geom,
            'bounds_pt': bounds_pt,
            'border': border_info,
            'effects': effects,
            'text': ' '.join(text_runs).strip()
        })

    # 2. Tables
    for tbl in root.findall('.//w:tbl', NS):
        tbl_pr = tbl.find('w:tblPr', NS)
        borders = {}
        if tbl_pr is not None:
            tbl_borders = tbl_pr.find('w:tblBorders', NS)
            if tbl_borders is not None:
                for b_tag in ['top', 'left', 'bottom', 'right', 'insideH', 'insideV']:
                    b_elem = tbl_borders.find(f'w:{b_tag}', NS)
                    if b_elem is not None:
                        val = b_elem.get(f'{{{NS["w"]}}}val', '')
                        sz = safe_int(b_elem.get(f'{{{NS["w"]}}}sz', '0'))
                        color = b_elem.get(f'{{{NS["w"]}}}color', '')
                        borders[b_tag] = {
                            'style': val,
                            'width_pt': round(sz / 8.0, 2),
                            'color': color
                        }

        # Rows and cols count
        rows = tbl.findall('w:tr', NS)
        row_count = len(rows)
        col_count = max([len(r.findall('w:tc', NS)) for r in rows]) if rows else 0

        # Cell shading
        shadings = []
        for shd in tbl.findall('.//w:shd', NS):
            fill = shd.get(f'{{{NS["w"]}}}fill', '')
            if fill and fill not in shadings:
                shadings.append(fill)

        tables.append({
            'rows': row_count,
            'cols': col_count,
            'borders': borders,
            'shading_fills': shadings
        })

    # 3. OMML Math Formulas
    for math_elem in root.findall('.//m:oMath', NS):
        tags = []
        if math_elem.find('.//m:f', NS) is not None:
            tags.append('fraction')
        if math_elem.find('.//m:rad', NS) is not None:
            tags.append('radical')
        if math_elem.find('.//m:sSup', NS) is not None:
            tags.append('superscript')
        if math_elem.find('.//m:sSub', NS) is not None:
            tags.append('subscript')
        if math_elem.find('.//m:m', NS) is not None:
            tags.append('matrix')
        if math_elem.find('.//m:nary', NS) is not None:
            tags.append('nary_integral_sum')
        if math_elem.find('.//m:d', NS) is not None:
            tags.append('delimiter')

        m_texts = [t.text for t in math_elem.findall('.//m:t', NS) if t.text]
        math_equations.append({
            'formula_text': ''.join(m_texts).strip(),
            'math_tags': tags
        })

    return {
        'shapes': shapes,
        'tables': tables,
        'math_equations': math_equations
    }

def main():
    print("=" * 65)
    print(" GENOFFICE GROUND TRUTH OPENXML EXTRACTOR & MANIFEST PIPELINE")
    print(f" Samples Source: {SAMPLES_DIR}")
    print(f" Ground Truth Output: {GROUND_TRUTH_DIR}")
    print("=" * 65)

    os.makedirs(GROUND_TRUTH_DIR, exist_ok=True)

    manifest = {
        'schema_version': '1.0',
        'generated_at': datetime.utcnow().isoformat() + 'Z',
        'statistics': {
            'total_sample_files': 0,
            'total_smartart_diagrams': 0,
            'total_smartart_nodes': 0,
            'total_shapes': 0,
            'total_tables': 0,
            'total_math_equations': 0
        },
        'categories': {},
        'samples': {}
    }

    # Discover all .docx files
    sample_files = []
    for root_dir, _, files in os.walk(SAMPLES_DIR):
        for f in files:
            if f.lower().endswith('.docx'):
                sample_files.append(os.path.join(root_dir, f))

    sample_files.sort()
    manifest['statistics']['total_sample_files'] = len(sample_files)
    print(f"Discovered {len(sample_files)} canonical sample documents.")

    target_parts = [
        ('word/document.xml', 'document.xml'),
        ('word/diagrams/drawing1.xml', 'drawing1.xml'),
        ('word/diagrams/data1.xml', 'data1.xml'),
        ('word/diagrams/layout1.xml', 'layout1.xml'),
        ('word/theme/theme1.xml', 'theme1.xml')
    ]

    for sample_path in sample_files:
        rel_path = os.path.relpath(sample_path, SAMPLES_DIR)
        sample_name = os.path.splitext(os.path.basename(sample_path))[0]
        sub_folder = os.path.dirname(rel_path)
        
        sample_unpack_dir = os.path.join(GROUND_TRUTH_DIR, sub_folder, sample_name)
        os.makedirs(sample_unpack_dir, exist_ok=True)

        sample_record = {
            'filename': os.path.basename(sample_path),
            'relative_path': rel_path.replace('\\', '/'),
            'category': sub_folder if sub_folder else 'root',
            'unpacked_parts': [],
            'smartart': None,
            'shapes': [],
            'tables': [],
            'math_equations': [],
            'theme_colors': {}
        }

        try:
            with zipfile.ZipFile(sample_path, 'r') as z:
                available_names = z.namelist()

                # Unpack and parse target parts
                for part_path, out_name in target_parts:
                    if part_path in available_names:
                        content = z.read(part_path)
                        # Save formatted XML to ground truth dir
                        formatted = pretty_xml(content)
                        out_file = os.path.join(sample_unpack_dir, out_name)
                        with open(out_file, 'w', encoding='utf-8') as of:
                            of.write(formatted)
                        sample_record['unpacked_parts'].append(out_name)

                        # Extract attributes
                        if out_name == 'drawing1.xml':
                            sa_drawing = extract_smartart_drawing(content)
                            sample_record['smartart'] = sa_drawing
                            manifest['statistics']['total_smartart_diagrams'] += 1
                            manifest['statistics']['total_smartart_nodes'] += sa_drawing['total_nodes']
                        elif out_name == 'data1.xml':
                            sa_data = extract_smartart_data(content)
                            if sample_record['smartart'] is not None:
                                sample_record['smartart']['data_model'] = sa_data
                        elif out_name == 'theme1.xml':
                            sample_record['theme_colors'] = extract_theme(content)
                        elif out_name == 'document.xml':
                            doc_features = extract_document_features(content)
                            sample_record['shapes'] = doc_features['shapes']
                            sample_record['tables'] = doc_features['tables']
                            sample_record['math_equations'] = doc_features['math_equations']
                            manifest['statistics']['total_shapes'] += len(doc_features['shapes'])
                            manifest['statistics']['total_tables'] += len(doc_features['tables'])
                            manifest['statistics']['total_math_equations'] += len(doc_features['math_equations'])

        except Exception as ex:
            print(f"Error processing {sample_path}: {ex}", file=sys.stderr)
            sample_record['error'] = str(ex)

        # Index by relative path
        norm_key = rel_path.replace('\\', '/')
        manifest['samples'][norm_key] = sample_record
        
        cat = sample_record['category']
        manifest['categories'][cat] = manifest['categories'].get(cat, 0) + 1

    # Write Manifest JSON
    with open(MANIFEST_PATH, 'w', encoding='utf-8') as mf:
        json.dump(manifest, mf, indent=2, ensure_ascii=False)

    print("-" * 65)
    print(" GROUND TRUTH EXTRACTION COMPLETE")
    print(f" Total Samples Processed : {manifest['statistics']['total_sample_files']}")
    print(f" SmartArt Diagrams Found : {manifest['statistics']['total_smartart_diagrams']}")
    print(f" SmartArt Nodes Extracted: {manifest['statistics']['total_smartart_nodes']}")
    print(f" Vector Shapes Extracted : {manifest['statistics']['total_shapes']}")
    print(f" Tables Extracted        : {manifest['statistics']['total_tables']}")
    print(f" Math Equations Extracted: {manifest['statistics']['total_math_equations']}")
    print(f" Manifest Saved To       : {MANIFEST_PATH}")
    print("=" * 65)

if __name__ == '__main__':
    main()
