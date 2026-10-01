using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;

public class RenderRealRibbon
{
    [STAThread]
    public static void Main()
    {
        int w = 1200, h = 160;
        RibbonBar ribbon = new RibbonBar();
        ribbon.DocTabs.Add(new DocTab("Báo cáo tháng 9.docx", OfficeAppMode.Writer, false));
        ribbon.ActiveDocIndex = 1;
        ribbon.CurrentMode = OfficeAppMode.Writer;
        ribbon.Alignment = 0; // Align Left active
        ribbon.IsBold = true;

        using (Bitmap fullBmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(fullBmp))
        {
            g.Clear(Color.White);
            ribbon.Draw(g, w, h, null, null, null, null);
            fullBmp.Save("scratch\\rendered_real_ribbon_full.png", ImageFormat.Png);

            // Crop Font and Paragraph area (X: 240 to 730, Y: 60 to 150)
            int cropX = 240;
            int cropY = 60;
            int cropW = 490;
            int cropH = 90;

            using (Bitmap cropBmp = new Bitmap(cropW, cropH))
            using (Graphics gCrop = Graphics.FromImage(cropBmp))
            {
                gCrop.DrawImage(fullBmp, new Rectangle(0, 0, cropW, cropH), new Rectangle(cropX, cropY, cropW, cropH), GraphicsUnit.Pixel);
                cropBmp.Save("scratch\\rendered_ribbon_crop.png", ImageFormat.Png);

                // 3x magnified
                using (Bitmap magBmp = new Bitmap(cropW * 3, cropH * 3))
                using (Graphics gMag = Graphics.FromImage(magBmp))
                {
                    gMag.InterpolationMode = InterpolationMode.NearestNeighbor;
                    gMag.PixelOffsetMode = PixelOffsetMode.Half;
                    gMag.DrawImage(cropBmp, 0, 0, magBmp.Width, magBmp.Height);
                    magBmp.Save("scratch\\rendered_ribbon_crop_3x.png", ImageFormat.Png);
                }
            }
        }

        // Test TextEffectsDropDownForm rendering
        using (TextEffectsDropDownForm fxForm = new TextEffectsDropDownForm(new Point(100, 100), null))
        using (Bitmap fxBmp = new Bitmap(fxForm.Width, fxForm.Height))
        {
            fxForm.DrawToBitmap(fxBmp, new Rectangle(0, 0, fxForm.Width, fxForm.Height));
            fxBmp.Save("scratch\\rendered_texteffects_form.png", ImageFormat.Png);
        }

        Console.WriteLine("SUCCESS: Rendered real ribbon and text effects form.");
    }
}
