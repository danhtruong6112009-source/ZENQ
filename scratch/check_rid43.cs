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
            var pRels = zip.GetEntry("ppt/_rels/presentation.xml.rels");
            using (var sr = new StreamReader(pRels.Open()))
            {
                var doc = new XmlDocument();
                doc.Load(sr);
                var rel = doc.SelectSingleNode("//*[@Id='rId43']");
                Console.WriteLine("rId43 Target: " + (rel != null ? rel.Attributes["Target"].Value : "not found"));
            }

            var pEntry = zip.GetEntry("ppt/presentation.xml");
            using (var sr = new StreamReader(pEntry.Open()))
            {
                var doc = new XmlDocument();
                doc.Load(sr);
                var fNodes = doc.SelectNodes("//*[local-name()='embeddedFont']");
                foreach (XmlNode f in fNodes)
                {
                    Console.WriteLine(f.OuterXml);
                }
            }
        }
    }
}
