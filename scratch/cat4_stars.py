# -*- coding: utf-8 -*-
"""Category 4: Stars & Badges (50 presets)"""

def get_code():
    return '''
        #region Category 4: Hình ngôi sao & Huy hiệu (50 mẫu)
        private static void PopulateStarsAndBadges()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Stars", Title = "Hình ngôi sao & Huy hiệu" };
            _categories.Add(cat);

            // 1-13: Ngôi sao chuẩn từ 4 đến 32 cánh
            int[] starPts = new int[] { 4, 5, 6, 7, 8, 9, 10, 12, 14, 16, 20, 24, 32 };
            for (int i = 0; i < starPts.Length; i++)
            {
                int pts = starPts[i];
                AddItem(cat, $"star_{pts}pt_std", $"Ngôi sao {pts} cánh chuẩn ({pts}-Point Star)", (w, h) => GStar(w, h, pts, 0.45f), (w, h) => WStar(w, h, pts, 0.45));
            }

            // 14-21: Ngôi sao cánh thon nhọn
            int[] sharpPts = new int[] { 4, 5, 6, 7, 8, 10, 12, 16 };
            for (int i = 0; i < sharpPts.Length; i++)
            {
                int pts = sharpPts[i];
                AddItem(cat, $"star_{pts}pt_sharp", $"Ngôi sao {pts} cánh nhọn ({pts}-Point Sharp)", (w, h) => GStar(w, h, pts, 0.25f), (w, h) => WStar(w, h, pts, 0.25));
            }

            // 22-29: Ngôi sao cánh mập đậm nét
            int[] fatPts = new int[] { 4, 5, 6, 7, 8, 10, 12, 16 };
            for (int i = 0; i < fatPts.Length; i++)
            {
                int pts = fatPts[i];
                AddItem(cat, $"star_{pts}pt_fat", $"Ngôi sao {pts} cánh mập ({pts}-Point Fat)", (w, h) => GStar(w, h, pts, 0.65f), (w, h) => WStar(w, h, pts, 0.65));
            }

            // 30-36: Huy hiệu sao nổ (Burst Explosions)
            int[] burstPts = new int[] { 8, 12, 14, 16, 20, 24, 32 };
            for (int i = 0; i < burstPts.Length; i++)
            {
                int pts = burstPts[i];
                AddItem(cat, $"star_burst_{pts}", $"Huy hiệu nổ sao {pts} tia (Burst {pts})", (w, h) => GStar(w, h, pts, 0.78f), (w, h) => WStar(w, h, pts, 0.78));
            }

            // 37-50: Các loại khiên giáp, huy hiệu & tem chứng nhận
            AddItem(cat, "shield_heater", "Khiên hiệp sĩ Heater Shield", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.6f), (0.5f, 1), (0, 0.6f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.6), (0.5, 1), (0, 0.6)));
            AddItem(cat, "shield_french", "Khiên vòm hoàng gia Pháp", (w, h) => GPoly(w, h, (0.05f, 0.05f), (0.95f, 0.05f), (0.95f, 0.65f), (0.5f, 0.98f), (0.05f, 0.65f)), (w, h) => WPoly(w, h, (0.05, 0.05), (0.95, 0.05), (0.95, 0.65), (0.5, 0.98), (0.05, 0.65)));
            AddItem(cat, "shield_gothic", "Khiên nhọn phong cách Gothic", (w, h) => GPoly(w, h, (0.15f, 0), (0.85f, 0), (1, 0.45f), (0.5f, 1), (0, 0.45f)), (w, h) => WPoly(w, h, (0.15, 0), (0.85, 0), (1, 0.45), (0.5, 1), (0, 0.45)));
            AddItem(cat, "shield_roman_crest", "Huy hiệu khiên La Mã cổ đại", (w, h) => GPoly(w, h, (0, 0.1f), (0.5f, 0), (1, 0.1f), (1, 0.75f), (0.5f, 1), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.1), (0.5, 0), (1, 0.1), (1, 0.75), (0.5, 1), (0, 0.75)));
            AddItem(cat, "shield_rounded", "Khiên bo góc tròn bảo vệ", (w, h) => GPoly(w, h, (0.1f, 0), (0.9f, 0), (1, 0.5f), (0.85f, 0.85f), (0.5f, 1), (0.15f, 0.85f), (0, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0), (0.9, 0), (1, 0.5), (0.85, 0.85), (0.5, 1), (0.15, 0.85), (0, 0.5)));
            AddItem(cat, "scalloped_seal_12", "Tem chứng nhận răng cưa 12 múi", (w, h) => GStar(w, h, 12, 0.82), (w, h) => WStar(w, h, 12, 0.82));
            AddItem(cat, "scalloped_seal_16", "Tem chứng nhận răng cưa 16 múi", (w, h) => GStar(w, h, 16, 0.84), (w, h) => WStar(w, h, 16, 0.84));
            AddItem(cat, "scalloped_seal_24", "Tem chất lượng vàng 24 răng cưa", (w, h) => GStar(w, h, 24, 0.88), (w, h) => WStar(w, h, 24, 0.88));
            AddItem(cat, "badge_rosette", "Huy chương hoa cúc Rosette", (w, h) => GPoly(w, h, (0.25f, 0), (0.75f, 0), (1, 0.25f), (1, 0.75f), (0.75f, 1), (0.25f, 1), (0, 0.75f), (0, 0.25f)), (w, h) => WPoly(w, h, (0.25, 0), (0.75, 0), (1, 0.25), (1, 0.75), (0.75, 1), (0.25, 1), (0, 0.75), (0, 0.25)));
            AddItem(cat, "badge_star_medallion", "Huy hiệu ngôi sao danh dự", (w, h) => GStar(w, h, 8, 0.55), (w, h) => WStar(w, h, 8, 0.55));
            AddItem(cat, "crown_crest_3peak", "Vương miện hoàng gia 3 đỉnh", (w, h) => GPoly(w, h, (0.05f, 0.25f), (0.25f, 0.55f), (0.5f, 0.15f), (0.75f, 0.55f), (0.95f, 0.25f), (0.85f, 0.9f), (0.15f, 0.9f)), (w, h) => WPoly(w, h, (0.05, 0.25), (0.25, 0.55), (0.5, 0.15), (0.75, 0.55), (0.95, 0.25), (0.85, 0.9), (0.15, 0.9)));
            AddItem(cat, "crown_crest_5peak", "Vương miện hoàng đế 5 đỉnh", (w, h) => GPoly(w, h, (0, 0.25f), (0.2f, 0.5f), (0.35f, 0.15f), (0.5f, 0.45f), (0.65f, 0.15f), (0.8f, 0.5f), (1, 0.25f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0, 0.25), (0.2, 0.5), (0.35, 0.15), (0.5, 0.45), (0.65, 0.15), (0.8, 0.5), (1, 0.25), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "badge_ribbon_bottom", "Huy hiệu đính đuôi nơ ruy băng", (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 0.35f), (0.8f, 0.7f), (0.9f, 1), (0.5f, 0.85f), (0.1f, 1), (0.2f, 0.7f), (0, 0.35f)), (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 0.35), (0.8, 0.7), (0.9, 1), (0.5, 0.85), (0.1, 1), (0.2, 0.7), (0, 0.35)));
            AddItem(cat, "badge_knight_cross", "Huy hiệu khiên chữ thập thập tự", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.9f, 0.55f), (0.5f, 0.95f), (0.1f, 0.55f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.9, 0.55), (0.5, 0.95), (0.1, 0.55)));
        }
        #endregion
'''
