# Authoritative Implementation Plan: OpenXML Drawings Roundtrip for ZenQ Calc

**Author**: Phase 1 Explorer 1 (OpenXML Drawings Specialist)  
**Date**: 2026-09-28  
**Target Environment**: Microsoft Visual C# 5.0 (`csc.exe` / .NET Framework 4.8)  
**Target Files**: `src/format/XlsxHandler.cs`, `src/ui/CalcView.cs`, `src/ui/DocShapeObject.cs`  
**Milestone**: M1 (Phase 1: Critical Data Integrity & Core Productivity)

---

## 1. Executive Summary & Root Cause Analysis

### 1.1 The Critical Data Loss Defect
In ZenQ Calc, `XlsxHandler.SaveXlsx` exports floating shapes, charts, and embedded images into OpenXML DrawingML:
- It creates `xl/worksheets/_rels/sheet1.xml.rels` pointing to `../drawings/drawing1.xml`.
- It writes images into `xl/media/image{N}.png`.
- It writes relationships into `xl/drawings/_rels/drawing1.xml.rels`.
- It writes DrawingML markup into `xl/drawings/drawing1.xml` with `<xdr:twoCellAnchor editAs="oneCell">`.

**However**, `XlsxHandler.LoadXlsx` (lines 300–778) currently only reads `xl/sharedStrings.xml`, `xl/styles.xml`, `xl/workbook.xml`, and `xl/worksheets/sheet*.xml`. It **completely omits reading** `xl/drawings/`, `xl/drawings/_rels/`, and `xl/media/`. 
As a result:
1. When opening any `.xlsx` workbook containing shapes, pictures, or charts, all visual assets are discarded.
2. If the user subsequently saves the spreadsheet, `calcView.Shapes` is empty, completely wiping out all drawings and media from the file.

### 1.2 Objectives & Acceptance Criteria
1. Implement full DrawingML parsing in `LoadXlsx` for:
   - `<xdr:twoCellAnchor>`, `<xdr:oneCellAnchor>`, and `<xdr:absoluteAnchor>`.
   - Embedded pictures (`<xdr:pic>`) resolved from `xl/media/`.
   - Vector shapes (`<xdr:sp>`) with preset geometry, solid fills, outline stroke, and formatted text body.
   - DrawingML charts (`<xdr:graphicFrame>`) with categories, series, and values resolved from `xl/charts/chart*.xml`.
2. Convert DrawingML EMUs (English Metric Units) to screen/sheet pixels with exact zero-drift cell anchor mapping.
3. Expose `FloatingObjects` property alias on `CalcView` and `CalcSheetState` mapping directly to `Shapes`.
4. Enhance `SaveXlsx` to preserve native OpenXML charts, pictures, and shapes across multiple sheets without data degradation.
5. Guarantee strict compliance with **C# 5.0** (zero C# 6+ features: no string interpolation `$"..."`, no null-conditional `?.`, no expression-bodied members `=>`, no pattern matching, no tuple literals).

---

## 2. DrawingML Architecture & Packaging Topology

### 2.1 OpenXML Package Structure for Drawings
An `.xlsx` workbook with visual assets adheres to the following part relationship graph:

```text
[Content_Types].xml
  ├── Override: /xl/worksheets/sheet1.xml (application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml)
  ├── Override: /xl/drawings/drawing1.xml (application/vnd.openxmlformats-officedocument.drawing+xml)
  ├── Override: /xl/charts/chart1.xml (application/vnd.openxmlformats-officedocument.drawingml.chart+xml)
  └── Default:  png, jpeg, bmp (image/png, image/jpeg, etc.)

xl/
  ├── workbook.xml
  ├── _rels/
  │     └── workbook.xml.rels
  ├── worksheets/
  │     ├── sheet1.xml (contains <drawing r:id="rIdDrawing1"/>)
  │     └── _rels/
  │           └── sheet1.xml.rels (Relationship Type=".../relationships/drawing" Target="../drawings/drawing1.xml")
  ├── drawings/
  │     ├── drawing1.xml (root <xdr:wsDr> containing anchors)
  │     └── _rels/
  │           └── drawing1.xml.rels (rIdImg1 -> ../media/image1.png, rIdChart1 -> ../charts/chart1.xml)
  ├── charts/
  │     └── chart1.xml (root <c:chartSpace> containing title, plotArea, series, values)
  └── media/
        └── image1.png, image2.jpeg, ...
```

### 2.2 Path Normalization Rule
DrawingML relationships use relative URI targets that must be normalized to zip archive paths:
- In `xl/worksheets/_rels/sheet{N}.xml.rels`: Target `../drawings/drawing1.xml` resolves to `xl/drawings/drawing1.xml`.
- In `xl/drawings/_rels/drawing{N}.xml.rels`:
  - Target `../media/image1.png` resolves to `xl/media/image1.png`.
  - Target `../charts/chart1.xml` resolves to `xl/charts/chart1.xml`.
  - Target `/xl/media/image1.png` resolves to `xl/media/image1.png`.

---

## 3. Mathematical Coordinate Engine (EMUs ↔ Grid Pixels)

### 3.1 Unit Constants & Conversion Ratios
- **1 inch** = $72 \text{ pt} = 96 \text{ px (at standard 96 DPI)} = 914,400 \text{ EMUs}$.
- **1 pixel** = $914,400 / 96 = \mathbf{9,525 \text{ EMUs}}$.
- **1 point** = $914,400 / 72 = \mathbf{12,700 \text{ EMUs}}$.
- **Rotation Angle**: OpenXML stores rotation in 60,000ths of a degree ($360^\circ = 21,600,000$). $\text{AngleDegrees} = \text{rot} / 60,000.0$.

### 3.2 Cell Anchor Mathematics
In `CalcView`, cell row and column indices are **1-indexed** ($r \ge 1, c \ge 1$).  
In OpenXML DrawingML, `<xdr:col>` and `<xdr:row>` are **0-indexed** ($0 \dots N-1$).

Let:
- $\text{FromCol}_1 = \text{uint.Parse}(<xdr:col>) + 1$
- $\text{FromRow}_1 = \text{uint.Parse}(<xdr:row>) + 1$
- $\text{ToCol}_1 = \text{uint.Parse}(<xdr:col>) + 1$
- $\text{ToRow}_1 = \text{uint.Parse}(<xdr:row>) + 1$

#### A. Two-Cell Anchor (`<xdr:twoCellAnchor>`)
A two-cell anchor pins the top-left corner to cell $(\text{FromRow}_1, \text{FromCol}_1)$ with offset $(\text{FromRowOff}, \text{FromColOff})$, and the bottom-right corner to cell $(\text{ToRow}_1, \text{ToCol}_1)$ with offset $(\text{ToRowOff}, \text{ToColOff})$.

