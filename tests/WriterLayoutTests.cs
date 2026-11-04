using System;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.UI;
using ZenQOffice.Format;
class WriterLayoutTests
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 [STAThread]static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void Run()
 {
  Directory.CreateDirectory("output/writer-layout");
  var parts=WriterTablePagination.Build(new[]{24,2400,32},0,200,p=>100+p*1100,p=>1000+p*1100);
  Check(parts.Where(p=>p.Row==1).Sum(p=>p.Height)==2400,"Split row retains all content");
  foreach(var part in parts)Check(part.Y>=100+part.Page*1100&&part.Y+part.Height<=1000+part.Page*1100,"Every fragment stays between its page margins");
  var row=parts.Where(p=>p.Row==1).ToList();for(int i=1;i<row.Count;i++)Check(row[i].Offset==row[i-1].Offset+row[i-1].Height,"Continuations neither duplicate nor skip source content");
  var kept=WriterTablePagination.Build(new[]{500},0,700,p=>100+p*1100,p=>1000+p*1100,r=>true);Check(kept.Count==1&&kept[0].Page==1,"cantSplit row moves intact when it fits the following page");
  Check(DocxHandler.LoadDocxFile("tests/fixtures/writer-lesson.docx",out DocxMetadata meta),"Load actual lesson document");
  Check(meta.Tables.Count==2&&meta.Tables.All(t=>t.Rows.Count==2),"Source keeps two tables and four actual rows");
  Check(meta.Tables.All(t=>t.Rows[0].RepeatHeader),"Imported repeating table headers are retained");
  Check(meta.PageWidthPt==612&&meta.PageHeightPt==792&&meta.LeftMarginPt==72,"US Letter page size and source margins remain intact");
  int heading=meta.TextContent.IndexOf("CHỦ ĐỀ 1.");int headingStart=meta.TextContent.LastIndexOf('\n',heading)+1;
  Check(meta.ParagraphAlignments.TryGetValue(headingStart,out int alignment)&&alignment==1,"Floating drawing insertion preserves the heading's centered paragraph alignment");
  var ribbon=new RibbonBar();var view=new WriterView{ViewMode=1,ZoomLevel=1};
  Check(view.LoadDocumentFile("tests/fixtures/writer-lesson.docx",ribbon),"Load source through Writer's normal open path");
  using var bitmap=new Bitmap(1200,1500);
  for(int pass=0;pass<2;pass++)using(var graphics=Graphics.FromImage(bitmap))view.Draw(graphics,1200,1500,ribbon);
  bitmap.Save("output/writer-layout/lesson-top.png");Console.WriteLine("Pages: "+view.CalculatedPageCount+" fragments: "+view.TableCellHits.Count);
  Check(view.TableCellHits.GroupBy(h=>(h.TableIndex,h.RowIndex,h.ColIndex)).Any(group=>group.Count()>1),"Long table rows create separate editable continuation regions");
  Check(view.TableCellHits.Count(h=>h.RowIndex==0)>=4,"Repeated header rows are rendered on continuation pages");
  for(int page=1;page<Math.Min(6,view.CalculatedPageCount);page++)
  {
   view.ScrollY=page*1080;using(var graphics=Graphics.FromImage(bitmap))view.Draw(graphics,1200,1500,ribbon);
   bitmap.Save("output/writer-layout/lesson-page-"+(page+1)+".png");
  }
  meta.Tables[0].Rows[1].CantSplit=true;
  Check(DocxHandler.SaveDocxFile("output/writer-layout/roundtrip.docx",meta.TextContent,meta.FormattedRuns,tables:meta.Tables,paraAlignments:meta.ParagraphAlignments),"Save source table model to a separate DOCX");
  Check(DocxHandler.LoadDocxFile("output/writer-layout/roundtrip.docx",out DocxMetadata restored),"Reload exported DOCX");
  Check(restored.Tables[0].Rows[0].RepeatHeader&&restored.Tables[0].Rows[1].CantSplit,"Repeat-header and keep-row flags survive save and reload");
  Console.WriteLine("PASS: real lesson import, source paper settings, repeated header metadata, row continuation coverage, page margins and Writer render smoke");
 }
}