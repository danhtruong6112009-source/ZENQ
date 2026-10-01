# BÁO CÁO KHẢO SÁT & BẢN THIẾT KẾ KIẾN TRÚC: CÔNG CỤ TẠO TÀI LIỆU WORD MẪU & GROUND TRUTH PIPELINE (R2)

**Document ID**: `survey-r2-word-sample-generator-ground-truth`  
**Date**: 2026-09-21  
**Author**: Word Sample Generator & Ground Truth Architect (Explorer AI 3)  
**Target Scope**: Requirement R2 & Acceptance Criteria trong Master Plan GenOffice:
1. Xây dựng công cụ tạo tài liệu Word mẫu toàn diện (200+ SmartArt diagrams thuộc đủ 8 danh mục, thư viện Shapes đầy đủ, Borders solid/dash/double/3D, Drop Shadow, Gradient, 3D effect, công thức toán học OMML, ký hiệu đặc biệt, font chữ và bảng biểu chuẩn).
2. Thiết kế pipeline bóc tách và trích xuất OpenXML ground truth (`word/document.xml`, `word/diagrams/data1.xml`, `word/diagrams/layout1.xml`, `word/diagrams/drawing1.xml`, v.v.) làm bộ test benchmark đối sánh tự động.
3. Đồng bộ hóa với engine parser và renderer của GenOffice (`OpenXmlSmartArtModel.cs`, `FormatHandlers.cs`, `SmartArtRenderer.cs`, `ShapeRenderer.cs`, `WriterView.cs`).

---

## 1. TỔNG QUAN & PHÂN TÍCH YÊU CẦU R2

### 1.1 Mục tiêu kỹ thuật cốt lõi
Để GenOffice đạt độ tương thích chuẩn xác 100% với Microsoft Word (ECMA-376 DrawingML & WordprocessingML), hệ thống cần một **bộ dữ liệu đối sánh chuẩn (Ground Truth)** không thể bàn cãi. Bộ dữ liệu này phải:
1. Chứa đầy đủ các cấu trúc đồ họa và văn bản phức tạp nhất của Word:
   - **200+ sơ đồ SmartArt** bao phủ trọn vẹn 8 danh mục tiêu chuẩn: **Cycle** (Vòng tròn), **Hierarchy** (Phân cấp / Cây sơ đồ), **Process** (Quy trình), **Matrix** (Ma trận), **Pyramid** (Kim tự tháp), **Relationship** (Mối quan hệ), **List** (Danh sách khối), **Picture** (Sơ đồ gắn ảnh).
   - **Thư viện Hình khối (Shapes)** toàn diện: 9 nhóm chuẩn MS Office, các kiểu viền (Solid, Dash, Double, Compound, 3D Bevel), hiệu ứng đổ bóng (Drop Shadow), tô màu chuyển sắc (Linear/Radial Gradient), chiều sâu và góc xiên 3D (3D Depth/Bevel).
   - **Công thức Toán học OMML (Office MathML)**: Phân số (`<m:f>`), căn số (`<m:rad>`), chỉ số trên/dưới (`<m:sSub>`, `<m:sSup>`, `<m:sSubSup>`), ma trận (`<m:m>`), dấu tích phân, tổng sigma, giới hạn và dấu ngoặc co giãn (`<m:d>`).
   - **Bảng biểu chuẩn (Tables) & Ký hiệu đặc biệt**: Viền `w:tblBorders` phức hợp (single, double, dashed, outset/inset, 3D), đổ bóng nền ô `w:shd`, căn chỉnh lề ô và ký tự đặc biệt.
2. Cung cấp cơ chế **bóc tách (unpack) và chuẩn hóa cấu trúc XML** tự động để trích xuất các part chuẩn của OpenXML:
   - `word/document.xml`: Định vị vị trí đối tượng, kích thước inline/anchor (`<wp:inline>`, `<wp:anchor>`).
   - `word/diagrams/data1.xml`: Mô hình dữ liệu ngữ nghĩa (`<dgm:pt>`, `<dgm:cxn>`).
   - `word/diagrams/layout1.xml`: Đặc tả thuật toán bố cục (`<dgm:layoutDef>`, `<dgm:alg>`, `<dgm:constrLst>`).
   - `word/diagrams/drawing1.xml`: Mô hình vector DrawingML thực tế được render với tọa độ EMU (English Metric Units: 1 pt = 12,700 EMUs), hình học `<a:prstGeom>` / `<a:custGeom>`, màu sắc theme/sRGB và connector `<a:cxnSp>`.
   - `word/diagrams/colors1.xml` & `word/diagrams/quickStyle1.xml`: Biến đổi màu và hiệu ứng trực quan.
   - `word/theme/theme1.xml`: Bảng màu gốc (Accent 1..6, Light 1..2, Dark 1..2).

---

## 2. KHẢO SÁT MÔI TRƯỜNG & ĐÁNH GIÁ CÁC PHƯƠNG ÁN TRIỂN KHAI