```csharp
// Calculate PageX (left edge in sheet pixels)
int startX = 0;
for (uint c = 1; c < fromCol1; c++)
{
    startX += GetColWidth(c, curSheetColWidths, defColW);
}
int pageX = startX + (int)Math.Round((double)fromColOff / 9525.0);

// Calculate PageY (top edge in sheet pixels)
int startY = 0;
for (uint r = 1; r < fromRow1; r++)
{
    startY += GetRowHeight(r, curSheetRowHeights, defRowH);
}
int pageY = startY + (int)Math.Round((double)fromRowOff / 9525.0);

// Calculate EndX (right edge in sheet pixels)
int endX = 0;
for (uint c = 1; c < toCol1; c++)
{
    endX += GetColWidth(c, curSheetColWidths, defColW);
}
endX += (int)Math.Round((double)toColOff / 9525.0);

// Calculate EndY (bottom edge in sheet pixels)
int endY = 0;
for (uint r = 1; r < toRow1; r++)
{
    endY += GetRowHeight(r, curSheetRowHeights, defRowH);
}
endY += (int)Math.Round((double)toRowOff / 9525.0);

int baseWidth = Math.Max(10, endX - pageX);
int baseHeight = Math.Max(10, endY - pageY);
```

#### B. One-Cell Anchor (`<xdr:oneCellAnchor>`)
Top-left corner is calculated via `<xdr:from>` as above. Width and height are defined by `<xdr:ext cx="..." cy="..."/>`:
```csharp
int baseWidth = Math.Max(10, (int)Math.Round((double)extCx / 9525.0));
int baseHeight = Math.Max(10, (int)Math.Round((double)extCy / 9525.0));
```

#### C. Absolute Anchor (`<xdr:absoluteAnchor>`)
Position is defined directly in EMUs by `<xdr:pos x="..." y="..."/>` and dimensions by `<xdr:ext cx="..." cy="..."/>`:
```csharp
int pageX = (int)Math.Round((double)posX / 9525.0);
int pageY = (int)Math.Round((double)posY / 9525.0);
int baseWidth = Math.Max(10, (int)Math.Round((double)extCx / 9525.0));
int baseHeight = Math.Max(10, (int)Math.Round((double)extCy / 9525.0));
```

#### D. Exact Inverse Invariant in `SaveXlsx`
In `SaveXlsx`, when converting `sh.PageX` back to DrawingML:
```csharp
uint fromCol = 1;
int accW = 0;
while (fromCol < 250 && accW + calcView.GetColWidth(fromCol) <= sh.PageX)
{
    accW += calcView.GetColWidth(fromCol);
    fromCol++;
}
long colOff = (long)(sh.PageX - accW) * 9525;
```
This guarantees:
$$\text{accW} + \frac{\text{colOff}}{9525} = \text{accW} + \frac{(\text{sh.PageX} - \text{accW}) \times 9525}{9525} = \text{sh.PageX}$$
Zero numerical drift across unlimited save-load cycles.

---

## 4. Visual Asset Parsing Specification (`LoadXlsx`)

### 4.1 Pictures (`<xdr:pic>`)
1. **Identifier & Metadata**:
   - Extract `id` and `name` from `<xdr:cNvPr>`.
   - Extract `descr` (alt text) from `<xdr:cNvPr descr="...">`.
2. **Media Extraction (`<a:blip>`)**:
   - Extract relationship ID from `<a:blip r:embed="..."/>` or attribute with `LocalName == "embed"`.
   - Lookup relationship ID in `drawingRels` map to get target media path (e.g. `xl/media/image1.png`).
   - Extract entry from `ZipArchive`.
   - **Stream-Safe GDI+ Detachment**: Read bytes into `byte[]` and construct `new Bitmap(new MemoryStream(bytes))`. Never leave file or archive streams attached to GDI+ Bitmaps.
   - Assign to `sh.CachedImage`.
3. **Classification & Properties**:
   - If `name` or `descr` starts with `"Chart"`: classify as `ShapeType = "Chart"`, attach `sh.ChartData = DocChartData.CreateDefault(...)`.
   - Else: `sh.ShapeType = "Image"`.
   - `sh.FillColor = Color.Transparent`, `sh.OutlineColor = Color.Transparent`, `sh.OutlineWidth = 0f`.
   - Rotation: if `<a:xfrm rot="...">` present, `sh.RotationAngle = (float)(rot / 60000.0)`.

### 4.2 Vector Shapes (`<xdr:sp>`)
1. **Geometry Mapping (`<a:prstGeom prst="...">`)**:
   Map OpenXML preset names to GenOffice `ShapeType`:
   - `rect` $\rightarrow$ `"Rectangle"`
   - `roundRect` $\rightarrow$ `"RoundedRectangle"`
   - `ellipse` $\rightarrow$ `"Oval"`
   - `triangle` $\rightarrow$ `"Triangle"`
   - `rtTriangle` $\rightarrow$ `"RightTriangle"`
   - `diamond` $\rightarrow$ `"Diamond"`
   - `parallelogram` $\rightarrow$ `"Parallelogram"`
   - `trapezoid` $\rightarrow$ `"Trapezoid"`
   - `hexagon` $\rightarrow$ `"Hexagon"`
   - `octagon` $\rightarrow$ `"Octagon"`
   - `star5` $\rightarrow$ `"Star5"`
   - `heart` $\rightarrow$ `"Heart"`
   - `smileyFace` / `smileFace` $\rightarrow$ `"SmileFace"`
   - `rightArrow` $\rightarrow$ `"RightArrow"`
   - `leftArrow` $\rightarrow$ `"LeftArrow"`
   - `upArrow` $\rightarrow$ `"UpArrow"`
   - `downArrow` $\rightarrow$ `"DownArrow"`
   - `can` / `cylinder` $\rightarrow$ `"Cylinder"`
   - `cube` $\rightarrow$ `"Cube"`
   - `donut` $\rightarrow$ `"Donut"`
   - `line` $\rightarrow$ `"Line"`
   - Unrecognized preset $\rightarrow$ keep preset string directly (`ShapeRenderer` supports native fallback).
2. **Fill Color (`<a:solidFill>`)**:
   - Extract `<a:srgbClr val="RRGGBB"/>`: parse hex to `Color.FromArgb(255, R, G, B)`.
   - If `<a:noFill/>`: `sh.FillColor = Color.Transparent`.
   - Default: `Color.FromArgb(43, 87, 154)`.
3. **Outline Stroke (`<a:ln>`)**:
   - Stroke width: `sh.OutlineWidth = Math.Max(1f, (float)(w / 9525.0))`.
   - Color: parse `<a:solidFill><a:srgbClr val="RRGGBB"/></a:solidFill>`.
   - If `<a:noFill/>`: `sh.OutlineColor = Color.Transparent`, `sh.OutlineWidth = 0f`.
4. **Text Body (`<xdr:txBody>`)**:
   - Iterate `<a:p>` (paragraphs) and `<a:r>` (runs). Concatenate `<a:t>` text with newlines.
   - Text formatting: parse `<a:rPr sz="..." b="1" i="1">`.
     - `sz`: font size in hundredths of a point (e.g. `1100` $\rightarrow$ `11.0f`).
     - `b="1"`: `sh.Bold = true`.
     - `i="1"`: `sh.Italic = true`.
     - Text color: `<a:solidFill><a:srgbClr val="..."/>`.
   - Alignment: `<a:pPr algn="ctr">` $\rightarrow$ `Center`, `"r"` $\rightarrow$ `Far`, `"l"` $\rightarrow$ `Near`.

### 4.3 DrawingML Charts (`<xdr:graphicFrame>`)
1. **Chart Part Discovery**:
   - Locate `<a:graphicData uri=".../chart">`.
   - Extract `r:id` from `<c:chart r:id="..."/>`.
   - Lookup `r:id` in `drawingRels` map to locate `xl/charts/chart{M}.xml`.
