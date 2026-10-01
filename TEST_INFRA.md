# E2E Test Infrastructure: ZenQ Calc 4-Phase Upgrade

## 1. Test Philosophy & Core Principles
- **Opaque-Box & Requirement-Driven**: Tests treat ZenQ Calc as a black/opaque box against official specifications: Microsoft Excel 365 calculation norms, ECMA-376 OpenXML spreadsheet schemas, OpenDocument OASIS standard (ODS v1.2), Adobe PDF 1.7 vector specifications, and Microsoft Compound File Binary Format (BIFF8).
- **Authoritative Oracles & Ground Truth**:
  1. *Excel 365 Reference Results*: Every formula return value, precision rule, and error token (`#N/A`, `#VALUE!`, `#REF!`, `#DIV/0!`, `#NUM!`, `#NAME?`, `#SPILL!`) is derived directly from Microsoft Excel 365 ground-truth evaluation.
  2. *DrawingML & Coordinate Geometry*: 1 Pixel = 9,525 EMUs (English Metric Units). Shapes, pictures, and charts must preserve exact bounding boxes, anchor offsets, and relationship IDs.
  3. *Mathematical & Algorithmic Invariants*: Convergence to within 0.001 for iterative solvers (Goal Seek); exact combinatorial sensitivity grids for Data Tables; deterministic sorting and deduplication.
- **Strict Compiler Strictness**: Tests strictly comply with **C# 5.0** as enforced by `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`. Prohibited: string interpolation `$"..."`, expression-bodied members `=>`, null-conditional `?.`, local functions, pattern matching, tuple syntax, and inline `out var`.
- **Progressive Testability & Dual-Track Execution**: Tests are organized across all 32 inventoried features across Phases 1-4. Active milestone implementations execute synchronously, while upcoming milestones are verified against interface contracts without blocking suite compilation.

---

## 2. Test Methodology & 4-Tier Hierarchy
The suite implements a four-tier testing hierarchy guaranteeing at least **11 test cases per feature** across all 32 features (Grand Total: **≥ 352 test cases**):

```
+---------------------------------------------------------------------------------+
|                       ZENQ CALC 4-TIER TEST SUITE                               |
+---------------------------------------------------------------------------------+
| Tier 1: Category-Partition Feature Tests (>= 5 per feature, Total >= 160)       |
|   - Equivalence class partitioning of valid inputs, typical use cases, formats  |
+---------------------------------------------------------------------------------+
| Tier 2: Boundary Value Analysis (BVA) (>= 3 per feature, Total >= 96)           |
|   - Min/max limits, empty strings, 0, null, extremes, division by zero, limits  |
+---------------------------------------------------------------------------------+
| Tier 3: Pairwise Combinatorial Tests (>= 2 per feature, Total >= 64)            |
|   - Cross-feature multi-subsystem interaction (e.g. Tables + Formulas + Arrays) |
+---------------------------------------------------------------------------------+
| Tier 4: Real-World Workload Scenarios (>= 1 per feature, Total >= 32)           |
|   - Multi-step realistic enterprise workflows (Financial, Sales, Analytics)   |
+---------------------------------------------------------------------------------+
```

---

## 3. Feature Inventory & Test Coverage Matrix

Every feature from `PROJECT.md § Feature Inventory` is mapped below with its minimum coverage threshold across all 4 tiers:

