using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ZenQOffice.UI
{
 public partial class WriterView
 {
  public readonly struct EquationHit
  {
   public readonly int Index;public readonly char Token;public readonly RectangleF Bounds;
   public EquationHit(int index,char token,RectangleF bounds){Index=index;Token=token;Bounds=bounds;}
  }
  readonly List<EquationHit> equationHits=new();
  bool equationPointerDown;
  ContextMenuStrip equationMenu;
  Control equationMenuOwner;
  bool showingEquationMenu;
  static readonly Font EquationMenuFont=new Font("Segoe UI",8.5f,FontStyle.Regular,GraphicsUnit.Point);
  public IReadOnlyList<EquationHit> EquationHits=>equationHits;
  public RectangleF EquationFrameBounds{get;private set;}
  public RectangleF EquationMenuButtonBounds{get;private set;}
  WriterMath.CaptureScope CaptureEquationDrawing(D2DGraphics g)
  {
   equationHits.Clear();EquationFrameBounds=EquationMenuButtonBounds=RectangleF.Empty;
   return new WriterMath.CaptureScope(g,(index,token,bounds)=>
   {
    if(bounds.Bottom<155||bounds.Top>LastViewHeight-26)return;
    var cell=TableCellHits.LastOrDefault(c=>index>=c.CharStartIndex&&index<c.CharStartIndex+c.CharLength);
    if(cell.CharLength>0)bounds=RectangleF.Intersect(bounds,cell.CellRect);
    if(bounds.Width>0&&bounds.Height>0)equationHits.Add(new EquationHit(index,token,bounds));
   });
  }
  int SelectedEquationIndex()
  {
   int start=Math.Min(SelectionStart,SelectionEnd);string text=PieceTable.GetText();
   return Math.Abs(SelectionEnd-SelectionStart)==1&&start>=0&&start<text.Length&&WriterMath.Contains(text[start].ToString())?start:-1;
  }
  void DrawEquationFrame(D2DGraphics g)
  {
   int index=SelectedEquationIndex();if(index<0)return;
   var hit=equationHits.LastOrDefault(h=>h.Index==index);if(hit.Bounds.IsEmpty)return;
   float scale=Math.Max(.7f,ZoomLevel),pad=2*scale,grip=8*scale,button=14*scale;
   var content=hit.Bounds;content.Inflate(pad,pad);
   EquationFrameBounds=new RectangleF(content.X-grip,content.Y,content.Width+grip+button,content.Height);
   EquationMenuButtonBounds=new RectangleF(content.Right,content.Y,button,content.Height);
   using var border=new Pen(Color.FromArgb(139,145,153),1);
   using var chrome=new SolidBrush(Color.FromArgb(239,241,244));
   g.FillRectangle(chrome,EquationFrameBounds.X,content.Y,grip,content.Height);
   g.FillRectangle(chrome,EquationMenuButtonBounds);g.DrawRectangle(border,EquationFrameBounds.X,EquationFrameBounds.Y,EquationFrameBounds.Width,EquationFrameBounds.Height);
   g.DrawLine(border,content.X,content.Top,content.X,content.Bottom);
   g.DrawLine(border,content.Right,content.Top,content.Right,content.Bottom);
   using var ink=new SolidBrush(Color.FromArgb(85,91,100));
   float mid=content.Top+content.Height/2;
   for(int row=-1;row<=1;row++)for(int col=0;col<2;col++)g.FillRectangle(ink,EquationFrameBounds.Left+(2+col*3)*scale,mid+row*3*scale,scale,scale);
   float cx=EquationMenuButtonBounds.Left+button/2;
   g.FillPolygon(ink,new[]{new PointF(cx-3*scale,mid-scale),new PointF(cx+3*scale,mid-scale),new PointF(cx,mid+2*scale)});
  }
  bool SelectEquationAt(int x,int y,RibbonBar ribbon)
  {
   if(y<155||DocumentShapes.Any(s=>!s.LastBounds.IsEmpty&&s.LastBounds.Contains(x,y)))return false;
   string text=PieceTable.GetText();
   for(int i=equationHits.Count-1;i>=0;i--)
   {
    var hit=equationHits[i];var target=hit.Bounds;target.Inflate(2,2);
    if(!target.Contains(x,y)||hit.Index<0||hit.Index>=text.Length||text[hit.Index]!=hit.Token)continue;
    SelectionStart=hit.Index;SelectionEnd=hit.Index+1;CursorPosition=SelectionEnd;
    ActiveShape=null;ActiveChart=null;IsSelecting=false;IsEditingEquation=true;
    equationPointerDown=true;_hasDraggedSelection=true;
    if(ribbon!=null)UpdateRibbonState(ribbon);
    WriterFloatingMiniToolbarForm.CloseCurrent();Invalidate();return true;
   }
   return false;
  }
  bool HandleEquationClick(int x,int y,RibbonBar ribbon)
  {
   equationPointerDown=false;
   if(SelectedEquationIndex()>=0&&EquationFrameBounds.Contains(x,y))
   {
    equationPointerDown=true;IsSelecting=false;_hasDraggedSelection=true;
    if(EquationMenuButtonBounds.Contains(x,y))ShowEquationMenu(Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Visible),new Point(x,y),ribbon);
    return true;
   }
   if(SelectEquationAt(x,y,ribbon))return true;
   if(y>=155)IsEditingEquation=false;
   return false;
  }
  bool CompleteEquationClick()
  {
   if(!equationPointerDown)return false;
   equationPointerDown=false;
   if(SelectedEquationIndex()<0)return false;
   IsSelecting=false;_hasDraggedSelection=true;
   WriterFloatingMiniToolbarForm.CloseCurrent();Invalidate();return true;
  }
  public void SetEquationLinear(bool linear,bool all=false)
  {
   int selected=SelectedEquationIndex();if(!all&&selected<0)return;
   string text=PieceTable.GetText();var replacements=new Dictionary<char,char>();
   for(int i=0;i<text.Length;i++)if((all||i==selected)&&WriterMath.Contains(text[i].ToString())&&!replacements.ContainsKey(text[i]))replacements[text[i]]=WriterMath.WithLinear(text[i],linear);
   if(replacements.Count==0||replacements.All(p=>p.Key==p.Value))return;
   SaveUndoState();
   string Change(string value)=>value==null?null:string.Concat(value.Select(c=>replacements.TryGetValue(c,out char replacement)?replacement:c));
   for(int i=text.Length-1;i>=0;i--)if((all||i==selected)&&replacements.TryGetValue(text[i],out char replacement)&&replacement!=text[i]){PieceTable.Remove(i,1);PieceTable.Insert(i,replacement.ToString());}
   for(int i=0;i<equationHits.Count;i++){var h=equationHits[i];if((all||h.Index==selected)&&replacements.TryGetValue(h.Token,out char rep))equationHits[i]=new EquationHit(h.Index,rep,h.Bounds);}
   var selectedCell=TableCellHits.LastOrDefault(c=>selected>=c.CharStartIndex&&selected<c.CharStartIndex+c.CharLength);
   // Update table metadata before a save without an intervening paint. A single
   // equation changes only its own occurrence, even when other cells reuse it.
   foreach(var table in DocumentTables)foreach(var row in table.Rows)foreach(var cell in row.Cells)
   {
    if(all){cell.Text=Change(cell.Text);foreach(var paragraph in cell.Paragraphs??Enumerable.Empty<DocCellParagraph>())paragraph.Text=Change(paragraph.Text);}
   }
   if(!all&&selectedCell.CharLength>0&&selectedCell.TableIndex<DocumentTables.Count)
   {
    var cell=DocumentTables[selectedCell.TableIndex].Rows[selectedCell.RowIndex].Cells[selectedCell.ColIndex];
    int local=selected-selectedCell.CharStartIndex;
    string ChangeAt(string value,int at)=>value!=null&&at>=0&&at<value.Length&&replacements.TryGetValue(value[at],out char replacement)?value.Substring(0,at)+replacement+value.Substring(at+1):value;
    cell.Text=ChangeAt(cell.Text,local);
    foreach(var paragraph in cell.Paragraphs??Enumerable.Empty<DocCellParagraph>()){if(local<(paragraph.Text??"").Length){paragraph.Text=ChangeAt(paragraph.Text,local);break;}local-=(paragraph.Text??"").Length+1;}
   }
   InvalidateLayoutAndTextCache();Invalidate();
  }
  public void SetEquationColorInRange(int start,int length,Color color)
  {
   if(length<=0)return;
   string text=PieceTable.GetText();int end=Math.Min(text.Length,start+length);
   var changedIndices=new List<(int Index,char OldToken,char NewToken)>();
   for(int i=Math.Max(0,start);i<end;i++)
   {
    if(WriterMath.Contains(text[i].ToString()))
    {
     char rep=WriterMath.WithColor(text[i],color);
     if(rep!=text[i])changedIndices.Add((i,text[i],rep));
    }
   }
   if(changedIndices.Count==0)return;
   for(int k=changedIndices.Count-1;k>=0;k--)
   {
    var item=changedIndices[k];
    PieceTable.Remove(item.Index,1);
    PieceTable.Insert(item.Index,item.NewToken.ToString());
   }
   for(int i=0;i<equationHits.Count;i++)
   {
    var h=equationHits[i];
    int matchIdx=changedIndices.FindIndex(c=>c.Index==h.Index);
    if(matchIdx>=0)equationHits[i]=new EquationHit(h.Index,changedIndices[matchIdx].NewToken,h.Bounds);
   }
   foreach(var item in changedIndices)
   {
    var selectedCell=TableCellHits.LastOrDefault(c=>item.Index>=c.CharStartIndex&&item.Index<c.CharStartIndex+c.CharLength);
    if(selectedCell.CharLength>0&&selectedCell.TableIndex<DocumentTables.Count)
    {
     var cell=DocumentTables[selectedCell.TableIndex].Rows[selectedCell.RowIndex].Cells[selectedCell.ColIndex];
     int local=item.Index-selectedCell.CharStartIndex;
     string ChangeAt(string value,int at)=>value!=null&&at>=0&&at<value.Length&&value[at]==item.OldToken?value.Substring(0,at)+item.NewToken+value.Substring(at+1):value;
     cell.Text=ChangeAt(cell.Text,local);
     foreach(var paragraph in cell.Paragraphs??Enumerable.Empty<DocCellParagraph>()){if(local<(paragraph.Text??"").Length){paragraph.Text=ChangeAt(paragraph.Text,local);break;}local-=(paragraph.Text??"").Length+1;}
    }
   }
  }
  public ContextMenuStrip BuildEquationMenu(Control owner=null,RibbonBar ribbon=null)
  {
   var menu=new FluentContextMenuStrip{Font=EquationMenuFont,CornerRadius=3,HoverCornerRadius=2,ItemPadding=new Padding(2,1,2,1),Padding=new Padding(2),ShowImageMargin=false,ShowCheckMargin=true};int index=SelectedEquationIndex();
   bool linear=index>=0&&WriterMath.IsLinear(PieceTable.GetText()[index]);
   menu.Items.Add("Nhập / Sửa công thức…",null,(s,e)=>ShowEquationEditor(owner));
   menu.Items.Add(new ToolStripSeparator());
   menu.Items.Add(new ToolStripMenuItem("Professional",null,(s,e)=>SetEquationLinear(false)){Checked=!linear});
   menu.Items.Add(new ToolStripMenuItem("Linear (LaTeX)",null,(s,e)=>SetEquationLinear(true)){Checked=linear});
   menu.Items.Add(new ToolStripSeparator());
   menu.Items.Add("Tất cả: Professional",null,(s,e)=>SetEquationLinear(false,true));
   menu.Items.Add("Tất cả: Linear",null,(s,e)=>SetEquationLinear(true,true));
   menu.Items.Add(new ToolStripSeparator());
   menu.Items.Add("Sao chép công thức",null,(s,e)=>{if(SelectedEquationIndex()>=0)Copy();});
   menu.Opening+=(s,e)=>
   {
    int selected=SelectedEquationIndex();bool isLinear=selected>=0&&WriterMath.IsLinear(PieceTable.GetText()[selected]);
    ((ToolStripMenuItem)menu.Items[2]).Checked=!isLinear;
    ((ToolStripMenuItem)menu.Items[3]).Checked=isLinear;
   };
   return menu;
  }
  void ShowEquationMenu(Control owner,Point point,RibbonBar ribbon)
  {
   if(showingEquationMenu)return;
   if(equationMenu==null||equationMenu.IsDisposed||equationMenuOwner!=owner)
   {
    equationMenu?.Dispose();equationMenu=BuildEquationMenu(owner,ribbon);equationMenuOwner=owner;
    var ownedMenu=equationMenu;
    if(owner!=null)owner.Disposed+=(s,e)=>ownedMenu.Dispose();
   }
   if(equationMenu.Visible)return;
   // Closed can run synchronously inside ToolStripDropDown.Show while WinForms is
   // still installing its owner window. Keep the menu alive until its owner ends.
   showingEquationMenu=true;
   try{if(owner!=null)equationMenu.Show(owner,point);else equationMenu.Show(Cursor.Position);}
   finally{showingEquationMenu=false;}
  }
  public void ShowEquationEditor(Control owner=null)
  {
   string selected=GetSelectedText();
   using var dialog=new Form{Text="Nhập / Sửa công thức",ClientSize=new Size(580,190),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.White,Font=new Font("Segoe UI",9)};
   var label=new Label{Text=@"Nhập công thức / LaTeX: \frac{a}{b}, \vec{AB}, x^2, \sqrt{x}",AutoSize=true,Location=new Point(16,16)};
   var input=new TextBox{Text=WriterMath.Contains(selected)?WriterMath.ToLinear(selected):@"\frac{a}{b}",Multiline=true,ScrollBars=ScrollBars.Vertical,Location=new Point(16,44),Size=new Size(548,80)};
   var ok=new Button{Text="Áp dụng",DialogResult=DialogResult.OK,Location=new Point(358,145),Size=new Size(100,30)};
   var cancel=new Button{Text="Hủy",DialogResult=DialogResult.Cancel,Location=new Point(468,145),Size=new Size(96,30)};
   dialog.Controls.AddRange(new Control[]{label,input,ok,cancel});dialog.AcceptButton=ok;dialog.CancelButton=cancel;
   if(dialog.ShowDialog(owner)==DialogResult.OK&&!string.IsNullOrWhiteSpace(input.Text)&&(!WriterMath.Contains(selected)||input.Text.Trim()!=WriterMath.ToLinear(selected).Trim())){InsertEquation(input.Text.Trim());InvalidateLayoutAndTextCache();Invalidate();}
  }
 }
}
