# BÁO CÁO ĐIỀU TRA & THIẾT KẾ KIẾN TRÚC: NEW TAB LAUNCHER (+) 60+ FPS RADIAL ARC (AI 3)

**Ngày khảo sát**: 17/09/2026  
**Người thực hiện**: Explorer AI 3 (Survey & Investigation)  
**Mục tiêu**: Điều tra toàn diện mã nguồn, xây dựng thiết kế chi tiết cho Task AI 3 theo yêu cầu R3 ("Thiết kế launcher tạo tab mới (+) với hiệu ứng vòng cung radial arc 60+ FPS, nền tối mờ scrim, 4 biểu tượng kính mờ frosted glass Writer/Calc/Slides/PDF, ease-out cubic & smooth spring, double-buffering, dismiss bằng Esc/click-outside") và thiết lập hàng rào bảo vệ tuyệt đối tính năng Writer (R4).

---

## 1. TỔNG QUAN HỆ THỐNG & CÔNG NGHỆ (TECH STACK & BUILD SYSTEM)

### 1.1. Công nghệ nền tảng
- **Ngôn ngữ & Nền tảng**: C# trên .NET Framework 4.8 / Windows Forms (WinForms) với đồ họa vector thuần GDI+ (`System.Drawing`, `System.Drawing.Drawing2D`).
- **Trình biên dịch**: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (phiên bản Visual C# Compiler 4.8 cho C# 5).
- **Quy tắc cú pháp cực kỳ quan trọng**: Chỉ hỗ trợ cú pháp **C# 5**. Tuyệt đối không dùng tính năng C# 6+ như nội suy chuỗi `$"..."`, toán tử null-conditional `?.`, biểu thức thân hàm `=>` cho method/property thông thường, hay local functions. Sử dụng `string.Format(...)`, kiểm tra `!= null` truyền thống.
- **Tập lệnh build**: `build.bat` thực hiện biên dịch toàn bộ `src\*.cs` thành file nhị phân duy nhất `GenOffice.exe` (3.38 MB). Lệnh build đã được kiểm chứng hoạt động thành công 100% với mã lỗi 0 (`[SUCCESS] GenOffice.exe built successfully!`).

### 1.2. Kiến trúc Render & Double-Buffering
- Tại `src/ui/AppForm.cs` (dòng 258–260, 263):
  ```csharp
  DoubleBuffered = true;
  SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
  TimeBeginPeriod(1); // Thiết lập độ phân giải timer hệ điều hành Windows về mức 1ms
  ```
- Toàn bộ bề mặt `AppForm` đã được kích hoạt đệm kép phần cứng (Double Buffering) ở tầng sâu WinForms. Mọi thao tác vẽ trên `Graphics g` trong `OnPaint` được rasterize vào bộ nhớ đệm trước khi BitBlt lên màn hình, triệt tiêu hoàn toàn hiện tượng nhấp nháy (flicker) và xé hình (tearing).
- Bộ đếm thời gian hoạt họa 60 FPS (`animTimer`, 16ms) đã sẵn sàng hoạt động tại `src/ui/AppForm.cs` (dòng 314–350).

---

## 2. HIỆN TRẠNG THANH TAB VÀ NÚT TẠO TAB MỚI (+)

### 2.1. Tọa độ và cơ chế vẽ nút "+" hiện tại
- File phụ trách: `src/ui/RibbonBar.cs`.
- Khai báo vùng biên độ: dòng 233:
  ```csharp
  public Rectangle DocTabPlusBounds = Rectangle.Empty;
  ```
- Tính toán tọa độ và vẽ: dòng 2138–2153:
  ```csharp
  docTabX += tabWidth + 4;
  Rectangle newTabRect = new Rectangle(docTabX + 2, 9, 24, 24);
  DocTabPlusBounds = newTabRect;
  bool isHoverAdd = docClipRect.Contains(HoverX, HoverY) && IsHovering(newTabRect);
  // Vẽ hình cầu nút tròn +
  DrawVectorTabPlus(g, newTabRect.X + 12, newTabRect.Y + 12, 10, ...);
  ```
  - Tọa độ tâm của nút `+`: `PointF center = new PointF(DocTabPlusBounds.X + 12, DocTabPlusBounds.Y + 12)` (thông thường `Y = 21`, `X` chạy theo chiều ngang phụ thuộc số lượng tab đang mở và cuộn ngang `TabScrollX`).

### 2.2. Hành vi khi nhấn nút "+" hiện tại
- Tại `src/ui/RibbonBar.cs` dòng 11031–11041:
  ```csharp
  if (!DocTabPlusBounds.IsEmpty && docClipRect.Contains(x, y) && DocTabPlusBounds.Contains(x, y))
  {
      int idx = DocTabs.Count;
      OfficeAppMode newMode = (CurrentMode == OfficeAppMode.Home) ? OfficeAppMode.Writer : CurrentMode;
      string ext = (newMode == OfficeAppMode.Slides) ? ".pptx" : ((newMode == OfficeAppMode.Calc) ? ".xlsx" : ((newMode == OfficeAppMode.Pdf) ? ".pdf" : ".docx"));
      string namePrefix = (newMode == OfficeAppMode.Slides) ? "Trình chiếu " : ((newMode == OfficeAppMode.Calc) ? "Bảng tính " : ((newMode == OfficeAppMode.Pdf) ? "Tài liệu PDF " : "Văn bản "));
      AddDocTab(namePrefix + idx + ext, newMode, slidesView, writerView, calcView, pdfView);
      if (parentForm != null) parentForm.Invalidate();
      return true;
  }
  ```
- **Hạn chế hiện tại**: Nhấn nút `+` lập tức sinh tab cùng loại mà không cho phép người dùng lựa chọn phân hệ ứng dụng (Writer / Calc / Slides / PDF), thiếu trải nghiệm trực quan hiện đại.

### 2.3. Quy trình tạo và kích hoạt Tab (`AddDocTab`)
- Tại `src/ui/RibbonBar.cs` dòng 779–836:
  ```csharp
  public void AddDocTab(string title, OfficeAppMode mode, SlidesView slidesView = null, WriterView writerView = null, CalcView calcView = null, PdfView pdfView = null)
  ```
  - Hỗ trợ đầy đủ 4 chế độ ứng dụng: `OfficeAppMode.Writer`, `OfficeAppMode.Calc`, `OfficeAppMode.Slides`, `OfficeAppMode.Pdf`.
  - Tự động lưu trạng thái tab cũ (`SaveTabState`), khởi tạo cấu trúc dữ liệu mới cho từng phân hệ (ví dụ: `PieceTable` cho Writer, `SparseMatrix` cho Calc, `PresentationRoot` cho Slides, `PdfDocument` cho PDF), thêm vào danh sách `DocTabs`, đặt `ActiveDocIndex = DocTabs.Count - 1`, cuộn đến tab mới và nạp view (`LoadTabState`).

---

## 3. THIẾT KẾ CHI TIẾT: LAUNCHER VÒNG CUNG RADIAL ARC 60+ FPS (R3)

### 3.1. Mô hình kiến trúc thành phần (Component Architecture)
Để tuân thủ nguyên tắc độc lập giữa các AI, tránh xung đột git merge với AI 1 (phím tắt) và AI 2 (khe hở 1px tab-ribbon), toàn bộ logic của launcher được đóng gói trong một file chuyên trách mới:
`src/ui/NewTabRadialLauncher.cs`
- `AppForm` và `RibbonBar` chỉ cần kết nối thông qua 4 điểm móc tối thiểu (minimal non-invasive hooks):
  1. `RibbonBar.HandleClick` (dòng 11032): Khi click `DocTabPlusBounds`, thay vì tạo tab trực tiếp thì gọi `Launcher.Toggle(new PointF(DocTabPlusBounds.X + 12, DocTabPlusBounds.Y + 12))`.
  2. `AppForm.OnPaint` (sau dòng 427): Nếu `Launcher.IsVisible`, gọi `Launcher.Draw(g, ClientSize.Width, ClientSize.Height)`.
  3. `AppForm.animTimer` (dòng 314–350): Thêm cờ `Launcher.UpdateAnimation()` để kích hoạt `Invalidate()` 60 FPS khi đang bay mở hoặc đóng.
  4. `AppForm.OnMouseDown`, `OnMouseMove`, `OnKeyDown`: Bắt sự kiện bàn phím (phím `Esc`) và chuột (chọn icon hoặc click ra ngoài scrim backdrop).

### 3.2. Động lực học chuyển động (Motion Dynamics) & Toán học nội suy
Theo nguyên lý thiết kế chất lỏng Apple Fluid Motion (WWDC 2018 / WWDC 2026):
1. **Gốc tọa độ phát tán (Anchored Origin)**:
   - Tâm phát sinh `(X0, Y0)` bắt nguồn chính xác từ tâm nút `+` trên màn hình: `X0 = DocTabPlusBounds.X + 12`, `Y0 = DocTabPlusBounds.Y + 12`.
2. **Hình học vòng cung tỏa tia (Radial Arc Geometry)**:
   - Nút `+` nằm ở đỉnh cửa sổ (`Y ≈ 21`). Vòng cung phải tỏa xuống khu vực thân cửa sổ (`Y > 21`).
   - Bán kính mở rộng tối đa: $R = 155\text{ px}$.
   - Góc tỏa tia: Phân bố đều 4 icon theo góc tỏa từ 20° đến 80° (khi `X0 + 170 <= ClientSize.Width`) hoặc đảo chiều từ 100° đến 160° (khi nút `+` nằm sát mép phải màn hình).
   - Tọa độ mục tiêu của mỗi icon $i \in \{0, 1, 2, 3\}$:
     $$X_i = X_0 + R \cdot \cos(\theta_i)$$
     $$Y_i = Y_0 + R \cdot \sin(\theta_i)$$
3. **Nội suy mượt Ease-Out Cubic kết hợp Smooth Spring**:
   - Thời lượng hoạt họa: $T = 280\text{ ms}$ (khoảng 17–18 khung hình ở 60 FPS).
   - Biến thời gian chuẩn hóa: $t = \min(1.0, \Delta t / T)$.
   - Công thức Ease-Out Cubic:
     $$f_{\text{cubic}}(t) = 1 - (1 - t)^3$$
   - Công thức Smooth Spring (bổ sung độ đàn hồi nhẹ 4% chuẩn Apple momentum):
     $$f_{\text{spring}}(t) = f_{\text{cubic}}(t) + \sin(t \cdot \pi) \cdot 0.04 \cdot (1 - t)$$
   - Tọa độ tức thời tại frame $t$:
     $$x_i(t) = X_0 + (X_i - X_0) \cdot f_{\text{spring}}(t)$$
     $$y_i(t) = Y_0 + (Y_i - Y_0) \cdot f_{\text{spring}}(t)$$
     $$\text{scale}(t) = 0.25 + 0.75 \cdot f_{\text{spring}}(t)$$
     $$\text{alpha}(t) = \min(1.0, t \cdot 1.4)$$
4. **Chuẩn 60+ FPS không phụ thuộc trễ timer (Delta-Time Loop)**:
   - Dùng `System.Diagnostics.Stopwatch` để tính $\Delta t$ chính xác đến mili-giây. Dù timer Windows Forms có dao động ±1ms thì đồ họa vẫn chuyển động trơn tru 60+ FPS, không giật cục.

### 3.3. Thiết kế vật liệu kính mờ Frosted Glass & Visual Styling
Kế thừa ngôn ngữ kính mờ đã có tại `LiquidGlassHelper.cs` và `HomeView.cs`:
1. **Nền tối mờ (Darkened Scrim Backdrop)**:
   - Phủ kín toàn bộ `ClientSize` cửa sổ với màu nền tối: `Color.FromArgb((int)(110 * alphaFade), 15, 23, 42)`.
   - Alpha tăng dần từ 0 lên 110 (khoảng 43% độ mờ).
2. **Thẻ kính mờ (Frosted Glass Card)**:
   - Kích thước thẻ: $96 \times 82\text{ px}$, bán kính bo góc $r = 14\text{ px}$.
   - Lớp thân kính: Gradient từ `Color.FromArgb(235, 255, 255, 255)` đến `Color.FromArgb(195, 245, 250, 255)`.
   - Đường viền kính phản quang (Specular Highlight Border): Gradient từ trắng `rgba(255, 255, 255, 0.8)` ở đỉnh xuống viền màu thương hiệu ứng dụng ở đáy.
   - Bóng đổ mềm (Multi-layer Drop Shadow): Độ sâu chân thực `Color.FromArgb(35, 0, 0, 0)`.
   - Vầng hào quang phát sáng (Ambient Hover Aura): Khi rê chuột vào icon, phát quang hào quang 3px theo màu đặc trưng của từng phân hệ.
3. **Bộ 4 biểu tượng ứng dụng (Vector Emblems)**:
   - **Writer**: Xanh dương `#2563eb` (`Color.FromArgb(37, 99, 235)`), biểu tượng trang văn bản với các dòng chữ và góc gấp giấy. Tên: **Writer**, Nhãn phụ: **Văn bản**.
   - **Calc**: Xanh ngọc lục bảo `#059669` (`Color.FromArgb(5, 150, 105)`), biểu tượng bảng lưới tính toán và cột biểu đồ số liệu. Tên: **Calc**, Nhãn phụ: **Bảng tính**.
   - **Slides**: Cam san hô `#ea580c` (`Color.FromArgb(234, 88, 12)`), biểu tượng trang thuyết trình với biểu đồ tròn pie & cột bar. Tên: **Slides**, Nhãn phụ: **Trình chiếu**.
   - **PDF**: Đỏ thắm `#dc2626` (`Color.FromArgb(220, 38, 38)`), biểu tượng tài liệu PDF với dải ruy băng đỏ. Tên: **PDF**, Nhãn phụ: **Tài liệu PDF**.
4. **Hiệu ứng nút "+" xoay đổi trạng thái**:
   - Khi launcher mở ra, dấu `+` xoay 45° thành dấu `×` mượt mà, đồng thời tỏa ra vòng hào quang xung quanh nút.

### 3.4. Cơ chế tương tác & Đóng Launcher (Event Handling & Dismissal)
- **Click vào 1 trong 4 icon**: Gọi `Ribbon.AddDocTab(...)` với mode tương ứng, kích hoạt hiệu ứng đóng mượt hoặc đóng tức thời và chuyển đến tab vừa tạo.
- **Click ra ngoài (Click-outside)**: Nhấn chuột vào bất kỳ vị trí nào trên vùng scrim backdrop (ngoài phạm vi 4 thẻ icon) sẽ kích hoạt đóng launcher và chặn (consume) click để không làm ảnh hưởng đến các nút hay văn bản bên dưới.
- **Phím `Esc` (Escape Dismiss)**: Bắt tại `AppForm.OnKeyDown`, nếu launcher đang hiển thị sẽ đóng ngay lập tức và đặt `e.Handled = true`.

---

## 4. HÀNG RÀO BẢO VỆ TUYỆT ĐỐI TÍNH NĂNG WRITER (R4 STRICT GUARDRAILS)

Yêu cầu R4 đặt ra tiêu chuẩn tối thượng: **Tuyệt đối không can thiệp, không thay đổi, không gây hồi quy (non-regression) bất kỳ tính năng Writer nào đã hoàn thiện**.

### 4.1. Bản đồ chi tiết các thành phần cốt lõi của Writer
| Tính năng | Vị trí file mã nguồn | Các cấu trúc / biến dữ liệu then chốt | Hành động của AI 3 |
|---|---|---|---|
| **Hệ thống 8 Bảng biểu** | `src/ui/DocTableObject.cs`<br>`src/ui/WriterView.cs`<br>`src/format/FormatHandlers.cs` | `DocTableObject`, `DocTableRow`, `DocTableCell`<br>`DocumentTables`, `TableColWidths`, `TableRowHeights`<br>`TableCellBackgrounds`, `TableCellBorders`<br>`ParseTableTag`, `DrawTableObject`, `LoadDocxFile` | **KHÔNG CHẠM**<br>Giữ nguyên 100% |
| **Gõ chỉnh sửa ô bảng (In-cell editing)** | `src/ui/WriterView.cs` (dòng 7400–7800, 11900–12100) | `InCellEditing`, `EditingTableCell`, `ActiveTableIndex`<br>`TableColWidths`, `DragGridlineTableIndex` | **KHÔNG CHẠM**<br>Giữ nguyên 100% |
| **Kích thước dòng/cột bảng** | `src/ui/WriterView.cs` (dòng 10500–10600) | `TableCustomSizes`, `TablePageX`, `TablePageY` | **KHÔNG CHẠM**<br>Giữ nguyên 100% |
| **Chèn ảnh & Hình khối vector** | `src/ui/DocShapeObject.cs`<br>`src/ui/WriterView.cs` (dòng 6940–7260, 8060–8080) | `DocShapeObject`, `DocumentShapes`, `DocumentCharts`<br>`GetOrCreateDocShape`, `DrawVectorShape`, `ParseShapeTag` | **KHÔNG CHẠM**<br>Giữ nguyên 100% |
| **Cơ chế bọc chữ quanh ảnh/bảng (`WrapMode`)** | `src/ui/WriterView.cs` (dòng 6950–7010, 8360–8380, 10860–11040) | `WrapMode`: 0=Inline, 1=Square, 2=Tight, 3=Through, 4=Top/Bottom, 5=Behind, 6=InFront<br>`TableWrapModes`, `HeaderWrapMode` | **KHÔNG CHẠM**<br>Giữ nguyên 100% |

### 4.2. Khảo sát tệp tài liệu mẫu 8 Bảng biểu (`Bài 1.ND GHI.docx`)
- Đã định vị chính xác file mẫu tại: `C:\Users\Danh\Downloads\Bài 1.ND GHI.docx`.
- Đã trích xuất và phân tích cấu trúc tệp DOCX:
  - File chứa chính xác **8 phần tử `<w:tbl>`** trong `word/document.xml`.
  - Toàn bộ các bảng này được xử lý độc lập thông qua `DocxHandler.LoadDocxFile` và đưa vào `WriterView.DocumentTables`.
  - Việc bổ sung launcher nút `+` hoàn toàn nằm ở tầng Shell UI (`AppForm` và `RibbonBar`), không có bất kỳ điểm giao nhau hay ảnh hưởng nào đến bộ giải mã hoặc render bảng biểu của `WriterView`.

---

## 5. KẾ HOẠCH TÍCH HỢP ĐỒNG THỜI VỚI AI 1 VÀ AI 2

Để đảm bảo 3 AI có thể hoàn thành nhiệm vụ song song không xung đột:
- **AI 1** tập trung vào `src/ui/WriterView.cs` (phím tắt `Ctrl+L/E/R/J`, `Ctrl+B/I/U`, `Ctrl+Z/Y`, `Home/End`, `PageUp/Down`).
- **AI 2** tập trung vào đường viền seam 1px giữa Tab Bar và Ribbon Bar tại vùng `ActiveTab.Bottom` (khoảng dòng 2050–2100 trong `src/ui/RibbonBar.cs`).
- **AI 3** tạo file mới độc lập `src/ui/NewTabRadialLauncher.cs`, chỉ chèn hook gọi mở tại dòng 11032 trong `src/ui/RibbonBar.cs` và vẽ overlay tại cuối hàm `OnPaint` trong `src/ui/AppForm.cs`.

---

## 6. THIẾT KẾ MÃ NGUỒN MẪU (PSEUDO-CODE & BLUEPRINT CHO TASK AI 3)

### 6.1. Cấu trúc `NewTabRadialLauncher.cs` (Phác thảo đầy đủ)
```csharp
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public enum LauncherState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    public class LauncherItem
    {
        public OfficeAppMode Mode;
        public string Title;
        public string Subtitle;
        public Color AccentColor;
        public Color DarkColor;
        public float Angle; // Radian
        public RectangleF Bounds;
    }

    public class NewTabRadialLauncher
    {
        private LauncherState _state = LauncherState.Closed;
        private PointF _origin;
        private Stopwatch _stopwatch = new Stopwatch();
        private const float DURATION_MS = 280.0f;
        private float _progress = 0.0f;
        private int _hoveredIndex = -1;
        private LauncherItem[] _items;

        public bool IsVisible { get { return _state != LauncherState.Closed; } }
        public bool IsAnimating { get { return _state == LauncherState.Opening || _state == LauncherState.Closing; } }

        public NewTabRadialLauncher()
        {
            _items = new LauncherItem[]
            {
                new LauncherItem { Mode = OfficeAppMode.Writer, Title = "Writer", Subtitle = "Văn bản", AccentColor = Color.FromArgb(37, 99, 235), DarkColor = Color.FromArgb(29, 78, 216) },
                new LauncherItem { Mode = OfficeAppMode.Calc, Title = "Calc", Subtitle = "Bảng tính", AccentColor = Color.FromArgb(16, 185, 129), DarkColor = Color.FromArgb(5, 150, 105) },
                new LauncherItem { Mode = OfficeAppMode.Slides, Title = "Slides", Subtitle = "Trình chiếu", AccentColor = Color.FromArgb(249, 115, 22), DarkColor = Color.FromArgb(217, 70, 239) },
                new LauncherItem { Mode = OfficeAppMode.Pdf, Title = "PDF", Subtitle = "Tài liệu PDF", AccentColor = Color.FromArgb(220, 38, 38), DarkColor = Color.FromArgb(185, 28, 28) }
            };
        }

        public void Open(PointF origin, int formWidth)
        {
            _origin = origin;
            _state = LauncherState.Opening;
            _stopwatch.Restart();
            CalculateItemAngles(formWidth);
        }

        public void Close()
        {
            if (_state == LauncherState.Closed || _state == LauncherState.Closing) return;
            _state = LauncherState.Closing;
            _stopwatch.Restart();
        }

        public bool UpdateAnimation()
        {
            if (!IsAnimating) return false;
            float elapsed = (float)_stopwatch.ElapsedMilliseconds;
            float t = Math.Min(1.0f, elapsed / DURATION_MS);

            if (_state == LauncherState.Opening)
            {
                _progress = InterpolateSpring(t);
                if (t >= 1.0f)
                {
                    _progress = 1.0f;
                    _state = LauncherState.Open;
                    _stopwatch.Stop();
                }
            }
            else if (_state == LauncherState.Closing)
            {
                _progress = 1.0f - InterpolateEaseOutCubic(t);
                if (t >= 1.0f)
                {
                    _progress = 0.0f;
                    _state = LauncherState.Closed;
                    _stopwatch.Stop();
                }
            }
            return true;
        }

        private float InterpolateEaseOutCubic(float t)
        {
            float inv = 1.0f - t;
            return 1.0f - inv * inv * inv;
        }

        private float InterpolateSpring(float t)
        {
            float cubic = InterpolateEaseOutCubic(t);
            float bounce = (float)(Math.Sin(t * Math.PI) * 0.045f * (1.0f - t));
            return cubic + bounce;
        }

        public void Draw(Graphics g, int width, int height)
        {
            if (!IsVisible) return;

            // 1. Vẽ Scrim tối mờ phủ kín màn hình
            int alpha = (int)(110 * Math.Max(0.0f, Math.Min(1.0f, _progress)));
            using (SolidBrush scrimBrush = new SolidBrush(Color.FromArgb(alpha, 15, 23, 42)))
            {
                g.FillRectangle(scrimBrush, 0, 0, width, height);
            }

            // 2. Vẽ các vòng cung kết nối phát quang (Ambient Radial Arc Guides)
            float radius = 155.0f * _progress;

            // 3. Vẽ 4 thẻ kính mờ (Frosted Glass Cards)
            for (int i = 0; i < _items.Length; i++)
            {
                LauncherItem item = _items[i];
                float cx = _origin.X + radius * (float)Math.Cos(item.Angle);
                float cy = _origin.Y + radius * (float)Math.Sin(item.Angle);
                float cardW = 96;
                float cardH = 82;
                RectangleF cardRect = new RectangleF(cx - cardW / 2, cy - cardH / 2, cardW, cardH);
                item.Bounds = cardRect;

                DrawFrostedGlassCard(g, cardRect, item, _hoveredIndex == i, _progress);
            }
        }

        // Tích hợp xử lý click, hover, và escape...
    }
}
```

---

## 7. KẾT LUẬN & ĐỀ XUẤT HÀNH ĐỘNG
1. **Tính khả thi**: Kiến trúc WinForms + GDI+ đệm kép của GenOffice hoàn toàn lý tưởng để hiện thực hóa launcher vòng cung 60+ FPS với độ trơn tru tuyệt hảo.
2. **Tuân thủ Guardrails**: Hoàn toàn cô lập khỏi phân hệ Writer, đảm bảo tính toàn vẹn 100% của hệ thống bảng biểu và chế độ wrap chữ.
3. **Sẵn sàng triển khai**: Task AI 3 có thể tiến hành viết `src/ui/NewTabRadialLauncher.cs` và gắn kết vào `AppForm.cs` / `RibbonBar.cs` ngay lập tức mà không cần chờ đợi AI 1 hoặc AI 2.
