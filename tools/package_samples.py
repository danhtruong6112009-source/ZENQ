import os
import zipfile
import shutil
from xml.etree import ElementTree as ET

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.dirname(BASE_DIR)
SAMPLES_DIR = os.path.join(REPO_ROOT, "tests", "samples")
SMARTART_DIR = os.path.join(SAMPLES_DIR, "smartart")
SUITES_DIR = os.path.join(SAMPLES_DIR, "suites")
SCENARIOS_DIR = os.path.join(SAMPLES_DIR, "scenarios")

for d in [SMARTART_DIR, SUITES_DIR, SCENARIOS_DIR]:
    os.makedirs(d, exist_ok=True)

# Read base template from sa_list_alternating_hexagons_base.docx
template_docx = os.path.join(SMARTART_DIR, "sa_list_alternating_hexagons_base.docx")
if not os.path.exists(template_docx):
    raise FileNotFoundError(f"Template docx not found at {template_docx}")

with zipfile.ZipFile(template_docx, 'r') as z:
    base_parts = {name: z.read(name) for name in z.namelist()}

def create_docx(dest_path, parts_dict):
    if os.path.exists(dest_path):
        os.remove(dest_path)
    with zipfile.ZipFile(dest_path, 'w', compression=zipfile.ZIP_DEFLATED) as z:
        for name, data in parts_dict.items():
            if isinstance(data, str):
                data = data.encode('utf-8')
            z.writestr(name, data)

# -----------------------------------------------------------------------------
# 1. SMARTART SAMPLES GENERATION
# -----------------------------------------------------------------------------
smartart_definitions = [
    # Cycle (Circle 1:1, Radial, Ring)
    {"cat": "cycle", "type": "basic_cycle", "title": "Basic Cycle", "geom": "ellipse", "circle": True, "count": 4},
    {"cat": "cycle", "type": "radial_cycle", "title": "Radial Cycle", "geom": "ellipse", "circle": True, "count": 5},
    {"cat": "cycle", "type": "block_cycle", "title": "Block Cycle", "geom": "roundRect", "circle": False, "count": 4},
    {"cat": "cycle", "type": "continuous_cycle", "title": "Continuous Cycle", "geom": "ellipse", "circle": True, "count": 5},
    
    # Hierarchy (Org Chart, Tree, Horizontal)
    {"cat": "hierarchy", "type": "organization_chart", "title": "Organization Chart", "geom": "roundRect", "circle": False, "count": 5},
    {"cat": "hierarchy", "type": "horizontal_hierarchy", "title": "Horizontal Hierarchy", "geom": "rect", "circle": False, "count": 4},
    {"cat": "hierarchy", "type": "hierarchy_list", "title": "Hierarchy List", "geom": "roundRect", "circle": False, "count": 4},
    
    # Process (Linear, Chevron, Timeline)
    {"cat": "process", "type": "basic_process", "title": "Basic Process", "geom": "rect", "circle": False, "count": 4},
    {"cat": "process", "type": "basic_chevron_process", "title": "Basic Chevron Process", "geom": "chevron", "circle": False, "count": 5},
    {"cat": "process", "type": "continuous_arrow_process", "title": "Continuous Arrow Process", "geom": "rightArrow", "circle": False, "count": 4},

    # Matrix (2x2 Quadrants)
    {"cat": "matrix", "type": "basic_matrix", "title": "Basic Matrix", "geom": "roundRect", "circle": False, "count": 4},
    {"cat": "matrix", "type": "grid_matrix", "title": "Grid Matrix", "geom": "rect", "circle": False, "count": 4},

    # Pyramid
    {"cat": "pyramid", "type": "basic_pyramid", "title": "Basic Pyramid", "geom": "trapezoid", "circle": False, "count": 4},
    {"cat": "pyramid", "type": "segmented_pyramid", "title": "Segmented Pyramid", "geom": "trapezoid", "circle": False, "count": 3},

    # Relationship (Venn, Target)
    {"cat": "relationship", "type": "basic_venn", "title": "Basic Venn", "geom": "ellipse", "circle": True, "count": 3},
    {"cat": "relationship", "type": "basic_target", "title": "Basic Target", "geom": "ellipse", "circle": True, "count": 4},
    {"cat": "relationship", "type": "converging_arrows", "title": "Converging Arrows", "geom": "rightArrow", "circle": False, "count": 4},

    # List
    {"cat": "list", "type": "basic_block_list", "title": "Basic Block List", "geom": "rect", "circle": False, "count": 4},
    {"cat": "list", "type": "vertical_box_list", "title": "Vertical Box List", "geom": "roundRect", "circle": False, "count": 4},

    # Picture
    {"cat": "picture", "type": "bubble_picture_list", "title": "Bubble Picture List", "geom": "ellipse", "circle": True, "count": 4},
]

