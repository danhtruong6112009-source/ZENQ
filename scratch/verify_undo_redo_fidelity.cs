using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.UI;
using GenOffice.Core;

namespace GenOffice.Test
{
    using DocTableObject = GenOffice.UI.DocTableObject;
    using DocTableRow = GenOffice.UI.DocTableRow;
    using DocTableCell = GenOffice.UI.DocTableCell;

    class UndoRedoFidelityVerifier
    {
        private static int passedTests = 0;
        private static int totalTests = 0;

        static void Assert(bool condition, string testName, string detail = "")
        {
            totalTests++;
            if (condition)
            {
                passedTests++;
                Console.WriteLine("[PASS] " + testName + (string.IsNullOrEmpty(detail) ? "" : " (" + detail + ")"));
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FAIL] " + testName + (string.IsNullOrEmpty(detail) ? "" : " (" + detail + ")"));
                Console.ResetColor();
                throw new Exception("Assertion failed: " + testName + " - " + detail);
            }
        }

        [STAThread]
        static int Main(string[] args)
        {
            Console.WriteLine("===============================================================");
            Console.WriteLine(" GenOffice AI 1: Tab Undo/Redo & Deep Snapshot Fidelity Tests");
            Console.WriteLine("===============================================================");

            try
            {
                Test1_TabHistoryStackIsolation();
                Test2_TextRunsCursorSelectionFidelity();
                Test3_ParagraphFormattingFidelity();
                Test4_TableDeepSnapshotFidelity();
                Test5_ShapeDeepSnapshotFidelity();
                Test6_StackOrderAndInversionProtection();
                Test7_StackCapAt50();

                Console.WriteLine("===============================================================");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" ALL " + passedTests + " OF " + totalTests + " UNIT & INTEGRATION TESTS PASSED 100%!");
                Console.ResetColor();
                Console.WriteLine("===============================================================");
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[ERROR] Test suite failed with exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                return 1;
            }
        }

        static void Test1_TabHistoryStackIsolation()
        {
            Console.WriteLine("\n--- TEST 1: Per-Tab History Stack Isolation ---");
            RibbonBar ribbon = new RibbonBar();
            WriterView writer = new WriterView();

            // Add Tab 1 and Tab 2
            ribbon.AddDocTab("Document_1.docx", OfficeAppMode.Writer, null, writer);
            ribbon.AddDocTab("Document_2.docx", OfficeAppMode.Writer, null, writer);

            Assert(ribbon.DocTabs.Count == 3, "Ribbon has 3 tabs (Home + 2 Writer tabs)");

            // 1. Switch to Tab 1
            ribbon.SwitchDocTab(1, null, writer, null, null);
            Assert(writer.CanUndo == false, "Tab 1 starts with empty undo stack");
            Assert(writer.PieceTable.GetText() == "", "Tab 1 starts empty");

            // Type in Tab 1
            writer.SaveUndoState();
            writer.PieceTable.SetInitialText("Tab 1 Initial Content");
            writer.CursorPosition = 21;
            writer.SaveUndoState();

            writer.PieceTable.SetInitialText("Tab 1 Second Edit Added");
            writer.CursorPosition = 24;

            Assert(writer.CanUndo == true, "Tab 1 can undo after typing");
            Assert(writer.GetUndoStack().Count == 2, "Tab 1 has 2 undo snapshots");

            // 2. Switch to Tab 2
            ribbon.SwitchDocTab(2, null, writer, null, null);
            Assert(writer.CanUndo == false, "Tab 2 has isolated empty undo stack (not polluted by Tab 1!)");
            Assert(writer.PieceTable.GetText() == "", "Tab 2 starts empty");

            // Type in Tab 2
            writer.PieceTable.SetInitialText("Tab 2 First Content");
            writer.CursorPosition = 19;
            writer.SaveUndoState();

            writer.PieceTable.SetInitialText("Tab 2 Second Content");
            writer.CursorPosition = 20;

            // Undo in Tab 2
            writer.Undo();
            Assert(writer.PieceTable.GetText() == "Tab 2 First Content", "Tab 2 undo restores Tab 2 only");

            // 3. Switch back to Tab 1
            ribbon.SwitchDocTab(1, null, writer, null, null);
            Assert(writer.PieceTable.GetText() == "Tab 1 Second Edit Added", "Tab 1 content is completely untouched by Tab 2");
            Assert(writer.GetUndoStack().Count == 2, "Tab 1 undo stack is preserved intact with 2 snapshots");

            // Undo in Tab 1
            writer.Undo();
            Assert(writer.PieceTable.GetText() == "Tab 1 Initial Content", "Tab 1 undo restores Tab 1 initial content");
            Assert(writer.CanRedo == true, "Tab 1 can redo");

            // Redo in Tab 1
            writer.Redo();
            Assert(writer.PieceTable.GetText() == "Tab 1 Second Edit Added", "Tab 1 redo restores second edit");

            // 4. Switch back to Tab 2
            ribbon.SwitchDocTab(2, null, writer, null, null);
            Assert(writer.PieceTable.GetText() == "Tab 2 First Content", "Tab 2 content preserved when switching back");
            Assert(writer.CanRedo == true, "Tab 2 redo stack preserved across tab switches");

            writer.Redo();
            Assert(writer.PieceTable.GetText() == "Tab 2 Second Content", "Tab 2 redo restores Tab 2 second content");
        }

        static void Test2_TextRunsCursorSelectionFidelity()
        {
            Console.WriteLine("\n--- TEST 2: Text, Formatted Runs, Caret & Selection Fidelity ---");
            WriterView writer = new WriterView();

            writer.PieceTable.SetInitialText("The quick brown fox jumps over the lazy dog.");
            writer.CursorPosition = 19;
            writer.SelectionStart = 10;
            writer.SelectionEnd = 19;
            writer.FormattedRuns = new List<FormattedRun>
            {
                new FormattedRun(0, 9, "Arial", 14.0f, true, false, false, false, Color.Red, Color.Transparent),
                new FormattedRun(10, 9, "Times New Roman", 12.0f, false, true, true, false, Color.Blue, Color.Yellow)
            };
            writer.ScrollY = 150;

            writer.SaveUndoState();

            // Mutate document
            writer.PieceTable.SetInitialText("Completely replaced text!");
            writer.CursorPosition = 0;
            writer.SelectionStart = 0;
            writer.SelectionEnd = 0;
            writer.FormattedRuns.Clear();
            writer.ScrollY = 0;

            // Undo
            writer.Undo();

            Assert(writer.PieceTable.GetText() == "The quick brown fox jumps over the lazy dog.", "Text restored 100%");
            Assert(writer.CursorPosition == 19, "Caret position preserved at 19 (did NOT jump to 0)");
            Assert(writer.SelectionStart == 10 && writer.SelectionEnd == 19, "Selection bounds restored (10..19)");
            Assert(writer.ScrollY == 150, "Scroll position restored to 150");
            Assert(writer.FormattedRuns.Count == 2, "Formatted runs count restored to 2");
            Assert(writer.FormattedRuns[0].FontName == "Arial" && writer.FormattedRuns[0].Bold == true && writer.FormattedRuns[0].Color == Color.Red, "Run 0 format matches exactly");
            Assert(writer.FormattedRuns[1].FontName == "Times New Roman" && writer.FormattedRuns[1].Italic == true && writer.FormattedRuns[1].Underline == true && writer.FormattedRuns[1].Highlight == Color.Yellow, "Run 1 format matches exactly");

            // Redo
            writer.Redo();
            Assert(writer.PieceTable.GetText() == "Completely replaced text!", "Redo restores replaced text");
            Assert(writer.CursorPosition == 0, "Redo cursor restored to 0");
        }

        static void Test3_ParagraphFormattingFidelity()
        {
            Console.WriteLine("\n--- TEST 3: Paragraph Formatting Deep Fidelity ---");
            WriterView writer = new WriterView();
            writer.PieceTable.SetInitialText("Paragraph 1\nParagraph 2\nParagraph 3");

            // Set detailed paragraph attributes on paragraph 0
            writer.ParagraphAlignments[0] = 2; // Center
            writer.ParagraphLineSpacings[0] = 1.5f;
            writer.ParagraphFirstLineIndents[0] = 28.35f; // 1 cm
            writer.ParagraphLeftIndents[0] = 36.0f;
            writer.ParagraphRightIndents[0] = 40.0f;
            writer.ParagraphHangingIndents[0] = 14.17f;
            writer.ParagraphSpacingsBefore[0] = 6.0f;
            writer.ParagraphSpacingsAfter[0] = 12.0f;
            writer.ParagraphBottomBorders[0] = Color.DarkBlue;
            writer.ParagraphBorders[0] = new ParagraphBorderInfo { Top = true, Bottom = true, Color = Color.DarkSlateGray, SizePt = 2.0f };
            writer.ParagraphTabStops[0] = new List<TabStopInfo>
            {
                new TabStopInfo { PositionPt = 72.0f, Type = TabStopType.Left },
                new TabStopInfo { PositionPt = 144.0f, Type = TabStopType.Decimal }
            };

            writer.SaveUndoState();

            // Clear or change formatting
            writer.ParagraphAlignments.Clear();
            writer.ParagraphLineSpacings.Clear();
            writer.ParagraphFirstLineIndents.Clear();
            writer.ParagraphLeftIndents.Clear();
            writer.ParagraphRightIndents.Clear();
            writer.ParagraphHangingIndents.Clear();
            writer.ParagraphSpacingsBefore.Clear();
            writer.ParagraphSpacingsAfter.Clear();
            writer.ParagraphBottomBorders.Clear();
            writer.ParagraphBorders.Clear();
            writer.ParagraphTabStops.Clear();

            // Undo
            writer.Undo();

            Assert(writer.ParagraphAlignments[0] == 2, "Paragraph alignment restored to Center (2)");
            Assert(writer.ParagraphLineSpacings[0] == 1.5f, "Line spacing restored to 1.5");
            Assert(writer.ParagraphFirstLineIndents[0] == 28.35f, "First line indent restored to 28.35 pt");
            Assert(writer.ParagraphLeftIndents[0] == 36.0f, "Left indent restored to 36.0 pt");
            Assert(writer.ParagraphRightIndents[0] == 40.0f, "Right indent restored to 40.0 pt");
            Assert(writer.ParagraphHangingIndents[0] == 14.17f, "Hanging indent restored to 14.17 pt");
            Assert(writer.ParagraphSpacingsBefore[0] == 6.0f, "Spacing before restored to 6.0 pt");
            Assert(writer.ParagraphSpacingsAfter[0] == 12.0f, "Spacing after restored to 12.0 pt");
            Assert(writer.ParagraphBottomBorders[0] == Color.DarkBlue, "Bottom border color restored to DarkBlue");
            Assert(writer.ParagraphBorders[0].Top == true && writer.ParagraphBorders[0].Color == Color.DarkSlateGray, "Paragraph border info restored");
            Assert(writer.ParagraphTabStops[0].Count == 2 && writer.ParagraphTabStops[0][1].Type == TabStopType.Decimal, "Tab stops restored with correct type and count");
        }

        static void Test4_TableDeepSnapshotFidelity()
        {
            Console.WriteLine("\n--- TEST 4: Document Tables Deep Snapshot Fidelity ---");
            WriterView writer = new WriterView();
            writer.PieceTable.SetInitialText("Before table\n| Cell 1 | Cell 2 |\n| Cell 3 | Cell 4 |\nAfter table");

            DocTableObject tbl = new DocTableObject();

            DocTableRow r1 = new DocTableRow();
            DocTableCell c1 = new DocTableCell();
            c1.Text = "Header 1";
            c1.Bold = true;
            c1.BackgroundColor = Color.LightGray;
            c1.WidthPt = 250;
            r1.Cells.Add(c1);

            DocTableCell c2 = new DocTableCell();
            c2.Text = "Header 2";
            c2.Bold = true;
            c2.BackgroundColor = Color.LightGray;
            c2.WidthPt = 250;
            r1.Cells.Add(c2);
            tbl.Rows.Add(r1);

            writer.DocumentTables.Add(tbl);
            writer.TablePageX[0] = 72;
            writer.TablePageY[0] = 150;
            writer.TableWrapModes[0] = 1; // Square
            writer.TableCustomSizes[0] = new Size(500, 200);
            writer.TableColWidths[0] = new List<int> { 250, 250 };
            writer.TableRowHeights[0] = new List<int> { 40, 40 };
            writer.TableFixPositions[0] = true;

            writer.SaveUndoState();

            // Mutate table
            tbl.Rows[0].Cells[0].Text = "MUTATED HEADER";
            tbl.Rows[0].Cells[0].Bold = false;
            tbl.Rows[0].Cells[0].BackgroundColor = Color.Red;
            writer.TableColWidths[0][0] = 100;
            writer.TableFixPositions[0] = false;

            // Undo
            writer.Undo();

            Assert(writer.DocumentTables.Count == 1, "Document tables count is 1");
            DocTableObject restoredTbl = writer.DocumentTables[0];
            Assert(restoredTbl.Rows[0].Cells[0].Text == "Header 1", "Cell text restored to 'Header 1' (not 'MUTATED HEADER')");
            Assert(restoredTbl.Rows[0].Cells[0].Bold == true, "Cell bold restored to true");
            Assert(restoredTbl.Rows[0].Cells[0].BackgroundColor == Color.LightGray, "Cell background restored to LightGray");
            Assert(writer.TableColWidths[0][0] == 250, "Column width restored to 250");
            Assert(writer.TableFixPositions[0] == true, "TableFixPositions[0] restored to true");

            // Redo
            writer.Redo();
            Assert(writer.DocumentTables[0].Rows[0].Cells[0].Text == "MUTATED HEADER", "Redo restores mutated cell text");
            Assert(writer.TableColWidths[0][0] == 100, "Redo restores mutated col width 100");
        }

        static void Test5_ShapeDeepSnapshotFidelity()
        {
            Console.WriteLine("\n--- TEST 5: Document Shapes Deep Snapshot Fidelity ---");
            WriterView writer = new WriterView();
            writer.PieceTable.SetInitialText("Document with Shape");

            DocShapeObject shape = new DocShapeObject();
            shape.Id = "shape_1";
            shape.ShapeType = "Diamond";
            shape.BaseWidth = 180;
            shape.BaseHeight = 120;
            shape.RotationAngle = 30.0f;
            shape.TextContent = "Original Diamond Text";
            shape.FillColor = Color.CornflowerBlue;
            shape.OutlineColor = Color.DarkSlateBlue;
            shape.OutlineWidth = 3.0f;

            writer.DocumentShapes.Add(shape);
            writer.SaveUndoState();

            // Mutate shape
            shape.BaseWidth = 350;
            shape.RotationAngle = 180.0f;
            shape.TextContent = "Changed Shape Text";
            shape.FillColor = Color.DarkRed;

            // Undo
            writer.Undo();

            Assert(writer.DocumentShapes.Count == 1, "Document shapes count restored to 1");
            DocShapeObject restoredShape = writer.DocumentShapes[0];
            Assert(restoredShape.BaseWidth == 180, "BaseWidth restored to 180");
            Assert(restoredShape.RotationAngle == 30.0f, "RotationAngle restored to 30 deg");
            Assert(restoredShape.TextContent == "Original Diamond Text", "TextContent restored");
            Assert(restoredShape.FillColor == Color.CornflowerBlue, "FillColor restored");

            // Redo
            writer.Redo();
            Assert(writer.DocumentShapes[0].BaseWidth == 350, "Redo restores BaseWidth 350");
            Assert(writer.DocumentShapes[0].TextContent == "Changed Shape Text", "Redo restores changed shape text");
        }

        static void Test6_StackOrderAndInversionProtection()
        {
            Console.WriteLine("\n--- TEST 6: Stack Order and .NET Inversion Protection ---");
            Stack<WriterUndoSnapshot> stack = new Stack<WriterUndoSnapshot>();
            for (int i = 1; i <= 5; i++)
            {
                var snap = new WriterUndoSnapshot();
                snap.Text = "State " + i;
                stack.Push(snap);
            }

            // Clone stack
            Stack<WriterUndoSnapshot> cloned = WriterUndoSnapshot.CloneSnapshotStack(stack);

            Assert(cloned.Count == 5, "Cloned stack has 5 items");

            // Verify LIFO order: top should be State 5, next State 4, etc.
            Assert(cloned.Pop().Text == "State 5", "Popped 1st: State 5 (Top preserved)");
            Assert(cloned.Pop().Text == "State 4", "Popped 2nd: State 4");
            Assert(cloned.Pop().Text == "State 3", "Popped 3rd: State 3");
            Assert(cloned.Pop().Text == "State 2", "Popped 4th: State 2");
            Assert(cloned.Pop().Text == "State 1", "Popped 5th: State 1 (Bottom preserved)");

            // Verify original stack is still intact and in correct order
            Assert(stack.Count == 5, "Original stack unchanged");
            Assert(stack.Pop().Text == "State 5", "Original top is State 5");
        }

        static void Test7_StackCapAt50()
        {
            Console.WriteLine("\n--- TEST 7: History Stack Cap at 50 Items ---");
            WriterView writer = new WriterView();
            for (int i = 1; i <= 60; i++)
            {
                writer.PieceTable.SetInitialText("Revision " + i);
                writer.SaveUndoState();
            }

            Stack<WriterUndoSnapshot> undoStack = writer.GetUndoStack();
            Assert(undoStack.Count == 50, "Undo stack capped at exactly 50 items (oldest 10 discarded)");

            // The top item should be Revision 60 (the last state pushed)
            WriterUndoSnapshot top = undoStack.Pop();
            Assert(top.Text == "Revision 60", "Top of stack is Revision 60");

            // The bottom item (oldest kept) should be Revision 11 (items 1..10 were pruned)
            WriterUndoSnapshot bottom = null;
            while (undoStack.Count > 0)
            {
                bottom = undoStack.Pop();
            }
            Assert(bottom.Text == "Revision 11", "Bottom of stack is Revision 11 (oldest kept revision)");
        }
    }
}
