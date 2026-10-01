using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

class Program
{
    static void Main()
    {
        using (var zip = ZipFile.OpenRead(@"c:\Users\Danh\Desktop\bài 2.pptx"))
        {
            var entry = zip.GetEntry("ppt/slides/slide8.xml");
            using (var sr = new StreamReader(entry.Open()))
            {
                var doc = new XmlDocument();
                doc.Load(sr);
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("p", "http://schemas.openxmlformats.org/presentationml/2006/main");
                nsmgr.AddNamespace("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

                var tb12 = doc.SelectSingleNode("//p:cNvPr[@name='TextBox 12']", nsmgr);
                if (tb12 != null)
                {
                    XmlNode sp = tb12.ParentNode.ParentNode;
                    Console.WriteLine(sp.OuterXml);
                }
            }
        }
    }
}
