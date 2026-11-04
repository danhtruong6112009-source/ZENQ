using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using ZenQOffice.Core;using Vortice.Direct2D1;
namespace ZenQOffice.UI {public partial class SlidesView {
 readonly Dictionary<(ShapeNode,ID2D1RenderTarget,float),FlowLayer> morphTextLayers=new();
 readonly Dictionary<(ShapeNode,ShapeNode),bool> morphLineMatches=new();
 void ClearStableMorphText(){foreach(var layer in morphTextLayers.Values)layer.Dispose();morphTextLayers.Clear();morphLineMatches.Clear();}
 FlowLayer GetStableMorphText(D2DGraphics g,ShapeNode source,float scale){
  var key=(source,g.RenderTarget,scale);if(morphTextLayers.TryGetValue(key,out var layer))return layer;
  var text=source.CreateRenderSnapshot();text.Transform.X=0;text.Transform.Y=0;text.Transform.Rotation=0;text.Transform.FlipHorizontal=text.Transform.FlipVertical=false;
  text.Type=ShapeType.TextBox;text.SmartArtType=null;text.ChartData=null;text.IsGraphic=false;text.IsSvg=false;
  text.HasFill=false;text.HasGradientFill=false;text.FillOpacity=0;text.StrokeWidth=0;text.HasShadow=false;text.CachedImage=null;text.ImagePath="";text.NativeD2DBitmap=null;text.NativeGifFrames=null;
  float pad=Math.Max(4,source.HasTextOutline?source.TextOutlineWidth*scale+3:4);
  var bounds=new RectangleF(-pad,-pad,Math.Max(1,source.Transform.Width*scale)+pad*2,Math.Max(1,source.Transform.Height*scale)+pad*2);
  layer=CaptureFlowLayer(g,bounds,drawing=>DrawShape(drawing,0,0,scale,text,false));morphTextLayers.Add(key,layer);return layer;
 }
 static bool UniformMorphText(ShapeNode shape)=>shape.TextRuns==null||shape.TextRuns.All(r=>r==null||string.IsNullOrEmpty(r.Text)||(!r.HasGradient&&!r.ParaAlignment.HasValue&&!r.ParaLineSpacing.HasValue&&r.FontName==shape.FontName&&Math.Abs(r.FontSize-GetMaxRunFontSize(shape))<.01f&&r.Bold==shape.Bold&&r.Italic==shape.Italic&&r.Underline==shape.Underline&&(r.TextColor&0xffffff)==(shape.TextColor&0xffffff)));
 bool SameMorphLines(D2DGraphics g,ShapeNode a,ShapeNode b){
  if(morphLineMatches.TryGetValue((a,b),out bool match))return match;
  string[] Lines(ShapeNode shape){using var font=SlideFontManager.CreateFont(shape.FontName,GetMaxRunFontSize(shape),(shape.Bold?FontStyle.Bold:FontStyle.Regular)|(shape.Italic?FontStyle.Italic:FontStyle.Regular),GraphicsUnit.Pixel);return GetParagraphVisualLines(g,shape,shape.Text,font,Math.Max(1,shape.Transform.Width-(shape.MarginLeft??6)-(shape.MarginRight??6)),shape.CharacterSpacing).Select(line=>line.Text).ToArray();}
  match=Lines(a).SequenceEqual(Lines(b));morphLineMatches[(a,b)]=match;return match;
 }
 void DrawStableMorphText(D2DGraphics g,float slideX,float slideY,float scale,ShapeNode a,ShapeNode b,float t,ShapeNode container){
  float font=GetMaxRunFontSize(a)+(GetMaxRunFontSize(b)-GetMaxRunFontSize(a))*t;
  bool same=a.Text==b.Text&&a.FontName==b.FontName&&a.Bold==b.Bold&&a.Italic==b.Italic&&a.Underline==b.Underline&&a.Alignment==b.Alignment&&a.VerticalAlignment==b.VerticalAlignment&&a.WordWrap==b.WordWrap&&a.CharacterSpacing==b.CharacterSpacing&&a.LineSpacing==b.LineSpacing&&(a.TextColor&0xffffff)==(b.TextColor&0xffffff)&&!a.HasTextGradient&&!b.HasTextGradient&&!a.HasTextOutline&&!b.HasTextOutline&&UniformMorphText(a)&&UniformMorphText(b)&&(a.ParagraphStyles?.Count??0)==0&&(b.ParagraphStyles?.Count??0)==0;
  if(same&&a.WordWrap) same=SameMorphLines(g,a,b);
  void Draw(ShapeNode source,float opacity){
   if(string.IsNullOrEmpty(source.Text)||opacity<=.001f)return;
   var layer=GetStableMorphText(g,source,scale);float factor=font/Math.Max(.01f,GetMaxRunFontSize(source));
   float w=container.Transform.Width*scale,h=container.Transform.Height*scale,sw=source.Transform.Width*scale,sh=source.Transform.Height*scale;
   float dx=source.Alignment==TextAlignment.Center?w/2-sw/2*factor:source.Alignment==TextAlignment.Right?w-(source.MarginRight??6)*scale-(sw-(source.MarginRight??6)*scale)*factor:(source.MarginLeft??6)*scale*(1-factor);
   float dy=source.VerticalAlignment==VerticalTextAlignment.Middle?h/2-sh/2*factor:source.VerticalAlignment==VerticalTextAlignment.Bottom?h-(source.MarginBottom??4)*scale-(sh-(source.MarginBottom??4)*scale)*factor:(source.MarginTop??4)*scale*(1-factor);
   var state=g.Save();try{
    g.TranslateTransform(slideX+container.Transform.X*scale+w/2,slideY+container.Transform.Y*scale+h/2);g.RotateTransform(container.Transform.Rotation);
    g.ScaleTransform(container.Transform.FlipHorizontal?-1:1,container.Transform.FlipVertical?-1:1);g.TranslateTransform(-w/2,-h/2);
    var r=layer.Bounds;layer.Draw(g,new RectangleF(dx+r.X*factor,dy+r.Y*factor,r.Width*factor,r.Height*factor),opacity);
   }finally{g.Restore(state);}
  }
  if(same)Draw(b,1);else{Draw(a,1-t);Draw(b,t);}
 }
}}