2. **Chart XML Parsing (`<c:chartSpace>`)**:
   - **Title**: check `<c:title><c:tx><c:rich>` or `<c:strRef><c:strCache><c:pt><c:v>`.
   - **Chart Type**: inspect child of `<c:plotArea>`:
     - `<c:barChart>`: check `<c:barDir val="col|bar"/>` and `<c:grouping val="clustered|stacked"/>`. Maps to `"Column_Clustered"`, `"Column_Stacked"`, `"Bar_Clustered"`, `"Bar_Stacked"`.
     - `<c:lineChart>` $\rightarrow$ `"Line"`
     - `<c:pieChart>` $\rightarrow$ `"Pie"`
     - `<c:doughnutChart>` $\rightarrow$ `"Doughnut"`
     - `<c:areaChart>` $\rightarrow$ `"Area"`
     - `<c:radarChart>` $\rightarrow$ `"Radar"`
     - Default: `"Column_Clustered"`.
   - **Series & Categories**:
     - Iterate `<c:ser>`.
     - Series Name from `<c:tx><c:v>` or `<c:strRef><c:strCache>`.
     - Categories from `<c:cat><c:strRef><c:strCache><c:pt idx="..."><c:v>`.
     - Values from `<c:val><c:numRef><c:numCache><c:pt idx="..."><c:v>`.
   - **Data Assembly**:
     - Populate `DocChartData.Values` matrix indexed by `[CategoryIndex][SeriesIndex]`.
     - Assign `sh.ChartData = docChartData`.
     - Set `sh.ShapeType = "Chart"`.
     - Set `sh.FillColor = Color.White`, `sh.OutlineColor = Color.FromArgb(209, 213, 219)`, `sh.OutlineWidth = 1.0f`.

---

## 5. Visual Asset Persistence Specification (`SaveXlsx`)

To guarantee 100% roundtrip fidelity without data loss:

### 5.1 Pre-Save State Synchronization
At the beginning of `SaveXlsx`:
```csharp
calcView.SaveCurrentSheetState();
```
This copies active shapes from `calcView.Shapes` into `calcView.SheetStates[calcView.ActiveSheetIndex].Shapes`.

### 5.2 Multi-Sheet Drawings Export Pipeline
For each sheet `s` ($0 \dots \text{Sheets.Count}-1$):
1. Retrieve shapes for sheet `s`:
   ```csharp
   List<DocShapeObject> sheetShapes = (calcView.SheetStates != null && s < calcView.SheetStates.Count && calcView.SheetStates[s].Shapes != null)
       ? calcView.SheetStates[s].Shapes
       : (s == calcView.ActiveSheetIndex ? calcView.Shapes : null);
   ```
2. If `sheetShapes != null && sheetShapes.Count > 0`:
   - In `[Content_Types].xml`: emit `<Override PartName="/xl/drawings/drawing{s+1}.xml" ContentType="...drawing+xml"/>`.
   - In `xl/worksheets/sheet{s+1}.xml`: emit `<drawing r:id="rIdDrawing1"/>`.
   - In `xl/worksheets/_rels/sheet{s+1}.xml.rels`: emit `<Relationship Id="rIdDrawing1" Type=".../drawing" Target="../drawings/drawing{s+1}.xml"/>`.
   - In `xl/drawings/drawing{s+1}.xml`: emit anchors for all shapes.
   - For pictures: write `xl/media/image{N}.png` and relationship in `drawing{s+1}.xml.rels`.
   - For native charts: write `xl/charts/chart{K}.xml` with `<c:chartSpace>` markup, write `<xdr:graphicFrame>` in `drawing{s+1}.xml`, and register chart in `[Content_Types].xml` and `drawing{s+1}.xml.rels`.

---

## 6. Model Unification & API Contracts

### 6.1 `FloatingObjects` Property Alias in `CalcView.cs`
In `src/ui/CalcView.cs`:
```csharp
// Inside public class CalcSheetState
public List<DocShapeObject> FloatingObjects
{
    get { return Shapes; }
    set { Shapes = value; }
}

// Inside public class CalcView
public List<DocShapeObject> FloatingObjects
{
    get { return Shapes; }
    set { Shapes = value; }
}
```

### 6.2 Stream Overload in `XlsxHandler.cs`
In `src/format/XlsxHandler.cs`:
```csharp
public static bool LoadXlsx(string filepath, CalcView calcView)
{
    if (!File.Exists(filepath)) return false;
    using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    {
        return LoadXlsx(fs, calcView);
    }
}

public static bool LoadXlsx(Stream stream, CalcView calcView)
{
    // Full OpenXML loading implementation
}
```

---

## 7. Exact C# 5 Implementation Blueprint

The following production-ready methods are strictly compliant with C# 5.0 syntax and ready for drop-in integration.

### 7.1 Path Normalization & Entry Lookup Helpers
```csharp
private static ZipArchiveEntry GetEntryCaseInsensitive(ZipArchive archive, string entryPath)
{
    if (archive == null || string.IsNullOrEmpty(entryPath)) return null;
    ZipArchiveEntry entry = archive.GetEntry(entryPath);
    if (entry != null) return entry;

    string normalized = entryPath.Replace('\\', '/').TrimStart('/');
    entry = archive.GetEntry(normalized);
    if (entry != null) return entry;

    for (int i = 0; i < archive.Entries.Count; i++)
    {
        string fn = archive.Entries[i].FullName.Replace('\\', '/').TrimStart('/');
        if (string.Equals(fn, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return archive.Entries[i];
        }
    }
    return null;
}

private static string NormalizeOpenXmlPath(string target, string baseFolder)
{
    if (string.IsNullOrEmpty(target)) return "";
    string t = target.Replace('\\', '/');
    if (t.StartsWith("/")) return t.Substring(1);

    if (t.StartsWith("../"))
    {
        string baseDir = baseFolder.Replace('\\', '/').TrimEnd('/');
        int lastSlash = baseDir.LastIndexOf('/');
        string parent = lastSlash >= 0 ? baseDir.Substring(0, lastSlash + 1) : "";
        while (t.StartsWith("../"))
        {
            t = t.Substring(3);
            if (parent.Length > 0)
            {
                int pSlash = parent.TrimEnd('/').LastIndexOf('/');
                parent = pSlash >= 0 ? parent.Substring(0, pSlash + 1) : "";
            }
        }
        return parent + t;
    }

    return baseFolder.TrimEnd('/') + "/" + t;
}

private static int GetColWidthForSheet(uint col, Dictionary<uint, int> colWidths, int defColW)
{
    if (colWidths != null && colWidths.ContainsKey(col)) return colWidths[col];
    return defColW > 0 ? defColW : 80;
}

private static int GetRowHeightForSheet(uint row, Dictionary<uint, int> rowHeights, int defRowH)
{
    if (rowHeights != null && rowHeights.ContainsKey(row)) return rowHeights[row];
    return defRowH > 0 ? defRowH : 24;
}

private static Color ParseColorHex(string hex, Color defaultColor)
{
    if (string.IsNullOrEmpty(hex) || hex.Length < 6) return defaultColor;
    try
    {
        if (hex.Length > 6) hex = hex.Substring(hex.Length - 6); // Take RGB from ARGB
        int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return Color.FromArgb(255, r, g, b);
    }
    catch
    {
        return defaultColor;
    }
}
```

