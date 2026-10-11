# Original User Request

## 2026-09-17T06:35:27Z

Chia dự án GenOffice cho nhóm 3 AI làm 3 nhiệm vụ song song:
1. AI 1: Hoàn thiện hệ thống phím tắt và điều hướng chuẩn MS Word (Ctrl+L/E/R/J, Ctrl+B/I/U, Ctrl+Z/Y, Home/End, PageUp/Down) cùng sửa các lỗi hành vi văn bản.
2. AI 2: Khắc phục triệt để khe hở 1 pixel giữa thanh tab tài liệu và thanh Ribbon.
3. AI 3: Thiết kế launcher tạo tab mới (+) với hiệu ứng vòng cung (radial arc) bay mượt mà 60+ FPS từ dấu +, nền tối mờ (scrim), đồng thời tuân thủ tuyệt đối nguyên tắc không can thiệp vào các tính năng Writer đã hoàn thiện (bảng biểu, ảnh, inline wrap).

Working directory: `c:\Users\Danh\Desktop\genofficepy`
Integrity mode: development

## Requirements

### R1. MS Word Shortcuts & Document Navigation Fidelity (AI 1)
- Bổ sung và đồng bộ toàn diện hệ thống phím tắt theo chuẩn Microsoft Word:
  - Căn lề đoạn văn: `Ctrl+L` (trái), `Ctrl+E` (giữa), `Ctrl+R` (phải), `Ctrl+J` (đều hai bên).
  - Định dạng ký tự: `Ctrl+B` (đậm), `Ctrl+I` (nghiêng), `Ctrl+U` (gạch chân).
  - Lịch sử thao tác: `Ctrl+Z` (Undo), `Ctrl+Y` (Redo).
  - Điều hướng con trỏ nhanh: `Home` (đầu dòng hiện tại), `End` (cuối dòng hiện tại), `Ctrl+Home` (đầu tài liệu), `Ctrl+End` (cuối tài liệu), `PageUp` (cuộn lùi 1 trang màn hình), `PageDown` (cuộn tới 1 trang màn hình).
- Đảm bảo khi nhấn phím tắt, con trỏ nhấp nháy, vùng chọn (selection) và giao diện thanh Ribbon cập nhật trạng thái tức thì.
- Sửa các lỗi văn bản tồn đọng, không để xung đột phím tắt với các tính năng khác của hệ thống.

### R2. Pixel-Perfect Ribbon Seam & Tab Alignment (AI 2)
- Khắc phục triệt để vết hở 1 pixel giữa mép dưới thanh Tab tài liệu (`DocTabs` / thanh tiêu đề) và mép trên khung thanh công cụ Ribbon (`RibbonBar`):
  - Tab đang được chọn (Active Tab) phải nối liền mạch 100% với bề mặt Ribbon, không xuất hiện đường viền rỗng hoặc đường kẻ ngắt quãng màu xám/trắng ở giữa.
  - Chuẩn hóa tọa độ tính toán vẽ `ActiveTab.Bottom` trùng khớp với `Ribbon.Top` ở mọi tỷ lệ DPI màn hình (100%, 125%, 150%) và ở cả hai trạng thái cửa sổ: Bình thường (Normal) và Toàn màn hình (Maximized).

### R3. Smooth 60+ FPS Radial Arc '+' Launcher Animation (AI 3)
- Khi người dùng nhấn chuột vào nút "+" tạo tab mới:
  - Lớp nền tối mờ (scrim / darkened backdrop) xuất hiện với hiệu ứng chuyển màu (alpha fade-in).
  - 4 biểu tượng chức năng đại diện cho 4 ứng dụng (Writer - Văn bản, Calc - Bảng tính, Slides - Trình chiếu, PDF - Tài liệu PDF) bay tỏa ra theo hình vòng cung (radial arc) bắt nguồn từ chính tọa độ tâm của nút "+".
  - Các icon được thiết kế theo phong cách kính mờ hiện đại (frosted glass / modern UI) kèm nhãn tên rõ ràng.
  - Khi click vào 1 biểu tượng: tạo tab tương ứng và đóng launcher mượt mà; bấm ra ngoài vùng chọn hoặc phím `Esc` sẽ đóng launcher.
  - Hiệu ứng chuyển động phải đạt chuẩn độ mượt **60+ FPS**, sử dụng thuật toán nội suy làm mượt (ease-out cubic / smooth spring) và double-buffering để loại bỏ hoàn toàn hiện tượng xé hình hay giật lag.

