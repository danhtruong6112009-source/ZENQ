using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Microsoft 365 Authentic Built-In Equation Gallery DropDown Popup
    /// Rendered entirely via Direct2D GPU (D2DGraphics) for crisp subpixel text and anti-aliased mathematical notation.
    /// </summary>
    public class QuickEquationDropDownForm : Form
    {
        public Action<string> OnEquationSelected;
        public Action OnInsertNewEquationClicked;

        private int HoveredCard = -1;
        private int HoveredAction = -1;

        private struct EquationEntry
        {
            public string Title;
            public string FormulaDisplay;
            public string Latex;

            public EquationEntry(string title, string formula, string latex)
            {
                Title = title;
                FormulaDisplay = formula;
                Latex = latex;
            }
        }

        private static readonly EquationEntry[] BuiltInEquations = new EquationEntry[]
        {
            new EquationEntry(
                "Area of Circle",
                "A = π r²",
                "A = \\pi r^2"
            ),
            new EquationEntry(
                "Binomial Theorem",
                "(x + a)ⁿ = ∑ [n! / (k!(n - k)!)] xᵏ aⁿ⁻ᵏ",
                "(x + a)^n = \\sum_{k=0}^n \\binom{n}{k} x^k a^{n-k}"
            ),
            new EquationEntry(
                "Expansion of a Sum",
                "(1 + x)ⁿ = 1 + (nx / 1!) + [n(n - 1)x² / 2!] + ⋯",
                "(1 + x)^n = 1 + \\frac{nx}{1!} + \\frac{n(n-1)x^2}{2!} + \\dots"
            ),
            new EquationEntry(
                "Fourier Series",
                "f(x) = a₀ + ∑ (aₙ cos(nπx / L) + bₙ sin(nπx / L))",
                "f(x) = a_0 + \\sum_{n=1}^\\infty \\left(a_n \\cos\\frac{n\\pi x}{L} + b_n \\sin\\frac{n\\pi x}{L}\\right)"
            ),
            new EquationEntry(
                "Pythagorean Theorem",
                "a² + b² = c²",
                "a^2 + b^2 = c^2"
            ),
            new EquationEntry(
                "Quadratic Formula",
                "x = (-b ± √(b² - 4ac)) / (2a)",
                "x = \\frac{-b \\pm \\sqrt{b^2 - 4ac}}{2a}"
            ),
            new EquationEntry(
                "Taylor Expansion",
                "f(x) = f(a) + f'(a)(x - a) + [f''(a) / 2!](x - a)² + ⋯",
                "f(x) = f(a) + f'(a)(x - a) + \\frac{f''(a)}{2!}(x - a)^2 + \\dots"
            ),
            new EquationEntry(
                "Trig Identity 1",
                "sin(α ± β) = sin α cos β ± cos α sin β",
                "\\sin(\\alpha \\pm \\beta) = \\sin\\alpha\\cos\\beta \\pm \\cos\\alpha\\sin\\beta"
            )
        };

        private int _scrollY = 0;
        private const int CardHeight = 62;
        private const int HeaderHeight = 28;
        private const int FooterHeight = 118;

        public QuickEquationDropDownForm(Point location)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(380, 520);

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
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);

            this.MouseMove += (s, e) =>
            {
                int prevC = HoveredCard;
                int prevA = HoveredAction;

                HoveredCard = -1;
                HoveredAction = -1;

                int cardsTop = HeaderHeight;
                int cardsBottom = this.Height - FooterHeight;

                if (e.Y >= cardsTop && e.Y < cardsBottom)
                {
                    int relY = e.Y - cardsTop + _scrollY;
                    int idx = relY / CardHeight;
                    if (idx >= 0 && idx < BuiltInEquations.Length)
                    {
                        HoveredCard = idx;
                    }
                }
                else if (e.Y >= cardsBottom)
                {
                    int footerRelY = e.Y - cardsBottom;
                    if (footerRelY >= 4 && footerRelY < 30) HoveredAction = 0;      // More from Office.com
                    else if (footerRelY >= 32 && footerRelY < 58) HoveredAction = 1; // Insert New Equation
                    else if (footerRelY >= 60 && footerRelY < 86) HoveredAction = 2; // Ink Equation
                    else if (footerRelY >= 88 && footerRelY < 114) HoveredAction = 3; // Save Selection (disabled)
                }

                if (prevC != HoveredCard || prevA != HoveredAction) Invalidate();
            };

            this.MouseWheel += (s, e) =>
            {
                int maxScroll = Math.Max(0, BuiltInEquations.Length * CardHeight - (this.Height - HeaderHeight - FooterHeight));
                _scrollY = Math.Max(0, Math.Min(maxScroll, _scrollY - e.Delta / 2));
                Invalidate();
            };

            this.Deactivate += (s, e) => this.Close();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (D2DGraphics g = new D2DGraphics(e.Graphics, this.Width, this.Height))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 1. Popup Background & Outer Border
                using (SolidBrush bgBrush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(bgBrush, 0, 0, Width, Height);
                }
                using (Pen borderPen = new Pen(Color.FromArgb(204, 208, 214), 1f))
                {
                    g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                }

                // 2. Header: "Built-In"
                using (Font headerFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(20, 20, 20)))
                {
                    g.DrawString("Built-In", headerFont, headerBrush, 14, 6);
                }

                // 3. Equation Cards List (Clipped between Header and Footer)
                int cardsTop = HeaderHeight;
                int cardsBottom = this.Height - FooterHeight;
                Rectangle cardsClip = new Rectangle(0, cardsTop, Width, cardsBottom - cardsTop);
                g.PushClip(cardsClip);

                using (Font titleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (Font mathFont = new Font("Cambria Math", 10.5f, FontStyle.Italic))
                using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(50, 50, 50)))
                using (SolidBrush mathBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (Pen bracketPen = new Pen(Color.FromArgb(100, 100, 100), 1.0f))
                {
                    for (int i = 0; i < BuiltInEquations.Length; i++)
                    {
                        int cardY = cardsTop + i * CardHeight - _scrollY;
                        Rectangle cardRect = new Rectangle(8, cardY, Width - 16, CardHeight - 4);

                        if (cardRect.Bottom < cardsTop || cardRect.Top > cardsBottom) continue;

                        bool isHover = (HoveredCard == i);
                        if (isHover)
                        {
                            using (SolidBrush hb = new SolidBrush(Color.FromArgb(238, 244, 254)))
                            {
                                g.FillRectangle(hb, cardRect);
                            }
                            using (Pen hp = new Pen(Color.FromArgb(120, 170, 235), 1.0f))
                            {
                                g.DrawRectangle(hp, cardRect);
                            }
                        }

                        // Title
                        g.DrawString(BuiltInEquations[i].Title, titleFont, titleBrush, cardRect.Left + 8, cardRect.Top + 4);

                        // Authentic MS Office Equation Display: Horizontal shelf line with vertical end ticks
                        int shelfY = cardRect.Top + 24;
                        int shelfLeft = cardRect.Left + 8;
                        int shelfRight = cardRect.Right - 8;
                        g.DrawLine(bracketPen, shelfLeft, shelfY, shelfRight, shelfY);
                        g.DrawLine(bracketPen, shelfLeft, shelfY, shelfLeft, shelfY + 28);
                        g.DrawLine(bracketPen, shelfRight, shelfY, shelfRight, shelfY + 28);

                        // Formula text inside shelf box
                        g.DrawString(BuiltInEquations[i].FormulaDisplay, mathFont, mathBrush, shelfLeft + 4, shelfY + 4);
                    }
                }

                g.PopClip();

                // 4. Divider Line before Footer Actions
                using (Pen divPen = new Pen(Color.FromArgb(226, 230, 235), 1f))
                {
                    g.DrawLine(divPen, 1, cardsBottom, Width - 2, cardsBottom);
                }

                // 5. Footer Actions (Matching MS Office 365)
                using (Font actionFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
                using (Font piIconFont = new Font("Segoe UI", 11.0f, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(20, 20, 20)))
                using (SolidBrush disabledBrush = new SolidBrush(Color.FromArgb(160, 165, 175)))
                using (SolidBrush blueBrush = new SolidBrush(Color.FromArgb(0, 95, 184)))
                using (Pen arrowPen = new Pen(Color.FromArgb(120, 120, 120), 1.2f))
                {
                    // Action 0: More Equations from Office.com
                    Rectangle r0 = new Rectangle(4, cardsBottom + 3, Width - 8, 26);
                    if (HoveredAction == 0)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(238, 244, 254))) g.FillRectangle(hb, r0);
                    }
                    // Globe icon (vector)
                    int gx = r0.Left + 12, gy = r0.Top + 6;
                    using (Pen globePen = new Pen(Color.FromArgb(0, 95, 184), 1.2f))
                    {
                        g.DrawEllipse(globePen, gx, gy, 13, 13);
                        g.DrawLine(globePen, gx, gy + 6.5f, gx + 13, gy + 6.5f);
                        g.DrawEllipse(globePen, gx + 3.5f, gy, 6, 13);
                    }
                    g.DrawString("More Equations from Office.com", actionFont, textBrush, r0.Left + 32, r0.Top + 4);
                    // Chevron right
                    g.DrawLine(arrowPen, r0.Right - 16, r0.Top + 9, r0.Right - 12, r0.Top + 13);
                    g.DrawLine(arrowPen, r0.Right - 12, r0.Top + 13, r0.Right - 16, r0.Top + 17);

                    // Action 1: Insert New Equation
                    Rectangle r1 = new Rectangle(4, cardsBottom + 31, Width - 8, 26);
                    if (HoveredAction == 1)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(238, 244, 254))) g.FillRectangle(hb, r1);
                    }
                    g.DrawString("π", piIconFont, blueBrush, r1.Left + 12, r1.Top + 2);
                    g.DrawString("Insert New Equation", actionFont, textBrush, r1.Left + 32, r1.Top + 4);

                    // Action 2: Ink Equation
                    Rectangle r2 = new Rectangle(4, cardsBottom + 59, Width - 8, 26);
                    if (HoveredAction == 2)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(238, 244, 254))) g.FillRectangle(hb, r2);
                    }
                    // Pen icon
                    using (Font penF = new Font("Segoe UI", 10.0f))
                    {
                        g.DrawString("✎", penF, blueBrush, r2.Left + 11, r2.Top + 3);
                    }
                    g.DrawString("Ink Equation", actionFont, textBrush, r2.Left + 32, r2.Top + 4);

                    // Action 3: Save Selection to Equation Gallery... (Disabled like Office when no selection)
                    Rectangle r3 = new Rectangle(4, cardsBottom + 87, Width - 8, 26);
                    // Save icon
                    using (Font saveF = new Font("Segoe UI", 9.5f))
                    {
                        g.DrawString("💾", saveF, disabledBrush, r3.Left + 11, r3.Top + 4);
                    }
                    g.DrawString("Save Selection to Equation Gallery...", actionFont, disabledBrush, r3.Left + 32, r3.Top + 4);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (HoveredCard >= 0 && HoveredCard < BuiltInEquations.Length)
            {
                string formula = BuiltInEquations[HoveredCard].Latex;
                this.Close();
                if (OnEquationSelected != null) OnEquationSelected(formula);
                return;
            }

            if (HoveredAction == 0 || HoveredAction == 1 || HoveredAction == 2)
            {
                this.Close();
                if (OnInsertNewEquationClicked != null) OnInsertNewEquationClicked();
                return;
            }
        }
    }
}
