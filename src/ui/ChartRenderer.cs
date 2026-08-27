using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public static class ChartRenderer
    {
        public static void DrawChart(Graphics g, Rectangle bounds, DocChartData data, bool isSelected, float zoomLevel)
        {
            if (bounds.Width <= 20 || bounds.Height <= 20) return;
            if (data == null) data = DocChartData.CreateDefault("Column_Clustered");

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Background Card & Border
            using (SolidBrush bgB = new SolidBrush(Color.White))
            using (Pen borderP = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.FillRectangle(bgB, bounds);
                g.DrawRectangle(borderP, bounds);
            }

            // Title
            int titleH = (int)(28 * zoomLevel);
            if (!string.IsNullOrEmpty(data.Title))
            {
                using (Font tFont = new Font("Segoe UI", Math.Max(8f, 10.5f * zoomLevel), FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    Rectangle tRect = new Rectangle(bounds.X, bounds.Y + (int)(4 * zoomLevel), bounds.Width, titleH);
                    g.DrawString(data.Title, tFont, new SolidBrush(Color.FromArgb(15, 23, 42)), tRect, sf);
                }
            }

            // Color Palettes (MS Office Standard Accent Colors)
            Color[] colors = new Color[]
            {
                Color.FromArgb(68, 114, 196),  // Accent 1 (Blue)
                Color.FromArgb(237, 125, 49),  // Accent 2 (Orange)
                Color.FromArgb(165, 165, 165), // Accent 3 (Gray)
                Color.FromArgb(255, 192, 0),   // Accent 4 (Gold)
                Color.FromArgb(91, 155, 213),  // Accent 5 (Light Blue)
                Color.FromArgb(112, 173, 71),  // Accent 6 (Green)
                Color.FromArgb(112, 48, 160),  // Accent 7 (Purple)
                Color.FromArgb(192, 0, 0)      // Accent 8 (Dark Red)
            };

            int legendH = (int)(24 * zoomLevel);
            int plotX = bounds.X + (int)(48 * zoomLevel);
            int plotY = bounds.Y + titleH + (int)(6 * zoomLevel);
            int plotW = bounds.Width - (int)(68 * zoomLevel);
            int plotH = bounds.Height - titleH - legendH - (int)(28 * zoomLevel);

            if (plotW <= 20 || plotH <= 20) return;

            string cType = data.ChartType ?? "Column_Clustered";

            if (cType.StartsWith("Pie") || cType.StartsWith("Doughnut"))
            {
                DrawPieChart(g, bounds, data, colors, plotX, plotY, plotW, plotH, cType.Contains("3D") || cType.Contains("3-D"), cType.Contains("Doughnut"), zoomLevel);
            }
            else if (cType.StartsWith("Bar"))
            {
                DrawBarChart(g, data, colors, plotX, plotY, plotW, plotH, cType.Contains("3D") || cType.Contains("3-D"), cType.Contains("Stacked") || cType.Contains("100"), zoomLevel);
            }
            else if (cType.StartsWith("Line"))
            {
                DrawLineChart(g, data, colors, plotX, plotY, plotW, plotH, cType.Contains("3D") || cType.Contains("3-D"), cType.Contains("Stacked") || cType.Contains("100"), zoomLevel);
            }
            else if (cType.StartsWith("Area"))
            {
                DrawAreaChart(g, data, colors, plotX, plotY, plotW, plotH, cType.Contains("3D") || cType.Contains("3-D"), cType.Contains("Stacked") || cType.Contains("100"), zoomLevel);
            }
            else if (cType.StartsWith("Surface"))
            {
                DrawSurfaceChart(g, data, colors, plotX, plotY, plotW, plotH, cType.Contains("Wireframe"), zoomLevel);
            }
            else if (cType.StartsWith("Radar"))
            {
                DrawRadarChart(g, data, colors, plotX, plotY, plotW, plotH, cType.Contains("Filled"), cType.Contains("Markers"), zoomLevel);
            }
            else if (cType.StartsWith("Treemap"))
            {
                DrawTreemapChart(g, data, colors, plotX, plotY, plotW, plotH, zoomLevel);
            }
            else if (cType.StartsWith("Sunburst"))
            {
                DrawSunburstChart(g, data, colors, plotX, plotY, plotW, plotH, zoomLevel);
            }
            else if (cType.StartsWith("Waterfall"))
            {
                DrawWaterfallChart(g, data, colors, plotX, plotY, plotW, plotH, zoomLevel);
            }
            else if (cType.StartsWith("Funnel"))
            {
                DrawFunnelChart(g, data, colors, plotX, plotY, plotW, plotH, zoomLevel);
            }
            else if (cType.StartsWith("Stock"))
            {
                DrawStockChart(g, data, colors, plotX, plotY, plotW, plotH, cType.Contains("OHLC"), zoomLevel);
            }
            else if (cType.StartsWith("Histogram"))
            {
                DrawHistogramChart(g, data, colors, plotX, plotY, plotW, plotH, cType.Contains("Pareto"), zoomLevel);
            }
            else if (cType.StartsWith("Box"))
            {
                DrawBoxWhiskerChart(g, data, colors, plotX, plotY, plotW, plotH, zoomLevel);
            }
            else if (cType.StartsWith("Scatter") || cType.StartsWith("Bubble"))
            {
                DrawScatterChart(g, data, colors, plotX, plotY, plotW, plotH, cType.Contains("Smooth"), cType.Contains("Bubble"), zoomLevel);
            }
            else if (cType.StartsWith("Map") || cType.StartsWith("FilledMap"))
            {
                DrawMapChart(g, data, colors, plotX, plotY, plotW, plotH, zoomLevel);
            }
            else if (cType.StartsWith("PivotChart"))
            {
                DrawPivotChart(g, data, colors, plotX, plotY, plotW, plotH, zoomLevel);
            }
            else if (cType.StartsWith("3DMap") || cType.StartsWith("Map3D"))
            {
                Draw3DMapChart(g, data, colors, plotX, plotY, plotW, plotH, zoomLevel);
            }
            else if (cType.StartsWith("Combo"))
            {
                DrawComboChart(g, data, colors, plotX, plotY, plotW, plotH, zoomLevel);
            }
            else // Default: Column (2D or 3D)
            {
                DrawColumnChart(g, data, colors, plotX, plotY, plotW, plotH, cType.Contains("3D") || cType.Contains("3-D"), cType.Contains("Stacked") || cType.Contains("100"), cType.Contains("Deep"), zoomLevel);
            }

            // Legend at Bottom
            if (!cType.StartsWith("Treemap") && !cType.StartsWith("Sunburst") && !cType.StartsWith("Funnel") && !cType.StartsWith("Map") && !cType.StartsWith("FilledMap") && !cType.StartsWith("3DMap") && !cType.StartsWith("Map3D"))
            {
                DrawLegend(g, bounds, data, colors, legendH, zoomLevel);
            }

            // Selection Handles
            if (isSelected)
            {
                using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
                {
                    selPen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(selPen, bounds);
                }
                int hs = (int)(7 * zoomLevel);
                Point[] handles = new Point[]
                {
                    new Point(bounds.Left, bounds.Top),
                    new Point(bounds.Left + bounds.Width / 2, bounds.Top),
                    new Point(bounds.Right, bounds.Top),
                    new Point(bounds.Right, bounds.Top + bounds.Height / 2),
                    new Point(bounds.Right, bounds.Bottom),
                    new Point(bounds.Left + bounds.Width / 2, bounds.Bottom),
                    new Point(bounds.Left, bounds.Bottom),
                    new Point(bounds.Left, bounds.Top + bounds.Height / 2)
                };
                for (int hi = 0; hi < handles.Length; hi++)
                {
                    Rectangle hR = new Rectangle(handles[hi].X - hs / 2, handles[hi].Y - hs / 2, hs, hs);
                    g.FillRectangle(Brushes.White, hR);
                    using (Pen hPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                    {
                        g.DrawRectangle(hPen, hR);
                    }
                }
            }
        }

        // ==================== 1. COLUMN CHART (2D & 3D CLUSTERED, STACKED, 100%, DEEP) ====================
        private static void DrawColumnChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool is3D, bool isStacked, bool isDeep, float zoom)
        {
            double maxVal = GetMaxDataValue(data, isStacked);
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            int serCount = Math.Max(1, data.SeriesNames != null ? data.SeriesNames.Count : 1);

            DrawYGridlines(g, plotX, plotY, plotW, plotH, maxVal, zoom);

            int groupW = plotW / catCount;
            int barGap = Math.Max(2, (int)(4 * zoom));
            int colW = isStacked || isDeep ? Math.Max(6, groupW - (int)(16 * zoom)) : Math.Max(4, (groupW - (int)(16 * zoom) - barGap * (serCount - 1)) / serCount);
            if (colW > 45) colW = 45;

            int depth = is3D ? Math.Max(4, (int)(9 * zoom)) : 0;

            if (isDeep && is3D) // Multi-row Z-axis series perspective
            {
                for (int s = serCount - 1; s >= 0; s--)
                {
                    Color col = colors[s % colors.Length];
                    int zOffX = s * (int)(6 * zoom);
                    int zOffY = -s * (int)(6 * zoom);

                    for (int c = 0; c < catCount; c++)
                    {
                        double val = GetValue(data, c, s);
                        int barH = (int)((val / maxVal) * plotH);
                        if (barH < 2 && val > 0) barH = 2;

                        int bx = plotX + c * groupW + (groupW - colW) / 2 + zOffX;
                        int by = plotY + plotH - barH + zOffY;

                        Draw3DColumnBar(g, bx, by, colW, barH, depth, col);
                    }
                }
            }
            else
            {
                for (int c = 0; c < catCount; c++)
                {
                    int groupStartX = plotX + c * groupW + (groupW - (isStacked ? colW : (colW * serCount + barGap * (serCount - 1)))) / 2;
                    double stackedAcc = 0;

                    for (int s = 0; s < serCount; s++)
                    {
                        double val = GetValue(data, c, s);
                        Color col = colors[s % colors.Length];

                        int barH = (int)((val / maxVal) * plotH);
                        if (barH < 2 && val > 0) barH = 2;

                        int bx = isStacked ? groupStartX : groupStartX + s * (colW + barGap);
                        int by = isStacked ? plotY + plotH - (int)(((stackedAcc + val) / maxVal) * plotH) : plotY + plotH - barH;

                        if (is3D)
                        {
                            Draw3DColumnBar(g, bx, by, colW, barH, depth, col);
                        }
                        else
                        {
                            using (SolidBrush b = new SolidBrush(col))
                            {
                                g.FillRectangle(b, bx, by, colW, barH);
                            }
                            using (Pen p = new Pen(Color.White, 0.8f))
                            {
                                g.DrawRectangle(p, bx, by, colW, barH);
                            }
                        }

                        if (isStacked) stackedAcc += val;
                    }

                    // X Axis Category Label
                    if (data.Categories != null && c < data.Categories.Count)
                    {
                        using (Font xFont = new Font("Segoe UI", Math.Max(6.5f, 8f * zoom)))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter })
                        {
                            Rectangle catRect = new Rectangle(plotX + c * groupW, plotY + plotH + (int)(4 * zoom), groupW, (int)(22 * zoom));
                            g.DrawString(data.Categories[c], xFont, new SolidBrush(Color.FromArgb(71, 85, 105)), catRect, sf);
                        }
                    }
                }
            }
        }

        // ==================== 2. BAR CHART (2D & 3D HORIZONTAL) ====================
        private static void DrawBarChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool is3D, bool isStacked, float zoom)
        {
            double maxVal = GetMaxDataValue(data, isStacked);
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            int serCount = Math.Max(1, data.SeriesNames != null ? data.SeriesNames.Count : 1);

            int groupH = plotH / catCount;
            int barGap = Math.Max(2, (int)(3 * zoom));
            int bH = isStacked ? Math.Max(6, groupH - (int)(10 * zoom)) : Math.Max(3, (groupH - (int)(10 * zoom) - barGap * (serCount - 1)) / serCount);
            if (bH > 35) bH = 35;

            // X Axis Gridlines
            for (int step = 0; step <= 4; step++)
            {
                int gx = plotX + (plotW * step) / 4;
                using (Pen gridP = new Pen(Color.FromArgb(241, 245, 249), 1.0f))
                {
                    g.DrawLine(gridP, gx, plotY, gx, plotY + plotH);
                }
            }

            for (int c = 0; c < catCount; c++)
            {
                int groupStartY = plotY + c * groupH + (groupH - (isStacked ? bH : (bH * serCount + barGap * (serCount - 1)))) / 2;
                double stackedAcc = 0;

                for (int s = 0; s < serCount; s++)
                {
                    double val = GetValue(data, c, s);
                    Color col = colors[s % colors.Length];

                    int bW = (int)((val / maxVal) * plotW);
                    if (bW < 2 && val > 0) bW = 2;

                    int bx = isStacked ? plotX + (int)((stackedAcc / maxVal) * plotW) : plotX;
                    int by = isStacked ? groupStartY : groupStartY + s * (bH + barGap);

                    if (is3D)
                    {
                        Draw3DHorzBar(g, bx, by, bW, bH, Math.Max(3, (int)(6 * zoom)), col);
                    }
                    else
                    {
                        using (SolidBrush b = new SolidBrush(col))
                        {
                            g.FillRectangle(b, bx, by, bW, bH);
                        }
                        using (Pen p = new Pen(Color.White, 0.8f))
                        {
                            g.DrawRectangle(p, bx, by, bW, bH);
                        }
                    }

                    if (isStacked) stackedAcc += val;
                }

                // Y-Axis Category Label
                if (data.Categories != null && c < data.Categories.Count)
                {
                    using (Font yFont = new Font("Segoe UI", Math.Max(6.5f, 8f * zoom)))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                    {
                        Rectangle yRect = new Rectangle(plotX - (int)(44 * zoom), plotY + c * groupH, (int)(40 * zoom), groupH);
                        g.DrawString(data.Categories[c], yFont, new SolidBrush(Color.FromArgb(71, 85, 105)), yRect, sf);
                    }
                }
            }
        }

        // ==================== 3. LINE CHART (2D & 3D RIBBON) ====================
        private static void DrawLineChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool is3D, bool isStacked, float zoom)
        {
            double maxVal = GetMaxDataValue(data, isStacked);
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            int serCount = Math.Max(1, data.SeriesNames != null ? data.SeriesNames.Count : 1);

            DrawYGridlines(g, plotX, plotY, plotW, plotH, maxVal, zoom);

            for (int s = 0; s < serCount; s++)
            {
                Color col = colors[s % colors.Length];
                PointF[] pts = new PointF[catCount];

                for (int c = 0; c < catCount; c++)
                {
                    double val = GetValue(data, c, s);
                    float px = plotX + (catCount == 1 ? plotW / 2f : (plotW * c) / (float)(catCount - 1));
                    float py = plotY + plotH - (float)((val / maxVal) * plotH);
                    pts[c] = new PointF(px, py);
                }

                if (is3D && pts.Length > 1) // 3D Ribbon Line
                {
                    int ribbonDepth = Math.Max(4, (int)(7 * zoom));
                    for (int i = 0; i < pts.Length - 1; i++)
                    {
                        PointF p1 = pts[i];
                        PointF p2 = pts[i + 1];
                        PointF[] ribbonQuad = new PointF[]
                        {
                            p1,
                            new PointF(p1.X + ribbonDepth, p1.Y - ribbonDepth),
                            new PointF(p2.X + ribbonDepth, p2.Y - ribbonDepth),
                            p2
                        };
                        using (SolidBrush rb = new SolidBrush(ControlPaint.Light(col, 0.25f))) g.FillPolygon(rb, ribbonQuad);
                        using (Pen rp = new Pen(ControlPaint.Dark(col, 0.15f), 1f)) g.DrawPolygon(rp, ribbonQuad);
                    }
                }

                if (pts.Length > 1)
                {
                    using (Pen lPen = new Pen(col, Math.Max(2f, 2.5f * zoom)))
                    {
                        g.DrawLines(lPen, pts);
                    }
                }

                // Markers
                int mrkSz = Math.Max(4, (int)(6 * zoom));
                for (int c = 0; c < catCount; c++)
                {
                    RectangleF mrkR = new RectangleF(pts[c].X - mrkSz / 2f, pts[c].Y - mrkSz / 2f, mrkSz, mrkSz);
                    g.FillEllipse(Brushes.White, mrkR);
                    using (Pen p = new Pen(col, 2f)) g.DrawEllipse(p, mrkR);
                }
            }

            // Category Labels
            if (data.Categories != null)
            {
                for (int c = 0; c < catCount && c < data.Categories.Count; c++)
                {
                    float px = plotX + (catCount == 1 ? plotW / 2f : (plotW * c) / (float)(catCount - 1));
                    using (Font xFont = new Font("Segoe UI", Math.Max(6.5f, 8f * zoom)))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
                    {
                        g.DrawString(data.Categories[c], xFont, new SolidBrush(Color.FromArgb(71, 85, 105)), px, plotY + plotH + (int)(4 * zoom), sf);
                    }
                }
            }
        }

        // ==================== 4. AREA CHART (2D & 3D TERRAIN) ====================
        private static void DrawAreaChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool is3D, bool isStacked, float zoom)
        {
            double maxVal = GetMaxDataValue(data, isStacked);
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            int serCount = Math.Max(1, data.SeriesNames != null ? data.SeriesNames.Count : 1);

            DrawYGridlines(g, plotX, plotY, plotW, plotH, maxVal, zoom);

            for (int s = serCount - 1; s >= 0; s--)
            {
                Color col = colors[s % colors.Length];
                List<PointF> poly = new List<PointF>();
                poly.Add(new PointF(plotX, plotY + plotH));

                for (int c = 0; c < catCount; c++)
                {
                    double val = GetValue(data, c, s);
                    float px = plotX + (catCount == 1 ? plotW / 2f : (plotW * c) / (float)(catCount - 1));
                    float py = plotY + plotH - (float)((val / maxVal) * plotH);
                    poly.Add(new PointF(px, py));
                }
                poly.Add(new PointF(plotX + plotW, plotY + plotH));

                if (is3D) // 3D Top Terrain Shadow
                {
                    int d = Math.Max(4, (int)(8 * zoom));
                    List<PointF> top3D = new List<PointF>();
                    for (int i = 1; i <= catCount; i++)
                    {
                        top3D.Add(new PointF(poly[i].X + d, poly[i].Y - d));
                    }
                    for (int i = catCount; i >= 1; i--)
                    {
                        top3D.Add(poly[i]);
                    }
                    using (SolidBrush tb = new SolidBrush(ControlPaint.Light(col, 0.3f)))
                    {
                        g.FillPolygon(tb, top3D.ToArray());
                    }
                }

                using (GraphicsPath ap = new GraphicsPath())
                {
                    ap.AddPolygon(poly.ToArray());
                    using (SolidBrush ab = new SolidBrush(Color.FromArgb(160, col.R, col.G, col.B)))
                    {
                        g.FillPath(ab, ap);
                    }
                    using (Pen p = new Pen(col, 2f))
                    {
                        g.DrawLines(p, poly.GetRange(1, catCount).ToArray());
                    }
                }
            }
        }

        // ==================== 5. PIE & DOUGHNUT CHART (2D & 3D) ====================
        private static void DrawPieChart(Graphics g, Rectangle bounds, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool is3D, bool isDoughnut, float zoom)
        {
            int pieSize = Math.Min(plotW, plotH) - (int)(10 * zoom);
            if (pieSize < 20) pieSize = 20;
            int px = plotX + (plotW - pieSize) / 2;
            int py = plotY + (plotH - pieSize) / 2;
            Rectangle pieRect = new Rectangle(px, py, pieSize, is3D ? (int)(pieSize * 0.65f) : pieSize);

            int catCount = data.Categories != null ? data.Categories.Count : 0;
            double total = 0;
            for (int i = 0; i < catCount; i++) total += Math.Max(0, GetValue(data, i, 0));
            if (total <= 0) total = 1.0;

            float startAngle = -90f;

            if (is3D)
            {
                int depth = (int)(14 * zoom);
                // Draw bottom rim of 3D pie slices
                for (int d = depth; d >= 1; d--)
                {
                    float angle = -90f;
                    for (int i = 0; i < catCount; i++)
                    {
                        double val = Math.Max(0, GetValue(data, i, 0));
                        float sweep = (float)((val / total) * 360.0);
                        if (sweep > 0)
                        {
                            Color darkCol = ControlPaint.Dark(colors[i % colors.Length], 0.25f);
                            using (SolidBrush db = new SolidBrush(darkCol))
                            {
                                g.FillPie(db, pieRect.X, pieRect.Y + d, pieRect.Width, pieRect.Height, angle, sweep);
                            }
                        }
                        angle += sweep;
                    }
                }
            }

            // Top surface
            startAngle = -90f;
            for (int i = 0; i < catCount; i++)
            {
                double val = Math.Max(0, GetValue(data, i, 0));
                float sweep = (float)((val / total) * 360.0);
                if (sweep > 0)
                {
                    Color col = colors[i % colors.Length];
                    using (SolidBrush pb = new SolidBrush(col))
                    {
                        g.FillPie(pb, pieRect, startAngle, sweep);
                    }
                    using (Pen p = new Pen(Color.White, 1.5f))
                    {
                        g.DrawPie(p, pieRect, startAngle, sweep);
                    }
                }
                startAngle += sweep;
            }

            // Hole for Doughnut
            if (isDoughnut)
            {
                int holeSize = (int)(pieSize * 0.5f);
                Rectangle holeRect = new Rectangle(px + (pieSize - holeSize) / 2, py + (pieSize - holeSize) / 2, holeSize, is3D ? (int)(holeSize * 0.65f) : holeSize);
                g.FillEllipse(Brushes.White, holeRect);
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f)) g.DrawEllipse(p, holeRect);
            }
        }

        // ==================== 6. 3D SURFACE & WIREFRAME ====================
        private static void DrawSurfaceChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool wireframeOnly, float zoom)
        {
            int rows = Math.Max(3, data.Categories != null ? data.Categories.Count : 4);
            int cols = Math.Max(3, data.SeriesNames != null ? data.SeriesNames.Count : 4);

            PointF[,] mesh = new PointF[rows, cols];
            float cellW = plotW / (float)(cols + 1);
            float cellH = plotH / (float)(rows + 1);

            double maxV = 100.0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double v = GetValue(data, r, c);
                    if (v > maxV) maxV = v;
                }
            }

            // Generate 3D topological undulating grid
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double val = GetValue(data, r, c);
                    float elevation = (float)((val / maxV) * (plotH * 0.4f));

                    float isoX = plotX + (c + 1) * cellW + (r * (cellW * 0.4f));
                    float isoY = plotY + (plotH * 0.7f) - elevation + (r * (cellH * 0.5f));

                    mesh[r, c] = new PointF(isoX, isoY);
                }
            }

            // Elevation gradient colors (Blue -> Cyan -> Green -> Yellow -> Red)
            Color[] surfCols = new Color[]
            {
                Color.FromArgb(68, 114, 196),
                Color.FromArgb(91, 155, 213),
                Color.FromArgb(112, 173, 71),
                Color.FromArgb(255, 192, 0),
                Color.FromArgb(237, 125, 49)
            };

            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    PointF[] quad = new PointF[]
                    {
                        mesh[r, c],
                        mesh[r, c + 1],
                        mesh[r + 1, c + 1],
                        mesh[r + 1, c]
                    };

                    double avgVal = (GetValue(data, r, c) + GetValue(data, r, c + 1) + GetValue(data, r + 1, c + 1) + GetValue(data, r + 1, c)) / 4.0;
                    int colIdx = Math.Min(surfCols.Length - 1, (int)((avgVal / maxV) * surfCols.Length));
                    Color sCol = surfCols[Math.Max(0, colIdx)];

                    if (!wireframeOnly)
                    {
                        using (SolidBrush sb = new SolidBrush(Color.FromArgb(180, sCol.R, sCol.G, sCol.B)))
                        {
                            g.FillPolygon(sb, quad);
                        }
                    }
                    using (Pen sp = new Pen(wireframeOnly ? sCol : Color.FromArgb(60, 70, 80), 1f))
                    {
                        g.DrawPolygon(sp, quad);
                    }
                }
            }
        }

        // ==================== 7. RADAR CHART ====================
        private static void DrawRadarChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool filled, bool showMarkers, float zoom)
        {
            int catCount = Math.Max(3, data.Categories != null ? data.Categories.Count : 5);
            int serCount = Math.Max(1, data.SeriesNames != null ? data.SeriesNames.Count : 1);
            PointF center = new PointF(plotX + plotW / 2f, plotY + plotH / 2f);
            float maxRadius = Math.Min(plotW, plotH) / 2.3f;

            double maxVal = GetMaxDataValue(data, false);

            // Draw concentric web rings
            using (Pen ringPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                for (int ring = 1; ring <= 4; ring++)
                {
                    float rDist = (maxRadius * ring) / 4f;
                    PointF[] ringPts = new PointF[catCount];
                    for (int a = 0; a < catCount; a++)
                    {
                        double ang = -Math.PI / 2.0 + (a * 2.0 * Math.PI / catCount);
                        ringPts[a] = new PointF(center.X + (float)(Math.Cos(ang) * rDist), center.Y + (float)(Math.Sin(ang) * rDist));
                    }
                    g.DrawPolygon(ringPen, ringPts);
                }
            }

            // Draw spokes & category labels
            using (Pen spokePen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            using (Font lblFont = new Font("Segoe UI", Math.Max(6.5f, 7.5f * zoom)))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(71, 85, 105)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int a = 0; a < catCount; a++)
                {
                    double ang = -Math.PI / 2.0 + (a * 2.0 * Math.PI / catCount);
                    PointF edgePt = new PointF(center.X + (float)(Math.Cos(ang) * maxRadius), center.Y + (float)(Math.Sin(ang) * maxRadius));
                    g.DrawLine(spokePen, center, edgePt);

                    if (data.Categories != null && a < data.Categories.Count)
                    {
                        PointF lblPt = new PointF(center.X + (float)(Math.Cos(ang) * (maxRadius + 14 * zoom)), center.Y + (float)(Math.Sin(ang) * (maxRadius + 14 * zoom)));
                        g.DrawString(data.Categories[a], lblFont, txtB, lblPt.X, lblPt.Y, sf);
                    }
                }
            }

            // Draw series data polygons
            for (int s = 0; s < serCount; s++)
            {
                Color col = colors[s % colors.Length];
                PointF[] dPts = new PointF[catCount];

                for (int c = 0; c < catCount; c++)
                {
                    double val = GetValue(data, c, s);
                    float rDist = (float)((val / maxVal) * maxRadius);
                    double ang = -Math.PI / 2.0 + (c * 2.0 * Math.PI / catCount);
                    dPts[c] = new PointF(center.X + (float)(Math.Cos(ang) * rDist), center.Y + (float)(Math.Sin(ang) * rDist));
                }

                if (filled)
                {
                    using (SolidBrush fb = new SolidBrush(Color.FromArgb(100, col.R, col.G, col.B)))
                    {
                        g.FillPolygon(fb, dPts);
                    }
                }
                using (Pen dp = new Pen(col, Math.Max(2f, 2.5f * zoom)))
                {
                    g.DrawPolygon(dp, dPts);
                }

                if (showMarkers)
                {
                    for (int c = 0; c < catCount; c++)
                    {
                        g.FillEllipse(Brushes.White, dPts[c].X - 3, dPts[c].Y - 3, 6, 6);
                        using (Pen mp = new Pen(col, 2f)) g.DrawEllipse(mp, dPts[c].X - 3, dPts[c].Y - 3, 6, 6);
                    }
                }
            }
        }

        // ==================== 8. TREEMAP CHART (HIERARCHY) ====================
        private static void DrawTreemapChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, float zoom)
        {
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            double total = 0;
            for (int i = 0; i < catCount; i++) total += Math.Max(1.0, GetValue(data, i, 0));

            // Squarified Treemap Partitioning
            Rectangle curRect = new Rectangle(plotX, plotY, plotW, plotH);
            int curX = plotX;
            int curY = plotY;
            int remW = plotW;
            int remH = plotH;

            using (Font tf = new Font("Segoe UI", Math.Max(7f, 9f * zoom), FontStyle.Bold))
            using (Font vf = new Font("Segoe UI", Math.Max(6.5f, 8f * zoom)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near })
            {
                for (int i = 0; i < catCount; i++)
                {
                    double val = Math.Max(1.0, GetValue(data, i, 0));
                    double fraction = val / total;
                    Color col = colors[i % colors.Length];

                    Rectangle tile;
                    if (i == catCount - 1)
                    {
                        tile = new Rectangle(curX, curY, remW, remH);
                    }
                    else if (remW >= remH)
                    {
                        int tW = Math.Max(20, (int)(remW * fraction * 1.8));
                        if (tW > remW - 20) tW = remW / 2;
                        tile = new Rectangle(curX, curY, tW, remH);
                        curX += tW;
                        remW -= tW;
                    }
                    else
                    {
                        int tH = Math.Max(20, (int)(remH * fraction * 1.8));
                        if (tH > remH - 20) tH = remH / 2;
                        tile = new Rectangle(curX, curY, remW, tH);
                        curY += tH;
                        remH -= tH;
                    }

                    using (SolidBrush tb = new SolidBrush(col)) g.FillRectangle(tb, tile);
                    using (Pen tp = new Pen(Color.White, 2f)) g.DrawRectangle(tp, tile);

                    // Text labels
                    string catName = (data.Categories != null && i < data.Categories.Count) ? data.Categories[i] : ("Mục " + (i + 1));
                    g.DrawString(catName, tf, Brushes.White, tile.X + 6, tile.Y + 6);
                    g.DrawString(val.ToString("0.#"), vf, Brushes.White, tile.X + 6, tile.Y + 22);
                }
            }
        }

        // ==================== 9. SUNBURST CHART ====================
        private static void DrawSunburstChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, float zoom)
        {
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            int size = Math.Min(plotW, plotH) - (int)(10 * zoom);
            Rectangle outerR = new Rectangle(plotX + (plotW - size) / 2, plotY + (plotH - size) / 2, size, size);

            double total = 0;
            for (int i = 0; i < catCount; i++) total += Math.Max(1.0, GetValue(data, i, 0));

            float startAngle = -90f;
            for (int i = 0; i < catCount; i++)
            {
                double val = Math.Max(1.0, GetValue(data, i, 0));
                float sweep = (float)((val / total) * 360.0);
                Color col = colors[i % colors.Length];

                using (SolidBrush b = new SolidBrush(col)) g.FillPie(b, outerR, startAngle, sweep);
                using (Pen p = new Pen(Color.White, 2f)) g.DrawPie(p, outerR, startAngle, sweep);
                startAngle += sweep;
            }

            // Inner Ring 1
            int inner1 = (int)(size * 0.65f);
            Rectangle r1 = new Rectangle(plotX + (plotW - inner1) / 2, plotY + (plotH - inner1) / 2, inner1, inner1);
            using (SolidBrush bWhite = new SolidBrush(Color.White)) g.FillEllipse(bWhite, r1);

            startAngle = -90f;
            for (int i = 0; i < catCount; i++)
            {
                double val = Math.Max(1.0, GetValue(data, i, 0));
                float sweep = (float)((val / total) * 360.0);
                Color col = ControlPaint.Light(colors[i % colors.Length], 0.35f);

                using (SolidBrush b = new SolidBrush(col)) g.FillPie(b, r1, startAngle, sweep);
                using (Pen p = new Pen(Color.White, 1.5f)) g.DrawPie(p, r1, startAngle, sweep);
                startAngle += sweep;
            }

            // Center Hole
            int inner2 = (int)(size * 0.32f);
            Rectangle r2 = new Rectangle(plotX + (plotW - inner2) / 2, plotY + (plotH - inner2) / 2, inner2, inner2);
            g.FillEllipse(Brushes.White, r2);
            using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f)) g.DrawEllipse(p, r2);
        }

        // ==================== 10. WATERFALL CHART ====================
        private static void DrawWaterfallChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, float zoom)
        {
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            DrawYGridlines(g, plotX, plotY, plotW, plotH, GetMaxDataValue(data, true), zoom);

            int colW = Math.Max(10, plotW / (catCount + 1));
            double runningTotal = 0;
            double maxV = GetMaxDataValue(data, true);

            Color cPos = Color.FromArgb(16, 124, 65);  // Green (Increase)
            Color cNeg = Color.FromArgb(216, 59, 1);   // Red (Decrease)
            Color cTot = Color.FromArgb(68, 114, 196);  // Blue (Total)

            int prevBottom = plotY + plotH;

            for (int i = 0; i < catCount; i++)
            {
                double val = GetValue(data, i, 0);
                bool isTotal = (i == catCount - 1);

                int bx = plotX + i * colW + 10;
                int by, bh;

                if (isTotal)
                {
                    bh = (int)((runningTotal / maxV) * plotH);
                    by = plotY + plotH - bh;
                    using (SolidBrush b = new SolidBrush(cTot)) g.FillRectangle(b, bx, by, colW - 6, bh);
                }
                else
                {
                    bh = (int)((Math.Abs(val) / maxV) * plotH);
                    if (bh < 2) bh = 2;
                    by = plotY + plotH - (int)(((runningTotal + val) / maxV) * plotH);

                    using (SolidBrush b = new SolidBrush(val >= 0 ? cPos : cNeg))
                    {
                        g.FillRectangle(b, bx, by, colW - 6, bh);
                    }

                    // Connector dashed line to next column
                    if (i < catCount - 1)
                    {
                        using (Pen dashP = new Pen(Color.Gray, 1f) { DashStyle = DashStyle.Dot })
                        {
                            g.DrawLine(dashP, bx + colW - 6, by, bx + colW + 10, by);
                        }
                    }
                    runningTotal += val;
                }

                // X-Axis Category
                if (data.Categories != null && i < data.Categories.Count)
                {
                    using (Font xFont = new Font("Segoe UI", Math.Max(6.5f, 7.5f * zoom)))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
                    {
                        Rectangle cR = new Rectangle(bx - 4, plotY + plotH + 4, colW + 2, 20);
                        g.DrawString(data.Categories[i], xFont, new SolidBrush(Color.FromArgb(71, 85, 105)), cR, sf);
                    }
                }
            }
        }

        // ==================== 11. FUNNEL CHART ====================
        private static void DrawFunnelChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, float zoom)
        {
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            int stageH = plotH / catCount;
            double maxV = Math.Max(1.0, GetValue(data, 0, 0));

            using (Font tf = new Font("Segoe UI", Math.Max(7f, 8.5f * zoom), FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int i = 0; i < catCount; i++)
                {
                    double vTop = GetValue(data, i, 0);
                    double vBot = (i < catCount - 1) ? GetValue(data, i + 1, 0) : vTop * 0.7;

                    float topW = (float)((vTop / maxV) * (plotW * 0.8f));
                    float botW = (float)((vBot / maxV) * (plotW * 0.8f));

                    float y1 = plotY + i * stageH;
                    float y2 = y1 + stageH - 3;

                    PointF[] trapezoid = new PointF[]
                    {
                        new PointF(plotX + (plotW - topW) / 2f, y1),
                        new PointF(plotX + (plotW + topW) / 2f, y1),
                        new PointF(plotX + (plotW + botW) / 2f, y2),
                        new PointF(plotX + (plotW - botW) / 2f, y2)
                    };

                    Color col = colors[i % colors.Length];
                    using (SolidBrush fb = new SolidBrush(col)) g.FillPolygon(fb, trapezoid);
                    using (Pen fp = new Pen(Color.White, 1.5f)) g.DrawPolygon(fp, trapezoid);

                    // Stage Label & Value
                    string lbl = (data.Categories != null && i < data.Categories.Count) ? data.Categories[i] + " (" + vTop.ToString("0.#") + ")" : vTop.ToString("0.#");
                    RectangleF rText = new RectangleF(plotX, y1, plotW, stageH - 3);
                    g.DrawString(lbl, tf, Brushes.White, rText, sf);
                }
            }
        }

        // ==================== 12. STOCK CHART (CANDLESTICK) ====================
        private static void DrawStockChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool showOpen, float zoom)
        {
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            double maxV = GetMaxDataValue(data, false);
            DrawYGridlines(g, plotX, plotY, plotW, plotH, maxV, zoom);

            int colW = Math.Max(8, plotW / (catCount + 1));

            for (int i = 0; i < catCount; i++)
            {
                double high = GetValue(data, i, 0);
                double low = (data.SeriesNames != null && data.SeriesNames.Count > 1) ? GetValue(data, i, 1) : high * 0.6;
                double close = (data.SeriesNames != null && data.SeriesNames.Count > 2) ? GetValue(data, i, 2) : (high + low) / 2.0;
                double open = (data.SeriesNames != null && data.SeriesNames.Count > 3) ? GetValue(data, i, 3) : low * 1.1;

                int kx = plotX + i * colW + colW / 2 + 10;
                int yHigh = plotY + plotH - (int)((high / maxV) * plotH);
                int yLow = plotY + plotH - (int)((low / maxV) * plotH);
                int yClose = plotY + plotH - (int)((close / maxV) * plotH);
                int yOpen = plotY + plotH - (int)((open / maxV) * plotH);

                // High-Low Wick
                using (Pen wickPen = new Pen(Color.FromArgb(60, 70, 80), 1.5f))
                {
                    g.DrawLine(wickPen, kx, yHigh, kx, yLow);
                }

                // Candle Body (Green for Up, Red for Down)
                bool isUp = close >= open;
                Color bodyCol = isUp ? Color.FromArgb(16, 124, 65) : Color.FromArgb(216, 59, 1);
                int bodyTop = Math.Min(yOpen, yClose);
                int bodyH = Math.Max(3, Math.Abs(yOpen - yClose));

                using (SolidBrush bb = new SolidBrush(bodyCol))
                {
                    g.FillRectangle(bb, kx - 5, bodyTop, 10, bodyH);
                }
                using (Pen bp = new Pen(Color.FromArgb(40, 50, 60), 1f))
                {
                    g.DrawRectangle(bp, kx - 5, bodyTop, 10, bodyH);
                }
            }
        }

        // ==================== 13. HISTOGRAM & BOX-WHISKER ====================
        private static void DrawHistogramChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool pareto, float zoom)
        {
            DrawColumnChart(g, data, colors, plotX, plotY, plotW, plotH, false, false, false, zoom);

            if (pareto) // Overlaid Pareto cumulative line
            {
                int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
                PointF[] linePts = new PointF[catCount];
                double acc = 0;
                double total = 0;
                for (int i = 0; i < catCount; i++) total += GetValue(data, i, 0);
                if (total <= 0) total = 1.0;

                for (int i = 0; i < catCount; i++)
                {
                    acc += GetValue(data, i, 0);
                    float px = plotX + (plotW * i) / (float)catCount + (plotW / (float)catCount) / 2f;
                    float py = plotY + plotH - (float)((acc / total) * plotH);
                    linePts[i] = new PointF(px, py);
                }

                using (Pen pPen = new Pen(Color.FromArgb(237, 125, 49), 2.5f))
                {
                    g.DrawLines(pPen, linePts);
                }
                for (int i = 0; i < catCount; i++)
                {
                    g.FillEllipse(Brushes.White, linePts[i].X - 3, linePts[i].Y - 3, 6, 6);
                    using (Pen p = new Pen(Color.FromArgb(237, 125, 49), 2f)) g.DrawEllipse(p, linePts[i].X - 3, linePts[i].Y - 3, 6, 6);
                }
            }
        }

        private static void DrawBoxWhiskerChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, float zoom)
        {
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            double maxV = GetMaxDataValue(data, false);
            DrawYGridlines(g, plotX, plotY, plotW, plotH, maxV, zoom);

            int boxW = Math.Max(12, plotW / (catCount + 1));

            for (int i = 0; i < catCount; i++)
            {
                double v = GetValue(data, i, 0);
                int kx = plotX + i * boxW + boxW / 2 + 10;

                int yMax = plotY + plotH - (int)((v / maxV) * plotH);
                int yQ3 = yMax + (int)(12 * zoom);
                int yMed = yMax + (int)(22 * zoom);
                int yQ1 = yMax + (int)(32 * zoom);
                int yMin = yMax + (int)(44 * zoom);

                // Whisker lines
                using (Pen p = new Pen(Color.FromArgb(60, 70, 80), 1.5f))
                {
                    g.DrawLine(p, kx, yMax, kx, yQ3);
                    g.DrawLine(p, kx - 4, yMax, kx + 4, yMax);

                    g.DrawLine(p, kx, yQ1, kx, yMin);
                    g.DrawLine(p, kx - 4, yMin, kx + 4, yMin);
                }

                // Box
                Color col = colors[i % colors.Length];
                using (SolidBrush b = new SolidBrush(Color.FromArgb(160, col.R, col.G, col.B)))
                {
                    g.FillRectangle(b, kx - 10, yQ3, 20, yQ1 - yQ3);
                }
                using (Pen bp = new Pen(col, 1.5f))
                {
                    g.DrawRectangle(bp, kx - 10, yQ3, 20, yQ1 - yQ3);
                    g.DrawLine(bp, kx - 10, yMed, kx + 10, yMed); // Median
                }
            }
        }

        // ==================== 14. SCATTER & BUBBLE CHART ====================
        private static void DrawScatterChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, bool isSmooth, bool isBubble, float zoom)
        {
            double maxVal = GetMaxDataValue(data, false);
            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            int serCount = Math.Max(1, data.SeriesNames != null ? data.SeriesNames.Count : 1);

            DrawYGridlines(g, plotX, plotY, plotW, plotH, maxVal, zoom);

            for (int s = 0; s < serCount; s++)
            {
                Color col = colors[s % colors.Length];
                PointF[] pts = new PointF[catCount];

                for (int c = 0; c < catCount; c++)
                {
                    double val = GetValue(data, c, s);
                    float px = plotX + (catCount == 1 ? plotW / 2f : (plotW * c) / (float)(catCount - 1));
                    float py = plotY + plotH - (float)((val / maxVal) * plotH);
                    pts[c] = new PointF(px, py);
                }

                if (isSmooth && pts.Length > 2)
                {
                    using (Pen p = new Pen(col, 2f)) g.DrawCurve(p, pts);
                }

                for (int c = 0; c < catCount; c++)
                {
                    int bSize = isBubble ? Math.Max(8, (int)(18 * zoom)) : Math.Max(4, (int)(6 * zoom));
                    RectangleF r = new RectangleF(pts[c].X - bSize / 2f, pts[c].Y - bSize / 2f, bSize, bSize);

                    if (isBubble)
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(140, col.R, col.G, col.B))) g.FillEllipse(b, r);
                        using (Pen p = new Pen(col, 1.5f)) g.DrawEllipse(p, r);
                    }
                    else
                    {
                        g.FillEllipse(Brushes.White, r);
                        using (Pen p = new Pen(col, 2f)) g.DrawEllipse(p, r);
                    }
                }
            }
        }

        // ==================== 15. COMBO CHART (DUAL AXIS) ====================
        private static void DrawComboChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, float zoom)
        {
            // Series 0 as Column on Left Axis, Series 1 as Line on Right Axis
            DrawColumnChart(g, data, colors, plotX, plotY, plotW, plotH, false, false, false, zoom);

            int catCount = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            int serIdx = Math.Min(1, (data.SeriesNames != null ? data.SeriesNames.Count - 1 : 0));
            double maxVal = GetMaxDataValue(data, false);

            PointF[] linePts = new PointF[catCount];
            for (int c = 0; c < catCount; c++)
            {
                double val = GetValue(data, c, serIdx);
                float px = plotX + (plotW * c) / (float)catCount + (plotW / (float)catCount) / 2f;
                float py = plotY + plotH - (float)((val / maxVal) * plotH);
                linePts[c] = new PointF(px, py);
            }

            Color lineCol = colors[1 % colors.Length];
            if (linePts.Length > 1)
            {
                using (Pen lPen = new Pen(lineCol, Math.Max(2f, 2.5f * zoom)))
                {
                    g.DrawLines(lPen, linePts);
                }
            }
            for (int c = 0; c < catCount; c++)
            {
                g.FillEllipse(Brushes.White, linePts[c].X - 4, linePts[c].Y - 4, 8, 8);
                using (Pen p = new Pen(lineCol, 2f)) g.DrawEllipse(p, linePts[c].X - 4, linePts[c].Y - 4, 8, 8);
            }
        }

        // ==================== 16. FILLED MAP (BẢN ĐỒ ĐỊA LÝ PHÂN VÙNG) ====================
        private static void DrawMapChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, float zoom)
        {
            int n = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            double minV = double.MaxValue;
            double maxV = double.MinValue;
            for (int i = 0; i < n; i++)
            {
                double v = GetValue(data, i, 0);
                if (v < minV) minV = v;
                if (v > maxV) maxV = v;
            }
            if (minV >= maxV) { minV = 0; maxV = Math.Max(10, maxV); }

            // 1. Draw Map Background & Grid
            using (SolidBrush mapBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (Pen mapBorder = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.FillRectangle(mapBg, plotX, plotY, plotW, plotH);
                g.DrawRectangle(mapBorder, plotX, plotY, plotW, plotH);
            }

            // Draw Subtle Longitude / Latitude Arc Gridlines
            using (Pen gridP = new Pen(Color.FromArgb(226, 232, 240), 1f) { DashStyle = DashStyle.Dash })
            {
                for (int gx = plotX + 40; gx < plotX + plotW; gx += 60) g.DrawLine(gridP, gx, plotY, gx, plotY + plotH);
                for (int gy = plotY + 30; gy < plotY + plotH; gy += 45) g.DrawLine(gridP, plotX, gy, plotX + plotW, gy);
            }

            // 2. Render Geographic Regions / Provinces with Choropleth Gradient Tiles
            int cols = Math.Max(2, (int)Math.Ceiling(Math.Sqrt(n)));
            int rows = (int)Math.Ceiling((double)n / cols);
            int cardW = Math.Max(40, (plotW - 20) / cols - 8);
            int cardH = Math.Max(30, (plotH - 45) / rows - 8);

            using (Font regFont = new Font("Segoe UI", Math.Max(7.0f, 8.5f * zoom), FontStyle.Bold))
            using (Font valFont = new Font("Segoe UI", Math.Max(6.5f, 7.8f * zoom), FontStyle.Regular))
            {
                for (int i = 0; i < n; i++)
                {
                    int rIdx = i / cols;
                    int cIdx = i % cols;
                    int cx = plotX + 10 + cIdx * (cardW + 8);
                    int cy = plotY + 10 + rIdx * (cardH + 8);
                    Rectangle cardR = new Rectangle(cx, cy, cardW, cardH);

                    double val = GetValue(data, i, 0);
                    float ratio = (float)((val - minV) / (maxV - minV));
                    ratio = Math.Max(0f, Math.Min(1f, ratio));

                    // Dynamic Choropleth Color Gradient (Light Mint -> Deep Ocean Navy)
                    int cr = (int)(219 - ratio * (219 - 30));
                    int cg = (int)(234 - ratio * (234 - 58));
                    int cb = (int)(254 - ratio * (254 - 138));
                    Color fillC = Color.FromArgb(cr, cg, cb);
                    Color textC = ratio > 0.5f ? Color.White : Color.FromArgb(15, 23, 42);

                    using (GraphicsPath regPath = CreateRoundedPath(cardR, 5))
                    using (SolidBrush regB = new SolidBrush(fillC))
                    using (Pen regP = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
                    using (SolidBrush tB = new SolidBrush(textC))
                    {
                        g.FillPath(regB, regPath);
                        g.DrawPath(regP, regPath);

                        string cName = (i < data.Categories.Count) ? data.Categories[i] : ("Vùng " + (i + 1));
                        g.DrawString("📍 " + cName, regFont, tB, cx + 5, cy + 4);
                        g.DrawString(val.ToString("N1") + (data.SeriesNames != null && data.SeriesNames.Count > 0 ? " " + data.SeriesNames[0] : ""), valFont, tB, cx + 5, cy + cardH - 18);
                    }
                }
            }

            // 3. Legend Gradient Scale Bar at bottom
            int barX = plotX + 12;
            int barY = plotY + plotH - 24;
            int barW = Math.Min(220, plotW - 24);
            int barH = 10;
            Rectangle barRect = new Rectangle(barX, barY, barW, barH);
            using (LinearGradientBrush lgb = new LinearGradientBrush(barRect, Color.FromArgb(219, 234, 254), Color.FromArgb(30, 58, 138), 0f))
            using (Pen bp = new Pen(Color.FromArgb(148, 163, 184), 1f))
            {
                g.FillRectangle(lgb, barRect);
                g.DrawRectangle(bp, barRect);
            }
            using (Font lgFont = new Font("Segoe UI", 7.0f * zoom))
            using (SolidBrush lgB = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                g.DrawString("Thấp: " + minV.ToString("N1"), lgFont, lgB, barX, barY - 12);
                g.DrawString("Cao: " + maxV.ToString("N1"), lgFont, lgB, barX + barW - 40, barY - 12);
            }
        }

        // ==================== 17. PIVOT CHART (BIỂU ĐỒ PIVOT CÓ NÚT LỌC TRƯỜNG) ====================
        private static void DrawPivotChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, float zoom)
        {
            // 1. Draw Pivot Filter Pill Buttons at Top
            int pillY = plotY;
            int pillH = (int)(22 * zoom);
            using (Font pillFont = new Font("Segoe UI", 7.8f * zoom, FontStyle.Bold))
            using (SolidBrush pillBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (Pen pillPen = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
            using (SolidBrush pillTxt = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                string[] pills = new string[] { "∑ Giá trị ▾", "Khu vực ▾", "Dòng dữ liệu ▾" };
                int pillX = plotX;
                for (int pi = 0; pi < pills.Length; pi++)
                {
                    SizeF sz = g.MeasureString(pills[pi], pillFont);
                    Rectangle pRect = new Rectangle(pillX, pillY, (int)sz.Width + 12, pillH);
                    using (GraphicsPath pp = CreateRoundedPath(pRect, 4))
                    {
                        g.FillPath(pillBg, pp);
                        g.DrawPath(pillPen, pp);
                    }
                    g.DrawString(pills[pi], pillFont, pillTxt, pillX + 6, pillY + 3);
                    pillX += (int)sz.Width + 18;
                }
            }

            // 2. Adjust plot area below pivot pills
            int effPlotY = plotY + pillH + 8;
            int effPlotH = plotH - pillH - 8;

            // 3. Render Clustered Column Chart for Pivot Data
            DrawColumnChart(g, data, colors, plotX, effPlotY, plotW, effPlotH, false, false, false, zoom);
        }

        // ==================== 18. 3D MAPS (BẢN ĐỒ KHÔNG GIAN 3 CHIỀU) ====================
        private static void Draw3DMapChart(Graphics g, DocChartData data, Color[] colors, int plotX, int plotY, int plotW, int plotH, float zoom)
        {
            // 1. Dark Spatial Canvas
            using (SolidBrush darkBg = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (Pen darkBorder = new Pen(Color.FromArgb(30, 41, 59), 1f))
            {
                g.FillRectangle(darkBg, plotX, plotY, plotW, plotH);
                g.DrawRectangle(darkBorder, plotX, plotY, plotW, plotH);
            }

            // 2. 3D Globe Latitude/Longitude Perspective Ellipses
            int globeCenterX = plotX + plotW / 2;
            int globeCenterY = plotY + plotH / 2 - 10;
            int globeRadius = Math.Min(plotW, plotH) / 2 - 20;

            using (Pen globePen = new Pen(Color.FromArgb(56, 189, 248), 1.2f))
            using (Pen gridPen = new Pen(Color.FromArgb(30, 58, 138), 1f) { DashStyle = DashStyle.Dot })
            {
                g.DrawEllipse(globePen, globeCenterX - globeRadius, globeCenterY - globeRadius, globeRadius * 2, globeRadius * 2);
                g.DrawEllipse(gridPen, globeCenterX - globeRadius, globeCenterY - (int)(globeRadius * 0.4f), globeRadius * 2, (int)(globeRadius * 0.8f));
                g.DrawEllipse(gridPen, globeCenterX - (int)(globeRadius * 0.4f), globeCenterY - globeRadius, (int)(globeRadius * 0.8f), globeRadius * 2);
                g.DrawLine(gridPen, globeCenterX - globeRadius, globeCenterY, globeCenterX + globeRadius, globeCenterY);
                g.DrawLine(gridPen, globeCenterX, globeCenterY - globeRadius, globeCenterX, globeCenterY + globeRadius);
            }

            // 3. Render 3D Extruded Data Towers with Glowing Heat Tops
            int n = Math.Max(1, data.Categories != null ? data.Categories.Count : 1);
            double maxVal = GetMaxDataValue(data, false);
            if (maxVal <= 0) maxVal = 100;

            using (Font tagFont = new Font("Segoe UI", Math.Max(6.8f, 7.5f * zoom), FontStyle.Bold))
            {
                for (int i = 0; i < n; i++)
                {
                    double angle = (i * 2 * Math.PI / n) - Math.PI / 2;
                    float towerBaseX = globeCenterX + (float)(Math.Cos(angle) * globeRadius * 0.65f);
                    float towerBaseY = globeCenterY + (float)(Math.Sin(angle) * globeRadius * 0.55f);

                    double val = GetValue(data, i, 0);
                    float towerH = Math.Max(15, (float)((val / maxVal) * (globeRadius * 0.7f)));
                    int towerW = 12;

                    // 3D Tower Column
                    Color tCol = colors[i % colors.Length];
                    Draw3DColumnBar(g, (int)towerBaseX - towerW / 2, (int)(towerBaseY - towerH), towerW, (int)towerH, 5, tCol);

                    // Glowing Beacon Top
                    using (SolidBrush glowB = new SolidBrush(Color.FromArgb(254, 240, 138)))
                    {
                        g.FillEllipse(glowB, towerBaseX - 3, towerBaseY - towerH - 8, 6, 6);
                    }

                    // Region Tag
                    string cName = (i < data.Categories.Count) ? data.Categories[i] : ("Vùng " + (i + 1));
                    using (SolidBrush txtB = new SolidBrush(Color.FromArgb(226, 232, 240)))
                    {
                        g.DrawString(cName + " (" + val.ToString("N0") + ")", tagFont, txtB, towerBaseX - 25, towerBaseY - towerH - 22);
                    }
                }
            }

            // 4. Bottom Timeline Player Control Bar
            int barY = plotY + plotH - 26;
            using (SolidBrush barBg = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (Font plFont = new Font("Segoe UI", 7.8f * zoom, FontStyle.Bold))
            using (SolidBrush playTxt = new SolidBrush(Color.FromArgb(56, 189, 248)))
            {
                g.FillRectangle(barBg, plotX + 10, barY, plotW - 20, 20);
                g.DrawString("▶ PHÁT BẢN ĐỒ 3D", plFont, playTxt, plotX + 18, barY + 3);
                g.DrawString("Mốc Thời Gian: 2026 - Q1   Q2   Q3   Q4", plFont, Brushes.White, plotX + 140, barY + 3);
            }
        }

        // ==================== 3D COLUMN / BAR DRAWING HELPERS ====================
        private static void Draw3DColumnBar(Graphics g, int x, int y, int w, int h, int depth, Color c)
        {
            if (h <= 0) return;
            Color topCol = ControlPaint.Light(c, 0.35f);
            Color sideCol = ControlPaint.Dark(c, 0.25f);

            // Front Face
            using (SolidBrush fb = new SolidBrush(c))
            {
                g.FillRectangle(fb, x, y, w, h);
            }

            // Top Face
            PointF[] topPts = new PointF[]
            {
                new PointF(x, y),
                new PointF(x + depth, y - depth),
                new PointF(x + w + depth, y - depth),
                new PointF(x + w, y)
            };
            using (SolidBrush tb = new SolidBrush(topCol))
            {
                g.FillPolygon(tb, topPts);
            }

            // Right Side Face
            PointF[] sidePts = new PointF[]
            {
                new PointF(x + w, y),
                new PointF(x + w + depth, y - depth),
                new PointF(x + w + depth, y + h - depth),
                new PointF(x + w, y + h)
            };
            using (SolidBrush sb = new SolidBrush(sideCol))
            {
                g.FillPolygon(sb, sidePts);
            }

            using (Pen borderP = new Pen(Color.White, 0.8f))
            {
                g.DrawRectangle(borderP, x, y, w, h);
                g.DrawPolygon(borderP, topPts);
                g.DrawPolygon(borderP, sidePts);
            }
        }

        private static void Draw3DHorzBar(Graphics g, int x, int y, int w, int h, int depth, Color c)
        {
            if (w <= 0) return;
            Color topCol = ControlPaint.Light(c, 0.35f);
            Color sideCol = ControlPaint.Dark(c, 0.25f);

            using (SolidBrush b = new SolidBrush(c)) g.FillRectangle(b, x, y, w, h);

            PointF[] topPts = new PointF[]
            {
                new PointF(x, y),
                new PointF(x + depth, y - depth),
                new PointF(x + w + depth, y - depth),
                new PointF(x + w, y)
            };
            using (SolidBrush tb = new SolidBrush(topCol)) g.FillPolygon(tb, topPts);

            PointF[] sidePts = new PointF[]
            {
                new PointF(x + w, y),
                new PointF(x + w + depth, y - depth),
                new PointF(x + w + depth, y + h - depth),
                new PointF(x + w, y + h)
            };
            using (SolidBrush sb = new SolidBrush(sideCol)) g.FillPolygon(sb, sidePts);

            using (Pen borderP = new Pen(Color.White, 0.8f))
            {
                g.DrawRectangle(borderP, x, y, w, h);
                g.DrawPolygon(borderP, topPts);
                g.DrawPolygon(borderP, sidePts);
            }
        }

        // ==================== Y-GRIDLINES & LABELS ====================
        private static void DrawYGridlines(Graphics g, int plotX, int plotY, int plotW, int plotH, double maxVal, float zoom)
        {
            using (Pen gridPen = new Pen(Color.FromArgb(241, 245, 249), 1.0f))
            using (Font yFont = new Font("Segoe UI", Math.Max(6.5f, 7.5f * zoom)))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(148, 163, 184)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            {
                for (int step = 0; step <= 4; step++)
                {
                    int gy = plotY + plotH - (plotH * step) / 4;
                    g.DrawLine(gridPen, plotX, gy, plotX + plotW, gy);

                    double num = (maxVal * step) / 4.0;
                    string numStr = (num >= 1000000) ? (num / 1000000.0).ToString("0.##") + "M" : ((num >= 10000) ? num.ToString("0") : ((num >= 1000) ? (num / 1000.0).ToString("0.#") + "k" : num.ToString("0")));
                    Rectangle numRect = new Rectangle(plotX - (int)(44 * zoom), gy - 8, (int)(40 * zoom), 16);
                    g.DrawString(numStr, yFont, txtB, numRect, sf);
                }
            }
        }

        // ==================== LEGEND ====================
        private static void DrawLegend(Graphics g, Rectangle bounds, DocChartData data, Color[] colors, int legendH, float zoom)
        {
            int serCount = (data.SeriesNames != null && data.SeriesNames.Count > 0) ? data.SeriesNames.Count : ((data.Categories != null) ? data.Categories.Count : 0);
            if (serCount <= 0) return;

            int legY = bounds.Bottom - legendH + (int)(2 * zoom);
            using (Font lFont = new Font("Segoe UI", Math.Max(6.5f, 8f * zoom)))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(71, 85, 105)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            {
                int totalW = 0;
                int[] itemWidths = new int[serCount];
                int sqSize = Math.Max(7, (int)(9 * zoom));

                for (int i = 0; i < serCount; i++)
                {
                    string label = (data.SeriesNames != null && i < data.SeriesNames.Count) ? data.SeriesNames[i] : ((data.Categories != null && i < data.Categories.Count) ? data.Categories[i] : "Chuỗi " + (i + 1));
                    SizeF sz = g.MeasureString(label, lFont);
                    itemWidths[i] = sqSize + (int)sz.Width + (int)(16 * zoom);
                    totalW += itemWidths[i];
                }

                int legX = bounds.X + (bounds.Width - totalW) / 2;
                if (legX < bounds.X + 8) legX = bounds.X + 8;

                for (int i = 0; i < serCount; i++)
                {
                    Color col = colors[i % colors.Length];
                    using (SolidBrush sqB = new SolidBrush(col))
                    {
                        g.FillRectangle(sqB, legX, legY + (legendH - sqSize) / 2, sqSize, sqSize);
                    }
                    g.DrawRectangle(Pens.White, legX, legY + (legendH - sqSize) / 2, sqSize, sqSize);

                    string label = (data.SeriesNames != null && i < data.SeriesNames.Count) ? data.SeriesNames[i] : ((data.Categories != null && i < data.Categories.Count) ? data.Categories[i] : "Chuỗi " + (i + 1));
                    g.DrawString(label, lFont, txtB, legX + sqSize + 4, legY + legendH / 2, sf);

                    legX += itemWidths[i];
                }
            }
        }

        // ==================== DATA UTILITIES ====================
        private static double GetMaxDataValue(DocChartData data, bool isStacked)
        {
            double max = 1.0;
            if (data == null || data.Values == null || data.Values.Count == 0) return 100.0;

            if (isStacked)
            {
                foreach (var row in data.Values)
                {
                    double sum = 0;
                    if (row != null)
                    {
                        foreach (var v in row) sum += Math.Max(0, v);
                    }
                    if (sum > max) max = sum;
                }
            }
            else
            {
                foreach (var row in data.Values)
                {
                    if (row != null)
                    {
                        foreach (var v in row) if (v > max) max = v;
                    }
                }
            }

            return max * 1.15;
        }

        private static double GetValue(DocChartData data, int catIdx, int serIdx)
        {
            if (data == null || data.Values == null) return 0;
            if (catIdx < 0 || catIdx >= data.Values.Count) return 0;
            List<double> row = data.Values[catIdx];
            if (row == null || serIdx < 0 || serIdx >= row.Count) return 0;
            return row[serIdx];
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (rect.Width < d || rect.Height < d)
            {
                path.AddRectangle(rect);
                return path;
            }
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
