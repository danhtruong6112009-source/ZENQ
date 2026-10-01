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
        var s8 = pres.Slides[7];
        Console.WriteLine("Slide 8 shape count: " + s8.Shapes.Count);
        for (int i = 0; i < s8.Shapes.Count; i++)
        {
            var sh = s8.Shapes[i];
            if (!string.IsNullOrEmpty(sh.Text))
            {
                Console.WriteLine(string.Format("Shape[{0}] Name='{1}' Type={2} X={3:F1} Y={4:F1} W={5:F1} H={6:F1} ML={7} MR={8} FS={9:F1} Font={10}",
                    i, sh.Name, sh.Type, sh.Transform.X, sh.Transform.Y, sh.Transform.Width, sh.Transform.Height,
                    sh.MarginLeft, sh.MarginRight, sh.FontSize, sh.FontName));
                Console.WriteLine("  Text: " + sh.Text.Replace("\n", " \\n "));
                if (sh.TextRuns != null)
                {
                    foreach (var r in sh.TextRuns)
                    {
                        Console.WriteLine(string.Format("    Run: font={0} sz={1:F1} b={2} text='{3}'", r.FontName, r.FontSize, r.Bold, r.Text));
                    }
                }
            }
        }

        // Also inspect raw XML of slide8.xml
        using (var zip = ZipFile.OpenRead(@"c:\Users\Danh\Desktop\bài 2.pptx"))
        {
            var entry = zip.GetEntry("ppt/slides/slide8.xml");
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
                    var tNode = sp.SelectSingleNode(".//a:t", nsmgr);
                    if (tNode != null && (tNode.InnerText.Contains("VAI TR") || tNode.InnerText.Contains("ngoài")))
                    {
                        var cNvPr = sp.SelectSingleNode(".//p:cNvPr", nsmgr);
                        Console.WriteLine("\n--- RAW XML FOR: " + (cNvPr != null ? cNvPr.Attributes["name"].Value : "") + " ---");
                        Console.WriteLine(sp.OuterXml);
                    }
                }
            }
        }
    }
}
