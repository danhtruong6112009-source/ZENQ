using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class InsertSymbolForm : Form
    {
        public string SelectedSymbol { get; private set; }
        private FlowLayoutPanel pnlSymbols;
        private Label lblPreview;

        private static readonly string[] SymbolsList = new string[]
        {
            // Greek
            "α", "β", "γ", "δ", "ε", "θ", "λ", "μ", "π", "σ", "φ", "ω", "Δ", "Σ", "Ω", "Φ",
            // Math
            "±", "×", "÷", "≠", "≤", "≥", "≈", "∞", "√", "∑", "∏", "∫", "∂", "∇", "∈", "∉", "⊂", "⊆", "∪", "∩", "∠", "⊥", "°", "‰",
            // Currency & Legal
            "$", "€", "¥", "£", "₫", "₩", "₹", "₽", "¢", "©", "®", "™", "§", "¶", "†", "‡",
            // Arrows & Shapes
            "←", "→", "↑", "↓", "↔", "↕", "⇒", "⇔", "▲", "▼", "◄", "►", "■", "□", "●", "○", "★", "☆", "✓", "✗", "♥", "♦"
        };

        public InsertSymbolForm()
        {
            this.Text = "Ký Hiệu Đặc Biệt (Symbols)";
            this.Size = new Size(520, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            BuildUI();
        }

        private void BuildUI()
        {
            Label lblTitle = new Label
            {
                Text = "Chọn ký hiệu để chèn vào vị trí đang chọn:",
                Location = new Point(15, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            this.Controls.Add(lblTitle);

            pnlSymbols = new FlowLayoutPanel
            {
                Location = new Point(15, 35),
                Size = new Size(475, 235),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            this.Controls.Add(pnlSymbols);

            foreach (var sym in SymbolsList)
            {
                Button btnSym = new Button
                {
                    Text = sym,
                    Size = new Size(34, 34),
                    Font = new Font("Segoe UI", 11.0f, FontStyle.Regular),
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(2)
                };
                btnSym.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
                string captureSym = sym;
                btnSym.Click += (s, e) =>
                {
                    SelectedSymbol = captureSym;
                    lblPreview.Text = "Ký hiệu đã chọn: " + captureSym;
                };
                btnSym.DoubleClick += (s, e) =>
                {
                    SelectedSymbol = captureSym;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                pnlSymbols.Controls.Add(btnSym);
            }

            lblPreview = new Label
            {
                Text = "Ký hiệu đã chọn: (chưa chọn)",
                Location = new Point(15, 280),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 124, 65)
            };
            this.Controls.Add(lblPreview);

            // Bottom Buttons
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnInsert = new Button
            {
                Text = "Chèn",
                Location = new Point(290, 10),
                Size = new Size(95, 28),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnInsert.FlatAppearance.BorderSize = 0;
            bottomPanel.Controls.Add(btnInsert);

            Button btnCancel = new Button
            {
                Text = "Đóng",
                Location = new Point(395, 10),
                Size = new Size(80, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnInsert;
            this.CancelButton = btnCancel;
        }
    }
}
