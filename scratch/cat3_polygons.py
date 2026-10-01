# -*- coding: utf-8 -*-
"""Category 3: Polygons & Geometry (45 presets)"""

def get_code():
    return '''
        #region Category 3: Đa giác đều & Hình học (45 mẫu)
        private static void PopulatePolygonsAndGeometry()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Polygons", Title = "Đa giác đều & Hình học" };
            _categories.Add(cat);

            // 1-12: Các đa giác đều từ 3 đến 16 cạnh
            int[] regularSides = new int[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16 };
            string[] polyNames = new string[] {
                "Tam giác đều 3 cạnh (Triangle)", "Tứ giác vuông 4 cạnh (Square)", "Ngũ giác đều 5 cạnh (Pentagon)",
                "Lục giác đều 6 cạnh (Hexagon)", "Thất giác đều 7 cạnh (Heptagon)", "Bát giác đều 8 cạnh (Octagon)",
                "Cửu giác đều 9 cạnh (Nonagon)", "Thập giác đều 10 cạnh (Decagon)", "Thập nhất giác 11 cạnh (Hendecagon)",
                "Thập nhị giác 12 cạnh (Dodecagon)", "Mười bốn giác 14 cạnh", "Mười sáu giác 16 cạnh"
            };
            for (int i = 0; i < regularSides.Length; i++)
            {
                int sides = regularSides[i];
                double offset = -Math.PI / 2.0;
                if (sides % 2 == 0) offset = Math.PI / sides;
                AddItem(cat, $"reg_polygon_{sides}", polyNames[i], (w, h) => GRegularPoly(w, h, sides, (float)offset), (w, h) => WRegularPoly(w, h, sides, offset));
            }

            // 13-19: Các biến thể tam giác hình học
            AddItem(cat, "tri_right_30_60", "Tam giác vuông tỷ lệ 30°-60°", (w, h) => GPoly(w, h, (0, 0), (0.6f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.6, 1), (0, 1)));
            AddItem(cat, "tri_right_45", "Tam giác vuông cân 45°-45°", (w, h) => GPoly(w, h, (0, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 1), (0, 1)));
            AddItem(cat, "tri_equilateral", "Tam giác đều cạnh chuẩn", (w, h) => GRegularPoly(w, h, 3, -(float)Math.PI / 2f), (w, h) => WRegularPoly(w, h, 3, -Math.PI / 2.0));
            AddItem(cat, "tri_equilateral_inv", "Tam giác đều lộn ngược đỉnh", (w, h) => GRegularPoly(w, h, 3, (float)Math.PI / 2f), (w, h) => WRegularPoly(w, h, 3, Math.PI / 2.0));
            AddItem(cat, "tri_acute_sharp", "Tam giác nhọn mũi mác thon", (w, h) => GPoly(w, h, (0.5f, 0), (0.75f, 1), (0.25f, 1)), (w, h) => WPoly(w, h, (0.5, 0), (0.75, 1), (0.25, 1)));
            AddItem(cat, "tri_obtuse_wide", "Tam giác tù góc bẹt thoải", (w, h) => GPoly(w, h, (0.15f, 0.4f), (1, 0.7f), (0, 0.9f)), (w, h) => WPoly(w, h, (0.15, 0.4), (1, 0.7), (0, 0.9)));
            AddItem(cat, "tri_golden_ratio", "Tam giác tỷ lệ vàng Fibonacci", (w, h) => GPoly(w, h, (0.5f, 0), (0.81f, 1), (0.19f, 1)), (w, h) => WPoly(w, h, (0.5, 0), (0.81, 1), (0.19, 1)));

            // 20-28: Các biến thể tứ giác chuyên biệt
            AddItem(cat, "quad_rhombus_60", "Hình thoi góc nhọn 60 độ", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.9f, 0.5f), (0.5f, 0.95f), (0.1f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.9, 0.5), (0.5, 0.95), (0.1, 0.5)));
            AddItem(cat, "quad_rhombus_45", "Hình thoi góc nhọn 45 độ", (w, h) => GPoly(w, h, (0.5f, 0), (0.8f, 0.5f), (0.5f, 1), (0.2f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0), (0.8, 0.5), (0.5, 1), (0.2, 0.5)));
            AddItem(cat, "quad_parallelogram_30", "Hình bình hành góc nghiêng 30°", (w, h) => GPoly(w, h, (0.3f, 0), (1, 0), (0.7f, 1), (0, 1)), (w, h) => WPoly(w, h, (0.3, 0), (1, 0), (0.7, 1), (0, 1)));
            AddItem(cat, "quad_parallelogram_45", "Hình bình hành góc nghiêng 45°", (w, h) => GPoly(w, h, (0.45f, 0), (1, 0), (0.55f, 1), (0, 1)), (w, h) => WPoly(w, h, (0.45, 0), (1, 0), (0.55, 1), (0, 1)));
            AddItem(cat, "quad_trapezoid_isosceles", "Hình thang cân góc đáy 60°", (w, h) => GPoly(w, h, (0.25f, 0), (0.75f, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0.25, 0), (0.75, 0), (1, 1), (0, 1)));
            AddItem(cat, "quad_trapezoid_right", "Hình thang vuông góc cạnh trái", (w, h) => GPoly(w, h, (0, 0), (0.75f, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.75, 0), (1, 1), (0, 1)));
            AddItem(cat, "quad_trapezoid_inverted", "Hình thang vuông góc cạnh phải", (w, h) => GPoly(w, h, (0.25f, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0.25, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "quad_kite_slim", "Hình cánh diều dáng thon cao", (w, h) => GPoly(w, h, (0.5f, 0), (0.85f, 0.25f), (0.5f, 1), (0.15f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.85, 0.25), (0.5, 1), (0.15, 0.25)));
            AddItem(cat, "quad_kite_wide", "Hình cánh diều dáng rộng ngang", (w, h) => GPoly(w, h, (0.5f, 0.1f), (1, 0.45f), (0.5f, 0.9f), (0, 0.45f)), (w, h) => WPoly(w, h, (0.5, 0.1), (1, 0.45), (0.5, 0.9), (0, 0.45)));

            // 29-37: Các hình phân chia cung góc (Circular Sectors)
            int[] sectorAngles = new int[] { 30, 45, 60, 90, 120, 135, 150, 180, 270 };
            for (int i = 0; i < sectorAngles.Length; i++)
            {
                int deg = sectorAngles[i];
                double rad = deg * Math.PI / 180.0;
                double ex = 0.5 + 0.5 * Math.Cos(rad - Math.PI / 2.0);
                double ey = 0.5 + 0.5 * Math.Sin(rad - Math.PI / 2.0);
                AddItem(cat, $"geom_sector_{deg}", $"Hình phân khúc góc {deg}°", (w, h) => GPoly(w, h, (0.5f, 0.5f), (0.5f, 0), ((float)ex, (float)ey)), (w, h) => WPoly(w, h, (0.5, 0.5), (0.5, 0), (ex, ey)));
            }

            // 38-45: Hình học cong & đặc biệt
            AddItem(cat, "geom_vesica_piscis", "Hình giao điểm Vesica Piscis", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.85f, 0.5f), (0.5f, 0.95f), (0.15f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.85, 0.5), (0.5, 0.95), (0.15, 0.5)));
            AddItem(cat, "geom_reuleaux_tri", "Tam giác cong Reuleaux", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.95f, 0.85f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.95, 0.85), (0.05, 0.85)));
            AddItem(cat, "geom_stadium", "Hình sân vận động Stadium", (w, h) => GRoundedRect(w, h, Math.Min(w, h) / 2f), (w, h) => WRoundedRect(w, h, Math.Min(w, h) / 2.0));
            AddItem(cat, "geom_deltoid", "Hình tam giác Deltoid lõm", (w, h) => GPoly(w, h, (0.5f, 0), (0.85f, 0.4f), (1, 0.8f), (0.6f, 0.7f), (0.5f, 1), (0.4f, 0.7f), (0, 0.8f), (0.15f, 0.4f)), (w, h) => WPoly(w, h, (0.5, 0), (0.85, 0.4), (1, 0.8), (0.6, 0.7), (0.5, 1), (0.4, 0.7), (0, 0.8), (0.15, 0.4)));
            AddItem(cat, "geom_rounded_triangle", "Tam giác bo tròn 3 góc", (w, h) => GPoly(w, h, (0.45f, 0.05f), (0.55f, 0.05f), (0.95f, 0.85f), (0.85f, 0.95f), (0.15f, 0.95f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.45, 0.05), (0.55, 0.05), (0.95, 0.85), (0.85, 0.95), (0.15, 0.95), (0.05, 0.85)));
            AddItem(cat, "geom_rounded_pentagon", "Ngũ giác bo tròn các góc", (w, h) => GRegularPoly(w, h, 5, -Math.PI / 2.0), (w, h) => WRegularPoly(w, h, 5, -Math.PI / 2.0));
            AddItem(cat, "geom_rounded_hexagon", "Lục giác bo góc trang nhã", (w, h) => GRegularPoly(w, h, 6, 0), (w, h) => WRegularPoly(w, h, 6, 0));
            AddItem(cat, "geom_rounded_octagon", "Bát giác bo viền mềm", (w, h) => GRegularPoly(w, h, 8, Math.PI / 8.0), (w, h) => WRegularPoly(w, h, 8, Math.PI / 8.0));
        }
        #endregion
'''
