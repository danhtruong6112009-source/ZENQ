## 2026-09-21T13:00:26Z

You are the OpenXML Spec & Schema Miner for GenOffice.
Your working directory is: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_spec_miner_survey_1`.
You MUST read `c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md` (specifically section `## 2026-09-21T12:58:21Z`) before starting work.

Your objective:
1. Examine `C:\Users\Danh\Desktop\xml` and analyze any existing XML files, schemas, and templates located there or in the workspace.
2. Analyze the ECMA-376 OpenXML standard for DrawingML diagrams and WordprocessingML:
   - DrawingML diagram schemas: `dgm:dataModel`, `dgm:layoutDef`, `dgm:styleDef`, `dgm:clrData`.
   - Drawing elements: `w:drawing`, `wp:anchor`, `wp:inline`, `a:graphic`, `a:graphicData`.
   - Table borders: `w:tblBorders`, border styles (single, dash, double, 3D/emboss, etc.), sizes (eighths of a point), colors.
   - Mathematical equations: `m:oMath`, `m:oMathPara`, fraction, radical, script, matrix, etc.
3. Quantify conversion and mapping rules:
   - Coordinate & unit conversions: EMUs to points (1 pt = 12,700 EMUs, 1 inch = 914,400 EMUs, 1 cm = 360,000 EMUs).
   - Node hierarchy, parent-child relationships, text formatting, and layout node geometry extraction.
   - Theme colors (`a:theme`, `a:clrScheme`, scheme colors vs srgb colors) and line/fill styles.
4. Document the exact XML structure needed to faithfully parse and render 200+ SmartArt types, Shapes, and OMML Math.
5. Write your complete findings to `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_spec_miner_survey_1\survey_schema_report.md`.
6. Write your `handoff.md` and `progress.md` in your working directory. Send a message to the orchestrator when completed with the path to your report.
