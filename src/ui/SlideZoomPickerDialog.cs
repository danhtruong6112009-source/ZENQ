using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class SlideZoomPickerDialog : Form
    {
        private PresentationRoot _presentation;
        private bool _isSectionMode;
        private ListView _listView;
        private Button _btnSelectAll;
        private Button _btnClearAll;
        private Button _btnInsert;
        private Button _btnCancel;
        private Label _lblHeader;

        public List<int> SelectedSlideIndices { get; } = new List<int>();

        public SlideZoomPickerDialog(PresentationRoot presentation, bool isSectionMode = false)
        {
            _presentation = presentation;
            _isSectionMode = isSectionMode;

            InitializeComponent();
            PopulateSlides();
        }

        private void InitializeComponent()
        {
            Text = _isSectionMode ? "Chèn Thu phóng Phân phần (Insert Section Zoom)" : "Chèn Thu phóng Trang chiếu (Insert Slide Zoom)";
            Width = 580;
            Height = 480;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            _lblHeader = new Label
            {
                Text = _isSectionMode
                    ? "Chọn một hoặc nhiều phân phần để chèn thẻ thu phóng phân phần vào trang chiếu hiện tại:"
                    : "Chọn một hoặc nhiều trang chiếu để chèn thẻ thu phóng trực tiếp vào trang chiếu hiện tại:",
                Location = new Point(16, 14),
                Size = new Size(530, 36),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            Controls.Add(_lblHeader);

            _listView = new ListView
            {
                Location = new Point(16, 56),
                Size = new Size(532, 320),
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            _listView.Columns.Add("Chọn", 50);
            _listView.Columns.Add("Slide #", 65);
            _listView.Columns.Add(_isSectionMode ? "Tên Phân phần (Section)" : "Tiêu đề Trang chiếu (Title)", 300);
            _listView.Columns.Add("Số đối tượng", 95);
            Controls.Add(_listView);

            _btnSelectAll = new Button
            {
                Text = "Chọn tất cả",
                Location = new Point(16, 390),
                Size = new Size(95, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            _btnSelectAll.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnSelectAll.Click += (s, e) =>
            {
                foreach (ListViewItem item in _listView.Items)
                {
                    item.Checked = true;
                }
            };
            Controls.Add(_btnSelectAll);

            _btnClearAll = new Button
            {
                Text = "Bỏ chọn",
                Location = new Point(118, 390),
                Size = new Size(85, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            _btnClearAll.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnClearAll.Click += (s, e) =>
            {
                foreach (ListViewItem item in _listView.Items)
                {
                    item.Checked = false;
                }
            };
            Controls.Add(_btnClearAll);

            _btnInsert = new Button
            {
                Text = "Chèn (Insert)",
                Location = new Point(345, 390),
                Size = new Size(110, 32),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(210, 71, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _btnInsert.FlatAppearance.BorderSize = 0;
            _btnInsert.Click += (s, e) =>
            {
                SelectedSlideIndices.Clear();
                foreach (ListViewItem item in _listView.Items)
                {
                    if (item.Checked && item.Tag is int idx)
                    {
                        SelectedSlideIndices.Add(idx);
                    }
                }
                if (SelectedSlideIndices.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một trang chiếu hoặc phân phần để chèn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.None;
                    return;
                }
            };
            Controls.Add(_btnInsert);

            _btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(463, 390),
                Size = new Size(85, 32),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            Controls.Add(_btnCancel);

            AcceptButton = _btnInsert;
            CancelButton = _btnCancel;
        }

        private void PopulateSlides()
        {
            if (_presentation == null || _presentation.Slides == null) return;

            _listView.Items.Clear();
            for (int i = 0; i < _presentation.Slides.Count; i++)
            {
                SlideNode slide = _presentation.Slides[i];

                if (_isSectionMode)
                {
                    if (!slide.IsSectionHeader && !(i == 0 && string.IsNullOrEmpty(slide.SectionName)))
                    {
                        continue;
                    }
                }

                string slideNum = (i + 1).ToString();
                string title = !string.IsNullOrEmpty(slide.SectionName)
                    ? slide.SectionName
                    : (!string.IsNullOrEmpty(slide.Title) ? slide.Title : $"Trang chiếu {i + 1}");

                int shapeCount = slide.Shapes != null ? slide.Shapes.Count : 0;

                ListViewItem item = new ListViewItem("");
                item.Tag = i;
                item.SubItems.Add(slideNum);
                item.SubItems.Add(title);
                item.SubItems.Add(shapeCount + " hình");

                _listView.Items.Add(item);
            }

            if (_listView.Items.Count > 0)
            {
                _listView.Items[0].Checked = true;
            }
        }
    }
}
