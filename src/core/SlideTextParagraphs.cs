using System;
using System.Collections.Generic;
namespace ZenQOffice.Core
{
 public readonly record struct SlideTextRange(int Index,int Start,int End);
 public static class SlideTextParagraphs
 {
  public static List<SlideTextRange> Ranges(string text)
  {
   text??="";var result=new List<SlideTextRange>();int start=0,index=0;
   for(int i=0;i<text.Length;i++)if(text[i]=='\n'){result.Add(new(index++,start,i));start=i+1;}
   result.Add(new(index,start,text.Length));return result;
  }
  public static SlideParagraphStyle Style(ShapeNode shape,int index)=>shape.ParagraphStyles?.Find(p=>p.Index==index);
 }
}
