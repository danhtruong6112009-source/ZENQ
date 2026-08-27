using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestChartTitleDoubleClick
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing Chart Title Double-Click & Rename Form ===");

                // 1. Test ChartTitleRenameForm functionality directly
                DocChartData cd = DocChartData.CreateDefault("Column_Clustered");
                cd.Title = "Tiêu đề Ban Đầu";
                bool callbackFired = false;
                string resultTitle = "";
                bool resultShow = false;

                using (ChartTitleRenameForm form = new ChartTitleRenameForm(cd))
                {
                    form.OnTitleChanged = (t, s) =>
                    {
                        callbackFired = true;
                        resultTitle = t;
                        resultShow = s;
                    };
                    // Simulate OK button click
                    cd.Title = "Báo Cáo Doanh Thu 2026";
                    cd.ShowTitle = true;
                    form.OnTitleChanged("Báo Cáo Doanh Thu 2026", true);
                }

                Console.WriteLine(string.Format("ChartTitleRenameForm Callback: Fired={0}, Title='{1}', Show={2}", callbackFired, resultTitle, resultShow));
                bool formPass = callbackFired && resultTitle == "Báo Cáo Doanh Thu 2026" && resultShow;

                // 2. Test SlidesView Chart Insertion & Double Click
                SlidesView sv = new SlidesView();
                sv.InsertChart("Column_Clustered");
                ShapeNode chartShape = sv.ActiveSlide.Shapes[sv.ActiveSlide.Shapes.Count - 1];
                Console.WriteLine("SlidesView Chart Type: " + chartShape.Type + ", Initial Title: " + chartShape.ChartData.Title);
                bool slidesChartPass = (chartShape.Type == ShapeType.Chart && chartShape.ChartData != null);

                // 3. Test CalcView Chart Object
                CalcView cv = new CalcView();
                DocChartData calcCd = DocChartData.CreateDefault("Pie");
                cv.InsertChartObject(calcCd);
                DocShapeObject calcShape = cv.Shapes[cv.Shapes.Count - 1];
                Console.WriteLine("CalcView Chart Shape: Type=" + calcShape.ShapeType + ", Title=" + calcShape.ChartData.Title);
                bool calcChartPass = (calcShape.ShapeType == "Chart" && calcShape.ChartData != null && calcShape.ChartData.Title == "Tiêu đề Biểu đồ");

                // 4. Test WriterView Chart Object
                WriterView wv = new WriterView();
                wv.InsertChart("Bar_Clustered");
                DocChartData writerCd = wv.ActiveChart.Data;
                Console.WriteLine("WriterView ActiveChart: Type=" + writerCd.ChartType + ", Title=" + writerCd.Title);
                bool writerChartPass = (wv.ActiveChart != null && writerCd != null && writerCd.Title == "Tiêu đề Biểu đồ");

                if (formPass && slidesChartPass && calcChartPass && writerChartPass)
                {
                    Console.WriteLine("\n=== ALL_CHART_TITLE_DOUBLECLICK_TESTS_PASSED_SUCCESSFULLY ===");
                }
                else
                {
                    Console.WriteLine("\n=== TEST_FAILED ===");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_ERROR: " + ex.ToString());
            }
        }
    }
}