### R4. Strict Guardrails: Preservation of Core Writer Features (AI 1, 2, 3)
- **Tuyệt đối không can thiệp hoặc thay đổi các tính năng đã hoạt động ổn định của Writer**:
  - Hệ thống 8 bảng biểu (`DocTableObject`, `DocumentTables`), tính năng gõ chỉnh sửa trực tiếp trong ô bảng (in-cell editing), chiều cao dòng, độ rộng cột, viền và màu nền ô.
  - Tính năng chèn ảnh và hình khối vector (`DocShapeObject`).
  - Cơ chế bọc văn bản quanh ảnh/bảng (`WrapMode`: Inline, Square, Wrap text).

## Acceptance Criteria

### MS Word Fidelity (AI 1)
- [ ] Phím tắt `Ctrl+L/E/R/J`, `Ctrl+B/I/U`, `Ctrl+Z/Y` hoạt động chính xác theo chuẩn Word trên WriterView.
- [ ] Phím `Home`, `End`, `Ctrl+Home`, `Ctrl+End`, `PageUp`, `PageDown` đưa con trỏ đến đúng vị trí văn bản ngay lập tức.
- [ ] `GenOffice.exe` biên dịch thành công 0 lỗi.

### Pixel-Perfect Ribbon (AI 2)
- [ ] Không còn bất kỳ khe hở 1 pixel nào giữa Tab Bar và Ribbon Bar ở cả chế độ Normal và Maximized.
- [ ] Tab đang chọn có nền liên tục và liền khối với thân Ribbon.

### 60+ FPS '+' Launcher (AI 3)
- [ ] Nút "+" kích hoạt hiệu ứng scrim tối nền và 4 icon bay tỏa ra theo hình vòng cung mượt mà.
- [ ] Tốc độ khung hình render đạt ổn định >= 60 FPS, không gây lag app.
- [ ] Bấm chọn icon mở đúng loại tab; bấm ESC hoặc click ngoài đóng launcher an toàn.

### Non-Regression Guardrail
- [ ] File mẫu `Bài 1.ND GHI.docx` vẫn mở đủ 8 bảng biểu, in-cell editing hoạt động bình thường, ảnh và chế độ wrap không bị ảnh hưởng.

## 2026-09-18T13:52:11Z

Sử dụng một đội tập trung gồm đúng 2 AI (small focused team of 2 agents) để giữ tốc độ triển khai cao và tiết kiệm quota tối đa.

Phân công dự án GenOffice cho nhóm 2 AI giải quyết dứt điểm 3 vấn đề cốt lõi:
1. AI 1: Độc lập hóa và chuẩn hóa hoàn toàn hệ thống Undo/Redo (Ctrl+Z / Ctrl+Y) theo từng Tab độc lập, lưu giữ và khôi phục toàn vẹn trạng thái tài liệu (văn bản, run format, con trỏ/vùng chọn, bảng biểu, hình vẽ, thụt lề).
2. AI 2: Đại tu thư viện và công cụ vẽ Hình khối (Shapes) đạt chuẩn MS Office / OnlyOffice theo đúng ảnh mẫu (9 nhóm danh mục chuẩn, icon vector sắc nét, gõ chữ trong shape, tay nắm xoay/chỉnh sửa), đồng thời khắc phục triệt để lỗi nhập/xuất SmartArt DOCX.

