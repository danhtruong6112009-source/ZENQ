# Native Calc PDF Export Implementation Plan & Technical Specification

**Author**: Phase 1 Explorer 2 (Native PDF Export Specialist)  
**Target Environment**: Microsoft Visual C# 5.0 (`csc.exe` / .NET Framework 4.8)  
**Assembly Dependency**: `PdfSharp.dll` (v1.32 / v1.50 standard build included in project root)  
**Affected Files**:
1. `src/format/FormatHandlers.cs` (or `src/format/CalcPdfExporter.cs`)
2. `src/ui/CalcView.cs`
3. `src/ui/AppForm.cs`

---

## 1. Executive Summary & Architectural Overview

ZenQ Calc currently lacks native PDF generation. In `src/ui/AppForm.cs` (line 4209), choosing "Xuất sang tệp PDF chuẩn Adobe (Export to PDF)" while in Calc mode creates a blank PDF document. This plan provides the complete, production-grade implementation strategy for **Native Calc PDF Export** utilizing vector graphics from `PdfSharp.dll`.

### Key Design Pillars
1. **High-Fidelity Vector Rendering**: Rather than rasterizing the entire worksheet to a low-res image, all cell backgrounds, gridlines, custom borders, text, and headers/footers are drawn as genuine PDF vector primitives via `PdfSharp.Drawing.XGraphics`.
2. **True 2D Multi-Page Pagination Banding**: Large worksheets extending both horizontally (e.g. 30 columns) and vertically (e.g. 500 rows) are sliced into 2D grid bands ("Down, then Over") with intelligent greedy packing.
3. **Print Titles & Repeating Headers**: When table data spans multiple vertical pages, the table header row (Row 1 or configured header row) is cleanly repeated at the top of each vertical page.
4. **Complete Cell Styling Fidelity**: Fonts (`Calibri`, `Arial`, `Times New Roman`, etc.), sizes, Bold, Italic, Underline, Strikethrough, cell background colors, foreground text colors, horizontal and vertical alignments, indentation, and custom cell borders (thin, medium, thick, double, diagonal splits).
5. **Floating Objects Integration**: Restored shapes, embedded pictures, charts, and SmartArt diagrams (`CalcView.Shapes` / `CalcView.FloatingObjects`) are translated from sheet pixel coordinates to PDF point coordinates and rendered crisply at their exact anchor positions.
6. **Strict C# 5.0 Compliance**: Zero C# 6+ features (`$"..."`, `?.`, `=>`, local functions, inline out vars, pattern matching).

---

## 2. PdfSharp.dll API Surface & Verification

The project root contains `PdfSharp.dll`, which is explicitly referenced in `build.bat` (`/r:PdfSharp.dll`). Reflection diagnostics confirmed the exact namespace and class structures available:

### 2.1 Core Types & Namespaces
- **Document & Page Management**:
  - `PdfSharp.Pdf.PdfDocument`: Primary document class.
    - `PdfPage AddPage()`: Appends a new page and returns reference.
    - `void Save(string path)`: Serializes document to disk.
    - `void Close()`: Closes document streams.
  - `PdfSharp.Pdf.PdfPage`: Individual page representation.
    - `PdfSharp.PageSize Size`: Enum (`A4`, `A3`, `Letter`, `Legal`, `Tabloid`, `A5`, etc.).
    - `PdfSharp.PageOrientation Orientation`: Enum (`Portrait`, `Landscape`).
    - `XUnit Width`, `XUnit Height`: Page dimensions in points (`page.Width.Point`, `page.Height.Point`).
      - Note: Setting `Orientation = Landscape` automatically inverts `page.Width` and `page.Height` (e.g., A4 becomes $842 \times 595\text{ pt}$).
- **Vector Graphics Context**:
  - `PdfSharp.Drawing.XGraphics`: Vector canvas attached to page.
    - Factory: `XGraphics.FromPdfPage(PdfPage page)`
    - Coordinate system: Origin $(0, 0)$ is **Top-Left**; $X$ increases rightwards; $Y$ increases **downwards** (`PageDirection.Downwards`), matching Windows Forms and GDI+ conventions.
    - Units: Default `XGraphicsUnit.Point` ($1\text{ pt} = 1/72\text{ inch}$).
