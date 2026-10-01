# Dispatch Record

## 2026-09-18T13:54:00Z

CRITICAL CONSTRAINTS & TEAM STRUCTURE:
- The user has explicitly specified: Sử dụng một đội tập trung gồm đúng 2 AI (small focused team of 2 agents) để giữ tốc độ triển khai cao và tiết kiệm quota tối đa.
- You must manage a focused team of exactly 2 AI worker/specialist subagents to solve 3 core problems:
  1. AI 1: Độc lập hóa và chuẩn hóa hoàn toàn hệ thống Undo/Redo (Ctrl+Z / Ctrl+Y) theo từng Tab độc lập, lưu giữ và khôi phục toàn vẹn trạng thái tài liệu (văn bản, run format, con trỏ/vùng chọn, bảng biểu, hình vẽ, thụt lề).
  2. AI 2: Đại tu thư viện và công cụ vẽ Hình khối (Shapes) đạt chuẩn MS Office / OnlyOffice theo đúng ảnh mẫu (9 nhóm danh mục chuẩn, icon vector sắc nét, gõ chữ trong shape, tay nắm xoay/chỉnh sửa), đồng thời khắc phục triệt để lỗi nhập/xuất SmartArt DOCX.

Detailed Requirements:
### AI 1: Độc lập hóa và chuẩn hóa hoàn toàn Undo / Redo (Ctrl+Z / Ctrl+Y) theo từng Tab
1. Cô lập History Stack theo Tab:
   - Mỗi DocTab sở hữu riêng UndoStack và RedoStack. Khi chuyển đổi qua lại giữa các tab trong AppForm và RibbonBar (SaveTabState / LoadTabState), stack lịch sử phải được lưu và nạp đúng tab tương ứng.
   - Tuyệt đối không bao giờ để Ctrl+Z hoặc Ctrl+Y tác động chéo sang tab khác, hay trộn lẫn văn bản giữa 2 tài liệu DOCX đang mở.
2. Khôi phục toàn vẹn trạng thái tài liệu (Deep Snapshot Fidelity):
   - Mở rộng WriterUndoSnapshot để lưu và phục hồi đầy đủ:
     - Văn bản (PieceTable), các đoạn định dạng (FormattedRuns).
     - Vị trí con trỏ (CursorPosition) và vùng bôi đen (SelectionStart, SelectionEnd).
     - Cấu trúc và dữ liệu bảng biểu (DocumentTables, vị trí TablePageX/Y, chiều cao dòng, độ rộng cột, viền, màu nền ô).
     - Toàn bộ danh sách hình khối và bản vẽ (DocumentShapes, vị trí, kích thước, góc xoay, màu sắc, chữ bên trong).
     - Định dạng đoạn văn (ParagraphAlignments, ParagraphLineSpacings, thụt đầu dòng FirstLineIndent, LeftIndent, RightIndent, khoảng cách trước/sau đoạn).
   - Đảm bảo khi bấm Ctrl+Z, tài liệu quay về đúng 100% trạng thái thực tế ngay trước hành động, không bị giật con trỏ về đầu trang hay méo mó cấu trúc.

### AI 2: Đại tu Hình khối (Shapes) chuẩn MS Office/OnlyOffice & Khắc phục SmartArt DOCX
1. Đại tu Thư viện Shape Picker Dropdown (ShapesGalleryDropDownForm.cs & Ribbon):
   - Loại bỏ các danh mục tự chế không chuẩn (Botanical, Luxury Jewels).
   - Xây dựng đầy đủ 9 nhóm chuẩn theo đặc tả:
     - Đã dùng gần đây (Recently Used Shapes)
     - Đường thẳng (Lines): Line, Arrow, Double Arrow...
     - Hình chữ nhật (Rectangles): Rectangle, Rounded Rectangle, Single Corner Snipped, Snipped and Rounded, v.v.
     - Hình dạng cơ bản (Basic Shapes): Text Box [A], Oval, Triangle, Right Triangle, Parallelogram, Trapezoid, Diamond, Pentagon, Hexagon, Octagon, Decagon, Cross, Cylinder, Cube, Donut, Heart, Lightning, Arc, Moon, Sun, Cloud, v.v.
     - Mũi tên khối (Block Arrows): Right, Left, Up, Down, Left-Right, Up-Down, Quad Arrow, Bent, U-Turn, v.v.
     - Toán học / Phương trình (Equation Shapes): Cộng, Trừ, Nhân, Chia, Bằng, Khác.
     - Lưu trình (Flowchart): Process, Decision, Data, Terminator, Document, Preparation, v.v.
     - Sao và Biểu ngữ (Stars and Banners): Star 4/5/6/8/10/12/16/24/32, Explosion, Banners, Scrolls.
     - Chú thích (Callouts): Rectangular Callout, Rounded Callout, Oval Callout, Cloud Callout, Line Callouts.
2. Chất lượng hiển thị & Biên tập Shape trong Writer (ShapeRenderer.cs, WriterView.cs):
   - Vẽ vector monoline thanh thoát, sắc nét, đúng tỷ lệ toán học, căn giữa hoàn hảo.
   - Hỗ trợ gõ và hiển thị văn bản trực tiếp trong hình khối (TextContent, font chữ, màu chữ, căn lề giữa/trên/dưới, tự động xuống dòng WordWrap).
   - Hỗ trợ đầy đủ tay nắm xoay, tay nắm thay đổi kích thước và tay nắm vàng điều chỉnh góc bo/độ dày (adjustment handles).
3. Khắc phục Toàn diện SmartArt DOCX Import/Export (FormatHandlers.cs, SmartArtRenderer.cs):
   - Nhập (Import) tệp DOCX chứa SmartArt: đọc và nhận diện đúng các sơ đồ SmartArt chuẩn (vòng tròn, quy trình tuyến tính, phân cấp cây/org chart, danh sách khối), vẽ trực tiếp lên trang văn bản trong Writer không bị crash hay biến mất.
   - Xuất (Export) tệp DOCX: ghi đúng cấu trúc OpenXML cho SmartArt (hoặc fallback drawingml/vml vector hợp lệ), đảm bảo khi mở lại bằng Microsoft Word tệp hoàn toàn nguyên vẹn, không báo lỗi hỏng (corrupt file/recovery error).

GUARDRAILS & ACCEPTANCE:
- Clean build with csc.exe (0 errors). Run build.bat or csc compilation tests to verify.
- Preserve core Writer features (8 tables, in-cell editing, text wrap, even/odd page margins).
- Maintain continuous progress updates in c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_orchestrator_3\progress.md.
- Send handoff report when all requirements are fully verified and ready for Victory Audit.
