import os

# Update SetupForm.cs
with open('src/setup/SetupForm.cs', 'r', encoding='utf-8') as f:
    setup_code = f.read()

setup_replacements = [
    ('ĐIỀU KHOẢN VÀ CHÍNH SÁCH SỬ DỤNG GENOFFICE', 'ĐIỀU KHOẢN VÀ CHÍNH SÁCH SỬ DỤNG ZENQ OFFICE'),
    ('GENOFFICE là bộ giải pháp phần mềm văn phòng thông minh', 'ZENQ OFFICE là bộ giải pháp phần mềm văn phòng thông minh'),
    ('Text = "Cài đặt GENOFFICE";', 'Text = "Cài đặt ZENQ OFFICE";'),
    ('Tôi đã đọc và đồng ý với điều khoản sử dụng của GENOFFICE', 'Tôi đã đọc và đồng ý với điều khoản sử dụng của ZENQ OFFICE'),
    ('Chọn thư mục cài đặt GENOFFICE', 'Chọn thư mục cài đặt ZENQ OFFICE'),
    ('Tạo lối tắt GENOFFICE trên màn hình nền Desktop', 'Tạo lối tắt ZENQ OFFICE trên màn hình nền Desktop'),
    ('Thêm lối tắt GENOFFICE vào Start Menu', 'Thêm lối tắt ZENQ OFFICE vào Start Menu'),
    ('Đang giải nén tập tin ứng dụng GENOFFICE...', 'Đang giải nén tập tin ứng dụng ZENQ OFFICE...'),
    ('"GENOFFICE.lnk"), "GENOFFICE"', '"ZENQ OFFICE.lnk"), "ZENQ OFFICE"'),
    ('Cài đặt hoàn tất! Đang khởi động GENOFFICE...', 'Cài đặt hoàn tất! Đang khởi động ZENQ OFFICE...'),
    ('string titleText = "Cài đặt GENOFFICE";', 'string titleText = "Cài đặt ZENQ OFFICE";'),
    ('titleText = "Chào mừng bạn đến với GENOFFICE";', 'titleText = "Chào mừng bạn đến với ZENQ OFFICE";'),
    ('titleText = "Tùy chọn cài đặt GENOFFICE";', 'titleText = "Tùy chọn cài đặt ZENQ OFFICE";'),
    ('titleText = "Đang cài đặt GENOFFICE...";', 'titleText = "Đang cài đặt ZENQ OFFICE...";')
]

for old, new in setup_replacements:
    setup_code = setup_code.replace(old, new)

with open('src/setup/SetupForm.cs', 'w', encoding='utf-8') as f:
    f.write(setup_code)
print('Updated SetupForm.cs')

# Update UninstallForm.cs
with open('src/setup/UninstallForm.cs', 'r', encoding='utf-8') as f:
    uninst_code = f.read()

uninst_replacements = [
    ('Text = "Gỡ cài đặt GENOFFICE";', 'Text = "Gỡ cài đặt ZENQ OFFICE";'),
    ('Text = "Ở lại với GENOFFICE",', 'Text = "Ở lại với ZENQ OFFICE",'),
    ('Đang đóng các tiến trình GENOFFICE...', 'Đang đóng các tiến trình ZENQ OFFICE...'),
    ('Đang gỡ cài đặt GENOFFICE...', 'Đang gỡ cài đặt ZENQ OFFICE...'),
    ('Bạn muốn gỡ cài đặt GENOFFICE?', 'Bạn muốn gỡ cài đặt ZENQ OFFICE?')
]

for old, new in uninst_replacements:
    uninst_code = uninst_code.replace(old, new)

with open('src/setup/UninstallForm.cs', 'w', encoding='utf-8') as f:
    f.write(uninst_code)
print('Updated UninstallForm.cs')

# Update ShortcutHelper.cs
with open('src/setup/ShortcutHelper.cs', 'r', encoding='utf-8') as f:
    sc_code = f.read()

sc_replacements = [
    ('"GENOFFICE.lnk"), "GENOFFICE"', '"ZENQ OFFICE.lnk"), "ZENQ OFFICE"'),
    ('key.SetValue("DisplayName", "GENOFFICE");', 'key.SetValue("DisplayName", "ZENQ OFFICE");')
]

for old, new in sc_replacements:
    sc_code = sc_code.replace(old, new)

with open('src/setup/ShortcutHelper.cs', 'w', encoding='utf-8') as f:
    f.write(sc_code)
print('Updated ShortcutHelper.cs')
