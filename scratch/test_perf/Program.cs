using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Program {
    [STAThread]
    static void Main(string[] args) {
        int tickCount = 0;
        var sw = Stopwatch.StartNew();
        var timer = new System.Windows.Forms.Timer();
        timer.Interval = 1;
        timer.Tick += (s, e) => {
            tickCount++;
            if (sw.ElapsedMilliseconds >= 1000) {
                timer.Stop();
                Application.Exit();
            }
        };
        timer.Start();
        Application.Run();

        Console.WriteLine($"WITHOUT TimeBeginPeriod, Interval=1: {tickCount} ticks in {sw.ElapsedMilliseconds} ms ({(tickCount * 1000.0 / sw.ElapsedMilliseconds):F1} FPS)");
    }
}