- **Drawing Primitives**:
  - `void DrawRectangle(XBrush brush, XRect rect)` / `(XPen pen, XBrush brush, XRect rect)`
  - `void DrawLine(XPen pen, double x1, double y1, double x2, double y2)`
  - `void DrawString(string text, XFont font, XBrush brush, XRect layoutRect, XStringFormat format)`
  - `void DrawImage(XImage image, double x, double y, double width, double height)`
- **Color & Style Classes**:
  - `XColor.FromArgb(int argb)` / `XColor.FromArgb(int a, int r, int g, int b)`
  - `XSolidBrush(XColor color)`
  - `XPen(XColor color, double width)`
  - `XFont(string familyName, double emSize, XFontStyle style)`
  - `XFontStyle`: `Regular`, `Bold`, `Italic`, `Underline`, `Strikeout`
  - `XStringFormat`:
    - `Alignment`: `XStringAlignment.Near` (Left), `Center`, `Far` (Right)
    - `LineAlignment`: `XLineAlignment.Near` (Top), `Center` (Middle), `Far` (Bottom)
  - `XImage.FromStream(Stream stream)`: Accepts PNG/JPEG streams directly from in-memory bitmaps.

---

## 3. Mathematical Dimensions & Coordinate Scaling

### 3.1 Screen Pixels to PDF Points
- Standard screen DPI in WinForms GDI+: $96\text{ DPI}$.
- PDF Point density: $72\text{ points per inch}$.
- Scale ratio:
  $$\text{ScaleFactor} = \frac{72}{96} = 0.75$$
- Column width conversion:
  $$\text{WidthPt}(c) = \text{GetColWidth}(c) \times 0.75$$
  (Standard 80 px column $\rightarrow 60.0\text{ pt}$).
- Row height conversion:
  $$\text{HeightPt}(r) = \text{GetRowHeight}(r) \times 0.75$$
  (Standard 24 px row $\rightarrow 18.0\text{ pt}$).

### 3.2 Paper Size Mapping
`CalcView.PaperSize` (string) maps directly to `PdfSharp.PageSize`:
- `"A4"` $\rightarrow$ `PageSize.A4` ($595.28 \times 841.89\text{ pt} = 210 \times 297\text{ mm}$)
- `"A3"` $\rightarrow$ `PageSize.A3` ($841.89 \times 1190.55\text{ pt} = 297 \times 420\text{ mm}$)
- `"Letter"` $\rightarrow$ `PageSize.Letter` ($612.0 \times 792.0\text{ pt} = 8.5 \times 11\text{ in}$)
- `"Legal"` $\rightarrow$ `PageSize.Legal` ($612.0 \times 1008.0\text{ pt} = 8.5 \times 14\text{ in}$)
- `"Tabloid"` / `"Ledger"` $\rightarrow$ `PageSize.Tabloid` ($792.0 \times 1224.0\text{ pt} = 11 \times 17\text{ in}$)
- Default: `PageSize.A4`.

### 3.3 Margin Presets
`CalcView.MarginPreset` (int) maps to printable page margins:
- Preset 0 (`Normal`): Top = 54 pt, Bottom = 54 pt, Left = 54 pt, Right = 54 pt ($0.75\text{ in}$).
- Preset 1 (`Narrow`): Top = 36 pt, Bottom = 36 pt, Left = 27 pt, Right = 27 pt ($0.5\text{ in} / 0.375\text{ in}$).
- Preset 2 (`Moderate`): Top = 54 pt, Bottom = 54 pt, Left = 43.2 pt, Right = 43.2 pt ($0.75\text{ in} / 0.6\text{ in}$).
- Preset 3 (`Wide`): Top = 72 pt, Bottom = 72 pt, Left = 72 pt, Right = 72 pt ($1.0\text{ in}$).

