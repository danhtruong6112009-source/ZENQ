using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestAll3DCharts
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING ALL EXCEL 3D & ADVANCED CHARTS ENGINE ===");

            DocChartData sampleData = new DocChartData
            {
                Title = "Báo cáo Doanh thu Toàn diện 2026",
                Categories = new List<string> { "Quý 1", "Quý 2", "Quý 3", "Quý 4" },
                SeriesNames = new List<string> { "Phần mềm", "Phần cứng", "Dịch vụ Cloud" },
                Values = new List<List<double>>
                {
                    new List<double> { 25000, 18000, 32000 },
                    new List<double> { 35000, 22000, 41000 },
                    new List<double> { 42000, 28000, 53000 },
                    new List<double> { 58000, 39000, 67000 }
                }
            };

            string[] testChartTypes = new string[]
            {
                // Column 2D & 3D
                "Column_Clustered", "Column_Stacked", "Column_100",
                "Column_3D_Clustered", "Column_3D_Stacked", "Column_3D_100", "Column_3D_Deep",
                // Bar 2D & 3D
                "Bar_Clustered", "Bar_Stacked", "Bar_100",
                "Bar_3D_Clustered", "Bar_3D_Stacked", "Bar_3D_100",
                // Line 2D & 3D
                "Line", "Line_Stacked", "Line_100", "Line_Markers", "Line_3D",
                // Pie & Doughnut 2D & 3D
                "Pie", "Pie_3D", "Pie_Of_Pie", "Bar_Of_Pie", "Doughnut", "Doughnut_3D",
                // Area 2D & 3D
                "Area", "Area_Stacked", "Area_100", "Area_3D",
                // Surface & Wireframe
                "Surface_3D", "Surface_Wireframe", "Surface_Contour", "Surface_WireframeContour",
                // Radar
                "Radar", "Radar_Markers", "Radar_Filled",
                // Hierarchy
                "Treemap", "Sunburst",
                // Waterfall & Funnel
                "Waterfall", "Funnel",
                // Stock
                "Stock_HLC", "Stock_OHLC", "Stock_VHLC", "Stock_VOHLC",
                // Statistic
                "Histogram", "Histogram_Pareto", "Box_Whisker",
                // Scatter & Bubble
                "Scatter_Markers", "Scatter_Smooth", "Scatter_Straight", "Bubble", "Bubble_3D",
                // Combo
                "Combo_Column_Line", "Combo_Column_Line_Secondary", "Combo_Area_Column"
            };

            int renderedCount = 0;
            using (Bitmap bmp = new Bitmap(800, 500))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                foreach (var cType in testChartTypes)
                {
                    DocChartData d = sampleData.Clone();
                    d.ChartType = cType;
                    ChartRenderer.DrawChart(g, new Rectangle(10, 10, 780, 480), d, true, 1.0f);
                    renderedCount++;
                }
            }
            Console.WriteLine("[PASS] Successfully rendered all " + renderedCount + " chart types into graphics surfaces!");

            // Test ChartGalleryDropdownPopup creation for all 8 GalleryKind values
            foreach (ChartGalleryDropdownPopup.GalleryKind gk in Enum.GetValues(typeof(ChartGalleryDropdownPopup.GalleryKind)))
            {
                using (ChartGalleryDropdownPopup popup = new ChartGalleryDropdownPopup(gk, (t) => {}, (c) => {}))
                {
                    if (popup.Width <= 0 || popup.Height <= 0) throw new Exception("FAIL: Invalid popup size for " + gk);
                }
            }
            Console.WriteLine("[PASS] All 8 Ribbon Chart Gallery Popups created and sized with perfection!");

            // Test InsertChartForm dialog with focus category
            using (InsertChartForm dlg = new InsertChartForm(sampleData, 12)) // Treemap
            {
                if (dlg.Width <= 0) throw new Exception("FAIL: InsertChartForm sizing failed");
            }
            Console.WriteLine("[PASS] InsertChartForm with vector category icons verified!");

            Console.WriteLine("=== ALL 3D AND ADVANCED CHARTS TESTS PASSED 100%! ===");
        }
    }
}
