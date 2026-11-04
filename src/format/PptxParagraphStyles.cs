using System;
using System.IO;
using System.Security;
using ZenQOffice.Core;
namespace ZenQOffice.Format
{
 internal static class PptxParagraphStyles
 {
  public static void Write(StreamWriter writer,ShapeNode shape,int index,string alignment,string spacing,float scale)
  {
   var style=SlideTextParagraphs.Style(shape,index);
   string attrs=style==null?"":$" lvl=\"{style.Level}\" marL=\"{(int)(style.LeftMargin*scale*9525)}\" marR=\"{(int)(style.RightMargin*scale*9525)}\" indent=\"{(int)(style.Indent*scale*9525)}\"";
   writer.Write("<a:pPr algn=\""+alignment+"\""+attrs+">");writer.Write(spacing);
   if(style!=null)
   {
    if(style.BulletSize!=1)writer.Write("<a:buSzPct val=\""+(int)(style.BulletSize*100000)+"\"/>");
    if(!string.IsNullOrEmpty(style.BulletFont))writer.Write("<a:buFont typeface=\""+SecurityElement.Escape(style.BulletFont)+"\"/>");
    if(style.BulletColor.HasValue)writer.Write("<a:buClr><a:srgbClr val=\""+(style.BulletColor.Value&0xffffff).ToString("X6")+"\"/></a:buClr>");
    if(!string.IsNullOrEmpty(style.Bullet))writer.Write("<a:buChar char=\""+SecurityElement.Escape(style.Bullet)+"\"/>");
    else if(!string.IsNullOrEmpty(style.Numbering))writer.Write("<a:buAutoNum type=\""+SecurityElement.Escape(style.Numbering)+"\""+(style.StartAt>1?" startAt=\""+style.StartAt+"\"":"")+"/>");
    else writer.Write("<a:buNone/>");
   }
   writer.WriteLine("</a:pPr>");
  }
 }
}
