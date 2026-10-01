## 2026-09-21T13:10:50Z
You are the OpenXML Parser & Schema Engineer for GenOffice.
Your working directory is: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m1`.
You MUST read:
1. `c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md` (specifically section `## 2026-09-21T12:58:21Z`)
2. `c:\Users\Danh\Desktop\genofficepy\PROJECT.md`
3. `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_spec_miner_survey_1\survey_schema_report.md`
4. `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_explorer_survey_2\survey_codebase_report.md`

Your exclusive write ownership:
- `src/format/OpenXmlSmartArtModel.cs`
- `src/format/FormatHandlers.cs`
- `src/ui/DocShapeObject.cs`
- `src/ui/DocTableObject.cs`

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Key implementation tasks:
1. STRICT C# 5 COMPATIBILITY: The project compiles with csc.exe 4.8.9232.0 targeting C# 5. DO NOT use C# 6+ features:
   - NO string interpolation `$""` (use `string.Format` or `+`)
   - NO null-conditional `?.` or `?[]`
   - NO pattern matching `is Type var`
   - NO expression-bodied members `=>` for methods/properties
   - NO `out var` inline declarations
   - NO auto-property initializers
2. OpenXML Unit Conversion:
   - Implement conversion formulas in OpenXmlSmartArtModel or FormatHandlers:
     - 1 pt = 12,700 EMUs (`emu / 12700.0f`)
     - 1 px (96 DPI) = 9,525 EMUs (`emu / 9525.0f`)
     - 1 pt = 20 Twips/dxa (`twips / 20.0f`)
     - 1 px = 15 Twips/dxa (`dxa / 15.0f`)
     - Border size: 1 pt = 8 eighths of pt (`sz / 8.0f`)
3. Upgrade `src/format/OpenXmlSmartArtModel.cs`:
   - Add structures for EMU original bounds (`OriginalBoundsEmu`), 1:1 AspectRatioLock flag (automatically set for Cycle/Radial), and resolved theme color palette.
   - Update `FromDocShapeObject` and `ApplyToDocShapeObject` to synchronize these properties.
4. Upgrade `src/format/FormatHandlers.cs`:
   - DrawingML Diagrams: In `TryParseSmartArtDiagram` and RenderDirectDrawingMLDiagram:
     - Parse `drawing1.xml` and `data1.xml` with exact EMU scaling.
     - Retain connector relationships `<a:cxnSp>` and circular arrows so connector geometry is preserved.
     - Provide `AspectRatioLock` flag when layout is Cycle or Radial.
   - Table Borders: In `TryParseDocxTable` and related border parsing:
     - Parse `w:tblBorders` and `w:tcBorders`: parse styles (`single`, `double`, `dashed`, `dotted`, `threeDEmboss`, `threeDEngrave`, `wave`), size in eighths of pt (`w:sz / 8.0f`), and color (`w:color`).
     - Store these on `DocTableCell` (e.g. `BorderTopStyle`, `BorderTopWidthPt`, `BorderTopColor`, etc.) and `DocTableObject`.
   - Math OMML: In `ExtractMathNodeRecursive` and equation handling:
     - Enhance parsing of `m:oMath` and `m:oMathPara` so fractions, radicals, scripts, delimiters, and operators are faithfully represented with proper Unicode/mathematical symbols and layout parameters.
5. Verification:
   - Run `build.bat` in the workspace root `c:\Users\Danh\Desktop\genofficepy` and verify clean compilation (0 errors, 0 warnings).
   - Document all changes in `changes.md`, write your `handoff.md` and `progress.md` in your working directory. Send a message to the orchestrator when completed.
