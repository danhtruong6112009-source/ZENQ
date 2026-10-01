using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

class Program
{
    static void Main()
    {
        string p = @"c:\Users\Danh\Desktop\bài 2.pptx";
        using (var zip = ZipFile.OpenRead(p))
        {
            var entry = zip.GetEntry("ppt/slides/slide5.xml");
            var doc = new XmlDocument();
            doc.Load(entry.Open());
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("p", "http://schemas.openxmlformats.org/presentationml/2006/main");
            nsmgr.AddNamespace("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

            var spNodes = doc.SelectNodes("//p:sp", nsmgr);
            foreach (XmlNode sp in spNodes)
            {
                var cNvPr = sp.SelectSingleNode(".//p:cNvPr", nsmgr);
                string name = cNvPr != null ? cNvPr.Attributes["name"].Value : "";
                if (name.Contains("TextBox 19") || name.Contains("TextBox 23"))
                {
                    Console.WriteLine("Shape: " + name);
                    Console.WriteLine(sp.OuterXml);
                }
            }
        }
    }
}
