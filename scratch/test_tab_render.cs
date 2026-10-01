using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;

class TabRendererTest
{
    [STAThread]
    static void Main()
    {
        // Test 1: Active Document tab + multiple open tabs
        {
            RibbonBar ribbon = new RibbonBar();
            ribbon.DocTabs.Add(new DocTab("Báo cáo tháng 9.docx", OfficeAppMode.Writer, false));
            ribbon.DocTabs.Add(new DocTab("Bảng chấm công.xlsx", OfficeAppMode.Calc, false));
            ribbon.DocTabs.Add(new DocTab("Kế hoạch.pptx", OfficeAppMode.Slides, false));
            ribbon.ActiveDocIndex = 1; // Active Writer tab
            ribbon.CurrentMode = OfficeAppMode.Writer;

            int w = 1200, h = 160;
            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                ribbon.Draw(g, w, h, null, null, null, null);
                bmp.Save("scratch\\test_new_tabs_active_doc.png", ImageFormat.Png);
            }
            Console.WriteLine("Saved: scratch\\test_new_tabs_active_doc.png");
        }

        // Test 2: Active Home tab
        {
            RibbonBar ribbon = new RibbonBar();
            ribbon.DocTabs.Add(new DocTab("Báo cáo tháng 9.docx", OfficeAppMode.Writer, false));
            ribbon.ActiveDocIndex = 0; // Active Home tab
            ribbon.CurrentMode = OfficeAppMode.Home;

            int w = 1200, h = 160;
            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                ribbon.Draw(g, w, h, null, null, null, null);
                bmp.Save("scratch\\test_new_tabs_active_home.png", ImageFormat.Png);
            }
            Console.WriteLine("Saved: scratch\\test_new_tabs_active_home.png");
        }

        // Test 3: Hovering over close button of inactive tab (checking NO RED!)
        {
            RibbonBar ribbon = new RibbonBar();
            ribbon.DocTabs.Add(new DocTab("Báo cáo tháng 9.docx", OfficeAppMode.Writer, false));
            ribbon.DocTabs.Add(new DocTab("Bảng chấm công.xlsx", OfficeAppMode.Calc, false));
            ribbon.ActiveDocIndex = 1;
            ribbon.CurrentMode = OfficeAppMode.Writer;

            // Trigger draw once to populate DocTabCloseBounds
            int w = 1200, h = 160;
            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                ribbon.Draw(g, w, h, null, null, null, null);
            }

            // Now set hover to close button of tab 2
            if (ribbon.DocTabCloseBounds.Count > 2)
            {
                Rectangle cRect = ribbon.DocTabCloseBounds[2];
                ribbon.HoverX = cRect.X + cRect.Width / 2;
                ribbon.HoverY = cRect.Y + cRect.Height / 2;
            }

            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                ribbon.Draw(g, w, h, null, null, null, null);
                bmp.Save("scratch\\test_new_tabs_hover_close.png", ImageFormat.Png);
            }
            Console.WriteLine("Saved: scratch\\test_new_tabs_hover_close.png");
        }
    }
}
