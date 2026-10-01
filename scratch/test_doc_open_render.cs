using System;
using System.Drawing;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

class Program
{
    [STAThread]
    static void Main()
    {
        Console.WriteLine("=== SIMULATING APP OPENING DOCUMENT ===");

        int width = 1920;
        int height = 1080;

        HomeView hv = new HomeView();
        RibbonBar ribbon = new RibbonBar();
        WriterView writerView = new WriterView();
        SlidesView slidesView = new SlidesView();
        CalcView calcView = new CalcView();
        PdfView pdfView = new PdfView();

        // 1. Check template click
        string docTitle, openFilePath;
        RectangleF tmplBounds = hv.GetTemplateCardBounds(0, width);
        Console.WriteLine("Template card 0 bounds: " + tmplBounds);
        
        OfficeAppMode? clickedMode = hv.HandleClick((int)(tmplBounds.X + 10), (int)(tmplBounds.Y + 10), width, height, out docTitle, out openFilePath);
        Console.WriteLine("clickedMode: " + clickedMode + ", docTitle: " + docTitle + ", path: " + openFilePath);

        if (clickedMode.HasValue)
        {
            ribbon.AddDocTab(docTitle, clickedMode.Value, slidesView, writerView, calcView, pdfView);
            int activeIdx = ribbon.ActiveDocIndex;
            if (activeIdx >= 0 && activeIdx < ribbon.DocTabs.Count)
            {
                ribbon.DocTabs[activeIdx].DraftFile = "";
            }

            if (!string.IsNullOrEmpty(openFilePath) && File.Exists(openFilePath))
            {
                if (activeIdx >= 0 && activeIdx < ribbon.DocTabs.Count)
                {
                    ribbon.DocTabs[activeIdx].FilePath = openFilePath;
                }
                if (clickedMode.Value == OfficeAppMode.Writer)
                {
                    bool ok = writerView.LoadDocumentFile(openFilePath, ribbon);
                    Console.WriteLine("writerView.LoadDocumentFile result: " + ok);
                    Console.WriteLine("writerView.PieceTable text len: " + writerView.PieceTable.GetText().Length);
                    Console.WriteLine("writerView.FormattedRuns count: " + writerView.FormattedRuns.Count);
                }
            }
            ribbon.SaveTabState(ribbon.ActiveDocIndex, slidesView, writerView, calcView, pdfView);

            // Now draw WriterView to bitmap
            using (Bitmap bmp = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                writerView.Draw(g, width, height, ribbon);
                ribbon.Draw(g, width, height, writerView, slidesView, calcView, pdfView);
                bmp.Save(@"scratch\rendered_opened_doc.png", System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("Saved rendered image to scratch\\rendered_opened_doc.png");
            }
        }

        // Check recent doc click
        Console.WriteLine("\n--- Testing Recent Document Click ---");
        hv.ActiveTab = 0;
        hv.SubFilter = 0;
        float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
        hv.GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);

        Console.WriteLine("RecentDocs count: " + hv.RecentDocs.Count);
        if (hv.RecentDocs.Count > 0)
        {
            Console.WriteLine("First doc in RecentDocs: " + hv.RecentDocs[0].Title + " -> " + hv.RecentDocs[0].FilePath);
            // Click first row
            float clickX = startX + 50;
            float clickY = listTopY + 20;
            OfficeAppMode? rowMode = hv.HandleClick((int)clickX, (int)clickY, width, height, out docTitle, out openFilePath);
            Console.WriteLine("Row click: mode=" + rowMode + ", title=" + docTitle + ", path=" + openFilePath);
        }
    }
}
