using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class CaptureAiTab
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppForm form = new AppForm();
            form.Size = new Size(1366, 768);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Show();

            // Switch to HomeView tab 3 (Thiết lập AI)
            form.ShowAiConfig();
            Application.DoEvents();

            using (Bitmap bmp = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
            {
                form.DrawToBitmap(bmp, new Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height));
                bmp.Save("scratch/rendered_ai_tab_full.png");
            }

            form.Close();
            Console.WriteLine("[PASS] Captured full AI tab to scratch/rendered_ai_tab_full.png");
        }
    }
}
