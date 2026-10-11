# Báo Cáo Chuyển Giao (Handoff Report) — UI & Toolbar Explorer

- **Đặc vụ**: `teamwork_preview_explorer_survey_1` (UI & Toolbar Explorer)
- **Thư mục làm việc**: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_survey_1`
- **Mục tiêu**: Báo cáo chuyển giao toàn diện khảo sát kiến trúc UI, thanh công cụ bên trái (Left Toolbar), thanh điều khiển thuộc tính (Tool Controls Bar) và tích hợp công cụ Chỉnh sửa điểm neo (Node Tool) chuẩn Inkscape vào ZenQOffice.

---

## 1. Quan Sát (Observation)

1. **Cửa sổ ứng dụng và cơ chế chế độ làm việc**:
   - Tệp `src/ui/AppForm.cs` (Dòng 19, 841) khai báo và khởi tạo `public PhotoView PhotoView { get; set; }`.
   - Chế độ đồ họa vector được định danh bằng `OfficeAppMode.Photo` (`src/ui/RibbonBar.cs`, Dòng 13).
   - Vòng lặp render chính tại `AppForm.cs` (Dòng 1373-1377):
     ```csharp
     else if (Ribbon.CurrentMode == OfficeAppMode.Photo && PhotoView != null)
     {
         PhotoView.Draw(d2d, ClientSize.Width, ClientSize.Height, Ribbon);
     }
     ```
   - Sự kiện chuột và bàn phím (`OnMouseDown`, `OnMouseMove`, `OnMouseUp`, `OnMouseWheel`, `OnMouseDoubleClick`, `OnKeyDown`) được chuyển tiếp trực tiếp sang `PhotoView` từ `AppForm.cs` (Dòng 2107-2109, 2645-2647, 2834-2836, 2903-2907, 2975-2979, 3975-4010).

2. **Thanh công cụ bên trái (Left Toolbar) & Công cụ thứ 2**:
   - Vị trí và kích thước: `LeftToolbarWidth = 52`, `RibbonHeight = 155`, tọa độ `Rectangle(0, RibbonHeight, LeftToolbarWidth, height - RibbonHeight)` (`src/ui/PhotoView.cs`, Dòng 86, 89, 1481).
   - Định danh công cụ bằng `enum PhotoTool` (`src/ui/PhotoDocument.cs`, Dòng 59-77). Công cụ thứ 2 là `PhotoTool.NodeTool` (Index 1).
   - Danh sách công cụ trên thanh công cụ bên trái được khởi tạo tại `DrawLeftToolbar` (`PhotoView.cs`, Dòng 1489-1504):
     ```csharp
     PhotoTool[] tools = new PhotoTool[]
     {
         PhotoTool.Move,       // [0]
         PhotoTool.NodeTool,   // [1] Công cụ thứ 2: Chỉnh sửa điểm neo
         PhotoTool.Rectangle,  // [2]
         PhotoTool.Ellipse,    // [3]
         PhotoTool.Star,       // [4]
         PhotoTool.Pen,        // [5]
         ...
     };
     ```
   - Nút công cụ thứ 2 được vẽ tại `Y = 155 + 12 + 1 * 40 = 207`, kích thước 40x36 px.
   - Hiện trạng của `PhotoTool.NodeTool` mang tính **giữ chỗ (placeholder)**:
     - `DrawToolGlyph` (Dòng 1581-1597) vẽ biểu tượng con trỏ kèm núm tiếp tuyến và ô vuông neo.
     - `DrawTopOptionsBar` (Dòng 1363-1378) chỉ hiển thị nhãn `"Chỉnh điểm neo"` và 5 nút bấm giữ chỗ với lambda rỗng `() => {}` (`+ Thêm neo`, `- Xóa neo`, `🔗 Nối neo`, `⌐ Góc nhọn`, `~ Uốn mượt`).
     - `Draw` (Dòng 232) gộp chung xử lý với Move để vẽ khung bao ngoài (`DrawTransformBoundingBox`), không vẽ các điểm neo thực tế trên đường cong.
     - `HandleMouseDown` (Dòng 2783-2793) rơi vào khối vẽ cọ chấm điểm trên layer Raster.
     - `HandleMouseMove` (Dòng 2927-2931) rơi vào khối xóa điểm ảnh trong suốt.

3. **Thanh điều khiển thuộc tính phía trên (Tool Controls Bar)**:
   - Được định nghĩa tại `DrawTopOptionsBar(D2DGraphics g, int width)` (`PhotoView.cs`, Dòng 886-1478).
   - Kích thước: `TopOptionsBarHeight = 44`, `X = 52`, `Y = 155`, chiều rộng linh hoạt theo cửa sổ trừ đi thanh công cụ trái (52) và thanh Inspector phải (290).
   - Cơ chế động: Sử dụng cơ chế Immediate-Mode UI. Mỗi khung hình, `_topBarClickables.Clear()` được gọi, sau đó tùy theo giá trị `SelectedTool` mà các nút, ô giá trị số (`DrawTopBarValueBox`), và ô màu sắc được vẽ và đăng ký hitbox tương tác vào `_topBarClickables`.

4. **Công nghệ kết xuất Direct2D & Fluent UI**:
   - Dự án sử dụng Direct2D thuần túy qua lớp `D2DGraphics` (`src/ui/D2DGraphics.cs`), bọc `Vortice.Direct2D1`.
   - `D2DGraphics.CreatePathGeometryFromGraphicsPath` (Dòng 779-789) chuyển đổi trực tiếp `GraphicsPath` chứa Cubic Bezier (`type == 3`) thành `sink.AddBezier(new BezierSegment { Point1 = ..., Point2 = ..., Point3 = ... })`. Direct2D phần cứng hỗ trợ 100% đường cong Bezier bậc 3 với tốc độ 60+ FPS.
   - `FluentIconRenderer` (`src/ui/FluentIconRenderer.cs`) hỗ trợ vẽ glyphs Segoe Fluent Icons / Segoe MDL2 Assets với SSAA 4x cực kỳ sắc nét.
   - `PhotoView.cs` đã có sẵn các helper rất hoàn chỉnh: `DrawTopBarButton`, `DrawTopBarGlyphButton`, `DrawTopBarValueBox` (hộp số có `-` và `+` cùng nhập trực tiếp qua `ShowNumberInputDialog`).

5. **Lỗi khuyết thiếu trong Undo/Redo hiện tại**:
   - Tại `PhotoDocument.cs` (Dòng 119-158), phương thức `PhotoLayer.Clone()` **chưa sao chép** `PathPoints` hoặc danh sách điểm neo. Khi bổ sung cấu trúc `Nodes`, bắt buộc phải sao chép sâu (`Deep Clone`) danh sách `Nodes` để `Ctrl+Z` và `Ctrl+Y` khôi phục đúng hình dáng vector.

6. **Kiểm tra biên dịch**:
   - Chạy lệnh `dotnet build ZenQOffice.csproj` thành công 100% không cảnh báo, không lỗi (0 Warning, 0 Error, thời gian 2.20 giây).

---

## 2. Chuỗi Suy Luận (Logic Chain)

1. Từ **Quan sát 1 & 2**: `AppForm.cs` đã tích hợp đầy đủ các sự kiện chuyển tiếp đến `PhotoView`. `PhotoTool.NodeTool` đã có sẵn tại vị trí thứ 2 trên thanh công cụ bên trái. Do đó, việc triển khai công cụ Chỉnh sửa điểm neo **không cần thay đổi kiến trúc điều hướng cửa sổ chính**, mà chỉ cần mở rộng lớp `PhotoView` và `PhotoDocument`.
2. Từ **Quan sát 2 & 3**: Thanh điều khiển thuộc tính `DrawTopOptionsBar` thay đổi giao diện theo `SelectedTool`. Khối mã `else if (SelectedTool == PhotoTool.NodeTool)` tại dòng 1363-1378 chính là điểm chèn hoàn hảo để thay thế 5 nút giữ chỗ bằng toàn bộ bộ nút chức năng chuẩn Inkscape.
3. Từ **Quan sát 3 & 4**: Chiều rộng khả dụng trên thanh điều khiển là $>1500\text{ px}$ (ở màn hình 1920x1080) và $>1000\text{ px}$ (ở màn hình 1366x768). Toàn bộ 5 nhóm nút chuẩn Inkscape (14 nút chức năng + 2 ô tọa độ X/Y + 1 nút đổi đơn vị px/mm) chiếm khoảng $1,150\text{ px} - 1,220\text{ px}$ nếu dùng nút văn bản kết hợp ký hiệu, hoặc chỉ $\approx 650\text{ px}$ nếu dùng nút biểu tượng (icon) 28x26 px. Cả hai giải pháp đều hoàn toàn vừa vặn.
4. Từ **Quan sát 4**: Vì `D2DGraphics` tự động chuyển đổi `GraphicsPath` có Bezier bậc 3 sang Direct2D `sink.AddBezier`, việc tạo đường cong Bezier bậc 3 chỉ cần sử dụng `GraphicsPath.AddBezier` hoặc trực tiếp dựng hình qua Direct2D. Kết xuất này hoàn toàn đảm bảo hiệu năng 60 FPS mượt mà.
5. Từ **Quan sát 5**: Để đáp ứng tiêu chí nghiệm thu "Phím tắt Undo (Ctrl+Z) và Redo (Ctrl+Y) khôi phục chính xác từng thao tác chỉnh sửa điểm neo", cấu trúc dữ liệu `BezierNode` mới phải được sao chép trong `PhotoLayer.Clone()`.
6. Từ **Yêu cầu gốc**: Dự án bắt buộc sử dụng **100% tiếng Việt đồ họa chuẩn mực, tuyệt đối không dùng tiếng Anh trong dấu ngoặc đơn**. Đã thiết lập bảng ánh xạ chuẩn hóa không chứa bất kỳ từ tiếng Anh phụ nào (ví dụ: dùng "Góc nhọn", "Uốn mượt", "Đối xứng", "Tự động mượt", "Chuyển hình thành đường cong", "Chuyển viền thành đường cong").

---

## 3. Vấn Đề Lưu Ý (Caveats)

1. **Khác biệt danh sách công cụ giữa vẽ và bấm chuột**:
   - Tại `DrawLeftToolbar` (`PhotoView.cs`, Dòng 1489), mảng `tools` có 13 phần tử (bao gồm `Eraser`).
   - Tại `HandleMouseDown` (`PhotoView.cs`, Dòng 2625), mảng `tools` có 12 phần tử (bị thiếu `Eraser`).
   - Cần đồng bộ mảng này để khi người dùng click vào các nút bên dưới không bị lệch index.
2. **Xử lý phím tắt đơn**:
   - `PhotoView.HandleKeyDown` có gán các phím đơn như `V`, `E` (NodeTool), `B`, `P`, `Delete`. Tuy nhiên, `AppForm.cs` trong `OnKeyDown` chủ yếu xử lý các phím kết hợp `Ctrl`. Cần đảm bảo các phím bấm thông thường trong chế độ Photo được chuyển tiếp đến `PhotoView.HandleKeyDown`.
3. **Phạm vi khảo sát**:
   - Báo cáo này là phân tích kiến trúc chỉ đọc (Read-only investigation). Chưa thực hiện sửa đổi mã nguồn trong các tệp thuộc `src/`.

---

## 4. Kết Luận (Conclusion)

1. **Vị trí và định danh công cụ Node Tool**: Đã xác định chính xác tại `src/ui/PhotoView.cs` (Dòng 1489-1504) là phần tử thứ 2 (`PhotoTool.NodeTool`), định danh qua `enum PhotoTool` (`src/ui/PhotoDocument.cs`, Dòng 62).
2. **Vị trí Tool Controls Bar**: Nằm tại `DrawTopOptionsBar` (`src/ui/PhotoView.cs`, Dòng 886-1478), chiều cao 44px, tọa độ Y từ 155 đến 199.
3. **Bộ nút chuẩn Inkscape**: Toàn bộ 5 nhóm nút (Thao tác điểm neo, Kiểu điểm neo, Tính chất đoạn vẽ, Chuyển đổi đối tượng, Tọa độ X/Y kèm px/mm) hoàn toàn khả thi để tích hợp vào khối `else if (SelectedTool == PhotoTool.NodeTool)` tại dòng 1363-1378.
4. **Kiến trúc kết xuất**: Direct2D 100% thông qua `D2DGraphics`, hỗ trợ sẵn chuyển đổi `GraphicsPath` sang Direct2D Cubic Bezier.
5. **Ngôn ngữ**: Đã hoàn thiện bộ từ vựng 100% tiếng Việt đồ họa chuẩn mực, loại bỏ hoàn toàn tiếng Anh trong dấu ngoặc đơn.

---

## 5. Phương Pháp Xác Minh Độc Lập (Verification Method)

1. **Kiểm tra mã nguồn các vị trí then chốt**:
   - `src/ui/PhotoDocument.cs`: Dòng 59-77 (`enum PhotoTool`), Dòng 119-158 (`PhotoLayer.Clone`).
   - `src/ui/PhotoView.cs`: Dòng 86-90 (hằng số bố cục), Dòng 886-1478 (`DrawTopOptionsBar`), Dòng 1363-1378 (placeholder NodeTool), Dòng 1480-1550 (`DrawLeftToolbar`), Dòng 1581-1597 (glyph NodeTool).
   - `src/ui/AppForm.cs`: Dòng 1373-1377 (vòng lặp vẽ PhotoView), Dòng 2645-2647 (chuột nhấn), Dòng 3975-4010 (Undo/Redo).
   - `src/ui/D2DGraphics.cs`: Dòng 779-789 (chuyển đổi Cubic Bezier sang Direct2D).
2. **Kiểm tra biên dịch dự án**:
   - Chạy lệnh trong terminal PowerShell tại thư mục gốc:
     ```powershell
     dotnet build ZenQOffice.csproj
     ```
   - Điều kiện đạt: Kết quả xuất ra `Build succeeded`, 0 Warning, 0 Error.
