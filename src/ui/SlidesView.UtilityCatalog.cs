using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;using System.Globalization;using System.Text;using ZenQOffice.Core;
namespace ZenQOffice.UI {
 public partial class SlidesView {
  sealed record UtilityEntry(string Id,string Name,string Detail,string Category,int Icon);
  static readonly UtilityEntry[] UtilityApps={
   new("qr","Mã QR","Liên kết và văn bản","Dữ liệu",0),new("chart","Biểu đồ","Biểu đồ có thể chỉnh sửa","Dữ liệu",1),new("icons","Đồ họa","Icon và minh họa vector","Thiết kế",2),new("type","Chữ nghệ thuật","Kiểu chữ và hiệu ứng","Chữ",3),
   new("frame","Khung ảnh","Bo góc và viền ảnh","Ảnh",4),new("palette","Bảng màu","Phối màu cho slide","Thiết kế",5),new("cutout","Xóa nền","Tách nền và tinh chỉnh vùng","Ảnh",6),new("gradient","Chuyển sắc","Tạo và sửa màu gradient","Thiết kế",7),
   new("shadow","Đổ bóng","Tạo chiều sâu cho đối tượng","Thiết kế",8),new("table","Bảng dữ liệu","Chèn bảng chỉnh sửa được","Dữ liệu",9),new("equation","Công thức","Kho công thức toán học","Dữ liệu",10),new("draw","Vẽ tự do","Vẽ nét bằng chuột hoặc bút","Thiết kế",11)};
  static readonly Lazy<Bitmap> utilityAtlas=new(()=>{using var stream=typeof(SlidesView).Assembly.GetManifestResourceStream("apps.zenq-utility-atlas.png");using var image=Image.FromStream(stream);return new Bitmap(image);});
  Rectangle catalogViewport,catalogTrack,catalogThumb;int catalogContentHeight;bool catalogDragging;int catalogDragOffset;TextBox catalogSearchBox;string utilityCategory="Tất cả",utilityStatus="";
  static string CatalogKey(string value){var normalized=(value??"").ToLowerInvariant().Normalize(NormalizationForm.FormD);var sb=new StringBuilder();foreach(char c in normalized)if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)sb.Append(c=='đ'?'d':c);return sb.ToString().Normalize(NormalizationForm.FormC);}
  public bool ScrollElementsCatalog(int delta){if(!IsElementsDrawerOpen||ElementsDrawerSelectedTab==1)return false;if(catalogDragging)return true;SetCatalogScroll(ElementsScrollY-delta/120f*36,true);InvalidateCallback?.Invoke();return true;}
  void SetCatalogScroll(float value,bool immediate){TargetElementsScrollY=Math.Clamp(value,0,Math.Max(0,catalogContentHeight-catalogViewport.Height));if(immediate){ElementsScrollY=(int)TargetElementsScrollY;SmoothElementsScrollY=TargetElementsScrollY;}}
  void ResetCatalogScroll(){if(catalogDragging&&ParentControl!=null)ParentControl.Capture=false;ElementsScrollY=0;SmoothElementsScrollY=TargetElementsScrollY=0;catalogDragging=false;}
  bool BeginCatalogScroll(Point point){if(!catalogTrack.Contains(point))return false;catalogDragging=true;if(ParentControl!=null)ParentControl.Capture=true;catalogDragOffset=catalogThumb.Contains(point)?point.Y-catalogThumb.Y:catalogThumb.Height/2;MoveCatalogScroll(point);return true;}
  void MoveCatalogScroll(Point point){float range=Math.Max(1,catalogTrack.Height-catalogThumb.Height);int previous=ElementsScrollY;SetCatalogScroll((point.Y-catalogTrack.Y-catalogDragOffset)/range*Math.Max(0,catalogContentHeight-catalogViewport.Height),true);if(previous!=ElementsScrollY)InvalidateCallback?.Invoke();}
  void PaintCatalogScrollbar(D2DGraphics g){SetCatalogScroll(TargetElementsScrollY,false);catalogTrack=catalogThumb=Rectangle.Empty;if(catalogContentHeight<=catalogViewport.Height)return;catalogTrack=new Rectangle(catalogViewport.Right-8,catalogViewport.Y+3,5,catalogViewport.Height-6);int h=Math.Min(catalogTrack.Height,Math.Max(28,catalogTrack.Height*catalogViewport.Height/Math.Max(1,catalogContentHeight)));catalogThumb=new Rectangle(catalogTrack.X,catalogTrack.Y+(int)(ElementsScrollY/(float)Math.Max(1,catalogContentHeight-catalogViewport.Height)*(catalogTrack.Height-h)),5,h);using var brush=new SolidBrush(Color.FromArgb(180,189,202));g.FillRoundedRectangle(brush,catalogThumb,2.5f);}
  void UpdateCatalogSearch(){if(ParentControl==null)return;bool show=IsElementsDrawerOpen&&!IsElementsDrawerCollapsed&&!IsPresenting&&ElementsDrawerSelectedTab!=1;if(ParentControl is AppForm app && (app.BackstageView?.IsOpen==true||app.Ribbon?.CurrentMode!=OfficeAppMode.Slides))show=false;if(!show){if(catalogSearchBox!=null)catalogSearchBox.Visible=false;return;}
   if(catalogSearchBox==null){catalogSearchBox=new TextBox{BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(248,250,252),ForeColor=Color.FromArgb(38,48,65),Font=new Font("Segoe UI",9.5f),PlaceholderText="Tìm trong danh mục…"};catalogSearchBox.TextChanged+=(s,e)=>{ElementsSearchQuery=catalogSearchBox.Text;ResetCatalogScroll();InvalidateCallback?.Invoke();};catalogSearchBox.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){catalogSearchBox.Clear();ParentControl.Focus();e.SuppressKeyPress=true;}};catalogSearchBox.MouseWheel+=(s,e)=>{ScrollElementsCatalog(e.Delta);if(e is HandledMouseEventArgs handled)handled.Handled=true;};ParentControl.Controls.Add(catalogSearchBox);}
   if(_shapeSearchBox!=null)_shapeSearchBox.Visible=false;
   var bounds=new Rectangle(TxtSearchBoxBounds.X+27,TxtSearchBoxBounds.Y+7,Math.Max(20,TxtSearchBoxBounds.Width-37),20);if(catalogSearchBox.Bounds!=bounds)catalogSearchBox.Bounds=bounds;
   if(!catalogSearchBox.Visible){catalogSearchBox.Visible=true;catalogSearchBox.BringToFront();}
  }
  bool CatalogMatch(string value)=>CatalogKey(value).Contains(CatalogKey(ElementsSearchQuery));
  void CatalogText(D2DGraphics g,string value,Font font,Color color,RectangleF bounds,bool center=false){using var brush=new SolidBrush(color);using var format=new StringFormat{Alignment=center?StringAlignment.Center:StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var state=g.Save();try{g.IntersectClip(bounds);g.DrawString(value,font,brush,bounds,format);}finally{g.Restore(state);}}
  void CatalogHit(Rectangle rect,string action){if(rect.IntersectsWith(catalogViewport)){DrawerItemBounds.Add(rect);DrawerItemActions.Add(action);}}
  void CatalogIcon(D2DGraphics g,int icon,RectangleF rect){var atlas=utilityAtlas.Value;float sx=atlas.Width/1448f,sy=atlas.Height/1086f;int[] columns={40,384,730,1074};int[] rows={36,363,689};g.DrawImage(atlas,rect,new RectangleF(columns[icon%4]*sx,rows[icon/4]*sy,336*sx,326*sy),GraphicsUnit.Pixel);}
  void DrawUtilityCatalog(D2DGraphics g,int left,int top,int width,int height){
   int y=top-ElementsScrollY;using var title=new Font("Segoe UI",10f,FontStyle.Bold);using var label=new Font("Segoe UI",8.5f);using var preview=new Font("Segoe UI",22f,FontStyle.Bold);
   if(ElementsDrawerSelectedTab==7){
    if(!string.IsNullOrEmpty(utilityStatus)){CatalogText(g,utilityStatus,label,Color.FromArgb(99,62,192),new RectangleF(left+14,y,width-28,34));y+=42;}
    if(string.IsNullOrWhiteSpace(ElementsSearchQuery)){
    var hero=new Rectangle(left+12,y+2,width-28,106);CatalogHit(hero,"app:type");
    using(var gradient=new LinearGradientBrush(hero,Color.FromArgb(238,232,255),Color.FromArgb(220,242,255),30))g.FillRoundedRectangle(gradient,hero,10);
    CatalogText(g,"Cho chữ thêm cá tính",title,Color.FromArgb(45,37,81),new RectangleF(hero.X+12,hero.Y+9,hero.Width-24,22));
    CatalogText(g,"Chọn kiểu chữ nghệ thuật  ›",label,Color.FromArgb(87,82,112),new RectangleF(hero.X+12,hero.Y+33,hero.Width-24,20));
    CatalogText(g,"Aa  /  ZENQ",preview,Color.FromArgb(101,63,195),new RectangleF(hero.X+12,hero.Y+53,hero.Width-24,44));y+=124;
    }
    string[] categories={"Tất cả","Thiết kế","Ảnh","Chữ","Dữ liệu"};int chipX=left+12;
    foreach(string category in categories){int chipW=category.Length*6+16;if(chipX+chipW>left+width-12){chipX=left+12;y+=29;}var chip=new Rectangle(chipX,y,chipW,25);using var fill=new SolidBrush(category==utilityCategory?Color.FromArgb(238,233,255):Color.FromArgb(245,247,250));g.FillRoundedRectangle(fill,chip,12);CatalogText(g,category,label,category==utilityCategory?Color.FromArgb(99,62,192):Color.FromArgb(88,99,115),chip,true);CatalogHit(chip,"appfilter:"+category);chipX+=chipW+5;}y+=39;
    CatalogText(g,"Công cụ của bạn",title,Color.FromArgb(32,42,60),new RectangleF(left+14,y,width-28,24));y+=32;
    var apps=UtilityApps.Where(app=>(utilityCategory=="Tất cả"||app.Category==utilityCategory)&&CatalogMatch(app.Name+" "+app.Detail+" "+app.Category)).ToList();
    int cols=3,cellW=(width-24)/cols;
    for(int i=0;i<apps.Count;i++){var app=apps[i];var cell=new Rectangle(left+12+i%cols*cellW,y+i/cols*102,cellW-4,96);if(!cell.IntersectsWith(catalogViewport))continue;
     if(cell.Contains(CurrentMouseCanvasPos)){using var hover=new SolidBrush(Color.FromArgb(246,243,255));g.FillRoundedRectangle(hover,cell,8);}
     CatalogIcon(g,app.Icon,new RectangleF(cell.X+(cell.Width-62)/2f,cell.Y,62,62));
     CatalogText(g,app.Name,label,Color.FromArgb(46,55,73),new RectangleF(cell.X,cell.Y+64,cell.Width,25),true);CatalogHit(cell,"app:"+app.Id);
    }
    y+=(int)Math.Ceiling(apps.Count/3d)*102;
    if(apps.Count==0){CatalogText(g,"Không tìm thấy công cụ phù hợp.",label,Color.SlateGray,new RectangleF(left+16,y,width-32,42));y+=46;}
    if(!string.IsNullOrEmpty(utilityStatus)){CatalogText(g,utilityStatus,label,Color.FromArgb(99,62,192),new RectangleF(left+14,y,width-28,30));y+=34;}
    CatalogText(g,"Tích hợp trong ZenQ Office",label,Color.SlateGray,new RectangleF(left+14,y,width-28,28));y+=34;
   }else{
    var import=new Rectangle(left+12,y+2,width-28,34);using(var fill=new SolidBrush(Color.FromArgb(242,238,255)))g.FillRoundedRectangle(fill,import,7);CatalogText(g,"+  Nhập phông chữ .ttf / .otf",label,Color.FromArgb(99,62,192),import,true);CatalogHit(import,"font:import");y+=50;
    CatalogText(g,"Chọn phông chữ",title,Color.FromArgb(32,42,60),new RectangleF(left+14,y,width-28,24));y+=32;
    var fonts=(SlideFontManager.GetCustomFontNames()??new List<string>()).Concat(new[]{"Segoe UI","Arial","Calibri","Times New Roman","Cambria","Georgia"}).Distinct(StringComparer.OrdinalIgnoreCase).Where(name=>!name.Contains("Icons",StringComparison.OrdinalIgnoreCase)&&!name.Contains("MDL2",StringComparison.OrdinalIgnoreCase)&&!name.Contains("Wingdings",StringComparison.OrdinalIgnoreCase)&&CatalogMatch(name)).ToList();
    foreach(string name in fonts){var card=new Rectangle(left+12,y,width-28,68);if(card.IntersectsWith(catalogViewport)){
      using var fill=new SolidBrush(SelectedShape?.FontName==name?Color.FromArgb(246,242,255):Color.White);using var border=new Pen(SelectedShape?.FontName==name?Color.FromArgb(167,139,250):Color.FromArgb(230,234,240));g.FillRoundedRectangle(fill,card,8);g.DrawRoundedRectangle(border,card,8);
      CatalogText(g,name,label,Color.FromArgb(54,65,84),new RectangleF(card.X+12,card.Y+5,card.Width-24,21));
      using var sample=SlideFontManager.CreateFont(name,17,FontStyle.Regular,GraphicsUnit.Pixel);CatalogText(g,"Cùng tạo điều tuyệt vời",sample,Color.FromArgb(82,93,111),new RectangleF(card.X+12,card.Y+29,card.Width-24,29));CatalogHit(card,"font:apply:"+name);
     }y+=76;}
    if(fonts.Count==0){CatalogText(g,"Không có phông chữ phù hợp.",label,Color.SlateGray,new RectangleF(left+16,y,width-32,40));y+=48;}
    CatalogText(g,"Thêm văn bản",title,Color.FromArgb(32,42,60),new RectangleF(left+14,y,width-28,24));y+=32;
    foreach(var item in new[]{("Tiêu đề","text:title"),("Tiêu đề phụ","text:subtitle"),("Nội dung","text:body")}){var rect=new Rectangle(left+12,y,width-28,38);using var fill=new SolidBrush(Color.FromArgb(246,248,251));g.FillRoundedRectangle(fill,rect,6);CatalogText(g,item.Item1,label,Color.FromArgb(46,55,73),new RectangleF(rect.X+12,rect.Y,rect.Width-24,rect.Height));CatalogHit(rect,item.Item2);y+=46;}
   }
   catalogContentHeight=Math.Max(0,y+ElementsScrollY-top+14);
  }
  bool ExecuteUtility(string action,Control parent){
   if(action.StartsWith("appfilter:")){utilityCategory=action.Substring(10);ResetCatalogScroll();return true;}
   if(!action.StartsWith("app:"))return false;
   string id=action.Substring(4);utilityStatus="";
   bool NeedSelection(bool image=false){if(SelectedShape!=null&&(!image||SelectedShape.CachedImage!=null||!string.IsNullOrEmpty(SelectedShape.ImagePath)))return true;utilityStatus=image?"Chọn ảnh trên slide để sử dụng.":"Chọn một đối tượng trên slide.";ResetCatalogScroll();return false;}
   switch(id){
    case "qr":using(var dlg=new CreateQrCodeDialogForm())if(dlg.ShowDialog(parent)==DialogResult.OK&&dlg.GeneratedQrBitmap!=null)InsertQrCodeBitmap(dlg.GeneratedQrBitmap);break;
    case "chart":InsertChart("Column_Clustered");break;
    case "icons":OpenGraphicsDrawer();break;
    case "type":using(var dlg=new WordArtGalleryForm())if(dlg.ShowDialog(parent)==DialogResult.OK)InsertWordArt(dlg.WordArtText,dlg.SelectedStyleIndex);break;
    case "frame":if(NeedSelection(true)){SaveUndoState();SelectedShape.Type=ShapeType.Image;SelectedShape.CustomPath=null;SelectedShape.PathWidth=SelectedShape.PathHeight=0;SelectedShape.CropShapeType="RoundedRectangle";SelectedShape.CornerRadius=20;SelectedShape.StrokeWidth=4;SelectedShape.StrokeColor=0xffffff;SelectedShape.InvalidateDisplayCache();ActiveSlide.IsThumbnailDirty=true;}break;
    case "palette":IsElementsDrawerOpen=true;IsElementsDrawerCollapsed=false;ElementsDrawerSelectedTab=3;break;
    case "cutout":if(NeedSelection(true))StartBackgroundRemoval();break;
    case "gradient":if(NeedSelection())OpenColorPicker(SelectedShape.Type==ShapeType.TextBox?ColorPickerTarget.TextColor:ColorPickerTarget.ShapeFill,0,true);break;
    case "shadow":if(NeedSelection()){SaveUndoState();SelectedShape.HasShadow=true;SelectedShape.ShadowBlur=16;SelectedShape.ShadowOpacity=.22f;SelectedShape.ShadowOffsetY=7;SelectedShape.ShadowOffsetX=0;SelectedShape.InvalidateDisplayCache();ActiveSlide.IsThumbnailDirty=true;}break;
    case "table":InsertTable(3,4);break;
    case "equation":using(var dlg=new EquationGalleryForm())if(dlg.ShowDialog(parent)==DialogResult.OK)InsertEquation(dlg.SelectedEquationText);break;
    case "draw":StartFreeformScribbleMode();break;
    default:return false;
   }return true;
  }
 }
}
