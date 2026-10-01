using System;

class CheckOffice {
    static void Main() {
        string[] progs = new string[] { "PowerPoint.Application", "Excel.Application", "Word.Application" };
        foreach (var prog in progs) {
            try {
                Type t = Type.GetTypeFromProgID(prog);
                if (t == null) {
                    Console.WriteLine("[NOT INSTALLED] " + prog);
                } else {
                    object obj = Activator.CreateInstance(t);
                    object version = t.InvokeMember("Version", System.Reflection.BindingFlags.GetProperty, null, obj, null);
                    Console.WriteLine("[INSTALLED] " + prog + " Version: " + version);
                    t.InvokeMember("Quit", System.Reflection.BindingFlags.InvokeMethod, null, obj, null);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                }
            } catch (Exception ex) {
                Console.WriteLine("[ERROR/NOT AVAILABLE] " + prog + ": " + ex.Message);
            }
        }
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        Console.WriteLine("[DESKTOP] " + desktop);
    }
}
