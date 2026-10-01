using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using GenOffice;
using GenOffice.UI;

class Program
{
    [STAThread]
    static void Main()
    {
        AppForm form = new AppForm();
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(100, 100);
        form.Size = new Size(1200, 800);
        
        // Show form
        form.Show();
        Application.DoEvents();
        Thread.Sleep(500);
        Application.DoEvents();

        // Capture the form window including borders
        Rectangle rect = form.Bounds;
        using (Bitmap bmp = new Bitmap(rect.Width, rect.Height))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
            }
            bmp.Save("scratch/actual_live_appform.png", ImageFormat.Png);
        }

        form.Close();
        Console.WriteLine("Saved scratch/actual_live_appform.png. Size: " + rect.Width + "x" + rect.Height);
    }
}
