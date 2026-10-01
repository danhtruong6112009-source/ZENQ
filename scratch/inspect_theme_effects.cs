using System;
using System.IO;
using System.IO.Compression;

class Program
{
    static void Main()
    {
        string f = @"sdkjs-master\slide\themes\src\08_safari.pptx";
        using (FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read))
        using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            var e = zip.GetEntry("ppt/slideLayouts/slideLayout7.xml");
            if (e != null)
            {
                using (StreamReader sr = new StreamReader(e.Open()))
                {
                    string txt = sr.ReadToEnd();
                    int idx = txt.IndexOf("blur");
                    if (idx >= 0)
                    {
                        Console.WriteLine(txt.Substring(Math.Max(0, idx - 100), Math.Min(300, txt.Length - idx)));
                    }
                }
            }
        }
    }
}
