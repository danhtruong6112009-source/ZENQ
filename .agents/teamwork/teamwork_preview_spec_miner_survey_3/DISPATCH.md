# DISPATCH: Survey Agent 3 (Phases 3 & 4 Spec & Architecture Miner)

**Role**: Phases 3 & 4 Spec & Architecture Miner
**Type**: teamwork_preview_spec_miner
**Working Directory**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_3
**Authoritative Request**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md
**Project Root**: c:\Users\Danh\Desktop\genofficepy

## Objectives
1. Read `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. Thoroughly investigate existing codebase touchpoints and technical requirements for:
   - **Phase 3: Structured Tables (ListObject) & Dynamic Arrays**:
     * Excel Tables (ListObject): Create Table from range (`Ctrl+T`), zebra striping, header sorting/filtering, structured formula references (e.g. `Table1[@Sales] * 1.1`), dynamic auto-expansion on adjacent typing, Total Row with aggregations (SUM, AVERAGE, COUNT, MIN, MAX).
     * Dynamic Arrays & Spill Engine: Spilling into adjacent empty cells, blue bounding border rendering, `#SPILL!` collision detection, functions: `SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`, basic `LET` and `LAMBDA` scoping.
   - **Phase 4: PivotTables & Advanced Analytics**:
     * PivotTable & PivotChart: Dialog with field list pane (Filters, Columns, Rows, Values), multi-dimensional aggregation engine (Sum, Count, Average, Min, Max, % of Column/Row), date and numeric grouping, linked PivotCharts reflecting dynamic pivot aggregations.
     * What-If Analysis: Goal Seek (iterative single-variable solver to reach target cell value), Data Tables (1-variable and 2-variable sensitivity tables).
     * Advanced Printing & Protection: Workbook structure protection with password prompt, Print Titles (repeat header rows/cols), manual Page Break management.
3. Produce a detailed Spec Mining Report in `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_3\spec_report.md` detailing:
   - Specific data models and class designs needed for Phase 3 & 4
   - Calculation engine modifications for Dynamic Arrays, Spilling, Structured References, and iterative Goal Seek / Data Tables
   - UI / ViewModel requirements for Table controls, Field List pane, Goal Seek dialog, etc.
   - Workbook protection and Print Title models
4. Write `handoff.md` and send completion message to parent.