Usable printable dimensions:
$$W_{\text{usable}} = W_{\text{page}} - (M_{\text{left}} + M_{\text{right}})$$
$$H_{\text{usable}} = H_{\text{page}} - (M_{\text{top}} + M_{\text{bottom}})$$

If page headers or footers exist, an additional 18 pt buffer is allocated at the top and bottom.

---

## 4. Printable Area & 2D Pagination Banding

### 4.1 Printable Area Determination
1. **Explicit Print Area**:
   If `calcView.HasPrintArea == true` and `PrintAreaEndRow >= PrintAreaStartRow`:
   $$R_{\min} = \text{PrintAreaStartRow}, \quad R_{\max} = \text{PrintAreaEndRow}$$
   $$C_{\min} = \text{PrintAreaStartCol}, \quad C_{\max} = \text{PrintAreaEndCol}$$
2. **Auto-Calculated Extents**:
   If no print area is defined, scan all non-empty cells in `CurrentSheet`:
   A cell is non-empty if `!string.IsNullOrEmpty(cell.EvaluatedStr) || !string.IsNullOrEmpty(cell.RawValue) || (cell.Format.BgColor != 0 && cell.Format.BgColor != 0xFFFFFFFF) || cell.Format.Borders has borders`.
   Enclosing rectangle: $[R_{\min}, R_{\max}, C_{\min}, C_{\max}]$.
3. **Floating Asset Boundary Expansion**:
   If `calcView.Shapes` contains visible floating objects, their rightmost ($X_{\text{sh}} + W_{\text{sh}}$) and bottommost ($Y_{\text{sh}} + H_{\text{sh}}$) pixel coordinates are translated into corresponding row and column indices and unioned into $[R_{\max}, C_{\max}]$.
4. **Empty Sheet Safeguard**:
   If the sheet is completely empty, default to $R_{\min}=1, R_{\max}=10, C_{\min}=1, C_{\max}=5$.

### 4.2 2D Multi-Page Pagination Banding Algorithm
Spreadsheets can have more columns than fit across a page and more rows than fit down a page.
We decompose the 2D sheet into **Column Bands** and **Row Bands**:

#### A. Horizontal Partitioning (Column Bands)
Contiguous ranges of columns $[C_a .. C_b]$ packed greedily:
```text
Band 0: Columns 1 to 7   (Width: 420 pt <= UsableWidth 487 pt)
Band 1: Columns 8 to 14  (Width: 420 pt <= UsableWidth 487 pt)
Band 2: Columns 15 to 20 (Width: 360 pt <= UsableWidth 487 pt)
```
Algorithm:
```csharp
List<ColBand> colBands = new List<ColBand>();
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
    colBands.Add(cb);
}
```

#### B. Vertical Partitioning (Row Bands) with Print Titles (Repeating Headers)
When `repeatHeaderRow` is enabled (default true, repeating Row 1 or specified header row):
- **Band 0 (Top Page)**: Contains Rows $1 .. R_k$ consuming full $H_{\text{usable}}$.
- **Band 1+ (Subsequent Pages)**: Reserves $H_{\text{header}}$ at the top for repeating Row 1, packing data rows $R_{k+1} .. R_m$ into $H_{\text{usable}} - H_{\text{header}}$.

Algorithm:
```csharp
List<RowBand> rowBands = new List<RowBand>();
uint r = minR;
double headerH = (repeatHeaderRow && repeatHeaderRowIdx >= minR && repeatHeaderRowIdx <= maxR)
    ? calcView.GetRowHeight(repeatHeaderRowIdx) * 0.75 : 0.0;

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
    rowBands.Add(rb);
    isFirst = false;
}
```

#### C. Page Emission Sequence ("Down, then Over")
Total pages generated per sheet: $N_{\text{pages}} = N_{\text{colBands}} \times N_{\text{rowBands}}$.
Order of emission:
1. Column Band 0, Row Band 0 (Page 1: Columns A-G, Rows 1-40)
2. Column Band 0, Row Band 1 (Page 2: Columns A-G, Header Row 1 + Rows 41-80)
3. Column Band 1, Row Band 0 (Page 3: Columns H-N, Rows 1-40)
4. Column Band 1, Row Band 1 (Page 4: Columns H-N, Header Row 1 + Rows 41-80)

