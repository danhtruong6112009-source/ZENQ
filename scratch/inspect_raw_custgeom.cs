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
                var custGeom = sp.SelectSingleNode(".//a:custGeom", nsmgr);
                if (custGeom != null)
                {
                    var cNvPr = sp.SelectSingleNode(".//p:cNvPr", nsmgr);
                    Console.WriteLine("Shape: " + (cNvPr != null ? cNvPr.Attributes["name"].Value : ""));
                    Console.WriteLine(custGeom.OuterXml.Substring(0, Math.Min(300, custGeom.OuterXml.Length)));
                    Console.WriteLine("...");
                }
            }
        }
    }
}
