using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] bytes = File.ReadAllBytes("GenOffice.exe");
            Assembly asm = Assembly.Load(bytes);
            Type writerType = asm.GetType("GenOffice.UI.WriterView");
            Type ribbonType = asm.GetType("GenOffice.UI.RibbonBar");

            dynamic w = Activator.CreateInstance(writerType);
            dynamic ribbon = Activator.CreateInstance(ribbonType);

            w.LoadDocumentFile("scratch\\test_math_doc.docx", ribbon);

            Bitmap bmp = new Bitmap(1000, 800);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                w.ScrollY = 10440; // Step 40 with multiple math equations
                w.Draw(g, 1000, 800, ribbon);
            }

            string outPath = "scratch\\rendered_math_page10.png";
            bmp.Save(outPath, ImageFormat.Png);
            Console.WriteLine("Saved screenshot to " + outPath);
            bmp.Dispose();
        }
    }
}