This matches standard Excel printing expectations.

---

## 5. Vector Painting Pipeline & Styling Engine

### 5.1 Cell Background Fills
- Check `cell.Format.BgColor`.
- If not white/empty (`BgColor != 0 && BgColor != unchecked((int)0xFFFFFFFF)`):
  - Ensure Alpha channel is present: `if ((bg & 0xFF000000) == 0) bg |= 0xFF000000;`
  - Emit:
    ```csharp
    using (XSolidBrush bgBrush = new XSolidBrush(XColor.FromArgb(bg)))
    {
        gfx.DrawRectangle(bgBrush, cellRect);
    }
    ```

### 5.2 Gridlines
- If `calcView.ShowGridlines == true`:
  - Emit light gray rectangle around each cell:
    ```csharp
    using (XPen gridPen = new XPen(XColor.FromArgb(226, 232, 240), 0.5))
    {
        gfx.DrawRectangle(gridPen, cellRect);
    }
    ```

### 5.3 Custom Cell Borders
`cell.Format.Borders` provides explicit border settings:
- **Thickness Mapping**:
  - `1` (Thin): $0.75\text{ pt}$
  - `2` (Medium): $1.5\text{ pt}$
  - `3` (Thick): $2.25\text{ pt}$
  - `4` (Double): Two parallel $0.6\text{ pt}$ lines separated by $1.0\text{ pt}$.
- **Colors**: Converted from integer ARGB via `XColor.FromArgb`.
- **Top Border**: `gfx.DrawLine(pen, cellRect.Left, cellRect.Top, cellRect.Right, cellRect.Top)`
- **Bottom Border**: `gfx.DrawLine(pen, cellRect.Left, cellRect.Bottom, cellRect.Right, cellRect.Bottom)`
- **Left Border**: `gfx.DrawLine(pen, cellRect.Left, cellRect.Top, cellRect.Left, cellRect.Bottom)`
- **Right Border**: `gfx.DrawLine(pen, cellRect.Right, cellRect.Top, cellRect.Right, cellRect.Bottom)`
- **Diagonal Borders**:
  - `DiagonalDown`: `gfx.DrawLine(pen, cellRect.Left, cellRect.Top, cellRect.Right, cellRect.Bottom)`
  - `DiagonalUp`: `gfx.DrawLine(pen, cellRect.Left, cellRect.Bottom, cellRect.Right, cellRect.Top)`

### 5.4 Typography & Text Rendering
- **Display String**: Extracted from `cell.GetDisplayText()` (evaluates formulas and number formatting).
- **Font Resolution**:
  - Font name: `cell.Format.FontName` (default: `"Calibri"`, with fallback to `"Arial"`).
  - Font size: `cell.Format.FontSize * 0.9` (default $11 \times 0.9 = 9.9\text{ pt}$).
  - Font style: `Bold`, `Italic`, `Underline`, `Strikeout`.
  - Instantiation:
    ```csharp
    XFont font;
    try { font = new XFont(fontName, fontSize, style); }
    catch { font = new XFont("Arial", fontSize, style); }
    ```
- **Text Color**:
  - Converted from `cell.Format.TextColor` (default: `0xFF000000` / Black).
- **Alignment Mapping**:
  - Horizontal:
    - `0` (Left) $\rightarrow$ `XStringAlignment.Near`
    - `1` (Center) $\rightarrow$ `XStringAlignment.Center`
    - `2` (Right) $\rightarrow$ `XStringAlignment.Far`
  - Vertical:
    - `0` (Top) $\rightarrow$ `XLineAlignment.Near`
    - `1` (Middle) $\rightarrow$ `XLineAlignment.Center`
    - `2` (Bottom) $\rightarrow$ `XLineAlignment.Far`
- **Cell Padding & Inset**:
  - Left padding: $3.0\text{ pt} + (\text{Indent} \times 6.0\text{ pt})$.
  - Right padding: $3.0\text{ pt}$.
  - Text bounding box:
    $$\text{TextRect} = [X + \text{Pad}_{\text{left}}, Y, W - \text{Pad}_{\text{left}} - \text{Pad}_{\text{right}}, H]$$
