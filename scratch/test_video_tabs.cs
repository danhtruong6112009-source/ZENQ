using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestVideoRibbon
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                PresentationRoot pres = new PresentationRoot();
                SlideNode slide = pres.Slides[0];

                // Create a sample video shape
                ShapeNode videoShape = new ShapeNode
                {
                    Name = "Demo Video Presentation.mp4",
                    Type = ShapeType.Video,
                    VideoPath = "C:\\Demo\\Presentation.mp4",
                    VideoStyleIndex = 1,
                    VideoStartMode = VideoStartMode.InClickSequence,
                    VideoVolume = VideoVolumeLevel.High,
                    VideoBookmarks = new System.Collections.Generic.List<float> { 15f, 45f },
                    Transform = new Transform2D(160, 90, 640, 360),
                    FillColor = 0x0F172A,
                    StrokeColor = 0xFFFFFF,
                    StrokeWidth = 2.0f,
                    Text = "",
                    TextColor = 0xFFFFFF
                };
                slide.AddShape(videoShape);

                SlidesView slidesView = new SlidesView();
                slidesView.Presentation = pres;
                slidesView.SelectedShapeIndex = 0;

                RibbonBar ribbon = new RibbonBar();
                ribbon.CurrentMode = OfficeAppMode.Slides;

                // 1. Render Tab 7: Định dạng Video (Video Format)
                ribbon.ActiveRibbonTab = 7;
                Bitmap bmpTab7 = new Bitmap(1300, 160);
                using (Graphics g = Graphics.FromImage(bmpTab7))
                {
                    g.Clear(Color.White);
                    ribbon.Draw(g, 1300, 160, slidesView: slidesView);
                }
                bmpTab7.Save(@"C:\Users\Danh\.gemini\antigravity-ide\brain\d502c5ad-64b2-4f6b-aec5-d943da78498f\tab_video_format_rendered.png", ImageFormat.Png);

                // 2. Render Tab 8: Phát lại (Playback)
                ribbon.ActiveRibbonTab = 8;
                Bitmap bmpTab8 = new Bitmap(1300, 160);
                using (Graphics g = Graphics.FromImage(bmpTab8))
                {
                    g.Clear(Color.White);
                    ribbon.Draw(g, 1300, 160, slidesView: slidesView);
                }
                bmpTab8.Save(@"C:\Users\Danh\.gemini\antigravity-ide\brain\d502c5ad-64b2-4f6b-aec5-d943da78498f\tab_video_playback_rendered.png", ImageFormat.Png);

                // 3. Render SetUpSlideShowForm
                using (SetUpSlideShowForm setupForm = new SetUpSlideShowForm(10, 1, false, false, false, true, true, 0, 1, 10))
                {
                    setupForm.Size = new Size(640, 530);
                    Bitmap bmpSetup = new Bitmap(640, 530);
                    setupForm.DrawToBitmap(bmpSetup, new Rectangle(0, 0, 640, 530));
                    bmpSetup.Save(@"C:\Users\Danh\.gemini\antigravity-ide\brain\d502c5ad-64b2-4f6b-aec5-d943da78498f\setup_slideshow_dialog_verified.png", ImageFormat.Png);
                }

                Console.WriteLine("SUCCESS_ALL_RENDERED");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}
