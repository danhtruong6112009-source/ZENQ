using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Test
{
    public class TestNeonLightDialog
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Testing Neon Light Dialog Render ===");
            try
            {
                ShapeAnimation demoAnim = new ShapeAnimation
                {
                    Type = AnimationType.GenOffice_NeonPulse,
                    NeonTarget = NeonGlowTarget.Both,
                    NeonColor = unchecked((int)0xFF00F0FF),
                    NeonSecondaryColor = unchecked((int)0xFFFF007F),
                    NeonColorMode = NeonColorMode.DualTone,
                    NeonInfiniteLoop = true,
                    NeonBloomRadius = 32f,
                    NeonIntensity = 1.35f,
                    NeonCornerFlares = true
                };
                ShapeNode demoShape = new ShapeNode
                {
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(0, 0, 240, 120),
                    FillColor = unchecked((int)0xFF141926),
                    StrokeColor = unchecked((int)0xFF00F0FF),
                    StrokeWidth = 3f,
                    Text = "GENOFFICE NEON",
                    TextColor = unchecked((int)0xFFFFFFFF)
                };

                using (NeonEffectOptionsDialogForm dialog = new NeonEffectOptionsDialogForm(demoAnim, demoShape, null))
                {
                    dialog.Show();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(300);

                    using (Bitmap dlgBmp = new Bitmap(dialog.Width, dialog.Height))
                    {
                        dialog.DrawToBitmap(dlgBmp, new Rectangle(0, 0, dialog.Width, dialog.Height));
                        dlgBmp.Save(@"C:\Users\Danh\.gemini\antigravity-ide\brain\8e6530d4-1a0a-4590-90e2-d532581fa545\neon_dialog_white_theme.png", ImageFormat.Png);
                        Console.WriteLine("Saved dialog render to neon_dialog_white_theme.png");
                    }
                    dialog.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Dialog render exception: " + ex.Message);
            }
            Console.WriteLine("Done!");
        }
    }
}
