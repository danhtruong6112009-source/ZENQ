# BÁO CÁO KHẢO SÁT TOÀN DIỆN KIẾN TRÚC OPENXML VÀ ENGINE KẾT XUẤT CANVAS GENOFFICE

**Người thực hiện**: Teamwork Explorer (Codebase Survey Agent 2)  
**Thời gian khảo sát**: 2026-09-21  
**Mục tiêu**: Bóc tách chi tiết mã nguồn GenOffice liên quan đến OpenXML parsing, mô hình SmartArt, hệ thống dựng hình Shape/SmartArt/Math trên Canvas, chẩn đoán tận gốc lỗi méo hình vòng tròn Cycle SmartArt, hở khớp nối node, chuyển đổi đơn vị EMU và viền/màu sắc.

---

## 1. TỔNG QUAN HẠ TẦNG VÀ MÔI TRƯỜNG BIÊN DỊCH

### 1.1. Môi trường biên dịch (`build.bat`)
- **Tập lệnh biên dịch**: `c:\Users\Danh\Desktop\genofficepy\build.bat`
- **Trình biên dịch**: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
- **Phiên bản trình biên dịch**: `Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5`
- **Mục tiêu thực thi**: `/target:winexe /optimize /main:GenOffice.Program /out:GenOffice.exe /recurse:src\*.cs`
- **Các thư viện đã tham chiếu**:
  - Chuẩn .NET: `System.dll`, `System.Drawing.dll`, `System.Windows.Forms.dll`, `System.Xml.dll`, `System.Xml.Linq.dll`, `System.IO.Compression.dll`, `System.IO.Compression.FileSystem.dll`, `Microsoft.CSharp.dll`, `WindowsBase.dll`
  - Bộ Microsoft Open-XML-SDK v3.5.1: `DocumentFormat.OpenXml.dll`, `DocumentFormat.OpenXml.Framework.dll`, `DocumentFormat.OpenXml.Features.dll`, `DocumentFormat.OpenXml.Linq.dll`
  - Thư viện PDF: `PdfiumViewer.dll`, `PdfSharp.dll`
  - Tài nguyên ứng dụng: `GENOFFICE.png`, `GenOffice.ico`

### 1.2. Ràng buộc cú pháp nghiêm ngặt (C# 5 Constraints)
Do trình biên dịch `csc.exe` của .NET Framework 4.0/4.8 chỉ hỗ trợ **C# 5**, mọi đoạn mã viết mới hoặc sửa đổi trong dự án bắt buộc phải tuân thủ nghiêm các quy tắc sau:
1. **Không sử dụng String Interpolation (`$"..."`)**: Phải dùng `string.Format(...)` hoặc phép cộng chuỗi `+`.
2. **Không sử dụng Expression-bodied members (`=>` cho method/property/getter)**: Chỉ lambda inline được dùng `=>`.
3. **Không sử dụng Null-conditional operator (`?.` hoặc `?[]`)**: Bắt buộc kiểm tra `if (obj != null)`.
4. **Không sử dụng Pattern matching (`is Type var`)**: Phải dùng `as` hoặc `if (x is Type) { Type t = (Type)x; }`.
5. **Không sử dụng `out var` inline**: Phải khai báo biến trước lời gọi hàm `int x; TryParse(..., out x);`.
6. **Không sử dụng Auto-property initializers (`public int Prop { get; set; } = 1;`)**: Phải khởi tạo giá trị trong constructor.
7. **Không sử dụng Local functions**: Phải khai báo private helper method hoặc `Func<...>`/`Action<...>`.

---

## 2. KHẢO SÁT MÔ HÌNH VÀ BỘ PHÂN TÍCH SMARTART (SMARTART MODELS & PARSERS)

### 2.1. Hiện trạng `src/format/OpenXmlSmartArtModel.cs`
- **Mục đích thiết kế**:
  - Khai báo lớp `OpenXmlSmartArtNode` (đại diện cho điểm nút `dgm:pt` trong OpenXML) với các thuộc tính: `Id`, `Text`, `FillColor`, `OutlineColor`, `TextColor`, `ShapeType`, `ScaleFactor`, `Level`.
  - Khai báo lớp `OpenXmlSmartArtModel` quản lý dữ liệu sơ đồ: `LayoutType`, `Title`, `VisualStylePreset`, `ColorPaletteIndex`, danh sách `Nodes`.
