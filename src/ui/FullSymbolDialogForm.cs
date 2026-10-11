using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class FullSymbolDialogForm : Form
    {
        public Action<string> OnInsertSymbol;

        private ComboBox cbFont;
        private ComboBox cbSubset;
        private DoubleBufferedPanel pnlGrid;
        private Label lblCharName;
        private TextBox txtCharCode;
        private Button btnInsert;
        private Button btnClose;

        private List<string> CurrentSymbols = new List<string>();
        private static List<string> RecentlyUsed = new List<string> {
            "€", "£", "¥", "©", "®", "™", "±", "≠", "≤", "≥", "÷", "×", "∞", "µ", "α", "β", "π", "Ω"
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
            this.Text = "Ký hiệu (Symbol)";
            this.Size = new Size(580, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.DoubleBuffered = true;

            InitSubsets();
            InitControls();
        }

        private void InitSubsets()
        {
            // 1. Currency Symbols (Ký hiệu Tiền tệ)
            Subsets["Currency Symbols (Ký hiệu Tiền tệ)"] = new string[] {
                "$", "¢", "£", "¤", "¥", "֏", "؋", "৲", "৳", "৻", "૱", "௹", "฿", "៛", "₠", "₡",
                "₢", "₣", "₤", "₥", "₦", "₧", "₨", "₩", "₪", "₫", "€", "₭", "₮", "₯", "₰", "₱",
                "₲", "₳", "₴", "₵", "₶", "₷", "₸", "₹", "₺", "₼", "₽", "₾", "₿", "元", "円", "Rp",
                "Kč", "zł", "Fr", "kr", "ден", "lei", "din", "Ft", "R$", "kn", "лв"
            };

            // 2. Mathematical Operators (Toán tử Toán học)
            Subsets["Mathematical Operators (Toán tử Toán học)"] = new string[] {
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

            // 3. Greek and Coptic (Ký tự Hy Lạp)
            Subsets["Greek and Coptic (Ký tự Hy Lạp)"] = new string[] {
                "α", "β", "γ", "δ", "ε", "ζ", "η", "θ", "ι", "κ", "λ", "μ", "ν", "ξ", "ο", "π",
                "ρ", "ς", "σ", "τ", "υ", "φ", "χ", "ψ", "ω", "ϑ", "ϒ", "ϖ", "ϰ", "ϱ", "ϵ", "϶",
                "Α", "Β", "Γ", "Δ", "Ε", "Ζ", "Η", "Θ", "Ι", "Κ", "Λ", "Μ", "Ν", "Ξ", "Ο", "Π",
                "Ρ", "Σ", "Τ", "Υ", "Φ", "Χ", "Ψ", "Ω", "Ϙ", "ϙ", "Ϛ", "ϛ", "Ϝ", "ϝ", "Ϟ", "ϟ"
            };

            // 4. Arrows & Directions (Mũi tên & Hướng)
            Subsets["Arrows (Mũi tên & Hướng)"] = new string[] {
                "←", "↑", "→", "↓", "↔", "↕", "↖", "↗", "↘", "↙", "↚", "↛", "↜", "↝", "↞", "↟",
                "↠", "↡", "↢", "↣", "↤", "↥", "↦", "↧", "↨", "↩", "↪", "↫", "↬", "↭", "↮", "↯",
                "↰", "↱", "↲", "↳", "↴", "↵", "↶", "↷", "↸", "↹", "↺", "↻", "↼", "↽", "↾", "↿",
                "⇀", "⇁", "⇂", "⇃", "⇄", "⇅", "⇆", "⇇", "⇈", "⇉", "⇊", "⇋", "⇌", "⇍", "⇎", "⇏",
                "⇐", "⇑", "⇒", "⇓", "⇔", "⇕", "⇖", "⇗", "⇘", "⇙", "⇚", "⇛", "⇜", "⇝", "⇞", "⇟",
                "⇠", "⇡", "⇢", "⇣", "⇤", "⇥", "⇦", "⇧", "⇨", "⇩", "⇪", "➔", "➘", "➙", "➚", "➛",
                "➜", "➝", "➞", "➟", "➠", "➡", "➢", "➣", "➤", "➥", "➦", "➧", "➨", "➩", "➪", "➫"
            };

            // 5. Number Forms & Fractions (Dạng chữ số & Phân số)
            Subsets["Number Forms (Dạng chữ số & Phân số)"] = new string[] {
                "¼", "½", "¾", "⅐", "⅑", "⅒", "⅓", "⅔", "⅕", "⅖", "⅗", "⅘", "⅙", "⅚", "⅛", "⅜",
                "⅝", "⅞", "⅟", "Ⅰ", "Ⅱ", "Ⅲ", "Ⅳ", "Ⅴ", "Ⅵ", "Ⅶ", "Ⅷ", "Ⅸ", "Ⅹ", "Ⅺ", "Ⅻ", "Ⅼ",
                "Ⅽ", "Ⅾ", "Ⅿ", "ⅰ", "ⅱ", "ⅲ", "ⅳ", "ⅴ", "ⅵ", "ⅶ", "ⅷ", "ⅸ", "ⅹ", "ⅺ", "ⅻ", "ⅼ",
                "ⅽ", "ⅾ", "ⅿ", "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧", "⑨", "⑩", "⑪", "⑫", "⑬",
                "⑭", "⑮", "⑯", "⑰", "⑱", "⑲", "⑳", "⑴", "⑵", "⑶", "⑷", "⑸", "⑹", "⑺", "⑻", "⑼",
                "⑽", "⒈", "⒉", "⒊", "⒋", "⒌", "⒍", "⒎", "⒏", "⒐", "⒑", "❶", "❷", "❸", "❹", "❺"
            };

            // 6. Letterlike Symbols & Units (Ký hiệu dạng chữ cái & Đơn vị)
            Subsets["Letterlike Symbols (Ký hiệu chữ cái & Đơn vị)"] = new string[] {
                "©", "®", "™", "℠", "№", "℗", "℃", "℉", "K", "Å", "℮", "ℵ", "ℓ", "℘", "ℜ", "ℑ",
                "ℬ", "ℰ", "ℯ", "ℱ", "ℊ", "ℋ", "ℌ", "ℍ", "ℎ", "ℏ", "ℐ", "ℒ", "ℳ", "ℕ", "№", "℗",
                "℘", "ℙ", "ℚ", "ℛ", "ℜ", "ℝ", "℞", "℟", "℠", "℡", "™", "℣", "ℤ", "℥", "Ω", "℧",
                "ℨ", "℩", "K", "Å", "ℬ", "ℭ", "℮", "ℯ", "ℰ", "ℱ", "Ⅎ", "ℳ", "µ", "‰", "‱", "ℶ"
            };

            // 7. Geometric Shapes & Stars (Hình khối & Biểu tượng)
            Subsets["Geometric Shapes (Hình khối & Biểu tượng)"] = new string[] {
                "■", "□", "▢", "▣", "▤", "▥", "▦", "▧", "▨", "▩", "▪", "▫", "▬", "▭", "▮", "▯",
                "▲", "△", "▴", "▵", "▶", "▷", "▸", "▹", "►", "▼", "▽", "▾", "▿", "◀", "◁", "◂",
                "◃", "◄", "◆", "◇", "◈", "◉", "◊", "○", "◌", "◍", "◎", "●", "◐", "◑", "◒", "◓",
                "★", "☆", "✦", "✧", "✩", "✪", "✫", "✬", "✭", "✮", "✯", "✰", "⭐", "🌟", "✓", "✔",
                "✕", "✖", "✗", "✘", "✚", "✛", "✜", "✝", "✞", "✟", "✠", "✡", "♠", "♣", "♥", "♦",
                "♪", "♫", "♬", "♭", "♮", "♯", "☺", "☻", "☼", "☽", "♀", "♂", "☎", "✉", "✂", "✈"
            };

            // 8. Box Drawing & Enclosed (Khung viền bảng biểu)
            Subsets["Box Drawing (Khung viền bảng biểu)"] = new string[] {
                "─", "━", "│", "┃", "┄", "┅", "┆", "┇", "┈", "┉", "┊", "┋", "┌", "┍", "┎", "┏",
                "┐", "┑", "┒", "┓", "└", "┕", "┖", "┗", "┘", "┙", "┚", "┛", "├", "┝", "┞", "┟",
                "┠", "┡", "┢", "┣", "┤", "┥", "┦", "┧", "┨", "┩", "┪", "┫", "┬", "┭", "┮", "┯",
                "┰", "┱", "┲", "┳", "┴", "┵", "┶", "┷", "┸", "┹", "┺", "┻", "┼", "┽", "┾", "┿",
                "═", "║", "╒", "╓", "╔", "╕", "╖", "╗", "╘", "╙", "╚", "╛", "╜", "╝", "╞", "╟",
                "╠", "╡", "╢", "╣", "╤", "╥", "╦", "╧", "╨", "╩", "╪", "╫", "╬", "╭", "╮", "╯"
            };

            // 9. Special Characters (Ký tự đặc biệt)
            Subsets["Special Characters (Ký tự đặc biệt)"] = new string[] {
                "—", "–", "―", "…", "‘", "’", "‚", "“", "”", "„", "′", "″", "‴", "‵", "‹", "›",
                "«", "»", "§", "¶", "†", "‡", "•", "‰", "‱", "※", "‼", "⁇", "⁈", "⁉", "‿", "⁀"
            };

            // Names map for Euro, Dollar, etc.
            SymbolNames["€"] = "Euro Sign";
            SymbolNames["$"] = "Dollar Sign";
            SymbolNames["£"] = "Pound Sign";
            SymbolNames["¥"] = "Yen Sign";
            SymbolNames["₫"] = "Dong Sign (Việt Nam)";
            SymbolNames["©"] = "Copyright Sign";
            SymbolNames["®"] = "Registered Sign";
            SymbolNames["™"] = "Trade Mark Sign";
            SymbolNames["±"] = "Plus-Minus Sign";
            SymbolNames["≠"] = "Not Equal To";
            SymbolNames["≤"] = "Less-Than or Equal To";
            SymbolNames["≥"] = "Greater-Than or Equal To";
            SymbolNames["÷"] = "Division Sign";
            SymbolNames["×"] = "Multiplication Sign";
            SymbolNames["∞"] = "Infinity";
            SymbolNames["µ"] = "Micro Sign";
            SymbolNames["α"] = "Greek Small Letter Alpha";
            SymbolNames["β"] = "Greek Small Letter Beta";
            SymbolNames["π"] = "Greek Small Letter Pi";
            SymbolNames["Ω"] = "Greek Capital Letter Omega";
            SymbolNames["∑"] = "N-Ary Summation";
            SymbolNames["√"] = "Square Root";
            SymbolNames["∫"] = "Integral";
        }

        private void InitControls()
        {
            TabControl tabCtrl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(540, 395)
            };

            TabPage tabSymbols = new TabPage { Text = "Ký hiệu (Symbols)", BackColor = Color.White };
            TabPage tabSpecial = new TabPage { Text = "Ký tự đặc biệt (Special Characters)", BackColor = Color.White };

            // Controls on tabSymbols
            Label lblF = new Label { Text = "Phông chữ (Font):", Location = new Point(15, 12), AutoSize = true, Font = new Font("Segoe UI", 9) };
            tabSymbols.Controls.Add(lblF);

            cbFont = new ComboBox { Location = new Point(135, 9), Size = new Size(130, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cbFont.Items.AddRange(new object[] { "Calibri", "Segoe UI Symbol", "Arial", "Times New Roman", "(normal text)" });
            cbFont.SelectedIndex = 0;
            cbFont.SelectedIndexChanged += (s, e) => pnlGrid.Invalidate();
            tabSymbols.Controls.Add(cbFont);

            Label lblS = new Label { Text = "Phân nhóm (Subset):", Location = new Point(275, 12), AutoSize = true, Font = new Font("Segoe UI", 9) };
            tabSymbols.Controls.Add(lblS);

            cbSubset = new ComboBox { Location = new Point(395, 9), Size = new Size(130, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var key in Subsets.Keys) cbSubset.Items.Add(key);
            cbSubset.SelectedIndex = 0;
            cbSubset.SelectedIndexChanged += (s, e) =>
            {
                LoadSubsetSymbols((string)cbSubset.SelectedItem);
            };
            tabSymbols.Controls.Add(cbSubset);

            // Symbol Grid Panel (Double Buffered - No Flickering)
            pnlGrid = new DoubleBufferedPanel
            {
                Location = new Point(15, 40),
                Size = new Size(505, 185),
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

            // Recently used label & paint
            Label lblRecent = new Label { Text = "Ký hiệu dùng gần đây (Recently used symbols):", Location = new Point(15, 235), AutoSize = true, Font = new Font("Segoe UI", 9) };
            tabSymbols.Controls.Add(lblRecent);

            DoubleBufferedPanel pnlRecent = new DoubleBufferedPanel
            {
                Location = new Point(15, 255),
                Size = new Size(505, 34),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRecent.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using (Font symFont = new Font(cbFont.Text == "(normal text)" ? "Segoe UI" : cbFont.Text, 11f))
                {
                    for (int i = 0; i < Math.Min(16, RecentlyUsed.Count); i++)
                    {
                        int x = i * 31;
                        Rectangle rect = new Rectangle(x, 0, 31, 32);

                        bool isSel = (RecentSelectedIndex == i);
                        bool isHov = (RecentHoveredIndex == i);

                        if (isSel)
                        {
                            g.FillRectangle(Brushes.SteelBlue, rect);
                        }
                        else if (isHov)
                        {
                            using (SolidBrush hb = new SolidBrush(Color.FromArgb(235, 243, 255))) g.FillRectangle(hb, rect);
                            g.DrawRectangle(Pens.SteelBlue, rect);
                        }

                        g.DrawLine(Pens.LightGray, x + 31, 0, x + 31, 32);

                        SizeF sz = g.MeasureString(RecentlyUsed[i], symFont);
                        Brush b = isSel ? Brushes.White : Brushes.Black;
                        g.DrawString(RecentlyUsed[i], symFont, b, x + (31 - sz.Width) / 2, (32 - sz.Height) / 2);
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

            // Unicode info section
            lblCharName = new Label
            {
                Text = "Tên: Euro Sign",
                Location = new Point(15, 300),
                Size = new Size(250, 20),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            tabSymbols.Controls.Add(lblCharName);

            Label lblCode = new Label { Text = "Mã ký tự (hex):", Location = new Point(280, 300), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            tabSymbols.Controls.Add(lblCode);

            txtCharCode = new TextBox { Location = new Point(365, 297), Size = new Size(60, 22), ReadOnly = true, Text = "20AC" };
            tabSymbols.Controls.Add(txtCharCode);

            Label lblFrom = new Label { Text = "từ: Unicode (hex)", Location = new Point(430, 300), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            tabSymbols.Controls.Add(lblFrom);

            // Special Characters list on tabSpecial
            ListView lvSpecial = new ListView
            {
                Location = new Point(15, 15),
                Size = new Size(505, 335),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9.5f)
            };
            lvSpecial.Columns.Add("Ký tự", 80);
            lvSpecial.Columns.Add("Tên Ký tự đặc biệt (Special Character)", 300);
            lvSpecial.Columns.Add("Phím tắt", 110);

            string[,] specials = new string[,] {
                { "—", "Em Dash (Gạch ngang dài)", "Alt+Ctrl+Num -" },
                { "–", "En Dash (Gạch ngang ngắn)", "Ctrl+Num -" },
                { "©", "Copyright (Bản quyền)", "Alt+Ctrl+C" },
                { "®", "Registered (Thương hiệu đăng ký)", "Alt+Ctrl+R" },
                { "™", "Trademark (Nhãn hiệu)", "Alt+Ctrl+T" },
                { "§", "Section (Mục đoạn)", "" },
                { "¶", "Paragraph (Dấu đoạn văn)", "" },
                { "…", "Ellipsis (Dấu ba chấm)", "Alt+Ctrl+." },
                { "‘", "Single Opening Quote (Ngoặc đơn mở)", "Ctrl+`, `" },
                { "’", "Single Closing Quote (Ngoặc đơn đóng)", "Ctrl+', '" },
                { "“", "Double Opening Quote (Ngoặc kép mở)", "Ctrl+`, \"" },
                { "”", "Double Closing Quote (Ngoặc kép đóng)", "Ctrl+', \"" },
                { "€", "Euro Sign (Đồng Euro)", "Alt+Ctrl+E" },
                { "°", "Degree Sign (Ký hiệu độ)", "" },
                { "µ", "Micro Sign (Micro)", "" },
                { "±", "Plus-Minus (Cộng trừ)", "" }
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

            // Bottom Buttons
            btnInsert = new Button
            {
                Text = "Chèn (Insert)",
                Location = new Point(360, 425),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(43, 87, 154),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
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
                Text = "Đóng",
                Location = new Point(468, 425),
                Size = new Size(84, 32),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
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
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            string fontName = cbFont.Text == "(normal text)" ? "Segoe UI" : cbFont.Text;
            using (Font symFont = new Font(fontName, 12f))
            {
                for (int i = 0; i < CurrentSymbols.Count; i++)
                {
                    int c = i % 16;
                    int r = i / 16;
                    int x = c * 31;
                    int y = r * 30 + pnlGrid.AutoScrollPosition.Y;
                    Rectangle rect = new Rectangle(x, y, 31, 30);

                    bool isSel = (SelectedIndex == i && RecentSelectedIndex == -1);
                    bool isHov = (HoveredIndex == i);

                    if (isSel)
                    {
                        g.FillRectangle(Brushes.SteelBlue, rect);
                    }
                    else if (isHov)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(235, 243, 255))) g.FillRectangle(hb, rect);
                        g.DrawRectangle(Pens.SteelBlue, rect);
                    }

                    g.DrawLine(Pens.LightGray, x + 31, y, x + 31, y + 30);
                    g.DrawLine(Pens.LightGray, x, y + 30, x + 31, y + 30);

                    SizeF sz = g.MeasureString(CurrentSymbols[i], symFont);
                    Brush b = isSel ? Brushes.White : Brushes.Black;
                    g.DrawString(CurrentSymbols[i], symFont, b, x + (31 - sz.Width) / 2, y + (30 - sz.Height) / 2);
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
            lblCharName.Text = "Tên: Ký tự: " + name;

            if (sym.Length > 0)
            {
                int code = char.ConvertToUtf32(sym, 0);
                txtCharCode.Text = code.ToString("X4");
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
