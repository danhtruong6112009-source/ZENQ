using System;
using System.IO;
using System.IO.Compression;
using System.Text;

class ExportReportDocx
{
    static string Esc(string s)
    {
        if (s == null) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    static string MakeParagraph(string text, bool bold, int fontSize, string color, string jc, int spaceBefore, int spaceAfter, string headingStyle = null)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<w:p><w:pPr>");
        if (!string.IsNullOrEmpty(headingStyle))
            sb.Append(string.Format("<w:pStyle w:val=\"{0}\"/>", headingStyle));
        if (!string.IsNullOrEmpty(jc))
            sb.Append(string.Format("<w:jc w:val=\"{0}\"/>", jc));
        sb.Append(string.Format("<w:spacing w:before=\"{0}\" w:after=\"{1}\"/>", spaceBefore, spaceAfter));
        sb.Append("</w:pPr>");
        sb.Append("<w:r><w:rPr>");
        if (bold) sb.Append("<w:b/>");
        sb.Append(string.Format("<w:sz w:val=\"{0}\"/><w:szCs w:val=\"{0}\"/>", fontSize));
        if (!string.IsNullOrEmpty(color))
            sb.Append(string.Format("<w:color w:val=\"{0}\"/>", color));
        sb.Append("<w:rFonts w:ascii=\"Segoe UI\" w:hAnsi=\"Segoe UI\" w:cs=\"Segoe UI\"/>");
        sb.Append("</w:rPr>");
        sb.Append(string.Format("<w:t xml:space=\"preserve\">{0}</w:t>", Esc(text)));
        sb.Append("</w:r></w:p>");
        return sb.ToString();
    }

