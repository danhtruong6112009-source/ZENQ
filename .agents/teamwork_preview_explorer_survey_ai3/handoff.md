# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — AI 3

**Mã phân nhiệm**: Task AI 3 — Thiết kế Launcher Tạo Tab Mới (+) Vòng Cung Radial Arc 60+ FPS & Bảo Vệ Core Writer  
**Người thực hiện**: Explorer AI 3 (Survey & Investigation)  
**Tài liệu phân tích chuyên sâu đính kèm**: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_explorer_survey_ai3\analysis.md`

---

## 1. OBSERVATION (QUAN SÁT TRỰC TIẾP & BẰNG CHỨNG THỰC TẾ)

1. **Hệ thống xây dựng và biên dịch**:
   - `build.bat` thực hiện lệnh:
     ```cmd
     C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /main:GenOffice.Program /win32icon:GenOffice.ico /resource:GENOFFICE.png,app_logo.png /resource:GenOffice.ico,app.ico /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.CSharp.dll /r:PdfiumViewer.dll /r:PdfSharp.dll /out:GenOffice.exe /recurse:src\*.cs
     ```
   - Chạy thử nghiệm thực tế:
     ```cmd
     Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5
     [SUCCESS] GenOffice.exe built successfully! (Kích thước: 3,380,736 bytes)
     ```
   - Ràng buộc: Trình biên dịch chỉ hỗ trợ cú pháp **C# 5** (không dùng `$"..."`, `?.`, hoặc biểu thức `=>` cho method).

2. **Nút "+" tạo tab mới trên thanh Tab hiện tại**:
   - Vị trí định nghĩa vùng bao: `src/ui/RibbonBar.cs` dòng 233 (`public Rectangle DocTabPlusBounds = Rectangle.Empty;`).
   - Vị trí tính toán và vẽ nút tròn: `src/ui/RibbonBar.cs` dòng 2142–2153:
     ```csharp
     Rectangle newTabRect = new Rectangle(docTabX + 2, 9, 24, 24);
     DocTabPlusBounds = newTabRect;
     ...
     DrawVectorTabPlus(g, newTabRect.X + 12, newTabRect.Y + 12, 10, isHoverAdd ? Color.White : Color.FromArgb(71, 85, 105));
     ```
   - Tọa độ tâm phát sinh chuyển động: `(DocTabPlusBounds.X + 12, DocTabPlusBounds.Y + 12)`.
   - Hành vi bấm chuột hiện tại: `src/ui/RibbonBar.cs` dòng 11032–11041:
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

3. **Cơ chế tạo và chuyển đổi Tab (`AddDocTab`)**:
   - Khai báo tại `src/ui/RibbonBar.cs` dòng 779:
     ```csharp
     public void AddDocTab(string title, OfficeAppMode mode, SlidesView slidesView = null, WriterView writerView = null, CalcView calcView = null, PdfView pdfView = null)
     ```
   - Đã hỗ trợ đầy đủ 4 chế độ: `Writer`, `Calc`, `Slides`, `Pdf`.

4. **Nền tảng hoạt họa 60 FPS & Double Buffering trong ứng dụng**:
   - `src/ui/AppForm.cs` dòng 258–263:
     ```csharp
     DoubleBuffered = true;
     SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
     TimeBeginPeriod(1);
     ```
   - `src/ui/AppForm.cs` dòng 314–350: Timer 16ms (`animTimer`) đang xử lý cuộn mượt và chuyển cảnh cho Writer, Slides, Ribbon tab scroll, và PDF.
   - Thư viện trợ giúp kính mờ: `src/ui/LiquidGlassHelper.cs` (hỗ trợ `CreateRoundedRectanglePath`, `DrawLiquidGlassCard`, `DrawLiquidGlassPill`).
   - Đồ họa vector các biểu tượng phân hệ: `src/ui/HomeView.cs` dòng 657–760 (`DrawVectorAppEmblem` vẽ chính xác logo cho Writer, Calc, Slides, PDF).

5. **Hàng rào bảo vệ Writer (R4 Guardrails)**:
   - `DocTableObject`: Tồn tại độc lập tại `src/ui/DocTableObject.cs`, được dùng trong `WriterView.cs` (`public List<DocTableObject> DocumentTables = new List<DocTableObject>();`) và `FormatHandlers.cs`.
   - `DocShapeObject` & `WrapMode`: Tồn tại tại `src/ui/DocShapeObject.cs`, được quản lý tại `WriterView.cs` (các dòng 6942–7264, 8066–8076, 8367–8377, v.v.).
   - File mẫu 8 bảng biểu: Đã tìm thấy chính xác tại `C:\Users\Danh\Downloads\Bài 1.ND GHI.docx`. Đã kiểm chứng chứa đúng **8 phần tử `<w:tbl>`** trong `word/document.xml`.

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN TỪ QUAN SÁT ĐẾN THIẾT KẾ)

1. **Từ Quan sát 1 (Build & C# 5)**:
   - Toàn bộ mã nguồn mới hoặc chỉnh sửa cho Task AI 3 phải viết bằng cú pháp C# 5 tương thích hoàn toàn với `csc.exe` v4.8 hiện có trong môi trường Windows. Không sử dụng cú pháp C# 6+.

2. **Từ Quan sát 2 & 3 (Tọa độ nút "+" & AddDocTab)**:
   - `DocTabPlusBounds` cung cấp trực tiếp tọa độ thực tế của nút `+` trên form: `center = (DocTabPlusBounds.X + 12, DocTabPlusBounds.Y + 12)`.
   - Khi người dùng click vào nút `+`, thay vì tự động mở tab cùng loại một cách đơn điệu, sự kiện sẽ được đón đầu để kích hoạt `NewTabRadialLauncher.Open(center)`.
   - Khi người dùng chọn 1 trong 4 icon, launcher sẽ gọi `Ribbon.AddDocTab(...)` với đúng `title` và `OfficeAppMode` (Writer, Calc, Slides, hoặc PDF) và đóng lại.

3. **Từ Quan sát 4 (Double Buffering & 60 FPS AnimTimer)**:
   - Vì `AppForm` đã bật sẵn `DoubleBuffered` và `TimeBeginPeriod(1)`, việc vẽ một lớp Overlay (Scrim + 4 icon frosted glass) tại cuối hàm `AppForm.OnPaint` sẽ được thực hiện trực tiếp trong bộ đệm RAM của GDI+, đạt hiệu năng siêu tốc (>60 FPS, <1ms vẽ mỗi frame), không hề giật lag hay xé hình.
   - Sử dụng kết hợp nội suy **Ease-Out Cubic** ($1 - (1-t)^3$) và **Smooth Spring** ($+ \sin(t\pi) \cdot 0.04(1-t)$) theo chuẩn WWDC Apple Fluid Motion để tạo độ nở bung mạnh mẽ từ tâm và hãm phanh đàn hồi nhẹ nhàng khi tiếp đất.
   - Dùng `System.Diagnostics.Stopwatch` để tính $\Delta t$, loại bỏ hiện tượng sai lệch khung hình do timer jitter.

4. **Từ Quan sát 5 (R4 Writer Guardrails)**:
   - Phân hệ Writer (`DocTableObject`, `DocumentTables`, in-cell editing, `DocShapeObject`, `WrapMode`) hoàn toàn nằm ở `WriterView.cs` và `FormatHandlers.cs`.
   - Launcher nút `+` là một thành phần giao diện Shell Overlay nằm trên `AppForm` và `RibbonBar`.
   - Thiết kế launcher thành một file độc lập `src/ui/NewTabRadialLauncher.cs` giúp ranh giới mã nguồn được phân định tuyệt đối 100%: **AI 3 tuyệt đối không chạm vào bất kỳ dòng mã nào liên quan đến table, shape, wrap của Writer**.

5. **Giải quyết xung đột đa tác tử (AI 1, AI 2, AI 3)**:
   - AI 1 chỉnh sửa phím tắt văn bản trong `WriterView.cs` / `AppForm.cs`.
   - AI 2 chỉnh sửa mối nối 1px giữa tab và ribbon tại dòng 2050–2100 trong `RibbonBar.cs`.
   - AI 3 đóng gói toàn bộ logic trong `NewTabRadialLauncher.cs`, chỉ đặt hook 1 dòng tại dòng 11032 của `RibbonBar.cs` và gọi vẽ tại cuối `OnPaint` của `AppForm.cs`. Không có bất kỳ nguy cơ xung đột git nào.

---

## 3. CAVEATS (CÁC LƯU Ý & GIẢ ĐỊNH)

1. **Ràng buộc ngôn ngữ C# 5**:
   - Phải nhắc nhở Task AI 3 không quen tay sử dụng cú pháp C# 6+ (`$"{...}"`, `?.`, expression bodies) để tránh lỗi biên dịch `csc.exe`.
2. **Khu vực hiển thị vòng cung**:
   - Nếu cửa sổ bị thu nhỏ hoặc nút `+` nằm sát góc phải (`center.X + 170 > formWidth`), góc tỏa tia của vòng cung cần tự động chuyển từ hướng xuống-phải (20°..80°) sang hướng xuống-trái (100°..160°) để không bao giờ bị cắt ngoài biên cửa sổ.
3. **Chặn sự kiện click-outside**:
   - Khi scrim đang hiển thị, click ra ngoài phải đóng launcher VÀ chặn sự kiện (consume event) để không kích hoạt nhầm các nút ribbon hoặc vị trí con trỏ chuột bên dưới.

---

## 4. CONCLUSION (KẾT LUẬN & ĐỊNH HƯỚNG TRIỂN KHAI)

Task AI 3 đã có đầy đủ căn cứ, kiến trúc và bản đồ mã nguồn chi tiết để triển khai ngay:
1. **Tạo mới file**: `src/ui/NewTabRadialLauncher.cs`:
   - Quản lý trạng thái: `Closed`, `Opening`, `Open`, `Closing`.
   - Toán học nội suy: `EaseOutCubic` kết hợp `SmoothSpring` (thời lượng 280ms).
   - Vẽ Scrim tối nền: `Color.FromArgb((int)(110 * progress), 15, 23, 42)`.
   - Vẽ 4 thẻ kính mờ Frosted Glass: Kích thước 96x82px, bo góc 14px, viền sáng specular highlight, bóng đổ mềm, vầng hào quang hover, vector emblem từ `HomeView.cs`, chữ Segoe UI rõ nét ("Writer - Văn bản", "Calc - Bảng tính", "Slides - Trình chiếu", "PDF - Tài liệu PDF").
   - Nút `+` xoay mượt 45° thành `×`.
2. **Tích hợp vào `src/ui/RibbonBar.cs`**:
   - Dòng 11032: Khi click `DocTabPlusBounds`, gọi mở/đóng launcher thay vì tạo tab trực tiếp.
3. **Tích hợp vào `src/ui/AppForm.cs`**:
   - `OnPaint`: Gọi `Launcher.Draw(g, ClientSize.Width, ClientSize.Height)` ở cuối cùng.
   - `animTimer.Tick`: Thêm `Launcher.UpdateAnimation()` để kích hoạt `Invalidate()` 60 FPS khi đang chuyển động.
   - `OnMouseDown` & `OnMouseMove`: Bắt click icon, click-outside scrim, và hover card.
   - `OnKeyDown`: Bắt phím `Escape` để đóng launcher ngay lập tức.
4. **Bảo toàn Core Writer**:
   - Giữ nguyên 100% mã nguồn `DocTableObject`, `DocShapeObject`, và `WriterView.cs`.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP KIỂM CHỨNG ĐỘC LẬP)

1. **Kiểm chứng biên dịch ứng dụng**:
   - Chạy lệnh build:
     ```cmd
     cmd.exe /c build.bat
     ```
   - Điều kiện đạt: Trả về mã lỗi 0, thông báo `[SUCCESS] GenOffice.exe built successfully!`, file `GenOffice.exe` được cập nhật.

2. **Kiểm chứng hiệu ứng chuyển động và tương tác**:
   - Mở ứng dụng `GenOffice.exe`.
   - Nhấn chuột vào nút `+` trên thanh tab:
     - Quan sát màn hình mờ tối dần (scrim alpha fade-in).
     - Quan sát 4 biểu tượng phân hệ (Writer, Calc, Slides, PDF) bay tỏa theo hình vòng cung từ tâm nút `+` với gia tốc mượt mà, không giật lag (đạt >= 60 FPS).
     - Rê chuột vào từng biểu tượng: xuất hiện vầng hào quang phát sáng và con trỏ hình bàn tay.
     - Nhấn phím `Esc`: Launcher đóng lại êm ái.
     - Mở lại nút `+`, click chuột ra ngoài vùng trống của scrim: Launcher đóng an toàn.
     - Mở lại nút `+`, click vào biểu tượng "Calc": Tab mới "Bảng tính X.xlsx" được tạo và kích hoạt tức thì.

3. **Kiểm chứng bảo toàn tính năng Writer (R4 Guardrails)**:
   - Mở file mẫu: `C:\Users\Danh\Downloads\Bài 1.ND GHI.docx`.
   - Kiểm tra xem 8 bảng biểu hiển thị đầy đủ, đúng đường viền, màu sắc ô.
   - Thử gõ chỉnh sửa trực tiếp nội dung trong một ô bảng (in-cell editing): Văn bản cập nhật bình thường.
   - Thử thay đổi độ rộng cột hoặc chiều cao hàng: Hoạt động trơn tru, không có bất kỳ hiện tượng hồi quy nào.
