using System;
using System.IO;
using System.IO.Compression;

namespace DiffXml
{
    class Program
    {
        static void Dump(string title, ZipArchive zip, string entryName)
        {
            var e = zip.GetEntry(entryName);
            Console.WriteLine("================ " + title + " : " + entryName + " ================");
            if (e != null)
            {
                using (var r = new StreamReader(e.Open()))
                {
                    Console.WriteLine(r.ReadToEnd());
                }
            }
            else
            {
                Console.WriteLine("[NOT FOUND]");
            }
        }

        static void Main()
        {
            string refPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\valid_powerpoint.pptx";
            string ourPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoThuyetTrinh_2026.pptx";

            using (var zipRef = ZipFile.OpenRead(refPptx))
            using (var zipOur = ZipFile.OpenRead(ourPptx))
            {
                Dump("REF", zipRef, "ppt/presentation.xml");
                Dump("OUR", zipOur, "ppt/presentation.xml");

                Dump("REF", zipRef, "ppt/_rels/presentation.xml.rels");
                Dump("OUR", zipOur, "ppt/_rels/presentation.xml.rels");

                Dump("REF", zipRef, "ppt/slideMasters/_rels/slideMaster1.xml.rels");
                Dump("OUR", zipOur, "ppt/slideMasters/_rels/slideMaster1.xml.rels");

                Dump("REF", zipRef, "ppt/slideMasters/slideMaster1.xml");
                Dump("OUR", zipOur, "ppt/slideMasters/slideMaster1.xml");

                Dump("REF", zipRef, "ppt/slideLayouts/_rels/slideLayout1.xml.rels");
                Dump("OUR", zipOur, "ppt/slideLayouts/_rels/slideLayout1.xml.rels");

                Dump("REF", zipRef, "ppt/slideLayouts/slideLayout1.xml");
                Dump("OUR", zipOur, "ppt/slideLayouts/slideLayout1.xml");
            }
        }
    }
}