- **Cơ chế hoạt động thực tế**:
  - Hiện tại, `OpenXmlSmartArtModel` đóng vai trò là cầu nối bộ nhớ (in-memory bridge) giữa thanh Ribbon (`RibbonBar.cs` dòng 14448, 14463, 14485, 14501) và đối tượng hình vẽ `DocShapeObject`.
  - Hàm `FromDocShapeObject(DocShapeObject shape)`: Trích xuất các thuộc tính, màu sắc, hình dáng, scale của từng node từ `shape.NodeColors`, `shape.NodeShapeTypes`, `shape.NodeScaleDeltas` và text từ `shape.GetNodeText(i)`.
  - Hàm `ApplyToDocShapeObject(DocShapeObject shape, Rectangle containerBounds)`: Đồng bộ ngược lại các mảng thuộc tính vào `DocShapeObject`, xóa cache hình ảnh (`ImagePath = null`, `CachedImage = null`) và gọi `SmartArtRenderer.PopulateSubShapeBounds` để tái tính toán khung toạ độ tương đối.
- **Hạn chế lớn nhất**:
  - Dù có khai báo `using DocumentFormat.OpenXml;` và `using DocumentFormat.OpenXml.Packaging;`, tệp này **chưa trực tiếp đọc/ghi các Part OpenXML chuẩn** (`DiagramDataPart`, `DiagramLayoutDefinitionPart`, `DiagramColorsPart`, `DiagramStylePart`).

### 2.2. Hiện trạng bộ phân tích SmartArt trong `src/format/FormatHandlers.cs`
Quá trình bóc tách SmartArt khi nạp file DOCX nằm tại hàm `TryParseSmartArtDiagram` (`FormatHandlers.cs`, dòng 2568-2923):
1. **Kiểm tra định dạng GenOffice legacy**:
   - Dòng 2578-2592: Kiểm tra thẻ `<wp:docPr descr="SmartArt|{type}|{title}|{text}">`. Nếu có, phân rã chuỗi để lấy loại SmartArt, tiêu đề và các mục văn bản.
2. **Kiểm tra DrawingML Diagram chuẩn**:
   - Dòng 2595-2601: Kiểm tra thẻ `<relIds>` hoặc `<a:graphicData uri="...diagram...">`.
   - Tìm kiếm relationship id `dm` (Data Part) và `lo` (Layout Part).
   - Đọc `dmEntry` (`diagrams/data1.xml`): Dùng `XmlDocument` để lọc danh sách thẻ `<dgm:pt>` (bỏ qua `doc`, `parTrans`, `sibTrans`, `pres`), lấy văn bản trong `<a:t>` hoặc placeholder `phldrT`.
   - Đọc `loEntry` (`diagrams/layout1.xml`): Quét thuộc tính `uniqueId` hoặc `title` để nhận diện layout (hỗ trợ phân loại: `Rel_Balance`, `Rel_Funnel`, `Process_Gear`, `Rel_BasicTarget`, `Rel_BasicVenn`, `Cycle_BasicPie`, `Process_BasicChevron`, `Process_BasicTimeline`, `Cycle_Radial`, `List_BasicBlock`, `Process_AlternatingFlow`, `Hierarchy_Tree`, `Matrix_Grid`, `Pyramid_Basic`).
3. **Cơ chế Direct DrawingML Rendering (`RenderDirectDrawingMLDiagram`)**:
   - Dòng 2192-2560: Nếu tìm thấy `drawing1.xml` và `data1.xml`:
     - Trích xuất bảng màu từ `word/theme/theme1.xml` (accent 1-6, lt1, dk1).
     - Đọc toàn bộ hình khối `<a:sp>` và đường nối `<a:cxnSp>` trong `drawing1.xml`, lấy toạ độ gốc EMU: `off.x`, `off.y`, `ext.cx`, `ext.cy`.
     - Tìm bounding box tổng: `totalW = maxX - minX`, `totalH = maxY - minY`.
     - Tạo một `Bitmap` với kích thước `renderW = targetW * 1.5`, `renderH = targetH * 1.5`.
     - Dùng GDI+ để vẽ toàn bộ connector và shape, sau đó lưu ra file tạm PNG trong thư mục `%TEMP%`.
     - **Tạo metadata các node**:
       ```csharp
       // FormatHandlers.cs: dòng 2546-2553
       float relX = (s.X - minX) / (float)totalW;
       float relY = (s.Y - minY) / (float)totalH;
       float relW = s.W / (float)totalW;
       float relH = s.H / (float)totalH;
       ```
       Chuỗi `nodesMeta` được đóng gói dạng `relX,relY,relW,relH,hexCol|...`.
4. **Tạo thẻ đối tượng tài liệu**:
   - Dòng 3204-3215: Tạo thẻ inline Markdown:
     `![SHAPE:ID={id}:TYPE=SmartArt:SMARTART={type}:TITLE={title}:TEXT={text}:STYLE=1:PATH={pngPath}:NODES={nodesMeta}:X={x}:Y={y}:W={w}:H={h}:WRAP={wrap}:PAGE=-1:YREL={yRel}]`
     Đồng thời lưu vào `DocShapeObject.TagCache[shapeId]`.

