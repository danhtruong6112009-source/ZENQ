using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    class DocxExportTest
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== TESTING OPENXML DOCX IMAGE EXPORT ===");
            
            // 1. Create a dummy test image
            string tempImg = Path.Combine(Path.GetTempPath(), "test_export_img.png");
            using (Bitmap bmp = new Bitmap(200, 100))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.RoyalBlue);
                    g.DrawString("TEST IMAGE", new Font("Arial", 12f, FontStyle.Bold), Brushes.White, 10, 35);
                }
                bmp.Save(tempImg, ImageFormat.Png);
            }

            string docxPath = Path.Combine(Path.GetTempPath(), "test_output.docx");
            string text = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\nĐộc lập - Tự do - Hạnh phúc\n\nVĂN BẢN MỚI\n\n1. Điều khoản chung:\nCăn cứ nhu cầu và thỏa thuận hợp tác...\n\n![SHAPE:ID=img1:TYPE=Image:PATH=" + tempImg + "]\n\n2. Quyền và nghĩa vụ:\nCác bên cam kết tuân thủ đúng tiến độ.";

            List<DocShapeObject> shapes = new List<DocShapeObject>();
            shapes.Add(new DocShapeObject
            {
                Id = "img1",
                ShapeType = "Image",
                ImagePath = tempImg,
                WrapMode = 0,
                BaseWidth = 400,
                BaseHeight = 200
            });

            bool saved = DocxHandler.SaveDocxFile(docxPath, text, null, false, 0, "", "", false, shapes, null);
            if (!saved)
            {
                Console.WriteLine("FAIL: SaveDocxFile returned false.");
                return;
            }

            Console.WriteLine("SaveDocxFile succeeded: " + docxPath);

            // 2. Validate zip structure and contents
            using (ZipArchive archive = ZipFile.OpenRead(docxPath))
            {
                var docXmlEntry = archive.GetEntry("word/document.xml");
                if (docXmlEntry == null) { Console.WriteLine("FAIL: word/document.xml missing"); return; }
                
                string docXml;
                using (StreamReader sr = new StreamReader(docXmlEntry.Open())) docXml = sr.ReadToEnd();
                
                if (docXml.Contains("![SHAPE:"))
                {
                    Console.WriteLine("FAIL: document.xml still contains raw Markdown ![SHAPE:...]");
                    return;
                }
                if (!docXml.Contains("<w:drawing>"))
                {
                    Console.WriteLine("FAIL: document.xml does not contain <w:drawing>");
                    return;
                }
                if (!docXml.Contains("<a:blip"))
                {
                    Console.WriteLine("FAIL: document.xml does not contain <a:blip>");
                    return;
                }
                Console.WriteLine("PASS: document.xml has valid <w:drawing> and <a:blip> without raw markdown!");

                var relsEntry = archive.GetEntry("word/_rels/document.xml.rels");
                if (relsEntry == null) { Console.WriteLine("FAIL: document.xml.rels missing"); return; }
                string relsXml;
                using (StreamReader sr = new StreamReader(relsEntry.Open())) relsXml = sr.ReadToEnd();
                if (!relsXml.Contains("Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\""))
                {
                    Console.WriteLine("FAIL: image relationship missing in document.xml.rels");
                    return;
                }
                Console.WriteLine("PASS: document.xml.rels contains image relationship!");

                var mediaEntry = archive.GetEntry("word/media/image1.png");
                if (mediaEntry == null) { Console.WriteLine("FAIL: word/media/image1.png missing"); return; }
                Console.WriteLine("PASS: word/media/image1.png exists (Size: " + mediaEntry.Length + " bytes)!");
            }

            Console.WriteLine("\n[ALL DOCX EXPORT IMAGE & DRAWING TESTS PASSED 100%!]");
        }
    }
}
