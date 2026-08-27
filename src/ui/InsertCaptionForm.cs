using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class CaptionItem
    {
        public string Id { get; set; }
        public string LabelType { get; set; } // Hình, Bảng, Phương trình
        public int Number { get; set; }
        public string Title { get; set; }
        public int AnchorChar { get; set; }
        public int PageNumber { get; set; }
        public bool IsBelow { get; set; }

        public CaptionItem()
        {
            Id = "CAP_" + DateTime.Now.Ticks.ToString().Substring(10);
            LabelType = "Hình";
            Number = 1;
            Title = "";
            AnchorChar = 0;
            PageNumber = 1;
            IsBelow = true;
        }

        public string GetFullCaption()
        {
            if (string.IsNullOrEmpty(Title))
            {
                return string.Format("{0} {1}", LabelType, Number);
            }
            return string.Format("{0} {1}: {2}", LabelType, Number, Title);
        }
    }

    public class InsertCaptionForm : Form
    {
        public string GeneratedCaption { get; private set; }
        public string SelectedLabelType { get; private set; }
        public int CaptionNumber { get; private set; }
        public string CaptionTitle { get; private set; }
        public bool IsBelowPosition { get; private set; }

        private ComboBox cboLabel;
        private ComboBox cboPosition;
        private TextBox txtTitle;
        private Label lblPreview;
        private WriterView _writerView;

        public InsertCaptionForm(WriterView writerView = null, string defaultLabel = "Hình")
        {
            _writerView = writerView;

            this.Text = "Chèn Phụ Đề (Insert Caption)";
            this.Size = new Size(460, 310);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            BuildUI(defaultLabel);
            UpdatePreview();
        }

        private void BuildUI(string defaultLabel)
        {
            Label lblCap = new Label { Text = "Tên phụ đề (Caption):", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblCap);

            txtTitle = new TextBox { Location = new Point(20, 38), Size = new Size(405, 24) };
            txtTitle.TextChanged += (s, e) => UpdatePreview();
            this.Controls.Add(txtTitle);

            Label lblLbl = new Label { Text = "Nhãn (Label):", Location = new Point(20, 75), AutoSize = true };
            this.Controls.Add(lblLbl);

            cboLabel = new ComboBox { Location = new Point(20, 96), Width = 195, DropDownStyle = ComboBoxStyle.DropDownList };
            cboLabel.Items.AddRange(new object[] { "Hình (Figure)", "Bảng (Table)", "Phương trình (Equation)" });
            if (defaultLabel == "Bảng") cboLabel.SelectedIndex = 1;
            else if (defaultLabel == "Phương trình") cboLabel.SelectedIndex = 2;
            else cboLabel.SelectedIndex = 0;
            cboLabel.SelectedIndexChanged += (s, e) => UpdatePreview();
            this.Controls.Add(cboLabel);

            Label lblPos = new Label { Text = "Vị trí (Position):", Location = new Point(230, 75), AutoSize = true };
            this.Controls.Add(lblPos);

            cboPosition = new ComboBox { Location = new Point(230, 96), Width = 195, DropDownStyle = ComboBoxStyle.DropDownList };
            cboPosition.Items.AddRange(new object[] { "Dưới mục đã chọn (Below)", "Trên mục đã chọn (Above)" });
            cboPosition.SelectedIndex = 0;
            this.Controls.Add(cboPosition);

            // Preview box
            GroupBox grpPrev = new GroupBox
            {
                Text = "Xem trước phụ đề hiển thị:",
                Location = new Point(20, 132),
                Size = new Size(405, 68),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            this.Controls.Add(grpPrev);

            lblPreview = new Label
            {
                Text = "Hình 1:",
                Location = new Point(12, 26),
                Size = new Size(380, 28),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(43, 87, 154)
            };
            grpPrev.Controls.Add(lblPreview);

            // Bottom bar
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOk = new Button
            {
                Text = "Chèn Phụ Đề",
                Location = new Point(225, 10),
                Size = new Size(115, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                int count = GetNextNumberForLabel(GetPureLabel());
                SelectedLabelType = GetPureLabel();
                CaptionNumber = count;
                CaptionTitle = txtTitle.Text.Trim();
                IsBelowPosition = cboPosition.SelectedIndex == 0;
                GeneratedCaption = string.IsNullOrEmpty(CaptionTitle)
                    ? string.Format("{0} {1}", SelectedLabelType, CaptionNumber)
                    : string.Format("{0} {1}: {2}", SelectedLabelType, CaptionNumber, CaptionTitle);
            };
            bottomPanel.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(350, 10),
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

        private string GetPureLabel()
        {
            if (cboLabel.SelectedIndex == 1) return "Bảng";
            if (cboLabel.SelectedIndex == 2) return "Phương trình";
            return "Hình";
        }

        private int GetNextNumberForLabel(string pureLabel)
        {
            int count = 1;
            if (_writerView != null && _writerView.CaptionsList != null)
            {
                foreach (var c in _writerView.CaptionsList)
                {
                    if (c.LabelType == pureLabel) count++;
                }
            }
            return count;
        }

        private void UpdatePreview()
        {
            string pureLabel = GetPureLabel();
            int count = GetNextNumberForLabel(pureLabel);
            string title = txtTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                lblPreview.Text = string.Format("{0} {1}", pureLabel, count);
            }
            else
            {
                lblPreview.Text = string.Format("{0} {1}: {2}", pureLabel, count, title);
            }
        }
    }
}
