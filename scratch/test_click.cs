using System;
using System.IO;
using System.Drawing;
using GenOffice.UI;
using GenOffice;

class TestClick
{
    [STAThread]
    static void Main()
    {
        HomeView hv = new HomeView();
        string title;
        RectangleF c0 = hv.GetCardBounds(0, 1280, 800);
        Console.WriteLine("Card 0 bounds: {0}", c0);
        OfficeAppMode? mode = hv.HandleClick((int)(c0.X + 10), (int)(c0.Y + 10), 1280, 800, out title);
        Console.WriteLine("Clicked Card 0: mode={0}, title={1}", mode, title);
        var rDoc = hv.RecentDocs.Find(d => d.Title == title);
        Console.WriteLine("Found in RecentDocs? {0}", (rDoc != null));
        if (rDoc != null) Console.WriteLine("Path: {0}", rDoc.FilePath);

        RectangleF t0 = hv.GetTemplateCardBounds(0, 1280);
        Console.WriteLine("Template 0 bounds: {0}", t0);
    }
}
