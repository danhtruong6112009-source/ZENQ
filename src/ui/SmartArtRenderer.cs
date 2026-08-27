using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public static class SmartArtRenderer
    {
        public static string[] GetSmartArtNodeList(string smartArtType, string textContent)
        {
            if (!string.IsNullOrEmpty(textContent))
            {
                string[] parts = textContent.Split(new char[] { '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts != null && parts.Length > 0)
                {
                    List<string> clean = new List<string>();
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string t = parts[i].Trim();
                        if (!string.IsNullOrEmpty(t) && !t.StartsWith("🗺️") && !t.StartsWith("❖") && !t.StartsWith("=="))
                        {
                            clean.Add(t);
                        }
                    }
                    if (clean.Count > 0) return clean.ToArray();
                }
            }

            string type = smartArtType ?? "Cycle_Radial";
            if (type == "Cycle_Radial")
            {
                return new string[] { "Lập kế hoạch", "Triển khai", "Kiểm thử", "Bàn giao" };
            }
            else if (type == "Process_Linear")
            {
                return new string[] { "Khởi tạo", "Thực thi", "Bàn giao" };
            }
            else if (type == "Process_Chevron")
            {
                return new string[] { "Khảo sát", "Thiết kế", "Triển khai", "Bàn giao" };
            }
            else if (type == "Hierarchy_Tree")
            {
                return new string[] { "Ban Giám Đốc", "Phòng Kỹ Thuật", "Phòng Kinh Doanh", "Phòng Nhân Sự" };
            }
            else if (type == "List_Blocks")
            {
                return new string[] { "Tối ưu hóa hiệu năng", "Trải nghiệm người dùng", "Mở rộng hệ sinh thái" };
            }
            else if (type == "Matrix_Grid")
            {
                return new string[] { "Điểm mạnh (S)", "Cơ hội (O)", "Điểm yếu (W)", "Thách thức (T)" };
            }
            else if (type == "Pyramid_Layers")
            {
                return new string[] { "Tầm nhìn & Chiến lược", "Kế hoạch & Quản trị", "Năng lực cốt lõi" };
            }

            return new string[] { "Bước 1", "Bước 2", "Bước 3" };
        }

        public static Color[] GetPaletteColors(int presetIndex, int count)
        {
            Color[][] presets = new Color[][]
            {
                // Preset 0: Office Warm (Cam & Vàng - Like user screenshot)
                new Color[] { Color.FromArgb(230, 81, 0), Color.FromArgb(245, 124, 0), Color.FromArgb(251, 140, 0), Color.FromArgb(255, 179, 0), Color.FromArgb(253, 216, 53) },
                // Preset 1: Office Blue (Xanh Lam & Nước Biển)
                new Color[] { Color.FromArgb(13, 71, 161), Color.FromArgb(25, 118, 210), Color.FromArgb(2, 136, 209), Color.FromArgb(0, 151, 167), Color.FromArgb(0, 131, 143) },
                // Preset 2: Colorful Accent (Đa Sắc Màu)
                new Color[] { Color.FromArgb(30, 136, 229), Color.FromArgb(67, 160, 71), Color.FromArgb(251, 140, 0), Color.FromArgb(142, 36, 170), Color.FromArgb(229, 57, 53) },
                // Preset 3: Emerald Green (Xanh Lục)
                new Color[] { Color.FromArgb(27, 94, 32), Color.FromArgb(46, 125, 50), Color.FromArgb(67, 160, 71), Color.FromArgb(102, 187, 106), Color.FromArgb(129, 199, 132) },
                // Preset 4: Purple & Magenta (Tím & Hồng)
                new Color[] { Color.FromArgb(74, 20, 140), Color.FromArgb(123, 31, 162), Color.FromArgb(171, 71, 188), Color.FromArgb(216, 27, 96), Color.FromArgb(173, 20, 87) },
                // Preset 5: Monochrome Gray (Đơn Sắc)
                new Color[] { Color.FromArgb(38, 50, 56), Color.FromArgb(55, 71, 79), Color.FromArgb(69, 90, 100), Color.FromArgb(84, 110, 122), Color.FromArgb(120, 144, 156) }
            };

            int pIdx = Math.Max(0, Math.Min(presets.Length - 1, presetIndex));
            Color[] src = presets[pIdx];

            Color[] res = new Color[count];
            for (int i = 0; i < count; i++)
            {
                res[i] = src[i % src.Length];
            }
            return res;
        }

        public static void DrawSmartArt(Graphics g, Rectangle bounds, string smartArtType, string smartArtTitle, Color baseColor, Color outlineColor, string customText, bool isSelected, float zoomLevel)
        {
            DrawSmartArt(g, bounds, smartArtType, smartArtTitle, 0, customText, isSelected, zoomLevel);
        }

        public static void DrawSmartArt(Graphics g, Rectangle bounds, string smartArtType, string smartArtTitle, int colorPresetIndex, string customText, bool isSelected, float zoomLevel)
        {
            if (bounds.Width <= 10 || bounds.Height <= 10) return;

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            string type = smartArtType ?? "Cycle_Radial";
            string[] nodes = GetSmartArtNodeList(type, customText);
            Color[] colors = GetPaletteColors(colorPresetIndex, nodes.Length);

            Rectangle contentRect = bounds;

            if (type == "Cycle_Radial")
            {
                DrawCycleRadial(g, contentRect, nodes, colors, colorPresetIndex, zoomLevel);
            }
            else if (type == "Process_Chevron")
            {
                DrawProcessChevron(g, contentRect, nodes, colors, zoomLevel);
            }
            else if (type == "Hierarchy_Tree")
            {
                DrawHierarchyTree(g, contentRect, nodes, colors, zoomLevel);
            }
            else if (type == "List_Blocks")
            {
                DrawListBlocks(g, contentRect, nodes, colors, zoomLevel);
            }
            else if (type == "Matrix_Grid")
            {
                DrawMatrixGrid(g, contentRect, nodes, colors, zoomLevel);
            }
            else if (type == "Pyramid_Layers")
            {
                DrawPyramidLayers(g, contentRect, nodes, colors, zoomLevel);
            }
            else // Default: Process_Linear
            {
                DrawProcessLinear(g, contentRect, nodes, colors, zoomLevel);
            }

            // Selection Handles if Selected
            if (isSelected)
            {
                using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
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

        // ==================== 1. CYCLE RADIAL (EXACT MATCH TO MS OFFICE) ====================
        private static void DrawCycleRadial(Graphics g, Rectangle r, string[] nodes, Color[] colors, int colorPreset, float zoom)
        {
            int centerX = r.X + r.Width / 2;
            int centerY = r.Y + r.Height / 2;
            int radius = Math.Min(r.Width, r.Height) / 2 - (int)(32 * zoom);
            if (radius < 25) radius = 25;

            // 1. Draw Dashed Circular Ring in the Center (Exact matching MS Office!)
            Color ringCol = (colorPreset == 0) ? Color.FromArgb(0, 120, 215) : colors[0];
            using (Pen ringPen = new Pen(ringCol, Math.Max(2f, 2.8f * zoom)))
            {
                ringPen.DashStyle = DashStyle.Dash;
                ringPen.DashPattern = new float[] { 4f, 3f };
                Rectangle ringRect = new Rectangle(centerX - radius, centerY - radius, radius * 2, radius * 2);
                g.DrawEllipse(ringPen, ringRect);
            }

            // 2. Draw Rectangular Nodes along the 4 cardinal positions (Top, Right, Bottom, Left)
            int count = nodes.Length;
            int cardW = Math.Max(40, (int)(110 * zoom));
            int cardH = Math.Max(24, (int)(46 * zoom));

            for (int i = 0; i < count; i++)
            {
                double angleDeg = (i * (360.0 / count)) - 90.0; // Start at Top (-90 deg)
                double rad = angleDeg * Math.PI / 180.0;

                int nx = centerX + (int)(radius * Math.Cos(rad)) - cardW / 2;
                int ny = centerY + (int)(radius * Math.Sin(rad)) - cardH / 2;
                Rectangle cardR = new Rectangle(nx, ny, cardW, cardH);

                Color c = colors[i % colors.Length];
                using (SolidBrush cb = new SolidBrush(c))
                {
                    g.FillRectangle(cb, cardR);
                }

                using (Font f = new Font("Segoe UI", Math.Max(7.5f, 9.5f * zoom), FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(nodes[i], f, Brushes.White, cardR, sf);
                }
            }
        }

        // ==================== 2. PROCESS LINEAR ====================
        private static void DrawProcessLinear(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = nodes.Length;
            if (count <= 0) return;

            int arrowW = Math.Max(12, (int)(22 * zoom));
            int totalSpacing = arrowW * (count - 1) + (int)(14 * zoom) * (count - 1);
            int cardW = Math.Max(30, (r.Width - totalSpacing) / count);
            int cardH = Math.Min(r.Height, Math.Max(30, (int)(70 * zoom)));
            int cardY = r.Y + (r.Height - cardH) / 2;

            int curX = r.X;
            for (int i = 0; i < count; i++)
            {
                Rectangle cardR = new Rectangle(curX, cardY, cardW, cardH);
                Color c = colors[i % colors.Length];

                using (SolidBrush cb = new SolidBrush(c))
                {
                    g.FillRectangle(cb, cardR);
                }

                using (Font f = new Font("Segoe UI", Math.Max(7.5f, 9.5f * zoom), FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(nodes[i], f, Brushes.White, cardR, sf);
                }

                curX += cardW;

                // Connecting Right Arrow ➔
                if (i < count - 1)
                {
                    int arrX = curX + (int)(6 * zoom);
                    int arrY = cardY + cardH / 2;
                    DrawVectorArrowRight(g, arrX, arrY, arrowW, Math.Max(8, (int)(16 * zoom)), Color.FromArgb(148, 163, 184));
                    curX += arrowW + (int)(14 * zoom);
                }
            }
        }

        // ==================== 3. PROCESS CHEVRON ====================
        private static void DrawProcessChevron(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = nodes.Length;
            if (count <= 0) return;

            int notch = Math.Max(8, (int)(18 * zoom));
            int chW = (r.Width + (count - 1) * notch) / count;
            int chH = Math.Min(r.Height, Math.Max(30, (int)(80 * zoom)));
            int chY = r.Y + (r.Height - chH) / 2;

            int curX = r.X;
            for (int i = 0; i < count; i++)
            {
                PointF[] pts = new PointF[]
                {
                    new PointF(curX, chY),
                    new PointF(curX + chW - notch, chY),
                    new PointF(curX + chW, chY + chH / 2f),
                    new PointF(curX + chW - notch, chY + chH),
                    new PointF(curX, chY + chH),
                    new PointF(curX + (i == 0 ? 0 : notch), chY + chH / 2f)
                };

                using (GraphicsPath cp = new GraphicsPath())
                {
                    cp.AddPolygon(pts);
                    Color c = colors[i % colors.Length];
                    using (SolidBrush cBrush = new SolidBrush(c))
                    {
                        g.FillPath(cBrush, cp);
                    }
                    using (Pen p = new Pen(Color.White, 2f * zoom))
                    {
                        g.DrawPath(p, cp);
                    }
                }

                int textLeft = curX + (i == 0 ? (int)(6 * zoom) : notch + (int)(4 * zoom));
                int textW = Math.Max(20, chW - notch - (int)(10 * zoom));
                Rectangle tRect = new Rectangle(textLeft, chY, textW, chH);

                using (Font tFont = new Font("Segoe UI", Math.Max(7.5f, 9.5f * zoom), FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(nodes[i], tFont, Brushes.White, tRect, sf);
                }

                curX += chW - notch;
            }
        }

        // ==================== 4. HIERARCHY TREE ====================
        private static void DrawHierarchyTree(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (nodes.Length == 0) return;

            // Root box (nodes[0])
            int rootW = Math.Max(60, (int)(160 * zoom));
            int rootH = Math.Max(24, (int)(42 * zoom));
            int rootX = r.X + (r.Width - rootW) / 2;
            int rootY = r.Y + (int)(6 * zoom);
            Rectangle rootRect = new Rectangle(rootX, rootY, rootW, rootH);

            Color rootCol = colors[0];
            using (SolidBrush rb = new SolidBrush(rootCol))
            {
                g.FillRectangle(rb, rootRect);
            }
            using (Font rf = new Font("Segoe UI", Math.Max(7.5f, 9.5f * zoom), FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(nodes[0], rf, Brushes.White, rootRect, sf);
            }

            // Children boxes (nodes[1..N])
            int childCount = nodes.Length - 1;
            if (childCount <= 0) return;

            int gap = Math.Max(8, (int)(14 * zoom));
            int childW = (r.Width - gap * (childCount - 1)) / childCount;
            int childH = Math.Max(24, (int)(42 * zoom));
            int childY = r.Bottom - childH - (int)(6 * zoom);

            int stemMidY = rootRect.Bottom + (childY - rootRect.Bottom) / 2;
            using (Pen linePen = new Pen(Color.FromArgb(100, 116, 139), 2f * zoom))
            {
                // Vertical line from root
                g.DrawLine(linePen, r.X + r.Width / 2, rootRect.Bottom, r.X + r.Width / 2, stemMidY);
                // Horizontal bar
                int leftMidX = r.X + childW / 2;
                int rightMidX = r.Right - childW / 2;
                g.DrawLine(linePen, leftMidX, stemMidY, rightMidX, stemMidY);
            }

            int curX = r.X;
            for (int i = 1; i <= childCount; i++)
            {
                Rectangle cRect = new Rectangle(curX, childY, childW, childH);
                using (Pen linePen = new Pen(Color.FromArgb(100, 116, 139), 2f * zoom))
                {
                    g.DrawLine(linePen, cRect.X + cRect.Width / 2, stemMidY, cRect.X + cRect.Width / 2, cRect.Y);
                }

                Color cc = colors[i % colors.Length];
                using (SolidBrush cb = new SolidBrush(cc))
                {
                    g.FillRectangle(cb, cRect);
                }

                using (Font cf = new Font("Segoe UI", Math.Max(7f, 8.5f * zoom), FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(nodes[i], cf, Brushes.White, cRect, sf);
                }

                curX += childW + gap;
            }
        }

        // ==================== 5. LIST BLOCKS ====================
        private static void DrawListBlocks(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = nodes.Length;
            if (count <= 0) return;

            int gap = Math.Max(6, (int)(10 * zoom));
            int blockH = (r.Height - gap * (count - 1)) / count;

            int curY = r.Y;
            for (int i = 0; i < count; i++)
            {
                Rectangle bRect = new Rectangle(r.X, curY, r.Width, blockH);
                Color c = colors[i % colors.Length];

                int badgeW = Math.Max(24, (int)(44 * zoom));
                Rectangle badgeR = new Rectangle(bRect.X, bRect.Y, badgeW, bRect.Height);
                using (SolidBrush bb = new SolidBrush(ControlPaint.Dark(c, 0.1f)))
                {
                    g.FillRectangle(bb, badgeR);
                }
                using (Font bf = new Font("Segoe UI", Math.Max(8f, 10.5f * zoom), FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString((i + 1).ToString("D2"), bf, Brushes.White, badgeR, sf);
                }

                Rectangle textR = new Rectangle(bRect.X + badgeW, bRect.Y, bRect.Width - badgeW, bRect.Height);
                using (SolidBrush tb = new SolidBrush(c))
                {
                    g.FillRectangle(tb, textR);
                }
                using (Font tf = new Font("Segoe UI", Math.Max(7.5f, 9.5f * zoom), FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    Rectangle innerText = new Rectangle(textR.X + (int)(10 * zoom), textR.Y, textR.Width - (int)(16 * zoom), textR.Height);
                    g.DrawString(nodes[i], tf, Brushes.White, innerText, sf);
                }

                curY += blockH + gap;
            }
        }

        // ==================== 6. MATRIX GRID ====================
        private static void DrawMatrixGrid(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int gap = Math.Max(6, (int)(10 * zoom));
            int cellW = (r.Width - gap) / 2;
            int cellH = (r.Height - gap) / 2;

            Rectangle[] cellRects = new Rectangle[]
            {
                new Rectangle(r.X, r.Y, cellW, cellH),                         // TL
                new Rectangle(r.X + cellW + gap, r.Y, cellW, cellH),           // TR
                new Rectangle(r.X, r.Y + cellH + gap, cellW, cellH),           // BL
                new Rectangle(r.X + cellW + gap, r.Y + cellH + gap, cellW, cellH)// BR
            };

            for (int i = 0; i < 4 && i < cellRects.Length; i++)
            {
                Rectangle cR = cellRects[i];
                Color c = colors[i % colors.Length];
                using (SolidBrush cb = new SolidBrush(c))
                {
                    g.FillRectangle(cb, cR);
                }

                string nodeText = (i < nodes.Length) ? nodes[i] : "Mục " + (i + 1);
                using (Font tf = new Font("Segoe UI", Math.Max(7.5f, 9.5f * zoom), FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(nodeText, tf, Brushes.White, cR, sf);
                }
            }
        }

        // ==================== 7. PYRAMID LAYERS ====================
        private static void DrawPyramidLayers(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = nodes.Length;
            if (count <= 0) return;

            int gap = Math.Max(3, (int)(4 * zoom));
            int layerH = (r.Height - gap * (count - 1)) / count;

            int centerX = r.X + r.Width / 2;
            int halfBase = r.Width / 2;

            for (int i = 0; i < count; i++)
            {
                float topRatio = (float)i / count;
                float botRatio = (float)(i + 1) / count;

                int topW = (int)(halfBase * topRatio * 2f);
                int botW = (int)(halfBase * botRatio * 2f);

                int layerY = r.Y + i * (layerH + gap);

                PointF[] pts = new PointF[]
                {
                    new PointF(centerX - topW / 2f, layerY),
                    new PointF(centerX + topW / 2f, layerY),
                    new PointF(centerX + botW / 2f, layerY + layerH),
                    new PointF(centerX - botW / 2f, layerY + layerH)
                };

                using (GraphicsPath pp = new GraphicsPath())
                {
                    pp.AddPolygon(pts);
                    Color c = colors[i % colors.Length];
                    using (SolidBrush pb = new SolidBrush(c))
                    {
                        g.FillPath(pb, pp);
                    }
                    using (Pen pPen = new Pen(Color.White, 2f * zoom))
                    {
                        g.DrawPath(pPen, pp);
                    }
                }

                Rectangle tBox = new Rectangle(centerX - botW / 2 + (int)(8 * zoom), layerY, Math.Max(20, botW - (int)(16 * zoom)), layerH);
                using (Font pf = new Font("Segoe UI", Math.Max(7.5f, 9f * zoom), FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(nodes[i], pf, Brushes.White, tBox, sf);
                }
            }
        }

        // ==================== Vector Arrow Helper ====================
        private static void DrawVectorArrowRight(Graphics g, int x, int y, int length, int headH, Color col)
        {
            int headW = headH / 2 + 2;
            int stemH = Math.Max(3, headH / 3);
            PointF[] arrowPts = new PointF[]
            {
                new PointF(x, y - stemH / 2f),
                new PointF(x + length - headW, y - stemH / 2f),
                new PointF(x + length - headW, y - headH / 2f),
                new PointF(x + length, y),
                new PointF(x + length - headW, y + headH / 2f),
                new PointF(x + length - headW, y + stemH / 2f),
                new PointF(x, y + stemH / 2f)
            };
            using (SolidBrush ab = new SolidBrush(col))
            {
                g.FillPolygon(ab, arrowPts);
            }
        }
    }
}