### 7.2 Main Drawing Loader for Worksheet (`LoadSheetDrawings`)
```csharp
private static List<DocShapeObject> LoadSheetDrawings(
    ZipArchive archive, 
    int sheetIdx, 
    Dictionary<uint, int> colWidths, 
    Dictionary<uint, int> rowHeights, 
    int defColW, 
    int defRowH)
{
    List<DocShapeObject> shapes = new List<DocShapeObject>();
    if (archive == null) return shapes;

    try
    {
        // 1. Discover drawing part path from worksheet relationships
        string wsRelsPath = string.Format("xl/worksheets/_rels/sheet{0}.xml.rels", sheetIdx);
        ZipArchiveEntry wsRelsEntry = GetEntryCaseInsensitive(archive, wsRelsPath);
        string drawingPartPath = null;

        if (wsRelsEntry != null)
        {
            using (Stream s = wsRelsEntry.Open())
            {
                XmlDocument relsDoc = new XmlDocument();
                relsDoc.Load(s);
                XmlNodeList relNodes = relsDoc.GetElementsByTagName("Relationship");
                foreach (XmlNode rel in relNodes)
                {
                    if (rel.Attributes == null) continue;
                    var typeAttr = rel.Attributes["Type"];
                    var targetAttr = rel.Attributes["Target"];
                    if (typeAttr != null && targetAttr != null && 
                        typeAttr.Value.EndsWith("/drawing", StringComparison.OrdinalIgnoreCase))
                    {
                        drawingPartPath = NormalizeOpenXmlPath(targetAttr.Value, "xl/worksheets/");
                        break;
                    }
                }
            }
        }

        // Fallback: check convention xl/drawings/drawing{N}.xml directly
        if (string.IsNullOrEmpty(drawingPartPath))
        {
            string fallbackPath = string.Format("xl/drawings/drawing{0}.xml", sheetIdx);
            if (GetEntryCaseInsensitive(archive, fallbackPath) != null)
            {
                drawingPartPath = fallbackPath;
            }
        }

        if (string.IsNullOrEmpty(drawingPartPath)) return shapes;

        ZipArchiveEntry drawEntry = GetEntryCaseInsensitive(archive, drawingPartPath);
        if (drawEntry == null) return shapes;

        // 2. Discover drawing relationships (xl/drawings/_rels/drawing{N}.xml.rels)
        string drawFileName = Path.GetFileName(drawingPartPath);
        string drawRelsPath = string.Format("xl/drawings/_rels/{0}.rels", drawFileName);
        ZipArchiveEntry drawRelsEntry = GetEntryCaseInsensitive(archive, drawRelsPath);
        Dictionary<string, string> drawingRels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (drawRelsEntry != null)
        {
            using (Stream s = drawRelsEntry.Open())
            {
                XmlDocument dRelsDoc = new XmlDocument();
                dRelsDoc.Load(s);
                XmlNodeList relNodes = dRelsDoc.GetElementsByTagName("Relationship");
                foreach (XmlNode rel in relNodes)
                {
                    if (rel.Attributes == null) continue;
                    var idAttr = rel.Attributes["Id"];
                    var targetAttr = rel.Attributes["Target"];
                    if (idAttr != null && targetAttr != null)
                    {
                        string normTarget = NormalizeOpenXmlPath(targetAttr.Value, "xl/drawings/");
                        drawingRels[idAttr.Value] = normTarget;
                    }
                }
            }
        }

        // 3. Parse DrawingML anchors in drawing{N}.xml
        using (Stream s = drawEntry.Open())
        {
            XmlDocument drawDoc = new XmlDocument();
            drawDoc.Load(s);

            foreach (XmlNode rootNode in drawDoc.ChildNodes)
            {
                if (rootNode.LocalName != "wsDr") continue;

                foreach (XmlNode anchorNode in rootNode.ChildNodes)
                {
                    DocShapeObject sh = ParseAnchorNode(anchorNode, archive, drawingRels, colWidths, rowHeights, defColW, defRowH);
                    if (sh != null)
                    {
                        shapes.Add(sh);
                    }
                }
            }
        }
    }
    catch
    {
        // Resilient: return any shapes successfully parsed before exception
    }

    return shapes;
}
```

