using System.Drawing;using System.IO;using ZenQOffice.UI;using ZenQOffice.Format;using System;using System.Reflection;using System.Collections.Generic;using ZenQOffice.Core;
class Test {
 static void Check(bool value,string msg){if(!value)throw new Exception(msg);}
 static Dictionary<string,object> Element(string type,params object[] values){var d=new Dictionary<string,object>{{"type",type},{"bounds",new List<object>{100d,120d,500d,700d}}};for(int i=0;i<values.Length;i+=2)d[(string)values[i]]=values[i+1];return d;}
 [STAThread]static void Main(){
 var prompt=SlideDesignMetadata.BuildSlidePrompt("3 slide 20/10 sáng tạo",3,0,"Bìa",1080,1920);Check(prompt.Contains("1080 x 1920"),"Actual canvas used");Check(!prompt.Contains("ÍT NHẤT 15")&&!prompt.Contains("Ambient Orbs")&&!prompt.Contains("TRỌNG TÂM CHIẾN LƯỢC"),"No compulsory visual template");
 var engine=typeof(AiActionEngine);var render=engine.GetMethod("RenderSlideFromCanvasDict",BindingFlags.Static|BindingFlags.NonPublic);
 string svg="<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><path d='M50 5 L95 95 L5 95 Z' fill='#e11d48'/></svg>";
 var elements=new List<object>{Element("shape","shapeType","rectangle","fill","#112233","strokeWidth",0d),Element("text","text","TỎA SÁNG","fontSize",80d),Element("svg","svg",svg)};
 var payload=new Dictionary<string,object>{{"title","Tỏa sáng"},{"background",new Dictionary<string,object>{{"type","solid"},{"color","#ffffff"}}},{"elements",elements}};
 var slide=new SlideNode();render.Invoke(null,new object[]{slide,payload,null,1920f,1080f});Check(slide.Shapes.Count==3,"All freeform elements rendered once");Check(slide.Shapes[0].Transform.Height==700&&slide.Shapes[1].Transform.Height==700,"Tall shape and text bounds stay as designed");Check(((List<object>)((Dictionary<string,object>)elements[0])["bounds"])[3].Equals(700d),"Input geometry not mutated");Check(slide.Shapes[2].IsSvg&&slide.Shapes[2].SvgContent==svg&&slide.Shapes[2].CachedImage!=null,"Inline original SVG and artwork retained");
 Check(!slide.Shapes[1].HasFill,"AI text is explicitly unfilled immediately after creation");
 var text=slide.Shapes[1];var view=new SlidesView();using(var image=new Bitmap(960,540))using(var graphics=Graphics.FromImage(image)){
  using(var drawing=new D2DGraphics(graphics,960,540)){drawing.Clear(Color.Maroon);view.DrawShape(drawing,0,0,.5f,text,false);}
  var pixel=image.GetPixel(290,400);Check(pixel.R==128&&pixel.G==0&&pixel.B==0,"Text box keeps the colored background without a white rectangle");
 }
 var deck=new PresentationRoot();deck.Slides.Clear();deck.Slides.Add(slide);Directory.CreateDirectory("output/ai-text-fill");PptxHandler.SavePptx("output/ai-text-fill/transparent-text.pptx",deck);var restored=PptxHandler.LoadPptx("output/ai-text-fill/transparent-text.pptx");Check(!restored.Slides[0].Shapes[1].HasFill,"AI text fill matches after PPTX reopen");
 Check(slide.Shapes[0].HasFill,"Intentional shape backgrounds remain filled");
 Console.WriteLine("PASS: free art direction, real canvas, preserved tall composition, editable typography and native inline SVG without API calls");
 }}