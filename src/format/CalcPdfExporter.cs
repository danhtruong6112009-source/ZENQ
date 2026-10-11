using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using ZenQOffice.Core;
using ZenQOffice.UI;

namespace ZenQOffice.Format
{
    public class CalcPdfOptions
    {
        public bool ExportAllSheets { get; set; }
        public bool ShowGridlines { get; set; }
        public bool RepeatHeaderRow { get; set; }
        public uint RepeatHeaderRowIndex { get; set; }
        public string PaperSize { get; set; }
        public bool IsLandscape { get; set; }
        public double MarginLeftPt { get; set; }
        public double MarginRightPt { get; set; }
        public double MarginTopPt { get; set; }
        public double MarginBottomPt { get; set; }
        public bool IncludeFloatingObjects { get; set; }
        public string HeaderLeft { get; set; }
        public string HeaderCenter { get; set; }
        public string HeaderRight { get; set; }
        public string FooterLeft { get; set; }
        public string FooterCenter { get; set; }
        public string FooterRight { get; set; }

        public CalcPdfOptions()
        {
            ExportAllSheets = false;
            ShowGridlines = true;
            RepeatHeaderRow = true;
            RepeatHeaderRowIndex = 1;
            PaperSize = "A4";
            IsLandscape = false;
            MarginLeftPt = 54.0;
            MarginRightPt = 54.0;
            MarginTopPt = 54.0;
            MarginBottomPt = 54.0;
            IncludeFloatingObjects = true;
            HeaderLeft = "";
            HeaderCenter = "";
            HeaderRight = "";
            FooterLeft = "";
            FooterCenter = "";
            FooterRight = "";
        }
    }

