using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Setup;

namespace GenOffice.Test
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            // 1. Capture Real SetupForm Step 1
            SetupForm frm = new SetupForm();
            frm.Show();
            Application.DoEvents();
            System.Threading.Thread.Sleep(200);

            Bitmap bmp1 = new Bitmap(frm.Width, frm.Height);
            frm.DrawToBitmap(bmp1, new Rectangle(0, 0, frm.Width, frm.Height));
            bmp1.Save(@"scratch\rendered_real_step1.png", ImageFormat.Png);

            // 2. Switch to Step 2 and Capture Real SetupForm Step 2
            Type type = typeof(SetupForm);
            System.Reflection.MethodInfo mi = type.GetMethod("SwitchToStep", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mi != null)
            {
                mi.Invoke(frm, new object[] { SetupStep.InstallOptions });
                Application.DoEvents();
                System.Threading.Thread.Sleep(200);

                Bitmap bmp2 = new Bitmap(frm.Width, frm.Height);
                frm.DrawToBitmap(bmp2, new Rectangle(0, 0, frm.Width, frm.Height));
                bmp2.Save(@"scratch\rendered_real_step2.png", ImageFormat.Png);
            }
            frm.Close();

            // 3. Capture Real UninstallForm
            UninstallForm unfrm = new UninstallForm();
            unfrm.Show();
            Application.DoEvents();
            System.Threading.Thread.Sleep(200);

            Bitmap bmpUn = new Bitmap(unfrm.Width, unfrm.Height);
            unfrm.DrawToBitmap(bmpUn, new Rectangle(0, 0, unfrm.Width, unfrm.Height));
            bmpUn.Save(@"scratch\rendered_real_uninstall.png", ImageFormat.Png);
            unfrm.Close();

            Console.WriteLine("REAL FORMS RENDERED SUCCESSFULLY!");
        }
    }
}
