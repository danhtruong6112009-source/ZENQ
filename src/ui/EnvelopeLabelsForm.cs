using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class EnvelopeLabelsForm : Form
    {
        private TabControl tabControl;
        private TabPage tabEnvelopes;
        private TabPage tabLabels;

        // Envelope fields
        private TextBox txtDeliveryAddress;
        private TextBox txtReturnAddress;
        private ComboBox cboEnvelopeSize;
        private Button btnAddEnvelopeToDoc;
        private Button btnPrintEnvelope;

        // Label fields
        private TextBox txtLabelText;
        private ComboBox cboLabelLayout;
        private Button btnCreateLabelsTable;

        private WriterView _writerView;

        public EnvelopeLabelsForm(int initialTab, WriterView writerView)
        {
            _writerView = writerView;
            InitializeComponent();
            if (initialTab >= 0 && initialTab < tabControl.TabPages.Count)
            {
                tabControl.SelectedIndex = initialTab;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Phong bì và Nhãn (Envelopes and Labels)";
            this.Size = new Size(540, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 9.2f, FontStyle.Regular);

            // ==================== TAB 1: PHONG BÌ ====================
            tabEnvelopes = new TabPage("✉ Phong bì (Envelopes)");
            tabEnvelopes.BackColor = Color.White;

            Label lblDeliv = new Label();
            lblDeliv.Text = "Địa chỉ người nhận (Delivery address):";
            lblDeliv.Location = new Point(16, 14);
            lblDeliv.AutoSize = true;
            lblDeliv.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            tabEnvelopes.Controls.Add(lblDeliv);

            txtDeliveryAddress = new TextBox();
            txtDeliveryAddress.Multiline = true;
            txtDeliveryAddress.ScrollBars = ScrollBars.Vertical;
            txtDeliveryAddress.Location = new Point(16, 36);
            txtDeliveryAddress.Size = new Size(490, 80);
            txtDeliveryAddress.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            txtDeliveryAddress.Text = "Kính gửi: Ông / Bà [Họ và tên]\r\nCông ty: [Tên doanh nghiệp]\r\nĐịa chỉ: [Số nhà, Đường, Quận/Huyện, Tỉnh/Thành]";
            tabEnvelopes.Controls.Add(txtDeliveryAddress);

            Label lblReturn = new Label();
            lblReturn.Text = "Địa chỉ người gửi (Return address):";
            lblReturn.Location = new Point(16, 126);
            lblReturn.AutoSize = true;
            lblReturn.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            tabEnvelopes.Controls.Add(lblReturn);

            txtReturnAddress = new TextBox();
            txtReturnAddress.Multiline = true;
            txtReturnAddress.ScrollBars = ScrollBars.Vertical;
            txtReturnAddress.Location = new Point(16, 148);
            txtReturnAddress.Size = new Size(490, 70);
            txtReturnAddress.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            txtReturnAddress.Text = "Người gửi: Ban Giám Đốc ZENQ OFFICE\r\nĐịa chỉ: Tòa nhà Văn phòng Quốc tế, Hà Nội, Việt Nam";
            tabEnvelopes.Controls.Add(txtReturnAddress);

            Label lblSize = new Label();
            lblSize.Text = "Khổ phong bì chuẩn:";
            lblSize.Location = new Point(16, 230);
            lblSize.AutoSize = true;
            lblSize.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            tabEnvelopes.Controls.Add(lblSize);

            cboEnvelopeSize = new ComboBox();
            cboEnvelopeSize.DropDownStyle = ComboBoxStyle.DropDownList;
            cboEnvelopeSize.Location = new Point(16, 252);
            cboEnvelopeSize.Size = new Size(320, 26);
            cboEnvelopeSize.Items.AddRange(new object[] {
                "Khổ #10 (241 x 105 mm) - Tiêu chuẩn thương mại Mỹ & Quốc tế",
                "Khổ DL (220 x 110 mm) - Bì thư phổ biến gấp 3 giấy A4",
                "Khổ C5 (229 x 162 mm) - Khổ thư A5 phẳng",
                "Khổ B4 (250 x 353 mm) - Khổ bưu phẩm hồ sơ lớn"
            });
            cboEnvelopeSize.SelectedIndex = 1;
            tabEnvelopes.Controls.Add(cboEnvelopeSize);

            btnAddEnvelopeToDoc = new Button();
            btnAddEnvelopeToDoc.Text = "Thêm vào tài liệu";
            btnAddEnvelopeToDoc.Location = new Point(16, 330);
            btnAddEnvelopeToDoc.Size = new Size(150, 34);
            btnAddEnvelopeToDoc.BackColor = Color.FromArgb(16, 110, 190);
            btnAddEnvelopeToDoc.ForeColor = Color.White;
            btnAddEnvelopeToDoc.FlatStyle = FlatStyle.Flat;
            btnAddEnvelopeToDoc.FlatAppearance.BorderSize = 0;
            btnAddEnvelopeToDoc.Click += (s, e) => AddEnvelopeToDocument();
            tabEnvelopes.Controls.Add(btnAddEnvelopeToDoc);

            btnPrintEnvelope = new Button();
            btnPrintEnvelope.Text = "In phong bì...";
            btnPrintEnvelope.Location = new Point(176, 330);
            btnPrintEnvelope.Size = new Size(120, 34);
            btnPrintEnvelope.BackColor = Color.FromArgb(235, 236, 238);
            btnPrintEnvelope.ForeColor = Color.FromArgb(50, 49, 48);
            btnPrintEnvelope.FlatStyle = FlatStyle.Flat;
            btnPrintEnvelope.FlatAppearance.BorderColor = Color.LightGray;
            btnPrintEnvelope.Click += (s, e) =>
            {
                using (PrintDialog pd = new PrintDialog())
                {
                    pd.ShowDialog(this);
                }
            };
            tabEnvelopes.Controls.Add(btnPrintEnvelope);

            Button btnClose1 = new Button();
            btnClose1.Text = "Đóng";
            btnClose1.Location = new Point(420, 330);
            btnClose1.Size = new Size(86, 34);
            btnClose1.BackColor = Color.FromArgb(235, 236, 238);
            btnClose1.ForeColor = Color.FromArgb(50, 49, 48);
            btnClose1.FlatStyle = FlatStyle.Flat;
            btnClose1.FlatAppearance.BorderColor = Color.LightGray;
            btnClose1.Click += (s, e) => this.Close();
            tabEnvelopes.Controls.Add(btnClose1);

            tabControl.TabPages.Add(tabEnvelopes);

            // ==================== TAB 2: NHÃN THƯ ====================
            tabLabels = new TabPage("🏷 Nhãn (Labels)");
            tabLabels.BackColor = Color.White;

            Label lblLblText = new Label();
            lblLblText.Text = "Nội dung in nhãn:";
            lblLblText.Location = new Point(16, 14);
            lblLblText.AutoSize = true;
            lblLblText.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            tabLabels.Controls.Add(lblLblText);

            txtLabelText = new TextBox();
            txtLabelText.Multiline = true;
            txtLabelText.ScrollBars = ScrollBars.Vertical;
            txtLabelText.Location = new Point(16, 36);
            txtLabelText.Size = new Size(490, 120);
            txtLabelText.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            txtLabelText.Text = "Họ và tên: [Người Nhận]\r\nĐịa chỉ: [Địa chỉ nhận hàng]\r\nĐiện thoại: [Số điện thoại]";
            tabLabels.Controls.Add(txtLabelText);

            Label lblLayout = new Label();
            lblLayout.Text = "Định dạng trang nhãn in:";
            lblLayout.Location = new Point(16, 170);
            lblLayout.AutoSize = true;
            lblLayout.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            tabLabels.Controls.Add(lblLayout);

            cboLabelLayout = new ComboBox();
            cboLabelLayout.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLabelLayout.Location = new Point(16, 192);
            cboLabelLayout.Size = new Size(320, 26);
            cboLabelLayout.Items.AddRange(new object[] {
                "30 nhãn / trang (3 cột x 10 hàng - Avery 5160)",
                "14 nhãn / trang (2 cột x 7 hàng - Avery 5162)",
                "8 nhãn / trang (2 cột x 4 hàng - Nhãn bưu kiện)"
            });
            cboLabelLayout.SelectedIndex = 0;
            tabLabels.Controls.Add(cboLabelLayout);

            btnCreateLabelsTable = new Button();
            btnCreateLabelsTable.Text = "Tạo trang nhãn mới";
            btnCreateLabelsTable.Location = new Point(16, 330);
            btnCreateLabelsTable.Size = new Size(160, 34);
            btnCreateLabelsTable.BackColor = Color.FromArgb(16, 110, 190);
            btnCreateLabelsTable.ForeColor = Color.White;
            btnCreateLabelsTable.FlatStyle = FlatStyle.Flat;
            btnCreateLabelsTable.FlatAppearance.BorderSize = 0;
            btnCreateLabelsTable.Click += (s, e) => CreateLabelsTable();
            tabLabels.Controls.Add(btnCreateLabelsTable);

            Button btnClose2 = new Button();
            btnClose2.Text = "Đóng";
            btnClose2.Location = new Point(420, 330);
            btnClose2.Size = new Size(86, 34);
            btnClose2.BackColor = Color.FromArgb(235, 236, 238);
            btnClose2.ForeColor = Color.FromArgb(50, 49, 48);
            btnClose2.FlatStyle = FlatStyle.Flat;
            btnClose2.FlatAppearance.BorderColor = Color.LightGray;
            btnClose2.Click += (s, e) => this.Close();
            tabLabels.Controls.Add(btnClose2);

            tabControl.TabPages.Add(tabLabels);

            this.Controls.Add(tabControl);
        }

        private void AddEnvelopeToDocument()
        {
            if (_writerView == null) return;
            string ret = txtReturnAddress.Text.Trim();
            string deliv = txtDeliveryAddress.Text.Trim();

            string headerEnvelope = string.Format(
                "==================== [KHỔ PHONG BÌ: {0}] ====================\r\n{1}\r\n\r\n\t\t\t\t\t\t{2}\r\n====================================================================\r\n\r\n",
                cboEnvelopeSize.SelectedItem != null ? cboEnvelopeSize.SelectedItem.ToString() : "DL",
                ret,
                deliv.Replace("\r\n", "\r\n\t\t\t\t\t\t")
            );

            _writerView.SaveUndoState();
            _writerView.PieceTable.Insert(0, headerEnvelope);
            _writerView.CursorPosition = headerEnvelope.Length;
            this.Close();
        }

        private void CreateLabelsTable()
        {
            if (_writerView == null) return;
            int rows = 10;
            int cols = 3;
            if (cboLabelLayout.SelectedIndex == 1) { rows = 7; cols = 2; }
            else if (cboLabelLayout.SelectedIndex == 2) { rows = 4; cols = 2; }

            string text = txtLabelText.Text.Trim();
            _writerView.InsertTable(cols, rows);
            this.Close();
        }
    }
}
