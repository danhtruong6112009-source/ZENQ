# BRIEFING — 2026-09-28T11:33:00Z

## Mission
Implement Phase 1 upgrades: OpenXML Drawings Roundtrip in XlsxHandler and CalcView, Native PDF Export in CalcPdfExporter, CalcView and AppForm, and Core Data Utilities (Find/Replace, Remove Duplicates, Text to Columns) in UI and RibbonBar with strict C# 5.0 compliance.

## 🔒 My Identity
- Archetype: teamwork_preview_worker
- Roles: implementer, qa, specialist
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_worker_phase1_2
- Original parent: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Milestone: Phase 1: Critical Data Integrity & Core Productivity Tools

## 🔒 Key Constraints
- Strictly enforce C# 5.0 syntax (csc.exe in build.bat). No $"...", ?., =>, local functions, pattern matching, tuple literals, inline out var.
- Genuine implementations only: no cheating, hardcoded test results, or dummy facades.
- Must run cmd.exe /c build.bat to verify 0 compilation errors.

## Current Parent
- Conversation ID: ddf357e9-0181-4327-b59b-767a2b9b4c82
- Updated: 2026-09-28T11:33:00Z

## Task Summary
- **What to build**:
  1. OpenXML Drawings Roundtrip in src/format/XlsxHandler.cs and CalcView.FloatingObjects.
  2. Native PDF Export in src/format/CalcPdfExporter.cs, CalcView.cs, and AppForm.cs.
  3. Core Data Utilities in src/ui/CalcFindReplaceDialog.cs, src/ui/CalcRemoveDuplicatesDialog.cs, src/ui/CalcTextToColumnsDialog.cs, wired into CalcView.cs, RibbonBar.cs, AppForm.cs.
- **Success criteria**: 0 compiler errors, full functionality implemented per explorer blueprints, comprehensive verification.
- **Interface contracts**: PROJECT.md and explorer blueprints.
- **Code layout**: src/format/, src/ui/

## Key Decisions Made
- Baseline build verified successfully with exit code 0.
- Implementing Task 1 (OpenXML Drawings Roundtrip), Task 2 (Native PDF Export), Task 3 (Core Data Utilities) step by step with verification after each step.

## Artifact Index
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_worker_phase1_2\progress.md — Liveness & progress tracking
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_worker_phase1_2\handoff.md — 5-component handoff report

## Change Tracker
- **Files modified**: None yet
- **Build status**: Baseline pass
- **Pending issues**: None

## Quality Status
- **Build/test result**: Pass (baseline)
- **Lint status**: Clean
- **Tests added/modified**: Pending
