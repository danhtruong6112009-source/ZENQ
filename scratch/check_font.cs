using System;
using System.Drawing;
using System.Drawing.Text;

class CheckFont
{
    static void Main()
    {
        InstalledFontCollection ifc = new InstalledFontCollection();
        bool found = false;
        foreach (var fam in ifc.Families)
        {
            if (fam.Name.IndexOf("Anton", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine("Found font: " + fam.Name);
                found = true;
            }
        }
        if (!found) Console.WriteLine("Font 'Anton' NOT found in InstalledFontCollection!");
    }
}