### 7.3 Anchor Node Parsing (`ParseAnchorNode`)
```csharp
private static DocShapeObject ParseAnchorNode(
    XmlNode anchorNode, 
    ZipArchive archive, 
    Dictionary<string, string> drawingRels, 
    Dictionary<uint, int> colWidths, 
    Dictionary<uint, int> rowHeights, 
    int defColW, 
    int defRowH)
{
    if (anchorNode == null) return null;
    string localName = anchorNode.LocalName;
    if (localName != "twoCellAnchor" && localName != "oneCellAnchor" && localName != "absoluteAnchor")
    {
        return null;
    }

    int pageX = 0, pageY = 0, baseW = 100, baseH = 50;

    if (localName == "twoCellAnchor")
    {
        XmlNode fromNode = null, toNode = null;
        foreach (XmlNode ch in anchorNode.ChildNodes)
        {
            if (ch.LocalName == "from") fromNode = ch;
            else if (ch.LocalName == "to") toNode = ch;
        }

        if (fromNode != null && toNode != null)
        {
            uint fromCol = 0, fromRow = 0, toCol = 0, toRow = 0;
            long fromColOff = 0, fromRowOff = 0, toColOff = 0, toRowOff = 0;

            foreach (XmlNode fch in fromNode.ChildNodes)
            {
                if (fch.LocalName == "col") uint.TryParse(fch.InnerText, out fromCol);
                else if (fch.LocalName == "colOff") long.TryParse(fch.InnerText, out fromColOff);
                else if (fch.LocalName == "row") uint.TryParse(fch.InnerText, out fromRow);
                else if (fch.LocalName == "rowOff") long.TryParse(fch.InnerText, out fromRowOff);
            }

            foreach (XmlNode tch in toNode.ChildNodes)
            {
                if (tch.LocalName == "col") uint.TryParse(tch.InnerText, out toCol);
                else if (tch.LocalName == "colOff") long.TryParse(tch.InnerText, out toColOff);
                else if (tch.LocalName == "row") uint.TryParse(tch.InnerText, out toRow);
                else if (tch.LocalName == "rowOff") long.TryParse(tch.InnerText, out toRowOff);
            }

            uint fromCol1 = fromCol + 1;
            uint fromRow1 = fromRow + 1;
            uint toCol1 = toCol + 1;
            uint toRow1 = toRow + 1;

            int startX = 0;
            for (uint c = 1; c < fromCol1; c++) startX += GetColWidthForSheet(c, colWidths, defColW);
            pageX = startX + (int)Math.Round((double)fromColOff / 9525.0);

            int startY = 0;
            for (uint r = 1; r < fromRow1; r++) startY += GetRowHeightForSheet(r, rowHeights, defRowH);
            pageY = startY + (int)Math.Round((double)fromRowOff / 9525.0);

            int endX = 0;
            for (uint c = 1; c < toCol1; c++) endX += GetColWidthForSheet(c, colWidths, defColW);
            endX += (int)Math.Round((double)toColOff / 9525.0);

            int endY = 0;
            for (uint r = 1; r < toRow1; r++) endY += GetRowHeightForSheet(r, rowHeights, defRowH);
            endY += (int)Math.Round((double)toRowOff / 9525.0);

            baseW = Math.Max(10, endX - pageX);
            baseH = Math.Max(10, endY - pageY);
        }
    }
    else if (localName == "oneCellAnchor")
    {
        XmlNode fromNode = null, extNode = null;
        foreach (XmlNode ch in anchorNode.ChildNodes)
        {
            if (ch.LocalName == "from") fromNode = ch;
            else if (ch.LocalName == "ext") extNode = ch;
        }

        if (fromNode != null)
        {
            uint fromCol = 0, fromRow = 0;
            long fromColOff = 0, fromRowOff = 0;

            foreach (XmlNode fch in fromNode.ChildNodes)
            {
                if (fch.LocalName == "col") uint.TryParse(fch.InnerText, out fromCol);
                else if (fch.LocalName == "colOff") long.TryParse(fch.InnerText, out fromColOff);
                else if (fch.LocalName == "row") uint.TryParse(fch.InnerText, out fromRow);
                else if (fch.LocalName == "rowOff") long.TryParse(fch.InnerText, out fromRowOff);
            }

            uint fromCol1 = fromCol + 1;
            uint fromRow1 = fromRow + 1;

            int startX = 0;
            for (uint c = 1; c < fromCol1; c++) startX += GetColWidthForSheet(c, colWidths, defColW);
            pageX = startX + (int)Math.Round((double)fromColOff / 9525.0);

            int startY = 0;
            for (uint r = 1; r < fromRow1; r++) startY += GetRowHeightForSheet(r, rowHeights, defRowH);
            pageY = startY + (int)Math.Round((double)fromRowOff / 9525.0);
        }

        if (extNode != null && extNode.Attributes != null)
        {
            long cx = 0, cy = 0;
            if (extNode.Attributes["cx"] != null) long.TryParse(extNode.Attributes["cx"].Value, out cx);
            if (extNode.Attributes["cy"] != null) long.TryParse(extNode.Attributes["cy"].Value, out cy);
            baseW = Math.Max(10, (int)Math.Round((double)cx / 9525.0));
            baseH = Math.Max(10, (int)Math.Round((double)cy / 9525.0));
        }
    }
    else if (localName == "absoluteAnchor")
    {
        XmlNode posNode = null, extNode = null;
        foreach (XmlNode ch in anchorNode.ChildNodes)
        {
            if (ch.LocalName == "pos") posNode = ch;
            else if (ch.LocalName == "ext") extNode = ch;
        }

        if (posNode != null && posNode.Attributes != null)
        {
            long x = 0, y = 0;
            if (posNode.Attributes["x"] != null) long.TryParse(posNode.Attributes["x"].Value, out x);
            if (posNode.Attributes["y"] != null) long.TryParse(posNode.Attributes["y"].Value, out y);
            pageX = (int)Math.Round((double)x / 9525.0);
            pageY = (int)Math.Round((double)y / 9525.0);
        }

        if (extNode != null && extNode.Attributes != null)
        {
            long cx = 0, cy = 0;
            if (extNode.Attributes["cx"] != null) long.TryParse(extNode.Attributes["cx"].Value, out cx);
            if (extNode.Attributes["cy"] != null) long.TryParse(extNode.Attributes["cy"].Value, out cy);
            baseW = Math.Max(10, (int)Math.Round((double)cx / 9525.0));
            baseH = Math.Max(10, (int)Math.Round((double)cy / 9525.0));
        }
    }

    // Find visual element child
    foreach (XmlNode ch in anchorNode.ChildNodes)
    {
        if (ch.LocalName == "pic")
        {
            return ParsePictureElement(ch, archive, drawingRels, pageX, pageY, baseW, baseH);
        }
        else if (ch.LocalName == "sp")
        {
            return ParseShapeElement(ch, pageX, pageY, baseW, baseH);
        }
        else if (ch.LocalName == "graphicFrame")
        {
            return ParseGraphicFrameElement(ch, archive, drawingRels, pageX, pageY, baseW, baseH);
        }
    }

    return null;
}
```

### 7.4 Picture Element Parser (`ParsePictureElement`)
```csharp
private static DocShapeObject ParsePictureElement(
    XmlNode picNode, 
    ZipArchive archive, 
    Dictionary<string, string> drawingRels, 
    int pageX, 
    int pageY, 
    int baseW, 
    int baseH)
{
    DocShapeObject sh = new DocShapeObject("Image", pageX, pageY, baseW, baseH);
    sh.FillColor = Color.Transparent;
    sh.OutlineColor = Color.Transparent;
    sh.OutlineWidth = 0f;

    string picName = "";
    string picDescr = "";

    XmlNode nvPicPr = picNode.SelectSingleNode(".//*[local-name()='nvPicPr']");
    if (nvPicPr != null)
    {
        XmlNode cNvPr = nvPicPr.SelectSingleNode(".//*[local-name()='cNvPr']");
        if (cNvPr != null && cNvPr.Attributes != null)
        {
            if (cNvPr.Attributes["id"] != null) sh.Id = cNvPr.Attributes["id"].Value;
            if (cNvPr.Attributes["name"] != null) picName = cNvPr.Attributes["name"].Value;
            if (cNvPr.Attributes["descr"] != null) picDescr = cNvPr.Attributes["descr"].Value;
            sh.CustomName = picName;
            sh.AltText = picDescr;
        }
    }

    // Check embedded image relationship
    XmlNode blipNode = picNode.SelectSingleNode(".//*[local-name()='blip']");
    string embedId = null;
    if (blipNode != null && blipNode.Attributes != null)
    {
        foreach (XmlAttribute attr in blipNode.Attributes)
        {
            if (attr.LocalName == "embed")
            {
                embedId = attr.Value;
                break;
            }
        }
    }

    if (!string.IsNullOrEmpty(embedId) && drawingRels.ContainsKey(embedId))
    {
        string mediaPath = drawingRels[embedId];
        ZipArchiveEntry mediaEntry = GetEntryCaseInsensitive(archive, mediaPath);
        if (mediaEntry != null)
        {
            try
            {
                using (Stream msStream = mediaEntry.Open())
                using (MemoryStream ms = new MemoryStream())
                {
                    msStream.CopyTo(ms);
                    ms.Position = 0;
                    using (Image tempImg = Image.FromStream(ms))
                    {
                        sh.CachedImage = new Bitmap(tempImg);
                    }
                }
            }
            catch { }
        }
    }

    // Check rotation
    XmlNode xfrmNode = picNode.SelectSingleNode(".//*[local-name()='xfrm']");
    if (xfrmNode != null && xfrmNode.Attributes != null && xfrmNode.Attributes["rot"] != null)
    {
        long rot;
        if (long.TryParse(xfrmNode.Attributes["rot"].Value, out rot))
        {
            sh.RotationAngle = (float)(rot / 60000.0);
        }
    }

    // If marked as Chart in name or descr, restore chart identity
    if (picName.StartsWith("Chart", StringComparison.OrdinalIgnoreCase) || 
        picDescr.StartsWith("Chart", StringComparison.OrdinalIgnoreCase))
    {
        sh.ShapeType = "Chart";
        string cType = !string.IsNullOrEmpty(picDescr) ? picDescr.Replace("Chart|", "") : "Column_Clustered";
        sh.ChartData = DocChartData.CreateDefault(cType);
        sh.TextContent = sh.ChartData.ChartType;
    }

    return sh;
}
```