    public static class CalcPdfExporter
    {
        static CalcPdfExporter()
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            }
            catch { }
        }

        public static bool Export(CalcView calcView, string filePath, bool exportAllSheets)
        {
            CalcPdfOptions options = new CalcPdfOptions();
            options.ExportAllSheets = exportAllSheets;
            if (calcView != null)
            {
                options.ShowGridlines = calcView.ShowGridlines;
                options.PaperSize = calcView.PaperSize;
                options.IsLandscape = calcView.IsLandscape;
                options.RepeatHeaderRow = true;
                options.RepeatHeaderRowIndex = 1;
                options.HeaderLeft = calcView.HeaderLeft ?? "";
                options.HeaderCenter = calcView.HeaderCenter ?? "";
                options.HeaderRight = calcView.HeaderRight ?? "";
                options.FooterLeft = calcView.FooterLeft ?? "";
                options.FooterCenter = calcView.FooterCenter ?? "";
                options.FooterRight = calcView.FooterRight ?? "";

                double mLeft, mRight, mTop, mBottom;
                ResolveMargins(calcView.MarginPreset, out mLeft, out mRight, out mTop, out mBottom);
                options.MarginLeftPt = mLeft;
                options.MarginRightPt = mRight;
                options.MarginTopPt = mTop;
                options.MarginBottomPt = mBottom;
            }

            return ExportWithOptions(calcView, filePath, options);
        }

        public static bool ExportWithOptions(CalcView calcView, string filePath, CalcPdfOptions options)
        {
            if (calcView == null || string.IsNullOrEmpty(filePath)) return false;
            if (options == null) options = new CalcPdfOptions();

            try
            {
                using (PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument())
                {
                    document.Info.Title = Path.GetFileNameWithoutExtension(filePath);
                    document.Info.Creator = "ZENQ OFFICE Suite - ZenQ Calc";

                    List<int> sheetIndices = new List<int>();
                    if (options.ExportAllSheets && calcView.Sheets != null && calcView.Sheets.Count > 0)
                    {
                        for (int i = 0; i < calcView.Sheets.Count; i++) sheetIndices.Add(i);
                    }
                    else
                    {
                        sheetIndices.Add(calcView.ActiveSheetIndex >= 0 ? calcView.ActiveSheetIndex : 0);
                    }

                    // First pass: Pre-calculate total page count across all sheets for accurate "Page X of Y"
                    int totalPageCount = 0;
                    for (int sIdx = 0; sIdx < sheetIndices.Count; sIdx++)
                    {
                        int sheetIndex = sheetIndices[sIdx];
                        if (sheetIndex < 0 || sheetIndex >= calcView.Sheets.Count) continue;
                        SparseMatrix sheet = calcView.Sheets[sheetIndex];
                        if (sheet == null) continue;

                        uint minR, maxR, minC, maxC;
                        GetSheetBounds(calcView, sheet, out minR, out maxR, out minC, out maxC);

                        double tempPageW, tempPageH;
                        GetPageDimensions(options.PaperSize, options.IsLandscape, out tempPageW, out tempPageH);
                        double tempUsableW = tempPageW - options.MarginLeftPt - options.MarginRightPt;
                        double tempUsableH = tempPageH - options.MarginTopPt - options.MarginBottomPt;

                        List<ColBand> cBands = ComputeColBands(calcView, minC, maxC, tempUsableW);
                        List<RowBand> rBands = ComputeRowBands(calcView, minR, maxR, tempUsableH, options.RepeatHeaderRow, options.RepeatHeaderRowIndex);
                        totalPageCount += (cBands.Count * rBands.Count);
                    }
                    if (totalPageCount == 0) totalPageCount = 1;

                    // Second pass: Render pages
                    int currentPageNumber = 1;
                    for (int sIdx = 0; sIdx < sheetIndices.Count; sIdx++)
                    {
                        int sheetIndex = sheetIndices[sIdx];
                        if (sheetIndex < 0 || sheetIndex >= calcView.Sheets.Count) continue;
                        SparseMatrix sheet = calcView.Sheets[sheetIndex];
                        if (sheet == null) continue;

                        uint minR, maxR, minC, maxC;
                        GetSheetBounds(calcView, sheet, out minR, out maxR, out minC, out maxC);

                        PageSize pSize = ResolvePaperSize(options.PaperSize);
                        PageOrientation pOrient = options.IsLandscape ? PageOrientation.Landscape : PageOrientation.Portrait;

                        // Create temporary page to query exact oriented dimensions
                        PdfPage samplePage = document.AddPage();
                        samplePage.Size = pSize;
                        samplePage.Orientation = pOrient;
                        double pageWidth = samplePage.Width.Point;
                        double pageHeight = samplePage.Height.Point;
                        document.Pages.Remove(samplePage);

                        double usableWidth = pageWidth - options.MarginLeftPt - options.MarginRightPt;
                        double usableHeight = pageHeight - options.MarginTopPt - options.MarginBottomPt;

                        List<ColBand> colBands = ComputeColBands(calcView, minC, maxC, usableWidth);
                        List<RowBand> rowBands = ComputeRowBands(calcView, minR, maxR, usableHeight, options.RepeatHeaderRow, options.RepeatHeaderRowIndex);

                        for (int cbIdx = 0; cbIdx < colBands.Count; cbIdx++)
                        {
                            ColBand colBand = colBands[cbIdx];
                            for (int rbIdx = 0; rbIdx < rowBands.Count; rbIdx++)
                            {
                                RowBand rowBand = rowBands[rbIdx];

                                PdfPage page = document.AddPage();
                                page.Size = pSize;
                                page.Orientation = pOrient;

                                using (XGraphics gfx = XGraphics.FromPdfPage(page))
                                {
                                    // 1. Headers & Footers
                                    RenderPageHeadersFooters(gfx, options, pageWidth, pageHeight, currentPageNumber, totalPageCount);

                                    // 2. Spreadsheet Cells & Grid
                                    double curY = options.MarginTopPt;

                                    // Render repeating header row if not on first row band
                                    if (!rowBand.IsFirstBand && options.RepeatHeaderRow && options.RepeatHeaderRowIndex >= minR && options.RepeatHeaderRowIndex <= maxR)
                                    {
                                        curY += RenderRow(gfx, calcView, sheet, options.RepeatHeaderRowIndex, colBand, options.MarginLeftPt, curY, options.ShowGridlines);
                                    }

                                    // Render data rows
                                    for (uint r = rowBand.StartRow; r <= rowBand.EndRow; r++)
                                    {
                                        curY += RenderRow(gfx, calcView, sheet, r, colBand, options.MarginLeftPt, curY, options.ShowGridlines);
                                    }

                                    // 3. Floating Visual Assets (Shapes, Images, Charts)
                                    if (options.IncludeFloatingObjects)
                                    {
                                        RenderFloatingObjects(gfx, calcView, colBand, rowBand, options.MarginLeftPt, options.MarginTopPt, options.RepeatHeaderRow, options.RepeatHeaderRowIndex);
                                    }
                                }

                                currentPageNumber++;
                            }
                        }
                    }

                    if (document.Pages.Count == 0)
                    {
                        PdfPage emptyPage = document.AddPage();
                        emptyPage.Size = ResolvePaperSize(options.PaperSize);
                        emptyPage.Orientation = options.IsLandscape ? PageOrientation.Landscape : PageOrientation.Portrait;
                    }

                    document.Save(filePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("PDF EXPORT ERROR: " + ex.ToString());
                return false;
            }
        }

        private static double RenderRow(XGraphics gfx, CalcView calcView, SparseMatrix sheet, uint rowIdx, ColBand colBand, double startX, double curY, bool showGridlines)
        {
            double rowH = calcView.GetRowHeight(rowIdx) * 0.75;
            double curX = startX;

            for (uint c = colBand.StartCol; c <= colBand.EndCol; c++)
            {
                double colW = calcView.GetColWidth(c) * 0.75;
                CellData cell = sheet.GetCell(rowIdx, c);

                // Skip merged children
                if (cell != null && cell.Format != null && cell.Format.IsMergedChild)
                {
                    curX += colW;
                    continue;
                }

                // Calculate merged dimensions
                double renderW = colW;
                double renderH = rowH;
                if (cell != null && cell.Format != null && cell.Format.IsMergedParent)
                {
                    renderW = 0.0;
                    for (uint mc = 0; mc < cell.Format.MergeColSpan; mc++)
                    {
                        uint targetC = c + mc;
                        if (targetC <= colBand.EndCol) renderW += calcView.GetColWidth(targetC) * 0.75;
                    }
                    renderH = 0.0;
                    for (uint mr = 0; mr < cell.Format.MergeRowSpan; mr++)
                    {
                        renderH += calcView.GetRowHeight(rowIdx + mr) * 0.75;
                    }
                }

                XRect cellRect = new XRect(curX, curY, renderW, renderH);

                // 1. Background fill
                if (cell != null && cell.Format != null && cell.Format.BgColor != 0 && cell.Format.BgColor != unchecked((int)0xFFFFFFFF))
                {
                    int bg = cell.Format.BgColor;
                    if ((bg & unchecked((int)0xFF000000)) == 0) bg |= unchecked((int)0xFF000000);
                    XSolidBrush bgBrush = new XSolidBrush(XColor.FromArgb(bg));
                    gfx.DrawRectangle(bgBrush, cellRect);
                }

                // 2. Gridline
                if (showGridlines)
                {
                    XPen gridPen = new XPen(XColor.FromArgb(226, 232, 240), 0.5);
                    gfx.DrawRectangle(gridPen, cellRect);
                }

                // 3. Custom borders
                if (cell != null && cell.Format != null && cell.Format.Borders != null)
                {
                    CellBorderSettings b = cell.Format.Borders;
                    if (b.Top)
                    {
                        DrawPdfBorderSegment(gfx, cellRect.Left, cellRect.Top, cellRect.Right, cellRect.Top, b.TopThickness > 0 ? b.TopThickness : b.Thickness, b.TopColor != 0 ? b.TopColor : b.Color);
                    }
                    if (b.Bottom)
                    {
                        DrawPdfBorderSegment(gfx, cellRect.Left, cellRect.Bottom, cellRect.Right, cellRect.Bottom, b.BottomThickness > 0 ? b.BottomThickness : b.Thickness, b.BottomColor != 0 ? b.BottomColor : b.Color);
                    }
                    if (b.Left)
                    {
                        DrawPdfBorderSegment(gfx, cellRect.Left, cellRect.Top, cellRect.Left, cellRect.Bottom, b.LeftThickness > 0 ? b.LeftThickness : b.Thickness, b.LeftColor != 0 ? b.LeftColor : b.Color);
                    }
                    if (b.Right)
                    {
                        DrawPdfBorderSegment(gfx, cellRect.Right, cellRect.Top, cellRect.Right, cellRect.Bottom, b.RightThickness > 0 ? b.RightThickness : b.Thickness, b.RightColor != 0 ? b.RightColor : b.Color);
                    }
                    if (b.DiagonalDown)
                    {
                        int dCol = b.DiagonalColor != 0 ? b.DiagonalColor : b.Color;
                        XPen pen = new XPen(ResolveXColor(dCol, 0xFF1E293B), b.DiagonalThickness > 0 ? b.DiagonalThickness * 0.75 : 0.75);
                        gfx.DrawLine(pen, cellRect.Left, cellRect.Top, cellRect.Right, cellRect.Bottom);
                    }
                    if (b.DiagonalUp)
                    {
                        int dCol = b.DiagonalColor != 0 ? b.DiagonalColor : b.Color;
                        XPen pen = new XPen(ResolveXColor(dCol, 0xFF1E293B), b.DiagonalThickness > 0 ? b.DiagonalThickness * 0.75 : 0.75);
                        gfx.DrawLine(pen, cellRect.Left, cellRect.Bottom, cellRect.Right, cellRect.Top);
                    }
                }

                // 4. Text Content
                if (cell != null)
                {
                    string dispText = cell.GetDisplayText();
                    if (!string.IsNullOrEmpty(dispText))
                    {
                        string fontName = (cell.Format != null && !string.IsNullOrEmpty(cell.Format.FontName)) ? cell.Format.FontName : "Calibri";
                        double fontSize = (cell.Format != null && cell.Format.FontSize > 0) ? cell.Format.FontSize * 0.9 : 9.9;
                        XFontStyle style = XFontStyle.Regular;
                        if (cell.Format != null)
                        {
                            if (cell.Format.Bold) style |= XFontStyle.Bold;
                            if (cell.Format.Italic) style |= XFontStyle.Italic;
                            if (cell.Format.Underline) style |= XFontStyle.Underline;
                            if (cell.Format.Strikethrough) style |= XFontStyle.Strikeout;
                        }

                        XFont font;
                        try { font = new XFont(fontName, fontSize, style); }
                        catch { font = new XFont("Arial", fontSize, style); }

                        XColor txtColor = ResolveXColor(cell.Format != null ? cell.Format.TextColor : 0, 0xFF000000);

                        XStringFormat format = new XStringFormat();
                        if (cell.Format != null)
                        {
                            if (cell.Format.Align == 1) format.Alignment = XStringAlignment.Center;
                            else if (cell.Format.Align == 2) format.Alignment = XStringAlignment.Far;
                            else format.Alignment = XStringAlignment.Near;

                            if (cell.Format.VAlign == 0) format.LineAlignment = XLineAlignment.Near;
                            else if (cell.Format.VAlign == 2) format.LineAlignment = XLineAlignment.Far;
                            else format.LineAlignment = XLineAlignment.Center;
                        }
                        else
                        {
                            format.Alignment = XStringAlignment.Near;
                            format.LineAlignment = XLineAlignment.Center;
                        }

                        double leftPad = 3.0 + ((cell.Format != null && cell.Format.Indent > 0) ? cell.Format.Indent * 6.0 : 0.0);
                        double rightPad = 3.0;
                        XRect textRect = new XRect(cellRect.X + leftPad, cellRect.Y, Math.Max(0.0, cellRect.Width - leftPad - rightPad), cellRect.Height);

                        gfx.DrawString(dispText, font, new XSolidBrush(txtColor), textRect, format);
                    }
                }

                curX += colW;
            }

            return rowH;
        }

        private static void DrawPdfBorderSegment(XGraphics gfx, double x1, double y1, double x2, double y2, int thicknessMode, int colorVal)
        {
            XColor col = ResolveXColor(colorVal, 0xFF0F172A);
            double widthPt = 0.75;
            if (thicknessMode == 2) widthPt = 1.5;
            else if (thicknessMode == 3) widthPt = 2.25;

            if (thicknessMode == 4) // Double border
            {
                XPen p1 = new XPen(col, 0.6);
                bool isHorz = Math.Abs(y1 - y2) < 0.01;
                if (isHorz)
                {
                    gfx.DrawLine(p1, x1, y1 - 1.0, x2, y2 - 1.0);
                    gfx.DrawLine(p1, x1, y1 + 1.0, x2, y2 + 1.0);
                }
                else
                {
                    gfx.DrawLine(p1, x1 - 1.0, y1, x2 - 1.0, y2);
                    gfx.DrawLine(p1, x1 + 1.0, y1, x2 + 1.0, y2);
                }
            }
            else
            {
                XPen p = new XPen(col, widthPt);
                gfx.DrawLine(p, x1, y1, x2, y2);
            }
        }

        private static void RenderFloatingObjects(XGraphics gfx, CalcView calcView, ColBand colBand, RowBand rowBand, double marginLeft, double marginTop, bool repeatHeaderRow, uint repeatHeaderRowIdx)
        {
            if (calcView.Shapes == null || calcView.Shapes.Count == 0) return;

            int bandPxLeft = 0;
            for (uint c = 1; c < colBand.StartCol; c++) bandPxLeft += calcView.GetColWidth(c);
            int bandPxWidth = 0;
            for (uint c = colBand.StartCol; c <= colBand.EndCol; c++) bandPxWidth += calcView.GetColWidth(c);
            int bandPxRight = bandPxLeft + bandPxWidth;

            int bandPxTop = 0;
            for (uint r = 1; r < rowBand.StartRow; r++) bandPxTop += calcView.GetRowHeight(r);
            int bandPxHeight = 0;
            for (uint r = rowBand.StartRow; r <= rowBand.EndRow; r++) bandPxHeight += calcView.GetRowHeight(r);
            int bandPxBottom = bandPxTop + bandPxHeight;

            double extraTopOffset = (!rowBand.IsFirstBand && repeatHeaderRow && repeatHeaderRowIdx >= 1) ? calcView.GetRowHeight(repeatHeaderRowIdx) * 0.75 : 0.0;

            foreach (DocShapeObject sh in calcView.Shapes)
            {
                if (sh == null || !sh.Visible) continue;
                int shLeft = sh.PageX;
                int shTop = sh.PageY;
                int shW = sh.BaseWidth + sh.WidthDelta;
                int shH = sh.BaseHeight + sh.HeightDelta;
                int shRight = shLeft + shW;
                int shBottom = shTop + shH;

                if (shRight <= bandPxLeft || shLeft >= bandPxRight || shBottom <= bandPxTop || shTop >= bandPxBottom)
                {
                    continue;
                }

                double pdfX = marginLeft + (shLeft - bandPxLeft) * 0.75;
                double pdfY = marginTop + extraTopOffset + (shTop - bandPxTop) * 0.75;
                double pdfW = shW * 0.75;
                double pdfH = shH * 0.75;

                if (sh.ShapeType == "Image" && (sh.CachedImage != null || (!string.IsNullOrEmpty(sh.ImagePath) && File.Exists(sh.ImagePath))))
                {
                    Image img = sh.CachedImage;
                    bool disposeImg = false;
                    if (img == null && !string.IsNullOrEmpty(sh.ImagePath))
                    {
                        try
                        {
                            img = Image.FromFile(sh.ImagePath);
                            disposeImg = true;
                        }
                        catch { }
                    }
                    if (img != null)
                    {
                        try
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Position = 0;
                                using (XImage ximg = XImage.FromStream(ms))
                                {
                                    gfx.DrawImage(ximg, pdfX, pdfY, pdfW, pdfH);
                                }
                            }
                        }
                        catch { }
                        if (disposeImg) img.Dispose();
                    }
                }
                else
                {
                    int bmpW = Math.Max(20, shW * 2);
                    int bmpH = Math.Max(20, shH * 2);
                    using (Bitmap bmp = new Bitmap(bmpW, bmpH))
                    {
                        using (Graphics gBmp = Graphics.FromImage(bmp))
                        {
                            gBmp.SmoothingMode = SmoothingMode.HighQuality;
                            gBmp.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            gBmp.Clear(Color.Transparent);
                            Rectangle rBounds = new Rectangle(0, 0, bmpW, bmpH);
                            ShapeRenderer.DrawShape(gBmp, sh, rBounds, null, 2.0f);
                        }
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ms.Position = 0;
                            using (XImage ximg = XImage.FromStream(ms))
                            {
                                gfx.DrawImage(ximg, pdfX, pdfY, pdfW, pdfH);
                            }
                        }
                    }
                }
            }
        }

        private static void RenderPageHeadersFooters(XGraphics gfx, CalcPdfOptions options, double pageWidth, double pageHeight, int pageNum, int totalPages)
        {
            XFont hFont = new XFont("Segoe UI", 9.0, XFontStyle.Regular);
            XSolidBrush hBrush = new XSolidBrush(XColor.FromArgb(100, 116, 139));

            // Headers
            XRect hRect = new XRect(options.MarginLeftPt, options.MarginTopPt - 22.0, Math.Max(0.0, pageWidth - options.MarginLeftPt - options.MarginRightPt), 18.0);
            if (!string.IsNullOrEmpty(options.HeaderLeft))
            {
                XStringFormat fl = new XStringFormat(); fl.Alignment = XStringAlignment.Near; fl.LineAlignment = XLineAlignment.Center;
                gfx.DrawString(options.HeaderLeft, hFont, hBrush, hRect, fl);
            }
            if (!string.IsNullOrEmpty(options.HeaderCenter))
            {
                XStringFormat fc = new XStringFormat(); fc.Alignment = XStringAlignment.Center; fc.LineAlignment = XLineAlignment.Center;
                gfx.DrawString(options.HeaderCenter, hFont, hBrush, hRect, fc);
            }
            if (!string.IsNullOrEmpty(options.HeaderRight))
            {
                XStringFormat fr = new XStringFormat(); fr.Alignment = XStringAlignment.Far; fr.LineAlignment = XLineAlignment.Center;
                gfx.DrawString(options.HeaderRight, hFont, hBrush, hRect, fr);
            }

            // Footers
            XRect fRect = new XRect(options.MarginLeftPt, pageHeight - options.MarginBottomPt + 6.0, Math.Max(0.0, pageWidth - options.MarginLeftPt - options.MarginRightPt), 18.0);
            if (!string.IsNullOrEmpty(options.FooterLeft))
            {
                XStringFormat fl = new XStringFormat(); fl.Alignment = XStringAlignment.Near; fl.LineAlignment = XLineAlignment.Center;
                gfx.DrawString(options.FooterLeft, hFont, hBrush, fRect, fl);
            }

            string fCenter = options.FooterCenter;
            if (string.IsNullOrEmpty(fCenter))
            {
                fCenter = string.Format("Trang {0} / {1}", pageNum, totalPages);
            }
            else
            {
                fCenter = fCenter.Replace("{PAGE}", pageNum.ToString()).Replace("{NUMPAGES}", totalPages.ToString());
            }
            XStringFormat fc2 = new XStringFormat(); fc2.Alignment = XStringAlignment.Center; fc2.LineAlignment = XLineAlignment.Center;
            gfx.DrawString(fCenter, hFont, hBrush, fRect, fc2);

            if (!string.IsNullOrEmpty(options.FooterRight))
            {
                XStringFormat fr = new XStringFormat(); fr.Alignment = XStringAlignment.Far; fr.LineAlignment = XLineAlignment.Center;
                gfx.DrawString(options.FooterRight, hFont, hBrush, fRect, fr);
            }
        }

        private static XColor ResolveXColor(int argb, uint defaultFallback)
        {
            if (argb == 0) argb = unchecked((int)defaultFallback);
            if ((argb & unchecked((int)0xFF000000)) == 0) argb |= unchecked((int)0xFF000000);
            return XColor.FromArgb(argb);
        }

        private static PageSize ResolvePaperSize(string paperSize)
        {
            if (string.IsNullOrEmpty(paperSize)) return PageSize.A4;
            string clean = paperSize.Trim().ToUpperInvariant();
            if (clean.StartsWith("LETTER")) return PageSize.Letter;
            if (clean.StartsWith("LEGAL")) return PageSize.Legal;
            if (clean.StartsWith("A3")) return PageSize.A3;
            if (clean.StartsWith("A5")) return PageSize.A5;
            if (clean.StartsWith("TABLOID") || clean.StartsWith("LEDGER")) return PageSize.Tabloid;
            return PageSize.A4;
        }

        private static void GetPageDimensions(string paperSize, bool isLandscape, out double width, out double height)
        {
            PageSize ps = ResolvePaperSize(paperSize);
            switch (ps)
            {
                case PageSize.Letter: width = 612.0; height = 792.0; break;
                case PageSize.Legal: width = 612.0; height = 1008.0; break;
                case PageSize.A3: width = 841.89; height = 1190.55; break;
                case PageSize.A5: width = 419.53; height = 595.28; break;
                case PageSize.Tabloid: width = 792.0; height = 1224.0; break;
                case PageSize.A4:
                default: width = 595.28; height = 841.89; break;
            }
            if (isLandscape)
            {
                double tmp = width;
                width = height;
                height = tmp;
            }
        }

        private static void ResolveMargins(int marginPreset, out double left, out double right, out double top, out double bottom)
        {
            switch (marginPreset)
            {
                case 1: // Narrow
                    top = 36.0; bottom = 36.0; left = 27.0; right = 27.0; break;
                case 2: // Moderate
                    top = 54.0; bottom = 54.0; left = 43.2; right = 43.2; break;
                case 3: // Wide
                    top = 72.0; bottom = 72.0; left = 72.0; right = 72.0; break;
                case 0: // Normal
                default:
                    top = 54.0; bottom = 54.0; left = 54.0; right = 54.0; break;
            }
        }

        private static void GetSheetBounds(CalcView calcView, SparseMatrix sheet, out uint minR, out uint maxR, out uint minC, out uint maxC)
        {
            if (calcView.HasPrintArea && calcView.PrintAreaEndRow >= calcView.PrintAreaStartRow && calcView.PrintAreaEndCol >= calcView.PrintAreaStartCol)
            {
                minR = calcView.PrintAreaStartRow;
                maxR = calcView.PrintAreaEndRow;
                minC = calcView.PrintAreaStartCol;
                maxC = calcView.PrintAreaEndCol;
                return;
            }

            minR = uint.MaxValue;
            maxR = 1;
            minC = uint.MaxValue;
            maxC = 1;

            Dictionary<ulong, CellData> grid = sheet.CloneGrid();
            foreach (KeyValuePair<ulong, CellData> kvp in grid)
            {
                CellData cd = kvp.Value;
                if (cd == null) continue;
                bool hasText = !string.IsNullOrEmpty(cd.EvaluatedStr) || !string.IsNullOrEmpty(cd.RawValue);
                bool hasBg = cd.Format != null && cd.Format.BgColor != 0 && cd.Format.BgColor != unchecked((int)0xFFFFFFFF);
                bool hasBorder = cd.Format != null && cd.Format.Borders != null && (cd.Format.Borders.Top || cd.Format.Borders.Bottom || cd.Format.Borders.Left || cd.Format.Borders.Right);

                if (hasText || hasBg || hasBorder)
                {
                    Tuple<uint, uint> coord = SparseMatrix.DecodeKey(kvp.Key);
                    if (coord.Item1 < minR) minR = coord.Item1;
                    if (coord.Item1 > maxR) maxR = coord.Item1;
                    if (coord.Item2 < minC) minC = coord.Item2;
                    if (coord.Item2 > maxC) maxC = coord.Item2;
                }
            }

            if (calcView.Shapes != null && calcView.Shapes.Count > 0)
            {
                foreach (DocShapeObject sh in calcView.Shapes)
                {
                    if (sh == null || !sh.Visible) continue;
                    int rightPx = sh.PageX + sh.BaseWidth + sh.WidthDelta;
                    int bottomPx = sh.PageY + sh.BaseHeight + sh.HeightDelta;

                    int curX = 0;
                    for (uint c = 1; c <= 256; c++)
                    {
                        curX += calcView.GetColWidth(c);
                        if (curX >= rightPx)
                        {
                            if (c > maxC) maxC = c;
                            break;
                        }
                    }

                    int curY = 0;
                    for (uint r = 1; r <= 1000; r++)
                    {
                        curY += calcView.GetRowHeight(r);
                        if (curY >= bottomPx)
                        {
                            if (r > maxR) maxR = r;
                            break;
                        }
                    }
                }
            }

            if (minR == uint.MaxValue) minR = 1;
            if (minC == uint.MaxValue) minC = 1;
            if (maxR < minR) maxR = minR + 9;
            if (maxC < minC) maxC = minC + 4;
        }

        private class ColBand
        {
            public uint StartCol;
            public uint EndCol;
            public double WidthPt;
        }

        private class RowBand
        {
            public uint StartRow;
            public uint EndRow;
            public double HeightPt;
            public bool IsFirstBand;
        }

        private static List<ColBand> ComputeColBands(CalcView calcView, uint minC, uint maxC, double usableWidth)
        {
            List<ColBand> bands = new List<ColBand>();
            uint c = minC;
            while (c <= maxC)
            {
                uint startC = c;
                double currentW = 0.0;
                while (c <= maxC)
                {
                    double colW = calcView.GetColWidth(c) * 0.75;
                    if (currentW > 0.0 && currentW + colW > usableWidth) break;
                    currentW += colW;
                    c++;
                }
                if (c == startC) { currentW += calcView.GetColWidth(c) * 0.75; c++; }
                ColBand cb = new ColBand();
                cb.StartCol = startC;
                cb.EndCol = c - 1;
                cb.WidthPt = currentW;
                bands.Add(cb);
            }
            return bands;
        }

        private static List<RowBand> ComputeRowBands(CalcView calcView, uint minR, uint maxR, double usableHeight, bool repeatHeaderRow, uint repeatHeaderRowIdx)
        {
            List<RowBand> bands = new List<RowBand>();
            uint r = minR;
            double headerH = (repeatHeaderRow && repeatHeaderRowIdx >= minR && repeatHeaderRowIdx <= maxR) ? calcView.GetRowHeight(repeatHeaderRowIdx) * 0.75 : 0.0;

            bool isFirst = true;
            while (r <= maxR)
            {
                uint startR = r;
                double currentH = 0.0;
                double limitH = isFirst ? usableHeight : (usableHeight - headerH);

                while (r <= maxR)
                {
                    double rowH = calcView.GetRowHeight(r) * 0.75;
                    if (currentH > 0.0 && currentH + rowH > limitH) break;
                    currentH += rowH;
                    r++;
                }
                if (r == startR) { currentH += calcView.GetRowHeight(r) * 0.75; r++; }
                RowBand rb = new RowBand();
                rb.StartRow = startR;
                rb.EndRow = r - 1;
                rb.HeightPt = currentH;
                rb.IsFirstBand = isFirst;
                bands.Add(rb);
                isFirst = false;
            }
            return bands;
        }
    }
}
