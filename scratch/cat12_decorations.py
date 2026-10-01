# -*- coding: utf-8 -*-
"""Category 12: Borders & Dividers (50 presets)"""

def get_code():
    return '''
        #region Category 12: Khung viền & Họa tiết trang trí (50 mẫu)
        private static void PopulateBordersAndDividers()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Decorations", Title = "Khung viền & Họa tiết trang trí" };
            _categories.Add(cat);

            // 1-10: Khung viền chữ nhật các kiểu
            AddItem(cat, "border_double_rect", "Khung viền đôi hai lớp (Double Line)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddRectangle(new RectangleF(w * 0.08f, h * 0.08f, w * 0.84f, h * 0.84f)); return gp; }, (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1), (0, 0.08), (0.92, 0.08), (0.92, 0.92), (0.08, 0.92), (0.08, 0.08)));
            AddItem(cat, "border_triple_rect", "Khung viền ba thanh mảnh", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddRectangle(new RectangleF(w * 0.05f, h * 0.05f, w * 0.9f, h * 0.9f)); gp.AddRectangle(new RectangleF(w * 0.1f, h * 0.1f, w * 0.8f, h * 0.8f)); return gp; }, (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1), (0, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9), (0.1, 0.1)));
            AddItem(cat, "border_snip_corner", "Khung viền vát 4 góc 45°", (w, h) => GPoly(w, h, (0.15f, 0), (0.85f, 0), (1, 0.15f), (1, 0.85f), (0.85f, 1), (0.15f, 1), (0, 0.85f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.15, 0), (0.85, 0), (1, 0.15), (1, 0.85), (0.85, 1), (0.15, 1), (0, 0.85), (0, 0.15)));
            AddItem(cat, "border_snip_round", "Khung viền vát trên, bo tròn dưới", (w, h) => GPoly(w, h, (0.15f, 0), (0.85f, 0), (1, 0.15f), (1, 0.85f), (0.85f, 1), (0.15f, 1), (0, 0.85f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.15, 0), (0.85, 0), (1, 0.15), (1, 0.85), (0.85, 1), (0.15, 1), (0, 0.85), (0, 0.15)));
            AddItem(cat, "border_round_corner", "Khung viền bo tròn 4 góc mềm", (w, h) => GRoundedRect(w, h, 10f), (w, h) => WRoundedRect(w, h, 10.0));
            AddItem(cat, "border_heavy_frame", "Khung viền bản đậm dày", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "border_dotted_stitch", "Khung viền đường may nét đứt", (w, h) => GRoundedRect(w, h, 6f), (w, h) => WRoundedRect(w, h, 6.0));
            AddItem(cat, "border_polaroid_photo", "Khung ảnh phong cách Polaroid", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "border_moroccan_arch", "Khung vòm phong cách Moroccan", (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.25f), (0.95f, 1), (0.05f, 1), (0.05f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.25), (0.95, 1), (0.05, 1), (0.05, 0.25)));
            AddItem(cat, "border_scalloped_edge", "Khung viền răng cưa cánh sen", (w, h) => GStar(w, h, 16, 0.92), (w, h) => WStar(w, h, 16, 0.92));

            // 11-20: Khung góc nghệ thuật & hoa văn
            AddItem(cat, "border_flourish_corners", "Khung họa tiết hoa văn 4 góc", (w, h) => GPoly(w, h, (0.15f, 0.05f), (0.85f, 0.05f), (0.95f, 0.15f), (0.95f, 0.85f), (0.85f, 0.95f), (0.15f, 0.95f), (0.05f, 0.85f), (0.05f, 0.15f)), (w, h) => WPoly(w, h, (0.15, 0.05), (0.85, 0.05), (0.95, 0.15), (0.95, 0.85), (0.85, 0.95), (0.15, 0.95), (0.05, 0.85), (0.05, 0.15)));
            AddItem(cat, "border_certificate_guilloche", "Khung viền bằng khen chứng chỉ", (w, h) => GPoly(w, h, (0.08f, 0.08f), (0.92f, 0.08f), (0.92f, 0.92f), (0.08f, 0.92f)), (w, h) => WPoly(w, h, (0.08, 0.08), (0.92, 0.08), (0.92, 0.92), (0.08, 0.92)));
            AddItem(cat, "border_greek_key", "Khung viền hoa văn Hy Lạp cổ", (w, h) => GPoly(w, h, (0.05f, 0.05f), (0.95f, 0.05f), (0.95f, 0.95f), (0.05f, 0.95f)), (w, h) => WPoly(w, h, (0.05, 0.05), (0.95, 0.05), (0.95, 0.95), (0.05, 0.95)));
            AddItem(cat, "border_corner_brackets", "Khung nhấn 4 góc thước thợ", (w, h) => GLines(w, h, (0.25f, 0.05f), (0.05f, 0.05f), (0.05f, 0.25f), (0.05f, 0.75f), (0.05f, 0.95f), (0.25f, 0.95f), (0.75f, 0.95f), (0.95f, 0.95f), (0.95f, 0.75f), (0.95f, 0.25f), (0.95f, 0.05f), (0.75f, 0.05f)), (w, h) => WLines(w, h, (0.25, 0.05), (0.05, 0.05), (0.05, 0.25)));
            AddItem(cat, "border_beaded_pearl", "Khung chuỗi hạt ngọc trai bao quanh", (w, h) => GRoundedRect(w, h, 8f), (w, h) => WRoundedRect(w, h, 8.0));
            AddItem(cat, "border_art_deco_geom", "Khung hình học Art Deco sang trọng", (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 0.2f), (1, 0.8f), (0.8f, 1), (0.2f, 1), (0, 0.8f), (0, 0.2f)), (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 0.2), (1, 0.8), (0.8, 1), (0.2, 1), (0, 0.8), (0, 0.2)));
            AddItem(cat, "border_vintage_filigree", "Khung hoa văn Vintage cổ điển", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "border_ivy_vine", "Khung viền dây leo trường xuân", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "border_floral_wreath", "Khung viền vòng hoa lá nguyệt quế", (w, h) => GEllipse(w, h), (w, h) => WEllipse(w, h));
            AddItem(cat, "border_gothic_pointed", "Khung vòm nhọn Gothic giáo đường", (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.35f), (0.95f, 1), (0.05f, 1), (0.05f, 0.35f)), (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.35), (0.95, 1), (0.05, 1), (0.05, 0.35)));

            // 21-35: Đường phân cách ngang (Horizontal Rule Dividers)
            AddItem(cat, "divider_diamond_center", "Đường kẻ ngang kim cương chính giữa", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.5f, w * 0.42f, h * 0.5f); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, h * 0.15f), new PointF(w * 0.58f, h * 0.5f), new PointF(w * 0.5f, h * 0.85f), new PointF(w * 0.42f, h * 0.5f) }); gp.AddLine(w * 0.58f, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => WPoly(w, h, (0.5, 0.15), (0.58, 0.5), (0.5, 0.85), (0.42, 0.5)));
            AddItem(cat, "divider_star_trio", "Đường kẻ ngang 3 ngôi sao chính giữa", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.5f, w * 0.35f, h * 0.5f); gp.AddLine(w * 0.65f, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => WLines(w, h, (0, 0.5f), (1, 0.5f)));
            AddItem(cat, "divider_floral_swirl", "Đường phân cách hoa văn uốn lượn", (w, h) => GLines(w, h, (0, 0.5f), (0.35f, 0.5f), (0.45f, 0.2f), (0.55f, 0.8f), (0.65f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.35, 0.5), (0.45, 0.2), (0.55, 0.8), (0.65, 0.5), (1, 0.5)));
            AddItem(cat, "divider_tapered_rule", "Đường kẻ vuốt nhọn hai đầu (Tapered)", (w, h) => GPoly(w, h, (0, 0.5f), (0.5f, 0.3f), (1, 0.5f), (0.5f, 0.7f)), (w, h) => WPoly(w, h, (0, 0.5), (0.5, 0.3), (1, 0.5), (0.5, 0.7)));
            AddItem(cat, "divider_double_thin", "Đường phân cách song song thanh mảnh", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.4f, w, h * 0.4f); gp.AddLine(0, h * 0.6f, w, h * 0.6f); return gp; }, (w, h) => WLines(w, h, (0, 0.5f), (1, 0.5f)));
            AddItem(cat, "divider_triple_beaded", "Đường ba thanh mảnh đính hạt ngọc", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_sine_wave", "Đường phân cách sóng sin mềm mại", (w, h) => GLines(w, h, (0, 0.5f), (0.25f, 0.2f), (0.75f, 0.8f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.25, 0.2), (0.75, 0.8), (1, 0.5)));
            AddItem(cat, "divider_zigzag_chevron", "Đường phân cách gấp khúc Chevron", (w, h) => GLines(w, h, (0, 0.5f), (0.4f, 0.5f), (0.45f, 0.2f), (0.5f, 0.8f), (0.55f, 0.2f), (0.6f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.4, 0.5), (0.45, 0.2), (0.5, 0.8), (0.55, 0.2), (0.6, 0.5), (1, 0.5)));
            AddItem(cat, "divider_dash_dot_heavy", "Đường gạch chấm đậm phong cách kỹ thuật", (w, h) => GLines(w, h, (0, 0.5f), (0.35f, 0.5f), (0.45f, 0.5f), (0.55f, 0.5f), (0.65f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.35, 0.5)));
            AddItem(cat, "divider_cross_heraldic", "Đường phân cách chữ thập thập tự", (w, h) => GLines(w, h, (0, 0.5f), (0.42f, 0.5f), (0.58f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_infinity_loop", "Đường phân cách vòng lặp vô cực", (w, h) => GLines(w, h, (0, 0.5f), (0.35f, 0.5f), (0.65f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_inward_brackets", "Đường phân cách cặp ngoặc đối hướng", (w, h) => GLines(w, h, (0, 0.5f), (0.4f, 0.5f), (0.45f, 0.25f), (0.45f, 0.75f), (0.55f, 0.25f), (0.55f, 0.75f), (0.6f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_fleur_de_lis", "Đường hoa diên vĩ hoàng gia", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_laurel_branch", "Đường nhánh lá ô-liu vinh quang", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_arrowheads_accent", "Đường phân cách hai đầu mũi tên đối xứng", (w, h) => GLines(w, h, (0.05f, 0.35f), (0, 0.5f), (0.05f, 0.65f), (0, 0.5f), (1, 0.5f), (0.95f, 0.35f), (1, 0.5f), (0.95f, 0.65f)), (w, h) => WLines(w, h, (0.05, 0.35), (0, 0.5), (0.05, 0.65), (0, 0.5), (1, 0.5), (0.95, 0.35), (1, 0.5), (0.95, 0.65)));

            // 36-50: Thanh ngăn trang & dải họa tiết (Section Separator Bars)
            AddItem(cat, "bar_ribbon_banner", "Thanh dải ruy băng ngăn slide", (w, h) => GPoly(w, h, (0, 0.2f), (1, 0.2f), (0.95f, 0.8f), (0.05f, 0.8f)), (w, h) => WPoly(w, h, (0, 0.2), (1, 0.2), (0.95, 0.8), (0.05, 0.8)));
            AddItem(cat, "bar_segmented_progress", "Thanh phân đoạn tiến độ 5 mắt", (w, h) => GPoly(w, h, (0, 0.35f), (1, 0.35f), (1, 0.65f), (0, 0.65f)), (w, h) => WPoly(w, h, (0, 0.35), (1, 0.35), (1, 0.65), (0, 0.65)));
            AddItem(cat, "bar_diamond_chain", "Thanh chuỗi kim cương liên hoàn", (w, h) => GPoly(w, h, (0, 0.5f), (0.25f, 0.2f), (0.5f, 0.5f), (0.75f, 0.2f), (1, 0.5f), (0.75f, 0.8f), (0.5f, 0.5f), (0.25f, 0.8f)), (w, h) => WPoly(w, h, (0, 0.5), (0.25, 0.2), (0.5, 0.5), (0.75, 0.2), (1, 0.5), (0.75, 0.8), (0.5, 0.5), (0.25, 0.8)));
            AddItem(cat, "bar_pearl_bead_string", "Chuỗi hạt ngọc trai ngăn dòng", (w, h) => GRoundedRect(w, h * 0.4f, 5f), (w, h) => WRoundedRect(w, h * 0.4, 5.0));
            AddItem(cat, "bar_geometric_hash", "Thanh sọc gạch chéo kỹ thuật", (w, h) => GPoly(w, h, (0, 0.3f), (1, 0.3f), (1, 0.7f), (0, 0.7f)), (w, h) => WPoly(w, h, (0, 0.3), (1, 0.3), (1, 0.7), (0, 0.7)));
            AddItem(cat, "bar_dotted_center_circle", "Đường nét đứt kèm vòng tròn tâm", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "bar_barbed_modern", "Thanh phân cách góc vát thể thao", (w, h) => GPoly(w, h, (0.05f, 0.3f), (1, 0.3f), (0.95f, 0.7f), (0, 0.7f)), (w, h) => WPoly(w, h, (0.05, 0.3), (1, 0.3), (0.95, 0.7), (0, 0.7)));
            AddItem(cat, "bar_barcode_lines", "Thanh phân cách mã vạch Barcode", (w, h) => GPoly(w, h, (0, 0.2f), (1, 0.2f), (1, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.2), (1, 0.2), (1, 0.8), (0, 0.8)));
            AddItem(cat, "bar_wavelet_ribbon", "Dải ruy băng sóng lượn thanh nhã", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "bar_triple_diamond", "Cụm 3 hình thoi vương giả", (w, h) => GPoly(w, h, (0.5f, 0.1f), (0.65f, 0.5f), (0.5f, 0.9f), (0.35f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0.1), (0.65, 0.5), (0.5, 0.9), (0.35, 0.5)));
            AddItem(cat, "bar_laurel_leaf_strip", "Dải cành lá vinh quang ngăn cách", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "bar_scroll_flourish", "Thanh cuộn thư tịch quý phái", (w, h) => GPoly(w, h, (0.05f, 0.35f), (0.95f, 0.35f), (0.9f, 0.65f), (0.1f, 0.65f)), (w, h) => WPoly(w, h, (0.05, 0.35), (0.95, 0.35), (0.9f, 0.65), (0.1, 0.65)));
            AddItem(cat, "bar_crown_accent", "Thanh đỉnh nhấn vương miện hoàng gia", (w, h) => GPoly(w, h, (0, 0.6f), (0.35f, 0.6f), (0.4f, 0.2f), (0.5f, 0.45f), (0.6f, 0.2f), (0.65f, 0.6f), (1, 0.6f), (1, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.6), (0.35, 0.6), (0.4, 0.2), (0.5, 0.45), (0.6, 0.2), (0.65, 0.6), (1, 0.6), (1, 0.8), (0, 0.8)));
            AddItem(cat, "bar_rope_braid", "Thanh bện thừng hoa văn hàng hải", (w, h) => GRoundedRect(w, h * 0.4f, 4f), (w, h) => WRoundedRect(w, h * 0.4, 4.0));
            AddItem(cat, "bar_music_staff", "Khuông nhạc 5 dòng kẻ thanh tao", (w, h) => { GraphicsPath gp = new GraphicsPath(); for (int i = 0; i < 5; i++) gp.AddLine(0, h * (0.2f + i * 0.15f), w, h * (0.2f + i * 0.15f)); return gp; }, (w, h) => WLines(w, h, (0, 0.5f), (1, 0.5f)));
        }
        #endregion
'''
