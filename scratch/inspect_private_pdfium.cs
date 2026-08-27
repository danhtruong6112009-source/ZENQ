using System;
using System.Reflection;

public class InspectPrivatePdfium
{
    public static void Main()
    {
        var asm = Assembly.LoadFrom("PdfiumViewer.dll");
        foreach (var t in asm.GetTypes())
        {
            if (t.Name.Contains("Native") || t.Name.Contains("Pdfium") || t.Name.Contains("Text"))
            {
                Console.WriteLine("Type: " + t.FullName);
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (m.Name.Contains("Font") || m.Name.Contains("Text") || m.Name.Contains("Char"))
                    {
                        Console.WriteLine("   " + m.ReturnType.Name + " " + m.Name + "(" + string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name + " " + p.Name)) + ")");
                    }
                }
            }
        }
    }
}
