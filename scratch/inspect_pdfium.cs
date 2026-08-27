using System;
using System.Reflection;

public class InspectPdfium
{
    public static void Main()
    {
        var asm = Assembly.LoadFrom("PdfiumViewer.dll");
        Console.WriteLine("=== TYPES IN PDFIUMVIEWER ===");
        foreach (var t in asm.GetTypes())
        {
            if (t.IsPublic)
            {
                Console.WriteLine(t.FullName);
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (!m.IsSpecialName)
                        Console.WriteLine("   " + m.ReturnType.Name + " " + m.Name + "(" + string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name + " " + p.Name)) + ")");
                }
                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Console.WriteLine("   [Prop] " + p.PropertyType.Name + " " + p.Name);
                }
            }
        }
    }
}
