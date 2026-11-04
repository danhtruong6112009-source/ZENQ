using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using ZenQOffice.Core;
using ZenQOffice.UI;
using ZenQOffice.Format;
class PptxConnectorImportTests
{
 static void Check(bool v,string message){if(!v)throw new Exception(message);}
 [STAThread]static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void Run()
 {
  Directory.CreateDirectory("output/pptx-import");
  var deck=PptxHandler.LoadPptx("tests/fixtures/import-web-form.pptx");var slide=deck.Slides[0];
  var title=slide.Shapes.First(s=>s.Text!=null&&s.Text.Contains("WEB"));
  Check(!title.HasShadow&&title.HasTextShadow,"Text-run shadow must stay on glyphs rather than an enclosing gray rectangle");
  Check(Math.Abs(title.ShadowOpacity-.43f)<.01f,"Text shadow retains alpha");
  var lines=slide.Shapes.Where(s=>s.Name!=null&&s.Name.Contains("Connector")).ToList();
  Check(lines.Count==2&&lines.All(s=>s.Type==ShapeType.Line&&s.LineEndArrowType=="triangle"),"Both imported connectors remain thin lines with triangular endpoint markers");
  Check(lines.Any(s=>s.Transform.FlipHorizontal),"Left connector keeps its mirrored diagonal");
  Check(lines[0].Clone().LineEndArrowType=="triangle","Cloning retains endpoint metadata");
  var view=new SlidesView{Presentation=deck};
  using var bitmap=new Bitmap(960,540);
  using(var graphics=Graphics.FromImage(bitmap))using(var g=new D2DGraphics(graphics,960,540))
   typeof(SlidesView).GetMethod("DrawSlideFull",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{g,new RectangleF(0,0,960,540),slide,960f/deck.CanvasWidth,1f,false});
  bitmap.Save("output/pptx-import/web-form.png");
  PptxHandler.SavePptx("output/pptx-import/roundtrip.pptx",deck);
  var restored=PptxHandler.LoadPptx("output/pptx-import/roundtrip.pptx");
  Check(restored.Slides[0].Shapes.Count(s=>s.Type==ShapeType.Line&&s.LineEndArrowType=="triangle")==2,"PPTX roundtrip retains both thin arrow connectors");
  var rtTitle=restored.Slides[0].Shapes.First(s=>s.Text!=null&&s.Text.Contains("WEB"));
  Check(rtTitle.HasTextShadow&&!rtTitle.HasShadow,"Roundtrip preserves glyph-only shadow");
  Console.WriteLine("PASS: real PPTX text-only shadow/alpha, thin arrow connectors, mirrored direction, clone and PPTX roundtrip");
 }
}
