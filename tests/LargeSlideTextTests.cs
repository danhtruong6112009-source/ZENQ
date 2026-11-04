using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using ZenQOffice.Core;
using ZenQOffice.UI;
class LargeSlideTextTests
{
 static void Check(bool condition,string text){if(!condition)throw new Exception(text);}
 static Rectangle Ink(Bitmap image)
 {
  int left=image.Width,top=image.Height,right=-1,bottom=-1;
  for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){var c=image.GetPixel(x,y);if(c.R<70&&c.G<70&&c.B<70){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}}
  return right<0?Rectangle.Empty:Rectangle.FromLTRB(left,top,right+1,bottom+1);
 }
 [STAThread]static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void Run()
 {
  Directory.CreateDirectory("output/large-slide-text");
  var deck=new PresentationRoot();deck.CanvasWidth=5000;deck.CanvasHeight=2500;deck.Slides.Clear();deck.Slides.Add(new SlideNode());
  var view=new SlidesView{Presentation=deck,ActiveSlideIndex=0};
  foreach(var type in new[]{ShapeType.TextBox,ShapeType.Rectangle})
  foreach(float size in new[]{44f,180f,372f})
  {
   var shape=new ShapeNode{Type=type,Text="VietNam",FontName="Segoe UI Black",FontSize=44,TextColor=0,HasFill=false,StrokeWidth=0,WordWrap=false,AutoFitHeight=true,VerticalAlignment=VerticalTextAlignment.Top,Alignment=TextAlignment.Left,Transform=new Transform2D(80,80,4200,100)};
   deck.Slides[0].Shapes.Clear();deck.Slides[0].Shapes.Add(shape);view.SelectedShapeIndex=0;view.SetSelectionFontSize(size);
   using var bitmap=new Bitmap(2000,900);
   using(var native=Graphics.FromImage(bitmap))using(var g=new D2DGraphics(native,2000,900))
   {
    g.Clear(Color.White);view.DrawShape(g,0,0,.4f,shape,false);

   }
   var ink=Ink(bitmap);
   Check(!ink.IsEmpty&&ink.Top>=31&&ink.Top<=36,"Top inset remains fixed as the font grows: "+size);
   Check(ink.Bottom<=Math.Ceiling(32+shape.Transform.Height*.4f)+1,"All glyphs remain within the resized frame: "+size);
   Check(shape.FontSize==size,"Requested large font size is retained");
   float fitted=shape.Transform.Height;using(var native=Graphics.FromImage(bitmap))view.DrawShape(native,0,0,.4f,shape,false);Check(Math.Abs(shape.Transform.Height-fitted)<1,"Repeated repaint does not inflate the frame");
   Console.WriteLine(type+" "+size+": frame "+shape.Transform.Height*.4f+", top gap "+(ink.Top-32)+", ink bottom "+(ink.Bottom-32));
   bitmap.Save("output/large-slide-text/"+type+"-"+size+".png");
   shape.TextRuns=new System.Collections.Generic.List<SlideTextRun>{new SlideTextRun(shape.Text,0,size,false,shape.FontName)};
   view.SetSelectionFontSize(24);
   Check(Math.Abs(shape.FontSize-24)<.01f&&Math.Abs(shape.TextRuns[0].FontSize-24)<.01f,"Reducing a rich-text font does not retain the previous maximum size");
   Check(shape.Transform.Height<fitted||size==24,"Auto-fit contracts when the font becomes smaller");
  }
  var wrapped=new ShapeNode{Type=ShapeType.TextBox,Text="Việt Nam Việt Nam\nHọc tập",FontName="Segoe UI Black",FontSize=180,TextColor=0,HasFill=false,StrokeWidth=0,WordWrap=true,AutoFitHeight=true,Transform=new Transform2D(80,80,1200,40)};
  deck.Slides[0].Shapes.Clear();deck.Slides[0].Shapes.Add(wrapped);
  using(var bitmap=new Bitmap(2000,1200))
  {
   using(var native=Graphics.FromImage(bitmap)){native.Clear(Color.White);view.DrawShape(native,0,0,.6f,wrapped,false);}
   var ink=Ink(bitmap);Check(ink.Top>=46&&ink.Bottom<=Math.Ceiling((wrapped.Transform.Y+wrapped.Transform.Height)*.6f)+1,"Accents, descenders and wrapped paragraphs remain inside the auto-fit box");
   bitmap.Save("output/large-slide-text/wrapped.png");
   float height=wrapped.Transform.Height;
   view.IsInlineEditing=true;view.EditingShape=wrapped;view.EditingTextBuffer=wrapped.Text;view.CaretIndex=3;view.TextSelectionStart=0;view.TextSelectionLength=4;view.CursorVisible=true;
   using(var native=Graphics.FromImage(bitmap))view.DrawShape(native,0,0,.6f,wrapped,true);
   Check(Math.Abs(wrapped.Transform.Height-height)<1,"Entering edit mode preserves the fitted frame");
   Check(view.CurrentCaretScreenH<=wrapped.Transform.Height*.6f,"Caret height fits inside the text frame");
   bitmap.Save("output/large-slide-text/editing.png");
   view.IsInlineEditing=false;view.EditingShape=null;view.TextSelectionLength=0;
  }
  var fixedBox=new ShapeNode{Type=ShapeType.Rectangle,Text="Fixed",FontName="Segoe UI",FontSize=44,AutoFitHeight=false,HasFill=false,StrokeWidth=0,Transform=new Transform2D(0,0,800,100)};
  deck.Slides[0].Shapes.Clear();deck.Slides[0].Shapes.Add(fixedBox);view.SetSelectionFontSize(372);Check(fixedBox.Transform.Height==100,"Explicitly fixed non-auto-fit shapes keep their dimensions");
  Console.WriteLine("PASS: large fonts, constant top margins, growing and shrinking frames, accents, wrapped text, edit-mode caret and fixed shapes");
 }
}