| # | Feature | Milestone | Source | Tier 1 (Feature) | Tier 2 (Boundary) | Tier 3 (Cross) | Tier 4 (Scenario) | Total |
|---|---------|-----------|--------|:----------------:|:-----------------:|:--------------:|:-----------------:|:-----:|
| 1 | OpenXML Drawings Parser | M1 | Req §1.1 | 5 | 3 | 2 | 1 | 11 |
| 2 | Drawings EMU Coordinate Transform | M1 | Survey 2 | 5 | 3 | 2 | 1 | 11 |
| 3 | Visual Assets Roundtrip | M1 | Req §1.1 | 5 | 3 | 2 | 1 | 11 |
| 4 | Native PDF Export Engine | M1 | Req §1.2 | 5 | 3 | 2 | 1 | 11 |
| 5 | PDF Styling & Gridlines | M1 | Req §1.2 | 5 | 3 | 2 | 1 | 11 |
| 6 | Find & Replace Dialog | M1 | Req §1.3 | 5 | 3 | 2 | 1 | 11 |
| 7 | Remove Duplicates Modal | M1 | Req §1.3 | 5 | 3 | 2 | 1 | 11 |
| 8 | Text to Columns Wizard | M1 | Req §1.3 | 5 | 3 | 2 | 1 | 11 |
| 9 | Formulas: Lookup & Reference | M2 | Req §2.1 | 5 | 3 | 2 | 1 | 11 |
| 10 | Formulas: Logical & Error | M2 | Req §2.1 | 5 | 3 | 2 | 1 | 11 |
| 11 | Formulas: Date & Time | M2 | Req §2.1 | 5 | 3 | 2 | 1 | 11 |
| 12 | Formulas: Text | M2 | Req §2.1 | 5 | 3 | 2 | 1 | 11 |
| 13 | Formulas: Math & Statistical | M2 | Req §2.1 | 5 | 3 | 2 | 1 | 11 |
| 14 | Formulas: Information | M2 | Req §2.1 | 5 | 3 | 2 | 1 | 11 |
| 15 | ODS Reader & Writer | M2 | Req §2.2 | 5 | 3 | 2 | 1 | 11 |
| 16 | XLS BIFF8 Binary Parser | M2 | Req §2.2 | 5 | 3 | 2 | 1 | 11 |
| 17 | Web & Delimited Formats | M2 | Req §2.2 | 5 | 3 | 2 | 1 | 11 |
| 18 | Excel Tables (ListObject) Core | M3 | Req §3.1 | 5 | 3 | 2 | 1 | 11 |
| 19 | Table Dynamic Auto-Expansion | M3 | Req §3.1 | 5 | 3 | 2 | 1 | 11 |
| 20 | Table Total Row Aggregations | M3 | Req §3.1 | 5 | 3 | 2 | 1 | 11 |
| 21 | Structured References | M3 | Req §3.1 | 5 | 3 | 2 | 1 | 11 |
| 22 | OpenXML Table Persistence | M3 | Survey 3 | 5 | 3 | 2 | 1 | 11 |
| 23 | Dynamic Array Evaluation | M3 | Req §3.2 | 5 | 3 | 2 | 1 | 11 |
| 24 | Spill Collision & `#SPILL!` | M3 | Req §3.2 | 5 | 3 | 2 | 1 | 11 |
| 25 | Array & Scoping Functions | M3 | Req §3.2 | 5 | 3 | 2 | 1 | 11 |
| 26 | PivotTable Engine & Cache | M4 | Req §4.1 | 5 | 3 | 2 | 1 | 11 |
| 27 | PivotTable Field List Pane | M4 | Req §4.1 | 5 | 3 | 2 | 1 | 11 |
| 28 | Pivot Grouping & Linked Charts | M4 | Req §4.1 | 5 | 3 | 2 | 1 | 11 |
| 29 | Goal Seek Iterative Solver | M4 | Req §4.2 | 5 | 3 | 2 | 1 | 11 |
| 30 | Data Tables Sensitivity Engine | M4 | Req §4.2 | 5 | 3 | 2 | 1 | 11 |
| 31 | Workbook Structure Protection | M4 | Req §4.3 | 5 | 3 | 2 | 1 | 11 |
| 32 | Print Titles & Page Breaks | M4 | Req §4.3 | 5 | 3 | 2 | 1 | 11 |
| **TOTAL** | **32 Core Features** | **Phases 1-4** | | **160** | **96** | **64** | **32** | **352** |

---

## 4. Test Harness Architecture & Layout

```
c:\Users\Danh\Desktop\genofficepy\
├── tests\
│   ├── TestHarness.cs              # TestRunner, Assertions, Attributes, Dynamic Invocation, Tier Reporting
│   ├── Tier1FeatureTests.cs        # Tier 1 Category-Partition Feature Tests (160 tests)
│   ├── Tier2BoundaryTests.cs       # Tier 2 Boundary Value Analysis Tests (96 tests)
│   ├── Tier3CrossFeatureTests.cs   # Tier 3 Pairwise Combinatorial Tests (64 tests)
│   ├── Tier4RealWorldTests.cs      # Tier 4 Real-World Enterprise Workload Scenarios (32 tests)
│   └── run_tests.bat               # Subdirectory runner script
├── run_tests.bat                   # Top-level one-click build and execution script
├── TEST_INFRA.md                   # This infrastructure design document
└── TEST_READY.md                   # Readiness summary and coverage breakdown
```

