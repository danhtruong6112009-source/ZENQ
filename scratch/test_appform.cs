using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Format;
using GenOffice.UI;

class TestForm : AppForm
{
    public void TestClick(int x, int y)
    {
        OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, x, y, 0));
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        Console.WriteLine("=== TESTING REAL APPFORM DOC OPENING ===");
        using (TestForm form = new TestForm())
        {
            form.Size = new Size(1920, 1080);
            form.Show();
            form.Visible = false;

            Console.WriteLine("AppForm loaded. CurrentMode: " + form.Ribbon.CurrentMode);
            Console.WriteLine("HomeView.RecentDocs count: " + form.HomeView.RecentDocs.Count);

            // Test 1: Click template card 0
            RectangleF tmpl0 = form.HomeView.GetTemplateCardBounds(0, form.ClientSize.Width);
            Console.WriteLine("Clicking template at: " + tmpl0);

            form.TestClick((int)(tmpl0.X + 10), (int)(tmpl0.Y + 10));

            Console.WriteLine("After click, CurrentMode: " + form.Ribbon.CurrentMode);
            Console.WriteLine("ActiveDocIndex: " + form.Ribbon.ActiveDocIndex);
            if (form.Ribbon.ActiveDocIndex >= 0)
            {
                var tab = form.Ribbon.DocTabs[form.Ribbon.ActiveDocIndex];
                Console.WriteLine("Active tab title: " + tab.Title + ", path: " + tab.FilePath);
                Console.WriteLine("WriterView text len: " + form.WriterView.PieceTable.GetText().Length);
                Console.WriteLine("WriterView formatted runs: " + form.WriterView.FormattedRuns.Count);

                using (Bitmap bmp = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                {
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height));
                    bmp.Save(@"scratch\appform_opened_template.png", System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine("Saved scratch\\appform_opened_template.png");
                }
            }

            // Switch back to Home
            form.Ribbon.SwitchDocTab(0, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
            Console.WriteLine("\nSwitched back to Home: CurrentMode=" + form.Ribbon.CurrentMode);

            // Test 2: Click first scanned doc in recent list
            float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
            form.HomeView.GetCenterContentMetrics(form.ClientSize.Width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);

            Console.WriteLine("Clicking scanned doc row at listTopY + 20: " + (listTopY + 20));
            form.TestClick((int)(startX + 50), (int)(listTopY + 20));

            Console.WriteLine("After recent click, CurrentMode: " + form.Ribbon.CurrentMode);
            Console.WriteLine("ActiveDocIndex: " + form.Ribbon.ActiveDocIndex);
            if (form.Ribbon.ActiveDocIndex >= 0)
            {
                var tab = form.Ribbon.DocTabs[form.Ribbon.ActiveDocIndex];
                Console.WriteLine("Active tab title: " + tab.Title + ", path: " + tab.FilePath);
                Console.WriteLine("WriterView text len: " + form.WriterView.PieceTable.GetText().Length);
                Console.WriteLine("WriterView formatted runs: " + form.WriterView.FormattedRuns.Count);

                using (Bitmap bmp = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                {
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height));
                    bmp.Save(@"scratch\appform_opened_recent.png", System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine("Saved scratch\\appform_opened_recent.png");
                }
            }
        }
    }
}