def build_smartart_drawing1(defn):
    geom = defn["geom"]
    is_circle = defn.get("circle", False)
    count = defn["count"]
    title = defn["title"]
    
    # 1 pt = 12700 EMUs
    # Frame is 480pt x 320pt = 6096000 x 4064000 EMUs
    sp_xml = []
    if is_circle and "radial" in defn["type"]:
        # Hub node + outer satellites
        # Hub node at center, 1:1 circle (e.g. 100pt = 1270000 EMU)
        hub_dim = 1270000
        cx = 3048000
        cy = 2032000
        sp_xml.append(f'''
    <dgm:sp modelId="node_0">
      <dgm:spPr>
        <a:xfrm>
          <a:off x="{cx - hub_dim//2}" y="{cy - hub_dim//2}"/>
          <a:ext cx="{hub_dim}" cy="{hub_dim}"/>
        </a:xfrm>
        <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
        <a:solidFill><a:schemeClr val="accent1"/></a:solidFill>
      </dgm:spPr>
      <dgm:txBody><a:bodyPr/><a:p><a:r><a:t>Central Hub: {title}</a:t></a:r></a:p></dgm:txBody>
    </dgm:sp>''')
        # Outer satellites
        import math
        sat_dim = 952500  # 75pt 1:1 circle
        orbit_r = 1600000
        num_sat = count - 1
        for i in range(num_sat):
            theta = (i * 2 * math.pi / num_sat) - math.pi / 2
            sat_x = int(cx + orbit_r * math.cos(theta)) - sat_dim // 2
            sat_y = int(cy + orbit_r * math.sin(theta)) - sat_dim // 2
            sp_xml.append(f'''
    <dgm:sp modelId="node_{i+1}">
      <dgm:spPr>
        <a:xfrm>
          <a:off x="{sat_x}" y="{sat_y}"/>
          <a:ext cx="{sat_dim}" cy="{sat_dim}"/>
        </a:xfrm>
        <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
        <a:solidFill><a:schemeClr val="accent{((i+1)%6)+1}"/></a:solidFill>
      </dgm:spPr>
      <dgm:txBody><a:bodyPr/><a:p><a:r><a:t>Satellite {i+1}</a:t></a:r></a:p></dgm:txBody>
    </dgm:sp>''')
    elif is_circle:
        # Ring / Cycle with perfect 1:1 circle nodes
        import math
        cx = 3048000
        cy = 2032000
        node_dim = 1143000 # 90pt 1:1 circle (cx == cy)
        orbit_r = 1350000
        for i in range(count):
            theta = (i * 2 * math.pi / count) - math.pi / 2
            nx = int(cx + orbit_r * math.cos(theta)) - node_dim // 2
            ny = int(cy + orbit_r * math.sin(theta)) - node_dim // 2
            sp_xml.append(f'''
    <dgm:sp modelId="node_{i+1}">
      <dgm:spPr>
        <a:xfrm>
          <a:off x="{nx}" y="{ny}"/>
          <a:ext cx="{node_dim}" cy="{node_dim}"/>
        </a:xfrm>
        <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
        <a:solidFill><a:schemeClr val="accent{(i%6)+1}"/></a:solidFill>
      </dgm:spPr>
      <dgm:txBody><a:bodyPr/><a:p><a:r><a:t>Step {i+1}: {title}</a:t></a:r></a:p></dgm:txBody>
    </dgm:sp>''')
    else:
        # Linear / Grid layout
        w = 1143000
        h = 762000
        spacing = 1270000
        for i in range(count):
            nx = 635000 + (i * spacing) % 4500000
            ny = 635000 + ((i * spacing) // 4500000) * 1000000
            sp_xml.append(f'''
    <dgm:sp modelId="node_{i+1}">
      <dgm:spPr>
        <a:xfrm>
          <a:off x="{nx}" y="{ny}"/>
          <a:ext cx="{w}" cy="{h}"/>
        </a:xfrm>
        <a:prstGeom prst="{geom}"><a:avLst/></a:prstGeom>
        <a:solidFill><a:schemeClr val="accent{(i%6)+1}"/></a:solidFill>
      </dgm:spPr>
      <dgm:txBody><a:bodyPr/><a:p><a:r><a:t>Item {i+1}: {title}</a:t></a:r></a:p></dgm:txBody>
    </dgm:sp>''')

    return f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<dgm:drawing xmlns:dgm="http://schemas.openxmlformats.org/drawingml/2006/diagram" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
  <dgm:spTree>
    <dgm:nvGrpSpPr><dgm:cNvPr id="1" name=""/><dgm:cNvGrpSpPr/></dgm:nvGrpSpPr>
    <dgm:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="6096000" cy="4064000"/><a:chOff x="0" y="0"/><a:chExt cx="6096000" cy="4064000"/></a:xfrm></dgm:grpSpPr>
    {''.join(sp_xml)}
  </dgm:spTree>
</dgm:drawing>'''

def build_smartart_data1(defn):
    pts = []
    cxns = []
    count = defn["count"]
    title = defn["title"]
    for i in range(count):
        pts.append(f'''<dgm:pt modelId="node_{i+1}" type="node"><dgm:prSet/><dgm:spPr/><dgm:t><a:bodyPr/><a:p><a:r><a:t>Node {i+1}: {title}</a:t></a:r></a:p></dgm:t></dgm:pt>''')
        if i > 0:
            cxns.append(f'''<dgm:cxn modelId="cxn_{i}" type="parOf" srcId="node_1" destId="node_{i+1}"/>''')
    return f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<dgm:dataModel xmlns:dgm="http://schemas.openxmlformats.org/drawingml/2006/diagram" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
  <dgm:ptLst>{''.join(pts)}</dgm:ptLst>
  <dgm:cxnLst>{''.join(cxns)}</dgm:cxnLst>
</dgm:dataModel>'''

print("1. Packaging SmartArt samples...")
for defn in smartart_definitions:
    cat = defn["cat"]
    stype = defn["type"]
    fname = f"sa_{cat}_{stype}_base.docx"
    fpath = os.path.join(SMARTART_DIR, fname)
    
    parts = dict(base_parts)
    parts["word/diagrams/drawing1.xml"] = build_smartart_drawing1(defn)
    parts["word/diagrams/data1.xml"] = build_smartart_data1(defn)
    create_docx(fpath, parts)
    print(f"  Created SmartArt: {fname}")

# -----------------------------------------------------------------------------
# 2. SUITES GENERATION
# -----------------------------------------------------------------------------

# A. Shapes Library Suite
shapes_doc_xml = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
            xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
            xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"
            xmlns:wps="http://schemas.microsoft.com/office/word/2010/wordprocessingShape">
  <w:body>
    <w:p><w:r><w:rPr><w:b/><w:sz w:val="36"/></w:rPr><w:t>GenOffice Canonical Shapes, Borders &amp; 3D Test Suite</w:t></w:r></w:p>
    
    <!-- Shape 1: Solid 2pt Rectangle -->
    <w:p><w:r><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0">
      <wp:extent cx="1778000" cy="889000"/>
      <wp:docPr id="1" name="SolidRect"/>
      <a:graphic><a:graphicData uri="http://schemas.microsoft.com/office/word/2010/wordprocessingShape">
        <wps:wsp>
          <wps:spPr>
            <a:xfrm><a:off x="762000" y="1397000"/><a:ext cx="1778000" cy="889000"/></a:xfrm>
            <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
            <a:solidFill><a:srgbClr val="4472C4"/></a:solidFill>
            <a:ln w="25400" cmpd="sng"><a:solidFill><a:srgbClr val="000000"/></a:solidFill><a:prstDash val="solid"/></a:ln>
          </wps:spPr>
          <wps:txbx><w:txbxContent><w:p><w:r><w:t>Solid 2pt</w:t></w:r></w:p></w:txbxContent></wps:txbx>
        </wps:wsp>
      </a:graphicData></a:graphic>
    </wp:inline></w:drawing></w:r></w:p>

    <!-- Shape 2: Double 4.5pt Rectangle -->
    <w:p><w:r><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0">
      <wp:extent cx="1778000" cy="889000"/>
      <wp:docPr id="2" name="DoubleRect"/>
      <a:graphic><a:graphicData uri="http://schemas.microsoft.com/office/word/2010/wordprocessingShape">
        <wps:wsp>
          <wps:spPr>
            <a:xfrm><a:off x="2794000" y="1397000"/><a:ext cx="1778000" cy="889000"/></a:xfrm>
            <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
            <a:solidFill><a:srgbClr val="ED7D31"/></a:solidFill>
            <a:ln w="57150" cmpd="dbl"><a:solidFill><a:srgbClr val="000000"/></a:solidFill><a:prstDash val="solid"/></a:ln>
          </wps:spPr>
          <wps:txbx><w:txbxContent><w:p><w:r><w:t>Double 4.5pt</w:t></w:r></w:p></w:txbxContent></wps:txbx>
        </wps:wsp>
      </a:graphicData></a:graphic>
    </wp:inline></w:drawing></w:r></w:p>

    <!-- Shape 3: Dashed 2.25pt Rounded Rectangle -->
    <w:p><w:r><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0">
      <wp:extent cx="1778000" cy="889000"/>
      <wp:docPr id="3" name="DashRoundRect"/>
      <a:graphic><a:graphicData uri="http://schemas.microsoft.com/office/word/2010/wordprocessingShape">
        <wps:wsp>
          <wps:spPr>
            <a:xfrm><a:off x="4826000" y="1397000"/><a:ext cx="1778000" cy="889000"/></a:xfrm>
            <a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>
            <a:solidFill><a:srgbClr val="70AD47"/></a:solidFill>
            <a:ln w="28575" cmpd="sng"><a:solidFill><a:srgbClr val="000000"/></a:solidFill><a:prstDash val="dash"/></a:ln>
          </wps:spPr>
          <wps:txbx><w:txbxContent><w:p><w:r><w:t>Dash 2.25pt</w:t></w:r></w:p></w:txbxContent></wps:txbx>
        </wps:wsp>
      </a:graphicData></a:graphic>
    </wp:inline></w:drawing></w:r></w:p>

    <!-- Shape 4: True 1:1 Circle (cx == cy == 1143000 EMU == 90pt) -->
    <w:p><w:r><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0">
      <wp:extent cx="1143000" cy="1143000"/>
      <wp:docPr id="4" name="Circle1to1"/>
      <a:graphic><a:graphicData uri="http://schemas.microsoft.com/office/word/2010/wordprocessingShape">
        <wps:wsp>
          <wps:spPr>
            <a:xfrm><a:off x="762000" y="2921000"/><a:ext cx="1143000" cy="1143000"/></a:xfrm>
            <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
            <a:solidFill><a:srgbClr val="5B9BD5"/></a:solidFill>
            <a:ln w="25400"><a:solidFill><a:srgbClr val="000000"/></a:solidFill></a:ln>
          </wps:spPr>
          <wps:txbx><w:txbxContent><w:p><w:r><w:t>Circle 1:1</w:t></w:r></w:p></w:txbxContent></wps:txbx>
        </wps:wsp>
      </a:graphicData></a:graphic>
    </wp:inline></w:drawing></w:r></w:p>

    <!-- Shape 8: Drop Shadow Rectangle -->
    <w:p><w:r><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0">
      <wp:extent cx="1651000" cy="952500"/>
      <wp:docPr id="8" name="ShadowRect"/>
      <a:graphic><a:graphicData uri="http://schemas.microsoft.com/office/word/2010/wordprocessingShape">
        <wps:wsp>
          <wps:spPr>
            <a:xfrm><a:off x="762000" y="4572000"/><a:ext cx="1651000" cy="952500"/></a:xfrm>
            <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
            <a:solidFill><a:srgbClr val="4472C4"/></a:solidFill>
            <a:effectLst><a:outerShdw blurRad="50800" dist="38100" dir="5400000"><a:srgbClr val="000000"><a:alpha val="40000"/></a:srgbClr></a:outerShdw></a:effectLst>
          </wps:spPr>
          <wps:txbx><w:txbxContent><w:p><w:r><w:t>Drop Shadow</w:t></w:r></w:p></w:txbxContent></wps:txbx>
        </wps:wsp>
      </a:graphicData></a:graphic>
    </wp:inline></w:drawing></w:r></w:p>

    <!-- Shape 10: 1:1 Circle with 3D Bevel -->
    <w:p><w:r><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0">
      <wp:extent cx="1206500" cy="1206500"/>
      <wp:docPr id="10" name="BevelCircle"/>
      <a:graphic><a:graphicData uri="http://schemas.microsoft.com/office/word/2010/wordprocessingShape">
        <wps:wsp>
          <wps:spPr>
            <a:xfrm><a:off x="4572000" y="4445000"/><a:ext cx="1206500" cy="1206500"/></a:xfrm>
            <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
            <a:solidFill><a:srgbClr val="ED7D31"/></a:solidFill>
            <a:sp3d><a:bevelT w="76200" h="76200" prst="circle"/></a:sp3d>
          </wps:spPr>
          <wps:txbx><w:txbxContent><w:p><w:r><w:t>Bevel 3D</w:t></w:r></w:p></w:txbxContent></wps:txbx>
        </wps:wsp>
      </a:graphicData></a:graphic>
    </wp:inline></w:drawing></w:r></w:p>
    
    <w:sectPr><w:pgSz w:w="12240" w:h="15840"/></w:sectPr>
  </w:body>
</w:document>'''

parts_shape = dict(base_parts)
parts_shape["word/document.xml"] = shapes_doc_xml
create_docx(os.path.join(SUITES_DIR, "shapes_library_suite.docx"), parts_shape)
print("  Created Suite: shapes_library_suite.docx")

# B. Table Borders & Shading Suite
table_doc_xml = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    <w:p><w:r><w:rPr><w:b/><w:sz w:val="36"/></w:rPr><w:t>GenOffice Canonical Table Borders &amp; Shading Test Suite</w:t></w:r></w:p>

    <!-- Table 1: Solid Single Borders 1pt (sz=8) -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Table 1: Single Solid Borders (sz=8 / 1.0pt)</w:t></w:r></w:p>
    <w:tbl>
      <w:tblPr>
        <w:tblBorders>
          <w:top w:val="single" w:sz="8" w:space="0" w:color="4472C4"/>
          <w:left w:val="single" w:sz="8" w:space="0" w:color="4472C4"/>
          <w:bottom w:val="single" w:sz="8" w:space="0" w:color="4472C4"/>
          <w:right w:val="single" w:sz="8" w:space="0" w:color="4472C4"/>
          <w:insideH w:val="single" w:sz="8" w:space="0" w:color="4472C4"/>
          <w:insideV w:val="single" w:sz="8" w:space="0" w:color="4472C4"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr><w:tc><w:p><w:r><w:t>T1 [1,1]</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>T1 [1,2]</w:t></w:r></w:p></w:tc></w:tr>
      <w:tr><w:tc><w:p><w:r><w:t>T1 [2,1]</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>T1 [2,2]</w:t></w:r></w:p></w:tc></w:tr>
    </w:tbl>

    <!-- Table 2: Double Borders 1.5pt (sz=12) -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Table 2: Double Line Borders (sz=12 / 1.5pt)</w:t></w:r></w:p>
    <w:tbl>
      <w:tblPr>
        <w:tblBorders>
          <w:top w:val="double" w:sz="12" w:space="0" w:color="ED7D31"/>
          <w:left w:val="double" w:sz="12" w:space="0" w:color="ED7D31"/>
          <w:bottom w:val="double" w:sz="12" w:space="0" w:color="ED7D31"/>
          <w:right w:val="double" w:sz="12" w:space="0" w:color="ED7D31"/>
          <w:insideH w:val="double" w:sz="12" w:space="0" w:color="ED7D31"/>
          <w:insideV w:val="double" w:sz="12" w:space="0" w:color="ED7D31"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr><w:tc><w:p><w:r><w:t>T2 [1,1]</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>T2 [1,2]</w:t></w:r></w:p></w:tc></w:tr>
      <w:tr><w:tc><w:p><w:r><w:t>T2 [2,1]</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>T2 [2,2]</w:t></w:r></w:p></w:tc></w:tr>
    </w:tbl>

    <!-- Table 3: Dashed Borders -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Table 3: Dashed &amp; Dotted Borders</w:t></w:r></w:p>
    <w:tbl>
      <w:tblPr>
        <w:tblBorders>
          <w:top w:val="dashed" w:sz="12" w:space="0" w:color="70AD47"/>
          <w:left w:val="dashed" w:sz="12" w:space="0" w:color="70AD47"/>
          <w:bottom w:val="dashed" w:sz="12" w:space="0" w:color="70AD47"/>
          <w:right w:val="dashed" w:sz="12" w:space="0" w:color="70AD47"/>
          <w:insideH w:val="dotted" w:sz="8" w:space="0" w:color="70AD47"/>
          <w:insideV w:val="dotted" w:sz="8" w:space="0" w:color="70AD47"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr><w:tc><w:p><w:r><w:t>T3 [1,1]</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>T3 [1,2]</w:t></w:r></w:p></w:tc></w:tr>
      <w:tr><w:tc><w:p><w:r><w:t>T3 [2,1]</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>T3 [2,2]</w:t></w:r></w:p></w:tc></w:tr>
    </w:tbl>

    <!-- Table 4: 3D Emboss Borders -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Table 4: 3D Emboss Borders</w:t></w:r></w:p>
    <w:tbl>
      <w:tblPr>
        <w:tblBorders>
          <w:top w:val="threeDEmboss" w:sz="18" w:space="0" w:color="auto"/>
          <w:left w:val="threeDEmboss" w:sz="18" w:space="0" w:color="auto"/>
          <w:bottom w:val="threeDEmboss" w:sz="18" w:space="0" w:color="auto"/>
          <w:right w:val="threeDEmboss" w:sz="18" w:space="0" w:color="auto"/>
          <w:insideH w:val="threeDEmboss" w:sz="18" w:space="0" w:color="auto"/>
          <w:insideV w:val="threeDEmboss" w:sz="18" w:space="0" w:color="auto"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr><w:tc><w:p><w:r><w:t>T4 [1,1]</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>T4 [1,2]</w:t></w:r></w:p></w:tc></w:tr>
    </w:tbl>

    <!-- Table 5: Cell Shading -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Table 5: Cell Shading</w:t></w:r></w:p>
    <w:tbl>
      <w:tr>
        <w:tc><w:tcPr><w:shd w:val="clear" w:color="auto" w:fill="4472C4"/></w:tcPr><w:p><w:r><w:t>Accent 1</w:t></w:r></w:p></w:tc>
        <w:tc><w:tcPr><w:shd w:val="clear" w:color="auto" w:fill="ED7D31"/></w:tcPr><w:p><w:r><w:t>Accent 2</w:t></w:r></w:p></w:tc>
        <w:tc><w:tcPr><w:shd w:val="clear" w:color="auto" w:fill="70AD47"/></w:tcPr><w:p><w:r><w:t>Accent 3</w:t></w:r></w:p></w:tc>
      </w:tr>
    </w:tbl>

    <w:sectPr><w:pgSz w:w="12240" w:h="15840"/></w:sectPr>
  </w:body>
</w:document>'''

parts_table = dict(base_parts)
parts_table["word/document.xml"] = table_doc_xml
create_docx(os.path.join(SUITES_DIR, "table_borders_suite.docx"), parts_table)
print("  Created Suite: table_borders_suite.docx")

# C. OMML Math Equations Suite
math_doc_xml = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
            xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
  <w:body>
    <w:p><w:r><w:rPr><w:b/><w:sz w:val="36"/></w:rPr><w:t>GenOffice Canonical OMML Math Equations Test Suite</w:t></w:r></w:p>

    <!-- Quadratic Formula: x = (-b +/- sqrt(b^2 - 4ac)) / (2a) -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Formula: Quadratic Formula</w:t></w:r></w:p>
    <w:p>
      <m:oMathPara>
        <m:oMath>
          <m:r><m:t>x=</m:t></m:r>
          <m:f>
            <m:num>
              <m:r><m:t>-b±</m:t></m:r>
              <m:rad>
                <m:deg/>
                <m:e>
                  <m:sSup><m:e><m:r><m:t>b</m:t></m:r></m:e><m:sup><m:r><m:t>2</m:t></m:r></m:sup></m:sSup>
                  <m:r><m:t>-4ac</m:t></m:r>
                </m:e>
              </m:rad>
            </m:num>
            <m:den><m:r><m:t>2a</m:t></m:r></m:den>
          </m:f>
        </m:oMath>
      </m:oMathPara>
    </w:p>

    <!-- Einstein Mass-Energy: E = mc^2 -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Formula: Einstein Mass-Energy Equivalence</w:t></w:r></w:p>
    <w:p>
      <m:oMath>
        <m:r><m:t>E=m</m:t></m:r>
        <m:sSup><m:e><m:r><m:t>c</m:t></m:r></m:e><m:sup><m:r><m:t>2</m:t></m:r></m:sup></m:sSup>
      </m:oMath>
    </w:p>

    <!-- Pythagorean Theorem: a^2 + b^2 = c^2 -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Formula: Pythagorean Theorem</w:t></w:r></w:p>
    <w:p>
      <m:oMath>
        <m:sSup><m:e><m:r><m:t>a</m:t></m:r></m:e><m:sup><m:r><m:t>2</m:t></m:r></m:sup></m:sSup>
        <m:r><m:t>+</m:t></m:r>
        <m:sSup><m:e><m:r><m:t>b</m:t></m:r></m:e><m:sup><m:r><m:t>2</m:t></m:r></m:sup></m:sSup>
        <m:r><m:t>=</m:t></m:r>
        <m:sSup><m:e><m:r><m:t>c</m:t></m:r></m:e><m:sup><m:r><m:t>2</m:t></m:r></m:sup></m:sSup>
      </m:oMath>
    </w:p>

    <!-- Definite Integral: \int_a^b f(x)dx = F(b) - F(a) -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Formula: Definite Integral</w:t></w:r></w:p>
    <w:p>
      <m:oMath>
        <m:nary>
          <m:naryPr><m:chr m:val="∫"/><m:limLoc m:val="subSup"/></m:naryPr>
          <m:sub><m:r><m:t>a</m:t></m:r></m:sub>
          <m:sup><m:r><m:t>b</m:t></m:r></m:sup>
          <m:e><m:r><m:t>f(x)dx=F(b)-F(a)</m:t></m:r></m:e>
        </m:nary>
      </m:oMath>
    </w:p>

    <!-- Matrix 2x2: [[a, b], [c, d]] -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Formula: 2x2 Matrix</w:t></w:r></w:p>
    <w:p>
      <m:oMath>
        <m:d>
          <m:dPr><m:begChr m:val="["/><m:endChr m:val="]"/></m:dPr>
          <m:e>
            <m:m>
              <m:mr><m:e><m:r><m:t>a</m:t></m:r></m:e><m:e><m:r><m:t>b</m:t></m:r></m:e></m:mr>
              <m:mr><m:e><m:r><m:t>c</m:t></m:r></m:e><m:e><m:r><m:t>d</m:t></m:r></m:e></m:mr>
            </m:m>
          </m:e>
        </m:d>
      </m:oMath>
    </w:p>

    <w:sectPr><w:pgSz w:w="12240" w:h="15840"/></w:sectPr>
  </w:body>
</w:document>'''

parts_math = dict(base_parts)
parts_math["word/document.xml"] = math_doc_xml
create_docx(os.path.join(SUITES_DIR, "math_omml_suite.docx"), parts_math)
print("  Created Suite: math_omml_suite.docx")

# -----------------------------------------------------------------------------
# 3. SCENARIOS GENERATION (1 to 5)
# -----------------------------------------------------------------------------
scenarios = [
    ("scenario_1_business_plan.docx", "BUSINESS PLAN &amp; FINANCIAL FORECAST 2026", "cycle", "basic_cycle", True),
    ("scenario_2_org_chart.docx", "ENTERPRISE GROUP CORPORATE ORGANIZATION CHART", "hierarchy", "organization_chart", False),
    ("scenario_3_iso_process.docx", "ISO 9001:2015 QUALITY ASSURANCE PIPELINE PROCESS", "process", "basic_chevron_process", False),
    ("scenario_4_stem_paper.docx", "RESEARCH PAPER: NUMERICAL METHODS AND DYNAMICAL SYSTEMS", None, None, True),
    ("scenario_5_market_matrix.docx", "MARKET MATRIX POSITIONING &amp; TARGET SEGMENTATION", "matrix", "basic_matrix", False),
]

for fname, title, sa_cat, sa_type, has_math in scenarios:
    sc_doc = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
            xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
  <w:body>
    <w:p><w:r><w:rPr><w:b/><w:sz w:val="36"/></w:rPr><w:t>{title}</w:t></w:r></w:p>
    
    <!-- Scenario Table -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Forecast &amp; Milestone Data:</w:t></w:r></w:p>
    <w:tbl>
      <w:tblPr>
        <w:tblBorders>
          <w:top w:val="double" w:sz="12" w:space="0" w:color="4472C4"/>
          <w:left w:val="double" w:sz="12" w:space="0" w:color="4472C4"/>
          <w:bottom w:val="double" w:sz="12" w:space="0" w:color="4472C4"/>
          <w:right w:val="double" w:sz="12" w:space="0" w:color="4472C4"/>
          <w:insideH w:val="single" w:sz="8" w:space="0" w:color="4472C4"/>
          <w:insideV w:val="single" w:sz="8" w:space="0" w:color="4472C4"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr>
        <w:tc><w:tcPr><w:shd w:val="clear" w:color="auto" w:fill="4472C4"/></w:tcPr><w:p><w:r><w:rPr><w:b/><w:color w:val="FFFFFF"/></w:rPr><w:t>Phase / Qtr</w:t></w:r></w:p></w:tc>
        <w:tc><w:tcPr><w:shd w:val="clear" w:color="auto" w:fill="4472C4"/></w:tcPr><w:p><w:r><w:rPr><w:b/><w:color w:val="FFFFFF"/></w:rPr><w:t>Target Metric</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Stage 1</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Initial Rollout</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Stage 2</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Full Operational Delivery</w:t></w:r></w:p></w:tc>
      </w:tr>
    </w:tbl>'''

    if has_math:
        sc_doc += '''
    <!-- Math ROI Metric -->
    <w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Operational Metric Formula:</w:t></w:r></w:p>
    <w:p>
      <m:oMath>
        <m:r><m:t>Metric=</m:t></m:r>
        <m:f>
          <m:num><m:r><m:t>Total Value</m:t></m:r></m:num>
          <m:den><m:r><m:t>Operational Cost</m:t></m:r></m:den>
        </m:f>
        <m:r><m:t>×100%</m:t></m:r>
      </m:oMath>
    </w:p>'''

    sc_doc += '''
    <w:sectPr><w:pgSz w:w="12240" w:h="15840"/></w:sectPr>
  </w:body>
</w:document>'''

    parts_sc = dict(base_parts)
    parts_sc["word/document.xml"] = sc_doc
    if sa_cat and sa_type:
        s_defn = next((d for d in smartart_definitions if d["cat"] == sa_cat and d["type"] == sa_type), None)
        if s_defn:
            parts_sc["word/diagrams/drawing1.xml"] = build_smartart_drawing1(s_defn)
            parts_sc["word/diagrams/data1.xml"] = build_smartart_data1(s_defn)

    create_docx(os.path.join(SCENARIOS_DIR, fname), parts_sc)
    print(f"  Created Scenario: {fname}")

print("\nAll canonical samples packaged successfully!")
