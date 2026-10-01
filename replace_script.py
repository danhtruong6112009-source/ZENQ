import os
import re

file_path = r"c:\Users\Danh\Desktop\genofficepy\src\ui\SmartArtGalleryForm.cs"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

type_ids = []
for match in re.finditer(r'new SmartArtTemplate\("[^"]+",\s*"([^"]+)"', content):
    type_ids.append(match.group(1))

mappings = []
for tid in type_ids:
    # 1. Exact matches for NEW specific preview methods
    if tid in ["List_IncreasingCircle", "List_VerticalCircle"]:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_IncreasingCircles(_g, _r); }};')
    elif tid == "List_PlusMinus":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_PlusMinus(_g, _r); }};')
    elif tid == "List_Reverse":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_Reverse(_g, _r); }};')
    elif tid == "List_Stacked":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_Stacked(_g, _r); }};')
    elif tid == "List_Lined":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_Lined(_g, _r); }};')
    elif tid in ["List_Grouped", "Rel_GroupedList"]:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_Grouped(_g, _r); }};')
    elif tid == "List_SquareAccent":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_SquareAccent(_g, _r); }};')
    elif tid == "List_VaryingWidth":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_VaryingWidth(_g, _r); }};')
    elif tid in ["Process_CircleArrow", "Cycle_CircleArrowProcess"]:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_CircleArrow(_g, _r); }};')
    elif tid == "Process_Phased":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_Phased(_g, _r); }};')
    elif tid in ["Process_PieProcess", "List_PieProcess"]:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_Pie(_g, _r); }};')
    elif tid in ["Process_Vertical", "Process_VerticalArrowList"]:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_VerticalProcess(_g, _r); }};')
    elif tid in ["Process_ContinuousArrow", "List_VerticalArrow"]:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_ContinuousArrow(_g, _r); }};')
    elif tid in ["Rel_SegmentedPyramid", "Pyramid_Segmented"]:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_SegmentedPyramid(_g, _r); }};')
    elif tid == "Picture_Spiral":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_Spiral(_g, _r); }};')
    elif tid == "Picture_Strips":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPreview_VerticalPictureStrips(_g, _r); }};')
    
    # 2. Existing catch-all categorizations
    elif "Gear" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawGearPreview(_g, _r); }};')
    elif "Funnel" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawFunnelPreview(_g, _r); }};')
    elif "Balance" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawBalancePreview(_g, _r); }};')
    elif "Target" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawTargetPreview(_g, _r); }};')
    elif "Venn" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawVennPreview(_g, _r, "{tid}"); }};')
    elif "Equation" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawEquationPreview(_g, _r, "{tid}"); }};')
    elif "Tab" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawTabPreview(_g, _r); }};')
    elif "Bracket" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawBracketPreview(_g, _r); }};')
    elif "Snapshot" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawSnapshotPreview(_g, _r); }};')
    elif "Bubble" in tid or "AlternatingCircles" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawBubblePreview(_g, _r); }};')
    elif "Hexagon" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawHexagonPreview(_g, _r, "{tid}"); }};')
    elif "Pyramid" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPyramidPreview(_g, _r, "{tid}"); }};')
    elif "Matrix" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawMatrixPreview(_g, _r, "{tid}"); }};')
    elif "Timeline" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawTimelinePreview(_g, _r, "{tid}"); }};')
    elif "Chevron" in tid or "VerticalAccent" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawChevronPreview(_g, _r, "{tid}"); }};')
    elif "Step" in tid or "Staggered" in tid or "Descending" in tid or "Ascending" in tid or "IncreasingArrows" in tid or "UpwardArrow" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawStepPreview(_g, _r, "{tid}"); }};')
    elif "Opposing" in tid or "Converging" in tid or "Diverging" in tid or "Ribbon" in tid or "CounterbalanceArrows" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawArrowsPreview(_g, _r, "{tid}"); }};')
    elif tid.startswith("Hierarchy_") or "Org" in tid or "Tree" in tid or tid in ["List_Hierarchy", "Rel_HierarchyList", "List_TableHierarchy", "Rel_TableHierarchy", "List_Table"]:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawHierarchyPreview(_g, _r, "{tid}"); }};')
    elif tid.startswith("Picture_") or "Picture" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawPicturePreview(_g, _r, "{tid}"); }};')
    elif tid.startswith("Cycle_") or "Cycle" in tid or "Radial" in tid or tid == "Rel_BasicPie":
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawCyclePreview(_g, _r, "{tid}"); }};')
    elif tid.startswith("Process_") or "Flow" in tid or "Bending" in tid or "SegmentedProcess" in tid or "DetailedProcess" in tid:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawProcessPreview(_g, _r, "{tid}"); }};')
    elif tid.startswith("Office_"):
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawOfficeSpecificPreview(_g, _r, "{tid}"); }};')
    else:
        mappings.append(f'            previewDispatch["{tid}"] = delegate(Graphics _g, Rectangle _r) {{ DrawListPreview(_g, _r, "{tid}"); }};')

