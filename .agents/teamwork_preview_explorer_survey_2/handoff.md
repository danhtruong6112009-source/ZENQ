# HANDOFF REPORT — GenOffice OpenXML & Canvas Codebase Survey

## 1. Observation

### 1.1. Compilation Environment & Build System
- Command run: `cmd /c build.bat` in `c:\Users\Danh\Desktop\genofficepy`.
- Tool result:
  ```
  Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5
  Copyright (C) Microsoft Corporation. All rights reserved.
  [SUCCESS] GenOffice.exe built successfully!
  ```
- Build script `build.bat` (lines 1-10) uses:
  `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /main:GenOffice.Program /win32icon:GenOffice.ico ... /out:GenOffice.exe /recurse:src\*.cs`
- Assemblies referenced include: `DocumentFormat.OpenXml.dll`, `DocumentFormat.OpenXml.Framework.dll`, `DocumentFormat.OpenXml.Features.dll`, `DocumentFormat.OpenXml.Linq.dll`, `WindowsBase.dll`, `PdfSharp.dll`, `PdfiumViewer.dll`.
- Language constraint: C# 5 only. C# 6+ features (`$"..."`, `?.`, `is Type var`, `=>` methods/properties, `out var`) will cause compilation errors.

### 1.2. SmartArt Model & OpenXML Handling
- `src/format/OpenXmlSmartArtModel.cs`: Defines `OpenXmlSmartArtNode` (lines 14-43) and `OpenXmlSmartArtModel` (lines 49-234). Used in `src/ui/RibbonBar.cs` (lines 14448, 14463, 14485, 14501). Currently functions as an in-memory bridge between UI and `DocShapeObject`, rather than directly reading/writing OpenXML package parts.
- `src/format/FormatHandlers.cs`:
  - `TryParseSmartArtDiagram` (lines 2568-2923): Detects `<wp:docPr descr="SmartArt|...">`, reads `diagrams/data1.xml` (nodes `<dgm:pt>` and `<dgm:cxn>`) and `diagrams/layout1.xml` (`uniqueId` to identify layout type).
  - `RenderDirectDrawingMLDiagram` (lines 2192-2560): Parses shapes and connectors in `drawing1.xml`, extracts palette from `word/theme/theme1.xml`, pre-renders to a Bitmap, and builds `nodesMeta` using normalized bounding boxes:
    `relX = (s.X - minX) / (float)totalW; relY = (s.Y - minY) / (float)totalH; relW = s.W / (float)totalW; relH = s.H / (float)totalH;` (lines 2546-2553). Discards connectors like `circularArrow` from `nodesMeta` (line 2544).
  - Markdown tag generation (lines 3204-3215):
    `![SHAPE:ID={0}:TYPE=SmartArt:SMARTART={1}:TITLE={2}:TEXT={3}:STYLE=1:PATH={4}:NODES={5}:X={6}:Y={7}:W={8}:H={9}:WRAP={10}:PAGE=-1:YREL={11}]`

### 1.3. Canvas Rendering & Circle Distortion
- `src/ui/SmartArtRenderer.cs`:
  - In `PopulateSubShapeBounds` (lines 274-293):
    `cardW = Math.Min(0.24f, 0.85f / Math.Max(2, count / 2));`
    `cardH = Math.Min(0.18f, 0.85f / Math.Max(2, count / 2));`
    Hardcoded aspect ratio $0.24 / 0.18 = 1.333$.
  - In `DrawCycleCircleArrow` (lines 1848-1854):
    `int rx = Math.Max(30, (int)(r.Width * 0.33f));`
    `int ry = Math.Max(30, (int)(r.Height * 0.33f));`
    `g.DrawArc(ringPen, cx - rx, cy - ry, rx * 2, ry * 2, 0, 360);`
    Directly draws an ellipse when `r.Width != r.Height`.
- `src/ui/ShapeRenderer.cs`:
  - Lines 894-897:
    `int baseW = (int)(rf.Width * bounds.Width);`
    `int baseH = (int)(rf.Height * bounds.Height);`
    When `bounds.Width != bounds.Height` (e.g. 480x270), `baseW != baseH`.
  - Line 126:
    `case "Oval": case "Circle": path.AddEllipse(x, y, w, h); break;`
    Draws an ellipse without enforcing `minDim = Math.Min(w, h)`.

### 1.4. Adjacent Node Gaps & Snapping
- In `SmartArtRenderer.PopulateSubShapeBounds`: Only outer satellite nodes are computed for `Cycle_Radial`; no central circle (node 0) is generated.
- Outer nodes are positioned at `pixelRad = Math.Min(bw, bh) * 0.33f` without using the chord distance $2R\sin(\pi/N)$ or tangency equation $R = R_{\text{center}} + r_{\text{sat}}$, creating floating nodes with arbitrary gaps.

### 1.5. EMU Conversion & Scaling
- In `FormatHandlers.cs`:
  - Extent: `w = (int)(cx / 9525L); h = (int)(cy / 9525L);` (1 px at 96 DPI = 9525 EMUs).
  - Width clamp: `if (w > 602 ...)` constrained to printable page width (lines 3075-3080).
  - Stroke width: `shape.StrokeWidth = Math.Max(0.5f, lnW / 12700.0f);` (1 pt = 12700 EMUs).
  - In `WriterView.cs` (lines 8326-8331, 8589-8590):
    `sW = (int)((sh.BaseWidth + sh.WidthDelta) * ZoomLevel);`
    `sH = (int)((sh.BaseHeight + sh.HeightDelta) * ZoomLevel);`

