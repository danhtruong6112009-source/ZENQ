# -*- coding: utf-8 -*-
"""Category 9: Isometric & 3D Shapes (35 presets)"""

def get_code():
    return '''
        #region Category 9: Hình khối 3D & Isometric (35 mẫu)
        private static void PopulateIsometric3DShapes()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Isometric3D", Title = "Hình khối 3D & Isometric" };
            _categories.Add(cat);

            // 1-9: Lăng trụ & khối đa diện
            AddItem(cat, "iso3d_cube", "Khối lập phương Isometric (Cube)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.25f), new PointF(w * 0.5f, h * 0.5f), new PointF(0, h * 0.25f) }); gp.AddPolygon(new PointF[] { new PointF(0, h * 0.25f), new PointF(w * 0.5f, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.75f) }); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, h * 0.5f), new PointF(w, h * 0.25f), new PointF(w, h * 0.75f), new PointF(w * 0.5f, h) }); return gp; },
                (w, h) => WPoly(w, h, (0.5, 0), (1, 0.25), (1, 0.75), (0.5, 1), (0, 0.75), (0, 0.25)));

            AddItem(cat, "iso3d_box_wide", "Khối hộp chữ nhật 3D rộng (Wide Box)",
                (w, h) => GPoly(w, h, (0.5f, 0.1f), (1, 0.3f), (1, 0.7f), (0.5f, 0.9f), (0, 0.7f), (0, 0.3f)),
                (w, h) => WPoly(w, h, (0.5, 0.1), (1, 0.3), (1, 0.7), (0.5, 0.9), (0, 0.7), (0, 0.3)));

            AddItem(cat, "iso3d_column_tall", "Khối cột trụ chữ nhật 3D đứng cao",
                (w, h) => GPoly(w, h, (0.5f, 0), (0.85f, 0.15f), (0.85f, 0.85f), (0.5f, 1), (0.15f, 0.85f), (0.15f, 0.15f)),
                (w, h) => WPoly(w, h, (0.5, 0), (0.85, 0.15), (0.85, 0.85), (0.5, 1), (0.15, 0.85), (0.15, 0.15)));

            AddItem(cat, "iso3d_prism_hex", "Lăng trụ lục giác 3D (Hexagonal Prism)",
                (w, h) => GPoly(w, h, (0.25f, 0), (0.75f, 0), (1, 0.2f), (1, 0.8f), (0.75f, 1), (0.25f, 1), (0, 0.8f), (0, 0.2f)),
                (w, h) => WPoly(w, h, (0.25, 0), (0.75, 0), (1, 0.2), (1, 0.8), (0.75, 1), (0.25, 1), (0, 0.8), (0, 0.2)));

            AddItem(cat, "iso3d_prism_tri", "Lăng trụ tam giác 3D (Triangular Prism)",
                (w, h) => GPoly(w, h, (0.5f, 0.05f), (1, 0.35f), (0.8f, 0.95f), (0.2f, 0.95f), (0, 0.35f)),
                (w, h) => WPoly(w, h, (0.5, 0.05), (1, 0.35), (0.8, 0.95), (0.2, 0.95), (0, 0.35)));

            AddItem(cat, "iso3d_dodecahedron", "Khối đa diện 12 mặt 3D",
                (w, h) => GPoly(w, h, (0.35f, 0.05f), (0.65f, 0.05f), (0.95f, 0.3f), (0.85f, 0.75f), (0.5f, 0.95f), (0.15f, 0.75f), (0.05f, 0.3f)),
                (w, h) => WPoly(w, h, (0.35, 0.05), (0.65, 0.05), (0.95, 0.3), (0.85, 0.75), (0.5, 0.95), (0.15, 0.75), (0.05, 0.3)));

            AddItem(cat, "iso3d_icosahedron", "Khối đa diện 20 mặt 3D",
                (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.25f), (0.9f, 0.75f), (0.5f, 1), (0.1f, 0.75f), (0.1f, 0.25f)),
                (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.25), (0.9, 0.75), (0.5, 1), (0.1, 0.75), (0.1, 0.25)));

            AddItem(cat, "iso3d_diamond_gem", "Khối kim cương giác cắt 3D (Gem)",
                (w, h) => GPoly(w, h, (0.25f, 0.15f), (0.75f, 0.15f), (1, 0.4f), (0.5f, 0.95f), (0, 0.4f)),
                (w, h) => WPoly(w, h, (0.25, 0.15), (0.75, 0.15), (1, 0.4), (0.5, 0.95), (0, 0.4)));

            AddItem(cat, "iso3d_octahedron", "Khối bát diện 8 mặt 3D",
                (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.5f), (0.5f, 1), (0.05f, 0.5f)),
                (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.5), (0.5, 1), (0.05, 0.5)));

            // 10-15: Hình trụ tròn & nón
            AddItem(cat, "iso3d_cylinder_vert", "Hình trụ tròn đứng 3D (Cylinder)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h * 0.3f); gp.AddLine(0, h * 0.15f, 0, h * 0.85f); gp.AddArc(0, h * 0.7f, w, h * 0.3f, 180, -180); gp.AddLine(w, h * 0.85f, w, h * 0.15f); return gp; },
                (w, h) => WPoly(w, h, (0, 0.15), (0.5, 0), (1, 0.15), (1, 0.85), (0.5, 1), (0, 0.85)));

            AddItem(cat, "iso3d_cylinder_horiz", "Hình trụ tròn nằm ngang 3D",
                (w, h) => GPoly(w, h, (0.15f, 0), (0.85f, 0), (1, 0.5f), (0.85f, 1), (0.15f, 1), (0, 0.5f)),
                (w, h) => WPoly(w, h, (0.15, 0), (0.85, 0), (1, 0.5), (0.85, 1), (0.15, 1), (0, 0.5)));

            AddItem(cat, "iso3d_cone_std", "Hình nón nhọn đỉnh 3D (Cone)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.5f, 0, w, h * 0.85f); gp.AddArc(0, h * 0.7f, w, h * 0.3f, 0, 180); gp.AddLine(0, h * 0.85f, w * 0.5f, 0); return gp; },
                (w, h) => WPoly(w, h, (0.5, 0), (1, 0.85), (0.5, 1), (0, 0.85)));

            AddItem(cat, "iso3d_cone_truncated", "Hình nón cụt 3D (Truncated Cone)",
                (w, h) => GPoly(w, h, (0.3f, 0.15f), (0.7f, 0.15f), (0.95f, 0.85f), (0.05f, 0.85f)),
                (w, h) => WPoly(w, h, (0.3, 0.15), (0.7, 0.15), (0.95, 0.85), (0.05, 0.85)));

            AddItem(cat, "iso3d_pipe_hollow", "Ống trụ rỗng ruột 3D (Hollow Pipe)",
                (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.9f, 0.15f), (0.9f, 0.85f), (0.1f, 0.85f)),
                (w, h) => WPoly(w, h, (0.1, 0.15), (0.9, 0.15), (0.9, 0.85), (0.1, 0.85)));

            AddItem(cat, "iso3d_capsule_tank", "Bình chứa áp suất 3D Capsule",
                (w, h) => GRoundedRect(w, h * 0.6f, 15f),
                (w, h) => WRoundedRect(w, h * 0.6, 15.0));

            // 16-20: Kim tự tháp & hình cầu
            AddItem(cat, "iso3d_pyramid_std", "Kim tự tháp 4 mặt 3D (Pyramid)",
                (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.75f), (0.5f, 1), (0.05f, 0.75f)),
                (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.75), (0.5, 1), (0.05, 0.75)));

            AddItem(cat, "iso3d_pyramid_truncated", "Kim tự tháp cụt 3D",
                (w, h) => GPoly(w, h, (0.35f, 0.2f), (0.65f, 0.2f), (0.95f, 0.85f), (0.05f, 0.85f)),
                (w, h) => WPoly(w, h, (0.35, 0.2), (0.65, 0.2), (0.95, 0.85), (0.05, 0.85)));

            AddItem(cat, "iso3d_sphere_globe", "Hình cầu 3D có vĩ tuyến (Sphere)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddEllipse(0, h * 0.35f, w, h * 0.3f); gp.AddEllipse(w * 0.35f, 0, w * 0.3f, h); return gp; },
                (w, h) => WEllipse(w, h));

            AddItem(cat, "iso3d_torus_ring", "Hình xuyến vòng đệm 3D (Torus)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, h * 0.15f, w, h * 0.7f); gp.AddEllipse(w * 0.25f, h * 0.3f, w * 0.5f, h * 0.4f); return gp; },
                (w, h) => WPoly(w, h, (0.5, 0.15), (1, 0.5), (0.5, 0.85), (0, 0.5)));

            AddItem(cat, "iso3d_dome_half", "Mái vòm bán cầu 3D (Dome)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(0, 0, w, h * 1.5f, 180, 180); gp.AddLine(w, h * 0.75f, 0, h * 0.75f); gp.CloseFigure(); return gp; },
                (w, h) => WPoly(w, h, (0, 0.75), (0.1, 0.4), (0.5, 0.1), (0.9, 0.4), (1, 0.75)));

            // 21-35: Cấu trúc kiến trúc & cơ khí 3D
            AddItem(cat, "iso3d_stairs_3step", "Bậc thang 3D 3 bậc vươn cao", (w, h) => GPoly(w, h, (0, 0.8f), (0.33f, 0.8f), (0.33f, 0.5f), (0.66f, 0.5f), (0.66f, 0.2f), (1, 0.2f), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0.8), (0.33, 0.8), (0.33, 0.5), (0.66, 0.5), (0.66, 0.2), (1, 0.2), (1, 1), (0, 1)));
            AddItem(cat, "iso3d_l_block", "Khối góc vuông chữ L 3D", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.4f, 0.1f), (0.4f, 0.6f), (0.9f, 0.6f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.4, 0.1), (0.4, 0.6), (0.9, 0.6), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "iso3d_t_block", "Khối chữ T đối xứng 3D", (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.9f, 0.15f), (0.9f, 0.45f), (0.65f, 0.45f), (0.65f, 0.9f), (0.35f, 0.9f), (0.35f, 0.45f), (0.1f, 0.45f)), (w, h) => WPoly(w, h, (0.1, 0.15), (0.9, 0.15), (0.9, 0.45), (0.65, 0.45), (0.65, 0.9), (0.35, 0.9), (0.35, 0.45), (0.1, 0.45)));
            AddItem(cat, "iso3d_u_block", "Khối rãnh chữ U lõm 3D", (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.35f, 0.15f), (0.35f, 0.65f), (0.65f, 0.65f), (0.65f, 0.15f), (0.9f, 0.15f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.15), (0.35, 0.15), (0.35, 0.65), (0.65, 0.65), (0.65, 0.15), (0.9, 0.15), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "iso3d_db_stack", "Khối đĩa dữ liệu Database 3 tầng", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "iso3d_podium_3tier", "Bục vinh danh trao giải 3 cấp", (w, h) => GPoly(w, h, (0.05f, 0.5f), (0.35f, 0.5f), (0.35f, 0.2f), (0.65f, 0.2f), (0.65f, 0.65f), (0.95f, 0.65f), (0.95f, 1), (0.05f, 1)), (w, h) => WPoly(w, h, (0.05, 0.5), (0.35, 0.5), (0.35, 0.2), (0.65, 0.2), (0.65, 0.65), (0.95, 0.65), (0.95, 1), (0.05, 1)));
            AddItem(cat, "iso3d_roman_column", "Cột La Mã cổ điển 3D", (w, h) => GPoly(w, h, (0.15f, 0.05f), (0.85f, 0.05f), (0.75f, 0.2f), (0.75f, 0.8f), (0.85f, 0.95f), (0.15f, 0.95f), (0.25f, 0.8f), (0.25f, 0.2f)), (w, h) => WPoly(w, h, (0.15, 0.05), (0.85, 0.05), (0.75, 0.2), (0.75, 0.8), (0.85, 0.95), (0.15, 0.95), (0.25, 0.8), (0.25, 0.2)));
            AddItem(cat, "iso3d_gear_3d", "Bánh xe răng cưa truyền động 3D", (w, h) => GStar(w, h, 8, 0.72), (w, h) => WStar(w, h, 8, 0.72));
            AddItem(cat, "iso3d_rubik_cube", "Khối rubik phân đoạn 3D", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.95f, 0.3f), (0.95f, 0.75f), (0.5f, 0.95f), (0.05f, 0.75f), (0.05f, 0.3f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.95, 0.3), (0.95, 0.75), (0.5, 0.95), (0.05, 0.75), (0.05, 0.3)));
            AddItem(cat, "iso3d_optical_prism", "Lăng kính quang học hội tụ 3D", (w, h) => GPoly(w, h, (0.5f, 0.1f), (0.95f, 0.85f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.5, 0.1), (0.95, 0.85), (0.05, 0.85)));
            AddItem(cat, "iso3d_screw_thread", "Trục vít truyền động xoắn ốc 3D", (w, h) => GPoly(w, h, (0.35f, 0.05f), (0.65f, 0.05f), (0.65f, 0.85f), (0.5f, 0.98f), (0.35f, 0.85f)), (w, h) => WPoly(w, h, (0.35, 0.05), (0.65, 0.05), (0.65, 0.85), (0.5, 0.98), (0.35, 0.85)));
            AddItem(cat, "iso3d_box_open_lid", "Khối hộp mở nắp trên 3D", (w, h) => GPoly(w, h, (0.1f, 0.25f), (0.9f, 0.25f), (0.8f, 0.9f), (0.2f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.25), (0.9, 0.25), (0.8, 0.9), (0.2, 0.9)));
            AddItem(cat, "iso3d_island_tile", "Mô hình đảo nổi Isometric Tile", (w, h) => GPoly(w, h, (0.5f, 0.15f), (0.95f, 0.45f), (0.5f, 0.95f), (0.05f, 0.45f)), (w, h) => WPoly(w, h, (0.5, 0.15), (0.95, 0.45), (0.5, 0.95), (0.05, 0.45)));
            AddItem(cat, "iso3d_bridge_span", "Nhịp cầu vượt kết nối 3D", (w, h) => GPoly(w, h, (0.05f, 0.35f), (0.95f, 0.35f), (0.95f, 0.75f), (0.8f, 0.75f), (0.8f, 0.55f), (0.2f, 0.55f), (0.2f, 0.75f), (0.05f, 0.75f)), (w, h) => WPoly(w, h, (0.05, 0.35), (0.95, 0.35), (0.95, 0.75), (0.8, 0.75), (0.8, 0.55), (0.2, 0.55), (0.2, 0.75), (0.05, 0.75)));
            AddItem(cat, "iso3d_wedge_ramp", "Mặt phẳng nghiêng dốc Ramp 3D", (w, h) => GPoly(w, h, (0.1f, 0.85f), (0.9f, 0.15f), (0.9f, 0.85f)), (w, h) => WPoly(w, h, (0.1, 0.85), (0.9, 0.15), (0.9, 0.85)));
        }
        #endregion
'''
