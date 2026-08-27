using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class SmartArtGalleryForm : Form
    {
        public string SelectedSmartArtType { get; private set; }
        public string SelectedSmartArtName { get; private set; }

        private ListBox lstCategories;
        private Panel previewPanel;
        private int selectedIndex = 0;

        private struct SmartArtItem
        {
            public string Category;
            public string TypeId;
            public string Name;
            public string Description;
            public SmartArtItem(string cat, string id, string name, string desc)
            {
                Category = cat; TypeId = id; Name = name; Description = desc;
            }
        }

        private SmartArtItem[] items = new SmartArtItem[]
        {
            new SmartArtItem("Quy trình", "Process_Linear", "Quy trình Tuyến tính", "Hiển thị tiến trình các bước nối tiếp theo chiều ngang (Bước 1 ➔ Bước 2 ➔ Bước 3)."),
            new SmartArtItem("Quy trình", "Process_Chevron", "Mũi tên Khối Quy trình", "Dùng để nhấn mạnh định hướng và lộ trình chuyển đổi qua từng pha."),
            new SmartArtItem("Vòng tròn", "Cycle_Radial", "Sơ đồ Vòng tròn Tuần hoàn", "Mô tả chu kỳ lặp lại hoặc vòng đời sản phẩm gồm 4 giai đoạn."),
            new SmartArtItem("Phân cấp", "Hierarchy_Tree", "Sơ đồ Cây Tổ chức (Org Chart)", "Biểu diễn cơ cấu nhân sự từ Cấp trên ➔ Trưởng phòng ➔ Nhân viên."),
            new SmartArtItem("Danh sách", "List_Blocks", "Danh sách Khối Nổi bật", "Nhóm các ý tưởng hoặc tính năng quan trọng theo thẻ màu gradient."),
            new SmartArtItem("Ma trận", "Matrix_Grid", "Ma trận 4 Ô (SWOT / BCG)", "Phân tích 4 góc phần tư đối chiếu 2 trục tiêu chí quan trọng."),
            new SmartArtItem("Kim tự tháp", "Pyramid_Layers", "Kim tự tháp Phân tầng", "Mô tả cấu trúc tầng bậc từ nền tảng dưới đáy đến đỉnh cao.")
        };

        public SmartArtGalleryForm()
        {
            this.Text = "Chọn Đồ Họa SmartArt (SmartArt Graphics)";
            this.Size = new Size(620, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            // Left Category List
            lstCategories = new ListBox { Location = new Point(16, 16), Size = new Size(160, 330), BorderStyle = BorderStyle.FixedSingle };
            foreach (var it in items)
            {
                lstCategories.Items.Add(it.Name);
            }
            lstCategories.SelectedIndex = 0;
            lstCategories.SelectedIndexChanged += (s, e) =>
            {
                selectedIndex = lstCategories.SelectedIndex;
                previewPanel.Invalidate();
            };
            this.Controls.Add(lstCategories);

            // Right Preview Panel
            previewPanel = new Panel { Location = new Point(190, 16), Size = new Size(400, 330), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            previewPanel.Paint += PreviewPanel_Paint;
            this.Controls.Add(previewPanel);

            // Bottom Buttons
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOk = new Button
            {
                Text = "Chèn (OK)",
                Location = new Point(415, 10),
                Size = new Size(85, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (selectedIndex >= 0 && selectedIndex < items.Length)
                {
                    SelectedSmartArtType = items[selectedIndex].TypeId;
                    SelectedSmartArtName = items[selectedIndex].Name;
                }
            };
            bottomPanel.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(508, 10),
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

        private void PreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (selectedIndex < 0 || selectedIndex >= items.Length) return;
            var it = items[selectedIndex];

            using (Font titleF = new Font("Segoe UI", 11.0f, FontStyle.Bold))
            using (Font descF = new Font("Segoe UI", 8.5f))
            {
                g.DrawString(it.Name, titleF, new SolidBrush(Color.FromArgb(0, 51, 102)), 16, 12);
                g.DrawString(it.Description, descF, Brushes.DimGray, new RectangleF(16, 36, 368, 38));
                g.DrawLine(Pens.LightGray, 16, 78, 384, 78);
            }

            Rectangle drawArea = new Rectangle(16, 92, 368, 225);
            SmartArtRenderer.DrawSmartArt(g, drawArea, it.TypeId, it.Name, 0, "", false, 0.92f);
        }
    }
}
