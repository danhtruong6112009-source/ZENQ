# Master Plan: GenOffice OpenXML Extraction & Canvas Renderer Synchronization

## Objectives
1. Bóc tách & Chuẩn hóa OpenXML Schema (DrawingML dgm:, w:drawing, w:tblBorders, m:oMath) từ C:\Users\Danh\Desktop\xml và ECMA-376.
2. Xây dựng công cụ tạo tài liệu Word mẫu toàn diện (200+ SmartArt, Shapes, Borders, OMML Math) làm ground truth.
3. Nâng cấp OpenXmlSmartArtModel.cs, FormatHandlers.cs, SmartArtRenderer.cs, ShapeRenderer.cs, WriterView.cs để render chuẩn xác 100% kích thước, tỷ lệ vòng tròn 1:1 hoàn hảo không méo, tự động gắn kết không khe hở, và đầy đủ viền/màu sắc.

## Phase Breakdown

### Phase 0: Survey & Technical Exploration (Parallel Explorers)
- [ ] Explorer 1 (`teamwork_preview_spec_miner`): Khảo sát thư mục C:\Users\Danh\Desktop\xml, phân tích cấu trúc DrawingML (dgm:), WordprocessingML (w:tblBorders, w:drawing) và Math (m:oMath), ánh xạ EMU -> pt (12700 EMU/pt), layout node, viền và theme color.
- [ ] Explorer 2 (`teamwork_preview_explorer`): Khảo sát hiện trạng mã nguồn GenOffice (OpenXmlSmartArtModel.cs, FormatHandlers.cs, SmartArtRenderer.cs, ShapeRenderer.cs, WriterView.cs), tìm hiểu cách thức parse và render hiện tại, nguyên nhân gây méo vòng tròn/khe hở, và quy trình build (build.bat).
- [ ] Explorer 3 (`teamwork_preview_explorer`): Khảo sát và thiết kế công cụ tạo tài liệu Word mẫu (200+ SmartArt layouts: Cycle, Hierarchy, Process, Matrix, Pyramid, Relationship, List, Picture; Shape/Borders/Shadow/Gradient/3D/Math) và cơ chế trích xuất XML ground truth.

### Phase 1: Synthesis & Master Architecture (PROJECT.md & TEST_INFRA.md)
- [ ] Hợp nhất các báo cáo khảo sát.
- [ ] Cập nhật PROJECT.md với danh mục tính năng hoàn chỉnh, interface contracts, và phân chia milestones.
- [ ] Thiết lập TEST_INFRA.md cho E2E Testing Track.

### Phase 2: Implementation & Verification Loop
- [ ] Milestone 1: Bóc tách & Chuẩn hóa OpenXML Schema & Parser Model.
- [ ] Milestone 2: Bộ sinh tài liệu Word mẫu 200+ SmartArt, Shape, Math và XML Ground Truth.
- [ ] Milestone 3: Canvas Renderer Upgrade (Vòng tròn 1:1, gắn kết không khe hở, viền & màu).
- [ ] Milestone 4: Dual Track E2E Verification & Adversarial Hardening.

### Phase 3: Final Acceptance & Handover
- [ ] Kiểm tra 100% tiêu chí nghiệm thu (Acceptance Criteria).
- [ ] Clean build với build.bat.
- [ ] Handoff báo cáo tổng thể cho Sentinel.