---

## 3. KHẢO SÁT ENGINE KẾT XUẤT ĐỒ HỌA (RENDERING ENGINES & CANVAS PIPELINE)

### 3.1. Đối tượng hình vẽ `DocShapeObject` (`src/ui/DocShapeObject.cs`)
- Đóng vai trò là mô hình dữ liệu đồ họa trung tâm của Writer.
- Thuộc tính hình học: `PageNumber`, `PageX`, `PageY`, `BaseWidth`, `BaseHeight`, `WidthDelta`, `HeightDelta`, `RotationAngle`, `WrapMode` (0=Inline, 1=Square, 2=Tight, 3=Through, 4=TopBottom, 5=Behind, 6=InFront).
- Thuộc tính SmartArt vector tương tác:
  - `SubShapeRelativeBounds`: Danh sách `RectangleF` lưu vị trí/kích thước tương đối `[0..1]` của từng node.
  - `NodeColors`, `DefaultNodeColors`: Bảng màu riêng từng node.
  - `NodeShapeTypes`: Hình dáng vector riêng của từng node (RoundedRectangle, Oval, Triangle, Chevron...).
  - `NodeScaleDeltas`: Tỉ lệ phóng to/thu nhỏ riêng ($\pm 10\%, \pm 20\%$).
  - `VisualStylePreset`: 0=Flat, 1=Gradient, 2=3D Shadow/Bevel, 3=Glass, 4=Outline.
  - `SelectedNodeIndex`: Chỉ số nút con đang được nhấp chọn chỉnh sửa.

### 3.2. Bộ kết xuất hình dạng `ShapeRenderer` (`src/ui/ShapeRenderer.cs`)
- Phương thức `DrawShape(Graphics g, DocShapeObject shape, Rectangle bounds, RibbonBar ribbon, float zoomLevel)`:
  - Dòng 821-826: Nếu `SubShapeRelativeBounds` trống, gọi `SmartArtRenderer.PopulateSubShapeBounds(bounds, saType, saNodes.Length, shape)`.
  - Dòng 864-872: Đặt cờ `SmartArtRenderer.SuppressNodeBodies = hasVectorNodes;` và gọi `SmartArtRenderer.DrawSmartArt(g, bounds, shape, zoomLevel);` để vẽ nền/đường nối.
  - Dòng 878-1055: **Vòng lặp vẽ trực tiếp các vector node lên Canvas**:
    ```csharp
    // ShapeRenderer.cs: dòng 892-901
    RectangleF rf = shape.SubShapeRelativeBounds[ni];
    float scaleDelta = hasCustomScale ? shape.NodeScaleDeltas[ni] : 0f;
    int baseW = (int)(rf.Width * bounds.Width);
    int baseH = (int)(rf.Height * bounds.Height);
    int nw = Math.Max(6, (int)(baseW * (1f + scaleDelta)));
    int nh = Math.Max(6, (int)(baseH * (1f + scaleDelta)));
    int nx = bounds.X + (int)(rf.X * bounds.Width) - (nw - baseW) / 2;
    int ny = bounds.Y + (int)(rf.Y * bounds.Height) - (nh - baseH) / 2;
    Rectangle nodeRect = new Rectangle(nx, ny, nw, nh);
    ```
    Sau đó áp dụng brush theo `VisualStylePreset` và dựng path thông qua `CreateShapePath(sType, nodeRect)`.
  - Dòng 1058-1065: Nếu node đang được chọn (`SelectedNodeIndex == ni`), vẽ viền nét đứt màu xanh dương và 8 tay nắm chỉnh sửa màu trắng.

### 3.3. Bộ kết xuất SmartArt `SmartArtRenderer` (`src/ui/SmartArtRenderer.cs`)
- Chứa các hàm dựng hình cho hơn 30 mẫu SmartArt được phân phối qua từ điển `renderDispatch`.
- Hàm `PopulateSubShapeBounds(Rectangle bounds, string smartArtType, int count, DocShapeObject shape)`:
  - Sinh toạ độ tương đối `RectangleF` cho các nút con dựa trên hình học sơ đồ.
- Hàm `DrawSmartArt(...)`: Dựng nền sơ đồ, các mũi tên nối, vòng tròn trung tâm, sau đó nếu `SuppressNodeBodies == false` thì vẽ luôn thân các nút và text.

