# -*- coding: utf-8 -*-
"""Category 6: Flowchart Symbols (50 presets) - 100% Unique & Real Geometry!"""

def get_code():
    return '''
        #region Category 6: Lưu đồ & Quy trình (50 mẫu)
        private static void PopulateFlowchartSymbols()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Flowchart", Title = "Lưu đồ & Quy trình" };
            _categories.Add(cat);

            // 1. Terminator (Điểm bắt đầu / kết thúc - Stadium Oval Pill)
            AddItem(cat, "flow_terminator", "Điểm bắt đầu/kết thúc (Terminator)",
                (w, h) => GRoundedRect(w, h, h / 2f),
                (w, h) => WRoundedRect(w, h, h / 2.0));

            // 2. Process (Bước xử lý quy trình - Chữ nhật chuẩn)
            AddItem(cat, "flow_process", "Bước xử lý quy trình (Process)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));

            // 3. Decision (Điểm quyết định rẽ nhánh - Hình thoi kim cương)
            AddItem(cat, "flow_decision", "Điểm quyết định rẽ nhánh (Decision)",
                (w, h) => GPoly(w, h, (0.5f, 0), (1, 0.5f), (0.5f, 1), (0, 0.5f)),
                (w, h) => WPoly(w, h, (0.5, 0), (1, 0.5), (0.5, 1), (0, 0.5)));

            // 4. Data / I/O (Dữ liệu vào/ra - Hình bình hành)
            AddItem(cat, "flow_data", "Dữ liệu vào/ra I/O (Data)",
                (w, h) => GPoly(w, h, (0.2f, 0), (1, 0), (0.8f, 1), (0, 1)),
                (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (0.8, 1), (0, 1)));

            // 5. Predefined Process (Quy trình con định nghĩa trước - Hai vạch dọc hai bên)
            AddItem(cat, "flow_predefined", "Quy trình con định nghĩa trước (Subroutine)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddLine(w * 0.15f, 0, w * 0.15f, h); gp.AddLine(w * 0.85f, 0, w * 0.85f, h); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h), true, false); ctx.LineTo(new WpfPoint(0, h), true, false); ctx.BeginFigure(new WpfPoint(w * 0.15, 0), false, false); ctx.LineTo(new WpfPoint(w * 0.15, h), true, false); ctx.BeginFigure(new WpfPoint(w * 0.85, 0), false, false); ctx.LineTo(new WpfPoint(w * 0.85, h), true, false); } g.Freeze(); return g; });

            // 6. Internal Storage (Kho lưu trữ nội bộ - Vạch ngang đỉnh và vạch dọc trái)
            AddItem(cat, "flow_internal_storage", "Kho lưu trữ nội bộ (Internal Storage)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddLine(0, h * 0.2f, w, h * 0.2f); gp.AddLine(w * 0.2f, 0, w * 0.2f, h); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h), true, false); ctx.LineTo(new WpfPoint(0, h), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.2), false, false); ctx.LineTo(new WpfPoint(w, h * 0.2), true, false); ctx.BeginFigure(new WpfPoint(w * 0.2, 0), false, false); ctx.LineTo(new WpfPoint(w * 0.2, h), true, false); } g.Freeze(); return g; });

            // 7. Document (Tài liệu giấy đơn - Đáy lượn sóng Bezier)
            AddItem(cat, "flow_document", "Tài liệu giấy đơn (Single Document)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, 0, w, 0); gp.AddLine(w, 0, w, h * 0.85f); gp.AddBezier(w, h * 0.85f, w * 0.75f, h * 0.7f, w * 0.25f, h * 1.05f, 0, h * 0.85f); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h * 0.85), true, false); ctx.BezierTo(new WpfPoint(w * 0.75, h * 0.7), new WpfPoint(w * 0.25, h * 1.05), new WpfPoint(0, h * 0.85), true, false); } g.Freeze(); return g; });

            // 8. Multi-Document (Đa tài liệu xếp chồng)
            AddItem(cat, "flow_multidocument", "Đa tài liệu xếp chồng (Multi-Document)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLines(new PointF[] { new PointF(w * 0.15f, 0), new PointF(w, 0), new PointF(w, h * 0.75f) }); gp.AddLines(new PointF[] { new PointF(w * 0.08f, h * 0.08f), new PointF(w * 0.92f, h * 0.08f), new PointF(w * 0.92f, h * 0.82f) }); gp.AddLine(0, h * 0.15f, w * 0.85f, h * 0.15f); gp.AddLine(w * 0.85f, h * 0.15f, w * 0.85f, h * 0.85f); gp.AddBezier(w * 0.85f, h * 0.85f, w * 0.65f, h * 0.7f, w * 0.2f, h * 1.05f, 0, h * 0.85f); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.15, 0), false, false); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h * 0.75), true, false); ctx.BeginFigure(new WpfPoint(w * 0.08, h * 0.08), false, false); ctx.LineTo(new WpfPoint(w * 0.92, h * 0.08), true, false); ctx.LineTo(new WpfPoint(w * 0.92, h * 0.82), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.15), true, true); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.15), true, false); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.85), true, false); ctx.BezierTo(new WpfPoint(w * 0.65, h * 0.7), new WpfPoint(w * 0.2, h * 1.05), new WpfPoint(0, h * 0.85), true, false); } g.Freeze(); return g; });

            // 9. Manual Input (Nhập liệu thủ công - Đỉnh vát dốc từ trái xuống phải)
            AddItem(cat, "flow_manual_input", "Nhập liệu thủ công (Manual Input)",
                (w, h) => GPoly(w, h, (0, 0.25f), (1, 0), (1, 1), (0, 1)),
                (w, h) => WPoly(w, h, (0, 0.25), (1, 0), (1, 1), (0, 1)));

            // 10. Manual Operation (Thao tác thủ công - Hình thang ngược)
            AddItem(cat, "flow_manual_operation", "Thao tác thủ công (Manual Operation)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (0.8f, 1), (0.2f, 1)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (0.8, 1), (0.2, 1)));

            // 11. Preparation (Chuẩn bị thiết lập - Hình lục giác kéo dài)
            AddItem(cat, "flow_preparation", "Chuẩn bị thiết lập (Preparation Hexagon)",
                (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 0.5f), (0.8f, 1), (0.2f, 1), (0, 0.5f)),
                (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 0.5), (0.8, 1), (0.2, 1), (0, 0.5)));

            // 12. Display (Màn hình hiển thị - Hình viên đạn vòm phải)
            AddItem(cat, "flow_display", "Màn hình hiển thị (Display)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.2f, 0, w * 0.6f, 0); gp.AddArc(w * 0.2f, 0, w * 0.8f, h, 270, 180); gp.AddLine(w * 0.6f, h, w * 0.2f, h); gp.AddLine(w * 0.2f, h, 0, h * 0.5f); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.5), true, true); ctx.LineTo(new WpfPoint(w * 0.2, 0), true, false); ctx.LineTo(new WpfPoint(w * 0.6, 0), true, false); ctx.ArcTo(new WpfPoint(w * 0.6, h), new WpfSize(w * 0.4, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(w * 0.2, h), true, false); } g.Freeze(); return g; });

            // 13. Delay (Bước chờ trì hoãn - Chữ D vòm phải)
            AddItem(cat, "flow_delay", "Bước chờ trì hoãn (Delay D-Shape)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, 0, w * 0.5f, 0); gp.AddArc(0, 0, w, h, 270, 180); gp.AddLine(w * 0.5f, h, 0, h); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w * 0.5, 0), true, false); ctx.ArcTo(new WpfPoint(w * 0.5, h), new WpfSize(w * 0.5, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(0, h), true, false); } g.Freeze(); return g; });

            // 14. Off-page Connector (Điểm nối khác trang - Ngũ giác chóp nhọn chúc xuống)
            AddItem(cat, "flow_offpage_connector", "Điểm nối khác trang (Off-page Connector)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.65f), (0.5f, 1), (0, 0.65f)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.65), (0.5, 1), (0, 0.65)));

            // 15. On-page Connector (Điểm nối cùng trang - Tròn nhỏ)
            AddItem(cat, "flow_onpage_connector", "Điểm nối cùng trang (On-page Connector)",
                (w, h) => GEllipse(w, h),
                (w, h) => WEllipse(w, h));

            // 16. Summing Junction (Tổng hợp cộng dồn - Tròn có dấu cộng)
            AddItem(cat, "flow_summing_junction", "Tổng hợp cộng dồn (Summing Junction)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.5f, 0, w * 0.5f, h); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w / 2.0, 0), false, false); ctx.LineTo(new WpfPoint(w / 2.0, h), true, false); ctx.BeginFigure(new WpfPoint(0, h / 2.0), false, false); ctx.LineTo(new WpfPoint(w, h / 2.0), true, false); } g.Freeze(); return g; });

            // 17. Or Junction (Điểm giao logic OR - Tròn có dấu chéo X)
            AddItem(cat, "flow_or_junction", "Điểm giao logic OR (Or Junction)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.15f, h * 0.15f, w * 0.85f, h * 0.85f); gp.AddLine(w * 0.85f, h * 0.15f, w * 0.15f, h * 0.85f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.15, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.85), true, false); ctx.BeginFigure(new WpfPoint(w * 0.85, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.15, h * 0.85), true, false); } g.Freeze(); return g; });

            // 18. Collate (Sắp xếp đối chiếu - Đồng hồ cát hai tam giác đấu đỉnh)
            AddItem(cat, "flow_collate", "Sắp xếp đối chiếu (Collate Hourglass)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (0, 1), (1, 1)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (0, 1), (1, 1)));

            // 19. Sort (Phân loại sắp xếp - Hình thoi chia đôi ngang)
            AddItem(cat, "flow_sort", "Phân loại sắp xếp (Sort Diamond)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); } g.Freeze(); return g; });

            // 20. Extract (Trích xuất phân nhánh - Tam giác trỏ xuống)
            AddItem(cat, "flow_extract", "Trích xuất phân nhánh (Extract Triangle)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (0.5f, 1)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (0.5, 1)));

            // 21. Merge (Hợp nhất quy trình - Tam giác trỏ lên)
            AddItem(cat, "flow_merge", "Hợp nhất quy trình (Merge Triangle)",
                (w, h) => GPoly(w, h, (0.5f, 0), (1, 1), (0, 1)),
                (w, h) => WPoly(w, h, (0.5, 0), (1, 1), (0, 1)));

            // 22. Database (Cơ sở dữ liệu - Hình trụ 3D đứng)
            AddItem(cat, "flow_database", "Cơ sở dữ liệu (Database Cylinder)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h * 0.3f); gp.AddLine(0, h * 0.15f, 0, h * 0.85f); gp.AddArc(0, h * 0.7f, w, h * 0.3f, 180, -180); gp.AddLine(w, h * 0.85f, w, h * 0.15f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.15), true, true); ctx.ArcTo(new WpfPoint(w, h * 0.15), new WpfSize(w / 2.0, h * 0.15), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(w, h * 0.85), true, false); ctx.ArcTo(new WpfPoint(0, h * 0.85), new WpfSize(w / 2.0, h * 0.15), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(0, h * 0.15), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.15), true, true); ctx.ArcTo(new WpfPoint(w, h * 0.15), new WpfSize(w / 2.0, h * 0.15), 0, false, WpfSweepDirection.Counterclockwise, true, false); ctx.ArcTo(new WpfPoint(0, h * 0.15), new WpfSize(w / 2.0, h * 0.15), 0, false, WpfSweepDirection.Counterclockwise, true, false); } g.Freeze(); return g; });

            // 23. Direct Access Storage (Đĩa lưu trữ trực tiếp - Trụ nằm ngang)
            AddItem(cat, "flow_direct_data", "Đĩa lưu trữ trực tiếp (Direct Access Storage)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.75f, 0, w * 0.25f, h); gp.AddLine(w * 0.15f, 0, w * 0.85f, 0); gp.AddArc(0, 0, w * 0.3f, h, 270, -180); gp.AddLine(w * 0.15f, h, w * 0.85f, h); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.15, 0), true, true); ctx.LineTo(new WpfPoint(w * 0.85, 0), true, false); ctx.ArcTo(new WpfPoint(w * 0.85, h), new WpfSize(w * 0.15, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(w * 0.15, h), true, false); ctx.ArcTo(new WpfPoint(w * 0.15, 0), new WpfSize(w * 0.15, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });

            // 24. Sequential Data / Tape (Băng từ tuần tự - Tròn kèm đuôi ngang)
            AddItem(cat, "flow_sequential_data", "Băng từ tuần tự (Magnetic Tape)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w * 0.85f, h * 0.85f); gp.AddLine(w * 0.42f, h * 0.85f, w, h * 0.85f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.42, 0), true, true); ctx.ArcTo(new WpfPoint(w * 0.42, h * 0.85), new WpfSize(w * 0.42, h * 0.42), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.42, 0), new WpfSize(w * 0.42, h * 0.42), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.42, h * 0.85), false, false); ctx.LineTo(new WpfPoint(w, h * 0.85), true, false); } g.Freeze(); return g; });

            // 25. Punched Card (Thẻ đục lỗ - Vát góc trên trái 45°)
            AddItem(cat, "flow_punched_card", "Thẻ đục lỗ ghi nhận (Punched Card)",
                (w, h) => GPoly(w, h, (0.2f, 0), (1, 0), (1, 1), (0, 1), (0, 0.2f)),
                (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (1, 1), (0, 1), (0, 0.2)));

            // 26. Punched Tape (Băng giấy đục lỗ - Sóng hai đầu)
            AddItem(cat, "flow_punched_tape", "Băng giấy đục lỗ (Paper Tape)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddBezier(0, h * 0.2f, w * 0.25f, 0, w * 0.75f, h * 0.4f, w, h * 0.2f); gp.AddLine(w, h * 0.2f, w, h * 0.8f); gp.AddBezier(w, h * 0.8f, w * 0.75f, h, w * 0.25f, h * 0.6f, 0, h * 0.8f); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.2), true, true); ctx.BezierTo(new WpfPoint(w * 0.25, 0), new WpfPoint(w * 0.75, h * 0.4), new WpfPoint(w, h * 0.2), true, false); ctx.LineTo(new WpfPoint(w, h * 0.8), true, false); ctx.BezierTo(new WpfPoint(w * 0.75, h), new WpfPoint(w * 0.25, h * 0.6), new WpfPoint(0, h * 0.8), true, false); } g.Freeze(); return g; });

            // 27. Stored Data (Dữ liệu lưu trữ - Bo tròn cong bên trái, nhọn bên phải)
            AddItem(cat, "flow_stored_data", "Dữ liệu lưu trữ (Stored Data)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(0, 0, w * 0.4f, h, 90, 180); gp.AddLine(w * 0.2f, 0, w, 0); gp.AddLine(w, 0, w * 0.8f, h * 0.5f); gp.AddLine(w * 0.8f, h * 0.5f, w, h); gp.AddLine(w, h, w * 0.2f, h); gp.CloseFigure(); return gp; },
                (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (0.8, 0.5), (1, 1), (0.2, 1), (0, 0.5)));

            // 28. Loop Limit (Kiểm tra giới hạn vòng lặp)
            AddItem(cat, "flow_loop_limit", "Giới hạn lặp vòng (Loop Limit)",
                (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 0.35f), (1, 1), (0, 1), (0, 0.35f)),
                (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 0.35), (1, 1), (0, 1), (0, 0.35)));

            // 29. Transmittal Tape (Dải băng chuyển giao - Đáy khuyết chữ V)
            AddItem(cat, "flow_transmittal_tape", "Dải băng chuyển giao (Transmittal Tape)",
                (w, h) => GPoly(w, h, (0, 0), (0.75f, 0), (1, 0.25f), (1, 1), (0.5f, 0.8f), (0, 1)),
                (w, h) => WPoly(w, h, (0, 0), (0.75, 0), (1, 0.25), (1, 1), (0.5, 0.8), (0, 1)));

            // 30. Alternate Process (Quy trình thay thế - Chữ nhật bo góc)
            AddItem(cat, "flow_alternate_process", "Quy trình thay thế (Alternate Process)",
                (w, h) => GRoundedRect(w, h, 6f),
                (w, h) => WRoundedRect(w, h, 6.0));

            // 31. Annotation (Ghi chú chú giải - Khung móc mở)
            AddItem(cat, "flow_annotation", "Khung chú giải ghi chú (Annotation Bracket)",
                (w, h) => GLines(w, h, (0.7f, 0), (0.1f, 0), (0.1f, 1), (0.7f, 1)),
                (w, h) => WLines(w, h, (0.7, 0), (0.1, 0), (0.1, 1), (0.7, 1)));

            // 32. Data Store (Kho dữ liệu mở - Hai thanh ngang song song)
            AddItem(cat, "flow_data_store", "Kho dữ liệu mở song song (Data Store)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.2f, w, h * 0.2f); gp.AddLine(0, h * 0.8f, w, h * 0.8f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.2), false, false); ctx.LineTo(new WpfPoint(w, h * 0.2), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.8), false, false); ctx.LineTo(new WpfPoint(w, h * 0.8), true, false); } g.Freeze(); return g; });

            // 33. External Entity (Thực thể bên ngoài - Khung viền đôi)
            AddItem(cat, "flow_external_entity", "Thực thể ngoài hệ thống (External Entity)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddRectangle(new RectangleF(w * 0.1f, h * 0.1f, w * 0.8f, h * 0.8f)); return gp; },
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1), (0, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9), (0.1, 0.1)));

            // 34. Cloud Service (Quy trình dịch vụ đám mây)
            AddItem(cat, "flow_cloud_service", "Dịch vụ đám mây (Cloud Service)",
                (w, h) => GStar(w, h, 10, 0.8),
                (w, h) => WStar(w, h, 10, 0.8));

            // 35. Timer Event (Sự kiện bộ đếm thời gian - Đồng hồ tròn có kim)
            AddItem(cat, "flow_timer_event", "Sự kiện thời gian chờ (Timer Event)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.5f, h * 0.2f); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.75f, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w / 2.0, h / 2.0), false, false); ctx.LineTo(new WpfPoint(w / 2.0, h * 0.2), true, false); ctx.BeginFigure(new WpfPoint(w / 2.0, h / 2.0), false, false); ctx.LineTo(new WpfPoint(w * 0.75, h / 2.0), true, false); } g.Freeze(); return g; });

            // 36. Message Event (Sự kiện gửi nhận thông điệp - Phong thư)
            AddItem(cat, "flow_message_event", "Sự kiện thông điệp thư từ (Message Event)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, h * 0.15f, w, h * 0.7f)); gp.AddLine(0, h * 0.15f, w * 0.5f, h * 0.55f); gp.AddLine(w * 0.5f, h * 0.55f, w, h * 0.15f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.15), true, true); ctx.LineTo(new WpfPoint(w, h * 0.15), true, false); ctx.LineTo(new WpfPoint(w, h * 0.85), true, false); ctx.LineTo(new WpfPoint(0, h * 0.85), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h * 0.55), true, false); ctx.LineTo(new WpfPoint(w, h * 0.15), true, false); } g.Freeze(); return g; });

            // 37. Rule Decision (Quy tắc nghiệp vụ - Cuốn sách)
            AddItem(cat, "flow_rule_decision", "Quy tắc nghiệp vụ (Business Rule)",
                (w, h) => GPoly(w, h, (0.05f, 0.1f), (0.5f, 0.25f), (0.95f, 0.1f), (0.95f, 0.85f), (0.5f, 0.98f), (0.05f, 0.85f)),
                (w, h) => WPoly(w, h, (0.05, 0.1), (0.5, 0.25), (0.95, 0.1), (0.95, 0.85), (0.5, 0.98), (0.05, 0.85)));

            // 38. Service Task (Tác vụ dịch vụ tự động - Bánh răng cơ khí)
            AddItem(cat, "flow_service_task", "Tác vụ hệ thống dịch vụ (Service Task Gear)",
                (w, h) => GStar(w, h, 8, 0.75),
                (w, h) => WStar(w, h, 8, 0.75));

            // 39. User Task (Tác vụ người dùng tương tác - Hình người)
            AddItem(cat, "flow_user_task", "Tác vụ người dùng tương tác (User Task)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.35f, 0, w * 0.3f, h * 0.4f); gp.AddArc(w * 0.1f, h * 0.45f, w * 0.8f, h * 0.6f, 180, 180); gp.CloseFigure(); return gp; },
                (w, h) => WPoly(w, h, (0.5, 0.05), (0.65, 0.2), (0.65, 0.35), (0.5, 0.45), (0.35, 0.35), (0.35, 0.2), (0.5, 0.05), (0.1, 0.95), (0.9, 0.95)));

            // 40. Manual Task (Tác vụ thủ công cờ lê)
            AddItem(cat, "flow_manual_task", "Tác vụ cờ lê thủ công (Manual Task)",
                (w, h) => GPoly(w, h, (0.1f, 0.9f), (0.5f, 0.5f), (0.4f, 0.3f), (0.6f, 0.1f), (0.8f, 0.1f), (0.85f, 0.25f), (0.75f, 0.35f), (0.9f, 0.4f), (0.7f, 0.6f), (0.5f, 0.5f), (0.2f, 1)),
                (w, h) => WPoly(w, h, (0.1, 0.9), (0.5, 0.5), (0.4, 0.3), (0.6, 0.1), (0.8, 0.1), (0.85, 0.25), (0.75, 0.35), (0.9, 0.4), (0.7, 0.6), (0.5, 0.5), (0.2, 1)));

            // 41. Script Task (Tác vụ kịch bản mã - Cuộn giấy)
            AddItem(cat, "flow_script_task", "Tác vụ kịch bản mã (Script Task Scroll)",
                (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.85f, 0.1f), (0.9f, 0.3f), (0.8f, 0.85f), (0.15f, 0.85f), (0.05f, 0.3f)),
                (w, h) => WPoly(w, h, (0.1, 0.1), (0.85, 0.1), (0.9, 0.3), (0.8, 0.85), (0.15, 0.85), (0.05, 0.3)));

            // 42. Send Task (Tác vụ gửi đi - Máy bay giấy)
            AddItem(cat, "flow_send_task", "Tác vụ gửi đi (Send Task Airplane)",
                (w, h) => GPoly(w, h, (0.05f, 0.5f), (0.95f, 0.05f), (0.55f, 0.95f), (0.45f, 0.6f)),
                (w, h) => WPoly(w, h, (0.05, 0.5), (0.95, 0.05), (0.55, 0.95), (0.45, 0.6)));

            // 43. Receive Task (Tác vụ tiếp nhận - Khay thư mũi tên xuống)
            AddItem(cat, "flow_receive_task", "Tác vụ tiếp nhận (Receive Task Inbox)",
                (w, h) => GPoly(w, h, (0, 0.4f), (0.2f, 0.4f), (0.3f, 0.7f), (0.7f, 0.7f), (0.8f, 0.4f), (1, 0.4f), (0.9f, 0.95f), (0.1f, 0.95f)),
                (w, h) => WPoly(w, h, (0, 0.4), (0.2, 0.4), (0.3, 0.7), (0.7, 0.7), (0.8, 0.4), (1, 0.4), (0.9, 0.95), (0.1, 0.95)));

            // 44. Parallel Gateway (Cổng phân luồng song song - Dấu cộng trong thoi)
            AddItem(cat, "flow_parallel_gateway", "Cổng phân luồng song song (Parallel Gateway +)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddLine(w * 0.5f, h * 0.25f, w * 0.5f, h * 0.75f); gp.AddLine(w * 0.25f, h * 0.5f, w * 0.75f, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.25), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h * 0.75), true, false); ctx.BeginFigure(new WpfPoint(w * 0.25, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.75, h * 0.5), true, false); } g.Freeze(); return g; });

            // 45. Inclusive Gateway (Cổng phân luồng bao hàm - Vòng tròn trong thoi)
            AddItem(cat, "flow_inclusive_gateway", "Cổng phân luồng bao hàm (Inclusive Gateway O)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddEllipse(w * 0.35f, h * 0.35f, w * 0.3f, h * 0.3f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.35), true, true); ctx.ArcTo(new WpfPoint(w * 0.5, h * 0.65), new WpfSize(w * 0.15, h * 0.15), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.5, h * 0.35), new WpfSize(w * 0.15, h * 0.15), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });

            // 46. Complex Gateway (Cổng phân luồng phức tạp - Dấu hoa thị * trong thoi)
            AddItem(cat, "flow_complex_gateway", "Cổng phân luồng phức tạp (Complex Gateway *)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddLine(w * 0.5f, h * 0.25f, w * 0.5f, h * 0.75f); gp.AddLine(w * 0.25f, h * 0.5f, w * 0.75f, h * 0.5f); gp.AddLine(w * 0.32f, h * 0.32f, w * 0.68f, h * 0.68f); gp.AddLine(w * 0.68f, h * 0.32f, w * 0.32f, h * 0.68f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.25), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h * 0.75), true, false); ctx.BeginFigure(new WpfPoint(w * 0.25, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.75, h * 0.5), true, false); } g.Freeze(); return g; });

            // 47. Event Gateway (Cổng kích hoạt sự kiện - Ngũ giác trong thoi)
            AddItem(cat, "flow_event_gateway", "Cổng kích hoạt sự kiện (Event Gateway)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, h * 0.28f), new PointF(w * 0.7f, h * 0.42f), new PointF(w * 0.62f, h * 0.68f), new PointF(w * 0.38f, h * 0.68f), new PointF(w * 0.3f, h * 0.42f) }); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.28), true, true); ctx.LineTo(new WpfPoint(w * 0.7, h * 0.42), true, false); ctx.LineTo(new WpfPoint(w * 0.62, h * 0.68), true, false); ctx.LineTo(new WpfPoint(w * 0.38, h * 0.68), true, false); ctx.LineTo(new WpfPoint(w * 0.3, h * 0.42), true, false); } g.Freeze(); return g; });

            // 48. Error Event (Sự kiện lỗi - Vòng tròn có tia sét)
            AddItem(cat, "flow_error_event", "Sự kiện lỗi ngoại lệ (Error Event Lightning)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddPolygon(new PointF[] { new PointF(w * 0.55f, h * 0.2f), new PointF(w * 0.35f, h * 0.55f), new PointF(w * 0.52f, h * 0.55f), new PointF(w * 0.45f, h * 0.8f), new PointF(w * 0.68f, h * 0.45f), new PointF(w * 0.52f, h * 0.45f) }); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.55, h * 0.2), true, true); ctx.LineTo(new WpfPoint(w * 0.35, h * 0.55), true, false); ctx.LineTo(new WpfPoint(w * 0.52, h * 0.55), true, false); ctx.LineTo(new WpfPoint(w * 0.45, h * 0.8), true, false); ctx.LineTo(new WpfPoint(w * 0.68, h * 0.45), true, false); ctx.LineTo(new WpfPoint(w * 0.52, h * 0.45), true, false); } g.Freeze(); return g; });

            // 49. Terminate Event (Sự kiện kết thúc toàn bộ - Hai vòng tròn đồng tâm)
            AddItem(cat, "flow_terminate_event", "Sự kiện kết thúc toàn bộ (Terminate End Event)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddEllipse(w * 0.25f, h * 0.25f, w * 0.5f, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w / 2.0, h * 0.25), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h * 0.75), new WpfSize(w * 0.25, h * 0.25), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, h * 0.25), new WpfSize(w * 0.25, h * 0.25), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });

            // 50. Cancel Event (Sự kiện hủy bỏ quy trình - Vòng tròn có dấu X)
            AddItem(cat, "flow_cancel_event", "Sự kiện hủy bỏ quy trình (Cancel End Event)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.3f, h * 0.3f, w * 0.7f, h * 0.7f); gp.AddLine(w * 0.7f, h * 0.3f, w * 0.3f, h * 0.7f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.3, h * 0.3), false, false); ctx.LineTo(new WpfPoint(w * 0.7, h * 0.7), true, false); ctx.BeginFigure(new WpfPoint(w * 0.7, h * 0.3), false, false); ctx.LineTo(new WpfPoint(w * 0.3, h * 0.7), true, false); } g.Freeze(); return g; });
        }
        #endregion
'''
