using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ZenQOffice.UI;
class WriterEquationInteractionTests
{
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 [STAThread]static int Main()
 {
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();
  try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}
 }
 static void Run()
 {
  Directory.CreateDirectory("output/writer-equation-interaction");
  var ribbon=new RibbonBar{CurrentMode=OfficeAppMode.Writer};var view=new WriterView{ViewMode=1,ZoomLevel=1};
  Check(view.LoadDocumentFile("tests/fixtures/writer-math-exam.docx",ribbon),"Load exam");
  using var bitmap=new Bitmap(1200,1500);using var graphics=Graphics.FromImage(bitmap);
  void Draw()=>view.Draw(graphics,1200,1500,ribbon);
  Draw();Draw();var equation=view.EquationHits.First();
  ribbon.ActiveRibbonTab=0;
  int x=(int)(equation.Bounds.Left+equation.Bounds.Width/2),y=(int)(equation.Bounds.Top+equation.Bounds.Height/2);
  view.HandleMouseDown(x,y,1200,graphics,ribbon);view.HandleMouseMove(x,y,1200,graphics,ribbon);view.HandleMouseUp(x,y,1200,graphics,ribbon);Draw();
  Check(view.SelectionStart==equation.Index&&view.SelectionEnd==equation.Index+1&&!view.EquationFrameBounds.IsEmpty,"Frame remains selected after a complete mouse press and release");
  Check(ribbon.ActiveRibbonTab==0&&ribbon.RibbonTabs.Contains("Công thức"),"Clicking an equation keeps the user's active Ribbon tab instead of jumping to the Equation tab");
  // Simulate Ribbon click and popup close MouseUp while equation is selected
  ribbon.HandleClick(460,108,view);
  view.HandleMouseUp(460,200,1200,graphics,ribbon);Draw();
  Check(view.SelectionStart==equation.Index&&view.SelectionEnd==equation.Index+1&&!view.EquationFrameBounds.IsEmpty,"Ribbon interaction and popup MouseUp preserve equation selection");
  Check(WriterMath.GetColor(view.PieceTable.GetText()[equation.Index])?.ToArgb()==Color.FromArgb(220,38,38).ToArgb(),"Font color button on Ribbon colors the selected equation");
  // Verify equation inside table cell (Option D) colors both vector arrows and letters without coloring 'D.'
  var cellD=view.TableCellHits.First(c=>c.TableIndex==0&&c.RowIndex==1&&c.ColIndex==1);
  var eqD=view.EquationHits.First(h=>h.Index>=cellD.CharStartIndex&&h.Index<cellD.CharStartIndex+cellD.CharLength);
  int dx=(int)(eqD.Bounds.Left+eqD.Bounds.Width/2),dy=(int)(eqD.Bounds.Top+eqD.Bounds.Height/2);
  ribbon.ActiveRibbonTab=1;
  view.HandleMouseDown(dx,dy,1200,graphics,ribbon);view.HandleMouseUp(dx,dy,1200,graphics,ribbon);Draw();
  Check(ribbon.ActiveRibbonTab==1,"Clicking an equation inside a table cell also preserves the user's active Ribbon tab");
  Color targetRed=Color.FromArgb(220,38,38);
  view.ApplyFontColorToSelectionOrTyping(targetRed);Draw();
  Check(WriterMath.GetColor(view.PieceTable.GetText()[eqD.Index])?.ToArgb()==targetRed.ToArgb(),"Table cell equation receives the chosen font color");
  Check(view.DocumentTables[0].Rows[1].Cells[1].TextColor.ToArgb()!=targetRed.ToArgb(),"Coloring only the equation inside a table cell does not turn the surrounding cell prefix 'D.' red");
  int redTop=0,redBottom=0;
  int midY=(int)(eqD.Bounds.Top+eqD.Bounds.Height*0.38f);
  for(int py=(int)eqD.Bounds.Top;py<(int)eqD.Bounds.Bottom;py++)
   for(int px=(int)eqD.Bounds.Left;px<(int)eqD.Bounds.Right;px++)
   {
    var c=bitmap.GetPixel(px,py);
    if(c.R>160&&c.R-c.G>60&&c.R-c.B>60){if(py<midY)redTop++;else redBottom++;}
   }
  Check(redTop>0&&redBottom>0,$"Both vector arrows ({redTop}) and equation text letters ({redBottom}) are painted in red");
  // Verify text block selection ('quét khối') is preserved across Ribbon clicks and stray MouseUp
  var plainLine=view.RenderedLines.First(l=>l.StartCharIndex==0);
  view.HandleTripleClick(plainLine.ScreenX+10,plainLine.ScreenY+plainLine.ScreenH/2,1200,graphics,ribbon);
  int selA=view.SelectionStart,selB=view.SelectionEnd;
  Check(selB>selA,"Triple click selects paragraph block");
  ribbon.HandleClick(272,108,view);
  view.HandleMouseUp(272,220,1200,graphics,ribbon);
  Check(view.SelectionStart==selA&&view.SelectionEnd==selB,"Ribbon action and popup release do not clear text block selection");
  // Reselect first equation for the rest of the interaction checks
  ribbon.ActiveRibbonTab=0;
  view.HandleMouseDown(x,y,1200,graphics,ribbon);view.HandleMouseUp(x,y,1200,graphics,ribbon);Draw();
  var grip=view.EquationFrameBounds;int gripX=(int)grip.Left+2;
  view.HandleMouseDown(gripX,y,1200,graphics,ribbon);view.HandleMouseUp(gripX,y,1200,graphics,ribbon);Draw();
  Check(view.SelectionStart==equation.Index&&view.SelectionEnd==equation.Index+1,"Clicking the frame grip preserves the equation selection");
  view.HandleMouseMove(x+300,y+100,1200,graphics,ribbon);Draw();Check(!view.EquationFrameBounds.IsEmpty,"Moving away after release does not clear the frame");
  bitmap.Save("output/writer-equation-interaction/frame-after-release.png");
  using var host=new Form{ClientSize=new Size(1200,700),ShowInTaskbar=false,Opacity=0};host.Show();
  var button=view.EquationMenuButtonBounds;int bx=(int)(button.Left+button.Width/2),by=(int)(button.Top+button.Height/2);
  view.HandleMouseDown(bx,by,1200,graphics,ribbon);
  var menu=(FluentContextMenuStrip)typeof(WriterView).GetField("equationMenu",Private).GetValue(view);
  Check(menu!=null&&menu.Visible,"The actual frame arrow opens the equation menu");
  Check(menu.Font.Name=="Segoe UI"&&!menu.Font.Bold&&menu.Font.SizeInPoints<=8.6,"Equation menu uses the compact regular font");
  Check(menu.CornerRadius==3&&menu.HoverCornerRadius==2,"Equation popup uses restrained corners");
  float scale=menu.DeviceDpi/96f;
  Check(menu.Width<=230*scale&&menu.Height<=210*scale,"Equation menu fits a compact popup");
  foreach(var item in menu.Items.OfType<ToolStripMenuItem>())Check(item.Bounds.Height<=24*scale,"Command rows have compact spacing");
  using(var shot=new Bitmap(menu.Width,menu.Height)){menu.DrawToBitmap(shot,menu.ClientRectangle);shot.Save("output/writer-equation-interaction/equation-menu.png");}
  menu.Opacity=0;view.HandleMouseUp(bx,by,1200,graphics,ribbon);menu.Close();Application.DoEvents();Draw();
  Check(!menu.IsDisposed&&!view.EquationFrameBounds.IsEmpty,"Closing the popup preserves its window and equation selection");
  var show=typeof(WriterView).GetMethod("ShowEquationMenu",Private);
  void Open()=>show.Invoke(view,new object[]{host,new Point(20,20),ribbon});
  // Reproduces closing inside Show, before WinForms finishes assigning its owner.
  // Disposing synchronously in Closed used to invalidate the handle at that point.
  EventHandler closeInsideOpen=(s,e)=>menu.Close(ToolStripDropDownCloseReason.AppClicked);
  menu.Opened+=closeInsideOpen;Open();menu.Opened-=closeInsideOpen;
  Check(!menu.IsDisposed,"Synchronous close during Show does not destroy the native menu");
  for(int trial=0;trial<20;trial++){Open();Application.DoEvents();menu.Close();Application.DoEvents();Check(!menu.IsDisposed,"Reopening reuses a live popup");}
  Open();((ToolStripMenuItem)menu.Items.Cast<ToolStripItem>().First(i=>i.Text=="Linear (LaTeX)")).PerformClick();menu.Close();Application.DoEvents();
  Check(WriterMath.IsLinear(view.PieceTable.GetText()[equation.Index]),"A command still acts on the selected equation after repeated opens");
  Open();Check(((ToolStripMenuItem)menu.Items[3]).Checked&&!((ToolStripMenuItem)menu.Items[2]).Checked,"Reused menu refreshes its checked state");menu.Close();
  view.SetEquationLinear(false);Draw();var plain=view.RenderedLines.First(l=>l.StartCharIndex==0);
  view.HandleMouseDown(plain.ScreenX+2,plain.ScreenY+plain.ScreenH/2,1200,graphics,ribbon);view.HandleMouseUp(plain.ScreenX+2,plain.ScreenY+plain.ScreenH/2,1200,graphics,ribbon);Draw();
  Check(view.EquationFrameBounds.IsEmpty,"A normal document click releases the equation frame");
  // Verify borderless tables (0..11) remain borderless and grid tables (12..13) keep borders after Undo/Redo and DOCX save/reload
  view.Undo();view.Redo();
  Check(view.DocumentTables.Take(12).All(t=>!t.HasBorderTop&&!t.HasBorderBottom&&!t.HasBorderLeft&&!t.HasBorderRight&&!t.HasBorderInsideH&&!t.HasBorderInsideV),"Undo/Redo preserves borderless formatting tables");
  Check(view.DocumentTables.Skip(12).All(t=>t.HasBorderTop&&t.HasBorderBottom&&t.HasBorderLeft&&t.HasBorderRight),"Undo/Redo preserves bordered TableGrid tables");
  string savedBorderless="output/writer-equation-interaction/borderless-roundtrip.docx";
  Check(view.SaveDocumentFile(savedBorderless,ribbon),"Save document after editing equation color and undo/redo");
  var reloadedView=new WriterView{ViewMode=1,ZoomLevel=1};
  Check(reloadedView.LoadDocumentFile(savedBorderless,ribbon),"Reload saved DOCX without XML namespace errors");
  Check(reloadedView.DocumentTables.Count==14&&reloadedView.DocumentTables.Take(12).All(t=>!t.HasBorderTop&&!t.HasBorderBottom&&!t.HasBorderLeft&&!t.HasBorderRight&&!t.HasBorderInsideH&&!t.HasBorderInsideV),"Reopened DOCX keeps all 12 answer formatting tables borderless");
  Check(reloadedView.DocumentTables.Skip(12).All(t=>t.HasBorderTop&&t.HasBorderBottom&&t.HasBorderLeft&&t.HasBorderRight),"Reopened DOCX keeps TableGrid borders on answer key tables");
  // Verify that when a table DOES have borders enabled, its top and left borders are not clipped away
  var t0=reloadedView.DocumentTables[0];
  t0.HasBorderTop=t0.HasBorderBottom=t0.HasBorderLeft=t0.HasBorderRight=t0.HasBorderInsideH=t0.HasBorderInsideV=true;
  t0.BorderTopStyle=t0.BorderBottomStyle=t0.BorderLeftStyle=t0.BorderRightStyle=t0.BorderInsideHStyle=t0.BorderInsideVStyle="single";
  foreach(var r in t0.Rows)foreach(var c in r.Cells){c.HasBorderTop=c.HasBorderBottom=c.HasBorderLeft=c.HasBorderRight=true;c.BorderTopStyle=c.BorderBottomStyle=c.BorderLeftStyle=c.BorderRightStyle="single";}
  reloadedView.Draw(graphics,1200,1500,ribbon);reloadedView.Draw(graphics,1200,1500,ribbon);
  var c00=reloadedView.TableCellHits.First(c=>c.TableIndex==0&&c.RowIndex==0&&c.ColIndex==0);
  int topMidX=(int)(c00.CellRect.Left+c00.CellRect.Width/2),topY=(int)c00.CellRect.Top;
  int leftX=(int)c00.CellRect.Left,leftMidY=(int)(c00.CellRect.Top+c00.CellRect.Height/2);
  bool hasTopInk=Enumerable.Range(-1,3).Any(dy=>{var p=bitmap.GetPixel(topMidX,topY+dy);return p.R<100&&p.G<100&&p.B<100;});
  bool hasLeftInk=Enumerable.Range(-1,3).Any(dx=>{var p=bitmap.GetPixel(leftX+dx,leftMidY);return p.R<100&&p.G<100&&p.B<100;});
  Check(hasTopInk&&hasLeftInk,"Table top and left borders are rendered without being clipped by the row fragment clip");
  // Verify Slides font size > 96 and Justify alignment
  var slidesView=new SlidesView();
  slidesView.Presentation.CanvasWidth=960f;slidesView.Presentation.CanvasHeight=540f;
  var tb=new ZenQOffice.Core.ShapeNode{Type=ZenQOffice.Core.ShapeType.TextBox,Text="VietNam",FontName="Segoe UI",FontSize=96f,Bold=true,TextColor=0x0F172A,AutoFitWidth=false,Transform=new ZenQOffice.Core.Transform2D{X=50f,Y=50f,Width=500f,Height=160f}};
  slidesView.ActiveSlide.Shapes.Add(tb);
  slidesView.SelectedShapeIndex=slidesView.ActiveSlide.Shapes.Count-1;
  slidesView.SetSelectionFontSize(140f);
  Check(Math.Abs(slidesView.GetActiveFontSize()-140f)<0.01f&&tb.Transform.Height>160f,"Slides SetSelectionFontSize allows sizes > 96 (e.g. 140pt) and expands TextBox height");
  slidesView.AdjustSelectionFontSize(20f);
  Check(Math.Abs(slidesView.GetActiveFontSize()-160f)<0.01f,"Slides AdjustSelectionFontSize increments above 96pt");
  // Verify inline-editing Ctrl+A + font size > 96 expands TextBox vertically so text never overflows
  tb.Transform.Height=60f;
  slidesView.StartInlineEditing(tb);
  slidesView.TextSelectionStart=0;slidesView.TextSelectionLength=slidesView.EditingTextBuffer.Length;
  slidesView.SetSelectionFontSize(180f);
  Check(Math.Abs(slidesView.GetActiveFontSize()-180f)<0.01f&&tb.Transform.Height>=230f,$"Inline-editing SetSelectionFontSize(180) expands TextBox height (Height={tb.Transform.Height})");
  slidesView.EndInlineEditing();
  Check(tb.Transform.Height>=230f,$"EndInlineEditing preserves expanded TextBox height for 180pt text (Height={tb.Transform.Height})");
  float appliedFromPopup=0f;
  using(var szPopup=new SizeDropDownForm(96f,new Point(10,10)))
  {
   szPopup.OnSizeSelected+=sz=>appliedFromPopup=sz;
   var txtField=typeof(SizeDropDownForm).GetField("txtCustomSize",Private);
   var tbCustom=(TextBox)txtField.GetValue(szPopup);
   tbCustom.Text="180";
   Check(Math.Abs(appliedFromPopup-180f)<0.01f,"SizeDropDownForm live-applies custom font sizes > 96 (e.g. 180pt)");
  }
  tb.FontSize=48f;if(tb.TextRuns!=null)foreach(var r in tb.TextRuns)r.FontSize=48f;
  tb.Transform.Width=500f;tb.Transform.Height=120f;
  using var slideBmpLeft=new Bitmap(600,250);
  using var slideBmpJustify=new Bitmap(600,250);
  var drawShapeTextMethod=typeof(SlidesView).GetMethod("DrawShapeTextUnified",Private);
  using var testFont=new Font("Segoe UI",48f,FontStyle.Bold,GraphicsUnit.Pixel);
  tb.Alignment=ZenQOffice.Core.TextAlignment.Left;if(tb.TextRuns!=null)foreach(var r in tb.TextRuns)r.ParaAlignment=ZenQOffice.Core.TextAlignment.Left;
  using(var gLeft=Graphics.FromImage(slideBmpLeft)){gLeft.Clear(Color.White);using var d2dL=new D2DGraphics(gLeft);drawShapeTextMethod.Invoke(slidesView,new object[]{d2dL,tb,tb.Text,testFont,Color.Black,new RectangleF(50,30,500,120),1f,1f,255,false});}
  slidesView.SetSelectionParagraphAlignment(ZenQOffice.Core.TextAlignment.Justify);
  Check(tb.Alignment==ZenQOffice.Core.TextAlignment.Justify&&tb.TextRuns.All(r=>r.ParaAlignment==ZenQOffice.Core.TextAlignment.Justify),"SetSelectionParagraphAlignment sets Justify on shape and runs");
  using(var gJust=Graphics.FromImage(slideBmpJustify)){gJust.Clear(Color.White);using var d2dJ=new D2DGraphics(gJust);drawShapeTextMethod.Invoke(slidesView,new object[]{d2dJ,tb,tb.Text,testFont,Color.Black,new RectangleF(50,30,500,120),1f,1f,255,false});}
  int MaxInkX(Bitmap b){int mx=0;for(int py=0;py<b.Height;py++)for(int px=0;px<b.Width;px++){var c=b.GetPixel(px,py);if(c.R<120&&c.G<120&&c.B<120&&px>mx)mx=px;}return mx;}
  int leftMaxX=MaxInkX(slideBmpLeft),justMaxX=MaxInkX(slideBmpJustify);
  Check(justMaxX>leftMaxX+100,$"Justify alignment spreads text across TextBox width (leftMaxX={leftMaxX}, justMaxX={justMaxX})");
  host.Dispose();Check(menu.IsDisposed,"The popup is disposed with its owner, after its show/close lifecycle");
  Console.WriteLine("PASS: press/release selection, grip, arrow, compact popup, synchronous-close crash regression, 20 reopens, command state and owner cleanup");
 }
}
