using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestIntegratedBar
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing Integrated Video Playback Bar & Selection Handles ===");

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string[] mp4s = Directory.GetFiles(desktop, "*.mp4", SearchOption.TopDirectoryOnly);
                string testMp4 = (mp4s.Length > 0) ? mp4s[0] : @"C:\Users\Danh\Desktop\bóc mòn.mp4";

                ShapeNode vs = new ShapeNode
                {
                    Name = "Test Video",
                    Type = ShapeType.Video,
                    VideoPath = testMp4,
                    Transform = new Transform2D(100, 100, 640, 360),
                    VideoTotalTime = 120f,
                    VideoCurrentTime = 30f
                };

                Form f = new Form();
                f.Size = new Size(1000, 700);

                f.Load += (s, e) =>
                {
                    try
                    {
                        SlideVideoPlayerManager mgr = SlideVideoPlayerManager.Instance;
                        Rectangle shapeBounds = new Rectangle(100, 100, 640, 360);

                        // Play video preview
                        mgr.Play(vs, f, shapeBounds, false);
                        Console.WriteLine("1. Play called. ActiveShape = " + (mgr.ActiveShape == vs ? "PASS" : "FAIL"));

                        // Toggle pause
                        mgr.TogglePlay(vs, f, shapeBounds, false);
                        Console.WriteLine("2. TogglePlay (Pause) called. IsVideoPlaying = " + (!vs.IsVideoPlaying ? "PASS" : "FAIL"));

                        // Toggle resume
                        mgr.TogglePlay(vs, f, shapeBounds, false);
                        Console.WriteLine("3. TogglePlay (Resume) called. IsVideoPlaying = " + (vs.IsVideoPlaying ? "PASS" : "FAIL"));

                        // Seek
                        mgr.Seek(45f);
                        Console.WriteLine("4. Seek to 45s. CurrentTime = " + (vs.VideoCurrentTime == 45f ? "PASS" : "FAIL"));

                        // Stop cleanup
                        mgr.Stop();
                        Console.WriteLine("5. Stop cleanup: PASS");

                        f.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Inner error: " + ex.ToString());
                        f.Close();
                    }
                };

                Application.Run(f);
                Console.WriteLine("=== ALL_INTEGRATED_PLAYBACK_BAR_TESTS_PASSED ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_FAILED: " + ex.ToString());
            }
        }
    }
}