### 1.6. Math / OMML Representation
- `FormatHandlers.cs` lines 1594-1604, 3896-3898 and `ExtractMathNodeRecursive` (lines 3347-3575):
  Converts `<m:oMath>` into flattened unicode pseudo-text (`x²`, `num/den`, `√`, `∫`), and inserts into `PieceTable` as regular text runs with `Times New Roman`, italic = true. No 2D math layout engine exists.

---

## 2. Logic Chain

1. **Circle Distortion Cause**:
   - Observation 1.3 shows `PopulateSubShapeBounds` hardcodes `cardW = 0.24` and `cardH = 0.18`.
   - Observation 1.3 shows `ShapeRenderer.cs` converts relative coordinates via `baseW = rf.Width * bounds.Width` and `baseH = rf.Height * bounds.Height`.
   - Observation 1.3 shows `ShapeRenderer.CreateShapePath` executes `path.AddEllipse(x, y, w, h)`.
   - **Inference**: For any non-square SmartArt container (such as 400x250), `baseW = 96` and `baseH = 45`, causing `AddEllipse` to draw a 2.13:1 flattened ellipse instead of a 1:1 circle.

2. **Adjacent Node Gaps Cause**:
   - Observation 1.4 shows `PopulateSubShapeBounds` generates satellite nodes at hardcoded radial distances with no central node.
   - Observation 1.2 shows `RenderDirectDrawingMLDiagram` drops connector geometry from `nodesMeta`.
   - **Inference**: Without geometric tangency constraints ($R_{\text{orbit}} = R_{\text{center}} + r_{\text{sat}}$ and $r_{\text{sat}} = R_{\text{orbit}}\sin(\pi/N)$), satellite nodes have no mathematical boundary alignment with the central ring or with each other, resulting in visual gaps.

3. **EMU Scaling Cause**:
   - Observation 1.5 shows OpenXML EMU dimensions are mapped to 96-DPI screen pixels using `/ 9525L` and points using `/ 12700.0f`.
   - In `FormatHandlers.cs`, `totalW` is computed from the bounding box of `drawing1.xml`, whereas `bounds.Width` in `WriterView` is computed from `wp:extent` clamped to 602 px.
   - **Inference**: Because `bounds.Width / bounds.Height != totalW / totalH`, unlinked relative coordinates `relW` and `relH` scale non-uniformly when projected onto the canvas.

4. **Math Representation Cause**:
   - Observation 1.6 confirms `m:oMath` is parsed exclusively into unicode strings and rendered via GDI+ `DrawString`.
   - **Inference**: GenOffice does not have a 2D math layout renderer for fractions, matrices, or radicals.

---

## 3. Caveats

- Investigation is strictly read-only; no modifications to project source code were made.
- Testing of visual rendering behavior was based on static code tracing of `SmartArtRenderer.cs`, `ShapeRenderer.cs`, and `FormatHandlers.cs`, as well as verification of successful compilation via `cmd /c build.bat`.
- Binary definition files in `sdkjs-master/common/SmartArts/SmartArtData/*.bin` were identified as OnlyOffice reference data assets; their internal binary schema was not reverse-engineered.

---

## 4. Conclusion

- GenOffice has a functional C# 5 compilation pipeline with Open-XML-SDK v3.5.1 integrated.
- The distortion of circle nodes in Cycle SmartArt is caused by anisotropic relative scaling (`rf.Width * bounds.Width` vs `rf.Height * bounds.Height`), unequal default dimensions (0.24 vs 0.18), and lack of a square bounding box clamp in `CreateShapePath("Circle")`.
- Adjacent node gaps stem from the absence of chord/tangency geometric calculations in `PopulateSubShapeBounds` and the omission of a central hub node in `Cycle_Radial`.
- Border parsing for tables only checks `nil`/`none` and omits border style, width, and color. Math is flattened into unicode text without 2D rendering.
- A comprehensive survey report with mathematical formulas and architectural recommendations has been written to `survey_codebase_report.md`.

---

## 5. Verification Method

To verify these findings independently:
1. **Compilation Check**:
   Run `cmd /c build.bat` in `c:\Users\Danh\Desktop\genofficepy`. Verify exit code 0 and output indicating `Microsoft (R) Visual C# Compiler version 4.8.9232.0 for C# 5`.
2. **Circle Distortion Inspection**:
   Inspect `src/ui/ShapeRenderer.cs` lines 894-901 and lines 124-127. Notice `baseW` and `baseH` calculation and `path.AddEllipse(x, y, w, h)`.
3. **SubShape Bounds Inspection**:
   Inspect `src/ui/SmartArtRenderer.cs` lines 274-293. Notice `cardW = 0.24f` and `cardH = 0.18f`.
4. **Math OMML Inspection**:
   Inspect `src/format/FormatHandlers.cs` lines 1594-1604 and lines 3347-3575. Notice unicode string replacement.
5. **Invalidation Conditions**:
   The conclusions would be invalidated if an external rendering engine (e.g. WPF FlowDocument or Direct2D) is discovered that bypasses `ShapeRenderer.cs` during document canvas painting.
