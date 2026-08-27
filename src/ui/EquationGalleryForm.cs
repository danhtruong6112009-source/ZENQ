using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class EquationGalleryForm : Form
    {
        public string SelectedEquationText { get; private set; }

        private ListBox lstEquations;

        private struct EquationEntry
        {
            public string Name;
            public string Formula;
            public string Description;
            public EquationEntry(string n, string f, string d)
            {
                Name = n; Formula = f; Description = d;
            }
        }

        private EquationEntry[] equations = new EquationEntry[]
        {
            new EquationEntry("Diện tích hình tròn", "A = πr²", "Công thức tính diện tích hình tròn bán kính r."),
            new EquationEntry("Phương trình bậc hai", "x = (-b ± √(b² - 4ac)) / (2a)", "Công thức nghiệm phương trình bậc hai ax² + bx + c = 0."),
            new EquationEntry("Định lý Pythagoras", "a² + b² = c²", "Mối quan hệ giữa 3 cạnh trong tam giác vuông."),
            new EquationEntry("Đẳng thức Euler", "e^(iπ) + 1 = 0", "Mối liên hệ giữa 5 hằng số toán học cơ bản."),
            new EquationEntry("Định lý Nhị thức Newton", "(x + a)ⁿ = ∑ₖ₌₀ⁿ (ⁿₖ) xᵏ aⁿ⁻ᵏ", "Khai triển lũy thừa bậc n của tổng hai số hạng."),
            new EquationEntry("Tích phân Fourier", "f(x) = a₀ + ∑ₙ₌₁^∞ (aₙ cos(nx) + bₙ sin(nx))", "Khai triển hàm tuần hoàn thành chuỗi lượng giác."),
            new EquationEntry("Tích phân xác định", "∫ₐᵇ f(x) dx = F(b) - F(a)", "Định lý cơ bản của Giải tích vi tích phân."),
            new EquationEntry("Định luật Vạn vật Hấp dẫn", "F = G * (m₁ * m₂) / r²", "Lực hấp dẫn giữa hai vật thể khối lượng m₁, m₂ cách nhau khoảng cách r."),
            new EquationEntry("Phương trình Einstein", "E = mc²", "Định luật bảo toàn khối lượng - năng lượng tương đối tính."),
            new EquationEntry("Ma trận Định thức 2x2", "det(A) = | a  b | = ad - bc\n         | c  d |", "Công thức tính định thức ma trận vuông cấp 2.")
        };

        public EquationGalleryForm()
        {
            this.Text = "Chèn Công Thức Toán Học (Insert Equation)";
            this.Size = new Size(540, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            Label lblHeading = new Label { Text = "Chọn công thức toán học từ thư viện:", Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblHeading);

            lstEquations = new ListBox { Location = new Point(20, 38), Size = new Size(485, 235), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.0f) };
            foreach (var eq in equations)
            {
                lstEquations.Items.Add(string.Format("{0}:   {1}", eq.Name, eq.Formula.Replace("\n", " ")));
            }
            lstEquations.SelectedIndex = 0;
            this.Controls.Add(lstEquations);

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOk = new Button
            {
                Text = "Chèn Công Thức",
                Location = new Point(300, 10),
                Size = new Size(125, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (lstEquations.SelectedIndex >= 0 && lstEquations.SelectedIndex < equations.Length)
                {
                    SelectedEquationText = equations[lstEquations.SelectedIndex].Formula;
                }
            };
            bottomPanel.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(430, 10),
                Size = new Size(75, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