### 2.1 Kết quả khảo sát môi trường thực tế trên máy trạm
Quá trình khảo sát trực tiếp hệ thống Windows của máy trạm đã xác nhận các sự thật quan trọng:

1. **Microsoft Word COM Automation (Win32 COM)**:
   - **Đã cài đặt sẵn**: Microsoft Word phiên bản **16.0** (Office 2016/2019/2021/365) hoạt động hoàn hảo thông qua COM Object `Word.Application`.
   - **Kho thư viện SmartArt chuẩn có sẵn**: Khi mở document qua Word COM, `$word.SmartArtLayouts` chứa chính xác **151 layout SmartArt gốc** được phân bổ chuẩn xác vào 8 danh mục:
     - `Process` (Quy trình): **37 layouts**
     - `Relationship` (Mối quan hệ): **30 layouts**
     - `List` (Danh sách): **28 layouts**
     - `Picture` (Hình ảnh): **25 layouts**
     - `Hierarchy` (Phân cấp): **15 layouts**
     - `Cycle` (Vòng tuần hoàn): **9 layouts**
     - `Pyramid` (Kim tự tháp): **4 layouts**
     - `Matrix` (Ma trận): **3 layouts**
   - **Kho mẫu bổ sung**: Thư mục `%APPDATA%\Microsoft\Templates\LiveContent\16\Managed\SmartArt Graphics\1033` chứa thêm **17 tệp `.glox`** (Graphic Layout OpenXML).
   - **OMML Math Engine tích hợp**: `$doc.OMaths.Add($range)` kết hợp `$doc.OMaths.Item(1).BuildUp()` chuyển đổi tức thì chuỗi phương trình tuyến tính (linear format) sang cấu trúc cây 2D OMML chuẩn xác 100%.

2. **Môi trường Python**:
   - Python **3.10.11** và pip **23.0.1** đã cài sẵn.
   - Các gói liên quan: `python-docx 1.2.0`, `lxml 6.1.1`, `pillow 12.3.0`, `openpyxl 3.1.5`, `python-pptx 1.0.2`, `sympy 1.14.0`, `pythonnet 3.1.0`.

