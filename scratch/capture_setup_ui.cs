using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Setup;

namespace GenOffice.Test
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            // 1. Render Step 1: Terms Agreement
            Bitmap bmpStep1 = new Bitmap(560, 480);
            using (Graphics g = Graphics.FromImage(bmpStep1))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                g.Clear(Color.White);
                using (GraphicsPath bPath = SetupForm.CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, 560 - 1f, 480 - 1f), 16f))
                using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
                {
                    g.DrawPath(bPen, bPath);
                }

                // Mascot Top Center (280, 105)
                MascotRenderer.DrawMascot(g, 280f, 105f, 1.05f, 1.2f, MascotMood.HappySmiling);

                // Title
                using (Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold))
                using (SolidBrush titleB = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString("Chào mừng bạn đến với GENOFFICE", titleFont, titleB, new RectangleF(0, 196f, 560, 30), sf);
                }

                // Terms Box
                Rectangle termsRect = new Rectangle(48, 246, 464, 130);
                using (GraphicsPath tp = SetupForm.CreateRoundedRectanglePath(termsRect, 6f))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (Pen tPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.FillPath(tb, tp);
                    g.DrawPath(tPen, tp);
                }

                string termsText =
                    "ĐIỀU KHOẢN VÀ CHÍNH SÁCH SỬ DỤNG GENOFFICE\n\n" +
                    "1. Bản quyền và mục đích sử dụng:\n" +
                    "GENOFFICE là bộ giải pháp phần mềm văn phòng thông minh được phát triển bởi NEXTGENEDUSPACE STUDIO nhằm mang lại công cụ xử lý văn bản, bảng tính, trình chiếu và tích hợp trí tuệ nhân tạo toàn diện cho người dùng.\n\n" +
                    "2. Quyền riêng tư và bảo mật dữ liệu:\n" +
                    "Toàn bộ tài liệu và nội dung do bạn tạo lập đều được xử lý và lưu trữ trực tiếp trên thiết bị cá nhân. Chúng tôi cam kết tuyệt đối tôn trọng quyền riêng tư và không can thiệp trái phép vào thông tin cá nhân của người dùng.";

                using (Font tf = new Font("Segoe UI", 8.8f, FontStyle.Regular))
                using (SolidBrush tTxt = new SolidBrush(Color.FromArgb(51, 65, 85)))
                {
                    g.DrawString(termsText, tf, tTxt, new RectangleF(58, 254, 444, 114));
                }

                // Checkbox
                using (Font chkFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                using (SolidBrush chkTxt = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    g.DrawString("☑  Tôi đã đọc và đồng ý với điều khoản sử dụng của GENOFFICE", chkFont, chkTxt, 48, 388);
                }

                // Button Next
                Rectangle btnNext = new Rectangle(362, 422, 150, 40);
                using (GraphicsPath bp = SetupForm.CreateRoundedRectanglePath(btnNext, 6f))
                using (SolidBrush bb = new SolidBrush(Color.FromArgb(37, 99, 235)))
                {
                    g.FillPath(bb, bp);
                    using (Font bf = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("Tiếp tục →", bf, Brushes.White, btnNext, sf);
                    }
                }
            }
            bmpStep1.Save(@"scratch\rendered_step1_terms.png", ImageFormat.Png);

            // 2. Render Step 2: Install Options
            Bitmap bmpStep2 = new Bitmap(560, 480);
            using (Graphics g = Graphics.FromImage(bmpStep2))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                g.Clear(Color.White);
                using (GraphicsPath bPath = SetupForm.CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, 560 - 1f, 480 - 1f), 16f))
                using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
                {
                    g.DrawPath(bPen, bPath);
                }

                // Mascot Top Center (280, 105)
                MascotRenderer.DrawMascot(g, 280f, 105f, 1.05f, 1.2f, MascotMood.HappySmiling);

                // Title
                using (Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold))
                using (SolidBrush titleB = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString("Tùy chọn cài đặt GENOFFICE", titleFont, titleB, new RectangleF(0, 196f, 560, 30), sf);
                }

                // Label Path
                using (Font lbf = new Font("Segoe UI", 9.2f, FontStyle.Bold))
                using (SolidBrush lbTxt = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    g.DrawString("Thư mục cài đặt:", lbf, lbTxt, 48, 246);
                }

                // TextBox Path
                Rectangle txtRect = new Rectangle(48, 270, 364, 26);
                using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1f))
                using (Font f = new Font("Segoe UI", 9.0f))
                {
                    g.DrawRectangle(p, txtRect);
                    g.DrawString(@"C:\Users\Username\AppData\Local\Programs\GenOffice", f, Brushes.Black, 52, 274);
                }

                // Browse Button
                Rectangle btnBr = new Rectangle(420, 268, 92, 28);
                using (GraphicsPath bp = SetupForm.CreateRoundedRectanglePath(btnBr, 4f))
                using (SolidBrush bb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (Pen bPen2 = new Pen(Color.FromArgb(203, 213, 225), 1f))
                {
                    g.FillPath(bb, bp);
                    g.DrawPath(bPen2, bp);
                    using (Font bf = new Font("Segoe UI", 9.0f))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("Duyệt...", bf, Brushes.Black, btnBr, sf);
                    }
                }

                // Checkboxes
                using (Font chkFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                using (SolidBrush chkTxt = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    g.DrawString("☑  Tạo lối tắt GENOFFICE trên màn hình nền Desktop", chkFont, chkTxt, 48, 314);
                    g.DrawString("☑  Thêm lối tắt GENOFFICE vào danh mục Start Menu", chkFont, chkTxt, 48, 344);
                }

                // Back Button
                Rectangle btnBack = new Rectangle(48, 422, 130, 40);
                using (GraphicsPath bp = SetupForm.CreateRoundedRectanglePath(btnBack, 6f))
                using (SolidBrush bb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (Pen bPen2 = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.FillPath(bb, bp);
                    g.DrawPath(bPen2, bp);
                    using (Font bf = new Font("Segoe UI", 9.0f))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("← Quay lại", bf, Brushes.Black, btnBack, sf);
                    }
                }

                // Install Button
                Rectangle btnInst = new Rectangle(362, 422, 150, 40);
                using (GraphicsPath bp = SetupForm.CreateRoundedRectanglePath(btnInst, 6f))
                using (SolidBrush bb = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillPath(bb, bp);
                    using (Font bf = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("Cài đặt ngay", bf, Brushes.White, btnInst, sf);
                    }
                }
            }
            bmpStep2.Save(@"scratch\rendered_step2_options.png", ImageFormat.Png);

            // 3. Render Step 3: Installing Progress
            Bitmap bmpStep3 = new Bitmap(560, 480);
            using (Graphics g = Graphics.FromImage(bmpStep3))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                g.Clear(Color.White);
                using (GraphicsPath bPath = SetupForm.CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, 560 - 1f, 480 - 1f), 16f))
                using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
                {
                    g.DrawPath(bPen, bPath);
                }

                // Mascot Top Center (280, 105)
                MascotRenderer.DrawMascot(g, 280f, 105f, 1.05f, 1.2f, MascotMood.HappySmiling);

                // Title
                using (Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold))
                using (SolidBrush titleB = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString("Đang cài đặt GENOFFICE...", titleFont, titleB, new RectangleF(0, 196f, 560, 30), sf);
                }

                // Progress Bar
                float barW = 400f;
                float barH = 4.5f;
                float barX = (560 - barW) / 2f;
                float barY = 290f;

                RectangleF trackRect = new RectangleF(barX, barY, barW, barH);
                using (GraphicsPath trackPath = SetupForm.CreateRoundedRectanglePath(trackRect, barH / 2f))
                using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillPath(trackBg, trackPath);
                }

                float fillW = barW * 0.65f;
                RectangleF fillRect = new RectangleF(barX, barY, fillW, barH);
                using (GraphicsPath fillPath = SetupForm.CreateRoundedRectanglePath(fillRect, barH / 2f))
                using (LinearGradientBrush fillBg = new LinearGradientBrush(fillRect, Color.FromArgb(59, 130, 246), Color.FromArgb(37, 99, 235), 0f))
                {
                    g.FillPath(fillBg, fillPath);
                }

                using (Font stFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
                using (SolidBrush stBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString("Đang giải nén tập tin ứng dụng GENOFFICE...", stFont, stBrush, new RectangleF(0, barY + 20, 560, 24), sf);
                }
            }
            bmpStep3.Save(@"scratch\rendered_step3_progress.png", ImageFormat.Png);

            // Zoomed Teeth Verification
            Bitmap bmpZoom = new Bitmap(180, 120);
            using (Graphics g = Graphics.FromImage(bmpZoom))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(bmpStep1, new Rectangle(0, 0, 180, 120), new Rectangle(240, 80, 80, 50), GraphicsUnit.Pixel);
            }
            bmpZoom.Save(@"scratch\rendered_mouth_zoom.png", ImageFormat.Png);

            Console.WriteLine("ALL SETUP STEPS PREVIEWS GENERATED SUCCESSFULLY!");
        }
    }
}