Working directory: `c:\Users\Danh\Desktop\genofficepy`
Integrity mode: development

## Requirements

### AI 1: Độc lập hóa và chuẩn hóa hoàn toàn Undo / Redo (Ctrl+Z / Ctrl+Y) theo từng Tab
1. Cô lập History Stack theo Tab:
   - Mỗi `DocTab` sở hữu riêng `UndoStack` và `RedoStack`. Khi chuyển đổi qua lại giữa các tab trong `AppForm` và `RibbonBar` (`SaveTabState` / `LoadTabState`), stack lịch sử phải được lưu và nạp đúng tab tương ứng.
   - Tuyệt đối không bao giờ để `Ctrl+Z` hoặc `Ctrl+Y` tác động chéo sang tab khác, hay trộn lẫn văn bản giữa 2 tài liệu DOCX đang mở.
2. Khôi phục toàn vẹn trạng thái tài liệu (Deep Snapshot Fidelity):
   - Mở rộng `WriterUndoSnapshot` để lưu và phục hồi đầy đủ:
     - Văn bản (`PieceTable`), các đoạn định dạng (`FormattedRuns`).
     - Vị trí con trỏ (`CursorPosition`) và vùng bôi đen (`SelectionStart`, `SelectionEnd`).
     - Cấu trúc và dữ liệu bảng biểu (`DocumentTables`, vị trí `TablePageX/Y`, chiều cao dòng, độ rộng cột, viền, màu nền ô).
     - Toàn bộ danh sách hình khối và bản vẽ (`DocumentShapes`, vị trí, kích thước, góc xoay, màu sắc, chữ bên trong).
     - Định dạng đoạn văn (`ParagraphAlignments`, `ParagraphLineSpacings`, thụt đầu dòng `FirstLineIndent`, `LeftIndent`, `RightIndent`, khoảng cách trước/sau đoạn).
   - Đảm bảo khi bấm `Ctrl+Z`, tài liệu quay về đúng 100% trạng thái thực tế ngay trước hành động, không bị giật con trỏ về đầu trang hay méo mó cấu trúc.

### AI 2: Đại tu Hình khối (Shapes) chuẩn MS Office/OnlyOffice & Khắc phục SmartArt DOCX
1. Đại tu Thư viện Shape Picker Dropdown (`ShapesGalleryDropDownForm.cs` & Ribbon):
   - Loại bỏ các danh mục tự chế không chuẩn (Botanical, Luxury Jewels).
   - Xây dựng đầy đủ các nhóm chuẩn theo ảnh chụp thực tế (`media_1789738040258.png`, `media_1789738130694.png`):
     - Đã dùng gần đây (Recently Used Shapes).
     - Đường thẳng (Lines): Line, Arrow, Double Arrow...
     - Hình chữ nhật (Rectangles): Rectangle, Rounded Rectangle, Single Corner Snipped, Snipped and Rounded, v.v.
     - Hình dạng cơ bản (Basic Shapes): Text Box [A], Oval, Triangle, Right Triangle, Parallelogram, Trapezoid, Diamond, Pentagon, Hexagon, Octagon, Decagon, Cross, Cylinder, Cube, Donut, Heart, Lightning, Arc, Moon, Sun, Cloud, v.v.
     - Mũi tên khối (Block Arrows): Right, Left, Up, Down, Left-Right, Up-Down, Quad Arrow, Bent, U-Turn, v.v.
     - Toán học / Phương trình (Equation Shapes): Cộng, Trừ, Nhân, Chia, Bằng, Khác.
     - Lưu trình (Flowchart): Process, Decision, Data, Terminator, Document, Preparation, v.v.
     - Sao và Biểu ngữ (Stars and Banners): Star 4/5/6/8/10/12/16/24/32, Explosion, Banners, Scrolls.
     - Chú thích (Callouts): Rectangular Callout, Rounded Callout, Oval Callout, Cloud Callout, Line Callouts.
