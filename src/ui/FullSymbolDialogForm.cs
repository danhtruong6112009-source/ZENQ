using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Microsoft 365 Authentic Full Symbol Dialog Form
    /// Direct2D GPU (D2DGraphics) accelerated symbol grid and recently used palette.
    /// Perfectly matches Microsoft Office Symbol dialog (Symbols & Special Characters tabs,
    /// dynamic Unicode name, hex character code, shortcut hint, and crisp antialiasing).
    /// </summary>
    public class FullSymbolDialogForm : Form
    {
        public Action<string> OnInsertSymbol;

        private ComboBox cbFont;
        private ComboBox cbSubset;
        private DoubleBufferedPanel pnlGrid;
        private Label lblCharName;
        private TextBox txtCharCode;
        private ComboBox cbFrom;
        private Label lblShortcut;
        private Button btnAutoCorrect;
        private Button btnShortcutKey;
        private Button btnInsert;
        private Button btnClose;

        private List<string> CurrentSymbols = new List<string>();
        private static List<string> RecentlyUsed = new List<string> {
            "√", "€", "£", "¥", "©", "®", "™", "±", "≠", "≤", "≥", "÷", "×", "∞", "µ", "α"
        };

        private int SelectedIndex = 0;
        private int HoveredIndex = -1;
        private int RecentHoveredIndex = -1;
        private int RecentSelectedIndex = -1;

        private Dictionary<string, string[]> Subsets = new Dictionary<string, string[]>();
        private Dictionary<string, string> SymbolNames = new Dictionary<string, string>();

        public class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                               ControlStyles.UserPaint |
                               ControlStyles.OptimizedDoubleBuffer |
                               ControlStyles.ResizeRedraw, true);
                this.UpdateStyles();
            }
        }

        public FullSymbolDialogForm()
        {
            this.Text = "Symbol";
            this.Size = new Size(580, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.DoubleBuffered = true;
            this.Font = new Font("Segoe UI", 9.0f);

            InitSubsets();
            InitSymbolNames();
            InitControls();
        }

        private void InitSubsets()
        {
            // 1. Mathematical Operators (Toán tử Toán học) - Standard Office 365 Default
            Subsets["Mathematical Operators"] = new string[] {
                "+", "−", "±", "∓", "×", "÷", "∕", "∗", "∘", "∙", "√", "∛", "∜", "∝", "∞", "∟",
                "∠", "∡", "∢", "∣", "∤", "∥", "∦", "∧", "∨", "∩", "∪", "∫", "∬", "∭", "∮", "∯",
                "∰", "∱", "∲", "∳", "∴", "∵", "∶", "∷", "∼", "∽", "≀", "≁", "≃", "≄", "≅", "≆",
                "≇", "≈", "≉", "≊", "≋", "≌", "≍", "≎", "≏", "≐", "≑", "≒", "≓", "≔", "≕", "≖",
                "≠", "≡", "≢", "≣", "≤", "≥", "≦", "≧", "≨", "≩", "≪", "≫", "≬", "≭", "≮", "≯",
                "≰", "≱", "≲", "≳", "⊂", "⊃", "⊄", "⊅", "⊆", "⊇", "⊈", "⊉", "⊊", "⊋", "⊏", "⊐",
                "⊕", "⊖", "⊗", "⊘", "⊙", "⊚", "⊛", "⊜", "⊝", "⊞", "⊟", "⊠", "⊡", "⊢", "⊣", "⊤",
                "⊥", "⊦", "⊧", "⊨", "⊩", "⊪", "⊫", "⊬", "⊭", "⊮", "⊯", "⋀", "⋁", "⋂", "⋃", "⋅",
                "⋆", "⋇", "⋈", "⋉", "⋊", "⋋", "⋌", "⋍", "⋎", "⋏", "⋐", "⋑", "⋒", "⋓", "⋮", "⋯"
            };

            // 2. Currency Symbols (Ký hiệu Tiền tệ)
            Subsets["Currency Symbols"] = new string[] {
                "$", "¢", "£", "¤", "¥", "֏", "؋", "৲", "৳", "৻", "૱", "௹", "฿", "៛", "₠", "₡",
                "₢", "₣", "₤", "₥", "₦", "₧", "₨", "₩", "₪", "₫", "€", "₭", "₮", "₯", "₰", "₱",
                "₲", "₳", "₴", "₵", "₶", "₷", "₸", "₹", "₺", "₼", "₽", "₾", "₿", "元", "円", "Rp",
                "Kč", "zł", "Fr", "kr", "ден", "lei", "din", "Ft", "R$", "kn", "лв"
            };

            // 3. Greek and Coptic (Ký tự Hy Lạp)
            Subsets["Greek and Coptic"] = new string[] {
                "α", "β", "γ", "δ", "ε", "ζ", "η", "θ", "ι", "κ", "λ", "μ", "ν", "ξ", "ο", "π",
                "ρ", "ς", "σ", "τ", "υ", "φ", "χ", "ψ", "ω", "ϑ", "ϒ", "ϖ", "ϰ", "ϱ", "ϵ", "϶",
                "Α", "Β", "Γ", "Δ", "Ε", "Ζ", "Η", "Θ", "Ι", "Κ", "Λ", "Μ", "Ν", "Ξ", "Ο", "Π",
                "Ρ", "Σ", "Τ", "Υ", "Φ", "Χ", "Ψ", "Ω", "Ϙ", "ϙ", "Ϛ", "ϛ", "Ϝ", "ϝ", "Ϟ", "ϟ"
            };

            // 4. Arrows (Mũi tên)
            Subsets["Arrows"] = new string[] {
                "←", "↑", "→", "↓", "↔", "↕", "↖", "↗", "↘", "↙", "↚", "↛", "↜", "↝", "↞", "↟",
                "↠", "↡", "↢", "↣", "↤", "↥", "↦", "↧", "↨", "↩", "↪", "↫", "↬", "↭", "↮", "↯",
                "↰", "↱", "↲", "↳", "↴", "↵", "↶", "↷", "↸", "↹", "↺", "↻", "↼", "↽", "↾", "↿",
                "⇀", "⇁", "⇂", "⇃", "⇄", "⇅", "⇆", "⇇", "⇈", "⇉", "⇊", "⇋", "⇌", "⇍", "⇎", "⇏",
                "⇐", "⇑", "⇒", "⇓", "⇔", "⇕", "⇖", "⇗", "⇘", "⇙", "⇚", "⇛", "⇜", "⇝", "⇞", "⇟"
            };

            // 5. Number Forms (Dạng chữ số & Phân số)
            Subsets["Number Forms"] = new string[] {
                "¼", "½", "¾", "⅐", "⅑", "⅒", "⅓", "⅔", "⅕", "⅖", "⅗", "⅘", "⅙", "⅚", "⅛", "⅜",
                "⅝", "⅞", "⅟", "Ⅰ", "Ⅱ", "Ⅲ", "Ⅳ", "Ⅴ", "Ⅵ", "Ⅶ", "Ⅷ", "Ⅸ", "Ⅹ", "Ⅺ", "Ⅻ", "Ⅼ",
                "Ⅽ", "Ⅾ", "Ⅿ", "ⅰ", "ⅱ", "ⅲ", "ⅳ", "ⅴ", "ⅵ", "ⅶ", "ⅷ", "ⅸ", "ⅹ", "ⅺ", "ⅻ", "ⅼ"
            };

            // 6. Letterlike Symbols (Ký hiệu chữ cái & Đơn vị)
            Subsets["Letterlike Symbols"] = new string[] {
                "©", "®", "™", "℠", "№", "℗", "℃", "℉", "K", "Å", "℮", "ℵ", "ℓ", "℘", "ℜ", "ℑ",
                "ℬ", "ℰ", "ℯ", "ℱ", "ℊ", "ℋ", "ℌ", "ℍ", "ℎ", "ℏ", "ℐ", "ℒ", "ℳ", "ℕ", "№", "℗",
                "℘", "ℙ", "ℚ", "ℛ", "ℜ", "ℝ", "℞", "℟", "℠", "℡", "™", "℣", "ℤ", "℥", "Ω", "℧"
            };

            // 7. Geometric Shapes (Hình khối & Biểu tượng)
            Subsets["Geometric Shapes"] = new string[] {
                "■", "□", "▢", "▣", "▤", "▥", "▦", "▧", "▨", "▩", "▪", "▫", "▬", "▭", "▮", "▯",
                "▲", "△", "▴", "▵", "▶", "▷", "▸", "▹", "►", "▼", "▽", "▾", "▿", "◀", "◁", "◂",
                "◃", "◄", "◆", "◇", "◈", "◉", "◊", "○", "◌", "◍", "◎", "●", "◐", "◑", "◒", "◓",
                "★", "☆", "✦", "✧", "✩", "✪", "✫", "✬", "✭", "✮", "✯", "✰", "⭐", "🌟", "✓", "✔"
            };

            // 8. Box Drawing (Khung viền)
            Subsets["Box Drawing"] = new string[] {
                "─", "━", "│", "┃", "┄", "┅", "┆", "┇", "┈", "┉", "┊", "┋", "┌", "┍", "┎", "┏",
                "┐", "┑", "┒", "┓", "└", "┕", "┖", "┗", "┘", "┙", "┚", "┛", "├", "┝", "┞", "┟",
                "┠", "┡", "┢", "┣", "┤", "┥", "┦", "┧", "┨", "┩", "┪", "┫", "┬", "┭", "┮", "┯",
                "═", "║", "╒", "╓", "╔", "╕", "╖", "╗", "╘", "╙", "╚", "╛", "╜", "╝", "╞", "╟"
            };
        }

        private void InitSymbolNames()
        {
            SymbolNames["√"] = "Square Root";
            SymbolNames["∛"] = "Cube Root";
            SymbolNames["∜"] = "Fourth Root";
            SymbolNames["∞"] = "Infinity";
            SymbolNames["∝"] = "Proportional To";
            SymbolNames["∫"] = "Integral";
            SymbolNames["∬"] = "Double Integral";
            SymbolNames["∭"] = "Triple Integral";
            SymbolNames["∮"] = "Contour Integral";
            SymbolNames["∑"] = "N-Ary Summation";
            SymbolNames["∏"] = "N-Ary Product";
            SymbolNames["±"] = "Plus-Minus Sign";
            SymbolNames["∓"] = "Minus-Or-Plus Sign";
            SymbolNames["×"] = "Multiplication Sign";
            SymbolNames["÷"] = "Division Sign";
            SymbolNames["≠"] = "Not Equal To";
            SymbolNames["≡"] = "Identical To";
            SymbolNames["≤"] = "Less-Than or Equal To";
            SymbolNames["≥"] = "Greater-Than or Equal To";
            SymbolNames["≪"] = "Much Less-Than";
            SymbolNames["≫"] = "Much Greater-Than";
            SymbolNames["≈"] = "Almost Equal To";
            SymbolNames["≅"] = "Approximately Equal To";
            SymbolNames["∂"] = "Partial Differential";
            SymbolNames["∇"] = "Nabla";
            SymbolNames["∈"] = "Element Of";
            SymbolNames["∉"] = "Not An Element Of";
            SymbolNames["∋"] = "Contains As Member";
            SymbolNames["⊂"] = "Subset Of";
            SymbolNames["⊃"] = "Superset Of";
            SymbolNames["∩"] = "Intersection";
            SymbolNames["∪"] = "Union";
            SymbolNames["€"] = "Euro Sign";
            SymbolNames["$"] = "Dollar Sign";
            SymbolNames["£"] = "Pound Sign";
            SymbolNames["¥"] = "Yen Sign";
            SymbolNames["₫"] = "Dong Sign";
            SymbolNames["©"] = "Copyright Sign";
            SymbolNames["®"] = "Registered Sign";
            SymbolNames["™"] = "Trade Mark Sign";
            SymbolNames["µ"] = "Micro Sign";
            SymbolNames["α"] = "Greek Small Letter Alpha";
            SymbolNames["β"] = "Greek Small Letter Beta";
            SymbolNames["γ"] = "Greek Small Letter Gamma";
            SymbolNames["δ"] = "Greek Small Letter Delta";
            SymbolNames["ε"] = "Greek Small Letter Epsilon";
            SymbolNames["θ"] = "Greek Small Letter Theta";
            SymbolNames["π"] = "Greek Small Letter Pi";
            SymbolNames["σ"] = "Greek Small Letter Sigma";
            SymbolNames["ω"] = "Greek Small Letter Omega";
            SymbolNames["Ω"] = "Greek Capital Letter Omega";
            SymbolNames["Δ"] = "Greek Capital Letter Delta";
            SymbolNames["°"] = "Degree Sign";
        }

        private void InitControls()
        {
            TabControl tabCtrl = new TabControl
            {
                Location = new Point(12, 10),
                Size = new Size(540, 420)
            };

            TabPage tabSymbols = new TabPage { Text = "Symbols", BackColor = Color.White };
            TabPage tabSpecial = new TabPage { Text = "Special Characters", BackColor = Color.White };

            // Controls on tabSymbols
            Label lblF = new Label { Text = "Font:", Location = new Point(15, 12), AutoSize = true, Font = new Font("Segoe UI", 9) };
            tabSymbols.Controls.Add(lblF);

            cbFont = new ComboBox { Location = new Point(55, 9), Size = new Size(180, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cbFont.Items.AddRange(new object[] { "Arial Black", "Calibri", "Segoe UI Symbol", "Arial", "Cambria Math", "Times New Roman", "(normal text)" });
            cbFont.SelectedIndex = 0;
            cbFont.SelectedIndexChanged += (s, e) => pnlGrid.Invalidate();
            tabSymbols.Controls.Add(cbFont);

            Label lblS = new Label { Text = "Subset:", Location = new Point(255, 12), AutoSize = true, Font = new Font("Segoe UI", 9) };
            tabSymbols.Controls.Add(lblS);

            cbSubset = new ComboBox { Location = new Point(310, 9), Size = new Size(210, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var key in Subsets.Keys) cbSubset.Items.Add(key);
            cbSubset.SelectedIndex = 0;
            cbSubset.SelectedIndexChanged += (s, e) =>
            {
                LoadSubsetSymbols((string)cbSubset.SelectedItem);
            };
            tabSymbols.Controls.Add(cbSubset);

            // Symbol Grid Panel (Direct2D GPU Accelerated)
            pnlGrid = new DoubleBufferedPanel
            {
                Location = new Point(15, 40),
                Size = new Size(505, 180),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            pnlGrid.Paint += DrawSymbolGrid;
            pnlGrid.MouseMove += (s, e) =>
            {
                int prev = HoveredIndex;
                int c = e.X / 31;
                int r = (e.Y - pnlGrid.AutoScrollPosition.Y) / 30;
                if (c >= 0 && c < 16 && e.X < 16 * 31)
                {
                    int idx = r * 16 + c;
                    HoveredIndex = (idx >= 0 && idx < CurrentSymbols.Count) ? idx : -1;
                }
                else HoveredIndex = -1;

                if (prev != HoveredIndex) pnlGrid.Invalidate();
            };
            pnlGrid.MouseLeave += (s, e) =>
            {
                if (HoveredIndex != -1)
                {
                    HoveredIndex = -1;
                    pnlGrid.Invalidate();
                }
            };
            pnlGrid.MouseClick += (s, e) =>
            {
                int c = e.X / 31;
                int r = (e.Y - pnlGrid.AutoScrollPosition.Y) / 30;
                if (c >= 0 && c < 16 && e.X < 16 * 31)
                {
                    int idx = r * 16 + c;
                    if (idx >= 0 && idx < CurrentSymbols.Count)
                    {
                        SelectedIndex = idx;
                        RecentSelectedIndex = -1;
                        UpdateSelectedInfo();
                        pnlGrid.Invalidate();
                        if (pnlGrid.Parent != null) pnlGrid.Parent.Invalidate(true);
                    }
                }
            };
            pnlGrid.DoubleClick += (s, e) =>
            {
                string sym = GetCurrentlySelectedSymbol();
                if (!string.IsNullOrEmpty(sym))
                {
                    InsertSelected(sym);
                }
            };
            tabSymbols.Controls.Add(pnlGrid);

            // Recently used label & panel (Direct2D GPU Accelerated)
            Label lblRecent = new Label { Text = "Recently used symbols:", Location = new Point(15, 230), AutoSize = true, Font = new Font("Segoe UI", 9) };
            tabSymbols.Controls.Add(lblRecent);

            DoubleBufferedPanel pnlRecent = new DoubleBufferedPanel
            {
                Location = new Point(15, 250),
                Size = new Size(505, 34),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRecent.Paint += (s, e) =>
            {
                using (D2DGraphics g = new D2DGraphics(e.Graphics, pnlRecent.Width, pnlRecent.Height))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    using (SolidBrush bgBrush = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(bgBrush, 0, 0, pnlRecent.Width, pnlRecent.Height);
                    }

                    string fontName = cbFont.Text == "(normal text)" ? "Segoe UI" : cbFont.Text;
                    using (Font symFont = new Font(fontName, 11.5f))
                    using (Pen gridPen = new Pen(Color.FromArgb(200, 204, 210), 1.0f))
                    using (Pen selBorderPen = new Pen(Color.FromArgb(0, 90, 180), 1.0f))
                    using (Pen hovBorderPen = new Pen(Color.FromArgb(120, 175, 235), 1.0f))
                    using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                    using (SolidBrush hovBrush = new SolidBrush(Color.FromArgb(235, 243, 255)))
                    using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                    using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(25, 25, 25)))
                    {
                        for (int i = 0; i < Math.Min(16, RecentlyUsed.Count); i++)
                        {
                            int x = i * 31;
                            Rectangle rect = new Rectangle(x, 0, 31, pnlRecent.Height);

                            bool isSel = (RecentSelectedIndex == i);
                            bool isHov = (RecentHoveredIndex == i);

                            if (isSel)
                            {
                                g.FillRectangle(selBrush, rect);
                                g.DrawRectangle(selBorderPen, rect);
                                using (Pen focusPen = new Pen(Color.White, 1.0f) { DashStyle = DashStyle.Dot })
                                {
                                    g.DrawRectangle(focusPen, x + 2, 2, 27, pnlRecent.Height - 5);
                                }
                            }
                            else if (isHov)
                            {
                                g.FillRectangle(hovBrush, rect);
                                g.DrawRectangle(hovBorderPen, rect);
                            }

                            g.DrawLine(gridPen, x + 31, 0, x + 31, pnlRecent.Height);

                            SizeF sz = g.MeasureString(RecentlyUsed[i], symFont);
                            SolidBrush b = isSel ? whiteBrush : textBrush;
                            g.DrawString(RecentlyUsed[i], symFont, b, x + (31 - sz.Width) / 2, (pnlRecent.Height - sz.Height) / 2);
                        }
                    }
                }
            };
            pnlRecent.MouseMove += (s, e) =>
            {
                int prev = RecentHoveredIndex;
                int idx = e.X / 31;
                RecentHoveredIndex = (idx >= 0 && idx < Math.Min(16, RecentlyUsed.Count)) ? idx : -1;
                if (prev != RecentHoveredIndex) pnlRecent.Invalidate();
            };
            pnlRecent.MouseLeave += (s, e) =>
            {
                if (RecentHoveredIndex != -1)
                {
                    RecentHoveredIndex = -1;
                    pnlRecent.Invalidate();
                }
            };
            pnlRecent.MouseClick += (s, e) =>
            {
                int idx = e.X / 31;
                if (idx >= 0 && idx < Math.Min(16, RecentlyUsed.Count))
                {
                    RecentSelectedIndex = idx;
                    SelectedIndex = -1;
                    UpdateSelectedInfo();
                    pnlRecent.Invalidate();
                    pnlGrid.Invalidate();
                }
            };
            pnlRecent.DoubleClick += (s, e) =>
            {
                string sym = GetCurrentlySelectedSymbol();
                if (!string.IsNullOrEmpty(sym))
                {
                    InsertSelected(sym);
                }
            };
            tabSymbols.Controls.Add(pnlRecent);

            // Unicode details row
            Label lblUnicodeNameHeader = new Label
            {
                Text = "Unicode name:",
                Location = new Point(15, 292),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
            };
            tabSymbols.Controls.Add(lblUnicodeNameHeader);

            lblCharName = new Label
            {
                Text = "Square Root",
                Location = new Point(15, 308),
                Size = new Size(200, 18),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            tabSymbols.Controls.Add(lblCharName);

            btnAutoCorrect = new Button
            {
                Text = "AutoCorrect...",
                Location = new Point(15, 332),
                Size = new Size(95, 25),
                BackColor = Color.White,
                FlatStyle = FlatStyle.System
            };
            tabSymbols.Controls.Add(btnAutoCorrect);

            btnShortcutKey = new Button
            {
                Text = "Shortcut Key...",
                Location = new Point(116, 332),
                Size = new Size(95, 25),
                BackColor = Color.White,
                FlatStyle = FlatStyle.System
            };
            tabSymbols.Controls.Add(btnShortcutKey);

            lblShortcut = new Label
            {
                Text = "Shortcut key: 221A, Alt+X",
                Location = new Point(220, 336),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f)
            };
            tabSymbols.Controls.Add(lblShortcut);

            Label lblCode = new Label
            {
                Text = "Character code:",
                Location = new Point(280, 298),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f)
            };
            tabSymbols.Controls.Add(lblCode);

            txtCharCode = new TextBox
            {
                Location = new Point(370, 295),
                Size = new Size(50, 22),
                ReadOnly = true,
                Text = "221A"
            };
            tabSymbols.Controls.Add(txtCharCode);

            Label lblFrom = new Label
            {
                Text = "from:",
                Location = new Point(425, 298),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f)
            };
            tabSymbols.Controls.Add(lblFrom);

            cbFrom = new ComboBox
            {
                Location = new Point(460, 295),
                Size = new Size(60, 22),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbFrom.Items.AddRange(new object[] { "Unicode (hex)", "ASCII (decimal)" });
            cbFrom.SelectedIndex = 0;
            tabSymbols.Controls.Add(cbFrom);

            // Special Characters list on tabSpecial
            ListView lvSpecial = new ListView
            {
                Location = new Point(15, 15),
                Size = new Size(505, 350),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9.5f)
            };
            lvSpecial.Columns.Add("Character", 80);
            lvSpecial.Columns.Add("Special Character", 300);
            lvSpecial.Columns.Add("Shortcut key", 110);

            string[,] specials = new string[,] {
                { "—", "Em Dash", "Alt+Ctrl+Num -" },
                { "–", "En Dash", "Ctrl+Num -" },
                { "©", "Copyright", "Alt+Ctrl+C" },
                { "®", "Registered", "Alt+Ctrl+R" },
                { "™", "Trademark", "Alt+Ctrl+T" },
                { "§", "Section", "" },
                { "¶", "Paragraph", "" },
                { "…", "Ellipsis", "Alt+Ctrl+." },
                { "‘", "Single Opening Quote", "Ctrl+`, `" },
                { "’", "Single Closing Quote", "Ctrl+', '" },
                { "“", "Double Opening Quote", "Ctrl+`, \"" },
                { "”", "Double Closing Quote", "Ctrl+', \"" },
                { "€", "Euro Sign", "Alt+Ctrl+E" },
                { "°", "Degree Sign", "" },
                { "µ", "Micro Sign", "" },
                { "±", "Plus-Minus", "" }
            };

            for (int i = 0; i < specials.GetLength(0); i++)
            {
                ListViewItem item = new ListViewItem(specials[i, 0]);
                item.SubItems.Add(specials[i, 1]);
                item.SubItems.Add(specials[i, 2]);
                lvSpecial.Items.Add(item);
            }
            lvSpecial.DoubleClick += (s, e) =>
            {
                if (lvSpecial.SelectedItems.Count > 0)
                {
                    InsertSelected(lvSpecial.SelectedItems[0].Text);
                }
            };
            tabSpecial.Controls.Add(lvSpecial);

            tabCtrl.TabPages.Add(tabSymbols);
            tabCtrl.TabPages.Add(tabSpecial);
            this.Controls.Add(tabCtrl);

            // Bottom Buttons (MS 365 Standard)
            btnInsert = new Button
            {
                Text = "Insert",
                Location = new Point(366, 442),
                Size = new Size(88, 28),
                BackColor = Color.FromArgb(0, 95, 184),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.System
            };
            btnInsert.Click += (s, e) =>
            {
                string sym = GetCurrentlySelectedSymbol();
                if (!string.IsNullOrEmpty(sym))
                {
                    InsertSelected(sym);
                }
            };
            this.Controls.Add(btnInsert);

            btnClose = new Button
            {
                Text = "Cancel",
                Location = new Point(464, 442),
                Size = new Size(88, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.Cancel
            };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            LoadSubsetSymbols((string)cbSubset.Items[0]);
        }

        private void LoadSubsetSymbols(string subsetName)
        {
            CurrentSymbols.Clear();
            if (Subsets.ContainsKey(subsetName))
            {
                CurrentSymbols.AddRange(Subsets[subsetName]);
            }
            SelectedIndex = 0;
            UpdateSelectedInfo();
            pnlGrid.Invalidate();
        }

        private void DrawSymbolGrid(object sender, PaintEventArgs e)
        {
            using (D2DGraphics g = new D2DGraphics(e.Graphics, pnlGrid.Width, pnlGrid.Height))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (SolidBrush bgBrush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(bgBrush, 0, 0, pnlGrid.Width, pnlGrid.Height);
                }

                string fontName = cbFont.Text == "(normal text)" ? "Segoe UI" : cbFont.Text;
                using (Font symFont = new Font(fontName, 12f))
                using (Pen gridPen = new Pen(Color.FromArgb(200, 204, 210), 1.0f))
                using (Pen selBorderPen = new Pen(Color.FromArgb(0, 90, 180), 1.0f))
                using (Pen hovBorderPen = new Pen(Color.FromArgb(120, 175, 235), 1.0f))
                using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                using (SolidBrush hovBrush = new SolidBrush(Color.FromArgb(235, 243, 255)))
                using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(25, 25, 25)))
                {
                    for (int i = 0; i < CurrentSymbols.Count; i++)
                    {
                        int c = i % 16;
                        int r = i / 16;
                        int x = c * 31;
                        int y = r * 30 + pnlGrid.AutoScrollPosition.Y;
                        Rectangle rect = new Rectangle(x, y, 31, 30);

                        if (rect.Bottom < 0 || rect.Top > pnlGrid.Height) continue;

                        bool isSel = (SelectedIndex == i && RecentSelectedIndex == -1);
                        bool isHov = (HoveredIndex == i);

                        if (isSel)
                        {
                            g.FillRectangle(selBrush, rect);
                            g.DrawRectangle(selBorderPen, rect);
                            using (Pen focusPen = new Pen(Color.White, 1.0f) { DashStyle = DashStyle.Dot })
                            {
                                g.DrawRectangle(focusPen, x + 2, y + 2, 27, 26);
                            }
                        }
                        else if (isHov)
                        {
                            g.FillRectangle(hovBrush, rect);
                            g.DrawRectangle(hovBorderPen, rect);
                        }

                        g.DrawLine(gridPen, x + 31, y, x + 31, y + 30);
                        g.DrawLine(gridPen, x, y + 30, x + 31, y + 30);

                        SizeF sz = g.MeasureString(CurrentSymbols[i], symFont);
                        SolidBrush b = isSel ? whiteBrush : textBrush;
                        g.DrawString(CurrentSymbols[i], symFont, b, x + (31 - sz.Width) / 2, y + (30 - sz.Height) / 2);
                    }
                }
            }
        }

        private string GetCurrentlySelectedSymbol()
        {
            if (RecentSelectedIndex >= 0 && RecentSelectedIndex < RecentlyUsed.Count)
            {
                return RecentlyUsed[RecentSelectedIndex];
            }
            if (SelectedIndex >= 0 && SelectedIndex < CurrentSymbols.Count)
            {
                return CurrentSymbols[SelectedIndex];
            }
            return "";
        }

        private void UpdateSelectedInfo()
        {
            string sym = GetCurrentlySelectedSymbol();
            if (string.IsNullOrEmpty(sym)) return;

            string name = SymbolNames.ContainsKey(sym) ? SymbolNames[sym] : sym;
            lblCharName.Text = name;

            if (sym.Length > 0)
            {
                int code = char.ConvertToUtf32(sym, 0);
                string hex = code.ToString("X4");
                txtCharCode.Text = hex;
                lblShortcut.Text = "Shortcut key: " + hex + ", Alt+X";
            }
        }

        private void InsertSelected(string sym)
        {
            if (string.IsNullOrEmpty(sym)) return;

            // Add to recently used
            RecentlyUsed.Remove(sym);
            RecentlyUsed.Insert(0, sym);
            if (RecentlyUsed.Count > 16) RecentlyUsed.RemoveAt(RecentlyUsed.Count - 1);

            if (OnInsertSymbol != null) OnInsertSymbol(sym);
        }
    }
}
