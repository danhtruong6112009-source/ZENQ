using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

class Program
{
    static void Main()
    {
        string[] files = Directory.GetFiles(@"C:\Users\Danh\Desktop", "*.pptx");
        foreach (var path in files)
        {
            Console.WriteLine("=== Scanning: " + path);
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var e in zip.Entries)
                {
                    if (e.FullName.StartsWith("ppt/slides/slide") && e.FullName.EndsWith(".xml"))
                    {
                        using (Stream s = e.Open())
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(s);
                            var pics = doc.SelectNodes("//*[local-name()='pic']");
                            var blips = doc.SelectNodes("//*[local-name()='blip']");
                            var softEdges = doc.SelectNodes("//*[local-name()='softEdge']");
                            var blurs = doc.SelectNodes("//*[local-name()='blur']");
                            var srcRects = doc.SelectNodes("//*[local-name()='srcRect']");

                            if (srcRects.Count > 0 || softEdges.Count > 0 || blurs.Count > 0)
                            {
                                Console.WriteLine("{0}: pics={1}, blips={2}, srcRects={3}, softEdge={4}, blur={5}",
                                    e.FullName, pics.Count, blips.Count, srcRects.Count, softEdges.Count, blurs.Count);
                            }
                        }
                    }
                }
            }
        }
    }
}