### 3.4. Vòng đời kết xuất Canvas trên `WriterView.cs`
- `WriterView.cs` (17,091 dòng code) quản lý phân trang, cuộn mượt và kết xuất tài liệu:
  - **Quét thẻ đối tượng**: `ScanDocumentShapesAndCharts()` (dòng 3311-3370) quét văn bản trong `PieceTable`, nhận diện thẻ `![SHAPE:...]`, cập nhật hoặc tạo mới `DocShapeObject`.
  - **Tính toán toạ độ hiển thị (Layout Pass)**:
    - Đối với Floating Shape (`WrapMode != 0`):
      ```csharp
      // WriterView.cs: dòng 8326-8331
      int sW = Math.Max((int)(20 * ZoomLevel), (int)((sh.BaseWidth + sh.WidthDelta) * ZoomLevel));
      int sH = Math.Max((int)(15 * ZoomLevel), (int)((sh.BaseHeight + sh.HeightDelta) * ZoomLevel));
      int pTopY = startPageY + sh.PageNumber * (pageH + 30) + marginY;
      int sX = pageX + (int)(sh.PageX * ZoomLevel);
      int sY = pTopY + (int)(sh.PageY * ZoomLevel);
      sh.LastBounds = new Rectangle(sX, sY, sW, sH);
      ```
    - Đối với Inline Shape (`WrapMode == 0`): Được tính toán toạ độ `curX, shapeY` tuần tự theo dòng văn bản trong vòng lặp đoạn văn (dòng 9680-9715).
  - **Vẽ 4 tầng (Z-Order Passes)**:
    - Tầng 1: `WrapMode == 5` (Behind Text) được vẽ trước khi vẽ chữ.
    - Tầng 2: Chữ văn bản, bảng biểu, ảnh inline và shape inline (`WrapMode == 0`).
    - Tầng 3: Floating shapes bao chữ (`WrapMode == 1, 2, 3, 4`).
    - Tầng 4: `WrapMode == 6` (In Front of Text), cùng tay nắm chọn và popup Layout Options.

---

## 4. CHẨN ĐOÁN CHUYÊN SÂU CÁC LỖI TRỌNG TÂM

### 4.1. Chẩn đoán 1: Vì sao vòng tròn trong Cycle SmartArt bị méo thành hình bầu dục (bất đối xứng 1:1)?

Từ khảo sát mã nguồn, chúng tôi phát hiện **3 nguyên nhân gốc rễ liên hoàn** dẫn đến hiện tượng méo elip:

#### Nguyên nhân 1: Sự dị hướng của toạ độ tương đối (Anisotropic Relative Scaling)
Trong `ShapeRenderer.cs` (dòng 894-897):
```csharp
int baseW = (int)(rf.Width * bounds.Width);
int baseH = (int)(rf.Height * bounds.Height);
```
- Khi lưu toạ độ tương đối `rf`, chiều rộng được chuẩn hóa theo `bounds.Width` còn chiều cao chuẩn hóa theo `bounds.Height`.
- Hầu hết các sơ đồ SmartArt trên trang tài liệu có khung chứa chữ nhật (ví dụ: `Width = 480px`, `Height = 270px`, tỉ lệ $16:9$).
- Do đó, dù `rf.Width == rf.Height`, thì khi nhân ngược lại:
  $$\text{baseW} = rf.Width \times 480 \ne \text{baseH} = rf.Height \times 270$$
  Kết quả là $\text{baseW}$ lớn hơn $\text{baseH}$ tới $77.8\%$, kéo giãn vòng tròn thành hình bầu dục nằm ngang!

#### Nguyên nhân 2: Tỉ lệ cố định sai lệch trong `SmartArtRenderer.PopulateSubShapeBounds`
Trong `SmartArtRenderer.cs` (dòng 274-293):
```csharp
else if (type.StartsWith("Cycle_") || type.Contains("Radial") || type.Contains("Ring"))
{
    float bw = Math.Max(10f, bounds.Width);
    float bh = Math.Max(10f, bounds.Height);
    float pixelRad = Math.Min(bw, bh) * 0.33f;
    float relRadX = pixelRad / bw;
    float relRadY = pixelRad / bh;

    float cardW = Math.Min(0.24f, 0.85f / Math.Max(2, count / 2));
    float cardH = Math.Min(0.18f, 0.85f / Math.Max(2, count / 2));
    for (int i = 0; i < count; i++)
    {
        double angleDeg = (i * (360.0 / count)) - 90.0;
        double rad = angleDeg * Math.PI / 180.0;
        float cx = 0.5f + relRadX * (float)Math.Cos(rad);
        float cy = 0.5f + relRadY * (float)Math.Sin(rad);
        shape.SubShapeRelativeBounds.Add(new RectangleF(cx - cardW / 2.0f, cy - cardH / 2.0f, cardW, cardH));
    }
}
```
- Bản thân `cardW` được gán cố định là `0.24f`, còn `cardH` là `0.18f` (ngay từ đầu đã lệch tỉ lệ: $0.24 / 0.18 = 1.333$).
- Khi nhân với `bounds.Width = 400px` và `bounds.Height = 250px`:
  $$\text{nodeW} = 0.24 \times 400 = 96\text{px}, \quad \text{nodeH} = 0.18 \times 250 = 45\text{px}$$
  Tỉ lệ méo hình lên tới $96 / 45 = 2.13:1$ (chiều ngang dài hơn gấp đôi chiều dọc)!

