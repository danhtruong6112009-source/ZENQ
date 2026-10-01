# -*- coding: utf-8 -*-
"""
Assembles all categories and writes src/ui/WpfShapeLibrary.cs
"""

import os
import sys

# Add scratch dir to sys.path
sys.path.append(os.path.dirname(__file__))

import cat1_lines
import cat2_basic
import cat3_polygons
import cat4_stars
import cat5_arrows
import cat6_flowchart
import cat7_callouts
import cat8_banners
import cat9_3d
import cat10_diagrams
import cat11_math
import cat12_decorations

out_path = r"C:\Users\Danh\Desktop\genofficepy\src\ui\WpfShapeLibrary.cs"

with open(r"C:\Users\Danh\Desktop\genofficepy\scratch\make_library_full.py", "r", encoding="utf-8") as f:
    header = f.read()

parts = [
    header,
    cat1_lines.get_code(),
    cat2_basic.get_code(),
    cat3_polygons.get_code(),
    cat4_stars.get_code(),
    cat5_arrows.get_code(),
    cat6_flowchart.get_code(),
    cat7_callouts.get_code(),
    cat8_banners.get_code(),
    cat9_3d.get_code(),
    cat10_diagrams.get_code(),
    cat11_math.get_code(),
    cat12_decorations.get_code(),
    "\n    }\n}\n"
]

full_code = "".join(parts)

with open(out_path, "w", encoding="utf-8") as f:
    f.write(full_code)

print(f"Successfully generated {out_path} ({len(full_code)} chars)")
