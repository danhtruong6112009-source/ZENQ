using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;

class Program
{
    static PrivateFontCollection pfc = new PrivateFontCollection();
    static Dictionary<string, FontFamily> fontMap = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);

    static void LoadFont(string file)
    {
        pfc.AddFontFile(file);
        foreach (FontFamily fam in pfc.Families)
        {
            if (!fontMap.ContainsKey(fam.Name))
            {
                fontMap[fam.Name] = fam;
                Console.WriteLine("Registered in fontMap: " + fam.Name);
            }
        }
    }

    static Font CreateFont(string name, float size, FontStyle style)
    {
        FontFamily fam;
        if (fontMap.TryGetValue(name, out fam))
        {
            return new Font(fam, size, style);
        }
        return new Font(name, size, style);
    }

    static void Main()
    {
        LoadFont("Anton.ttf");
        using (Font f = CreateFont("Anton", 24, FontStyle.Regular))
        {
            Console.WriteLine("Created: " + f.Name + " (" + f.FontFamily.Name + ")");
        }
    }
}