#### Nguyên nhân 3: `ShapeRenderer.CreateShapePath` không áp đặt điều kiện 1:1 cho Oval/Circle
Trong `ShapeRenderer.cs` (dòng 124-127):
```csharp
case "Oval":
case "Circle":
    path.AddEllipse(x, y, w, h);
    break;
```
- Hàm `AddEllipse` của GDI+ vẽ một đường elip nội tiếp trong hình chữ nhật `(x, y, w, h)`.
- Nếu $w \ne h$, nó vẽ elip thay vì hình tròn. Mã nguồn hoàn toàn thiếu bước chuẩn hóa:
  $$\text{dim} = \min(w, h), \quad x' = x + \frac{w - \text{dim}}{2}, \quad y' = y + \frac{h - \text{dim}}{2}, \quad \text{AddEllipse}(x', y', \text{dim}, \text{dim})$$
- Tương tự trong `SmartArtRenderer.DrawCycleCircleArrow` (dòng 1848-1854):
  `rx = r.Width * 0.33f`, `ry = r.Height * 0.33f`, và vẽ vòng bằng `g.DrawArc(ringPen, cx - rx, cy - ry, rx * 2, ry * 2, 0, 360)`. Khi khung hình chữ nhật, chính vòng tròn dẫn hướng cũng bị vẽ thành elip méo mó!

---

### 4.2. Chẩn đoán 2: Vì sao các nút con liền kề bị hở khe hoặc không bắt dính (snap/anchor) chuẩn xác vào vòng tròn?

Khảo sát phát hiện **3 thiếu sót cốt lõi trong thuật toán bố cục hình học**:

1. **Thiếu nút trung tâm (Central Hub Node) trong mô hình dữ liệu**:
   - Trong sơ đồ Radial Cycle của Microsoft Word (hoặc OnlyOffice), luôn tồn tại một nút tròn trung tâm (Core/Root) và các nút vệ tinh xung quanh tiếp xúc với mép ngoài hoặc liên kết vào tâm.
   - Tuy nhiên, trong `PopulateSubShapeBounds`, thuật toán chỉ sinh `count` nút con xung quanh, hoàn toàn **bỏ quên nút trung tâm**.
   - Ở `SmartArtRenderer.DrawCycleRadial` (dòng 453-457), nó chỉ vẽ một đường kẻ elip rỗng ở giữa. Khi chuyển sang chế độ vector tương tác, các nút vệ tinh bay lơ lửng ngoài không gian, để lại khoảng trống mênh mông ở giữa!
2. **Không áp dụng phương trình lượng giác tính khoảng cách tiếp xúc (Chord & Tangency Equations)**:
   - Trong sơ đồ chu trình có $N$ hình tròn tiếp xúc nhau (Segmented / Continuous Cycle):
     Đoạn dây cung kết nối tâm của 2 nút liền kề trên đường tròn bán kính $R$ là:
     $$C = 2 R \sin\left(\frac{\pi}{N}\right)$$
     Để 2 nút tròn tiếp xúc khít nhau không có khe hở ($gap = 0$), đường kính mỗi nút bắt buộc phải là:
     $$D = C = 2 R \sin\left(\frac{\pi}{N}\right)$$
   - Để các nút vệ tinh có bán kính $r_{\text{sat}}$ tiếp xúc khít với nút tròn trung tâm bán kính $R_{\text{center}}$ và đồng thời chạm khít nhau:
     $$R_{\text{orbit}} = R_{\text{center}} + r_{\text{sat}}, \quad r_{\text{sat}} = R_{\text{orbit}} \sin\left(\frac{\pi}{N}\right) \implies r_{\text{sat}} = R_{\text{center}} \frac{\sin(\pi/N)}{1 - \sin(\pi/N)}$$
   - Trong mã nguồn GenOffice hiện tại: Toạ độ tâm được tính bằng $pixelRad = \min(bw, bh) \times 0.33f$, còn kích thước nút lại gán cứng bằng hằng số $0.24$ và $0.18$, hoàn toàn không có mối liên hệ toán học nào với $N$ hay $R$! Vì vậy các nút hoặc đè chùm lên nhau, hoặc hở toác ra.
3. **Mất liên kết mối nối (Connection Anchors / `<a:cxn>`) khi nạp từ OpenXML**:
   - Tại `FormatHandlers.cs` dòng 2544:
     `if (!s.IsConnector && s.Geom != "circularArrow" && s.Geom != "leftCircularArrow")`
     Mã nguồn đã chủ động **bỏ qua toàn bộ các mũi tên uốn tròn và đường nối**. Do đó các nút vệ tinh mất hoàn toàn mỏ neo (anchor points) để neo vào vòng tròn trung tâm.

