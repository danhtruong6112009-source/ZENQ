# Changes Report — Milestone 1: OpenXML Parser & Schema Upgrades

## 1. Overview of Changes
This milestone implemented full OpenXML unit conversions, DrawingML Diagram (SmartArt) EMU scaling & connector preservation, Table borders styling/widths/colors, and OMML Math parsing in GenOffice, adhering strictly to C# 5 compatibility.

---

## 2. Modified Files Detail

### 1. `src/ui/DocTableObject.cs`
- **DocTableCell**:
  - Added properties: `BorderTopStyle`, `BorderTopWidthPt`, `BorderTopColor`, `BorderBottomStyle`, `BorderBottomWidthPt`, `BorderBottomColor`, `BorderLeftStyle`, `BorderLeftWidthPt`, `BorderLeftColor`, `BorderRightStyle`, `BorderRightWidthPt`, `BorderRightColor`.
  - Initialized default values in `DocTableCell` constructor (`"single"`, `0.5f`, `Color.FromArgb(208, 215, 222)`).
- **DocTableObject**:
  - Added table-level border properties: `BorderTopStyle`, `BorderTopWidthPt`, `BorderTopColor` (and Bottom, Left, Right, InsideH, InsideV).
  - Added table-level border visibility flags: `HasBorderTop`, `HasBorderBottom`, `HasBorderLeft`, `HasBorderRight`, `HasBorderInsideH`, `HasBorderInsideV`.
  - Initialized default values in `DocTableObject` constructor.

### 2. `src/ui/DocShapeObject.cs`
- **DocShapeTagProperties**:
  - Added `OriginalBoundsEmu` (Rectangle), `AspectRatioLock` (bool), `ResolvedThemePalette` (Color[]).
- **DocShapeObject**:
  - Added properties: `OriginalBoundsEmu`, `AspectRatioLock`, `ResolvedThemePalette`.
  - Updated `ApplyCachedTagMetadata()` to apply `OriginalBoundsEmu`, `AspectRatioLock`, and `ResolvedThemePalette` when restoring cached shapes.
  - Updated `ParseNodesMeta()`: parsed the 6th token (`vals[5]`) into `NodeShapeTypes[i]` to preserve vector shape geometries across previews and serialization.
  - Updated `ParseShapeTag()`: added parsing for keys `:ARLOCK=`, `:EMUX=`, `:EMUY=`, `:EMUW=`, `:EMUH=`.

### 3. `src/format/OpenXmlSmartArtModel.cs`
- **OpenXmlUnitConverter**:
  - Implemented exact static conversion utilities:
    - `EmuToPoints` (1 pt = 12,700 EMU) and `PointsToEmu`
    - `EmuToPixels` (1 px = 9,525 EMU at 96 DPI) and `PixelsToEmu`
    - `TwipsToPoints` (1 pt = 20 twips/dxa) and `PointsToTwips`
    - `TwipsToPixels` (1 px = 15 twips at 96 DPI) and `PixelsToTwips`
    - `BorderSzToPoints` (1 pt = 8 eighths of a pt) and `PointsToBorderSz`
    - `DrawingMLAngleToDegrees` (60,000 units per degree) and `DegreesToDrawingMLAngle`
- **OpenXmlSmartArtModel**:
  - Added properties: `OriginalBoundsEmu`, `AspectRatioLock`, `ResolvedThemePalette`.
  - Added `IsCycleOrRadialLayout(string layoutType)` helper method.
  - Added `LoadFromDocShapeObject` and `PopulateFromDocShapeObject`.
  - Updated `FromDocShapeObject` and `ApplyToDocShapeObject` overloads to synchronize EMU bounds, theme palette, and auto-lock aspect ratio for Cycle/Radial layouts.

### 4. `src/format/FormatHandlers.cs`
- **DrawingML Diagram (SmartArt)**:
  - Upgraded `RenderDirectDrawingMLDiagram`:
    - Resolved theme color palette from `theme1.xml` (including `sysClr`).
    - Handled connector shapes (`cxnSp`) and anchors (`stCxn`, `endCxn`).
    - Rendered `circularArrow` and `leftCircularArrow` using `SmartArtRenderer.CreateCircularArrowPath`.
    - Output `outNodesMeta` with geometry type in the 6th position.
    - Output `outPalette` and `outEmuBounds`.
  - Upgraded `TryParseSmartArtDiagram` with overload returning `aspectRatioLock`, `origBoundsEmu`, and `resolvedPalette`, with fallbacks to `wp:extent` and standard Office palette.
  - Updated `ExtractDrawingImage`: assigned `aspectRatioLock`, `origBoundsEmu`, and `resolvedPalette` to `saObj` and `DocShapeTagProperties`, and formatted Markdown shape tag with `:ARLOCK=`, `:EMUX=`, `:EMUY=`, `:EMUW=`, `:EMUH=`.
- **Table Borders**:
  - Added `ParseBorderAttributes(XmlNode borderNode, out bool hasBorder, out string style, out float widthPt, out Color color, Color defaultColor)` helper method.
  - Upgraded `ParseTableNode`: extracted `w:tblBorders` and `w:tcBorders` styles, eighths-of-a-point widths (`w:sz / 8.0f`), and hex/auto colors, populating `DocTableObject` and `DocTableCell`.
- **Math OMML**:
  - Upgraded `ToSuperscript` and `ToSubscript` with full digit, latin alphabet, and math symbols support.
  - Upgraded `ExtractMathNodeRecursive`:
    - Handled vulgar fractions (`½`, `¼`, `¾`, `⅓`, `⅔`, `⅕`, `⅖`, `⅗`, `⅘`, `⅙`, `⅚`, `⅛`, `⅜`, `⅝`, `⅞`) and complex fraction parenthesization.
    - Handled radical degree hide (`m:degHide`), square root `√`, cube root `∛`, fourth root `∜`, n-th root, and composite radicands.
    - Handled delimiters (`m:d`) with custom `begChr`, `endChr`, and `sepChr`.
    - Handled n-ary operators (`m:nary`) with bounds and integrand.
    - Handled matrices (`m:m`) row and column parsing.
    - Handled lower and upper limits (`m:limLow`, `m:limUpp`).
    - Handled accents (`m:acc`), overbar/underbar (`m:bar`), border boxes (`m:borderBox`), and equation arrays (`m:eqArr`).
    - Set math run font to standard `Cambria Math`.

---

## 3. Verification Commands and Results
1. Project Build:
   - Command: `cmd.exe /c "build.bat"`
   - Result: `[SUCCESS] GenOffice.exe built successfully!` (0 errors).
2. Verification Suite:
   - Command: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:test_milestone1_verification.exe /r:GenOffice.exe scratch\test_milestone1_verification.cs && test_milestone1_verification.exe && del test_milestone1_verification.exe`
   - Result: `Results: 53 passed, 0 failed.`
