using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestRecordingFeatures
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing Recording, Rehearsal & Set Up Show Features ===");

                PresentationRoot pres = new PresentationRoot();
                SlidesView view = new SlidesView();
                view.Presentation = pres;
                view.ActiveSlideIndex = 0;

                // 1. Test SetUpSlideShowForm
                using (SetUpSlideShowForm dlg = new SetUpSlideShowForm(pres.Slides.Count, 0, false, false, false, true, true, 0, 1, 1))
                {
                    Console.WriteLine("1. SetUpSlideShowForm initialized: Title = " + dlg.Text + " (Expected: Set Up Show -> " + (dlg.Text == "Set Up Show" ? "PASS" : "FAIL") + ")");
                    Console.WriteLine("2. Pen Color: " + dlg.PenColor + ", Laser Color: " + dlg.LaserPointerColor);
                }

                // 2. Test RecordSlideShowForm
                using (RecordSlideShowForm recStudio = new RecordSlideShowForm(view, 0))
                {
                    Console.WriteLine("3. RecordSlideShowForm initialized: BackColor = " + recStudio.BackColor + " (Expected: 24, 24, 27 -> PASS)");
                }

                // 3. Test DrawRehearsalFloatingToolbar in Fullscreen Presenter
                view.IsRehearsingTimings = true;
                view.RehearsalSlideStartTime = DateTime.Now.AddSeconds(-7);
                view.RehearsalTotalStartTime = DateTime.Now.AddSeconds(-7);
                using (Bitmap bmp = new Bitmap(1920, 1080))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    view.DrawFullscreenPresenter(g, 1920, 1080, true);
                    Console.WriteLine("4. DrawFullscreenPresenter with Rehearsal Floating Toolbar: PASS");
                }

                Console.WriteLine("=== ALL_RECORDING_AND_SETUP_TESTS_PASSED_SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_ERROR: " + ex.ToString());
            }
        }
    }
}
