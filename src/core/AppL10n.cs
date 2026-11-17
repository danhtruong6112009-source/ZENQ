using System;
using System.Collections.Generic;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Centralized runtime Vietnamese / English localization helper driven by AppSettings.Instance.Language.
    /// Internal state keys (Ribbon tab IDs, template category keys, etc.) stay canonical while display strings translate live.
    /// </summary>
    public static partial class AppL10n
    {
        public static bool IsEn => AppSettings.Instance.Language == AppLanguage.English;

        public static string T(string vi, string en) => IsEn ? en : vi;

        private static readonly Dictionary<string, string> TabMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Trang chính"] = "Home",
            ["Tệp"] = "File",
            ["Trang đầu"] = "Home",
            ["Chèn"] = "Insert",
            ["Vẽ"] = "Draw",
            ["Thiết kế"] = "Design",
            ["Bố trí"] = "Layout",
            ["Bố cục"] = "Layout",
            ["Bố trí trang"] = "Page Layout",
            ["Tham chiếu"] = "References",
            ["Gửi thư"] = "Mailings",
            ["Công thức"] = "Formulas",
            ["Dữ liệu"] = "Data",
            ["Chuyển tiếp"] = "Transitions",
            ["Hoạt họa"] = "Animations",
            ["Trình chiếu"] = "Slide Show",
            ["Ghi hình"] = "Record",
            ["Xem lại"] = "Review",
            ["Hiển thị"] = "View",
            ["Mở rộng"] = "Add-ins",
            ["Chú thích"] = "Annotate",
            ["Chỉnh sửa"] = "Edit",
            ["Trang"] = "Pages",
            ["Ký & Bảo mật"] = "Sign & Protect",
            ["Lớp & Đối tượng"] = "Layers & Objects",
            ["Bộ lọc & Màu"] = "Filters & Color",
            ["Vẽ & Vector"] = "Paint & Vector",
            ["Văn bản"] = "Text",
            ["Sắp xếp & Căn gióng"] = "Arrange & Align",
            ["Xem & Thước đo"] = "View & Rulers",
            ["Xuất tệp"] = "Export",
            ["Thiết kế Bảng"] = "Table Design",
            ["Bố cục Bảng"] = "Table Layout",
            ["Định dạng Hình ảnh"] = "Picture Format",
            ["Định dạng Hình dạng"] = "Shape Format",
            ["Định dạng Đồ họa"] = "Graphics Format",
            ["Định dạng Hộp chữ"] = "Text Box Format",
            ["Đầu trang & Chân trang"] = "Header & Footer",
            ["Thiết kế Biểu đồ"] = "Chart Design",
            ["Thiết kế SmartArt"] = "SmartArt Design",
            ["Định dạng"] = "Format",
            ["Loại bỏ nền (Background Removal)"] = "Background Removal",
            ["Định dạng Video"] = "Video Format",
            ["Phát lại"] = "Playback",
            ["Định dạng Âm thanh"] = "Audio Format",
        };

        private static readonly Dictionary<string, string> CommonMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Template Discovery Categories & Quick Cards
            ["Tất cả"] = "All",
            ["Thuyết trình"] = "Presentations",
            ["Kinh doanh"] = "Business",
            ["Tài chính"] = "Finance",
            ["Thương hiệu"] = "Brand",
            ["Giáo dục"] = "Education",
            ["Marketing"] = "Marketing",
            ["Bài thuyết trình"] = "Presentations",
            ["Chuẩn 16:9 sắc nét"] = "Crisp 16:9 widescreen",
            ["Kế hoạch kinh doanh"] = "Business Plans",
            ["Pitch Deck gọi vốn"] = "Investor pitch decks",
            ["Báo cáo tài chính"] = "Financial Reports",
            ["Quản trị dòng tiền & KPI"] = "Cash flow & KPI tracking",
            ["Hồ sơ năng lực"] = "Company Profiles",
            ["Thương hiệu & Đội ngũ"] = "Brand & team showcases",
            ["Bài giảng giáo dục"] = "Education Lectures",
            ["Khóa học trực quan"] = "Visual course decks",

            // User roles
            ["Quản trị viên"] = "Administrator",
            ["Nhà sáng tạo"] = "Creator",
            ["Thành viên"] = "Member",

            // Backstage Nav
            ["Trang chủ"] = "Home",
            ["Mới"] = "New",
            ["Mở"] = "Open",
            ["Thông tin"] = "Info",
            ["Lưu"] = "Save",
            ["Lưu dưới dạng"] = "Save As",
            ["In"] = "Print",
            ["Chia sẻ & Gửi Email"] = "Share & Email",
            ["Xuất"] = "Export",
            ["Bản sao lưu tệp gốc"] = "Original Backups",
            ["Font & Tiếng Việt"] = "Fonts & Vietnamese",
            ["Đóng"] = "Close",

            // Backstage App Titles & Pills
            ["Văn bản"] = "Writer Document",
            ["Bảng tính"] = "Spreadsheet",
            ["Bản trình chiếu"] = "Presentation",
            ["Thiết kế & Ảnh"] = "Design & Photo",
            ["Tài liệu PDF"] = "PDF Document",

            // Backstage Presets & Templates
            ["Thuyết trình 16:9"] = "16:9 Widescreen",
            ["Thuyết trình 4:3"] = "4:3 Classic",
            ["Tài liệu A4 Ngang"] = "A4 Landscape",
            ["Tài liệu A4 Dọc"] = "A4 Portrait",
            ["Bài đăng Facebook"] = "Facebook Post",
            ["Bài đăng Instagram"] = "Instagram Post",
            ["Nội dung TikTok và Reels"] = "TikTok & Reels",
            ["Áp phích Poster"] = "Poster",
            ["Đồ họa Thông tin"] = "Infographic",
            ["Tùy chỉnh"] = "Custom Size",
            ["Tự đặt kích thước"] = "Set custom dimensions",
            ["Bản vẽ Full HD 1920x1080"] = "Full HD Canvas 1920x1080",
            ["Ảnh vuông 1080x1080"] = "Square Image 1080x1080",
            ["Ảnh chuẩn 1280x720"] = "HD Canvas 1280x720",
            ["Áp phích áp ảnh 4K"] = "4K Photo Poster",
            ["Biểu ngữ mạng xã hội"] = "Social Banner",
            ["Sổ làm việc trống"] = "Blank Workbook",
            ["Bảng tính ngân sách"] = "Budget Sheet",
            ["Lịch kế hoạch tuần"] = "Weekly Planner",
            ["Bảng chấm công"] = "Timesheet",
            ["Báo cáo doanh số"] = "Sales Report",
            ["Tài liệu trống"] = "Blank Document",
            ["Báo cáo trang bìa"] = "Cover Page Report",
            ["Sơ yếu lý lịch"] = "Resume / CV",
            ["Công văn và Thư từ"] = "Letters & Memos",
            ["Bản tin nội bộ"] = "Internal Newsletter",

            // Shapes & Graphics Pane Categories
            ["Đường kẻ & Mũi tên"] = "Lines & Arrows",
            ["Hình dạng cơ bản"] = "Basic Shapes",
            ["Đa giác đều & Hình học"] = "Polygons & Geometry",
            ["Hình ngôi sao & Huy hiệu"] = "Stars & Badges",
            ["Mũi tên khối & Chỉ hướng"] = "Block & Direction Arrows",
            ["Lưu đồ & Quy trình"] = "Flowchart & Process",
            ["Bóng thoại & Chú thích"] = "Callouts & Bubbles",
            ["Dải ruy băng & Biểu ngữ"] = "Ribbons & Banners",
            ["Hình khối 3D & Isometric"] = "3D & Isometric Shapes",
            ["Sơ đồ tư duy & Cụm liên kết"] = "Diagrams & Mind Maps",
            ["Ký hiệu toán học & Kỹ thuật"] = "Math & Technical Symbols",
            ["Khung viền & Họa tiết trang trí"] = "Frames & Ornaments",
            ["Vàng Gold"] = "Gold Foil",
            ["Bối cảnh minh họa"] = "Illustrations & Scenes",
            ["Dùng gần đây"] = "Recently Used",
            ["Nổi bật"] = "Featured",
            ["Định dạng"] = "Format",
            ["Bố cục"] = "Layout",
            ["Chung"] = "General",
        };

        public static string Tr(string viText)
        {
            if (!IsEn || string.IsNullOrEmpty(viText)) return viText;
            if (CommonMap.TryGetValue(viText, out string en)) return en;
            if (UiMap.TryGetValue(viText, out string uiEn)) return uiEn;
            if (GalleryDialogMap.TryGetValue(viText, out string gdEn)) return gdEn;
            if (FormulaMap.TryGetValue(viText, out string fmEn)) return fmEn;
            if (viText.StartsWith("☑ ", StringComparison.Ordinal) || viText.StartsWith("☐ ", StringComparison.Ordinal) || viText.StartsWith("● ", StringComparison.Ordinal))
                return viText.Substring(0, 2) + Tr(viText.Substring(2));
            if (viText.StartsWith("Độ mờ ", StringComparison.Ordinal))
                return "Opacity " + viText.Substring("Độ mờ ".Length);
            if (viText.StartsWith("Cỡ: ", StringComparison.Ordinal))
                return "Size: " + viText.Substring("Cỡ: ".Length);
            if (viText.StartsWith("Kiểu: ", StringComparison.Ordinal))
                return "Style: " + viText.Substring("Kiểu: ".Length);
            if (viText.StartsWith("Màn hình ", StringComparison.Ordinal))
                return "Monitor " + viText.Substring("Màn hình ".Length);
            if (viText.StartsWith("Màu phát hiện ", StringComparison.Ordinal))
                return "Detected Colors " + viText.Substring("Màu phát hiện ".Length);
            if (viText.StartsWith("Màu ", StringComparison.Ordinal) && viText.Length > 4 && char.IsDigit(viText[4]))
                return "Color " + viText.Substring("Màu ".Length);
            if (viText.StartsWith("Thu phóng (", StringComparison.Ordinal))
                return "Zoom (" + viText.Substring("Thu phóng (".Length);
            if (viText.StartsWith("↔ Rộng: ", StringComparison.Ordinal))
                return "↔ W: " + viText.Substring("↔ Rộng: ".Length);
            if (viText.StartsWith("Rộng: ", StringComparison.Ordinal))
                return "W: " + viText.Substring("Rộng: ".Length);
            if (viText.StartsWith("Thời lượng: ", StringComparison.Ordinal))
                return "Duration: " + viText.Substring("Thời lượng: ".Length);
            if (viText.StartsWith("Thời gian: ", StringComparison.Ordinal))
                return "Duration: " + viText.Substring("Thời gian: ".Length);
            if (viText.StartsWith("Đầu trang Trên: ", StringComparison.Ordinal))
                return "Header Top: " + viText.Substring("Đầu trang Trên: ".Length);
            if (viText.StartsWith("Chân trang Dưới: ", StringComparison.Ordinal))
                return "Footer Bottom: " + viText.Substring("Chân trang Dưới: ".Length);
            if (viText.StartsWith("Chọn cột ", StringComparison.Ordinal))
                return "Select Column " + viText.Substring("Chọn cột ".Length).Replace("(Select Column)", "").TrimEnd();
            if (viText.StartsWith("Chọn hàng ", StringComparison.Ordinal))
                return "Select Row " + viText.Substring("Chọn hàng ".Length).Replace("(Select Row)", "").TrimEnd();
            if (viText.StartsWith("Xóa cột ", StringComparison.Ordinal))
                return "Delete Column " + viText.Substring("Xóa cột ".Length).Replace("(Delete Column)", "").TrimEnd();
            if (viText.StartsWith("Xóa hàng ", StringComparison.Ordinal))
                return "Delete Row " + viText.Substring("Xóa hàng ".Length).Replace("(Delete Row)", "").TrimEnd();
            return viText;
        }

        public static string TrTab(string internalTabName)
        {
            if (!IsEn || string.IsNullOrEmpty(internalTabName)) return internalTabName;
            return TabMap.TryGetValue(internalTabName, out string en) ? en : internalTabName;
        }

        public static string TrCategory(string category) => Tr(category);

        public static string TrRole(string roleVi) => Tr(roleVi);

        public static string TrDocTitle(string title)
        {
            if (!IsEn || string.IsNullOrEmpty(title)) return title;
            if (title == "Trang chính") return "Home";
            if (title == "Mẫu - Đơn xin nghỉ phép.docx") return "Template - Leave Request.docx";
            if (title == "Mẫu - Báo cáo tiến độ dự án.docx") return "Template - Project Progress Report.docx";
            if (title == "Mẫu - Bảng chấm công.csv") return "Template - Timesheet.csv";
            if (title == "Thiết kế Vuông 1080x1080.svg") return "Square Design 1080x1080.svg";
            if (title == "Thiết kế HD 1280x720.svg") return "HD Design 1280x720.svg";
            if (title == "Áp phích Poster 4K.svg") return "4K Poster Design.svg";
            if (title == "Tài liệu mới" || title == "Văn bản Mới.docx") return title.EndsWith(".docx") ? "New Document.docx" : "New Document";
            if (title == "Bảng tính mới" || title == "Bảng tính Mới.xlsx") return title.EndsWith(".xlsx") ? "New Workbook.xlsx" : "New Workbook";
            if (title == "Bản trình chiếu mới" || title == "Trình chiếu Mới.pptx" || title == "Bản trình chiếu Mới.pptx") return title.EndsWith(".pptx") ? "New Presentation.pptx" : "New Presentation";
            if (title == "Bản vẽ mới" || title == "Thiết kế mới" || title == "Thiết kế Mới.svg" || title == "Thiết kế Mới 1.svg") return title.EndsWith(".svg") ? "New Design.svg" : "New Design";
            if (title == "Tài liệu PDF mới" || title == "Tài liệu PDF Mới.pdf") return title.EndsWith(".pdf") ? "New PDF.pdf" : "New PDF";
            string ext = "";
            string core = title;
            foreach (string e in new[] { ".docx", ".xlsx", ".pptx", ".pdf", ".svg" })
            {
                if (title.EndsWith(e, StringComparison.OrdinalIgnoreCase))
                {
                    ext = title.Substring(title.Length - e.Length);
                    core = title.Substring(0, title.Length - e.Length);
                    break;
                }
            }
            if (core.StartsWith("Tài liệu ") && int.TryParse(core.Substring("Tài liệu ".Length), out int wn))
                return "Document " + wn + ext;
            if (core.StartsWith("Văn bản ") && int.TryParse(core.Substring("Văn bản ".Length), out int wn2))
                return "Document " + wn2 + ext;
            if (core.StartsWith("Bảng tính ") && int.TryParse(core.Substring("Bảng tính ".Length), out int cn))
                return "Workbook " + cn + ext;
            if (core.StartsWith("Trình chiếu ") && int.TryParse(core.Substring("Trình chiếu ".Length), out int sn))
                return "Presentation " + sn + ext;
            if (core.StartsWith("Bản trình chiếu ") && int.TryParse(core.Substring("Bản trình chiếu ".Length), out int pn))
                return "Presentation " + pn + ext;
            if (core.StartsWith("Bài trình chiếu ") && int.TryParse(core.Substring("Bài trình chiếu ".Length), out int pn2))
                return "Presentation " + pn2 + ext;
            if (core.StartsWith("Tài liệu PDF ") && int.TryParse(core.Substring("Tài liệu PDF ".Length), out int pdfn))
                return "PDF Document " + pdfn + ext;
            if (core.StartsWith("Thiết kế ") && int.TryParse(core.Substring("Thiết kế ".Length), out int phn))
                return "Design " + phn + ext;
            return title;
        }

        public static string TrTodayDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return dateStr;
            if (IsEn && dateStr.StartsWith("Hôm nay, ", StringComparison.Ordinal))
                return "Today, " + dateStr.Substring("Hôm nay, ".Length);
            if (!IsEn && dateStr.StartsWith("Today, ", StringComparison.Ordinal))
                return "Hôm nay, " + dateStr.Substring("Today, ".Length);
            return dateStr;
        }
    }
}