2. Chất lượng hiển thị & Biên tập Shape trong Writer (`ShapeRenderer.cs`, `WriterView.cs`):
   - Vẽ vector monoline thanh thoát, sắc nét, đúng tỷ lệ toán học, căn giữa hoàn hảo.
   - Hỗ trợ gõ và hiển thị văn bản trực tiếp trong hình khối (`TextContent`, font chữ, màu chữ, căn lề giữa/trên/dưới, tự động xuống dòng WordWrap).
   - Hỗ trợ đầy đủ tay nắm xoay, tay nắm thay đổi kích thước và tay nắm vàng điều chỉnh góc bo/độ dày (adjustment handles).
3. Khắc phục Toàn diện SmartArt DOCX Import/Export (`FormatHandlers.cs`, `SmartArtRenderer.cs`):
   - Nhập (Import) tệp DOCX chứa SmartArt: đọc và nhận diện đúng các sơ đồ SmartArt chuẩn (vòng tròn, quy trình tuyến tính, phân cấp cây/org chart, danh sách khối), vẽ trực tiếp lên trang văn bản trong Writer không bị crash hay biến mất.
   - Xuất (Export) tệp DOCX: ghi đúng cấu trúc OpenXML cho SmartArt (hoặc fallback drawingml/vml vector hợp lệ), đảm bảo khi mở lại bằng Microsoft Word tệp hoàn toàn nguyên vẹn, không báo lỗi hỏng (corrupt file/recovery error).

## Acceptance Criteria

### Undo / Redo (AI 1)
- [ ] Mở đồng thời 2 tệp DOCX ở 2 tab khác nhau: gõ văn bản và chèn đối tượng ở cả 2 tab; bấm `Ctrl+Z` hoặc `Ctrl+Y` ở tab A chỉ hoàn tác ở tab A, tuyệt đối không nhảy qua tab B và không bị trộn văn bản tab B sang tab A.
- [ ] Bấm `Ctrl+Z` hoàn tác chính xác trạng thái ngay trước đó cả về văn bản, con trỏ, bảng biểu và hình vẽ.

### Shapes & Drawing Fidelity (AI 2)
- [ ] Dropdown chọn Shape hiển thị chuẩn xác 9 nhóm danh mục theo mẫu MS Office / OnlyOffice với icon vector sắc nét.
- [ ] Chèn shape vào Writer hoạt động trơn tru: vẽ đúng hình dáng, đổi màu nền/viền đúng, cho phép click vào gõ văn bản bên trong shape chuẩn xác.
- [ ] Không còn các danh mục tự chế lệch chuẩn MS Office.

### SmartArt DOCX (AI 2)
- [ ] Mở tệp DOCX có SmartArt: hiển thị đầy đủ các khối nội dung và văn bản trong trang Writer.
- [ ] Lưu tệp DOCX và mở lại trên Microsoft Word: tài liệu mở thành công 100%, không có cảnh báo tệp lỗi.

### Non-Regression Guardrails
- [ ] Dự án biên dịch sạch với `csc.exe` (0 lỗi).
- [ ] Các tính năng cốt lõi của Writer (8 bảng biểu, in-cell editing, text wrap, căn lề trang chẵn/lẻ) tiếp tục hoạt động hoàn hảo.

## 2026-09-21T12:58:21Z

Xây dựng và hoàn thiện hệ thống bóc tách, chuẩn hóa OpenXML từ Microsoft Word và đồng bộ hóa engine kết xuất đồ họa Canvas trong GenOffice. Đảm bảo hiển thị chuẩn xác 100% kích thước, tỷ lệ vòng tròn (tròn hoàn hảo 1:1, không bầu dục), đường viền, 200+ kiểu SmartArt, Shapes, màu sắc, font chữ và công thức toán học OMML.

Working directory: c:\Users\Danh\Desktop\genofficepy
Integrity mode: development

## Requirements

