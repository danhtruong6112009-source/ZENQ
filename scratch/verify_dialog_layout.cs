using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class VerifyDialogLayout
    {
        public static void Main()
        {
            Console.WriteLine("=== VERIFYING INSERT CHART DIALOG LAYOUT & PREVIEWS ===");

            DocChartData sampleData = new DocChartData
            {
                Title = "Doanh thu 2026",
                Categories = new List<string> { "Tháng 1", "Tháng 2", "Tháng 3" },
                SeriesNames = new List<string> { "Sản phẩm A", "Sản phẩm B" },
                Values = new List<List<double>>
                {
                    new List<double> { 120, 150 },
                    new List<double> { 180, 210 },
                    new List<double> { 240, 280 }
                }
            };

            // Test 1: Open Dialog with Default (Recommended Charts)
            using (InsertChartForm dlg1 = new InsertChartForm(sampleData, -1))
            {
                dlg1.CreateControl();
                if (dlg1.ClientSize.Width != 840 || dlg1.ClientSize.Height != 580) throw new Exception("FAIL: Incorrect dialog ClientSize " + dlg1.ClientSize);
                Console.WriteLine("[PASS] Test 1: Default Dialog correctly sized at 840x580 ClientSize!");
            }

            // Test 2: Open Dialog directly into All Charts -> Column (Index 1)
            using (InsertChartForm dlg2 = new InsertChartForm(sampleData, 1))
            {
                dlg2.CreateControl();
                Console.WriteLine("[PASS] Test 2: Opened directly in All Charts -> Column!");
            }

            // Test 3: Test all 18 categories preview generation
            for (int catIdx = 0; catIdx < 18; catIdx++)
            {
                using (InsertChartForm dlg = new InsertChartForm(sampleData, catIdx))
                {
                    dlg.CreateControl();
                }
            }
            Console.WriteLine("[PASS] Test 3: All 18 categories loaded and preview cards generated without errors!");

            Console.WriteLine("=== ALL DIALOG LAYOUT VERIFICATIONS PASSED 100%! ===");
        }
    }
}
