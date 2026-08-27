using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class DocChartData
    {
        public string ChartType { get; set; }
        public string Title { get; set; }
        public List<string> Categories { get; set; }
        public List<string> SeriesNames { get; set; }
        public List<List<double>> Values { get; set; } // [CategoryIndex][SeriesIndex]
        public List<bool> SeriesVisible { get; set; }
        public List<bool> CategoriesVisible { get; set; }
        public bool ShowTitle { get; set; }
        public bool ShowLegend { get; set; }
        public bool ShowGridlines { get; set; }
        public bool ShowDataLabels { get; set; }
        public bool ShowDataTable { get; set; }
        public bool ShowAxes { get; set; }
        public int StylePresetIndex { get; set; }
        public int ColorPaletteIndex { get; set; }

        public DocChartData()
        {
            ChartType = "Column_Clustered";
            Title = "Tiêu đề Biểu đồ";
            Categories = new List<string>();
            SeriesNames = new List<string>();
            Values = new List<List<double>>();
            SeriesVisible = new List<bool>();
            CategoriesVisible = new List<bool>();
            ShowTitle = true;
            ShowLegend = true;
            ShowGridlines = true;
            ShowAxes = true;
            ShowDataLabels = false;
            ShowDataTable = false;
            StylePresetIndex = 0;
            ColorPaletteIndex = 0;
        }

        public DocChartData Clone()
        {
            DocChartData clone = new DocChartData
            {
                ChartType = this.ChartType,
                Title = this.Title,
                Categories = new List<string>(this.Categories),
                SeriesNames = new List<string>(this.SeriesNames),
                Values = new List<List<double>>(),
                SeriesVisible = this.SeriesVisible != null ? new List<bool>(this.SeriesVisible) : new List<bool>(),
                CategoriesVisible = this.CategoriesVisible != null ? new List<bool>(this.CategoriesVisible) : new List<bool>(),
                ShowTitle = this.ShowTitle,
                ShowLegend = this.ShowLegend,
                ShowGridlines = this.ShowGridlines,
                ShowDataLabels = this.ShowDataLabels,
                ShowDataTable = this.ShowDataTable,
                ShowAxes = this.ShowAxes,
                StylePresetIndex = this.StylePresetIndex,
                ColorPaletteIndex = this.ColorPaletteIndex
            };
            foreach (var row in this.Values)
            {
                clone.Values.Add(new List<double>(row));
            }
            return clone;
        }

        public static DocChartData CreateDefault(string chartType)
        {
            DocChartData data = new DocChartData
            {
                ChartType = string.IsNullOrEmpty(chartType) ? "Column_Clustered" : chartType,
                Title = "Tiêu đề Biểu đồ",
                Categories = new List<string> { "Danh mục 1", "Danh mục 2", "Danh mục 3", "Danh mục 4" },
                SeriesNames = new List<string> { "Chuỗi 1", "Chuỗi 2", "Chuỗi 3" },
                Values = new List<List<double>>
                {
                    new List<double> { 4.3, 2.4, 2.0 },
                    new List<double> { 2.5, 4.4, 2.0 },
                    new List<double> { 3.5, 1.8, 3.0 },
                    new List<double> { 4.5, 2.8, 5.0 }
                }
            };
            if (chartType == "Pie" || chartType == "Doughnut")
            {
                data.Categories = new List<string> { "Quý 1", "Quý 2", "Quý 3", "Quý 4" };
                data.SeriesNames = new List<string> { "Doanh số" };
                data.Values = new List<List<double>>
                {
                    new List<double> { 8.2 },
                    new List<double> { 3.2 },
                    new List<double> { 1.4 },
                    new List<double> { 1.2 }
                };
            }
            else if (chartType != null && (chartType.StartsWith("Map") || chartType.StartsWith("FilledMap")))
            {
                data.Title = "Bản đồ Phân bổ Mật độ / Doanh thu (Filled Map)";
                data.Categories = new List<string> { "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ", "Quảng Ninh" };
                data.SeriesNames = new List<string> { "Doanh thu (Tỷ VNĐ)" };
                data.Values = new List<List<double>>
                {
                    new List<double> { 88.5 },
                    new List<double> { 96.2 },
                    new List<double> { 65.4 },
                    new List<double> { 58.0 },
                    new List<double> { 46.2 },
                    new List<double> { 52.8 }
                };
            }
            else if (chartType != null && chartType.StartsWith("PivotChart"))
            {
                data.Title = "Biểu đồ Pivot Đa chiều (PivotChart)";
                data.Categories = new List<string> { "Miền Bắc", "Miền Trung", "Miền Nam", "Xuất khẩu" };
                data.SeriesNames = new List<string> { "Sản phẩm A", "Sản phẩm B", "Sản phẩm C" };
                data.Values = new List<List<double>>
                {
                    new List<double> { 120.5, 95.2, 45.0 },
                    new List<double> { 80.0, 110.4, 60.5 },
                    new List<double> { 150.2, 140.0, 95.8 },
                    new List<double> { 70.4, 65.0, 110.2 }
                };
            }
            else if (chartType != null && (chartType.StartsWith("3DMap") || chartType.StartsWith("Map3D")))
            {
                data.Title = "Bản đồ 3D Không gian (3D Maps / Power Map)";
                data.Categories = new List<string> { "Đông Nam Á", "Đông Á", "Bắc Mỹ", "Tây Âu", "Châu Úc" };
                data.SeriesNames = new List<string> { "Khối lượng 3D (Tấn/Tháng)" };
                data.Values = new List<List<double>>
                {
                    new List<double> { 450.0 },
                    new List<double> { 385.0 },
                    new List<double> { 290.0 },
                    new List<double> { 315.5 },
                    new List<double> { 180.0 }
                };
            }
            return data;
        }
    }

    public class ChartDataExcelForm : Form
    {
        public DocChartData ChartData { get; private set; }
        public Action<DocChartData> OnDataChanged;

        private DataGridView grid;
        private Panel headerPanel;
        private Label lblTitle;
        private Button btnClose;

        public ChartDataExcelForm(DocChartData initialData)
        {
            this.ChartData = initialData ?? DocChartData.CreateDefault("Column_Clustered");
            InitializeComponent();
            PopulateGridFromData();
        }

        private void InitializeComponent()
        {
            this.Text = "Bảng dữ liệu Biểu đồ (GenOffice Calc Mini)";
            this.Size = new Size(640, 380);
            this.MinimumSize = new Size(420, 240);
            this.StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.TopMost = true;
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.FromArgb(243, 243, 243);

            // Top Header Bar (GenOffice Calc Teal #0078d7 / #0f766e)
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(16, 124, 65)
            };

            lblTitle = new Label
            {
                Text = "📊 Bảng số liệu Biểu đồ (Nhập số liệu để cập nhật biểu đồ tức thì)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(10, 7),
                AutoSize = true
            };

            btnClose = new Button
            {
                Text = "✕",
                Size = new Size(26, 26),
                Location = new Point(this.Width - 42, 4),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Hide();

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(btnClose);

            // DataGridView Setup with full scrollbars & 30 rows + 26 columns
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                RowHeadersWidth = 55,
                ColumnHeadersHeight = 28,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ScrollBars = ScrollBars.Both,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9.5f)
            };
            grid.RowTemplate.Height = 24;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 238, 242);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 40, 40);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 238, 242);
            grid.RowHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 40, 40);
            grid.RowHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.CellValueChanged += Grid_CellValueChanged;
            grid.RowsAdded += (s, e) => SyncGridToData();
            grid.RowsRemoved += (s, e) => SyncGridToData();

            this.Controls.Add(grid);
            this.Controls.Add(headerPanel);
            grid.BringToFront();
        }

        public void PopulateGridFromData()
        {
            grid.CellValueChanged -= Grid_CellValueChanged;
            grid.Columns.Clear();
            grid.Rows.Clear();

            // Create 15 Columns: A, B, C, D, E, F, G, H, I, J, K, L, M, N, O
            int totalCols = Math.Max(15, ChartData.SeriesNames.Count + 4);
            for (int i = 0; i < totalCols; i++)
            {
                char letter = (char)('A' + i);
                string colName = "Col" + letter;
                grid.Columns.Add(colName, letter.ToString());
                grid.Columns[i].Width = (i == 0) ? 120 : 90;
            }

            // Create 30 Rows
            int totalRows = Math.Max(30, ChartData.Categories.Count + 10);
            for (int r = 0; r < totalRows; r++)
            {
                int rIdx = grid.Rows.Add();
                grid.Rows[rIdx].HeaderCell.Value = (r + 1).ToString();
            }

            // Row 1 (Index 0): Series Names (Starting from Col B / Index 1)
            grid.Rows[0].Cells[0].Value = "";
            for (int s = 0; s < ChartData.SeriesNames.Count; s++)
            {
                if (s + 1 < grid.Columns.Count)
                {
                    grid.Rows[0].Cells[s + 1].Value = ChartData.SeriesNames[s];
                }
            }

            // Rows 2.. (Index 1..): Categories and Values
            for (int c = 0; c < ChartData.Categories.Count; c++)
            {
                int rowIdx = c + 1;
                if (rowIdx < grid.Rows.Count)
                {
                    grid.Rows[rowIdx].Cells[0].Value = ChartData.Categories[c];

                    if (c < ChartData.Values.Count)
                    {
                        for (int s = 0; s < ChartData.SeriesNames.Count; s++)
                        {
                            if (s < ChartData.Values[c].Count && s + 1 < grid.Columns.Count)
                            {
                                grid.Rows[rowIdx].Cells[s + 1].Value = ChartData.Values[c][s].ToString();
                            }
                        }
                    }
                }
            }

            grid.CellValueChanged += Grid_CellValueChanged;
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            SyncGridToData();
        }

        private void SyncGridToData()
        {
            if (grid.Rows.Count < 2) return;

            try
            {
                List<string> seriesList = new List<string>();
                for (int col = 1; col < grid.Columns.Count; col++)
                {
                    object val = grid.Rows[0].Cells[col].Value;
                    if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        seriesList.Add(val.ToString().Trim());
                    }
                }
                if (seriesList.Count == 0) seriesList.Add("Chuỗi 1");

                List<string> catList = new List<string>();
                List<List<double>> valMatrix = new List<List<double>>();

                for (int r = 1; r < grid.Rows.Count; r++)
                {
                    if (grid.Rows[r].IsNewRow) continue;
                    object catVal = grid.Rows[r].Cells[0].Value;
                    bool hasAnyVal = false;
                    for (int s = 0; s < seriesList.Count; s++)
                    {
                        if (s + 1 < grid.Columns.Count && grid.Rows[r].Cells[s + 1].Value != null && !string.IsNullOrWhiteSpace(grid.Rows[r].Cells[s + 1].Value.ToString()))
                        {
                            hasAnyVal = true;
                            break;
                        }
                    }

                    if ((catVal != null && !string.IsNullOrWhiteSpace(catVal.ToString())) || hasAnyVal)
                    {
                        string catName = (catVal != null && !string.IsNullOrWhiteSpace(catVal.ToString())) ? catVal.ToString().Trim() : ("Danh mục " + r);
                        catList.Add(catName);

                        List<double> rowVals = new List<double>();
                        for (int s = 0; s < seriesList.Count; s++)
                        {
                            double num = 1.0;
                            if (s + 1 < grid.Columns.Count)
                            {
                                object cellV = grid.Rows[r].Cells[s + 1].Value;
                                double parsed;
                                if (cellV != null && double.TryParse(cellV.ToString().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                                {
                                    num = parsed;
                                }
                            }
                            rowVals.Add(num);
                        }
                        valMatrix.Add(rowVals);
                    }
                }

                if (catList.Count > 0)
                {
                    ChartData.SeriesNames = seriesList;
                    ChartData.Categories = catList;
                    ChartData.Values = valMatrix;
                    if (OnDataChanged != null)
                    {
                        OnDataChanged(ChartData);
                    }
                }
            }
            catch { }
        }
    }
}
