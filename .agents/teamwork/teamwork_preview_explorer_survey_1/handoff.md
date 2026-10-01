# Handoff Report: Survey of ZenQ Calc Codebase & Architecture

**Agent**: Survey Agent 1 (Codebase & Architecture Explorer)  
**Working Directory**: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_survey_1`  
**Date**: 2026-09-28  
**Handoff Type**: Hard (Task Complete)  

---

## 1. Observation

1. **Compilation Environment & Toolchain**:
   - Running `cmd.exe /c build.bat` returned exit code 0 and output:
     ```
     Microsoft (R) Visual C# Compiler version 4.8.9232.0
     for C# 5
     Copyright (C) Microsoft Corporation. All rights reserved.
     [SUCCESS] GenOffice.exe built successfully!
     09/28/2026 05:43 PM 19,241,472 GenOffice.exe
     ```
   - Target compiler is `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, which supports **only C# 5**.
   - Assemblies referenced: `System.dll`, `System.Drawing.dll`, `System.Windows.Forms.dll`, `System.Xml.dll`, `System.Xml.Linq.dll`, `System.IO.Compression.dll`, `System.IO.Compression.FileSystem.dll`, `Microsoft.CSharp.dll`, `WindowsBase.dll`, `DocumentFormat.OpenXml.dll`, `DocumentFormat.OpenXml.Framework.dll`, `DocumentFormat.OpenXml.Features.dll`, `DocumentFormat.OpenXml.Linq.dll`, `PdfiumViewer.dll`, `PdfSharp.dll`.
   - Source flag: `/recurse:src\*.cs`.

2. **OpenXML Drawing Export vs Load Gap in `XlsxHandler.cs`**:
   - `SaveXlsx` in `src/format/XlsxHandler.cs` lines 1216-1440 writes drawing ML parts:
     - Line 1219: `archive.CreateEntry("xl/worksheets/_rels/sheet1.xml.rels")`
     - Line 1272: `archive.CreateEntry(string.Format("xl/media/image{0}.png", nextImgId))`
     - Line 1281: `archive.CreateEntry("xl/drawings/_rels/drawing1.xml.rels")`
     - Line 1293: `archive.CreateEntry("xl/drawings/drawing1.xml")`
     - Lines 1351-1418: Writes `<xdr:twoCellAnchor editAs="oneCell">`, `<xdr:pic>`, `<xdr:sp>`.
   - In contrast, `LoadXlsx` (lines 300-770) contains **zero occurrences** of `"drawing"`, `"xl/drawings"`, or `"xl/media"`. It loads only shared strings, styles, workbook sheet names, and `sheet*.xml` cell values/formats/merges.

3. **Calc PDF Export Missing**:
   - In `src/ui/AppForm.cs` lines 4200-4212:
     ```csharp
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
   - In Calc mode, `AppForm` exports a blank PDF. `CalcView.cs` has no `ExportToPdf` method.

4. **Visual Objects & Shapes in `CalcView.cs`**:
   - In `src/ui/CalcView.cs` line 260:
     ```csharp
     public List<DocShapeObject> Shapes { get; set; }
     ```
   - In `CalcSheetState` line 111:
     ```csharp
     public List<DocShapeObject> Shapes { get; set; }
     ```
   - Visual objects (shapes, images, charts, SmartArt) are stored in `Shapes`. `FloatingObjects` does not yet exist as an alias property.

5. **FormulaEngine Coverage in `src/core/FormulaEngine.cs`**:
   - 1447 lines implementing ~35 functions (SUM, AVERAGE, MIN, MAX, COUNT, COUNTA, PRODUCT, ROUND, ROUNDUP, ROUNDDOWN, ABS, SQRT, POWER, IF, AND, OR, NOT, CHOOSE, COUNTIF, SUMIF, AVERAGEIF, MOD, INT, TRUNC, MEDIAN, LARGE, SMALL, PROPER, SUBSTITUTE, EXACT, DATE, TIME, CONCAT, LEFT, RIGHT, MID, LEN, UPPER, LOWER, TRIM, TODAY, NOW, VLOOKUP, HLOOKUP, XLOOKUP, INDEX, MATCH, OFFSET, INDIRECT, IFS, XOR, SWITCH, IFERROR, TEXTJOIN, TEXT, DAY, MONTH, YEAR, HOUR, MINUTE, SECOND).
   - Functions like `XMATCH`, `ROW`, `COLUMN`, `ROWS`, `COLUMNS`, `IFNA`, `WEEKDAY`, `EDATE`, `EOMONTH`, `NETWORKDAYS`, `DATEDIF`, `VALUE`, `FIND`, `SEARCH`, `REPLACE`, `REPT`, `TEXTSPLIT`, `TEXTBEFORE`, `TEXTAFTER`, `CHAR`, `CODE`, `CLEAN`, `RANDBETWEEN`, `RAND`, `CEILING`, `FLOOR`, `SIGN`, `STDEV`, `VAR`, `RANK`, `MODE`, `ISNUMBER`, `ISTEXT`, `ISBLANK`, `ISERROR`, `ISNA` are missing.

6. **File Formats Beyond XLSX/CSV**:
   - `.ods` and `.xls` (BIFF8 binary): No reader or writer exists in `src/format/`. Passing `.xls` to `LoadXlsx` fails because `ZipArchive` cannot parse binary OLE CFBF files.
   - HTML export (`.html`) and space-delimited text (`.prn`): No implementation exists.

7. **Structured Tables & Dynamic Arrays**:
   - `ExcelTableInfo` exists in `src/ui/CalcTableAndPivotDialogs.cs` and `CreateTableFromRange` applies zebra colors. However, `Ctrl+T` shortcut, auto-expansion upon typing in adjacent cells, Total Row aggregations, and structured formula references (`Table1[@Col]`) are missing.
   - Dynamic arrays, spill engine (`#SPILL!`), thin blue border rendering, and array functions (`SORT`, `FILTER`, `UNIQUE`, `SEQUENCE`) are absent.