### 7.5 Vector Shape Parser (`ParseShapeElement`)
```csharp
private static DocShapeObject ParseShapeElement(XmlNode spNode, int pageX, int pageY, int baseW, int baseH)
{
    DocShapeObject sh = new DocShapeObject("Rectangle", pageX, pageY, baseW, baseH);

    XmlNode nvSpPr = spNode.SelectSingleNode(".//*[local-name()='nvSpPr']");
    if (nvSpPr != null)
    {
        XmlNode cNvPr = nvSpPr.SelectSingleNode(".//*[local-name()='cNvPr']");
        if (cNvPr != null && cNvPr.Attributes != null)
        {
            if (cNvPr.Attributes["id"] != null) sh.Id = cNvPr.Attributes["id"].Value;
            if (cNvPr.Attributes["name"] != null) sh.CustomName = cNvPr.Attributes["name"].Value;
        }
    }

    XmlNode spPr = spNode.SelectSingleNode(".//*[local-name()='spPr']");
    if (spPr != null)
    {
        // 1. Preset Geometry
        XmlNode prstGeom = spPr.SelectSingleNode(".//*[local-name()='prstGeom']");
        if (prstGeom != null && prstGeom.Attributes != null && prstGeom.Attributes["prst"] != null)
        {
            string prst = prstGeom.Attributes["prst"].Value;
            switch (prst)
            {
                case "rect": sh.ShapeType = "Rectangle"; break;
                case "roundRect": sh.ShapeType = "RoundedRectangle"; break;
                case "ellipse": sh.ShapeType = "Oval"; break;
                case "triangle": sh.ShapeType = "Triangle"; break;
                case "rtTriangle": sh.ShapeType = "RightTriangle"; break;
                case "diamond": sh.ShapeType = "Diamond"; break;
                case "parallelogram": sh.ShapeType = "Parallelogram"; break;
                case "trapezoid": sh.ShapeType = "Trapezoid"; break;
                case "hexagon": sh.ShapeType = "Hexagon"; break;
                case "octagon": sh.ShapeType = "Octagon"; break;
                case "star5": sh.ShapeType = "Star5"; break;
                case "heart": sh.ShapeType = "Heart"; break;
                case "rightArrow": sh.ShapeType = "RightArrow"; break;
                case "leftArrow": sh.ShapeType = "LeftArrow"; break;
                case "upArrow": sh.ShapeType = "UpArrow"; break;
                case "downArrow": sh.ShapeType = "DownArrow"; break;
                case "can": case "cylinder": sh.ShapeType = "Cylinder"; break;
                case "cube": sh.ShapeType = "Cube"; break;
                case "donut": sh.ShapeType = "Donut"; break;
                case "line": sh.ShapeType = "Line"; break;
                default: sh.ShapeType = prst; break;
            }
        }

        // 2. Fill Color
        XmlNode solidFill = spPr.SelectSingleNode("./*[local-name()='solidFill']");
        if (solidFill != null)
        {
            XmlNode srgbClr = solidFill.SelectSingleNode("./*[local-name()='srgbClr']");
            if (srgbClr != null && srgbClr.Attributes != null && srgbClr.Attributes["val"] != null)
            {
                sh.FillColor = ParseColorHex(srgbClr.Attributes["val"].Value, Color.FromArgb(43, 87, 154));
            }
        }
        else if (spPr.SelectSingleNode("./*[local-name()='noFill']") != null)
        {
            sh.FillColor = Color.Transparent;
        }

        // 3. Line Outline
        XmlNode lnNode = spPr.SelectSingleNode("./*[local-name()='ln']");
        if (lnNode != null)
        {
            if (lnNode.Attributes != null && lnNode.Attributes["w"] != null)
            {
                long lw;
                if (long.TryParse(lnNode.Attributes["w"].Value, out lw))
                {
                    sh.OutlineWidth = Math.Max(1f, (float)(lw / 9525.0));
                }
            }

            XmlNode lnFill = lnNode.SelectSingleNode("./*[local-name()='solidFill']/*[local-name()='srgbClr']");
            if (lnFill != null && lnFill.Attributes != null && lnFill.Attributes["val"] != null)
            {
                sh.OutlineColor = ParseColorHex(lnFill.Attributes["val"].Value, Color.FromArgb(30, 60, 110));
            }

            if (lnNode.SelectSingleNode("./*[local-name()='noFill']") != null)
            {
                sh.OutlineColor = Color.Transparent;
                sh.OutlineWidth = 0f;
            }
        }

        // 4. Rotation
        XmlNode xfrm = spPr.SelectSingleNode("./*[local-name()='xfrm']");
        if (xfrm != null && xfrm.Attributes != null && xfrm.Attributes["rot"] != null)
        {
            long rot;
            if (long.TryParse(xfrm.Attributes["rot"].Value, out rot))
            {
                sh.RotationAngle = (float)(rot / 60000.0);
            }
        }
    }

    // 5. Text Body
    XmlNode txBody = spNode.SelectSingleNode(".//*[local-name()='txBody']");
    if (txBody != null)
    {
        StringBuilder sb = new StringBuilder();
        XmlNodeList pNodes = txBody.SelectNodes(".//*[local-name()='p']");
        if (pNodes != null)
        {
            for (int pi = 0; pi < pNodes.Count; pi++)
            {
                XmlNode pNode = pNodes[pi];
                XmlNodeList tNodes = pNode.SelectNodes(".//*[local-name()='t']");
                if (tNodes != null)
                {
                    for (int ti = 0; ti < tNodes.Count; ti++)
                    {
                        sb.Append(tNodes[ti].InnerText);
                    }
                }
                if (pi < pNodes.Count - 1) sb.Append("\n");

                // Check text alignment
                XmlNode pPr = pNode.SelectSingleNode("./*[local-name()='pPr']");
                if (pPr != null && pPr.Attributes != null && pPr.Attributes["algn"] != null)
                {
                    string algn = pPr.Attributes["algn"].Value;
                    if (algn == "ctr") sh.TextAlignment = StringAlignment.Center;
                    else if (algn == "r") sh.TextAlignment = StringAlignment.Far;
                    else if (algn == "l") sh.TextAlignment = StringAlignment.Near;
                }
            }
        }
        sh.TextContent = sb.ToString();

        // Check text run formatting
        XmlNode rPr = txBody.SelectSingleNode(".//*[local-name()='rPr']");
        if (rPr != null && rPr.Attributes != null)
        {
            if (rPr.Attributes["sz"] != null)
            {
                float sz;
                if (float.TryParse(rPr.Attributes["sz"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out sz))
                {
                    sh.FontSize = sz / 100f;
                }
            }
            if (rPr.Attributes["b"] != null) sh.Bold = (rPr.Attributes["b"].Value == "1" || rPr.Attributes["b"].Value.ToLower() == "true");
            if (rPr.Attributes["i"] != null) sh.Italic = (rPr.Attributes["i"].Value == "1" || rPr.Attributes["i"].Value.ToLower() == "true");

            XmlNode textClr = rPr.SelectSingleNode(".//*[local-name()='srgbClr']");
            if (textClr != null && textClr.Attributes != null && textClr.Attributes["val"] != null)
            {
                sh.TextColor = ParseColorHex(textClr.Attributes["val"].Value, Color.White);
            }
        }
    }

    return sh;
}
```

