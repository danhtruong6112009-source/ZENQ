# Project: ZenQOffice Node Tool & Tool Controls Bar

## Architecture
- **UI Presentation Layer (`src/ui/PhotoView.cs`, `src/ui/FluentIconRenderer.cs`)**:
  - Left Toolbar: Index 1 (`PhotoTool.NodeTool`), 2nd tool button.
  - Top Tool Controls Bar (`DrawTopOptionsBar`): Dynamic immediate-mode bar rendering Inkscape-standard tool buttons with Direct2D/Fluent UI glyphs and 100% Vietnamese labels.
  - Coordinate Spinboxes: Interactive X and Y values with `+` and `-` buttons, direct typing, and unit toggle (`px` / `mm`).
- **Vector Data & Math Layer (`src/ui/PhotoDocument.cs`, `src/ui/VectorPathModel.cs`)**:
  - `VectorNodeType`: `Corner` (Cusp / Góc nhọn), `Smooth` (Uốn mượt), `Symmetric` (Đối xứng), `AutoSmooth` (Tự động mượt).
  - `VectorPathNode`: Anchor point (`Pt`), `InHandle`, `OutHandle`, `NodeType`, `IsSelected`.
  - `VectorPathModel`: Cubic Bezier math engine, handle synchronization algorithms, De Casteljau subdivision for node insertion, node removal, node joining/breaking, segment conversion (line <-> curve).
  - Object to Path (Rect, Ellipse with $\kappa \approx 0.552285$, Star, Polygon) and Stroke to Path conversions.
  - Deep clone in `PhotoLayer.Clone()` ensuring flawless Undo/Redo (`Ctrl+Z`, `Ctrl+Y`).
- **Hardware Acceleration Layer (`src/ui/D2DGraphics.cs`)**:
  - Vortice Direct2D1 hardware path geometry sink (`sink.AddBezier`) rendering Cubic Bezier curves at 60-120fps.
- **Canvas Interaction Layer (`src/ui/PhotoView.cs`)**:
  - Hierarchical hit-testing: Tangent Handle Knobs -> Anchor Nodes -> Segments -> Marquee Box.
  - Multi-selection: Click, Shift+Click, rubberband drag selection.
  - Interactive manipulation: Real-time dragging of anchor nodes and tangent handles with live deformation.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Thêm điểm neo (+) | Thêm neo vào đoạn cong hoặc thẳng tại vị trí trung điểm hoặc chọn trước | M1 | ORIGINAL_REQUEST §R1 |
| 2 | Xóa điểm neo (-) | Xóa các điểm neo được chọn và kết nối lại các nút liền kề | M1 | ORIGINAL_REQUEST §R1 |
| 3 | Nối điểm neo | Hợp nhất các điểm neo được chọn thành một điểm duy nhất | M1 | ORIGINAL_REQUEST §R1 |
| 4 | Tách đường | Tách đường cong tại điểm neo đã chọn thành 2 đoạn mở | M1 | ORIGINAL_REQUEST §R1 |
| 5 | Nối điểm mút | Nối 2 điểm mút tự do bằng một đoạn nối mới | M1 | ORIGINAL_REQUEST §R1 |
| 6 | Xóa đoạn giữa 2 neo | Xóa phân đoạn nằm giữa 2 điểm neo được chọn | M1 | ORIGINAL_REQUEST §R1 |
| 7 | Điểm neo góc nhọn | Chuyển điểm neo sang loại Góc nhọn (Cusp), 2 tay nắm di chuyển độc lập | M1 | ORIGINAL_REQUEST §R1, R2 |
| 8 | Điểm neo uốn mượt | Chuyển điểm neo sang Uốn mượt (Smooth), 2 tay nắm thẳng hàng 180° | M1 | ORIGINAL_REQUEST §R1, R2 |
| 9 | Điểm neo đối xứng | Chuyển điểm neo sang Đối xứng (Symmetric), 2 tay nắm thẳng hàng và cùng độ dài | M1 | ORIGINAL_REQUEST §R1, R2 |
| 10 | Điểm neo tự động mượt | Chuyển điểm neo sang Tự động mượt (Auto-smooth), tính toán tự động qua Catmull-Rom | M1 | ORIGINAL_REQUEST §R1, R2 |
| 11 | Biến đoạn cong thành thẳng | Triệt tiêu tay nắm tiếp tuyến trên phân đoạn được chọn | M1 | ORIGINAL_REQUEST §R1 |
| 12 | Biến đoạn thẳng thành cong | Khởi tạo tay nắm tiếp tuyến trên đoạn thẳng để uốn cong | M1 | ORIGINAL_REQUEST §R1 |
| 13 | Khối thành đường (Object to Path) | Chuyển hình học nguyên thủy (hình chữ nhật, elip, sao) thành đường cong Bezier | M1 | ORIGINAL_REQUEST §R1 |
| 14 | Viền thành đường (Stroke to Path) | Chuyển nét viền thành đường bao khép kín có thể sửa từng điểm neo | M1 | ORIGINAL_REQUEST §R1 |
| 15 | Hộp tọa độ X và Y (+/-) | Nhập trực tiếp tọa độ điểm neo, nút +/- điều chỉnh và chọn đơn vị px/mm | M1 | ORIGINAL_REQUEST §R1 |
| 16 | Cấu trúc dữ liệu Bezier & Node | `VectorNodeType`, `VectorPathNode` (Pt, InHandle, OutHandle) | M1 | ORIGINAL_REQUEST §R2 |
| 17 | Động cơ nội suy Cubic Bezier | Dựng hình đường cong bậc 3 chính xác trên Direct2D và GDI+ | M1 | ORIGINAL_REQUEST §R2 |
| 18 | Hiển thị điểm neo trên Canvas | Hình vuông nhỏ có viền rõ nét cho neo, núm tròn cho tiếp tuyến | M1 | ORIGINAL_REQUEST §R3 |
| 19 | Đa chọn điểm neo | Chọn 1 neo, Shift đa chọn, quét khung marquee chọn hàng loạt | M1 | ORIGINAL_REQUEST §R3 |
| 20 | Kéo rê tương tác 60fps | Nắn chỉnh hình dạng đường vẽ và góc tiếp tuyến thời gian thực mượt mà | M1 | ORIGINAL_REQUEST §R3 |
| 21 | Phím tắt Undo/Redo | Khôi phục chính xác từng thao tác chỉnh sửa neo qua Ctrl+Z và Ctrl+Y | M1 | ORIGINAL_REQUEST §AC |
| 22 | 100% Tiếng Việt chuẩn mực | Toàn bộ nhãn, tooltip sử dụng tiếng Việt chuyên nghiệp, không tiếng Anh trong ngoặc | M1 | ORIGINAL_REQUEST §AC |
| 23 | Build & Đóng gói nhị phân | `dotnet build ZenQOffice.csproj` 0 lỗi và đồng bộ `ZenQOffice.exe` ra thư mục gốc | M2 | ORIGINAL_REQUEST §AC |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Triển khai Hoàn chỉnh Node Tool & Tool Controls Bar | Toàn bộ R1 (Tool Controls Bar, glyphs, 100% tiếng Việt, spinbox X/Y), R2 (VectorPathModel, Bezier cubic, node types, handles, Object/Stroke to Path), và R3 (Canvas rendering, hit-test, multi-select, drag 60fps, Undo/Redo) | none | IN_PROGRESS |
| M2 | Kiểm thử E2E Toàn diện, Tôi luyện Đối kháng & Đóng gói Build | Kiểm thử E2E 23 tính năng, kiểm thử đối kháng độ cong Bezier & đa chọn, Forensic Auditor xác thực tính toàn vẹn, biên dịch 0 lỗi và xuất binary ra thư mục gốc | M1 | PLANNED |