### R1. Bóc tách & Chuẩn hóa OpenXML Schema (DrawingML & WordprocessingML)
- Phân tích cấu trúc thư mục C:\Users\Danh\Desktop\xml và đặc tả ECMA-376 (DrawingML dgm:, w:drawing, w:tblBorders, m:oMath).
- Trích xuất định lượng chuẩn xác các thông số kích thước (EMU to points: 1 pt = 12700 EMUs), căn lề, viền khối, màu sắc theme và bố cục các node trong SmartArt.

### R2. Xây dựng công cụ tạo tài liệu Word mẫu toàn diện (200+ SmartArt, Shapes, Borders, Math)
- Tạo tài liệu/mô-đun mẫu đầy đủ các định dạng hiện đại nhất của Word:
  - 200+ sơ đồ SmartArt (Cycle, Hierarchy, Process, Matrix, Pyramid, Relationship, List, Picture).
  - Đầy đủ hệ thống Shape, Borders, Drop Shadow, Gradient, 3D effect.
  - Công thức toán học (OMML / MathML), ký hiệu đặc biệt, font chữ và bảng biểu chuẩn.
- Đảm bảo tài liệu có thể bóc tách XML trực tiếp để làm bộ test đối sánh chuẩn (ground truth).

### R3. Nâng cấp Engine Parser & Canvas Renderer trong GenOffice
- Cập nhật OpenXmlSmartArtModel.cs và FormatHandlers.cs để đọc và ánh xạ đúng kích thước gốc, tọa độ, thuộc tính viền từ file XML Word.
- Cập nhật SmartArtRenderer.cs, ShapeRenderer.cs và WriterView.cs:
  - Giữ tỷ lệ vòng tròn tròn 1:1 chuẩn xác tuyệt đối (không bị méo thành bầu dục).
  - Tự động gắn kết các ô hai bên vào vòng tròn tròn chuẩn.
  - Đầy đủ các loại viền (solid, dash, double, 3D) và màu sắc theo chuẩn Word.

## Acceptance Criteria

### Đồ họa SmartArt & Vòng tròn
- [ ] Vòng tròn trong sơ đồ Cycle được vẽ tròn hoàn hảo (tỷ lệ 1:1), các ô tiếp xúc chuẩn xác không có khe hở.
- [ ] Kích thước SmartArt nhập từ Word XML hiển thị đúng tỷ lệ và kích thước ban đầu, không bị co kéo sai lệch.

### OpenXML Parsing & Compatibility
- [ ] Engine đọc và giải mã thành công các phần tử DrawingML, SmartArt, Shape, Border, Math từ bộ XML mẫu.
- [ ] GenOffice biên dịch sạch sẽ (build.bat thành công) và hiển thị tương đương với tài liệu Word gốc.

## 2026-09-21T14:25:50Z

LỆNH KHẨN CẤP TỪ NGƯỜI DÙNG:
Yêu cầu tăng tốc tối đa toàn bộ đội ngũ, PHẢI HOÀN TẤT VÀ BÀN GIAO TOÀN BỘ KẾT QUẢ TRONG TỐI ĐA 5 PHÚT NỮA!
Không chạy tuần tự kéo dài qua Word COM nữa. Ngay lập tức:
1. Đóng gói bộ mẫu đại diện cốt lõi (đầy đủ các nhóm Cycle, Hierarchy, Process, Matrix, Pyramid, List, Rel, Shapes, Borders, Math OMML).
2. Kích hoạt Milestone 4 kiểm thử E2E ngay lập tức, xác thực tỷ lệ vòng tròn 1:1, viền và OpenXML.
3. Hoàn tất nghiệm thu, biên dịch sạch sẽ GenOffice.exe và phát lệnh bàn giao kết quả cuối cùng (Victory Audit) ngay!

## 2026-09-28T10:39:51Z

Execute a comprehensive 4-phase upgrade for ZenQ Calc to resolve data-loss bugs, expand file format compatibility (PDF, ODS, XLS binary, HTML), add 50+ essential formulas, and introduce modern Excel 365 features (Tables, Dynamic Arrays, PivotTables) while maintaining code quality and zero data regressions.

