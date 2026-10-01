using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using GenOffice.Core;
using GenOffice.Format;

class Program
{
    static void Main()
    {
        PresentationRoot pres;
        PptxHandler.LoadPptx(@"c:\Users\Danh\Desktop\bài 2.pptx", out pres);
        var s9 = pres.Slides[8]; // slide 9
        Console.WriteLine("Slide 9 shape count: " + s9.Shapes.Count);
        for (int i = 0; i < s9.Shapes.Count; i++)
        {
            var sh = s9.Shapes[i];
            Console.WriteLine(string.Format("Shape[{0}] Name='{1}' Type={2} X={3:F1} Y={4:F1} W={5:F1} H={6:F1} VAlign={7} Align={8} FS={9:F1} Font={10} Text='{11}'",
                i, sh.Name, sh.Type, sh.Transform.X, sh.Transform.Y, sh.Transform.Width, sh.Transform.Height,
                sh.VerticalAlignment, sh.Alignment, sh.FontSize, sh.FontName, (sh.Text ?? "").Replace("\n", " ")));
        }

        using (var zip = ZipFile.OpenRead(@"c:\Users\Danh\Desktop\bài 2.pptx"))
        {
            var entry = zip.GetEntry("ppt/slides/slide9.xml");
            using (var sr = new StreamReader(entry.Open()))
            {
                string xml = sr.ReadToEnd();
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("p", "http://schemas.openxmlformats.org/presentationml/2006/main");
                nsmgr.AddNamespace("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

                var spNodes = doc.SelectNodes("//p:sp", nsmgr);
                foreach (XmlNode sp in spNodes)
                {
                    var cNvPr = sp.SelectSingleNode(".//p:cNvPr", nsmgr);
                    Console.WriteLine("\n--- RAW SP: " + (cNvPr != null ? cNvPr.Attributes["name"].Value : "") + " ---");
                    Console.WriteLine(sp.OuterXml);
                }
            }
        }
    }
}
