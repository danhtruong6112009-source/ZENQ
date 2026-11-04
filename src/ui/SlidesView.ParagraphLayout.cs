using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using ZenQOffice.Core;
namespace ZenQOffice.UI
{
 public partial class SlidesView
 {
  private List<VisualTextLine> GetParagraphVisualLines(D2DGraphics g,ShapeNode shape,string text,Font font,float width,float spacing,float layoutScale=1f)
  {
   if(shape.ParagraphStyles==null||shape.ParagraphStyles.Count==0)return GetVisualWrappedLines(g,text,font,width,shape.WordWrap,spacing);
   var result=new List<VisualTextLine>();
   foreach(var range in SlideTextParagraphs.Ranges(text))
   {
    var style=SlideTextParagraphs.Style(shape,range.Index);float available=Math.Max(1,width-((style?.LeftMargin??0)+(style?.RightMargin??0))*layoutScale);
    var lines=GetVisualWrappedLines(g,text.Substring(range.Start,range.End-range.Start),font,available,shape.WordWrap,spacing);
    if(lines.Count==0)result.Add(new VisualTextLine{Text="",StartCharIndex=range.Start,Length=0});
    foreach(var line in lines)result.Add(new VisualTextLine{Text=line.Text,Length=line.Length,StartCharIndex=line.StartCharIndex+range.Start});
   }
   return result;
  }
  private static RectangleF ParagraphTextBounds(ShapeNode shape,string text,int index,RectangleF original,float scale)
  {
   int paragraph=0;foreach(var range in SlideTextParagraphs.Ranges(text))if(index>=range.Start)paragraph=range.Index;
   var style=SlideTextParagraphs.Style(shape,paragraph);original.X+=(style?.LeftMargin??0)*scale;
   original.Width=Math.Max(1,original.Width-((style?.LeftMargin??0)+(style?.RightMargin??0))*scale);return original;
  }
  private void DrawParagraphBullet(D2DGraphics g,ShapeNode shape,SlideParagraphStyle style,int paragraph,Font font,RectangleF bounds,float y,float lineHeight,float scale,Color color)
  {
   if(style==null||string.IsNullOrEmpty(style.Bullet)&&string.IsNullOrEmpty(style.Numbering))return;
   float x=bounds.X+(style.LeftMargin+style.Indent)*scale;
   if(style.BulletColor.HasValue)color=Color.FromArgb(color.A,Color.FromArgb(style.BulletColor.Value&0xffffff));
   using var fill=new SolidBrush(color);float size=font.Size*style.BulletSize;
   if((style.BulletFont??"").StartsWith("Wingdings",StringComparison.OrdinalIgnoreCase)&&style.Bullet=="§")
   {float edge=Math.Max(2,size*.21f);g.FillRectangle(fill,x,y+size*.46f,edge,edge);return;}
   string bullet=style.Bullet;
   if(!string.IsNullOrEmpty(style.Numbering))
   {
    int number=style.StartAt;
    if(shape.ParagraphStyles!=null)foreach(var previous in shape.ParagraphStyles)if(previous.Index<paragraph&&previous.Level==style.Level&&previous.Numbering==style.Numbering)number++;
    bullet=style.Numbering.StartsWith("alpha")?((char)((style.Numbering.Contains("Uc")?'A':'a')+(number-1)%26)).ToString():number.ToString();
    bullet+=style.Numbering.Contains("ParenR")?")":".";
   }
   using var bulletFont=SlideFontManager.CreateFont(string.IsNullOrEmpty(style.BulletFont)?shape.FontName:style.BulletFont,Math.Max(1,size),FontStyle.Regular,GraphicsUnit.Pixel);
   g.DrawString(bullet,bulletFont,fill,new PointF(x,y),StringFormat.GenericTypographic);
  }
  public float GetActiveFontSize()
  {
   ShapeNode targetShape=(IsInlineEditing&&EditingShape!=null)?EditingShape:SelectedShape;
   if(targetShape==null)return 18f;
   if(IsInlineEditing&&targetShape.TextRuns!=null&&targetShape.TextRuns.Count>0)
   {
    int sLen=Math.Abs(TextSelectionLength);
    int checkPos=sLen>0?Math.Min(TextSelectionStart,TextSelectionStart+TextSelectionLength):Math.Max(0,CaretIndex-1);
    int acc=0;
    foreach(var r in targetShape.TextRuns)
    {
     int rLen=(r.Text??"").Length;
     if(checkPos>=acc&&checkPos<acc+rLen&&r.FontSize>0)return r.FontSize;
     acc+=rLen;
    }
   }
   return GetMaxRunFontSize(targetShape);
  }
  public static float GetMaxRunFontSize(ShapeNode shape)
  {
   if(shape==null)return 18f;
   float maxSz=0;
   if(shape.TextRuns!=null)
   {
    foreach(var r in shape.TextRuns)
    {
     if(r!=null&&!string.IsNullOrEmpty(r.Text)&&r.FontSize>maxSz)maxSz=r.FontSize;
    }
   }
   return maxSz>0?maxSz:shape.FontSize>0?shape.FontSize:18f;
  }
  private void EnsureTextBoxFitsFontSize(ShapeNode shape)
  {
   if(shape==null||shape.Transform==null||(shape.Type!=ShapeType.TextBox&&!UsesInkTextAutoFit(shape)))return;
   float maxSz=GetMaxRunFontSize(shape);
   shape.FontSize=maxSz;
   float padY=(shape.MarginTop??4f)+(shape.MarginBottom??4f)+4f;
   float effSpacing=(shape.TextRuns!=null&&shape.TextRuns.Count>0&&shape.TextRuns[0].ParaLineSpacing.HasValue)?shape.TextRuns[0].ParaLineSpacing.Value:shape.LineSpacing;
   float baseLineH=maxSz*1.28f;
   if(effSpacing>4f)baseLineH=Math.Max(baseLineH,effSpacing);
   else if(effSpacing<0)baseLineH=maxSz*1.25f*(-effSpacing);
   else if(effSpacing>0&&Math.Abs(effSpacing-1.2f)>0.01f)baseLineH=maxSz*1.25f*(effSpacing/1.2f);
   float minLineH=baseLineH+padY;
   if(!UsesInkTextAutoFit(shape)&&shape.Transform.Height<minLineH)shape.Transform.Height=minLineH;
   string raw=((IsInlineEditing&&EditingShape==shape)?(EditingTextBuffer??""):(shape.Text??"")).Replace("\r\n","\n").Replace("\r","\n");
   if(!string.IsNullOrEmpty(raw))
   {
    try
    {
     FontStyle fs=(shape.Bold?FontStyle.Bold:FontStyle.Regular)|(shape.Italic?FontStyle.Italic:FontStyle.Regular);
     using var font=SlideFontManager.CreateFont(shape.FontName,Math.Max(1f,maxSz),fs,GraphicsUnit.Pixel);
     using var g=new D2DGraphics();
     float padX=(shape.MarginLeft??6f)+(shape.MarginRight??6f);
     if(shape.AutoFitWidth)
     {
      float maxW=0f;
      foreach(var p in raw.Split('\n'))
      {
       float w=string.IsNullOrEmpty(p)?0f:g.MeasureString(p,font,shape.CharacterSpacing).Width;
       if(w>maxW)maxW=w;
      }
      float maxAllowedW=(Presentation!=null)?Math.Max(80f,Presentation.CanvasWidth-shape.Transform.X-20f):1800f;
      shape.Transform.Width=Math.Min(maxAllowedW,Math.Max(shape.Transform.Width,maxW+padX));
     }
     float availW=Math.Max(20f,shape.Transform.Width-padX);
     var vLines=GetParagraphVisualLines(g,shape,raw,font,availW,shape.CharacterSpacing);
     float reqH=Math.Max(1,vLines.Count)*Math.Max(baseLineH,font.Height)+padY;
     if(UsesInkTextAutoFit(shape))
     {
      ComputeInlineTextVerticalMetrics(shape,font,new RectangleF(0,0,availW,shape.Transform.Height),Math.Max(1,vLines.Count),1,out float advance,out _,out _);
      reqH=(Math.Max(1,vLines.Count)-1)*advance+GetAutoFitTextInk(shape,font).Height+(shape.MarginTop??4f)+(shape.MarginBottom??4f);
     }
     if(shape.AutoFitHeight||shape.Transform.Height<reqH)shape.Transform.Height=reqH;
    }
    catch{}
   }
  }
  private static bool UsesInkTextAutoFit(ShapeNode shape)=>shape!=null&&shape.AutoFitHeight&&(shape.Type==ShapeType.TextBox||!string.IsNullOrEmpty(shape.Text)&&(shape.WordArtPresetIndex>=0||((shape.Type==ShapeType.Rectangle||shape.Type==ShapeType.RoundedRectangle)&&(!shape.HasFill||shape.FillOpacity<=0)&&shape.StrokeWidth<=0)));
  private static RectangleF GetAutoFitTextInk(ShapeNode shape,Font font)
  {
   var ink=D2DGraphics.MeasureTextInkBounds(shape.Text,font);
   if(shape.TextRuns!=null&&shape.TextRuns.Count>1)
   {
    float scale=font.Size/Math.Max(1,GetMaxRunFontSize(shape));bool found=false;
    foreach(var run in shape.TextRuns)
    {
     if(string.IsNullOrEmpty(run.Text))continue;
     var style=(run.Bold?FontStyle.Bold:FontStyle.Regular)|(run.Italic?FontStyle.Italic:FontStyle.Regular)|(run.Underline?FontStyle.Underline:FontStyle.Regular)|(run.Strikethrough?FontStyle.Strikeout:FontStyle.Regular);
     using var face=SlideFontManager.CreateFont(run.FontName,Math.Max(1,run.FontSize*scale),style,GraphicsUnit.Pixel);
     var part=D2DGraphics.MeasureTextInkBounds(run.Text,face);ink=found?RectangleF.Union(ink,part):part;found=true;
    }
   }
   if(shape.HasTextOutline){float border=Math.Max(0,shape.TextOutlineWidth)*font.Size/Math.Max(1,GetMaxRunFontSize(shape))*.5f;ink.Inflate(border,border);}
   return ink;
  }
  private static float TextCaretLineHeight(ShapeNode shape,Font font,float advance)=>UsesInkTextAutoFit(shape)?Math.Min(advance,GetAutoFitTextInk(shape,font).Height):advance;
  private void SyncTextBoxRenderHeight(ShapeNode shape,Font font,int lineCount,float lineHeight,float scale,ref RectangleF textBounds)
  {
   if(shape==null||shape.Transform==null||IsResizingShape||freezeMorphTextLayout||(shape.Type!=ShapeType.TextBox&&!UsesInkTextAutoFit(shape)))return;
   float s=Math.Max(0.01f,scale);
   float padY=(shape.MarginTop??4f)+(shape.MarginBottom??4f)+4f;
   float reqCanvasH=Math.Max(1,lineCount)*(lineHeight/s)+padY;
   if(UsesInkTextAutoFit(shape))reqCanvasH=((Math.Max(1,lineCount)-1)*lineHeight+GetAutoFitTextInk(shape,font).Height)/s+(shape.MarginTop??4f)+(shape.MarginBottom??4f);
   if(shape.Transform.Height+0.5f<reqCanvasH)
   {
    shape.Transform.Height=reqCanvasH;
    textBounds.Height=(shape.Transform.Height-(shape.MarginTop??4f)-(shape.MarginBottom??4f))*s;
   }
  }
  public void SetSelectionParagraphAlignment(TextAlignment align)
  {
   ShapeNode targetShape=(IsInlineEditing&&EditingShape!=null)?EditingShape:SelectedShape;
   if(targetShape==null)return;
   SaveUndoState();
   if(targetShape.Type==ShapeType.Table)
   {
    string cellKey=(targetShape.TableSelectedCellRow>=0?targetShape.TableSelectedCellRow:0)+","+(targetShape.TableSelectedCellCol>=0?targetShape.TableSelectedCellCol:0);
    if(targetShape.TableCellAlignments==null)targetShape.TableCellAlignments=new Dictionary<string,TextAlignment>();
    targetShape.TableCellAlignments[cellKey]=align;
   }
   targetShape.Alignment=align;
   if(align==TextAlignment.Justify&&targetShape.Type==ShapeType.TextBox&&targetShape.AutoFitWidth)
   {
    targetShape.AutoFitWidth=false;
   }
   if(targetShape.TextRuns!=null)
   {
    foreach(var r in targetShape.TextRuns)r.ParaAlignment=align;
   }
   targetShape.InvalidateDisplayCache();
   if(ActiveSlide!=null)ActiveSlide.IsThumbnailDirty=true;
   if(InvalidateCallback!=null)InvalidateCallback();
  }
  public bool TryHandleAlignmentShortcut(System.Windows.Forms.Keys key)
  {
   if(key==System.Windows.Forms.Keys.L){SetSelectionParagraphAlignment(TextAlignment.Left);return true;}
   if(key==System.Windows.Forms.Keys.E){SetSelectionParagraphAlignment(TextAlignment.Center);return true;}
   if(key==System.Windows.Forms.Keys.R){SetSelectionParagraphAlignment(TextAlignment.Right);return true;}
   if(key==System.Windows.Forms.Keys.J){SetSelectionParagraphAlignment(TextAlignment.Justify);return true;}
   return false;
  }
  private void GetJustifyLineMetrics(D2DGraphics g,ShapeNode shape,string fullText,List<VisualTextLine> visualLines,List<SlideTextRange> paragraphRanges,int lineIdx,float availWidth,float scale,Font font,out bool shouldJustify,out int trimmedLen,out float extraSpaceW,out float extraCharW)
  {
   shouldJustify=false;trimmedLen=0;extraSpaceW=0f;extraCharW=0f;
   if(visualLines==null||lineIdx<0||lineIdx>=visualLines.Count)return;
   VisualTextLine vl=visualLines[lineIdx];
   string lineText=vl.Text??"";
   string trimmed=lineText.TrimEnd();
   trimmedLen=trimmed.Length;
   if(trimmedLen<=1)return;
   int paragraph=0;
   for(int pi=0;pi<paragraphRanges.Count;pi++)if(vl.StartCharIndex>=paragraphRanges[pi].Start)paragraph=pi;
   int pStart=paragraph<paragraphRanges.Count?paragraphRanges[paragraph].Start:0;
   int pEnd=paragraph<paragraphRanges.Count?paragraphRanges[paragraph].End:fullText.Length;
   int paraLineCount=0;
   int lastLineInPara=lineIdx;
   for(int i=0;i<visualLines.Count;i++)
   {
    if(visualLines[i].StartCharIndex>=pStart&&visualLines[i].StartCharIndex<=pEnd)
    {
     paraLineCount++;
     lastLineInPara=i;
    }
   }
   shouldJustify=(paraLineCount<=1)||(lineIdx<lastLineInPara);
   if(!shouldJustify)return;
   float naturalW=MeasureSegmentsWidth(g,shape,fullText,vl.StartCharIndex,trimmedLen,scale,font);
   float extraW=availWidth-naturalW;
   if(extraW<=0.5f)
   {
    shouldJustify=false;
    return;
   }
   int spaceCount=0;
   for(int i=0;i<trimmedLen;i++)if(trimmed[i]==' ')spaceCount++;
   if(spaceCount>0)
   {
    extraSpaceW=extraW/spaceCount;
   }
   else
   {
    extraCharW=extraW/(trimmedLen-1);
   }
  }
  private float MeasureJustifiedLinePrefixWidth(D2DGraphics g,ShapeNode shape,string fullText,VisualTextLine vl,int colCount,float scale,Font font,bool shouldJustify,int trimmedLen,float extraSpaceW,float extraCharW)
  {
   colCount=Math.Max(0,Math.Min(colCount,vl.Length));
   if(colCount<=0)return 0f;
   float baseW=MeasureSegmentsWidth(g,shape,fullText,vl.StartCharIndex,colCount,scale,font);
   if(!shouldJustify||(extraSpaceW<=0f&&extraCharW<=0f))return baseW;
   string lineText=vl.Text??"";
   int effCols=Math.Min(colCount,Math.Min(trimmedLen,lineText.Length));
   if(extraSpaceW>0f)
   {
    int spaces=0;
    for(int i=0;i<effCols;i++)if(lineText[i]==' ')spaces++;
    return baseW+spaces*extraSpaceW;
   }
   if(extraCharW>0f&&effCols>1)
   {
    return baseW+(effCols-1)*extraCharW;
   }
   return baseW;
  }
  private void DrawStyledSegmentToken(D2DGraphics g,ShapeNode shape,VisualLineRunSegment seg,string tokenText,float x,float y,float tokenW,float lineHeight,float scale,float opacityMultiplier)
  {
   if(string.IsNullOrEmpty(tokenText))return;
   if(shape.HasTextShadow)
   {
    Color sc=(shape.ShadowColor!=0)
     ?Color.FromArgb((int)(110*opacityMultiplier),Color.FromArgb(shape.ShadowColor&0xFFFFFF))
     :Color.FromArgb((int)(110*opacityMultiplier),80,80,80);
    using var shadowBr=new SolidBrush(sc);
    g.DrawString(tokenText,seg.Font,shadowBr,x+1.2f*scale,y+1.2f*scale,StringFormat.GenericTypographic,seg.CharacterSpacing);
   }
   if(shape.HasTextOutline&&shape.TextOutlineWidth>0f)
   {
    using var tokPath=new GraphicsPath();
    float emSize=seg.Font.Unit==GraphicsUnit.Pixel?seg.Font.Size:seg.Font.SizeInPoints*96f/72f;
    tokPath.AddString(tokenText,seg.Font.FontFamily,(int)seg.Font.Style,emSize,new PointF(x,y),StringFormat.GenericTypographic);
    Color outCol=Color.FromArgb((int)(255*opacityMultiplier),Color.FromArgb(shape.TextOutlineColor&0xFFFFFF));
    using var outPen=new Pen(outCol,Math.Max(1.0f,shape.TextOutlineWidth*scale)){LineJoin=LineJoin.Round};
    g.DrawPath(outPen,tokPath);
    using var fillBr=new SolidBrush(seg.Color);
    g.FillPath(fillBr,tokPath);
   }
   else if(seg.HasGradient&&seg.GradientStops!=null&&seg.GradientStops.Count>=2)
   {
    RectangleF segR=new RectangleF(x,y,Math.Max(1f,tokenW),lineHeight);
    using var lgb=new LinearGradientBrush(segR,seg.Color,seg.GradientColor2,seg.GradientAngle);
    ColorBlend cb=new ColorBlend(seg.GradientStops.Count);
    for(int k=0;k<seg.GradientStops.Count;k++)
    {
     cb.Colors[k]=Color.FromArgb((int)(255 * Math.Clamp(1f-seg.GradientStops[k].Transparency,0f,1f) * opacityMultiplier), Color.FromArgb(seg.GradientStops[k].Color & 0xFFFFFF));
     cb.Positions[k]=seg.GradientStops[k].Position;
    }
    lgb.InterpolationColors=cb;
    g.DrawString(tokenText,seg.Font,lgb,x,y,StringFormat.GenericTypographic,seg.CharacterSpacing);
   }
   else
   {
    using var txtB=new SolidBrush(seg.Color);
    g.DrawString(tokenText,seg.Font,txtB,x,y,StringFormat.GenericTypographic,seg.CharacterSpacing);
   }
  }
  private bool TryDrawJustifiedShapeText(D2DGraphics g,ShapeNode shape,string dispText,Font font,Color txtCol,RectangleF textBounds,float scale,float opacityMultiplier,int textAlpha,bool isEditingThis,TextAlignment effAlign,List<VisualTextLine> visualLines,float lineHeight,float drawStartY,float boxStartY)
  {
   if(effAlign!=TextAlignment.Justify)return false;
   RectangleF originalTextBounds=textBounds;
   var paragraphRanges=SlideTextParagraphs.Ranges(dispText);
   if(isEditingThis&&TextSelectionLength!=0)
   {
    int selStart=Math.Min(TextSelectionStart,TextSelectionStart+TextSelectionLength);
    int selEnd=Math.Max(TextSelectionStart,TextSelectionStart+TextSelectionLength);
    using var selBrush=new SolidBrush(Color.FromArgb(100,59,130,246));
    for(int li=0;li<visualLines.Count;li++)
    {
     VisualTextLine vl=visualLines[li];
     textBounds=ParagraphTextBounds(shape,dispText,vl.StartCharIndex,originalTextBounds,scale);
     int lineStart=vl.StartCharIndex;
     int lineEnd=vl.StartCharIndex+vl.Length;
     if(selEnd>lineStart&&selStart<lineEnd)
     {
      GetJustifyLineMetrics(g,shape,dispText,visualLines,paragraphRanges,li,textBounds.Width,scale,font,out bool shouldJustify,out int trimmedLen,out float extraSpaceW,out float extraCharW);
      int subStart=Math.Max(0,selStart-lineStart);
      int subEnd=Math.Min(vl.Length,selEnd-lineStart);
      float preW=MeasureJustifiedLinePrefixWidth(g,shape,dispText,vl,subStart,scale,font,shouldJustify,trimmedLen,extraSpaceW,extraCharW);
      float endW=MeasureJustifiedLinePrefixWidth(g,shape,dispText,vl,subEnd,scale,font,shouldJustify,trimmedLen,extraSpaceW,extraCharW);
      float curY=boxStartY+li*lineHeight;
      g.FillRectangle(selBrush,textBounds.X+preW,curY,Math.Max(2f,endW-preW),TextCaretLineHeight(shape,font,lineHeight));
     }
    }
   }
   textBounds=originalTextBounds;
   if(!string.IsNullOrEmpty(dispText)&&textAlpha>0)
   {
    for(int li=0;li<visualLines.Count;li++)
    {
     VisualTextLine vl=visualLines[li];
     if(string.IsNullOrEmpty(vl.Text))continue;
     if(shape.RenderTextStart>=0&&(vl.StartCharIndex+vl.Length<=shape.RenderTextStart||vl.StartCharIndex>=shape.RenderTextEnd))continue;
     int paragraph=0;
     for(int pi=0;pi<paragraphRanges.Count;pi++)if(vl.StartCharIndex>=paragraphRanges[pi].Start)paragraph=pi;
     var paraStyle=SlideTextParagraphs.Style(shape,paragraph);
     textBounds=originalTextBounds;
     textBounds.X+=(paraStyle?.LeftMargin??0)*scale;
     textBounds.Width=Math.Max(1,textBounds.Width-((paraStyle?.LeftMargin??0)+(paraStyle?.RightMargin??0))*scale);
     float lineY=drawStartY+li*lineHeight;
     if(vl.StartCharIndex==paragraphRanges[paragraph].Start&&(shape.RenderTextStart<0||shape.RenderTextStart<=vl.StartCharIndex&&shape.RenderTextEnd>vl.StartCharIndex))
      DrawParagraphBullet(g,shape,paraStyle,paragraph,font,originalTextBounds,lineY,lineHeight,scale,txtCol);
     GetJustifyLineMetrics(g,shape,dispText,visualLines,paragraphRanges,li,textBounds.Width,scale,font,out bool shouldJustify,out int trimmedLen,out float extraSpaceW,out float extraCharW);
     var segments=FilterTextAnimationSegments(g,shape,GetVisualLineRunSegments(g,shape,dispText,vl.StartCharIndex,vl.Length,scale,font,txtCol),vl.StartCharIndex);
     float curX=textBounds.X;
     float curY=drawStartY+li*lineHeight;
     int charPosInLine=0;
     foreach(var seg in segments)
     {
      string sText=seg.Text??"";
      if(sText.Length==0)continue;
      if(!shouldJustify||(extraSpaceW<=0f&&extraCharW<=0f))
      {
       if(seg.HasHighlight&&(seg.HighlightColor.ToArgb()&0xFFFFFF)!=0)
       {
        using var hlB=new SolidBrush(seg.HighlightColor);
        g.FillRectangle(hlB,curX,curY,seg.Width,lineHeight);
       }
       DrawStyledSegmentToken(g,shape,seg,sText,curX,curY,seg.Width,lineHeight,scale,opacityMultiplier);
       curX+=seg.Width;
       charPosInLine+=sText.Length;
      }
      else if(extraSpaceW>0f)
      {
       int idx=0;
       while(idx<sText.Length)
       {
        if(sText[idx]==' ')
        {
         float spW=g.MeasureString(" ",seg.Font,seg.CharacterSpacing).Width+((charPosInLine<trimmedLen)?extraSpaceW:0f);
         if(seg.HasHighlight&&(seg.HighlightColor.ToArgb()&0xFFFFFF)!=0)
         {
          using var hlB=new SolidBrush(seg.HighlightColor);
          g.FillRectangle(hlB,curX,curY,spW,lineHeight);
         }
         curX+=spW;
         idx++;
         charPosInLine++;
        }
        else
        {
         int wStart=idx;
         while(idx<sText.Length&&sText[idx]!=' ')idx++;
         string word=sText.Substring(wStart,idx-wStart);
         float wWidth=g.MeasureString(word,seg.Font,seg.CharacterSpacing).Width;
         if(seg.HasHighlight&&(seg.HighlightColor.ToArgb()&0xFFFFFF)!=0)
         {
          using var hlB=new SolidBrush(seg.HighlightColor);
          g.FillRectangle(hlB,curX,curY,wWidth,lineHeight);
         }
         DrawStyledSegmentToken(g,shape,seg,word,curX,curY,wWidth,lineHeight,scale,opacityMultiplier);
         curX+=wWidth;
         charPosInLine+=word.Length;
        }
       }
      }
      else
      {
       for(int ci=0;ci<sText.Length;ci++)
       {
        string chStr=sText[ci].ToString();
        float chW=g.MeasureString(chStr,seg.Font,seg.CharacterSpacing).Width;
        float adv=chW+((charPosInLine<trimmedLen-1)?extraCharW:0f);
        if(seg.HasHighlight&&(seg.HighlightColor.ToArgb()&0xFFFFFF)!=0)
        {
         using var hlB=new SolidBrush(seg.HighlightColor);
         g.FillRectangle(hlB,curX,curY,adv,lineHeight);
        }
        DrawStyledSegmentToken(g,shape,seg,chStr,curX,curY,chW,lineHeight,scale,opacityMultiplier);
        curX+=adv;
        charPosInLine++;
       }
      }
     }
     foreach(var seg in segments)
     {
      if(seg.Font!=null&&seg.Font!=font)
      {
       try{seg.Font.Dispose();}catch{}
      }
     }
    }
   }
   if(isEditingThis)
   {
    string fullText=EditingTextBuffer??"";
    int caret=Math.Max(0,Math.Min(CaretIndex,fullText.Length));
    int curLineIdx=Math.Max(0,visualLines.Count-1);
    int colInLine=visualLines.Count>0?visualLines[curLineIdx].Length:0;
    for(int i=0;i<visualLines.Count;i++)
    {
     if(caret<=visualLines[i].StartCharIndex+visualLines[i].Length)
     {
      curLineIdx=i;
      colInLine=caret-visualLines[i].StartCharIndex;
      break;
     }
    }
    VisualTextLine cvl=(curLineIdx<visualLines.Count)?visualLines[curLineIdx]:new VisualTextLine{Text="",StartCharIndex=0,Length=0};
    textBounds=ParagraphTextBounds(shape,dispText,cvl.StartCharIndex,originalTextBounds,scale);
    GetJustifyLineMetrics(g,shape,dispText,visualLines,paragraphRanges,curLineIdx,textBounds.Width,scale,font,out bool shouldJustify,out int trimmedLen,out float extraSpaceW,out float extraCharW);
    int colLen=Math.Min(colInLine,cvl.Length);
    float beforeWidth=MeasureJustifiedLinePrefixWidth(g,shape,dispText,cvl,colLen,scale,font,shouldJustify,trimmedLen,extraSpaceW,extraCharW);
    float caretX=textBounds.X+beforeWidth;
    float caretY=boxStartY+curLineIdx*lineHeight;
    CurrentCaretScreenX=(int)caretX;
    float caretHeight=TextCaretLineHeight(shape,font,lineHeight);
    CurrentCaretScreenY=(int)(caretY+caretHeight+2);
    CurrentCaretScreenH=(int)caretHeight;
    if(CursorVisible)
    {
     Color caretCol=Color.FromArgb(38,38,38);
     using var caretPen=new Pen(caretCol,Math.Max(1.2f,1.8f*scale));
     g.DrawLine(caretPen,caretX,caretY+1.5f*scale,caretX,caretY+caretHeight-1.5f*scale);
    }
   }
   return true;
  }
  private int GetJustifiedTextCharIndexFromPoint(D2DGraphics g,ShapeNode shape,string text,Font font,List<VisualTextLine> visualLines,int lineIdx,VisualTextLine vLine,RectangleF textBounds,float relX,float scale)
  {
   var paragraphRanges=SlideTextParagraphs.Ranges(text);
   GetJustifyLineMetrics(g,shape,text,visualLines,paragraphRanges,lineIdx,textBounds.Width,scale,font,out bool shouldJustify,out int trimmedLen,out float extraSpaceW,out float extraCharW);
   float clickRelToLineX=relX-textBounds.X;
   if(clickRelToLineX<=0f)return vLine.StartCharIndex;
   string lineText=vLine.Text??"";
   int bestCol=0;
   float bestDist=float.MaxValue;
   for(int c=0;c<=lineText.Length;c++)
   {
    float subW=MeasureJustifiedLinePrefixWidth(g,shape,text,vLine,c,scale,font,shouldJustify,trimmedLen,extraSpaceW,extraCharW);
    float dist=Math.Abs(subW-clickRelToLineX);
    if(dist<bestDist)
    {
     bestDist=dist;
     bestCol=c;
    }
   }
   return vLine.StartCharIndex+bestCol;
  }
 }
}
