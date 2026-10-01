## 2026-09-18T13:59:11Z

<USER_REQUEST>
You are AI 1 (Worker: Tab Undo/Redo & Deep Snapshot Fidelity Specialist) for GenOffice.
Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai1_undo
Workspace root: c:\Users\Danh\Desktop\genofficepy
Original request path: c:\Users\Danh\Desktop\genofficepy\.agents\ORIGINAL_REQUEST.md (Read section ## 2026-09-18T13:52:11Z)
Scope document: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_orchestrator_3\PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

EXCLUSIVE WRITE OWNERSHIP:
- src/ui/WriterView.cs (Undo/Redo subsystem, WriterUndoSnapshot, snapshot capture & restore)
- src/ui/RibbonBar.cs (DocTab history stacks, SaveTabState/LoadTabState undo/redo hookup)
- src/ui/AppForm.cs (tab switching / undo/redo hooks if needed)
Do NOT write to files owned by AI 2 (ShapesGalleryDropDownForm.cs, ShapeRenderer.cs, FormatHandlers.cs, SmartArtRenderer.cs).

DETAILED ASSIGNMENT:
1. Tab History Stack Isolation:
   - Each DocTab in RibbonBar.cs must own its own UndoStack and RedoStack (e.g. Stack<WriterUndoSnapshot> or wrapper).
   - When switching tabs via SaveTabState and LoadTabState (or active tab change in RibbonBar / AppForm), save current WriterView undo/redo stack into the current DocTab, and restore the target DocTab's undo/redo stack into WriterView.
   - When creating a new tab or closing a tab, initialize or clean up properly.
   - Ensure Ctrl+Z or Ctrl+Y NEVER affects another tab or mixes document text/state across multiple open documents.
2. Deep Snapshot Fidelity:
   - Expand WriterUndoSnapshot in WriterView.cs so that it captures and faithfully restores:
     - Document text and piece table state (PieceTable buffer / text)
     - Formatted runs (FormattedRuns - make sure to clone/copy list)
     - Caret position (CursorPosition) and selection bounds (SelectionStart, SelectionEnd)
     - Document tables (DocumentTables - deep copy tables, rows, cells, text, TablePageX/Y, row heights, col widths, borders, background colors)
     - Document shapes (DocumentShapes - deep copy of shape list with positions, sizes, rotation angles, colors, TextContent, etc.)
     - Paragraph formatting (ParagraphAlignments, ParagraphLineSpacings, FirstLineIndent, LeftIndent, RightIndent, SpaceBefore, SpaceAfter)
   - When Ctrl+Z is triggered:
     - Restore text, runs, cursor, selection, tables, shapes, and paragraph indents/alignments 100% faithfully.
     - Scroll caret into view so the viewport updates.
     - The caret must NOT jump randomly to the start of the document (index 0) if it was elsewhere.
     - Structures must not be corrupted or deformed.
   - When Ctrl+Y (Redo) is triggered:
     - Push current state to undoStack, pop redoStack, restore state faithfully.

VERIFICATION REQUIREMENTS:
1. Build the application using build.bat or csc.exe. Compilation MUST succeed with 0 errors.
2. Write a verification test script (e.g. verify_undo_fidelity.ps1) or automated C# console/test runner that validates:
   - Opening 2 tabs with different content.
   - Typing in Tab 1, typing in Tab 2.
   - Pressing Undo in Tab 2 undoes Tab 2 ONLY; Tab 1 is completely untouched.
   - Modifying a table or shape or paragraph indent in Tab 1, pressing Undo restores it 100%.
   - Caret position and selection are restored.
3. Record progress in c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai1_undo\progress.md.
4. When finished, write c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_worker_ai1_undo\handoff.md and report back to parent with send_message.
</USER_REQUEST>

## 2026-09-18T14:21:02Z
**Context**: Liveness check & status query.
**Content**: It has been 20 minutes since dispatch. Please report your current progress, whether you encountered any obstacles, and update your progress.md with your latest status and timestamp.
**Action**: Reply with your current status and update progress.md.
