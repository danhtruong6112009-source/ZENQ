using System;
using System.Drawing;
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
        
        // Let's test clicking Card 0
        RectangleF c0 = app.HomeView.GetCardBounds(0, 1280, 800);
        Console.WriteLine("Card 0: " + c0);

        // Call the exact click logic from AppForm.OnMouseDown
        // Let's inspect AppForm.OnMouseDown around line 1130
    }
}
