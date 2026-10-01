## 2026-09-18T13:59:12Z

You are AI 2 (Worker: Shape Library Overhaul & SmartArt DOCX Specialist) for GenOffice.
Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2_shapes
Workspace root: c:\Users\Danh\Desktop\genofficepy
Original request path: c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md (Read section ## 2026-09-18T13:52:11Z)
Scope document: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_orchestrator_3\PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

EXCLUSIVE WRITE OWNERSHIP:
- src/ui/ShapesGalleryDropDownForm.cs
- src/ui/ShapeRenderer.cs
- src/ui/DocShapeObject.cs
- src/ui/SmartArtRenderer.cs
- src/ui/SmartArtGalleryForm.cs
- src/ui/SmartArtEditorDialog.cs
- src/format/FormatHandlers.cs
Do NOT write to files owned by AI 1 (WriterView.cs, RibbonBar.cs, AppForm.cs) unless strictly necessary for minimal interface binding that does not conflict with Undo/Redo.

DETAILED ASSIGNMENT:
1. Shape Picker Dropdown Overhaul (ShapesGalleryDropDownForm.cs & Ribbon):
   - Completely remove non-standard categories (Botanical & Nature, Luxury Badges & Jewels).
   - Build all 9 standard MS Office / OnlyOffice groups with crisp vector monoline icons:
     1. Đã dùng gần đây (Recently Used Shapes)
     2. Đường thẳng (Lines): Line, Arrow, Double Arrow, Elbow Connector, Curved Connector...
     3. Hình chữ nhật (Rectangles): Rectangle, Rounded Rectangle, Single Corner Snipped, Snipped and Rounded, etc.
     4. Hình dạng cơ bản (Basic Shapes): Text Box [A], Oval, Triangle, Right Triangle, Parallelogram, Trapezoid, Diamond, Pentagon, Hexagon, Octagon, Decagon, Cross, Cylinder, Cube, Donut, Heart, Lightning, Arc, Moon, Sun, Cloud, Teardrop, etc.
     5. Mũi tên khối (Block Arrows): Right, Left, Up, Down, Left-Right, Up-Down, Quad Arrow, Bent, U-Turn, Chevron, etc.
     6. Toán học / Phương trình (Equation Shapes): Cộng (+), Trừ (-), Nhân (×), Chia (÷), Bằng (=), Khác (≠).
     7. Lưu trình (Flowchart): Process, Decision, Data, Terminator, Document, Preparation, etc.
     8. Sao và Biểu ngữ (Stars and Banners): Star 4/5/6/8/10/12/16/24/32, Explosion 1/2, Horizontal Scroll, Vertical Scroll, Ribbon Up, Ribbon Down.
     9. Chú thích (Callouts): Rectangular Callout, Rounded Callout, Oval Callout, Cloud Callout, Line Callouts.
2. Shape Vector Rendering & In-Shape Text Editing (ShapeRenderer.cs, DocShapeObject.cs):
   - Render clean, elegant monoline vectors with accurate mathematical proportions and smooth anti-aliasing.
   - Text inside shape: Support typing and rendering text inside shapes (TextContent, font family, font size, text color, horizontal alignment [Left, Center, Right], vertical alignment [Top, Middle, Bottom], and WordWrap).
   - Interactive handles: Rotation handle (top circular green/blue grip), 8 sizing handles, and yellow adjustment handles (adjusting corner radius of rounded rect, arrow neck width/head size, callout tail position).
3. SmartArt DOCX Import/Export Fidelity (FormatHandlers.cs, SmartArtRenderer.cs):
   - DOCX Import:
     - Read and parse SmartArt diagrams from DOCX (whether drawingml dgm:relIds / diagram drawings or shape fallbacks).
     - Map correctly to standard diagrams: Cycle (radial cycle), Linear Process, Hierarchy Tree / Org Chart, Block List.
     - Extract text content, titles, and nodes, creating corresponding DocShapeObject / SmartArt elements in the document without crashing or disappearing.
   - DOCX Export:
     - When saving to DOCX in DocxHandler.SaveDocxFile, serialize SmartArt shapes into valid OpenXML drawingml structures or compliant vector fallback representations.
     - Ensure that when the exported .docx is opened in Microsoft Word, it opens 100% cleanly without corruption errors, "Word found unreadable content", or recovery warnings.

VERIFICATION REQUIREMENTS:
1. Build the application using build.bat or csc.exe. Compilation MUST succeed with 0 errors.
2. Write a verification test script (e.g. verify_shapes_smartart.ps1) or automated C# test that validates:
   - ShapesGalleryDropDownForm has exactly the 9 standard groups and 0 botanical/luxury groups.
   - Shapes render with crisp vector graphics, handles, and support text inside shapes.
   - Importing a DOCX with SmartArt preserves diagram nodes and text.
   - Exporting a DOCX with SmartArt creates a valid DOCX package that passes OpenXML validation.
3. Record progress in c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2_shapes\progress.md.
4. When finished, write c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai2_shapes\handoff.md and report back to parent with send_message.

## 2026-09-18T14:21:06Z

**Context**: Liveness check & status query.
**Content**: It has been 20 minutes since dispatch. Please report your current progress, whether you encountered any obstacles, and update your progress.md with your latest status and timestamp.
**Action**: Reply with your current status and update progress.md.
