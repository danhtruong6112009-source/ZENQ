/**
 * ZENQ OFFICE - OFFICIAL BILINGUAL DICTIONARY (VI / EN)
 * Được biên soạn thủ công chuẩn xác ngữ nghĩa công nghệ & hành văn doanh nghiệp quốc tế.
 * Tuyệt đối không dùng máy dịch thô sơ.
 */

const DICTIONARY = {
  vi: {
    // Navigation
    nav_brand: "ZENQ OFFICE",
    nav_home: "Trang chủ",
    nav_features: "Phân hệ ứng dụng",
    nav_architecture: "Kiến trúc & Hiệu năng",
    nav_about: "Về chúng tôi",
    nav_faq: "Hỏi & Đáp",
    nav_download: "Tải ứng dụng",
    nav_lang_btn: "English",

    // Hero Section
    hero_badge: "Phiên bản v4.0 • Kiến trúc .NET 10 & Direct2D Siêu Tốc",
    hero_title_1: "Bộ Ứng Dụng Văn Phòng",
    hero_title_accent: "Thế Hệ Mới Cho Windows",
    hero_desc: "Tận hưởng tốc độ mở tệp trong chớp mắt 0.05 giây, công nghệ đồ họa Direct2D mượt mà 120 FPS, và bảo mật ngoại tuyến 100%. Tương thích hoàn hảo chuẩn tài liệu Microsoft Office mà không tiêu tốn tài nguyên hệ thống.",
    hero_cta_download: "Tải ZenQ Office Cho Windows",
    hero_cta_explore: "Khám Phá Tính Năng",
    hero_subtext: "Miễn phí • Hoạt động độc lập không cần Internet • Dung lượng cài đặt ~77MB",

    // Quick Stats Bar
    stat_speed_val: "0.05s",
    stat_speed_lbl: "Khởi động tức thì",
    stat_speed_sub: "Nhanh hơn 10x so với MS Office",
    stat_fps_val: "120 FPS",
    stat_fps_lbl: "Mượt mà Direct2D",
    stat_fps_sub: "Tăng tốc phần cứng GPU",
    stat_privacy_val: "100%",
    stat_privacy_lbl: "Bảo mật ngoại tuyến",
    stat_privacy_sub: "Không thu thập Telemetry",
    stat_ram_val: "-85%",
    stat_ram_lbl: "Tiết kiệm bộ nhớ",
    stat_ram_sub: "Chỉ ~60MB RAM vận hành",

    // Apps Showcase Section
    apps_section_badge: "HỆ SINH THÁI ĐỒNG BỘ",
    apps_section_title: "Bốn Phân Hệ Cốt Lõi. Một Nền Tảng Đột Phá.",
    apps_section_desc: "ZenQ Office cho phép mở từng phân hệ độc lập từ lối tắt Desktop hoặc sử dụng chung trong giao diện Tab thông minh. Không cửa sổ thừa, không giật lag.",
    
    app_tab_writer: "ZenQ Writer",
    app_tab_calc: "ZenQ Calc",
    app_tab_slides: "ZenQ Slides",
    app_tab_pdf: "ZenQ PDF",
    app_tab_mobile: "ZenQ Remote",

    // Writer Details
    writer_title: "ZenQ Writer — Soạn thảo văn bản chuẩn in ấn đỉnh cao",
    writer_desc: "Xử lý định dạng .docx, .doc, .rtf, .txt với độ trung thực tuyệt đối. Hỗ trợ dàn trang hai mặt, bảng biểu tự động điều chỉnh, bộ đếm từ trực tiếp, và tính năng dự đoán từ thông minh giúp đẩy nhanh tốc độ gõ phím.",
    writer_feat_1: "Tương thích 100% chuẩn OpenXML Microsoft Word",
    writer_feat_2: "Dự đoán từ thông minh song ngữ Việt - Anh thời gian thực",
    writer_feat_3: "Hỗ trợ chèn công thức toán học, bảng biểu phức tạp và hình vẽ vector",
    writer_feat_4: "Tự động lưu bản thảo an toàn, phòng chống mất dữ liệu khi mất điện",

    // Calc Details
    calc_title: "ZenQ Calc — Bảng tính điện tử triệu dòng mượt mà",
    calc_desc: "Tải và tính toán các tập dữ liệu bảng tính .xlsx khổng lồ chỉ trong vài phần trăm giây. Hệ thống công thức tài chính, logic, tìm kiếm chuẩn mực kết hợp cùng bộ vẽ biểu đồ phân tích trực quan hóa dữ liệu hiện đại.",
    calc_feat_1: "Đọc/ghi tệp .xlsx, .xls, .csv, .tsv với tốc độ cực đại",
    calc_feat_2: "Hơn 300+ hàm tính toán chuẩn Excel (SUM, VLOOKUP, XLOOKUP, IF, INDEX/MATCH)",
    calc_feat_3: "Vẽ biểu đồ cột, tròn, đường nét và phân tán thời gian thực",
    calc_feat_4: "Định dạng có điều kiện, cố định dòng cột và lọc dữ liệu tốc độ cao",

    // Slides Details
    slides_title: "ZenQ Slides — Thiết kế trình chiếu chuẩn PowerPoint 365",
    slides_desc: "Bộ công cụ thiết kế bài thuyết trình mạnh mẽ với hiệu ứng hoạt họa mượt mà, chuyển trang tinh tế, hỗ trợ vector shape, bảng biểu biểu diễn và chế độ Người thuyết trình (Presenter View) toàn màn hình chuyên nghiệp.",
    slides_feat_1: "Mở và biên tập tệp .pptx, .ppt giữ nguyên bố cục và phông chữ",
    slides_feat_2: "Chế độ Người thuyết trình 2 màn hình chuẩn mực (Ghi chú, đồng hồ đếm, slide kế tiếp)",
    slides_feat_3: "Tăng tốc hiển thị hoạt họa slide bằng Direct2D không giật khung hình",
    slides_feat_4: "Bộ sưu tập hình khối chuẩn và công cụ căn chỉnh nam châm thông minh",

    // PDF Details
    pdf_title: "ZenQ PDF — Đọc & Biên tập tài liệu bảo mật tuyệt đối",
    pdf_desc: "Mở các tệp tài liệu PDF dung lượng hàng ngàn trang ngay lập tức. Tích hợp công cụ tìm kiếm từ khóa, đánh dấu văn bản, xoay trang, xuất ảnh chất lượng cao và bảo vệ quyền riêng tư không gửi tệp lên máy chủ đám mây.",
    pdf_feat_1: "Động cơ kết xuất PDFium chuyên dụng cho độ nét văn bản tối đa",
    pdf_feat_2: "Tìm kiếm văn bản tức thì và xem trước thumbnail trang mượt mà",
    pdf_feat_3: "Đăng ký ứng dụng đọc PDF mặc định Windows 10 & Windows 11 tiện lợi",
    pdf_feat_4: "Bảo mật nội bộ 100%, không lo rò rỉ tài liệu nhạy cảm",

    // Mobile Remote Details
    mobile_title: "ZenQ Remote — Biến điện thoại thành bàn di chuột & bàn phím thông minh",
    mobile_desc: "Kết nối điện thoại thông minh cùng mạng LAN để điều khiển slide từ xa, nhập văn bản từ xa, hỗ trợ cử chỉ vuốt chạm 2 ngón, con trỏ ảo, và phím tắt điều khiển thuyết trình tiện lợi.",
    mobile_feat_1: "Quét mã QR kết nối tức thì, không cần cài đặt ứng dụng phức tạp",
    mobile_feat_2: "Bàn di chuột cảm ứng mượt mà với chuột trái, phải và con lăn hai ngón",
    mobile_feat_3: "Bàn phím ảo hỗ trợ đầy đủ phím Ctrl, Alt, Shift và chụp màn hình",
    mobile_feat_4: "Tự động ẩn con trỏ chuột khi điều khiển cảm ứng để tránh nhiễu màn hình",

    // Engineering Architecture
    arch_section_badge: "CÔNG NGHỆ LÕI",
    arch_section_title: "Được Thiết Kế Vì Tốc Độ & Quyền Tự Chủ Kỹ Thuật",
    arch_section_desc: "Không dùng Electron nặng nề, không đóng gói trình duyệt web cồng kềnh. ZenQ Office được xây dựng thuần túy bằng ngôn ngữ C# và .NET 10 tối ưu cấp nhị phân, giao tiếp trực tiếp với Windows API.",

    arch_card1_title: "Tăng Tốc Phần Cứng Direct2D",
    arch_card1_desc: "Tận dụng tối đa card đồ họa GPU để vẽ từng ký tự, nét vẽ vector và chuyển động slide ở tần số quét màn hình 60Hz - 120Hz mượt mà.",
    arch_card2_title: "Khởi Chạy App Nhánh Độc Lập",
    arch_card2_desc: "Mở thẳng tệp Word vào ZenQ Writer, Excel vào ZenQ Calc từ màn hình chính Desktop mà không phải tải toàn bộ App Mẹ cồng kềnh.",
    arch_card3_title: "100% Ngoại Tuyến & Riêng Tư",
    arch_card3_desc: "Không tài khoản bắt buộc, không gửi dữ liệu người dùng về máy chủ, không quảng cáo chen ngang. Tài liệu của bạn an toàn tuyệt đối trên máy tính của bạn.",
    arch_card4_title: "Bộ Nhớ Tinh Gọn ~60MB RAM",
    arch_card4_desc: "Thuật toán quản lý bộ nhớ đệm thông minh giải phóng RAM ngay khi đóng tệp, giữ cho máy tính luôn mát mẻ và mượt mà dù cấu hình khiêm tốn.",

    // Comparison Table
    comp_section_badge: "BẢNG SO SÁNH",
    comp_section_title: "ZenQ Office So Với Các Giải Pháp Khác",
    comp_section_desc: "Xem sự khác biệt rõ rệt khi một bộ phần mềm văn phòng được tối ưu hóa chuẩn mực cho người dùng Windows.",
    comp_th_feature: "Tiêu chí so sánh",
    comp_th_zenq: "ZenQ Office 4.0",
    comp_th_mso: "Microsoft 365",
    comp_th_wps: "WPS Office",
    comp_row1_title: "Thời gian khởi động",
    comp_row1_zenq: "~0.05 giây (Tức thì)",
    comp_row1_mso: "4 - 8 giây",
    comp_row1_wps: "3 - 6 giây",
    comp_row2_title: "RAM tiêu thụ trung bình",
    comp_row2_zenq: "50 - 80 MB",
    comp_row2_mso: "450 - 900 MB",
    comp_row2_wps: "300 - 600 MB",
    comp_row3_title: "Dung lượng bộ cài đặt",
    comp_row3_zenq: "~77 MB",
    comp_row3_mso: "3.5 GB - 5 GB",
    comp_row3_wps: "250 - 400 MB",
    comp_row4_title: "Chính sách quảng cáo",
    comp_row4_zenq: "100% Không quảng cáo",
    comp_row4_mso: "Mời mua gói trả phí",
    comp_row4_wps: "Nhiều quảng cáo bật lên",
    comp_row5_title: "Bảo mật ngoại tuyến",
    comp_row5_zenq: "Ngoại tuyến tuyệt đối",
    comp_row5_mso: "Bắt buộc đăng nhập đám mây",
    comp_row5_wps: "Đồng bộ đám mây bên thứ ba",
    comp_row6_title: "Chi phí bản quyền",
    comp_row6_zenq: "Miễn phí vĩnh viễn",
    comp_row6_mso: "1.490.000đ/năm",
    comp_row6_wps: "Gói Pro tính phí định kỳ",

    // About Us Page / Section
    about_section_badge: "VỀ CHÚNG TÔI",
    about_section_title: "Khát Vọng Phần Mềm Văn Phòng Việt Nam Vươn Tầm Quốc Tế",
    about_lead: "Được sáng lập bởi đội ngũ NextGenEduSpace Studio, ZenQ Office ra đời từ một câu hỏi giản đơn nhưng đầy trăn trở: Tại sao người dùng phải chịu đựng những bộ phần mềm văn phòng nặng hàng gigabyte, khởi động chậm chạp và chứa đầy quảng cáo rác?",
    
    about_story_title: "Câu Chuyện Khởi Nguồn",
    about_story_p1: "Trong kỷ nguyên số, tài liệu là tri thức và công cụ làm việc cốt lõi của mỗi học sinh, sinh viên, giáo viên và chuyên gia. Tuy nhiên, các giải pháp hiện tại ngày càng trở nên nặng nề, phụ thuộc vào kết nối mạng liên tục và yêu cầu cấu hình máy tính đắt tiền.",
    about_story_p2: "Chúng tôi bắt đầu dự án ZenQ Office với mục tiêu duy nhất: Tái định nghĩa trải nghiệm văn phòng trên Windows bằng công nghệ .NET 10 hiện đại, đưa tốc độ mở tài liệu về mốc tức thì (dưới 0.1 giây), đồng thời tôn trọng tuyệt đối quyền riêng tư của người sử dụng.",
    
    about_vision_title: "Tầm Nhìn & Sứ Mệnh",
    about_vision_desc: "Trở thành bộ ứng dụng văn phòng bản địa hóa chuẩn mực cao nhất tại Việt Nam, cạnh tranh sòng phẳng về mặt hiệu năng với các tập đoàn công nghệ toàn cầu, và cung cấp giải pháp miễn phí trọn đời cho nền giáo dục nước nhà.",

    about_values_title: "Ba Trụ Cột Giá Trị Cốt Lõi",
    about_val1_title: "1. Tốc Độ Không Thỏa Hiệp",
    about_val1_desc: "Mọi tính năng được lập trình đều phải vượt qua bài kiểm tra hiệu năng nghiêm ngặt. Không một thao tác nào của người dùng bị hoãn lại bởi vòng xoay chờ đợi.",
    about_val2_title: "2. Quyền Riêng Tư Bất Khả Xâm Phạm",
    about_val2_desc: "Dữ liệu của bạn chỉ nằm trên máy của bạn. Chúng tôi không bao giờ cài cắm mã theo dõi hành vi, không bán quảng cáo và không yêu cầu đăng ký tài khoản bắt buộc.",
    about_val3_title: "3. Tinh Tế & Chuẩn Mực",
    about_val3_desc: "Giao diện tuân thủ tiêu chuẩn Fluent Design của Windows 11, màu sắc hài hòa, thao tác trực quan quen thuộc giúp bạn làm việc ngay mà không mất thời gian học lại.",

    about_team_title: "Đội Ngũ Kỹ Sư NextGenEduSpace",
    about_team_desc: "Tập hợp những kỹ sư tâm huyết với kiến trúc hệ điều hành, đồ họa máy tính Direct2D/DirectX và khoa học dữ liệu. Chúng tôi tự hào tạo nên sản phẩm 100% làm chủ công nghệ tại Việt Nam.",

    // FAQ Section
    faq_section_badge: "CÂU HỎI THƯỜNG GẶP",
    faq_section_title: "Giải Đáp Mọi Thắc Mắc Về ZenQ Office",
    faq_section_desc: "Tìm câu trả lời nhanh chóng cho các vấn đề kỹ thuật, bản quyền, cài đặt và tính năng thường gặp.",
    
    faq_filter_all: "Tất cả câu hỏi",
    faq_filter_install: "Cài đặt & Bản quyền",
    faq_filter_compat: "Định dạng & Tương thích",
    faq_filter_security: "Bảo mật & Quyền riêng tư",
    faq_filter_perf: "Kỹ thuật & Hiệu năng",

    faq_q1: "ZenQ Office có thực sự miễn phí không? Có bị thu phí ẩn sau này không?",
    faq_a1: "ZenQ Office là phần mềm hoàn toàn miễn phí, không có bất kỳ khoản phí ẩn nào, không yêu cầu thẻ tín dụng và không giới hạn thời gian dùng thử. Chúng tôi cam kết duy trì phiên bản cốt lõi này miễn phí vĩnh viễn cho học sinh, sinh viên và cộng đồng người dùng.",

    faq_q2: "ZenQ Office có mở được file Word (.docx), Excel (.xlsx), PowerPoint (.pptx) không?",
    faq_a2: "Có, hoàn toàn tương thích 100%. ZenQ Office đọc và ghi chuẩn định dạng OpenXML quốc tế của Microsoft Office (.docx, .doc, .rtf, .txt, .xlsx, .xls, .csv, .pptx, .ppt, .pdf). Bố cục văn bản, bảng biểu, công thức toán và định dạng slide được giữ nguyên vẹn khi bạn gửi tệp cho đối tác sử dụng Microsoft Office.",

    faq_q3: "Làm thế nào để đặt ZenQ PDF hoặc các ứng dụng ZenQ làm app mặc định trên Windows?",
    faq_a3: "Trong ZenQ Office, bạn chỉ cần vào menu Cài đặt -> Ứng dụng mặc định, sau đó gạt công tắc cho phân hệ mong muốn (Writer, Calc, Slides, PDF). Trên Windows 10/11, hệ thống sẽ mở hộp thoại hoặc trang Windows Settings chỉ định tới ZenQ Office để bạn xác nhận chỉ bằng một lần nhấp chuột.",

    faq_q4: "ZenQ Office có an toàn không? Có thu thập dữ liệu cá nhân hay tài liệu không?",
    faq_a4: "An toàn tuyệt đối. ZenQ Office là ứng dụng chạy cục bộ ngoại tuyến 100%. Không có bất kỳ dòng lệnh nào gửi tệp hay nội dung tài liệu của bạn lên máy chủ internet. Bộ cài đặt đã được quét sạch virus và đáp ứng các tiêu chuẩn bảo mật khắt khe nhất của hệ điều hành Windows.",

    faq_q5: "Máy tính cấu hình yếu hoặc máy cũ chạy Windows 10 có dùng mượt không?",
    faq_a5: "Rất mượt mà! Nhờ vào kiến trúc tối ưu trên .NET 10 và Direct2D, ZenQ Office chỉ tiêu tốn từ 50MB đến 80MB RAM khi hoạt động, khởi động chỉ mất ~0.05 giây và vận hành trơn tru ngay cả trên các dòng laptop văn phòng đời cũ có cấu hình RAM 2GB - 4GB.",

    faq_q6: "Chức năng điều khiển bằng điện thoại (ZenQ Remote) hoạt động như thế nào?",
    faq_a6: "Khi bạn mở một bài trình chiếu hoặc tài liệu trong ZenQ Office, bấm vào biểu tượng Kết nối điện thoại trên thanh tiêu đề. Quét mã QR hiển thị trên màn hình bằng điện thoại (cùng mạng Wi-Fi/LAN), giao diện điều khiển sẽ mở ngay trong trình duyệt điện thoại mà không cần cài thêm app. Bạn có thể vuốt chuyển slide, dùng trackpad ảo, bàn phím và các phím chức năng F5/Esc mượt mà.",

    faq_q7: "ZenQ Office có hỗ trợ in ấn và xuất file PDF không?",
    faq_a7: "Có. Cả ba phân hệ Writer, Calc và Slides đều tích hợp sẵn bộ xuất file chuẩn PDF chất lượng cao, đồng thời hỗ trợ hộp thoại in ấn tiêu chuẩn của Windows với đầy đủ tùy chọn khổ giấy A4, A3, in hai mặt và tỷ lệ trang.",

    faq_q8: "Làm sao để cập nhật lên phiên bản ZenQ Office mới nhất?",
    faq_a8: "ZenQ Office tích hợp sẵn cơ chế kiểm tra cập nhật thông minh. Bạn có thể nhấn vào biểu tượng Cập nhật hình Windows 11 ở góc dưới trang chủ hoặc trong Cài đặt. Nếu có bản phát hành mới, ứng dụng sẽ thông báo rõ ràng kèm nút 'Cập nhật ngay' để bạn nâng cấp nhanh chóng chỉ trong vài giây.",

    // Download & Footer
    dl_section_badge: "TẢI VỀ NGAY",
    dl_section_title: "Trải Nghiệm Sự Khác Biệt Cùng ZenQ Office",
    dl_section_desc: "Tải bản cài đặt chính thức v4.0. Cài đặt chỉ trong 5 giây và bắt đầu làm việc hiệu quả ngay hôm nay.",
    dl_btn_main: "Tải Bộ Cài Đặt (ZenQOffice-Setup.exe)",
    dl_btn_portable: "Tải Bản Portable (.zip)",
    dl_meta_ver: "Phiên bản:",
    dl_meta_size: "Dung lượng:",
    dl_meta_os: "Hệ điều hành:",
    dl_meta_req: "Yêu cầu:",
    dl_meta_req_val: "Windows 10 / 11 (64-bit), .NET 10 Runtime (tự động đính kèm)",

    footer_desc: "Bộ ứng dụng văn phòng thế hệ mới cho Windows. Tối ưu hiệu năng, bảo mật ngoại tuyến và miễn phí trọn đời.",
    footer_col_products: "Sản phẩm",
    footer_col_resources: "Tài nguyên",
    footer_col_company: "Tổ chức",
    footer_col_legal: "Pháp lý",
    footer_copy: "© 2026 NextGenEduSpace Studio. Đã đăng ký bản quyền. Được phát triển tự hào tại Việt Nam."
  },

  en: {
    // Navigation
    nav_brand: "ZENQ OFFICE",
    nav_home: "Home",
    nav_features: "Suite Apps",
    nav_architecture: "Architecture & Speed",
    nav_about: "About Us",
    nav_faq: "FAQ",
    nav_download: "Download",
    nav_lang_btn: "Tiếng Việt",

    // Hero Section
    hero_badge: "Release v4.0 • Built on .NET 10 & Direct2D Engine",
    hero_title_1: "Next-Generation Office",
    hero_title_accent: "Suite Built For Windows",
    hero_desc: "Experience instantaneous 0.05-second launch times, fluid 120 FPS Direct2D hardware acceleration, and 100% offline privacy. True pixel-perfect compatibility with Microsoft Office documents without burning system resources.",
    hero_cta_download: "Download for Windows",
    hero_cta_explore: "Explore Features",
    hero_subtext: "Free Forever • Fully Offline Autonomous • Lightweight ~77MB Installer",

    // Quick Stats Bar
    stat_speed_val: "0.05s",
    stat_speed_lbl: "Instant Cold Launch",
    stat_speed_sub: "10x faster than traditional suites",
    stat_fps_val: "120 FPS",
    stat_fps_lbl: "Direct2D Fluidity",
    stat_fps_sub: "GPU hardware-accelerated canvas",
    stat_privacy_val: "100%",
    stat_privacy_lbl: "Offline Privacy",
    stat_privacy_sub: "Zero telemetry & tracking",
    stat_ram_val: "-85%",
    stat_ram_lbl: "Memory Efficiency",
    stat_ram_sub: "Lightweight ~60MB RAM footprint",

    // Apps Showcase Section
    apps_section_badge: "UNIFIED ECOSYSTEM",
    apps_section_title: "Four Core Applications. One Groundbreaking Platform.",
    apps_section_desc: "ZenQ Office empowers you to launch each application independently from Desktop shortcuts or work unified within our multi-document tab hub. Zero clutter, zero lag.",
    
    app_tab_writer: "ZenQ Writer",
    app_tab_calc: "ZenQ Calc",
    app_tab_slides: "ZenQ Slides",
    app_tab_pdf: "ZenQ PDF",
    app_tab_mobile: "ZenQ Remote",

    // Writer Details
    writer_title: "ZenQ Writer — Master Typography & Print-Ready Formatting",
    writer_desc: "Seamlessly process .docx, .doc, .rtf, and .txt documents with complete fidelity. Features multi-column pagination, automated tables, live word counting, and an intelligent bilingual text completion engine.",
    writer_feat_1: "100% compliant with standard Microsoft Word OpenXML formats",
    writer_feat_2: "Real-time bilingual predictive text autocomplete engine",
    writer_feat_3: "Rich formula insertion, nested tables, and vector shape objects",
    writer_feat_4: "Automated draft safeguards to prevent work loss during system outages",

    // Calc Details
    calc_title: "ZenQ Calc — Lightning-Fast Million-Row Spreadsheets",
    calc_desc: "Open and calculate heavy .xlsx spreadsheets in fractions of a second. Comprehensive formula support for financial and statistical modeling, paired with dynamic real-time visualization charting.",
    calc_feat_1: "High-throughput read/write for .xlsx, .xls, .csv, and .tsv formats",
    calc_feat_2: "Over 300+ standard formulas (SUM, VLOOKUP, XLOOKUP, IF, INDEX/MATCH)",
    calc_feat_3: "Real-time bar, line, pie, and scatter analytics charting",
    calc_feat_4: "Conditional formatting, pane freezing, and lightning data filtering",

    // Slides Details
    slides_title: "ZenQ Slides — Pro Presentations Built for PowerPoint 365",
    slides_desc: "A presentation powerhouse with buttery-smooth animations, slide transitions, vector graphics, and a dedicated dual-screen Presenter View designed for stage and conference delivery.",
    slides_feat_1: "Full support for .pptx and .ppt presentations preserving styling and layouts",
    slides_feat_2: "Dual-monitor Presenter Mode with presenter notes, timer, and next slide peek",
    slides_feat_3: "GPU-accelerated slide transitions via Direct2D with zero stutter",
    slides_feat_4: "Comprehensive shape palette with smart magnetic alignment guides",

    // PDF Details
    pdf_title: "ZenQ PDF — High-Performance Reader & Private Annotation",
    pdf_desc: "Open multi-thousand-page PDF documents instantaneously. Built-in fast text searching, highlighting, document rotation, high-resolution rendering, and zero-cloud local security.",
    pdf_feat_1: "Powered by a dedicated PDFium rendering engine for razor-sharp typography",
    pdf_feat_2: "Instantaneous text lookup and smooth thumbnail navigation sidebar",
    pdf_feat_3: "Official default PDF handler registration on Windows 10 & Windows 11",
    pdf_feat_4: "100% on-device processing protecting confidential documents",

    // Mobile Remote Details
    mobile_title: "ZenQ Remote — Transform Your Smartphone into a Smart Trackpad & Keyboard",
    mobile_desc: "Connect your mobile device over local Wi-Fi to advance slides wirelessly, input text remotely, execute two-finger scroll gestures, and access essential presentation shortcuts.",
    mobile_feat_1: "Instant QR code pairing without installing mobile app stores software",
    mobile_feat_2: "Fluid virtual trackpad with left/right clicks and dual-finger scrolling",
    mobile_feat_3: "Full functional keyboard with Ctrl, Alt, Shift, and screenshot hotkeys",
    mobile_feat_4: "Intelligent cursor auto-hiding during touch control to eliminate screen clutter",

    // Engineering Architecture
    arch_section_badge: "CORE ARCHITECTURE",
    arch_section_title: "Engineered for Velocity & Sovereign Independence",
    arch_section_desc: "No bloated Electron runtimes. No embedded heavy web browsers. ZenQ Office is purely authored in C# and .NET 10, executing directly against the native Windows API.",

    arch_card1_title: "Direct2D GPU Acceleration",
    arch_card1_desc: "Harnesses your GPU to render every character glyph, vector shape, and slide transition at silky 60Hz - 120Hz monitor refresh rates.",
    arch_card2_title: "Independent App-Branches",
    arch_card2_desc: "Launch directly into ZenQ Writer for Word or ZenQ Calc for Excel straight from Desktop shortcuts without loading heavy hub shells.",
    arch_card3_title: "100% Offline & Private",
    arch_card3_desc: "No mandatory sign-ins, zero cloud telemetry collection, and no intrusive ads. Your documents remain strictly your property.",
    arch_card4_title: "Ultra-Lean ~60MB RAM",
    arch_card4_desc: "Intelligent buffer management immediately reclaims memory upon closing files, keeping your PC cool even on entry-level hardware.",

    // Comparison Table
    comp_section_badge: "FEATURE MATRIX",
    comp_section_title: "ZenQ Office vs. Legacy Productivity Suites",
    comp_section_desc: "See how ZenQ Office stands out when a productivity tool is purely engineered for speed, privacy, and user delight.",
    comp_th_feature: "Evaluation Criteria",
    comp_th_zenq: "ZenQ Office 4.0",
    comp_th_mso: "Microsoft 365",
    comp_th_wps: "WPS Office",
    comp_row1_title: "Cold Startup Time",
    comp_row1_zenq: "~0.05 seconds (Instant)",
    comp_row1_mso: "4 - 8 seconds",
    comp_row1_wps: "3 - 6 seconds",
    comp_row2_title: "Average RAM Footprint",
    comp_row2_zenq: "50 - 80 MB",
    comp_row2_mso: "450 - 900 MB",
    comp_row2_wps: "300 - 600 MB",
    comp_row3_title: "Installer Package Size",
    comp_row3_zenq: "~77 MB",
    comp_row3_mso: "3.5 GB - 5 GB",
    comp_row3_wps: "250 - 400 MB",
    comp_row4_title: "Advertising Policy",
    comp_row4_zenq: "100% Ad-Free Guarantee",
    comp_row4_mso: "Subscription Upsells",
    comp_row4_wps: "Intrusive Pop-ups & Ads",
    comp_row5_title: "Offline Autonomy",
    comp_row5_zenq: "Strictly On-Device",
    comp_row5_mso: "Mandatory Cloud Sync",
    comp_row5_wps: "Third-Party Cloud Storage",
    comp_row6_title: "Licensing Model",
    comp_row6_zenq: "Free Forever",
    comp_row6_mso: "$69.99 / Year Subscription",
    comp_row6_wps: "Freemium with Pro Paywall",

    // About Us Page / Section
    about_section_badge: "ABOUT US",
    about_section_title: "Championing High-Performance Native Software from Vietnam",
    about_lead: "Founded by the engineering team at NextGenEduSpace Studio, ZenQ Office arose from a fundamental frustration: Why should users be forced to tolerate multi-gigabyte office suites that crawl on startup and bombard them with adware?",
    
    about_story_title: "The Genesis Story",
    about_story_p1: "In today's digital landscape, documents embody knowledge and constitute the fundamental productivity bedrock for students, educators, and enterprise leaders. Yet prevailing software has become increasingly bloated, tethered to perpetual cloud dependencies, and demanding of costly hardware upgrades.",
    about_story_p2: "We established the ZenQ Office initiative with a singular mission: Reimagine the desktop office experience on Windows with modern .NET 10, bringing document loading down to an instant sub-0.1s threshold while rigorously honoring user privacy.",
    
    about_vision_title: "Vision & Mission",
    about_vision_desc: "To deliver the highest-caliber native desktop productivity suite from Vietnam, competing directly on execution speed and technical excellence with global software giants, while remaining permanently free for education.",

    about_values_title: "Our Three Fundamental Pillars",
    about_val1_title: "1. Uncompromising Velocity",
    about_val1_desc: "Every algorithmic routine undergoes rigorous profiling. No user interaction is ever blocked by spinning progress circles or synthetic delays.",
    about_val2_title: "2. Absolute Privacy Protection",
    about_val2_desc: "Your files never leave your computer. We do not harvest behavioral telemetry, we never display ads, and we mandate zero forced account registrations.",
    about_val3_title: "3. Precision & Refinement",
    about_val3_desc: "Thoughtfully adhering to Windows 11 Fluent design principles, every control and layout is instinctively intuitive so you can be productive immediately.",

    about_team_title: "The NextGenEduSpace Engineering Lab",
    about_team_desc: "A passionate collective of systems software engineers, Direct2D/DirectX graphics architects, and algorithms researchers devoted to advancing sovereign native software.",

    // FAQ Section
    faq_section_badge: "FREQUENTLY ASKED QUESTIONS",
    faq_section_title: "Clear Answers to Everything About ZenQ Office",
    faq_section_desc: "Quick answers to licensing, format compatibility, installation, security, and technical specifications.",
    
    faq_filter_all: "All Questions",
    faq_filter_install: "Setup & Licensing",
    faq_filter_compat: "Formats & Compatibility",
    faq_filter_security: "Security & Privacy",
    faq_filter_perf: "Engineering & Speed",

    faq_q1: "Is ZenQ Office genuinely free? Are there hidden fees or paywalls?",
    faq_a1: "ZenQ Office is 100% free with zero hidden costs, no credit card requirements, and no trial period limits. We are committed to keeping this core desktop productivity suite perpetually free for students, educators, and the global computing community.",

    faq_q2: "Can ZenQ Office open Microsoft Word (.docx), Excel (.xlsx), and PowerPoint (.pptx) files?",
    faq_a2: "Yes, with 100% native fidelity. ZenQ Office reads and writes ISO OpenXML international standard formats (.docx, .doc, .rtf, .txt, .xlsx, .xls, .csv, .pptx, .ppt, .pdf). Layouts, typographic hierarchies, mathematical formulas, and presentation graphics remain fully preserved when shared with Microsoft Office users.",

    faq_q3: "How do I configure ZenQ PDF or other ZenQ apps as the default handler on Windows?",
    faq_a3: "Simply navigate to Settings -> Default Applications inside ZenQ Office and toggle the switch for your desired program (Writer, Calc, Slides, or PDF). On Windows 10/11, the app will trigger the official Windows Default Apps interface to confirm your association with a single click.",

    faq_q4: "Is ZenQ Office secure? Does it upload my documents or collect telemetry?",
    faq_a4: "It is strictly private and secure. ZenQ Office is an entirely autonomous offline application. Not a single line of code uploads your files or keystrokes to any remote server. The installer is clean, virus-free, and satisfies standard Windows enterprise deployment criteria.",

    faq_q5: "Can older or low-spec computers running Windows 10 run ZenQ Office smoothly?",
    faq_a5: "Flawlessly. Thanks to native .NET 10 compilation and direct GPU rasterization via Direct2D, ZenQ Office consumes merely 50MB to 80MB of RAM, launches in ~0.05 seconds, and operates smoothly even on older laptops with 2GB - 4GB of system memory.",

    faq_q6: "How does the ZenQ Remote smartphone controller feature work?",
    faq_a6: "When presenting or viewing documents in ZenQ Office, click the mobile phone icon in the title bar. Scan the displayed QR code with any smartphone camera on the same local network (LAN/Wi-Fi). The remote controller runs in your mobile browser without installing third-party apps, providing slide triggers, a smooth virtual trackpad, and function keys.",

    faq_q7: "Does ZenQ Office support printing and direct PDF document exporting?",
    faq_a7: "Yes. All three authoring applications (Writer, Calc, and Slides) feature native high-resolution PDF export and integrate with the official Windows print subsystem, supporting standard A4, A3, landscape, duplex, and custom scaling options.",

    faq_q8: "How do I upgrade to the latest release of ZenQ Office?",
    faq_a8: "ZenQ Office includes an automatic update verification utility. You can click the Windows 11 pill button on the home dashboard or check inside Settings. When a new release is available, a prompt will provide a one-click 'Update Now' button to upgrade smoothly within seconds.",

    // Download & Footer
    dl_section_badge: "GET STARTED",
    dl_section_title: "Experience the Velocity of ZenQ Office",
    dl_section_desc: "Download the official v4.0 Windows release. Installs in under 5 seconds and equips you for peak productivity immediately.",
    dl_btn_main: "Download Installer (ZenQOffice-Setup.exe)",
    dl_btn_portable: "Download Portable (.zip)",
    dl_meta_ver: "Version:",
    dl_meta_size: "File Size:",
    dl_meta_os: "Supported OS:",
    dl_meta_req: "Requirements:",
    dl_meta_req_val: "Windows 10 / 11 (64-bit), self-contained .NET 10 engine",

    footer_desc: "Next-generation office suite for Windows. Maximized velocity, absolute offline privacy, and permanently free.",
    footer_col_products: "Products",
    footer_col_resources: "Resources",
    footer_col_company: "Company",
    footer_col_legal: "Legal",
    footer_copy: "© 2026 NextGenEduSpace Studio. All rights reserved. Proudly engineered in Vietnam."
  }
};
