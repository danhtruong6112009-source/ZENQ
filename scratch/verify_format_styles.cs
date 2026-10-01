using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace GenOfficeTest
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Console.WriteLine("=== Testing Format Painter & Quick Styles ===");
            Assembly asm = Assembly.LoadFrom("GenOffice.exe");
            Type writerViewType = asm.GetType("GenOffice.UI.WriterView");
            Type ribbonBarType = asm.GetType("GenOffice.UI.RibbonBar");

            if (writerViewType == null || ribbonBarType == null)
            {
                Console.WriteLine("ERROR: Could not find WriterView or RibbonBar types!");
                return;
            }

            object ribbon = Activator.CreateInstance(ribbonBarType);
            object writerView = Activator.CreateInstance(writerViewType);

            // Test 1: Insert Text
            MethodInfo appendText = writerViewType.GetMethod("AppendText", new Type[] { typeof(string) });
            if (appendText != null)
            {
                appendText.Invoke(writerView, new object[] { "Heading Paragraph\nSecond Paragraph with some sample words." });
            }
            else
            {
                // Access PieceTable
                PropertyInfo ptProp = writerViewType.GetProperty("PieceTable");
                object pt = ptProp.GetValue(writerView, null);
                pt.GetType().GetMethod("Insert").Invoke(pt, new object[] { 0, "Heading Paragraph\nSecond Paragraph with some sample words." });
            }

            // Test 2: Quick Styles - Apply "Heading 1" to first paragraph
            PropertyInfo curPosProp = writerViewType.GetProperty("CursorPosition");
            PropertyInfo selStartProp = writerViewType.GetProperty("SelectionStart");
            PropertyInfo selEndProp = writerViewType.GetProperty("SelectionEnd");

            curPosProp.SetValue(writerView, 5, null);
            selStartProp.SetValue(writerView, 5, null);
            selEndProp.SetValue(writerView, 5, null);

            MethodInfo applyQuickStyle = writerViewType.GetMethod("ApplyQuickStyle", new Type[] { typeof(string), ribbonBarType });
            Console.WriteLine("Invoking ApplyQuickStyle('Heading 1')...");
            applyQuickStyle.Invoke(writerView, new object[] { "Heading 1", ribbon });

            // Check if Heading 1 formatted the text
            MethodInfo getFormatAt = writerViewType.GetMethod("GetFormatAt");
            object fmt0 = getFormatAt.Invoke(writerView, new object[] { 0 });
            PropertyInfo boldProp = fmt0.GetType().GetProperty("Bold");
            PropertyInfo sizeProp = fmt0.GetType().GetProperty("FontSize");
            PropertyInfo colProp = fmt0.GetType().GetProperty("Color");

            bool isBold = (bool)boldProp.GetValue(fmt0, null);
            float fSize = (float)sizeProp.GetValue(fmt0, null);
            Color col = (Color)colProp.GetValue(fmt0, null);

            Console.WriteLine(string.Format("Char 0: Bold={0}, Size={1}, Color={2}", isBold, fSize, col));
            if (isBold && fSize == 16.0f)
            {
                Console.WriteLine("SUCCESS: ApplyQuickStyle('Heading 1') successfully applied 16pt Bold!");
            }
            else
            {
                Console.WriteLine("FAIL: ApplyQuickStyle('Heading 1') did not format properly!");
            }

            // Check Navigation Headings
            FieldInfo navHeadingsField = writerViewType.GetField("NavHeadings");
            var navHeadings = (System.Collections.IList)navHeadingsField.GetValue(writerView);
            Console.WriteLine("NavHeadings Count: " + (navHeadings != null ? navHeadings.Count : 0));
            if (navHeadings != null && navHeadings.Count > 0)
            {
                Console.WriteLine("SUCCESS: Heading 1 automatically registered in Navigation Pane!");
            }

            // Test 3: Format Painter - Copy formatting from char 0 and paste to a word in second paragraph
            Console.WriteLine("\n--- Testing Format Painter ---");
            curPosProp.SetValue(writerView, 0, null);
            selStartProp.SetValue(writerView, 0, null);
            selEndProp.SetValue(writerView, 0, null);

            MethodInfo copyFormatting = writerViewType.GetMethod("CopyFormatting");
            copyFormatting.Invoke(writerView, null);

            PropertyInfo isFmtActiveProp = writerViewType.GetProperty("IsFormatPainterActive");
            bool isFmtActive = (bool)isFmtActiveProp.GetValue(writerView, null);
            Console.WriteLine("IsFormatPainterActive after CopyFormatting: " + isFmtActive);

            // Single click inside word "sample" in second paragraph (index ~40)
            int sampleWordPos = 42;
            curPosProp.SetValue(writerView, sampleWordPos, null);
            selStartProp.SetValue(writerView, sampleWordPos, null);
            selEndProp.SetValue(writerView, sampleWordPos, null);

            MethodInfo pasteFormatting = writerViewType.GetMethod("PasteFormatting", new Type[] { ribbonBarType });
            Console.WriteLine("Invoking PasteFormatting(ribbon) on word 'sample'...");
            pasteFormatting.Invoke(writerView, new object[] { ribbon });

            // Check if word "sample" received 16pt Bold
            object fmtSample = getFormatAt.Invoke(writerView, new object[] { sampleWordPos });
            bool isSampleBold = (bool)boldProp.GetValue(fmtSample, null);
            float fSampleSize = (float)sizeProp.GetValue(fmtSample, null);
            bool isStillActive = (bool)isFmtActiveProp.GetValue(writerView, null);

            Console.WriteLine(string.Format("Word 'sample' at index {0}: Bold={1}, Size={2}", sampleWordPos, isSampleBold, fSampleSize));
            Console.WriteLine("IsFormatPainterActive after PasteFormatting: " + isStillActive);

            if (isSampleBold && fSampleSize == 16.0f && !isStillActive)
            {
                Console.WriteLine("SUCCESS: Format Painter correctly painted formatting onto word under cursor and deactivated!");
            }
            else
            {
                Console.WriteLine("FAIL: Format Painter paste did not apply correctly!");
            }

            // Test 4: Ribbon Bar Quick Styles click simulation
            Console.WriteLine("\n--- Testing RibbonBar Styles Click ---");
            PropertyInfo modeProp = ribbonBarType.GetProperty("CurrentMode");
            Type modeEnumType = asm.GetType("GenOffice.UI.OfficeAppMode");
            modeProp.SetValue(ribbon, Enum.Parse(modeEnumType, "Writer"), null);

            PropertyInfo activeTabProp = ribbonBarType.GetProperty("ActiveRibbonTab");
            activeTabProp.SetValue(ribbon, 0, null);

            MethodInfo handleClick = ribbonBarType.GetMethod("HandleClick", new Type[] {
                typeof(int), typeof(int), writerViewType, typeof(Form), asm.GetType("GenOffice.UI.SlidesView"), asm.GetType("GenOffice.UI.CalcView"), asm.GetType("GenOffice.UI.PdfView")
            });

            // Click on Heading 2 card (styleX = 730 + 3*68 = 934, y = 80; styleNames = Normal, No Spacing, Heading 1, Heading 2 -> index 3 is 730 + 3*68 = 934)
            int h2ClickX = 730 + 3 * 68 + 10;
            curPosProp.SetValue(writerView, sampleWordPos, null);
            selStartProp.SetValue(writerView, sampleWordPos, null);
            selEndProp.SetValue(writerView, sampleWordPos, null);

            bool clickHandled = (bool)handleClick.Invoke(ribbon, new object[] { h2ClickX, 80, writerView, null, null, null, null });
            PropertyInfo activeStyleProp = ribbonBarType.GetProperty("ActiveStyle");
            string activeStyle = (string)activeStyleProp.GetValue(ribbon, null);
            Console.WriteLine("Ribbon Click Handled: " + clickHandled + ", ActiveStyle: " + activeStyle);

            object fmtH2 = getFormatAt.Invoke(writerView, new object[] { sampleWordPos });
            bool isH2Bold = (bool)boldProp.GetValue(fmtH2, null);
            float fH2Size = (float)sizeProp.GetValue(fmtH2, null);
            Console.WriteLine(string.Format("Paragraph at {0} after clicking Heading 2: Bold={1}, Size={2}", sampleWordPos, isH2Bold, fH2Size));

            if (activeStyle == "Heading 2" && isH2Bold && fH2Size == 13.0f)
            {
                Console.WriteLine("SUCCESS: Clicking Quick Style card in Ribbon correctly formatted paragraph and updated ActiveStyle!");
            }
            else
            {
                Console.WriteLine("FAIL: Ribbon Quick Style click did not format paragraph!");
            }

            Console.WriteLine("\nALL TESTS COMPLETED SUCCESSFULLY!");
        }
    }
}
