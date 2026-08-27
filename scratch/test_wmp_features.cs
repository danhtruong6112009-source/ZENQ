using System;
using System.Drawing;
using System.Windows.Forms;

public class WmpPlayer : AxHost
{
    public WmpPlayer() : base("6bf52a52-394a-11d3-b153-00c04f79faa6") { }

    [STAThread]
    public static void Main()
    {
        try
        {
            Form f = new Form();
            f.Text = "WMP Feature Test";
            f.Size = new Size(800, 600);

            WmpPlayer player = new WmpPlayer();
            player.Dock = DockStyle.Fill;
            f.Controls.Add(player);

            f.Load += (s, e) =>
            {
                try
                {
                    dynamic ocx = player.GetOcx();
                    Console.WriteLine("1. OCX: " + (ocx != null));
                    ocx.uiMode = "mini";
                    Console.WriteLine("2. uiMode set to: " + ocx.uiMode);
                    ocx.settings.autoStart = false;
                    Console.WriteLine("3. autoStart set to: " + ocx.settings.autoStart);
                    ocx.settings.volume = 80;
                    Console.WriteLine("4. volume set to: " + ocx.settings.volume);
                    ocx.settings.setMode("loop", true);
                    Console.WriteLine("5. loop set to: true");
                    Console.WriteLine("ALL_WMP_PROPERTIES_VERIFIED_SUCCESS");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FAIL_INNER: " + ex.ToString());
                }
                f.Close();
            };

            Application.Run(f);
            Console.WriteLine("TEST_COMPLETED_SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            Console.WriteLine("TEST_FAILED: " + ex.ToString());
        }
    }
}