mappings_str = "\n".join(mappings)

new_init_code = f"""
        private Dictionary<string, Action<Graphics, Rectangle>> previewDispatch;

        private void InitPreviewDispatch()
        {{
            previewDispatch = new Dictionary<string, Action<Graphics, Rectangle>>();
{mappings_str}
        }}
"""

content = content.replace("private List<SmartArtTemplate> filteredTemplates = new List<SmartArtTemplate>();", new_init_code + "\n        private List<SmartArtTemplate> filteredTemplates = new List<SmartArtTemplate>();")

old_draw_office = re.search(r'private void DrawOfficePreviewDiagram\(Graphics g, Rectangle r, string typeId\)\s*\{.*?\n        \}', content, re.DOTALL)
if old_draw_office:
    new_draw_office = """private void DrawOfficePreviewDiagram(Graphics g, Rectangle r, string typeId)
        {
            if (previewDispatch == null) InitPreviewDispatch();
            Action<Graphics, Rectangle> drawer;
            if (previewDispatch.TryGetValue(typeId, out drawer))
            {
                drawer(g, r);
            }
            else
            {
                // Fallback for any unknown type
                DrawListPreview(g, r, typeId);
            }
        }"""
    content = content.replace(old_draw_office.group(0), new_draw_office)

# Try replacing DrawRelationshipPreview more precisely
content = re.sub(r'private void DrawRelationshipPreview\(Graphics g, Rectangle r, string typeId\)\s*\{\s*DrawProcessPreview\(g,\s*r,\s*typeId\);\s*\}', 
                 'private void DrawRelationshipPreview(Graphics g, Rectangle r, string typeId)\n        {\n            // Obsolete, replaced by exact dispatch\n        }', content)

