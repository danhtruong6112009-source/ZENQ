using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class ScreenSnippingForm : Form
    {
        public Bitmap CapturedBitmap { get; private set; }

        private Bitmap fullScreenBmp;
        private Point startPoint;
        private Rectangle selectRect;
        private bool isSelecting = false;

        public ScreenSnippingForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Cross;

            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            this.Bounds = bounds;

            // Capture screen before showing form
            fullScreenBmp = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(fullScreenBmp))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            if (fullScreenBmp != null)
            {
                g.DrawImage(fullScreenBmp, 0, 0);
            }

            // Dark overlay
            using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            {
                g.FillRectangle(darkBrush, this.ClientRectangle);
            }

            // If selecting, cut out the bright selected region
            if (!selectRect.IsEmpty && fullScreenBmp != null)
            {
                g.DrawImage(fullScreenBmp, selectRect, selectRect, GraphicsUnit.Pixel);

                using (Pen borderPen = new Pen(Color.FromArgb(0, 120, 215), 2.0f))
                {
                    g.DrawRectangle(borderPen, selectRect);
                }

                using (Font infoFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                using (SolidBrush infoBg = new SolidBrush(Color.FromArgb(0, 120, 215)))
                {
                    string szText = string.Format("{0} x {1} px", selectRect.Width, selectRect.Height);
                    SizeF sz = g.MeasureString(szText, infoFont);
                    int tagY = selectRect.Bottom + 5;
                    if (tagY + sz.Height > ClientSize.Height) tagY = selectRect.Top - (int)sz.Height - 5;
                    g.FillRectangle(infoBg, selectRect.Left, tagY, sz.Width + 8, sz.Height + 4);
                    g.DrawString(szText, infoFont, Brushes.White, selectRect.Left + 4, tagY + 2);
                }
            }

            // Helper hint at top
            using (Font hintFont = new Font("Segoe UI", 11.0f, FontStyle.Bold))
            using (SolidBrush hintBg = new SolidBrush(Color.FromArgb(200, 30, 30, 30)))
            {
                string hint = "Kéo chuột để chọn vùng cần chụp màn hình | Nhấn ESC để hủy";
                SizeF hSz = g.MeasureString(hint, hintFont);
                int hX = (ClientSize.Width - (int)hSz.Width) / 2;
                g.FillRectangle(hintBg, hX - 10, 20, hSz.Width + 20, hSz.Height + 10);
                g.DrawString(hint, hintFont, Brushes.White, hX, 25);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                selectRect = new Rectangle(e.Location, new Size(0, 0));
                this.Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isSelecting)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int w = Math.Abs(startPoint.X - e.X);
                int h = Math.Abs(startPoint.Y - e.Y);
                selectRect = new Rectangle(x, y, w, h);
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (isSelecting)
            {
                isSelecting = false;
                if (selectRect.Width > 10 && selectRect.Height > 10 && fullScreenBmp != null)
                {
                    CapturedBitmap = new Bitmap(selectRect.Width, selectRect.Height);
                    using (Graphics g = Graphics.FromImage(CapturedBitmap))
                    {
                        g.DrawImage(fullScreenBmp, new Rectangle(0, 0, selectRect.Width, selectRect.Height), selectRect, GraphicsUnit.Pixel);
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (fullScreenBmp != null) { fullScreenBmp.Dispose(); fullScreenBmp = null; }
            }
            base.Dispose(disposing);
        }
    }
}
