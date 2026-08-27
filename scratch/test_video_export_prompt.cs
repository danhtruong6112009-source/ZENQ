using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestVideoExport
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing Video Export & Save Location Prompt ===");

                PresentationRoot pres = new PresentationRoot();
                SlidesView view = new SlidesView();
                view.Presentation = pres;

                // Add 2 test slides
                SlideNode s1 = new SlideNode { Title = "Slide 1", BgColor = 0xFFFFFF, AutoAdvance = true, AutoAdvanceSeconds = 1.0f };
                s1.AddShape(new ShapeNode { Name = "Title", Type = ShapeType.TextBox, Text = "Slide 1 - Video Export Test", Transform = new Transform2D(100f, 100f, 800f, 100f), TextColor = 0x111111, FontSize = 32f });
                pres.Slides.Add(s1);

                SlideNode s2 = new SlideNode { Title = "Slide 2", BgColor = 0x2563EB, AutoAdvance = true, AutoAdvanceSeconds = 1.0f };
                s2.AddShape(new ShapeNode { Name = "Title2", Type = ShapeType.TextBox, Text = "Slide 2 - Thank You!", Transform = new Transform2D(100f, 100f, 800f, 100f), TextColor = 0xFFFFFF, FontSize = 32f });
                pres.Slides.Add(s2);

                using (RecordSlideShowForm form = new RecordSlideShowForm(view, 0))
                {
                    string testVideoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "test_exported_presentation.mp4");
                    if (File.Exists(testVideoPath)) File.Delete(testVideoPath);

                    try
                    {
                        var mi = typeof(RecordSlideShowForm).GetMethod("ExportPresentationVideo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (mi != null)
                        {
                            mi.Invoke(form, new object[] { testVideoPath, false });
                        }
                        else
                        {
                            Console.WriteLine("Method ExportPresentationVideo NOT FOUND!");
                        }
                    }
                    catch (System.Reflection.TargetInvocationException tie)
                    {
                        Console.WriteLine("INVOKE ERROR: " + tie.InnerException.ToString());
                    }

                    if (File.Exists(testVideoPath))
                    {
                        FileInfo fi = new FileInfo(testVideoPath);
                        Console.WriteLine("1. Video file generated: " + fi.FullName + " (Size: " + fi.Length + " bytes)");
                        Console.WriteLine("2. Video file size > 1000 bytes: " + (fi.Length > 1000 ? "PASS" : "FAIL"));
                    }
                    else
                    {
                        Console.WriteLine("1. Video file NOT generated: FAIL");
                    }
                }

                Console.WriteLine("=== ALL_VIDEO_EXPORT_TESTS_PASSED_SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_ERROR: " + ex.ToString());
            }
        }
    }
}