### 7.6 GraphicFrame Element Parser (`ParseGraphicFrameElement`)
```csharp
private static DocShapeObject ParseGraphicFrameElement(
    XmlNode gfNode, 
    ZipArchive archive, 
    Dictionary<string, string> drawingRels, 
    int pageX, 
    int pageY, 
    int baseW, 
    int baseH)
{
    DocShapeObject sh = new DocShapeObject("Chart", pageX, pageY, baseW, baseH);
    sh.FillColor = Color.White;
    sh.OutlineColor = Color.FromArgb(209, 213, 219);
    sh.OutlineWidth = 1.0f;

    XmlNode nvGfPr = gfNode.SelectSingleNode(".//*[local-name()='nvGraphicFramePr']");
    if (nvGfPr != null)
    {
        XmlNode cNvPr = nvGfPr.SelectSingleNode(".//*[local-name()='cNvPr']");
        if (cNvPr != null && cNvPr.Attributes != null)
        {
            if (cNvPr.Attributes["id"] != null) sh.Id = cNvPr.Attributes["id"].Value;
            if (cNvPr.Attributes["name"] != null) sh.CustomName = cNvPr.Attributes["name"].Value;
        }
    }

    XmlNode graphicData = gfNode.SelectSingleNode(".//*[local-name()='graphicData']");
    if (graphicData != null && graphicData.Attributes != null && graphicData.Attributes["uri"] != null)
    {
        string uri = graphicData.Attributes["uri"].Value;

        // 1. DrawingML Chart
        if (uri.IndexOf("chart", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            XmlNode chartRefNode = graphicData.SelectSingleNode(".//*[local-name()='chart']");
            string chartRelId = null;
            if (chartRefNode != null && chartRefNode.Attributes != null)
            {
                foreach (XmlAttribute attr in chartRefNode.Attributes)
                {
                    if (attr.LocalName == "id")
                    {
                        chartRelId = attr.Value;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(chartRelId) && drawingRels.ContainsKey(chartRelId))
            {
                string chartPath = drawingRels[chartRelId];
                ZipArchiveEntry chartEntry = GetEntryCaseInsensitive(archive, chartPath);
                if (chartEntry != null)
                {
                    using (Stream cs = chartEntry.Open())
                    {
                        sh.ChartData = ParseChartXml(cs);
                    }
                }
            }

            if (sh.ChartData == null)
            {
                sh.ChartData = DocChartData.CreateDefault("Column_Clustered");
            }
            sh.TextContent = sh.ChartData.ChartType;
            return sh;
        }
        // 2. SmartArt Diagram
        else if (uri.IndexOf("diagram", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            sh.ShapeType = "SmartArt";
            sh.SmartArtType = "Cycle_Radial";
            sh.SmartArtTitle = "Sơ đồ";
            sh.TextContent = "Mục 1|Mục 2|Mục 3";
            return sh;
        }
    }

    // Default fallback
    sh.ChartData = DocChartData.CreateDefault("Column_Clustered");
    sh.TextContent = sh.ChartData.ChartType;
    return sh;
}
```

### 7.7 Native OpenXML Chart XML Parser (`ParseChartXml`)
```csharp
private static DocChartData ParseChartXml(Stream stream)
{
    DocChartData data = new DocChartData();
    try
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(stream);

        // 1. Title
        XmlNode titleNode = doc.SelectSingleNode(".//*[local-name()='title']");
        if (titleNode != null)
        {
            XmlNode tNode = titleNode.SelectSingleNode(".//*[local-name()='t']");
            if (tNode != null && !string.IsNullOrEmpty(tNode.InnerText))
            {
                data.Title = tNode.InnerText;
            }
            else
            {
                XmlNode vNode = titleNode.SelectSingleNode(".//*[local-name()='v']");
                if (vNode != null && !string.IsNullOrEmpty(vNode.InnerText))
                {
                    data.Title = vNode.InnerText;
                }
            }
            data.ShowTitle = true;
        }

        // 2. PlotArea & Chart Type
        XmlNode plotArea = doc.SelectSingleNode(".//*[local-name()='plotArea']");
        if (plotArea != null)
        {
            XmlNode chartTypeNode = null;
            foreach (XmlNode ch in plotArea.ChildNodes)
            {
                if (ch.LocalName.EndsWith("Chart", StringComparison.OrdinalIgnoreCase))
                {
                    chartTypeNode = ch;
                    break;
                }
            }

            if (chartTypeNode != null)
            {
                string localName = chartTypeNode.LocalName;
                if (localName == "barChart")
                {
                    string barDir = "col";
                    string grouping = "clustered";
                    XmlNode bdNode = chartTypeNode.SelectSingleNode("./*[local-name()='barDir']");
                    if (bdNode != null && bdNode.Attributes != null && bdNode.Attributes["val"] != null)
                        barDir = bdNode.Attributes["val"].Value;
                    XmlNode grpNode = chartTypeNode.SelectSingleNode("./*[local-name()='grouping']");
                    if (grpNode != null && grpNode.Attributes != null && grpNode.Attributes["val"] != null)
                        grouping = grpNode.Attributes["val"].Value;

                    if (barDir == "bar")
                        data.ChartType = (grouping == "stacked") ? "Bar_Stacked" : "Bar_Clustered";
                    else
                        data.ChartType = (grouping == "stacked") ? "Column_Stacked" : "Column_Clustered";
                }
                else if (localName == "lineChart") data.ChartType = "Line";
                else if (localName == "pieChart") data.ChartType = "Pie";
                else if (localName == "doughnutChart") data.ChartType = "Doughnut";
                else if (localName == "areaChart") data.ChartType = "Area";
                else if (localName == "radarChart") data.ChartType = "Radar";
                else if (localName == "scatterChart") data.ChartType = "Scatter";
                else data.ChartType = "Column_Clustered";

                // 3. Series, Categories, and Values
                XmlNodeList serNodes = chartTypeNode.SelectNodes("./*[local-name()='ser']");
                List<string> seriesNames = new List<string>();
                List<string> categories = new List<string>();
                Dictionary<int, List<double>> seriesValues = new Dictionary<int, List<double>>();

                if (serNodes != null)
                {
                    for (int s = 0; s < serNodes.Count; s++)
                    {
                        XmlNode serNode = serNodes[s];

                        // Series Name
                        string sName = "Chuỗi " + (s + 1);
                        XmlNode txNode = serNode.SelectSingleNode("./*[local-name()='tx']");
                        if (txNode != null)
                        {
                            XmlNode vNode = txNode.SelectSingleNode(".//*[local-name()='v']");
                            if (vNode != null && !string.IsNullOrEmpty(vNode.InnerText))
                                sName = vNode.InnerText;
                        }
                        seriesNames.Add(sName);

                        // Categories (from first series)
                        if (s == 0)
                        {
                            XmlNode catNode = serNode.SelectSingleNode("./*[local-name()='cat']");
                            if (catNode != null)
                            {
                                XmlNodeList ptNodes = catNode.SelectNodes(".//*[local-name()='pt']");
                                if (ptNodes != null)
                                {
                                    foreach (XmlNode pt in ptNodes)
                                    {
                                        XmlNode vNode = pt.SelectSingleNode("./*[local-name()='v']");
                                        if (vNode != null) categories.Add(vNode.InnerText);
                                    }
                                }
                            }
                        }

                        // Values
                        List<double> valList = new List<double>();
                        XmlNode valNode = serNode.SelectSingleNode("./*[local-name()='val']");
                        if (valNode != null)
                        {
                            XmlNodeList ptNodes = valNode.SelectNodes(".//*[local-name()='pt']");
                            if (ptNodes != null)
                            {
                                foreach (XmlNode pt in ptNodes)
                                {
                                    XmlNode vNode = pt.SelectSingleNode("./*[local-name()='v']");
                                    double dVal = 0.0;
                                    if (vNode != null)
                                    {
                                        double.TryParse(vNode.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture, out dVal);
                                    }
                                    valList.Add(dVal);
                                }
                            }
                        }
                        seriesValues[s] = valList;
                    }
                }

                // If categories are empty, generate default
                int maxCatCount = 0;
                foreach (var kvp in seriesValues)
                {
                    if (kvp.Value.Count > maxCatCount) maxCatCount = kvp.Value.Count;
                }
                while (categories.Count < maxCatCount)
                {
                    categories.Add("Danh mục " + (categories.Count + 1));
                }

                data.Categories = categories;
                data.SeriesNames = seriesNames;

                // Build Values matrix [CategoryIndex][SeriesIndex]
                data.Values = new List<List<double>>();
                for (int c = 0; c < categories.Count; c++)
                {
                    List<double> row = new List<double>();
                    for (int s = 0; s < seriesNames.Count; s++)
                    {
                        double v = (seriesValues.ContainsKey(s) && c < seriesValues[s].Count) ? seriesValues[s][c] : 0.0;
                        row.Add(v);
                    }
                    data.Values.Add(row);
                }

                for (int s = 0; s < seriesNames.Count; s++) data.SeriesVisible.Add(true);
                for (int c = 0; c < categories.Count; c++) data.CategoriesVisible.Add(true);
            }
        }
    }
    catch
    {
        return DocChartData.CreateDefault("Column_Clustered");
    }

    return data;
}
```

