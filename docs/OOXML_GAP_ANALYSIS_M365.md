# BÁO CÁO PHÂN TÍCH CHUYÊN SÂU LỖ HỔNG VÀ MỨC ĐỘ TUÂN THỦ CHUẨN OOXML
## (Comprehensive OOXML Gap Analysis: ECMA-376 / ISO/IEC 29500 / Microsoft 365 vs ZenQ Office)

- **Tác giả**: Đội ngũ Kỹ thuật Tích hợp Kiến trúc Định dạng ZenQ Office (Teamwork Specialist & Worker)
- **Hệ thống thẩm định**: ZenQ Office Codebase (`src/format/`, `src/ui/`, `src/core/`)
- **Phạm vi khảo sát**: Cả 3 phân hệ cốt lõi:
  1. **ZenQ Writer** — WordprocessingML (`.docx`)
  2. **ZenQ Calc** — SpreadsheetML (`.xlsx`)
  3. **ZenQ Slides** — PresentationML (`.pptx`)
  4. **Shared DrawingML** — Khung đồ họa vector, hình học và hiệu ứng dùng chung
- **Chuẩn đối chiếu**:
  - ECMA-376 4th Edition (Part 1: Fundamentals, Part 2: OPC, Part 3: MCE, Part 4: Transitional & Strict)
  - ISO/IEC 29500:2016 (Transitional & Strict Schemas)
  - Microsoft Open Specifications: `[MS-DOCX]`, `[MS-XLSX]`, `[MS-PPTX]`, `[MS-ODRAWXML]`
  - Microsoft Office 365 Apps for Enterprise (Build 16.0+, Desktop & Web Engine)
- **Ngày lập**: 25/10/2026
- **Trạng thái**: Hoàn thiện - Bản chính thức (Master Technical Deliverable)

---

## MỤC LỤC TỔNG THỂ