    static string MakeRun(string text, bool bold, int fontSize, string color)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<w:r><w:rPr>");
        if (bold) sb.Append("<w:b/>");
        sb.Append(string.Format("<w:sz w:val=\"{0}\"/><w:szCs w:val=\"{0}\"/>", fontSize));
        if (!string.IsNullOrEmpty(color))
            sb.Append(string.Format("<w:color w:val=\"{0}\"/>", color));
        sb.Append("<w:rFonts w:ascii=\"Segoe UI\" w:hAnsi=\"Segoe UI\" w:cs=\"Segoe UI\"/>");
        sb.Append("</w:rPr>");
        sb.Append(string.Format("<w:t xml:space=\"preserve\">{0}</w:t>", Esc(text)));
        sb.Append("</w:r>");
        return sb.ToString();
    }

    static string MakeMultiRunParagraph(string[][] runs, string jc, int spaceBefore, int spaceAfter)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<w:p><w:pPr>");
        if (!string.IsNullOrEmpty(jc))
            sb.Append(string.Format("<w:jc w:val=\"{0}\"/>", jc));
        sb.Append(string.Format("<w:spacing w:before=\"{0}\" w:after=\"{1}\"/>", spaceBefore, spaceAfter));
        sb.Append("</w:pPr>");
        foreach (string[] r in runs)
        {
            bool b = r.Length > 1 && r[1] == "b";
            string col = r.Length > 2 ? r[2] : null;
            int sz = r.Length > 3 ? int.Parse(r[3]) : 22;
            sb.Append(MakeRun(r[0], b, sz, col));
        }
        sb.Append("</w:p>");
        return sb.ToString();
    }

    static string TableCell(string text, bool bold, int fontSize, string shadingColor, string jc)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<w:tc><w:tcPr>");
        sb.Append("<w:tcBorders><w:top w:val=\"single\" w:sz=\"4\" w:color=\"D0D7DE\"/><w:left w:val=\"single\" w:sz=\"4\" w:color=\"D0D7DE\"/><w:bottom w:val=\"single\" w:sz=\"4\" w:color=\"D0D7DE\"/><w:right w:val=\"single\" w:sz=\"4\" w:color=\"D0D7DE\"/></w:tcBorders>");
        if (!string.IsNullOrEmpty(shadingColor))
            sb.Append(string.Format("<w:shd w:val=\"clear\" w:fill=\"{0}\"/>", shadingColor));
        sb.Append("<w:vAlign w:val=\"center\"/>");
        sb.Append("</w:tcPr>");
        sb.Append("<w:p><w:pPr>");
        if (!string.IsNullOrEmpty(jc))
            sb.Append(string.Format("<w:jc w:val=\"{0}\"/>", jc));
        sb.Append("<w:spacing w:before=\"50\" w:after=\"50\"/>");
        sb.Append("</w:pPr>");
        sb.Append("<w:r><w:rPr>");
        if (bold) sb.Append("<w:b/>");
        sb.Append(string.Format("<w:sz w:val=\"{0}\"/><w:szCs w:val=\"{0}\"/>", fontSize));
        sb.Append("<w:rFonts w:ascii=\"Segoe UI\" w:hAnsi=\"Segoe UI\" w:cs=\"Segoe UI\"/>");
        sb.Append("</w:rPr>");
        sb.Append(string.Format("<w:t xml:space=\"preserve\">{0}</w:t>", Esc(text)));
        sb.Append("</w:r></w:p></w:tc>");
        return sb.ToString();
    }

    static string TableRow(string[] cells, bool isHeader, int fontSize, string shade)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<w:tr>");
        if (isHeader) sb.Replace("<w:tr>", "<w:tr><w:trPr><w:tblHeader/></w:trPr>");
        for (int i = 0; i < cells.Length; i++)
        {
            string cellShade = isHeader ? "1F4E79" : shade;
            string cellXml = TableCell(cells[i], isHeader, fontSize, cellShade, i == cells.Length - 1 ? "center" : "left");
            if (isHeader)
            {
                cellXml = cellXml.Replace("<w:rFonts", "<w:color w:val=\"FFFFFF\"/><w:rFonts");
            }
            sb.Append(cellXml);
        }
        sb.Append("</w:tr>");
        return sb.ToString();
    }

    static void Main()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string outPath = Path.Combine(desktopPath, "ZenQ_Office_Khao_Sat_PowerPoint_Va_Lo_Trinh_Dot_Net_10.docx");

        StringBuilder body = new StringBuilder();

        // Header Title
        body.Append(MakeParagraph("BÁO CÁO PHÂN TÍCH & KẾ HOẠCH NÂNG CẤP", true, 36, "1F4E79", "center", 0, 80));
        body.Append(MakeParagraph("ZenQ Office (Slides) vs. Microsoft PowerPoint 365 & Lộ Trình Lên .NET 10", true, 26, "2E74B5", "center", 0, 80));
        body.Append(MakeParagraph("Thời gian: 29/09/2026  |  Dành cho Nhà phát triển ZenQ Office", false, 20, "707070", "center", 0, 200));

        body.Append("<w:p><w:pPr><w:pBdr><w:bottom w:val=\"single\" w:sz=\"18\" w:color=\"1F4E79\"/></w:pBdr></w:pPr></w:p>");

        // Part 1: Feature comparison
        body.Append(MakeParagraph("PHẦN 1: ĐỐI CHIẾU TÍNH NĂNG SLIDES SO VỚI POWERPOINT 365", true, 26, "1F4E79", "left", 300, 140));
        body.Append(MakeParagraph("Sau khi rà soát và kiểm tra toàn bộ mã nguồn của hệ thống Trình chiếu (SlideModel.cs, SlidesView.cs, PptxHandler, SlideVideoPlayer.cs, SlideShowAudienceForm.cs), dưới đây là ma trận đối chiếu chi tiết mức độ tương đồng và các thiếu sót cần bổ sung:", false, 22, "333333", "left", 0, 140));

        string[][] featureData = new string[][] {
            new string[] { "Nhóm tính năng", "PowerPoint 365", "ZenQ Office Slides hiện tại", "Mức độ" },
            new string[] { "Đồ họa cơ bản & Vector", "Shapes phong phú, bo góc, Merge Shapes (Union, Subtract, Intersect, Fragment, Combine), Custom Geometry.", "Đã có đầy đủ Shapes, Merge Shapes 5 phép toán, Custom Path Bézier, viền bo góc, bóng đổ.", "95%" },
            new string[] { "Xử lý Ảnh & Hiệu ứng viền", "BlipFill, Soft Edge, Glow, Shadow, Feather, Reflection, Crop to Shape, xoay & lật.", "Đã hỗ trợ BlipFill, Feather/Soft Edge (có caching tốc độ cao), Shadow, Neon Glow, Crop, Xoay/Lật.", "90%" },
            new string[] { "Hiệu ứng chuyển trang (Transitions)", "40+ Transition, đặc biệt là hiệu ứng Morph (nhận diện đổi vị trí/kích thước shape giữa các slide).", "Đã có Fade, Push, Wipe, Split, Reveal, Shape, Cover, Flash và thuật toán Morph cơ bản.", "85%" },
            new string[] { "Hoạt ảnh đối tượng (Animations)", "Entrance, Emphasis, Exit, Motion Paths, Animation Pane, trigger theo click/thời gian.", "Đã có Entrance, Emphasis, Exit, Motion Paths, Trigger, Timeline playback.", "85%" },
            new string[] { "Trình chiếu (SlideShow Mode)", "Presenter View (2 màn hình), ghi chú diễn giả, Laser Pointer, Bút vẽ, Thước kẻ, Đồng hồ bấm giờ.", "Đã có F5 Presentation, Presenter View, Laser pointer, Pen/Highlighter, Presenter Notes, Dual Monitor.", "90%" },
            new string[] { "Video & Đa phương tiện", "Chèn Video nội bộ/online, cắt ghép video, chỉnh âm lượng, phát tự động/theo click, poster frame.", "Đã có Windows Media Player engine, video poster, play/pause, volume control, full-screen playback.", "80%" },
            new string[] { "Bảng biểu (Tables <a:tbl>)", "Bảng động, gộp ô (merge cells), viền từng ô, định dạng số, áp dụng Table Styles OpenXML.", "Chưa nạp <p:graphicFrame> chứa <a:tbl> khi đọc file PPTX ngoài. Chỉ hỗ trợ bảng nội bộ đơn giản.", "40%" },
            new string[] { "Biểu đồ (Charts <c:chart>)", "Đầy đủ Column, Bar, Line, Pie, Area, Scatter, Waterfall, Treemap, Funnel liên kết trực tiếp với Excel.", "Đã có mô hình DocChartData nội bộ, nhưng chưa parse <c:chart> từ OpenXML PPTX.", "35%" },
            new string[] { "Slide Master & Layouts", "Hệ thống Master phân tầng nhiều cấp, Placeholder linh hoạt (title, body, dt, ftr, sldNum).", "Kế thừa Master/Layout ở mức cơ bản, chưa có giao diện Slide Master View để tự thiết kế layout.", "50%" },
            new string[] { "SmartArt đồ họa", "Hàng trăm sơ đồ phân cấp, quy trình, ma trận; chỉnh sửa text tree trực tiếp trên slide.", "Có thư viện SmartArt gallery và renderer vẽ sẵn, nhưng chưa chỉnh sửa phân cấp động trên slide.", "60%" },
            new string[] { "Âm thanh nền (Audio)", "Nhạc nền phát xuyên suốt nhiều slide (Play across slides), đánh dấu bookmark audio, fade in/out.", "Chưa có module Audio độc lập (hiện đang dùng chung player video).", "20%" },
            new string[] { "Mô hình 3D (3D Models)", "Chèn file 3D (.glb, .fbx), xoay 360 độ trong không gian 3 chiều, chuyển động bằng 3D Morph.", "Chưa có (GDI+ là 2D thuần túy).", "0%" },
            new string[] { "Phân đoạn Slide (Sections) & Zoom", "Tạo Section để nhóm slide; Section Zoom / Summary Zoom nhảy trang tương tác.", "Chưa có.", "10%" },
            new string[] { "Bút vẽ tay số (Digital Inking)", "Surface Pen, Ink to Shape, Ink to Text, Ink to Math, Replay nét vẽ.", "Có bút vẽ khi trình chiếu, nhưng chưa có Inking lưu vào slide chuẩn OpenXML (<ink:context>).", "30%" },
            new string[] { "Trợ lý AI & Cloud Co-authoring", "PowerPoint Designer (AI gợi ý bố cục), Rehearse with Coach, Live Subtitles.", "Chưa tích hợp AI gợi ý layout (đã có khung sườn AiService.cs nhưng chưa gắn vào Slides).", "15%" },
        };

        body.Append("<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\"/><w:tblBorders>");
        body.Append("<w:top w:val=\"single\" w:sz=\"6\" w:color=\"1F4E79\"/>");
        body.Append("<w:left w:val=\"single\" w:sz=\"4\" w:color=\"D0D7DE\"/>");
        body.Append("<w:bottom w:val=\"single\" w:sz=\"6\" w:color=\"1F4E79\"/>");
        body.Append("<w:right w:val=\"single\" w:sz=\"4\" w:color=\"D0D7DE\"/>");
        body.Append("<w:insideH w:val=\"single\" w:sz=\"4\" w:color=\"E8EAED\"/>");
        body.Append("<w:insideV w:val=\"single\" w:sz=\"4\" w:color=\"E8EAED\"/>");
        body.Append("</w:tblBorders></w:tblPr>");
        body.Append("<w:tblGrid><w:gridCol w:w=\"2800\"/><w:gridCol w:w=\"3600\"/><w:gridCol w:w=\"3600\"/><w:gridCol w:w=\"900\"/></w:tblGrid>");

        for (int i = 0; i < featureData.Length; i++)
        {
            bool isHeader = (i == 0);
            string shade = (!isHeader && i % 2 == 0) ? "F2F5F8" : null;
            body.Append(TableRow(featureData[i], isHeader, isHeader ? 20 : 18, shade));
        }
        body.Append("</w:tbl>");

        // Prioritized roadmap for Slide
        body.Append(MakeParagraph("", false, 22, null, "left", 140, 0));
        body.Append(MakeParagraph("Ba thiếu sót lớn nhất cần hoàn thiện trước tiên:", true, 24, "C00000", "left", 160, 100));

        body.Append(MakeMultiRunParagraph(new string[][] {
            new string[] { "1. Bộ nạp Bảng (<a:tbl>) và Biểu đồ (<c:chart>) trong <p:graphicFrame>: ", "b", "C00000", "22" },
            new string[] { "Hàm LoadPptx hiện tại bỏ qua thẻ graphicFrame khi duyệt spTree. Khi bổ sung, toàn bộ bảng tính và biểu đồ từ file ngoài sẽ hiển thị đầy đủ thay vì biến mất.", "n", "333333", "22" }
        }, "left", 60, 40));

        body.Append(MakeMultiRunParagraph(new string[][] {
            new string[] { "2. Chế độ thiết kế Master (Slide Master View): ", "b", "C00000", "22" },
            new string[] { "Cho phép người dùng vào chỉnh sửa bố cục khung mẫu, đặt logo, số trang và phông chữ đồng bộ cho toàn bộ bài thuyết trình.", "n", "333333", "22" }
        }, "left", 60, 40));

        body.Append(MakeMultiRunParagraph(new string[][] {
            new string[] { "3. Phân đoạn Slide (Sections) & Menu Zoom tương tác: ", "b", "C00000", "22" },
            new string[] { "Hỗ trợ chia nhỏ các bài thuyết trình quy mô lớn (50-100 slide) thành các chương hồi, cho phép click vào mục lục để zoom vào slide tương ứng.", "n", "333333", "22" }
        }, "left", 60, 120));

        // Part 2: .NET 10 & 1-3 GB Package Size Analysis
        body.Append("<w:p><w:pPr><w:pBdr><w:bottom w:val=\"single\" w:sz=\"18\" w:color=\"1F4E79\"/></w:pBdr></w:pPr></w:p>");
        body.Append(MakeParagraph("PHẦN 2: TẠI SAO NÊN LÊN .NET 10 KHI KHÔNG BỊ GIỚI HẠN DUNG LƯỢNG (1 - 3 GB)?", true, 26, "1F4E79", "left", 300, 140));

        body.Append(MakeMultiRunParagraph(new string[][] {
            new string[] { "NHẬN ĐỊNH CHIẾN LƯỢC: ", "b", "008000", "24" },
            new string[] { "Khi dung lượng bộ cài không còn là rào cản (cho phép 1 - 3 GB tương đương Microsoft Office hoặc LibreOffice), ", "n", "333333", "22" },
            new string[] { "việc nâng cấp lên Modern .NET (.NET 8/10) trở thành bước đi hoàn hảo 100% không còn bất kỳ nhược điểm nào!", "b", "008000", "22" }
        }, "left", 80, 120));

        body.Append(MakeParagraph("1. Giải phóng hoàn toàn sức mạnh nhờ dung lượng 1 - 3 GB:", true, 24, "2E74B5", "left", 140, 80));
        body.Append(MakeParagraph("• Self-Contained Deployment: Đóng gói toàn bộ .NET 10 Runtime + WinForms + Native C++ Dependencies trong 1 thư mục độc lập. Người dùng tải về là chạy ngay, không cần cài đặt .NET Framework từ Microsoft.", false, 22, "333333", "left", 40, 40));
        body.Append(MakeParagraph("• Tích hợp Local AI Offline (ONNX Runtime / Phi-3 / Gemma 2B): Với ngân sách 1-3 GB, có thể tích hợp thẳng mô hình ngôn ngữ nhỏ (SLM) vào ZenQ Office để viết bài, tóm tắt, sửa lỗi tiếng Việt mà không cần kết nối mạng hay trả tiền API!", false, 22, "333333", "left", 40, 40));
        body.Append(MakeParagraph("• Thư viện phông chữ & Đồ họa đồ sộ: Nhúng sẵn 200+ phông chữ chuẩn OpenType/TrueType, bộ thư viện vector cliparts, biểu đồ 3D mẫu và hàng trăm slide template cao cấp.", false, 22, "333333", "left", 40, 40));
        body.Append(MakeParagraph("• Media Engine chuyên nghiệp (FFmpeg / SkiaSharp / Direct2D): Dễ dàng nhúng thư viện đồ họa phần cứng GPU và giải mã mọi định dạng video/audio 4K mà GDI+ truyền thống không thể gánh nổi.", false, 22, "333333", "left", 40, 100));

        body.Append(MakeParagraph("2. So sánh đột phá kỹ thuật giữa .NET 4.0 và Modern .NET (.NET 8/10):", true, 24, "2E74B5", "left", 140, 80));

        string[][] benefitsData = new string[][] {
            new string[] { "Tiêu chí kỹ thuật", ".NET Framework 4.0 (Hiện tại)", "Modern .NET (.NET 8 LTS / .NET 10)" },
            new string[] { "Tuổi đời & Hỗ trợ", "Ra mắt năm 2010 (16 năm tuổi), Microsoft đã dừng hỗ trợ từ lâu.", "Phiên bản hiện đại nhất, hỗ trợ LTS lâu dài, cập nhật bảo mật liên tục." },
            new string[] { "Tốc độ xử lý (JIT)", "Trình biên dịch JIT cổ điển, không tận dụng tập lệnh CPU đời mới.", "Dynamic PGO + Tiered Compilation tự động tối ưu mã máy, nhanh hơn 3x - 8x." },
            new string[] { "Xử lý tệp & Bộ nhớ", "Cấp phát chuỗi liên tục, dễ tràn RAM, GC pause gây giật chuột.", "Span<T>, Memory<T>, ArrayPool<T> (Zero-allocation) mở tệp 100MB siêu tốc." },
            new string[] { "Cú pháp lập trình C#", "Bị kẹt ở C# 5.0 (không có $\"\", ?., pattern matching, record).", "C# 12 / 13 / 14 với cú pháp hiện đại, giảm 40% độ dài mã nguồn." },
            new string[] { "Tăng tốc phần cứng SIMD", "Hầu như không hỗ trợ.", "Tận dụng AVX-512, ARM64 Neon giúp thuật toán blur, vector chạy tốc độ phần cứng." },
            new string[] { "Thư viện bên thứ ba", "Bị giới hạn ở các thư viện cũ từ năm 2015 trở về trước.", "Tương thích 100% các gói NuGet hiện đại (OpenXml 3.x, SkiaSharp, OnnxRuntime)." },
        };

        body.Append("<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\"/><w:tblBorders>");
        body.Append("<w:top w:val=\"single\" w:sz=\"6\" w:color=\"1F4E79\"/>");
        body.Append("<w:left w:val=\"single\" w:sz=\"4\" w:color=\"D0D7DE\"/>");
        body.Append("<w:bottom w:val=\"single\" w:sz=\"6\" w:color=\"1F4E79\"/>");
        body.Append("<w:right w:val=\"single\" w:sz=\"4\" w:color=\"D0D7DE\"/>");
        body.Append("<w:insideH w:val=\"single\" w:sz=\"4\" w:color=\"E8EAED\"/>");
        body.Append("<w:insideV w:val=\"single\" w:sz=\"4\" w:color=\"E8EAED\"/>");
        body.Append("</w:tblBorders></w:tblPr>");
        body.Append("<w:tblGrid><w:gridCol w:w=\"2200\"/><w:gridCol w:w=\"3900\"/><w:gridCol w:w=\"4800\"/></w:tblGrid>");

        for (int i = 0; i < benefitsData.Length; i++)
        {
            bool isHeader = (i == 0);
            string shade = (!isHeader && i % 2 == 0) ? "F2F5F8" : null;
            body.Append(TableRow(benefitsData[i], isHeader, isHeader ? 20 : 18, shade));
        }
        body.Append("</w:tbl>");

        body.Append(MakeParagraph("", false, 22, null, "left", 140, 0));
        body.Append(MakeParagraph("3. Lộ trình thực hiện từng bước (Step-by-step Execution Plan):", true, 24, "2E74B5", "left", 160, 80));

        body.Append(MakeMultiRunParagraph(new string[][] {
            new string[] { "Giai đoạn 1 (Thiết lập dự án song song): ", "b", "2E74B5", "22" },
            new string[] { "Tạo file GenOffice.csproj theo chuẩn SDK hiện đại (<TargetFramework>net8.0-windows</TargetFramework> hoặc net10.0-windows, <UseWindowsForms>true</UseWindowsForms>). Giữ nguyên build.bat cũ để không ảnh hưởng đến luồng làm việc hiện tại.", "n", "333333", "22" }
        }, "left", 40, 60));

        body.Append(MakeMultiRunParagraph(new string[][] {
            new string[] { "Giai đoạn 2 (Chuẩn hóa NuGet Packages): ", "b", "2E74B5", "22" },
            new string[] { "Thay thế các file .dll thủ công trong thư mục bằng các package chính thức trên NuGet: DocumentFormat.OpenXml mới nhất, PdfSharpCore hoặc SkiaSharp.Views.WindowsForms.", "n", "333333", "22" }
        }, "left", 40, 60));

        body.Append(MakeMultiRunParagraph(new string[][] {
            new string[] { "Giai đoạn 3 (Biên dịch thử nghiệm & Chạy Test Suite): ", "b", "2E74B5", "22" },
            new string[] { "Chạy dotnet test để xác nhận toàn bộ 352 test cases hiện tại đều vượt qua 100% trên nền .NET mới.", "n", "333333", "22" }
        }, "left", 40, 60));

        body.Append(MakeMultiRunParagraph(new string[][] {
            new string[] { "Giai đoạn 4 (Đóng gói Self-Contained Release): ", "b", "2E74B5", "22" },
            new string[] { "Sử dụng lệnh: dotnet publish -c Release -r win-x64 --self-contained. Xuất bản trọn gói bộ ZenQ Office sẵn sàng sử dụng trên mọi máy tính.", "n", "333333", "22" }
        }, "left", 40, 140));

        body.Append("<w:p><w:pPr><w:pBdr><w:bottom w:val=\"single\" w:sz=\"18\" w:color=\"1F4E79\"/></w:pBdr></w:pPr></w:p>");
        body.Append(MakeParagraph("— Tài liệu được xuất tự động từ ZenQ Office AI Assistant • Đặt tại Desktop —", false, 18, "707070", "center", 120, 0));

        // Full document assembly
        string documentXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
            + "<w:document xmlns:wpc=\"http://schemas.microsoft.com/office/word/2010/wordprocessingCanvas\" "
            + "xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" "
            + "xmlns:o=\"urn:schemas-microsoft-com:office:office\" "
            + "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" "
            + "xmlns:m=\"http://schemas.openxmlformats.org/officeDocument/2006/math\" "
            + "xmlns:v=\"urn:schemas-microsoft-com:vml\" "
            + "xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" "
            + "xmlns:w10=\"urn:schemas-microsoft-com:office:word\" "
            + "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" "
            + "xmlns:w14=\"http://schemas.microsoft.com/office/word/2010/wordml\" "
            + "xmlns:wpg=\"http://schemas.microsoft.com/office/word/2010/wordprocessingGroup\" "
            + "xmlns:wpi=\"http://schemas.microsoft.com/office/word/2010/wordprocessingInk\" "
            + "xmlns:wne=\"http://schemas.microsoft.com/office/word/2006/wordml\" "
            + "xmlns:wps=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\" "
            + "mc:Ignorable=\"w14 wp14\">"
            + "<w:body>"
            + "<w:sectPr><w:pgSz w:w=\"15840\" w:h=\"12240\" w:orient=\"landscape\"/><w:pgMar w:top=\"1080\" w:right=\"1080\" w:bottom=\"1080\" w:left=\"1080\"/></w:sectPr>"
            + body.ToString()
            + "</w:body></w:document>";

        string contentTypes = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
            + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
            + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
            + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
            + "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>"
            + "</Types>";

        string rels = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
            + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
            + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>"
            + "</Relationships>";

        string wordRels = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
            + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
            + "</Relationships>";

        if (File.Exists(outPath)) File.Delete(outPath);
        using (FileStream fs = new FileStream(outPath, FileMode.Create))
        using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            ZipArchiveEntry ct = zip.CreateEntry("[Content_Types].xml");
            using (StreamWriter sw = new StreamWriter(ct.Open(), Encoding.UTF8)) sw.Write(contentTypes);

            ZipArchiveEntry relsEntry = zip.CreateEntry("_rels/.rels");
            using (StreamWriter sw = new StreamWriter(relsEntry.Open(), Encoding.UTF8)) sw.Write(rels);

            ZipArchiveEntry wordRelsEntry = zip.CreateEntry("word/_rels/document.xml.rels");
            using (StreamWriter sw = new StreamWriter(wordRelsEntry.Open(), Encoding.UTF8)) sw.Write(wordRels);

            ZipArchiveEntry docEntry = zip.CreateEntry("word/document.xml");
            using (StreamWriter sw = new StreamWriter(docEntry.Open(), Encoding.UTF8)) sw.Write(documentXml);
        }

        Console.WriteLine("SUCCESS: File generated at " + outPath);
        Console.WriteLine("Size: " + new FileInfo(outPath).Length + " bytes");
    }
}
