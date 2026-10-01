using System;
using System.IO;
using System.IO.Compression;

class Program
{
    static void Main()
    {
        string[] files = new string[] {
            @"C:\Users\Danh\Desktop\bài 2.pptx",
            @"C:\Users\Danh\Desktop\Ke_Hoach_Khoi_Nghiep_Tra_Chom_Chom_Viet.pptx"
        };
        foreach (var path in files)
        {
            if (!File.Exists(path)) continue;
            Console.WriteLine("--- Checking: " + path);
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var e in zip.Entries)
                {
                    if (e.FullName.EndsWith(".xml"))
                    {
                        using (Stream s = e.Open())
                        using (StreamReader sr = new StreamReader(s))
                        {
                            string txt = sr.ReadToEnd();
                            bool hasCrop = txt.Contains("srcRect");
                            bool hasSoft = txt.Contains("softEdge");
                            bool hasBlur = txt.Contains("blur");
                            if (hasCrop || hasSoft || hasBlur)
                            {
                                Console.WriteLine("  {0}: crop={1}, softEdge={2}, blur={3}", e.FullName, hasCrop, hasSoft, hasBlur);
                            }
                        }
                    }
                }
            }
        }

        Console.WriteLine("\n--- Checking themes PPTX for crop, softEdge, blur ---");
        string themeDir = @"sdkjs-master\slide\themes\src";
        if (Directory.Exists(themeDir))
        {
            foreach (var f in Directory.GetFiles(themeDir, "*.pptx"))
            {
                using (FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    foreach (var e in zip.Entries)
                    {
                        if (e.FullName.EndsWith(".xml"))
                        {
                            using (Stream s = e.Open())
                            using (StreamReader sr = new StreamReader(s))
                            {
                                string txt = sr.ReadToEnd();
                                bool hasCrop = txt.Contains("srcRect");
                                bool hasSoft = txt.Contains("softEdge");
                                bool hasBlur = txt.Contains("blur");
                                if (hasCrop || hasSoft || hasBlur)
                                {
                                    Console.WriteLine("  {0} -> {1}: crop={2}, softEdge={3}, blur={4}", Path.GetFileName(f), e.FullName, hasCrop, hasSoft, hasBlur);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
