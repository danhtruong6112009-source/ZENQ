using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

class DumpSlide5Xml
{
    static void Main()
    {
        string pptx = @"C:\Users\Danh\Desktop\bài 2.pptx";
        using (ZipArchive zip = ZipFile.OpenRead(pptx))
        {
            var entry = zip.GetEntry("ppt/slides/slide5.xml");
            using (var s = entry.Open())
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(s);
                XmlNodeList shapes = doc.SelectNodes("//*[local-name()='sp']");
                Console.WriteLine("Total sp in slide5.xml: " + (shapes != null ? shapes.Count : 0));
                foreach (XmlNode sp in shapes)
                {
                    XmlNode cNvPr = sp.SelectSingleNode(".//*[local-name()='cNvPr']");
                    string name = cNvPr != null && cNvPr.Attributes["name"] != null ? cNvPr.Attributes["name"].Value : "";
                    if (name.Contains("25") || name.Contains("9") || name.Contains("23"))
                    {
                        Console.WriteLine("\n=== SHAPE: " + name + " ===");
                        Console.WriteLine(sp.OuterXml);
                    }
                }
            }
        }
    }
}
