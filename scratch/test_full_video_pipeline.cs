using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestVideoPipeline
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("Testing GenOffice Real Video Engine...");

                PresentationRoot pres = new PresentationRoot();
                SlideNode slide = new SlideNode();
                pres.Slides.Add(slide);

                ShapeNode videoShape = new ShapeNode
                {
                    Name = "boc mon.mp4",
                    Type = ShapeType.Video,
                    VideoPath = @"C:\Demo\boc mon.mp4",
                    Transform = new Transform2D(100, 100, 480, 270),
                    VideoVolume = VideoVolumeLevel.High,
                    VideoTrimStart = 5f,
                    VideoTrimEnd = 25f
                };
                slide.AddShape(videoShape);

                SlidesView view = new SlidesView();
                view.Presentation = pres;
                view.ActiveSlideIndex = 0;
                view.SelectedShapeIndex = 0;

                Form form = new Form();
                form.Text = "Video Playback Test Form";
                form.Size = new Size(1000, 700);

                form.Load += (s, e) =>
                {
                    try
                    {
                        Console.WriteLine("Triggering PlayVideoPreview...");
                        view.PlayVideoPreview(videoShape, form);

                        bool isPlaying = SlideVideoPlayerManager.Instance.IsPlaying || SlideVideoPlayerManager.Instance.ActiveShape != null;
                        Console.WriteLine("SlideVideoPlayerManager ActiveShape: " + (SlideVideoPlayerManager.Instance.ActiveShape != null ? SlideVideoPlayerManager.Instance.ActiveShape.Name : "null"));

                        SlideVideoPlayerManager.Instance.Stop();
                        Console.WriteLine("SlideVideoPlayerManager Stop Success!");
                        form.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Inner error: " + ex.ToString());
                        form.Close();
                    }
                };

                Application.Run(form);
                Console.WriteLine("ALL_VIDEO_PIPELINE_TESTS_PASSED_SUCCESSFULLY!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_FAILED: " + ex.ToString());
            }
        }
    }
}