new_methods = """
        private void DrawPreview_IncreasingCircles(Graphics g, Rectangle r)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(91, 155, 213)))
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(237, 125, 49)))
            using (SolidBrush b3 = new SolidBrush(Color.FromArgb(112, 173, 71)))
            {
                g.FillEllipse(b1, cx - 60, cy - 10, 20, 20);
                g.FillEllipse(b2, cx - 30, cy - 15, 30, 30);
                g.FillEllipse(b3, cx + 10, cy - 20, 40, 40);
            }
        }

        private void DrawPreview_PlusMinus(Graphics g, Rectangle r)
        {
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 20, r.Width / 2 - 25, 40), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 2, false);
            DrawGradientNode(g, new Rectangle(r.X + r.Width / 2 + 5, r.Y + 20, r.Width / 2 - 25, 40), Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.Black, 2, false);
            using (Font f = new Font("Segoe UI", 16, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(Color.White))
            {
                g.DrawString("+", f, b, r.X + 35, r.Y + 25);
                g.DrawString("-", f, b, r.X + r.Width / 2 + 20, r.Y + 25);
            }
        }

        private void DrawPreview_Reverse(Graphics g, Rectangle r)
        {
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 30, 60, 40), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 5, false);
            DrawGradientNode(g, new Rectangle(r.X + 120, r.Y + 30, 60, 40), Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.Black, 5, false);
            using (Pen p = new Pen(Color.Gray, 3))
            {
                g.DrawLine(p, r.X + 85, r.Y + 40, r.X + 115, r.Y + 40);
                g.DrawLine(p, r.X + 85, r.Y + 60, r.X + 115, r.Y + 60);
            }
        }

        private void DrawPreview_Stacked(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 3; i++)
            {
                int y = r.Y + 15 + i * 25;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(91, 155, 213)))
                {
                    g.FillEllipse(b, r.X + 20, y, 20, 20);
                }
                DrawGradientNode(g, new Rectangle(r.X + 45, y, 100, 20), Color.FromArgb(225, 225, 225), Color.LightGray, Color.Gray, 2, false);
            }
        }

        private void DrawPreview_Lined(Graphics g, Rectangle r)
        {
            using (Pen p = new Pen(Color.Gray, 2))
            {
                for (int i = 0; i < 4; i++)
                {
                    int y = r.Y + 20 + i * 20;
                    g.DrawLine(p, r.X + 20, y, r.X + 180, y);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(91, 155, 213)))
                    {
                        g.FillRectangle(b, r.X + 25, y + 2, 80, 10);
                    }
                }
            }
        }

        private void DrawPreview_Grouped(Graphics g, Rectangle r)
        {
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 20, 70, 70), Color.LightGray, Color.Gray, Color.DarkGray, 5, false);
            DrawGradientNode(g, new Rectangle(r.X + 110, r.Y + 20, 70, 70), Color.LightGray, Color.Gray, Color.DarkGray, 5, false);
            for(int i=0; i<2; i++)
            {
                for(int j=0; j<3; j++)
                {
                    DrawGradientNode(g, new Rectangle(r.X + 25 + i*90, r.Y + 25 + j*20, 60, 15), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 2, false);
                }
            }
        }

        private void DrawPreview_SquareAccent(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 3; i++)
            {
                int y = r.Y + 20 + i * 30;
                using (SolidBrush b = new SolidBrush(i == 0 ? Color.FromArgb(91, 155, 213) : (i == 1 ? Color.FromArgb(237, 125, 49) : Color.FromArgb(112, 173, 71))))
                {
                    g.FillRectangle(b, r.X + 20, y, 20, 20);
                }
                DrawGradientNode(g, new Rectangle(r.X + 45, y, 120, 20), Color.FromArgb(240, 240, 240), Color.LightGray, Color.Gray, 0, false);
            }
        }

        private void DrawPreview_VaryingWidth(Graphics g, Rectangle r)
        {
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 20, 140, 20), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 2, false);
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 50, 100, 20), Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.Black, 2, false);
            DrawGradientNode(g, new Rectangle(r.X + 20, r.Y + 80, 160, 20), Color.FromArgb(112, 173, 71), Color.FromArgb(83, 129, 53), Color.Black, 2, false);
        }

        private void DrawPreview_CircleArrow(Graphics g, Rectangle r)
        {
            using (Pen p = new Pen(Color.FromArgb(91, 155, 213), 5))
            {
                g.DrawArc(p, r.X + 50, r.Y + 20, 100, 60, 0, 270);
            }
            using (SolidBrush b = new SolidBrush(Color.FromArgb(237, 125, 49)))
            {
                g.FillEllipse(b, r.X + 90, r.Y + 10, 20, 20);
                g.FillEllipse(b, r.X + 140, r.Y + 40, 20, 20);
                g.FillEllipse(b, r.X + 90, r.Y + 70, 20, 20);
            }
        }

        private void DrawPreview_Phased(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 3; i++)
            {
                int x = r.X + 10 + i * 60;
                DrawGradientNode(g, new Rectangle(x, r.Y + 30, 50, 50), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 3, false);
                using (Pen p = new Pen(Color.Gray, 2))
                {
                    g.DrawLine(p, x + 10, r.Y + 90, x + 40, r.Y + 90);
                }
            }
        }

        private void DrawPreview_Pie(Graphics g, Rectangle r)
        {
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(91, 155, 213)))
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(237, 125, 49)))
            using (SolidBrush b3 = new SolidBrush(Color.FromArgb(112, 173, 71)))
            {
                g.FillPie(b1, r.X + 50, r.Y + 20, 100, 100, 0, 120);
                g.FillPie(b2, r.X + 45, r.Y + 15, 100, 100, 120, 120);
                g.FillPie(b3, r.X + 55, r.Y + 15, 100, 100, 240, 120);
            }
        }

        private void DrawPreview_VerticalProcess(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 3; i++)
            {
                int y = r.Y + 10 + i * 30;
                DrawGradientNode(g, new Rectangle(r.X + 50, y, 100, 20), Color.FromArgb(91, 155, 213), Color.FromArgb(43, 91, 136), Color.Black, 2, false);
                if (i < 2)
                {
                    using (SolidBrush b = new SolidBrush(Color.Gray))
                    {
                        g.FillPolygon(b, new PointF[] { new PointF(r.X + 100, y + 20), new PointF(r.X + 95, y + 28), new PointF(r.X + 105, y + 28) });
                    }
                }
            }
        }

        private void DrawPreview_ContinuousArrow(Graphics g, Rectangle r)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(91, 155, 213)))
            {
                g.FillPolygon(b, new PointF[] { 
                    new PointF(r.X + 20, r.Y + 40), 
                    new PointF(r.X + 160, r.Y + 40), 
                    new PointF(r.X + 160, r.Y + 30),
                    new PointF(r.X + 190, r.Y + 50),
                    new PointF(r.X + 160, r.Y + 70),
                    new PointF(r.X + 160, r.Y + 60),
                    new PointF(r.X + 20, r.Y + 60)
                });
            }
            using (SolidBrush wt = new SolidBrush(Color.White))
            using (Font f = new Font("Segoe UI", 8))
            {
                g.DrawString("Step 1 -> Step 2 -> Step 3", f, wt, r.X + 30, r.Y + 42);
            }
        }

        private void DrawPreview_SegmentedPyramid(Graphics g, Rectangle r)
        {
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(91, 155, 213)))
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(237, 125, 49)))
            using (SolidBrush b3 = new SolidBrush(Color.FromArgb(112, 173, 71)))
            {
                g.FillPolygon(b1, new PointF[] { new PointF(r.X + 100, r.Y + 10), new PointF(r.X + 85, r.Y + 40), new PointF(r.X + 115, r.Y + 40) });
                g.FillPolygon(b2, new PointF[] { new PointF(r.X + 80, r.Y + 45), new PointF(r.X + 120, r.Y + 45), new PointF(r.X + 135, r.Y + 75), new PointF(r.X + 65, r.Y + 75) });
                g.FillPolygon(b3, new PointF[] { new PointF(r.X + 60, r.Y + 80), new PointF(r.X + 140, r.Y + 80), new PointF(r.X + 155, r.Y + 110), new PointF(r.X + 45, r.Y + 110) });
            }
        }

        private void DrawPreview_Spiral(Graphics g, Rectangle r)
        {
            for (int i = 0; i < 4; i++)
            {
                int x = r.X + 80 + (int)(os.sys.modules['math'].cos(i * 1.5) * i * 15) if hasattr(os, 'sys') else r.X + 80 + [0, 1, -2, -3][i]*5
                int y = r.Y + 50 + [0, 1, 2, -3][i]*5
                # Wait, python generating C# code! No need for eval here, just hardcode
            }
        }
"""

new_methods = new_methods.replace("int x = r.X + 80 + (int)(os.sys.modules['math'].cos(i * 1.5) * i * 15) if hasattr(os, 'sys') else r.X + 80 + [0, 1, -2, -3][i]*5", "int x = r.X + 80 + (int)(Math.Cos(i * 1.5) * i * 15);")
new_methods = new_methods.replace("int y = r.Y + 50 + [0, 1, 2, -3][i]*5", "int y = r.Y + 50 + (int)(Math.Sin(i * 1.5) * i * 15);")
new_methods = new_methods.replace("# Wait, python generating C# code! No need for eval here, just hardcode", "DrawPicturePlaceholder(g, new Rectangle(x, y, 30, 30), false);")


content = content.replace("        // =========================================================================\n        // VECTOR GRAPHICS HELPERS (C# 5 COMPATIBLE)", new_methods + "\n        // =========================================================================\n        // VECTOR GRAPHICS HELPERS (C# 5 COMPATIBLE)")

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("Done replacing.")
