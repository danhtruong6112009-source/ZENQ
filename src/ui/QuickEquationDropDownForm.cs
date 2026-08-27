using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class QuickEquationDropDownForm : Form
    {
        public Action<string> OnEquationSelected;
        public Action OnInsertNewEquationClicked;

        private int HoveredCard = -1;
        private int HoveredAction = -1;

        private string[] EqTitles = new string[] {
            "Diện tích hình tròn (Area of Circle)",
            "Khai triển nhị thức (Binomial)",
            "Chuỗi Fourier (Fourier Series)",
            "Định lý Pytago (Pythagoras)",
            "Công thức nghiệm bậc 2 (Quadratic)",
            "Đồng nhất thức lượng giác (Trig)"
        };

        private string[] EqFormulas = new string[] {
            "A = π r²",
            "(x + a)ⁿ = ∑ [n!/(k!(n-k)!)] xᵏ aⁿ⁻ᵏ",
            "f(x) = a₀ + ∑ (aₙ cos(nπx/L) + bₙ sin(nπx/L))",
            "a² + b² = c²",
            "x = (-b ± √(b² - 4ac)) / (2a)",
            "sin(α ± β) = sin α cos β ± cos α sin β"
        };

        public QuickEquationDropDownForm(Point location)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(320, 410);

            // Screen boundary clamping to prevent spilling off-screen
            Rectangle screen = Screen.FromPoint(location).WorkingArea;
            if (location.X + this.Width > screen.Right)
            {
                location.X = screen.Right - this.Width - 10;
            }
            if (location.Y + this.Height > screen.Bottom)
            {
                location.Y = screen.Bottom - this.Height - 10;
            }
            this.Location = location;

            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

            this.MouseMove += (s, e) =>
            {
                int prevC = HoveredCard;
                int prevA = HoveredAction;

                HoveredCard = -1;
                HoveredAction = -1;

                if (e.X >= 10 && e.X <= Width - 10 && e.Y >= 10 && e.Y <= 330)
                {
                    HoveredCard = (e.Y - 10) / 53;
                    if (HoveredCard > 5) HoveredCard = 5;
                }
                else if (e.Y >= 340 && e.Y <= 370) HoveredAction = 0; // Insert New
                else if (e.Y >= 372 && e.Y <= 402) HoveredAction = 1; // Ink

                if (prevC != HoveredCard || prevA != HoveredAction) Invalidate();
            };

            this.Deactivate += (s, e) => this.Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Border
            using (Pen borderPen = new Pen(Color.FromArgb(200, 205, 215), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            using (Font titleFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
            using (Font mathFont = new Font("Cambria Math", 9.5f, FontStyle.Italic))
            using (Font itemFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            {
                // 6 Equation Cards
                for (int i = 0; i < 6; i++)
                {
                    int y = 10 + i * 53;
                    Rectangle cardRect = new Rectangle(10, y, Width - 20, 48);

                    bool isHover = (HoveredCard == i);
                    if (isHover)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(235, 243, 255))) g.FillRectangle(hb, cardRect);
                        using (Pen hp = new Pen(Color.FromArgb(68, 114, 196), 1.2f)) g.DrawRectangle(hp, cardRect);
                    }
                    else
                    {
                        using (Pen p = new Pen(Color.FromArgb(235, 238, 242))) g.DrawRectangle(p, cardRect);
                    }

                    g.DrawString(EqTitles[i], titleFont, Brushes.DimGray, 16, y + 4);
                    g.DrawString(EqFormulas[i], mathFont, Brushes.Navy, 20, y + 20);
                }

                // Divider line
                g.DrawLine(Pens.LightGray, 0, 335, Width, 335);

                // Action 1: Insert New Equation
                Rectangle r1 = new Rectangle(4, 340, Width - 8, 30);
                if (HoveredAction == 0)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(235, 243, 255))) g.FillRectangle(hb, r1);
                }
                using (Font piFont = new Font("Segoe UI", 11.0f, FontStyle.Bold))
                {
                    g.DrawString("π", piFont, Brushes.Navy, 14, 344);
                }
                g.DrawString("Chèn công thức mới (New Equation)", itemFont, Brushes.Black, 34, 346);

                // Action 2: Ink Equation
                Rectangle r2 = new Rectangle(4, 372, Width - 8, 30);
                if (HoveredAction == 1)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(235, 243, 255))) g.FillRectangle(hb, r2);
                }
                g.DrawString("✎  Viết công thức tay (Ink Equation)...", itemFont, Brushes.Black, 14, 378);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (HoveredCard >= 0 && HoveredCard < 6)
            {
                if (OnEquationSelected != null) OnEquationSelected(EqFormulas[HoveredCard]);
                this.Close();
                return;
            }

            if (HoveredAction == 0 || HoveredAction == 1)
            {
                this.Close();
                if (OnInsertNewEquationClicked != null) OnInsertNewEquationClicked();
                return;
            }
        }
    }
}