- **Emission**:
  ```csharp
  gfx.DrawString(dispText, font, new XSolidBrush(textColor), textRect, format);
  ```

### 5.5 Merged Cells
- If `cell.Format.IsMergedChild`:
  The cell is completely skipped during individual cell rendering.
- If `cell.Format.IsMergedParent`:
  - Calculate combined width by summing widths of all spanned columns within current column band:
    $$W_{\text{merged}} = \sum_{c=C}^{\min(C + \text{ColSpan} - 1, C_{\text{bandEnd}})} \text{GetColWidth}(c) \times 0.75$$
  - Calculate combined height by summing heights of spanned rows:
    $$H_{\text{merged}} = \sum_{r=R}^{\min(R + \text{RowSpan} - 1, R_{\text{bandEnd}})} \text{GetRowHeight}(r) \times 0.75$$
  - Render background, borders, and text spanning the entire $W_{\text{merged}} \times H_{\text{merged}}$ box.

---

## 6. Floating Visual Assets (Shapes, Images, Charts)

Spreadsheets often include floating pictures, corporate logos, shape callouts, and data charts.
In `CalcView`, all visual assets are stored in `List<DocShapeObject> Shapes` (aliased as `FloatingObjects`).

### Coordinate Translation
Each shape has:
- `shape.PageX`, `shape.PageY`: Absolute worksheet pixel coordinates.
- `shape.BaseWidth + shape.WidthDelta`: Width in pixels.
- `shape.BaseHeight + shape.HeightDelta`: Height in pixels.

For a page band covering columns $[C_{\text{start}} .. C_{\text{end}}]$ and rows $[R_{\text{start}} .. R_{\text{end}}]$:
$$\text{BandPxLeft} = \sum_{c=1}^{C_{\text{start}}-1} \text{GetColWidth}(c), \quad \text{BandPxWidth} = \sum_{c=C_{\text{start}}}^{C_{\text{end}}} \text{GetColWidth}(c)$$
$$\text{BandPxTop} = \sum_{r=1}^{R_{\text{start}}-1} \text{GetRowHeight}(r), \quad \text{BandPxHeight} = \sum_{r=R_{\text{start}}}^{R_{\text{end}}} \text{GetRowHeight}(r)$$

If the shape rectangle $[X_{\text{sh}}, Y_{\text{sh}}, W_{\text{sh}}, H_{\text{sh}}]$ intersects the band bounding box:
$$\text{PdfX} = M_{\text{left}} + (X_{\text{sh}} - \text{BandPxLeft}) \times 0.75$$
$$\text{PdfY} = M_{\text{top}} + H_{\text{headerOffset}} + (Y_{\text{sh}} - \text{BandPxTop}) \times 0.75$$
$$\text{PdfW} = W_{\text{sh}} \times 0.75, \quad \text{PdfH} = H_{\text{sh}} \times 0.75$$

### Image & Chart Rendering via In-Memory Stream
1. **Pictures (`ShapeType == "Image"`)**:
   If `shape.CachedImage` exists (or `File.Exists(shape.ImagePath)`), serialize the image to a PNG `MemoryStream`, create `XImage.FromStream(ms)`, and call `gfx.DrawImage(ximg, PdfX, PdfY, PdfW, PdfH)`.
2. **Vector Shapes, Charts & SmartArt (`ShapeType == "Chart"`, etc.)**:
   Render the shape using `ShapeRenderer.DrawShape` onto a high-resolution $2\times$ GDI+ Bitmap ($192\text{ DPI}$), serialize to PNG `MemoryStream`, and draw via `gfx.DrawImage`. This ensures 100% visual fidelity for all chart types, series markers, and geometry without code duplication.

---

## 7. Headers, Footers & Dynamic Page Numbering

