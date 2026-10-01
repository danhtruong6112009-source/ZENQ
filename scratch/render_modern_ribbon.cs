using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Threading;
using GenOffice;
using GenOffice.UI;

namespace GenOfficeTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Initializing MainForm with modern WPF vector ribbon...");
                using (MainForm form = new MainForm())
                {
                    form.Size = new Size(1366, 768);
                    form.Show();
                    Application.DoEvents();

                    // Switch to Slides module
                    form.SwitchToModule(AppMode.Slides);
                    Application.DoEvents();
                    Thread.Sleep(500);

                    // Find RibbonBar control
                    RibbonBar ribbon = null;
                    foreach (Control c in form.Controls)
                    {
                        if (c is RibbonBar rb)
                        {
                            ribbon = rb;
                            break;
                        }
                    }

                    if (ribbon == null)
                    {
                        Console.WriteLine("RibbonBar not found directly in form controls, searching hierarchy...");
                        ribbon = FindControl<RibbonBar>(form);
                    }

                    if (ribbon != null)
                    {
                        Console.WriteLine("RibbonBar found! Rendering Slides tabs with modern WPF vector graphics...");

                        // Render Home Tab (tab 0)
                        ribbon.ActiveTabIndex = 0;
                        form.Refresh();
                        Application.DoEvents();
                        Thread.Sleep(300);
                        using (Bitmap bmp = new Bitmap(ribbon.Width, ribbon.Height))
                        {
                            ribbon.DrawToBitmap(bmp, new Rectangle(0, 0, ribbon.Width, ribbon.Height));
                            bmp.Save("scratch/wpf_slides_tab0_home.png", ImageFormat.Png);
                            bmp.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\wpf_slides_tab0_home.png", ImageFormat.Png);
                            Console.WriteLine("Saved wpf_slides_tab0_home.png");
                        }

                        // Render Insert Tab (tab 1)
                        ribbon.ActiveTabIndex = 1;
                        form.Refresh();
                        Application.DoEvents();
                        Thread.Sleep(300);
                        using (Bitmap bmp = new Bitmap(ribbon.Width, ribbon.Height))
                        {
                            ribbon.DrawToBitmap(bmp, new Rectangle(0, 0, ribbon.Width, ribbon.Height));
                            bmp.Save("scratch/wpf_slides_tab1_insert.png", ImageFormat.Png);
                            bmp.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\wpf_slides_tab1_insert.png", ImageFormat.Png);
                            Console.WriteLine("Saved wpf_slides_tab1_insert.png");
                        }

                        // Render Transitions Tab (tab 3)
                        ribbon.ActiveTabIndex = 3;
                        form.Refresh();
                        Application.DoEvents();
                        Thread.Sleep(300);
                        using (Bitmap bmp = new Bitmap(ribbon.Width, ribbon.Height))
                        {
                            ribbon.DrawToBitmap(bmp, new Rectangle(0, 0, ribbon.Width, ribbon.Height));
                            bmp.Save("scratch/wpf_slides_tab3_transitions.png", ImageFormat.Png);
                            bmp.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\wpf_slides_tab3_transitions.png", ImageFormat.Png);
                            Console.WriteLine("Saved wpf_slides_tab3_transitions.png");
                        }

                        // Render Slide Show Tab (tab 5)
                        ribbon.ActiveTabIndex = 5;
                        form.Refresh();
                        Application.DoEvents();
                        Thread.Sleep(300);
                        using (Bitmap bmp = new Bitmap(ribbon.Width, ribbon.Height))
                        {
                            ribbon.DrawToBitmap(bmp, new Rectangle(0, 0, ribbon.Width, ribbon.Height));
                            bmp.Save("scratch/wpf_slides_tab5_slideshow.png", ImageFormat.Png);
                            bmp.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\wpf_slides_tab5_slideshow.png", ImageFormat.Png);
                            Console.WriteLine("Saved wpf_slides_tab5_slideshow.png");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Could not locate RibbonBar!");
                    }

                    form.Close();
                }
                Console.WriteLine("Render complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        static T FindControl<T>(Control parent) where T : Control
        {
            foreach (Control c in parent.Controls)
            {
                if (c is T match) return match;
                T nested = FindControl<T>(c);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