Working directory: c:\Users\Danh\Desktop\genofficepy
Integrity mode: development

## Roadmap & Phased Requirements

### Phase 1: Critical Data Integrity & Core Productivity Tools
1. **Fix OpenXML Drawings Roundtrip**:
   - Update `XlsxHandler.cs` `LoadXlsx` to parse `xl/drawings/drawing1.xml`, `xl/drawings/_rels/`, and `xl/media/`.
   - Restore all floating shapes, embedded images, and charts into `CalcView.FloatingObjects` so saving and reopening `.xlsx` retains 100% of visual assets.
2. **Native PDF Export for Calc**:
   - Implement spreadsheet PDF export in `FormatHandlers.cs` and `CalcView.cs` honoring page orientation, margins, paper size, gridlines, fonts, and cell styling.
3. **Core Data Utilities**:
   - **Find & Replace Dialog**: Dedicated floating dialog supporting Match Case, Match Entire Cell, Search By Rows/Cols, and Look In Formulas/Values.
   - **Remove Duplicates**: Column-selection modal identifying and removing duplicate rows across selected ranges.
   - **Text to Columns Wizard**: Multi-step or direct dialog supporting Delimited (Tab, Semicolon, Comma, Space, Custom) and Fixed-width parsing.

### Phase 2: Formula Engine Expansion (50+ Functions) & File Format Diversity
1. **Formula Engine Expansion (50+ high-demand functions)**:
   - *Lookup & Reference*: `INDEX`, `MATCH`, `XLOOKUP`, `XMATCH`, `ROW`, `COLUMN`, `ROWS`, `COLUMNS`, `INDIRECT`, `OFFSET`.
   - *Logical & Error*: `IFS`, `SWITCH`, `IFERROR`, `IFNA`, `XOR`.
   - *Date & Time*: `YEAR`, `MONTH`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `EDATE`, `EOMONTH`, `NETWORKDAYS`, `DATEDIF`.
   - *Text*: `TEXT`, `VALUE`, `FIND`, `SEARCH`, `REPLACE`, `REPT`, `TEXTSPLIT`, `TEXTBEFORE`, `TEXTAFTER`, `CHAR`, `CODE`, `CLEAN`.
   - *Math & Statistical*: `RANDBETWEEN`, `RAND`, `CEILING`, `FLOOR`, `SIGN`, `STDEV`, `VAR`, `RANK`, `MODE`.
   - *Information*: `ISNUMBER`, `ISTEXT`, `ISBLANK`, `ISERROR`, `ISNA`.
2. **Additional File Format Support**:
   - **OpenDocument Spreadsheet (.ods)**: Bidirectional reader/writer converting sheets, styles, values, formulas.
   - **Excel 97-2003 (.xls)**: Genuine BIFF8 binary workbook parser for legacy files.
   - **Web & Delimited Formats**: HTML table export (`.html`), TSV (`.tsv`), and space-delimited text (`.prn`).

### Phase 3: Structured Tables (ListObject) & Dynamic Arrays
1. **Excel Tables (ListObject)**:
   - Create Table from range (`Ctrl+T`), styled with zebra striping and header sorting/filtering.
   - Structured formula references (e.g. `Table1[@Sales] * 1.1`).
   - Dynamic auto-expansion upon typing in adjacent rows/columns.
   - Optional Total Row with per-column aggregations (SUM, AVERAGE, COUNT, MIN, MAX).
2. **Dynamic Arrays & Spill Engine**:
   - Calculation engine supporting array-valued formulas spilling into adjacent empty cells with thin blue bounding borders.
   - Implement `SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`.
   - Basic support for `LET` and `LAMBDA` scoping.

