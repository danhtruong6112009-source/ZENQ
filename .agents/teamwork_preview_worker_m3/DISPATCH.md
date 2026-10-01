## 2026-09-21T13:31:35Z
You are the Canvas Graphic & Vector Rendering Engineer for GenOffice.
Your working directory is: `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m3`.
You MUST read:
1. `c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md` (specifically section `## 2026-09-21T12:58:21Z`)
2. `c:\Users\Danh\Desktop\genofficepy\PROJECT.md`
3. `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_spec_miner_survey_1\survey_schema_report.md`
4. `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_explorer_survey_2\survey_codebase_report.md`
5. `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_m1\changes.md`

Your exclusive write ownership:
- `src/ui/SmartArtRenderer.cs`
- `src/ui/ShapeRenderer.cs`
- `src/ui/WriterView.cs`

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
2. Perfect 1:1 Circle Geometry (Zero Distortion):
   - In `src/ui/SmartArtRenderer.cs`:
     - Update `PopulateSubShapeBounds`:
       For all Cycle, Radial, and Ring layouts (`Cycle_BasicPie`, `Cycle_Radial`, `Cycle_BlockLoop`, `Cycle_Segmented`, `Cycle_Gear`, `Cycle_CircleArrow`, etc.):
       - Calculate uniform radius: `float minDim = Math.Min(bounds.Width, bounds.Height); float pixelRad = Math.Max(30f, Math.Min(300f, minDim * 0.32f));`
       - For `Cycle_Radial`: The FIRST node ($i=0$) MUST be the Central Core Hub! Position it at relative center $(0.5f, 0.5f)$ with diameter $2 \times R_{\text{core}}$ where $R_{\text{core}} = pixelRad \times 0.45f$.
       - Arrange satellite nodes ($i=1..count-1$) on the orbit circle: $R_{\text{orbit}} = R_{\text{core}} + r_{\text{sat}} + \Delta_{\text{gap}}$.
       - Enforce 1:1 square bounding boxes for all circular satellite nodes ($cardW_{\text{px}} = cardH_{\text{px}} = 2 \times r_{\text{sat}}$), so that when scaled back on canvas they are 100% perfectly round!
     - In drawing functions (`DrawCycleRadial`, `DrawCycleCircleArrow`, `DrawCycleSegmented`, `DrawCycleBlockLoop`, `DrawCycleBasicPie`):
       - Eliminate anisotropic radii (`rx = r.Width * 0.33f` vs `ry = r.Height * 0.33f`).
       - Use uniform radius `rad = Math.Min(r.Width, r.Height) * 0.32f;` and draw circles using `(cx - rad, cy - rad, rad * 2, rad * 2)`.
   - In `src/ui/ShapeRenderer.cs`:
     - In `CreateShapePath`:
       For `"Circle"`, or when drawing circular nodes with `shape.AspectRatioLock`:
       Clamp to minimum dimension: `int dim = Math.Min(w, h); int nx = x + (w - dim) / 2; int ny = y + (h - dim) / 2; path.AddEllipse(nx, ny, dim, dim);`.
     - In `DrawShape`:
       In the sub-shapes loop (lines 892-901):
       When `shape.AspectRatioLock` is true or node shape is `"Circle"`/`"Oval"`:
       Use isotropic base dimension `int baseDim = Math.Min(baseW, baseH); int nw = Math.Max(6, (int)(baseDim * (1f + scaleDelta))); int nh = nw; int nx = bounds.X + (int)(rf.X * bounds.Width) + (baseW - nw) / 2; int ny = bounds.Y + (int)(rf.Y * bounds.Height) + (baseH - nh) / 2; Rectangle nodeRect = new Rectangle(nx, ny, nw, nh);`.
3. Seamless Outer Attachment & Tangential Snapping:
   - Calculate outer attachment distance so satellite nodes seamlessly dock to the central circle perimeter with zero unwanted gap or overlap.
   - For circular arrows and connecting lines, ensure they anchor directly to the tangent contact points.
4. Multi-Style Borders & Dynamic Theme Colors:
   - In `SmartArtRenderer.cs`:
     - If `shape.ResolvedThemePalette != null && shape.ResolvedThemePalette.Length > 0`, use the resolved theme colors dynamically instead of fixed presets.
     - Implement border styles: solid, dashed, double line (`Pen` compound or double stroke), 3D bevel / drop shadow.
   - In `WriterView.cs`:
     - In table rendering: render table and cell borders with their parsed styles (`single`, `double`, `dashed`, `dotted`, `threeDEmboss`, `threeDEngrave`), width in points, and colors.
5. Compilation & Verification:
   - Run `build.bat` in the workspace root `c:\Users\Danh\Desktop\genofficepy` and verify clean compilation (0 errors).
   - Write a test verification script in `scratch/` to verify that rendered circular bounds have aspect ratio == 1.000 ± 0.005.
   - Document changes in `changes.md`, write `handoff.md` and `progress.md` in your working directory. Send a message to the orchestrator when completed.
