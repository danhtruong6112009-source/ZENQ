using System;
using System.Windows.Forms;

public class WmpPlayer : AxHost
{
    public WmpPlayer() : base("6bf52a52-394a-11d3-b153-00c04f79faa6") { }
    
    [STAThread]
    public static void Main()
    {
        try {
            Form f = new Form();
            f.Text = "Test WMP";
            f.Size = new System.Drawing.Size(640, 480);
            WmpPlayer player = new WmpPlayer();
            player.Dock = DockStyle.Fill;
            f.Controls.Add(player);
            f.Load += (s, e) => {
                try {
                    dynamic ocx = player.GetOcx();
                    Console.WriteLine("OCX initialized: " + (ocx != null));
                    if (ocx != null) {
                        Console.WriteLine("uiMode: " + ocx.uiMode);
                    }
                } catch (Exception ex) {
                    Console.WriteLine("Inner error: " + ex.Message);
                }
                f.Close();
            };
            Application.Run(f);
            Console.WriteLine("TEST_WMP_SUCCESS");
        } catch (Exception ex) {
            Console.WriteLine("TEST_WMP_ERROR: " + ex.ToString());
        }
    }
}
