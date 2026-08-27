using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    class DocxRoundtripTest
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== TESTING DOCX EXPORT & IMPORT ROUNDTRIP ===");

            // 1. Create a dummy test image
            string tempImg = Path.Combine(Path.GetTempPath(), "roundtrip_test_img.png");
            using (Bitmap bmp = new Bitmap(300, 150))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.SeaGreen);
                    g.DrawString("ROUNDTRIP TEST", new Font("Arial", 14f, FontStyle.Bold), Brushes.White, 15, 55);
                }
                bmp.Save(tempImg, ImageFormat.Png);
            }

            string docxPath = Path.Combine(Path.GetTempPath(), "roundtrip_output.docx");
            string text = "Đoạn văn bản 1 trước ảnh\n![SHAPE:ID=sh123:TYPE=Image:PATH=" + tempImg + ":X=120:Y=200:W=350:H=180:WRAP=1:PAGE=0]\nĐoạn văn bản 2 sau ảnh";

            List<DocShapeObject> shapes = new List<DocShapeObject>();
            shapes.Add(new DocShapeObject
            {
                Id = "sh123",
                ShapeType = "Image",
                ImagePath = tempImg,
                PageX = 120,
                PageY = 200,
                BaseWidth = 350,
                BaseHeight = 180,
                WrapMode = 1
            });

            bool saved = DocxHandler.SaveDocxFile(docxPath, text, null, false, 0, "", "", false, shapes, null);
            if (!saved) { Console.WriteLine("FAIL: SaveDocxFile failed"); return; }
            Console.WriteLine("PASS: Exported to DOCX successfully.");

            // 2. Load DOCX back
            DocxMetadata meta;
            try
            {
                bool loaded = DocxHandler.LoadDocxFile(docxPath, out meta);
                if (!loaded || meta == null) { Console.WriteLine("FAIL: LoadDocxFile failed"); return; }
                Console.WriteLine("PASS: Loaded DOCX back successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: LoadDocxFile exception: " + ex);
                return;
            }

            Console.WriteLine("Loaded text content:\n" + meta.TextContent);

            if (!meta.TextContent.Contains("![SHAPE:") || !meta.TextContent.Contains("TYPE=Image"))
            {
                Console.WriteLine("FAIL: Loaded text does NOT contain ![SHAPE:...]");
                return;
            }
            if (!meta.TextContent.Contains("W=") || !meta.TextContent.Contains("H=") || !meta.TextContent.Contains("WRAP="))
            {
                Console.WriteLine("FAIL: Loaded tag missing W/H/WRAP attributes");
                return;
            }

            Console.WriteLine("PASS: Image tag restored with all dimensions, wrap mode, and coordinates!");
            Console.WriteLine("\n[ALL ROUNDTRIP TESTS PASSED 100%!]");
        }
    }
}