1. [Tóm tắt Điều hành & Tổng quan Hệ thống (Executive Summary & System Overview)](#1-tóm-tắt-điều-hành--tổng-quan-hệ-thống)
2. [Kiến trúc Đóng gói OPC & Nền tảng Định dạng (OPC Packaging Foundation)](#2-kiến-trúc-đóng-gói-opc--nền-tảng-định-dạng)
3. [Phân hệ R1: WordprocessingML (ZenQ Writer - .docx)](#3-phân-hệ-r1-wordprocessingml-zenq-writer---docx)
   - 3.1. Sơ đồ Kiến trúc & Phân bố Mã nguồn
   - 3.2. Đoạn văn, Ký tự & Định dạng Văn bản (`<w:rPr>`, `<w:pPr>`, Tab stops, Highlight)
   - 3.3. Danh sách Đánh số & Đầu dòng Đa cấp (`<w:numbering>`, `<w:num>`, `<w:abstractNum>`)
   - 3.4. Hệ thống Kiểu dáng (`word/styles.xml` & Cây thừa kế Style)
   - 3.5. Bố cục Trang & Phân vùng Phân đoạn (`<w:sectPr>`, `<w:cols>`, Header/Footer, `<w:titlePg>`)
   - 3.6. Bảng biểu Chuyên sâu (`<w:tbl>`, `<w:gridSpan>`, `<w:vMerge>`, `<w:tcBorders>`)
   - 3.7. Các Thành phần Văn bản Nâng cao (Footnotes, SDT, Track Changes, Raster hóa DrawingML)
4. [Phân hệ R2: SpreadsheetML (ZenQ Calc - .xlsx)](#4-phân-hệ-r2-spreadsheetml-zenq-calc---xlsx)
   - 4.1. Sơ đồ Kiến trúc & Phân bố Mã nguồn
   - 4.2. Cấu trúc Sổ làm việc & Quan hệ (`xl/workbook.xml`, `xl/_rels/workbook.xml.rels`)
   - 4.3. Bảng Chuỗi Dùng chung (`xl/sharedStrings.xml` & Đánh giá `inlineStr`)
   - 4.4. Trang tính, Cột, Hàng & Ô Dữ liệu (`<cols>`, `<row>`, `<c>`, kiểu dữ liệu `s`, `n`, `b`, `str`)
   - 4.5. Công thức, Tiền tố M365 `_xlfn.` & Chuỗi Tính toán (`<f>`, shared formulas, `calcChain.xml`)
   - 4.6. Kiểu dáng Hiển thị & Định dạng Số (`xl/styles.xml`, `<numFmts>`, `<fonts>`, `<fills>`, `<borders>`)
   - 4.7. Tính năng Bảng tính Nâng cao (Freeze Panes, Data Validation, AutoFilter, Tables, Charts)
5. [Phân hệ R3: PresentationML (ZenQ Slides - .pptx)](#5-phân-hệ-r3-presentationml-zenq-slides---pptx)
   - 5.1. Sơ đồ Kiến trúc & Phân bố Mã nguồn
   - 5.2. Cấu trúc Bản trình chiếu & Tỷ lệ Màn hình (`ppt/presentation.xml`, sai lệch EMU 16:9)
   - 5.3. Lỗi Hỏng Tệp Ghi chú Diễn giả (`ppt/notesSlides/` & Khuyết thiếu Quan hệ Rels)
   - 5.4. Slide Master, Slide Layout & Kế thừa Giữ chỗ (`<p:ph>`, ép cứng `slideLayout1.xml`)
   - 5.5. Cây Hình dạng Trang chiếu (`<p:spTree>`, nhóm `<p:grpSp>`, bảng, mất trắng biểu đồ)
   - 5.6. Hiệu ứng Chuyển tiếp, Hoạt ảnh & Đa phương tiện (`<p:transition>`, `<p:timing>`, media nhúng)
6. [Phân hệ R4: Shared DrawingML (Universal Vector & Visual FX)](#6-phân-hệ-r4-shared-drawingml)
   - 6.1. Khung Đồ họa Dùng chung & Hệ Tọa độ Biến đổi 2D (`<a:xfrm>`, lỗi bỏ rơi `flipH`/`flipV`)
   - 6.2. Hình học Định sẵn & Đường vẽ Tự do (`<a:prstGeom>`, `<a:avLst>`, `<a:custGeom>`, cú pháp `<a:rect>`)
   - 6.3. Hệ thống Tô màu & Chuyển sắc (`<a:solidFill>`, `<a:gradFill>`, `<a:blipFill>`, `<a:pattFill>`)
   - 6.4. Đường viền, Nét vẽ & Mũi tên Chỉ dẫn (`<a:ln>`, mất `<a:prstDash>`, mất `<a:headEnd>`/`<a:tailEnd>`)
   - 6.5. Hiệu ứng Thị giác Bóng đổ, Mờ biên & Không gian 3D (`<a:effectLst>`, `<a:sp3d>`)
7. [Bảng Ma trận Đối chiếu Tính năng & Tỷ lệ Tương thích (% Compliance Matrix)](#7-bảng-ma-trận-đối-chiếu-tính-năng--tỷ-lệ-tương-thích)
8. [Phân loại Lỗ hổng theo 3 Cấp độ Nghiêm trọng (Severity Assessment)](#8-phân-loại-lỗ-hổng-theo-3-cấp-độ-nghiêm-trọng)
   - 8.1. Cấp độ Critical (Nghiêm trọng: Hỏng tệp / File Repair / Mất dữ liệu)
   - 8.2. Cấp độ Major (Quan trọng: Biến dạng bố cục / Mất định dạng trực quan)
   - 8.3. Cấp độ Minor (Mở rộng: Siêu dữ liệu / In ấn / Thuộc tính tùy chọn)
9. [Lộ trình Kỹ thuật Từng bước & Giải pháp Mã nguồn Cụ thể (Actionable Engineering Roadmap)](#9-lộ-trình-kỹ-thuật-từng-bước--giải-pháp-mã-nguồn-cụ-thể)
   - 9.1. Giai đoạn 1: Khắc phục Triệt để các Lỗi Critical Gây Hỏng Tệp M365 (Ngay lập tức)
   - 9.2. Giai đoạn 2: Nâng cấp Cấu trúc Dữ liệu Cốt lõi & Khôi phục Bố cục (Ngắn hạn)
   - 9.3. Giai đoạn 3: Hoàn thiện Chuẩn hóa Toàn diện các Tính năng Nâng cao (Dài hạn)
10. [Kết luận & Khuyến nghị Kiến trúc (Conclusion & Architectural Recommendations)](#10-kết-luận--khuyến-nghị-kiến-trúc)

---

## 1. TÓM TẮT ĐIỀU HÀNH & TỔNG QUAN HỆ THỐNG

### 1.1. Bối cảnh Khảo sát
ZenQ Office là bộ ứng dụng văn phòng tích hợp đa nền tảng được phát triển trên ngôn ngữ C# (.NET Framework / .NET Core) với mục tiêu làm chủ hoàn toàn các định dạng tài liệu mà không phụ thuộc vào các thư viện thương mại độc quyền (như Aspose, Spire) hay OpenXML SDK của bên thứ ba. Hệ thống bao gồm 3 phân hệ ứng dụng then chốt:
- **ZenQ Writer**: Trình soạn thảo văn bản hành chính và nghiệp vụ định dạng `.docx` (WordprocessingML).
- **ZenQ Calc**: Ứng dụng bảng tính ma trận thưa hiệu năng cao định dạng `.xlsx` (SpreadsheetML).
- **ZenQ Slides**: Ứng dụng thuyết trình và diễn họa đồ họa vector định dạng `.pptx` (PresentationML).

Để đảm bảo tài liệu được khởi tạo, chỉnh sửa và lưu trữ từ ZenQ Office có thể lưu chuyển trơn tru, không gặp bất kỳ rào cản cảnh báo lỗi nào khi người dùng trao đổi với hệ sinh thái **Microsoft 365 (Word, Excel, PowerPoint build 16.0+)**, LibreOffice, Google Docs, một cuộc điều tra kỹ thuật chuyên sâu (Forensic Deep-Dive Audit) đã được tiến hành trên toàn bộ mã nguồn xử lý định dạng.

### 1.2. Đánh giá Tổng thể Năng lực Tuân thủ
Hệ thống thể hiện năng lực lập trình nội tại rất ấn tượng ở tầng hiển thị đồ họa GDI+, giải mã cú pháp XML và thuật toán tính toán công thức:
- **Động cơ Bảng tính (Calc Core)**: `SparseMatrix.cs` và `FormulaEngine.cs` (hơn 5.400 dòng mã) thông dịch được hàng trăm hàm Excel phức tạp, hỗ trợ cấu trúc dữ liệu ma trận thưa 64-bit và cơ chế mảng động hiện đại.
- **Động cơ Trình chiếu (Slides Core)**: `SlidesView.cs` và `PptxHandler` xuất sắc trong việc hỗ trợ chuyển tiếp Morph của Office 365 cùng 28 hiệu ứng chuyển cảnh hiện đại, giải mã cây hình dạng đệ quy và đóng gói media nhị phân.
- **Động cơ Văn bản (Writer Core)**: `DocxHandler` giải mã nhanh cấu trúc văn bản phân cấp, trích xuất hình ảnh, xử lý đường biên và chuyển đổi đối tượng bảng biểu.

**Tuy nhiên, ở tầng chuyển đổi chuẩn định dạng OpenXML (Format Serialization Layer), hệ thống bộc lộ sự mất cân đối nghiêm trọng giữa khả năng nạp (Load/Import) và khả năng xuất (Save/Export), kèm theo các lỗ hổng kiến trúc chí mạng**:
1. **Lỗi Kích hoạt Hộp thoại Phục hồi Tệp ("File Repair / Unreadable Content") trên Microsoft 365**:
   - Trong `.pptx`: Quên tạo tệp quan hệ `ppt/notesSlides/_rels/notesSlide*.xml.rels` khiến tệp có ghi chú diễn giả bị xem là chứa part mồ côi (dangling part). Cơ chế bắt lỗi `catch` nguy hiểm tại `FormatHandlers.cs:8204` ghi đè tệp XML trần không nén mang đuôi `.pptx`.
   - Trong `.xlsx`: Xuất các hàm Excel 365 mới (như `XLOOKUP`, `SORT`, `FILTER`, `UNIQUE`) mà không thêm tiền tố bắt buộc `_xlfn.`, khiến Excel 365 coi đây là hàm lỗi và bật cảnh báo phục hồi.
2. **Nguy cơ Mất Trắng Dữ liệu (Silent Data Loss)**:
   - Trong `.docx`: Hoàn toàn bỏ rơi các thẻ Khung nội dung (`<w:sdt>`), khiến toàn bộ nội dung trong biểu mẫu hợp đồng bị biến mất khi nạp; xóa sạch lịch sử Theo dõi Chỉnh sửa (`TrackChanges`); triệt tiêu chú thích cuối trang (`<w:footnote>`).
   - Trong `.xlsx`: Khi nạp tệp Excel chuẩn chứa công thức dùng chung (`<f t="shared">`), trình nạp bỏ qua thẻ `<f>` rỗng của các ô phụ thuộc, biến hàng ngàn ô công thức thành số chết tĩnh (`static values`). Trình nạp bỏ qua quan hệ `xl/_rels/workbook.xml.rels`, dừng đọc ngay khi gặp lỗ hổng số hiệu sheet, làm mất các sheet phía sau.
   - Trong `.pptx`: Biểu đồ (`ShapeType.Chart`) hoàn toàn không được xuất ra XML, bị biến thành hình chữ nhật trống rỗng.
3. **Suy Giảm Bố cục & Mất Khả năng Chỉnh sửa (Layout Degradation & Format Flattening)**:
   - Toàn bộ vector shapes, SmartArt và Charts trong Writer bị **raster hóa thành ảnh chụp bitmap PNG** (`<pic:pic>`), làm mất khả năng chỉnh sửa văn bản bên trong và vỡ hạt khi in ấn.
   - Bảng biểu trong `.docx` không hỗ trợ gộp ô ngang/dọc (`<w:gridSpan>`, `<w:vMerge>`), làm vỡ hoàn toàn ma trận ô.
   - Trong PresentationML, toàn bộ slide bị ép cứng về `slideLayout1.xml` và bỏ qua thẻ giữ chỗ `<p:ph>`, làm vô hiệu hóa Microsoft AI Designer, hỏng Outline View và gãy tính năng Reset Slide.
   - Trong DrawingML, bỏ quên cờ lật hình `flipH`/`flipV`, biến mũi tên thành đường thẳng trần, biến đường nét đứt thành nét liền.

### 1.3. Bảng Điểm Tương Thích Chuẩn Hóa Theo Từng Phân Hệ
Dưới đây là bảng tổng hợp mức độ tuân thủ chuẩn ISO/IEC 29500 / ECMA-376 được xác lập trên cơ sở kiểm tra thực chứng từng dòng mã:

| Phân hệ Nghiệp vụ | Trọng tâm Khảo sát | Tỷ lệ Đọc (Load) | Tỷ lệ Ghi (Save) | Mức Tuân Thủ Chung | Đánh giá Cấp độ Rủi ro M365 |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **WordprocessingML** | ZenQ Writer (`.docx`) | 55.7% | 30.0% | **42.8%** | **CRITICAL**: Mất gộp ô bảng, mất SDT, mất Footnotes, raster hóa vector |
| **SpreadsheetML** | ZenQ Calc (`.xlsx`) | 58.2% | 34.8% | **46.5%** | **CRITICAL**: Lỗi `_xlfn.`, mất shared formula, mất sheet phi tuần tự |
| **PresentationML** | ZenQ Slides (`.pptx`) | 62.5% | 33.5% | **48.0%** | **CRITICAL**: Lỗi notes rels, fallback XML trần, mất biểu đồ, mất `<p:ph>` |
| **Shared DrawingML** | Khung Đồ họa Chung | 66.0% | 36.0% | **51.0%** | **MAJOR**: Bỏ rơi flipH/flipV, mất nét đứt, mất mũi tên, ép linear gradient |
| **TOÀN BỘ BỘ OFFICE** | **Tổng thể 4 Phân hệ** | **60.6%** | **33.6%** | **~47.1%** | **CẦN NÂNG CẤP THEO LỘ TRÌNH CHUẨN HÓA** |

---

## 2. KIẾN TRÚC ĐÓNG GÓI OPC & NỀN TẢNG ĐỊNH DẠNG (OPC PACKAGING FOUNDATION)

Chuẩn **ECMA-376 Part 2** và **ISO/IEC 29500-2** định nghĩa Quy ước Đóng gói Mở (Open Packaging Conventions - OPC). Một tệp OOXML về bản chất là một kho lưu trữ nén chuẩn ZIP chứa các phần dữ liệu (Parts), các tệp quan hệ phân cấp (`.rels`), và tệp định nghĩa kiểu nội dung cốt lõi `[Content_Types].xml`.

```
                    [ GÓI TỆP OPC (.docx / .xlsx / .pptx) ]
                                      │
            ┌─────────────────────────┼─────────────────────────┐
            ▼                         ▼                         ▼
   [Content_Types].xml           _rels/.rels            docProps/core.xml & app.xml
   (Định danh MIME Type)     (Quan hệ Gói Cấp 0)         (Siêu dữ liệu Dublin Core)
            │                         │                         │
            └────────────┬────────────┘                         │
                         ▼                                      │
             Part Chính của Ứng dụng                            │
       (word/document.xml | xl/workbook.xml | ppt/presentation.xml)
                         │
             ┌───────────┴───────────┐
             ▼                       ▼
    _rels/*.xml.rels           Các Part Con (Styles, Theme,
  (Cây Quan hệ Cấp 1)        Sheets, Slides, Media, Drawings)
```

### 2.1. Phân tích Tệp Khai báo Kiểu Nội dung `[Content_Types].xml`
Tệp `[Content_Types].xml` nằm ở thư mục gốc của gói nén, có vai trò bắt buộc chỉ dẫn cho trình đọc nhận biết kiểu MIME của từng phần dữ liệu.

#### Codebase Hiện trạng:
- WordprocessingML: `FormatHandlers.cs` dòng 6656–6682 (`SaveDocxFile`).
- SpreadsheetML: `XlsxHandler.cs` dòng 857–885 (`SaveXlsx`).
- PresentationML: `FormatHandlers.cs` dòng 7803–7853 (`SavePptx`).

#### Các sai lệch kỹ thuật nghiêm trọng:
1. **Thiếu Khai báo Siêu dữ liệu Gói Chuẩn (`docProps`)**:
   Cả 3 phân hệ hoàn toàn không đăng ký ContentType cho:
   - `/docProps/core.xml`: `application/vnd.openxmlformats-package.core-properties+xml`
   - `/docProps/app.xml`: `application/vnd.openxmlformats-officedocument.extended-properties+xml`
   *Hậu quả trên M365*: Khi nhấp chuột phải vào tệp trong Windows File Explorer hoặc mở menu *File -> Info* trong Office 365, các thông tin Tác giả (Author), Ngày tạo (Created), Lần sửa cuối (Modified), Thống kê số từ, số trang, số dòng đều trống rỗng.
2. **Thiếu Khai báo Bảng Chuỗi Dùng chung trong Calc**:
   Trong `XlsxHandler.cs`, gói `[Content_Types].xml` không có phần tử:
   `<Override PartName="/xl/sharedStrings.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"/>`.
   Do ZenQ Calc ép toàn bộ chuỗi sang `inlineStr`, khi nâng cấp lên Shared Strings bắt buộc phải bổ sung dòng khai báo này nếu không Excel sẽ từ chối nạp bảng chuỗi.
3. **Thiếu Khai báo Kiểu Nội dung cho Biểu đồ và SmartArt trong Slides**:
   Trong `PptxHandler`, danh sách Override thiếu hoàn toàn các part biểu đồ (`/ppt/charts/chart*.xml`: `application/vnd.openxmlformats-officedocument.drawingml.chart+xml`). Nếu chèn biểu đồ vào slide, PowerPoint 365 sẽ cảnh báo tệp chứa part không xác định.

### 2.2. Hệ thống Tệp Quan hệ (`_rels/.rels` và `_rels/*.xml.rels`)
Theo chuẩn OPC, các phần trong gói không bao giờ được tham chiếu trực tiếp bằng đường dẫn tương đối cứng trong nội dung XML mà phải thông qua định danh quan hệ (`r:id`). Trình phân giải sẽ tra cứu `r:id` trong tệp `.rels` tương ứng để tìm đến tài nguyên đích (`Target`).

#### Codebase Hiện trạng:
- Package Rels: `FormatHandlers.cs:6684` (Docx), `XlsxHandler.cs:887` (Xlsx), `FormatHandlers.cs:7783` (Pptx trích từ zip template).
- Document Rels: `FormatHandlers.cs:7690` (`word/_rels/document.xml.rels`), `XlsxHandler.cs:927` (`xl/_rels/workbook.xml.rels`), `FormatHandlers.cs:8140` (`ppt/slides/_rels/slide{i}.xml.rels`).

#### Các lỗ hổng chí mạng:
1. **Lỗi Part Mồ côi trong Tệp Ghi chú Diễn giả (`notesSlide*.xml`) - BUG CRITICAL**:
   Tại `FormatHandlers.cs:8144-8186`, khi slide có ghi chú diễn giả, hệ thống ghi quan hệ `rIdNotes1` trong `slide{i}.xml.rels` trỏ tới `../notesSlides/notesSlide{i}.xml`. Nhưng hệ thống **hoàn toàn quên tạo tệp `ppt/notesSlides/_rels/notesSlide{i}.xml.rels`**.
   Theo chuẩn ECMA-376 Part 1 §13.3.4, phần `notesSlide` bắt buộc phải có quan hệ trỏ ngược về `slide{i}.xml` và trỏ tới `notesMaster1.xml`. Thiếu tệp quan hệ này khiến PowerPoint 365 kích hoạt ngay lập tức thông báo lỗi hỏng tệp:
   > *"PowerPoint found a problem with content in presentation.pptx. PowerPoint can attempt to repair the presentation."*
2. **Bỏ qua Bảng Quan hệ khi Đọc Sổ làm việc Excel (`XlsxHandler.cs:504-521`) - BUG CRITICAL**:
   Trình nạp `LoadXlsx` **hoàn toàn không đọc `xl/_rels/workbook.xml.rels`**. Thay vào đó, nó duyệt cứng chuỗi tên file `sheet1.xml`, `sheet2.xml`. Nếu một tệp Excel 365 từng bị xóa Sheet1 (chỉ còn `sheet2.xml` gắn với quan hệ `rId1`), biến `wsEntry` trả về `null` ngay lần lặp đầu tiên, kích hoạt lệnh `break;` và **làm mất sạch toàn bộ các sheet còn lại** trong sổ làm việc.
3. **Mất Siêu liên kết Ngoài (External Hyperlinks)**:
   Khi tài liệu có link dẫn tới trang web (ví dụ `https://example.com`), chuẩn OOXML yêu cầu tạo thẻ `<Relationship Id="rIdX" Type=".../relationships/hyperlink" Target="https://..." TargetMode="External"/>`. Cả Writer và Calc đều không tạo quan hệ này, làm các đường link web bị vô hiệu hóa hoặc biến thành văn bản chết.

### 2.3. Lỗ hổng Fallback XML Trần Cực kỳ Nguy hiểm trong PptxHandler
Tại `src/format/FormatHandlers.cs` dòng 8201–8206:
```csharp
catch (Exception ex)
{
    Console.WriteLine("EXPORT PPTX ERROR: " + ex.Message + "\n" + ex.StackTrace);
    return SavePptxXml(filepath, pres);
}
```
Và tại dòng 12533:
```csharp
public static bool SavePptxXml(string filepath, PresentationRoot pres)
{
    using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.UTF8))
    {
        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sw.WriteLine("<p:presentation xmlns:a=\"...\" xmlns:p=\"...\">");
        ...
```
**Đánh giá rủi ro**: Đây là một cơ chế phòng vệ lỗi sai lầm nghiêm trọng (Anti-pattern). Khi xuất tệp `.pptx` gặp bất kỳ sự cố nào (ví dụ ảnh nhúng bị khóa stream, xung đột I/O ZIP), hệ thống bắt lỗi và gọi `SavePptxXml`. Hàm này ghi toàn bộ bài thuyết trình thành **MỘT TỆP VĂN BẢN XML THUẦN TÚY KHÔNG NÉN MANG PHẦN MỞ RỘNG `.pptx`**.
Khi người dùng mở tệp này trong Microsoft PowerPoint 365, PowerPoint nhận diện phần mở rộng `.pptx` nhưng khi giải nén bằng thuật toán PKZip thì gặp dữ liệu văn bản góc nhọn XML, dẫn đến thông báo lỗi từ chối mở tệp hoàn toàn:
> *"PowerPoint cannot open the file 'presentation.pptx' because the file format or file extension is not valid. Verify that the file has not been corrupted and that the file extension matches the format of the file."*

---

## 3. PHÂN HỆ R1: WORDPROCESSINGML (ZENQ WRITER - .DOCX)

WordprocessingML là ngôn ngữ đánh dấu văn bản được quy định tại ISO/IEC 29500-1 Clause 17.

### 3.1. Sơ đồ Kiến trúc & Phân bố Mã nguồn
- **Bộ xử lý Định dạng chính**: `src/format/FormatHandlers.cs` (khối `DocxHandler`, dòng 18 đến 7768).
- **Bộ chuyển đổi ODF tương đương**: `src/format/OdtHandler.cs` (314 dòng).
- **Giao diện & Điều khiển Soạn thảo**: `src/ui/WriterView.cs` (27.677 dòng, phương thức `LoadDocxFile` dòng 7919–8015, `SaveDocxFile` dòng 8210–8247).
- **Mô hình Dữ liệu Bảng**: `src/ui/DocTableObject.cs` & `src/core/DocTableObject.cs` (`DocTableObject`, `DocTableRow`, `DocTableCell`, `DocCellParagraph`).
- **Mô hình Hình vẽ & Vector**: `src/ui/DocShapeObject.cs` (`DocShapeObject`, `DocSmartArtObject`, `DocChartObject`).

```
                    LUỒNG DỮ LIỆU WORDPROCESSINGML (ZENQ WRITER)
┌─────────────────────────────────────────────────────────────────────────────┐
│ Tệp .docx (ZIP Archive)                                                     │
│   ├── [Content_Types].xml                                                   │
│   ├── word/document.xml (Nội dung chính: w:p, w:r, w:tbl, w:drawing)        │
│   ├── word/styles.xml (Định nghĩa kiểu dáng Heading, Normal)                 │
│   ├── word/numbering.xml (Định nghĩa danh sách đánh số)                     │
│   └── word/header*.xml, footer*.xml                                         │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Trình nạp DocxHandler.LoadDocxFile (FormatHandlers.cs:200-2500)             │
│   ├── ParseDocumentXmlFull: Trích xuất Text, Styles, Numbering               │
│   ├── ParseTableNode (5855-6574): Nạp w:tbl -> DocTableObject               │
│   ├── ParseDrawingML (2529-5220): Nạp w:drawing -> DocShapeObject           │
│   └── Gán vào WriterView (WriterView.cs:7922): Revisions.Clear() [MẤT REVS]  │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Trình xuất DocxHandler.SaveDocxFile (FormatHandlers.cs:6630-7768)            │
│   ├── Ghi w:styles rỗng (6858): CHỈ GHI DOCDEFAULTS, MẤT TOÀN BỘ HEADING    │
│   ├── Ghi w:tbl (7114-7150): MẤT GRIDSPAN, MẤT VMERGE, MẤT TCBORDERS        │
│   ├── Ghi w:drawing (7208-7243): RASTER HÓA SMARTART & VECTOR SANG ẢNH PNG  │
│   └── Ghi w:r (7624-7637): MẤT W:HIGHLIGHT, ÉP W:U VAL="SINGLE"             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 3.2. Đoạn văn, Ký tự & Định dạng Văn bản

#### A. Thẻ Ký tự (`<w:rPr>` và `<w:r>`)
- **Khảo sát Trình đọc (`FormatHandlers.cs:2200-2437`)**:
  Nhận diện đầy đủ các thẻ định dạng ký tự cơ bản: phông chữ (`<w:rFonts>`), cỡ chữ (`<w:sz>`), in đậm (`<w:b>`), in nghiêng (`<w:i>`), gạch chân (`<w:u>`), gạch ngang (`<w:strike>`), chỉ số trên/dưới (`<w:vertAlign>`), màu sắc (`<w:color>`), màu dạ quang (`<w:highlight>`), bóng chữ (`<w:shd>`), và khoảng cách dãn chữ (`<w:spacing>`).
- **Khảo sát Trình xuất (`FormatHandlers.cs:7624-7637`)**:
  ```csharp
  sw.WriteLine("      <w:r>");
  sw.WriteLine("        <w:rPr>");
  sw.WriteLine(string.Format("          <w:rFonts w:ascii=\"{0}\" w:hAnsi=\"{0}\" w:cs=\"{0}\" w:eastAsia=\"{0}\"/>", EscapeXml(font)));
  if (b) { sw.WriteLine("          <w:b/>"); sw.WriteLine("          <w:bCs/>"); }
  if (it) { sw.WriteLine("          <w:i/>"); sw.WriteLine("          <w:iCs/>"); }
  if (strk) sw.WriteLine("          <w:strike/>");
  if (c != Color.Black && c != Color.Empty && c.A > 0)
  {
      sw.WriteLine(string.Format("          <w:color w:val=\"{0:X2}{1:X2}{2:X2}\"/>", c.R, c.G, c.B));
  }
  sw.WriteLine(string.Format("          <w:sz w:val=\"{0}\"/>", halfPts));
  sw.WriteLine(string.Format("          <w:szCs w:val=\"{0}\"/>", halfPts));
  if (u) sw.WriteLine("          <w:u w:val=\"single\"/>");
  if (sub) sw.WriteLine("          <w:vertAlign w:val=\"subscript\"/>");
  else if (sup) sw.WriteLine("          <w:vertAlign w:val=\"superscript\"/>");
  sw.WriteLine("        </w:rPr>");
  sw.WriteLine("        <w:t xml:space=\"preserve\">" + EscapeXml(runText) + "</w:t>");
  sw.WriteLine("      </w:r>");
  ```
- **Lỗ hổng 1: Mất Toàn bộ Màu Dạ quang (`<w:highlight>`) khi Lưu (Bug Major M6)**:
  Mặc dù đối tượng `FormattedRun` trong bộ nhớ có lưu giữ thuộc tính màu highlight, trong toàn bộ khối xuất `<w:rPr>` (dòng 7624–7637), **hoàn toàn không có câu lệnh ghi `<w:highlight w:val="..."/>`**! Khi người dùng mở tệp có highlight hoặc bôi màu ghi chú trong ZenQ Writer rồi lưu lại sang `.docx`, khi mở lại trên Word 365, toàn bộ các vùng đánh dấu dạ quang bị biến mất sạch sẽ.
- **Lỗ hổng 2: Thoái hóa Kiểu Gạch chân (Underline Flattening)**:
  Chuẩn ECMA-376 quy định 18 kiểu gạch chân khác nhau (`words`, `double`, `thick`, `dotted`, `dash`, `wave`, `wavyDouble`, v.v.) kèm thuộc tính màu gạch chân riêng biệt (`<w:u w:val="double" w:color="FF0000"/>`). Trình xuất của ZenQ chỉ kiểm tra cờ nhị phân `if (u)` và ép cứng thành `<w:u w:val="single"/>`. Toàn bộ các định dạng gạch chân đôi trong văn bản tài chính hay gạch chân lượn sóng báo hiệu đều bị biến thành gạch đơn màu đen.
- **Lỗ hổng 3: Bỏ qua Hiệu ứng Ký tự Mở rộng**:
  Hoàn toàn không hỗ trợ `<w:caps/>` (chữ in hoa toàn bộ), `<w:smallCaps/>` (chữ in hoa nhỏ), `<w:vanish/>` (văn bản ẩn phục vụ in ấn chuyên dụng).

#### B. Thẻ Đoạn văn (`<w:pPr>` và `<w:p>`)
- **Khảo sát Trình xuất (`FormatHandlers.cs:7496-7648`)**:
  Đã xử lý tốt: Căn lề `<w:jc w:val="left|center|right|both"/>`, thụt lề đầu dòng `<w:ind w:firstLine="..." w:left="..." w:right="..."/>`, khoảng cách dòng và đoạn `<w:spacing w:line="..." w:before="..." w:after="..."/>`, và đường viền đoạn `<w:pBdr>`.
- **Lỗ hổng 4: Triệt tiêu Điểm Dừng Tab (`<w:tabs>`) khi Lưu Tệp (Bug Major M4)**:
  Trong hàm đọc `ParseParagraphNode` (dòng 2088–2133), hệ thống phân tích rất chi tiết danh sách tab: `<w:tabs><w:tab w:val="left|center|right|decimal" w:pos="..." w:leader="dot|hyphen|underscore"/></w:tabs>`.
  Tuy nhiên, khi xuất tệp trong `SaveDocxFile` (dòng 7500–7585), **thẻ `<w:tabs>` hoàn toàn không được ghi ra**!
  Nghiêm trọng hơn, tại `src/ui/WriterView.cs` dòng 8216–8247, lời gọi `SaveDocxFile` thậm chí còn không truyền từ điển `ParagraphTabStops` vào hàm xử lý.
  *Hậu quả trên Word 365*: Toàn bộ các văn bản hành chính (hợp đồng, đơn từ có dòng dấu chấm `..........` tạo bằng tab leader, bảng giá thẳng hàng số thập phân bằng tab decimal) đều bị xô lệch, các cột chữ nhảy lộn xộn.
- **Lỗ hổng 5: Thiếu Thuộc tính Phân trang Thông minh Đoạn văn**:
  Chuẩn WordprocessingML sử dụng các cờ kiểm soát ngắt trang: `<w:keepNext/>` (giữ tiêu đề luôn nằm cùng trang với đoạn văn bản liền sau), `<w:keepLines/>` (không ngắt đôi đoạn văn qua 2 trang), `<w:pageBreakBefore/>` (buộc ngắt trang trước đoạn), và `<w:widowControl/>` (chống dòng mồ côi). Do ZenQ không xuất các cờ này, khi in ấn tài liệu từ Word 365, các tiêu đề chương mục có thể bị rớt lại trơ trọi ở đáy trang trước.

---

### 3.3. Danh sách Đánh số & Đầu dòng Đa cấp (`<w:numbering>`)

#### Cơ chế của Chuẩn ECMA-376 (§17.9)
Chuẩn OOXML tách biệt tuyệt đối giữa nội dung văn bản và logic đánh số. Tài liệu chứa một tệp riêng `word/numbering.xml` định nghĩa:
- `<w:abstractNum w:abstractNumId="0">`: Định nghĩa cấu trúc mẫu của danh sách đa cấp (từ cấp 0 đến cấp 8), quy định phông chữ bullet, ký tự bullet, định dạng số `<w:numFmt w:val="decimal|lowerLetter|lowerRoman|bullet"/>`, chuỗi mẫu `<w:lvlText w:val="%1.%2. "/>`, và độ thụt lề thụt lề treo `<w:ind>`.
- `<w:num w:numId="1">`: Thể hiện một danh sách thực tế trỏ tới `abstractNumId`.
- Trong `word/document.xml`, các đoạn danh sách chỉ khai báo:
  `<w:pPr><w:numPr><w:ilvl w:val="0"/><w:numId w:val="1"/></w:numPr></w:pPr>`.
  Văn bản trong `<w:t>` hoàn toàn KHÔNG chứa số thứ tự hay ký tự bullet! Word sẽ tự động tính toán số thứ tự khi dựng hình.

#### Thực trạng Codebase ZenQ Writer:
- **Trình đọc (`FormatHandlers.cs:1135-1261` & `2009-2020`)**:
  Hàm `ParseNumberingXml` đọc đúng các thẻ `<w:abstractNum>`, `<w:lvl>`, `<w:num>`.
  **Tuy nhiên, tại dòng 2009–2020, hệ thống thực hiện "Làm phẳng" (Flattening)**:
  Trình đọc tính toán chuỗi số (ví dụ `"1.1. "` hoặc `"• "`), rồi **nối trực tiếp chuỗi này vào bộ đệm văn bản** (`sb.Append(prefixText)`).
- **Trình xuất (`SaveDocxFile:7496-7648`)**:
  - **Hoàn toàn KHÔNG TẠO `word/numbering.xml`**!
  - Không tạo quan hệ numbering trong `word/_rels/document.xml.rels`.
  - Không ghi thẻ `<w:numPr>` vào đoạn văn.
- **Hậu quả thảm hại trên Microsoft Word 365 (Bug Major M5)**:
  - Khi mở tệp trên Word 365, các danh sách đánh số biến thành các đoạn văn bản thường mang chuỗi tĩnh (hardcoded plain text).
  - Người dùng đặt con trỏ ở cuối dòng `1.1.` và nhấn `Enter`, Word không tự động nhảy số `1.2.`.
  - Nhấn phím `Tab` hay `Shift + Tab` không thể tăng giảm cấp độ thụt lề danh sách.
  - Người dùng xóa bớt một mục ở giữa, các mục bên dưới không tự động đánh lại số thứ tự.

---

### 3.4. Hệ thống Kiểu dáng (`word/styles.xml` & Cây thừa kế Style)

Hệ thống Style là linh hồn của tài liệu văn phòng chuyên nghiệp, quyết định cấu trúc ngữ nghĩa của toàn bộ tài liệu (Document Semantic Tree).

#### Codebase Hiện trạng:
- Đọc Styles: `FormatHandlers.cs` dòng 1414–1656 trong `ParseDocumentXmlFull`.
- Xử lý Đệ quy Kế thừa: Dòng 1600–1635 (giải quyết `w:basedOn` lên tới 10 tầng).
- Ghi Styles: `FormatHandlers.cs` dòng 6858–6873.

#### Điểm yếu Cốt lõi & Lỗ hổng Kiến trúc (Bug Major M1):
Đoạn mã xuất `word/styles.xml` tại dòng 6858–6873 được viết như sau:
```csharp
sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
sw.WriteLine("<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">");
sw.WriteLine("  <w:docDefaults>");
sw.WriteLine("    <w:rPrDefault>");
sw.WriteLine("      <w:rPr>");
sw.WriteLine("        <w:rFonts w:ascii=\"Times New Roman\" w:hAnsi=\"Times New Roman\" w:eastAsia=\"Times New Roman\" w:cs=\"Times New Roman\"/>");
sw.WriteLine("        <w:sz w:val=\"24\"/>");
sw.WriteLine("        <w:szCs w:val=\"24\"/>");
sw.WriteLine("      </w:rPr>");
sw.WriteLine("    </w:rPrDefault>");
sw.WriteLine("  </w:docDefaults>");
sw.WriteLine("</w:styles>");
```

#### Hậu quả Trực tiếp trên Microsoft Word 365:
1. **Không có bất kỳ định nghĩa `<w:style>` nào**:
   Tệp `word/styles.xml` không chứa bất kỳ định nghĩa kiểu đoạn văn hay kiểu ký tự nào (kể cả 4 style tối thiểu bắt buộc: `Normal`, `Heading 1`, `Heading 2`, `Heading 3`, `Title`).
2. **Đoạn văn trong `word/document.xml` không mang thuộc tính ngữ nghĩa**:
   Khi ghi nội dung các đoạn tiêu đề, hàm ghi chỉ xuất các thẻ thuộc tính định dạng trực tiếp (`w:sz`, `w:b`, `w:color`) mà **hoàn toàn không có thẻ `<w:pStyle w:val="Heading1"/>`**!
3. **Thanh Điều hướng Word 365 (Navigation Pane / Document Map) Hoàn toàn Trống rỗng**:
   Khi người dùng mở khung *Navigation Pane* trên Word 365, tab *Headings* không hiển thị bất kỳ mục nào, người dùng không thể nhấp chuột để điều hướng nhanh giữa các chương mục của tài liệu dài.
4. **Vô hiệu hóa Tính năng Tự động Tạo Mục lục (Table of Contents - TOC)**:
   Mọi tính năng chèn Mục lục tự động của Word (`<w:sdt>` chứa `TOC \o "1-3"`) đều bị tê liệt vì Word quét tài liệu và không phát hiện bất kỳ đoạn văn nào được đánh dấu là `Heading`.

---

### 3.5. Bố cục Trang & Phân vùng Phân đoạn (`<w:sectPr>`)

#### A. Giới hạn Đơn Phân vùng (Single Section Limitation)
- **Đặc tả OOXML (§17.6)**: Cho phép tài liệu chia thành vô số phân vùng độc lập (`Section`), mỗi phân vùng có khổ giấy, hướng xoay (ngang/dọc), khoảng cách lề, số cột báo, và bộ đầu trang/chân trang riêng biệt. Thẻ `<w:sectPr>` có thể xuất hiện bên trong `<w:pPr>` của đoạn văn cuối cùng của phân vùng đó, hoặc ở cuối thẻ `<w:body>`.
- **Khảo sát Codebase**:
  - *Đọc (`FormatHandlers.cs:1356`)*: Lệnh `bodyNode.SelectSingleNode(".//w:sectPr")` chỉ lấy duy nhất section đầu tiên tìm thấy. Khi gặp các ngắt phân vùng nằm rải rác giữa tài liệu (dòng 2462–2479), ZenQ chỉ kiểm tra nếu khác `continuous` thì chèn chuỗi text giả `"[--- PAGE BREAK ---]\n"`, vứt bỏ toàn bộ cấu hình lề và hướng giấy của các section tiếp theo.
  - *Ghi (`FormatHandlers.cs:7657-7685`)*: Chỉ xuất duy nhất một thẻ `<w:sectPr>` ở đáy cùng của `<w:body>`.
- *Hậu quả*: Không thể xử lý các tài liệu phức tạp (như báo cáo tài chính có trang đầu dọc, trang phụ lục ngang chứa bảng số liệu rộng, rồi trang kết luận dọc trở lại). Tất cả các trang đều bị ép về cùng một hướng giấy và cùng kích thước lề.

#### B. Bố cục Nhiều Cột Báo (`<w:cols>`) - Bug Major M7
- Hoàn toàn **không có mã xử lý** (0 dòng code) cho thẻ `<w:cols w:num="2" w:space="720"/>`.
- Mọi bài báo khoa học, văn bản quy phạm pháp luật chia 2 cột hoặc 3 cột đều bị duỗi phẳng thành văn bản 1 cột trải dài toàn trang.

#### C. Lỗ hổng Mất Tiêu đề Trang Bìa (`<w:titlePg>`) - Bug Major M8
- **Đặc tả OOXML (§17.10.6)**: Khi tài liệu có trang bìa (Title Page / First Page), phân vùng phải chứa thẻ rỗng `<w:titlePg/>` trong `sectPr`, và khai báo liên kết tiêu đề riêng:
  `<w:headerReference w:type="first" r:id="rIdHeaderFirst"/>`.
- **Thực tế Codebase**:
  Từ khóa `titlePg` xuất hiện **0 lần** trong toàn bộ codebase ZenQ Office!
  Tại dòng 275 và dòng 7659, ZenQ chỉ hỗ trợ liên kết kiểu `default` (`header1.xml`) và `even` (`header2.xml`).
- *Hậu quả*: Khi mở một tài liệu có trang bìa trong ZenQ Writer, tiêu đề của các trang sau (chứa tên công ty, số điện thoại, tiêu đề báo cáo) bị **áp tràn ngược lên toàn bộ trang bìa**, làm mất thẩm mỹ nghiêm trọng. Khi lưu lại tệp, cấu hình trang bìa biến mất vĩnh viễn.

---

### 3.6. Bảng biểu Chuyên sâu (Tables - `<w:tbl>`)

Bảng biểu trong WordprocessingML được định nghĩa bởi cấu trúc phân cấp:
`<w:tbl>` -> `<w:tblPr>` -> `<w:tblGrid>` -> `<w:tr>` -> `<w:tc>` -> `<w:tcPr>` -> `<w:p>`.

#### Sơ đồ Đối chiếu Cấu trúc Bảng Word OOXML vs ZenQ Writer:
```
           CHUẨN ECMA-376 (WORD 365)                    ZENQ WRITER (FORMAT HANDLERS)
┌───────────────────────────────────────────┐   ┌───────────────────────────────────────────┐
│ <w:tbl>                                   │   │ <w:tbl>                                   │
│   <w:tblPr>...</w:tblPr>                  │   │   <w:tblPr>...</w:tblPr>                  │
│   <w:tblGrid>                             │   │   <w:tblGrid>                             │
│     <w:gridCol w:w="3000"/>               │   │     <w:gridCol w:w="3000"/>               │
│     <w:gridCol w:w="3000"/>               │   │     <w:gridCol w:w="3000"/>               │
│   </w:tblGrid>                            │   │   </w:tblGrid>                            │
│   <w:tr>                                  │   │   <w:tr>                                  │
│     <w:tc>                                │   │     <w:tc>                                │
│       <w:tcPr>                            │   │       <w:tcPr>                            │
│         <w:gridSpan w:val="2"/>  [GỘP CỘT]│   │         <!-- THIẾU HOÀN TOÀN gridSpan --> │
│         <w:vMerge w:val="restart"/>[GỘP HÀNG] │   │         <!-- THIẾU HOÀN TOÀN vMerge -->   │
│         <w:tcBorders>...         [VIỀN Ô] │   │         <!-- THIẾU HOÀN TOÀN tcBorders -->│
│       </w:tcPr>                           │   │       </w:tcPr>                           │
│       <w:p>Đoạn 1</w:p>                   │   │       <w:p>                               │
│       <w:p>Đoạn 2</w:p>                   │   │         Run 1 <w:br/> Run 2 [LÀM PHẲNG]   │
│     </w:tc>                               │   │       </w:p>                              │
│   </w:tr>                                 │   │     </w:tc>                               │
│ </w:tbl>                                  │   │   </w:tbl>                                │
└───────────────────────────────────────────┘   └───────────────────────────────────────────┘
```

#### Lỗ hổng Trọng yếu Gây Phá Hủy Cấu trúc Bảng:
1. **Hoàn Toàn Không Hỗ Trợ Gộp Ô Ngang (`<w:gridSpan>`) - BUG CRITICAL C1**:
   - *Chuẩn OOXML (§17.4.17)*: Thuộc tính `<w:gridSpan w:val="N"/>` xác định ô này chiếm bao nhiêu cột trong lưới `tblGrid`.
   - *Mã nguồn*: Trong hàm nạp `ParseTableNode` (dòng 5855–6574) và hàm xuất `SaveDocxFile` (dòng 7114–7150), **từ khóa `gridSpan` xuất hiện 0 lần**!
   - *Hậu quả*: Một ô tiêu đề gộp 4 cột bị ZenQ coi là 1 ô đơn lẻ có độ rộng lớn. Khi xuất ra, số lượng ô `<w:tc>` trên dòng tiêu đề chỉ có 1, trong khi các dòng dưới có 4 ô. Cấu trúc bảng bị lệch hoàn toàn so với `tblGrid`, khiến Word 365 báo lỗi *"Table structure corrupted"* và buộc phải sửa bảng.
2. **Hoàn Toàn Không Hỗ Trợ Gộp Ô Dọc (`<w:vMerge>`) - BUG CRITICAL C1**:
   - *Chuẩn OOXML (§17.4.84)*: Ô bắt đầu gộp mang `<w:vMerge w:val="restart"/>`, các ô bên dưới bị gộp mang `<w:vMerge/>` (hoặc `w:val="continue"`).
   - *Mã nguồn*: **Từ khóa `vMerge` xuất hiện 0 lần** trong toàn bộ mã nguồn!
   - *Hậu quả*: Trình nạp tạo ra các ô trắng thừa trên các hàng bên dưới. Khi lưu lại tệp, các ô này biến thành các ô độc lập chứa khoảng trắng, phá hỏng hoàn toàn biểu mẫu báo cáo.
3. **Mất Toàn bộ Đường Viền Riêng Của Từng Ô Bảng (`<w:tcBorders>`) - Bug Major M9**:
   - Khảo sát dòng 7114–7125:
     ```csharp
     sw.WriteLine("        <w:tc>");
     sw.WriteLine("          <w:tcPr>");
     if (cObj != null && cObj.WidthPt > 0)
         sw.WriteLine(string.Format("            <w:tcW w:w=\"{0}\" w:type=\"dxa\"/>", cObj.WidthPt * 20));
     if (!string.IsNullOrEmpty(shdHex))
         sw.WriteLine(string.Format("            <w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"{0}\"/>", shdHex));
     sw.WriteLine(string.Format("            <w:vAlign w:val=\"{0}\"/>", vAlign));
     sw.WriteLine("          </w:tcPr>");
     ```
   - Trong `DocTableCell.cs`, mô hình đã có đủ các trường `BorderTopStyle`, `BorderTopWidthPt`, `BorderTopColor`, v.v. Nhưng tại dòng 7115–7125, **hoàn toàn không có câu lệnh ghi `<w:tcBorders>`**!
   - *Hậu quả*: Toàn bộ viền đặc thù của từng ô (ví dụ ô tiêu đề kẻ viền dưới đôi, ô tổng cộng đóng khung đậm, hay ẩn viền dọc giữa các cột dữ liệu) đều bị triệt tiêu, tất cả bị ép theo viền chung mặc định của bảng.
4. **Làm Phẳng Đa Đoạn Văn Trong Ô Thành Một Đoạn Đơn**:
   Tại dòng 7141–7146, nội dung ô có nhiều dòng xuống hàng (`\n`) bị ghép vào một thẻ `<w:p>` duy nhất và ngắt dòng bằng `<w:br/>`. Hậu quả là không thể áp dụng các căn lề khác nhau hay thụt dòng bullet độc lập cho từng đoạn trong ô.
5. **Thiếu Cờ Chống Tách Hàng Qua Trang (`<w:cantSplit/>`)**:
   Bảng biểu có nội dung hàng dài khi in trên Word 365 sẽ bị cắt ngang hàng chữ ở mép trang giấy.

---

### 3.7. Các Thành Phần Văn Bản Nâng Cao

#### 1. Raster Hóa Đồ Họa Vector, SmartArt & Biểu đồ Sang Ảnh Bitmap PNG (Bug Major M2, M3)
- **Vị trí Mã nguồn**: `FormatHandlers.cs` dòng 7208–7243 và dòng 7398–7494.
- **Thực trạng Cơ chế**:
  ```csharp
  using (Bitmap saBmp = SmartArtRenderer.RenderToBitmap(shapeObj, renderW, renderH, 2.0f))
  using (MemoryStream ms = new MemoryStream())
  {
      saBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
      imgBytes = ms.ToArray();
      ext = "png";
  }
  ```
- **Hậu quả**:
  Mọi hình khối vector (Shapes), sơ đồ tư duy SmartArt, và Biểu đồ tương tác khi lưu ra file `.docx` đều bị **CHỤP MÀN HÌNH BIẾN THÀNH ẢNH RASTER PNG** (`<pic:pic>`).
  - Người dùng trên Word 365 không thể nhấp đúp để chỉnh sửa chữ bên trong hình vẽ.
  - Sơ đồ SmartArt mất cấu trúc node phân cấp, không thể đổi màu hay đổi layout.
  - Biểu đồ mất hoàn toàn bảng số liệu Excel nền tảng, không thể cập nhật số liệu.
  - In ấn độ phân giải cao bị vỡ hạt và mờ nét.

#### 2. Các Thành phần Nâng cao Hoàn Toàn Bị Bỏ Rơi (0% Supported)
| Thành phần OOXML | Thẻ XML Chuẩn ECMA-376 | Hiện trạng Codebase | Mức độ Rủi ro M365 | Hậu quả Thực tế |
| :--- | :--- | :--- | :---: | :--- |
| **Footnotes / Endnotes** | `<w:footnote>`, `<w:endnote>`, `word/footnotes.xml` | **0 dòng code** (Không có) | **CRITICAL (C4)** | Mất sạch toàn bộ chú thích chân trang của luận văn, báo cáo, tài liệu luật. |
| **Content Controls (SDT)** | `<w:sdt>`, `<w:sdtPr>`, `<w:sdtContent>` | Bị bỏ qua tại dòng 1665 | **CRITICAL (C2)** | Mất trắng dữ liệu trong các khung nhập liệu, bìa mẫu, trường ngày tháng. |
| **Track Changes & Revisions** | `<w:ins>`, `<w:del>`, `<w:rPrChange>` | Chủ động xóa tại `WriterView:7915` | **CRITICAL (C3)** | Xóa sạch toàn bộ lịch sử theo dõi sửa đổi của đối tác mà không cảnh báo. |
| **Comments / Ghi chú** | `<w:comment>`, `word/comments.xml` | **0 dòng code** (Không có) | **MAJOR** | Toàn bộ bình luận phê duyệt hợp đồng bị biến mất hoàn toàn. |
| **Toán học OMML** | `<m:oMath>`, `<m:oMathPara>`, `<m:f>` | Ép sang text Unicode tại dòng 5407 | **MAJOR** | Công thức toán chuyên nghiệp bị duỗi phẳng thành ký tự chữ thường thô sơ. |


---

## 4. PHÂN HỆ R2: SPREADSHEETML (ZENQ CALC - .XLSX)

SpreadsheetML là ngôn ngữ đánh dấu bảng tính được quy định tại **ISO/IEC 29500-1 Clause 18** và tài liệu kỹ thuật của Microsoft `[MS-XLSX]`.

### 4.1. Sơ đồ Kiến trúc & Phân bố Mã nguồn
Trái ngược với suy đoán ban đầu rằng toàn bộ logic nằm trong `FormatHandlers.cs`, phân hệ bảng tính ZenQ Calc được tổ chức tách biệt thành một hệ sinh thái mô-đun hóa chuyên nghiệp:
- **Bộ chuyển đổi Định dạng OpenXML**:
  - `src/format/XlsxHandler.cs` (2.832 dòng, 157 KB): Bộ giải mã (Load) và tuần tự hóa (Save) toàn bộ cấu trúc tệp `.xlsx`.
  - `src/format/XlsxHandler.hpp` (115 dòng): Nhân C++ header-only hỗ trợ nạp XML worksheet thô cho các tác vụ native.
  - `src/format/XlsBiff8Handler.cs` (285 dòng): Bộ nạp tệp nhị phân Excel cũ (.xls BIFF8).
  - `src/format/OdsHandler.cs` (290 dòng): Bộ nạp/lưu định dạng OpenDocument Spreadsheet (.ods).
- **Giao diện & Tương tác Bảng tính**:
  - `src/ui/CalcView.cs` (9.381 dòng, 430 KB): Quản lý ma trận ô, vùng chọn, cuộn mượt, kéo fill handle, bộ nhớ đệm font/brush, trạng thái Freeze Panes, vùng in, và hiển thị hình vẽ DrawingML.
  - `src/ui/CalcTableAndPivotDialogs.cs` (550 dòng): Mô hình `ExcelTableInfo` định nghĩa bảng thông minh Office.
  - `src/ui/CalcHeaderFooterForm.cs` (155 dòng): Giao diện thiết lập đầu trang/chân trang.
- **Tầng Lõi Dữ liệu & Động cơ Tính toán (Core Engines)**:
  - `src/core/SparseMatrix.cs` (867 dòng): Cấu trúc dữ liệu ma trận thưa 64-bit khóa ô (`CellData`, `CellFormat`, `CellBorderSettings`), hỗ trợ `MergeCells`, đường kẻ chéo `DiagonalHeader`, và mảng động 365 (`IsSpillOrigin`, `IsSpilledChild`).
  - `src/core/FormulaEngine.cs` (4.568 dòng): Động cơ phân tích cú pháp (Lexer/Parser) và tính toán hơn 250 hàm Excel.
  - `src/core/DataValidationEngine.cs` (489 dòng): Động cơ kiểm tra ràng buộc nhập liệu `DataValidationRule`.

```
                   LUỒNG DỮ LIỆU SPREADSHEETML (ZENQ CALC)
┌─────────────────────────────────────────────────────────────────────────────┐
│ Tệp .xlsx (ZIP Archive)                                                     │
│   ├── [Content_Types].xml                                                   │
│   ├── xl/workbook.xml & xl/_rels/workbook.xml.rels                          │
│   ├── xl/sharedStrings.xml (Bảng chuỗi dùng chung)                          │
│   ├── xl/styles.xml (Bảng font, fill, border, numFmts, cellXfs)             │
│   ├── xl/worksheets/sheet*.xml (Hàng, cột, ô, công thức, mergeCell)          │
│   ├── xl/drawings/drawing*.xml & xl/charts/chart*.xml                       │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Trình nạp XlsxHandler.LoadXlsx (XlsxHandler.cs:308-820)                     │
│   ├── Đọc sharedStrings.xml (316-351)                                       │
│   ├── Đọc styles.xml (353-500) -> MẤT MÀU ĐƯỜNG VIỀN BORDER (421)           │
│   ├── Đọc sheet qua vòng lặp cứng (504-521) -> BỎ QUA WORKBOOK.XML.RELS     │
│   │     └── GẶP SHEET PHI TUẦN TỰ: NGẮT VÒNG LẶP, MẤT TOÀN BỘ SHEET SAU!    │
│   ├── Đọc ô <c> & công thức <f> (635) -> MẤT CÔNG THỨC SHARED FORMULA       │
│   │     └── Ô PHỤ THUỘC <f t="shared"/> BỊ ÉP THÀNH SỐ CHẾT TĨNH!            │
│   └── Nạp vào CalcView.Sheets -> Khởi động FormulaEngine                     │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Trình xuất XlsxHandler.SaveXlsx (XlsxHandler.cs:825-1317)                    │
│   ├── Ghi workbook.xml (898): THIẾU <calcPr fullCalcOnLoad="1">, MẤT NAMES   │
│   ├── Ghi styles.xml (1061): HOÀN TOÀN THIẾU <numFmts> (MẤT NGÀY & TIỀN VN) │
│   ├── Ghi sheetData (1214-1280):                                             │
│   │     ├── Ô Boolean thuần bị ghi thành t="inlineStr" (1265)               │
│   │     ├── Công thức M365 (XLOOKUP...) THIẾU TIỀN TỐ _xlfn. -> HỎNG FILE!  │
│   │     └── 100% Text ghi bằng inlineStr -> KHÔNG XUẤT sharedStrings.xml!   │
│   ├── BỎ RƠI HOÀN TOÀN: Freeze Panes, Data Validation, AutoFilter, Tables   │
│   └── Ghi drawing & charts (2468-2518): BIỂU ĐỒ CHỈ GHI c:numLit TĨNH!       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 4.2. Cấu trúc Sổ làm việc & Quan hệ (Workbook Structure & OPC Packaging)

#### A. Tệp Sổ làm việc `xl/workbook.xml`
- **Hiện trạng Codebase (`XlsxHandler.cs:898-925`)**:
  ```csharp
  sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
  sw.WriteLine("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
  sw.WriteLine("  <sheets>");
  for (int i = 0; i < calcView.Sheets.Count; i++)
  {
      string sName = (i < calcView.SheetNames.Count) ? calcView.SheetNames[i] : ("Sheet" + (i + 1));
      sw.WriteLine(string.Format("    <sheet name=\"{0}\" sheetId=\"{1}\" r:id=\"rId{2}\"/>", EscapeXml(sName), i + 1, i + 1));
  }
  sw.WriteLine("  </sheets>");
  sw.WriteLine("</workbook>");
  ```
- **Lỗ hổng 1: Thiếu Thuộc tính Bắt buộc Tính toán lại (`<calcPr fullCalcOnLoad="1"/>`) - BUG CRITICAL**:
  - Chuẩn ECMA-376 Part 1 §18.2.2 quy định thẻ `<calcPr>` chứa các thuộc tính tính toán của sổ làm việc.
  - Microsoft Excel duy trì một cây phụ thuộc các ô tính toán (`xl/calcChain.xml`). Do ZenQ Calc tính toán độc lập và không xuất `calcChain.xml`, khi mở tệp trên Excel 365 mà không có cờ `fullCalcOnLoad="1"`, Excel 365 có thể **không tự động kích hoạt tính toán lại**. Người dùng sẽ thấy các ô công thức hiển thị kết quả cũ hoặc trắng tinh cho đến khi bấm tổ hợp phím `Ctrl + Alt + F9`.
- **Lỗ hổng 2: Mất Dải ô Đặt tên (Defined Names - `<definedNames>`) - Bug Major**:
  - Trong `src/ui/CalcView.cs` dòng 381 có khai báo `public List<DefinedNameModel> DefinedNames`, cho phép người dùng đặt tên cho vùng dữ liệu (ví dụ đặt tên vùng `A2:A100` là `DoanhThu`).
  - Trong `XlsxHandler.cs`, cả hai hàm `LoadXlsx` và `SaveXlsx` **hoàn toàn không đọc và không ghi thẻ `<definedNames>`**. Toàn bộ các công thức Excel sử dụng tên dải ô (ví dụ `=SUM(DoanhThu)`) khi lưu qua ZenQ Calc và mở trên Excel 365 đều bị lỗi `#NAME?`.
- **Lỗ hổng 3: Thiếu Thuộc tính Thẻ Kích hoạt (`<bookViews>`)**:
  Không ghi `<workbookView activeTab="0"/>`, khiến Excel 365 không ghi nhận được sheet nào đang được kích hoạt trước khi lưu.

#### B. Lỗi Bỏ qua Bảng Quan hệ Sổ làm việc `xl/_rels/workbook.xml.rels` - BUG CRITICAL
- **Vị trí Mã nguồn**: `src/format/XlsxHandler.cs` dòng 504–521:
  ```csharp
  for (int sheetIdx = 1; sheetIdx <= 100; sheetIdx++)
  {
      string sheetPath = string.Format("xl/worksheets/sheet{0}.xml", sheetIdx);
      ZipArchiveEntry wsEntry = archive.GetEntry(sheetPath);
      if (wsEntry == null)
      {
          sheetPath = string.Format("xl/worksheets/Sheet{0}.xml", sheetIdx);
          wsEntry = archive.GetEntry(sheetPath);
      }
      if (wsEntry == null) break; // <--- ĐIỂM SỤP ĐỔ CHÍ MẠNG
      ...
  ```
- **Cơ chế Lỗi Thực tế**:
  Trình nạp **không đọc `xl/_rels/workbook.xml.rels`** để ánh xạ `rId` sang tên tệp thực tế. Thay vào đó, mã nguồn chạy vòng lặp tuần tự từ `sheet1.xml` đến `sheet100.xml`.
  Trong quy trình sử dụng thực tế của Microsoft Excel 365, khi người dùng tạo 3 sheet rồi xóa bỏ Sheet 1, Excel sẽ giữ lại `sheet2.xml` và `sheet3.xml`, đồng thời cập nhật `workbook.xml.rels`:
  `<Relationship Id="rId1" Target="worksheets/sheet2.xml"/>`.
  Khi nạp tệp này vào ZenQ Calc, ở vòng lặp đầu tiên (`sheetIdx = 1`), lệnh tìm `sheet1.xml` trả về `null`. Hệ thống ngay lập tức kích hoạt lệnh `break;` và **kết thúc nạp toàn bộ sổ làm việc**.
  *Hậu quả*: Người dùng bị **mất trắng 100% dữ liệu** của các trang tính còn lại!

---

### 4.3. Bảng Chuỗi Dùng chung (`xl/sharedStrings.xml` & Đánh giá `inlineStr`)

#### Chuẩn ECMA-376 (§18.4) & Tối ưu hóa Dung lượng
SpreadsheetML được thiết kế để xử lý hàng triệu ô dữ liệu. Chuẩn định nghĩa bảng chuỗi dùng chung (Shared Strings Table - SST) trong tệp `xl/sharedStrings.xml`. Khi một chuỗi (như `"Hà Nội"`) xuất hiện tại 10.000 ô khác nhau, chuỗi đó chỉ được lưu 1 lần trong SST tại vị trí index 0, và 10.000 ô trong worksheet chỉ ghi: `<c r="A1" t="s"><v>0</v></c>`.

#### Hiện trạng Codebase ZenQ Calc:
- **Khi nạp (`LoadXlsx:316-351`)**: Hỗ trợ đọc `<sst>` rất tốt, giải mã được cả chuỗi đơn `<si><t>` và chuỗi ghép đoạn văn phong phú `<si><r><t>`.
- **Khi lưu (`SaveXlsx:1278`)**: **HOÀN TOÀN KHÔNG TẠO `xl/sharedStrings.xml`**!
  Mọi chuỗi văn bản được ghi trực tiếp vào từng ô dưới dạng chuỗi cục bộ:
  `<c r="A1" t="inlineStr"><is><t xml:space="preserve">Nội dung</t></is></c>`.
- **Hậu quả & Đánh giá Rủi ro (Bug Major)**:
  - Mặc dù chuẩn ISO/IEC 29500 chấp nhận `inlineStr`, Microsoft Excel 365 luôn ưu tiên và coi Shared Strings là chuẩn mặc định khi lưu tệp.
  - Với các tệp dữ liệu lớn (hàng chục ngàn dòng), việc sử dụng 100% `inlineStr` làm tệp `.xlsx` phình to dung lượng gấp **5 đến 10 lần**, làm tăng thời gian giải nén XML.
  - Nhiều hệ thống xử lý dữ liệu doanh nghiệp của bên thứ ba (như các bộ nạp dữ liệu ETL của SAP, Oracle, Python thư viện `pandas` hoặc các thư viện cũ) chỉ chấp nhận ô kiểu `t="s"` và sẽ báo lỗi hoặc trả về ô trống khi gặp `inlineStr`.

---

### 4.4. Trang tính, Cột, Hàng & Ô Dữ liệu

#### A. Cột `<cols>` & Hàng `<row>`
- **Hiện trạng**:
  - `cols` được xuất tại `XlsxHandler.cs:1187-1196`: Tính toán độ rộng ký tự chuẩn OpenXML `wChars = (px - 5) / 7.5`, xuất đúng `min`, `max`, `width`, `customWidth="1"`.
  - `row` được xuất tại `XlsxHandler.cs:1214-1224`: Tính toán chiều cao chuẩn `htPt = px / 1.333`, xuất `r`, `ht`, `customHeight="1"`.
- **Lỗ hổng Bỏ rơi Thuộc tính Hàng Ẩn (`hidden="1"`) - Bug Major**:
  - Trong `CalcView.cs:402`, hệ thống có HashSet quản lý danh sách hàng ẩn: `public HashSet<uint> HiddenRows`.
  - Khi lưu tệp (`XlsxHandler.cs:1216`), mã nguồn **không hề kiểm tra và không ghi thuộc tính `hidden="1"`** vào thẻ `<row>`.
  - *Hậu quả*: Toàn bộ các hàng bị ẩn thủ công hoặc các hàng bị ẩn do tính năng lọc dữ liệu đều **tự động hiện ra trở lại** khi tệp được mở trên Excel 365.

#### B. Ô `<c>` & Lỗi Xuất Ô Luận lý Tĩnh Thành Text (Static Boolean Bug) - Bug Major
- Chuẩn OOXML quy định ô kiểu Boolean bắt buộc phải mang thuộc tính `t="b"` và giá trị `<v>0</v>` hoặc `<v>1</v>`.
- Tại `SparseMatrix.cs:7`, mô hình có định nghĩa `CellType.Boolean`.
- Tuy nhiên, trong `SaveXlsx` dòng 1260–1280:
  ```csharp
  if (cell.Type == CellType.Formula) { ... }
  else if (cell.Type == CellType.Number) { ... }
  else if (!string.IsNullOrEmpty(cell.RawValue))
  {
      // RƠI VÀO ĐÂY VÌ THIẾU NHÁNH KIỂM TRA cell.Type == CellType.Boolean!
      sw.Write(string.Format(" t=\"inlineStr\"><is><t xml:space=\"preserve\">{0}</t></is></c>", EscapeXml(cell.RawValue)));
  }
  ```
- *Hậu quả trên Excel 365*: Một ô Boolean thuần túy mang giá trị `TRUE` bị xuất thành `<c r="A1" t="inlineStr"><is><t>TRUE</t></is></c>`.
  Trên Excel 365, ô này là một chuỗi văn bản chứ không phải giá trị Boolean thực thụ. Các hàm kiểm tra luận lý `=ISLOGICAL(A1)` trả về `FALSE`, và các công thức điều kiện `=IF(A1, "Đúng", "Sai")` sẽ bị tính toán sai lệch hoàn toàn.

---

### 4.5. Công thức, Tiền tố M365 `_xlfn.` & Chuỗi Tính toán

#### Lỗ hổng 1: Thiếu Tiền tố `_xlfn.` Cho Các Hàm M365 Hiện đại - BUG CRITICAL
- **Nguyên nhân Kỹ thuật**:
  Chuẩn ECMA-376 ấn bản đầu tiên (2006) chỉ định nghĩa danh mục hàm của Excel 2007. Khi Microsoft bổ sung các hàm mới qua các phiên bản Excel 2010, 2013, 2016, 2019 và Microsoft 365 (như `XLOOKUP`, `SORT`, `FILTER`, `UNIQUE`, `SEQUENCE`, `TEXTJOIN`, `IFS`, `CONCAT`), chuẩn ISO/IEC 29500 mở rộng và đặc tả `[MS-XLSX]` quy định **tất cả các hàm này bắt buộc phải được gắn tiền tố `_xlfn.`** khi lưu vào XML của ô `<f>`.
- **Khảo sát Codebase (`XlsxHandler.cs:2313-2336`)**:
  Hàm `NormalizeExcelFormula` hiện tại chỉ làm duy nhất một thao tác đổi dấu chấm phẩy `;` thành dấu phẩy `,`:
  ```csharp
  private static string NormalizeExcelFormula(string formula)
  {
      if (string.IsNullOrEmpty(formula)) return "";
      StringBuilder sb = new StringBuilder(formula.Length);
      // Chỉ thay thế ';' thành ','
      return sb.ToString();
  }
  ```
- **Hậu quả Trực tiếp khi Mở trên Microsoft Excel 365**:
  Khi tệp chứa công thức `=XLOOKUP(A2, B2:B10, C2:C10)` được lưu mà không có tiền tố `_xlfn.`, Excel 365 không thể nhận diện đây là hàm nội tại, xem đây là một hàm do người dùng tự viết (UDF) bị lỗi cú pháp, và kích hoạt ngay hộp thoại cảnh báo:
  > *"We found a problem with some content in 'filename.xlsx'. Do you want us to try to recover as much as we can? If you trust the source of this workbook, click Yes."*
  Khi người dùng nhấn "Yes", Excel sẽ xóa bỏ công thức và biến ô thành `=#NAME?`.

#### Lỗ hổng 2: Phá Hủy Công thức Dùng chung (`<f t="shared">`) khi Nạp Tệp - BUG CRITICAL
- **Đặc tả Chuẩn OOXML (§18.3.1.40)**:
  Khi một công thức được kéo thả qua một vùng ô (ví dụ từ C2 đến C1000), Excel tối ưu hóa bằng cách định nghĩa ô chủ:
  `<c r="C2"><f t="shared" ref="C2:C1000" si="0">A2+B2</f><v>30</v></c>`.
  Và tại 998 ô còn lại (từ C3 đến C1000), Excel chỉ lưu thẻ rỗng:
  `<c r="C3"><f t="shared" si="0"/><v>45</v></c>`.
- **Khảo sát Trình nạp (`XlsxHandler.cs:635-649`)**:
  ```csharp
  if (fNode != null && !string.IsNullOrEmpty(fNode.InnerText))
  {
      // Xử lý công thức của ô C2...
      sheet.SetCellText(r, c, "=" + rawFormula);
  }
  else if (cellType == "s" && vNode != null) { ... }
  else if (vNode != null)
  {
      // Ô C3 RƠI VÀO ĐÂY VÌ fNode.InnerText RỖNG!
      sheet.SetCellText(r, c, vNode.InnerText);
  }
  ```
- **Hậu quả Thảm khốc**: Do `fNode.InnerText` của ô C3 rỗng, điều kiện kiểm tra công thức bị bỏ qua! Ô C3 bị gán giá trị tĩnh `45` từ thẻ `<v>`. **Toàn bộ 998 ô còn lại trong dải công thức đều bị biến thành số chết**, mất hoàn toàn tính năng tự động tính toán khi nạp qua ZenQ Calc!

#### Lỗ hổng 3: Xung đột Tràn Mảng Động (Dynamic Array Spilling Collision) - Bug Major
- Khi thực thi các hàm mảng động của Excel 365 (như `=SORT(A1:A5)` tại ô C1 tràn kết quả xuống C2:C5), nếu `SaveXlsx` xuất cả công thức tại C1 lẫn giá trị text tĩnh tại các ô C2:C5, khi Excel 365 mở tệp, nó phát hiện các ô C2:C5 đã bị chiếm dụng bởi dữ liệu tĩnh, dẫn đến việc công thức tại C1 bị văng lỗi `#SPILL!`.

---

### 4.6. Kiểu dáng Hiển thị & Định dạng Số (Styles: `xl/styles.xml`)

Tệp `xl/styles.xml` định nghĩa bảng các phần tử định dạng:
`<styleSheet>` -> `<numFmts>` -> `<fonts>` -> `<fills>` -> `<borders>` -> `<cellXfs>`.

#### Khảo sát Hiện trạng Codebase (`XlsxHandler.cs:1061-1169`):
1. **Hoàn Toàn Thiếu Bảng Định dạng Số Tùy biến `<numFmts>` - Bug Major**:
   - *Chuẩn OOXML (§18.8.30)*: Các mã định dạng tích hợp sẵn chỉ có ID từ 0 đến 163. Mọi định dạng tùy biến (Custom formats) bắt buộc phải có `numFmtId >= 164` và phải được khai báo trong `<numFmts><numFmt numFmtId="164" formatCode="..."/>`.
   - *Codebase*: `SaveXlsx` **hoàn toàn không tạo thẻ `<numFmts>`**! Nó chỉ ánh xạ cứng sang một số ID mặc định (`0`: General, `1-2`: Number, `9-10`: Percent, `14`: Date mm-dd-yy, `44`: Currency USD `$`). Trình đọc `LoadXlsx:678` cũng hoàn toàn không nạp `<numFmts>`.
   - *Hậu quả Thực tế*:
     - Định dạng ngày tháng chuẩn Việt Nam / Châu Âu (`dd/mm/yyyy`, `dd/MM/yyyy HH:mm`) bị mất hoàn toàn và rơi về định dạng của hệ thống máy tính.
     - Định dạng tiền tệ Đồng Việt Nam (`#,##0 "₫"`) bị ép về ID 44 của đồng Đô la Mỹ (`$`).
     - Định dạng số có nhiều chữ số thập phân, định dạng mã bưu chính, căn lề số âm bằng ngoặc đơn đỏ bị xóa sạch khi lưu.
2. **Mất Màu Sắc Đường Viền Khi Nạp Tệp (Border Color Loss)**:
   Tại `XlsxHandler.cs:421-425`, khi nạp bảng `<borders>`, mã nguồn chỉ đọc thuộc tính `style` (`thin`, `medium`) mà **hoàn toàn bỏ qua thẻ màu `<color rgb="..."/>`**. Kết quả là mọi đường viền bảng tính màu xanh, đỏ, xám của Excel khi mở trong ZenQ Calc đều bị biến thành màu đen mặc định.
3. **Mất Kiểu Gạch Ngang Chữ (`<strike/>`)**:
   Lớp `CellFormat` trong `SparseMatrix.cs:64` có thuộc tính `Strikethrough`, nhưng lớp `XlsxFont` (`XlsxHandler.cs:227`) không khai báo trường này, dẫn đến không thể lưu hoặc nạp kiểu gạch ngang chữ trong bảng tính.

---

### 4.7. Tính năng Bảng tính Nâng cao

#### A. Cố định Dòng & Cột (Freeze Panes - `<pane>`) - Bug Major
- Chuẩn OOXML (§18.3.1.66) quy định trạng thái cố định dòng/cột nằm trong thẻ `<sheetViews><sheetView><pane ySplit="1" topLeftCell="A2" state="frozen"/></sheetView></sheetViews>`.
- Trong `CalcView.cs:241-242` có các cờ `IsFreezeTopRow` và `IsFreezeFirstCol`.
- Trong `XlsxHandler.cs`: **0 dòng code đọc hoặc ghi thẻ `<pane>`**. Người dùng thiết lập cố định tiêu đề bảng tính khi mở trên Excel sẽ thấy bảng bị trôi tự do khi cuộn chuột.

#### B. Xác thực Dữ liệu (Data Validation - `<dataValidation>`) - Bug Major
- Trong `CalcView.cs:376` có `public List<DataValidationRule> DataValidations` và cả một động cơ hoàn chỉnh `src/core/DataValidationEngine.cs` hỗ trợ toàn bộ các quy tắc (WholeNumber, Decimal, List dropdown, Date, TextLength, Custom).
- Trong `XlsxHandler.cs`: **0% hỗ trợ** (không đọc, không ghi). Mọi danh sách chọn Dropdown và các quy tắc kiểm tra nhập liệu bị biến mất khi lưu tệp.

#### C. Bảng Thông minh & Tự động Lọc (AutoFilter & Tables) - Bug Major
- `CalcView.cs:401` có `public List<ExcelTableInfo> Tables` và giao diện quản lý bảng trong `CalcTableAndPivotDialogs.cs`.
- Trong `XlsxHandler.cs`: **0% hỗ trợ**. Không ghi nhận `<autoFilter ref="..."/>`, không tạo thư mục `xl/tables/table1.xml`. Các nút lọc Dropdown ở dòng tiêu đề và kiểu dáng sọc màu thông minh biến mất khi lưu tệp.

#### D. Đứt Gãy Liên kết Dữ liệu Biểu đồ (Disconnected Chart Series Bug) - Bug Major
- Khảo sát hàm `WriteChartXml` (`XlsxHandler.cs:2468-2518`):
  Dữ liệu biểu đồ được ghi hoàn toàn dưới dạng danh sách giá trị số tĩnh (`<c:numLit>`) và nhãn tĩnh (`<c:strLit>`):
  ```xml
  <c:val>
    <c:numLit>
      <c:formatCode>General</c:formatCode>
      <c:ptCount val="3"/>
      <c:pt idx="0"><c:v>100</c:v></c:pt>
      <c:pt idx="1"><c:v>200</c:v></c:pt>
    </c:numLit>
  </c:val>
  ```
- Chuẩn Excel bắt buộc biểu đồ liên kết bảng tính phải có thẻ tham chiếu công thức:
  ```xml
  <c:val>
    <c:numRef>
      <c:f>Sheet1!$B$2:$B$4</c:f>
      <c:numCache>...</c:numCache>
    </c:numRef>
  </c:val>
  ```
- *Hậu quả*: Biểu đồ bị xuất dưới dạng "ảnh số chết" (bị ngắt kết nối với bảng tính). Khi người dùng chỉnh sửa dữ liệu trong các ô A2:B4 trên Microsoft Excel 365, biểu đồ không hề tự động cập nhật.

---

## 5. PHÂN HỆ R3: PRESENTATIONML (ZENQ SLIDES - .PPTX)

PresentationML là ngôn ngữ đánh dấu bài thuyết trình được quy định tại **ISO/IEC 29500-1 Clause 19** và tài liệu kỹ thuật Microsoft `[MS-PPTX]`.

### 5.1. Sơ đồ Kiến trúc & Phân bố Mã nguồn
- **Bộ xử lý Định dạng chính**: `src/format/FormatHandlers.cs` (khối `PptxHandler`, dòng 7769 đến 12653).
- **Mẫu Gói Nền Base64**: `src/format/PptxTemplate.cs` (30 dòng, chứa mẫu tệp ZIP OpenXML hợp lệ được mã hóa chuỗi Base64).
- **Giao diện Trình chiếu**: `src/ui/SlidesView.cs`.
- **Mô hình Dữ liệu Slide Cốt lõi**: `src/core/SlideModel.cs` (`PresentationDocument`, `SlideNode`, `ShapeNode`, `Transform2D`, `SlideLayoutType`, `TransitionType`, `AnimationEffect`).

```
                    LUỒNG DỮ LIỆU PRESENTATIONML (ZENQ SLIDES)
┌─────────────────────────────────────────────────────────────────────────────┐
│ Khởi tạo Gói từ PptxTemplate.cs (BASE64_TEMPLATE)                           │
│   ├── [Content_Types].xml & _rels/.rels                                     │
│   ├── ppt/presentation.xml & ppt/_rels/presentation.xml.rels                │
│   ├── ppt/slideMasters/slideMaster1.xml                                     │
│   ├── ppt/slideLayouts/slideLayout1.xml .. slideLayout11.xml                │
│   └── ppt/theme/theme1.xml                                                  │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Trình xuất PptxHandler.SavePptx (FormatHandlers.cs:7769-8206)                │
│   ├── Xuất presentation.xml (7913): SAI LỆCH KÍCH THƯỚC EMU (20" x 11.25")  │
│   ├── Ghi slide{i}.xml.rels (8143): ÉP CỨNG slideLayout1.xml CHO MỌI SLIDE  │
│   ├── Ghi ghi chú (8144-8186): TẠO notesSlide*.xml NHƯNG QUÊN TẠO .RELS     │
│   │     └── GÂY HỎNG TỆP NGHIÊM TRỌNG TRÊN M365 (FILE REPAIR PROMPT!)       │
│   ├── Xuất hình dạng WriteSlideXml (8325-8823):                             │
│   │     ├── San phẳng nhóm hình dạng <p:grpSp> thành các <p:sp> đơn lẻ      │
│   │     ├── MẤT TRẮNG DỮ LIỆU BIỂU ĐỒ (ShapeType.Chart biến thành <p:sp>)   │
│   │     ├── Bỏ rơi thẻ <p:ph> (Hỏng Master Slide, Outline View, AI Designer)│
│   │     └── DrawingML: Bỏ rơi flipH, flipV, prstDash, headEnd/tailEnd       │
│   └── Khối Catch (8204): GỌI SavePptxXml GHI XML TRẦN ĐUÔI .PPTX -> HỎNG FILE│
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 5.2. Cấu trúc Bản trình chiếu & Tỷ lệ Màn hình

#### A. Sai lệch Kích thước Màn hình 16:9 Chuẩn OpenXML
- **Khảo sát Codebase (`FormatHandlers.cs:7790-7793` & `7913`)**:
  ```csharp
  long cxEmu = (long)(pres.CanvasWidth * 9525.0f);  // 1920 * 9525 = 18.288.000 EMU
  long cyEmu = (long)(pres.CanvasHeight * 9525.0f); // 1080 * 9525 = 10.287.000 EMU
  sw.WriteLine(string.Format("  <p:sldSz cx=\"{0}\" cy=\"{1}\" type=\"{2}\"/>", cxEmu, cyEmu, isWide ? "screen16x9" : "screen4x3"));
  ```
- **Sai lệch so với Chuẩn ECMA-376 / M365**:
  Chuẩn Microsoft Office 365 quy định kích thước chuẩn của màn hình rộng 16:9 (`screen16x9`) là:
  - Office 2013/365: `cx="12192000"` (13.333 inches) và `cy="6858000"` (7.5 inches).
  - Việc ghi kích thước `18.288.000 x 10.287.000 EMU` tương đương kích thước trang giấy khổng lồ **20 inches x 11.25 inches** (gấp 1.5 lần chuẩn).
- *Hậu quả trên PowerPoint 365*: Trong hộp thoại *Slide Size / Page Setup*, PowerPoint báo kích thước tùy chỉnh (*Custom*) thay vì *Widescreen (16:9)*. Khi người dùng in ấn hoặc xuất bản trình chiếu sang định dạng PDF từ PowerPoint, tỷ lệ trang giấy bị co giãn sai lệch và mép lề bị cắt xén.

#### B. Mã Chết Không Được Tích Hợp (Dead Code)
Các hàm `WriteThemeXml` (dòng 8208), `WriteSlideMasterXml` (dòng 8241), `WriteSlideLayoutXml` (dòng 8261) được xây dựng sẵn trong `FormatHandlers.cs` nhưng **hoàn toàn không được gọi** trong luồng `SavePptx`. Theme và Master trong tệp xuất ra hoàn toàn bị đóng băng theo tệp mẫu Base64 tĩnh, làm vô hiệu hóa khả năng thay đổi Theme màu từ giao diện ZenQ Slides.

---

### 5.3. Lỗi Hỏng Tệp Ghi chú Diễn giả (`ppt/notesSlides/`) - BUG CRITICAL C1

Đây là phát hiện điều tra có tính chất nghiêm trọng nhất đối với sự toàn vẹn của tệp `.pptx`.

#### Cơ chế Phát sinh Lỗi trong Codebase:
Tại `FormatHandlers.cs` dòng 8144–8147 và 8159–8186:
1. Tại dòng 8146, trong tệp quan hệ slide `ppt/slides/_rels/slide{i}.xml.rels`, hệ thống tạo liên kết:
   `<Relationship Id="rIdNotes1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/notesSlide" Target="../notesSlides/notesSlide{i}.xml"/>`.
2. Tại dòng 8164, hệ thống tạo entry ZIP `ppt/notesSlides/notesSlide{i}.xml` và ghi nội dung XML hợp lệ chứa ghi chú của diễn giả.
3. **TUY NHIÊN, HỆ THỐNG HOÀN TOÀN QUÊN TẠO TỆP QUAN HỆ CỦA NOTES**: `ppt/notesSlides/_rels/notesSlide{i}.xml.rels`!
4. **VÀ KHÔNG KIỂM TRA SỰ TỒN TẠI CỦA**: `ppt/notesMasters/notesMaster1.xml`!

#### Hậu quả Trực tiếp trên Microsoft Office 365:
Theo đặc tả ECMA-376 Part 1 §13.3.4, phần `notesSlide` bắt buộc phải có một tệp quan hệ tương ứng trỏ ngược lại slide chủ (`Target="../slides/slide{i}.xml"`) và trỏ tới slide ghi chú mẫu (`Target="../notesMasters/notesMaster1.xml"`).
Khi người dùng mở một bài thuyết trình có ghi chú diễn giả trong Microsoft PowerPoint 365:
- Trình kiểm tra tính hợp lệ OPC của PowerPoint quét gói tệp, phát hiện phần `notesSlide` bị mồ côi (dangling part không có liên kết quan hệ 2 chiều).
- PowerPoint lập tức hiển thị hộp thoại cảnh báo lỗi nghiêm trọng:
  > **"PowerPoint found a problem with content in presentation.pptx. PowerPoint can attempt to repair the presentation. If you trust the source of this presentation, click Repair."**
- Khi người dùng bấm "Repair", cơ chế tự sửa lỗi của PowerPoint sẽ **xóa bỏ hoàn toàn phần `notesSlide`**, dẫn đến việc diễn giả bị mất sạch toàn bộ ghi chú thuyết trình!

---

### 5.4. Slide Master, Slide Layout & Kế thừa Giữ chỗ (Placeholders)

#### A. Ép Cứng Toàn bộ Slide vào `slideLayout1.xml` (Bug Major M1)
- Tại `FormatHandlers.cs` dòng 8143:
  ```csharp
  sw.WriteLine("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout\" Target=\"../slideLayouts/slideLayout1.xml\"/>");
  ```
- *Thực tế*: Mẫu `PptxTemplate.cs` cung cấp đủ 11 slide layout chuẩn (`TitleSlide`, `TitleAndContent`, `SectionHeader`, `TwoContent`, `Comparison`, `TitleOnly`, `Blank`, v.v.). Tuy nhiên, khi xuất tệp, **100% các slide đều bị ép cứng liên kết tới `slideLayout1.xml`** (Layout Title Slide).
- *Hậu quả*: PowerPoint 365 quản lý sai lệch bố cục; khi người dùng chuyển chế độ xem bố cục (*Layout*), PowerPoint không nhận diện đúng slide nào là tiêu đề, slide nào là nội dung 2 cột.

#### B. Hoàn Toàn Bỏ Rơi Thẻ Giữ Chỗ Placeholder (`<p:ph>`) - Bug Major M2
- Trong hàm `WriteSlideXml` (dòng 8522–8560), khi xuất các hộp tiêu đề, phụ đề và nội dung:
  ```csharp
  sw.WriteLine("      <p:sp>");
  sw.WriteLine("        <p:nvSpPr>");
  sw.WriteLine(string.Format("          <p:cNvPr id=\"{0}\" name=\"{1}\"/>", spId, EscapeXml(shapeName)));
  sw.WriteLine("          <p:cNvSpPr/>");
  sw.WriteLine("          <p:nvPr/>"); // <--- HOÀN TOÀN KHÔNG CÓ THẺ <p:ph> !
  sw.WriteLine("        </p:nvSpPr>");
  ```
- **Hậu quả trên Hệ sinh thái Microsoft 365**:
  1. **Tê liệt Tính năng PowerPoint Designer (AI Design Ideas)**:
     Bộ máy trí tuệ nhân tạo Designer của Office 365 quét cấu trúc slide dựa trên thẻ `<p:ph type="title"/>` và `<p:ph type="body"/>` để tự động bố cục hình ảnh và gợi ý thiết kế. Do thiếu `<p:ph>`, nút Designer bị xám mờ hoặc đưa ra gợi ý vô nghĩa.
  2. **Hỏng Dạng xem Dàn bài (Outline View)**: Cột mục lục dàn bài bên trái của PowerPoint hiển thị trống trơn vì không tìm thấy tiêu đề mang thuộc tính placeholder.
  3. **Phá vỡ Tính năng Đặt lại Slide (Reset Slide)**: Khi người dùng nhấp nút *Reset Slide*, PowerPoint sẽ chèn đè các khung placeholder mặc định mới tinh lên trên mặt chữ cũ, tạo ra hiện tượng chữ đè chữ.

---

### 5.5. Cây Hình dạng Trang chiếu (`<p:spTree>`)

#### A. San Phẳng Nhóm Hình dạng (`<p:grpSp>`) - Bug Major M5
- Trình đọc `ExtractGroupShapes` (dòng 11487) hỗ trợ rất tốt việc đọc đệ quy các nhóm hình dạng lồng nhau và giải ma trận tỷ lệ `parentScaleX/Y`.
- Tuy nhiên, trong hàm xuất `WriteSlideXml`, **hoàn toàn không có mã xuất thẻ `<p:grpSp>`**.
- Toàn bộ các đối tượng hình vẽ nằm trong nhóm bị san phẳng (flattened) và xuất ra dưới dạng các hình dạng `<p:sp>` độc lập trên cùng cấp độ slide. Người dùng trên PowerPoint 365 không thể chọn, di chuyển hay co giãn cả nhóm đối tượng cùng lúc.

#### B. Mất Trắng Dữ liệu Biểu đồ (`ShapeType.Chart`) - BUG CRITICAL C3
- **Vị trí Mã nguồn**: `FormatHandlers.cs` dòng 8440–8520.
- **Thực trạng**:
  Trình đọc `LoadPptx` có hàm `ParseOpenXmlChart` (dòng 11915) đọc được biểu đồ cột, thanh, đường, tròn, donut.
  **Tuy nhiên, trong `WriteSlideXml`, hoàn toàn không có khối mã nào xuất `<c:chart>` cho Slide**!
  Khi slide chứa đối tượng `ShapeType.Chart`, nó bị rơi vào nhánh xử lý hình khối thông thường (`Case C: Shapes & Text Boxes`), và được ghi ra XML dưới dạng một hình chữ nhật rỗng hoặc text box không có nội dung.
- *Hậu quả*: **100% dữ liệu biểu đồ bị mất trắng** khi lưu bài thuyết trình sang định dạng `.pptx`.

#### C. Bảng biểu PresentationML (`<p:graphicFrame>` chứa `<a:tbl>`)
- Đọc/ghi cấu trúc khung đồ họa `<p:graphicFrame>` rất chuẩn mực.
- **Hạn chế**:
  - Không hỗ trợ gộp ô ngang/dọc (`gridSpan`, `rowSpan`, `hMerge`, `vMerge`) trong `<a:tc>`, làm vỡ ma trận bảng phức tạp.
  - Không ghi đường viền ô `<a:tcPr>` (dòng 8503), làm bảng biểu bị mất toàn bộ màu sắc và nét viền trên M365.
  - Ép cứng căn lề dọc `anchor="ctr"` (dòng 8492), bỏ qua căn trên (`t`) hoặc căn dưới (`b`).

---

### 5.6. Hiệu ứng Chuyển tiếp, Hoạt ảnh & Đa phương tiện

#### A. Hiệu ứng Chuyển tiếp Slide (`<p:transition>`)
- **Điểm sáng Công nghệ**:
  - Hỗ trợ xuất sắc hiệu ứng **Morph** của Office 365 thông qua khối `mc:AlternateContent` và namespace `http://schemas.microsoft.com/office/powerpoint/2015/09/main` (`p159:morph option="byObject"`).
  - Hỗ trợ đầy đủ 28 hiệu ứng chuyển tiếp Office 2010 (`p14:flash`, `p14:curtains`, `p14:wind`, `p14:airplane`, `p14:origami`, `p14:honeycomb`, `p14:ripple`, `p14:glitter`, `p14:vortex`, v.v.).
  - Hỗ trợ 18 hiệu ứng chuyển tiếp cơ bản ECMA-376.
- **Hạn chế**: Cố định thời lượng Morph `p14:dur="2000"` (2 giây), bỏ qua giá trị `slide.TransitionDuration` thực tế. Chưa liên kết âm thanh chuyển cảnh `slide.TransitionSound` với `<p:snd>`.

#### B. Hoạt ảnh Đối tượng (`<p:timing>` & `<p:bldLst>`)
- Đã xuất thành công cây thời gian chuẩn PresentationML với nút gốc `<p:timing>`, tuần tự `<p:seq nodeType="mainSeq">`, và nút hiệu ứng `<p:animEffect>`.
- **Hạn chế**:
  - Chỉ xuất duy nhất thẻ `<p:animEffect>`. Các hoạt ảnh xoay Spin (`<p:animRot>`), co giãn Grow/Shrink (`<p:animScale>`), đổi màu (`<p:animClr>`), và chuyển động theo quỹ đạo Motion Path (`<p:animMotion>`) hoàn toàn không được xuất, làm các hoạt ảnh phức tạp bị biến mất hoặc thoái hóa thành Fade.
  - Thiếu thẻ phân đoạn dàn bài `<p:bldLst><p:bldP spid="..." build="p"/></p:bldLst>`, khiến toàn bộ các gạch đầu dòng của khối văn bản xuất hiện cùng một lúc thay vì từng dòng một.

#### C. Tệp Đa phương tiện Nhúng (Audio / Video)
- Trích xuất và lưu tệp nhị phân media chuẩn xác vào thư mục `ppt/media/` với cấp độ không nén (`CompressionLevel.NoCompression`).
- Ghi chuẩn quan hệ kép: quan hệ chuẩn ISO/IEC 29500 và quan hệ Office 2010 (`http://schemas.microsoft.com/office/2007/relationships/media`), kèm thẻ `<p14:media>`.
- Thiếu các thuộc tính phát nâng cao trong `<p:timing>`: phát toàn màn hình (`fullScrn="1"`), lặp vô tận (`loop="1"`), ẩn khi không phát (`hide="1"`).

---

## 6. PHÂN HỆ R4: SHARED DRAWINGML (UNIVERSAL VECTOR & VISUAL FX)

DrawingML (`xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"`) là khung đồ họa vector thống nhất được định nghĩa tại **ISO/IEC 29500-1 Clause 20**, dùng chung trên cả ba phân hệ:
- WordprocessingML: `<w:drawing>` -> `<wp:inline>` / `<wp:anchor>` -> `<a:graphic>` -> `<wps:wsp>`
- SpreadsheetML: `xl/drawings/drawing*.xml` -> `<xdr:twoCellAnchor>` -> `<xdr:sp>`
- PresentationML: `ppt/slides/slide*.xml` -> `<p:spTree>` -> `<p:sp>`

### 6.1. Khung Đồ họa Dùng chung & Hệ Tọa độ Biến đổi 2D (`<a:xfrm>`)
- **Vị trí Mã nguồn**: `FormatHandlers.cs` dòng 8402–8409, dòng 8562–8569; `XlsxHandler.cs` dòng 1340–1360; `SlideModel.cs` dòng 371–416 (`Transform2D`).
- **Đơn vị Đo lường EMU (English Metric Units)**:
  OOXML sử dụng đơn vị EMU để triệt tiêu sai số số học dấu phẩy động (1 inch = 914.400 EMU; 1 cm = 360.000 EMU; 1 pt = 12.700 EMU; 1 px tại 96 DPI = 9.525 EMU).
  ZenQ Office thực hiện việc quy đổi EMU rất chuẩn xác tại cả 3 phân hệ.

#### Bảng Đối chiếu Thẻ Biến đổi DrawingML:
| Thuộc tính / Thẻ OOXML | Ý nghĩa Chuẩn ECMA-376 | Đọc (Load) | Ghi (Save) | Mức Tuân thủ | Đánh giá & Rủi ro |
| :--- | :--- | :---: | :---: | :---: | :--- |
| `<a:off x="..." y="..."/>` | Tọa độ điểm gốc (Top-Left) tính bằng EMU | ✅ Có | ✅ Có | **100%** | Hoạt động chính xác |
| `<a:ext cx="..." cy="..."/>` | Chiều rộng và chiều cao tính bằng EMU | ✅ Có | ✅ Có | **100%** | Hoạt động chính xác |
| `<a:xfrm rot="...">` | Góc xoay hình (đơn vị 1/60.000 độ) | ✅ Có | ✅ Có | **100%** | Đã nhân góc 60.000 chuẩn |
| `<a:xfrm flipH="1">` | **Lật ngang hình vẽ (Horizontal Flip)** | ✅ Có | ❌ **BỎ QUA** | **0% (Major M3)** | **Hình lật bị quay ngược lại** |
| `<a:xfrm flipV="1">` | **Lật dọc hình vẽ (Vertical Flip)** | ✅ Có | ❌ **BỎ QUA** | **0% (Major M3)** | **Hình lật bị quay ngược lại** |
| `<a:chOff>`, `<a:chExt>` | Tọa độ và kích thước con trong nhóm | ✅ Có | ❌ Không ghi | **0% (Major M5)** | Mất gốc nhóm con |

> **Phát hiện Trọng điểm (Bug Major M3)**: Mô hình `Transform2D` có đủ hai thuộc tính `FlipHorizontal` và `FlipVertical`. Trình nạp `LoadPptx` đọc thuộc tính này rất chuẩn xác tại dòng 10235–10240. Nhưng trong toàn bộ các hàm ghi `<a:xfrm>` tại dòng 8402 và 8562, hai thuộc tính `flipH="1"` và `flipV="1"` **hoàn toàn không được in ra XML**!
> Bất kỳ hình vẽ mũi tên chỉ hướng, icon minh họa, hay hình ảnh nào được người dùng lật gương trong ZenQ Office khi mở trên Microsoft Office 365 đều bị **quay ngược trở lại trạng thái ban đầu**.

---

### 6.2. Hình học Định sẵn & Đường vẽ Tự do (`<a:prstGeom>` & `<a:custGeom>`)

#### A. Hình dạng Định sẵn (`<a:prstGeom>`)
- Hệ thống hỗ trợ 38 hình học định sẵn qua hàm `GetOpenXmlGeomName` (`FormatHandlers.cs:9124-9168`): `rect`, `roundRect`, `ellipse`, `triangle`, `diamond`, `star5`, `rightArrow`, `chevron`, `heart`, `cloud`, `sun`, `cube`, v.v.
- **Lỗ hổng Bỏ rơi Danh sách Tham số Điều chỉnh (`<a:avLst>`) - Bug Major**:
  Tại dòng 8578, mã nguồn luôn ghi rỗng:
  `<a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>`.
  Trong khi đó, mô hình `ShapeNode` có thuộc tính `CornerRadius` và `DocShapeObject` có các thuộc tính `AdjustValue1`, `AdjustValue2`, `AdjustValue3` (dùng để kéo chỉnh độ cong góc bo, độ vát mũi tên, độ dày mặt cười).
  Do không ghi `<a:gd name="adj" fmla="val ..."/>` vào `<a:avLst>`, khi mở trên Office 365, PowerPoint/Word sẽ tự áp dụng giá trị mặc định của hình học, làm độ bo góc bị nhảy về 16.67% bất kể người dùng đã căn chỉnh tỉ mỉ ra sao.

#### B. Đường vẽ Hình học Tự do (`<a:custGeom>`)
- Trong hàm `WriteCustomPathXml` (`FormatHandlers.cs:8838-8881`): Hỗ trợ tốt `<a:pathLst>`, `<a:moveTo>`, `<a:lnTo>`, `<a:cubicBezTo>`, `<a:close>`.
- **Lỗi Cú pháp DrawingML Nguy cơ Cao**:
  Dòng 8842 ghi cố định: `<a:rect l="l" t="t" r="r" b="b"/>` trong khi danh sách guide `<a:gdLst/>` lại để trống. Theo đặc tả ISO/IEC 29500-1 §20.1.9.22, các thuộc tính `l`, `t`, `r`, `b` trong `<a:rect>` tham chiếu tới biến tính toán hình học trong `<a:gdLst>`. Nếu không khai báo biến `l, t, r, b`, một số bộ dựng hình OpenXML nghiêm ngặt sẽ báo lỗi phân giải cú pháp hình học.
- **Thiếu Lệnh Vẽ Cung tròn & Neo Thông minh**: Thiếu `<a:quadBezTo>`, `<a:arcTo>`, và danh sách điểm kết nối tự hút mũi tên `<a:cxnLst>`.

---

### 6.3. Hệ thống Tô màu & Chuyển sắc (Fills & Gradients)

1. **Tô Đơn sắc (`<a:solidFill>`)**:
   - Hỗ trợ tốt màu RGB (`<a:srgbClr val="RRGGBB"/>`) và độ trong suốt (`<a:alpha val="..."/>`).
   - Phía bộ đọc hỗ trợ giải mã màu chủ đề (`<a:schemeClr>`) kèm các biến điệu độ sáng `lumMod`, sắc độ `satMod`, phủ màu `tint`.
   - *Hạn chế*: Khi ghi, hệ thống luôn ép sang mã màu tuyệt đối `srgbClr`, làm đứt gãy liên kết với bảng màu chủ đề (Theme). Khi người dùng đổi Theme trong Office 365, các hình vẽ không tự động đổi màu theo.
2. **Tô Chuyển sắc (`<a:gradFill>`) - Bug Major M7**:
   - Hỗ trợ danh sách điểm dừng đa điểm (`<a:gsLst><a:gs pos="...">`) với độ trong suốt alpha độc lập.
   - **Hạn chế Nghiêm trọng**: Bộ ghi **chỉ xuất kiểu chuyển sắc tuyến tính** (`<a:lin ang="..."/>`).
     Thuộc tính `GradientType` trong `SlideModel.cs` hỗ trợ `Radial` (xuyên tâm), `Rectangular` (chữ nhật), và `Path` (đường dẫn), nhưng khi xuất ra XML tại dòng 8602, tất cả đều bị ép thành tuyến tính. Thẻ chuyển sắc xuyên tâm `<a:path path="circle"><a:fillToRect l="50000" t="50000" r="50000" b="50000"/></a:path>` hoàn toàn chưa được hỗ trợ.
3. **Tô Hình ảnh Nhúng (`<a:blipFill>`)**:
   - Hỗ trợ tốt quan hệ nhúng `r:embed`, chế độ kéo giãn `<a:stretch><a:fillRect/>`, cắt cúp ảnh nguồn `<a:srcRect l="..." t="..." r="..." b="..."/>`, và độ mờ đục `<a:alphaModFix>`.
   - Chưa hỗ trợ chế độ lặp ngói ảnh (`<a:tile>`) và bộ lọc ảnh nhúng (`<a:duotone>`, `<a:grayscl>`).
4. **Tô Hoa văn Mẫu kẻ (`<a:pattFill>`)**:
   - Trong `SlideModel.cs`, enum `PatternFillStyle` định nghĩa 12 kiểu hoa văn, nhưng bộ ghi DrawingML hoàn toàn không sinh thẻ `<a:pattFill prst="...">`.

---

### 6.4. Đường viền, Nét vẽ & Mũi tên Chỉ dẫn (`<a:ln>`)
Đây là khu vực phát hiện các sai lệch hiển thị trực quan phổ biến nhất trong DrawingML:

#### Khảo sát Mã nguồn Bộ ghi (`FormatHandlers.cs:8629-8632`):
```csharp
sw.WriteLine(string.Format("          <a:ln w=\"{0}\"><a:solidFill><a:srgbClr val=\"{1:X6}\"/></a:solidFill></a:ln>",
    (int)(shape.StrokeWidth * exportScaleX * 12700.0f), shape.StrokeColor & 0xFFFFFF));
```

#### Lỗ hổng Trực quan Nghiêm trọng:
1. **Mất Toàn bộ Kiểu Nét đứt (`<a:prstDash>`) - BUG MAJOR M4**:
   - Phía bộ đọc (dòng 10446–10455) đọc rất chuẩn các kiểu: `dash`, `sysdash`, `dot`, `sysdot`, `dashdot`, `lgdashdotdot`.
   - Phía bộ ghi (dòng 8631): **Hoàn toàn bỏ qua thuộc tính `shape.DashStyle`**!
   - *Hậu quả*: Mọi đường viền nét đứt (Dashed), nét chấm (Dotted), nét gạch-chấm của hình vẽ và khung text box đều bị biến thành đường liền nét (Solid).
2. **Mất Hoàn toàn Đầu Mũi tên Chỉ dẫn (`<a:headEnd>` & `<a:tailEnd>`) - BUG MAJOR M4**:
   - Dòng 8631 hoàn toàn không ghi `<a:headEnd>` và `<a:tailEnd>`.
   - *Hậu quả trên Office 365*: Khi người dùng vẽ sơ đồ quy trình, lưu đồ thuật toán có mũi tên một đầu hoặc hai đầu (`ShapeType.Arrow` hoặc `ShapeType.Line`), khi mở trong PowerPoint / Word 365, **toàn bộ mũi tên đều bị rụng mất đầu**, chỉ còn lại một đoạn thẳng trần vô nghĩa!
3. **Thiếu Kiểu Ghép Góc (Joins) & Đầu Mút (Caps)**:
   - Chưa hỗ trợ `<a:round/>`, `<a:bevel/>`, `<a:miter/>` và viền đôi/ba (`cmpd="dbl"`).

---

### 6.5. Hiệu ứng Thị giác Bóng đổ, Mờ biên & Không gian 3D (`<a:effectLst>`)

#### A. Các Hiệu ứng Đã Triển khai Tốt
- **Bóng đổ ngoài (`<a:outerShdw>`)**: Tính toán chính xác bán kính mờ `blurRad`, khoảng cách bóng `dist`, góc hướng bóng `dir` (tính qua hàm lượng giác `Math.Atan2` từ độ lệch X/Y), màu sắc và độ mờ đục alpha.
- **Làm mờ cạnh (`<a:softEdge>`)**: Xuất bán kính `rad` chính xác theo đơn vị EMU (`SoftEdgeRadius * 12700.0f`).
- **Làm mờ nét (`<a:blur>`)**: Xuất bán kính chuẩn EMU.

#### B. Các Hiệu ứng Hoàn toàn Thiếu Vắng trong DrawingML
- **Bóng đổ trong (`<a:innerShdw>`)**: Chưa hỗ trợ.
- **Phát sáng Neon (`<a:glow rad="...">`)**: Giao diện và mô hình `DocShapeObject` có `HasGlow`, nhưng không được xuất ra XML.
- **Phản chiếu Bề mặt (`<a:reflection>`)**: Giao diện có thuộc tính `PictureHasReflection`, nhưng chưa được xuất sang DrawingML.
- **Khối Không gian 3D (`<a:sp3d>`)**: Vát cạnh trên (`<a:bevelT>`), vát cạnh dưới (`<a:bevelB>`), độ sâu khối hộp (`extrusionH`), và vật liệu bề mặt (`prstMaterial`).
- **Khung cảnh Chiếu sáng 3D (`<a:scene3d>`)**: Góc camera 3D (`<a:camera>`) và dàn đèn chiếu sáng (`<a:lightRig>`).

---

## 7. BẢNG MA TRẬN ĐỐI CHIẾU TÍNH NĂNG & TỶ LỆ TƯƠNG THÍCH (% COMPATIBILITY MATRIX)

Dưới đây là bảng ma trận đối chiếu toàn diện hơn 50 tính năng của cả 4 phân hệ so với chuẩn ECMA-376 / ISO/IEC 29500 và Microsoft 365:

| STT | Phân hệ Nghiệp vụ | Tính năng / Thẻ XML Chuẩn | Ngữ cảnh ECMA-376 / M365 | Vị trí Codebase ZenQ Office | Nạp (%) | Xuất (%) | Chung (%) | Mức độ Rủi ro M365 |
| :---: | :--- | :--- | :--- | :--- | :---: | :---: | :---: | :--- |
| **1** | **OPC Packaging** | `[Content_Types].xml` | ISO/IEC 29500-2 §9.1 | `FormatHandlers:6656, 7803`; `Xlsx:857` | 90% | 60% | **75.0%** | **Major**: Thiếu override core/app/theme |
| **2** | **OPC Packaging** | `_rels/.rels` (Package Rels) | ISO/IEC 29500-2 §9.3 | `FormatHandlers:6684`; `Xlsx:887` | 85% | 55% | **70.0%** | **Major**: Thiếu metadata rels |
| **3** | **OPC Packaging** | `docProps/core.xml` (Dublin Core)| ISO/IEC 29500-2 §11 | `FormatHandlers.cs` (Không có) | 0% | 0% | **0.0%** | **Minor**: Mất tác giả, ngày tạo file |
| **4** | **OPC Packaging** | Fallback XML trần nguy hiểm | Nguyên tắc Gói OPC | `FormatHandlers:8204, 12533` | N/A | **LỖI** | **0.0%** | **CRITICAL**: Xuất XML trần làm hỏng file |
| **5** | **WordprocessingML**| Định dạng Ký tự `<w:rPr>` | ISO/IEC 29500-1 §17.3.2 | `FormatHandlers:2200, 7624` | 90% | 65% | **77.5%** | **Major**: Ép gạch chân single |
| **6** | **WordprocessingML**| Màu dạ quang `<w:highlight>` | ISO/IEC 29500-1 §17.3.2.15 | `FormatHandlers:7624-7637` (Bỏ sót) | 90% | **0%** | **45.0%** | **Major (M6)**: Mất highlight khi lưu |
| **7** | **WordprocessingML**| Điểm dừng Tab `<w:tabs>` | ISO/IEC 29500-1 §17.3.1.38 | `FormatHandlers:2088, 7500` | 85% | **0%** | **42.5%** | **Major (M4)**: Lệch cột phím tab |
| **8** | **WordprocessingML**| Danh sách Đa cấp `<w:numbering>`| ISO/IEC 29500-1 §17.9 | `FormatHandlers:1135, 2009, 7500` | 75% | **0%** | **37.5%** | **Major (M5)**: Làm phẳng thành text thô |
| **9** | **WordprocessingML**| Kiểu dáng `word/styles.xml` | ISO/IEC 29500-1 §17.7 | `FormatHandlers:1414, 6858-6873` | 75% | **5%** | **40.0%** | **Major (M1)**: Rỗng Navigation, mất TOC |
| **10**| **WordprocessingML**| Đơn/Đa Phân vùng `<w:sectPr>` | ISO/IEC 29500-1 §17.6 | `FormatHandlers:1356, 7657` | 50% | 35% | **42.5%** | **Major**: Ép về đơn phân vùng |
| **11**| **WordprocessingML**| Bố cục Cột báo `<w:cols>` | ISO/IEC 29500-1 §17.6.4 | `FormatHandlers.cs` (Không có) | 0% | 0% | **0.0%** | **Major (M7)**: Mất bố cục 2 cột |
| **12**| **WordprocessingML**| Tiêu đề Trang bìa `<w:titlePg>` | ISO/IEC 29500-1 §17.10.6 | `FormatHandlers.cs` (Không có) | 0% | 0% | **0.0%** | **Major (M8)**: Đè header lên trang bìa |
| **13**| **WordprocessingML**| Gộp Ô Bảng Ngang `<w:gridSpan>`| ISO/IEC 29500-1 §17.4.17 | `FormatHandlers:5855, 7114` (0 match) | **0%** | **0%** | **0.0%** | **CRITICAL (C1)**: Vỡ nát bảng biểu |
| **14**| **WordprocessingML**| Gộp Ô Bảng Dọc `<w:vMerge>` | ISO/IEC 29500-1 §17.4.84 | `FormatHandlers:5855, 7114` (0 match) | **0%** | **0%** | **0.0%** | **CRITICAL (C1)**: Vỡ nát bảng biểu |
| **15**| **WordprocessingML**| Viền Riêng Từng Ô `<w:tcBorders>`| ISO/IEC 29500-1 §17.4.67 | `FormatHandlers:6067, 7115` | 80% | **0%** | **40.0%** | **Major (M9)**: Mất viền riêng ô |
| **16**| **WordprocessingML**| Khung Nội dung `<w:sdt>` (SDT)| ISO/IEC 29500-1 §17.5.2 | `FormatHandlers:1665` (Bỏ qua) | **0%** | **0%** | **0.0%** | **CRITICAL (C2)**: Mất trắng văn bản mẫu |
| **17**| **WordprocessingML**| Theo dõi Sửa đổi `<w:ins>/<w:del>`| ISO/IEC 29500-1 §17.13 | `WriterView.cs:7915` (Chủ động xóa) | **0%** | **0%** | **0.0%** | **CRITICAL (C3)**: Mất sạch lịch sử sửa đổi |
| **18**| **WordprocessingML**| Chú thích Cuối trang `<w:footnote>`| ISO/IEC 29500-1 §17.11 | `FormatHandlers.cs` (Không có) | **0%** | **0%** | **0.0%** | **CRITICAL (C4)**: Mất sạch Footnotes |
| **19**| **WordprocessingML**| Raster hóa Vector/SmartArt | ISO/IEC 29500-1 §20.1 | `FormatHandlers:7208-7243, 7398` | 75% | **0%** | **37.5%** | **Major (M2, M3)**: Nướng thành ảnh PNG |
| **20**| **SpreadsheetML** | Sổ làm việc `xl/workbook.xml` | ISO/IEC 29500-1 §18.2 | `XlsxHandler.cs:898-925` | 80% | 60% | **70.0%** | Thiếu calcPr, definedNames |
| **21**| **SpreadsheetML** | Quan hệ Sổ làm việc `.rels` | OPC Part Relationships | `XlsxHandler.cs:504-521` | **20%** | 60% | **40.0%** | **CRITICAL**: Mất sheet phi tuần tự |
| **22**| **SpreadsheetML** | Bắt buộc Tính lại `<calcPr>` | ISO/IEC 29500-1 §18.2.2 | `XlsxHandler.cs:902` (Không có) | 0% | 0% | **0.0%** | **CRITICAL**: Không tự tính công thức |
| **23**| **SpreadsheetML** | Bảng Chuỗi Dùng chung `<sst>` | ISO/IEC 29500-1 §18.4 | `XlsxHandler.cs:316, 1278` | 90% | **0%** | **45.0%** | **Major**: Phình to file gấp 5-10 lần |
| **24**| **SpreadsheetML** | Hàng Ẩn `hidden="1"` trong `<row>`| ISO/IEC 29500-1 §18.3.1.73 | `XlsxHandler.cs:1216` (Bỏ sót) | 80% | **0%** | **40.0%** | **Major**: Hàng ẩn tự động hiện |
| **25**| **SpreadsheetML** | Xuất Ô Luận lý Tĩnh `t="b"` | ISO/IEC 29500-1 §18.3.1.4 | `XlsxHandler.cs:1265` | 85% | **15%** | **50.0%** | **Major**: Xuất thành text inlineStr |
| **26**| **SpreadsheetML** | Tiền tố Hàm M365 `_xlfn.` | MS-XLSX 2.2.2 / OpenXML | `XlsxHandler.cs:2313-2336` | 80% | **0%** | **40.0%** | **CRITICAL**: Cảnh báo File Repair M365 |
| **27**| **SpreadsheetML** | Công thức Dùng chung `<f t="shared">`| ISO/IEC 29500-1 §18.3.1.40 | `XlsxHandler.cs:635-649` | **15%** | 0% | **7.5%** | **CRITICAL**: Mất công thức hàng loạt |
| **28**| **SpreadsheetML** | Xung đột Tràn Mảng Động Spill | OpenXML Dynamic Arrays | `XlsxHandler.cs:1265` | 70% | 20% | **45.0%** | **Major**: Gây lỗi #SPILL! trên Excel |
| **29**| **SpreadsheetML** | Bảng Định dạng Số `<numFmts>` | ISO/IEC 29500-1 §18.8.30 | `XlsxHandler.cs:1061-1169` | 20% | **0%** | **10.0%** | **Major**: Mất định dạng ngày & tiền VN |
| **30**| **SpreadsheetML** | Màu sắc Đường viền Borders | ISO/IEC 29500-1 §18.8.4 | `XlsxHandler.cs:421` (Bỏ qua màu) | **30%** | 80% | **55.0%** | **Major**: Viền màu biến thành đen |
| **31**| **SpreadsheetML** | Cố định Dòng/Cột Freeze Panes | ISO/IEC 29500-1 §18.3.1.66 | `XlsxHandler.cs:1184` (Không có) | 0% | 0% | **0.0%** | **Major**: Tiêu đề bảng bị trôi |
| **32**| **SpreadsheetML** | Xác thực Dữ liệu Data Validation| ISO/IEC 29500-1 §18.3.1.32 | `XlsxHandler.cs:1171` (Không có) | 0% | 0% | **0.0%** | **Major**: Mất Dropdown & kiểm tra nhập |
| **33**| **SpreadsheetML** | Bảng Tính năng & AutoFilter | ISO/IEC 29500-1 §18.5 | `XlsxHandler.cs` (Không có) | 0% | 0% | **0.0%** | **Major**: Mất nút lọc dữ liệu |
| **34**| **SpreadsheetML** | Ô Hợp nhất Merged Cells | ISO/IEC 29500-1 §18.3.1.55 | `XlsxHandler.cs:748, 1286` | 100% | 100% | **100.0%** | Hoàn toàn đạt chuẩn |
| **35**| **SpreadsheetML** | Biểu đồ Liên kết `<c:numRef>` | ISO/IEC 29500-1 §21.2 | `XlsxHandler.cs:2507` (Chỉ numLit) | 75% | **25%** | **50.0%** | **Major**: Biểu đồ bị ngắt liên kết ô |
| **36**| **PresentationML** | Kích thước Màn hình 16:9 EMU | ISO/IEC 29500-1 §19.2.2 | `FormatHandlers:7790, 7913` | 80% | **40%** | **60.0%** | **Major (M6)**: Lệch Page Setup 20" |
| **37**| **PresentationML** | Quan hệ Ghi chú `notesSlide*.rels`| ISO/IEC 29500-1 §13.3.4 | `FormatHandlers:8144-8186` (Quên ghi)| 70% | **0%** | **35.0%** | **CRITICAL (C1)**: Hộp thoại File Repair! |
| **38**| **PresentationML** | Kế thừa Slide Layout | ISO/IEC 29500-1 §13.3.6 | `FormatHandlers:8143` (Ép layout1) | 80% | **10%** | **45.0%** | **Major (M1)**: Ép mọi slide về layout 1 |
| **39**| **PresentationML** | Thẻ Giữ chỗ Placeholder `<p:ph>`| ISO/IEC 29500-1 §19.3.1.37 | `FormatHandlers:8556-8560` (Bỏ sót) | 85% | **0%** | **42.5%** | **Major (M2)**: Hỏng AI Designer & Outline |
| **40**| **PresentationML** | Nhóm Hình dạng `<p:grpSp>` | ISO/IEC 29500-1 §19.3.1.22 | `FormatHandlers:8334, 11487` | 85% | **0%** | **42.5%** | **Major (M5)**: San phẳng nhóm hình con |
| **41**| **PresentationML** | Biểu đồ trong Slide `c:chart` | ISO/IEC 29500-1 §14.2.1 | `FormatHandlers:8440-8520` (Bỏ rơi) | 70% | **0%** | **35.0%** | **CRITICAL (C3)**: Mất trắng 100% biểu đồ |
| **42**| **PresentationML** | Bảng Biểu `<a:tbl>` trong Slide | ISO/IEC 29500-1 §20.1.4 | `FormatHandlers:8441, 11682` | 75% | 45% | **60.0%** | **Major**: Mất gộp ô, mất viền ô |
| **43**| **PresentationML** | Chuyển tiếp Slide Morph/2010 | ISO/IEC 29500-1 §19.5 | `FormatHandlers:8883, 12111` | 95% | 85% | **90.0%** | Rất tốt; cố định dur=2000 |
| **44**| **PresentationML** | Hoạt ảnh Đối tượng `<p:timing>` | ISO/IEC 29500-1 §19.5.2 | `FormatHandlers:9036, 12190` | 80% | 40% | **60.0%** | **Major (M8)**: Chỉ có animEffect |
| **45**| **Shared DrawingML**| Lật Hình học `flipH` / `flipV` | ISO/IEC 29500-1 §20.1.7.6 | `FormatHandlers:8402, 8562` (Bỏ sót) | 90% | **0%** | **45.0%** | **Major (M3)**: Hình bị quay ngược lại |
| **46**| **Shared DrawingML**| Bo góc Tùy biến `<a:avLst>` | ISO/IEC 29500-1 §20.1.9.5 | `FormatHandlers:8578` (Luôn rỗng) | 70% | **0%** | **35.0%** | **Major**: Bo góc nhảy về mặc định |
| **47**| **Shared DrawingML**| Tô Chuyển sắc Xuyên tâm Radial | ISO/IEC 29500-1 §20.1.8.31 | `FormatHandlers:8602` (Ép lin) | 75% | **15%** | **45.0%** | **Major (M7)**: Ép thành tuyến tính |
| **48**| **Shared DrawingML**| Kiểu Nét đứt `<a:prstDash>` | ISO/IEC 29500-1 §20.1.2.2.8| `FormatHandlers:8631` (Bỏ sót) | 85% | **0%** | **42.5%** | **Major (M4)**: Biến thành nét liền |
| **49**| **Shared DrawingML**| Đầu Mũi tên `<a:headEnd/tailEnd>`| ISO/IEC 29500-1 §20.1.2.2.6| `FormatHandlers:8631` (Bỏ sót) | 80% | **0%** | **40.0%** | **Major (M4)**: Mũi tên bị rụng đầu |
| **50**| **Shared DrawingML**| Hiệu ứng 3D `<a:sp3d>`/`<scene3d>`| ISO/IEC 29500-1 §20.1.5 | `FormatHandlers:8634` (Không có) | 10% | **0%** | **5.0%** | **Minor**: Mất hiệu ứng khối 3D |

---

### Bảng Điểm Tổng Hợp Tuân Thủ Theo Từng Phân Hệ
| STT | Phân hệ Cốt lõi | Tỷ lệ Đọc (Import) | Tỷ lệ Ghi (Export) | Điểm Tuân Thủ Hiện Tại | Mục tiêu Sau Lộ trình | Rào cản Kỹ thuật Lớn nhất Cần Giải quyết |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- |
| **1** | **WordprocessingML (.docx)** | 55.7% | 30.0% | **~42.8%** | **~86.5%** | Gộp ô bảng (`gridSpan`, `vMerge`), Styles Heading, SDT, Footnotes, bảo tồn Vector |
| **2** | **SpreadsheetML (.xlsx)** | 58.2% | 34.8% | **~46.5%** | **~88.0%** | Tiền tố `_xlfn.`, Shared formula, Rels sheet, Bảng `numFmts`, Freeze panes |
| **3** | **PresentationML (.pptx)** | 62.5% | 33.5% | **~48.0%** | **~87.5%** | Notes rels corruption, Naked XML fallback, Biểu đồ slide, Placeholder `<p:ph>` |
| **4** | **Shared DrawingML** | 66.0% | 36.0% | **~51.0%** | **~85.0%** | Cờ lật `flipH/V`, Nét đứt, Đầu mũi tên, Gradient xuyên tâm, Nhóm hình dạng |
| **--** | **TỔNG THỂ TOÀN BỘ OFFICE** | **60.6%** | **33.6%** | **~47.1%** | **~86.8%** | **Đủ điều kiện nâng cấp toàn diện theo 3 giai đoạn** |

---

## 8. PHÂN LOẠI LỖ HỔNG THEO 3 CẤP ĐỘ NGHIÊM TRỌNG (SEVERITY ASSESSMENT)

Mọi sai lệch định dạng được phân bổ nghiêm ngặt theo 3 cấp độ tác động khi tệp được trao đổi với Microsoft 365:

```
                            THÁP PHÂN LOẠI MỨC ĐỘ RỦI RO
                                        ▲
                                       / \
                                      /   \
                                     /  C  \  CRITICAL (Nghiêm trọng)
                                    /───────\ Gây File Repair Dialog, Hỏng Cấu Trúc, Mất Sạch Dữ Liệu
                                   /    M    \ MAJOR (Quan trọng)
                                  /───────────\ Lệch Bố Cục, Mất Định Dạng, Đứt Gãy Tính Năng Doanh Nghiệp
                                 /      m      \ MINOR (Mở rộng)
                                /───────────────\ Siêu Dữ Liệu Phụ Trợ, Cờ Tương Thích Cũ, Hiệu Ứng Nâng Cao
```

### 8.1. Cấp độ Critical (Nghiêm trọng - Gây Hỏng Tệp / File Repair / Mất Trắng Dữ Liệu)
*Định nghĩa: Gây lỗi "Word/Excel/PowerPoint found unreadable content", bật hộp thoại phục hồi tệp (File Recovery Prompt), hoặc làm mất vĩnh viễn dữ liệu người dùng mà không thể phục hồi.*

1. **[C1 - PPTX] Lỗi Part Mồ Côi Tệp Ghi Chú Diễn Giả (`FormatHandlers.cs:8144-8186`)**:
   - *Thẻ XML*: `ppt/notesSlides/notesSlide*.xml` thiếu `ppt/notesSlides/_rels/notesSlide*.xml.rels` và `notesMaster1.xml`.
   - *Hậu quả M365*: PowerPoint 365 bật ngay hộp thoại phục hồi tệp: *"PowerPoint found a problem with content in presentation.pptx. PowerPoint can attempt to repair the presentation"*. Nếu người dùng bấm Repair, toàn bộ ghi chú diễn giả bị xóa sổ.
2. **[C2 - PPTX] Cơ Chế Fallback XML Trần Cực Kỳ Nguy Hiểm (`FormatHandlers.cs:8204 & 12533`)**:
   - *Thao tác*: Bắt lỗi `catch (Exception)` và ghi văn bản XML thuần túy không nén mang đuôi `.pptx`.
   - *Hậu quả M365*: PowerPoint 365 từ chối mở tệp hoàn toàn: *"PowerPoint cannot open the file because the file format or file extension is not valid"*.
3. **[C3 - PPTX] Mất Trắng 100% Dữ Liệu Biểu Đồ trong Slide (`FormatHandlers.cs:8440-8520`)**:
   - *Thẻ XML*: Thiếu `<c:chart>` trong `<p:graphicFrame>`.
   - *Hậu quả M365*: Mọi biểu đồ tròn, cột, đường của người dùng bị biến mất, thay thế bằng hình chữ nhật rỗng.
4. **[C4 - XLSX] Thiếu Tiền Tố `_xlfn.` Cho Các Hàm Excel 365 (`XlsxHandler.cs:2313-2336`)**:
   - *Thẻ XML*: `<f>XLOOKUP(...)</f>` thay vì `<f>_xlfn.XLOOKUP(...)</f>`.
   - *Hậu quả M365*: Excel 365 hiển thị hộp thoại cảnh báo: *"We found a problem with some content in 'document.xlsx'. Do you want us to try to recover as much as we can?"*. Sau khi sửa, công thức bị xóa hoặc biến thành `=#NAME?`.
5. **[C5 - XLSX] Phá Hủy Công Thức Dùng Chung Khi Nạp Tệp (`XlsxHandler.cs:635-649`)**:
   - *Thẻ XML*: `<f t="shared" si="0"/>`.
   - *Hậu quả M365*: Trình nạp bỏ qua thẻ `<f>` rỗng, gán giá trị tĩnh `<v>` cho hàng ngàn ô phụ thuộc, biến toàn bộ công thức bảng tính thành số chết.
6. **[C6 - XLSX] Bỏ Qua Bảng Quan Hệ Sổ Làm Việc (`XlsxHandler.cs:504-521`)**:
   - *Thao tác*: Duyệt cứng `sheet1.xml` thay vì đọc `xl/_rels/workbook.xml.rels`.
   - *Hậu quả M365*: Nếu tệp Excel từng bị xóa Sheet1, ZenQ Calc ngắt đọc ngay lập tức và **làm mất sạch 100% các sheet còn lại**.
7. **[C7 - DOCX] Không Hỗ Trợ Gộp Ô Bảng Ngang/Dọc (`FormatHandlers.cs:5855, 7114`)**:
   - *Thẻ XML*: Thiếu `<w:gridSpan>` và `<w:vMerge>`.
   - *Hậu quả M365*: Cấu trúc ô không khớp lưới `tblGrid`, gây vỡ nát bảng báo cáo và kích hoạt cảnh báo sửa bảng trong Word.
8. **[C8 - DOCX] Bỏ Rơi Khung Nội Dung Điều Khiển SDT (`FormatHandlers.cs:1665`)**:
   - *Thẻ XML*: `<w:sdt>`, `<w:sdtContent>`.
   - *Hậu quả M365*: Toàn bộ văn bản nằm trong thẻ form biểu mẫu bị nuốt mất dữ liệu.
9. **[C9 - DOCX] Chủ Động Xóa Sạch Lịch Sử Track Changes (`WriterView.cs:7915`)**:
   - *Thao tác*: `TrackChanges = false; Revisions.Clear();`.
   - *Hậu quả M365*: Xóa sạch toàn bộ lịch sử chỉnh sửa tài liệu pháp lý/hợp đồng của đối tác.
10. **[C10 - DOCX] Hoàn Toàn Bỏ Rơi Chú Thích Cuối Trang (`FormatHandlers.cs` 0 dòng code)**:
    - *Thẻ XML*: `<w:footnote>`, `word/footnotes.xml`.
    - *Hậu quả M365*: Mất sạch trích dẫn học thuật và ghi chú chân trang.

---

### 8.2. Cấp độ Major (Quan trọng - Sai Lệch Bố Cục & Mất Tính Năng Nghiệp Vụ)
*Định nghĩa: Tệp mở được bình thường nhưng bị sai lệch nghiêm trọng về kiểu dáng, mất định dạng màu sắc/phông chữ/bảng biểu, hoặc mất khả năng tương tác.*

1. **[M1 - DOCX] Không Định Nghĩa Kiểu Dáng trong `word/styles.xml` (`FormatHandlers.cs:6858`)**:
   Mất toàn bộ Heading 1, 2, 3; khung Navigation Pane của Word trống rỗng; tính năng tạo Mục lục tự động (TOC) bị vô hiệu hóa.
2. **[M2 - DOCX] Raster Hóa Vector Shapes, SmartArt & Charts Sang Ảnh PNG (`FormatHandlers.cs:7208`)**:
   Biến hình vẽ và biểu đồ thành ảnh chụp bitmap chết, không thể sửa text, vỡ hạt khi in ấn.
3. **[M3 - DOCX] Mất Toàn Bộ Màu Dạ Quang `<w:highlight>` Khi Lưu (`FormatHandlers.cs:7624`)**:
   Các đoạn bôi màu ghi chú của người dùng bị biến mất hoàn toàn.
4. **[M4 - DOCX] Triệt Tiêu Điểm Dừng Tab `<w:tabs>` Khi Lưu (`FormatHandlers.cs:7500`)**:
   Mọi văn bản sử dụng tab (hợp đồng có dòng chấm, bảng giá) bị nhảy lộn xộn.
5. **[M5 - DOCX] Làm Phẳng Danh Sách Đa Cấp `<w:numbering>` Thành Text Thô (`FormatHandlers.cs:2009`)**:
   Mất tính năng tự động nhảy số thứ tự khi nhấn Enter, mất khả năng tăng giảm thụt lề bằng phím Tab.
6. **[M6 - DOCX] Mất Viền Riêng Ô Bảng `<w:tcBorders>` (`FormatHandlers.cs:7115`)**:
   Toàn bộ viền đặc thù của từng ô bị triệt tiêu, ép theo viền chung của bảng.
7. **[M7 - DOCX] Đơn Phân Vùng & Thiếu Cột Báo `<w:cols>` (`FormatHandlers.cs:1356`)**:
   Tài liệu nhiều section bị ép về 1 trang dọc; bài báo 2 cột bị duỗi thành 1 cột.
8. **[M8 - DOCX] Đè Header Lên Trang Bìa Do Thiếu `<w:titlePg>` (`FormatHandlers.cs:275`)**:
   Tiêu đề của các trang sau bị đè lên bìa tài liệu.
9. **[M9 - XLSX] Thiếu Bảng Định Dạng Số Tùy Biến `<numFmts>` (`XlsxHandler.cs:1061`)**:
   Mất định dạng ngày Việt Nam (`dd/mm/yyyy`), mất tiền tệ Đồng (`₫`), mất định dạng số tùy chỉnh.
10. **[M10 - XLSX] Biểu Đồ Ngắt Kết Nối Bảng Tính Do Chỉ Xuất `<c:numLit>` (`XlsxHandler.cs:2507`)**:
    Biểu đồ biến thành số chết, không cập nhật khi sửa bảng tính trên Excel.
11. **[M11 - XLSX] Không Lưu Cố Định Dòng/Cột Freeze Panes (`XlsxHandler.cs:1184`)**:
    Tiêu đề bảng tính bị trôi tự do khi cuộn chuột trên Excel.
12. **[M12 - XLSX] Không Lưu Xác Thực Dữ Liệu Data Validation (`XlsxHandler.cs:1171`)**:
    Mất toàn bộ Dropdown và quy tắc kiểm tra nhập liệu dù Core đã có sẵn.
13. **[M13 - XLSX] Không Lưu Bảng Tính Năng Nâng Cao & AutoFilter (`XlsxHandler.cs`)**:
    Mất nút lọc dữ liệu, mất sọc màu bảng thông minh; hàng ẩn tự động hiện do thiếu `hidden="1"`.
14. **[M14 - XLSX] Lỗi Xuất Ô Luận Lý Tĩnh Thành Text InlineStr (`XlsxHandler.cs:1265`)**:
    Giá trị `TRUE`/`FALSE` bị hiểu là Text thông thường, làm sai hàm `=IF` và `=ISLOGICAL`.
15. **[M15 - PPTX] Ép Mọi Slide Về `slideLayout1.xml` (`FormatHandlers.cs:8143`)**:
    Sai lệch kiểu bố cục; không thể quản lý bài thuyết trình theo layout chuẩn.
16. **[M16 - PPTX] Hoàn Toàn Bỏ Rơi Thẻ Giữ Chỗ `<p:ph>` (`FormatHandlers.cs:8556`)**:
    Vô hiệu hóa Microsoft AI Designer; Outline View trống rỗng; phím Reset Slide chèn đè khung mới.
17. **[M17 - PPTX] San Phẳng Nhóm Hình Dạng `<p:grpSp>` (`FormatHandlers.cs:8334`)**:
    Mất cấu trúc nhóm, không thể chọn hoặc co giãn cụm đối tượng cùng lúc.
18. **[M18 - PPTX] Sai Lệch Kích Thước Màn Hình 16:9 EMU (20" x 11.25") (`FormatHandlers.cs:7790`)**:
    Lệch hộp thoại Page Setup, co giãn méo hình khi in ấn hoặc xuất PDF từ PowerPoint.
19. **[M19 - DRAWING] Bỏ Quên Thuộc Tính Lật Hình `flipH` và `flipV` (`FormatHandlers.cs:8402, 8562`)**:
    Các hình vẽ, mũi tên, icon đã lật bị quay ngược lại hướng ban đầu trên M365.
20. **[M20 - DRAWING] Mất Toàn Bộ Kiểu Nét Đứt `<a:prstDash>` & Đầu Mũi Tên (`FormatHandlers.cs:8631`)**:
    Nét đứt/chấm biến thành nét liền; mũi tên chỉ dẫn bị rụng mất đầu.
21. **[M21 - DRAWING] Ép Mọi Gradient Xuyên Tâm Radial Về Tuyến Tính (`FormatHandlers.cs:8602`)**:
    Mất hiệu ứng chuyển sắc tròn từ tâm của các nút bấm nghệ thuật.

---

### 8.3. Cấp độ Minor (Mở rộng - Siêu Dữ Liệu & Thuộc Tính Tùy Chọn)
*Định nghĩa: Thuộc tính bổ trợ ít ảnh hưởng đến trải nghiệm đọc/in tài liệu thông thường.*

1. **[m1] Thiếu Siêu Dữ Liệu Dublin Core `docProps/core.xml` & `docProps/app.xml`**:
   Không hiển thị Tác giả, Ngày tạo, Thống kê trang/từ trong Windows Explorer.
2. **[m2] Không Xuất Bảng Chuỗi Dùng Chung `xl/sharedStrings.xml`**:
   Tệp phình to dung lượng gấp nhiều lần do dùng 100% `inlineStr`.
3. **[m3] Mất Kiểu Gạch Ngang Chữ `<strike/>` Trong Bảng Tính**:
   Lớp font của Calc thiếu trường `Strikethrough`.
4. **[m4] Thiếu Cờ Chống Tách Hàng Qua Trang Bảng Biểu `<w:cantSplit/>`**:
   Hàng bảng dài có thể bị cắt ngang dòng chữ khi in qua mép trang giấy.
5. **[m5] Bỏ Rơi Âm Thanh Chuyển Cảnh Slide `<p:snd>`**:
   Mất âm thanh click, whoosh khi chuyển trang chiếu.
6. **[m6] Thiếu Thuộc Tính Khối 3D `<a:sp3d>` & Phát Sáng `<a:glow>`**:
   Các hình khối 3D hiển thị dưới dạng phẳng 2D.
7. **[m7] Thiếu Căn Dọc Trang Giấy `<w:vAlign w:val="center"/>` trong `sectPr`**:
   Trang bìa không tự căn giữa theo chiều dọc nếu có thiết lập này.

---

## 9. LỘ TRÌNH KỸ THUẬT TỪNG BƯỚC & GIẢI PHÁP MÃ NGUỒN CỤ THỂ (ACTIONABLE ENGINEERING ROADMAP)

Để đưa mức độ tuân thủ OOXML của ZenQ Office từ **~47.1%** lên trên **85%** chuẩn ECMA-376 / Microsoft 365 và triệt tiêu 100% lỗi cảnh báo File Repair, lộ trình tái cấu trúc 3 giai đoạn được thiết kế chi tiết dưới đây:

```
                               LỘ TRÌNH KỸ THUẬT 3 GIAI ĐOẠN
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ GIAI ĐOẠN 1: KHẮC PHỤC TRIỆT ĐỂ CÁC LỖI CRITICAL CHỐNG HỎNG TỆP M365 (ƯU TIÊN 1)        │
│ ├── Vá lỗi 1: Bổ sung tiền tố _xlfn. cho hàm Excel 365 (NormalizeExcelFormula)         │
│ ├── Vá lỗi 2: Khôi phục công thức dùng chung t="shared" khi nạp tệp XLSX               │
│ ├── Vá lỗi 3: Nạp Worksheet qua quan hệ xl/_rels/workbook.xml.rels                     │
│ ├── Vá lỗi 4: Bổ sung notesSlide*.xml.rels & notesMaster1.xml triệt tiêu lỗi PPTX       │
│ ├── Vá lỗi 5: Triệt tiêu cơ chế Fallback XML trần nguy hiểm SavePptxXml                │
│ └── Vá lỗi 6: Xuất biểu đồ c:chart cho PresentationML ngăn mất dữ liệu                 │
└───────────────────────────────────────────┬────────────────────────────────────────────┘
                                            │
                                            ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ GIAI ĐOẠN 2: NÂNG CẤP CẤU TRÚC DỮ LIỆU CỐT LÕI & KHÔI PHỤC BỐ CỤC (ƯU TIÊN 2)          │
│ ├── Vá lỗi 7: Nâng cấp DocTableCell, xuất <w:gridSpan> và <w:vMerge> cho bảng           │
│ ├── Vá lỗi 8: Bảo toàn flipH/V, nét đứt prstDash và mũi tên headEnd trong DrawingML     │
│ ├── Vá lỗi 9: Khôi phục xuất <w:highlight> và Tab stops <w:tabs> cho Writer            │
│ ├── Vá lỗi 10: Triển khai Cố định Dòng/Cột Freeze Panes <pane> cho Calc                │
│ ├── Vá lỗi 11: Bổ sung bảng định dạng số tùy biến <numFmts> (Ngày & Tiền tệ VN)        │
│ └── Vá lỗi 12: Xuất thẻ Placeholder <p:ph> và ánh xạ slideLayout tương ứng             │
└───────────────────────────────────────────┬────────────────────────────────────────────┘
                                            │
                                            ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ GIAI ĐOẠN 3: HOÀN THIỆN CHUẨN HÓA TOÀN DIỆN CÁC TÍNH NĂNG NÂNG CAO (ƯU TIÊN 3)         │
│ ├── Vá lỗi 13: Xuất hệ thống Styles chuẩn (Normal, Heading 1-3) trong styles.xml       │
│ ├── Vá lỗi 14: Bảo tồn Vector DrawingML và SmartArt thay vì raster hóa PNG             │
│ ├── Vá lỗi 15: Chuyển đổi xuất chuỗi sang Bảng Chuỗi Dùng Chung sharedStrings.xml      │
│ ├── Vá lỗi 16: Xuất Data Validation <dataValidations> và AutoFilter/Tables             │
│ ├── Vá lỗi 17: Hỗ trợ cấu trúc nhóm hình dạng phân cấp <p:grpSp>                       │
│ └── Vá lỗi 18: Đọc và giải mã Theme Colors (word/theme, xl/theme, ppt/theme)           │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

### 9.1. Giai đoạn 1: Khắc phục Triệt để các Lỗi Critical Gây Hỏng Tệp M365 (Ngay Lập Tức)

#### Vá lỗi 1: Bổ Sung Tiền Tố `_xlfn.` Cho Các Hàm Excel 365 Hiện Đại
- **Vị trí**: `src/format/XlsxHandler.cs`, thay thế phương thức `NormalizeExcelFormula` tại dòng 2313–2336:
```csharp
// MÃ NGUỒN NÂNG CẤP TẠI DÒNG 2313 TRONG XlsxHandler.cs:
private static readonly HashSet<string> ModernExcelFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "XLOOKUP", "XMATCH", "SORT", "SORTBY", "FILTER", "UNIQUE", "SEQUENCE", "RANDARRAY",
    "CONCAT", "TEXTJOIN", "IFS", "SWITCH", "MAXIFS", "MINIFS",
    "STDEV.S", "STDEV.P", "VAR.S", "VAR.P", "MODE.SNGL", "MODE.MULT",
    "COVARIANCE.S", "COVARIANCE.P", "PERCENTILE.EXC", "PERCENTILE.INC",
    "RANK.AVG", "RANK.EQ", "NETWORKDAYS.INTL", "WORKDAY.INTL", "NORM.DIST"
};

private static string NormalizeExcelFormula(string formula)
{
    if (string.IsNullOrEmpty(formula)) return "";
    
    // 1. Chuẩn hóa dấu phân cách tham số từ ';' sang ',' (bỏ qua chuỗi nằm trong ngoặc kép)
    StringBuilder sb = new StringBuilder(formula.Length + 32);
    bool inQuotes = false;
    for (int i = 0; i < formula.Length; i++)
    {
        char ch = formula[i];
        if (ch == '"') { inQuotes = !inQuotes; sb.Append(ch); }
        else if (ch == ';' && !inQuotes) { sb.Append(','); }
        else { sb.Append(ch); }
    }
    string norm = sb.ToString();

    // 2. Tự động thêm tiền tố _xlfn. cho các hàm hiện đại theo chuẩn OpenXML
    foreach (var fn in ModernExcelFunctions)
    {
        string pattern = @"\b(" + Regex.Escape(fn) + @")\s*\(";
        norm = Regex.Replace(norm, pattern, match =>
        {
            return "_xlfn." + match.Groups[1].Value.ToUpperInvariant() + "(";
        }, RegexOptions.IgnoreCase);
    }

    return norm;
}
```

#### Vá lỗi 2: Khôi Phục Công Thức Dùng Chung (`t="shared"`) Khi Nạp Tệp
- **Vị trí**: `src/format/XlsxHandler.cs`, trước vòng lặp duyệt hàng tại dòng 606:
```csharp
// Khai báo bộ nhớ đệm lưu vết công thức chủ: Key = si, Value = (masterFormula, baseRow, baseCol)
Dictionary<int, Tuple<string, uint, uint>> sharedFormulaMasters = new Dictionary<int, Tuple<string, uint, uint>>();
```
- Thay thế khối kiểm tra thẻ `<f>` tại dòng 635–649:
```csharp
if (fNode != null)
{
    string fType = fNode.Attributes != null && fNode.Attributes["t"] != null ? fNode.Attributes["t"].Value : "";
    string siAttr = fNode.Attributes != null && fNode.Attributes["si"] != null ? fNode.Attributes["si"].Value : "";
    int siId;
    int.TryParse(siAttr, out siId);

    if (fType == "shared" && string.IsNullOrEmpty(fNode.InnerText))
    {
        // ĐÂY LÀ Ô PHỤ THUỘC TRONG DẢI CÔNG THỨC DÙNG CHUNG!
        if (sharedFormulaMasters.ContainsKey(siId))
        {
            var master = sharedFormulaMasters[siId];
            string translatedFormula = ShiftFormulaReferences(master.Item1, (int)(r - master.Item2), (int)(c - master.Item3));
            sheet.SetCellText(r, c, "=" + translatedFormula);
        }
        else
        {
            // Dự phòng nếu không tìm thấy chủ: lấy giá trị cache từ vNode
            if (vNode != null) sheet.SetCellText(r, c, vNode.InnerText);
        }
    }
    else if (!string.IsNullOrEmpty(fNode.InnerText))
    {
        string rawFormula = fNode.InnerText;
        while (rawFormula.IndexOf("_xlfn.", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            int idx = rawFormula.IndexOf("_xlfn.", StringComparison.OrdinalIgnoreCase);
            rawFormula = rawFormula.Remove(idx, 6);
        }
        while (rawFormula.IndexOf("_xlws.", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            int idx = rawFormula.IndexOf("_xlws.", StringComparison.OrdinalIgnoreCase);
            rawFormula = rawFormula.Remove(idx, 6);
        }

        if (fType == "shared" && !string.IsNullOrEmpty(siAttr))
        {
            sharedFormulaMasters[siId] = new Tuple<string, uint, uint>(rawFormula, r, c);
        }

        sheet.SetCellText(r, c, "=" + rawFormula);
    }
}
```
- Hàm phụ trợ tịnh tiến tham chiếu ô `ShiftFormulaReferences`:
```csharp
private static string ShiftFormulaReferences(string formula, int dRow, int dCol)
{
    if (dRow == 0 && dCol == 0) return formula;
    // Regex nhận diện địa chỉ ô Excel (ví dụ A1, $B$2, C$3, $D4)
    string pattern = @"(\$?)([A-Za-z]{1,3})(\$?)(\d{1,7})";
    return Regex.Replace(formula, pattern, match =>
    {
        bool colAbsolute = match.Groups[1].Value == "$";
        string colName = match.Groups[2].Value;
        bool rowAbsolute = match.Groups[3].Value == "$";
        int rowNum = int.Parse(match.Groups[4].Value);

        string newCol = colName;
        if (!colAbsolute)
        {
            int colIdx = SparseMatrix.NameToColIndex(colName);
            int shiftedCol = Math.Max(1, colIdx + dCol);
            newCol = SparseMatrix.ColIndexToName(shiftedCol);
        }

        int newRow = rowNum;
        if (!rowAbsolute)
        {
            newRow = Math.Max(1, rowNum + dRow);
        }

        return (colAbsolute ? "$" : "") + newCol + (rowAbsolute ? "$" : "") + newRow;
    });
}
```

#### Vá lỗi 3: Nạp Worksheet Qua Bảng Quan Hệ `xl/_rels/workbook.xml.rels`
- **Vị trí**: Thay thế dòng 504–521 trong `src/format/XlsxHandler.cs`:
```csharp
// Đọc chính xác ánh xạ Relationship Id -> File Target từ xl/_rels/workbook.xml.rels
Dictionary<string, string> sheetRels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
ZipArchiveEntry wbRelsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels") ?? archive.GetEntry("xl/_rels/Workbook.xml.rels");
if (wbRelsEntry != null)
{
    using (Stream s = wbRelsEntry.Open())
    {
        XmlDocument rDoc = new XmlDocument();
        rDoc.Load(s);
        foreach (XmlNode relNode in rDoc.GetElementsByTagName("Relationship"))
        {
            if (relNode.Attributes != null && relNode.Attributes["Id"] != null && relNode.Attributes["Target"] != null)
            {
                sheetRels[relNode.Attributes["Id"].Value] = relNode.Attributes["Target"].Value;
            }
        }
    }
}

// Duyệt danh sách các thẻ <sheet> đã đọc từ xl/workbook.xml
for (int sheetIdx = 0; sheetIdx < sheetNodesList.Count; sheetIdx++)
{
    string rId = sheetNodesList[sheetIdx].RelId;
    string targetPath = "";
    if (sheetRels.ContainsKey(rId))
    {
        targetPath = sheetRels[rId].Replace('\\', '/');
        if (!targetPath.StartsWith("xl/")) targetPath = "xl/" + targetPath.TrimStart('/');
    }
    else
    {
        targetPath = string.Format("xl/worksheets/sheet{0}.xml", sheetIdx + 1);
    }

    ZipArchiveEntry wsEntry = archive.GetEntry(targetPath) ?? archive.GetEntry(targetPath.ToLower());
    if (wsEntry == null) continue; // Tiếp tục với sheet kế tiếp thay vì kích hoạt break!

    // Tiến hành nạp nội dung worksheet...
}
```

#### Vá lỗi 4: Khắc Phục Lỗi Hỏng Tệp Ghi Chú Diễn Giả PPTX (Notes Slide Rels)
- **Vị trí**: `src/format/FormatHandlers.cs`, trong khối `if (hasNotes)` (ngay sau dòng 8186):
```csharp
// 1. Tạo tệp quan hệ chuẩn cho notesSlide trỏ về slide chính và notesMaster
string notesRelPath = string.Format("ppt/notesSlides/_rels/notesSlide{0}.xml.rels", slideIndex);
var oldNotesRel = archive.GetEntry(notesRelPath);
if (oldNotesRel != null) oldNotesRel.Delete();
ZipArchiveEntry notesRelEntry = archive.CreateEntry(notesRelPath);
using (StreamWriter swRel = new StreamWriter(notesRelEntry.Open(), new UTF8Encoding(false)))
{
    swRel.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
    swRel.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
    swRel.WriteLine(string.Format("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"../slides/slide{0}.xml\"/>", slideIndex));
    swRel.WriteLine("  <Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/notesMaster\" Target=\"../notesMasters/notesMaster1.xml\"/>");
    swRel.WriteLine("</Relationships>");
}

// 2. Bảo đảm notesMaster1.xml tồn tại trong gói presentation
if (archive.GetEntry("ppt/notesMasters/notesMaster1.xml") == null)
{
    ZipArchiveEntry nmEntry = archive.CreateEntry("ppt/notesMasters/notesMaster1.xml");
    using (StreamWriter swNm = new StreamWriter(nmEntry.Open(), new UTF8Encoding(false)))
    {
        swNm.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        swNm.WriteLine("<p:notesMaster xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">");
        swNm.WriteLine("  <p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld>");
        swNm.WriteLine("  <p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>");
        swNm.WriteLine("</p:notesMaster>");
    }
}
```

#### Vá lỗi 5: Triệt Tiêu Fallback XML Trần Nguy Hiểm trong PptxHandler
- **Vị trí**: Thay thế khối `catch` tại dòng 8201–8206 trong `src/format/FormatHandlers.cs`:
```csharp
catch (Exception ex)
{
    Console.WriteLine("EXPORT PPTX FATAL ERROR: " + ex.Message + "\n" + ex.StackTrace);
    // TUYỆT ĐỐI KHÔNG GỌI: return SavePptxXml(filepath, pres);
    // Xóa bỏ tệp hỏng đang ghi dở để không kích hoạt lỗi file extension:
    try { if (File.Exists(filepath)) File.Delete(filepath); } catch { }
    throw new InvalidOperationException("Không thể đóng gói tệp OpenXML PPTX chuẩn: " + ex.Message, ex);
}
```

#### Vá lỗi 6: Xuất Biểu Đồ DrawingML (`<c:chart>`) Cho PresentationML
- **Vị trí**: Trong `WriteSlideXml` (`FormatHandlers.cs:8440`), bổ sung nhánh xử lý cho `shape.Type == ShapeType.Chart`:
```csharp
else if (shape.Type == ShapeType.Chart && shape.ChartData != null)
{
    int chartIdx = slideIndex * 100 + spId;
    string chartPartPath = string.Format("ppt/charts/chart{0}.xml", chartIdx);
    var chartEntry = archive.CreateEntry(chartPartPath);
    using (StreamWriter csw = new StreamWriter(chartEntry.Open(), new UTF8Encoding(false)))
    {
        // Tái sử dụng logic xuất c:chartSpace từ XlsxHandler:2344
        WriteDrawingMlChartSpace(csw, shape.ChartData);
    }

    string rIdChart = string.Format("rIdChart{0}", chartIdx);
    slideRelXmlLines.Add(string.Format("  <Relationship Id=\"{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart\" Target=\"../charts/chart{1}.xml\"/>", rIdChart, chartIdx));

    sw.WriteLine("      <p:graphicFrame>");
    sw.WriteLine(string.Format("        <p:nvGraphicFramePr><p:cNvPr id=\"{0}\" name=\"{1}\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>", spId, EscapeXml(shapeName)));
    sw.WriteLine(string.Format("        <p:xfrm><a:off x=\"{0}\" y=\"{1}\"/><a:ext cx=\"{2}\" cy=\"{3}\"/></p:xfrm>", offX, offY, extW, extH));
    sw.WriteLine("        <a:graphic>");
    sw.WriteLine("          <a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\">");
    sw.WriteLine(string.Format("            <c:chart xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" r:id=\"{0}\"/>", rIdChart));
    sw.WriteLine("          </a:graphicData>");
    sw.WriteLine("        </a:graphic>");
    sw.WriteLine("      </p:graphicFrame>");
}
```

---

### 9.2. Giai đoạn 2: Nâng Cấp Cấu Trúc Dữ Liệu Cốt Lõi & Khôi Phục Bố Cục (Ngắn Hạn)

#### Vá lỗi 7: Hỗ Trợ Gộp Ô Bảng (`gridSpan` & `vMerge`) trong WordprocessingML
- **Nâng cấp Model**: Trong `src/ui/DocTableObject.cs` (lớp `DocTableCell`):
```csharp
public int ColSpan { get; set; } = 1;      // Tương ứng <w:gridSpan w:val="..."/>
public string VMergeType { get; set; }     // "restart", "continue", hoặc null
```
- **Nâng cấp Trình xuất `SaveDocxFile`** (ngay sau dòng 7116 trong `FormatHandlers.cs`):
```csharp
if (cObj != null && cObj.ColSpan > 1)
{
    sw.WriteLine(string.Format("            <w:gridSpan w:val=\"{0}\"/>", cObj.ColSpan));
}
if (cObj != null && !string.IsNullOrEmpty(cObj.VMergeType))
{
    if (cObj.VMergeType == "restart")
        sw.WriteLine("            <w:vMerge w:val=\"restart\"/>");
    else
        sw.WriteLine("            <w:vMerge/>");
}
```

#### Vá lỗi 8: Bảo Toàn Lật Hình `flipH/V`, Nét Đứt và Mũi Tên trong DrawingML
- **Vị trí 1 (Lật hình)**: `FormatHandlers.cs` dòng 8402 và dòng 8562 (`<a:xfrm>`):
```csharp
string flipAttrs = "";
if (shape.Transform.FlipHorizontal) flipAttrs += " flipH=\"1\"";
if (shape.Transform.FlipVertical) flipAttrs += " flipV=\"1\"";

sw.WriteLine("          <a:xfrm" + flipAttrs + ">");
sw.WriteLine(string.Format("            <a:off x=\"{0}\" y=\"{1}\"/>", offX, offY));
sw.WriteLine(string.Format("            <a:ext cx=\"{0}\" cy=\"{1}\"/>", extW, extH));
```
- **Vị trí 2 (Nét đứt & Mũi tên)**: `FormatHandlers.cs` dòng 8631 (`<a:ln>`):
```csharp
string dashXml = "";
switch (shape.DashStyle)
{
    case System.Drawing.Drawing2D.DashStyle.Dash: dashXml = "<a:prstDash val=\"dash\"/>"; break;
    case System.Drawing.Drawing2D.DashStyle.Dot: dashXml = "<a:prstDash val=\"dot\"/>"; break;
    case System.Drawing.Drawing2D.DashStyle.DashDot: dashXml = "<a:prstDash val=\"dashDot\"/>"; break;
    case System.Drawing.Drawing2D.DashStyle.DashDotDot: dashXml = "<a:prstDash val=\"lgDashDotDot\"/>"; break;
}

string arrowXml = "";
if (shape.Type == ShapeType.Arrow)
{
    arrowXml = "<a:tailEnd type=\"none\"/><a:headEnd type=\"triangle\" w=\"med\" len=\"med\"/>";
}

int strokeW = (int)(Math.Max(0.5f, shape.StrokeWidth) * exportScaleX * 12700.0f);
sw.WriteLine(string.Format("          <a:ln w=\"{0}\"><a:solidFill><a:srgbClr val=\"{1:X6}\"/></a:solidFill>{2}{3}</a:ln>",
    strokeW, shape.StrokeColor & 0xFFFFFF, dashXml, arrowXml));
```

#### Vá lỗi 9: Khôi Phục Xuất `<w:highlight>` và Tab Stops `<w:tabs>` cho Writer
- **Màu dạ quang**: Tại dòng 7631 `FormatHandlers.cs`:
```csharp
if (hl != Color.Empty && hl != Color.Transparent)
{
    string hlVal = MapColorToOpenXmlHighlight(hl);
    sw.WriteLine(string.Format("          <w:highlight w:val=\"{0}\"/>", hlVal));
}
```
- **Điểm dừng Tab**: Truyền `ParagraphTabStops` từ `WriterView.cs:8216` vào `SaveDocxFile` và xuất thẻ `<w:tabs>` trước dòng 7575:
```csharp
if (paraTabStops != null && paraTabStops.ContainsKey(charOffset))
{
    var tList = paraTabStops[charOffset];
    if (tList != null && tList.Count > 0)
    {
        sw.WriteLine("        <w:tabs>");
        foreach (var ts in tList)
        {
            string valStr = ts.Type.ToString().ToLowerInvariant();
            string leaderStr = ts.Leader == 1 ? " w:leader=\"dot\"" : (ts.Leader == 2 ? " w:leader=\"hyphen\"" : "");
            int posTwips = (int)Math.Round(ts.PositionPt * 20);
            sw.WriteLine(string.Format("          <w:tab w:val=\"{0}\"{1} w:pos=\"{2}\"/>", valStr, leaderStr, posTwips));
        }
        sw.WriteLine("        </w:tabs>");
    }
}
```

#### Vá lỗi 10: Cố Định Dòng/Cột Freeze Panes (`<pane>`) trong Calc
- **Vị trí**: `src/format/XlsxHandler.cs`, ngay sau thẻ mở `<worksheet>`:
```csharp
if (calcView.IsFreezeTopRow || calcView.IsFreezeFirstCol)
{
    int xSplit = calcView.IsFreezeFirstCol ? 1 : 0;
    int ySplit = calcView.IsFreezeTopRow ? 1 : 0;
    string topLeft = (calcView.IsFreezeFirstCol && calcView.IsFreezeTopRow) ? "B2" : (calcView.IsFreezeFirstCol ? "B1" : "A2");
    string activePane = (xSplit > 0 && ySplit > 0) ? "bottomRight" : (xSplit > 0 ? "topRight" : "bottomLeft");

    sw.WriteLine("  <sheetViews>");
    sw.WriteLine("    <sheetView tabSelected=\"1\" workbookViewId=\"0\">");
    sw.WriteLine(string.Format("      <pane xSplit=\"{0}\" ySplit=\"{1}\" topLeftCell=\"{2}\" activePane=\"{3}\" state=\"frozen\"/>", xSplit, ySplit, topLeft, activePane));
    sw.WriteLine("    </sheetView>");
    sw.WriteLine("  </sheetViews>");
}
```

#### Vá lỗi 11: Bổ Sung Bảng Định Dạng Số Tùy Biến `<numFmts>` trong Calc
- **Vị trí**: `src/format/XlsxHandler.cs:1061`:
```csharp
Dictionary<string, int> customNumFmts = new Dictionary<string, int>();
int nextCustomNumFmtId = 164;

// Quét định dạng ô và cấp phát ID >= 164 cho các chuỗi format tùy biến
if (fmt.NumberFormat == "Date") numFmtId = 14;
else if (!string.IsNullOrEmpty(fmt.CustomFormatCode))
{
    if (!customNumFmts.ContainsKey(fmt.CustomFormatCode))
        customNumFmts[fmt.CustomFormatCode] = nextCustomNumFmtId++;
    numFmtId = customNumFmts[fmt.CustomFormatCode];
}

// Ghi bảng <numFmts> vào xl/styles.xml:
if (customNumFmts.Count > 0)
{
    sw.WriteLine(string.Format("  <numFmts count=\"{0}\">", customNumFmts.Count));
    foreach (var kvp in customNumFmts)
    {
        sw.WriteLine(string.Format("    <numFmt numFmtId=\"{0}\" formatCode=\"{1}\"/>", kvp.Value, EscapeXml(kvp.Key)));
    }
    sw.WriteLine("  </numFmts>");
}
```

#### Vá lỗi 12: Xuất Thẻ Placeholder `<p:ph>` và Chọn Slide Layout Tương Ứng
- **Vị trí 1 (Chọn Layout)**: `FormatHandlers.cs:8143`:
```csharp
int layoutIndex = 2; // Mặc định Title and Content
switch (slide.Layout)
{
    case SlideLayoutType.TitleSlide: layoutIndex = 1; break;
    case SlideLayoutType.TitleAndContent: layoutIndex = 2; break;
    case SlideLayoutType.SectionHeader: layoutIndex = 3; break;
    case SlideLayoutType.TwoContent: layoutIndex = 4; break;
    case SlideLayoutType.Comparison: layoutIndex = 5; break;
    case SlideLayoutType.TitleOnly: layoutIndex = 6; break;
    case SlideLayoutType.Blank: layoutIndex = 7; break;
}
sw.WriteLine(string.Format("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout\" Target=\"../slideLayouts/slideLayout{0}.xml\"/>", layoutIndex));
```
- **Vị trí 2 (Thẻ `<p:ph>`)**: `FormatHandlers.cs:8556`:
```csharp
string phXml = "";
if (shape.PlaceholderType == "Title" || (shape == slide.Shapes.FirstOrDefault(s => s.Type == ShapeType.TextBox && s.FontSize >= 28f)))
{
    phXml = (slide.Layout == SlideLayoutType.TitleSlide) ? "<p:ph type=\"ctrTitle\"/>" : "<p:ph type=\"title\"/>";
}
else if (shape.PlaceholderType == "Subtitle")
{
    phXml = "<p:ph type=\"subTitle\" idx=\"1\"/>";
}
else if (shape.PlaceholderType == "Body")
{
    phXml = "<p:ph idx=\"1\"/>";
}

sw.WriteLine(string.Format("          <p:cNvPr id=\"{0}\" name=\"{1}\"/>", spId, EscapeXml(shapeName)));
sw.WriteLine("          <p:cNvSpPr/>");
sw.WriteLine(string.Format("          <p:nvPr>{0}</p:nvPr>", phXml));
```

---

### 9.3. Giai đoạn 3: Hoàn Thiện Chuẩn Hóa Toàn Diện các Tính Năng Nâng Cao (Dài Hạn)

1. **Bảo Tồn Bản Vẽ Vector DrawingML Cho Writer**:
   Thay thế khối `SmartArtRenderer.RenderToBitmap` tại dòng 7208–7243 bằng hàm tuần tự hóa trực tiếp thẻ DrawingML `<w:drawing><wp:anchor><a:graphic><a:graphicData uri=".../wordprocessingShape"><wps:wsp>`. Giữ nguyên hình học vector `<a:custGeom>` và text bên trong để người dùng Word 365 có thể chỉnh sửa trực tiếp.
2. **Chuyển Đổi Xuất Chuỗi Sang Bảng Chuỗi Dùng Chung (`xl/sharedStrings.xml`)**:
   Xây dựng `Dictionary<string, int> sharedStringMap` trong `XlsxHandler.cs`. Quét toàn bộ chuỗi ký tự trong các sheet, xuất bảng `xl/sharedStrings.xml` và ghi thẻ ô `<c t="s"><v>{id}</v></c>`. Giảm dung lượng tệp 70-80% trên các sổ làm việc lớn.
3. **Tuần Tự Hóa Xác Thực Dữ Liệu (`<dataValidations>`) & AutoFilter trong Calc**:
   Đồng bộ `CalcView.DataValidations` ra thẻ `<dataValidations>` và `CalcView.Tables` ra `xl/tables/table1.xml`, đồng thời thêm `hidden="1"` vào thẻ `<row>` cho các hàng nằm trong `HiddenRows`.
4. **Hỗ Trợ Cấu Trúc Nhóm Hình Dạng `<p:grpSp>` trong Slides**:
   Hiện thực hàm đệ quy `WriteGroupShapeXml` ghi nhận `<p:grpSp>`, tính toán tỷ lệ `chOff`/`chExt`, bảo tồn cấu trúc nhóm phân cấp thay vì san phẳng.
5. **Hỗ Trợ Thẻ Khung Nội Dung `<w:sdt>` trong Writer**:
   Điều chỉnh vòng lặp duyệt `bodyNode.ChildNodes` tại dòng 1663 để khi gặp `child.LocalName == "sdt"`, hệ thống đệ quy vào `<w:sdtContent>` trích xuất các đoạn văn và bảng biểu bên trong, ngăn chặn hoàn toàn việc mất dữ liệu mẫu biểu.

---

## 10. KẾT LUẬN & KHUYẾN NGHỊ KIẾN TRÚC (CONCLUSION & ARCHITECTURAL RECOMMENDATIONS)

### 10.1. Kết luận Đánh giá
Báo cáo phân tích chuyên sâu trên đã vẽ nên bức tranh hiện trạng toàn cảnh, trung thực và chi tiết đến từng dòng lệnh của cả 3 phân hệ ứng dụng trong ZenQ Office đối chiếu với chuẩn quốc tế ECMA-376 / ISO/IEC 29500 và hành vi thực tế của Microsoft Office 365.

Dù được xây dựng hoàn toàn độc lập bằng mã nguồn thuần C# không phụ thuộc thư viện ngoài với nền tảng tính toán và hiển thị rất mạnh mẽ, tầng chuyển đổi định dạng OOXML của ZenQ Office hiện chỉ đạt mức tương thích trung bình **~47.1%**:
- **Writer (~42.8%)**: Bị nghẽn ở khâu gộp ô bảng, xuất kiểu dáng Heading, bảo tồn vector và các phần tử nâng cao (SDT, Footnotes).
- **Calc (~46.5%)**: Bị đe dọa bởi lỗi thiếu tiền tố `_xlfn.` gây hỏng file, lỗi nuốt công thức dùng chung và mất liên kết công thức của biểu đồ.
- **Slides (~48.0%)**: Tồn tại lỗi part mồ côi nghiêm trọng ở tệp ghi chú diễn giả gây hộp thoại File Repair, cơ chế fallback XML trần, mất trắng biểu đồ và bỏ rơi thẻ giữ chỗ `<p:ph>`.
- **Shared DrawingML (~51.0%)**: Bỏ rơi thuộc tính lật hình `flipH`/`flipV`, mất kiểu nét đứt và rụng đầu mũi tên chỉ dẫn.

### 10.2. Khuyến nghị Kiến trúc Chiến lược
1. **Ưu tiên Triệt Tiêu Tuyệt Đối Các Điểm Lỗi Critical**:
   Toàn bộ 6 bản vá lỗi trong Giai đoạn 1 cần được triển khai ngay lập tức. Đây là điều kiện tiên quyết để tài liệu xuất bản từ ZenQ Office không bao giờ bị Microsoft 365 cảnh báo tệp bị hỏng hoặc bật hộp thoại Repair.
2. **Chuẩn Hóa Mô Hình Dữ Liệu Đối Tượng (Object Model Symmetry)**:
   Cần tạo sự đối xứng 1-1 giữa thuộc tính hiển thị trong giao diện UI và thuộc tính xuất/nạp trong bộ Handler (ví dụ: `DocTableCell` phải có `ColSpan`/`RowSpan`; `XlsxFont` phải có `Strikethrough`; `Transform2D` phải được in ra `flipH`/`flipV`).
3. **Thiết Lập Bộ Kiểm Thử Hồi Quy Tự Động Định Dạng (Automated OOXML Round-Trip Testing)**:
   Xây dựng bộ test suite tự động: Tạo tệp -> Lưu ra đĩa -> Nạp lại -> Mở bằng Microsoft 365 COM Interop / OpenXML Validation SDK -> So sánh cây AST để phát hiện sớm mọi sự thoái hóa định dạng trước khi phát hành sản phẩm.

Việc hoàn tất lộ trình kỹ thuật 3 giai đoạn nêu trên sẽ nâng mức độ tuân thủ của ZenQ Office lên trên **85%**, xác lập vị thế của ZenQ Office như một bộ ứng dụng văn phòng tự chủ công nghệ, đạt chuẩn tương thích thương mại hoàn hảo với hệ sinh thái Microsoft 365.
