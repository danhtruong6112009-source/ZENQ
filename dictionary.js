/**
 * ZENQ OFFICE - OFFICIAL BILINGUAL DICTIONARY (VI / EN)
 * Biên soạn thủ công chuẩn xác ngữ nghĩa công nghệ & hành văn doanh nghiệp quốc tế.
 * Tuyệt đối không dùng máy dịch thô sơ.
 */

const DICTIONARY = {
  vi: {
    // Navigation
    nav_brand: "ZENQ OFFICE",
    nav_home: "Trang chủ",
    nav_features: "Ứng dụng",
    nav_architecture: "Đặc điểm nổi bật",
    nav_about: "Về chúng tôi",
    nav_faq: "Hỏi & Đáp",
    nav_download: "Tải ứng dụng",
    nav_lang_btn: "English",

    // Hero Section
    hero_badge: "Phiên bản chính thức v4.0 • Bộ Ứng Dụng Văn Phòng Thế Hệ Mới",
    hero_title_1: "Bộ Ứng Dụng Văn Phòng",
    hero_title_accent: "Hiện Đại Cho Windows",
    hero_desc: "Trải nghiệm soạn thảo văn bản, bảng tính, bài trình chiếu và tài liệu PDF mượt mà. Tương thích hoàn hảo chuẩn tài liệu Microsoft Office và bảo mật ngoại tuyến 100%.",
    hero_cta_download: "Tải ZenQ Office Cho Windows",
    hero_cta_explore: "Khám Phá Tính Năng",
    hero_subtext: "Miễn phí trọn đời • Hoạt động ngoại tuyến không cần Internet • Tối ưu cho Windows 10 & 11",

    // Quick Highlights Bar
    stat_apps_val: "4 Ứng dụng",
    stat_apps_lbl: "Bộ công cụ toàn diện",
    stat_apps_sub: "Writer, Calc, Slides và PDF",

    stat_compat_val: "100%",
    stat_compat_lbl: "Tương thích chuẩn Office",
    stat_compat_sub: "Định dạng .docx, .xlsx, .pptx, .pdf",

    stat_privacy_val: "100%",
    stat_privacy_lbl: "Bảo mật ngoại tuyến",
    stat_privacy_sub: "Dữ liệu lưu an toàn trên máy bạn",

    stat_free_val: "Miễn phí",
    stat_free_lbl: "Không quảng cáo phiền toái",
    stat_free_sub: "Trải nghiệm làm việc thuần khiết",

    // Mockup Sidebar
    mockup_prop_format_lbl: "Định dạng tệp",
    mockup_prop_format_val: "Chuẩn Microsoft Office",
    mockup_prop_save_lbl: "Trạng thái lưu",
    mockup_prop_save_val: "Đã lưu tự động an toàn",
    mockup_prop_view_lbl: "Chế độ xem",
    mockup_prop_view_val: "Khổ giấy in ấn A4 chuẩn",

    // Apps Showcase Section (Only 4 Apps: Writer, Calc, Slides, PDF)
    apps_section_badge: "BỘ ỨNG DỤNG VĂN PHÒNG",
    apps_section_title: "Bốn Phân Hệ Cốt Lõi Cho Mọi Nhu Cầu Công Việc",
    apps_section_desc: "ZenQ Office cho phép mở từng ứng dụng độc lập từ lối tắt Desktop hoặc sử dụng chung trong giao diện Tab thông minh. Thao tác tiện lợi, không cửa sổ thừa.",
    
    app_tab_writer: "ZenQ Writer",
    app_tab_calc: "ZenQ Calc",
    app_tab_slides: "ZenQ Slides",
    app_tab_pdf: "ZenQ PDF",

    // Writer Details
    writer_title: "ZenQ Writer — Soạn thảo văn bản chuẩn in ấn đỉnh cao",
    writer_desc: "Xử lý định dạng .docx, .doc, .rtf, .txt với độ trung thực tuyệt đối. Hỗ trợ dàn trang hai mặt, bảng biểu tự động điều chỉnh, bộ đếm từ trực tiếp, và tính năng dự đoán từ thông minh giúp đẩy nhanh tốc độ gõ phím.",
    writer_feat_1: "Tương thích 100% chuẩn OpenXML Microsoft Word",
    writer_feat_2: "Dự đoán từ thông minh song ngữ Việt - Anh thời gian thực",
    writer_feat_3: "Hỗ trợ chèn công thức toán học, bảng biểu phức tạp và hình vẽ vector",
    writer_feat_4: "Tự động lưu bản thảo an toàn, phòng chống mất dữ liệu khi mất điện",

    // Calc Details
    calc_title: "ZenQ Calc — Bảng tính điện tử thông minh & mạnh mẽ",
    calc_desc: "Xử lý các tập dữ liệu bảng tính .xlsx nhanh chóng và chính xác. Hệ thống công thức tài chính, logic, tìm kiếm chuẩn mực kết hợp cùng bộ vẽ biểu đồ phân tích trực quan hóa dữ liệu hiện đại.",
    calc_feat_1: "Đọc và ghi tệp .xlsx, .xls, .csv, .tsv chuẩn xác",
    calc_feat_2: "Hơn 300+ hàm tính toán chuẩn Excel (SUM, VLOOKUP, XLOOKUP, IF, INDEX/MATCH)",
    calc_feat_3: "Vẽ biểu đồ cột, tròn, đường nét và phân tán thời gian thực",
    calc_feat_4: "Định dạng có điều kiện, cố định dòng cột và lọc dữ liệu thông minh",

    // Slides Details
    slides_title: "ZenQ Slides — Thiết kế bài thuyết trình chuyên nghiệp",
    slides_desc: "Bộ công cụ thiết kế bài trình chiếu ấn tượng với chuyển động chuyển trang tinh tế, hỗ trợ vector shape, bảng biểu biểu diễn, chế độ Người thuyết trình 2 màn hình và hỗ trợ điều khiển từ xa bằng điện thoại.",
    slides_feat_1: "Mở và biên tập tệp .pptx, .ppt giữ nguyên bố cục và phông chữ",
    slides_feat_2: "Chế độ Người thuyết trình chuẩn mực (Ghi chú, đồng hồ đếm, xem trước slide kế tiếp)",
    slides_feat_3: "Hiển thị chuyển động hoạt họa slide mượt mà, sắc nét từng chi tiết",
    slides_feat_4: "Bộ sưu tập hình khối chuẩn và công cụ căn chỉnh nam châm thông minh",

    // PDF Details
    pdf_title: "ZenQ PDF — Đọc & Biên tập tài liệu bảo mật tuyệt đối",
    pdf_desc: "Mở các tệp tài liệu PDF dung lượng lớn nhanh chóng. Tích hợp công cụ tìm kiếm từ khóa, đánh dấu văn bản, xoay trang, xuất ảnh chất lượng cao và bảo vệ quyền riêng tư không gửi tệp lên máy chủ đám mây.",
    pdf_feat_1: "Động cơ kết xuất PDF chuyên dụng cho độ nét văn bản tối đa",
    pdf_feat_2: "Tìm kiếm từ khóa tức thì và xem trước thumbnail trang tiện lợi",
    pdf_feat_3: "Đăng ký ứng dụng đọc PDF mặc định Windows 10 & Windows 11 dễ dàng",
    pdf_feat_4: "Bảo mật nội bộ 100%, không lo rò rỉ tài liệu nhạy cảm",

    // Core Highlights Section (Thay thế bảng so sánh, ghi thẳng tên ZenQ Office)
    highlights_section_badge: "TÍNH NĂNG VƯỢT TRỘI",
    highlights_section_title: "Những Giá Trị Cốt Lõi Của ZenQ Office",
    highlights_section_desc: "Được xây dựng với tiêu chuẩn cao nhất nhằm mang lại sự tiện nghi, an toàn và hiệu quả công việc tối đa cho bạn.",

    feature_card1_title: "Tương Thích Tuyệt Đối Chuẩn Microsoft Office",
    feature_card1_desc: "Đọc, chỉnh sửa và lưu các định dạng tệp .docx, .xlsx, .pptx, .pdf với bố cục, bảng biểu và phông chữ chuẩn xác. Dễ dàng trao đổi tài liệu với đồng nghiệp mà không lo lệch trang hay lỗi định dạng.",

    feature_card2_title: "An Toàn Tuyệt Đối & 100% Ngoại Tuyến",
    feature_card2_desc: "ZenQ Office hoạt động hoàn toàn cục bộ trên máy tính của bạn. Không bắt buộc đăng nhập tài khoản, không gửi tệp lên máy chủ bên thứ ba và không thu thập bất kỳ dữ liệu cá nhân nào.",

    feature_card3_title: "Lối Tắt Ứng Dụng Nhánh & Giao Diện Tab",
    feature_card3_desc: "Bạn có thể mở trực tiếp tài liệu Word vào ZenQ Writer hoặc bảng tính vào ZenQ Calc từ lối tắt ngoài Desktop mà không bị vướng bận, hoặc gộp chung tất cả trong một cửa sổ Tab làm việc thông minh.",

    feature_card4_title: "Hoàn Toàn Miễn Phí & Không Quảng Cáo",
    feature_card4_desc: "Cam kết phục vụ trọn đời cho học sinh, sinh viên, giáo viên và cộng đồng người dùng. Trải nghiệm làm việc thuần túy, sạch sẽ, không có bất kỳ quảng cáo chen ngang hay chi phí ẩn nào.",

    // About Us Page / Section
    about_section_badge: "VỀ CHÚNG TÔI",
    about_section_title: "Khát Vọng Phần Mềm Văn Phòng Việt Nam Vươn Tầm Quốc Tế",
    about_lead: "Được sáng lập bởi đội ngũ NextGenEduSpace Studio, ZenQ Office ra đời từ một câu hỏi giản đơn nhưng đầy trăn trở: Tại sao người dùng phải chịu đựng những bộ phần mềm văn phòng cồng kềnh, ép buộc đăng ký tài khoản đám mây và chứa đầy quảng cáo phiền toái?",
    
    about_story_title: "Câu Chuyện Khởi Nguồn",
    about_story_p1: "Trong kỷ nguyên số, tài liệu là tri thức và công cụ làm việc cốt lõi của mỗi học sinh, sinh viên, giáo viên và chuyên gia. Tuy nhiên, các giải pháp hiện tại ngày càng trở nên nặng nề, phụ thuộc vào kết nối mạng liên tục và yêu cầu cấu hình máy tính đắt tiền.",
    about_story_p2: "Chúng tôi bắt đầu dự án ZenQ Office với mục tiêu duy nhất: Tái định nghĩa trải nghiệm văn phòng trên Windows bằng công nghệ hiện đại, mang lại sự mượt mà tối đa, đồng thời tôn trọng tuyệt đối quyền riêng tư và quyền tự chủ tài liệu của người sử dụng.",
    
    about_vision_title: "Tầm Nhìn & Sứ Mệnh",
    about_vision_desc: "Trở thành bộ ứng dụng văn phòng bản địa hóa chuẩn mực cao nhất tại Việt Nam, mang đến giải pháp phần mềm độc lập, an toàn và hoàn toàn miễn phí trọn đời cho nền giáo dục nước nhà.",

    about_values_title: "Ba Trụ Cột Giá Trị Cốt Lõi",
    about_val1_title: "1. Tiện Lợi & Mượt Mà",
    about_val1_desc: "Mọi thao tác soạn thảo, tính toán và trình chiếu đều được tối ưu hóa để diễn ra liền mạch, phản hồi tức thì và không làm gián đoạn dòng suy nghĩ của bạn.",
    about_val2_title: "2. Quyền Riêng Tư Bất Khả Xâm Phạm",
    about_val2_desc: "Dữ liệu của bạn chỉ nằm trên máy của bạn. Chúng tôi không bao giờ cài cắm mã theo dõi hành vi, không bán quảng cáo và không yêu cầu đăng ký tài khoản bắt buộc.",
    about_val3_title: "3. Tinh Tế & Chuẩn Mực",
    about_val3_desc: "Giao diện tuân thủ tiêu chuẩn thiết kế hiện đại, màu sắc hài hòa, thao tác trực quan quen thuộc giúp bạn làm việc ngay mà không mất thời gian học lại.",

    // FAQ Section
    faq_section_badge: "CÂU HỎI THƯỜNG GẶP",
    faq_section_title: "Giải Đáp Mọi Thắc Mắc Về ZenQ Office",
    faq_section_desc: "Tìm câu trả lời nhanh chóng cho các vấn đề kỹ thuật, bản quyền, cài đặt và tính năng thường gặp.",
    
    faq_filter_all: "Tất cả câu hỏi",
    faq_filter_install: "Cài đặt & Bản quyền",
    faq_filter_compat: "Định dạng & Tương thích",
    faq_filter_security: "Bảo mật & Quyền riêng tư",
    faq_filter_perf: "Trải nghiệm & Tính năng",

    faq_q1: "ZenQ Office có thực sự miễn phí không? Có bị thu phí ẩn sau này không?",
    faq_a1: "ZenQ Office là phần mềm hoàn toàn miễn phí, không có bất kỳ khoản phí ẩn nào, không yêu cầu thẻ tín dụng và không giới hạn thời gian dùng thử. Chúng tôi cam kết duy trì phiên bản cốt lõi này miễn phí vĩnh viễn cho học sinh, sinh viên và cộng đồng người dùng.",

    faq_q2: "ZenQ Office có mở được file Word (.docx), Excel (.xlsx), PowerPoint (.pptx) không?",
    faq_a2: "Có, hoàn toàn tương thích 100%. ZenQ Office đọc và ghi chuẩn định dạng OpenXML quốc tế của Microsoft Office (.docx, .doc, .rtf, .txt, .xlsx, .xls, .csv, .pptx, .ppt, .pdf). Bố cục văn bản, bảng biểu, công thức toán và định dạng slide được giữ nguyên vẹn khi bạn gửi tệp cho đối tác sử dụng Microsoft Office.",

    faq_q3: "Làm thế nào để đặt ZenQ PDF hoặc các ứng dụng ZenQ làm app mặc định trên Windows?",
    faq_a3: "Trong ZenQ Office, bạn chỉ cần vào menu Cài đặt -> Ứng dụng mặc định, sau đó gạt công tắc cho phân hệ mong muốn (Writer, Calc, Slides, PDF). Trên Windows 10/11, hệ thống sẽ mở hộp thoại hoặc trang Windows Settings chỉ định tới ZenQ Office để bạn xác nhận chỉ bằng một lần nhấp chuột.",

    faq_q4: "ZenQ Office có an toàn không? Có thu thập dữ liệu cá nhân hay tài liệu không?",
    faq_a4: "An toàn tuyệt đối. ZenQ Office là ứng dụng chạy cục bộ ngoại tuyến 100%. Không có bất kỳ dòng lệnh nào gửi tệp hay nội dung tài liệu của bạn lên máy chủ internet. Bộ cài đặt đã được quét sạch virus và đáp ứng các tiêu chuẩn bảo mật khắt khe nhất của hệ điều hành Windows.",

    faq_q5: "Máy tính cấu hình bình thường hoặc máy tính cũ dùng có mượt không?",
    faq_a5: "Rất mượt mà! ZenQ Office được tối ưu hóa tinh gọn, vận hành trơn tru và nhẹ nhàng ngay cả trên các dòng máy tính, laptop văn phòng có cấu hình khiêm tốn.",

    faq_q6: "ZenQ Office có hỗ trợ in ấn và xuất file PDF không?",
    faq_a6: "Có. Cả ba phân hệ Writer, Calc và Slides đều tích hợp sẵn bộ xuất file chuẩn PDF chất lượng cao, đồng thời hỗ trợ hộp thoại in ấn tiêu chuẩn của Windows với đầy đủ tùy chọn khổ giấy A4, A3, in hai mặt và tỷ lệ trang.",

    faq_q7: "Làm sao để cập nhật lên phiên bản ZenQ Office mới nhất?",
    faq_a7: "ZenQ Office tích hợp sẵn cơ chế kiểm tra cập nhật thông minh. Bạn có thể nhấn vào nút Cập nhật ở góc dưới màn hình chính hoặc trong Cài đặt để nâng cấp phiên bản mới nhất một cách nhanh chóng.",

    // Download Section
    dl_section_badge: "TẢI VỀ NGAY",
    dl_section_title: "Trải Nghiệm ZenQ Office Cho Windows",
    dl_section_desc: "Tải bản cài đặt chính thức v4.0. Cài đặt nhanh chóng và bắt đầu làm việc hiệu quả ngay hôm nay.",
    dl_btn_main: "Tải Bộ Cài Đặt (ZenQOffice-Setup.exe)",
    dl_btn_portable: "Tải Bản Portable (.zip)",
    
    dl_meta_ver: "Phiên bản:",
    dl_meta_os: "Hệ điều hành:",
    dl_meta_os_val: "Windows 10 / 11 (64-bit)",
    dl_meta_formats: "Định dạng:",
    dl_meta_formats_val: "DOCX, XLSX, PPTX, PDF",
    dl_meta_license: "Bản quyền:",
    dl_meta_license_val: "Miễn phí trọn đời",

    // Footer
    footer_desc: "Bộ ứng dụng văn phòng thế hệ mới cho Windows. Tối ưu trải nghiệm, bảo mật ngoại tuyến và miễn phí trọn đời.",
    footer_col_products: "Sản phẩm",
    footer_col_resources: "Tài nguyên",
    footer_col_company: "Tổ chức",
    footer_copy: "© 2026 NextGenEduSpace Studio. Đã đăng ký bản quyền. Được phát triển tự hào tại Việt Nam."
  },

  en: {
    // Navigation
    nav_brand: "ZENQ OFFICE",
    nav_home: "Home",
    nav_features: "Applications",
    nav_architecture: "Highlights",
    nav_about: "About Us",
    nav_faq: "FAQ",
    nav_download: "Download",
    nav_lang_btn: "Tiếng Việt",

    // Hero Section
    hero_badge: "Official Release v4.0 • Next-Gen Office Suite",
    hero_title_1: "Modern Office Suite",
    hero_title_accent: "Crafted for Windows",
    hero_desc: "Enjoy fluid document authoring, powerful spreadsheets, captivating presentations, and secure PDF handling. 100% offline and seamlessly compatible with Microsoft Office standard formats.",
    hero_cta_download: "Download ZenQ Office for Windows",
    hero_cta_explore: "Explore Applications",
    hero_subtext: "Lifetime Free • 100% Offline with Zero Cloud Requirement • Optimized for Windows 10 & 11",

    // Quick Highlights Bar
    stat_apps_val: "4 Core Apps",
    stat_apps_lbl: "Complete Suite",
    stat_apps_sub: "Writer, Calc, Slides & PDF",

    stat_compat_val: "100%",
    stat_compat_lbl: "Office Standard Ready",
    stat_compat_sub: "Full DOCX, XLSX, PPTX & PDF support",

    stat_privacy_val: "100%",
    stat_privacy_lbl: "Offline & Private",
    stat_privacy_sub: "Your data stays securely on your PC",

    stat_free_val: "Free",
    stat_free_lbl: "Zero Distracting Ads",
    stat_free_sub: "Pure, focused workspace experience",

    // Mockup Sidebar
    mockup_prop_format_lbl: "File Format",
    mockup_prop_format_val: "Microsoft Office Standard",
    mockup_prop_save_lbl: "Save Status",
    mockup_prop_save_val: "Safely Auto-Saved",
    mockup_prop_view_lbl: "Page View",
    mockup_prop_view_val: "Standard A4 Layout",

    // Apps Showcase Section (Only 4 Apps: Writer, Calc, Slides, PDF)
    apps_section_badge: "OFFICE APPLICATIONS",
    apps_section_title: "Four Essential Apps for Every Workflow",
    apps_section_desc: "Launch each application independently from Desktop shortcuts or work seamlessly in a unified tabbed interface. Convenient, uncluttered, and efficient.",
    
    app_tab_writer: "ZenQ Writer",
    app_tab_calc: "ZenQ Calc",
    app_tab_slides: "ZenQ Slides",
    app_tab_pdf: "ZenQ PDF",

    // Writer Details
    writer_title: "ZenQ Writer — Professional Document Publishing",
    writer_desc: "Process .docx, .doc, .rtf, and .txt files with fidelity. Features two-page layout, auto-fitting tables, real-time word counting, and bilingual predictive text to accelerate your typing workflow.",
    writer_feat_1: "100% compatible with Microsoft Word OpenXML standard",
    writer_feat_2: "Real-time bilingual Vietnamese - English smart word prediction",
    writer_feat_3: "Insert mathematical formulas, complex tables, and vector graphics",
    writer_feat_4: "Secure auto-save drafts to prevent data loss during unexpected power loss",

    // Calc Details
    calc_title: "ZenQ Calc — Smart & Powerful Spreadsheets",
    calc_desc: "Open and analyze .xlsx datasets swiftly and accurately. Standard financial, logical, and lookup formulas paired with contemporary chart visualization tools.",
    calc_feat_1: "Accurately read and write .xlsx, .xls, .csv, and .tsv formats",
    calc_feat_2: "Over 300+ standard Excel functions (SUM, VLOOKUP, XLOOKUP, IF, INDEX/MATCH)",
    calc_feat_3: "Render bar, pie, line, and scatter charts in real time",
    calc_feat_4: "Conditional formatting, freeze panes, and high-speed data filtering",

    // Slides Details
    slides_title: "ZenQ Slides — Dynamic & Professional Presentations",
    slides_desc: "Design impactful presentation decks with refined transition animations, vector shapes, table layouts, dual-monitor Presenter View, and smartphone remote control integration.",
    slides_feat_1: "Open and edit .pptx and .ppt files preserving layout and typography",
    slides_feat_2: "Dual-monitor Presenter View with speaker notes, timer, and next-slide preview",
    slides_feat_3: "Fluid, high-fidelity slide transitions and vector graphic rendering",
    slides_feat_4: "Standard shape library with smart magnetic alignment guides",

    // PDF Details
    pdf_title: "ZenQ PDF — Secure Document Reader & Editor",
    pdf_desc: "Open large PDF documents with ease. Built-in keyword search, text highlighting, page rotation, high-resolution image export, and privacy protection with zero cloud uploads.",
    pdf_feat_1: "Dedicated PDF rendering engine for crisp text and line art",
    pdf_feat_2: "Instant keyword search and smooth thumbnail page previews",
    pdf_feat_3: "Convenient default PDF reader registration on Windows 10 & 11",
    pdf_feat_4: "100% Local privacy with zero data transmission to external servers",

    // Core Highlights Section (Direct ZenQ Office capabilities)
    highlights_section_badge: "CORE ADVANTAGES",
    highlights_section_title: "The Pillars of ZenQ Office",
    highlights_section_desc: "Engineered to deliver convenience, airtight security, and seamless daily productivity.",

    feature_card1_title: "Full Microsoft Office Standard Compatibility",
    feature_card1_desc: "Open, edit, and save .docx, .xlsx, .pptx, and .pdf documents with accurate layouts, tables, and fonts. Collaborate with colleagues effortlessly without formatting discrepancies.",

    feature_card2_title: "100% Offline Security & Total Data Privacy",
    feature_card2_desc: "ZenQ Office runs purely on your local machine. No mandatory accounts, no file telemetry to external servers, and zero collection of personal information.",

    feature_card3_title: "Independent App Shortcuts or Tabbed Window",
    feature_card3_desc: "Open Word files directly into ZenQ Writer or spreadsheets into ZenQ Calc from desktop shortcuts without unnecessary overhead, or combine them in a clean tabbed window.",

    feature_card4_title: "Lifetime Free & 100% Ad-Free",
    feature_card4_desc: "Committed to students, teachers, schools, and professionals forever. A pristine, distraction-free environment with zero popups and no hidden subscription costs.",

    // About Us Page / Section
    about_section_badge: "ABOUT US",
    about_section_title: "Building Modern Office Software for the World",
    about_lead: "Founded by NextGenEduSpace Studio, ZenQ Office was born from a fundamental question: Why should users tolerate bloated office software, forced cloud sign-ins, and intrusive advertisements?",
    
    about_story_title: "The Origin Story",
    about_story_p1: "In the digital era, documents represent knowledge and everyday productivity for students, educators, and professionals. Yet existing suites have grown increasingly complex, tied to constant online connections, and requiring expensive hardware.",
    about_story_p2: "We established ZenQ Office with a singular focus: Redefine the Windows office experience with modern engineering, smooth workflows, and uncompromised respect for user privacy and document sovereignty.",
    
    about_vision_title: "Vision & Mission",
    about_vision_desc: "Deliver high-standard localized office software in Vietnam that provides an independent, secure, and permanently free productivity foundation for education and communities.",

    about_values_title: "Three Core Values",
    about_val1_title: "1. Convenience & Fluidity",
    about_val1_desc: "Writing, calculating, and presenting are optimized to feel natural, responsive, and uninterrupted.",
    about_val2_title: "2. Inviolable Privacy",
    about_val2_desc: "Your data belongs exclusively to you on your machine. We never inject telemetry trackers, display ads, or force account registrations.",
    about_val3_title: "3. Elegance & Standards",
    about_val3_desc: "A clean modern interface adhering to Windows standards with harmonious aesthetics and familiar workflows.",

    // FAQ Section
    faq_section_badge: "FAQ",
    faq_section_title: "Frequently Asked Questions",
    faq_section_desc: "Quick answers to common questions about licensing, compatibility, and features.",
    
    faq_filter_all: "All Questions",
    faq_filter_install: "Installation & License",
    faq_filter_compat: "Formats & Compatibility",
    faq_filter_security: "Security & Privacy",
    faq_filter_perf: "Experience & Features",

    faq_q1: "Is ZenQ Office truly free? Are there any hidden fees?",
    faq_a1: "ZenQ Office is 100% free with no hidden fees, no credit card requirements, and no trial expiration. We are committed to keeping this core edition free forever for students, educators, and the community.",

    faq_q2: "Can ZenQ Office open Word (.docx), Excel (.xlsx), and PowerPoint (.pptx) files?",
    faq_a2: "Yes, fully compatible. ZenQ Office reads and writes Microsoft Office OpenXML standards (.docx, .doc, .rtf, .txt, .xlsx, .xls, .csv, .pptx, .ppt, .pdf), preserving formatting, tables, formulas, and slide layouts.",

    faq_q3: "How do I set ZenQ PDF or other ZenQ apps as the default on Windows?",
    faq_a3: "In ZenQ Office, navigate to Settings -> Default Apps, then toggle your preferred app (Writer, Calc, Slides, PDF). Windows Settings will guide you to confirm with a single click.",

    faq_q4: "Is ZenQ Office secure? Does it collect any personal data or documents?",
    faq_a4: "Completely secure. ZenQ Office runs 100% locally offline. No code transmits your documents or personal information to the internet. The installer is clean and adheres to Windows security guidelines.",

    faq_q5: "Does it run smoothly on standard or older PCs?",
    faq_a5: "Yes, exceptionally smoothly. ZenQ Office is streamlined to operate quietly and responsively on standard office laptops and everyday PCs.",

    faq_q6: "Does ZenQ Office support printing and exporting to PDF?",
    faq_a6: "Yes. Writer, Calc, and Slides all include high-quality PDF export and full Windows print dialog support for A4/A3 page sizes, duplex printing, and custom scaling.",

    faq_q7: "How do I update to the latest ZenQ Office release?",
    faq_a7: "ZenQ Office includes an update checking mechanism. Click the Update button on the home screen or in Settings to quickly upgrade to the newest release.",

    // Download Section
    dl_section_badge: "DOWNLOAD NOW",
    dl_section_title: "Experience ZenQ Office for Windows",
    dl_section_desc: "Download official release v4.0. Quick setup to start working productively today.",
    dl_btn_main: "Download Installer (ZenQOffice-Setup.exe)",
    dl_btn_portable: "Download Portable (.zip)",
    
    dl_meta_ver: "Version:",
    dl_meta_os: "Operating System:",
    dl_meta_os_val: "Windows 10 / 11 (64-bit)",
    dl_meta_formats: "Supported Formats:",
    dl_meta_formats_val: "DOCX, XLSX, PPTX, PDF",
    dl_meta_license: "License:",
    dl_meta_license_val: "Free for Lifetime",

    // Footer
    footer_desc: "Next-generation office suite for Windows. Modern workflow, 100% offline security, and lifetime free.",
    footer_col_products: "Products",
    footer_col_resources: "Resources",
    footer_col_company: "Organization",
    footer_copy: "© 2026 NextGenEduSpace Studio. All rights reserved. Developed proudly in Vietnam."
  }
};
