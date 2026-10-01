# DISPATCH: Survey Agent 2 (Phases 1 & 2 Spec & API Miner)

**Role**: Phases 1 & 2 Spec & API Miner
**Type**: teamwork_preview_spec_miner
**Working Directory**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2
**Authoritative Request**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md
**Project Root**: c:\Users\Danh\Desktop\genofficepy

## Objectives
1. Read `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. Thoroughly investigate existing codebase touchpoints and technical requirements for:
   - **Phase 1: Critical Data Integrity & Core Productivity Tools**:
     * OpenXML Drawings Roundtrip (`XlsxHandler.cs`, `xl/drawings/drawing1.xml`, `xl/drawings/_rels/`, `xl/media/`, `CalcView.FloatingObjects`, anchors, shapes, images, charts).
     * Native PDF Export for Calc (`FormatHandlers.cs`, `CalcView.cs`, margins, orientation, paper size, gridlines, fonts, styling).
     * Core Data Utilities: Find & Replace Dialog, Remove Duplicates modal, Text to Columns Wizard (Delimited & Fixed-width).
   - **Phase 2: Formula Engine Expansion (50+ Functions) & File Formats**:
     * Formula engine parsing and execution pipeline (`FormulaEngine.cs` or equivalent). How are ASTs, tokens, functions evaluated?
     * The 50+ functions to add:
       - Lookup & Reference: INDEX, MATCH, XLOOKUP, XMATCH, ROW, COLUMN, ROWS, COLUMNS, INDIRECT, OFFSET.
       - Logical & Error: IFS, SWITCH, IFERROR, IFNA, XOR.
       - Date & Time: YEAR, MONTH, DAY, HOUR, MINUTE, SECOND, WEEKDAY, EDATE, EOMONTH, NETWORKDAYS, DATEDIF.
       - Text: TEXT, VALUE, FIND, SEARCH, REPLACE, REPT, TEXTSPLIT, TEXTBEFORE, TEXTAFTER, CHAR, CODE, CLEAN.
       - Math & Statistical: RANDBETWEEN, RAND, CEILING, FLOOR, SIGN, STDEV, VAR, RANK, MODE.
       - Information: ISNUMBER, ISTEXT, ISBLANK, ISERROR, ISNA.
     * File Formats: ODS (OpenDocument Spreadsheet XML/ZIP structure, sheets, styles, values, formulas), XLS (BIFF8 binary workbook format, records, streams), Web/Delimited (.html, .tsv, .prn).
3. Produce a detailed Spec Mining Report in `c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2\spec_report.md` detailing:
   - Specific data models and class designs needed for Phase 1 & 2
   - Exact signature, semantics, error handling (`#N/A`, `#VALUE!`, `#REF!`, etc.) for all 50+ formulas
   - File format parsers/writers architecture
   - Specific integration points in CalcView, XlsxHandler, and FormatHandlers
4. Write `handoff.md` and send completion message to parent.

## 2026-09-28T10:42:16Z
You are Survey Agent 2 (Phases 1 & 2 Spec & API Miner).
Your working directory is: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2
Your task assignment is in: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2\DISPATCH.md
The authoritative user request is: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md

Read ORIGINAL_REQUEST.md and DISPATCH.md.
Investigate existing code and specifications for Phase 1 & Phase 2:
1. OpenXML Drawings roundtrip (drawing1.xml, _rels, media, FloatingObjects, shapes, images, charts).
2. Native PDF export (FormatHandlers, CalcView, margins, paper size, orientation, gridlines, styling).
3. Core Data Utilities (Find & Replace, Remove Duplicates, Text to Columns).
4. Formula engine architecture and detailed specifications/semantics for the 50+ new functions across Lookup/Ref, Logical, Date/Time, Text, Math/Stat, Information.
5. File formats: ODS reader/writer, XLS BIFF8 binary reader, HTML/TSV/PRN export.
6. Document full specifications, models, and integration points in c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_spec_miner_survey_2\spec_report.md.
7. Create handoff.md and send a completion message to parent.
