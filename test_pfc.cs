using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;

class Program
{
    static void Main()
    {
        PrivateFontCollection pfc = new PrivateFontCollection();
        foreach (string file in Directory.GetFiles(".", "font*.*"))
        {
            if (file.EndsWith(".ttf") || file.EndsWith(".otf"))
            {
                try
                {
                    pfc.AddFontFile(file);
                    Console.WriteLine("Added: " + file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed " + file + ": " + ex.Message);
                }
            }
        }
        Console.WriteLine("Total families loaded: " + pfc.Families.Length);
        foreach (FontFamily fam in pfc.Families)
        {
            Console.WriteLine("  Family: " + fam.Name);
        }
    }
}
