# Progress Log

Last visited: 2026-09-28T17:46:50Z

## Current Status
- Inspected codebase:
  * Verified build succeeds cleanly with C# 5 compiler (`build.bat` using .NET 4.0/4.8 `csc.exe`).
  * Discovered existing table infrastructure in `CalcTableAndPivotDialogs.cs`, `CalcView.cs` (styling, sorting, filtering), and `RibbonBar.cs`.
  * Identified gaps in Table functionality: missing `Ctrl+T` shortcut, auto-expansion on cell commit, per-column Total Row aggregations, structured reference parser (`Table1[@Sales]`), and OpenXML table part persistence.
  * Investigated FormulaEngine (`FormulaEngine.cs`): currently evaluates scalar values only; lacks array return types, spill engine, collision detection, blue bounding borders, `#` spill operator, dynamic array functions (`SORT`, `SORTBY`, `FILTER`, `UNIQUE`, `SEQUENCE`, `RANDARRAY`), and `LET`/`LAMBDA` scoping.
  * Investigated PivotTable infrastructure: basic static dialog and placeholder generator exist in `CalcTableAndPivotDialogs.cs` and `CalcView.cs`; lacks interactive Field List Task Pane, multi-dimensional aggregation engine, date/numeric grouping dialog, linked PivotCharts, and OpenXML pivot parts.
  * Investigated What-If Analysis: Goal Seek (Secant solver targeting 0.001 tolerance) and Data Tables (1-variable and 2-variable sensitivity tables) completely missing.
  * Investigated Protection and Printing: Sheet protection exists in `ProtectSheetDialog.cs` and `CalcView.cs`, but Workbook Structure Protection is missing. Print Area exists, but Print Titles (repeating rows/cols) and manual Page Breaks (`<rowBreaks>`, `<colBreaks>`, dashed rendering) are missing.
- Now writing comprehensive `spec_report.md`.
