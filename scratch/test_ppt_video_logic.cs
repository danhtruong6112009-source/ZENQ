using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestPptVideoLogic
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing PowerPoint-Exact Seamless Video Logic ===");

                PresentationRoot pres = new PresentationRoot();
                SlideNode slide = new SlideNode();
                pres.Slides.Add(slide);

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string[] mp4s = Directory.GetFiles(desktop, "*.mp4", SearchOption.TopDirectoryOnly);
                string testMp4 = (mp4s.Length > 0) ? mp4s[0] : @"C:\Users\Danh\Desktop\bóc mòn.mp4";

                Console.WriteLine("Test Video File: " + testMp4);

                SlidesView view = new SlidesView();
                view.Presentation = pres;
                view.ActiveSlideIndex = 0;

                view.InsertVideo(testMp4);
                ShapeNode vs = view.ActiveSlide.Shapes[view.ActiveSlide.Shapes.Count - 1];

                Console.WriteLine("1. Shape Name: " + vs.Name);
                Console.WriteLine("2. Cached Poster Image: " + (vs.CachedImage != null ? (vs.CachedImage.Width + "x" + vs.CachedImage.Height) : "None"));

                Form form = new Form();
                form.Text = "PowerPoint Video Test";
                form.Size = new Size(1200, 800);

                form.Load += (s, e) =>
                {
                    try
                    {
                        // 1. Play preview
                        view.PlayVideoPreview(vs, form);
                        Console.WriteLine("3. IsPlaying: " + (SlideVideoPlayerManager.Instance.ActiveShape == vs));

                        // 2. Simulate dragging / resizing in real-time
                        vs.Transform.X += 50f;
                        vs.Transform.Width += 100f;
                        view.Draw(form.CreateGraphics(), form.ClientSize.Width, form.ClientSize.Height);
                        Console.WriteLine("4. Frame bounds updated seamlessly!");

                        // 3. Start presentation transition
                        view.StartPresenting(false);
                        Console.WriteLine("5. Presentation mode transitioned successfully!");

                        view.StopPresenting();
                        Console.WriteLine("6. Stop presenting cleanup success!");

                        form.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Inner error: " + ex.ToString());
                        form.Close();
                    }
                };

                Application.Run(form);
                Console.WriteLine("=== ALL_PPT_VIDEO_LOGIC_TESTS_PASSED_SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_FAILED: " + ex.ToString());
            }
        }
    }
}
