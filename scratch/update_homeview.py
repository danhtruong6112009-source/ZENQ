import os

with open('src/ui/HomeView.cs', 'r', encoding='utf-8') as f:
    content = f.read()

replacements = [
    ('dlg.Text = "Giới thiệu & Hướng dẫn - GENOFFICE";',
     'dlg.Text = "Giới thiệu & Hướng dẫn - ZENQ OFFICE";'),
    ('Text = "GENOFFICE v3.5",',
     'Text = "ZENQ OFFICE 1",'),
    ('Text = "GENOFFICE luôn nỗ lực tối đa để hiển thị và xử lý tài liệu chuẩn xác nhất.',
     'Text = "ZENQ OFFICE luôn nỗ lực tối đa để hiển thị và xử lý tài liệu chuẩn xác nhất.'),
    ('new TemplateLineInfo("Tháng 09/2026 • Ban Quản lý Dự án GenOffice", 1, false, true, 12f),',
     'new TemplateLineInfo("Tháng 09/2026 • Ban Quản lý Dự án ZENQ OFFICE", 1, false, true, 12f),'),
    ('new TemplateLineInfo("• Tên dự án: Triển khai Hệ thống Văn phòng Thông minh GenOffice", 0, false, false, 13f),',
     'new TemplateLineInfo("• Tên dự án: Triển khai Hệ thống Văn phòng Thông minh ZENQ OFFICE", 0, false, false, 13f),'),
    ('g.DrawString("GenOffice", brandFont, brandBrush, 56f, 18f);',
     'g.DrawString("ZENQ OFFICE", brandFont, brandBrush, 56f, 18f);'),
    ('g.DrawString("GenOffice Writer (Soạn thảo văn bản)", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 11f);',
     'g.DrawString("ZenQ Writer (Soạn thảo văn bản)", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 11f);'),
    ('bool hasWriterSc = DesktopShortcutManager.IsShortcutExists("GenOffice Writer");',
     'bool hasWriterSc = DesktopShortcutManager.IsShortcutExists("ZenQ Writer") || DesktopShortcutManager.IsShortcutExists("GenOffice Writer");'),
    ('g.DrawString("GenOffice Calc (Bảng tính)", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 11f);',
     'g.DrawString("ZenQ Calc (Bảng tính)", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 11f);'),
    ('bool hasCalcSc = DesktopShortcutManager.IsShortcutExists("GenOffice Calc");',
     'bool hasCalcSc = DesktopShortcutManager.IsShortcutExists("ZenQ Calc") || DesktopShortcutManager.IsShortcutExists("GenOffice Calc");'),
    ('g.DrawString("GenOffice Slides (Trình chiếu)", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 11f);',
     'g.DrawString("ZenQ Slides (Trình chiếu)", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 11f);'),
    ('bool hasSlidesSc = DesktopShortcutManager.IsShortcutExists("GenOffice Slides");',
     'bool hasSlidesSc = DesktopShortcutManager.IsShortcutExists("ZenQ Slides") || DesktopShortcutManager.IsShortcutExists("GenOffice Slides");'),
    ('g.DrawString("GenOffice PDF (Tài liệu PDF)", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 11f);',
     'g.DrawString("ZenQ PDF (Tài liệu PDF)", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 11f);'),
    ('bool hasPdfSc = DesktopShortcutManager.IsShortcutExists("GenOffice PDF");',
     'bool hasPdfSc = DesktopShortcutManager.IsShortcutExists("ZenQ PDF") || DesktopShortcutManager.IsShortcutExists("GenOffice PDF");'),
    ('g.DrawString("GENOFFICE Professional Suite v1.5.0", rTitleFont, rTitleBrush, card5.X + 16f, card5.Y + 12f);',
     'g.DrawString("ZENQ OFFICE Professional Suite 1", rTitleFont, rTitleBrush, card5.X + 16f, card5.Y + 12f);'),
    ('ofd.Title = "Mở tài liệu văn phòng - GenOffice";',
     'ofd.Title = "Mở tài liệu văn phòng - ZENQ OFFICE";'),
    ('if (SettingShortcutWriterRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("GenOffice Writer", "-writer", "icon\\\\writer.ico", "Soạn thảo văn bản GenOffice Writer"); return null; }',
     'if (SettingShortcutWriterRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ Writer", "-writer", "icon\\\\writer.ico", "Soạn thảo văn bản ZenQ Writer"); DesktopShortcutManager.DeleteShortcut("GenOffice Writer"); return null; }'),
    ('if (SettingShortcutCalcRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("GenOffice Calc", "-calc", "icon\\\\calc.ico", "Bảng tính GenOffice Calc"); return null; }',
     'if (SettingShortcutCalcRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ Calc", "-calc", "icon\\\\calc.ico", "Bảng tính ZenQ Calc"); DesktopShortcutManager.DeleteShortcut("GenOffice Calc"); return null; }'),
    ('if (SettingShortcutSlidesRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("GenOffice Slides", "-slides", "icon\\\\present.ico", "Trình chiếu GenOffice Slides"); return null; }',
     'if (SettingShortcutSlidesRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ Slides", "-slides", "icon\\\\present.ico", "Trình chiếu ZenQ Slides"); DesktopShortcutManager.DeleteShortcut("GenOffice Slides"); return null; }'),
    ('if (SettingShortcutPdfRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("GenOffice PDF", "-pdf", "icon\\\\pdf.ico", "Đọc và chỉnh sửa GenOffice PDF"); return null; }',
     'if (SettingShortcutPdfRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ PDF", "-pdf", "icon\\\\pdf.ico", "Đọc và chỉnh sửa ZenQ PDF"); DesktopShortcutManager.DeleteShortcut("GenOffice PDF"); return null; }')
]

count = 0
for old, new in replacements:
    if old in content:
        content = content.replace(old, new)
        count += 1
    else:
        print(f'NOT FOUND: {old[:50]}...')

with open('src/ui/HomeView.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print(f'Successfully replaced {count}/{len(replacements)} in HomeView.cs')
