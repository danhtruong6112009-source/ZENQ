using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class CaptureDropdownGallery
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== Capturing Animations Gallery DropDown Form ===");

                AnimationsGalleryDropDownForm galleryForm = new AnimationsGalleryDropDownForm(new Point(50, 50), AnimationType.GenOffice_NeonPulse);
                galleryForm.Show();

                Application.DoEvents();

                int w = galleryForm.Width;
                int h = galleryForm.Height;
                using (Bitmap bmp = new Bitmap(w, h))
                {
                    galleryForm.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
                    string outPath = @"C:\Users\Danh\.gemini\antigravity-ide\brain\8e6530d4-1a0a-4590-90e2-d532581fa545\animations_gallery_dropdown.png";
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("[PASS] Dropdown Gallery screenshot saved to: " + outPath);
                }

                galleryForm.Close();
                galleryForm.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: " + ex);
            }
        }
    }
}
