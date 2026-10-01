using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace FullWriterTestSuite
{
    class Program
    {
        static string DesktopDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GenOffice_Test_All_Modules_Report");
        static string WriterDir = Path.Combine(DesktopDir, "03_WRITER_TEST");
        static string WriterFilesDir = Path.Combine(WriterDir, "Files");
        static string WriterImagesDir = Path.Combine(WriterDir, "Images");
        static string WriterVideosDir = Path.Combine(WriterDir, "Videos");
        static string WriterFramesDir = Path.Combine(WriterDir, "AnimFrames");

        static StringBuilder sb = new StringBuilder();
        static List<FormattedRun> runs = new List<FormattedRun>();
        static Dictionary<int, int> paraAligns = new Dictionary<int, int>();
        static Dictionary<int, ParagraphBorderInfo> paraBorders = new Dictionary<int, ParagraphBorderInfo>();
        static Dictionary<int, float> lineSpacings = new Dictionary<int, float>();
        static Dictionary<int, float> spacingsBefore = new Dictionary<int, float>();
        static Dictionary<int, float> spacingsAfter = new Dictionary<int, float>();

        static void AddParagraph(string text, string font, float sz, bool b, bool it, bool u, Color col, int align, float lSpace, float sB, float sA, ParagraphBorderInfo bdr)
        {
            int offset = sb.Length;
            paraAligns[offset] = align;
            lineSpacings[offset] = lSpace;
            spacingsBefore[offset] = sB;
            spacingsAfter[offset] = sA;
            if (bdr != null) paraBorders[offset] = bdr;

            runs.Add(new FormattedRun(offset, text.Length, font, sz, b, it, u, false, col, Color.Transparent));
            sb.Append(text);
            sb.Append('\n');
        }

        [STAThread]
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("==================================================");
            Console.WriteLine(">>> BẮT ĐẦU KIỂM THỬ PHÂN HỆ WRITER (DOCUMENT) <<<");
            Console.WriteLine("==================================================");

            Directory.CreateDirectory(WriterFilesDir);
            Directory.CreateDirectory(WriterImagesDir);
            Directory.CreateDirectory(WriterVideosDir);
            Directory.CreateDirectory(WriterFramesDir);

            // 1. TẠO FILE DOCX TỪ GENOFFICE
            Console.WriteLine("[1/5] Tạo file DOCX từ GenOffice...");
            string genDocxPath = Path.Combine(WriterFilesDir, "GenOffice_Created_Document.docx");
            CreateGenOfficeDocument(genDocxPath);

            // 2. TẠO FILE DOCX TỪ MICROSOFT WORD (COM AUTOMATION)
            Console.WriteLine("[2/5] Tạo file DOCX từ Microsoft Word COM...");
            string wordDocxPath = Path.Combine(WriterFilesDir, "MSWord_Created_Document.docx");
            CreateWordViaCOM(wordDocxPath);

            // 3. KIỂM THỬ MỞ 2 CHIỀU (BIDIRECTIONAL COMPATIBILITY)
            Console.WriteLine("[3/5] Kiểm thử mở 2 chiều...");
            TestBidirectionalDocx(genDocxPath, wordDocxPath);

            // 4. CHỤP ẢNH TẤT CẢ CÁC TAB RIBBON VÀ GIAO DIỆN WRITER
            Console.WriteLine("[4/5] Chụp ảnh toàn bộ giao diện & Ribbon Tabs của Writer...");
            CaptureWriterRibbonAndViews(genDocxPath);

            // 5. RENDER CHUỖI KHUNG HÌNH SOẠN THẢO & CUỘN TRANG MƯỢT
            Console.WriteLine("[5/5] Render chuỗi khung hình cuộn trang & định dạng văn bản...");
            RenderWriterAnimationFrames(genDocxPath);

            Console.WriteLine("\n[HOÀN THÀNH] Đã hoàn tất kiểm thử Writer!");
        }

        static void CreateGenOfficeDocument(string outDocx)
        {
            sb.Clear();
            runs.Clear();
            paraAligns.Clear();
            paraBorders.Clear();
            lineSpacings.Clear();
            spacingsBefore.Clear();
            spacingsAfter.Clear();

            // 1. Header Title
            AddParagraph("BÁO CÁO TỔNG KẾT DỰ ÁN GENOFFICE 2026", "Segoe UI", 16f, true, false, false, Color.FromArgb(37, 99, 235), 1, 1.2f, 10f, 4f, null);

            // 2. Subtitle
            AddParagraph("Đơn vị: Phòng Nghiên cứu & Phát triển Phần mềm Doanh nghiệp", "Segoe UI", 10.5f, false, true, false, Color.FromArgb(100, 116, 139), 1, 1.15f, 0f, 2f, null);
            AddParagraph("Thời gian: Ngày 28 tháng 08 năm 2026  |  Phiên bản: 3.5.0 Enterprise", "Segoe UI", 9.5f, false, false, false, Color.FromArgb(148, 163, 184), 1, 1.15f, 0f, 12f, null);

            // Divider line
            ParagraphBorderInfo divBorder = new ParagraphBorderInfo { IsHorizontalLine = true, Color = Color.FromArgb(203, 213, 225), SizePt = 1.5f };
            AddParagraph("", "Segoe UI", 6f, false, false, false, Color.Transparent, 1, 1f, 0f, 8f, divBorder);

            // 3. Heading 1
            ParagraphBorderInfo h1Border = new ParagraphBorderInfo { Bottom = true, Color = Color.FromArgb(16, 124, 65), SizePt = 1.5f };
            AddParagraph("1. TỔNG QUAN DỰ ÁN & MỤC TIÊU CHIẾN LƯỢC", "Segoe UI", 12.5f, true, false, false, Color.FromArgb(16, 124, 65), 0, 1.2f, 8f, 6f, h1Border);

            // 4. Intro Paragraph
            string introText = "Dự án GenOffice 2026 được triển khai với sứ mệnh xây dựng bộ công cụ văn phòng toàn diện, hiện đại và tốc độ cao dành cho doanh nghiệp. Hệ thống được tối ưu hóa toàn diện trên nền tảng .NET và OpenXML, mang lại khả năng tương thích hoàn hảo 100% với hệ sinh thái Microsoft Office (Word, Excel, PowerPoint).";
            AddParagraph(introText, "Segoe UI", 11f, false, false, false, Color.FromArgb(30, 41, 59), 3, 1.25f, 0f, 8f, null);

            // 5. Heading 2: Table Section
            AddParagraph("2. BẢNG TIẾN ĐỘ THỰC HIỆN & PHÂN BỔ NGÂN SÁCH", "Segoe UI", 12.5f, true, false, false, Color.FromArgb(16, 124, 65), 0, 1.2f, 10f, 6f, h1Border);

            // 6. Data Table
            string tblHeader = "| Hạng mục công việc | Phụ trách | Thời gian | Ngân sách (VNĐ) | Tiến độ hoàn thành |";
            string tblSep    = "| ------------------ | --------- | --------- | ---------------- | ------------------ |";
            string tblR1     = "| Nâng cấp Lõi Soạn thảo Writer | Nguyễn Văn An | Q1/2026 | 150,000,000 | 100% Hoàn thành |";
            string tblR2     = "| Xử lý Bảng tính Calc & Xlsx | Trần Thị Bình | Q2/2026 | 180,000,000 | 100% Hoàn thành |";
            string tblR3     = "| Giao diện Hiện đại Ribbon Bar | Lê Hoàng Cường | Q2/2026 | 95,000,000 | 100% Hoàn thành |";
            string tblR4     = "| Kiểm thử & Tương thích MS Office | Phạm Minh Đức | Q3/2026 | 75,000,000 | 100% Đạt chuẩn |";
            string tblR5     = "| TỔNG CỘNG TOÀN DỰ ÁN | Ban Giám Đốc | 2026 | 500,000,000 | 100% Xuất sắc |";

            string fullTbl = string.Join("\n", new string[] { tblHeader, tblSep, tblR1, tblR2, tblR3, tblR4, tblR5 });
            AddParagraph(fullTbl, "Segoe UI", 10.5f, false, false, false, Color.FromArgb(15, 23, 42), 0, 1f, 4f, 8f, null);

            // 7. Heading 3: Key Features
            AddParagraph("3. CÁC TÍNH NĂNG ĐỘT PHÁ NỔI BẬT", "Segoe UI", 12.5f, true, false, false, Color.FromArgb(16, 124, 65), 0, 1.2f, 10f, 6f, h1Border);

            AddParagraph("• Tính năng Gộp ô & Chia đôi ô đường chéo (Diagonal Split): Cho phép tạo 2 tiêu đề trên cùng 1 ô chuẩn xác.", "Segoe UI", 11f, false, false, false, Color.FromArgb(30, 41, 59), 0, 1.2f, 2f, 3f, null);
            AddParagraph("• Động cơ hiển thị mượt mà 60 FPS: Không còn hiện tượng giật khung hay mất nét văn bản khi phóng to/thu nhỏ.", "Segoe UI", 11f, false, false, false, Color.FromArgb(30, 41, 59), 0, 1.2f, 2f, 3f, null);
            AddParagraph("• Tương thích OpenXML 2 chiều: Mở và lưu file .docx, .xlsx, .pptx chuẩn xác mà không bị sai lệch định dạng.", "Segoe UI", 11f, false, false, false, Color.FromArgb(30, 41, 59), 0, 1.2f, 2f, 8f, null);

            // 8. Note Callout Box
            ParagraphBorderInfo noteBorder = new ParagraphBorderInfo { Left = true, Color = Color.FromArgb(245, 158, 11), SizePt = 3.5f };
            AddParagraph("  * Ghi chú quan trọng: Tất cả dữ liệu và định dạng đã được thẩm định bảo mật đạt chuẩn ISO/IEC 27001:2022.", "Segoe UI", 10f, false, true, false, Color.FromArgb(180, 83, 9), 0, 1.2f, 4f, 4f, noteBorder);

            if (File.Exists(outDocx)) File.Delete(outDocx);

            bool saveOk = DocxHandler.SaveDocxFile(
                outDocx,
                sb.ToString(),
                runs,
                false, // Portrait
                0,     // Normal margin
                "GENOFFICE 2026 ENTERPRISE - BÁO CÁO NỘI BỘ",
                "Trang 1 / 1  -  Bảo mật cấp độ cao",
                true,
                null,
                null,
                paraAligns,
                lineSpacings,
                spacingsBefore,
                spacingsAfter,
                paraBorders
            );

            Console.WriteLine("  -> Lưu DOCX: " + (saveOk ? "THÀNH CÔNG" : "THẤT BẠI") + " (" + (File.Exists(outDocx) ? new FileInfo(outDocx).Length : 0) + " bytes)");
        }

        static void CreateWordViaCOM(string outPath)
        {
            try
            {
                Type wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType == null)
                {
                    Console.WriteLine("  [CẢNH BÁO] Không tìm thấy Word COM.");
                    return;
                }

                object wordApp = Activator.CreateInstance(wordType);
                wordApp.GetType().InvokeMember("Visible", BindingFlags.SetProperty, null, wordApp, new object[] { false });

                object docs = wordType.InvokeMember("Documents", BindingFlags.GetProperty, null, wordApp, null);
                object doc = docs.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, docs, null);

                object selection = wordApp.GetType().InvokeMember("Selection", BindingFlags.GetProperty, null, wordApp, null);
                
                // Set Title
                object font = selection.GetType().InvokeMember("Font", BindingFlags.GetProperty, null, selection, null);
                font.GetType().InvokeMember("Name", BindingFlags.SetProperty, null, font, new object[] { "Segoe UI" });
                font.GetType().InvokeMember("Size", BindingFlags.SetProperty, null, font, new object[] { 18f });
                font.GetType().InvokeMember("Bold", BindingFlags.SetProperty, null, font, new object[] { 1 });

                selection.GetType().InvokeMember("TypeText", BindingFlags.InvokeMethod, null, selection, new object[] { "MICROSOFT WORD OFFICIAL TEST DOCUMENT 2026\n" });

                // Set Body
                font.GetType().InvokeMember("Size", BindingFlags.SetProperty, null, font, new object[] { 11f });
                font.GetType().InvokeMember("Bold", BindingFlags.SetProperty, null, font, new object[] { 0 });
                selection.GetType().InvokeMember("TypeText", BindingFlags.InvokeMethod, null, selection, new object[] { "Tài liệu kiểm thử được tạo từ Microsoft Word COM Automation để xác thực khả năng tương thích 2 chiều hoàn hảo với GenOffice Writer." });

                if (File.Exists(outPath)) File.Delete(outPath);
                // wdFormatXMLDocument = 12 (.docx)
                doc.GetType().InvokeMember("SaveAs", BindingFlags.InvokeMethod, null, doc, new object[] { outPath, 12 });
                doc.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, doc, new object[] { false });
                wordApp.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, wordApp, new object[] { false });

                Marshal.ReleaseComObject(doc);
                Marshal.ReleaseComObject(wordApp);

                Console.WriteLine("  -> Đã tạo thành công file DOCX từ MS Word: " + outPath + " (" + new FileInfo(outPath).Length + " bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI COM WORD] " + ex.Message);
            }
        }

        static void TestBidirectionalDocx(string genDocxPath, string wordDocxPath)
        {
            // 1. Mở file của GenOffice trong Word COM & kiểm tra số đoạn văn
            try
            {
                Type wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType != null && File.Exists(genDocxPath))
                {
                    object wordApp = Activator.CreateInstance(wordType);
                    wordApp.GetType().InvokeMember("Visible", BindingFlags.SetProperty, null, wordApp, new object[] { false });

                    object docs = wordType.InvokeMember("Documents", BindingFlags.GetProperty, null, wordApp, null);
                    object doc = docs.GetType().InvokeMember("Open", BindingFlags.InvokeMethod, null, docs, new object[] { genDocxPath });

                    object paragraphs = doc.GetType().InvokeMember("Paragraphs", BindingFlags.GetProperty, null, doc, null);
                    int pCount = (int)paragraphs.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, paragraphs, null);
                    Console.WriteLine("  [THÀNH CÔNG] Word đã mở file DOCX của GenOffice! Số lượng đoạn văn: " + pCount);

                    doc.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, doc, new object[] { false });
                    wordApp.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, wordApp, new object[] { false });
                    Marshal.ReleaseComObject(wordApp);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI TEST WORD COM] " + ex.Message);
            }

            // 2. Mở file của Word trong GenOffice
            try
            {
                if (File.Exists(wordDocxPath))
                {
                    WriterView wv = new WriterView();
                    wv.LoadDocumentFile(wordDocxPath, null);
                    Console.WriteLine("  [THÀNH CÔNG] GenOffice đã nạp thành công file DOCX của Word! Ký tự: " + wv.PieceTable.GetText().Length);
                    
                    string roundtripBack = Path.Combine(WriterFilesDir, "Roundtrip_MSWord_re-saved_by_GenOffice.docx");
                    DocxHandler.SaveDocxFile(
                        roundtripBack,
                        wv.PieceTable.GetText(),
                        wv.FormattedRuns,
                        false,
                        0,
                        "GENOFFICE ROUNDTRIP HEADER",
                        "Trang 1 / 1",
                        true,
                        null,
                        null,
                        wv.ParagraphAlignments,
                        null,
                        null,
                        null,
                        null
                    );
                    Console.WriteLine("  -> Đã lưu lại bản roundtrip: " + roundtripBack);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI GENOFFICE LOAD DOCX] " + ex.Message);
            }
        }

        static void CaptureWriterRibbonAndViews(string genDocxPath)
        {
            try
            {
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(1366, 800);
                    form.Ribbon.CurrentMode = OfficeAppMode.Writer;
                    form.WriterView.LoadDocumentFile(genDocxPath, null);

                    string[] tabNames = new string[] {
                        "01_Home_TrangDau",
                        "02_Insert_Chen",
                        "03_Design_ThietKe",
                        "04_Layout_BoTri",
                        "05_References_ThamKhao",
                        "06_Review_XemLai",
                        "07_View_Xem"
                    };

                    for (int i = 0; i < tabNames.Length; i++)
                    {
                        form.Ribbon.ActiveRibbonTab = i;
                        using (Bitmap bmp = new Bitmap(1366, 800))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                form.WriterView.Draw(g, 1366, 800, form.Ribbon);
                                form.Ribbon.Draw(g, 1366, 800, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                            }
                            string imgPath = Path.Combine(WriterImagesDir, "Ribbon_Tab_" + tabNames[i] + ".png");
                            bmp.Save(imgPath, ImageFormat.Png);
                        }
                    }

                    // Capture High-Res Document Layout View
                    using (Bitmap bmp = new Bitmap(1366, 800))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            form.WriterView.Draw(g, 1366, 800, form.Ribbon);
                            form.Ribbon.Draw(g, 1366, 800, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                        }
                        string imgPath = Path.Combine(WriterImagesDir, "GenOffice_Writer_Full_Document_Layout.png");
                        bmp.Save(imgPath, ImageFormat.Png);
                    }
                }

                Console.WriteLine("  -> Đã chụp toàn bộ ảnh Ribbon tabs và Trang tài liệu của Writer!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI CAPTURE WRITER] " + ex.Message);
            }
        }

        static void RenderWriterAnimationFrames(string genDocxPath)
        {
            try
            {
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(1280, 720);
                    form.Ribbon.CurrentMode = OfficeAppMode.Writer;
                    form.WriterView.LoadDocumentFile(genDocxPath, null);

                    int totalFrames = 60;
                    for (int f = 0; f < totalFrames; f++)
                    {
                        form.WriterView.ScrollY = (int)(Math.Sin(f * 0.1) * 150.0 + 150.0);
                        form.WriterView.CursorVisible = (f % 16 < 8);

                        using (Bitmap bmp = new Bitmap(1280, 720))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                form.WriterView.Draw(g, 1280, 720, form.Ribbon);
                                form.Ribbon.Draw(g, 1280, 720, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                            }
                            string framePath = Path.Combine(WriterFramesDir, string.Format("writer_frame_{0:D3}.png", f));
                            bmp.Save(framePath, ImageFormat.Png);
                        }
                    }

                    Console.WriteLine("  -> Đã xuất " + totalFrames + " khung hình cuộn trang & soạn thảo chất lượng cao!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI RENDER WRITER FRAMES] " + ex.Message);
            }
        }
    }
}
