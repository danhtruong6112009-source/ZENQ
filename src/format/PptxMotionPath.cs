using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
namespace ZenQOffice.Format
{
 internal static class PptxMotionPath
 {
  public static List<PointF> Parse(string value,float width,float height)
  {
   var result=new List<PointF>();if(string.IsNullOrEmpty(value))return result;
   var tokens=Regex.Matches(value,@"[A-Za-z]|[-+]?(?:[0-9]*\.)?[0-9]+(?:[eE][-+]?[0-9]+)?");int i=0;char command='M';PointF cursor=new(),first=new();
   bool Number()=>i<tokens.Count&&float.TryParse(tokens[i].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out _);
   float Read()=>float.Parse(tokens[i++].Value,CultureInfo.InvariantCulture);
   PointF Point(bool relative){float x=Read()*width,y=Read()*height;return relative?new PointF(cursor.X+x,cursor.Y+y):new PointF(x,y);}
   try
   {
    while(i<tokens.Count)
    {
     if(!Number())command=tokens[i++].Value[0];
     bool relative=char.IsLower(command);char op=char.ToUpperInvariant(command);
     if(op=='E')break;
     if(op=='Z'){result.Add(first);cursor=first;continue;}
     if(!Number())continue;
     if(op=='M'||op=='L'){cursor=Point(relative);if(op=='M')first=cursor;result.Add(cursor);command=relative?'l':'L';}
     else if(op=='C')
     {
      var a=cursor;var b=Point(relative);var c=Point(relative);var d=Point(relative);
      for(int n=1;n<=32;n++){float t=n/32f,u=1-t;result.Add(new PointF(u*u*u*a.X+3*u*u*t*b.X+3*u*t*t*c.X+t*t*t*d.X,u*u*u*a.Y+3*u*u*t*b.Y+3*u*t*t*c.Y+t*t*t*d.Y));}cursor=d;
     }
     else if(op=='Q')
     {
      var a=cursor;var b=Point(relative);var c=Point(relative);
      for(int n=1;n<=24;n++){float t=n/24f,u=1-t;result.Add(new PointF(u*u*a.X+2*u*t*b.X+t*t*c.X,u*u*a.Y+2*u*t*b.Y+t*t*c.Y));}cursor=c;
     }
     else {i++;}
    }
   }catch(FormatException){}catch(ArgumentOutOfRangeException){}
   return result;
  }
 }
}
