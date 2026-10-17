# ZENQ OFFICE - OFFICIAL WEBSITE

Trang web chính thức giới thiệu bộ ứng dụng văn phòng thế hệ mới **ZENQ OFFICE 4.2** (Writer, Calc, Slides, PDF, Remote).

## Tính Năng Nổi Bật Của Website
- **Song ngữ Anh - Việt (Bilingual VI / EN)**: Mặc định ưu tiên Tiếng Việt, có nút chuyển đổi tức thì. Toàn bộ nội dung được biên soạn từ bộ từ điển thuật ngữ công nghệ chuẩn xác (`dictionary.js`), không dùng máy dịch tự động.
- **Thiết kế chuẩn mực đẳng cấp quốc tế**: Phong cách Dark Modern tối giản, sang trọng (lấy cảm hứng từ Apple, Linear, Stripe). Bo góc vừa phải (10px - 14px), màu sắc hài hòa, không lố bịch.
- **Hoạt ảnh & Tương tác chuyên nghiệp**:
  - Tăng tốc phần cứng CSS & SVG, mượt mà 60-120 FPS.
  - Hiệu ứng phát sáng viền thẻ theo con trỏ chuột (Mouse Glow Tracking).
  - Ticker số liệu động khi cuộn trang.
  - Tab chuyển đổi phân hệ trực quan (Writer, Calc, Slides, PDF, Remote) với mô phỏng giao diện ribbon.
  - Bộ câu hỏi thường gặp (FAQ Accordion) có lọc theo chủ đề (Cài đặt, Tương thích, Bảo mật, Kỹ thuật).
- **Đầy đủ nội dung các trang**:
  - **Trang chủ (HOME)**: Giới thiệu tổng quan, số liệu ấn tượng, mô phỏng giao diện, bảng so sánh tính năng với MS Office & WPS.
  - **Về chúng tôi (ABOUT US)**: Câu chuyện khởi nguồn của NextGenEduSpace Studio, Tầm nhìn, Sứ mệnh và 3 Giá trị cốt lõi.
  - **Hỏi & Đáp (FAQ)**: 8 câu hỏi cốt lõi giải đáp chi tiết mọi thắc mắc của người dùng.
  - **Tải về (DOWNLOAD)**: Liên kết tải bản cài đặt `ZenQOffice-Setup.exe` (v4.2) và thông số kỹ thuật.

## Hướng Dẫn Deploy Lên GitHub Pages (`https://github.com/danhtruong6112009-source/ZENQ.git`)

### Cách 1: Push trực tiếp từ thư mục `website`
```bash
cd website
git init
git branch -M main
git remote add origin https://github.com/danhtruong6112009-source/ZENQ.git
git add .
git commit -m "feat(website): Deploy official bilingual ZenQ Office website"
git push -u origin main --force
```

Sau khi push, vào GitHub repository:
1. Chọn **Settings** -> **Pages**.
2. Tại mục **Build and deployment** / **Source**: Chọn **Deploy from a branch**.
3. Chọn nhánh `main` và thư mục `/ (root)`, nhấn **Save**.
4. Trang web sẽ trực tiếp hoạt động tại: `https://danhtruong6112009-source.github.io/ZENQ/`.
