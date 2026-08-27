using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace CreateFullDocxSample
{
    class Program
    {
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
            try
            {
                // 1. Header Title
                AddParagraph("BÁO CÁO TỔNG KẾT DỰ ÁN GENOFFICE 2026", "Segoe UI", 16f, true, false, false, Color.FromArgb(37, 99, 235), 1, 1.2f, 10f, 4f, null);

                // 2. Subtitle
                AddParagraph("Đơn vị: Phòng Nghiên cứu & Phát triển Phần mềm Doanh nghiệp", "Segoe UI", 10.5f, false, true, false, Color.FromArgb(100, 116, 139), 1, 1.15f, 0f, 2f, null);
                AddParagraph("Thời gian: Ngày 27 tháng 08 năm 2026  |  Phiên bản: 3.5.0 Enterprise", "Segoe UI", 9.5f, false, false, false, Color.FromArgb(148, 163, 184), 1, 1.15f, 0f, 12f, null);

                // Divider line under title
                ParagraphBorderInfo divBorder = new ParagraphBorderInfo { IsHorizontalLine = true, Color = Color.FromArgb(203, 213, 225), SizePt = 1.5f };
                AddParagraph("", "Segoe UI", 6f, false, false, false, Color.Transparent, 1, 1f, 0f, 8f, divBorder);

                // 3. Heading 1
                ParagraphBorderInfo h1Border = new ParagraphBorderInfo { Bottom = true, Color = Color.FromArgb(16, 124, 65), SizePt = 1.5f };
                AddParagraph("1. TỔNG QUAN DỰ ÁN & MỤC TIÊU CHIẾN LƯỢC", "Segoe UI", 12.5f, true, false, false, Color.FromArgb(16, 124, 65), 0, 1.2f, 8f, 6f, h1Border);

                // 4. Intro Paragraph with rich formatting
                string introText = "Dự án GenOffice 2026 được triển khai với sứ mệnh xây dựng bộ công cụ văn phòng toàn diện, hiện đại và tốc độ cao dành cho doanh nghiệp. Hệ thống được tối ưu hóa toàn diện trên nền tảng .NET và OpenXML, mang lại khả năng tương thích hoàn hảo 100% với hệ sinh thái Microsoft Office (Word, Excel, PowerPoint).";
                AddParagraph(introText, "Segoe UI", 11f, false, false, false, Color.FromArgb(30, 41, 59), 3, 1.25f, 0f, 8f, null);

                // 5. Heading 1: Table Section
                AddParagraph("2. BẢNG TIẾN ĐỘ THỰC HIỆN & PHÂN BỔ NGÂN SÁCH", "Segoe UI", 12.5f, true, false, false, Color.FromArgb(16, 124, 65), 0, 1.2f, 10f, 6f, h1Border);

                // 6. Data Table
                string tblHeader = "| Hạng mục công việc | Phụ trách | Thời gian | Ngân sách (VNĐ) | Tiến độ hoàn thành |";
                string tblSep    = "| ------------------ | --------- | --------- | ---------------- | ------------------ |";
                string tblR1     = "| Nâng cấp Lõi Soạn thảo Writer | Nguyễn Văn An | Q1/2026 | 150,000,000 | 100% Hoàn thành |";
                string tblR2     = "| Xử lý Bảng tính Calc & Xlsx | Trần Thị Bình | Q2/2026 | 180,000,000 | 100% Hoàn thành |";
                string tblR3     = "| Giao diện Hiện đại Ribbon Bar | Lê Hoàng Cường | Q2/2026 | 95,000,000 | 100% Hoàn thành |";
                string tblR4     = "| Kiểm thử & Tương thích MS Office | Phạm Minh Đức | Q3/2026 | 75,000,000 | 98% Đạt chuẩn |";
                string tblR5     = "| TỔNG CỘNG TOÀN DỰ ÁN | Ban Giám Đốc | 2026 | 500,000,000 | 99.5% Xuất sắc |";

                string fullTbl = string.Join("\n", new string[] { tblHeader, tblSep, tblR1, tblR2, tblR3, tblR4, tblR5 });
                AddParagraph(fullTbl, "Segoe UI", 10.5f, false, false, false, Color.FromArgb(15, 23, 42), 0, 1f, 4f, 8f, null);

                // 7. Heading 1: Key Highlights
                AddParagraph("3. CÁC TÍNH NĂNG ĐỘT PHÁ NỔI BẬT", "Segoe UI", 12.5f, true, false, false, Color.FromArgb(16, 124, 65), 0, 1.2f, 10f, 6f, h1Border);

                AddParagraph("• Tính năng Gộp ô & Chia đôi ô đường chéo (Diagonal Split): Cho phép tạo 2 tiêu đề trên cùng 1 ô chuẩn xác.", "Segoe UI", 11f, false, false, false, Color.FromArgb(30, 41, 59), 0, 1.2f, 2f, 3f, null);
                AddParagraph("• Động cơ hiển thị mượt mà: Không còn hiện tượng giật khung hay mất nét văn bản khi phóng to/thu nhỏ.", "Segoe UI", 11f, false, false, false, Color.FromArgb(30, 41, 59), 0, 1.2f, 2f, 3f, null);
                AddParagraph("• Tương thích OpenXML 2 chiều: Mở và lưu file .docx, .xlsx, .pptx chuẩn xác mà không bị sai lệch định dạng.", "Segoe UI", 11f, false, false, false, Color.FromArgb(30, 41, 59), 0, 1.2f, 2f, 8f, null);

                // 8. Note / Callout Box
                ParagraphBorderInfo noteBorder = new ParagraphBorderInfo { Left = true, Color = Color.FromArgb(245, 158, 11), SizePt = 3.5f };
                AddParagraph("  * Ghi chú quan trọng: Tất cả dữ liệu và định dạng đã được thẩm định bảo mật đạt chuẩn ISO/IEC 27001:2022.", "Segoe UI", 10f, false, true, false, Color.FromArgb(180, 83, 9), 0, 1.2f, 4f, 4f, noteBorder);

                // 9. Save .docx File
                string outDocx = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "BaoCaoTongKet_2026.docx");
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

                Console.WriteLine("SaveDocxFile: " + (saveOk ? "SUCCESS" : "FAILED") + " -> " + outDocx);

                // 10. Load into WriterView and Render GenOffice Screenshot
                WriterView wv = new WriterView();
                wv.LoadDocumentFile(outDocx, null);

                RibbonBar ribbon = new RibbonBar();
                Bitmap bmpWv = new Bitmap(950, 1100);
                using (Graphics g = Graphics.FromImage(bmpWv))
                {
                    g.Clear(Color.FromArgb(240, 243, 246));
                    wv.Draw(g, 950, 1100, ribbon);
                }
                string wvImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "genoffice_rendered_docx.png");
                bmpWv.Save(wvImgPath);
                Console.WriteLine("GenOffice Writer Screenshot: " + wvImgPath);

                Console.WriteLine("=== DOCX_SAMPLE_GENERATION_SUCCESSFUL ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}
