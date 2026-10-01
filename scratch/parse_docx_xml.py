import xml.etree.ElementTree as ET
import sys

def parse_xml(file_path):
    tree = ET.parse(file_path)
    root = tree.getroot()
    
    # Namespaces
    ns = {
        'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main',
        'wp': 'http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing',
        'a': 'http://schemas.openxmlformats.org/drawingml/2006/main',
        'pic': 'http://schemas.openxmlformats.org/drawingml/2006/picture'
    }
    
    print("--- FONT SIZES (w:sz) ---")
    sizes = set()
    for sz in root.findall('.//w:sz', ns):
        sizes.add(sz.get(f"{{{ns['w']}}}val"))
    for sz in root.findall('.//w:szCs', ns):
        sizes.add(sz.get(f"{{{ns['w']}}}val"))
    print(sorted(sizes, key=lambda x: int(x) if x and x.isdigit() else 0))
    
    print("\n--- IMAGES (wp:extent) ---")
    for drawing in root.findall('.//w:drawing', ns):
        extent = drawing.find('.//wp:extent', ns)
        if extent is not None:
            cx = extent.get('cx')
            cy = extent.get('cy')
            print(f"Image extent: cx={cx} ({int(cx)/360000:.2f} cm), cy={cy} ({int(cy)/360000:.2f} cm)")
            
if __name__ == "__main__":
    parse_xml(sys.argv[1])
