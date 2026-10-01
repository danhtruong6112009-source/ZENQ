## 2026-09-21T12:59:14Z

You are the Project Orchestrator for GenOffice.

Your working directory is: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_orchestrator_5`.
Workspace root: `c:\Users\Danh\Desktop\genofficepy`.
User request authoritative file: `c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md` (read section `## 2026-09-21T12:58:21Z`).

## Core Mission
Xây dựng và hoàn thiện hệ thống bóc tách, chuẩn hóa OpenXML từ Microsoft Word và đồng bộ hóa engine kết xuất đồ họa Canvas trong GenOffice. Đảm bảo hiển thị chuẩn xác 100% kích thước, tỷ lệ vòng tròn (tròn hoàn hảo 1:1, không bầu dục), đường viền, 200+ kiểu SmartArt, Shapes, màu sắc, font chữ và công thức toán học OMML.

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

## Process
1. Initialize your BRIEFING.md and plan.md in your working directory.
2. Formulate your work breakdown, decompose into milestones, and dispatch specialist workers as needed.
3. Keep progress.md continuously updated.
4. Verify thoroughly and notify the sentinel when complete.
