using System;using System.Drawing;using System.Windows.Forms;using System.Reflection;using System.Diagnostics;using System.Collections.Generic;using System.Linq;using ZenQOffice.UI;using ZenQOffice.Core;using ZenQOffice.Format;
class Test{
 [STAThread]static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static void Run(){var deck=PptxHandler.LoadPptx(@"C:\Users\Danh\Downloads\Đỏ Vàng Bài Thuyết Trình Truyền Thống Việt Nam.pptx");var view=new SlidesView{Presentation=deck,ActiveSlideIndex=0};deck.Slides[0].Transition=TransitionType.GenOffice_SmoothFlow;view.PlayTransitionPreview();
 using var control=new Control{Size=new Size(1366,768)};var surface=GpuEngine.GetOrCreateSurface(control.Handle,1366,768);var field=typeof(SlidesView).GetField("flowTo",BindingFlags.Instance|BindingFlags.NonPublic);object previous=null;int rebuilds=0;var timings=new List<double>();
 try{for(int i=0;i<32;i++){view.TransitionProgress=.02f+i*.029f;var watch=Stopwatch.StartNew();using(var g=D2DGraphics.FromGpuSurface(surface))view.Draw(g,1366,768);surface.Present(false);timings.Add(watch.Elapsed.TotalMilliseconds);var current=field.GetValue(view);if(previous!=null&&!ReferenceEquals(previous,current))rebuilds++;previous=current;}
 Console.WriteLine("Actual first-slide preview: scene rebuilds="+rebuilds+", warm median="+timings.Skip(1).OrderBy(t=>t).ElementAt(15).ToString("F1")+"ms, warm max="+timings.Skip(1).Max().ToString("F1")+"ms");if(rebuilds!=0)throw new Exception("Opening preview recreated its Flow scene between frames");
 Console.WriteLine("PASS: actual first-slide preview uses the same captured scene throughout GPU playback");
 }finally{view.ClearTransitionCache();GpuEngine.DestroySurface(control.Handle);}}
}