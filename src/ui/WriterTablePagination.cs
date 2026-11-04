using System;
using System.Collections.Generic;
namespace ZenQOffice.UI
{
 public readonly record struct WriterTableFragment(int Row,int Page,int Y,int Height,int Offset);
 public static class WriterTablePagination
 {
  public static List<WriterTableFragment> Build(IReadOnlyList<int> heights,int firstPage,int startY,Func<int,int> top,Func<int,int> bottom,Func<int,bool> keepRow=null,Func<int,int,int,int> splitHeight=null,bool repeatHeader=false)
  {
   var result=new List<WriterTableFragment>();int page=firstPage,y=Math.Max(startY,top(page));
   for(int row=0;row<heights.Count;row++)
   {
    int remaining=Math.Max(1,heights[row]),offset=0;
    void NextPage(){page++;y=top(page);if(repeatHeader&&row>0&&heights[0]<bottom(page)-y-20){result.Add(new WriterTableFragment(0,page,y,heights[0],0));y+=heights[0];}}
    if((keepRow?.Invoke(row)??false)&&remaining<=bottom(page)-top(page)&&y+remaining>bottom(page)){NextPage();}
    while(remaining>0)
    {
     int available=bottom(page)-y;
     if(available<=0){NextPage();available=bottom(page)-y;}
     if(available<=0)throw new ArgumentException("Page margins leave no table content area");
     int take=Math.Min(remaining,available);
     if(remaining>available&&splitHeight!=null)take=Math.Clamp(splitHeight(row,offset,take),1,take);
     result.Add(new WriterTableFragment(row,page,y,take,offset));remaining-=take;offset+=take;y+=take;
     if(remaining>0){NextPage();}
    }
   }
   return result;
  }
 }
}
