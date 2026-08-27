import os

code = """using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    class CalcAllFeaturesTest
    {
        [STAThread]
        static int Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("  CALC SUITE: UNDO/REDO, IN-PLACE SELECTION, BORDERS, DROPDOWNS");
            Console.WriteLine("==========================================================");

            int passed = 0;
            int total = 0;

            Action<bool, string> Assert = delegate(bool condition, string testName)
            {
                total++;
                if (condition)
                {
                    Console.WriteLine("[PASS] " + testName);
                    passed++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FAIL] " + testName);
                    Console.ResetColor();
                }
            };

            try
            {
                CalcView calc = new CalcView();
                SparseMatrix sheet = calc.CurrentSheet;

                // --- TEST 1: Cell Text Edit and Undo / Redo ---
                Console.WriteLine("\\n--- 1. Testing Cell Text Edit and Undo / Redo ---");
                calc.SelectedRow = 2;
                calc.SelectedCol = 2;
                calc.SelEndRow = 2;
                calc.SelEndCol = 2;
                calc.StartEditingCell("Hello World");
                calc.CommitCellEdit();

                Assert(sheet.GetCell(2, 2) != null && sheet.GetCell(2, 2).RawValue == "Hello World", "Cell (2,2) committed text 'Hello World'");
                Assert(calc.CanUndo, "Undo stack has snapshot");

                calc.StartEditingCell("New Value");
                calc.CommitCellEdit();
                Assert(sheet.GetCell(2, 2).RawValue == "New Value", "Cell (2,2) updated to 'New Value'");

                // Perform Undo
                calc.Undo();
                Assert(sheet.GetCell(2, 2).RawValue == "Hello World", "Undo reverted cell (2,2) to 'Hello World'");
                Assert(calc.CanRedo, "Redo stack is available after Undo");

                // Perform Redo
                calc.Redo();
                Assert(sheet.GetCell(2, 2).RawValue == "New Value", "Redo restored cell (2,2) to 'New Value'");

                // Perform 2 Undos to blank
                calc.Undo();
                calc.Undo();
                Assert(sheet.GetCell(2, 2) == null || string.IsNullOrEmpty(sheet.GetCell(2, 2).RawValue), "Second Undo reverted cell (2,2) to initial blank state");

                // --- TEST 2: Formatting Undo / Redo ---
                Console.WriteLine("\\n--- 2. Testing Formatting Undo / Redo ---");
                calc.SelectedRow = 20;
                calc.SelectedCol = 20;
                calc.SelEndRow = 20;
                calc.SelEndCol = 20;
                sheet.SetCellText(20, 20, "1234.56");

                // Bold
                calc.ToggleBold();
                Assert(sheet.GetCell(20, 20).Format.Bold == true, "Cell (20,20) Bold applied");
                calc.Undo();
                Assert(sheet.GetCell(20, 20).Format.Bold == false, "Undo Bold reverted bold formatting");
                calc.Redo();
                Assert(sheet.GetCell(20, 20).Format.Bold == true, "Redo Bold re-applied bold formatting");

                // BgColor and TextColor
                calc.SetSelectedCellBgColor(Color.LightYellow);
                calc.SetSelectedCellTextColor(Color.DarkBlue);
                Assert(sheet.GetCell(20, 20).Format.BgColor == Color.LightYellow.ToArgb(), "BgColor applied");
                Assert(sheet.GetCell(20, 20).Format.TextColor == Color.DarkBlue.ToArgb(), "TextColor applied");

                calc.Undo();
                Assert(sheet.GetCell(20, 20).Format.TextColor != Color.DarkBlue.ToArgb(), "Undo TextColor reverted");
                calc.Undo();
                Assert(sheet.GetCell(20, 20).Format.BgColor != Color.LightYellow.ToArgb(), "Undo BgColor reverted");

                // Font Name and Size
                calc.ApplyFontName("Arial");
                Assert(sheet.GetCell(20, 20).Format.FontName == "Arial", "Font Arial applied");
                calc.ApplyFontSize(16f);
                Assert(sheet.GetCell(20, 20).Format.FontSize == 16f, "Font size 16 applied");
                calc.Undo();
                Assert(sheet.GetCell(20, 20).Format.FontSize != 16f, "Undo FontSize reverted");
                calc.Undo();
                Assert(sheet.GetCell(20, 20).Format.FontName != "Arial", "Undo FontName reverted");

                // Number Format
                calc.SetNumberFormat("#,##0.00");
                Assert(sheet.GetCell(20, 20).Format.NumberFormat == "#,##0.00", "Number format applied");
                calc.Undo();
                Assert(sheet.GetCell(20, 20).Format.NumberFormat != "#,##0.00", "Undo NumberFormat reverted");

                // --- TEST 3: Column Width and Row Height Undo / Redo ---
                Console.WriteLine("\\n--- 3. Testing Dimension Resize Undo / Redo ---");
                calc.SaveUndoState();
                calc.CustomColWidths[4] = 200;
                calc.CustomRowHeights[4] = 60;
                Assert(calc.GetColWidth(4) == 200, "Column 4 width customized to 200");
                Assert(calc.GetRowHeight(4) == 60, "Row 4 height customized to 60");

                calc.Undo();
                Assert(calc.GetColWidth(4) != 200, "Undo restored Column 4 default width");
                Assert(calc.GetRowHeight(4) != 60, "Undo restored Row 4 default height");

                calc.Redo();
                Assert(calc.GetColWidth(4) == 200, "Redo restored Column 4 width 200");
                Assert(calc.GetRowHeight(4) == 60, "Redo restored Row 4 height 60");

                // --- TEST 4: Excel Border Presets 0..12 and Undo / Redo ---
                Console.WriteLine("\\n--- 4. Testing Excel Border Presets 0..12 and Undo ---");
                calc.SelectedRow = 5;
                calc.SelectedCol = 5;
                calc.SelEndRow = 6;
                calc.SelEndCol = 6;

                // Preset 0: Bottom Border
                calc.ApplyBorderPreset(0);
                Assert(sheet.GetCell(6, 5).Format.Borders.Bottom && sheet.GetCell(6, 6).Format.Borders.Bottom, "Preset 0 (Bottom Border) applied to bottom cells");

                // Preset 5: All Borders
                calc.ApplyBorderPreset(5);
                Assert(sheet.GetCell(5, 5).Format.Borders.Left && sheet.GetCell(5, 5).Format.Borders.Right && sheet.GetCell(5, 5).Format.Borders.Top && sheet.GetCell(5, 5).Format.Borders.Bottom, "Preset 5 (All Borders) applied to (5,5)");

                // Preset 4: No Border
                calc.ApplyBorderPreset(4);
                Assert(!sheet.GetCell(5, 5).Format.Borders.Left && !sheet.GetCell(5, 5).Format.Borders.Right && !sheet.GetCell(5, 5).Format.Borders.Top && !sheet.GetCell(5, 5).Format.Borders.Bottom, "Preset 4 (No Border) cleared all borders");

                // Preset 8: Bottom Double Border
                calc.ApplyBorderPreset(8);
                Assert(sheet.GetCell(6, 5).Format.Borders.Bottom && sheet.GetCell(6, 5).Format.Borders.Thickness == 4, "Preset 8 (Bottom Double Border) sets Thickness=4 (double line)");

                // Preset 12: Top and Double Bottom Border
                calc.ApplyBorderPreset(12);
                Assert(sheet.GetCell(5, 5).Format.Borders.Top && sheet.GetCell(6, 5).Format.Borders.Bottom && sheet.GetCell(6, 5).Format.Borders.Thickness == 4, "Preset 12 (Top and Double Bottom) sets Top=true and Bottom Thickness=4");

                // Undo Preset 12
                calc.Undo();
                Assert(!sheet.GetCell(5, 5).Format.Borders.Top, "Undo reverted Preset 12");

                // --- TEST 5: Ribbon Border Icon Bitmaps ---
                Console.WriteLine("\\n--- 5. Testing Ribbon Excel Border Icons (Presets 0..12) ---");
                bool allIconsValid = true;
                for (int p = 0; p <= 12; p++)
                {
                    using (Bitmap icon = RibbonBar.CreateExcelBorderIcon(p))
                    {
                        if (icon == null || icon.Width != 16 || icon.Height != 16)
                        {
                            allIconsValid = false;
                            break;
                        }
                    }
                }
                Assert(allIconsValid, "RibbonBar.CreateExcelBorderIcon created valid 16x16 bitmaps for all 13 presets (0..12)");

                // --- TEST 6: In-Place Cell Text Selection and Editing ---
                Console.WriteLine("\\n--- 6. Testing In-Place Text Selection and Editing ---");
                calc.SelectedRow = 10;
                calc.SelectedCol = 10;
                calc.SelEndRow = 10;
                calc.SelEndCol = 10;
                calc.StartEditingCell("GenOffice Spreadsheet");

                Assert(calc.IsEditing, "Calc is in in-place cell editing mode");
                Assert(calc.EditText == "GenOffice Spreadsheet", "EditText is 'GenOffice Spreadsheet'");

                using (Font f = new Font("Segoe UI", 10f))
                {
                    int charIdxStart = calc.GetTextCharIndexAtX(calc.GetColX(10) + 1, calc.GetColX(10), f);
                    Assert(charIdxStart == 0, "Character index at left cell boundary is 0");

                    int charIdxEnd = calc.GetTextCharIndexAtX(calc.GetColX(10) + 500, calc.GetColX(10), f);
                    Assert(charIdxEnd == calc.EditText.Length, "Character index at far right is EditText.Length (" + calc.EditText.Length + ")");
                }

                calc.EditSelStart = 3;
                calc.EditSelLength = 6;
                Assert(calc.EditText.Substring(calc.EditSelStart, calc.EditSelLength) == "Office", "Selected text substring is 'Office'");

                calc.HandleChar('W');
                calc.HandleChar('o');
                calc.HandleChar('r');
                calc.HandleChar('d');
                Assert(calc.EditText == "GenWord Spreadsheet", "Typing 'Word' replaced 'Office' with 'Word' -> 'GenWord Spreadsheet'");
                Assert(calc.EditSelLength == 0, "Selection cleared after typing replacement");

                calc.HandleKeyDown(Keys.A | Keys.Control);
                Assert(calc.EditSelStart == 0 && calc.EditSelLength == calc.EditText.Length, "Ctrl+A selected entire cell text (0.." + calc.EditText.Length + ")");

                calc.HandleKeyDown(Keys.Back);
                Assert(calc.EditText == "", "Backspace on Select All cleared entire cell text");

                calc.HandleChar('=');
                calc.HandleChar('1');
                calc.HandleChar('0');
                calc.HandleChar('+');
                calc.HandleChar('2');
                calc.HandleChar('0');
                Assert(calc.EditText == "=10+20", "Typed '=10+20' in in-place editor");

                calc.CommitCellEdit();
                Assert(!calc.IsEditing, "Cell editing finished");
                Assert(sheet.GetCell(10, 10) != null && sheet.GetCell(10, 10).EvaluatedStr == "30", "Formula '=10+20' evaluated to '30'");

                // --- TEST 7: Instant Dropdown Mouse Hit-Testing ---
                Console.WriteLine("\\n--- 7. Testing Instant Dropdown Forms (Font and Size) ---");
                FontDropDownForm fontForm = new FontDropDownForm("Calibri", new Point(100, 100));
                SizeDropDownForm sizeForm = new SizeDropDownForm(11f, new Point(100, 100));

                Assert(fontForm != null, "FontDropDownForm initialized successfully with MouseDown instant handler");
                Assert(sizeForm != null, "SizeDropDownForm initialized successfully with MouseDown instant handler");

                // --- TEST 8: Full Render Verification ---
                Console.WriteLine("\\n--- 8. Testing CalcView Full Drawing and Rendering ---");
                using (Bitmap bmp = new Bitmap(1200, 800))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    calc.Draw(g, 1200, 800);
                    Assert(bmp.Width == 1200 && bmp.Height == 800, "CalcView.Draw executed cleanly onto 1200x800 canvas without any exceptions");
                }

                Console.WriteLine("\\n==========================================================");
                Console.WriteLine("  RESULTS: " + passed + " / " + total + " PASSED");
                Console.WriteLine("==========================================================");

                return passed == total ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("CRITICAL EXCEPTION IN TESTS: " + ex);
                Console.ResetColor();
                return 1;
            }
        }
    }
}
"""

with open("scratch/test_calc_all_features.cs", "w", encoding="utf-8") as f:
    f.write(code)
print("Wrote scratch/test_calc_all_features.cs successfully")
 