On each generated PDF page:
- **Top Header**:
  - `HeaderLeft` drawn at $(M_{\text{left}}, M_{\text{top}} - 16\text{ pt})$ with `XStringAlignment.Near`.
  - `HeaderCenter` drawn centered between margins with `XStringAlignment.Center`.
  - `HeaderRight` drawn at $(W_{\text{page}} - M_{\text{right}}, M_{\text{top}} - 16\text{ pt})$ with `XStringAlignment.Far`.
- **Bottom Footer**:
  - `FooterLeft` drawn at $(M_{\text{left}}, H_{\text{page}} - M_{\text{bottom}} + 12\text{ pt})$.
  - `FooterCenter`: If specified, draws center text. If empty, automatically formats:
    $$\text{"Trang } P \text{ / } N_{\text{total}}\text{" (or "Page } P \text{ of } N_{\text{total}}\text{")}$$
  - `FooterRight` drawn at $(W_{\text{page}} - M_{\text{right}}, H_{\text{page}} - M_{\text{bottom}} + 12\text{ pt})$.
- Header/Footer typography: $9.0\text{ pt}$ `Segoe UI` in `DimGray` ($0xFF696969$).

---

## 8. Authoritative C# 5 Implementation Code

Below is the complete, drop-in code for `CalcPdfExporter.cs` in `src/format/`, along with the method hooks in `CalcView.cs` and `AppForm.cs`.