### Phase 4: PivotTables & Advanced Analytics
1. **PivotTable & PivotChart**:
   - PivotTable creation dialog with field list pane (Filters, Columns, Rows, Values).
   - Multi-dimensional aggregation engine (Sum, Count, Average, Min, Max, % of Column/Row).
   - Date and numeric grouping.
   - Linked PivotCharts reflecting dynamic pivot aggregations.
2. **What-If Analysis**:
   - **Goal Seek**: Iterative single-variable solver to reach a target cell value.
   - **Data Tables**: One-variable and two-variable sensitivity tables.
3. **Advanced Printing & Protection**:
   - Workbook structure protection with password prompt.
   - Print Titles (repeat header rows/columns on every page) and manual Page Break management.

## Acceptance Criteria

### Phase 1 Verification
- [ ] Saving an .xlsx file with 1 chart, 1 shape, and 1 picture, then reloading it through `LoadXlsx` reproduces all 3 objects at their exact cell anchor coordinates.
- [ ] Exporting a multi-sheet table to PDF produces a valid, readable PDF file matching print preview.
- [ ] Find & Replace successfully finds occurrences and replaces all matching cell values without damaging formulas.
- [ ] Remove Duplicates removes redundant rows and updates sheet row spans correctly.
- [ ] Text to Columns correctly splits comma-separated and tab-separated cell text across adjacent columns.

### Phase 2 Verification
- [ ] An automated test suite executing all 50+ new formulas passes 100% of test assertions matching Excel output.
- [ ] A sample `.ods` file opens, loads all cell contents and formatting, and saves back out cleanly.
- [ ] A legacy `.xls` (BIFF8) file opens without ZIP/XML parsing exceptions.
- [ ] Export to HTML produces clean table markup rendering accurately in standard web browsers.

### Phase 3 Verification
- [ ] Creating an Excel Table formats the range, shows filter drop-downs, and automatically extends when typing below the last row.
- [ ] Structured references evaluate correctly in FormulaEngine.
- [ ] `UNIQUE` and `SORT` formulas spill downward and rightward into empty cells, and display `#SPILL!` if blocked.

### Phase 4 Verification
- [ ] A PivotTable summarizes a 5,000-row sales dataset, permitting drag-and-drop rearrangement of row and column dimensions.
- [ ] Goal Seek converges to within 0.001 of target value for non-linear formula goals.
- [ ] The solution builds cleanly (`build.bat`) with 0 compilation errors.





## 2026-10-09T11:44:42Z

Xây dựng và tích hợp hoàn chỉnh công cụ Chỉnh sửa điểm neo (Node Tool - công cụ thứ 2 trên thanh công cụ) và Thanh điều khiển thuộc tính công cụ (Tool Controls Bar) chuẩn Inkscape vào ZenQOffice, hỗ trợ biên tập đường cong Bezier, điều khiển tiếp tuyến và thao tác trực quan trên Canvas.

Working directory: c:\Users\Danh\Desktop\genofficepy
Integrity mode: development

## Requirements

### R1. Thanh điều khiển thuộc tính công cụ Chỉnh sửa điểm neo (Node Tool Controls Bar)
Triển khai thanh điều khiển thuộc tính phía trên khi chọn công cụ thứ 2 (Chỉnh sửa điểm neo), hiển thị đầy đủ các nút chức năng đồ họa vector chuẩn mực như Inkscape:
- Thêm điểm neo mới vào đoạn cong hoặc đoạn thẳng đã chọn (+).
- Xóa các điểm neo đang được chọn (-).
- Nối các điểm neo đã chọn lại với nhau thành một điểm duy nhất.
- Tách đường tại điểm neo đã chọn (chia đường cong thành 2 đoạn rời).
- Nối hai điểm mút tự do bằng một đoạn nối mới.
- Xóa đoạn đường giữa hai điểm neo được chọn.
- Chuyển đổi trạng thái điểm neo:
  * Điểm neo góc nhọn (Corner/Cusp node)
  * Điểm neo uốn mượt (Smooth node)
  * Điểm neo đối xứng (Symmetric node)
  * Điểm neo tự động mượt (Auto-smooth node)
