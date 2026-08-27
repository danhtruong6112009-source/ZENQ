using System;
using System.IO;
using System.IO.Compression;

namespace DumpMaster
{
    class Program
    {
        static void Main()
        {
            string refPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\valid_powerpoint.pptx";
            using (var zip = ZipFile.OpenRead(refPptx))
            {
                var e = zip.GetEntry("ppt/slideMasters/slideMaster1.xml");
                using (var r = new StreamReader(e.Open()))
                {
                    File.WriteAllText(@"C:\Users\Danh\Desktop\genofficepy\scratch\ref_slideMaster1.xml", r.ReadToEnd());
                }
                var eLayout = zip.GetEntry("ppt/slideLayouts/slideLayout1.xml");
                using (var r = new StreamReader(eLayout.Open()))
                {
                    File.WriteAllText(@"C:\Users\Danh\Desktop\genofficepy\scratch\ref_slideLayout1.xml", r.ReadToEnd());
                }
                var eTheme = zip.GetEntry("ppt/theme/theme1.xml");
                using (var r = new StreamReader(eTheme.Open()))
                {
                    File.WriteAllText(@"C:\Users\Danh\Desktop\genofficepy\scratch\ref_theme1.xml", r.ReadToEnd());
                }
            }
            Console.WriteLine("DUMPED_REF_FILES");
        }
    }
}
