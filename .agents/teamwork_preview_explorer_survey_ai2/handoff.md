# Handoff Report — Explorer AI 2 (Survey & Investigation)

## 1. Observation
- **Build System**:
  - `c:\Users\Danh\Desktop\genofficepy\build.bat` compiles `src\*.cs` using `.NET Framework 4 csc.exe` (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`) into `GenOffice.exe`.
  - Execution of `cmd /c build.bat` returned exit code 0 (`[SUCCESS] GenOffice.exe built successfully!`). Output size: 3,380,736 bytes.
- **Seam Geometry & Painting**:
  - `src/ui/RibbonBar.cs`:
    - Line 1888: `Rectangle topBarRect = new Rectangle(0, 0, width, 36);`
    - Line 1910-1913: `using (Pen btmPen = new Pen(Color.FromArgb(203, 213, 225), 1.0f)) { g.DrawLine(btmPen, 0, 35.5f, width, 35.5f); }`
    - Line 1953: `Rectangle homeRect = new Rectangle(136, 6, 124, 30);` -> `homeRect.Bottom = 36`.
    - Line 2013: `Rectangle docClipRect = new Rectangle(docAreaX, 0, docAreaW, 36);`
    - Line 2032: `Rectangle tabRect = new Rectangle(docTabX, 6, tabWidth, 30);` -> `tabRect.Bottom = 36`.
    - Lines 1699-1711: `CreateTopRoundedTabPath` adds `path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);` and closes the figure.
    - Lines 1973 & 2065: `g.DrawPath(glassBorder, tabPath);` strokes the closed path with `glassBorder = Color.FromArgb(195, 210, 235)`.
    - Lines 2213-2216: `using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(248, 250, 252))) ... g.FillRectangle(headerBrush, 0, 36, width, 28);`
- **Empirical Pixel Sampling**:
  - Sampled directly from bitmap rendered by `Ribbon.Draw(g, 1000, 200)` at X = 320 (middle of active Document Tab):
    - At 100% DPI (Scale 1.0):
      - Y = 35: `R=248, G=250, B=255` (Tab porcelain body)
      - **Y = 36: `R=229, G=235, B=242`** (Darker grey-blue seam line / 1-pixel gap)
      - Y = 37: `R=248, G=250, B=252` (Ribbon Header surface)
    - At 125% DPI (Scale 1.25):
      - PhysY = 44: `R=248, G=250, B=255`
      - **PhysY = 45: `R=233, G=237, B=242`** (1-pixel seam gap)
      - PhysY = 46: `R=248, G=250, B=252`
    - At 150% DPI (Scale 1.50):
      - PhysY = 53: `R=248, G=250, B=255`
      - **PhysY = 54: `R=235, G=239, B=243`** (1-pixel seam gap)
      - PhysY = 55: `R=248, G=250, B=252`
  - Horizontal span: The `(229, 235, 242)` line runs continuously along the entire width of the tab from X = 268 to X = 428.
  - When simulated seam bridge `FillRectangle((248, 250, 252), X+1, 35, W-2, 2)` was tested, pixel row 36 shifted to `R=248, G=250, B=252`, matching Ribbon Header perfectly.

## 2. Logic Chain
1. *From Observation (Lines 1699-1711, 1973, 2065)*: `CreateTopRoundedTabPath` defines a closed contour that includes `path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom)`. Stroking this contour with `glassBorder` (`195, 210, 235`) places an explicit border line on the bottom edge of the active tab at Y = 36.
2. *From Observation (Lines 1910-1913)*: Title Bar Layer 4 draws an unbroken line of `(203, 213, 225)` at `Y = 35.5f` across the entire width `0` to `width`. This line is drawn before the tabs and underlies the active tab region.
3. *From Observation (Line 2013)*: `docClipRect` is set to height 36 (`[0, 36)`). This restricts all tab rendering strictly to Y < 36. Tab fills cannot paint on row 36.
4. *From Observation (Lines 2213-2216)*: `g.FillRectangle(headerBrush, 0, 36, width, 28)` runs after tab rendering. Due to GDI+ `SmoothingMode.HighQuality`, the top boundary edge at Y = 36 is antialiased, blending partial coverage of `headerBrush` (`248, 250, 252`) with the pre-existing Layer 4 stroke (`203, 213, 225`).
5. *From Observations 1-4 & Pixel Sampling*: Because the active tab is clipped at Y = 36, and the Ribbon top edge antialiases over the Layer 4 line, pixel row 36 renders as `(229, 235, 242)` across the entire width of the tab. This produces the visible 1-pixel grey line separating the Active Tab from the Ribbon.
6. *From DPI observations*: Without unifying `tabBottom` and `ribbonTop` through an explicit constraint (`tabHeight = ribbonTop - tabY`), separate integer conversions of `(int)(tabY * scale) + (int)(tabHeight * scale)` drift from `(int)(ribbonTop * scale)` at non-integer scale factors (1.25, 1.50).

## 3. Caveats
- `GenOffice.exe` must not be running in the background when running `build.bat`, as `csc.exe` requires exclusive write access to `GenOffice.exe`.
- The tests were executed against the compiled binary and in-memory GDI+ graphics contexts. Visual appearance on specific monitor hardware with custom GPU driver antialiasing may show slight subpixel variations, which the proposed seamless bridge completely masks.
- No source code was modified during this survey, strictly honoring read-only investigation rules.

## 4. Conclusion
The 1-pixel gap between the document tab bar and Ribbon toolbar is caused by:
1. Closed bottom outline stroked on the active tab (`CreateTopRoundedTabPath`).
2. Global title bar baseline (`btmPen`) running through the active tab at Y = 35.5.
3. Clipping rectangle (`docClipRect`) truncating at Y = 36.
4. HighQuality antialiasing blending at Y = 36 between the Layer 4 line and Ribbon Header top edge.

**Actionable Solution for Task AI 2**:
1. Implement `CreateActiveTabBorderPath(RectangleF rect, float radius)` that leaves the bottom open (no horizontal line at `rect.Bottom`).
2. Expand `docClipRect` to `docClipRect = new Rectangle(docAreaX, 0, docAreaW, ribbonTop + 2)`.
3. Draw a seamless bridge fill `[activeTab.X + 1, ribbonTop - 1, activeTab.Width - 2, 2]` using `Color.FromArgb(248, 250, 252)` across the junction, unifying the active tab and Ribbon surface into a single continuous body.
4. Compute coordinates using unified DPI math: `tabBottom = ribbonTop = (int)Math.Round(36f * dpiScale); tabHeight = tabBottom - tabY;`.
5. Apply identically to both Pinned Home Tab and Document Tabs.

## 5. Verification Method
1. **Compilation Check**:
   ```cmd
   cmd /c build.bat
   ```
   Must succeed with 0 errors and output `[SUCCESS] GenOffice.exe built successfully!`.
2. **Pixel Sampling Verification**:
   Run the verification script in PowerShell:
   ```cmd
   powershell -ExecutionPolicy Bypass -File c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_explorer_survey_ai2\test_seam.ps1
   ```
   Inspect physical pixel colors at Y = 35, 36, 37 across scale factors (1.0, 1.25, 1.50).
   *Success Condition*: Pixel row 36 must transition seamlessly between active tab body and ribbon surface with no darker grey seam (`R >= 248, G >= 250, B >= 252`).
3. **Core Feature Guardrails**:
   Verify `Bài 1.ND GHI.docx` opens with 8 tables intact, table in-cell editing functional, shapes and wrap modes working properly.
