using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace TestCalcRibbonPerfect
{
    class Program
    {
        static void Main(string[] args)
        {
            int w = 1280;
            int h = 160;
            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(238, 238, 238));
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Draw Island Card
                int islandMargin = 10;
                Rectangle islandRect = new Rectangle(islandMargin, 39, w - islandMargin * 2, 111);
                using (GraphicsPath shadowPath = CreateRoundedRectanglePath(new RectangleF(islandRect.X, islandRect.Y + 1f, islandRect.Width, islandRect.Height), 10f))
                using (Pen shadowPen = new Pen(Color.FromArgb(20, 0, 0, 0), 2f))
                {
                    g.DrawPath(shadowPen, shadowPath);
                }
                using (GraphicsPath cardPath = CreateRoundedRectanglePath(new RectangleF(islandRect.X, islandRect.Y, islandRect.Width, islandRect.Height), 10f))
                using (SolidBrush cardBg = new SolidBrush(Color.White))
                using (Pen cardBorder = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
                {
                    g.FillPath(cardBg, cardPath);
                    g.DrawPath(cardBorder, cardPath);
                }

                // Divider line between Island Header and Action Body
                using (Pen sepPen = new Pen(Color.FromArgb(241, 245, 249), 1.0f))
                {
                    g.DrawLine(sepPen, islandRect.X + 8, 66, islandRect.Right - 8, 66);
                }

                // Top-Left Blue "Tệp"
                Rectangle fileBtnRect = new Rectangle(islandRect.X + 8, 42, 54, 21);
                using (GraphicsPath fp = CreateRoundedRectanglePath(new RectangleF(fileBtnRect.X, fileBtnRect.Y, fileBtnRect.Width, fileBtnRect.Height), 4f))
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(0, 120, 212)))
                using (Font fileFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.FillPath(fb, fp);
                    g.DrawString("Tệp", fileFont, Brushes.White, fileBtnRect, sf);
                }

                // Tabs: Trang đầu (active), Chèn, Bố trí trang, Công thức, Dữ liệu, Xem lại, Hiển thị
                string[] tabs = new string[] { "Trang đầu", "Chèn", "Bố trí trang", "Công thức", "Dữ liệu", "Xem lại", "Hiển thị" };
                int curTabX = fileBtnRect.Right + 8;
                using (Font tabFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                using (Font tabBoldFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    for (int i = 0; i < tabs.Length; i++)
                    {
                        int tW = Math.Max(64, (int)g.MeasureString(tabs[i], i == 0 ? tabBoldFont : tabFont).Width + 18);
                        Rectangle tRect = new Rectangle(curTabX, 42, tW, 21);
                        if (i == 0)
                        {
                            using (SolidBrush txtBrush = new SolidBrush(Color.FromArgb(16, 124, 65)))
                            {
                                g.DrawString(tabs[i], tabBoldFont, txtBrush, tRect, sf);
                                g.FillRectangle(txtBrush, curTabX + 4, 63, tW - 8, 2.5f);
                            }
                        }
                        else
                        {
                            using (SolidBrush txtBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                            {
                                g.DrawString(tabs[i], tabFont, txtBrush, tRect, sf);
                            }
                        }
                        curTabX += tW + 4;
                    }
                }

                // Top-Right Toggle Style Button inside Island: [Dạng Đảo]
                int toggleW = 100;
                Rectangle toggleRect = new Rectangle(islandRect.Right - toggleW - 8, 42, toggleW, 21);
                using (GraphicsPath tp = CreateRoundedRectanglePath(new RectangleF(toggleRect.X, toggleRect.Y, toggleRect.Width, toggleRect.Height), 10f))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (Pen tPen = new Pen(Color.FromArgb(203, 213, 225), 1.0f))
                {
                    g.FillPath(tb, tp);
                    g.DrawPath(tPen, tp);
                    float icX = toggleRect.X + 8;
                    float icY = toggleRect.Y + 5;
                    Color icColor = Color.FromArgb(71, 85, 105);
                    using (Pen icPen = new Pen(icColor, 1.2f))
                    using (SolidBrush icBlue = new SolidBrush(Color.FromArgb(13, 110, 253)))
                    {
                        g.DrawRectangle(icPen, icX, icY, 13, 10);
                        g.FillRectangle(icBlue, icX + 2, icY + 2, 4, 3);
                        g.DrawLine(icPen, icX + 7, icY + 3, icX + 11, icY + 3);
                        g.DrawLine(icPen, icX + 2, icY + 7, icX + 11, icY + 7);
                    }
                    Rectangle textRect = new Rectangle(toggleRect.X + 24, toggleRect.Y, toggleRect.Width - 26, toggleRect.Height);
                    using (Font tFont = new Font("Segoe UI", 8.4f, FontStyle.Bold))
                    using (SolidBrush tTxtB = new SolidBrush(icColor))
                    using (StringFormat lsf = new StringFormat { LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("Dạng Đảo", tFont, tTxtB, textRect, lsf);
                    }
                }

                // ==================== DRAW BODY TABS ====================
                DrawNewCalcHomeTab(g);

                string outPath = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\test_calc_ribbon_perfect.png";
                bmp.Save(outPath, ImageFormat.Png);
                Console.WriteLine("SAVED TO: " + outPath);
            }
        }

        static void DrawNewCalcHomeTab(Graphics g)
        {
            using (Font labelFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (Font btnFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font boldFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Font miniFont = new Font("Segoe UI", 7.0f, FontStyle.Regular))
            using (Pen pBorder = new Pen(Color.FromArgb(203, 213, 225), 1f))
            using (Pen pDivider = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                // ==================== Group 1: Bảng tạm (X: 14..130) ====================
                Rectangle pasteBox = new Rectangle(14, 68, 42, 62);
                DrawBtn(g, pasteBox, false);
                DrawPasteIconLarge(g, 22, 71);
                g.DrawString("Dán ▾", btnFont, Brushes.Black, 18, 110);

                Rectangle cutBox = new Rectangle(58, 68, 72, 20);
                DrawBtn(g, cutBox, false);
                DrawScissorsIcon(g, 62, 72);
                g.DrawString("Cắt", btnFont, Brushes.Black, 80, 70);

                Rectangle copyBox = new Rectangle(58, 89, 72, 20);
                DrawBtn(g, copyBox, false);
                DrawCopySheetsIcon(g, 62, 93);
                g.DrawString("Sao chép ▾", btnFont, Brushes.Black, 80, 91);

                Rectangle fmtBox = new Rectangle(58, 110, 72, 20);
                DrawBtn(g, fmtBox, false);
                DrawFormatPainterIcon(g, 62, 113);
                g.DrawString("Chép ĐD", btnFont, Brushes.Black, 80, 111);

                g.DrawString("Bảng tạm", labelFont, Brushes.Gray, 50, 137);
                DrawDialogLauncher(g, 122, 136);
                g.DrawLine(pDivider, 136, 68, 136, 148);

                // ==================== Group 2: Phông chữ (X: 144..332) ====================
                Rectangle fontCombo = new Rectangle(144, 68, 100, 24);
                g.FillRectangle(Brushes.White, fontCombo);
                g.DrawRectangle(pBorder, fontCombo);
                g.DrawString("Calibri", btnFont, Brushes.Black, 147, 71);
                DrawChevron(g, 232, 78);

                Rectangle sizeCombo = new Rectangle(248, 68, 38, 24);
                g.FillRectangle(Brushes.White, sizeCombo);
                g.DrawRectangle(pBorder, sizeCombo);
                g.DrawString("11", btnFont, Brushes.Black, 252, 71);
                DrawChevron(g, 274, 78);

                Rectangle aIncBox = new Rectangle(290, 68, 20, 24);
                DrawBtn(g, aIncBox, false);
                DrawAIncrease(g, 292, 70);

                Rectangle aDecBox = new Rectangle(312, 68, 20, 24);
                DrawBtn(g, aDecBox, false);
                DrawADecrease(g, 314, 70);

                // Row 2: B, I, U, Border, Fill, Font Color
                Rectangle bRect = new Rectangle(144, 96, 20, 24);
                DrawBtn(g, bRect, false);
                g.DrawString("B", boldFont, Brushes.Black, 148, 98);

                Rectangle iRect = new Rectangle(166, 96, 20, 24);
                DrawBtn(g, iRect, false);
                using (Font fI = new Font("Segoe UI", 9f, FontStyle.Italic))
                    g.DrawString("I", fI, Brushes.Black, 172, 98);

                Rectangle uRect = new Rectangle(188, 96, 22, 24);
                DrawBtn(g, uRect, false);
                using (Font fU = new Font("Segoe UI", 9f, FontStyle.Underline))
                    g.DrawString("U", fU, Brushes.Black, 190, 98);
                DrawChevron(g, 201, 106);

                Rectangle bdrRect = new Rectangle(214, 96, 28, 24);
                DrawBtn(g, bdrRect, false);
                DrawCellBordersIcon(g, 217, 100);
                DrawChevron(g, 232, 106);

                Rectangle fillRect = new Rectangle(246, 96, 28, 24);
                DrawBtn(g, fillRect, false);
                DrawFillBucketIcon(g, 248, 98, Color.FromArgb(250, 204, 21));
                DrawChevron(g, 264, 106);

                Rectangle colRect = new Rectangle(278, 96, 28, 24);
                DrawBtn(g, colRect, false);
                DrawTextColorIcon(g, 281, 98, Color.FromArgb(220, 38, 38));
                DrawChevron(g, 296, 106);

                g.DrawString("Phông chữ", labelFont, Brushes.Gray, 210, 137);
                DrawDialogLauncher(g, 322, 136);
                g.DrawLine(pDivider, 338, 68, 338, 148);

                // ==================== Group 3: Căn chỉnh (X: 346..534) ====================
                // Row 1: Top, Mid, Btm, ab↗, Ngắt dòng
                Rectangle tAlign = new Rectangle(346, 68, 22, 24);
                DrawBtn(g, tAlign, false);
                DrawVAlign(g, 349, 72, 0);

                Rectangle mAlign = new Rectangle(370, 68, 22, 24);
                DrawBtn(g, mAlign, true); // Active state like Excel screenshot
                DrawVAlign(g, 373, 72, 1);

                Rectangle bAlign = new Rectangle(394, 68, 22, 24);
                DrawBtn(g, bAlign, false);
                DrawVAlign(g, 397, 72, 2);

                Rectangle oriBtn = new Rectangle(420, 68, 26, 24);
                DrawBtn(g, oriBtn, false);
                DrawOrientation(g, 422, 72);
                DrawChevron(g, 437, 78);

                Rectangle wrapBtn = new Rectangle(450, 68, 84, 24);
                DrawBtn(g, wrapBtn, false);
                DrawWrapTextIcon(g, 453, 72);
                g.DrawString("Ngắt dòng", btnFont, Brushes.Black, 471, 71);

                // Row 2: Left, Center, Right, Dec Indent, Inc Indent, Gộp ô ▾
                Rectangle lAlign = new Rectangle(346, 96, 22, 24);
                DrawBtn(g, lAlign, true); // Active state like Excel screenshot
                DrawHAlign(g, 349, 100, 0);

                Rectangle cAlign = new Rectangle(370, 96, 22, 24);
                DrawBtn(g, cAlign, false);
                DrawHAlign(g, 373, 100, 1);

                Rectangle rAlign = new Rectangle(394, 96, 22, 24);
                DrawBtn(g, rAlign, false);
                DrawHAlign(g, 397, 100, 2);

                Rectangle decInd = new Rectangle(420, 96, 22, 24);
                DrawBtn(g, decInd, false);
                DrawIndent(g, 423, 100, false);

                Rectangle incInd = new Rectangle(444, 96, 22, 24);
                DrawBtn(g, incInd, false);
                DrawIndent(g, 447, 100, true);

                Rectangle mergeBtn = new Rectangle(470, 96, 64, 24);
                DrawBtn(g, mergeBtn, false);
                DrawMergeCenter(g, 473, 100);
                g.DrawString("Gộp ô", btnFont, Brushes.Black, 489, 99);
                DrawChevron(g, 524, 106);

                g.DrawString("Căn chỉnh", labelFont, Brushes.Gray, 414, 137);
                DrawDialogLauncher(g, 524, 136);
                g.DrawLine(pDivider, 540, 68, 540, 148);

                // ==================== Group 4: Định dạng Số (X: 548..664) ====================
                Rectangle numCombo = new Rectangle(548, 68, 116, 24);
                g.FillRectangle(Brushes.White, numCombo);
                g.DrawRectangle(pBorder, numCombo);
                g.DrawString("Chung (General)", btnFont, Brushes.Black, 551, 71);
                DrawChevron(g, 650, 78);

                // Row 2: $, %, ,, Inc Dec, Dec Dec
                Rectangle currBtn = new Rectangle(548, 96, 22, 24);
                DrawBtn(g, currBtn, false);
                g.DrawString("$", boldFont, Brushes.Black, 550, 98);
                DrawChevron(g, 560, 106);

                Rectangle pctBtn = new Rectangle(572, 96, 20, 24);
                DrawBtn(g, pctBtn, false);
                g.DrawString("%", boldFont, Brushes.Black, 574, 98);

                Rectangle comBtn = new Rectangle(594, 96, 20, 24);
                DrawBtn(g, comBtn, false);
                g.DrawString(",", boldFont, Brushes.Black, 598, 98);

                Rectangle incDec = new Rectangle(616, 96, 22, 24);
                DrawBtn(g, incDec, false);
                DrawDecimalShift(g, 618, 99, true);

                Rectangle decDec = new Rectangle(640, 96, 22, 24);
                DrawBtn(g, decDec, false);
                DrawDecimalShift(g, 642, 99, false);

                g.DrawString("Định dạng Số", labelFont, Brushes.Gray, 576, 137);
                DrawDialogLauncher(g, 654, 136);
                g.DrawLine(pDivider, 670, 68, 670, 148);

                // ==================== Group 5: Kiểu dáng (X: 678..838) ====================
                Rectangle condBtn = new Rectangle(678, 68, 52, 62);
                DrawBtn(g, condBtn, false);
                DrawConditionalFormatExcel(g, 694, 73);
                g.DrawString("Định dạng\ncó đ.kiện ▾", miniFont, Brushes.Black, 680, 100);

                Rectangle tblBtn = new Rectangle(734, 68, 52, 62);
                DrawBtn(g, tblBtn, false);
                DrawFormatTableExcel(g, 750, 73);
                g.DrawString("Định dạng\nBảng ▾", miniFont, Brushes.Black, 740, 100);

                Rectangle cellSty = new Rectangle(790, 68, 48, 62);
                DrawBtn(g, cellSty, false);
                DrawCellStylesExcel(g, 804, 73);
                g.DrawString("Kiểu ô\ntính ▾", miniFont, Brushes.Black, 798, 100);

                g.DrawString("Kiểu dáng", labelFont, Brushes.Gray, 738, 137);
                g.DrawLine(pDivider, 844, 68, 844, 148);

                // ==================== Group 6: Ô tính (X: 852..990) ====================
                Rectangle insBtn = new Rectangle(852, 68, 42, 62);
                DrawBtn(g, insBtn, false);
                DrawInsertCellsExcel(g, 863, 73);
                g.DrawString("Chèn ▾", miniFont, Brushes.Black, 856, 106);

                Rectangle delBtn = new Rectangle(898, 68, 42, 62);
                DrawBtn(g, delBtn, false);
                DrawDeleteCellsExcel(g, 909, 73);
                g.DrawString("Xóa ▾", miniFont, Brushes.Black, 905, 106);

                Rectangle fmtCell = new Rectangle(944, 68, 46, 62);
                DrawBtn(g, fmtCell, false);
                DrawFormatCellsExcel(g, 957, 73);
                g.DrawString("Định dạng ▾", miniFont, Brushes.Black, 945, 106);

                g.DrawString("Ô tính", labelFont, Brushes.Gray, 908, 137);
                g.DrawLine(pDivider, 996, 68, 996, 148);

                // ==================== Group 7: Chỉnh sửa (X: 1004..1182) ====================
                // Left Column: AutoSum, Điền, Xóa
                Rectangle sumBtn = new Rectangle(1004, 68, 78, 20);
                DrawBtn(g, sumBtn, false);
                DrawAutoSumExcel(g, 1007, 72);
                g.DrawString("AutoSum", btnFont, Brushes.Black, 1022, 70);
                DrawChevron(g, 1070, 76);

                Rectangle fillBtn = new Rectangle(1004, 89, 78, 20);
                DrawBtn(g, fillBtn, false);
                DrawFillExcel(g, 1008, 93);
                g.DrawString("Điền ▾", btnFont, Brushes.Black, 1022, 91);

                Rectangle clearBtn = new Rectangle(1004, 110, 78, 20);
                DrawBtn(g, clearBtn, false);
                DrawEraserExcel(g, 1008, 113);
                g.DrawString("Xóa ▾", btnFont, Brushes.Black, 1022, 111);

                // Right Column: Sắp xếp & Lọc, Tìm & Chọn
                Rectangle sortBtn = new Rectangle(1086, 68, 46, 62);
                DrawBtn(g, sortBtn, false);
                DrawSortFilterExcel(g, 1098, 73);
                g.DrawString("Sắp xếp\n& Lọc ▾", miniFont, Brushes.Black, 1088, 100);

                Rectangle findBtn = new Rectangle(1136, 68, 46, 62);
                DrawBtn(g, findBtn, false);
                DrawFindSelectExcel(g, 1150, 73);
                g.DrawString("Tìm &\nChọn ▾", miniFont, Brushes.Black, 1140, 100);

                g.DrawString("Chỉnh sửa", labelFont, Brushes.Gray, 1060, 137);
            }
        }

        // ==================== HELPER DRAWINGS ====================
        static void DrawBtn(Graphics g, Rectangle r, bool active)
        {
            if (active)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(226, 232, 240)))
                using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1f))
                using (GraphicsPath path = CreateRoundedRectanglePath(new RectangleF(r.X, r.Y, r.Width, r.Height), 3f))
                {
                    g.FillPath(b, path);
                    g.DrawPath(p, path);
                }
            }
        }

        static void DrawChevron(Graphics g, float x, float y)
        {
            using (Pen p = new Pen(Color.FromArgb(100, 116, 139), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, x, y, x + 3f, y + 3f);
                g.DrawLine(p, x + 3f, y + 3f, x + 6f, y);
            }
        }

        static void DrawDialogLauncher(Graphics g, int x, int y)
        {
            using (Pen p = new Pen(Color.FromArgb(148, 163, 184), 1.0f))
            {
                g.DrawRectangle(p, x, y, 7, 7);
                g.DrawLine(p, x + 2, y + 5, x + 5, y + 2);
                g.DrawLine(p, x + 3, y + 2, x + 5, y + 2);
                g.DrawLine(p, x + 5, y + 4, x + 5, y + 2);
            }
        }

        static void DrawPasteIconLarge(Graphics g, float x, float y)
        {
            // Clipboard plate
            using (SolidBrush bBoard = new SolidBrush(Color.FromArgb(202, 138, 4)))
            using (GraphicsPath pBoard = CreateRoundedRectanglePath(new RectangleF(x, y + 3, 20, 24), 2f))
            {
                g.FillPath(bBoard, pBoard);
            }
            // Clip top
            using (SolidBrush bClip = new SolidBrush(Color.FromArgb(161, 161, 170)))
            using (GraphicsPath pClip = CreateRoundedRectanglePath(new RectangleF(x + 5, y + 1, 10, 4), 1f))
            {
                g.FillPath(bClip, pClip);
            }
            // Document paper
            using (SolidBrush bDoc = new SolidBrush(Color.White))
            using (Pen pDoc = new Pen(Color.FromArgb(203, 213, 225), 1f))
            using (GraphicsPath pPaper = CreateRoundedRectanglePath(new RectangleF(x + 4, y + 6, 17, 21), 1f))
            {
                g.FillPath(bDoc, pPaper);
                g.DrawPath(pDoc, pPaper);
            }
            // Lines on paper
            using (Pen pLine = new Pen(Color.FromArgb(59, 130, 246), 1.2f))
            {
                g.DrawLine(pLine, x + 7, y + 11, x + 17, y + 11);
                g.DrawLine(pLine, x + 7, y + 15, x + 17, y + 15);
                g.DrawLine(pLine, x + 7, y + 19, x + 14, y + 19);
            }
        }

        static void DrawScissorsIcon(Graphics g, float x, float y)
        {
            using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1.1f))
            {
                g.DrawEllipse(p, x, y + 1, 5, 5);
                g.DrawEllipse(p, x, y + 8, 5, 5);
                g.DrawLine(p, x + 4, y + 3, x + 13, y + 11);
                g.DrawLine(p, x + 4, y + 10, x + 13, y + 2);
            }
        }

        static void DrawCopySheetsIcon(Graphics g, float x, float y)
        {
            using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 1.1f))
            using (SolidBrush b = new SolidBrush(Color.White))
            {
                g.FillRectangle(b, x + 3, y, 9, 11);
                g.DrawRectangle(p, x + 3, y, 9, 11);
                g.FillRectangle(b, x, y + 3, 9, 11);
                g.DrawRectangle(p, x, y + 3, 9, 11);
            }
        }

        static void DrawFormatPainterIcon(Graphics g, float x, float y)
        {
            using (Pen pWood = new Pen(Color.FromArgb(180, 83, 9), 1.3f))
            using (Pen pFerr = new Pen(Color.FromArgb(148, 163, 184), 1.6f))
            using (SolidBrush bTip = new SolidBrush(Color.FromArgb(37, 99, 235)))
            {
                g.DrawLine(pWood, x + 2, y + 12, x + 7, y + 6);
                g.DrawLine(pFerr, x + 7, y + 6, x + 9, y + 4);
                g.FillRectangle(bTip, x + 8, y + 1, 5, 4);
            }
        }

        static void DrawAIncrease(Graphics g, float x, float y)
        {
            using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Pen p = new Pen(Color.FromArgb(37, 99, 235), 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawString("A", f, Brushes.Black, x - 1, y);
                g.DrawLine(p, x + 12, y + 12, x + 12, y + 4);
                g.DrawLine(p, x + 10, y + 6, x + 12, y + 4);
                g.DrawLine(p, x + 14, y + 6, x + 12, y + 4);
            }
        }

        static void DrawADecrease(Graphics g, float x, float y)
        {
            using (Font f = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (Pen p = new Pen(Color.FromArgb(37, 99, 235), 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawString("A", f, Brushes.Black, x, y + 2);
                g.DrawLine(p, x + 11, y + 4, x + 11, y + 12);
                g.DrawLine(p, x + 9, y + 10, x + 11, y + 12);
                g.DrawLine(p, x + 13, y + 10, x + 11, y + 12);
            }
        }

        static void DrawCellBordersIcon(Graphics g, float x, float y)
        {
            using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 1.1f))
            {
                g.DrawRectangle(p, x, y, 12, 12);
                g.DrawLine(p, x + 6, y, x + 6, y + 12);
                g.DrawLine(p, x, y + 6, x + 12, y + 6);
            }
        }

        static void DrawFillBucketIcon(Graphics g, float x, float y, Color col)
        {
            using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 1.1f))
            using (SolidBrush bBar = new SolidBrush(col))
            {
                PointF[] pts = new PointF[] {
                    new PointF(x + 2, y + 6),
                    new PointF(x + 8, y + 1),
                    new PointF(x + 13, y + 5),
                    new PointF(x + 7, y + 11)
                };
                g.DrawPolygon(p, pts);
                g.FillEllipse(bBar, x + 1, y + 9, 3, 4); // Drop
                g.FillRectangle(bBar, x, y + 14, 15, 3); // Underline bar
            }
        }

        static void DrawTextColorIcon(Graphics g, float x, float y, Color col)
        {
            using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(Color.Black))
            using (SolidBrush bBar = new SolidBrush(col))
            {
                g.DrawString("A", f, b, x + 1, y - 1);
                g.FillRectangle(bBar, x + 1, y + 14, 13, 3);
            }
        }

        static void DrawVAlign(Graphics g, float x, float y, int type)
        {
            using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (Pen pRef = new Pen(Color.FromArgb(148, 163, 184), 1.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (type == 0) // Top
                {
                    g.DrawLine(pRef, x, y + 1, x + 14, y + 1);
                    g.DrawLine(p, x + 2, y + 5, x + 12, y + 5);
                    g.DrawLine(p, x + 4, y + 9, x + 10, y + 9);
                }
                else if (type == 1) // Middle
                {
                    g.DrawLine(p, x + 1, y + 3, x + 13, y + 3);
                    g.DrawLine(p, x + 3, y + 7, x + 11, y + 7);
                    g.DrawLine(p, x + 1, y + 11, x + 13, y + 11);
                }
                else // Bottom
                {
                    g.DrawLine(p, x + 4, y + 3, x + 10, y + 3);
                    g.DrawLine(p, x + 2, y + 7, x + 12, y + 7);
                    g.DrawLine(pRef, x, y + 11, x + 14, y + 11);
                }
            }
        }

        static void DrawHAlign(Graphics g, float x, float y, int type)
        {
            using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (type == 0) // Left
                {
                    g.DrawLine(p, x, y + 2, x + 14, y + 2);
                    g.DrawLine(p, x, y + 6, x + 9, y + 6);
                    g.DrawLine(p, x, y + 10, x + 12, y + 10);
                }
                else if (type == 1) // Center
                {
                    g.DrawLine(p, x, y + 2, x + 14, y + 2);
                    g.DrawLine(p, x + 3, y + 6, x + 11, y + 6);
                    g.DrawLine(p, x + 1, y + 10, x + 13, y + 10);
                }
                else // Right
                {
                    g.DrawLine(p, x, y + 2, x + 14, y + 2);
                    g.DrawLine(p, x + 5, y + 6, x + 14, y + 6);
                    g.DrawLine(p, x + 2, y + 10, x + 14, y + 10);
                }
            }
        }

        static void DrawOrientation(Graphics g, float x, float y)
        {
            using (Font f = new Font("Segoe UI", 6.8f, FontStyle.Bold))
            using (Pen p = new Pen(Color.FromArgb(37, 99, 235), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawString("ab", f, Brushes.Black, x, y + 1);
                g.DrawLine(p, x + 9, y + 10, x + 14, y + 3);
                g.DrawLine(p, x + 11, y + 3, x + 14, y + 3);
                g.DrawLine(p, x + 14, y + 6, x + 14, y + 3);
            }
        }

        static void DrawWrapTextIcon(Graphics g, float x, float y)
        {
            using (Font f = new Font("Segoe UI", 6.8f, FontStyle.Bold))
            using (Pen p = new Pen(Color.FromArgb(37, 99, 235), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawString("ab", f, Brushes.Black, x, y - 1);
                g.DrawString("c", f, Brushes.Black, x, y + 7);
                // Return arrow
                g.DrawLine(p, x + 11, y + 3, x + 15, y + 3);
                g.DrawLine(p, x + 15, y + 3, x + 15, y + 10);
                g.DrawLine(p, x + 15, y + 10, x + 7, y + 10);
                g.DrawLine(p, x + 9, y + 8, x + 7, y + 10);
                g.DrawLine(p, x + 9, y + 12, x + 7, y + 10);
            }
        }

        static void DrawIndent(Graphics g, float x, float y, bool inc)
        {
            using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (Pen pArr = new Pen(Color.FromArgb(37, 99, 235), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, x + 5, y + 1, x + 14, y + 1);
                g.DrawLine(p, x + 5, y + 5, x + 11, y + 5);
                g.DrawLine(p, x + 5, y + 9, x + 14, y + 9);
                if (inc)
                {
                    g.DrawLine(pArr, x, y + 5, x + 4, y + 5);
                    g.DrawLine(pArr, x + 2, y + 3, x + 4, y + 5);
                    g.DrawLine(pArr, x + 2, y + 7, x + 4, y + 5);
                }
                else
                {
                    g.DrawLine(pArr, x + 4, y + 5, x, y + 5);
                    g.DrawLine(pArr, x + 2, y + 3, x, y + 5);
                    g.DrawLine(pArr, x + 2, y + 7, x, y + 5);
                }
            }
        }

        static void DrawMergeCenter(Graphics g, float x, float y)
        {
            using (Pen pBox = new Pen(Color.FromArgb(59, 130, 246), 1.2f))
            using (Pen pArr = new Pen(Color.FromArgb(37, 99, 235), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawRectangle(pBox, x, y, 14, 11);
                g.DrawLine(pArr, x + 3, y + 5, x + 11, y + 5);
                g.DrawLine(pArr, x + 5, y + 3, x + 3, y + 5);
                g.DrawLine(pArr, x + 5, y + 7, x + 3, y + 5);
                g.DrawLine(pArr, x + 9, y + 3, x + 11, y + 5);
                g.DrawLine(pArr, x + 9, y + 7, x + 11, y + 5);
            }
        }

        static void DrawDecimalShift(Graphics g, float x, float y, bool inc)
        {
            using (Font f = new Font("Segoe UI", 6.2f, FontStyle.Bold))
            using (Pen p = new Pen(Color.FromArgb(37, 99, 235), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (inc)
                {
                    g.DrawString(".00", f, Brushes.Black, x + 2, y + 4);
                    g.DrawLine(p, x + 12, y + 3, x + 3, y + 3);
                    g.DrawLine(p, x + 5, y + 1, x + 3, y + 3);
                    g.DrawLine(p, x + 5, y + 5, x + 3, y + 3);
                }
                else
                {
                    g.DrawString(".0", f, Brushes.Black, x + 2, y + 4);
                    g.DrawLine(p, x + 3, y + 3, x + 12, y + 3);
                    g.DrawLine(p, x + 10, y + 1, x + 12, y + 3);
                    g.DrawLine(p, x + 10, y + 5, x + 12, y + 3);
                }
            }
        }

        static void DrawConditionalFormatExcel(Graphics g, float x, float y)
        {
            float w = 20, h = 18;
            using (Pen pGrid = new Pen(Color.FromArgb(148, 163, 184), 1.0f) { LineJoin = LineJoin.Round })
            using (Pen pThin = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            using (SolidBrush bRed1 = new SolidBrush(Color.FromArgb(248, 113, 113)))
            using (SolidBrush bRed2 = new SolidBrush(Color.FromArgb(252, 165, 165)))
            using (SolidBrush bBlue = new SolidBrush(Color.FromArgb(147, 197, 253)))
            {
                g.FillRectangle(Brushes.White, x, y, w, h);
                // Heatmap middle column cells
                g.FillRectangle(bRed1, x + 7, y, 7, 6);
                g.FillRectangle(bRed2, x + 7, y + 6, 7, 6);
                g.FillRectangle(bBlue, x + 7, y + 12, 7, 6);

                g.DrawRectangle(pGrid, x, y, w, h);
                g.DrawLine(pThin, x + 7, y, x + 7, y + h);
                g.DrawLine(pThin, x + 14, y, x + 14, y + h);
                g.DrawLine(pThin, x, y + 6, x + w, y + 6);
                g.DrawLine(pThin, x, y + 12, x + w, y + 12);
            }
        }

        static void DrawFormatTableExcel(Graphics g, float x, float y)
        {
            float w = 20, h = 18;
            using (Pen pGrid = new Pen(Color.FromArgb(148, 163, 184), 1.0f) { LineJoin = LineJoin.Round })
            using (Pen pThin = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            using (SolidBrush bHead = new SolidBrush(Color.FromArgb(37, 99, 235)))
            using (SolidBrush bAlt = new SolidBrush(Color.FromArgb(239, 246, 255)))
            {
                g.FillRectangle(Brushes.White, x, y, w, h);
                g.FillRectangle(bHead, x, y, w, 5);
                g.FillRectangle(bAlt, x, y + 9, w, 4);

                g.DrawRectangle(pGrid, x, y, w, h);
                g.DrawLine(pThin, x, y + 5, x + w, y + 5);
                g.DrawLine(pThin, x, y + 9, x + w, y + 9);
                g.DrawLine(pThin, x, y + 13, x + w, y + 13);
                g.DrawLine(pThin, x + 7, y + 5, x + 7, y + h);
                g.DrawLine(pThin, x + 14, y + 5, x + 14, y + h);

                // Elegant diagonal paintbrush
                using (Pen pWood = new Pen(Color.FromArgb(180, 83, 9), 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                using (Pen pFerr = new Pen(Color.FromArgb(156, 163, 175), 1.5f))
                using (SolidBrush bTip = new SolidBrush(Color.FromArgb(37, 99, 235)))
                {
                    g.DrawLine(pWood, x + w + 1, y - 1, x + w - 4, y + 7);
                    g.DrawLine(pFerr, x + w - 4, y + 7, x + w - 6, y + 10);
                    PointF[] tip = new PointF[] {
                        new PointF(x + w - 6, y + 10),
                        new PointF(x + w - 9, y + 14),
                        new PointF(x + w - 4, y + 12)
                    };
                    g.FillPolygon(bTip, tip);
                }
            }
        }

        static void DrawCellStylesExcel(Graphics g, float x, float y)
        {
            float w = 20, h = 18;
            using (Pen pGrid = new Pen(Color.FromArgb(148, 163, 184), 1.0f) { LineJoin = LineJoin.Round })
            using (Pen pThin = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            using (SolidBrush bBlue = new SolidBrush(Color.FromArgb(59, 130, 246)))
            using (SolidBrush bSoft = new SolidBrush(Color.FromArgb(219, 234, 254)))
            {
                g.FillRectangle(Brushes.White, x, y, w, h);
                g.FillRectangle(bSoft, x + 7, y + 6, 7, 6);
                g.FillRectangle(bBlue, x + 14, y + 6, 6, 6);

                g.DrawRectangle(pGrid, x, y, w, h);
                g.DrawLine(pThin, x + 7, y, x + 7, y + h);
                g.DrawLine(pThin, x + 14, y, x + 14, y + h);
                g.DrawLine(pThin, x, y + 6, x + w, y + 6);
                g.DrawLine(pThin, x, y + 12, x + w, y + 12);
            }
        }

        static void DrawInsertCellsExcel(Graphics g, float x, float y)
        {
            float w = 20, h = 16;
            using (Pen pGrid = new Pen(Color.FromArgb(148, 163, 184), 1.0f) { LineJoin = LineJoin.Round })
            using (Pen pThin = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            using (SolidBrush bBlueCol = new SolidBrush(Color.FromArgb(191, 219, 254)))
            using (SolidBrush bIndicator = new SolidBrush(Color.FromArgb(37, 99, 235)))
            {
                g.FillRectangle(Brushes.White, x, y + 2, w, h);
                g.FillRectangle(bBlueCol, x, y + 2, 7, h);

                g.DrawRectangle(pGrid, x, y + 2, w, h);
                g.DrawLine(pThin, x + 7, y + 2, x + 7, y + 2 + h);
                g.DrawLine(pThin, x + 14, y + 2, x + 14, y + 2 + h);
                g.DrawLine(pThin, x, y + 2 + h / 2, x + w, y + 2 + h / 2);

                // Clean small insert tab indicator at top of column
                g.FillRectangle(bIndicator, x + 1, y, 5, 3);
            }
        }

        static void DrawDeleteCellsExcel(Graphics g, float x, float y)
        {
            float w = 20, h = 16;
            using (Pen pGrid = new Pen(Color.FromArgb(148, 163, 184), 1.0f) { LineJoin = LineJoin.Round })
            using (Pen pThin = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            using (Pen pRed = new Pen(Color.FromArgb(239, 68, 68), 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.FillRectangle(Brushes.White, x, y + 2, w, h);
                g.DrawRectangle(pGrid, x, y + 2, w, h);
                g.DrawLine(pThin, x + 7, y + 2, x + 7, y + 2 + h);
                g.DrawLine(pThin, x + 14, y + 2, x + 14, y + 2 + h);
                g.DrawLine(pThin, x, y + 2 + h / 2, x + w, y + 2 + h / 2);

                // Thin elegant red cross in middle column
                g.DrawLine(pRed, x + 8.5f, y + 5.5f, x + 12.5f, y + 13.5f);
                g.DrawLine(pRed, x + 12.5f, y + 5.5f, x + 8.5f, y + 13.5f);
            }
        }

        static void DrawFormatCellsExcel(Graphics g, float x, float y)
        {
            float w = 20, h = 16;
            using (Pen pGrid = new Pen(Color.FromArgb(148, 163, 184), 1.0f) { LineJoin = LineJoin.Round })
            using (Pen pThin = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            using (SolidBrush bTab = new SolidBrush(Color.FromArgb(59, 130, 246)))
            {
                g.FillRectangle(Brushes.White, x, y + 2, w, h);
                g.DrawRectangle(pGrid, x, y + 2, w, h);
                g.DrawLine(pThin, x + 7, y + 2, x + 7, y + 2 + h);
                g.DrawLine(pThin, x + 14, y + 2, x + 14, y + 2 + h);
                g.DrawLine(pThin, x, y + 2 + h / 2, x + w, y + 2 + h / 2);

                // Sizing/dimension blue bar on top
                g.FillRectangle(bTab, x + 5, y, 10, 3);
            }
        }

        static void DrawAutoSumExcel(Graphics g, float x, float y)
        {
            using (Font f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                g.DrawString("∑", f, Brushes.Black, x, y - 2);
            }
        }

        static void DrawFillExcel(Graphics g, float x, float y)
        {
            using (Pen pBox = new Pen(Color.FromArgb(148, 163, 184), 1.0f))
            using (Pen pArr = new Pen(Color.FromArgb(37, 99, 235), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawRectangle(pBox, x, y, 11, 11);
                g.DrawLine(pArr, x + 5.5f, y + 2, x + 5.5f, y + 9);
                g.DrawLine(pArr, x + 3, y + 6.5f, x + 5.5f, y + 9);
                g.DrawLine(pArr, x + 8, y + 6.5f, x + 5.5f, y + 9);
            }
        }

        static void DrawEraserExcel(Graphics g, float x, float y)
        {
            using (Pen p = new Pen(Color.FromArgb(100, 116, 139), 1.0f) { LineJoin = LineJoin.Round })
            using (SolidBrush bPurple = new SolidBrush(Color.FromArgb(192, 132, 252)))
            using (SolidBrush bWhite = new SolidBrush(Color.White))
            {
                PointF[] pTop = new PointF[] {
                    new PointF(x + 2, y + 7),
                    new PointF(x + 7, y + 1),
                    new PointF(x + 11, y + 5),
                    new PointF(x + 6, y + 11)
                };
                g.FillPolygon(bPurple, pTop);
                g.DrawPolygon(p, pTop);

                PointF[] pBtm = new PointF[] {
                    new PointF(x + 4, y + 9),
                    new PointF(x + 6, y + 11),
                    new PointF(x + 9, y + 14),
                    new PointF(x + 7, y + 12)
                };
                g.FillPolygon(bWhite, pBtm);
            }
        }

        static void DrawSortFilterExcel(Graphics g, float x, float y)
        {
            using (Font f = new Font("Segoe UI", 6.5f, FontStyle.Bold))
            using (SolidBrush bBlue = new SolidBrush(Color.FromArgb(37, 99, 235)))
            using (Pen pFunnel = new Pen(Color.FromArgb(100, 116, 139), 1.1f) { LineJoin = LineJoin.Round })
            {
                g.DrawString("A", f, bBlue, x - 3, y - 1);
                g.DrawString("Z", f, bBlue, x - 3, y + 7);
                using (Pen pArr = new Pen(Color.FromArgb(37, 99, 235), 1.1f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(pArr, x + 6, y + 1, x + 6, y + 14);
                    g.DrawLine(pArr, x + 4, y + 11, x + 6, y + 14);
                    g.DrawLine(pArr, x + 8, y + 11, x + 6, y + 14);
                }

                // Funnel
                PointF[] pts = new PointF[] {
                    new PointF(x + 10, y + 1),
                    new PointF(x + 20, y + 1),
                    new PointF(x + 16, y + 8),
                    new PointF(x + 16, y + 14),
                    new PointF(x + 14, y + 14),
                    new PointF(x + 14, y + 8)
                };
                g.FillPolygon(Brushes.White, pts);
                g.DrawPolygon(pFunnel, pts);
            }
        }

        static void DrawFindSelectExcel(Graphics g, float x, float y)
        {
            using (Pen pLens = new Pen(Color.FromArgb(100, 116, 139), 1.3f))
            using (Pen pHandle = new Pen(Color.FromArgb(71, 85, 105), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (SolidBrush bGlass = new SolidBrush(Color.FromArgb(239, 246, 255)))
            {
                g.FillEllipse(bGlass, x, y, 12, 12);
                g.DrawEllipse(pLens, x, y, 12, 12);
                g.DrawLine(pHandle, x + 10, y + 10, x + 16, y + 16);
            }
        }

        static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
