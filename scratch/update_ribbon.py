import os

with open('src/ui/RibbonBar.cs', 'r', encoding='utf-8') as f:
    content = f.read()

replacements = [
    ('"Cảnh báo chưa lưu - GenOffice"', '"Cảnh báo chưa lưu - ZENQ OFFICE"'),
    ('string appName = "GenOffice " + CurrentMode.ToString();\n            if (CurrentMode == OfficeAppMode.Writer) appName = "GenOffice Writer";\n            else if (CurrentMode == OfficeAppMode.Calc) appName = "GenOffice Calc";\n            else if (CurrentMode == OfficeAppMode.Slides) appName = "GenOffice Slides";\n            else if (CurrentMode == OfficeAppMode.Pdf) appName = "GenOffice PDF";',
     'string appName = "ZenQ " + CurrentMode.ToString();\n            if (CurrentMode == OfficeAppMode.Writer) appName = "ZenQ Writer";\n            else if (CurrentMode == OfficeAppMode.Calc) appName = "ZenQ Calc";\n            else if (CurrentMode == OfficeAppMode.Slides) appName = "ZenQ Slides";\n            else if (CurrentMode == OfficeAppMode.Pdf) appName = "ZenQ PDF";'),
    ('ofd.Title = "Mở tài liệu GenOffice Writer (Chuẩn Microsoft Word OpenXML .docx)";',
     'ofd.Title = "Mở tài liệu ZenQ Writer (Chuẩn Microsoft Word OpenXML .docx)";'),
    ('MessageBox.Show("Đã mở tài liệu \'" + fileName + "\' thành công!", "GenOffice Writer",',
     'MessageBox.Show("Đã mở tài liệu \'" + fileName + "\' thành công!", "ZenQ Writer",'),
    ('sfd.Title = "Lưu tài liệu GenOffice Writer (Chuẩn Microsoft Word OpenXML .docx)";',
     'sfd.Title = "Lưu tài liệu ZenQ Writer (Chuẩn Microsoft Word OpenXML .docx)";'),
    ('MessageBox.Show("Đã lưu tài liệu \'" + fileName + "\' thành công (Chuẩn OpenXML DOCX tương thích 100% với MS Word)!", "GenOffice Writer",',
     'MessageBox.Show("Đã lưu tài liệu \'" + fileName + "\' thành công (Chuẩn OpenXML DOCX tương thích 100% với MS Word)!", "ZenQ Writer",'),
    ('"Đã thay thế tất cả " + c + " vị trí trùng khớp trong tài liệu!", "GenOffice Writer"',
     '"Đã thay thế tất cả " + c + " vị trí trùng khớp trong tài liệu!", "ZenQ Writer"'),
    ('[Tác giả: Người dùng GenOffice]', '[Tác giả: Người dùng ZENQ OFFICE]'),
    ('[Công ty: GenOffice Enterprise]', '[Công ty: ZENQ OFFICE Enterprise]'),
    ('"Đã lưu các thiết lập làm mặc định cho tất cả tài liệu mới!", "GenOffice Writer"',
     '"Đã lưu các thiết lập làm mặc định cho tất cả tài liệu mới!", "ZenQ Writer"'),
    ('"GenOffice Writer - Đặt làm mặc định"', '"ZenQ Writer - Đặt làm mặc định"'),
    ('"Đã lưu thành công bộ thiết lập làm mặc định cho các tài liệu mới!", "GenOffice Writer"',
     '"Đã lưu thành công bộ thiết lập làm mặc định cho các tài liệu mới!", "ZenQ Writer"'),
    ('"Chế độ chia tách cửa sổ (Split View) đã được kích hoạt!", "GenOffice Writer"',
     '"Chế độ chia tách cửa sổ (Split View) đã được kích hoạt!", "ZenQ Writer"'),
    ('"GenOffice Biểu đồ"', '"ZENQ OFFICE Biểu đồ"'),
    ('"GenOffice Định dạng Hình ảnh"', '"ZENQ OFFICE Định dạng Hình ảnh"'),
    ('", "GenOffice", MessageBoxButtons.OK', '", "ZENQ OFFICE", MessageBoxButtons.OK'),
    ('"GenOffice Ink Equation"', '"ZENQ Ink Equation"'),
    ('"Tài liệu GenOffice - Tác giả: Người dùng"', '"Tài liệu ZENQ OFFICE - Tác giả: Người dùng"'),
    ('"GenOffice SmartArt"', '"ZENQ OFFICE SmartArt"'),
    ('ofd.Title = "Mở Bảng tính GenOffice Calc (Chuẩn Microsoft Excel .xlsx / .csv)";',
     'ofd.Title = "Mở Bảng tính ZenQ Calc (Chuẩn Microsoft Excel .xlsx / .csv)";'),
    ('MessageBox.Show("Đã mở bảng tính thành công!", "GenOffice Calc"',
     'MessageBox.Show("Đã mở bảng tính thành công!", "ZenQ Calc"'),
    ('sfd.Title = "Lưu Bảng tính GenOffice Calc (Chuẩn Microsoft Excel .xlsx / .csv)";',
     'sfd.Title = "Lưu Bảng tính ZenQ Calc (Chuẩn Microsoft Excel .xlsx / .csv)";'),
    ('MessageBox.Show("Đã lưu bảng tính thành công!", "GenOffice Calc"',
     'MessageBox.Show("Đã lưu bảng tính thành công!", "ZenQ Calc"'),
    ('"Đã quét và làm sạch các dòng dữ liệu trùng lặp trong bảng " + tbl.Name + "!", "GenOffice Calc"',
     '"Đã quét và làm sạch các dòng dữ liệu trùng lặp trong bảng " + tbl.Name + "!", "ZenQ Calc"'),
    ('"Bạn có muốn chuyển đổi bảng này thành vùng dữ liệu thông thường không?", "GenOffice Calc"',
     '"Bạn có muốn chuyển đổi bảng này thành vùng dữ liệu thông thường không?", "ZenQ Calc"'),
    ('"Xuất dữ liệu bảng ra tệp CSV / Excel thành công!", "GenOffice Calc"',
     '"Xuất dữ liệu bảng ra tệp CSV / Excel thành công!", "ZenQ Calc"'),
    ('"Đã mở tệp PowerPoint thành công!", "GenOffice PowerPoint"',
     '"Đã mở tệp PowerPoint thành công!", "ZenQ Slides"'),
    ('"Đã lưu bản trình chiếu chuẩn .pptx thành công!", "GenOffice PowerPoint"',
     '"Đã lưu bản trình chiếu chuẩn .pptx thành công!", "ZenQ Slides"'),
    ('"Đã áp dụng hiệu ứng chuyển cảnh, thời lượng và hướng cho toàn bộ trang chiếu!", "GenOffice Slides"',
     '"Đã áp dụng hiệu ứng chuyển cảnh, thời lượng và hướng cho toàn bộ trang chiếu!", "ZenQ Slides"'),
    ('MessageBox.Show("Đã mở tệp PDF \'" + ofd.FileName + "\' thành công!", "GenOffice PDF Editor"',
     'MessageBox.Show("Đã mở tệp PDF \'" + ofd.FileName + "\' thành công!", "ZenQ PDF Editor"'),
    ('MessageBox.Show("Đã lưu tệp PDF \'" + sfd.FileName + "\' thành công (Chuẩn PDF 1.7 tương thích 100% với Adobe Acrobat & Chrome)!", "GenOffice PDF Editor"',
     'MessageBox.Show("Đã lưu tệp PDF \'" + sfd.FileName + "\' thành công (Chuẩn PDF 1.7 tương thích 100% với Adobe Acrobat & Chrome)!", "ZenQ PDF Editor"'),
    ('"Bảo mật GenOffice PDF"', '"Bảo mật ZenQ PDF"')
]

count = 0
for old, new in replacements:
    if old in content:
        content = content.replace(old, new)
        count += 1
    else:
        print(f'NOT FOUND: {old[:50]}...')

with open('src/ui/RibbonBar.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print(f'Successfully replaced {count}/{len(replacements)} in RibbonBar.cs')