8. **PivotTables, What-If & Protection**:
   - `CreatePivotTable` in `CalcView.cs` lines 6166-6176 hardcodes source columns 2 and 5 into a static 2-column table. Dynamic multidimensional aggregation, Field List pane, date/numeric grouping, and linked PivotCharts are absent.
   - Goal Seek and Data Tables have zero occurrences across the codebase.
   - Sheet protection exists (`IsSheetProtected`), but workbook structure protection with password prompt, Print Titles, and Page Breaks are absent.

---

## 2. Logic Chain

1. **Drawings Roundtrip Bug**:
   - From Observation 2: `XlsxHandler.SaveXlsx` serializes `calcView.Shapes` into `xl/drawings/drawing1.xml` and `xl/media/`.
   - From Observation 2: `XlsxHandler.LoadXlsx` does not inspect `xl/drawings/` or `xl/media/`.
   - Inference: Any saved file with shapes, pictures, or charts loses all of them when reopened in GenOffice Calc. This directly breaks Acceptance Criterion 1 ("Saving an .xlsx file with 1 chart, 1 shape, and 1 picture, then reloading it through LoadXlsx reproduces all 3 objects at their exact cell anchor coordinates").
   - Remedy: Implement OpenXML drawing parsing in `LoadXlsx` using `ZipArchiveEntry` reading, mapping relationship IDs, computing cell anchor positions, and populating `calcView.Shapes` (aliased as `FloatingObjects`).

2. **Spreadsheet PDF Export Gap**:
   - From Observation 3: `AppForm.PerformExportPdf` branches to a fallback that outputs a blank PDF when in `OfficeAppMode.Calc`.
   - From Observation 1: `PdfSharp.dll` is available and referenced.
   - Inference: Implementing `CalcView.ExportToPdf(filePath)` using `PdfSharp.Drawing.XGraphics` to render cell grid, text, borders, colors, and margins will satisfy Acceptance Criterion 2.

3. **C# 5 Compatibility Rule**:
   - From Observation 1: `csc.exe` version 4.8.9232.0 compiles for C# 5.
   - Inference: Any implementation across Phases 1 to 4 using modern C# features (e.g. `$"..."`, `?.`, `=>`, `(a, b)`) will trigger compilation errors CS1056, CS1525, or CS1644. All changes must be verified against `build.bat` using strict C# 5 syntax.

---

## 3. Caveats

- **No Caveats**: All key files (`CalcView.cs`, `XlsxHandler.cs`, `FormatHandlers.cs`, `FormulaEngine.cs`, `SparseMatrix.cs`, `RibbonBar.cs`, `AppForm.cs`, `InsertFunctionDialog.cs`, `CalcTableAndPivotDialogs.cs`, `FindReplaceForm.cs`) were directly inspected. Build behavior was verified through execution of `build.bat`.

---

## 4. Conclusion

The ZenQ Calc subsystem is built on a solid, performant foundation (sparse matrix storage, custom GDI+ canvas, rich formula bar, and ribbon dispatch). However, significant gaps exist across all 4 phases:
- **Phase 1**: Critical roundtrip data loss for OpenXML drawings, blank PDF export in Calc mode, and missing data utilities (Find/Replace options, column modal for Remove Duplicates, Text to Columns wizard).
- **Phase 2**: FormulaEngine requires 50+ additional functions; file format diversity requires `.ods`, BIFF8 `.xls`, and HTML handlers.
- **Phase 3**: Excel Tables require auto-expansion, Total Row, and structured references; Dynamic Arrays require a spill evaluation and rendering engine.
- **Phase 4**: PivotTables require true dynamic multidimensional aggregation and a Field List pane; What-If requires Goal Seek and Data Tables; protection requires workbook-level locking.

All architectural findings, file paths, line references, and implementation specifications are formally documented in `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_survey_1\survey_report.md`.

---

## 5. Verification Method

1. **Verify Build System**:
   Execute:
   ```cmd
   cmd.exe /c build.bat
   ```
   Must produce `[SUCCESS] GenOffice.exe built successfully!` with exit code 0.

2. **Verify File References in Report**:
   - Inspect `src/format/XlsxHandler.cs` lines 1216-1440 (`SaveXlsx` drawing export) vs lines 300-770 (`LoadXlsx`).
   - Inspect `src/ui/AppForm.cs` lines 4200-4215 (`PerformExportPdf`).
   - Inspect `src/core/FormulaEngine.cs` line 120+ (function catalog).
   - Inspect `src/ui/CalcView.cs` line 260 (`Shapes` property).

3. **Invalidation Conditions**:
   - If modern C# features (C# 6+) are used, `build.bat` fails compilation.
   - If `LoadXlsx` is tested with an .xlsx containing drawings, `calcView.Shapes.Count` is currently 0 after load.
