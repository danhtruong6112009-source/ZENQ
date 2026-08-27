using System;
using System.IO;

class Program
{
    static void Main()
    {
        string[] lines = File.ReadAllLines(@"src\ui\SlidesView.cs");
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Contains("MouseDown(") || line.Contains("MouseMove(") || line.Contains("MouseUp(") || line.Contains("HandleMouseDown") || line.Contains("HandleMouseUp") || line.Contains("HandleMouseMove"))
            {
                Console.WriteLine((i + 1) + ": " + line.Trim());
            }
        }
    }
}
