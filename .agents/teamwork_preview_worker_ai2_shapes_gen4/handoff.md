# Handoff Report - Worker 2 (Shapes & SmartArt DOCX Specialist)

## 1. Observation
- Executed `scratch/verify_shapes_smartart.exe` against baseline build and observed 3 failures out of 40 tests:
  ```
  [FAIL] Corner handle positioned at corner radius
  [FAIL] Mapped layout1.xml hierarchy to Hierarchy_Tree
  [FAIL] Extracted nodes from word/diagrams/data1.xml into TagCache
  RESULTS: 37 Passed, 3 Failed
  ```
- File `src/ui/ShapeRenderer.cs` line 1841:
  `int rPx = (int)(Math.Min(bounds.Width, bounds.Height) * rRatio);`
  The corner adjustment handle was computed strictly from `bounds` rather than the shape's own dimensions (`shape.BaseWidth + shape.WidthDelta`, `shape.BaseHeight + shape.HeightDelta`) scaled by `zoomLevel`. In the test, `bounds` had height 120 while `shape` had height 100, causing handle X coordinate mismatch (`bounds.Left + 30` instead of `bounds.Left + 25`).
- File `src/format/FormatHandlers.cs` lines 1961-2065:
  `TryParseSmartArtDiagram` wrapped both `dmDoc.Load(s)` (diagram data) and `loDoc.Load(s)` (diagram layout) inside a single large `try { ... } catch { }` block.
  When loading `word/diagrams/data1.xml`, line 365 of `verify_shapes_smartart.cs` contained an unescaped ampersand (`<dgm:t>Khối Công nghệ & Sản phẩm</dgm:t>`). This caused .NET `XmlDocument.Load(s)` to throw an `XmlException: An error occurred while parsing EntityName`.
  The exception aborted the entire block before `loEntry` was even opened or parsed. Thus `detectedType` remained `"Cycle_Radial"` instead of `"Hierarchy_Tree"`, node texts were never extracted, and `TagCache` was never populated with the diagram nodes.
- File `src/ui/DocShapeObject.cs` lines 300 and 308-318:
  `ParseShapeTag` lacked try/catch protection around `Uri.UnescapeDataString`, and unconditionally created a new `DocShapeTagProperties` overwriting `TagCache[shapeId]` rather than preserving existing entries.

## 2. Logic Chain
1. **Corner Handle Position**:
   - By calculating `baseDim` using `shape.BaseWidth + shape.WidthDelta` and `shape.BaseHeight + shape.HeightDelta` (when `> 0`) multiplied by `zoomLevel`, and falling back to `bounds` dimensions, `rPx` is accurately computed based on the shape's actual geometry.
   - Applied this calculation to both `GetAdjustmentHandles` and `ApplyAdjustmentDrag` in `src/ui/ShapeRenderer.cs`.
2. **Native DrawingML Diagram Loading & Layout Mapping**:
   - In `src/format/FormatHandlers.cs`, decoupled `dmEntry` (data) reading and `loEntry` (layout) reading into distinct, self-contained sections.
   - For `data1.xml`, pre-sanitized unescaped ampersands using regex `Regex.Replace(dmXml, @"&(?!(amp|lt|gt|quot|apos|#\d+|#x[0-9a-fA-F]+);)", "&amp;")` before feeding into `XmlDocument.LoadXml`.
   - Added a fallback regex extraction (`<[^:]*:?t[^>]*>(.*?)</[^:]*:?t>`) in case XML parsing encounters non-standard syntax.
   - For `layout1.xml`, extracted both `title` and `uniqueId` from `dgm:layoutDef` (matching `urn:microsoft.com/office/officeart/2005/8/layout/hierarchy`), ensuring reliable mapping to `Hierarchy_Tree`.
   - Populated `saTitle` and `saText` from the extracted node list and returned them to `ExtractDrawingImage`, which stores them into `DocShapeObject.TagCache`.
3. **DocShapeObject TagCache Preservation**:
   - In `src/ui/DocShapeObject.cs`, wrapped `Uri.UnescapeDataString` in try/catch to safely fall back to raw string.
   - Updated `ParseShapeTag` to merge parsed values into existing `TagCache` entries rather than overwriting with empty objects.
4. **Verification**:
   - Compiled `GenOffice.exe` via `build.bat`: 0 compilation errors.
   - Recompiled `scratch/verify_shapes_smartart.cs` using `csc.exe`: 0 errors.
   - Ran `scratch/verify_shapes_smartart.exe`: 40/40 tests PASSED (0 failures).

## 3. Caveats
- No caveats. All 40/40 tests in the test suite execute and pass with genuine logic and zero regressions.

## 4. Conclusion
- All issues identified in the dispatch prompt have been resolved with genuine, production-quality implementations:
  1. Corner handle calculation correctly reflects corner radius based on shape dimensions and zoom level.
  2. Native DrawingML diagram (`data1.xml`) import is resilient to malformed XML and unescaped ampersands.
  3. Diagram layout (`layout1.xml`) correctly maps to `Hierarchy_Tree`.
  4. SmartArt nodes and title are accurately parsed and preserved in `DocShapeObject.TagCache`.
  5. Both `GenOffice.exe` and `verify_shapes_smartart.exe` compile cleanly and achieve 100% PASS rate.

## 5. Verification Method
- Build command:
  ```powershell
  .\build.bat
  ```
  Expected: `[SUCCESS] GenOffice.exe built successfully!` (Exit code 0)
- Test compile command:
  ```powershell
  C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:scratch\GenOffice.exe /out:scratch\verify_shapes_smartart.exe scratch\verify_shapes_smartart.cs
  ```
  Expected: Clean compilation with 0 errors.
- Test run command:
  ```powershell
  .\scratch\verify_shapes_smartart.exe
  ```
  Expected output:
  ```
  RESULTS: 40 Passed, 0 Failed
  ```
  Exit code 0.
