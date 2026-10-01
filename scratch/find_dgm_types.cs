using System;
using System.Reflection;

class Program {
    static void Main() {
        Assembly asm = Assembly.LoadFrom("DocumentFormat.OpenXml.dll");
        foreach (Type t in asm.GetTypes()) {
            if (t.Namespace != null && t.Namespace.Contains("Diagrams")) {
                if (t.Name.Contains("Point") || t.Name.Contains("DataModel")) {
                    Console.WriteLine(t.FullName);
                }
            }
        }
    }
}
