using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class SmartArtEditorDialog : Form
    {
        private string _smartArtType;
        private List<TextBox> _nodeTextBoxes = new List<TextBox>();
        private FlowLayoutPanel _pnlNodes;
        private ComboBox _cboColorTheme;
        private Action<string, int> _onSave;

        public SmartArtEditorDialog(string smartArtType, string currentText, int currentColorPreset, Action<string, int> onSave)
        {
            _smartArtType = smartArtType ?? "Cycle_Radial";
            _onSave = onSave;

            InitializeUI(currentText, currentColorPreset);
        }

        private void InitializeUI(string currentText, int currentColorPreset)
        {
            Text = "✏️ Chỉnh sửa nội dung & Màu sắc SmartArt";
            Size = new Size(520, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Header Banner
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(15, 23, 42) };
            Label lblTitle = new Label
            {
                Text = "❖ Chỉnh sửa Sơ đồ SmartArt (" + GetSmartArtDisplayName(_smartArtType) + ")",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            pnlHeader.Controls.Add(lblTitle);
            Controls.Add(pnlHeader);

            // Color Preset Selector Panel
            Panel pnlColor = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(16, 8, 16, 4) };
            Label lblColor = new Label { Text = "🎨 Bảng màu sắc:", AutoSize = true, Location = new Point(16, 12), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            _cboColorTheme = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(140, 9),
                Width = 330,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
            _cboColorTheme.Items.Add("🟠 Cam & Vàng (Office Warm)");
            _cboColorTheme.Items.Add("🔵 Xanh Lam Chuẩn (Office Blue)");
            _cboColorTheme.Items.Add("🌈 Đa sắc màu (Colorful Accent)");
            _cboColorTheme.Items.Add("🟢 Xanh Lục (Emerald Green)");
            _cboColorTheme.Items.Add("🟣 Tím & Hồng (Purple & Magenta)");
            _cboColorTheme.Items.Add("⚫ Đơn sắc (Monochrome Gray)");
            _cboColorTheme.SelectedIndex = Math.Max(0, Math.Min(5, currentColorPreset));
            pnlColor.Controls.Add(lblColor);
            pnlColor.Controls.Add(_cboColorTheme);
            Controls.Add(pnlColor);

            // Actions Toolbar (Add step / Delete step)
            Panel pnlActions = new Panel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(16, 4, 16, 4) };
            Label lblNodes = new Label { Text = "Danh sách các khối / bước:", AutoSize = true, Location = new Point(16, 8), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            Button btnAdd = new Button { Text = "➕ Thêm bước", Location = new Point(270, 4), Size = new Size(105, 28), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat };
            Button btnRemove = new Button { Text = "➖ Xóa bớt", Location = new Point(385, 4), Size = new Size(90, 28), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat };
            btnAdd.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnRemove.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);

            btnAdd.Click += (s, e) => { AddNodeRow("Bước mới " + (_nodeTextBoxes.Count + 1)); };
            btnRemove.Click += (s, e) => {
                if (_nodeTextBoxes.Count > 2)
                {
                    int lastIdx = _nodeTextBoxes.Count - 1;
                    _pnlNodes.Controls.RemoveAt(lastIdx);
                    _nodeTextBoxes.RemoveAt(lastIdx);
                }
            };
            pnlActions.Controls.Add(lblNodes);
            pnlActions.Controls.Add(btnAdd);
            pnlActions.Controls.Add(btnRemove);
            Controls.Add(pnlActions);

            // Flow Panel for Nodes
            _pnlNodes = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(16, 6, 16, 6)
            };
            Controls.Add(_pnlNodes);

            // Populate existing nodes
            string[] rawNodes = SmartArtRenderer.GetSmartArtNodeList(_smartArtType, currentText);
            foreach (var node in rawNodes)
            {
                AddNodeRow(node);
            }

            // Bottom Buttons
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(16, 10, 16, 10) };
            Button btnSave = new Button
            {
                Text = "✓ Lưu & Áp dụng",
                DialogResult = DialogResult.OK,
                Size = new Size(130, 34),
                Location = new Point(220, 10),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                List<string> resultNodes = new List<string>();
                for (int i = 0; i < _nodeTextBoxes.Count; i++)
                {
                    string t = _nodeTextBoxes[i].Text.Trim();
                    if (!string.IsNullOrEmpty(t)) resultNodes.Add(t);
                }
                string combined = string.Join("|", resultNodes.ToArray());
                int selectedColor = _cboColorTheme.SelectedIndex;
                if (_onSave != null) _onSave(combined, selectedColor);
                Close();
            };

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                DialogResult = DialogResult.Cancel,
                Size = new Size(90, 34),
                Location = new Point(360, 10),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (s, e) => Close();

            pnlBottom.Controls.Add(btnSave);
            pnlBottom.Controls.Add(btnCancel);
            Controls.Add(pnlBottom);

            // Proper Docking order
            Controls.SetChildIndex(pnlBottom, 0);
            Controls.SetChildIndex(_pnlNodes, 1);
            Controls.SetChildIndex(pnlActions, 2);
            Controls.SetChildIndex(pnlColor, 3);
            Controls.SetChildIndex(pnlHeader, 4);
        }

        private void AddNodeRow(string text)
        {
            int idx = _nodeTextBoxes.Count + 1;
            Panel row = new Panel { Width = 460, Height = 36, Margin = new Padding(0, 0, 0, 4) };
            Label lbl = new Label { Text = "Mục " + idx + ":", Width = 65, Location = new Point(0, 7), Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            TextBox txt = new TextBox { Text = text, Location = new Point(70, 4), Width = 385, Font = new Font("Segoe UI", 10f, FontStyle.Regular) };
            
            row.Controls.Add(lbl);
            row.Controls.Add(txt);
            _nodeTextBoxes.Add(txt);
            _pnlNodes.Controls.Add(row);
        }

        private static string GetSmartArtDisplayName(string type)
        {
            if (type == "Cycle_Radial") return "Vòng tròn Tuần hoàn";
            if (type == "Process_Linear") return "Quy trình Tuyến tính";
            if (type == "Process_Chevron") return "Mũi tên Khối Chevron";
            if (type == "Hierarchy_Tree") return "Sơ đồ Cây Tổ chức";
            if (type == "List_Blocks") return "Danh sách Khối";
            if (type == "Matrix_Grid") return "Ma trận 4 Ô";
            if (type == "Pyramid_Layers") return "Kim tự tháp Phân tầng";
            return "Sơ đồ";
        }
    }
}
