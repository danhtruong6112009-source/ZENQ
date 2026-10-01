using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class TestFormSeq : AppForm
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
        Console.WriteLine("=== REPRODUCING USER SEQUENCE ===");

        using (TestFormSeq form = new TestFormSeq())
        {
            form.Size = new Size(1382, 736);
            form.Show();
            form.Visible = false;

            // 1. Restore session (just like src/Program.cs does!)
            SessionManager.RestoreSessionIntoApp(form);

            Console.WriteLine("After session restore:");
            Console.WriteLine("Ribbon.DocTabs count: " + form.Ribbon.DocTabs.Count);
            for (int i = 0; i < form.Ribbon.DocTabs.Count; i++)
            {
                Console.WriteLine(string.Format("Tab {0}: Title='{1}', Mode={2}, FilePath='{3}', Active={4}",
                    i, form.Ribbon.DocTabs[i].Title, form.Ribbon.DocTabs[i].Mode, form.Ribbon.DocTabs[i].FilePath, form.Ribbon.DocTabs[i].Active));
            }
            Console.WriteLine("ActiveDocIndex: " + form.Ribbon.ActiveDocIndex);
            Console.WriteLine("CurrentMode: " + form.Ribbon.CurrentMode);

            using (Bitmap b = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
            using (Graphics g = Graphics.FromImage(b))
            {
                form.Ribbon.Draw(g, form.ClientSize.Width, form.ClientSize.Height, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
            }

            // 2. User clicks "Trang chính" tab to go to HomeView
            // Click Home tab bounds
            Rectangle homeRect = form.Ribbon.DocTabBounds[0];
            Console.WriteLine("\nClicking Home Tab at: " + homeRect);
            form.TestClick(homeRect.X + 10, homeRect.Y + 10);

            Console.WriteLine("After clicking Home Tab: CurrentMode=" + form.Ribbon.CurrentMode + ", ActiveDocIndex=" + form.Ribbon.ActiveDocIndex);

            // 3. User clicks on "Đơn xin nghỉ phép" in template cards (or Promo button)
            RectangleF tmpl0 = form.HomeView.GetTemplateCardBounds(0, form.ClientSize.Width);
            Console.WriteLine("\nClicking template 0 at: " + tmpl0);
            form.TestClick((int)(tmpl0.X + 10), (int)(tmpl0.Y + 10));

            Console.WriteLine("After clicking template 0:");
            Console.WriteLine("CurrentMode=" + form.Ribbon.CurrentMode);
            Console.WriteLine("ActiveDocIndex=" + form.Ribbon.ActiveDocIndex);
            var activeTab = form.Ribbon.DocTabs[form.Ribbon.ActiveDocIndex];
            Console.WriteLine("Active Tab Title: " + activeTab.Title + ", FilePath: " + activeTab.FilePath);
            Console.WriteLine("WriterView text len: " + form.WriterView.PieceTable.GetText().Length);
            Console.WriteLine("WriterView runs: " + form.WriterView.FormattedRuns.Count);

            // 4. Go back to Home
            homeRect = form.Ribbon.DocTabBounds[0];
            form.TestClick(homeRect.X + 10, homeRect.Y + 10);

            // 5. User clicks on a scanned doc in recent list
            float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
            form.HomeView.GetCenterContentMetrics(form.ClientSize.Width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);

            Console.WriteLine("\nClicking recent doc at: " + (startX + 50) + ", " + (listTopY + 20));
            form.TestClick((int)(startX + 50), (int)(listTopY + 20));

            Console.WriteLine("After clicking recent doc:");
            Console.WriteLine("CurrentMode=" + form.Ribbon.CurrentMode);
            Console.WriteLine("ActiveDocIndex=" + form.Ribbon.ActiveDocIndex);
            activeTab = form.Ribbon.DocTabs[form.Ribbon.ActiveDocIndex];
            Console.WriteLine("Active Tab Title: " + activeTab.Title + ", FilePath: " + activeTab.FilePath);
            Console.WriteLine("WriterView text len: " + form.WriterView.PieceTable.GetText().Length);
            Console.WriteLine("WriterView runs: " + form.WriterView.FormattedRuns.Count);
        }
    }
}
