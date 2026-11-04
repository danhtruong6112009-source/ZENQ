using System;using System.Drawing;using System.Diagnostics;using ZenQOffice.UI;using ZenQOffice.Core;using ZenQOffice.Format;
class Test{[STAThread]static void Main(){var deck=PptxHandler.LoadPptx(@"C:\Users\Danh\Downloads\Đỏ Vàng Bài Thuyết Trình Truyền Thống Việt Nam.pptx");var v=new SlidesView{Presentation=deck};using var b=new Bitmap(1366,768);using var graphics=Graphics.FromImage(b);
foreach(var s in deck.Slides[0].Shapes){var watch=Stopwatch.StartNew();for(int i=0;i<12;i++){using var g=new D2DGraphics(graphics,1366,768);v.DrawShape(g,250,180-i, .48f,s,false);}Console.WriteLine(s.Type+" "+s.Text+" "+s.CachedImage?.Size+" avg "+watch.Elapsed.TotalMilliseconds/12);}
using(var warm=new D2DGraphics(graphics,1366,768))v.Draw(warm,1366,768);
while(v.PreloadNextPendingThumbnail()){}
using(var warm=new D2DGraphics(graphics,1366,768))v.Draw(warm,1366,768);
for(int i=0;i<20;i++){v.TargetCanvasScrollY=100;v.CanvasScrollY=i*3;v.TargetThumbScrollY=100;var w=Stopwatch.StartNew();using(var g=new D2DGraphics(graphics,1366,768))v.Draw(g,1366,768);Console.WriteLine("FRAME "+i+" "+w.Elapsed.TotalMilliseconds);} using(var cached=new Bitmap(1366,768))using(var direct=new Bitmap(1366,768)){
 using(var cg=Graphics.FromImage(cached))using(var drawing=new D2DGraphics(cg,1366,768))v.Draw(drawing,1366,768);
 v.IsPlayingAnimationPreview=true;
 using(var dg=Graphics.FromImage(direct))using(var drawing=new D2DGraphics(dg,1366,768))v.Draw(drawing,1366,768);
 v.IsPlayingAnimationPreview=false;double difference=0;int samples=0;for(int y=170;y<680;y+=3)for(int x=220;x<1340;x+=3){var c=cached.GetPixel(x,y);var d=direct.GetPixel(x,y);difference+=Math.Abs(c.R-d.R)+Math.Abs(c.G-d.G)+Math.Abs(c.B-d.B);samples+=3;}Console.WriteLine("Cache/direct pixel difference "+difference/samples);if(difference/samples>1.5)throw new Exception("Cached scrolling altered slide content");
 } var smooth=new SlidesView();smooth.TargetCanvasScrollY=100;for(int i=0;i<9;i++){System.Threading.Thread.Sleep(15);smooth.UpdateSmoothScroll();}if(smooth.CanvasScrollY!=100)throw new Exception("Scroll failed to settle promptly");
 v.TargetCanvasScrollY=v.SmoothCanvasScrollY;v.TargetThumbScrollY=v.SmoothThumbScrollY;System.Threading.Thread.Sleep(220);deck.Slides[0].IsThumbnailDirty=true;if(!v.PreloadNextPendingThumbnail())throw new Exception("Thumbnail generation did not resume after scrolling");Console.WriteLine("PASS: thumbnail work deferred during scrolling and resumed after idle");
}}