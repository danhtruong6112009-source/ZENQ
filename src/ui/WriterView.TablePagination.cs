using System;
using System.Collections.Generic;
using System.Drawing;
namespace ZenQOffice.UI
{
 public partial class WriterView
 {
  private int SafeTableSplit(D2DGraphics g,DocTableObject table,int row,List<int> widths,int offset,int available)
  {
   if(table==null||row>=table.Rows.Count)return available;
   var spans=new List<(float Start,float End)>();
   for(int column=0;column<table.Rows[row].Cells.Count&&column<widths.Count;column++)
   {
    var cell=table.Rows[row].Cells[column];
    float left=table.CellMarginLeftPt>=0?table.CellMarginLeftPt:(table.CellPaddingPt>0?table.CellPaddingPt:4f);
    float right=table.CellMarginRightPt>=0?table.CellMarginRightPt:(table.CellPaddingPt>0?table.CellPaddingPt:4f);
    float pad=table.CellMarginTopPt>=0?Math.Max(1,(int)(table.CellMarginTopPt*1.333333f*ZoomLevel)):1;
    float width=Math.Max(10,widths[column]-(left+right)*1.333333f*ZoomLevel),y=pad;
    if(cell.Paragraphs==null)continue;
    foreach(var paragraph in cell.Paragraphs)
    {
     y+=Math.Max(0,paragraph.SpacingBeforePt)*1.333333f*ZoomLevel;
     float size=paragraph.FontSize>0?paragraph.FontSize:11f;
     var style=(paragraph.Bold?FontStyle.Bold:FontStyle.Regular)|(paragraph.Italic?FontStyle.Italic:FontStyle.Regular);
     var font=GetCachedFont(paragraph.FontName??"Times New Roman",size*ZoomLevel,style);
     var baseFont=GetCachedFont(paragraph.FontName??"Times New Roman",size,style);
     var lines=BreakCellTextIntoLines(g,paragraph.Text??"",baseFont,width/ZoomLevel);
     float lineHeight=Math.Max(font.GetHeight(g)*(paragraph.LineSpacing>0?paragraph.LineSpacing:1f),WriterMath.Height(g,paragraph.Text,font)+2);
     for(int line=0;line<Math.Max(1,lines.Count);line++)
     {spans.Add((y,y+Math.Max(1,lineHeight-1)));y+=lineHeight;}
     y+=Math.Max(0,paragraph.SpacingAfterPt)*1.333333f*ZoomLevel;
    }
   }
   int end=offset+available;
   for(int pass=0;pass<spans.Count;pass++)
   {
    int previous=end;
    foreach(var span in spans)if(span.Start<end&&span.End>end)end=Math.Min(end,(int)Math.Floor(span.Start));
    if(end==previous)break;
   }
   return end>offset+1?end-offset:available;
  }
 }
}
