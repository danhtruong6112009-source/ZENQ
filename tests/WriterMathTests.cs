using System;
using System.Linq;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Windows.Forms;
using ZenQOffice.UI;
using ZenQOffice.Format;
class WriterMathTests
{
 const string M="http://schemas.openxmlformats.org/officeDocument/2006/math";
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static XmlDocument Read(string path){using var zip=ZipFile.OpenRead(path);var doc=new XmlDocument();using var input=zip.GetEntry("word/document.xml").Open();doc.Load(input);return doc;}
 static int Count(XmlDocument doc,string name)=>doc.SelectNodes("//*[local-name()='"+name+"' and namespace-uri()='"+M+"']").Count;
 static string Atom(string xml){var doc=new XmlDocument();doc.LoadXml("<m:oMath xmlns:m='"+M+"'>"+xml+"</m:oMath>");return WriterMath.Register(doc.DocumentElement);}
 [STAThread]static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void FontUnits()
 {
  using var bitmap=new Bitmap(900,230);
  using var points=new Font("Times New Roman",14,FontStyle.Bold,GraphicsUnit.Point);
  using var pixels=new Font("Times New Roman",14,FontStyle.Bold,GraphicsUnit.Pixel);
  using var body=new Font("Times New Roman",10.5f,FontStyle.Bold,GraphicsUnit.Point);
  const string caption="Câu 1. [Mức độ 2] ";
  foreach(bool pointFirst in new[]{true,false})
  {
   D2DGraphics.ClearFontCache();
   using var native=Graphics.FromImage(bitmap);using var g=new D2DGraphics(native,false,900,230);
   g.MeasureString(caption,pointFirst?points:pixels);
   float pointWidth=g.MeasureString(caption,points).Width,pixelWidth=g.MeasureString(caption,pixels).Width;
   Check(pointWidth/pixelWidth>1.30f&&pointWidth/pixelWidth<1.37f,"Point and pixel font sizes remain distinct regardless of which size is cached first");
   Check(Math.Abs(g.MeasureString(caption,body).Width-pixelWidth)<.1f,"Equivalent physical font sizes share consistent measurement");
  }
  D2DGraphics.ClearFontCache();
  using(var native=Graphics.FromImage(bitmap))using(var g=new D2DGraphics(native,false,900,230))
  {
   g.Clear(Color.White);g.MeasureString(caption,points);
   g.DrawString(caption,body,Brushes.Black,20,20);
   string atom=WriterMath.FromLinear(@"\vec{AB}");
   using(var baseline=WriterMath.BeginLine(g,caption+atom,body,100))g.DrawString(caption,body,Brushes.Black,20,100);
  }
  int InkWidth(int top,int bottom)
  {
   int left=bitmap.Width,right=-1;
   for(int y=top;y<bottom;y++)for(int x=0;x<bitmap.Width;x++){var c=bitmap.GetPixel(x,y);if(c.R<160&&c.G<160&&c.B<160){left=Math.Min(left,x);right=Math.Max(right,x);}}
   return right-left+1;
  }
  Check(Math.Abs(InkWidth(0,75)-InkWidth(80,160))<=1,"A bold caption has the same painted width on plain and equation lines after toolbar fonts are warmed");
  bitmap.Save("output/writer-math/font-units.png");
 }
 static void Run()
 {
  Directory.CreateDirectory("output/writer-math");
  FontUnits();
  string vector=Atom("<m:acc><m:accPr><m:chr m:val='⃗'/></m:accPr><m:e><m:r><m:t>AB</m:t></m:r></m:e></m:acc>");
  string fraction=Atom("<m:f><m:num><m:r><m:t>3</m:t></m:r></m:num><m:den><m:r><m:t>2</m:t></m:r></m:den></m:f>");
  string radical=Atom("<m:rad><m:deg/><m:e><m:sSup><m:e><m:r><m:t>x</m:t></m:r></m:e><m:sup><m:r><m:t>2</m:t></m:r></m:sup></m:sSup><m:r><m:t>+1</m:t></m:r></m:e></m:rad>");
  string input=WriterMath.FromLinear(@"x = \frac{-b \pm \sqrt{b^2-4ac}}{2a}");
  Check(WriterMath.ToLinear(input).Contains(@"\frac{"),"Linear entry becomes a structured fraction");
  Check(WriterMath.ToLinear(WriterMath.Unpack(WriterMath.Pack("Test "+vector+fraction)))=="Test "+WriterMath.ToLinear(vector+fraction),"Clipboard payload preserves native equations without exposing private editor characters");
  string matrix=WriterMath.FromLinear(@"\matrix{a&b\\c&d}");Check(WriterMath.ToLinear(matrix)==@"\matrix{a&b\\c&d}","Matrix input retains its two rows and columns");
  Check(DocxHandler.SaveDocxFile("output/writer-math/new-structures.docx",input+matrix),"Export newly constructed equations");
  var constructed=Read("output/writer-math/new-structures.docx");var namespaces=new XmlNamespaceManager(constructed.NameTable);namespaces.AddNamespace("m",M);
  Check(constructed.SelectNodes("//m:oMath/m:e | //m:num/m:e | //m:den/m:e | //m:sub/m:e | //m:sup/m:e | //m:e/m:e",namespaces).Count==0,"New OMML uses valid argument contents without nested argument wrappers");
  using(var bmp=new Bitmap(900,220))
  {
   using(var native=Graphics.FromImage(bmp))using(var g=new D2DGraphics(native,false,900,220))using(var f=new Font("Times New Roman",24,GraphicsUnit.Pixel))
   {
    g.Clear(Color.White);g.DrawString("Vector: "+vector+" = "+vector,f,Brushes.Black,20,20);g.DrawString("Fraction: "+fraction+" a²",f,Brushes.Black,20,80);g.DrawString("Radical: "+radical,f,Brushes.Black,350,80);g.DrawString(input,f,Brushes.Black,20,150);g.DrawString(matrix,f,Brushes.Black,650,25);
    Check(WriterMath.Height(g,fraction,f)>f.Size*1.5,"Stacked fraction requires vertical room");Check(WriterMath.Measure(g,vector,f).Width>15,"Vector body has measurable width");
   }
   bmp.Save("output/writer-math/structures.png");
  }
  const string source="tests/fixtures/writer-math-exam.docx";
  Check(DocxHandler.LoadDocxFile(source,out DocxMetadata meta),"Load real mathematics exam");
  var original=Read(source);Check(Count(original,"oMath")==182,"Fixture has all 182 native equations");
  Console.WriteLine("Imported text atoms: "+meta.TextContent.Count(c=>WriterMath.Contains(c.ToString()))+" tables: "+meta.Tables.Count);
  var ribbon=new RibbonBar();var view=new WriterView{ViewMode=1,ZoomLevel=1};Check(view.LoadDocumentFile(source,ribbon),"Open mathematics exam in Writer");
  using(var bitmap=new Bitmap(1200,1500))
  {
   for(int pass=0;pass<2;pass++)using(var graphics=Graphics.FromImage(bitmap))view.Draw(graphics,1200,1500,ribbon);
   bitmap.Save("output/writer-math/exam-page-1.png");Console.WriteLine("Pages: "+view.CalculatedPageCount);
   for(int page=1;page<Math.Min(3,view.CalculatedPageCount);page++){view.ScrollY=page*1080;using(var graphics=Graphics.FromImage(bitmap))view.Draw(graphics,1200,1500,ribbon);bitmap.Save("output/writer-math/exam-page-"+(page+1)+".png");}
  }
  const string saved="output/writer-math/roundtrip.docx";
  Check(DocxHandler.SaveDocxFile(saved,meta.TextContent,meta.FormattedRuns,tables:meta.Tables,paraAlignments:meta.ParagraphAlignments),"Save native equations");
  var restored=Read(saved);
  foreach(string kind in new[]{"oMath","acc","f","rad","sSub","sSup","d"}){Console.WriteLine(kind+": "+Count(original,kind)+" -> "+Count(restored,kind));Check(Count(original,kind)==Count(restored,kind),"Preserve all native "+kind+" structures");}
  Check(!WriterMath.Contains(restored.OuterXml),"DOCX contains native OMML instead of editor tokens");
  var sourceEquations=original.SelectNodes("//*[local-name()='oMath' and namespace-uri()='"+M+"']").Cast<XmlNode>().Select(n=>n.InnerText).OrderBy(s=>s).ToArray();
  var savedEquations=restored.SelectNodes("//*[local-name()='oMath' and namespace-uri()='"+M+"']").Cast<XmlNode>().Select(n=>n.InnerText).OrderBy(s=>s).ToArray();
  Check(sourceEquations.SequenceEqual(savedEquations),"Equation variables and values are unchanged by saving");
  Check(DocxHandler.LoadDocxFile(saved,out DocxMetadata reloaded)&&WriterMath.Contains(reloaded.TextContent),"Reopen equations from saved DOCX");
  var editor=new WriterView{ViewMode=1,ZoomLevel=1};editor.InsertEquation(@"\frac{a}{b}");
  Check(editor.PieceTable.GetText().Length==1&&WriterMath.Contains(editor.GetSelectedText()),"Equation insertion creates one editable native atom");
  using(var bmp=new Bitmap(1200,1500))using(var graphics=Graphics.FromImage(bmp))
  {
   editor.Draw(graphics,1200,1500,ribbon);editor.Draw(graphics,1200,1500,ribbon);
   var hit=editor.RenderedLines.First(l=>l.StartCharIndex==0);editor.HandleDoubleClick(hit.ScreenX+2,hit.ScreenY+hit.ScreenH/2,1200,graphics,ribbon);
   Check(editor.IsEditingEquation&&editor.SelectionStart==0&&editor.SelectionEnd==1,"Double click selects the equation atom and activates its tools");
  }
  editor.InsertEquation(@"\vec{AB}");Check(WriterMath.ToLinear(editor.PieceTable.GetText())==@"\vec{AB}","Editing replaces the selected equation without appending duplicate text");
  Check(editor.SaveDocumentFile("output/writer-math/inserted.docx",ribbon),"Save equation inserted through Writer UI model");
  Check(Count(Read("output/writer-math/inserted.docx"),"acc")==1,"New vector saves as native Office math accent");
  Check(editor.SaveDocumentFile("output/writer-math/linear.txt",ribbon),"Plain text export succeeds");
  Check(File.ReadAllText("output/writer-math/linear.txt")==@"\vec{AB}","Plain text export does not leak private atom IDs");
  view.ScrollY=0;
  using(var bmp=new Bitmap(1200,1500))using(var graphics=Graphics.FromImage(bmp))
  {
   view.Draw(graphics,1200,1500,ribbon);
   WriterView.EquationHit Answer(int row,int col)
   {
    var cell=view.TableCellHits.First(c=>c.TableIndex==0&&c.RowIndex==row&&c.ColIndex==col);
    Check(cell.Alignment==StringAlignment.Near,"Imported answers retain left alignment even when equation atoms make the text short");
    return view.EquationHits.First(h=>h.Index>=cell.CharStartIndex&&h.Index<cell.CharStartIndex+cell.CharLength);
   }
   var a=Answer(0,0);var b=Answer(0,1);var c=Answer(1,0);var d=Answer(1,1);
   Check(Math.Abs(a.Bounds.Left-c.Bounds.Left)<2&&Math.Abs(b.Bounds.Left-d.Bounds.Left)<2,"A/C and B/D begin at the same source column positions");
   view.HandleMouseDown((int)(d.Bounds.Left+3),(int)(d.Bounds.Top+d.Bounds.Height/2),1200,graphics,ribbon);
   Check(view.SelectionStart==d.Index&&view.SelectionEnd==d.Index+1&&view.IsEditingEquation,"Single click selects the exact equation in its table cell");
   view.Draw(graphics,1200,1500,ribbon);
   Check(!view.EquationFrameBounds.IsEmpty&&view.EquationMenuButtonBounds.Right==view.EquationFrameBounds.Right,"Selected equation has a frame, grip and right-side options button");
   bmp.Save("output/writer-math/equation-frame.png");
   using(var menu=view.BuildEquationMenu())((ToolStripMenuItem)menu.Items.Cast<ToolStripItem>().First(i=>i.Text=="Linear (LaTeX)")).PerformClick();
   Check(WriterMath.IsLinear(view.PieceTable.GetText()[d.Index]),"Equation options actually switch the selected object to linear notation");
   Check(view.SaveDocumentFile("output/writer-math/linear-mode.docx",ribbon),"Save linear presentation without requiring another paint");
   var modeDoc=Read("output/writer-math/linear-mode.docx");
   Check(Count(modeDoc,"oMath")==182&&Count(modeDoc,"acc")==103,"Linear mode retains the original native math structures");
   Check(modeDoc.SelectNodes("//*[@*[local-name()='linear' and namespace-uri()='urn:zenqoffice:math-presentation:1']]").Count==1,"Changing one equation does not alter identical objects elsewhere");
   Check(DocxHandler.LoadDocxFile("output/writer-math/linear-mode.docx",out DocxMetadata modeMeta)&&modeMeta.TextContent.Any(WriterMath.IsLinear),"Writer retains linear presentation when reopening its DOCX");
   view.Undo();Check(!WriterMath.IsLinear(view.PieceTable.GetText()[d.Index]),"Undo restores the original equation presentation");
   view.Redo();Check(WriterMath.IsLinear(view.PieceTable.GetText()[d.Index]),"Redo restores the chosen linear presentation");
   view.Draw(graphics,1200,1500,ribbon);bmp.Save("output/writer-math/equation-linear.png");
   view.SetEquationLinear(false);Check(!WriterMath.IsLinear(view.PieceTable.GetText()[d.Index]),"Professional mode restores the structured equation");
   view.SetEquationLinear(true,true);Check(view.PieceTable.GetText().Where(ch=>WriterMath.Contains(ch.ToString())).All(WriterMath.IsLinear),"All Linear changes every formula");
   view.SetEquationLinear(false,true);Check(!view.PieceTable.GetText().Any(WriterMath.IsLinear),"All Professional restores every formula");
   view.ZoomLevel=1.5f;view.Draw(graphics,1200,1500,ribbon);
   Check(!view.EquationFrameBounds.IsEmpty&&view.EquationHits.Any(h=>h.Index==d.Index),"Equation hit targets and frame are rebuilt at the new zoom");
  }
  var duplicate=new WriterView();duplicate.PieceTable.SetInitialText(vector+" "+vector);duplicate.SelectionStart=0;duplicate.SelectionEnd=1;
  duplicate.SetEquationLinear(true);Check(WriterMath.IsLinear(duplicate.PieceTable.GetText()[0])&&!WriterMath.IsLinear(duplicate.PieceTable.GetText()[2]),"Identical equations can have independent presentation modes");
  Console.WriteLine("PASS: structured math render, real 182-equation exam, and native DOCX equation roundtrip");
 }
}