---

### 4.3. Chẩn đoán 3: Cơ chế chuyển đổi toạ độ và kích thước từ OpenXML (EMU) lên WriterView Canvas

#### Bảng quy đổi chuẩn ECMA-376:
| Đơn vị gốc | Tỉ lệ quy đổi sang EMU | Quy đổi sang Points (pt) | Quy đổi sang Pixels (96 DPI) |
|---|---|---|---|
| **1 Inch** | 914,400 EMU | 72 pt | 96 px |
| **1 Point (pt)** | 12,700 EMU | 1 pt | 1.333 px ($96 / 72$) |
| **1 Pixel (96 DPI)** | 9,525 EMU | 0.75 pt | 1 px |
| **1 Centimet (cm)** | 360,000 EMU | 28.346 pt | 37.795 px |
| **1 Millimet (mm)** | 36,000 EMU | 2.835 pt | 3.780 px |
| **1 Twip / DXA** | 635 EMU | 0.05 pt ($1/20$ pt) | 0.0667 px ($1/15$ px) |

#### Dòng chảy dữ liệu (Data Pipeline) trong GenOffice:
1. **Trích xuất từ WordprocessingML / DrawingML**:
   - Thẻ `<wp:extent cx="..." cy="..."/>`:
     `w = (int)(cx / 9525L);`
     `h = (int)(cy / 9525L);`
     (Quy đổi trực tiếp từ EMU sang 96-DPI screen pixels).
   - Toạ độ dịch chuyển `<wp:posOffset>`:
     `x = Math.Max(0, (int)(emu / 9525L));`
     `y = Math.Max(0, (int)(emu / 9525L));`
   - Đơn vị DXA trong bảng/văn bản:
     `tableRow.HeightPt = hTwips / 20;` (sang Points).
     `w = dxa / 15;` (sang 96-DPI Pixels, vì $\frac{1}{20} \times \frac{96}{72} = \frac{1}{15}$).
2. **Kẹp vùng in ấn (Printable Bounds Clamping)**:
   - `FormatHandlers.cs` (dòng 3075-3080):
     Khổ trang A4 trong GenOffice có chiều rộng chuẩn là `794px` (8.27 inch). Lề trái + phải là 2 inch (`192px`).
     Vùng in tối đa: $794 - 192 = 602\text{px}$.
     Nếu $w > 602$, tỉ lệ được co lại: $w = 602$, $h = (int)(602 \times \frac{h}{w})$.
3. **Chiếu lên Canvas với ZoomLevel trong `WriterView.cs`**:
   - `sW = (int)((sh.BaseWidth + sh.WidthDelta) * ZoomLevel);`
   - `sH = (int)((sh.BaseHeight + sh.HeightDelta) * ZoomLevel);`
   - `sX = pageX + (int)(sh.PageX * ZoomLevel);`
   - `sY = pTopY + (int)(sh.PageY * ZoomLevel);`
   - `sh.LastBounds = new Rectangle(sX, sY, sW, sH);`
4. **Lỗi chuyển đổi phát hiện**:
   - Trong `FormatHandlers.RenderDirectDrawingMLDiagram`:
     Biến `scale` được tính bằng `Math.Min((renderW - 2 * pad) / totalW, (renderH - 2 * pad) / totalH)` (đẳng hướng).
     Tuy nhiên khi xuất sang `nodesMeta`, `relW = s.W / totalW` và `relH = s.H / totalH` lại bị tách độc lập. Khi nạp vào `DocShapeObject` với `bounds` có tỉ lệ khác `totalW / totalH`, hình vẽ bị biến dạng.

---

### 4.4. Chẩn đoán 4: Cơ chế bóc tách và dựng viền (Borders) và màu sắc (Colors)

#### 1. Hệ thống Viền (Borders):
- **Bảng biểu (`w:tblBorders`, `w:tcBorders`)**:
  - `FormatHandlers.cs` dòng 3667-3684: Hiện tại parser chỉ kiểm tra nếu thuộc tính `w:val` bằng `"nil"` hoặc `"none"` để đặt cờ boolean `tblHasTop = false`.
  - Toàn bộ các giá trị kiểu dáng viền của Word như: `single`, `dashed`, `dotted`, `double` (viền đôi), `triple` (viền ba), `threeDEmboss` (nổi 3D), `threeDEngrave` (khắc chìm 3D), `wave` (lượn sóng) và độ dày viền `w:sz` (1/8 pt) đều **chưa được lưu trữ vào `DocTableCell`**.
  - Kết quả: Trên Canvas bảng chỉ vẽ được nét liền mảnh 1px màu xám/đen.
