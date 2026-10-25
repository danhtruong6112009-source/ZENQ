# Original User Request

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


## 2026-10-21T14:28:44Z

This is a single self-contained fix; keep it small and focused.

Working directory: c:\Users\Danh\Desktop\genofficepy
Integrity mode: development

## Project Overview
Fix document pagination and formatting fidelity in ZenQ Office to match Microsoft Word for the test document C:\Users\Danh\Downloads\thuvienhoclieu.com-Bai-tap-chuyen-sau-Anh-12-Global-Unit-2-A-Multicultural-World.docx. Eliminate table row jumping across zoom levels, refactor Header/Footer to support multi-color rich text runs (e.g. blue "thuvienhoclieu" and red ".com", and "Trang 1" with proper colors and page number replacement), and ensure accurate table alignment.

## Verification Resources
- Test document: C:\Users\Danh\Downloads\thuvienhoclieu.com-Bai-tap-chuyen-sau-Anh-12-Global-Unit-2-A-Multicultural-World.docx
- Build script: build_setup.bat (outputs ZenQOffice-Setup.exe and ZenQOffice.exe)

## Requirements

### R1. Zoom-Invariant Table Pagination
- The document pagination (page breaks and table row breaks) must be completely identical across all zoom levels (40% to 400%), exactly matching Microsoft Word.
- In the vocabulary table of the test document, rows 0..41 (header row + vocab items 1 to 41) must strictly reside on Page 1 at all zoom levels.
- Rows 42 ("42.confusion") and 43 ("43.anxiety") must strictly break to Page 2 at all zoom levels. Zooming in or out must NEVER cause rows to jump between pages.

### R2. Rich Multi-Run Header & Footer Model & Rendering
- Refactor Header and Footer from a flat single-color string to a rich multi-run structure (e.g., List<DocCellParagraph> or List<DocTextRun>) that preserves individual run properties: text, font family, font size, bold, italic, and color.
- In FormatHandlers.cs: Parse all runs inside <w:hdr> and <w:ftr> preserving their individual <w:color> and styling.
- Header Rendering: Render multiple runs with their individual colors (e.g., "thuvienhoclieu" in #00B0F0 blue and ".com" in #FF0000 red), correctly aligned (e.g. centered).
- Footer Rendering: Support multiple colored runs and dynamic page number fields. Preserve the full text "Trang {PAGE}" (where "Trang" is red and the page number is blue) rather than extracting only a bare number.
- Dynamic replacement of {PAGE} and {NUMPAGES} must update per page without destroying the surrounding run formatting.

### R3. Table Margin & Right-Edge Alignment
- Table cell margins (w:tblCellMar: top, bottom, left, right) and table body width must align with document margins without overflowing or clipping.

## Acceptance Criteria

### Pagination & Zoom Stability
- [ ] At 40%, 100%, 150%, and 200% zoom, Page 1 ends at row 41; rows 42 and 43 are on Page 2.
- [ ] Zooming in or out never causes table rows to jump between pages.

### Header/Footer Fidelity
- [ ] Header displays "thuvienhoclieu" in blue (#00B0F0) and ".com" in red (#FF0000) centered at top of page.
- [ ] Footer displays "thuvienhoclieu.com" on the left and "Trang 1" (with "Trang" in red and "1" in blue) on the right.
- [ ] Page numbers dynamically increment across pages while preserving prefix text and colors.

### Build & Compilation
- [ ] Project compiles with 0 errors via dotnet build ZenQOffice.csproj.
- [ ] build_setup.bat succeeds and generates ZenQOffice-Setup.exe.
