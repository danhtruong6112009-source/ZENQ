using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;
using GenOffice.Core;
using GenOffice.Format;

namespace TestHybridPptx
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                PresentationRoot pres = new PresentationRoot();
                pres.CanvasWidth = 1280;
                pres.CanvasHeight = 720;
                pres.IsWidescreen = true;
                pres.Slides.Clear();

                // ==========================================
                // SLIDE 1: Title & Hero Cover (Dark Gradient + Fade)
                // ==========================================
                SlideNode s1 = new SlideNode();
                s1.IsGradientBg = true;
                s1.BgColor = 0x0F172A;
                s1.BgColor2 = 0x1E3A8A;
                s1.BgGradientAngle = 45f;
                s1.Transition = TransitionType.Fade;

                s1.AddShape(new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(100f, 160f, 1080f, 120f),
                    Text = "GENOFFICE SLIDES 2026",
                    FontName = "Segoe UI",
                    FontSize = 44f,
                    Bold = true,
                    TextColor = 0x38BDF8,
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                });

                s1.AddShape(new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(100f, 300f, 1080f, 100f),
                    Text = "NỀN TẢNG THUYẾT TRÌNH THẾ HỆ MỚI\nChuẩn OpenXML PPTX & Hiệu ứng Chuyển cảnh Morph Siêu Mượt",
                    FontName = "Segoe UI",
                    FontSize = 18f,
                    TextColor = 0xE2E8F0,
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                });

                s1.AddShape(new ShapeNode
                {
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(460f, 440f, 360f, 50f),
                    Text = "★ PHIÊN BẢN DOANH NGHIỆP 3.5 ★",
                    FontName = "Segoe UI",
                    FontSize = 13f,
                    Bold = true,
                    TextColor = 0xFFFFFF,
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x2563EB,
                    StrokeColor = 0x60A5FA,
                    StrokeWidth = 1.5f
                });
                pres.Slides.Add(s1);

                // ==========================================
                // SLIDE 2: 3 Feature Cards (Morph Transition)
                // ==========================================
                SlideNode s2 = new SlideNode();
                s2.BgColor = 0xF8FAFC;
                s2.Transition = TransitionType.Morph;
                s2.MorphMode = MorphType.Objects;

                s2.AddShape(new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(80f, 50f, 1120f, 70f),
                    Text = "TÍNH NĂNG ĐỘT PHÁ & TỐI ƯU HIỆU NĂNG",
                    FontName = "Segoe UI",
                    FontSize = 28f,
                    Bold = true,
                    TextColor = 0x0F172A,
                    Alignment = TextAlignment.Left,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                });

                string[] cardTitles = new string[] { "⚡ Lõi Vector Siêu Tốc", "✨ Chuyển Cảnh Morph", "🔄 Tương Thích 100% PPTX" };
                string[] cardDescs = new string[] {
                    "Xử lý hàng nghìn đối tượng đồ họa mượt mà ở tốc độ 60 FPS không giật lag.",
                    "Hiệu ứng biến hình Morph Object/Word mượt mà chuẩn Microsoft PowerPoint.",
                    "Mở, chỉnh sửa và lưu định dạng PresentationML (.pptx) chuẩn xác tuyệt đối."
                };
                int[] cardFills = new int[] { 0xEFF6FF, 0xF0FDF4, 0xFEF2F2 };
                int[] cardBorders = new int[] { 0x3B82F6, 0x22C55E, 0xEF4444 };
                int[] titleColors = new int[] { 0x1D4ED8, 0x15803D, 0xB91C1C };

                for (int i = 0; i < 3; i++)
                {
                    float cardX = 80f + i * 380f;
                    s2.AddShape(new ShapeNode
                    {
                        Type = ShapeType.RoundedRectangle,
                        Transform = new Transform2D(cardX, 150f, 350f, 480f),
                        FillColor = cardFills[i],
                        StrokeColor = cardBorders[i],
                        StrokeWidth = 2f
                    });
                    s2.AddShape(new ShapeNode
                    {
                        Type = ShapeType.TextBox,
                        Transform = new Transform2D(cardX + 20f, 180f, 310f, 60f),
                        Text = cardTitles[i],
                        FontName = "Segoe UI",
                        FontSize = 18f,
                        Bold = true,
                        TextColor = titleColors[i],
                        Alignment = TextAlignment.Center,
                        FillColor = 0x00FFFFFF,
                        StrokeWidth = 0f
                    });
                    s2.AddShape(new ShapeNode
                    {
                        Type = ShapeType.TextBox,
                        Transform = new Transform2D(cardX + 20f, 260f, 310f, 320f),
                        Text = cardDescs[i],
                        FontName = "Segoe UI",
                        FontSize = 14f,
                        TextColor = 0x334155,
                        Alignment = TextAlignment.Left,
                        FillColor = 0x00FFFFFF,
                        StrokeWidth = 0f
                    });
                }
                pres.Slides.Add(s2);

                // ==========================================
                // SLIDE 3: Comparison Table (Push Transition)
                // ==========================================
                SlideNode s3 = new SlideNode();
                s3.BgColor = 0x0F172A;
                s3.Transition = TransitionType.Push;

                s3.AddShape(new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(80f, 50f, 1120f, 70f),
                    Text = "BẢNG SO SÁNH THÔNG SỐ HIỆU NĂNG THỰC TẾ",
                    FontName = "Segoe UI",
                    FontSize = 26f,
                    Bold = true,
                    TextColor = 0x38BDF8,
                    Alignment = TextAlignment.Left,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                });

                s3.AddShape(new ShapeNode
                {
                    Type = ShapeType.Table,
                    Transform = new Transform2D(80f, 150f, 1120f, 480f),
                    TableHeaderRow = true,
                    TableBandedRows = true,
                    TableData = new List<List<string>>
                    {
                        new List<string> { "Chỉ số đánh giá", "GenOffice Slides 2026", "Phiên bản cũ", "Độ cải thiện" },
                        new List<string> { "Tốc độ nạp file .pptx", "0.45 giây", "1.80 giây", "Nhanh hơn 400%" },
                        new List<string> { "Dung lượng RAM tiêu thụ", "82 MB", "310 MB", "Tiết kiệm 73.5%" },
                        new List<string> { "Tỷ lệ khung hình chuyển cảnh", "60 FPS Cố định", "30 - 45 FPS", "Mượt mà tuyệt đối" },
                        new List<string> { "Độ chính xác định dạng OpenXML", "100% Pixel-Perfect", "85% Cơ bản", "Chuẩn quốc tế" }
                    },
                    FillColor = 0x1E293B,
                    StrokeColor = 0x334155,
                    StrokeWidth = 1f
                });
                pres.Slides.Add(s3);

                // ==========================================
                // SLIDE 4: Conclusion & Action (Wipe Transition)
                // ==========================================
                SlideNode s4 = new SlideNode();
                s4.IsGradientBg = true;
                s4.BgColor = 0x064E3B;
                s4.BgColor2 = 0x047857;
                s4.BgGradientAngle = 90f;
                s4.Transition = TransitionType.Wipe;

                s4.AddShape(new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(100f, 120f, 1080f, 90f),
                    Text = "KẾT LUẬN & ĐỊNH HƯỚNG TRIỂN KHAI",
                    FontName = "Segoe UI",
                    FontSize = 36f,
                    Bold = true,
                    TextColor = 0xFFFFFF,
                    Alignment = TextAlignment.Center,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                });

                s4.AddShape(new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(150f, 240f, 980f, 240f),
                    Text = "✔ Hoàn tất đồng bộ 100% chuẩn OpenXML cho toàn bộ bộ ứng dụng (Word, Excel, PowerPoint).\n✔ Bộ chuyển cảnh Morph & Animations chạy mượt mà trên mọi màn hình độ phân giải 4K.\n✔ Sẵn sàng triển khai mở rộng cho toàn thể doanh nghiệp trong Quý 4/2026.",
                    FontName = "Segoe UI",
                    FontSize = 18f,
                    TextColor = 0xD1FAE5,
                    Alignment = TextAlignment.Left,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f
                });

                s4.AddShape(new ShapeNode
                {
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(460f, 510f, 360f, 65f),
                    Text = "BẮT ĐẦU SỬ DỤNG NGAY",
                    FontName = "Segoe UI",
                    FontSize = 16f,
                    Bold = true,
                    TextColor = 0x064E3B,
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle,
                    FillColor = 0x34D399,
                    StrokeColor = 0xFFFFFF,
                    StrokeWidth = 2f
                });
                pres.Slides.Add(s4);

                // ==========================================
                // Save to PPTX
                // ==========================================
                string refPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\valid_powerpoint.pptx";
                string outPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoThuyetTrinh_2026.pptx";

                if (File.Exists(outPptx)) File.Delete(outPptx);
                File.Copy(refPptx, outPptx);

                using (FileStream fs = new FileStream(outPptx, FileMode.Open, FileAccess.ReadWrite))
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Update))
                {
                    // 1. Remove old slide1.xml, slide1.xml.rels
                    var oldS1 = archive.GetEntry("ppt/slides/slide1.xml");
                    if (oldS1 != null) oldS1.Delete();
                    var oldS1Rels = archive.GetEntry("ppt/slides/_rels/slide1.xml.rels");
                    if (oldS1Rels != null) oldS1Rels.Delete();

                    // 2. Update [Content_Types].xml
                    var ctEntry = archive.GetEntry("[Content_Types].xml");
                    if (ctEntry != null) ctEntry.Delete();
                    ctEntry = archive.CreateEntry("[Content_Types].xml");
                    using (StreamWriter sw = new StreamWriter(ctEntry.Open(), new UTF8Encoding(false)))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
                        sw.WriteLine("  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                        sw.WriteLine("  <Default Extension=\"xml\" ContentType=\"application/xml\"/>");
                        sw.WriteLine("  <Default Extension=\"png\" ContentType=\"image/png\"/>");
                        sw.WriteLine("  <Default Extension=\"jpeg\" ContentType=\"image/jpeg\"/>");
                        sw.WriteLine("  <Override PartName=\"/ppt/presentation.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/ppt/slideMasters/slideMaster1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml\"/>");
                        for (int lay = 1; lay <= 11; lay++)
                            sw.WriteLine(string.Format("  <Override PartName=\"/ppt/slideLayouts/slideLayout{0}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml\"/>", lay));
                        sw.WriteLine("  <Override PartName=\"/ppt/theme/theme1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.theme+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/ppt/tableStyles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.tableStyles+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/ppt/presProps.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presProps+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/ppt/viewProps.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.viewProps+xml\"/>");
                        for (int i = 0; i < pres.Slides.Count; i++)
                        {
                            sw.WriteLine(string.Format("  <Override PartName=\"/ppt/slides/slide{0}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>", i + 1));
                        }
                        sw.WriteLine("</Types>");
                    }

                    // 3. Update ppt/_rels/presentation.xml.rels
                    var presRels = archive.GetEntry("ppt/_rels/presentation.xml.rels");
                    if (presRels != null) presRels.Delete();
                    presRels = archive.CreateEntry("ppt/_rels/presentation.xml.rels");
                    using (StreamWriter sw = new StreamWriter(presRels.Open(), new UTF8Encoding(false)))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                        sw.WriteLine("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster\" Target=\"slideMasters/slideMaster1.xml\"/>");
                        sw.WriteLine("  <Relationship Id=\"rIdPresProps\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/presProps\" Target=\"presProps.xml\"/>");
                        sw.WriteLine("  <Relationship Id=\"rIdViewProps\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/viewProps\" Target=\"viewProps.xml\"/>");
                        sw.WriteLine("  <Relationship Id=\"rIdTheme\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme\" Target=\"theme/theme1.xml\"/>");
                        sw.WriteLine("  <Relationship Id=\"rIdTableStyles\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/tableStyles\" Target=\"tableStyles.xml\"/>");
                        for (int i = 0; i < pres.Slides.Count; i++)
                        {
                            sw.WriteLine(string.Format("  <Relationship Id=\"rIdSlide{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide{1}.xml\"/>", i + 1, i + 1));
                        }
                        sw.WriteLine("</Relationships>");
                    }

                    // 4. Update ppt/presentation.xml
                    var presEntry = archive.GetEntry("ppt/presentation.xml");
                    if (presEntry != null) presEntry.Delete();
                    presEntry = archive.CreateEntry("ppt/presentation.xml");
                    using (StreamWriter sw = new StreamWriter(presEntry.Open(), new UTF8Encoding(false)))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<p:presentation xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
                        sw.WriteLine("  <p:sldMasterIdLst>");
                        sw.WriteLine("    <p:sldMasterId id=\"2147483648\" r:id=\"rId1\"/>");
                        sw.WriteLine("  </p:sldMasterIdLst>");
                        sw.WriteLine("  <p:sldIdLst>");
                        for (int i = 0; i < pres.Slides.Count; i++)
                        {
                            sw.WriteLine(string.Format("    <p:sldId id=\"{0}\" r:id=\"rIdSlide{1}\"/>", 256 + i, i + 1));
                        }
                        sw.WriteLine("  </p:sldIdLst>");
                        sw.WriteLine("  <p:sldSz cx=\"12192000\" cy=\"6858000\" type=\"screen16x9\"/>");
                        sw.WriteLine("  <p:notesSz cx=\"6858000\" cy=\"9144000\"/>");
                        sw.WriteLine("</p:presentation>");
                    }

                    // 5. Write each Slide and Slide Rels
                    float exportScaleX = 1280f / pres.CanvasWidth;
                    float exportScaleY = 720f / pres.CanvasHeight;

                    for (int i = 0; i < pres.Slides.Count; i++)
                    {
                        SlideNode slide = pres.Slides[i];
                        int slideIndex = i + 1;

                        ZipArchiveEntry sRelEntry = archive.CreateEntry(string.Format("ppt/slides/_rels/slide{0}.xml.rels", slideIndex));
                        using (StreamWriter sw = new StreamWriter(sRelEntry.Open(), new UTF8Encoding(false)))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                            sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                            sw.WriteLine("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout\" Target=\"../slideLayouts/slideLayout1.xml\"/>");
                            sw.WriteLine("</Relationships>");
                        }

                        ZipArchiveEntry sEntry = archive.CreateEntry(string.Format("ppt/slides/slide{0}.xml", slideIndex));
                        using (StreamWriter sw = new StreamWriter(sEntry.Open(), new UTF8Encoding(false)))
                        {
                            WriteSlideXml(sw, slide, slideIndex, exportScaleX, exportScaleY, pres);
                        }
                    }
                }

                Console.WriteLine("HYBRID_PPTX_GENERATED_SUCCESSFULLY -> " + outPptx);

                // Render GenOffice Slides Screenshots
                for (int i = 0; i < pres.Slides.Count; i++)
                {
                    using (Bitmap slideBmp = RenderSlide(pres, i, 1280, 720))
                    {
                        string sImg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", string.Format("genoffice_slide{0}.png", i + 1));
                        if (File.Exists(sImg)) File.Delete(sImg);
                        slideBmp.Save(sImg, ImageFormat.Png);
                        Console.WriteLine("GenOffice Slide " + (i + 1) + " Screenshot: " + sImg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }

        static void WriteSlideXml(StreamWriter sw, SlideNode slide, int slideIndex, float exportScaleX, float exportScaleY, PresentationRoot pres)
        {
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sw.WriteLine("<p:sld xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\">");
            sw.WriteLine("  <p:cSld>");

            // 1. Background
            if (slide.IsGradientBg)
            {
                int c1 = slide.BgColor != 0 ? slide.BgColor : 0xFFFFFF;
                int c2 = slide.BgColor2 != 0 ? slide.BgColor2 : 0xE2E8F0;
                int ang = (int)(slide.BgGradientAngle * 60000);
                sw.WriteLine(string.Format("    <p:bg><p:bgPr><a:gradFill><a:gsLst><a:gs pos=\"0\"><a:srgbClr val=\"{0:X6}\"/></a:gs><a:gs pos=\"100000\"><a:srgbClr val=\"{1:X6}\"/></a:gs></a:gsLst><a:lin ang=\"{2}\"/></a:gradFill></p:bgPr></p:bg>", c1 & 0xFFFFFF, c2 & 0xFFFFFF, ang));
            }
            else if (slide.BgColor != 0 && (slide.BgColor & 0xFFFFFF) != 0xFFFFFF)
            {
                sw.WriteLine(string.Format("    <p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"{0:X6}\"/></a:solidFill></p:bgPr></p:bg>", slide.BgColor & 0xFFFFFF));
            }

            // 2. Shape Tree
            sw.WriteLine("    <p:spTree>");
            sw.WriteLine("      <p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>");
            sw.WriteLine("      <p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>");

            for (int s = 0; s < slide.Shapes.Count; s++)
            {
                ShapeNode shape = slide.Shapes[s];
                int spId = s + 2;
                string shapeName = "Shape " + spId;

                long offX = (long)(shape.Transform.X * exportScaleX * 9525.0f);
                long offY = (long)(shape.Transform.Y * exportScaleY * 9525.0f);
                long extW = Math.Max(9525L, (long)(shape.Transform.Width * exportScaleX * 9525.0f));
                long extH = Math.Max(9525L, (long)(shape.Transform.Height * exportScaleY * 9525.0f));

                if (shape.Type == ShapeType.Table && shape.TableData != null && shape.TableData.Count > 0)
                {
                    int numRows = shape.TableData.Count;
                    int numCols = shape.TableData[0].Count;
                    long colW = numCols > 0 ? (extW / numCols) : extW;
                    long rowH = numRows > 0 ? (extH / numRows) : extH;

                    sw.WriteLine("      <p:graphicFrame>");
                    sw.WriteLine(string.Format("        <p:nvGraphicFramePr><p:cNvPr id=\"{0}\" name=\"{1}\"/><p:cNvGraphicFramePr><a:graphicFrameLocks noGrp=\"1\"/></p:cNvGraphicFramePr><p:nvPr/></p:nvGraphicFramePr>", spId, shapeName));
                    sw.WriteLine(string.Format("        <p:xfrm><a:off x=\"{0}\" y=\"{1}\"/><a:ext cx=\"{2}\" cy=\"{3}\"/></p:xfrm>", offX, offY, extW, extH));
                    sw.WriteLine("        <a:graphic>");
                    sw.WriteLine("          <a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">");
                    sw.WriteLine(string.Format("            <a:tbl><a:tblPr firstRow=\"{0}\" bandRow=\"{1}\"/>", shape.TableHeaderRow ? "1" : "0", shape.TableBandedRows ? "1" : "0"));
                    sw.WriteLine("              <a:tblGrid>");
                    for (int c = 0; c < numCols; c++) sw.WriteLine(string.Format("                <a:gridCol w=\"{0}\"/>", colW));
                    sw.WriteLine("              </a:tblGrid>");
                    for (int r = 0; r < numRows; r++)
                    {
                        sw.WriteLine(string.Format("              <a:tr h=\"{0}\">", rowH));
                        for (int c = 0; c < numCols; c++)
                        {
                            string cellTxt = (c < shape.TableData[r].Count) ? shape.TableData[r][c] : "";
                            bool isH = (r == 0);
                            string cellFill = isH ? "2563EB" : (r % 2 == 0 ? "1E293B" : "0F172A");
                            string txtColor = isH ? "FFFFFF" : (c == 3 ? "38BDF8" : "E2E8F0");

                            sw.WriteLine("                <a:tc>");
                            sw.WriteLine("                  <a:txBody><a:bodyPr anchor=\"ctr\"/><a:lstStyle/>");
                            sw.WriteLine("                    <a:p>");
                            sw.WriteLine(string.Format("                      <a:pPr algn=\"{0}\"/>", isH ? "ctr" : (c == 0 ? "l" : "ctr")));
                            sw.WriteLine("                      <a:r>");
                            sw.WriteLine(string.Format("                        <a:rPr sz=\"{0}\" b=\"{1}\"><a:solidFill><a:srgbClr val=\"{2}\"/></a:solidFill><a:latin typeface=\"Segoe UI\"/></a:rPr>",
                                (int)((isH ? 13f : 12f) * 100), isH ? "1" : "0", txtColor));
                            sw.WriteLine(string.Format("                        <a:t>{0}</a:t>", EscapeXml(cellTxt)));
                            sw.WriteLine("                      </a:r>");
                            sw.WriteLine("                    </a:p>");
                            sw.WriteLine("                  </a:txBody>");
                            sw.WriteLine(string.Format("                  <a:tcPr><a:solidFill><a:srgbClr val=\"{0}\"/></a:solidFill></a:tcPr>", cellFill));
                            sw.WriteLine("                </a:tc>");
                        }
                        sw.WriteLine("              </a:tr>");
                    }
                    sw.WriteLine("            </a:tbl>");
                    sw.WriteLine("          </a:graphicData>");
                    sw.WriteLine("        </a:graphic>");
                    sw.WriteLine("      </p:graphicFrame>");
                }
                else
                {
                    string geom = shape.Type == ShapeType.RoundedRectangle ? "roundRect" : "rect";
                    sw.WriteLine("      <p:sp>");
                    sw.WriteLine("        <p:nvSpPr>");
                    sw.WriteLine(string.Format("          <p:cNvPr id=\"{0}\" name=\"{1}\"/>", spId, shapeName));
                    sw.WriteLine("          <p:cNvSpPr/>");
                    sw.WriteLine("          <p:nvPr/>");
                    sw.WriteLine("        </p:nvSpPr>");
                    sw.WriteLine("        <p:spPr>");
                    sw.WriteLine("          <a:xfrm>");
                    sw.WriteLine(string.Format("            <a:off x=\"{0}\" y=\"{1}\"/>", offX, offY));
                    sw.WriteLine(string.Format("            <a:ext cx=\"{0}\" cy=\"{1}\"/>", extW, extH));
                    sw.WriteLine("          </a:xfrm>");
                    sw.WriteLine(string.Format("          <a:prstGeom prst=\"{0}\"><a:avLst/></a:prstGeom>", geom));

                    if (shape.FillColor != 0x00FFFFFF)
                    {
                        sw.WriteLine(string.Format("          <a:solidFill><a:srgbClr val=\"{0:X6}\"/></a:solidFill>", shape.FillColor & 0xFFFFFF));
                    }
                    else
                    {
                        sw.WriteLine("          <a:noFill/>");
                    }

                    if (shape.StrokeWidth > 0.01f)
                    {
                        sw.WriteLine(string.Format("          <a:ln w=\"{0}\"><a:solidFill><a:srgbClr val=\"{1:X6}\"/></a:solidFill></a:ln>", (int)(shape.StrokeWidth * exportScaleX * 12700.0f), shape.StrokeColor & 0xFFFFFF));
                    }
                    sw.WriteLine("        </p:spPr>");

                    if (!string.IsNullOrEmpty(shape.Text))
                    {
                        string ancStr = shape.VerticalAlignment == VerticalTextAlignment.Middle ? "ctr" : (shape.VerticalAlignment == VerticalTextAlignment.Bottom ? "b" : "t");
                        string algnStr = shape.Alignment == TextAlignment.Center ? "ctr" : (shape.Alignment == TextAlignment.Right ? "r" : "l");
                        int szPt100 = (int)(shape.FontSize * 100.0f);

                        sw.WriteLine("        <p:txBody>");
                        sw.WriteLine(string.Format("          <a:bodyPr anchor=\"{0}\" wrap=\"square\"/>", ancStr));
                        sw.WriteLine("          <a:lstStyle/>");
                        string[] lines = shape.Text.Split('\n');
                        foreach (string line in lines)
                        {
                            sw.WriteLine("          <a:p>");
                            sw.WriteLine(string.Format("            <a:pPr algn=\"{0}\"/>", algnStr));
                            sw.WriteLine("            <a:r>");
                            sw.WriteLine(string.Format("              <a:rPr sz=\"{0}\" b=\"{1}\"><a:solidFill><a:srgbClr val=\"{2:X6}\"/></a:solidFill><a:latin typeface=\"{3}\"/></a:rPr>",
                                szPt100,
                                shape.Bold ? "1" : "0",
                                shape.TextColor & 0xFFFFFF,
                                EscapeXml(shape.FontName ?? "Segoe UI")));
                            sw.WriteLine(string.Format("              <a:t>{0}</a:t>", EscapeXml(line)));
                            sw.WriteLine("            </a:r>");
                            sw.WriteLine("          </a:p>");
                        }
                        sw.WriteLine("        </p:txBody>");
                    }
                    sw.WriteLine("      </p:sp>");
                }
            }

            sw.WriteLine("    </p:spTree>");
            sw.WriteLine("  </p:cSld>");
            sw.WriteLine("  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>");

            // Transition
            if (slide.Transition == TransitionType.Fade)
            {
                sw.WriteLine("  <p:transition spd=\"med\" advClick=\"1\"><p:fade/></p:transition>");
            }
            else if (slide.Transition == TransitionType.Push)
            {
                sw.WriteLine("  <p:transition spd=\"med\" advClick=\"1\"><p:push dir=\"r\"/></p:transition>");
            }
            else if (slide.Transition == TransitionType.Wipe)
            {
                sw.WriteLine("  <p:transition spd=\"med\" advClick=\"1\"><p:wipe dir=\"r\"/></p:transition>");
            }
            else if (slide.Transition == TransitionType.Morph)
            {
                sw.WriteLine("  <mc:AlternateContent xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\">");
                sw.WriteLine("    <mc:Choice xmlns:p159=\"http://schemas.microsoft.com/office/powerpoint/2015/09/main\" Requires=\"p159\">");
                sw.WriteLine("      <p:transition spd=\"med\" xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\" p14:dur=\"1500\" advClick=\"1\">");
                sw.WriteLine("        <p159:morph option=\"byObject\"/>");
                sw.WriteLine("      </p:transition>");
                sw.WriteLine("    </mc:Choice>");
                sw.WriteLine("    <mc:Fallback>");
                sw.WriteLine("      <p:transition spd=\"med\" advClick=\"1\"><p:fade/></p:transition>");
                sw.WriteLine("    </mc:Fallback>");
                sw.WriteLine("  </mc:AlternateContent>");
            }

            sw.WriteLine("</p:sld>");
        }

        static Bitmap RenderSlide(PresentationRoot pres, int slideIndex, int targetW, int targetH)
        {
            Bitmap bmp = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                Rectangle r = new Rectangle(0, 0, targetW, targetH);
                SlideNode slide = pres.Slides[slideIndex];

                // Background
                if (slide.IsGradientBg)
                {
                    Color c1 = Color.FromArgb(slide.BgColor | unchecked((int)0xFF000000));
                    Color c2 = Color.FromArgb(slide.BgColor2 | unchecked((int)0xFF000000));
                    using (LinearGradientBrush lgb = new LinearGradientBrush(r, c1, c2, slide.BgGradientAngle))
                    {
                        g.FillRectangle(lgb, r);
                    }
                }
                else
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(slide.BgColor | unchecked((int)0xFF000000))))
                    {
                        g.FillRectangle(sb, r);
                    }
                }

                // Shapes
                float scale = (float)targetW / pres.CanvasWidth;
                for (int s = 0; s < slide.Shapes.Count; s++)
                {
                    ShapeNode shape = slide.Shapes[s];
                    if (!shape.IsVisible) continue;

                    float baseW = shape.Transform.Width * scale;
                    float baseH = shape.Transform.Height * scale;
                    float sx = shape.Transform.X * scale;
                    float sy = shape.Transform.Y * scale;
                    RectangleF sRect = new RectangleF(sx, sy, baseW, baseH);

                    // Draw Shape Background / Geometry
                    if (shape.Type == ShapeType.Table && shape.TableData != null && shape.TableData.Count > 0)
                    {
                        int nR = shape.TableData.Count;
                        int nC = shape.TableData[0].Count;
                        float colW = baseW / nC;
                        float rowH = baseH / nR;

                        for (int tr = 0; tr < nR; tr++)
                        {
                            bool isH = (tr == 0);
                            for (int tc = 0; tc < nC; tc++)
                            {
                                RectangleF cRect = new RectangleF(sx + tc * colW, sy + tr * rowH, colW, rowH);
                                Color cellBg = isH ? Color.FromArgb(37, 99, 235) : (tr % 2 == 0 ? Color.FromArgb(30, 41, 59) : Color.FromArgb(15, 23, 42));
                                using (SolidBrush cb = new SolidBrush(cellBg))
                                using (Pen cp = new Pen(Color.FromArgb(51, 65, 85), 1f))
                                {
                                    g.FillRectangle(cb, cRect);
                                    g.DrawRectangle(cp, cRect.X, cRect.Y, cRect.Width, cRect.Height);
                                }

                                string txt = (tc < shape.TableData[tr].Count) ? shape.TableData[tr][tc] : "";
                                if (!string.IsNullOrEmpty(txt))
                                {
                                    using (Font tf = new Font("Segoe UI", isH ? 13f : 12f, isH ? FontStyle.Bold : FontStyle.Regular))
                                    using (SolidBrush tb = new SolidBrush(isH ? Color.White : (tc == 3 ? Color.FromArgb(56, 189, 248) : Color.FromArgb(226, 232, 240))))
                                    {
                                        StringFormat sf = new StringFormat
                                        {
                                            Alignment = isH ? StringAlignment.Center : (tc == 0 ? StringAlignment.Near : StringAlignment.Center),
                                            LineAlignment = StringAlignment.Center
                                        };
                                        RectangleF padRect = new RectangleF(cRect.X + 8, cRect.Y, cRect.Width - 16, cRect.Height);
                                        g.DrawString(txt, tf, tb, padRect, sf);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (shape.FillColor != 0x00FFFFFF)
                        {
                            using (SolidBrush fb = new SolidBrush(Color.FromArgb(shape.FillColor | unchecked((int)0xFF000000))))
                            {
                                if (shape.Type == ShapeType.RoundedRectangle)
                                {
                                    using (GraphicsPath path = GetRoundedRectPath(sRect, 12f * scale))
                                    {
                                        g.FillPath(fb, path);
                                        if (shape.StrokeWidth > 0)
                                        {
                                            using (Pen sp = new Pen(Color.FromArgb(shape.StrokeColor | unchecked((int)0xFF000000)), shape.StrokeWidth * scale))
                                                g.DrawPath(sp, path);
                                        }
                                    }
                                }
                                else
                                {
                                    g.FillRectangle(fb, sRect);
                                    if (shape.StrokeWidth > 0)
                                    {
                                        using (Pen sp = new Pen(Color.FromArgb(shape.StrokeColor | unchecked((int)0xFF000000)), shape.StrokeWidth * scale))
                                            g.DrawRectangle(sp, sRect.X, sRect.Y, sRect.Width, sRect.Height);
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(shape.Text))
                        {
                            using (Font tf = new Font(shape.FontName ?? "Segoe UI", Math.Max(10f, shape.FontSize * scale), shape.Bold ? FontStyle.Bold : FontStyle.Regular))
                            using (SolidBrush tb = new SolidBrush(Color.FromArgb(shape.TextColor | unchecked((int)0xFF000000))))
                            {
                                StringFormat sf = new StringFormat
                                {
                                    Alignment = shape.Alignment == TextAlignment.Center ? StringAlignment.Center : (shape.Alignment == TextAlignment.Right ? StringAlignment.Far : StringAlignment.Near),
                                    LineAlignment = shape.VerticalAlignment == VerticalTextAlignment.Middle ? StringAlignment.Center : (shape.VerticalAlignment == VerticalTextAlignment.Bottom ? StringAlignment.Far : StringAlignment.Near)
                                };
                                RectangleF textPad = new RectangleF(sRect.X + 8, sRect.Y + 4, sRect.Width - 16, sRect.Height - 8);
                                g.DrawString(shape.Text, tf, tb, textPad, sf);
                            }
                        }
                    }
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedRectPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static string EscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }
    }
}
