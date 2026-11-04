using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ZenQOffice.UI;
class SmartArtGalleryTests
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static Bitmap Render(SmartArtGalleryForm gallery,SmartArtGalleryForm.SmartArtTemplate template,bool preview)
 {
  var image=new Bitmap(480,220);using(var surface=Graphics.FromImage(image))using(var g=new D2DGraphics(surface,480,220))
  {
   g.Clear(Color.White);
   if(preview)typeof(SmartArtGalleryForm).GetMethod("DrawOfficePreviewDiagram",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gallery,new object[]{g,new Rectangle(0,0,480,220),template.TypeId});
   else ShapeRenderer.DrawShape(g,SmartArtRenderer.CreateDefaultSmartArt(template.TypeId,template.Name),new Rectangle(0,0,480,220),null,1f);
  }
  return image;
 }
 [STAThread]static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void Run()
 {
  Directory.CreateDirectory("output/smartart-gallery");using var gallery=new SmartArtGalleryForm();
  var templates=(List<SmartArtGalleryForm.SmartArtTemplate>)typeof(SmartArtGalleryForm).GetField("allTemplates",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  Check(templates.Select(t=>t.TypeId).Distinct().Count()==templates.Count,"Catalog IDs remain unique");
  foreach(var template in templates)
  {
   using var preview=Render(gallery,template,true);using var actual=Render(gallery,template,false);
   for(int y=0;y<220;y+=2)for(int x=0;x<480;x+=2)Check(preview.GetPixel(x,y)==actual.GetPixel(x,y),"Preview differs from insertion: "+template.TypeId);
  }
  var dispatch=(System.Collections.IDictionary)typeof(SmartArtRenderer).GetField("renderDispatch",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  Check(templates.All(t=>dispatch.Contains(t.TypeId)),"Every selectable template has an explicit renderer");
  var varied=templates.First(t=>t.TypeId=="List_VaryingWidth");var basic=templates.First(t=>t.TypeId=="List_BasicBlock");
  using(var a=Render(gallery,varied,true))using(var b=Render(gallery,basic,true))
  {
   int differences=0;for(int y=0;y<220;y++)for(int x=0;x<480;x++)if(a.GetPixel(x,y)!=b.GetPixel(x,y))differences++;
   Check(differences>5000,"Varying Width List no longer duplicates Basic Block List");a.Save("output/smartart-gallery/varying-width.png");b.Save("output/smartart-gallery/basic-block.png");
  }
  var writer=new WriterView();writer.InsertSmartArt(varied.TypeId,varied.Name);var inserted=writer.DocumentShapes.Last();var expected=SmartArtRenderer.CreateDefaultSmartArt(varied.TypeId,varied.Name);
  Check(inserted.TextContent==expected.TextContent&&inserted.BaseWidth==480&&inserted.BaseHeight==220&&inserted.StylePresetIndex==expected.StylePresetIndex,"Writer inserts the same node data, size and theme as the preview");
  Check(SmartArtRenderer.GetSmartArtNodeList("Process_StepDown","|Beta|Gamma").Length==3,"An empty text node retains its original slot");
  foreach(string type in new[]{"Process_StepDown","Process_Staggered","List_BasicBlock","Cycle_Radial"})
  {
   var node=SmartArtRenderer.CreateDefaultSmartArt(type,type);node.TextContent="Alpha|Beta|Gamma";node.IsSelected=true;node.SelectedNodeIndex=0;
   Bitmap Frame(){var image=new Bitmap(480,220);using(var surface=Graphics.FromImage(image))using(var g=new D2DGraphics(surface,480,220)){g.Clear(Color.White);ShapeRenderer.DrawShape(g,node,new Rectangle(0,0,480,220),null,1f);}return image;}
   using var before=Frame();var positions=node.SubShapeRelativeBounds.ToArray();
   writer.StartSmartArtInlineTextEdit(node,0,480,null,null);
   using var during=Frame();int changed=0;
   for(int y=0;y<220;y++)for(int x=0;x<480;x++)if(before.GetPixel(x,y)!=during.GetPixel(x,y))changed++;
   Check(changed<600,"Entering edit mode must not repaint the node as a generic blue shape: "+type+" changed="+changed);
   Check(node.SubShapeRelativeBounds.SequenceEqual(positions),"Editing preserves the rendered node positions: "+type);
   if(type=="Process_StepDown")
   {
    var boxes=node.SubShapeRelativeBounds;
    Check(boxes.Count==3&&boxes.All(b=>Math.Abs(b.Height-boxes[0].Height)<.001f)&&boxes[0].Y<boxes[1].Y&&boxes[1].Y<boxes[2].Y,"Word Step Down uses equal-size nodes descending diagonally, not columns of varying heights");
   }
   during.Save("output/smartart-gallery/edit-"+type+".png");
  }
  var blank=SmartArtRenderer.CreateDefaultSmartArt("Process_StepDown","Empty slot");var prior=SmartArtRenderer.GetSmartArtNodeList(blank.SmartArtType,blank.TextContent).Length;writer.StartSmartArtInlineTextEdit(blank,0,480,null,null);
  Check(SmartArtRenderer.GetSmartArtNodeList(blank.SmartArtType,blank.TextContent).Length==prior,"Clearing a placeholder on double click cannot shift later nodes");
  Console.WriteLine("PASS: "+templates.Count+" catalog previews match insertion; double-click preserves geometry/colors on four layouts; empty nodes retain slots; Word Basic Block and Step Down geometry");
 }
}