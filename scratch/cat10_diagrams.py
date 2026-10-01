# -*- coding: utf-8 -*-
"""Category 10: Diagrams & Mindmaps (50 presets)"""

def get_code():
    return '''
        #region Category 10: Sơ đồ tư duy & Cụm liên kết (50 mẫu)
        private static void PopulateDiagramComponents()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Diagrams", Title = "Sơ đồ tư duy & Cụm liên kết" };
            _categories.Add(cat);

            // 1-5: Phễu chuyển đổi (Conversion Funnels)
            AddItem(cat, "funnel_3stage", "Phễu chuyển đổi 3 tầng (Funnel 3)", (w, h) => GPoly(w, h, (0.05f, 0.05f), (0.95f, 0.05f), (0.65f, 0.95f), (0.35f, 0.95f)), (w, h) => WPoly(w, h, (0.05, 0.05), (0.95, 0.05), (0.65, 0.95), (0.35, 0.95)));
            AddItem(cat, "funnel_4stage", "Phễu chuyển đổi 4 tầng (Funnel 4)", (w, h) => GPoly(w, h, (0.02f, 0.05f), (0.98f, 0.05f), (0.62f, 0.95f), (0.38f, 0.95f)), (w, h) => WPoly(w, h, (0.02, 0.05), (0.98, 0.05), (0.62, 0.95), (0.38, 0.95)));
            AddItem(cat, "funnel_5stage", "Phễu chuyển đổi 5 tầng (Funnel 5)", (w, h) => GPoly(w, h, (0, 0.05f), (1, 0.05f), (0.6f, 0.95f), (0.4f, 0.95f)), (w, h) => WPoly(w, h, (0, 0.05), (1, 0.05), (0.6, 0.95), (0.4, 0.95)));
            AddItem(cat, "funnel_inverted_expand", "Mô hình phễu ngược mở rộng", (w, h) => GPoly(w, h, (0.35f, 0.05f), (0.65f, 0.05f), (0.95f, 0.95f), (0.05f, 0.95f)), (w, h) => WPoly(w, h, (0.35, 0.05), (0.65, 0.05), (0.95, 0.95), (0.05, 0.95)));
            AddItem(cat, "funnel_multichannel", "Phễu lọc bán hàng đa kênh", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.7f, 0.9f), (0.3f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.7, 0.9), (0.3, 0.9)));

            // 6-10: Kim tự tháp phân cấp (Hierarchy Pyramids)
            AddItem(cat, "pyramid_3tier", "Kim tự tháp phân cấp 3 tầng (Pyramid 3)", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.95f, 0.95f), (0.05f, 0.95f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.95, 0.95), (0.05, 0.95)));
            AddItem(cat, "pyramid_4tier", "Kim tự tháp phân cấp 4 tầng (Pyramid 4)", (w, h) => GPoly(w, h, (0.5f, 0.02f), (0.98f, 0.98f), (0.02f, 0.98f)), (w, h) => WPoly(w, h, (0.5, 0.02), (0.98, 0.98), (0.02, 0.98)));
            AddItem(cat, "pyramid_5tier", "Kim tự tháp Maslow 5 tầng", (w, h) => GPoly(w, h, (0.5f, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0.5, 0), (1, 1), (0, 1)));
            AddItem(cat, "pyramid_inverted", "Kim tự tháp lộn ngược thách thức", (w, h) => GPoly(w, h, (0.05f, 0.05f), (0.95f, 0.05f), (0.5f, 0.95f)), (w, h) => WPoly(w, h, (0.05, 0.05), (0.95, 0.05), (0.5, 0.95)));
            AddItem(cat, "pyramid_stepped", "Kim tự tháp bậc thang phân bậc", (w, h) => GPoly(w, h, (0.35f, 0.1f), (0.65f, 0.1f), (0.65f, 0.4f), (0.8f, 0.4f), (0.8f, 0.7f), (0.95f, 0.7f), (0.95f, 1), (0.05f, 1), (0.05f, 0.7f), (0.2f, 0.7f), (0.2f, 0.4f), (0.35f, 0.4f)), (w, h) => WPoly(w, h, (0.35, 0.1), (0.65, 0.1), (0.65, 0.4), (0.8, 0.4), (0.8, 0.7), (0.95, 0.7), (0.95, 1), (0.05, 1), (0.05, 0.7), (0.2, 0.7), (0.2, 0.4), (0.35, 0.4)));

            // 11-15: Biểu đồ Venn & giao thoa
            AddItem(cat, "venn_2circle", "Biểu đồ Venn 2 vòng tròn giao nhau", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, h * 0.15f, w * 0.65f, h * 0.7f); gp.AddEllipse(w * 0.35f, h * 0.15f, w * 0.65f, h * 0.7f); return gp; }, (w, h) => WPoly(w, h, (0.35, 0.15), (0.65, 0.15), (1, 0.5), (0.65, 0.85), (0.35, 0.85), (0, 0.5)));
            AddItem(cat, "venn_3circle", "Biểu đồ Venn 3 vòng tròn giao thoa", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.2f, 0, w * 0.6f, h * 0.6f); gp.AddEllipse(0, h * 0.4f, w * 0.6f, h * 0.6f); gp.AddEllipse(w * 0.4f, h * 0.4f, w * 0.6f, h * 0.6f); return gp; }, (w, h) => WStar(w, h, 3, 0.6));
            AddItem(cat, "venn_4circle", "Biểu đồ Venn 4 cánh giao hòa", (w, h) => GStar(w, h, 4, 0.65), (w, h) => WStar(w, h, 4, 0.65));
            AddItem(cat, "euler_diagram", "Sơ đồ tập hợp con Euler", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddEllipse(w * 0.4f, h * 0.3f, w * 0.45f, h * 0.45f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "ikigai_4circle", "Sơ đồ Ikigai 4 vòng tròn hội tụ", (w, h) => GStar(w, h, 8, 0.7), (w, h) => WStar(w, h, 8, 0.7));

            // 16-20: Chu trình lặp (Cycles)
            AddItem(cat, "cycle_3arrow", "Chu trình tròn 3 mũi tên khép kín", (w, h) => GStar(w, h, 3, 0.5), (w, h) => WStar(w, h, 3, 0.5));
            AddItem(cat, "cycle_4arrow", "Chu trình tròn 4 mũi tên khép kín", (w, h) => GStar(w, h, 4, 0.55), (w, h) => WStar(w, h, 4, 0.55));
            AddItem(cat, "cycle_5arrow", "Chu trình tròn 5 mũi tên khép kín", (w, h) => GStar(w, h, 5, 0.6), (w, h) => WStar(w, h, 5, 0.6));
            AddItem(cat, "cycle_6arrow", "Chu trình tròn 6 mũi tên khép kín", (w, h) => GStar(w, h, 6, 0.65), (w, h) => WStar(w, h, 6, 0.65));
            AddItem(cat, "flywheel_growth", "Bánh đà tăng trưởng Flywheel", (w, h) => GStar(w, h, 8, 0.75), (w, h) => WStar(w, h, 8, 0.75));

            // 21-27: Bánh răng & bia ngắm
            AddItem(cat, "target_bullseye_3ring", "Bia ngắm mục tiêu 3 vòng (Bullseye)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddEllipse(w * 0.2f, h * 0.2f, w * 0.6f, h * 0.6f); gp.AddEllipse(w * 0.4f, h * 0.4f, w * 0.2f, h * 0.2f); gp.AddLine(w * 0.5f, 0, w * 0.5f, h); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "target_arrow_center", "Bia ngắm trúng hồng tâm", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.1f, h * 0.9f, w * 0.5f, h * 0.5f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "gear_6tooth", "Bánh răng cơ khí 6 răng (Gear 6)", (w, h) => GStar(w, h, 6, 0.65), (w, h) => WStar(w, h, 6, 0.65));
            AddItem(cat, "gear_8tooth", "Bánh răng cơ khí 8 răng (Gear 8)", (w, h) => GStar(w, h, 8, 0.7), (w, h) => WStar(w, h, 8, 0.7));
            AddItem(cat, "gear_12tooth", "Bánh răng cơ khí 12 răng chính xác", (w, h) => GStar(w, h, 12, 0.75), (w, h) => WStar(w, h, 12, 0.75));
            AddItem(cat, "gear_dual_interlock", "Cụm bánh răng lồng ghép 2 trục", (w, h) => GPoly(w, h, (0.3f, 0.1f), (0.6f, 0.1f), (0.7f, 0.4f), (0.9f, 0.4f), (0.9f, 0.7f), (0.6f, 0.9f), (0.3f, 0.9f), (0.1f, 0.6f)), (w, h) => WPoly(w, h, (0.3, 0.1), (0.6, 0.1), (0.7, 0.4), (0.9, 0.4), (0.9, 0.7), (0.6, 0.9), (0.3, 0.9), (0.1, 0.6)));
            AddItem(cat, "gear_train_3gear", "Hệ thống truyền động 3 bánh răng", (w, h) => GStar(w, h, 6, 0.7), (w, h) => WStar(w, h, 6, 0.7));

            // 28-33: Sơ đồ tư duy & tổ ong
            AddItem(cat, "mindmap_hub_center", "Sơ đồ tư duy nút trung tâm 4 nhánh", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.35f, h * 0.35f, w * 0.3f, h * 0.3f); gp.AddLine(w * 0.5f, h * 0.35f, w * 0.5f, 0.05f); gp.AddLine(w * 0.5f, h * 0.65f, w * 0.5f, 0.95f); gp.AddLine(w * 0.35f, h * 0.5f, 0.05f, h * 0.5f); gp.AddLine(w * 0.65f, h * 0.5f, 0.95f, h * 0.5f); return gp; }, (w, h) => WPoly(w, h, (0.5, 0.05), (0.65, 0.35), (0.95, 0.5), (0.65, 0.65), (0.5, 0.95), (0.35, 0.65), (0.05, 0.5), (0.35, 0.35)));
            AddItem(cat, "mindmap_tree_branch", "Bản đồ tư duy nút nhánh bo tròn", (w, h) => GRoundedRect(w, h * 0.5f, 8f), (w, h) => WRoundedRect(w, h * 0.5, 8.0));
            AddItem(cat, "honeycomb_3cluster", "Cụm tổ ong 3 lục giác liên hoàn", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.25f), (0.9f, 0.75f), (0.5f, 1), (0.1f, 0.75f), (0.1f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.25), (0.9, 0.75), (0.5, 1), (0.1, 0.75), (0.1, 0.25)));
            AddItem(cat, "honeycomb_7cluster", "Cụm tổ ong 7 lục giác hoa văn", (w, h) => GRegularPoly(w, h, 6, 0), (w, h) => WRegularPoly(w, h, 6, 0));
            AddItem(cat, "neural_network_nodes", "Mạng lưới nơ-ron kết nối", (w, h) => GPoly(w, h, (0.15f, 0.2f), (0.85f, 0.2f), (0.85f, 0.8f), (0.15f, 0.8f)), (w, h) => WPoly(w, h, (0.15, 0.2), (0.85, 0.2), (0.85, 0.8), (0.15, 0.8)));
            AddItem(cat, "binary_tree_hierarchy", "Sơ đồ phân nhánh nhị phân Binary", (w, h) => GLines(w, h, (0.5f, 0.1f), (0.25f, 0.5f), (0.1f, 0.9f), (0.25f, 0.5f), (0.4f, 0.9f), (0.5f, 0.1f), (0.75f, 0.5f), (0.6f, 0.9f), (0.75f, 0.5f), (0.9f, 0.9f)), (w, h) => WLines(w, h, (0.5, 0.1), (0.25, 0.5), (0.1, 0.9), (0.25, 0.5), (0.4, 0.9), (0.5, 0.1), (0.75, 0.5), (0.6, 0.9), (0.75, 0.5), (0.9, 0.9)));

            // 34-50: Các mô hình quản trị chiến lược & tiến độ
            AddItem(cat, "diag_fishbone_ishikawa", "Sơ đồ xương cá phân tích lỗi", (w, h) => GLines(w, h, (0.05f, 0.5f), (0.85f, 0.5f), (0.85f, 0.35f), (0.95f, 0.5f), (0.85f, 0.65f), (0.85f, 0.5f), (0.25f, 0.5f), (0.4f, 0.15f), (0.25f, 0.5f), (0.4f, 0.85f), (0.55f, 0.5f), (0.7f, 0.15f), (0.55f, 0.5f), (0.7f, 0.85f)), (w, h) => WLines(w, h, (0.05, 0.5), (0.85, 0.5), (0.85, 0.35), (0.95, 0.5), (0.85, 0.65), (0.85, 0.5), (0.25, 0.5), (0.4, 0.15), (0.25, 0.5), (0.4, 0.85), (0.55, 0.5), (0.7, 0.15), (0.55, 0.5), (0.7, 0.85)));
            AddItem(cat, "diag_matrix_2x2", "Ma trận 4 ô góc vuông 2x2", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddLine(w * 0.5f, 0, w * 0.5f, h); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h), true, false); ctx.LineTo(new WpfPoint(0, h), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, 0), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); } g.Freeze(); return g; });
            AddItem(cat, "diag_org_chart", "Sơ đồ cơ cấu tổ chức Org Chart", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(w * 0.35f, 0, w * 0.3f, h * 0.25f)); gp.AddLine(w * 0.5f, h * 0.25f, w * 0.5f, h * 0.5f); gp.AddLine(w * 0.15f, h * 0.5f, w * 0.85f, h * 0.5f); gp.AddRectangle(new RectangleF(0, h * 0.6f, w * 0.3f, h * 0.25f)); gp.AddRectangle(new RectangleF(w * 0.35f, h * 0.6f, w * 0.3f, h * 0.25f)); gp.AddRectangle(new RectangleF(w * 0.7f, h * 0.6f, w * 0.3f, h * 0.25f)); return gp; }, (w, h) => WPoly(w, h, (0.35f, 0), (0.65f, 0), (0.65f, 0.25f), (0.35f, 0.25f)));
            AddItem(cat, "diag_swot_box", "Mô hình SWOT 4 góc nổi bật", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "diag_chevron_pipeline", "Chuỗi quy trình nối tiếp 3 Chevron", (w, h) => GPoly(w, h, (0, 0), (0.75f, 0), (1, 0.5f), (0.75f, 1), (0, 1), (0.25f, 0.5f)), (w, h) => WPoly(w, h, (0, 0), (0.75, 0), (1, 0.5), (0.75, 1), (0, 1), (0.25, 0.5)));
            AddItem(cat, "diag_arrow_pipeline", "Đường ống mũi tên chuyển tiếp", (w, h) => GPoly(w, h, (0, 0.25f), (0.75f, 0.25f), (0.75f, 0.05f), (1, 0.5f), (0.75f, 0.95f), (0.75f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.25), (0.75, 0.25), (0.75, 0.05), (1, 0.5), (0.75, 0.95), (0.75, 0.75), (0, 0.75)));
            AddItem(cat, "diag_chain_link", "Chuỗi mắt xích liên kết 2 mắt", (w, h) => GPoly(w, h, (0.15f, 0.3f), (0.85f, 0.3f), (0.85f, 0.7f), (0.15f, 0.7f)), (w, h) => WPoly(w, h, (0.15, 0.3), (0.85, 0.3), (0.85, 0.7), (0.15, 0.7)));
            AddItem(cat, "diag_balance_scale", "Cân thăng bằng đối trọng", (w, h) => GLines(w, h, (0.5f, 0.1f), (0.5f, 0.9f), (0.3f, 0.9f), (0.7f, 0.9f), (0.5f, 0.2f), (0.15f, 0.4f), (0.5f, 0.2f), (0.85f, 0.4f)), (w, h) => WLines(w, h, (0.5, 0.1), (0.5, 0.9), (0.3, 0.9), (0.7, 0.9), (0.5, 0.2), (0.15, 0.4), (0.5, 0.2), (0.85, 0.4)));
            AddItem(cat, "diag_hourglass_timer", "Đồng hồ cát đo thời hạn dự án", (w, h) => GPoly(w, h, (0.1f, 0.05f), (0.9f, 0.05f), (0.5f, 0.5f), (0.9f, 0.95f), (0.1f, 0.95f), (0.5f, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0.05), (0.9, 0.05), (0.5, 0.5), (0.9, 0.95), (0.1, 0.95), (0.5, 0.5)));
            AddItem(cat, "diag_temple_pillars", "Cột trụ thành công 3 cột đền", (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.25f), (0.85f, 0.25f), (0.85f, 0.8f), (0.95f, 0.8f), (0.95f, 1), (0.05f, 1), (0.05f, 0.8f), (0.15f, 0.8f), (0.15f, 0.25f), (0.05f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.25), (0.85, 0.25), (0.85, 0.8), (0.95, 0.8), (0.95, 1), (0.05, 1), (0.05, 0.8), (0.15, 0.8), (0.15, 0.25), (0.05, 0.25)));
            AddItem(cat, "diag_timeline_axis", "Trục thời gian mốc Node kim cương", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f), (0.25f, 0.35f), (0.35f, 0.5f), (0.25f, 0.65f), (0.15f, 0.5f), (0.25f, 0.35f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5), (0.25, 0.35), (0.35, 0.5), (0.25, 0.65), (0.15, 0.5), (0.25, 0.35)));
            AddItem(cat, "diag_roadmap_spiral", "Lộ trình quanh co phát triển", (w, h) => GLines(w, h, (0.1f, 0.9f), (0.4f, 0.6f), (0.2f, 0.35f), (0.7f, 0.15f), (0.9f, 0.05f)), (w, h) => WLines(w, h, (0.1, 0.9), (0.4, 0.6), (0.2, 0.35), (0.7, 0.15), (0.9, 0.05)));
            AddItem(cat, "diag_porter_diamond", "Mô hình kim cương Porter Diamond", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.95f, 0.5f), (0.5f, 0.95f), (0.05f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.95, 0.5), (0.5, 0.95), (0.05, 0.5)));
            AddItem(cat, "diag_kpi_dashboard", "Bảng đồng hồ tiến độ 3 chỉ số", (w, h) => GRoundedRect(w, h, 6f), (w, h) => WRoundedRect(w, h, 6.0));
            AddItem(cat, "diag_heat_matrix", "Bản đồ nhiệt ma trận rủi ro", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "diag_pricing_tier", "Bộ 3 thẻ bài bảng giá dịch vụ", (w, h) => GPoly(w, h, (0.05f, 0.15f), (0.95f, 0.15f), (0.95f, 0.85f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.05, 0.15), (0.95, 0.15), (0.95, 0.85), (0.05, 0.85)));
            AddItem(cat, "diag_scorecard_compass", "Thẻ điểm cân bằng chiến lược", (w, h) => GStar(w, h, 4, 0.45), (w, h) => WStar(w, h, 4, 0.45));
        }
        #endregion
'''