### Key Technical Characteristics of `TestHarness.cs`
1. **Zero External Framework Dependency**: No NUnit or MSTest required. Fully self-contained C# 5 console application compiling directly with .NET 4.8 `csc.exe`.
2. **Assembly Resolution Hook**: Automatically registers `AppDomain.CurrentDomain.AssemblyResolve` to resolve `GenOffice.exe`, `DocumentFormat.OpenXml.dll`, `PdfSharp.dll`, and `PdfiumViewer.dll` across root and test directories.
3. **Structured Reporting**:
   - Live colorized console output with Tier and Feature grouping.
   - Comprehensive Tier Breakdown table (Total, Passed, Failed, Pending).
   - Milestone Status table (M1, M2, M3, M4).
   - Detailed failure diagnostic including call site and expected vs actual values.
4. **Exit Codes**:
   - `0`: All executed tests passed without regressions.
   - `1`: One or more test assertions failed (implementation defect).

---

## 5. Tier Breakdown & Methodology Specifications

### Tier 1: Category-Partition Feature Testing
- **Goal**: Validate core functional requirements under normal, representative operating conditions for each of the 32 features.
- **Partitioning Strategy**:
  - Formulas: Test standard operand types (literal values, direct cell references, 2D ranges, nested function composition).
  - File Handlers: Validate valid format loading, correct sheet count, cell value recovery, and style preservation.
  - Dialogs & Modals: Validate state initialization, option configuration, execution against target matrices, and clean cancellation.
  - Tables & Dynamic Arrays: Validate creation, header generation, range expansion, and array spilling into empty cells.
  - Analytics: Validate 1-variable/2-variable sensitivity grid generation, Goal Seek root convergence, and PivotTable multi-dimensional summarization.

### Tier 2: Boundary Value Analysis (BVA)
- **Goal**: Probe edge conditions, resource boundaries, limit behaviors, and failure modes across all 32 features.
- **Boundary Conditions Tested**:
  - Extreme coordinates: row 0, row 1,048,575, column 0, column 16,383 (`XFD`).
  - Empty inputs: 0-length strings `""`, whitespace-only strings `"   "`, null pointers, empty sheets, 0-row selections.
  - Mathematical singularities: division by zero (`10 / 0`), square root of negative numbers (`SQRT(-1)`), logarithm of zero, overflow/underflow.
  - Off-by-one errors: ranges ending at table boundaries, single-cell selections, single-row tables.
  - File format corruption: truncated ZIP archives, invalid BIFF8 stream markers, missing XML namespaces, missing `_rels`.
  - Spill collision: spilling onto non-empty cells triggering `#SPILL!`, clearing obstacle triggering automatic unblock.

### Tier 3: Pairwise Combinatorial & Cross-Feature Integration
- **Goal**: Verify robustness when two or more distinct subsystems interact simultaneously.
- **Selected Interaction Pairs**:
  1. *Tables + Formulas + Structured References*: Using `Table1[@Sales]` within nested `IFS` and `SUM` calculations.
  2. *OpenXML Drawings + Page Setup + PDF Export*: Exporting sheets containing floating shapes and charts to PDF with landscape orientation and repeating print titles.
  3. *Dynamic Arrays + Spilling + ListObject Tables*: Array formula spilling adjacent to structured tables without collisions.
  4. *ODS/XLS Import + Find/Replace + Formula Re-evaluation*: Ingesting legacy BIFF8 or ODS files, executing batch text replacement, and triggering full dependency tree recalculation.
  5. *Password Protection + Sheet Manipulation*: Verifying that structure protection blocks sheet add/delete while permitting unlocked cell edits.
  6. *Goal Seek + Complex Financial Formulas*: Solving for target loan payment when formula involves compound interest, EDATE schedules, and rounding.

