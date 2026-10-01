using System;
using System.Windows.Forms;
using GenOffice.UI;
using GenOffice.Core;

class SimulateApp
{
    [STAThread]
    static void Main()
    {
        AppForm app = new AppForm();
        app.Size = new System.Drawing.Size(1280, 800);
        
        // Check initial state
        Console.WriteLine("DocTabs count before: " + app.Ribbon.DocTabs.Count);
        for (int i = 0; i < app.Ribbon.DocTabs.Count; i++)
        {
            Console.WriteLine("  Tab {0}: Title='{1}', Mode={2}", i, app.Ribbon.DocTabs[i].Title, app.Ribbon.DocTabs[i].Mode);
        }

        // Simulate restore session if any
        SessionManager.RestoreSessionIntoApp(app);
        Console.WriteLine("DocTabs count after RestoreSession: " + app.Ribbon.DocTabs.Count);
        for (int i = 0; i < app.Ribbon.DocTabs.Count; i++)
        {
            Console.WriteLine("  Tab {0}: Title='{1}', Mode={2}", i, app.Ribbon.DocTabs[i].Title, app.Ribbon.DocTabs[i].Mode);
        }

        if (app.WriterView != null && app.WriterView.PieceTable != null)
        {
            string txt = app.WriterView.PieceTable.GetText();
            Console.WriteLine("WriterView text len: " + txt.Length);
            if (txt.Length > 0)
            {
                Console.WriteLine("WriterView text preview:\n" + txt.Substring(0, Math.Min(200, txt.Length)));
            }
        }
    }
}
