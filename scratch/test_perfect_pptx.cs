using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using GenOffice.Core;
using GenOffice.Format;

namespace TestPerfectPptx
{
    class Program
    {
        static void Main()
        {
            try
            {
                string refPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\valid_powerpoint.pptx";
                string outPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoThuyetTrinh_2026.pptx";

                if (File.Exists(outPptx)) File.Delete(outPptx);

                // Clone base template structure
                File.Copy(refPptx, outPptx);

                // Now open outPptx, remove old slide1 and inject our rich 4 slides
                // Let's test modifying presentation.xml and adding slide1..4
                Console.WriteLine("CLONED_BASE_PPTX_TEMPLATE");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }
    }
}