- Chuyển đổi tính chất đoạn vẽ:
  * Biến đoạn cong thành đoạn thẳng
  * Biến đoạn thẳng thành đoạn cong
- Chuyển đổi đối tượng:
  * Chuyển hình khối hình học thành đường cong Bezier (Object to Path)
  * Chuyển nét viền thành đường cong (Stroke to Path)
- Hộp thông số tọa độ chính xác X và Y với nút tăng giảm +/- và cho phép nhập giá trị trực tiếp, đi kèm đơn vị đo (px/mm).

### R2. Cấu trúc dữ liệu đường cong Bezier và điểm neo
- Mở rộng cấu trúc điểm của lớp hình vector để lưu trữ trạng thái chi tiết của từng điểm neo: vị trí tọa độ, kiểu điểm neo (góc nhọn, uốn mượt, đối xứng, tự động), và cặp điểm điều khiển tay nắm tiếp tuyến (In-Handle / Out-Handle).
- Thuật toán nội suy và dựng hình đường cong Bezier bậc 3 (Cubic Bezier) chính xác trên Direct2D và GDI+ path.

### R3. Thao tác tương tác trực quan trên Canvas
- Khi đang chọn công cụ thứ 2 và chọn một hình khối hoặc đường vẽ, canvas tự động hiển thị tất cả các điểm neo dưới dạng hình vuông nhỏ có viền rõ nét.
- Hỗ trợ chọn điểm neo: click chuột để chọn một điểm neo, nhấn Shift để chọn nhiều điểm neo, hoặc quét chuột kéo khung vùng chọn để chọn hàng loạt điểm neo.
- Cho phép kéo rê trực tiếp điểm neo trên màn hình để nắn chỉnh hình dạng đường vẽ trong thời gian thực.
- Khi chọn một điểm neo cong hoặc mượt, hiển thị các đường tay đòn và núm tiếp tuyến hình tròn; cho phép kéo tay nắm tiếp tuyến để thay đổi góc uốn và độ cong.
- Tối ưu hóa hiệu năng render qua Direct2D để đảm bảo khung hình 60fps mượt mà không giật lag khi kéo thả.

## Acceptance Criteria

### Giao diện và thanh điều khiển
- [ ] Thanh thuộc tính trên cùng hiển thị đầy đủ toàn bộ bộ nút chức năng tương ứng hình ảnh Inkscape người dùng đã cung cấp.
- [ ] Các icon được vẽ rõ nét bằng glyph Direct2D hoặc vector hình học chuẩn Fluent UI.
- [ ] Giao diện sử dụng 100% tiếng Việt đồ họa chuẩn mực, tuyệt đối không dùng tiếng Anh trong dấu ngoặc đơn.

### Tính năng biên tập điểm neo
- [ ] Nhấn vào công cụ thứ 2 trên thanh công cụ bên trái và click vào đối tượng vector trên canvas sẽ hiển thị các điểm neo.
- [ ] Thao tác kéo rê điểm neo làm biến dạng đường vẽ ngay lập tức trên canvas.
- [ ] Nút thêm neo, xóa neo, đổi góc nhọn, đổi uốn mượt thực thi chính xác và cập nhật lại hình dạng đối tượng.
- [ ] Hỗ trợ chuyển hình chữ nhật/hình sao/hình tròn thành đường cong Bezier để chỉnh sửa tự do từng điểm neo.
- [ ] Phím tắt Undo (Ctrl+Z) và Redo (Ctrl+Y) khôi phục chính xác từng thao tác chỉnh sửa điểm neo.

### Kiểm thử và đóng gói
- [ ] Dự án biên dịch thành công 0 lỗi với lệnh `dotnet build ZenQOffice.csproj`.
- [ ] Tệp thực thi `ZenQOffice.exe` và các thư viện liên quan được đồng bộ ra thư mục gốc để người dùng kiểm tra ngay lập tức.
