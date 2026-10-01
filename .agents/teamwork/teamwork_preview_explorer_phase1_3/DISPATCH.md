# DISPATCH: Phase 1 Explorer 3 (Core Data Utilities Specialist)

**Role**: Core Data Utilities Specialist
**Type**: teamwork_preview_explorer
**Working Directory**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_3
**Authoritative Request**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md
**Project Index**: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
**Survey References**:
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2\spec_report.md`
- `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_survey_1\survey_report.md`

## Objectives
1. Read `ORIGINAL_REQUEST.md`, `PROJECT.md`, and the survey reports.
2. Investigate the exact implementation strategy for Core Data Utilities:
   - **Find & Replace Dialog (`CalcFindReplaceDialog.cs`)**:
     * Dedicated floating modeless/modal dialog.
     * Options: Match Case, Match Entire Cell, Search By Rows / Search By Columns, Look In Formulas / Look In Values.
     * Actions: Find Next, Find Previous, Replace, Replace All, Close.
     * Formula-safety: Replacing values must not corrupt formulas unless specifically asked.
   - **Remove Duplicates Dialog (`CalcRemoveDuplicatesDialog.cs`)**:
     * Modal dialog with "My data has headers" checkbox and column checklist (Select All / Unselect All).
     * Compares selected columns for row equality across selection range.
     * Deletes duplicate rows and shifts remaining rows up cleanly without corrupting sheet spans or formatting.
     * Reports summary message: "X duplicate values found and removed; Y unique values remain."
   - **Text to Columns Wizard (`CalcTextToColumnsDialog.cs`)**:
     * Wizard supporting Delimited (Tab, Semicolon, Comma, Space, Other custom) and Fixed-Width parsing.
     * Treat consecutive delimiters as one option.
     * Live preview data grid.
     * Splits cell text into target adjacent columns, warning before overwriting existing data.
   - Wire each dialog into `CalcView.cs` and ribbon menu items / keyboard shortcuts (`Ctrl+F`, `Ctrl+H`).
3. Ensure strict **C# 5.0** compliance.
4. Write detailed implementation plan in `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_3\utilities_plan.md` and deliver `handoff.md`.

## 2026-09-28T10:51:10Z
You are Phase 1 Explorer 3 (Core Data Utilities Specialist).
Your working directory is: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_3
Your task assignment is in: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_explorer_phase1_3\DISPATCH.md
The project index is: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
The authoritative user request is: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md

Read DISPATCH.md, PROJECT.md, and survey reports.
Formulate the exact C# 5 implementation strategy for Core Data Utilities: CalcFindReplaceDialog, CalcRemoveDuplicatesDialog, CalcTextToColumnsDialog, and their integration with CalcView.cs. Write utilities_plan.md and handoff.md.