### Tier 4: Real-World Enterprise Workload Scenarios
- **Goal**: Validate complete, multi-step, realistic business workflows exercising end-to-end integration.
- **32 Canonical Application Scenarios**:
  1. *Scenario 1 (M1)*: Corporate Financial Dashboard with company logo image, flowchart shape, and revenue chart.
  2. *Scenario 2 (M1)*: Multi-page executive invoice export to vector PDF with custom margins and gridlines.
  3. *Scenario 3 (M1)*: Global tax rate overhaul: replacing 10% tax rate across 500 cell formulas via Find & Replace.
  4. *Scenario 4 (M1)*: Customer CRM database deduplication: purging redundant rows from a 1,000-entry contact list.
  5. *Scenario 5 (M1)*: Transaction data ingestion: splitting raw bank export strings into Date, Description, and Amount columns.
  6. *Scenario 6 (M2)*: Employee Compensation Model: dual-key salary lookup using `XLOOKUP` and `INDEX/MATCH`.
  7. *Scenario 7 (M2)*: Tiered sales commission matrix evaluating quotas with `IFS`, `SWITCH`, and `IFERROR`.
  8. *Scenario 8 (M2)*: Project Management Gantt Schedule computing milestones using `EDATE`, `EOMONTH`, and `NETWORKDAYS`.
  9. *Scenario 9 (M2)*: Customer address sanitization pipeline utilizing `TEXTSPLIT`, `TEXTBEFORE`, `TEXTAFTER`, and `CLEAN`.
  10. *Scenario 10 (M2)*: Manufacturing quality control chart tracking tolerance deviations with `STDEV`, `VAR`, and `RANK`.
  11. *Scenario 11 (M2)*: Input form validation engine checking data integrity with `ISNUMBER`, `ISTEXT`, and `ISBLANK`.
  12. *Scenario 12 (M2)*: Cross-platform OpenOffice/LibreOffice budget exchange via `.ods` roundtrip.
  13. *Scenario 13 (M2)*: Legacy 2003 ERP inventory migration parsing binary BIFF8 `.xls` workbooks.
  14. *Scenario 14 (M2)*: Intranet portal pricing table export to styled HTML table markup.
  15. *Scenario 15 (M3)*: Dynamic retail sales ledger created as ListObject table with auto-expanding rows.
  16. *Scenario 16 (M3)*: Executive sales report with Total Row computing dynamic subtotal metrics.
  17. *Scenario 17 (M3)*: Inventory valuation ledger evaluating structured column formulas `=[@Qty]*[@Price]`.
  18. *Scenario 18 (M3)*: OpenXML table packaging roundtrip preserving ListObject definitions in `xl/tables/`.
  19. *Scenario 19 (M3)*: Live pricing schedule generated via dynamic 2D array formulas and `#` spill references.
  20. *Scenario 20 (M3)*: Dynamic array collision resolution: clearing blocking cells and dynamically resolving `#SPILL!`.
  21. *Scenario 21 (M3)*: High-performance portfolio dashboard combining `SORT`, `UNIQUE`, and `FILTER`.
  22. *Scenario 22 (M4)*: E-commerce 5,000-row transaction summary aggregated in a multi-dimensional PivotTable.
  23. *Scenario 23 (M4)*: Interactive analyst field list restructuring: dynamically shifting dimensions between Rows and Columns.
  24. *Scenario 24 (M4)*: Quarterly revenue trend analysis grouping dates by Year/Quarter with linked PivotCharts.
  25. *Scenario 25 (M4)*: Mortgage loan feasibility analysis using Goal Seek to determine maximum borrowing limit.
  26. *Scenario 26 (M4)*: Project investment risk underwriting using 2-variable Data Table sensitivity analysis.
  27. *Scenario 27 (M4)*: Enterprise budgeting security: locking workbook structure with password encryption.
  28. *Scenario 28 (M4)*: Multi-page employee roster with repeating print titles and forced department page breaks.
  29. *Scenario 29 (M1-M4)*: Comprehensive corporate consolidation workbook roundtrip across XLSX, PDF, and HTML.
  30. *Scenario 30 (M1-M4)*: End-to-end automated financial modeling workflow from raw CSV to PivotTable and PDF.
  31. *Scenario 31 (M1-M4)*: High-volume stress test: 10,000 rows, 50 columns with concurrent formulas and formatting.
  32. *Scenario 32 (M1-M4)*: Zero-defect regression acceptance test verifying 100% data fidelity.

---

## 6. Execution Commands & Reporting

### Compilation & Test Run Command
```cmd
cmd.exe /c run_tests.bat
```
Or directly via .NET 4.8 `csc.exe`:
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /optimize /r:GenOffice.exe /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.Xml.Linq.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:WindowsBase.dll /r:DocumentFormat.OpenXml.dll /r:PdfSharp.dll /out:TestRunner.exe tests\TestHarness.cs tests\Tier1FeatureTests.cs tests\Tier2BoundaryTests.cs tests\Tier3CrossFeatureTests.cs tests\Tier4RealWorldTests.cs
TestRunner.exe
```

### Granular Report Output Format
The test runner outputs:
1. Per-test execution trace with status indicator (`[PASS]`, `[FAIL]`, `[PENDING]`).
2. Tier Summary Table (Tiers 1, 2, 3, 4 breakdown).
3. Milestone Status Table (M1, M2, M3, M4 breakdown).
4. Feature Summary Table (Features 1 through 32 coverage counts).
5. Comprehensive execution duration and exit verdict.
