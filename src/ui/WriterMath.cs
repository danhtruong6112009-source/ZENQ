using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
namespace ZenQOffice.UI
{
 // Inline equation atoms occupy one editor character. Original OMML is retained, not
 // reconstructed from the preview, and is restored as native equation XML on DOCX save.
 public static partial class WriterMath
 {
  const string M="http://schemas.openxmlformats.org/officeDocument/2006/math", W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
  static readonly ConcurrentDictionary<char,XmlElement> Equations=new();
  static readonly Dictionary<string,char> Ids=new(StringComparer.Ordinal);
  static readonly Dictionary<(char,float),Box> Layouts=new();
  static readonly ConcurrentDictionary<(float,FontStyle,string),Font> Fonts=new();
  static readonly object Gate=new();
  static int next=0xe000;
  public static string Register(XmlNode source)
  {
   var doc=new XmlDocument();var root=(XmlElement)doc.ImportNode(source,true);doc.AppendChild(root);
   root.SetAttribute("xmlns:m",M);root.SetAttribute("xmlns:w",W);string xml=root.OuterXml;
   lock(Gate)
   {
    if(Ids.TryGetValue(xml,out char existing))return existing.ToString();
    if(next>0xf8ff)throw new InvalidOperationException("Too many distinct equations in the current session");
    char id=(char)next++;Ids.Add(xml,id);Equations.TryAdd(id,root);return id.ToString();
   }
  }
  public static bool Contains(string text)=>text!=null&&text.Any(c=>c>=0xe000&&c<=0xf8ff&&Equations.ContainsKey(c));
  static bool Icon(Font font)=>font.Name.Contains("Fluent",StringComparison.OrdinalIgnoreCase)||font.Name.Contains("MDL2",StringComparison.OrdinalIgnoreCase)||font.Name.Contains("SegoeIcons",StringComparison.OrdinalIgnoreCase)||font.Name.Contains("Wingdings",StringComparison.OrdinalIgnoreCase)||font.Name=="Symbol";
  internal static bool IsMathText(string text,Font font)=>Contains(text)&&!Icon(font);
  static float Pixels(Font font)=>font.Unit==GraphicsUnit.Pixel?font.Size:font.SizeInPoints*96f/72f;
  static Font Face(float size,FontStyle style=FontStyle.Regular,string family="Cambria Math")
  {
   size=(float)Math.Round(Math.Max(4,size),2);var key=(size,style,family);
   return Fonts.GetOrAdd(key,k=>new Font(k.Item3,k.Item1,k.Item2,GraphicsUnit.Pixel));
  }
  sealed class Box
  {
   public float Width,Height,Ascent;
   public Action<D2DGraphics,float,float,Color> Paint;
   public void Draw(D2DGraphics g,float x,float y,Color color)=>Paint?.Invoke(g,x,y,color);
  }
  static Box Text(D2DGraphics g,string text,float size,FontStyle style=FontStyle.Regular,string family="Cambria Math",Color? ink=null)
  {
   var font=Face(size,style,family);float width=string.IsNullOrEmpty(text)?0:g.MeasureString(text,font).Width;
   float ascent=size*.8f,height=size*1.1f;
   float nativeAscent=g.TextBaseline(font);
   return new Box{Width=width,Height=height,Ascent=ascent,Paint=(d,x,y,color)=>{using var brush=new SolidBrush(ink??color);d.DrawString(text,font,brush,x,y+ascent-nativeAscent);}};
  }
  static Box Row(IEnumerable<Box> input)
  {
   var boxes=input.ToArray();float ascent=boxes.Length==0?0:boxes.Max(b=>b.Ascent),desc=boxes.Length==0?0:boxes.Max(b=>b.Height-b.Ascent);
   return new Box{Width=boxes.Sum(b=>b.Width),Ascent=ascent,Height=ascent+desc,Paint=(g,x,y,c)=>{foreach(var b in boxes){b.Draw(g,x,y+ascent-b.Ascent,c);x+=b.Width;}}};
  }
  static XmlNode Child(XmlNode n,string name)=>n?.ChildNodes.Cast<XmlNode>().FirstOrDefault(c=>c.LocalName==name);
  static string Val(XmlNode node,string fallback="")=>node?.Attributes?.Cast<XmlAttribute>().FirstOrDefault(a=>a.LocalName=="val")?.Value??fallback;
  static Box Build(D2DGraphics g,XmlNode n,float s)
  {
   if(n==null)return Row(Array.Empty<Box>());
   Box Part(string name,float scale=1)=>Build(g,Child(n,name),s*scale);
   string kind=n.LocalName;
   if(kind.EndsWith("Pr",StringComparison.Ordinal)||kind=="ctrlPr")return Row(Array.Empty<Box>());
   if(kind=="r")
   {
    string text=string.Concat(n.SelectNodes(".//*[local-name()='t']").Cast<XmlNode>().Select(t=>t.InnerText));
    string style=Val(Child(Child(n,"rPr"),"sty"));FontStyle fs=style=="p"?FontStyle.Regular:style=="b"?FontStyle.Bold:style=="bi"?FontStyle.Bold|FontStyle.Italic:text.Any(char.IsLetter)?FontStyle.Italic:FontStyle.Regular;
    Color? ink=null;var color=n.SelectSingleNode(".//*[local-name()='color']");if(int.TryParse(Val(color),System.Globalization.NumberStyles.HexNumber,null,out int rgb))ink=Color.FromArgb(255,(rgb>>16)&255,(rgb>>8)&255,rgb&255);
    var leaf=Text(g,text,s,fs,ink:ink);
    if(text.Length==1&&"=+−±×÷<>≤≥≠≈".Contains(text[0]))
     return new Box{Width=leaf.Width+s*.28f,Height=leaf.Height,Ascent=leaf.Ascent,Paint=(d,x,y,c)=>leaf.Draw(d,x+s*.14f,y,c)};
    return leaf;
   }
   if(kind=="t")return Text(g,n.InnerText,s);
   if(kind=="f")
   {
    var a=Part("num",.72f);var b=Part("den",.72f);float gap=Math.Max(1.5f,s*.12f),pad=s*.12f,w=Math.Max(a.Width,b.Width)+2*pad,bar=a.Height+gap;
    bool noBar=Val(Child(Child(n,"fPr"),"type"))=="noBar";
    return new Box{Width=w,Height=a.Height+b.Height+2*gap,Ascent=bar+s*.2f,Paint=(d,x,y,c)=>{a.Draw(d,x+(w-a.Width)/2,y,c);b.Draw(d,x+(w-b.Width)/2,y+bar+gap,c);if(!noBar){using var pen=new Pen(c,Math.Max(.7f,s*.045f));d.DrawLine(pen,x,y+bar,x+w,y+bar);}}};
   }
   if(kind is "sSup" or "sSub" or "sSubSup" or "sPre")
   {
    var body=Part("e");var sub=Part("sub",.65f);var sup=Part("sup",.65f);
    float up=sup.Height>0?Math.Max(0,sup.Ascent+s*.42f-body.Ascent):0;
    float subY=up+body.Ascent+s*.22f-sub.Ascent,supY=up+body.Ascent-s*.42f-sup.Ascent;
    float script=Math.Max(sub.Width,sup.Width),h=Math.Max(up+body.Height,sub.Height>0?subY+sub.Height:0);
    return new Box{Width=body.Width+script,Height=h,Ascent=body.Ascent+up,Paint=(d,x,y,c)=>{bool pre=kind=="sPre";body.Draw(d,x+(pre?script:0),y+up,c);float sx=x+(pre?0:body.Width);sup.Draw(d,sx,y+supY,c);sub.Draw(d,sx,y+subY,c);}};
   }
   if(kind is "acc" or "bar" or "groupChr")
   {
    var body=Part("e");float accent=s*.22f;var pr=Child(n,kind+"Pr");string chr=Val(Child(pr,"chr"),"̂");bool bottom=Val(Child(pr,"pos"))=="bot";
    return new Box{Width=body.Width,Height=body.Height+accent,Ascent=body.Ascent+(bottom?0:accent),Paint=(d,x,y,c)=>
    {
     body.Draw(d,x,y+(bottom?0:accent),c);float ay=bottom?y+body.Height+accent*.65f:y+accent*.9f;
     using var pen=new Pen(c,Math.Max(.7f,s*.045f)){LineJoin=LineJoin.Round};float right=x+Math.Max(2,body.Width);
     if(kind=="bar")d.DrawLine(pen,x,ay,right,ay);
     else if(chr.Contains('̇')||chr.Contains('̈')){float dot=Math.Max(1,s*.09f);using var fill=new SolidBrush(c);if(chr.Contains('̈')){d.FillEllipse(fill,x+body.Width*.3f-dot/2,ay-dot/2,dot,dot);d.FillEllipse(fill,x+body.Width*.7f-dot/2,ay-dot/2,dot,dot);}else d.FillEllipse(fill,x+body.Width/2-dot/2,ay-dot/2,dot,dot);}
     else if(chr.Contains('⃗')||chr.Contains('→')||chr.Contains('↔')){d.DrawLine(pen,x,ay,right,ay);float head=Math.Min(4,s*.22f);d.DrawLines(pen,new[]{new PointF(right-head,ay-head*.6f),new PointF(right,ay),new PointF(right-head,ay+head*.6f)});}
     else if(chr.Contains('̃')||chr.Contains('~'))d.DrawBezier(pen,new PointF(x,ay),new PointF(x+body.Width*.3f,ay-accent),new PointF(x+body.Width*.7f,ay+accent),new PointF(right,ay));
     else d.DrawLines(pen,new[]{new PointF(x,ay+accent*.4f),new PointF(x+body.Width/2,ay-accent*.3f),new PointF(right,ay+accent*.4f)});
    }};
   }
   if(kind=="rad")
   {
    var body=Part("e");var degree=Part("deg",.55f);float left=Math.Max(s*.65f,degree.Width),top=s*.15f;
    return new Box{Width=left+body.Width+s*.12f,Height=body.Height+top,Ascent=body.Ascent+top,Paint=(d,x,y,c)=>{body.Draw(d,x+left,y+top,c);degree.Draw(d,x,y,c);using var pen=new Pen(c,Math.Max(.8f,s*.05f));d.DrawLines(pen,new[]{new PointF(x+left*.12f,y+body.Height*.55f),new PointF(x+left*.4f,y+body.Height*.9f),new PointF(x+left*.8f,y+top),new PointF(x+left+body.Width,y+top)});}};
   }
   if(kind=="d")
   {
    var content=Row(n.ChildNodes.Cast<XmlNode>().Where(c=>c.LocalName=="e").Select(c=>Build(g,c,s)));var props=Child(n,"dPr");string begin=Val(Child(props,"begChr"),"("),end=Val(Child(props,"endChr"),")");
    float scale=Math.Max(1,Math.Min(2,content.Height/(s*1.2f)));
    return Row(new[]{Text(g,begin,s*scale),content,Text(g,end,s*scale)});
   }
   if(kind=="nary")
   {
    string symbol=Val(Child(Child(n,"naryPr"),"chr"),"∫");var op=Text(g,symbol,s*1.4f);var lo=Part("sub",.6f);var hi=Part("sup",.6f);float up=hi.Height*.6f;
    var limits=new Box{Width=op.Width+Math.Max(lo.Width,hi.Width),Height=op.Height+up+lo.Height*.5f,Ascent=op.Ascent+up,Paint=(d,x,y,c)=>{op.Draw(d,x,y+up,c);hi.Draw(d,x+op.Width,y,c);lo.Draw(d,x+op.Width,y+up+op.Ascent,c);}};
    return Row(new[]{limits,Part("e")});
   }
   if(kind is "limLow" or "limUpp")
   {
    var body=Part("e");var limit=Part("lim",.65f);bool upper=kind=="limUpp";float w=Math.Max(body.Width,limit.Width);
    return new Box{Width=w,Height=body.Height+limit.Height,Ascent=body.Ascent+(upper?limit.Height:0),Paint=(d,x,y,c)=>{body.Draw(d,x+(w-body.Width)/2,y+(upper?limit.Height:0),c);limit.Draw(d,x+(w-limit.Width)/2,y+(upper?0:body.Height),c);}};
   }
   if(kind is "m" or "eqArr")
   {
    var rows=kind=="m"?n.ChildNodes.Cast<XmlNode>().Where(c=>c.LocalName=="mr").Select(r=>r.ChildNodes.Cast<XmlNode>().Where(c=>c.LocalName=="e").Select(c=>Build(g,c,s)).ToArray()).ToArray():n.ChildNodes.Cast<XmlNode>().Where(c=>c.LocalName=="e").Select(c=>new[]{Build(g,c,s)}).ToArray();
    int cols=rows.Length==0?0:rows.Max(r=>r.Length);var widths=new float[cols];foreach(var row in rows)for(int i=0;i<row.Length;i++)widths[i]=Math.Max(widths[i],row[i].Width);
    float gap=s*.45f,h=rows.Sum(r=>r.Length==0?0:r.Max(b=>b.Height))+Math.Max(0,rows.Length-1)*gap,w=widths.Sum()+Math.Max(0,cols-1)*gap;
    return new Box{Width=w,Height=h,Ascent=h/2+s*.3f,Paint=(d,x,y,c)=>{foreach(var row in rows){float xx=x,ascent=row.Length==0?0:row.Max(b=>b.Ascent),height=row.Length==0?0:row.Max(b=>b.Height);for(int i=0;i<row.Length;i++){row[i].Draw(d,xx+(widths[i]-row[i].Width)/2,y+ascent-row[i].Ascent,c);xx+=widths[i]+gap;}y+=height+gap;}}};
   }
   return Row(n.ChildNodes.Cast<XmlNode>().Where(c=>c.NodeType==XmlNodeType.Element&&!c.LocalName.EndsWith("Pr",StringComparison.Ordinal)).Select(c=>Build(g,c,s)));
  }
  static Box Equation(D2DGraphics g,char token,float size)
  {
   var key=(token,(float)Math.Round(size,2));lock(Gate){if(!Layouts.TryGetValue(key,out var box)){if(Layouts.Count>4096)Layouts.Clear();bool previous=g.DrawingMath;g.DrawingMath=true;try{box=IsLinear(token)?Text(g,Linear(Equations[token]),size):Build(g,Equations[token],size);}finally{g.DrawingMath=previous;}Layouts.Add(key,box);}return box;}
  }
  static Box Line(D2DGraphics g,string text,Font font)
  {
   float size=Pixels(font);var boxes=new List<Box>();int start=0;
   for(int i=0;i<text.Length;i++)if(Equations.ContainsKey(text[i]))
   {
    if(i>start)boxes.Add(Text(g,text.Substring(start,i-start),size,font.Style,font.Name));
    var equation=Equation(g,text[i],size);int offset=i;char token=text[i];Color? eqColor=GetColor(token);
    boxes.Add(new Box{Width=equation.Width,Height=equation.Height,Ascent=equation.Ascent,Paint=(d,x,y,c)=>
    {
     equation.Draw(d,x,y,eqColor??c);
     if(d.MathTextStart>=0)d.MathPainted?.Invoke(d.MathTextStart+offset,token,new RectangleF(x,y,equation.Width,equation.Height));
    }});
    start=i+1;
   }
   if(start<text.Length)boxes.Add(Text(g,text.Substring(start),size,font.Style,font.Name));return Row(boxes);
  }
  public static SizeF Measure(D2DGraphics g,string text,Font font){var box=Line(g,text,font);return new SizeF(box.Width,box.Height);}
  public static float Height(D2DGraphics g,string text,Font font)=>Contains(text)?Measure(g,text,font).Height:0;
  public readonly struct TextScope:IDisposable
  {
   readonly D2DGraphics g;readonly int previous;
   public TextScope(D2DGraphics drawing,int start){g=drawing;previous=g.MathTextStart;g.MathTextStart=start;}
   public void Dispose(){if(g!=null)g.MathTextStart=previous;}
  }
  public static TextScope CaptureText(D2DGraphics g,int start)=>new TextScope(g,start);
  public readonly struct CaptureScope:IDisposable
  {
   readonly D2DGraphics g;readonly Action<int,char,RectangleF> previous;readonly int previousStart;
   public CaptureScope(D2DGraphics drawing,Action<int,char,RectangleF> painted){g=drawing;previous=g.MathPainted;previousStart=g.MathTextStart;g.MathPainted=painted;g.MathTextStart=-1;}
   public void Dispose(){g.MathPainted=previous;g.MathTextStart=previousStart;}
  }
  public readonly struct BaselineScope:IDisposable
  {
   readonly D2DGraphics g;readonly float? previous;readonly float top;
   public BaselineScope(D2DGraphics drawing,float baseline,float y){g=drawing;previous=g.MathBaseline;top=g.MathLineTop;g.MathBaseline=baseline;g.MathLineTop=y;}
   public void Dispose(){if(g!=null){g.MathBaseline=previous;g.MathLineTop=top;}}
  }
  public static BaselineScope BeginLine(D2DGraphics g,string text,Font font,float y)=>Contains(text)?new BaselineScope(g,y+Line(g,text,font).Ascent,y):default;
  internal static bool TryDraw(D2DGraphics g,string text,Font font,Brush brush,RectangleF bounds,StringFormat format)
  {
   if(g.DrawingMath||Icon(font)||(!Contains(text)&&!g.MathBaseline.HasValue))return false;
   g.DrawingMath=true;
   try
   {
    var box=Line(g,text,font);float x=bounds.X,y=bounds.Y;
    if(format?.Alignment==StringAlignment.Center)x+=(bounds.Width-box.Width)/2;
    else if(format?.Alignment==StringAlignment.Far)x+=bounds.Width-box.Width;
    if(g.MathBaseline.HasValue)y=g.MathBaseline.Value+(bounds.Y-g.MathLineTop)-box.Ascent;
    else if(format?.LineAlignment==StringAlignment.Center)y+=(bounds.Height-box.Height)/2;
    else if(format?.LineAlignment==StringAlignment.Far)y+=bounds.Height-box.Height;
    box.Draw(g,x,y,brush is SolidBrush solid?solid.Color:Color.Black);return true;
   }
   finally{g.DrawingMath=false;}
  }
  public static void RestoreOmml(string path)
  {
   using var archive=ZipFile.Open(path,ZipArchiveMode.Update);
   foreach(string part in archive.Entries.Select(e=>e.FullName).Where(n=>n.StartsWith("word/")&&n.EndsWith(".xml")&&!n.Contains("/_rels/")).ToArray())
   {
    var entry=archive.GetEntry(part);string xml;using(var reader=new StreamReader(entry.Open()))xml=reader.ReadToEnd();if(!Contains(xml))continue;
    var doc=new XmlDocument{PreserveWhitespace=true};doc.LoadXml(xml);
    foreach(XmlElement run in doc.SelectNodes("//*[local-name()='r' and namespace-uri()='"+W+"']").Cast<XmlNode>().ToArray())
    {
     if(!run.ChildNodes.Cast<XmlNode>().Any(n=>n.LocalName=="t"&&Contains(n.InnerText)))continue;
     var parent=run.ParentNode;XmlElement pending=null;
     XmlElement NewRun(){var nextRun=doc.CreateElement("w","r",W);var props=run.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.LocalName=="rPr");if(props!=null)nextRun.AppendChild(props.CloneNode(true));parent.InsertBefore(nextRun,run);return nextRun;}
     foreach(XmlNode child in run.ChildNodes)
     {
      if(child.LocalName=="rPr")continue;
      if(child.LocalName!="t"||!Contains(child.InnerText)){(pending??=NewRun()).AppendChild(child.CloneNode(true));continue;}
      var chunk=new StringBuilder();
      void Flush(){if(chunk.Length==0)return;var t=doc.CreateElement("w","t",W);var space=doc.CreateAttribute("xml","space","http://www.w3.org/XML/1998/namespace");space.Value="preserve";t.Attributes.Append(space);t.InnerText=chunk.ToString();(pending??=NewRun()).AppendChild(t);chunk.Clear();}
      foreach(char c in child.InnerText)if(Equations.TryGetValue(c,out var equation)){Flush();pending=null;var imported=doc.ImportNode(equation,true);parent.InsertBefore(imported,run);}else chunk.Append(c);
      Flush();
     }
     parent.RemoveChild(run);
    }
    entry.Delete();using var writer=new StreamWriter(archive.CreateEntry(part).Open(),new UTF8Encoding(false));doc.Save(writer);
   }
  }
 }
}
