using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice.UI;

namespace TestOfficeSplash
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                string outDir = @"C:\Users\Danh\Desktop\genofficepy\scratch";
                string[] msgs = new string[]
                {
                    "Đang kiểm tra phiên làm việc trước...",
                    "Đang khôi phục: BaoCaoThuyetTrinh_2026.pptx...",
                    "Khởi tạo hoàn tất. Đang mở Workspace..."
                };
                float[] progs = new float[] { 0.25f, 0.68f, 1.0f };

                for (int i = 0; i < msgs.Length; i++)
                {
                    Bitmap bmp = new Bitmap(580, 320, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        using (var splash = new GenOfficeSplashScreen(false))
                        {
                            var onPaintMethod = typeof(GenOfficeSplashScreen).GetMethod("OnPaint",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            var progField = typeof(GenOfficeSplashScreen).GetField("_progress",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            var msgField = typeof(GenOfficeSplashScreen).GetField("_statusMessage",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            progField.SetValue(splash, progs[i]);
                            msgField.SetValue(splash, msgs[i]);

                            PaintEventArgs pea = new PaintEventArgs(g, new Rectangle(0, 0, 580, 320));
                            onPaintMethod.Invoke(splash, new object[] { pea });
                        }
                    }

                    string outPath = Path.Combine(outDir, string.Format("office_splash_step{0}.png", i + 1));
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("CAPTURED: " + outPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }
    }
}