### 8.1 File: `src/format/CalcPdfExporter.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Format
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
        public static bool Export(CalcView calcView, string filePath, bool exportAllSheets = false)
        {
            CalcPdfOptions options = new CalcPdfOptions();
            options.ExportAllSheets = exportAllSheets;
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

            return ExportWithOptions(calcView, filePath, options);
        }

        public static bool ExportWithOptions(CalcView calcView, string filePath, CalcPdfOptions options)
        {
            if (calcView == null || string.IsNullOrEmpty(filePath)) return false;
            if (options == null) options = new CalcPdfOptions();

            try
            {
                using (PdfDocument document = new PdfDocument())
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
                    using (XSolidBrush bgBrush = new XSolidBrush(XColor.FromArgb(bg)))
                    {
                        gfx.DrawRectangle(bgBrush, cellRect);
                    }
                }

                // 2. Gridline
                if (showGridlines)
                {
                    using (XPen gridPen = new XPen(XColor.FromArgb(226, 232, 240), 0.5))
                    {
                        gfx.DrawRectangle(gridPen, cellRect);
                    }
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
                        using (XPen pen = new XPen(ResolveXColor(dCol, 0xFF1E293B), b.DiagonalThickness > 0 ? b.DiagonalThickness * 0.75 : 0.75))
                        {
                            gfx.DrawLine(pen, cellRect.Left, cellRect.Top, cellRect.Right, cellRect.Bottom);
                        }
                    }
                    if (b.DiagonalUp)
                    {
                        int dCol = b.DiagonalColor != 0 ? b.DiagonalColor : b.Color;
                        using (XPen pen = new XPen(ResolveXColor(dCol, 0xFF1E293B), b.DiagonalThickness > 0 ? b.DiagonalThickness * 0.75 : 0.75))
                        {
                            gfx.DrawLine(pen, cellRect.Left, cellRect.Bottom, cellRect.Right, cellRect.Top);
                        }
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
                using (XPen p1 = new XPen(col, 0.6))
                {
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
            }
            else
            {
                using (XPen p = new XPen(col, widthPt))
                {
                    gfx.DrawLine(p, x1, y1, x2, y2);
                }
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
                        img = Image.FromFile(sh.ImagePath);
                        disposeImg = true;
                    }
                    if (img != null)
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
            using (XFont hFont = new XFont("Segoe UI", 9.0, XFontStyle.Regular))
            using (XSolidBrush hBrush = new XSolidBrush(XColor.FromArgb(100, 116, 139)))
            {
                // Headers
                double hY = options.MarginTopPt - 16.0;
                if (!string.IsNullOrEmpty(options.HeaderLeft))
                {
                    gfx.DrawString(options.HeaderLeft, hFont, hBrush, new XPoint(options.MarginLeftPt, hY), XStringFormats.BaseLineLeft);
                }
                if (!string.IsNullOrEmpty(options.HeaderCenter))
                {
                    gfx.DrawString(options.HeaderCenter, hFont, hBrush, new XPoint(pageWidth / 2.0, hY), XStringFormats.BaseLineCenter);
                }
                if (!string.IsNullOrEmpty(options.HeaderRight))
                {
                    gfx.DrawString(options.HeaderRight, hFont, hBrush, new XPoint(pageWidth - options.MarginRightPt, hY), XStringFormats.BaseLineRight);
                }

                // Footers
                double fY = pageHeight - options.MarginBottomPt + 14.0;
                if (!string.IsNullOrEmpty(options.FooterLeft))
                {
                    gfx.DrawString(options.FooterLeft, hFont, hBrush, new XPoint(options.MarginLeftPt, fY), XStringFormats.BaseLineLeft);
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
                gfx.DrawString(fCenter, hFont, hBrush, new XPoint(pageWidth / 2.0, fY), XStringFormats.BaseLineCenter);

                if (!string.IsNullOrEmpty(options.FooterRight))
                {
                    gfx.DrawString(options.FooterRight, hFont, hBrush, new XPoint(pageWidth - options.MarginRightPt, fY), XStringFormats.BaseLineRight);
                }
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
```

---

### 8.2 File: `src/ui/CalcView.cs` Method Addition

Append to `CalcView` class in `src/ui/CalcView.cs`:

```csharp
        /// <summary>
        /// Native vector PDF export honoring orientation, paper size, margins, gridlines, fonts, styling, 2D pagination, and floating objects.
        /// </summary>
        public bool ExportToPdf(string filePath, bool exportAllSheets = false)
        {
            return GenOffice.Format.CalcPdfExporter.Export(this, filePath, exportAllSheets);
        }
```

---

### 8.3 File: `src/ui/AppForm.cs` Modification

In `src/ui/AppForm.cs`, within `PerformExportPdf()` (around line 4200):

**Before:**
```csharp
                    bool ok = false;
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        ok = WriterView.ExportToPdf(sfd.FileName, Ribbon);
                    }
                    else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null && PdfView.Document != null)
                    {
                        ok = GenOffice.Format.PdfHandler.SavePdf(PdfView.Document, sfd.FileName);
                    }
                    else
                    {
                        var blankDoc = new GenOffice.Core.PdfDocument(System.IO.Path.GetFileName(sfd.FileName));
                        ok = GenOffice.Format.PdfHandler.SavePdf(blankDoc, sfd.FileName);
                    }
```

**After:**
```csharp
                    bool ok = false;
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        ok = WriterView.ExportToPdf(sfd.FileName, Ribbon);
                    }
                    else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                    {
                        ok = CalcView.ExportToPdf(sfd.FileName);
                    }
                    else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null && PdfView.Document != null)
                    {
                        ok = GenOffice.Format.PdfHandler.SavePdf(PdfView.Document, sfd.FileName);
                    }
                    else
                    {
                        var blankDoc = new GenOffice.Core.PdfDocument(System.IO.Path.GetFileName(sfd.FileName));
                        ok = GenOffice.Format.PdfHandler.SavePdf(blankDoc, sfd.FileName);
                    }
```

---

## 9. Verification & Acceptance Criteria

### Verification Commands
1. **Build Validation**:
   ```cmd
   cmd /c build.bat
   ```
   Must output: `[SUCCESS] GenOffice.exe built successfully!` with 0 compilation errors.
2. **Automated Headless Export Verification**:
   Execute automated test harness loading sample spreadsheet with:
   - Orientation: Landscape
   - Paper Size: A4
   - 25 columns $\times$ 100 rows
   - Repeating Header Row (Row 1)
   - Mixed formatting: Bold headers, currency formatting, custom cell fill colors, thin/medium/double borders
   - 1 embedded picture, 1 vector shape, 1 chart
   - Verify output `.pdf` is valid, has $> 1$ pages, non-zero file size ($> 15\text{ KB}$), and loads in `PdfiumViewer`.
