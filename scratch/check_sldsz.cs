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
            var entry = zip.GetEntry("ppt/presentation.xml");
            using (var sr = new StreamReader(entry.Open()))
            {
                var doc = new XmlDocument();
                doc.Load(sr);
                var sldSz = doc.SelectSingleNode("//*[local-name()='sldSz']");
                Console.WriteLine(sldSz != null ? sldSz.OuterXml : "sldSz NOT FOUND");
            }
        }
    }
}
