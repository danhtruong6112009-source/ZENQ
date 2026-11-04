using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ZenQOffice.UI;
using ZenQOffice.Core;
class AiRenderingTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Paint(Control control,string name)
    {
        Check(!control.IsHandleCreated,"Offscreen test must use the native D2D DC path rather than presenting to an HWND");
        using var bitmap=new Bitmap(control.Width,control.Height);
        using(var graphics=Graphics.FromImage(bitmap))
        using(var args=new PaintEventArgs(graphics,control.ClientRectangle))
            control.GetType().GetMethod("OnPaint",Private).Invoke(control,new object[]{args});
        int magenta=0;
        for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++)
        {var c=bitmap.GetPixel(x,y);if(c.R>180&&c.B>180&&c.G<70)magenta++;}
        Check(magenta==0,name+" must not contain a magenta transparency fringe");
        bitmap.Save("output/ai-rendering/"+name+".png");
    }
    [STAThread] static int Main()
    {
        try {Run();return 0;}catch(Exception error){Console.WriteLine(error);return 1;}
    }
    static void Run()
    {
        Directory.CreateDirectory("output/ai-rendering");
        var settings=AppSettings.Instance;var originalStyle=settings.RibbonStyle;bool originalFloat=settings.AiChatIslandMode;
        try
        {
            settings.RibbonStyle=RibbonStyle.Standard;settings.AiChatIslandMode=false;
            using var panel=new AiChatSidebarPanel(null){Size=new Size(380,620)};
            var size=panel.Size;panel.UpdateIslandStyle();Check(!panel.UsesRoundedFrame&&panel.Region==null,"Standard docked sidebar remains rectangular");
            settings.RibbonStyle=RibbonStyle.Island;settings.NotifyChanged();
            Check(panel.UsesRoundedFrame&&!panel.IsIslandMode,"Ribbon Island rounds the docked AI without switching its placement mode");
            Check(panel.Size==size,"Rounding preserves sidebar proportions");
            Check(panel.Region!=null&&!panel.Region.IsVisible(0,0)&&panel.Region.IsVisible(190,310),"Whole Island frame clips all external corners");
            Check(!panel.Region.IsVisible(1,20)&&panel.Region.IsVisible(4,20),"HWND mask stays inside the host's opaque antialiased rim");
            using(var bitmap=new Bitmap(400,640))
            {
                using(var graphics=Graphics.FromImage(bitmap))using(var g=new D2DGraphics(graphics,400,640))
                {
                    g.Clear(Color.FromArgb(155,30,35));
                    panel.GetType().GetMethod("DrawHostBackdrop",Private).Invoke(panel,new object[]{g,new Rectangle(10,10,380,620)});
                }
                Check(bitmap.GetPixel(10,10).R==155,"Floating corners reveal the actual slide beneath the card");
                Check(bitmap.GetPixel(13,30).R>245&&bitmap.GetPixel(13,30).G>245,"Host paints opaque white beneath the hard native mask");
                int blended=0;for(int y=10;y<23;y++)for(int x=10;x<23;x++){var c=bitmap.GetPixel(x,y);if(c.G>40&&c.G<200)blended++;}
                Check(blended>3,"Outer corner contains subpixel coverage rather than a binary region edge");
                bitmap.Save("output/ai-rendering/host-rounded-rim.png");
            }
            var header=(Control)typeof(AiChatSidebarPanel).GetField("pnlHeader",Private).GetValue(panel);
            foreach(Control child in header.Controls)Check(child is not PictureBox,"Header logo is composited directly instead of using a transparent HWND");
            Paint(header,"header-logo");
            using(var bitmap=new Bitmap(380,620))
            {
                using(var graphics=Graphics.FromImage(bitmap))using(var g=new D2DGraphics(graphics,380,620))
                {panel.GetType().GetMethod("DrawSidebarFrame",Private).Invoke(panel,new object[]{g});}
                Check(bitmap.GetPixel(0,0).ToArgb()!=bitmap.GetPixel(190,310).ToArgb(),"Antialiased outer frame has a separate corner background");
                bitmap.Save("output/ai-rendering/island-frame.png");
            }
            foreach(string field in new[]{"pnlHeader","pnlInputContainer"})
            {
                var child=(Control)typeof(AiChatSidebarPanel).GetField(field,Private).GetValue(panel);
                Check(child.Region!=null&&!child.Region.IsVisible(0,field=="pnlHeader"?0:child.Height-1),"Header/input cannot cover the rounded AI corners");
            }
            panel.Width=760;
            var quiz=new AiCanvasQuizData();quiz.Questions.Add(new AiCanvasQuestion{QuestionText="Canvas corner rendering"});
            panel.OpenCanvas(quiz);
            var canvas=(Control)typeof(AiChatSidebarPanel).GetField("_canvasControl",Private).GetValue(panel);
            Check(canvas.Region!=null&&!canvas.Region.IsVisible(canvas.Width-1,0)&&!canvas.Region.IsVisible(canvas.Width-1,canvas.Height-1),"Expanded Canvas preserves both external right corners");
            Paint(canvas,"expanded-canvas");
            panel.CloseCanvas();panel.Width=380;
            settings.RibbonStyle=RibbonStyle.Standard;typeof(AiChatSidebarPanel).GetField("_isIslandMode",Private).SetValue(panel,true);panel.UpdateIslandStyle();
            Check(panel.UsesRoundedFrame&&panel.Region!=null,"Floating AI stays rounded with the standard ribbon");
            using var combo=new ModernPillComboBox{Size=new Size(230,28)};combo.Items.Add("gemini-3.5-flash");Paint(combo,"model-pill");
            using var popup=new GlassDropDownPopup(new List<string>{"gemini-3.7-flash","gemini-3.6-flash","gemini-3.5-flash","gemini-3.5-flash-lite","gemini-3-pro-preview"},2,combo.Font,230);
            Check(popup.TransparencyKey.IsEmpty,"Popup uses an opaque native composition window, never a chroma key");
            Paint(popup,"model-dropdown");
            var many=new List<string>();for(int i=0;i<80;i++)many.Add("Model "+i);
            using var scrolling=new GlassDropDownPopup(many,79,combo.Font,230);
            scrolling.GetType().GetMethod("EnsureVisible",Private).Invoke(scrolling,new object[]{79});
            Check((int)scrolling.GetType().GetField("_firstVisible",Private).GetValue(scrolling)>60,"Long model lists can reach their last entry");
            Paint(scrolling,"long-model-dropdown");
            using var greeting=new GeminiWelcomeControl(360,OfficeAppMode.Slides,null){Size=new Size(360,380)};Paint(greeting,"welcome");
            var frameType=typeof(AiChatSidebarPanel).Assembly.GetType("ZenQOffice.UI.AiGpuPaint");
            var surfaces=typeof(GpuEngine).GetField("_surfaces",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            int initial=(int)surfaces.GetType().GetProperty("Count").GetValue(surfaces);
            using(var native=new ModernPillComboBox{Size=new Size(230,28)})
            using(var dummy=new Bitmap(230,28))using(var graphics=Graphics.FromImage(dummy))using(var args=new PaintEventArgs(graphics,native.ClientRectangle))
            {
                var handle=native.Handle;
                for(int cycle=0;cycle<3;cycle++)
                {
                    native.Width=230+cycle*12;
                    using var frame=(IDisposable)frameType.GetMethod("Begin",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{native,args});
                    var target=(D2DGraphics)frameType.GetProperty("Graphics").GetValue(frame);
                    Check(target.RenderTarget!=null,"AI creates an actual native Direct2D render target");
                    Check(frameType.GetField("_surface",Private).GetValue(frame)!=null,"AI presents through the Direct3D/WARP HWND surface");
                    target.FillRoundedRectangle(Brushes.White,new RectangleF(1,1,native.Width-2,26),13);
                }
                Check((int)surfaces.GetType().GetProperty("Count").GetValue(surfaces)==initial+1,"Resize reuses the same GPU surface");
                using(var failed=(IDisposable)frameType.GetMethod("Begin",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{native,args}))
                {
                    var drawing=(D2DGraphics)frameType.GetProperty("Graphics").GetValue(failed);
                    typeof(D2DGraphics).GetProperty("LastDrawFailed").SetValue(drawing,true);
                }
                Check((int)surfaces.GetType().GetProperty("Count").GetValue(surfaces)==initial,"Failed AI frame releases its broken GPU surface");
                using(var restored=(IDisposable)frameType.GetMethod("Begin",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{native,args}))
                    Check(frameType.GetField("_surface",Private).GetValue(restored)!=null,"AI recreates its GPU surface after a failed frame");

            }
            Check((int)surfaces.GetType().GetProperty("Count").GetValue(surfaces)==initial,"Disposing an AI control releases its native target");
            Console.WriteLine("PASS: Island/docked/floating corner policy, preserved proportions, clipped child corners, D2D AI frame/pills/dropdowns/welcome, no purple fringe, long dropdown scrolling, expanded Canvas corners, native GPU resize/disposal, host antialiased floating rim, direct header icon compositing");
        }
        finally {settings.RibbonStyle=originalStyle;settings.AiChatIslandMode=originalFloat;}
    }
}