## Interface Contracts
### `VectorPathModel` ↔ `PhotoLayer`
- `PhotoLayer.Nodes`: `List<VectorPathNode>` lưu trữ danh sách các điểm neo và tay nắm.
- `PhotoLayer.EnsureNodesInitialized()`: Tự động khởi tạo `Nodes` từ hình khối hiện tại nếu `Nodes` rỗng.
- `PhotoLayer.BuildGraphicsPath()`: Sinh `GraphicsPath` từ các đoạn Cubic Bezier nối các `VectorPathNode`.
- `PhotoLayer.Clone()`: Thực hiện Deep Copy toàn bộ danh sách `Nodes` (bao gồm `InHandle`, `OutHandle`, `NodeType`, `IsSelected`).

### `PhotoView` ↔ `VectorPathModel`
- `VectorPathModel.InsertNodeAt(layer, point)`: Chèn điểm neo mới vào đoạn cong gần nhất bằng De Casteljau.
- `VectorPathModel.DeleteSelectedNodes(layer)`: Xóa các nút được đánh dấu `IsSelected = true`.
- `VectorPathModel.JoinSelectedNodes(layer)`: Nối các nút đang chọn.
- `VectorPathModel.BreakPathAtSelectedNodes(layer)`: Tách đường tại nút được chọn.
- `VectorPathModel.SetSelectedNodesType(layer, VectorNodeType newType)`: Chuyển đổi loại điểm neo và đồng bộ tay nắm.
- `VectorPathModel.ConvertSegmentToLine(layer)` / `ConvertSegmentToCurve(layer)`: Biến đổi tính chất đoạn vẽ.
- `VectorPathModel.ObjectToPath(layer)`: Chuyển đổi khối hình học sang Bezier path.
- `VectorPathModel.StrokeToPath(layer)`: Chuyển đổi nét viền sang Bezier path.

### `PhotoView (Top Controls Bar)` ↔ `PhotoView (Canvas Interaction)`
- `SelectedTool == PhotoTool.NodeTool`: Kích hoạt hiển thị nút thanh trên và chế độ tương tác canvas riêng biệt.
- Khi giá trị X, Y trong Spinbox thay đổi -> Cập nhật vị trí tọa độ của các điểm neo đang chọn trong `layer.Nodes` và gọi `Invalidate()`.

## Code Layout
- `src/ui/VectorPathModel.cs`: Tệp mới chứa định nghĩa kiểu dữ liệu `VectorNodeType`, `VectorPathNode`, và các thuật toán hình học Bezier, chuyển đổi Object/Stroke to Path.
- `src/ui/PhotoDocument.cs`: Bổ sung `Nodes` vào `PhotoLayer`, tích hợp Deep Clone cho Undo/Redo.
- `src/ui/PhotoView.cs`: Nâng cấp `DrawTopOptionsBar` (Tool Controls Bar) và `HandleMouseDown/MouseMove/MouseUp` cho `NodeTool`.
- `src/ui/D2DGraphics.cs`: Giữ nguyên hoặc mở rộng helper nếu cần cho kết xuất glyph.
- `ZenQOffice.csproj`: Thêm `src/ui/VectorPathModel.cs` (nếu cần theo định dạng csproj).