3. **Môi trường C# / .NET**:
   - Runtime .NET Framework 4.0/4.8 với trình biên dịch `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.
   - Dự án `genofficepy` đã tích hợp và tham chiếu trực tiếp các assembly OpenXML chính thức của Microsoft:
     - `DocumentFormat.OpenXml.dll`
     - `DocumentFormat.OpenXml.Framework.dll`
     - `DocumentFormat.OpenXml.Features.dll`
     - `DocumentFormat.OpenXml.Linq.dll`
     - `System.IO.Compression.dll` & `System.IO.Compression.FileSystem.dll`
     - `PdfSharp.dll` & `PdfiumViewer.dll`.

4. **Khảo sát thư mục `C:\Users\Danh\Desktop\xml`**:
   - Thư mục này là tài liệu giải nén từ tệp DOCX báo cáo kế hoạch kinh doanh ("Bản thuyết minh..."), dung lượng `document.xml` là 1.83 MB.
   - Tệp này chứa 24 đối tượng hình ảnh DrawingML (`<w:drawing>`), nhưng **hoàn toàn không có SmartArt diagram** (`word/diagrams/`) và **không có công thức toán OMML** (`m:oMath`).
   - *Kết luận*: Thư mục `C:\Users\Danh\Desktop\xml` là tài liệu tham khảo tốt cho cấu trúc Word thông thường (ảnh, paragraph, run style), nhưng **không thể** dùng làm bộ dữ liệu 200+ SmartArt và Math. Cần một công cụ chủ động sinh ra bộ tài liệu mẫu chuẩn.

---

### 2.2 Đánh giá ma trận kỹ thuật: 4 phương án triển khai công cụ tạo mẫu

| Tiêu chí | Phương án 1: Python (`python-docx` + zip) | Phương án 2: C# (.NET OpenXML SDK thuần) | Phương án 3: PowerShell / Word COM Automation | Phương án 4: Kiến trúc Lai (Hybrid Word COM + Python/C# Extractor) - KHUYÊN DÙNG |
|---|---|---|---|---|
| **Khả năng tạo 200+ SmartArt** | 🔴 Kém. `python-docx` không có engine tính toán bố cục constraint/algorithm SmartArt. Chỉ có thể copy paste XML thô. | 🟡 Trung bình. OpenXML SDK chỉ hỗ trợ DOM schema, không có layout engine để tính tọa độ EMU cho `drawing1.xml`. | 🟢 Hoàn hảo. Microsoft Word tự giải bài toán constraint layout và sinh đủ 5 part OpenXML SmartArt. | 🟢 **Tuyệt đối**. Word COM sinh DOCX chuẩn 100%, bộ trích xuất bóc tách và phân loại tự động. |
| **Độ chính xác DrawingML `drawing1.xml`** | 🔴 Không tự tính được tọa độ pixel/EMU. | 🔴 Phải tự viết solver ECMA-376 cực kỳ nặng nề. | 🟢 Sinh chuẩn xác 100% tọa độ EMU gốc từ engine của Word. | 🟢 **100% chuẩn xác theo đặc tả ECMA-376**. |
| **Khả năng tạo công thức OMML** | 🟡 Có thể chèn XML OMML thô nếu tự sinh chuỗi XML. | 🟡 Phải dựng tay từng phần tử `Run`, `Fraction`, `Radical`. | 🟢 Chỉ cần nhập text tự nhiên (Linear syntax) và gọi `BuildUp()`. | 🟢 **Cực nhanh, đa dạng và không có lỗi schema**. |
| **Hiệu ứng 3D, Gradient, Borders** | 🔴 `python-docx` không có API cho 3D Bevel/Shadow. | 🟡 Viết XML schema thủ công rất dài và dễ sai lệch namespace. | 🟢 Word COM hỗ trợ toàn bộ enum thuộc tính `ThreeD`, `Shadow`, `Line`, `Fill`. | 🟢 **Đầy đủ 100% các biến thể thị giác**. |
| **Tính độc lập & Tự động hóa** | 🟢 Chạy CLI độc lập. | 🟢 Chạy CLI độc lập. | 🟡 Yêu cầu môi trường Windows có cài Word (đã thỏa mãn trên máy này). | 🟢 **Script PowerShell/Python chạy tự động 1 lệnh duy nhất**. |

**Quyết định kiến trúc**: **Chọn Phương án 4 (Kiến trúc Lai - Hybrid Pipeline)**.
- **Tầng 1 (Generator - Bộ sinh mẫu chuẩn)**: Kịch bản PowerShell tự động hóa Word COM (`generate_word_samples.ps1`) tận dụng engine đồ họa của Microsoft Word để tạo ra bộ tài liệu chuẩn tuyệt đối chứa 200+ SmartArt, Shapes, Borders và Math OMML.
- **Tầng 2 (Ground Truth Extractor - Bộ trích xuất & chuẩn hóa)**: Kịch bản Python/C# (`extract_ground_truth.py`) tự động giải nén, định dạng Pretty-XML, bóc tách `drawing1.xml`, `data1.xml`, `layout1.xml`, chuyển đổi EMU sang Points/Pixels, trích xuất text và màu sắc, tạo tệp chỉ mục JSON `ground_truth_manifest.json`.
- **Tầng 3 (Visual Baseline Renderer - Bộ kết xuất ảnh tham chiếu)**: Tự động xuất trang tài liệu sang PDF / Ảnh PNG độ phân giải cao thông qua Word COM / PdfiumViewer để làm "Golden Visual Reference" đối sánh điểm ảnh (Pixel-diff / SSIM).

---

## 3. CHI TIẾT DANH MỤC 200+ SMARTART VÀ QUY CÁCH TẠO MẪU

### 3.1 Bao phủ 8 danh mục SmartArt chính (200+ biến thể)
Word COM cung cấp sẵn 151 layout. Để đạt số lượng 200+ mẫu tài liệu đối sánh phong phú, công cụ sinh mẫu kết hợp 151 layout với các ma trận thuộc tính:
1. **Biến thể số lượng nút (Node Cardinality)**: 3 nút, 4 nút, 5 nút, 7 nút, và cấu trúc cây phân cấp 2 tầng / 3 tầng.
2. **Biến thể bảng màu (Color Palettes)**:
   - Primary Accent (Đơn sắc xanh Word #4472C4)
   - Colorful Accents (Đa sắc 6 màu chủ đạo)
   - Accent 1..6 Duotone / Monochromatic
3. **Biến thể phong cách thị giác (Visual Style Presets)**:
   - Simple Flat (Thiết kế phẳng chuẩn)
   - Subtle Gradient (Chuyển sắc nhẹ nhàng)
   - 3D Polished (Bóng bẩy nổi 3D, Bevel cong)
   - 3D Metallic / Brick (Khối kim loại cứng cáp)
   - White / Colored Outline (Viền rỗng nét thanh)

#### Bảng ánh xạ 8 Danh mục & Layout định danh:

| STT | Danh mục (Category) | Số layout gốc | Số biến thể sinh mẫu (Total Targets) | Các Layout đại diện quan trọng |
|---|---|---|---|---|
| 1 | **Cycle (Vòng tuần hoàn)** | 9 | **25 mẫu** | `Basic Cycle`, `Radial Cycle`, `Block Cycle`, `Continuous Cycle`, `Multidirectional Cycle`, `Nondirectional Cycle`, `Cycle Matrix`, `Radial Venn`, `Segmented Cycle`. |
| 2 | **Hierarchy (Phân cấp / Cây)** | 15 | **30 mẫu** | `Organization Chart`, `Picture Organization Chart`, `Name and Title Organization Chart`, `Half Circle Org Chart`, `Hierarchy`, `Labeled Hierarchy`, `Horizontal Hierarchy`, `Horizontal Org Chart`, `Hierarchy List`, `Architecture Layout`, `Table Hierarchy`. |
| 3 | **Process (Quy trình)** | 37 | **45 mẫu** | `Basic Process`, `Basic Chevron Process`, `Continuous Arrow Process`, `Detailed Process`, `Increasing Arrows Process`, `Phased Process`, `Random to Result`, `Staggered Process`, `Step Down / Step Up Process`, `Accent Process`, `Gear Process`, `Timeline Pipe`, `Funnel Process`, `Equation Process`. |
| 4 | **Matrix (Ma trận)** | 3 | **15 mẫu** | `Basic Matrix`, `Grid Matrix`, `Titled Matrix`, `Cycle Matrix` (kết hợp các biến thể 4 góc phần tư có trọng số). |
| 5 | **Pyramid (Kim tự tháp)** | 4 | **15 mẫu** | `Basic Pyramid`, `Inverted Pyramid`, `Segmented Pyramid`, `Pyramid List` (3 tầng, 4 tầng, 5 tầng với nhãn chú thích bên cạnh). |
| 6 | **Relationship (Mối quan hệ)** | 30 | **35 mẫu** | `Balance`, `Counterbalance Arrows`, `Converging Arrows`, `Diverging Arrows`, `Opposing Ideas`, `Basic Venn`, `Linear Venn`, `Radial Venn`, `Stacked Venn`, `Basic Target`, `Nested Target`, `Target List`, `Radial Cluster`, `Hexagon Cluster`. |
| 7 | **List (Danh sách)** | 28 | **35 mẫu** | `Basic Block List`, `Alternating Hexagons`, `Horizontal Bullet List`, `Vertical Box List`, `Vertical Chevron List`, `Vertical Bracket List`, `Tab List`, `Grouped List`, `Trapezoid List`, `Descending Block List`. |
| 8 | **Picture (Sơ đồ gắn ảnh)** | 25 | **30 mẫu** | `Picture Accent Blocks`, `Picture Grid`, `Picture Lineup`, `Picture Strips`, `Bubble Picture List`, `Circle Picture Hierarchy`, `Continuous Picture List`, `Spiral Picture`. |
| **TỔNG** | **8 Danh mục** | **151** | **230 MẪU ĐỐI SÁNH** | **Bao phủ 100% hệ sinh thái SmartArt của Microsoft Office** |

---

### 3.2 Thư viện Shapes, Borders, Shadows, Gradients & 3D
Bộ sinh mẫu sẽ tạo các trang tài liệu chuyên đề kiểm thử hệ thống Shape của GenOffice:

1. **Thư viện 9 nhóm Shape tiêu chuẩn**:
   - Lines (Đường thẳng, Mũi tên đơn, Mũi tên 2 chiều, Elbow Connector, Curved Connector).
   - Rectangles (Hình chữ nhật, Bo tròn 1/2/4 góc, Vát góc Snipped, Vát kèm bo tròn).
   - Basic Shapes (Oval, Tam giác đều, Tam giác vuông, Hình bình hành, Hình thang, Hình thoi, Ngũ giác, Lục giác, Bát giác, Thập tự, Trụ Cylinder, Khối hộp Cube, Donut, Trái tim, Tia sét, Mặt trời, Mặt trăng, Đám mây).
   - Block Arrows (Mũi tên khối Phải, Trái, Lên, Xuống, 2 chiều, 4 chiều, Mũi tên gấp khúc, U-Turn).
   - Equation Shapes (Dấu Cộng, Trừ, Nhân, Chia, Bằng, Khác).
   - Flowchart (Process, Decision, Data, Document, Terminator, Preparation).
   - Stars and Banners (Sao 4/5/6/8/10/12/16/24/32 cánh, Sóng nổ Explosion, Cuộn giấy Banners/Scrolls).
   - Callouts (Chú thích hình chữ nhật, bo góc, oval, đám mây, đường kẻ con trỏ).

2. **Hệ thống viền (Borders)**:
   - Kiểu nét (`DashStyle`): Solid, Dash, RoundDot, SquareDot, DashDot, DashDotDot.
   - Độ dày (`Weight`): 0.5 pt, 1.0 pt, 1.5 pt, 2.25 pt, 3.0 pt, 4.5 pt, 6.0 pt.
   - Kiểu viền phức hợp (`CompoundStyle`): Single, Double (Thin-Thin: nét đôi song song), Thick-Thin, Thin-Thick, Triple.
   - Bảng viền bảng biểu (`w:tblBorders`): `val="single"`, `val="double"`, `val="dashed"`, `val="dotted"`, `val="ridge"`, `val="groove"`, `val="inset"`, `val="outset"`.

3. **Hiệu ứng thị giác hiện đại (Visual Effects)**:
   - **Drop Shadow**: Outer Shadow (offset 3px, 5px; hướng 45°, 90°, 135°, 225°), Inner Shadow, Perspective Shadow.
   - **Gradient Fill**: Two-Color Gradient (Linear ngang, dọc, chéo 45°), Radial Gradient (tỏa từ tâm ra viền), Preset Gradients (Gold, Ocean, Sapphire).
   - **3D Effects**:
     - 3D Bevel Top: Circle (bo cong mượt), Relaxed, Slope, Convex, Cross.
     - 3D Depth & Extrusion: Chiều sâu đùn khối 12pt, 24pt, 36pt với màu thân khối tối màu.
     - 3D Rotation: Góc quay trục X/Y/Z (Isometric Top Up, Isometric Right Down, Perspective Relaxed).

---

### 3.3 Công thức toán học OMML (Office MathML)
Tài liệu mẫu toán học sẽ bao gồm các bài toán và công thức kinh điển đại diện cho toàn bộ schema `m:oMath`:
1. **Phân số & Căn thức (`<m:f>`, `<m:rad>`)**:
   - Công thức nghiệm phương trình bậc hai: $x = \frac{-b \pm \sqrt{b^2 - 4ac}}{2a}$
   - Phân số liên tục nhiều tầng: $x = 1 + \frac{1}{1 + \frac{1}{1 + \frac{1}{2}}}$
2. **Chỉ số trên & dưới (`<m:sSub>`, `<m:sSup>`, `<m:sSubSup>`)**:
   - Định lý Pythagoras mở rộng: $a^2 + b^2 = c^2$, phương trình Einstein: $E = m c^2$
   - Chỉ số dãy số: $x_1, x_2, \dots, x_n$, tổ hợp chập: $C_n^k = \frac{n!}{k!(n-k)!}$
3. **Tích phân, Tổng & Giới hạn (`<m:nary>`, `<m:limLow>`)**:
   - Tích phân xác định: $\int_{a}^{b} f(x) \, dx = F(b) - F(a)$
   - Tổng chuỗi vô hạn: $\sum_{n=1}^{\infty} \frac{1}{n^2} = \frac{\pi^2}{6}$
   - Giới hạn lượng giác: $\lim_{x \to 0} \frac{\sin x}{x} = 1$
4. **Ma trận & Dấu ngoặc co giãn (`<m:m>`, `<m:d>`)**:
   - Ma trận 2x2, 3x3 với ngoặc vuông và ngoặc đơn: $\begin{bmatrix} a & b \\ c & d \end{bmatrix}$
   - Hệ phương trình 2 ẩn dùng ngoặc nhọn piecewise: $\begin{cases} 2x + 3y = 7 \\ 4x - y = 5 \end{cases}$

---

## 4. BẢN THIẾT KẾ GROUND TRUTH EXTRACTION & VERIFICATION PIPELINE

Pipeline đối sánh tự động được thiết kế thành chu trình khép kín 4 giai đoạn:

```
[Phase 1: Generator] 
   powershell generate_samples.ps1 (Word COM)
         │
         ▼
   Bộ tệp DOCX Ground Truth (Samples/*.docx)
         │
         ├─────────────────────────────────────────┐
         ▼                                         ▼
[Phase 2: Unpack & Manifest]             [Phase 3: Visual Baseline]
   python extract_ground_truth.py           Word COM / PDF Renderer
         │                                         │
         ▼                                         ▼
   - word/document.xml                      - Reference_Page1.png
   - word/diagrams/data1.xml                - Reference_Page2.png
   - word/diagrams/drawing1.xml             - Reference_Nodes.png
   - word/theme/theme1.xml                         │
   - ground_truth_index.json                       │
         │                                         │
         └────────────────────┬────────────────────┘
                              ▼
                 [Phase 4: GenOffice Verification Engine]
                              │
            ┌─────────────────┴─────────────────┐
            ▼                                   ▼
   [A. Structural Verification]        [B. Visual Fidelity Verification]
   - Parse OpenXML nodes & rels        - Render Canvas to Bitmap
   - Match Node Text & Count           - Pixel-diff & SSIM compare
   - Assert Circle Aspect Ratio 1:1    - Verify Gap = 0 on dock borders
```

---

### 4.1 Chi tiết Giai đoạn 1: Bộ sinh mẫu `generate_word_samples.ps1`
Tận dụng trực tiếp Microsoft Word COM (`Word.Application`) trên máy tính để tạo tệp DOCX chuẩn hóa.

```powershell
# Script: tests/generators/generate_word_samples.ps1
param(
    [string]$OutputDir = "$PSScriptRoot/../../ground_truth_samples"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }

Write-Host "=== KHỞI ĐỘNG MS WORD COM AUTOMATION GENERATOR ===" -ForegroundColor Cyan
$word = New-Object -ComObject 'Word.Application'
$word.Visible = $false
$word.DisplayAlerts = 0

try {
    # 1. SINH TÀI LIỆU BỘ TOÀN BỘ SMARTART 8 DANH MỤC
    $doc = $word.Documents.Add()
    $layouts = $word.SmartArtLayouts
    Write-Host "Tìm thấy $($layouts.Count) layout SmartArt có sẵn." -ForegroundColor Green

    $manifest = @()
    $counter = 1

    foreach ($layout in $layouts) {
        $cat = $layout.Category
        $name = $layout.Name
        $id = $layout.Id
        
        Write-Host "[$counter/$($layouts.Count)] Generating SmartArt: [$cat] $name..."
        
        # Thêm trang mới hoặc tạo file docx riêng lẻ để test cô lập từng layout
        $singleDoc = $word.Documents.Add()
        
        # Chèn SmartArt với kích thước tiêu chuẩn 500pt x 320pt
        $shape = $singleDoc.Shapes.AddSmartArt($layout, 50, 50, 500, 320)
        $sa = $shape.SmartArt
        
        # Điền nội dung kiểm thử chuẩn hóa
        for ($ni = 1; $ni -le $sa.AllNodes.Count; $ni++) {
            $node = $sa.AllNodes.Item($ni)
            $node.TextFrame2.TextRange.Text = "Node $ni: $name"
        }

        # Lưu DOCX
        $safeName = ($name -replace '[^a-zA-Z0-9_]', '_')
        $docxPath = Join-Path $OutputDir "SmartArt_${cat}_${safeName}.docx"
        $singleDoc.SaveAs2($docxPath, 16) # wdFormatXMLDocument = 16
        
        # Xuất ảnh tham chiếu PDF / PNG
        $pdfPath = Join-Path $OutputDir "SmartArt_${cat}_${safeName}.pdf"
        $singleDoc.SaveAs2($pdfPath, 17) # wdFormatPDF = 17
        $singleDoc.Close(0)

        $manifest += [PSCustomObject]@{
            Index = $counter
            Category = $cat
            Name = $name
            Id = $id
            DocxFile = "SmartArt_${cat}_${safeName}.docx"
            PdfFile = "SmartArt_${cat}_${safeName}.pdf"
        }
        $counter++
    }

    # 2. SINH TÀI LIỆU TOÁN HỌC OMML
    Write-Host "Generating Math & OMML Ground Truth..." -ForegroundColor Yellow
    $mathDoc = $word.Documents.Add()
    $mathEquations = @(
        "x = (-b \pm \sqrt(b^2 - 4ac))/(2a)",
        "E = mc^2",
        "a^2 + b^2 = c^2",
        "\int_a^b f(x)dx = F(b) - F(a)",
        "\sum_(n=1)^\infty 1/n^2 = \pi^2/6",
        "\lim_(x\to 0) (sin x)/x = 1"
    )
    foreach ($eq in $mathEquations) {
        $p = $mathDoc.Paragraphs.Add()
        $r = $p.Range
        $r.Text = $eq
        $mathDoc.OMaths.Add($r) | Out-Null
        $mathDoc.OMaths.Item($mathDoc.OMaths.Count).BuildUp()
    }
    $mathDoc.SaveAs2((Join-Path $OutputDir "Math_OMML_Suite.docx"), 16)
    $mathDoc.SaveAs2((Join-Path $OutputDir "Math_OMML_Suite.pdf"), 17)
    $mathDoc.Close(0)

    # 3. SINH TÀI LIỆU SHAPES & BORDERS TOÀN DIỆN
    Write-Host "Generating Shapes & Borders Ground Truth..." -ForegroundColor Yellow
    $shapeDoc = $word.Documents.Add()
    # Thêm Rectangle với Gradient và Drop Shadow
    $sh1 = $shapeDoc.Shapes.AddShape(1, 60, 60, 200, 100) # msoShapeRectangle
    $sh1.Fill.TwoColorGradient(1, 1) # msoGradientHorizontal
    $sh1.Line.Weight = 3.0
    $sh1.Line.Style = 2 # msoLineThinThin (Double)
    $sh1.Shadow.Type = 21 # Drop Shadow
    
    # Thêm Oval với 3D Bevel
    $sh2 = $shapeDoc.Shapes.AddShape(9, 300, 60, 150, 150) # msoShapeOval (1:1 Ratio)
    $sh2.ThreeD.BevelTopType = 2 # msoBevelCircle
    $sh2.ThreeD.Depth = 20

    $shapeDoc.SaveAs2((Join-Path $OutputDir "Shapes_Borders_3D_Suite.docx"), 16)
    $shapeDoc.SaveAs2((Join-Path $OutputDir "Shapes_Borders_3D_Suite.pdf"), 17)
    $shapeDoc.Close(0)

    # Ghi Manifest JSON
    $manifest | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputDir "ground_truth_manifest.json") -Encoding UTF8
    Write-Host "=== HOÀN TẤT SINH GROUND TRUTH THÀNH CÔNG ===" -ForegroundColor Green
}
finally {
    $word.Quit()
}
```

---

### 4.2 Chi tiết Giai đoạn 2: Bộ bóc tách OpenXML `extract_ground_truth.py`
Kịch bản Python chịu trách nhiệm giải nén tệp DOCX, đọc cấu trúc XML, trích xuất dữ liệu số học định lượng và chuẩn hóa để phục vụ so sánh tự động:

1. **Trích xuất `word/diagrams/data1.xml`**:
   - Duyệt qua danh sách `<dgm:pt>` để lấy ID, Type (`node`, `asst`), và Text từ các thẻ `<a:t>`.
   - Duyệt qua `<dgm:cxn>` để lập bản đồ quan hệ cha - con (Parent-Child) và trình bày (`presOf`).
2. **Trích xuất `word/diagrams/drawing1.xml` (Tọa độ trực quan)**:
   - Đọc các phần tử `<dgm:sp>` và `<dgm:cxnSp>`.
   - Lấy tọa độ `<a:off x="..." y="..."/>` và kích thước `<a:ext cx="..." cy="..."/>` theo đơn vị EMU.
   - Chuyển đổi chính xác: $\text{Width (pt)} = \frac{\text{cx}}{12700}$, $\text{Height (pt)} = \frac{\text{cy}}{12700}$.
   - Kiểm tra thuộc tính hình học: `<a:prstGeom prst="...">` hoặc đường dẫn vector `<a:custGeom>`.
   - Kiểm tra tỷ lệ vòng tròn: Với `prst="ellipse"` hoặc sơ đồ hình tròn, khẳng định tỷ lệ:
     $$\text{Aspect Ratio} = \frac{\text{cx}}{\text{cy}} = 1.000 \quad (\pm 0.005)$$
3. **Trích xuất `word/theme/theme1.xml`**:
   - Trích xuất bảng mã màu hex RGB cho `accent1` đến `accent6`, `dk1`, `lt1`.
4. **Trích xuất `word/document.xml`**:
   - Đo kích thước toàn khung diagram: `<wp:extent cx="..." cy="..."/>`.
   - Kiểm tra chế độ bọc văn bản (`<wp:inline>` hoặc `<wp:anchor>`).
5. **Sinh tệp cấu trúc chuẩn hóa `ground_truth_spec.json`**:
   Lưu trữ toàn bộ thông số định lượng vào tệp JSON để các bài test C# có thể `Assert` trực tiếp mà không cần phụ thuộc Word.

---

### 4.3 Chi tiết Giai đoạn 3: Visual Baseline (Render tham chiếu)
Để kiểm tra độ tương đồng thị giác (Visual Fidelity):
- Tệp PDF được tạo từ Word COM (`SaveAs2(..., wdFormatPDF)`) sẽ được chuyển đổi sang hình ảnh PNG độ phân giải cao (300 DPI) sử dụng thư viện `pdf2image` hoặc `PdfiumViewer.dll` đã có sẵn trong GenOffice.
- Ảnh này là **Golden Standard 100%** từ chính Microsoft Word.
- Engine của GenOffice (`SmartArtRenderer.RenderToBitmap`) sẽ kết xuất cùng một đối tượng SmartArt sang Bitmap với cùng kích thước.
- Kỹ thuật đối sánh:
  1. **Structural Match**: Kiểm tra bounding box của từng node, số lượng node, thứ tự text (Pass nếu độ lệch vị trí $\le 1.5 \text{ pt}$).
  2. **Circle 1:1 Invariant Match**: Kiểm tra đường viền vòng tròn tâm và các node vệ tinh. Trục ngang $R_x$ và trục dọc $R_y$ phải bằng nhau tuyệt đối ($R_x == R_y$).
  3. **Visual Perceptual Diff**: Tính toán sai khác hình ảnh qua pHash hoặc SSIM (Structural Similarity Index $\ge 0.92$).

---

## 5. GIẢI PHÁP ĐỒNG BỘ CHO CANVAS RENDERER TRONG GENOFFICE (R3)

Qua việc phân tích `SmartArtRenderer.cs`, `ShapeRenderer.cs` và `FormatHandlers.cs`, chúng tôi đã xác định được nguyên nhân chính xác gây méo vòng tròn thành hình bầu dục và khe hở nối node:

### 5.1 Nguyên nhân gốc rễ gây méo vòng tròn (Hình bầu dục thay vì tròn 1:1)
Tại dòng 1848–1854 trong `SmartArtRenderer.cs` (`DrawCycleCircleArrow`):
```csharp
int rx = Math.Max(30, (int)(r.Width * 0.33f));
int ry = Math.Max(30, (int)(r.Height * 0.33f));
g.DrawArc(ringPen, cx - rx, cy - ry, rx * 2, ry * 2, 0, 360);
```
- Khi khung chứa của SmartArt có chiều rộng khác chiều cao (ví dụ: $W = 500$, $H = 300$), $r_x = 165$, trong khi $r_y = 99$.
- Hàm `DrawArc` vẽ một Elip có bán kính ngang gấp 1.66 lần bán kính dọc!
- Tọa độ các nút vệ tinh được tính:
  ```csharp
  int nx = (int)(cx + rx * Math.Cos(ang));
  int ny = (int)(cy + ry * Math.Sin(ang));
  ```
  khiến toàn bộ các nút bị kéo dẹt theo chiều elip, phá hỏng hoàn toàn tỷ lệ tròn chuẩn 1:1.

### 5.2 Giải pháp chuẩn hóa Toán học: Đảm bảo tròn 1:1 tuyệt đối và Tự động gắn kết (Zero-Gap Docking)
Để đáp ứng Acceptance Criteria:
1. **Khóa tỷ lệ bán kính 1:1**:
   ```csharp
   // Luôn lấy min(Width, Height) để giữ bán kính R duy nhất:
   int diameter = Math.Min(r.Width, r.Height);
   int R = (int)(diameter * 0.35f); // Bán kính chuẩn 1:1
   // Vòng tròn trung tâm luôn có width == height == R * 2:
   Rectangle centerCircleRect = new Rectangle(cx - R, cy - R, R * 2, R * 2);
   g.DrawEllipse(ringPen, centerCircleRect);
   ```

2. **Gắn kết tự động hai bên vào đường tròn chuẩn (Zero-Gap Tangential Attachment)**:
   Đối với các sơ đồ có ô khối nằm 2 bên tiếp xúc trực tiếp vào vòng tròn (như sơ đồ Cycle có thẻ chữ 2 cánh):
   - Thay vì đặt ô cách vòng tròn một khoảng hở tùy ý, tọa độ mép tiếp xúc của thẻ chữ phải được tính theo phương trình tiếp tuyến:
     $$X_{\text{contact}} = C_x \pm R \cdot \cos(\theta), \quad Y_{\text{contact}} = C_y + R \cdot \sin(\theta)$$
   - Thẻ chữ bên trái có mép phải trùng khớp $X_{\text{right}} = X_{\text{contact}}$, thẻ chữ bên phải có mép trái trùng khớp $X_{\text{left}} = X_{\text{contact}}$.
   - Loại bỏ hoàn toàn khe hở 1–3 pixel, đảm bảo tiếp xúc liền khối hoàn mỹ như Microsoft Word.

3. **Tận dụng `drawing1.xml` khi mở tài liệu**:
   Trong `FormatHandlers.cs`, khi DOCX có sẵn `word/diagrams/drawing1.xml`, phương thức `RenderDirectDrawingMLDiagram` đã tính toán scale đồng nhất:
   ```csharp
   float scale = Math.Min((renderW - 2 * pad) / (float)totalW, (renderH - 2 * pad) / (float)totalH);
   ```
   Do đó, mọi hình tròn 1:1 trong DrawingML gốc của Word sẽ được giữ nguyên 100% tỷ lệ hình học khi mở trong GenOffice!

---

## 6. KẾ HOẠCH HÀNH ĐỘNG TRIỂN KHAI CHO MILESTONE 2

Để hoàn tất trọn vẹn Requirement R2 và chuẩn bị sẵn sàng cho Milestone 3 & 4:

| Bước | Nhiệm vụ cụ thể | Đầu ra dự kiến | Công cụ thực hiện |
|---|---|---|---|
| **B1** | Xây dựng kịch bản PowerShell sinh 200+ SmartArt và Samples | `tests/generators/generate_word_samples.ps1` | PowerShell + Word COM |
| **B2** | Chạy kịch bản sinh bộ DOCX ground truth | Thư mục `tests/ground_truth/samples/*.docx` (200+ tệp) | Word COM Engine |
| **B3** | Xây dựng kịch bản trích xuất XML và tạo tệp chỉ mục JSON | `tests/generators/extract_ground_truth.py` | Python 3.10 (`zipfile`, `lxml`) |
| **B4** | Trích xuất toàn bộ `drawing1.xml`, `data1.xml`, `layout1.xml` | Thư mục `tests/ground_truth/unpacked/` & `ground_truth_manifest.json` | Python Extractor |
| **B5** | Xây dựng bộ test đối sánh tự động C# (Unit / Integration Test) | `src/test/GroundTruthVerificationTests.cs` | C# .NET / `csc.exe` |
| **B6** | Cập nhật `SmartArtRenderer.cs` và `ShapeRenderer.cs` sửa lỗi méo vòng tròn | Mã nguồn C# đạt chuẩn 1:1 Aspect Ratio và Zero-Gap Docking | C# WinForms GDI+ |

---

## 7. KẾT LUẬN

1. **Khả thi 100%**: Môi trường Windows hiện tại có đầy đủ Microsoft Word 16.0 COM, Python 3.10 và .NET C# compiler `csc.exe`.
2. **Kho SmartArt phong phú**: Word COM cung cấp sẵn 151 layout tiêu chuẩn bao phủ đủ 8 nhóm danh mục, kết hợp với các preset phong cách và số nút sẽ dễ dàng vượt mốc 200+ diagram mẫu chất lượng cao.
3. **Chất lượng Ground Truth tối thượng**: Tệp DOCX sinh ra từ Word COM chứa toàn bộ các part OpenXML chuẩn mực (`data1.xml`, `layout1.xml`, `drawing1.xml`, `oMath`), đảm bảo GenOffice có bộ dữ liệu benchmark chính xác tuyệt đối để đối sánh.
4. **Đường lối kỹ thuật rõ ràng**: Đã định vị chính xác vị trí gây lỗi méo elip trong `SmartArtRenderer.cs` và cung cấp giải pháp toán học khóa bán kính 1:1 và gắn kết không khe hở.
