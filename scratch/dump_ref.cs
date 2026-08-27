using System;
using System.IO;
using System.IO.Compression;

namespace DumpRef
{
    class Program
    {
        static void Dump(ZipArchive zip, string entryName)
        {
            var e = zip.GetEntry(entryName);
            Console.WriteLine("================ " + entryName + " ================");
            if (e != null)
            {
                using (var r = new StreamReader(e.Open()))
                {
                    Console.WriteLine(r.ReadToEnd());
                }
            }
        }

        static void Main()
        {
            string refPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\valid_powerpoint.pptx";
            using (var zip = ZipFile.OpenRead(refPptx))
            {
                Dump(zip, "ppt/slideMasters/slideMaster1.xml");
                Dump(zip, "ppt/presProps.xml");
                Dump(zip, "ppt/viewProps.xml");
                Dump(zip, "ppt/tableStyles.xml");
            }
        }
    }
}
