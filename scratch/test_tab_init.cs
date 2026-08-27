using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestTabInit
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING DEFAULT ACTIVE TAB ON STARTUP & NEW DOCS ===");

            RibbonBar ribbon = new RibbonBar();
            if (ribbon.ActiveRibbonTab != 0) throw new Exception("RibbonBar default tab is not 0!");
            Console.WriteLine("[PASS] RibbonBar default tab is: " + ribbon.ActiveRibbonTab + " (Trang đầu)");

            // Add Writer Doc
            ribbon.AddDocTab("Test.docx", OfficeAppMode.Writer);
            if (ribbon.ActiveRibbonTab != 0) throw new Exception("Writer tab default is not 0!");
            Console.WriteLine("[PASS] Writer new tab is on: " + ribbon.ActiveRibbonTab + " (Trang đầu)");

            // Add Calc Doc
            ribbon.AddDocTab("Test.xlsx", OfficeAppMode.Calc);
            if (ribbon.ActiveRibbonTab != 0) throw new Exception("Calc tab default is not 0!");
            Console.WriteLine("[PASS] Calc new tab is on: " + ribbon.ActiveRibbonTab + " (Trang đầu)");

            // Add Slides Doc
            ribbon.AddDocTab("Test.pptx", OfficeAppMode.Slides);
            if (ribbon.ActiveRibbonTab != 0) throw new Exception("Slides tab default is not 0!");
            Console.WriteLine("[PASS] Slides new tab is on: " + ribbon.ActiveRibbonTab + " (Trang đầu)");

            Console.WriteLine("=== ALL DEFAULT TAB CHECKS PASSED SUCCESSFULLY! ===");
        }
    }
}
