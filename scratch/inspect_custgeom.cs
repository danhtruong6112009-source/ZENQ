using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string p = @"c:\Users\Danh\Desktop\bài 2.pptx";
        using (var zip = ZipFile.OpenRead(p))
        {
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.StartsWith("ppt/slides/slide") && entry.FullName.EndsWith(".xml"))
                {
                    using (var sr = new StreamReader(entry.Open()))
                    {
                        string txt = sr.ReadToEnd();
                        if (txt.Contains("custGeom"))
                        {
                            Console.WriteLine("Found custGeom in " + entry.FullName);
                            MatchCollection mc = Regex.Matches(txt, @"<a:custGeom[\s\S]*?</a:custGeom>");
                            foreach (Match m in mc)
                            {
                                Console.WriteLine("  Match length: " + m.Length);
                                MatchCollection cmdMatches = Regex.Matches(m.Value, @"<a:(moveTo|lnTo|arcTo|quadBezTo|cubicBezTo|close)");
                                foreach (Match cm in cmdMatches)
                                {
                                    Console.Write(cm.Groups[1].Value + " ");
                                }
                                Console.WriteLine();
                            }
                        }
                    }
                }
            }
        }
    }
}