- **Hình khối Vector (`a:ln`)**:
  - `spPr/*[local-name()='ln']`:
    Độ dày viền được tính: `shape.StrokeWidth = Math.Max(0.5f, lnW / 12700.0f);` (chuyển EMU sang Points chuẩn xác).
    Màu viền: lấy từ `<a:srgbClr val="..."/>` hoặc `<a:schemeClr val="..."/>`.
  - Trong `ShapeRenderer.cs` (dòng 1446-1452):
    Hỗ trợ `Pen.DashStyle = shape.OutlineDashStyle` (Solid, Dash, Dot, DashDot).
    **Chưa hỗ trợ**: Viền đôi (`cmpd="dbl"`), viền lồi lõm 3D (`threeDEmboss`).

#### 2. Hệ thống Màu sắc (Theme Colors & Palettes):
- Chuẩn OpenXML sử dụng bảng màu chủ đề (`word/theme/theme1.xml`):
  - 12 màu cơ bản: `dk1`, `lt1`, `dk2`, `lt2`, `accent1`, `accent2`, `accent3`, `accent4`, `accent5`, `accent6`, `hlink`, `folHlink`.
  - Các phép biến đổi màu OpenXML:
    - `<a:tint val="..."/>`: Pha màu trắng (tăng độ sáng).
    - `<a:shade val="..."/>`: Pha màu đen (giảm độ sáng).
    - `<a:lumMod val="..."/>`, `<a:lumOff val="..."/>`: Điều chế độ sáng.
    - `<a:satMod val="..."/>`: Điều chế độ bão hòa.
- Trong GenOffice hiện tại:
  - `FormatHandlers.cs` (dòng 2200-2230) đã đọc thành công 6 màu Accent từ `theme1.xml` và xử lý cơ bản thẻ `<a:tint>`.
  - Nhưng trong `SmartArtRenderer.cs` (dòng 68-95), bảng màu lại được gán tĩnh bằng 6 mảng màu cứng (`presets = new Color[][]`). Khi người dùng đổi Theme trong Word, GenOffice không cập nhật động theo theme.

---

### 4.5. Chẩn đoán 5: Hiện trạng xử lý công thức toán học (OMML / Math Formulas)

- **Vị trí xử lý**: `FormatHandlers.cs` dòng 1594-1604, dòng 3896-3900 và hàm `ExtractMathNodeRecursive` (dòng 3347-3575).
- **Cơ chế hiện tại**:
  - Khi gặp thẻ `<m:oMath>` hoặc `<m:oMathPara>`:
    - Hàm `ExtractMathNodeRecursive` duyệt đệ quy cây thẻ toán học.
    - **Chuyển đổi thành chuỗi ký tự Unicode giả lập**:
      - Phân số `<m:f>`: chuyển thành dạng text ngang `num / den`.
      - Số mũ `<m:sSup>`: chuyển ký tự 0-9 thành ký tự Unicode `⁰¹²³⁴⁵⁶⁷⁸⁹`.
      - Chỉ số dưới `<m:sSub>`: chuyển ký tự 0-9 thành ký tự Unicode `₀₁₂₃₄₅₆₇₈₉`.
      - Căn thức `<m:rad>`: chuyển thành ký hiệu `√`, `∛`, `∜`.
      - Dấu ngoặc `<m:d>`: bọc trong `( ... )`.
      - Tích phân / Tổng `<m:nary>`: chuyển thành `∫` hoặc `∑` kèm chỉ số Unicode.
    - Tạo một đoạn định dạng văn bản thường `FormattedRun` với font `"Times New Roman"`, in nghiêng (`italic = true`), màu đen.
  - **Trên Canvas (`WriterView.cs`)**:
    - Công thức toán học được vẽ bằng lệnh `Graphics.DrawString` như một chuỗi text phẳng thông thường.
    - **Hạn chế**: Không có engine sắp chữ toán học 2D (chưa vẽ được dấu gạch ngang phân số chuẩn, dấu căn bậc kéo dài trùm lên biểu thức, hay ma trận 2D).
  - **Khi xuất DOCX**:
    - Chuỗi Unicode này được ghi ra như một đoạn text thông thường `<w:t>`. Thẻ `<m:oMath>` nguyên bản của Word bị mất hoàn toàn.

---

## 5. ĐỀ XUẤT KIẾN TRÚC VÀ GIẢI PHÁP KỸ THUẬT (ARCHITECTURAL RECOMMENDATIONS)

### 5.1. Giải pháp dứt điểm méo hình vòng tròn 1:1 trong SmartArt

