using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;

namespace CaptureHelpDialog
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Form dlg = new Form();
                dlg.Text = "Giới thiệu & Hướng dẫn - GENOFFICE";
                dlg.Size = new Size(620, 520);
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.Location = new Point(-2000, -2000);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;
                dlg.Font = new Font("Segoe UI", 9.5f);

                Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24) };
                dlg.Controls.Add(pnl);

                Label lblTitle = new Label
                {
                    Text = "GENOFFICE v3.5",
                    Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    AutoSize = true,
                    Location = new Point(20, 20),
                    UseMnemonic = false
                };
                pnl.Controls.Add(lblTitle);

                Label lblSub = new Label
                {
                    Text = "Phát triển bởi NEXTGENEDUSPACE STUDIO • Tác giả: Trương Nguyễn Thế Danh © 2026",
                    Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(37, 99, 235),
                    AutoSize = true,
                    Location = new Point(20, 52),
                    UseMnemonic = false
                };
                pnl.Controls.Add(lblSub);

                Label lblDesc = new Label
                {
                    Text = "Bộ ứng dụng văn phòng đa năng hiện đại tích hợp đầy đủ:\n" +
                           "• Writer: Soạn thảo văn bản chuyên nghiệp (.docx)\n" +
                           "• Calc: Bảng tính số liệu thông minh (.xlsx, .csv)\n" +
                           "• Slides: Trình chiếu & Thuyết trình chuyên nghiệp (.pptx)\n" +
                           "• PDF: Đọc, chú thích, ký & chỉnh sửa tài liệu PDF\n\n" +
                           "Phím tắt nhanh:\n" +
                           "• Ctrl + N: Tạo tài liệu mới          • Ctrl + O: Mở tài liệu có sẵn\n" +
                           "• Ctrl + S: Lưu tài liệu hiện tại        • Ctrl + F: Tìm kiếm & Thay thế\n" +
                           "• F5: Bắt đầu trình chiếu Slide",
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Color.FromArgb(51, 65, 85),
                    Location = new Point(20, 82),
                    Size = new Size(560, 185),
                    UseMnemonic = false
                };
                pnl.Controls.Add(lblDesc);

                Panel pnlNote = new Panel
                {
                    Location = new Point(20, 275),
                    Size = new Size(560, 125),
                    BackColor = Color.FromArgb(248, 250, 252),
                    Padding = new Padding(12)
                };
                pnlNote.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    {
                        pe.Graphics.DrawRectangle(bPen, 0, 0, pnlNote.Width - 1, pnlNote.Height - 1);
                    }
                    using (SolidBrush barB = new SolidBrush(Color.FromArgb(245, 158, 11)))
                    {
                        pe.Graphics.FillRectangle(barB, 0, 0, 4, pnlNote.Height);
                    }
                };

                Label lblNoteTitle = new Label
                {
                    Text = "• Lưu ý về tính tương thích định dạng:",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(180, 83, 9),
                    AutoSize = true,
                    Location = new Point(14, 10),
                    UseMnemonic = false
                };
                pnlNote.Controls.Add(lblNoteTitle);

                Label lblNoteContent = new Label
                {
                    Text = "GENOFFICE luôn nỗ lực tối đa để hiển thị và xử lý tài liệu chuẩn xác nhất. Tuy nhiên, do sự khác biệt về đặc tả kỹ thuật và tiêu chuẩn phần mềm giữa các nền tảng, chúng tôi không thể cam kết rằng mọi định dạng nâng cao sẽ hiển thị giống hệt 100% như trên các bộ ứng dụng Office khác.",
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Location = new Point(14, 34),
                    Size = new Size(530, 80),
                    UseMnemonic = false
                };
                pnlNote.Controls.Add(lblNoteContent);
                pnl.Controls.Add(pnlNote);

                Button btnClose = new Button
                {
                    Text = "Đóng",
                    Size = new Size(95, 34),
                    Location = new Point(485, 418),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                };
                pnl.Controls.Add(btnClose);

                dlg.Show();

                using (Bitmap bmp = new Bitmap(dlg.Width, dlg.Height))
                {
                    dlg.DrawToBitmap(bmp, new Rectangle(0, 0, dlg.Width, dlg.Height));
                    string outPath = @"C:\Users\Danh\Desktop\genofficepy\scratch\help_dialog_preview2.png";
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("SAVED: " + outPath);
                }

                dlg.Close();
                dlg.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }
    }
}
