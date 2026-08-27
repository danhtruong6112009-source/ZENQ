using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ShortcutsHelpForm : Form
    {
        public ShortcutsHelpForm()
        {
            this.Text = "Bảng Tổng Hợp Phím Tắt Chuẩn Microsoft Word - GenOffice Suite";
            this.Size = new Size(820, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            // Header Banner
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(30, 64, 175) // Royal Blue
            };
            this.Controls.Add(headerPanel);

            Label lblTitle = new Label
            {
                Text = "⌨ BẢNG TRA CỨU PHÍM TẮT GENOFFICE / MICROSOFT WORD",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(18, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = "Tương thích 100% phím tắt chuẩn Microsoft Word 2016-365",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(224, 231, 255),
                Location = new Point(20, 38),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblSub);

            // Tab Control for 7 Categories
            TabControl tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            this.Controls.Add(tabControl);
            tabControl.BringToFront();

            AddCategoryTab(tabControl, "1. Tệp & Hệ Thống", new string[,] {
                { "Ctrl + N", "Tạo tài liệu mới (New Document)" },
                { "Ctrl + O", "Mở tài liệu có sẵn (Open Document)" },
                { "Ctrl + S", "Lưu tài liệu hiện tại (Save)" },
                { "F12", "Lưu tài liệu với tên/đường dẫn khác (Save As)" },
                { "Ctrl + P", "Mở cửa sổ in ấn (Print)" },
                { "Ctrl + W", "Đóng tài liệu đang mở (Close)" },
                { "Ctrl + Z", "Hoàn tác thao tác trước đó (Undo)" },
                { "Ctrl + Y / F4", "Khôi phục thao tác vừa Undo (Redo / Repeat)" },
                { "Ctrl + F", "Mở thanh tìm kiếm & điều hướng (Find / Nav Pane)" },
                { "Ctrl + H", "Mở hộp thoại Tìm kiếm & Thay thế (Find & Replace)" },
                { "Ctrl + K", "Chèn siêu liên kết (Insert Hyperlink)" }
            });

            AddCategoryTab(tabControl, "2. Phông Chữ & Ký Tự", new string[,] {
                { "Ctrl + B", "Bật/Tắt in đậm (Bold)" },
                { "Ctrl + I", "Bật/Tắt in nghiêng (Italic)" },
                { "Ctrl + U", "Bật/Tắt gạch chân (Underline)" },
                { "Ctrl + Shift + D", "Gạch chân nét đôi (Double Underline)" },
                { "Ctrl + Shift + W", "Chỉ gạch chân dưới các từ (bỏ qua khoảng trắng)" },
                { "Ctrl + D / Ctrl + Shift + F", "Mở hộp thoại tùy chỉnh phông chữ (Font Dialog)" },
                { "Ctrl + ] / Ctrl + [", "Tăng / Giảm cỡ chữ theo từng point (1pt)" },
                { "Ctrl + Shift + > / <", "Tăng / Giảm cỡ chữ theo nấc định sẵn" },
                { "Ctrl + =", "Bật/Tắt chỉ số dưới (Subscript, ví dụ: H2O)" },
                { "Ctrl + Shift + +", "Bật/Tắt chỉ số trên (Superscript, ví dụ: X2)" },
                { "Shift + F3", "Đổi kiểu chữ: HOA -> thường -> Viết Hoa Chữ Đầu" },
                { "Ctrl + Shift + A", "Đổi tất cả thành chữ in hoa (All Caps)" },
                { "Ctrl + Space", "Xóa mọi định dạng ký tự, trả về font mặc định" },
                { "Ctrl + Shift + C", "Sao chép định dạng (Copy Format / Format Painter)" },
                { "Ctrl + Shift + V", "Dán định dạng (Paste Format)" }
            });

            AddCategoryTab(tabControl, "3. Đoạn Văn Bản", new string[,] {
                { "Ctrl + L", "Căn lề trái (Align Left)" },
                { "Ctrl + E", "Căn lề giữa (Align Center)" },
                { "Ctrl + R", "Căn lề phải (Align Right)" },
                { "Ctrl + J", "Căn đều hai bên (Justify)" },
                { "Ctrl + 1", "Giãn dòng đơn 1.0 (Single Spacing)" },
                { "Ctrl + 2", "Giãn dòng đôi 2.0 (Double Spacing)" },
                { "Ctrl + 5", "Giãn dòng 1.5 lines" },
                { "Ctrl + 0", "Thêm hoặc bỏ khoảng cách 1 dòng (12pt) trước đoạn văn" },
                { "Ctrl + M", "Thụt lề đoạn văn vào trong (Indent)" },
                { "Ctrl + Shift + M", "Giảm thụt lề đoạn văn ra ngoài (Outdent)" },
                { "Ctrl + T", "Tạo thụt lề treo (Hanging Indent)" },
                { "Ctrl + Shift + T", "Giảm thụt lề treo" },
                { "Ctrl + Q", "Xóa định dạng đoạn văn, đưa về mặc định của Style" }
            });

            AddCategoryTab(tabControl, "4. Điều Hướng Con Trỏ", new string[,] {
                { "Ctrl + ◄ / ►", "Di chuyển con trỏ qua từng từ (trái/phải)" },
                { "Ctrl + ▲ / ▼", "Di chuyển con trỏ lên đầu đoạn trước / sau" },
                { "Home / End", "Nhảy về đầu dòng / cuối dòng" },
                { "Ctrl + Home / End", "Nhảy về đầu trang đầu tiên / cuối trang cuối cùng" },
                { "Page Up / Page Down", "Cuộn trang hiển thị lên / xuống một màn hình" },
                { "Ctrl + G / F5", "Mở thanh tìm kiếm & nhảy tới trang (Go To)" }
            });

            AddCategoryTab(tabControl, "5. Bôi Đen Chọn Văn Bản", new string[,] {
                { "Ctrl + A", "Chọn toàn bộ tài liệu (Select All)" },
                { "Shift + ◄ / ►", "Bôi đen từng ký tự" },
                { "Ctrl + Shift + ◄ / ►", "Bôi đen từng từ nguyên vẹn" },
                { "Shift + ▲ / ▼", "Bôi đen theo từng dòng" },
                { "Shift + Home / End", "Bôi đen từ vị trí con trỏ đến đầu / cuối dòng" },
                { "Ctrl + Shift + Home / End", "Bôi đen từ vị trí con trỏ về đầu / cuối tài liệu" },
                { "F8", "Chế độ mở rộng vùng chọn (2x: từ, 3x: câu, 4x: đoạn, 5x: toàn bài)" }
            });

            AddCategoryTab(tabControl, "6. Chèn & Ngắt Trang", new string[,] {
                { "Ctrl + Enter", "Chèn ngắt trang cứng (Page Break)" },
                { "Shift + Enter", "Chèn ngắt dòng mềm (xuống dòng không ngắt đoạn)" },
                { "Ctrl + Shift + Enter", "Chèn ngắt cột (Column Break)" },
                { "Alt + Shift + D", "Chèn ngày hiện tại (Insert Date: dd/MM/yyyy)" },
                { "Alt + Shift + T", "Chèn giờ hiện tại (Insert Time: HH:mm:ss)" },
                { "Ctrl + Alt + M", "Chèn bình luận mới (New Comment)" },
                { "Alt + =", "Mở trình soạn thảo công thức toán học (Equation)" },
                { "Tab (ở ô cuối bảng)", "Tự động thêm một hàng mới vào đáy bảng" }
            });

            AddCategoryTab(tabControl, "7. Phím Chức Năng", new string[,] {
                { "F1", "Mở bảng trợ giúp & phím tắt (Help)" },
                { "F4", "Lặp lại hành động vừa thực hiện gần nhất (Repeat/Redo)" },
                { "F5", "Mở thanh điều hướng tìm kiếm (Go To / Find)" },
                { "F8", "Kích hoạt chế độ mở rộng vùng chọn văn bản" },
                { "F12", "Lưu tài liệu với tên/đường dẫn khác (Save As)" },
                { "Escape", "Hủy chọn / Thoát chế độ chọn mở rộng F8" }
            });

            // Bottom Panel
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(bottomPanel);

            Button btnClose = new Button
            {
                Text = "Đóng (Close)",
                Size = new Size(110, 30),
                Location = new Point(this.ClientSize.Width - 130, 8),
                BackColor = Color.FromArgb(30, 64, 175),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            bottomPanel.Controls.Add(btnClose);

            this.CancelButton = btnClose;
        }

        private void AddCategoryTab(TabControl tabCtrl, string title, string[,] items)
        {
            TabPage page = new TabPage(title)
            {
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            ListView lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = new Font("Segoe UI", 9.5f)
            };

            lv.Columns.Add("Phím Tắt", 220);
            lv.Columns.Add("Chức Năng Chi Tiết", 520);

            for (int i = 0; i < items.GetLength(0); i++)
            {
                ListViewItem lvi = new ListViewItem(items[i, 0]);
                lvi.SubItems.Add(items[i, 1]);
                lvi.UseItemStyleForSubItems = false;
                lvi.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lvi.ForeColor = Color.FromArgb(30, 64, 175);
                lvi.SubItems[1].Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                lvi.SubItems[1].ForeColor = Color.FromArgb(30, 41, 59);

                if (i % 2 == 1)
                {
                    lvi.BackColor = Color.FromArgb(248, 250, 252);
                    lvi.SubItems[1].BackColor = Color.FromArgb(248, 250, 252);
                }

                lv.Items.Add(lvi);
            }

            page.Controls.Add(lv);
            tabCtrl.TabPages.Add(page);
        }
    }
}
