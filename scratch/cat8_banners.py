# -*- coding: utf-8 -*-
"""Category 8: Banners & Ribbons (35 presets)"""

def get_code():
    return '''
        #region Category 8: Dải ruy băng & Biểu ngữ (35 mẫu)
        private static void PopulateBannersAndRibbons()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Banners", Title = "Dải ruy băng & Biểu ngữ" };
            _categories.Add(cat);

            // 1-5: Ruy băng uốn lượn cong
            AddItem(cat, "banner_arch_up", "Dải ruy băng cong vồng lên (Arch Up)", (w, h) => GPoly(w, h, (0, 0.4f), (0.15f, 0.15f), (0.85f, 0.15f), (1, 0.4f), (0.85f, 0.85f), (0.15f, 0.85f)), (w, h) => WPoly(w, h, (0, 0.4), (0.15, 0.15), (0.85, 0.15), (1, 0.4), (0.85, 0.85), (0.15, 0.85)));
            AddItem(cat, "banner_arch_down", "Dải ruy băng cong võng xuống (Arch Down)", (w, h) => GPoly(w, h, (0.15f, 0.15f), (0.85f, 0.15f), (1, 0.6f), (0.85f, 0.85f), (0.15f, 0.85f), (0, 0.6f)), (w, h) => WPoly(w, h, (0.15, 0.15), (0.85, 0.15), (1, 0.6), (0.85, 0.85), (0.15, 0.85), (0, 0.6)));
            AddItem(cat, "banner_s_wave", "Dải ruy băng lượn sóng chữ S (Wave)", (w, h) => GPoly(w, h, (0, 0.35f), (0.5f, 0.15f), (1, 0.45f), (1, 0.85f), (0.5f, 0.55f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.35), (0.5, 0.15), (1, 0.45), (1, 0.85), (0.5, 0.55), (0, 0.75)));
            AddItem(cat, "banner_double_wave", "Dải ruy băng lượn sóng đôi", (w, h) => GPoly(w, h, (0, 0.3f), (0.25f, 0.1f), (0.75f, 0.4f), (1, 0.2f), (1, 0.7f), (0.75f, 0.9f), (0.25f, 0.6f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.3), (0.25, 0.1), (0.75, 0.4), (1, 0.2), (1, 0.7), (0.75, 0.9), (0.25, 0.6), (0, 0.8)));
            AddItem(cat, "banner_wave_reverse", "Dải ruy băng sóng ngược chiều", (w, h) => GPoly(w, h, (0, 0.45f), (0.5f, 0.75f), (1, 0.35f), (1, 0.65f), (0.5f, 1), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.45), (0.5, 0.75), (1, 0.35), (1, 0.65), (0.5, 1), (0, 0.75)));

            // 6-10: Ruy băng gấp nếp & 3 khúc
            AddItem(cat, "banner_3fold_std", "Ruy băng gấp nếp 3 khúc cổ điển", (w, h) => GPoly(w, h, (0.15f, 0.25f), (0.85f, 0.25f), (0.85f, 0.75f), (0.15f, 0.75f)), (w, h) => WPoly(w, h, (0.15, 0.25), (0.85, 0.25), (0.85, 0.75), (0.15, 0.75)));
            AddItem(cat, "banner_3fold_curved", "Ruy băng 3 khúc uốn cong vòm", (w, h) => GPoly(w, h, (0.2f, 0.15f), (0.8f, 0.15f), (0.85f, 0.65f), (0.15f, 0.65f)), (w, h) => WPoly(w, h, (0.2, 0.15), (0.8, 0.15), (0.85, 0.65), (0.15, 0.65)));
            AddItem(cat, "banner_z_fold", "Ruy băng gấp nếp zic-zac chữ Z", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.7f, 0.5f), (0.9f, 0.9f), (0.1f, 0.9f), (0.3f, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.7, 0.5), (0.9, 0.9), (0.1, 0.9), (0.3, 0.5)));
            AddItem(cat, "banner_corner_45", "Dải ruy băng chéo góc 45 độ", (w, h) => GPoly(w, h, (0, 0.35f), (0.35f, 0), (1, 0.65f), (0.65f, 1)), (w, h) => WPoly(w, h, (0, 0.35), (0.35, 0), (1, 0.65), (0.65, 1)));
            AddItem(cat, "banner_pleated_sash", "Dải băng rôn xếp ly vinh danh", (w, h) => GPoly(w, h, (0.05f, 0.3f), (0.95f, 0.3f), (0.85f, 0.7f), (0.15f, 0.7f)), (w, h) => WPoly(w, h, (0.05, 0.3), (0.95, 0.3), (0.85, 0.7), (0.15, 0.7)));

            // 11-15: Cuộn giấy & thư tịch cổ
            AddItem(cat, "scroll_horizontal", "Cuộn giấy cổ nằm ngang (Horizontal Scroll)", (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.9f, 0.15f), (0.85f, 0.85f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.1, 0.15), (0.9, 0.15), (0.85, 0.85), (0.05, 0.85)));
            AddItem(cat, "scroll_vertical", "Cuộn giấy cổ thẳng đứng (Vertical Scroll)", (w, h) => GPoly(w, h, (0.15f, 0.1f), (0.85f, 0.05f), (0.85f, 0.9f), (0.15f, 0.85f)), (w, h) => WPoly(w, h, (0.15, 0.1), (0.85, 0.05), (0.85, 0.9), (0.15, 0.85)));
            AddItem(cat, "scroll_ancient_papyrus", "Thư tịch sáp phong ấn Papyrus", (w, h) => GPoly(w, h, (0.12f, 0.1f), (0.88f, 0.12f), (0.85f, 0.9f), (0.15f, 0.88f)), (w, h) => WPoly(w, h, (0.12, 0.1), (0.88, 0.12), (0.85, 0.9), (0.15, 0.88)));
            AddItem(cat, "scroll_double_rolled", "Cuộn thư tịch mở rộng hai phía", (w, h) => GPoly(w, h, (0.08f, 0.2f), (0.92f, 0.2f), (0.92f, 0.8f), (0.08f, 0.8f)), (w, h) => WPoly(w, h, (0.08, 0.2), (0.92, 0.2), (0.92, 0.8), (0.08, 0.8)));
            AddItem(cat, "scroll_parchment_open", "Trang sách da cừu mở rộng", (w, h) => GPoly(w, h, (0.05f, 0.15f), (0.5f, 0.25f), (0.95f, 0.15f), (0.95f, 0.85f), (0.5f, 0.95f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.05, 0.15), (0.5, 0.25), (0.95, 0.15), (0.95, 0.85), (0.5, 0.95), (0.05, 0.85)));

            // 16-21: Cờ phướn & đuôi cá
            AddItem(cat, "flag_pennant_right", "Cờ tam giác đuôi nheo sang phải", (w, h) => GPoly(w, h, (0, 0.15f), (1, 0.5f), (0, 0.85f)), (w, h) => WPoly(w, h, (0, 0.15), (1, 0.5), (0, 0.85)));
            AddItem(cat, "flag_pennant_left", "Cờ tam giác đuôi nheo sang trái", (w, h) => GPoly(w, h, (1, 0.15f), (0, 0.5f), (1, 0.85f)), (w, h) => WPoly(w, h, (1, 0.15), (0, 0.5), (1, 0.85)));
            AddItem(cat, "flag_swallowtail_right", "Cờ đuôi cá hai ngạnh sang phải", (w, h) => GPoly(w, h, (0, 0.2f), (1, 0.2f), (0.75f, 0.5f), (1, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.2), (1, 0.2), (0.75, 0.5), (1, 0.8), (0, 0.8)));
            AddItem(cat, "flag_swallowtail_left", "Cờ đuôi cá hai ngạnh sang trái", (w, h) => GPoly(w, h, (0, 0.2f), (0.25f, 0.5f), (0, 0.8f), (1, 0.8f), (1, 0.2f)), (w, h) => WPoly(w, h, (0, 0.2), (0.25, 0.5), (0, 0.8), (1, 0.8), (1, 0.2)));
            AddItem(cat, "flag_double_swallowtail", "Cờ hiệu hoàng gia khuyết hai đầu", (w, h) => GPoly(w, h, (0.15f, 0.2f), (0.85f, 0.2f), (0.7f, 0.5f), (0.85f, 0.8f), (0.15f, 0.8f), (0.3f, 0.5f)), (w, h) => WPoly(w, h, (0.15, 0.2), (0.85, 0.2), (0.7, 0.5), (0.85, 0.8), (0.15, 0.8), (0.3, 0.5)));
            AddItem(cat, "flag_burgee_marine", "Cờ hiệu hàng hải Burgee vát nhọn", (w, h) => GPoly(w, h, (0, 0.1f), (0.9f, 0.35f), (0.7f, 0.5f), (0.9f, 0.65f), (0, 0.9f)), (w, h) => WPoly(w, h, (0, 0.1), (0.9, 0.35), (0.7, 0.5), (0.9, 0.65), (0, 0.9)));

            // 22-26: Huy hiệu & Rosette
            AddItem(cat, "medal_rosette_tails", "Huy chương hoa cúc hai dải lụa", (w, h) => GPoly(w, h, (0.3f, 0), (0.7f, 0), (0.9f, 0.3f), (0.7f, 0.6f), (0.85f, 1), (0.6f, 0.8f), (0.5f, 0.9f), (0.4f, 0.8f), (0.15f, 1), (0.3f, 0.6f), (0.1f, 0.3f)), (w, h) => WPoly(w, h, (0.3, 0), (0.7, 0), (0.9, 0.3), (0.7, 0.6), (0.85, 1), (0.6, 0.8), (0.5, 0.9), (0.4, 0.8), (0.15, 1), (0.3, 0.6), (0.1, 0.3)));
            AddItem(cat, "badge_drapery_shield", "Biểu ngữ dải lụa nẹp khiên", (w, h) => GPoly(w, h, (0.1f, 0.2f), (0.9f, 0.2f), (0.85f, 0.6f), (0.5f, 0.9f), (0.15f, 0.6f)), (w, h) => WPoly(w, h, (0.1, 0.2), (0.9, 0.2), (0.85, 0.6), (0.5, 0.9), (0.15, 0.6)));
            AddItem(cat, "plaque_victorian", "Biển bảng biểu ngữ Victorian", (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.9f, 0.15f), (0.95f, 0.5f), (0.9f, 0.85f), (0.1f, 0.85f), (0.05f, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0.15), (0.9, 0.15), (0.95, 0.5), (0.9, 0.85), (0.1, 0.85), (0.05, 0.5)));
            AddItem(cat, "cartouche_baroque", "Khung tên phù điêu Baroque", (w, h) => GPoly(w, h, (0.2f, 0.1f), (0.8f, 0.1f), (1, 0.3f), (0.9f, 0.7f), (0.8f, 0.9f), (0.2f, 0.9f), (0.1f, 0.7f), (0, 0.3f)), (w, h) => WPoly(w, h, (0.2, 0.1), (0.8, 0.1), (1, 0.3), (0.9, 0.7), (0.8, 0.9), (0.2, 0.9), (0.1, 0.7), (0, 0.3)));
            AddItem(cat, "banner_star_crest", "Dải ruy băng đính huy hiệu sao", (w, h) => GPoly(w, h, (0.05f, 0.3f), (0.95f, 0.3f), (0.9f, 0.7f), (0.05f, 0.7f)), (w, h) => WPoly(w, h, (0.05, 0.3), (0.95, 0.3), (0.9, 0.7), (0.05, 0.7)));

            // 27-35: Nhãn thương mại & sự kiện
            AddItem(cat, "tag_sale_hanging", "Thẻ bài giảm giá Sale treo", (w, h) => GPoly(w, h, (0.25f, 0), (0.75f, 0), (1, 0.4f), (0.85f, 1), (0.15f, 1), (0, 0.4f)), (w, h) => WPoly(w, h, (0.25, 0), (0.75, 0), (1, 0.4), (0.85, 1), (0.15, 1), (0, 0.4)));
            AddItem(cat, "tag_price_slanted", "Thẻ giá vát nhọn đục lỗ", (w, h) => GPoly(w, h, (0, 0.2f), (0.7f, 0.2f), (1, 0.5f), (0.7f, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.2), (0.7, 0.2), (1, 0.5), (0.7, 0.8), (0, 0.8)));
            AddItem(cat, "ribbon_gift_bow", "Dải nơ gói quà tặng nghệ thuật", (w, h) => GPoly(w, h, (0.5f, 0.3f), (0.85f, 0.1f), (0.75f, 0.5f), (0.95f, 0.9f), (0.55f, 0.65f), (0.5f, 0.85f), (0.45f, 0.65f), (0.05f, 0.9f), (0.25f, 0.5f), (0.15f, 0.1f)), (w, h) => WPoly(w, h, (0.5, 0.3), (0.85, 0.1), (0.75, 0.5), (0.95, 0.9), (0.55, 0.65), (0.5, 0.85), (0.45, 0.65), (0.05, 0.9), (0.25, 0.5), (0.15, 0.1)));
            AddItem(cat, "ribbon_bookmark_strip", "Dải ruy băng đánh dấu trang", (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (0.8f, 1), (0.5f, 0.8f), (0.2f, 1)), (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (0.8, 1), (0.5, 0.8), (0.2, 1)));
            AddItem(cat, "seal_gold_quality", "Dải ruy băng chứng nhận chất lượng", (w, h) => GPoly(w, h, (0.15f, 0.2f), (0.85f, 0.2f), (0.9f, 0.8f), (0.5f, 0.65f), (0.1f, 0.8f)), (w, h) => WPoly(w, h, (0.15, 0.2), (0.85, 0.2), (0.9, 0.8), (0.5, 0.65), (0.1, 0.8)));
            AddItem(cat, "banner_grand_opening", "Băng khánh thành khai trương", (w, h) => GPoly(w, h, (0, 0.25f), (1, 0.25f), (0.95f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.25), (1, 0.25), (0.95, 0.75), (0, 0.75)));
            AddItem(cat, "banner_pro_badge", "Dải biểu ngữ phiên bản Pro", (w, h) => GPoly(w, h, (0.1f, 0.25f), (0.9f, 0.25f), (0.85f, 0.75f), (0.15f, 0.75f)), (w, h) => WPoly(w, h, (0.1, 0.25), (0.9, 0.25), (0.85, 0.75), (0.15, 0.75)));
            AddItem(cat, "wreath_laurel_ribbon", "Dải ruy băng vòng nguyệt quế", (w, h) => GPoly(w, h, (0.2f, 0.3f), (0.8f, 0.3f), (0.95f, 0.7f), (0.75f, 0.6f), (0.5f, 0.85f), (0.25f, 0.6f), (0.05f, 0.7f)), (w, h) => WPoly(w, h, (0.2, 0.3), (0.8, 0.3), (0.95, 0.7), (0.75, 0.6), (0.5, 0.85), (0.25, 0.6), (0.05, 0.7)));
            AddItem(cat, "banner_award_sash", "Dải ruy băng trao giải danh giá", (w, h) => GPoly(w, h, (0.05f, 0.2f), (0.95f, 0.2f), (0.85f, 0.8f), (0.15f, 0.8f)), (w, h) => WPoly(w, h, (0.05, 0.2), (0.95, 0.2), (0.85, 0.8), (0.15, 0.8)));
        }
        #endregion
'''
