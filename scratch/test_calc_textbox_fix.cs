using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    static class Program
    {
        [STAThread]
        static int Main()
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("  CALC TEXTBOX IN-PLACE EDITING & CELL CLICK-THROUGH TEST");
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

                // 1. Insert TextBox into Calc
                calc.InsertTextBox(null);
                DocShapeObject tb = calc.SelectedShape;
                Assert(tb != null, "TextBox shape inserted and selected");
                Assert(tb.ShapeType == "TextBox", "ShapeType is 'TextBox'");
                Assert(tb.IsSelected && tb.IsEditingText, "TextBox is immediately selected and in text editing mode");

                // 2. Type text into TextBox
                string textToType = "Báo cáo doanh thu Quý 3/2026\nKế hoạch tăng trưởng 35%";
                tb.TextContent = textToType;
                tb.TextCursorIndex = textToType.Length;
                Assert(tb.TextContent == textToType, "Typed text inside TextBox: '" + textToType.Replace("\n", "\\n") + "'");

                // 3. Test HitTestShape: Border vs Body
                int headerW = 48;
                int startY = 155 + 30;
                int headerH = 26;
                Rectangle sb = calc.GetShapeScreenBounds(tb, headerW, startY, headerH);

                int bodyClickX = sb.X + sb.Width / 2;
                int bodyClickY = sb.Y + sb.Height / 2;
                int handleBody;
                DocShapeObject hitBody = calc.HitTestShape(bodyClickX, bodyClickY, headerW, startY, headerH, out handleBody);
                Assert(hitBody == tb, "HitTestShape on TextBox body returns the TextBox");
                Assert(handleBody == -1, "HitTestShape on body returns handle = -1 (In-Place Text Editing mode, NOT move handle 8)");

                int borderClickX = sb.X + sb.Width / 4;
                int borderClickY = sb.Y + 2;
                int handleBorder;
                DocShapeObject hitBorder = calc.HitTestShape(borderClickX, borderClickY, headerW, startY, headerH, out handleBorder);
                Assert(hitBorder == tb, "HitTestShape on TextBox border returns the TextBox");
                Assert(handleBorder == 8, "HitTestShape on outer border returns handle = 8 (Move / Drag handle)");

                // 4. Test HandleMouseDown on TextBox Body
                bool handledMouseDown = calc.HandleMouseDown(bodyClickX, bodyClickY, 1280, 800, MouseButtons.Left, null);
                Assert(handledMouseDown == true, "HandleMouseDown returned true (handled, preventing click-through to background cell)");
                Assert(tb.IsEditingText == true, "Shape text editing remains active on body click");
                Assert(!calc.IsEditing, "Sheet in-cell editing was NOT activated");

                // Verify background cell at that coordinate was NOT touched
                uint cIdx = calc.GetColAtX(bodyClickX, headerW);
                uint rIdx = calc.GetRowAtY(bodyClickY, startY + headerH);
                CellData cellUnderneath = calc.CurrentSheet.GetCell(rIdx, cIdx);
                string rawUnder = cellUnderneath != null ? cellUnderneath.RawValue : "";
                Assert(string.IsNullOrEmpty(rawUnder), "Background cell (" + rIdx + "," + cIdx + ") is completely empty (no text leaked)");

                // 5. Test Double-Click on TextBox Body
                calc.HandleDoubleClick(bodyClickX, bodyClickY, 1280, 800);
                Assert(!calc.IsEditing, "Double-clicking on TextBox did NOT activate in-cell editing for cell behind it");
                Assert(tb.IsEditingText == true, "Double-clicking kept in-place TextBox editing active");

                // 6. Test Cursor Detection
                Cursor bodyCursor = calc.GetCursorForPosition(bodyClickX, bodyClickY, 1280, 800);
                Assert(bodyCursor == Cursors.IBeam, "Cursor over TextBox body is IBeam (Text Cursor)");

                Cursor borderCursor = calc.GetCursorForPosition(borderClickX, borderClickY, 1280, 800);
                Assert(borderCursor == Cursors.SizeAll, "Cursor over TextBox border rim is SizeAll (Drag Move Cursor)");

                // 7. Render full visual verification artifact
                using (Bitmap bmp = new Bitmap(1280, 800))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    calc.Draw(g, 1280, 800);

                    string artPath = @"C:\Users\Danh\.gemini\antigravity-ide\brain\d502c5ad-64b2-4f6b-aec5-d943da78498f\calc_textbox_editing_verified.png";
                    bmp.Save(artPath, ImageFormat.Png);
                    Console.WriteLine("[IMAGE] Saved verification artifact to: " + artPath);
                }

                Console.WriteLine("\n==========================================================");
                Console.WriteLine("  RESULTS: " + passed + " / " + total + " PASSED");
                Console.WriteLine("==========================================================");

                return passed == total ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("CRITICAL EXCEPTION: " + ex);
                Console.ResetColor();
                return 1;
            }
        }
    }
}
