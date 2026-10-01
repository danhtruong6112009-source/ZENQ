using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.UI;

namespace VisualVerification
{
    class Program
    {
        static void Main(string[] args)
        {
            string outDir = @"C:\Users\Danh\.gemini\antigravity\brain\e0ac9d33-3f0e-43d6-aece-a38f98c824ac";
            Console.WriteLine("Initializing WpfShapeLibrary...");
            WpfShapeLibrary.EnsureInitialized();
            Console.WriteLine($"Total Categories: {WpfShapeLibrary.Categories.Count}");
            Console.WriteLine($"Total Presets: {WpfShapeLibrary.TotalPresetCount}");

            foreach (var cat in WpfShapeLibrary.Categories)
            {
                Console.WriteLine($"  [{cat.Key}] {cat.Title}: {cat.Items.Count} items");
            }

            // 1. Render Default Drawer (with category rows, search box vector icon, and visible scrollbar)
            using (Bitmap bmp = new Bitmap(360, 700))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                WpfShapeTaskPane pane = new WpfShapeTaskPane();
                pane.Draw(g, new Rectangle(0, 0, 360, 700));

                string path1 = Path.Combine(outDir, "verified_elements_drawer_scrollbar.png");
                bmp.Save(path1, ImageFormat.Png);
                Console.WriteLine($"Saved {path1}");
            }

            // 2. Render Flowchart Expanded Category (all 50 symbols in grid)
            using (Bitmap bmp = new Bitmap(360, 800))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                WpfShapeTaskPane pane = new WpfShapeTaskPane();
                pane.ExpandedCategoryKey = "Flowchart";
                pane.Draw(g, new Rectangle(0, 0, 360, 800));

                string path2 = Path.Combine(outDir, "verified_flowchart_symbols_grid.png");
                bmp.Save(path2, ImageFormat.Png);
                Console.WriteLine($"Saved {path2}");
            }

            // 2b. Render Basic Shapes Expanded Category (all 65 symbols in grid)
            using (Bitmap bmp = new Bitmap(360, 800))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                WpfShapeTaskPane pane = new WpfShapeTaskPane();
                pane.ExpandedCategoryKey = "Basic";
                pane.Draw(g, new Rectangle(0, 0, 360, 800));

                string path2b = Path.Combine(outDir, "verified_basic_shapes_grid.png");
                bmp.Save(path2b, ImageFormat.Png);
                Console.WriteLine($"Saved {path2b}");
            }

            // 3. Render Close-up crop of Search Box to verify vector magnifying glass (NO tofu box)
            using (Bitmap bmp = new Bitmap(360, 100))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                WpfShapeTaskPane pane = new WpfShapeTaskPane();
                pane.Draw(g, new Rectangle(0, 0, 360, 100));

                // Crop search bar
                Rectangle searchRect = new Rectangle(0, 46, 360, 50);
                using (Bitmap crop = new Bitmap(searchRect.Width, searchRect.Height))
                using (Graphics gc = Graphics.FromImage(crop))
                {
                    gc.DrawImage(bmp, new Rectangle(0, 0, searchRect.Width, searchRect.Height), searchRect, GraphicsUnit.Pixel);
                    string path3 = Path.Combine(outDir, "verified_search_box_vector_icon.png");
                    crop.Save(path3, ImageFormat.Png);
                    Console.WriteLine($"Saved {path3}");
                }
            }

            // 4. Render Close-up crop of Flowchart 6 symbols to inspect SSAA anti-aliasing quality
            using (Bitmap bmp = new Bitmap(360, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                WpfShapeTaskPane pane = new WpfShapeTaskPane();
                pane.ExpandedCategoryKey = "Flowchart";
                pane.Draw(g, new Rectangle(0, 0, 360, 200));

                string path4 = Path.Combine(outDir, "verified_flowchart_ssaa_quality.png");
                bmp.Save(path4, ImageFormat.Png);
                Console.WriteLine($"Saved {path4}");
            }

            Console.WriteLine("All visual verification artifacts generated successfully!");
        }
    }
}
