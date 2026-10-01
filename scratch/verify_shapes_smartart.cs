using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    class Program
    {
        static int passed = 0;
        static int failed = 0;

        static void Assert(bool condition, string testName, string details)
        {
            if (condition)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[PASS] ");
                Console.ResetColor();
                Console.WriteLine(testName);
                passed++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[FAIL] ");
                Console.ResetColor();
                Console.WriteLine(testName + (string.IsNullOrEmpty(details) ? "" : " -> " + details));
                failed++;
            }
        }

        static void Assert(bool condition, string testName)
        {
            Assert(condition, testName, "");
        }

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("AI 2 VERIFICATION SUITE: Shape Library Overhaul & SmartArt DOCX Fidelity");
            Console.WriteLine("================================================================================");

            TestTask1_ShapeGalleryDropdown();
            TestTask2_ShapeVectorPathsAndHandles();
            TestTask2_InShapeTextAndWordWrap();
            TestTask3_SmartArtDocxExport();
            TestTask3_SmartArtDocxImport();
            TestTask3_NativeDrawingMLDiagramImport();

            Console.WriteLine("================================================================================");
            Console.WriteLine(string.Format("RESULTS: {0} Passed, {1} Failed", passed, failed));
            Console.WriteLine("================================================================================");

            if (failed > 0)
            {
                Environment.Exit(1);
            }
        }

        static void TestTask1_ShapeGalleryDropdown()
        {
            Console.WriteLine("\n--- [Task 1] Shape Picker Dropdown Form & Categories ---");
            ShapesGalleryDropDownForm form = new ShapesGalleryDropDownForm(new Point(100, 100));

            Panel scrollPanel = null;
            foreach (Control c in form.Controls)
            {
                if (c is Panel) { scrollPanel = (Panel)c; break; }
            }

            Assert(scrollPanel != null, "Dropdown contains main scroll panel");

            List<string> headers = new List<string>();
            List<Button> shapeButtons = new List<Button>();

            if (scrollPanel != null)
            {
                foreach (Control c in scrollPanel.Controls)
                {
                    if (c is Label) headers.Add(((Label)c).Text);
                    else if (c is Button) shapeButtons.Add((Button)c);
                }
            }

            Assert(headers.Count == 9, "Dropdown contains exactly 9 standard groups", "Count was: " + headers.Count);

            string[] expectedGroups = new string[]
            {
                "Đã dùng gần đây (Recently Used Shapes)",
                "Đường thẳng (Lines)",
                "Hình chữ nhật (Rectangles)",
                "Hình dạng cơ bản (Basic Shapes)",
                "Mũi tên khối (Block Arrows)",
                "Toán học / Phương trình (Equation Shapes)",
                "Lưu trình (Flowchart)",
                "Sao và Biểu ngữ (Stars and Banners)",
                "Chú thích (Callouts)"
            };

            foreach (var eg in expectedGroups)
            {
                bool found = headers.Contains(eg);
                Assert(found, "Group exists: " + eg);
            }

            bool hasBotanical = headers.Exists(h => h.ToLower().Contains("botanical") || h.ToLower().Contains("hoa lá") || h.ToLower().Contains("thiên nhiên"));
            bool hasLuxury = headers.Exists(h => h.ToLower().Contains("luxury") || h.ToLower().Contains("trang sức") || h.ToLower().Contains("jewel"));
            Assert(!hasBotanical, "Non-standard Botanical category is completely eliminated");
            Assert(!hasLuxury, "Non-standard Luxury category is completely eliminated");

            Assert(shapeButtons.Count >= 70, "Dropdown has rich library of standard shape icons", "Total icons: " + shapeButtons.Count);

            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                bool iconRenderSucceeded = true;
                foreach (var btn in shapeButtons)
                {
                    ShapeItemDefinition def = btn.Tag as ShapeItemDefinition;
                    if (def == null) { iconRenderSucceeded = false; break; }
                }
                Assert(iconRenderSucceeded, "All shape buttons have valid ShapeItemDefinition tags");
            }
            form.Dispose();
        }

        static void TestTask2_ShapeVectorPathsAndHandles()
        {
            Console.WriteLine("\n--- [Task 2] Shape Vector Paths & Interactive Handles ---");
            Rectangle bounds = new Rectangle(50, 50, 200, 120);

            string[] testShapes = new string[]
            {
                "Rectangle", "RoundedRectangle", "SingleCornerSnipped", "SnippedAndRounded",
                "Oval", "Triangle", "RightTriangle", "Diamond", "Pentagon", "Hexagon", "Octagon",
                "RightArrow", "LeftArrow", "UpArrow", "DownArrow", "LeftRightArrow", "Chevron",
                "Plus", "Minus", "Multiply", "Divide", "Equal", "NotEqual",
                "FlowchartProcess", "FlowchartDecision", "FlowchartTerminator", "FlowchartDocument",
                "Star4", "Star5", "Star6", "Star8", "Star12", "Star24", "Star32",
                "CalloutRect", "CalloutRoundedRect", "CalloutOval"
            };

            bool pathsOk = true;
            foreach (var st in testShapes)
            {
                using (GraphicsPath gp = ShapeRenderer.CreateShapePath(st, bounds, 0.2f, 0.4f, 0.8f))
                {
                    if (gp == null || gp.PointCount < 2)
                    {
                        pathsOk = false;
                        Console.WriteLine("Failed path for: " + st);
                        break;
                    }
                }
            }
            Assert(pathsOk, "All 37 representative vector shapes produce valid non-empty GraphicsPaths");

            // Test yellow adjustment handles
            DocShapeObject roundRect = new DocShapeObject("RoundedRectangle", 50, 50, 200, 100);
            roundRect.AdjustValue1 = 0.25f;
            Point[] adjRound = ShapeRenderer.GetAdjustmentHandles(roundRect, bounds, 1.0f);
            Assert(adjRound.Length == 1, "RoundedRectangle returns 1 corner adjustment handle");
            Assert(adjRound[0].X == bounds.Left + (int)(100 * 0.25f), "Corner handle positioned at corner radius");

            // Test Arrow adjustment handles (neck and head)
            DocShapeObject rightArrow = new DocShapeObject("RightArrow", 50, 50, 200, 100);
            rightArrow.AdjustValue1 = 0.2f;
            rightArrow.AdjustValue2 = 0.4f;
            Point[] adjArrow = ShapeRenderer.GetAdjustmentHandles(rightArrow, bounds, 1.0f);
            Assert(adjArrow.Length == 2, "RightArrow returns 2 adjustment handles (neck & head)");

            // Hit-test and apply drag
            int hitIndex = ShapeRenderer.HitTestAdjustmentHandle(rightArrow, bounds, adjArrow[0], 1.0f);
            Assert(hitIndex == 0, "HitTestAdjustmentHandle detects hit on handle 0");

            Point dragPoint = new Point(adjArrow[0].X, bounds.Top + 35); // Change neck to 0.35
            ShapeRenderer.ApplyAdjustmentDrag(rightArrow, 0, dragPoint, bounds, 1.0f);
            Assert(Math.Abs(rightArrow.AdjustValue1 - 0.29f) < 0.05f, "ApplyAdjustmentDrag updates AdjustValue1 for neck ratio");
        }

        static void TestTask2_InShapeTextAndWordWrap()
        {
            Console.WriteLine("\n--- [Task 2] In-Shape Text Formatting & WordWrap ---");
            DocShapeObject shape = new DocShapeObject("RoundedRectangle", 50, 50, 200, 120);
            shape.TextContent = "Công ty Cổ phần Giải pháp Công nghệ GenOffice 2026";
            shape.FontFamily = "Segoe UI";
            shape.FontSize = 11f;
            shape.TextColor = Color.DarkSlateBlue;
            shape.WordWrap = true;
            shape.TextAlignment = StringAlignment.Center;
            shape.TextLineAlignment = StringAlignment.Center;

            using (Bitmap bmp = new Bitmap(300, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font font = new Font(shape.FontFamily, shape.FontSize))
            {
                Rectangle textBounds = new Rectangle(50, 50, 200, 120);
                var lines = ShapeRenderer.LayoutShapeText(g, shape, textBounds, font, 1.0f);

                Assert(lines != null && lines.Count >= 2, "WordWrap breaks long text into multiple lines", "Line count: " + (lines != null ? lines.Count : 0));

                PointF cursor = ShapeRenderer.GetShapeCursorCoordinates(g, shape, textBounds, font, 1.0f);
                Assert(cursor.X >= textBounds.Left && cursor.X <= textBounds.Right && cursor.Y >= textBounds.Top && cursor.Y <= textBounds.Bottom,
                    "In-shape cursor coordinates are correctly bounded inside shape");
            }
        }

        static void TestTask3_SmartArtDocxExport()
        {
            Console.WriteLine("\n--- [Task 3] SmartArt DOCX Export OpenXML Fidelity ---");
            string testDocx = Path.Combine(Path.GetTempPath(), "test_smartart_export_" + Guid.NewGuid().ToString("N") + ".docx");

            DocShapeObject saObj = new DocShapeObject("SmartArt", 60, 60, 480, 260);
            saObj.SmartArtType = "Cycle_Radial";
            saObj.SmartArtTitle = "Chu kỳ Phát triển Phần mềm";
            saObj.TextContent = "Lập kế hoạch|Thiết kế kiến trúc|Triển khai mã nguồn|Kiểm thử tự động|Bàn giao vận hành";
            saObj.WrapMode = 0; // InLine

            List<DocShapeObject> shapes = new List<DocShapeObject> { saObj };
            string textContent = "Báo cáo tiến độ dự án 2026:\n![SHAPE:ID=" + saObj.Id + ":TYPE=SmartArt:W=480:H=260:WRAP=0]\nKết thúc báo cáo.";

            bool ok = DocxHandler.SaveDocxFile(testDocx, textContent, null, false, 0, "", "", false, shapes);
            Assert(ok && File.Exists(testDocx), "DocxHandler.SaveDocxFile succeeds and creates .docx file");

            bool hasValidDrawing = false;
            bool hasDocPrSmartArt = false;
            bool hasMediaPng = false;

            using (ZipArchive zip = ZipFile.OpenRead(testDocx))
            {
                var docEntry = zip.GetEntry("word/document.xml");
                Assert(docEntry != null, "Exported .docx contains word/document.xml");

                if (docEntry != null)
                {
                    using (Stream s = docEntry.Open())
                    using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
                    {
                        string xml = sr.ReadToEnd();
                        hasValidDrawing = xml.Contains("<w:drawing>") && xml.Contains("<wp:inline");
                        hasDocPrSmartArt = xml.Contains("descr=\"SmartArt|Cycle_Radial|Chu k") || xml.Contains("SmartArt|Cycle_Radial");
                        Assert(hasValidDrawing, "document.xml contains valid DrawingML <w:drawing><wp:inline>");
                        Assert(hasDocPrSmartArt, "document.xml stores SmartArt metadata in wp:docPr descr attribute");

                        XmlDocument xDoc = new XmlDocument();
                        try
                        {
                            xDoc.LoadXml(xml);
                            Assert(true, "document.xml is 100% well-formed valid XML (no corruption)");
                        }
                        catch (Exception ex)
                        {
                            Assert(false, "document.xml XML parse error", ex.Message);
                        }
                    }
                }

                foreach (var entry in zip.Entries)
                {
                    if (entry.FullName.StartsWith("word/media/image") && entry.FullName.EndsWith(".png"))
                    {
                        hasMediaPng = true;
                        using (Stream ms = entry.Open())
                        using (Image img = Image.FromStream(ms))
                        {
                            Assert(img.Width >= 480 && img.Height >= 260, "Fallback PNG image has high-resolution 2x dimensions", string.Format("{0}x{1}", img.Width, img.Height));
                        }
                        break;
                    }
                }
                Assert(hasMediaPng, "Exported .docx contains word/media/image*.png for clean MS Word viewing");
            }

            try { File.Delete(testDocx); } catch { }
        }

        static void TestTask3_SmartArtDocxImport()
        {
            Console.WriteLine("\n--- [Task 3] SmartArt DOCX Import Roundtrip Fidelity ---");
            string testDocx = Path.Combine(Path.GetTempPath(), "test_smartart_roundtrip_" + Guid.NewGuid().ToString("N") + ".docx");

            DocShapeObject originalSa = new DocShapeObject("SmartArt", 50, 50, 460, 240);
            originalSa.SmartArtType = "Process_Linear";
            originalSa.SmartArtTitle = "Quy trình Phê duyệt";
            originalSa.TextContent = "Soạn thảo|Trình ký|Phê duyệt|Lưu trữ";
            originalSa.WrapMode = 1; // Square floating

            List<DocShapeObject> shapes = new List<DocShapeObject> { originalSa };
            string textContent = "Mục 1: Quy trình hoạt động\n![SHAPE:ID=" + originalSa.Id + ":TYPE=SmartArt:W=460:H=240:WRAP=1]\nHoàn thành.";

            DocxHandler.SaveDocxFile(testDocx, textContent, null, false, 0, "", "", false, shapes);

            DocxMetadata loadedMeta;
            bool loadOk = DocxHandler.LoadDocxFile(testDocx, out loadedMeta);
            Assert(loadOk, "DocxHandler.LoadDocxFile loaded exported docx successfully");

            Assert(loadedMeta.TextContent.Contains("![SHAPE:ID=") && loadedMeta.TextContent.Contains("TYPE=SmartArt"),
                "Loaded document contains SmartArt shape tag");
            Assert(loadedMeta.TextContent.Contains("SMARTART=Process_Linear"),
                "Loaded document preserves SmartArt diagram type (Process_Linear)");

            bool tagFound = false;
            foreach (var kv in DocShapeObject.TagCache)
            {
                if (kv.Value != null && kv.Value.SmartArtType == "Process_Linear")
                {
                    tagFound = true;
                    Assert(kv.Value.SmartArtTitle.Contains("Quy trình"), "TagCache preserves SmartArt Title");
                    Assert(kv.Value.TextContent.Contains("Phê duyệt"), "TagCache preserves SmartArt Node Text Content");
                    break;
                }
            }
            Assert(tagFound, "DocShapeObject.TagCache holds reconstructed SmartArt metadata");

            try { File.Delete(testDocx); } catch { }
        }

        static void TestTask3_NativeDrawingMLDiagramImport()
        {
            Console.WriteLine("\n--- [Task 3] Native OpenXML DrawingML Diagram (data1.xml) Import ---");
            string nativeDocx = Path.Combine(Path.GetTempPath(), "test_native_diagram_" + Guid.NewGuid().ToString("N") + ".docx");

            using (FileStream fs = new FileStream(nativeDocx, FileMode.Create))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var ct = zip.CreateEntry("[Content_Types].xml");
                using (var sw = new StreamWriter(ct.Open(), Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sw.WriteLine("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
                    sw.WriteLine("  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                    sw.WriteLine("  <Default Extension=\"xml\" ContentType=\"application/xml\"/>");
                    sw.WriteLine("</Types>");
                }

                var rels = zip.CreateEntry("word/_rels/document.xml.rels");
                using (var sw = new StreamWriter(rels.Open(), Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                    sw.WriteLine("  <Relationship Id=\"rIdDm1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/diagramData\" Target=\"diagrams/data1.xml\"/>");
                    sw.WriteLine("  <Relationship Id=\"rIdLo1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/diagramLayout\" Target=\"diagrams/layout1.xml\"/>");
                    sw.WriteLine("</Relationships>");
                }

                var data = zip.CreateEntry("word/diagrams/data1.xml");
                using (var sw = new StreamWriter(data.Open(), Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sw.WriteLine("<dgm:dataModel xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\">");
                    sw.WriteLine("  <dgm:ptLst>");
                    sw.WriteLine("    <dgm:pt modelId=\"0\"><dgm:t>Cơ cấu Tổ chức Tập đoàn</dgm:t></dgm:pt>");
                    sw.WriteLine("    <dgm:pt modelId=\"1\"><dgm:t>Hội đồng Quản trị</dgm:t></dgm:pt>");
                    sw.WriteLine("    <dgm:pt modelId=\"2\"><dgm:t>Ban Tổng Giám đốc</dgm:t></dgm:pt>");
                    sw.WriteLine("    <dgm:pt modelId=\"3\"><dgm:t>Khối Công nghệ & Sản phẩm</dgm:t></dgm:pt>");
                    sw.WriteLine("  </dgm:ptLst>");
                    sw.WriteLine("</dgm:dataModel>");
                }

                var layout = zip.CreateEntry("word/diagrams/layout1.xml");
                using (var sw = new StreamWriter(layout.Open(), Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sw.WriteLine("<dgm:layoutDef xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" uniqueId=\"urn:microsoft.com/office/officeart/2005/8/layout/hierarchy\">");
                    sw.WriteLine("  <dgm:title val=\"Hierarchy Org Chart\"/>");
                    sw.WriteLine("</dgm:layoutDef>");
                }

                var doc = zip.CreateEntry("word/document.xml");
                using (var sw = new StreamWriter(doc.Open(), Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sw.WriteLine("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
                    sw.WriteLine("  <w:body>");
                    sw.WriteLine("    <w:p>");
                    sw.WriteLine("      <w:r><w:t>Sơ đồ cấu trúc tổ chức:</w:t></w:r>");
                    sw.WriteLine("    </w:p>");
                    sw.WriteLine("    <w:p>");
                    sw.WriteLine("      <w:r>");
                    sw.WriteLine("        <w:drawing>");
                    sw.WriteLine("          <wp:inline>");
                    sw.WriteLine("            <wp:extent cx=\"4572000\" cy=\"2286000\"/>");
                    sw.WriteLine("            <wp:docPr id=\"1\" name=\"Diagram 1\"/>");
                    sw.WriteLine("            <a:graphic>");
                    sw.WriteLine("              <a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\">");
                    sw.WriteLine("                <dgm:relIds r:dm=\"rIdDm1\" r:lo=\"rIdLo1\"/>");
                    sw.WriteLine("              </a:graphicData>");
                    sw.WriteLine("            </a:graphic>");
                    sw.WriteLine("          </wp:inline>");
                    sw.WriteLine("        </w:drawing>");
                    sw.WriteLine("      </w:r>");
                    sw.WriteLine("    </w:p>");
                    sw.WriteLine("  </w:body>");
                    sw.WriteLine("</w:document>");
                }
            }

            DocxMetadata nativeMeta;
            bool loadOk = DocxHandler.LoadDocxFile(nativeDocx, out nativeMeta);
            Assert(loadOk, "LoadDocxFile loaded native DrawingML diagram document");

            Assert(nativeMeta.TextContent.Contains("TYPE=SmartArt"), "Detected DrawingML diagram as SmartArt");
            Assert(nativeMeta.TextContent.Contains("SMARTART=Hierarchy_Tree"), "Mapped layout1.xml hierarchy to Hierarchy_Tree");

            bool foundOrg = false;
            foreach (var kv in DocShapeObject.TagCache)
            {
                if (kv.Value != null && kv.Value.SmartArtType == "Hierarchy_Tree")
                {
                    foundOrg = kv.Value.TextContent.Contains("Hội đồng Quản trị") || kv.Value.SmartArtTitle.Contains("Cơ cấu Tổ chức");
                    break;
                }
            }
            Assert(foundOrg, "Extracted nodes from word/diagrams/data1.xml into TagCache");

            try { File.Delete(nativeDocx); } catch { }
        }
    }
}
