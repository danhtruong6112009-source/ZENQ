using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class ChartGalleryDropdownPopup : Form
    {
        public enum GalleryKind
        {
            ColumnAndBar,
            Hierarchy,
            WaterfallStockSurfaceRadar,
            LineAndArea,
            PieAndDoughnut,
            ScatterAndBubble,
            Statistic,
            Combo
        }

        private struct GallerySubtypeItem
        {
            public string TypeId;
            public string Name;
            public Rectangle Bounds;
            public int CategoryIndex;
            public int SubtypeIndex;

            public GallerySubtypeItem(string id, string name, int catIdx, int subIdx)
            {
                TypeId = id;
                Name = name;
                CategoryIndex = catIdx;
                SubtypeIndex = subIdx;
                Bounds = Rectangle.Empty;
            }
        }

        private struct SectionGroup
        {
            public string Header;
            public List<GallerySubtypeItem> Items;
            public SectionGroup(string hdr)
            {
                Header = hdr;
                Items = new List<GallerySubtypeItem>();
            }
        }

        private GalleryKind kind;
        private List<SectionGroup> sections = new List<SectionGroup>();
        private string moreChartsText = "More Column Charts...";
        private int moreChartsCategoryIndex = 2;
        private Rectangle moreChartsRect = Rectangle.Empty;
        private int hoveredItemIndex = -1;
        private bool isHoveringMore = false;
        private List<GallerySubtypeItem> flatItems = new List<GallerySubtypeItem>();

        private Action<string> onSelected;
        private Action<int> onOpenDialog;

        public ChartGalleryDropdownPopup(GalleryKind kind, Action<string> onSelected, Action<int> onOpenDialog)
        {
            this.kind = kind;
            this.onSelected = onSelected;
            this.onOpenDialog = onOpenDialog;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.BackColor = Color.White;
            this.DoubleBuffered = true;
            this.Font = new Font("Segoe UI", 9f);

            BuildSections();
            CalculateLayout();
        }

        private void BuildSections()
        {
            sections.Clear();
            flatItems.Clear();

            switch (kind)
            {
                case GalleryKind.ColumnAndBar:
                    moreChartsText = "More Column Charts...";
                    moreChartsCategoryIndex = 1; // Column

                    SectionGroup g2dCol = new SectionGroup("2-D Column");
                    g2dCol.Items.Add(new GallerySubtypeItem("Column_Clustered", "Clustered Column", 1, 0));
                    g2dCol.Items.Add(new GallerySubtypeItem("Column_Stacked", "Stacked Column", 1, 1));
                    g2dCol.Items.Add(new GallerySubtypeItem("Column_100", "100% Stacked Column", 1, 2));
                    sections.Add(g2dCol);

                    SectionGroup g3dCol = new SectionGroup("3-D Column");
                    g3dCol.Items.Add(new GallerySubtypeItem("Column_3D_Clustered", "3-D Clustered Column", 1, 3));
                    g3dCol.Items.Add(new GallerySubtypeItem("Column_3D_Stacked", "3-D Stacked Column", 1, 4));
                    g3dCol.Items.Add(new GallerySubtypeItem("Column_3D_100", "3-D 100% Stacked Column", 1, 5));
                    g3dCol.Items.Add(new GallerySubtypeItem("Column_3D_Deep", "3-D Column", 1, 6));
                    sections.Add(g3dCol);

                    SectionGroup g2dBar = new SectionGroup("2-D Bar");
                    g2dBar.Items.Add(new GallerySubtypeItem("Bar_Clustered", "Clustered Bar", 4, 0));
                    g2dBar.Items.Add(new GallerySubtypeItem("Bar_Stacked", "Stacked Bar", 4, 1));
                    g2dBar.Items.Add(new GallerySubtypeItem("Bar_100", "100% Stacked Bar", 4, 2));
                    sections.Add(g2dBar);

                    SectionGroup g3dBar = new SectionGroup("3-D Bar");
                    g3dBar.Items.Add(new GallerySubtypeItem("Bar_3D_Clustered", "3-D Clustered Bar", 4, 3));
                    g3dBar.Items.Add(new GallerySubtypeItem("Bar_3D_Stacked", "3-D Stacked Bar", 4, 4));
                    g3dBar.Items.Add(new GallerySubtypeItem("Bar_3D_100", "3-D 100% Stacked Bar", 4, 5));
                    sections.Add(g3dBar);
                    break;

                case GalleryKind.Hierarchy:
                    moreChartsText = "More Hierarchy Charts...";
                    moreChartsCategoryIndex = 11; // Treemap

                    SectionGroup gTree = new SectionGroup("Treemap");
                    gTree.Items.Add(new GallerySubtypeItem("Treemap", "Treemap", 11, 0));
                    sections.Add(gTree);

                    SectionGroup gSun = new SectionGroup("Sunburst");
                    gSun.Items.Add(new GallerySubtypeItem("Sunburst", "Sunburst", 12, 0));
                    sections.Add(gSun);
                    break;

                case GalleryKind.WaterfallStockSurfaceRadar:
                    moreChartsText = "More Stock Charts...";
                    moreChartsCategoryIndex = 8; // Stock

                    SectionGroup gWf = new SectionGroup("Waterfall");
                    gWf.Items.Add(new GallerySubtypeItem("Waterfall", "Waterfall", 15, 0));
                    sections.Add(gWf);

                    SectionGroup gFn = new SectionGroup("Funnel");
                    gFn.Items.Add(new GallerySubtypeItem("Funnel", "Funnel", 16, 0));
                    sections.Add(gFn);

                    SectionGroup gStk = new SectionGroup("Stock");
                    gStk.Items.Add(new GallerySubtypeItem("Stock_HLC", "High-Low-Close", 8, 0));
                    gStk.Items.Add(new GallerySubtypeItem("Stock_OHLC", "Open-High-Low-Close", 8, 1));
                    gStk.Items.Add(new GallerySubtypeItem("Stock_VHLC", "Volume-High-Low-Close", 8, 2));
                    gStk.Items.Add(new GallerySubtypeItem("Stock_VOHLC", "Volume-Open-High-Low-Close", 8, 3));
                    sections.Add(gStk);

                    SectionGroup gSurf = new SectionGroup("Surface");
                    gSurf.Items.Add(new GallerySubtypeItem("Surface_3D", "3-D Surface", 9, 0));
                    gSurf.Items.Add(new GallerySubtypeItem("Surface_Wireframe", "Wireframe 3-D Surface", 9, 1));
                    gSurf.Items.Add(new GallerySubtypeItem("Surface_Contour", "Contour", 9, 2));
                    gSurf.Items.Add(new GallerySubtypeItem("Surface_WireframeContour", "Wireframe Contour", 9, 3));
                    sections.Add(gSurf);

                    SectionGroup gRad = new SectionGroup("Radar");
                    gRad.Items.Add(new GallerySubtypeItem("Radar", "Radar", 10, 0));
                    gRad.Items.Add(new GallerySubtypeItem("Radar_Markers", "Radar with Markers", 10, 1));
                    gRad.Items.Add(new GallerySubtypeItem("Radar_Filled", "Filled Radar", 10, 2));
                    sections.Add(gRad);
                    break;

                case GalleryKind.LineAndArea:
                    moreChartsText = "More Line Charts...";
                    moreChartsCategoryIndex = 2; // Line

                    SectionGroup g2dLine = new SectionGroup("2-D Line");
                    g2dLine.Items.Add(new GallerySubtypeItem("Line", "Line", 2, 0));
                    g2dLine.Items.Add(new GallerySubtypeItem("Line_Stacked", "Stacked Line", 2, 1));
                    g2dLine.Items.Add(new GallerySubtypeItem("Line_100", "100% Stacked Line", 2, 2));
                    g2dLine.Items.Add(new GallerySubtypeItem("Line_Markers", "Line with Markers", 2, 3));
                    sections.Add(g2dLine);

                    SectionGroup g3dLine = new SectionGroup("3-D Line");
                    g3dLine.Items.Add(new GallerySubtypeItem("Line_3D", "3-D Line", 2, 4));
                    sections.Add(g3dLine);

                    SectionGroup g2dArea = new SectionGroup("2-D Area");
                    g2dArea.Items.Add(new GallerySubtypeItem("Area", "Area", 5, 0));
                    g2dArea.Items.Add(new GallerySubtypeItem("Area_Stacked", "Stacked Area", 5, 1));
                    g2dArea.Items.Add(new GallerySubtypeItem("Area_100", "100% Stacked Area", 5, 2));
                    sections.Add(g2dArea);

                    SectionGroup g3dArea = new SectionGroup("3-D Area");
                    g3dArea.Items.Add(new GallerySubtypeItem("Area_3D", "3-D Area", 5, 3));
                    sections.Add(g3dArea);
                    break;

                case GalleryKind.PieAndDoughnut:
                    moreChartsText = "More Pie Charts...";
                    moreChartsCategoryIndex = 3; // Pie

                    SectionGroup g2dPie = new SectionGroup("2-D Pie");
                    g2dPie.Items.Add(new GallerySubtypeItem("Pie", "Pie", 3, 0));
                    g2dPie.Items.Add(new GallerySubtypeItem("Pie_Of_Pie", "Pie of Pie", 3, 2));
                    g2dPie.Items.Add(new GallerySubtypeItem("Bar_Of_Pie", "Bar of Pie", 3, 3));
                    sections.Add(g2dPie);

                    SectionGroup g3dPie = new SectionGroup("3-D Pie");
                    g3dPie.Items.Add(new GallerySubtypeItem("Pie_3D", "3-D Pie", 3, 1));
                    sections.Add(g3dPie);

                    SectionGroup gDoughnut = new SectionGroup("Doughnut");
                    gDoughnut.Items.Add(new GallerySubtypeItem("Doughnut", "Doughnut", 3, 4));
                    gDoughnut.Items.Add(new GallerySubtypeItem("Doughnut_3D", "3-D Doughnut", 3, 4));
                    sections.Add(gDoughnut);
                    break;

                case GalleryKind.ScatterAndBubble:
                    moreChartsText = "More Scatter Charts...";
                    moreChartsCategoryIndex = 6; // Scatter

                    SectionGroup gScat = new SectionGroup("X Y (Scatter)");
                    gScat.Items.Add(new GallerySubtypeItem("Scatter_Markers", "Scatter with Markers", 6, 0));
                    gScat.Items.Add(new GallerySubtypeItem("Scatter_Smooth", "Scatter with Smooth Lines", 6, 1));
                    gScat.Items.Add(new GallerySubtypeItem("Scatter_Straight", "Scatter with Straight Lines", 6, 2));
                    sections.Add(gScat);

                    SectionGroup gBub = new SectionGroup("Bubble");
                    gBub.Items.Add(new GallerySubtypeItem("Bubble", "Bubble", 6, 3));
                    gBub.Items.Add(new GallerySubtypeItem("Bubble_3D", "3-D Bubble", 6, 3));
                    sections.Add(gBub);
                    break;

                case GalleryKind.Statistic:
                    moreChartsText = "More Statistic Charts...";
                    moreChartsCategoryIndex = 13; // Histogram

                    SectionGroup gHist = new SectionGroup("Histogram");
                    gHist.Items.Add(new GallerySubtypeItem("Histogram", "Histogram", 13, 0));
                    gHist.Items.Add(new GallerySubtypeItem("Histogram_Pareto", "Pareto", 13, 1));
                    sections.Add(gHist);

                    SectionGroup gBox = new SectionGroup("Box & Whisker");
                    gBox.Items.Add(new GallerySubtypeItem("Box_Whisker", "Box & Whisker", 14, 0));
                    sections.Add(gBox);
                    break;

                case GalleryKind.Combo:
                    moreChartsText = "More Combo Charts...";
                    moreChartsCategoryIndex = 17; // Combo

                    SectionGroup gCmb = new SectionGroup("Combo");
                    gCmb.Items.Add(new GallerySubtypeItem("Combo_Column_Line", "Clustered Column - Line", 17, 0));
                    gCmb.Items.Add(new GallerySubtypeItem("Combo_Column_Line_Secondary", "Column - Line on Secondary Axis", 17, 1));
                    gCmb.Items.Add(new GallerySubtypeItem("Combo_Area_Column", "Stacked Area - Clustered Column", 17, 2));
                    sections.Add(gCmb);
                    break;
            }

            foreach (var sec in sections)
            {
                foreach (var item in sec.Items)
                {
                    flatItems.Add(item);
                }
            }
        }

        private void CalculateLayout()
        {
            int itemW = 54;
            int itemH = 46;
            int itemSpacing = 8;
            int sectionHdrH = 22;
            int padding = 12;
            int currentY = padding;

            int maxCols = 4;
            foreach (var sec in sections)
            {
                maxCols = Math.Max(maxCols, sec.Items.Count);
            }
            if (maxCols < 3) maxCols = 3;

            int totalW = padding * 2 + maxCols * itemW + (maxCols - 1) * itemSpacing;

            for (int s = 0; s < sections.Count; s++)
            {
                SectionGroup sec = sections[s];
                currentY += sectionHdrH;

                for (int i = 0; i < sec.Items.Count; i++)
                {
                    GallerySubtypeItem it = sec.Items[i];
                    int ix = padding + i * (itemW + itemSpacing);
                    it.Bounds = new Rectangle(ix, currentY, itemW, itemH);
                    sec.Items[i] = it;

                    // Update flat item
                    for (int f = 0; f < flatItems.Count; f++)
                    {
                        if (flatItems[f].TypeId == it.TypeId)
                        {
                            flatItems[f] = it;
                            break;
                        }
                    }
                }
                currentY += itemH + 12;
            }

            // Footer link
            currentY += 4;
            moreChartsRect = new Rectangle(padding, currentY, totalW - padding * 2, 28);
            currentY += 34;

            this.Size = new Size(totalW, currentY);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.Close();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prevHover = hoveredItemIndex;
            bool prevHoverMore = isHoveringMore;

            hoveredItemIndex = -1;
            for (int i = 0; i < flatItems.Count; i++)
            {
                if (flatItems[i].Bounds.Contains(e.Location))
                {
                    hoveredItemIndex = i;
                    break;
                }
            }

            isHoveringMore = moreChartsRect.Contains(e.Location);

            if (prevHover != hoveredItemIndex || prevHoverMore != isHoveringMore)
            {
                this.Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoveredItemIndex = -1;
            isHoveringMore = false;
            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            for (int i = 0; i < flatItems.Count; i++)
            {
                if (flatItems[i].Bounds.Contains(e.Location))
                {
                    string tid = flatItems[i].TypeId;
                    this.Close();
                    if (onSelected != null) onSelected(tid);
                    return;
                }
            }

            if (moreChartsRect.Contains(e.Location))
            {
                int catIdx = moreChartsCategoryIndex;
                this.Close();
                if (onOpenDialog != null) onOpenDialog(catIdx);
                return;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Border & Shadow background
            using (Pen borderPen = new Pen(Color.FromArgb(204, 208, 214), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            using (Font hdrFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush hdrBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                int curY = 10;
                int itemH = 46;

                for (int s = 0; s < sections.Count; s++)
                {
                    SectionGroup sec = sections[s];
                    g.DrawString(sec.Header, hdrFont, hdrBrush, 12, curY);
                    curY += 22;

                    for (int i = 0; i < sec.Items.Count; i++)
                    {
                        GallerySubtypeItem it = sec.Items[i];
                        bool isHovered = (hoveredItemIndex >= 0 && flatItems[hoveredItemIndex].TypeId == it.TypeId);

                        if (isHovered)
                        {
                            using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(238, 242, 255)))
                            {
                                g.FillRectangle(hoverBg, it.Bounds);
                            }
                            using (Pen hoverP = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                            {
                                g.DrawRectangle(hoverP, it.Bounds);
                            }
                        }
                        else
                        {
                            using (Pen p = new Pen(Color.FromArgb(235, 238, 242), 1f))
                            {
                                g.DrawRectangle(p, it.Bounds);
                            }
                        }

                        // Draw vector icon for this subtype
                        DrawTileVectorIcon(g, it.Bounds, it.TypeId);
                    }

                    curY += itemH + 12;

                    // Separator line between sections
                    if (s < sections.Count - 1)
                    {
                        using (Pen divPen = new Pen(Color.FromArgb(240, 242, 245), 1f))
                        {
                            g.DrawLine(divPen, 10, curY - 6, this.Width - 10, curY - 6);
                        }
                    }
                }
            }

            // Separator before footer
            using (Pen divPen = new Pen(Color.FromArgb(230, 235, 240), 1f))
            {
                g.DrawLine(divPen, 0, moreChartsRect.Top - 3, this.Width, moreChartsRect.Top - 3);
            }

            // Footer Link ("More Column Charts...")
            if (isHoveringMore)
            {
                using (SolidBrush hBg = new SolidBrush(Color.FromArgb(240, 244, 250)))
                {
                    g.FillRectangle(hBg, moreChartsRect);
                }
            }

            // Footer icon
            DrawMoreChartsFooterIcon(g, moreChartsRect.Left + 4, moreChartsRect.Top + 6);

            using (Font moreFont = new Font("Segoe UI", 9.2f, isHoveringMore ? (FontStyle.Underline) : FontStyle.Regular))
            using (SolidBrush moreBrush = new SolidBrush(isHoveringMore ? Color.FromArgb(0, 90, 180) : Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(moreChartsText, moreFont, moreBrush, moreChartsRect.Left + 28, moreChartsRect.Top + 4);
            }
        }

        private void DrawMoreChartsFooterIcon(Graphics g, int x, int y)
        {
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(0, 120, 212))) g.FillRectangle(b1, x + 2, y + 8, 3, 8);
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(16, 124, 65))) g.FillRectangle(b2, x + 7, y + 3, 3, 13);
            using (SolidBrush b3 = new SolidBrush(Color.FromArgb(242, 153, 0))) g.FillRectangle(b3, x + 12, y + 6, 3, 10);
            using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
            {
                g.DrawRectangle(p, x + 2, y + 8, 3, 8);
                g.DrawRectangle(p, x + 7, y + 3, 3, 13);
                g.DrawRectangle(p, x + 12, y + 6, 3, 10);
            }
        }

        private void DrawTileVectorIcon(Graphics g, Rectangle r, string typeId)
        {
            int cx = r.X + 4;
            int cy = r.Y + 4;
            int w = r.Width - 8;
            int h = r.Height - 8;

            Color cBlue = Color.FromArgb(91, 155, 213);
            Color cGray = Color.FromArgb(175, 180, 188);
            Color cDarkBlue = Color.FromArgb(68, 114, 196);
            Color cOrange = Color.FromArgb(237, 125, 49);

            // Column Charts (2-D & 3-D)
            if (typeId == "Column_Clustered")
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 6, cy + 12, 6, h - 14);
                g.DrawRectangle(Pens.Black, cx + 6, cy + 12, 6, h - 14);
                g.DrawRectangle(Pens.Black, cx + 14, cy + 20, 6, h - 22);
                using (SolidBrush b = new SolidBrush(cGray)) g.FillRectangle(b, cx + 24, cy + 16, 6, h - 18);
                g.DrawRectangle(Pens.Black, cx + 24, cy + 16, 6, h - 18);
                g.DrawRectangle(Pens.Black, cx + 32, cy + 8, 6, h - 10);
            }
            else if (typeId == "Column_Stacked")
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                g.DrawRectangle(Pens.Black, cx + 10, cy + 20, 10, h - 22);
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 10, cy + 10, 10, 10);
                g.DrawRectangle(Pens.Black, cx + 10, cy + 10, 10, 10);

                g.DrawRectangle(Pens.Black, cx + 26, cy + 16, 10, h - 18);
                using (SolidBrush b = new SolidBrush(cGray)) g.FillRectangle(b, cx + 26, cy + 4, 10, 12);
                g.DrawRectangle(Pens.Black, cx + 26, cy + 4, 10, 12);
            }
            else if (typeId == "Column_100")
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                g.DrawRectangle(Pens.Black, cx + 10, cy + 18, 10, h - 20);
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 10, cy + 4, 10, 14);
                g.DrawRectangle(Pens.Black, cx + 10, cy + 4, 10, 14);

                g.DrawRectangle(Pens.Black, cx + 26, cy + 14, 10, h - 16);
                using (SolidBrush b = new SolidBrush(cGray)) g.FillRectangle(b, cx + 26, cy + 4, 10, 10);
                g.DrawRectangle(Pens.Black, cx + 26, cy + 4, 10, 10);
            }
            else if (typeId.StartsWith("Column_3D"))
            {
                // 3D Isometric Axes
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 8, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 4, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + 8, cy + h - 8);
                }
                // 3D Cuboids
                DrawMini3DCuboid(g, cx + 10, cy + 12, 8, h - 18, 4, cBlue);
                DrawMini3DCuboid(g, cx + 22, cy + 18, 8, h - 24, 4, Color.White);
                DrawMini3DCuboid(g, cx + 32, cy + 14, 8, h - 20, 4, cGray);
            }
            // Bar Charts (2-D & 3-D Horizontal)
            else if (typeId == "Bar_Clustered")
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 3, cy + 6, 28, 5);
                g.DrawRectangle(Pens.Black, cx + 3, cy + 6, 28, 5);
                g.DrawRectangle(Pens.Black, cx + 3, cy + 13, 18, 5);
                using (SolidBrush b = new SolidBrush(cGray)) g.FillRectangle(b, cx + 3, cy + 20, 14, 5);
                g.DrawRectangle(Pens.Black, cx + 3, cy + 20, 14, 5);
                g.DrawRectangle(Pens.Black, cx + 3, cy + 27, 22, 5);
            }
            else if (typeId == "Bar_Stacked" || typeId == "Bar_100")
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                g.DrawRectangle(Pens.Black, cx + 3, cy + 8, 14, 8);
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 17, cy + 8, 18, 8);
                g.DrawRectangle(Pens.Black, cx + 17, cy + 8, 18, 8);

                g.DrawRectangle(Pens.Black, cx + 3, cy + 20, 20, 8);
                using (SolidBrush b = new SolidBrush(cGray)) g.FillRectangle(b, cx + 23, cy + 20, 14, 8);
                g.DrawRectangle(Pens.Black, cx + 23, cy + 20, 14, 8);
            }
            else if (typeId.StartsWith("Bar_3D"))
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 4);
                    g.DrawLine(p, cx + 2, cy + h - 4, cx + w - 2, cy + h - 4);
                    g.DrawLine(p, cx + 2, cy + h - 4, cx + 8, cy + h - 10);
                }
                DrawMini3DHorzBar(g, cx + 6, cy + 8, 26, 6, 4, cBlue);
                DrawMini3DHorzBar(g, cx + 6, cy + 18, 18, 6, 4, Color.White);
                DrawMini3DHorzBar(g, cx + 6, cy + 26, 22, 6, 4, cGray);
            }
            // Hierarchy: Treemap & Sunburst
            else if (typeId == "Treemap")
            {
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 2, cy + 2, 20, 32);
                g.DrawRectangle(Pens.Black, cx + 2, cy + 2, 20, 32);
                g.DrawRectangle(Pens.Black, cx + 22, cy + 2, 18, 14);
                g.DrawRectangle(Pens.Black, cx + 22, cy + 16, 18, 18);
            }
            else if (typeId == "Sunburst")
            {
                g.DrawEllipse(Pens.Black, cx + 6, cy + 4, 30, 30);
                g.DrawEllipse(Pens.Black, cx + 13, cy + 11, 16, 16);
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillPie(b, cx + 6, cy + 4, 30, 30, 270, 90);
                g.DrawPie(Pens.Black, cx + 6, cy + 4, 30, 30, 270, 90);
                g.DrawLine(Pens.Black, cx + 6, cy + 19, cx + 13, cy + 19);
                g.DrawLine(Pens.Black, cx + 29, cy + 19, cx + 36, cy + 19);
                g.DrawLine(Pens.Black, cx + 21, cy + 27, cx + 21, cy + 34);
                using (SolidBrush wB = new SolidBrush(Color.White)) g.FillEllipse(wB, cx + 16, cy + 14, 10, 10);
                g.DrawEllipse(Pens.Black, cx + 16, cy + 14, 10, 10);
            }
            // Waterfall & Funnel
            else if (typeId == "Waterfall")
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                g.DrawRectangle(Pens.Black, cx + 4, cy + 16, 6, 16);
                using (SolidBrush b = new SolidBrush(cGray)) g.FillRectangle(b, cx + 12, cy + 20, 6, 8);
                g.DrawRectangle(Pens.Black, cx + 12, cy + 20, 6, 8);
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 20, cy + 10, 6, 10);
                g.DrawRectangle(Pens.Black, cx + 20, cy + 10, 6, 10);
                g.DrawRectangle(Pens.Black, cx + 28, cy + 10, 6, 22);
            }
            else if (typeId == "Funnel")
            {
                using (SolidBrush b = new SolidBrush(cBlue))
                {
                    PointF[] t1 = new PointF[] { new PointF(cx + 4, cy + 6), new PointF(cx + w - 4, cy + 6), new PointF(cx + w - 8, cy + 14), new PointF(cx + 8, cy + 14) };
                    g.FillPolygon(b, t1);
                    g.DrawPolygon(Pens.Black, t1);

                    PointF[] t2 = new PointF[] { new PointF(cx + 8, cy + 16), new PointF(cx + w - 8, cy + 16), new PointF(cx + w - 12, cy + 24), new PointF(cx + 12, cy + 24) };
                    g.FillPolygon(b, t2);
                    g.DrawPolygon(Pens.Black, t2);

                    PointF[] t3 = new PointF[] { new PointF(cx + 12, cy + 26), new PointF(cx + w - 12, cy + 26), new PointF(cx + w - 12, cy + 32), new PointF(cx + 12, cy + 32) };
                    g.FillPolygon(b, t3);
                    g.DrawPolygon(Pens.Black, t3);
                }
            }
            // Stock
            else if (typeId.StartsWith("Stock"))
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                for (int i = 0; i < 4; i++)
                {
                    int kx = cx + 8 + i * 8;
                    int topY = cy + 6 + (i % 2) * 6;
                    int botY = cy + h - 6 - ((i + 1) % 2) * 4;
                    g.DrawLine(Pens.Black, kx, topY, kx, botY);
                    g.DrawLine(Pens.Black, kx - 3, topY + 4, kx, topY + 4);
                    g.DrawLine(Pens.Black, kx, botY - 4, kx + 3, botY - 4);
                }
            }
            // Surface (3D topological mesh)
            else if (typeId.StartsWith("Surface"))
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 4, cy + 8, cx + 4, cy + h - 4);
                    g.DrawLine(p, cx + 4, cy + h - 4, cx + w - 4, cy + h - 4);
                    g.DrawLine(p, cx + 4, cy + 8, cx + 18, cy + 2);
                }
                using (GraphicsPath sp = new GraphicsPath())
                {
                    PointF[] sPts = new PointF[] { new PointF(cx + 6, cy + 26), new PointF(cx + 20, cy + 10), new PointF(cx + 34, cy + 24), new PointF(cx + 20, cy + 30) };
                    sp.AddPolygon(sPts);
                    using (SolidBrush sb = new SolidBrush(cBlue)) g.FillPath(sb, sp);
                    g.DrawPolygon(Pens.Black, sPts);
                }
                g.DrawLine(Pens.Black, cx + 13, cy + 18, cx + 27, cy + 27);
                g.DrawLine(Pens.Black, cx + 20, cy + 10, cx + 20, cy + 30);
            }
            // Radar
            else if (typeId.StartsWith("Radar"))
            {
                PointF center = new PointF(cx + w / 2f, cy + h / 2f);
                float radius = 14f;
                PointF[] webPts = new PointF[5];
                for (int a = 0; a < 5; a++)
                {
                    double ang = -Math.PI / 2.0 + (a * 2.0 * Math.PI / 5.0);
                    webPts[a] = new PointF(center.X + (float)(Math.Cos(ang) * radius), center.Y + (float)(Math.Sin(ang) * radius));
                    g.DrawLine(Pens.LightGray, center, webPts[a]);
                }
                g.DrawPolygon(Pens.LightGray, webPts);

                PointF[] dataPts = new PointF[5];
                for (int a = 0; a < 5; a++)
                {
                    double ang = -Math.PI / 2.0 + (a * 2.0 * Math.PI / 5.0);
                    float rDist = radius * (0.4f + (a % 3) * 0.28f);
                    dataPts[a] = new PointF(center.X + (float)(Math.Cos(ang) * rDist), center.Y + (float)(Math.Sin(ang) * rDist));
                }
                using (SolidBrush b = new SolidBrush(Color.FromArgb(120, cBlue.R, cBlue.G, cBlue.B))) g.FillPolygon(b, dataPts);
                using (Pen p = new Pen(cDarkBlue, 1.4f)) g.DrawPolygon(p, dataPts);
            }
            // Pie & Doughnut
            else if (typeId == "Pie" || typeId == "Pie_3D")
            {
                if (typeId == "Pie_3D")
                {
                    for (int d = 5; d >= 1; d--)
                    {
                        using (SolidBrush db = new SolidBrush(Color.FromArgb(40, 80, 150)))
                        {
                            g.FillPie(db, cx + 4, cy + 6 + d, 32, 22, 0, 360);
                        }
                    }
                    using (SolidBrush b = new SolidBrush(cBlue)) g.FillPie(b, cx + 4, cy + 6, 32, 22, 0, 240);
                    using (SolidBrush b = new SolidBrush(cOrange)) g.FillPie(b, cx + 4, cy + 6, 32, 22, 240, 120);
                    g.DrawPie(Pens.White, cx + 4, cy + 6, 32, 22, 0, 240);
                    g.DrawPie(Pens.White, cx + 4, cy + 6, 32, 22, 240, 120);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(cBlue)) g.FillPie(b, cx + 6, cy + 4, 28, 28, 0, 240);
                    using (SolidBrush b = new SolidBrush(cOrange)) g.FillPie(b, cx + 6, cy + 4, 28, 28, 240, 120);
                    g.DrawPie(Pens.White, cx + 6, cy + 4, 28, 28, 0, 240);
                    g.DrawPie(Pens.White, cx + 6, cy + 4, 28, 28, 240, 120);
                }
            }
            else if (typeId.StartsWith("Doughnut"))
            {
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillPie(b, cx + 6, cy + 4, 28, 28, 0, 240);
                using (SolidBrush b = new SolidBrush(cOrange)) g.FillPie(b, cx + 6, cy + 4, 28, 28, 240, 120);
                using (SolidBrush wb = new SolidBrush(Color.White)) g.FillEllipse(wb, cx + 13, cy + 11, 14, 14);
                g.DrawEllipse(Pens.White, cx + 13, cy + 11, 14, 14);
            }
            // Line & Area
            else if (typeId.StartsWith("Line"))
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                using (Pen p = new Pen(cDarkBlue, 2f))
                {
                    g.DrawLine(p, cx + 4, cy + 24, cx + 14, cy + 12);
                    g.DrawLine(p, cx + 14, cy + 12, cx + 24, cy + 18);
                    g.DrawLine(p, cx + 24, cy + 18, cx + 36, cy + 6);
                }
            }
            else if (typeId.StartsWith("Area"))
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                PointF[] aPts = new PointF[] { new PointF(cx + 4, cy + h - 2), new PointF(cx + 4, cy + 22), new PointF(cx + 16, cy + 12), new PointF(cx + 28, cy + 18), new PointF(cx + 36, cy + 6), new PointF(cx + 36, cy + h - 2) };
                using (SolidBrush b = new SolidBrush(Color.FromArgb(140, cBlue.R, cBlue.G, cBlue.B))) g.FillPolygon(b, aPts);
                using (Pen p = new Pen(cDarkBlue, 1.5f)) g.DrawLines(p, new PointF[] { aPts[1], aPts[2], aPts[3], aPts[4] });
            }
            // Scatter
            else if (typeId.StartsWith("Scatter") || typeId.StartsWith("Bubble"))
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                int[] pxs = new int[] { 8, 14, 20, 26, 32 };
                int[] pys = new int[] { 22, 12, 16, 6, 14 };
                for (int i = 0; i < 5; i++)
                {
                    g.FillEllipse(Brushes.Navy, cx + pxs[i], cy + pys[i], 4, 4);
                }
            }
            // Histogram
            else if (typeId.StartsWith("Histogram") || typeId.StartsWith("Box"))
            {
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1f))
                {
                    g.DrawLine(p, cx + 2, cy + 2, cx + 2, cy + h - 2);
                    g.DrawLine(p, cx + 2, cy + h - 2, cx + w - 2, cy + h - 2);
                }
                using (SolidBrush b = new SolidBrush(cBlue))
                {
                    g.FillRectangle(b, cx + 6, cy + 20, 6, h - 22);
                    g.FillRectangle(b, cx + 13, cy + 10, 8, h - 12);
                    g.FillRectangle(b, cx + 22, cy + 6, 8, h - 8);
                    g.FillRectangle(b, cx + 31, cy + 16, 6, h - 18);
                }
                g.DrawRectangle(Pens.Black, cx + 6, cy + 20, 6, h - 22);
                g.DrawRectangle(Pens.Black, cx + 13, cy + 10, 8, h - 12);
                g.DrawRectangle(Pens.Black, cx + 22, cy + 6, 8, h - 8);
                g.DrawRectangle(Pens.Black, cx + 31, cy + 16, 6, h - 18);
            }
            // Default / Combo
            else
            {
                using (SolidBrush b = new SolidBrush(cBlue)) g.FillRectangle(b, cx + 6, cy + 12, 8, h - 14);
                g.DrawRectangle(Pens.Black, cx + 6, cy + 12, 8, h - 14);
                using (Pen p = new Pen(cOrange, 2f)) g.DrawLine(p, cx + 4, cy + 20, cx + 36, cy + 8);
            }
        }

        private void DrawMini3DCuboid(Graphics g, int x, int y, int w, int h, int d, Color c)
        {
            if (h <= 0) return;
            Color topC = ControlPaint.Light(c, 0.35f);
            Color sideC = ControlPaint.Dark(c, 0.25f);

            // Front
            using (SolidBrush b = new SolidBrush(c)) g.FillRectangle(b, x, y, w, h);
            // Top
            PointF[] tPts = new PointF[] { new PointF(x, y), new PointF(x + d, y - d), new PointF(x + w + d, y - d), new PointF(x + w, y) };
            using (SolidBrush b = new SolidBrush(topC)) g.FillPolygon(b, tPts);
            // Side
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
    }
}
