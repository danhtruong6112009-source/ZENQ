using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class InsertChartForm : Form
    {
        public string SelectedChartType { get; private set; }
        public DocChartData SourceChartData { get; private set; }

        public event Action<string> OnChartConfirmed;

        private int activeTopTab = 0; // 0 = Recommended Charts, 1 = All Charts
        private int selectedRecIndex = 0;
        private int selectedSubtypeIndex = 0;
        private string previewSelectedTypeId = "Column_Clustered";

        private Panel pnlTopTabs;
        private Panel pnlContent;
        private Panel pnlBottom;
        private Panel pnlRecommended;
        private Panel pnlAllCharts;
        private FlowLayoutPanel pnlRecList;
        private Panel pnlRecPreview;
        private Label lblRecTitle;
        private Label lblRecDesc;
        private ListBox lstAllCategories;
        private Panel pnlSubtypesStrip;
        private Label lblAllTypeName;
        private FlowLayoutPanel pnlAllPreviews;

        private Button btnTabRec;
        private Button btnTabAll;

        private struct RecChartItem
        {
            public string TypeId;
            public string Name;
            public string Description;
            public RecChartItem(string id, string n, string d)
            {
                TypeId = id; Name = n; Description = d;
            }
        }

        private RecChartItem[] recCharts = new RecChartItem[]
        {
            new RecChartItem("Column_Clustered", "Clustered Column", "A clustered column chart is used to compare values across a few categories. Use it when the order of categories is not important."),
            new RecChartItem("Line", "Line", "A line chart is used to display trends over time (years, months, and days) or categories."),
            new RecChartItem("Column_Stacked", "Stacked Column", "A stacked column chart is used to compare parts of a whole across categories."),
            new RecChartItem("Bar_Clustered", "Clustered Bar", "A clustered bar chart is used to compare values across categories horizontally."),
            new RecChartItem("Pie", "Pie", "A pie chart shows the size of items in one data series, proportional to the sum of the items.")
        };

        private string[] allCategories = new string[]
        {
            "Templates",
            "Column",
            "Line",
            "Pie",
            "Bar",
            "Area",
            "X Y (Scatter)",
            "Map",
            "Stock",
            "Surface",
            "Radar",
            "Treemap",
            "Sunburst",
            "Histogram",
            "Box & Whisker",
            "Waterfall",
            "Funnel",
            "Combo"
        };

        private string[][] allSubtypes = new string[][]
        {
            new string[] { "Clustered Column Template", "Line Template" }, // Templates (0)
            new string[] { "Clustered Column", "Stacked Column", "100% Stacked Column", "3-D Clustered Column", "3-D Stacked Column", "3-D 100% Stacked Column", "3-D Column" }, // Column (1)
            new string[] { "Line", "Stacked Line", "100% Stacked Line", "Line with Markers", "Stacked Line with Markers", "100% Stacked Line with Markers", "3-D Line" }, // Line (2)
            new string[] { "2-D Pie", "3-D Pie", "Pie of Pie", "Bar of Pie", "Doughnut", "3-D Doughnut" }, // Pie (3)
            new string[] { "Clustered Bar", "Stacked Bar", "100% Stacked Bar", "3-D Clustered Bar", "3-D Stacked Bar", "3-D 100% Stacked Bar" }, // Bar (4)
            new string[] { "Area", "Stacked Area", "100% Stacked Area", "3-D Area", "3-D Stacked Area", "3-D 100% Stacked Area" }, // Area (5)
            new string[] { "Scatter", "Scatter with Smooth Lines and Markers", "Scatter with Smooth Lines", "Scatter with Straight Lines and Markers", "Scatter with Straight Lines", "Bubble", "3-D Bubble" }, // Scatter (6)
            new string[] { "Filled Map" }, // Map (7)
            new string[] { "High-Low-Close", "Open-High-Low-Close", "Volume-High-Low-Close", "Volume-Open-High-Low-Close" }, // Stock (8)
            new string[] { "3-D Surface", "Wireframe 3-D Surface", "Contour", "Wireframe Contour" }, // Surface (9)
            new string[] { "Radar", "Radar with Markers", "Filled Radar" }, // Radar (10)
            new string[] { "Treemap" }, // Treemap (11)
            new string[] { "Sunburst" }, // Sunburst (12)
            new string[] { "Histogram", "Pareto" }, // Histogram (13)
            new string[] { "Box & Whisker" }, // Box & Whisker (14)
            new string[] { "Waterfall" }, // Waterfall (15)
            new string[] { "Funnel" }, // Funnel (16)
            new string[] { "Clustered Column - Line", "Clustered Column - Line on Secondary Axis", "Stacked Area - Clustered Column", "Custom Combination" } // Combo (17)
        };

        private int initialCategoryIndex = 1; // Default to Column

        public InsertChartForm(DocChartData sourceData = null, int defaultCategoryIndex = -1)
        {
            SourceChartData = sourceData != null ? sourceData.Clone() : DocChartData.CreateDefault("Column_Clustered");
            SelectedChartType = "Column_Clustered";
            previewSelectedTypeId = "Column_Clustered";

            if (defaultCategoryIndex >= 0 && defaultCategoryIndex < allCategories.Length)
            {
                initialCategoryIndex = defaultCategoryIndex;
                activeTopTab = 1; // Direct to All Charts focused on category
            }
            else
            {
                initialCategoryIndex = 1; // Default to Column
                activeTopTab = 0; // Default to Recommended Charts
            }

            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Insert Chart";
            this.ClientSize = new Size(840, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9.0f);
            this.BackColor = Color.FromArgb(243, 244, 246);

            // 1. Top Tab Strip (Fixed at Top: 0..44)
            pnlTopTabs = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(840, 44),
                BackColor = Color.FromArgb(243, 244, 246)
            };

            btnTabRec = new Button
            {
                Text = "Recommended Charts",
                Location = new Point(14, 8),
                Size = new Size(165, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, activeTopTab == 0 ? FontStyle.Bold : FontStyle.Regular),
                BackColor = activeTopTab == 0 ? Color.White : Color.FromArgb(243, 244, 246),
                Cursor = Cursors.Hand
            };
            btnTabRec.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);

            btnTabAll = new Button
            {
                Text = "All Charts",
                Location = new Point(184, 8),
                Size = new Size(115, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, activeTopTab == 1 ? FontStyle.Bold : FontStyle.Regular),
                BackColor = activeTopTab == 1 ? Color.White : Color.FromArgb(243, 244, 246),
                Cursor = Cursors.Hand
            };
            btnTabAll.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);

            btnTabRec.Click += (s, e) => SwitchTab(0);
            btnTabAll.Click += (s, e) => SwitchTab(1);

            pnlTopTabs.Controls.Add(btnTabRec);
            pnlTopTabs.Controls.Add(btnTabAll);
            this.Controls.Add(pnlTopTabs);

            // 2. Bottom Action Buttons (Fixed at Bottom: 528..580)
            pnlBottom = new Panel
            {
                Location = new Point(0, 528),
                Size = new Size(840, 52),
                BackColor = Color.FromArgb(243, 244, 246)
            };

            Button btnOk = new Button
            {
                Text = "OK",
                Size = new Size(90, 30),
                Location = new Point(620, 11),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (activeTopTab == 0)
                {
                    SelectedChartType = recCharts[selectedRecIndex].TypeId;
                }
                else
                {
                    SelectedChartType = previewSelectedTypeId;
                }
                if (OnChartConfirmed != null) OnChartConfirmed(SelectedChartType);
                this.Close();
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 30),
                Location = new Point(720, 11),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnCancel.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(btnOk);
            pnlBottom.Controls.Add(btnCancel);
            this.Controls.Add(pnlBottom);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            // 3. Central Content Area (Fixed: Y: 44, Height: 484)
            pnlContent = new Panel
            {
                Location = new Point(0, 44),
                Size = new Size(840, 484),
                BackColor = Color.White
            };
            this.Controls.Add(pnlContent);

            // Build Both Tab Pages inside pnlContent
            BuildRecommendedTab();
            BuildAllChartsTab();

            SwitchTab(activeTopTab);
        }

        private void SwitchTab(int tabIndex)
        {
            activeTopTab = tabIndex;
            if (activeTopTab == 0)
            {
                btnTabRec.BackColor = Color.White;
                btnTabRec.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btnTabAll.BackColor = Color.FromArgb(243, 244, 246);
                btnTabAll.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                pnlRecommended.Visible = true;
                pnlAllCharts.Visible = false;
                UpdateRecommendedPreview();
            }
            else
            {
                btnTabAll.BackColor = Color.White;
                btnTabAll.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btnTabRec.BackColor = Color.FromArgb(243, 244, 246);
                btnTabRec.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                pnlRecommended.Visible = false;
                pnlAllCharts.Visible = true;
                UpdateAllChartsSubtypes();
            }
        }

        // ==================== RECOMMENDED CHARTS TAB ====================
        private void BuildRecommendedTab()
        {
            pnlRecommended = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(840, 484),
                BackColor = Color.White
            };

            // Left Thumbnail List
            pnlRecList = new FlowLayoutPanel
            {
                Location = new Point(16, 12),
                Size = new Size(240, 460),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(4)
            };

            for (int i = 0; i < recCharts.Length; i++)
            {
                int itemIdx = i;
                Panel card = new Panel { Size = new Size(210, 110), Margin = new Padding(4), BackColor = Color.White, Cursor = Cursors.Hand };
                card.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    bool isSel = (itemIdx == selectedRecIndex);

                    using (Pen p = new Pen(isSel ? Color.FromArgb(0, 120, 215) : Color.FromArgb(209, 213, 219), isSel ? 2.5f : 1f))
                    {
                        g.DrawRectangle(p, 1, 1, card.Width - 3, card.Height - 3);
                    }

                    DocChartData thumbData = SourceChartData.Clone();
                    thumbData.ChartType = recCharts[itemIdx].TypeId;
                    thumbData.Title = "";
                    ChartRenderer.DrawChart(g, new Rectangle(6, 6, card.Width - 12, card.Height - 12), thumbData, false, 0.48f);
                };
                card.Click += (s, e) =>
                {
                    selectedRecIndex = itemIdx;
                    UpdateRecommendedPreview();
                    pnlRecList.Invalidate(true);
                };
                card.DoubleClick += (s, e) =>
                {
                    selectedRecIndex = itemIdx;
                    SelectedChartType = recCharts[selectedRecIndex].TypeId;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                pnlRecList.Controls.Add(card);
            }

            pnlRecommended.Controls.Add(pnlRecList);

            // Right Large Preview Area
            Panel pnlRightContainer = new Panel
            {
                Location = new Point(270, 12),
                Size = new Size(550, 460),
                BackColor = Color.White
            };

            lblRecTitle = new Label
            {
                Location = new Point(8, 4),
                Size = new Size(534, 26),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Text = recCharts[0].Name
            };

            pnlRecPreview = new Panel
            {
                Location = new Point(8, 34),
                Size = new Size(534, 320),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRecPreview.Paint += (s, e) =>
            {
                DocChartData mainData = SourceChartData.Clone();
                mainData.ChartType = recCharts[selectedRecIndex].TypeId;
                ChartRenderer.DrawChart(e.Graphics, new Rectangle(4, 4, pnlRecPreview.Width - 8, pnlRecPreview.Height - 8), mainData, false, 0.95f);
            };

            lblRecDesc = new Label
            {
                Location = new Point(8, 362),
                Size = new Size(534, 90),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Text = recCharts[0].Description
            };

            pnlRightContainer.Controls.Add(lblRecTitle);
            pnlRightContainer.Controls.Add(pnlRecPreview);
            pnlRightContainer.Controls.Add(lblRecDesc);
            pnlRecommended.Controls.Add(pnlRightContainer);

            pnlContent.Controls.Add(pnlRecommended);
        }

        private void UpdateRecommendedPreview()
        {
            if (selectedRecIndex >= 0 && selectedRecIndex < recCharts.Length)
            {
                lblRecTitle.Text = recCharts[selectedRecIndex].Name;
                lblRecDesc.Text = recCharts[selectedRecIndex].Description;
                pnlRecPreview.Invalidate();
            }
        }

        // ==================== ALL CHARTS TAB ====================
        private void BuildAllChartsTab()
        {
            pnlAllCharts = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(840, 484),
                BackColor = Color.White
            };

            // Left Category List (Exact MS Excel Categories)
            lstAllCategories = new ListBox
            {
                Location = new Point(16, 12),
                Size = new Size(185, 460),
                ItemHeight = 28,
                DrawMode = DrawMode.OwnerDrawFixed,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            foreach (var cat in allCategories) lstAllCategories.Items.Add(cat);
            lstAllCategories.SelectedIndex = initialCategoryIndex;

            lstAllCategories.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (SolidBrush bg = new SolidBrush(isSelected ? Color.FromArgb(224, 231, 255) : Color.White))
                {
                    e.Graphics.FillRectangle(bg, e.Bounds);
                }
                if (isSelected)
                {
                    using (Pen p = new Pen(Color.FromArgb(0, 120, 215), 2.5f))
                    {
                        e.Graphics.DrawLine(p, e.Bounds.Left + 1, e.Bounds.Top + 2, e.Bounds.Left + 1, e.Bounds.Bottom - 2);
                    }
                }

                // Draw Custom 16x16 Vector Category Mini-Icon
                DrawCategoryMiniVectorIcon(e.Graphics, e.Bounds.Left + 8, e.Bounds.Top + 6, e.Index, isSelected);

                Brush txtB = isSelected ? new SolidBrush(Color.FromArgb(0, 90, 180)) : new SolidBrush(Color.FromArgb(15, 23, 42));
                e.Graphics.DrawString(allCategories[e.Index], lstAllCategories.Font, txtB, e.Bounds.Left + 30, e.Bounds.Top + 5);
            };

            lstAllCategories.SelectedIndexChanged += (s, e) =>
            {
                selectedSubtypeIndex = 0;
                UpdateAllChartsSubtypes();
            };

            pnlAllCharts.Controls.Add(lstAllCategories);

            // Right Area (Fixed X: 212, Width: 612, Height: 460)
            Panel pnlAllRight = new Panel
            {
                Location = new Point(212, 10),
                Size = new Size(612, 462),
                BackColor = Color.White
            };

            // Top Subtypes Strip
            pnlSubtypesStrip = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(612, 54),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            pnlSubtypesStrip.Paint += PnlSubtypesStrip_Paint;
            pnlSubtypesStrip.MouseDown += PnlSubtypesStrip_MouseDown;

            lblAllTypeName = new Label
            {
                Location = new Point(4, 58),
                Size = new Size(604, 24),
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Text = "Clustered Column"
            };

            // Main Preview Cards Panel
            pnlAllPreviews = new FlowLayoutPanel
            {
                Location = new Point(0, 84),
                Size = new Size(612, 376),
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(2)
            };

            pnlAllRight.Controls.Add(pnlSubtypesStrip);
            pnlAllRight.Controls.Add(lblAllTypeName);
            pnlAllRight.Controls.Add(pnlAllPreviews);
            pnlAllCharts.Controls.Add(pnlAllRight);

            pnlContent.Controls.Add(pnlAllCharts);

            UpdateAllChartsSubtypes();
        }

        private void DrawCategoryMiniVectorIcon(Graphics g, int x, int y, int catIdx, bool isSelected)
        {
            Color cMain = isSelected ? Color.FromArgb(0, 90, 180) : Color.FromArgb(91, 155, 213);
            Color cSec = isSelected ? Color.FromArgb(234, 88, 12) : Color.FromArgb(237, 125, 49);

            switch (catIdx)
            {
                case 0: // Templates
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(245, 158, 11))) g.FillRectangle(b, x + 1, y + 2, 14, 11);
                    using (Pen p = new Pen(Color.FromArgb(180, 83, 9), 1f)) g.DrawRectangle(p, x + 1, y + 2, 14, 11);
                    break;
                case 1: // Column
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillRectangle(b, x + 2, y + 6, 3, 9);
                    using (SolidBrush b = new SolidBrush(cSec)) g.FillRectangle(b, x + 6, y + 2, 3, 13);
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillRectangle(b, x + 10, y + 8, 3, 7);
                    break;
                case 2: // Line
                    using (Pen p = new Pen(cMain, 1.4f)) { g.DrawLine(p, x + 1, y + 12, x + 6, y + 4); g.DrawLine(p, x + 6, y + 4, x + 10, y + 9); g.DrawLine(p, x + 10, y + 9, x + 15, y + 2); }
                    g.FillEllipse(Brushes.Navy, x + 5, y + 3, 3, 3);
                    g.FillEllipse(Brushes.Navy, x + 9, y + 8, 3, 3);
                    break;
                case 3: // Pie
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillPie(b, x + 1, y + 1, 14, 14, 0, 240);
                    using (SolidBrush b = new SolidBrush(cSec)) g.FillPie(b, x + 1, y + 1, 14, 14, 240, 120);
                    break;
                case 4: // Bar
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillRectangle(b, x + 1, y + 2, 12, 3);
                    using (SolidBrush b = new SolidBrush(cSec)) g.FillRectangle(b, x + 1, y + 6, 8, 3);
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillRectangle(b, x + 1, y + 10, 14, 3);
                    break;
                case 5: // Area
                    PointF[] aPts = new PointF[] { new PointF(x + 1, y + 14), new PointF(x + 1, y + 10), new PointF(x + 6, y + 4), new PointF(x + 11, y + 8), new PointF(x + 15, y + 2), new PointF(x + 15, y + 14) };
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(140, cMain.R, cMain.G, cMain.B))) g.FillPolygon(b, aPts);
                    using (Pen p = new Pen(cMain, 1.2f)) g.DrawLines(p, new PointF[] { aPts[1], aPts[2], aPts[3], aPts[4] });
                    break;
                case 6: // Scatter
                    using (Pen p = new Pen(Color.Gray, 1f)) { g.DrawLine(p, x + 1, y + 1, x + 1, y + 14); g.DrawLine(p, x + 1, y + 14, x + 14, y + 14); }
                    g.FillEllipse(Brushes.Navy, x + 3, y + 10, 2, 2);
                    g.FillEllipse(Brushes.Navy, x + 6, y + 5, 2, 2);
                    g.FillEllipse(Brushes.Navy, x + 10, y + 7, 2, 2);
                    g.FillEllipse(Brushes.Navy, x + 12, y + 3, 2, 2);
                    break;
                case 7: // Map
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(220, 235, 252))) g.FillEllipse(b, x + 1, y + 1, 14, 14);
                    using (Pen p = new Pen(cMain, 1f)) g.DrawEllipse(p, x + 1, y + 1, 14, 14);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(16, 124, 65))) { g.FillEllipse(b, x + 3, y + 3, 5, 4); g.FillEllipse(b, x + 7, y + 7, 5, 4); }
                    break;
                case 8: // Stock
                    using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                    {
                        g.DrawLine(p, x + 4, y + 2, x + 4, y + 13); g.DrawLine(p, x + 2, y + 4, x + 4, y + 4); g.DrawLine(p, x + 4, y + 10, x + 6, y + 10);
                        g.DrawLine(p, x + 10, y + 4, x + 10, y + 15); g.DrawLine(p, x + 8, y + 7, x + 10, y + 7); g.DrawLine(p, x + 10, y + 12, x + 12, y + 12);
                    }
                    break;
                case 9: // Surface
                    PointF[] sPts = new PointF[] { new PointF(x + 2, y + 12), new PointF(x + 8, y + 3), new PointF(x + 14, y + 10), new PointF(x + 8, y + 14) };
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillPolygon(b, sPts);
                    g.DrawPolygon(Pens.Black, sPts);
                    break;
                case 10: // Radar
                    PointF center = new PointF(x + 8, y + 8);
                    PointF[] rPts = new PointF[5];
                    for (int a = 0; a < 5; a++)
                    {
                        double ang = -Math.PI / 2.0 + (a * 2.0 * Math.PI / 5.0);
                        rPts[a] = new PointF(center.X + (float)(Math.Cos(ang) * 6), center.Y + (float)(Math.Sin(ang) * 6));
                    }
                    g.DrawPolygon(Pens.Gray, rPts);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(120, cMain.R, cMain.G, cMain.B))) g.FillPolygon(b, rPts);
                    break;
                case 11: // Treemap
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillRectangle(b, x + 1, y + 1, 8, 13);
                    g.DrawRectangle(Pens.Black, x + 1, y + 1, 8, 13);
                    g.DrawRectangle(Pens.Black, x + 9, y + 1, 6, 6);
                    g.DrawRectangle(Pens.Black, x + 9, y + 7, 6, 7);
                    break;
                case 12: // Sunburst
                    g.DrawEllipse(Pens.Black, x + 1, y + 1, 14, 14);
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillPie(b, x + 1, y + 1, 14, 14, 270, 90);
                    using (SolidBrush wb = new SolidBrush(Color.White)) g.FillEllipse(wb, x + 5, y + 5, 6, 6);
                    break;
                case 13: // Histogram
                    using (SolidBrush b = new SolidBrush(cMain))
                    {
                        g.FillRectangle(b, x + 1, y + 9, 3, 6);
                        g.FillRectangle(b, x + 5, y + 4, 3, 11);
                        g.FillRectangle(b, x + 9, y + 2, 3, 13);
                        g.FillRectangle(b, x + 13, y + 7, 3, 8);
                    }
                    break;
                case 14: // Box & Whisker
                    using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                    {
                        g.DrawLine(p, x + 7, y + 1, x + 7, y + 14);
                        g.FillRectangle(Brushes.White, x + 4, y + 4, 7, 7);
                        g.DrawRectangle(p, x + 4, y + 4, 7, 7);
                        g.DrawLine(p, x + 4, y + 8, x + 11, y + 8);
                    }
                    break;
                case 15: // Waterfall
                    g.DrawRectangle(Pens.Black, x + 1, y + 7, 3, 7);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(16, 124, 65))) g.FillRectangle(b, x + 5, y + 4, 3, 4);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(216, 59, 1))) g.FillRectangle(b, x + 9, y + 8, 3, 4);
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillRectangle(b, x + 13, y + 4, 3, 10);
                    break;
                case 16: // Funnel
                    using (SolidBrush b = new SolidBrush(cMain))
                    {
                        PointF[] f1 = new PointF[] { new PointF(x + 1, y + 2), new PointF(x + 14, y + 2), new PointF(x + 11, y + 6), new PointF(x + 4, y + 6) };
                        g.FillPolygon(b, f1);
                        PointF[] f2 = new PointF[] { new PointF(x + 4, y + 7), new PointF(x + 11, y + 7), new PointF(x + 9, y + 11), new PointF(x + 6, y + 11) };
                        g.FillPolygon(b, f2);
                        g.FillRectangle(b, x + 6, y + 12, 3, 3);
                    }
                    break;
                case 17: // Combo
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillRectangle(b, x + 2, y + 6, 4, 8);
                    using (SolidBrush b = new SolidBrush(cMain)) g.FillRectangle(b, x + 8, y + 3, 4, 11);
                    using (Pen p = new Pen(cSec, 1.4f)) g.DrawLine(p, x + 1, y + 12, x + 14, y + 4);
                    break;
            }
        }

        private void UpdateAllChartsSubtypes()
        {
            int catIdx = lstAllCategories.SelectedIndex;
            if (catIdx < 0 || catIdx >= allSubtypes.Length) catIdx = 1;

            string[] subtypes = allSubtypes[catIdx];
            if (selectedSubtypeIndex >= subtypes.Length) selectedSubtypeIndex = 0;

            lblAllTypeName.Text = subtypes[selectedSubtypeIndex];
            pnlSubtypesStrip.Invalidate();

            // Populate Preview Cards in FlowPanel
            pnlAllPreviews.Controls.Clear();
            string selectedTypeId = GetChartTypeIdFromSelection();
            previewSelectedTypeId = selectedTypeId;

            int cardW = 295;
            int cardH = 350;

            // Card 1: Main Selected Variant (e.g. Clustered Column)
            string title1 = subtypes[selectedSubtypeIndex];
            Panel card1 = CreatePreviewCard(selectedTypeId, title1, cardW, cardH);
            pnlAllPreviews.Controls.Add(card1);

            // Card 2: 3D or Alternate Variant
            string altTypeId;
            string title2;
            if (selectedTypeId.Contains("3D") || selectedTypeId.Contains("3-D"))
            {
                altTypeId = selectedTypeId.Replace("3D_", "").Replace("3-D_", "").Replace("3D", "").Replace("3-D", "");
                if (string.IsNullOrEmpty(altTypeId)) altTypeId = "Column_Clustered";
                title2 = "2-D Variant";
            }
            else
            {
                if (selectedTypeId.StartsWith("Column")) { altTypeId = "Column_3D_Clustered"; title2 = "3-D Clustered Column"; }
                else if (selectedTypeId.StartsWith("Bar")) { altTypeId = "Bar_3D_Clustered"; title2 = "3-D Clustered Bar"; }
                else if (selectedTypeId.StartsWith("Pie")) { altTypeId = "Pie_3D"; title2 = "3-D Pie"; }
                else if (selectedTypeId.StartsWith("Line")) { altTypeId = "Line_3D"; title2 = "3-D Line"; }
                else if (selectedTypeId.StartsWith("Area")) { altTypeId = "Area_3D"; title2 = "3-D Area"; }
                else if (selectedTypeId.StartsWith("Surface")) { altTypeId = "Surface_Wireframe"; title2 = "Wireframe Surface"; }
                else { altTypeId = selectedTypeId; title2 = title1; }
            }

            if (altTypeId != selectedTypeId)
            {
                Panel card2 = CreatePreviewCard(altTypeId, title2, cardW, cardH);
                pnlAllPreviews.Controls.Add(card2);
            }
        }

        private Panel CreatePreviewCard(string chartTypeId, string title, int w, int h)
        {
            Panel card = new Panel { Size = new Size(w, h), Margin = new Padding(6), BackColor = Color.White, Cursor = Cursors.Hand };
            card.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                bool isSelected = (previewSelectedTypeId == chartTypeId);

                using (Pen p = new Pen(isSelected ? Color.FromArgb(0, 120, 215) : Color.FromArgb(209, 213, 219), isSelected ? 2.5f : 1.0f))
                {
                    g.DrawRectangle(p, 1, 1, card.Width - 3, card.Height - 3);
                }

                if (isSelected)
                {
                    using (SolidBrush selBadge = new SolidBrush(Color.FromArgb(0, 120, 215)))
                    {
                        g.FillRectangle(selBadge, card.Width - 24, 2, 22, 22);
                        using (Font ckFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                        {
                            g.DrawString("✓", ckFont, Brushes.White, card.Width - 20, 4);
                        }
                    }
                }

                // Card Title Header
                using (Font cFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (SolidBrush tb = new SolidBrush(isSelected ? Color.FromArgb(0, 90, 180) : Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString(title, cFont, tb, 10, 8);
                }

                // Live Chart Render
                Rectangle chartArea = new Rectangle(8, 30, card.Width - 16, card.Height - 38);
                DocChartData cData = SourceChartData.Clone();
                cData.ChartType = chartTypeId;
                cData.Title = "";
                ChartRenderer.DrawChart(g, chartArea, cData, false, 0.82f);
            };
            card.Click += (s, e) =>
            {
                previewSelectedTypeId = chartTypeId;
                pnlAllPreviews.Invalidate(true);
            };
            card.DoubleClick += (s, e) =>
            {
                SelectedChartType = chartTypeId;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            return card;
        }

        private void PnlSubtypesStrip_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;

            int catIdx = lstAllCategories.SelectedIndex;
            if (catIdx < 0 || catIdx >= allSubtypes.Length) catIdx = 1;
            string[] subtypes = allSubtypes[catIdx];

            int boxW = 56;
            int boxH = 46;

            for (int i = 0; i < subtypes.Length && i < 8; i++)
            {
                Rectangle box = new Rectangle(6 + i * (boxW + 8), 4, boxW, boxH);
                bool isSel = (i == selectedSubtypeIndex);

                if (isSel)
                {
                    using (SolidBrush selBg = new SolidBrush(Color.FromArgb(224, 231, 255))) g.FillRectangle(selBg, box);
                    using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.5f)) g.DrawRectangle(selPen, box);
                }
                else
                {
                    g.FillRectangle(Brushes.White, box);
                    using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f)) g.DrawRectangle(p, box);
                }

                // Render Subtype Icon
                DrawSubtypeIcon(g, box, catIdx, i);
            }
        }

        private void DrawSubtypeIcon(Graphics g, Rectangle box, int catIdx, int subtypeIdx)
        {
            string tid = MapSubtypeToTypeId(catIdx, subtypeIdx);
            int cx = box.X + 4;
            int cy = box.Y + 4;
            int w = box.Width - 8;
            int h = box.Height - 8;

            Color cBlue = Color.FromArgb(91, 155, 213);
            Color cGray = Color.FromArgb(175, 180, 188);

            if (catIdx == 1) // Column
            {
                bool is3D = subtypeIdx >= 3;
                if (is3D)
                {
                    DrawMini3DCuboid(g, cx + 8, cy + 10, 8, h - 16, 4, cBlue);
                    DrawMini3DCuboid(g, cx + 22, cy + 16, 8, h - 22, 4, Color.White);
                    DrawMini3DCuboid(g, cx + 34, cy + 12, 8, h - 18, 4, cGray);
                }
                else
                {
                    g.FillRectangle(Brushes.SteelBlue, cx + 4, cy + 14, 8, 18);
                    g.FillRectangle(Brushes.DarkOrange, cx + 16, cy + 6, 8, 26);
                    g.FillRectangle(Brushes.Gray, cx + 28, cy + 10, 8, 22);
                }
            }
            else if (catIdx == 2) // Line
            {
                using (Pen p = new Pen(Color.SteelBlue, 2f))
                {
                    g.DrawLine(p, cx + 4, cy + 26, cx + 18, cy + 10);
                    g.DrawLine(p, cx + 18, cy + 10, cx + 36, cy + 18);
                }
            }
            else if (catIdx == 3) // Pie
            {
                if (subtypeIdx == 1) // 3-D Pie
                {
                    for (int d = 4; d >= 1; d--)
                    {
                        using (SolidBrush db = new SolidBrush(Color.FromArgb(40, 80, 150))) g.FillPie(db, cx + 6, cy + 6 + d, 30, 20, 0, 360);
                    }
                    g.FillPie(Brushes.SteelBlue, cx + 6, cy + 6, 30, 20, 0, 240);
                    g.FillPie(Brushes.DarkOrange, cx + 6, cy + 6, 30, 20, 240, 120);
                }
                else
                {
                    g.FillPie(Brushes.SteelBlue, cx + 6, cy + 4, 30, 30, 0, 120);
                    g.FillPie(Brushes.DarkOrange, cx + 6, cy + 4, 30, 30, 120, 100);
                    g.FillPie(Brushes.Gray, cx + 6, cy + 4, 30, 30, 220, 140);
                    if (subtypeIdx >= 4) // Doughnut
                    {
                        g.FillEllipse(Brushes.White, cx + 14, cy + 12, 14, 14);
                    }
                }
            }
            else if (catIdx == 4) // Bar
            {
                bool is3D = subtypeIdx >= 3;
                if (is3D)
                {
                    DrawMini3DHorzBar(g, cx + 6, cy + 6, 26, 6, 4, cBlue);
                    DrawMini3DHorzBar(g, cx + 6, cy + 16, 18, 6, 4, Color.White);
                    DrawMini3DHorzBar(g, cx + 6, cy + 24, 22, 6, 4, cGray);
                }
                else
                {
                    g.FillRectangle(Brushes.SteelBlue, cx + 4, cy + 6, 22, 6);
                    g.FillRectangle(Brushes.DarkOrange, cx + 4, cy + 14, 32, 6);
                    g.FillRectangle(Brushes.Gray, cx + 4, cy + 22, 18, 6);
                }
            }
            else if (catIdx == 5) // Area
            {
                PointF[] pts = new PointF[] { new PointF(cx + 4, cy + 28), new PointF(cx + 16, cy + 10), new PointF(cx + 36, cy + 16), new PointF(cx + 36, cy + 28) };
                using (SolidBrush b = new SolidBrush(Color.FromArgb(160, 68, 114, 196))) g.FillPolygon(b, pts);
            }
            else if (catIdx == 11) // Treemap
            {
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 2, cy + 2, 20, 30);
                g.DrawRectangle(Pens.Black, cx + 2, cy + 2, 20, 30);
                g.DrawRectangle(Pens.Black, cx + 22, cy + 2, 18, 14);
                g.DrawRectangle(Pens.Black, cx + 22, cy + 16, 18, 16);
            }
            else if (catIdx == 12) // Sunburst
            {
                g.DrawEllipse(Pens.Black, cx + 6, cy + 4, 30, 30);
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillPie(b, cx + 6, cy + 4, 30, 30, 270, 90);
                using (SolidBrush wb = new SolidBrush(Color.White)) g.FillEllipse(wb, cx + 16, cy + 14, 10, 10);
            }
            else if (catIdx == 15) // Waterfall
            {
                g.DrawRectangle(Pens.Black, cx + 4, cy + 16, 6, 16);
                using (SolidBrush b = new SolidBrush(cGray)) g.FillRectangle(b, cx + 12, cy + 20, 6, 8);
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 20, cy + 10, 6, 10);
                g.DrawRectangle(Pens.Black, cx + 28, cy + 10, 6, 22);
            }
            else if (catIdx == 16) // Funnel
            {
                using (SolidBrush b = new SolidBrush(cBlue))
                {
                    PointF[] t1 = new PointF[] { new PointF(cx + 4, cy + 6), new PointF(cx + w - 4, cy + 6), new PointF(cx + w - 8, cy + 14), new PointF(cx + 8, cy + 14) };
                    g.FillPolygon(b, t1);
                    PointF[] t2 = new PointF[] { new PointF(cx + 8, cy + 16), new PointF(cx + w - 8, cy + 16), new PointF(cx + w - 12, cy + 24), new PointF(cx + 12, cy + 24) };
                    g.FillPolygon(b, t2);
                }
            }
            else
            {
                g.FillRectangle(Brushes.SteelBlue, cx + 8, cy + 8, 26, 22);
            }
        }

        private void DrawMini3DCuboid(Graphics g, int x, int y, int w, int h, int d, Color c)
        {
            if (h <= 0) return;
            Color topC = ControlPaint.Light(c, 0.35f);
            Color sideC = ControlPaint.Dark(c, 0.25f);

            using (SolidBrush b = new SolidBrush(c)) g.FillRectangle(b, x, y, w, h);
            PointF[] tPts = new PointF[] { new PointF(x, y), new PointF(x + d, y - d), new PointF(x + w + d, y - d), new PointF(x + w, y) };
            using (SolidBrush b = new SolidBrush(topC)) g.FillPolygon(b, tPts);
            PointF[] sPts = new PointF[] { new PointF(x + w, y), new PointF(x + w + d, y - d), new PointF(x + w + d, y + h - d), new PointF(x + w, y + h) };
            using (SolidBrush b = new SolidBrush(sideC)) g.FillPolygon(b, sPts);

            using (Pen p = new Pen(Color.FromArgb(40, 50, 60), 0.8f))
            {
                g.DrawRectangle(p, x, y, w, h);
                g.DrawPolygon(p, tPts);
                g.DrawPolygon(p, sPts);
            }
        }

        private void DrawMini3DHorzBar(Graphics g, int x, int y, int w, int h, int d, Color c)
        {
            if (w <= 0) return;
            Color topC = ControlPaint.Light(c, 0.35f);
            Color sideC = ControlPaint.Dark(c, 0.25f);

            using (SolidBrush b = new SolidBrush(c)) g.FillRectangle(b, x, y, w, h);
            PointF[] tPts = new PointF[] { new PointF(x, y), new PointF(x + d, y - d), new PointF(x + w + d, y - d), new PointF(x + w, y) };
            using (SolidBrush b = new SolidBrush(topC)) g.FillPolygon(b, tPts);
            PointF[] sPts = new PointF[] { new PointF(x + w, y), new PointF(x + w + d, y - d), new PointF(x + w + d, y + h - d), new PointF(x + w, y + h) };
            using (SolidBrush b = new SolidBrush(sideC)) g.FillPolygon(b, sPts);

            using (Pen p = new Pen(Color.FromArgb(40, 50, 60), 0.8f))
            {
                g.DrawRectangle(p, x, y, w, h);
                g.DrawPolygon(p, tPts);
                g.DrawPolygon(p, sPts);
            }
        }

        private void PnlSubtypesStrip_MouseDown(object sender, MouseEventArgs e)
        {
            int catIdx = lstAllCategories.SelectedIndex;
            if (catIdx < 0 || catIdx >= allSubtypes.Length) catIdx = 1;
            string[] subtypes = allSubtypes[catIdx];

            int boxW = 56;
            for (int i = 0; i < subtypes.Length && i < 8; i++)
            {
                Rectangle box = new Rectangle(6 + i * (boxW + 8), 4, boxW, 46);
                if (box.Contains(e.Location))
                {
                    selectedSubtypeIndex = i;
                    lblAllTypeName.Text = subtypes[i];
                    pnlSubtypesStrip.Invalidate();
                    UpdateAllChartsSubtypes();
                    break;
                }
            }
        }

        private string GetChartTypeIdFromSelection()
        {
            int catIdx = lstAllCategories.SelectedIndex;
            return MapSubtypeToTypeId(catIdx, selectedSubtypeIndex);
        }

        private string MapSubtypeToTypeId(int catIdx, int subIdx)
        {
            switch (catIdx)
            {
                case 0: // Templates
                    if (subIdx == 1) return "Line";
                    return "Column_Clustered";

                case 1: // Column
                    if (subIdx == 1) return "Column_Stacked";
                    if (subIdx == 2) return "Column_100";
                    if (subIdx == 3) return "Column_3D_Clustered";
                    if (subIdx == 4) return "Column_3D_Stacked";
                    if (subIdx == 5) return "Column_3D_100";
                    if (subIdx == 6) return "Column_3D_Deep";
                    return "Column_Clustered";

                case 2: // Line
                    if (subIdx == 1) return "Line_Stacked";
                    if (subIdx == 2) return "Line_100";
                    if (subIdx == 3) return "Line_Markers";
                    if (subIdx == 4) return "Line_Stacked_Markers";
                    if (subIdx == 5) return "Line_100_Markers";
                    if (subIdx == 6) return "Line_3D";
                    return "Line";

                case 3: // Pie
                    if (subIdx == 1) return "Pie_3D";
                    if (subIdx == 2) return "Pie_Of_Pie";
                    if (subIdx == 3) return "Bar_Of_Pie";
                    if (subIdx == 4) return "Doughnut";
                    if (subIdx == 5) return "Doughnut_3D";
                    return "Pie";

                case 4: // Bar
                    if (subIdx == 1) return "Bar_Stacked";
                    if (subIdx == 2) return "Bar_100";
                    if (subIdx == 3) return "Bar_3D_Clustered";
                    if (subIdx == 4) return "Bar_3D_Stacked";
                    if (subIdx == 5) return "Bar_3D_100";
                    return "Bar_Clustered";

                case 5: // Area
                    if (subIdx == 1) return "Area_Stacked";
                    if (subIdx == 2) return "Area_100";
                    if (subIdx == 3) return "Area_3D";
                    if (subIdx == 4) return "Area_3D_Stacked";
                    if (subIdx == 5) return "Area_3D_100";
                    return "Area";

                case 6: // Scatter
                    if (subIdx == 1) return "Scatter_Smooth_Markers";
                    if (subIdx == 2) return "Scatter_Smooth";
                    if (subIdx == 3) return "Scatter_Straight_Markers";
                    if (subIdx == 4) return "Scatter_Straight";
                    if (subIdx == 5) return "Bubble";
                    if (subIdx == 6) return "Bubble_3D";
                    return "Scatter_Markers";

                case 7: return "Map_Filled";

                case 8: // Stock
                    if (subIdx == 1) return "Stock_OHLC";
                    if (subIdx == 2) return "Stock_VHLC";
                    if (subIdx == 3) return "Stock_VOHLC";
                    return "Stock_HLC";

                case 9: // Surface
                    if (subIdx == 1) return "Surface_Wireframe";
                    if (subIdx == 2) return "Surface_Contour";
                    if (subIdx == 3) return "Surface_WireframeContour";
                    return "Surface_3D";

                case 10: // Radar
                    if (subIdx == 1) return "Radar_Markers";
                    if (subIdx == 2) return "Radar_Filled";
                    return "Radar";

                case 11: return "Treemap";
                case 12: return "Sunburst";

                case 13: // Histogram
                    if (subIdx == 1) return "Histogram_Pareto";
                    return "Histogram";

                case 14: return "Box_Whisker";
                case 15: return "Waterfall";
                case 16: return "Funnel";

                case 17: // Combo
                    if (subIdx == 1) return "Combo_Column_Line_Secondary";
                    if (subIdx == 2) return "Combo_Area_Column";
                    return "Combo_Column_Line";

                default: return "Column_Clustered";
            }
        }
    }
}