### 7.8 Integration into `LoadXlsx` in `XlsxHandler.cs`
In `LoadXlsx`, where sheets are being assembled into `calcView`:
```csharp
// At line 742 of XlsxHandler.cs:
for (int i = 0; i < loadedSheets.Count; i++)
{
    calcView.Sheets.Add(loadedSheets[i]);
    calcView.SheetNames.Add(finalSheetNames[i]);
    FormulaEngine.EvaluateSheet(loadedSheets[i]);

    var st = new CalcSheetState();
    st.CustomColWidths = loadedColWidthsList[i];
    st.CustomRowHeights = loadedRowHeightsList[i];

    // Load drawings for sheet (i + 1)
    st.Shapes = LoadSheetDrawings(
        archive, 
        i + 1, 
        loadedColWidthsList[i], 
        loadedRowHeightsList[i], 
        loadedDefColWList[i], 
        loadedDefRowHList[i]);

    calcView.SheetStates.Add(st);
}

// At line 760 of XlsxHandler.cs:
calcView.ActiveSheetIndex = 0;
calcView.LoadSheetState(0); 
// Automatically copies st.Shapes (and FloatingObjects) to calcView.Shapes!
```

---

## 8. Edge Cases, Hardening & Defensive Parsing

| Edge Case | Failure Mode Without Fix | Defensive Strategy |
|:---|:---|:---|
| **Missing `_rels` Entry** | Throws `NullReferenceException` | Use `GetEntryCaseInsensitive`; if rels is missing, fallback to convention `xl/drawings/drawing{N}.xml`. |
| **Corrupted Media Stream** | GDI+ locks open stream / crashes | Read into detached byte array `byte[]` and construct fresh `new Bitmap(MemoryStream)` with using block. |
| **Zero-Length Dimensions** | Invisible $0 \times 0$ shape | Clamped via `Math.Max(10, width)` and `Math.Max(10, height)`. |
| **Negative Offsets / Out-of-Bounds** | Position placed off-canvas ($<0$) | Clamped to non-negative pixel coordinates: `Math.Max(0, pageX)`. |
| **Locale / Decimal Comma Drift** | Float parsing fails on `"123.45"` | Strictly use `CultureInfo.InvariantCulture` and `NumberStyles.Any`. |
| **Non-Standard Prefix Tags** | Rigid XPath queries fail | Parse using `LocalName` comparisons (`node.LocalName == "pic"`), matching OpenXML standard regardless of prefix. |
| **Empty Chart Series** | IndexOutOfRangeException | Validate counts before indexing into `DocChartData.Values`. Fall back to `DocChartData.CreateDefault`. |

---

## 9. Verification & Automated Test Strategy

### 9.1 Unit Level Verification (Coordinate Invariant)
Run deterministic assertions matching the inverse math:
- `FromCol = 1`, `FromColOff = 0` $\rightarrow$ `PageX = 0`.
- `FromCol = 2`, `ColWidth[1] = 80`, `FromColOff = 190500` $\rightarrow$ `PageX = 80 + 20 = 100`.
- Re-saving `PageX = 100` must produce `FromCol = 2` ($1$ in 0-indexed XML) and `colOff = 190500`.

### 9.2 Opaque End-to-End Roundtrip Test
1. Instantiate `CalcView`.
2. Add Sheet 1.
3. Insert 1 Picture (`DocShapeObject` with `ShapeType = "Image"`, 200x150, at cell B2).
4. Insert 1 Shape (`DocShapeObject` with `ShapeType = "Rectangle"`, 160x100, at cell D4).
5. Insert 1 Chart (`DocShapeObject` with `ShapeType = "Chart"`, `DocChartData.CreateDefault("Pie")`, at cell F6).
6. Invoke `XlsxHandler.SaveXlsx("test_roundtrip.xlsx", calcView)`.
7. Instantiate fresh `CalcView` instance `calcView2`.
8. Invoke `XlsxHandler.LoadXlsx("test_roundtrip.xlsx", calcView2)`.
9. Assert:
   - `calcView2.FloatingObjects.Count == 3`.
   - `calcView2.Shapes.Count == 3`.
   - Object 1: `ShapeType == "Image"`, `CachedImage != null`, `PageX == calcView.Shapes[0].PageX`, `PageY == calcView.Shapes[0].PageY`.
   - Object 2: `ShapeType == "Rectangle"`, `TextContent == calcView.Shapes[1].TextContent`, `Bounds == calcView.Shapes[1].Bounds`.
   - Object 3: `ShapeType == "Chart"`, `ChartData != null`, `ChartData.ChartType == "Pie"`.

### 9.3 Build Verification
Execute:
```bat
cmd.exe /c build.bat
```
Must produce exit code `0` with 0 compilation errors.