1. **Chuẩn hóa khung hình học vuông trong `PopulateSubShapeBounds`**:
   - Đối với tất cả các mẫu Cycle (`Cycle_Radial`, `Cycle_Basic`, `Cycle_CircleArrowProcess`...):
     Tính toán kích thước nút theo pixel thực tế trước khi chuẩn hóa:
     ```csharp
     float minDim = Math.Min(bounds.Width, bounds.Height);
     float nodeDiamPx = minDim * 0.22f; // Đường kính pixel cố định 1:1
     float relW = nodeDiamPx / bounds.Width;
     float relH = nodeDiamPx / bounds.Height;
     ```
2. **Ép tỉ lệ 1:1 trong `ShapeRenderer.cs`**:
   - Trong nhánh vẽ `Circle` / `Oval` (dòng 894-901 và `CreateShapePath`):
     ```csharp
     if (sType == "Circle" || sType == "Oval")
     {
         int minSide = Math.Min(nw, nh);
         nx += (nw - minSide) / 2;
         ny += (nh - minSide) / 2;
         nw = minSide;
         nh = minSide;
         nodeRect = new Rectangle(nx, ny, nw, nh);
     }
     ```
     Điều này đảm bảo dù khung chứa SmartArt bị co kéo thành hình chữ nhật bất kỳ, vòng tròn con luôn được giữ tròn hoàn hảo 100%!

### 5.2. Giải pháp bắt dính (Snapping & Anchoring) nút con liền kề

1. **Bổ sung Nút Trung Tâm (Node 0) cho Radial Cycle**:
   - `SubShapeRelativeBounds[0]` đặt tại tâm: `new RectangleF(0.5f - relR0_X, 0.5f - relR0_Y, relR0_W, relR0_H)`.
   - Các nút vệ tinh bắt đầu từ chỉ số 1 đến $N$.
2. **Áp dụng công thức tiếp xúc hình học chuẩn xác**:
   - Với $N$ nút con xung quanh:
     $$R_{\text{center}} = \text{minDim} \times 0.20, \quad \theta_i = \frac{2\pi \cdot i}{N} - \frac{\pi}{2}$$
     Bán kính nút vệ tinh:
     $$r_{\text{sat}} = R_{\text{center}} \times \frac{\sin(\pi/N)}{1 + \sin(\pi/N)}$$
     Khoảng cách từ tâm đến các nút vệ tinh:
     $$R_{\text{orbit}} = R_{\text{center}} + r_{\text{sat}}$$
     Khi đó, mép ngoài của các nút vệ tinh **chạm khít hoàn hảo vào nút tròn trung tâm và chạm khít vào nhau**, loại bỏ 100% khe hở rỗng!

### 5.3. Nâng cấp mô hình OpenXML với `DocumentFormat.OpenXml`

- Tận dụng 4 DLL Open-XML-SDK v3.5.1 đã có trong dự án:
  - Sử dụng `DocumentFormat.OpenXml.Drawing.Diagrams.DataModelRoot` để quản lý các node SmartArt.
  - Khi lưu DOCX: Đóng gói đầy đủ các part `diagrams/data1.xml`, `diagrams/layout1.xml`, `diagrams/colors1.xml`, `diagrams/quickStyle1.xml` và quan hệ `.rels` hợp lệ để Microsoft Word mở ra nhận diện 100% SmartArt nguyên bản, cho phép người dùng trên Word tiếp tục chỉnh sửa.

### 5.4. Lộ trình nâng cấp công thức toán học OMML
- **Giai đoạn 1**: Bảo toàn thẻ `<m:oMath>` nguyên vẹn trong snapshot tài liệu để khi xuất DOCX không làm suy hao chất lượng toán học gốc.
- **Giai đoạn 2**: Xây dựng renderer 2D nhẹ trong `ShapeRenderer` hoặc `WriterView` để vẽ phân số (dấu gạch ngang nằm giữa tử và mẫu) và căn thức có gờ kéo dài theo chiều rộng biểu thức bên trong.

---

## 6. KẾT LUẬN

1. Dự án GenOffice có cấu trúc xử lý OpenXML rất phong phú và tham vọng trong `FormatHandlers.cs` và `SmartArtRenderer.cs`.
2. Môi trường biên dịch `csc.exe` của .NET Framework 4.8 đang khóa chặt ở chuẩn ngôn ngữ **C# 5**, đòi hỏi mọi mã nguồn triển khai phải viết theo cú pháp cổ điển, tuyệt đối không dùng tính năng C# 6+.
3. Các lỗi đồ họa cốt lõi về SmartArt (méo vòng tròn, hở khớp nối node) đã được xác định chính xác vị trí dòng lệnh và có phương án giải quyết toán học rõ ràng.
4. Báo cáo này cung cấp đầy đủ cơ sở kỹ thuật để đội ngũ kỹ thuật triển khai trực tiếp các bản vá và nâng cấp chất lượng canvas đạt chuẩn Microsoft Office.
