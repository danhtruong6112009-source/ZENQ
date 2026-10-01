# -*- coding: utf-8 -*-
"""Category 7: Callouts & Speech Bubbles (35 presets)"""

def get_code():
    return '''
        #region Category 7: Bóng thoại & Chú thích (35 mẫu)
        private static void PopulateCallouts()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Callouts", Title = "Bóng thoại & Chú thích" };
            _categories.Add(cat);

            // 1-6: Bóng thoại chữ nhật các hướng đuôi
            AddItem(cat, "callout_rect_bl", "Bóng thoại chữ nhật đuôi góc trái", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.75f), (0.35f, 0.75f), (0.1f, 1), (0.2f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.75), (0.35, 0.75), (0.1, 1), (0.2, 0.75), (0, 0.75)));
            AddItem(cat, "callout_rect_br", "Bóng thoại chữ nhật đuôi góc phải", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.75f), (0.9f, 1), (0.75f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.75), (0.9, 1), (0.75, 0.75), (0, 0.75)));
            AddItem(cat, "callout_rect_bc", "Bóng thoại chữ nhật đuôi chính giữa", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.75f), (0.6f, 0.75f), (0.5f, 1), (0.4f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.75), (0.6, 0.75), (0.5, 1), (0.4, 0.75), (0, 0.75)));
            AddItem(cat, "callout_rect_left", "Bóng thoại chữ nhật đuôi sang trái", (w, h) => GPoly(w, h, (0.2f, 0), (1, 0), (1, 1), (0.2f, 1), (0.2f, 0.65f), (0, 0.5f), (0.2f, 0.35f)), (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (1, 1), (0.2, 1), (0.2, 0.65), (0, 0.5), (0.2, 0.35)));
            AddItem(cat, "callout_rect_right", "Bóng thoại chữ nhật đuôi sang phải", (w, h) => GPoly(w, h, (0, 0), (0.8f, 0), (0.8f, 0.35f), (1, 0.5f), (0.8f, 0.65f), (0.8f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.8, 0), (0.8, 0.35), (1, 0.5), (0.8, 0.65), (0.8, 1), (0, 1)));
            AddItem(cat, "callout_rect_top", "Bóng thoại chữ nhật đuôi trỏ lên trên", (w, h) => GPoly(w, h, (0, 0.25f), (0.4f, 0.25f), (0.5f, 0), (0.6f, 0.25f), (1, 0.25f), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0.25), (0.4, 0.25), (0.5, 0), (0.6, 0.25), (1, 0.25), (1, 1), (0, 1)));

            // 7-11: Bóng thoại bo tròn các hướng đuôi
            AddItem(cat, "callout_round_bl", "Bóng thoại bo góc tròn đuôi trái", (w, h) => GPoly(w, h, (0.1f, 0), (0.9f, 0), (1, 0.15f), (1, 0.6f), (0.9f, 0.75f), (0.35f, 0.75f), (0.1f, 1), (0.2f, 0.75f), (0.1f, 0.75f), (0, 0.6f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.9, 0), (1, 0.15), (1, 0.6), (0.9, 0.75), (0.35, 0.75), (0.1, 1), (0.2, 0.75), (0.1, 0.75), (0, 0.6), (0, 0.15)));
            AddItem(cat, "callout_round_br", "Bóng thoại bo góc tròn đuôi phải", (w, h) => GPoly(w, h, (0.1f, 0), (0.9f, 0), (1, 0.15f), (1, 0.6f), (0.9f, 0.75f), (0.9f, 1), (0.75f, 0.75f), (0.1f, 0.75f), (0, 0.6f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.9, 0), (1, 0.15), (1, 0.6), (0.9, 0.75), (0.9, 1), (0.75, 0.75), (0.1, 0.75), (0, 0.6), (0, 0.15)));
            AddItem(cat, "callout_round_bc", "Bóng thoại bo góc tròn đuôi giữa", (w, h) => GPoly(w, h, (0.1f, 0), (0.9f, 0), (1, 0.15f), (1, 0.6f), (0.9f, 0.75f), (0.6f, 0.75f), (0.5f, 1), (0.4f, 0.75f), (0.1f, 0.75f), (0, 0.6f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.9, 0), (1, 0.15), (1, 0.6), (0.9, 0.75), (0.6, 0.75), (0.5, 1), (0.4, 0.75), (0.1, 0.75), (0, 0.6), (0, 0.15)));
            AddItem(cat, "callout_round_left", "Bóng thoại bo góc đuôi sang trái", (w, h) => GPoly(w, h, (0.25f, 0), (0.9f, 0), (1, 0.15f), (1, 0.85f), (0.9f, 1), (0.25f, 1), (0.2f, 0.85f), (0.2f, 0.65f), (0, 0.5f), (0.2f, 0.35f), (0.2f, 0.15f)), (w, h) => WPoly(w, h, (0.25, 0), (0.9, 0), (1, 0.15), (1, 0.85), (0.9, 1), (0.25, 1), (0.2, 0.85), (0.2, 0.65), (0, 0.5), (0.2, 0.35), (0.2, 0.15)));
            AddItem(cat, "callout_round_right", "Bóng thoại bo góc đuôi sang phải", (w, h) => GPoly(w, h, (0.1f, 0), (0.75f, 0), (0.8f, 0.15f), (0.8f, 0.35f), (1, 0.5f), (0.8f, 0.65f), (0.8f, 0.85f), (0.75f, 1), (0.1f, 1), (0, 0.85f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.75, 0), (0.8, 0.15), (0.8, 0.35), (1, 0.5), (0.8, 0.65), (0.8, 0.85), (0.75, 1), (0.1, 1), (0, 0.85), (0, 0.15)));

            // 12-14: Bóng thoại hình bầu dục
            AddItem(cat, "callout_oval_bl", "Bóng thoại bầu dục đuôi trái", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.15f), (1, 0.4f), (0.9f, 0.65f), (0.5f, 0.75f), (0.25f, 0.75f), (0.1f, 1), (0.2f, 0.7f), (0.1f, 0.65f), (0, 0.4f), (0.1f, 0.15f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.15), (1, 0.4), (0.9, 0.65), (0.5, 0.75), (0.25, 0.75), (0.1, 1), (0.2, 0.7), (0.1, 0.65), (0, 0.4), (0.1, 0.15)));
            AddItem(cat, "callout_oval_br", "Bóng thoại bầu dục đuôi phải", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.15f), (1, 0.4f), (0.9f, 0.65f), (0.8f, 0.7f), (0.9f, 1), (0.75f, 0.75f), (0.5f, 0.75f), (0.1f, 0.65f), (0, 0.4f), (0.1f, 0.15f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.15), (1, 0.4), (0.9, 0.65), (0.8, 0.7), (0.9, 1), (0.75, 0.75), (0.5, 0.75), (0.1, 0.65), (0, 0.4), (0.1, 0.15)));
            AddItem(cat, "callout_oval_bc", "Bóng thoại bầu dục đuôi giữa", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.15f), (1, 0.4f), (0.9f, 0.65f), (0.58f, 0.75f), (0.5f, 1), (0.42f, 0.75f), (0.1f, 0.65f), (0, 0.4f), (0.1f, 0.15f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.15), (1, 0.4), (0.9, 0.65), (0.58, 0.75), (0.5, 1), (0.42, 0.75), (0.1, 0.65), (0, 0.4), (0.1, 0.15)));

            // 15-18: Bóng thoại đám mây & suy nghĩ
            AddItem(cat, "callout_thought_cloud", "Bóng thoại đám mây suy nghĩ 7 múi", (w, h) => GStar(w, h, 8, 0.75), (w, h) => WStar(w, h, 8, 0.75));
            AddItem(cat, "callout_cloud_dream", "Bóng thoại giấc mơ bồng bềnh", (w, h) => GStar(w, h, 10, 0.78), (w, h) => WStar(w, h, 10, 0.78));
            AddItem(cat, "callout_storm_cloud", "Bóng thoại sấm chớp tức giận", (w, h) => GPoly(w, h, (0.2f, 0.1f), (0.8f, 0.1f), (0.95f, 0.35f), (0.85f, 0.65f), (0.55f, 0.65f), (0.45f, 1), (0.48f, 0.75f), (0.15f, 0.65f), (0.05f, 0.35f)), (w, h) => WPoly(w, h, (0.2, 0.1), (0.8, 0.1), (0.95, 0.35), (0.85, 0.65), (0.55, 0.65), (0.45, 1), (0.48, 0.75), (0.15, 0.65), (0.05, 0.35)));
            AddItem(cat, "callout_bubbles_cluster", "Cụm bóng thoại suy tưởng liên hoàn", (w, h) => GStar(w, h, 6, 0.8), (w, h) => WStar(w, h, 6, 0.8));

            // 19-22: Hét lớn & nổ tung
            AddItem(cat, "callout_scream_burst", "Bóng thoại la hét 12 cánh nhọn", (w, h) => GStar(w, h, 12, 0.6), (w, h) => WStar(w, h, 12, 0.6));
            AddItem(cat, "callout_explosion_shout", "Bóng thoại tiếng thét nổ 16 tia", (w, h) => GStar(w, h, 16, 0.68), (w, h) => WStar(w, h, 16, 0.68));
            AddItem(cat, "callout_zigzag_burst", "Bóng thoại gấp khúc cảm thán", (w, h) => GStar(w, h, 10, 0.55), (w, h) => WStar(w, h, 10, 0.55));
            AddItem(cat, "callout_jagged_alert", "Bóng thoại tia gai báo động", (w, h) => GStar(w, h, 14, 0.72), (w, h) => WStar(w, h, 14, 0.72));

            // 23-26: Phong cách tin nhắn hiện đại
            AddItem(cat, "callout_imessage_left", "Bóng thoại tin nhắn iOS đuôi trái", (w, h) => GPoly(w, h, (0.15f, 0), (0.9f, 0), (1, 0.15f), (1, 0.85f), (0.9f, 1), (0.2f, 1), (0, 1), (0.1f, 0.85f), (0.05f, 0.15f)), (w, h) => WPoly(w, h, (0.15, 0), (0.9, 0), (1, 0.15), (1, 0.85), (0.9, 1), (0.2, 1), (0, 1), (0.1, 0.85), (0.05, 0.15)));
            AddItem(cat, "callout_imessage_right", "Bóng thoại tin nhắn iOS đuôi phải", (w, h) => GPoly(w, h, (0.1f, 0), (0.85f, 0), (0.95f, 0.15f), (0.9f, 0.85f), (1, 1), (0.8f, 1), (0.1f, 1), (0, 0.85f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.85, 0), (0.95, 0.15), (0.9, 0.85), (1, 1), (0.8, 1), (0.1, 1), (0, 0.85), (0, 0.15)));
            AddItem(cat, "callout_telegram_bubble", "Bóng thoại tròn trịa hiện đại", (w, h) => GRoundedRect(w, h * 0.85f, 10f), (w, h) => WRoundedRect(w, h * 0.85, 10.0));
            AddItem(cat, "callout_dual_dialogue", "Hội thoại đối đáp song phương", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.7f, 0.1f), (0.7f, 0.5f), (0.5f, 0.5f), (0.4f, 0.7f), (0.45f, 0.5f), (0.1f, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.7, 0.1), (0.7, 0.5), (0.5, 0.5), (0.4, 0.7), (0.45, 0.5), (0.1, 0.5)));

            // 27-30: Chú thích đường dẫn (Line Callouts)
            AddItem(cat, "callout_line_1seg", "Chú thích đường dẫn 1 đoạn nghiêng", (w, h) => GLines(w, h, (0, 0.85f), (0.8f, 0.85f), (1, 0.15f)), (w, h) => WLines(w, h, (0, 0.85), (0.8, 0.85), (1, 0.15)));
            AddItem(cat, "callout_line_2seg", "Chú thích đường khuỷu 2 đoạn vuông", (w, h) => GLines(w, h, (0, 0.85f), (0.6f, 0.85f), (0.6f, 0.15f), (1, 0.15f)), (w, h) => WLines(w, h, (0, 0.85), (0.6, 0.85), (0.6, 0.15), (1, 0.15)));
            AddItem(cat, "callout_line_3seg_dot", "Chú thích 3 đoạn kèm chấm neo", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLines(new PointF[] { new PointF(0, h * 0.85f), new PointF(w * 0.5f, h * 0.85f), new PointF(w * 0.75f, h * 0.35f), new PointF(w, h * 0.35f) }); gp.AddEllipse(w * 0.9f, h * 0.25f, w * 0.1f, h * 0.2f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.85), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h * 0.85), true, false); ctx.LineTo(new WpfPoint(w * 0.75, h * 0.35), true, false); ctx.LineTo(new WpfPoint(w, h * 0.35), true, false); ctx.BeginFigure(new WpfPoint(w * 0.9, h * 0.25), true, true); ctx.ArcTo(new WpfPoint(w * 0.9, h * 0.45), new WpfSize(w * 0.05, h * 0.1), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.9, h * 0.25), new WpfSize(w * 0.05, h * 0.1), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "callout_line_accent_bar", "Chú thích đường dẫn có thanh nhấn dọc", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, 0, 0, h); gp.AddLine(0, h * 0.5f, w * 0.7f, h * 0.5f); gp.AddLine(w * 0.7f, h * 0.5f, w, h * 0.15f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), false, false); ctx.LineTo(new WpfPoint(0, h), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.7, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w, h * 0.15), true, false); } g.Freeze(); return g; });

            // 31-35: Bóng thoại chú thích đặc biệt
            AddItem(cat, "callout_quote_box", "Hộp trích dẫn danh ngôn (Quote Box)", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.8f), (0.4f, 0.8f), (0.2f, 1), (0.25f, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.8), (0.4, 0.8), (0.2, 1), (0.25, 0.8), (0, 0.8)));
            AddItem(cat, "callout_whisper_box", "Bóng thoại thì thầm nét đứt", (w, h) => GRoundedRect(w, h * 0.85f, 6f), (w, h) => WRoundedRect(w, h * 0.85, 6.0));
            AddItem(cat, "callout_tag_note", "Thẻ gắn ghi chú đục lỗ nhãn", (w, h) => GPoly(w, h, (0.2f, 0), (1, 0), (1, 1), (0.2f, 1), (0, 0.5f)), (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (1, 1), (0.2, 1), (0, 0.5)));
            AddItem(cat, "callout_sticky_curled", "Giấy dán ghi chú quăn mép góc", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.75f), (0.75f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.75), (0.75, 1), (0, 1)));
            AddItem(cat, "callout_ai_sparkle", "Bóng thoại hội thoại trợ lý AI", (w, h) => GPoly(w, h, (0.1f, 0), (0.85f, 0), (1, 0.2f), (1, 0.7f), (0.85f, 0.85f), (0.35f, 0.85f), (0.1f, 1), (0.2f, 0.85f), (0.05f, 0.7f), (0.05f, 0.2f)), (w, h) => WPoly(w, h, (0.1, 0), (0.85, 0), (1, 0.2), (1, 0.7), (0.85, 0.85), (0.35, 0.85), (0.1, 1), (0.2, 0.85), (0.05, 0.7), (0.05, 0.2)));
        }
        #endregion
'''
