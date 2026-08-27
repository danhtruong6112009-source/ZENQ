using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice.UI;

namespace TestSplash
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                int[] captureTimesMs = new int[] { 300, 900, 1600, 2200 };
                string outDir = @"C:\Users\Danh\Desktop\genofficepy\scratch";

                for (int i = 0; i < captureTimesMs.Length; i++)
                {
                    int timeMs = captureTimesMs[i];
                    Bitmap bmp = new Bitmap(720, 420, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        using (var splash = new GenOfficeSplashScreen())
                        {
                            // Trigger OnPaint via reflection or direct Draw
                            var onPaintMethod = typeof(GenOfficeSplashScreen).GetMethod("OnPaint",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            // Set time / stopwatch
                            var swField = typeof(GenOfficeSplashScreen).GetField("_stopwatch",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            var sw = new System.Diagnostics.Stopwatch();
                            swField.SetValue(splash, sw);

                            var progField = typeof(GenOfficeSplashScreen).GetField("_progress",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            var msgField = typeof(GenOfficeSplashScreen).GetField("_statusMessage",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            if (timeMs <= 400)
                            {
                                progField.SetValue(splash, 0.15f);
                                msgField.SetValue(splash, "Khởi tạo lõi đồ họa phần cứng & Giao diện Vector 60 FPS...");
                            }
                            else if (timeMs <= 1000)
                            {
                                progField.SetValue(splash, 0.42f);
                                msgField.SetValue(splash, "Nạp bộ giải mã tài liệu OpenXML (DOCX, XLSX, PPTX)...");
                            }
                            else if (timeMs <= 1800)
                            {
                                progField.SetValue(splash, 0.78f);
                                msgField.SetValue(splash, "Kích hoạt công cụ chuyển cảnh Morph & Animations...");
                            }
                            else
                            {
                                progField.SetValue(splash, 0.98f);
                                msgField.SetValue(splash, "Sẵn sàng! Đang mở GenOffice Workspace...");
                            }

                            PaintEventArgs pea = new PaintEventArgs(g, new Rectangle(0, 0, 720, 420));
                            onPaintMethod.Invoke(splash, new object[] { pea });
                        }
                    }

                    string outPath = Path.Combine(outDir, string.Format("splash_frame_{0}ms.png", timeMs));
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
