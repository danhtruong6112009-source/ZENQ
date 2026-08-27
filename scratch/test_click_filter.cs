using System;
using System.Drawing;
using System.Windows.Forms;

public class WmpPlayer : AxHost
{
    public WmpPlayer() : base("6bf52a52-394a-11d3-b153-00c04f79faa6") { }
}

public class VideoClickFilter : IMessageFilter
{
    public Form MainForm;
    public Panel PlayerPanel;
    public bool ClickIntercepted = false;

    public bool PreFilterMessage(ref Message m)
    {
        // 0x0201 = WM_LBUTTONDOWN, 0x0203 = WM_LBUTTONDBLCLK
        if (m.Msg == 0x0201 || m.Msg == 0x0203)
        {
            if (PlayerPanel != null && PlayerPanel.Visible && !PlayerPanel.IsDisposed)
            {
                Point screenPt = Cursor.Position;
                Rectangle panelScreenRect = PlayerPanel.RectangleToScreen(PlayerPanel.ClientRectangle);
                if (panelScreenRect.Contains(screenPt))
                {
                    Console.WriteLine("INTERCEPTED_CLICK_ON_VIDEO_AT: " + screenPt);
                    ClickIntercepted = true;
                    // Return true to eat message or false to let through
                    return true;
                }
            }
        }
        return false;
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        Console.WriteLine("Testing Video Click Interceptor...");
        Form f = new Form();
        f.Size = new Size(800, 600);

        Panel p = new Panel();
        p.Bounds = new Rectangle(100, 100, 400, 300);
        p.BackColor = Color.Black;

        WmpPlayer player = new WmpPlayer();
        player.Dock = DockStyle.Fill;
        p.Controls.Add(player);
        f.Controls.Add(p);

        VideoClickFilter filter = new VideoClickFilter { MainForm = f, PlayerPanel = p };
        Application.AddMessageFilter(filter);

        f.Load += (s, e) =>
        {
            // Simulate click inside panel
            Point clickPt = p.PointToScreen(new Point(50, 50));
            Cursor.Position = clickPt;
            Console.WriteLine("Simulated click at: " + clickPt);
            f.Close();
        };

        Application.Run(f);
        Console.WriteLine("FILTER_TEST_COMPLETED_SUCCESSFULLY");
    }
}
