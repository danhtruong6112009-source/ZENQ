using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.Text;
using System.Collections.Generic;

namespace ZenQOffice.UI
{
    internal static class FluentUi
    {
        internal static readonly Color Border=Color.FromArgb(213,221,232), Ink=Color.FromArgb(35,48,65), Hover=Color.FromArgb(234,242,251);
        internal static readonly FluentMenuRenderer MenuRenderer=new();
        internal static readonly HashSet<ToolStripDropDown> OpenMenus=new();
        internal static bool IsMenuOpen=>OpenMenus.Count>0;
        internal static void Initialize()=>ToolStripManager.Renderer=MenuRenderer;
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
        private static readonly ConditionalWeakTable<Control,object> Rounded=new();
        internal static void RoundWindow(Control control,bool small=false)
        {
            if(control.IsDisposed||control.Disposing||!control.IsHandleCreated||Rounded.TryGetValue(control,out _))return;
            try {int round=small?3:2;DwmSetWindowAttribute(control.Handle,33,ref round,4);Rounded.Add(control,new object());control.HandleDestroyed+=(s,e)=>Rounded.Remove(control);}catch(DllNotFoundException){}
        }
        internal static string MnemonicText(string text,out int index)
        {
            index=-1;var result=new StringBuilder();text??="";
            for(int i=0;i<text.Length;i++)
            {
                if(text[i]=='&'&&i+1<text.Length)
                {
                    if(text[i+1]=='&'){result.Append('&');i++;}
                    else if(index<0)index=result.Length;
                }
                else result.Append(text[i]);
            }
            return result.ToString();
        }
        internal static void Text(D2DGraphics g,string text,Font font,Color color,RectangleF rect,bool center=false)
        {
            using var brush=new SolidBrush(color);using var format=new StringFormat{LineAlignment=StringAlignment.Center,Alignment=center?StringAlignment.Center:StringAlignment.Near,FormatFlags=StringFormatFlags.NoWrap,Trimming=StringTrimming.EllipsisCharacter};
            g.DrawString(text??"",font,brush,rect,format);
        }
        internal static void Chevron(D2DGraphics g,RectangleF bounds,Color color,bool right=false)
        {
            using var pen=new Pen(color,1.4f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
            float x=bounds.X+bounds.Width/2,y=bounds.Y+bounds.Height/2;
            g.DrawLines(pen,right?new[]{new PointF(x-2,y-4),new PointF(x+2,y),new PointF(x-2,y+4)}:new[]{new PointF(x-4,y-2),new PointF(x,y+2),new PointF(x+4,y-2)});
        }
        internal static void Row(D2DGraphics g,Rectangle bounds,string text,Font font,bool selected,bool enabled=true)
        {
            using var fill=new SolidBrush(selected?Hover:Color.White);g.FillRectangle(fill,bounds);
            Text(g,text,font,enabled?Ink:SystemColors.GrayText,new RectangleF(bounds.X+7,bounds.Y,bounds.Width-14,bounds.Height));
        }
    }

    internal static class FluentMotion
    {
        internal static double NowMs => System.Diagnostics.Stopwatch.GetTimestamp() * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        internal static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        internal static float EaseOutCubic(float t)
        {
            t = Clamp01(t);
            float u = 1f - t;
            return 1f - (u * u * u);
        }
        internal static float EaseOutQuint(float t)
        {
            t = Clamp01(t);
            float u = 1f - t;
            return 1f - (u * u * u * u * u);
        }
        internal static float Lerp(float a, float b, float t) => a + (b - a) * t;
        internal static double DeltaSeconds(ref long lastTimestamp)
        {
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            double dt = lastTimestamp == 0 ? 0.016 : Math.Clamp((now - lastTimestamp) / (double)System.Diagnostics.Stopwatch.Frequency, 0.004, 0.05);
            lastTimestamp = now;
            return dt;
        }
        internal static float DampFactor(double dt, double timeConstantSec)
        {
            if (timeConstantSec <= 0.0001) return 1f;
            return Clamp01(1f - (float)Math.Exp(-dt / timeConstantSec));
        }
        internal static bool StepAxis(ref float current, float target, float blend, float snapEps = 0.5f)
        {
            float diff = target - current;
            if (Math.Abs(diff) <= snapEps)
            {
                if (current != target) { current = target; return true; }
                return false;
            }
            float next = current + diff * blend;
            if (Math.Abs(target - next) <= snapEps) current = target;
            else current = next;
            return true;
        }
    }

    public sealed class FluentMenuRenderer : ToolStripProfessionalRenderer
    {
        public FluentMenuRenderer(){RoundedEdges=false;}
        private sealed class ItemCache
        {
            internal readonly Dictionary<string,Bitmap> Images=new();
            internal void Clear(){foreach(var image in Images.Values)image.Dispose();Images.Clear();}
        }
        private readonly ConditionalWeakTable<ToolStripItem,ItemCache> _itemCache=new();
        private readonly Dictionary<string,Bitmap> _captionCache=new();
        private void Cached(Graphics graphics,ToolStripItem item,string key,Rectangle bounds,Action<D2DGraphics> draw)
        {
            if(bounds.Width<=0||bounds.Height<=0)return;
            bool caption=key.StartsWith("text:",StringComparison.Ordinal);
            var cache=caption?null:_itemCache.GetValue(item,i=>{var c=new ItemCache();i.Disposed+=(s,e)=>c.Clear();return c;});
            var images=caption?_captionCache:cache.Images;
            key+=":"+bounds+":"+graphics.DpiX+":"+graphics.DpiY+":"+item.Selected+":"+item.Enabled;
            if(!images.TryGetValue(key,out var bitmap))
            {
                if(images.Count>=(caption?384:12)){foreach(var old in images.Values)old.Dispose();images.Clear();}
                bitmap=new Bitmap(bounds.Width,bounds.Height);
                using(var surface=Graphics.FromImage(bitmap))using(var g=new D2DGraphics(surface,bounds.Width,bounds.Height))
                {
                    g.Clear(item.Selected&&item.Enabled?FluentUi.Hover:Color.FromArgb(253,254,255));
                    g.TranslateTransform(-bounds.X,-bounds.Y);draw(g);
                }
                // DC interop can leave alpha zero on glyph pixels. These tiles have an opaque
                // background: publish all RGB pixels, including black text, as opaque.
                var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),System.Drawing.Imaging.ImageLockMode.ReadWrite,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    unsafe {for(int y=0;y<bitmap.Height;y++){byte* row=(byte*)data.Scan0+y*data.Stride;for(int x=0;x<bitmap.Width;x++)row[x*4+3]=255;}}
                }
                finally {bitmap.UnlockBits(data);}
                images.Add(key,bitmap);
            }
            graphics.DrawImageUnscaled(bitmap,bounds.Location);
        }
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if(e.ToolStrip is ToolStripDropDown)FluentUi.RoundWindow(e.ToolStrip,e.ToolStrip is FluentContextMenuStrip menu&&menu.CornerRadius<=4);
            using var g=new D2DGraphics(e.Graphics,e.ToolStrip.Width,e.ToolStrip.Height);g.Clear(Color.FromArgb(253,254,255));
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var g=new D2DGraphics(e.Graphics,e.ToolStrip.Width,e.ToolStrip.Height);using var p=new Pen(FluentUi.Border);
            g.DrawRoundedRectangle(p,new RectangleF(.5f,.5f,e.ToolStrip.Width-1,e.ToolStrip.Height-1),e.ToolStrip is FluentContextMenuStrip menu?menu.CornerRadius:8f);
        }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if(!e.Item.Enabled||(!e.Item.Selected&&!e.Item.Pressed))return;
            using var g=new D2DGraphics(e.Graphics,e.Item.Width,e.Item.Height);using var b=new SolidBrush(FluentUi.Hover);
            g.FillRoundedRectangle(b,new RectangleF(3,1,e.Item.Width-6,e.Item.Height-2),e.Item.Owner is FluentContextMenuStrip menu?menu.HoverCornerRadius:5f);
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // WinForms also asks to render an empty shortcut over the caption bounds.
            // An opaque cached tile for that empty string would erase the caption.
            if(string.IsNullOrEmpty(e.Text))return;
            float pixelSize=e.TextFont.Unit==GraphicsUnit.Pixel?e.TextFont.Size:e.TextFont.SizeInPoints*e.Graphics.DpiY/72f;
            Rectangle tileBounds=e.TextRectangle;
            if((e.TextFormat&TextFormatFlags.Right)!=0)
            {
                int width=Math.Min(tileBounds.Width,TextRenderer.MeasureText(e.Graphics,e.Text,e.TextFont,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width+6);
                tileBounds.X=tileBounds.Right-width;tileBounds.Width=width;
            }
            Cached(e.Graphics,e.Item,"text:"+e.Text+":"+e.TextFont+":"+e.TextColor.ToArgb()+":"+e.TextFormat,tileBounds,g=>
            {
            using var textFont=new Font(e.TextFont.FontFamily,pixelSize,e.TextFont.Style,GraphicsUnit.Pixel);
            // ToolStrip already resolves accelerator mnemonics and keyboard shortcut placement.
            // Native ToolStrip supplies white HighlightText on selection; our hover is light.
            bool nativeInk=e.TextColor==SystemColors.ControlText||e.TextColor==SystemColors.MenuText||e.TextColor==SystemColors.HighlightText;
            using var b=new SolidBrush(e.Item.Enabled?(nativeInk||e.Item.Selected?FluentUi.Ink:e.TextColor):SystemColors.GrayText);
            using var f=new StringFormat{LineAlignment=StringAlignment.Center,Alignment=(e.TextFormat&TextFormatFlags.Right)!=0?StringAlignment.Far:StringAlignment.Near,FormatFlags=StringFormatFlags.NoWrap,HotkeyPrefix=(e.TextFormat&TextFormatFlags.HidePrefix)!=0?System.Drawing.Text.HotkeyPrefix.Hide:System.Drawing.Text.HotkeyPrefix.Show};
            string text=FluentUi.MnemonicText(e.Text,out int mnemonic);
            g.DrawString(text,textFont,b,e.TextRectangle,f);
            if(mnemonic>=0&&mnemonic<text.Length&&(e.TextFormat&TextFormatFlags.HidePrefix)==0&&(e.TextFormat&TextFormatFlags.Right)==0)
            {
                float left=e.TextRectangle.Left+g.MeasureString(text.Substring(0,mnemonic),textFont).Width;
                float width=g.MeasureString(text.Substring(mnemonic,1),textFont).Width;
                float y=e.TextRectangle.Top+(e.TextRectangle.Height+textFont.GetHeight(g))*.5f-2;
                using var underline=new Pen(e.Item.Enabled?FluentUi.Ink:SystemColors.GrayText);g.DrawLine(underline,left,y,left+Math.Max(2,width-1),y);
            }
                    });
        }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var g=new D2DGraphics(e.Graphics,e.Item.Width,e.Item.Height);using var p=new Pen(Color.FromArgb(232,237,243));g.DrawLine(p,8,e.Item.Height/2,e.Item.Width-8,e.Item.Height/2);
        }
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            using var g=new D2DGraphics(e.Graphics,e.Item.Width,e.Item.Height);var r=e.ArrowRectangle;float x=r.X+r.Width/2f,y=r.Y+r.Height/2f;
            using var pen=new Pen(e.Item.Enabled?FluentUi.Ink:SystemColors.GrayText,1.4f){StartCap=LineCap.Round,EndCap=LineCap.Round};
            g.DrawLines(pen,e.Direction switch{ArrowDirection.Left=>new[]{new PointF(x+2,y-4),new PointF(x-2,y),new PointF(x+2,y+4)},ArrowDirection.Up=>new[]{new PointF(x-4,y+2),new PointF(x,y-2),new PointF(x+4,y+2)},ArrowDirection.Down=>new[]{new PointF(x-4,y-2),new PointF(x,y+2),new PointF(x+4,y-2)},_=>new[]{new PointF(x-2,y-4),new PointF(x+2,y),new PointF(x-2,y+4)}});
        }
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            using var g=new D2DGraphics(e.Graphics,e.Item.Width,e.Item.Height);using var p=new Pen(Color.FromArgb(32,108,186),1.7f){StartCap=LineCap.Round,EndCap=LineCap.Round};
            float x=e.ImageRectangle.X+3,y=e.ImageRectangle.Y+4;
            if(e.Item is ToolStripMenuItem mixed&&mixed.CheckState==CheckState.Indeterminate)g.DrawLine(p,x,y+4,x+9,y+4);
            else g.DrawLines(p,new[]{new PointF(x,y+4),new PointF(x+3,y+7),new PointF(x+10,y)});
        }
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if(e.Image==null)return;
            Cached(e.Graphics,e.Item,"icon:"+RuntimeHelpers.GetHashCode(e.Image),e.ImageRectangle,g=>g.DrawImage(e.Image,e.ImageRectangle,e.Item.Enabled?1f:.4f));
        }
    }

    public sealed class FluentMenuHeading : ToolStripMenuItem
    {
        public FluentMenuHeading(string text,Image image=null):base(text,image)
        {Enabled=false;AutoToolTip=false;AccessibleRole=AccessibleRole.StaticText;}
        public override bool CanSelect=>false;
    }

    public class FluentContextMenuStrip : ContextMenuStrip
    {
        public FluentContextMenuStrip(){Renderer=FluentUi.MenuRenderer;BackColor=Color.White;Padding=new Padding(4);}
        public float CornerRadius{get;set;}=8f;
        public float HoverCornerRadius{get;set;}=5f;
        public Padding ItemPadding{get;set;}=new Padding(2);
        private static readonly ConditionalWeakTable<ToolStrip,object> ConfiguredMenus=new();
        private static readonly ConditionalWeakTable<ToolStripItem,object> ConfiguredItems=new();
        private static readonly ConditionalWeakTable<ToolStrip,ToolStripItem> HoverItems=new();
        private static readonly ConditionalWeakTable<ToolStripItem,string> OriginalItemTexts=new();
        private static string GetOriginalText(ToolStripItem item)
        {
            if(OriginalItemTexts.TryGetValue(item,out string orig))return orig;
            orig=item.Text??"";
            OriginalItemTexts.Add(item,orig);
            return orig;
        }
        private static void LocalizeItems(ToolStripDropDown menu)
        {
            foreach(ToolStripItem item in menu.Items)
            {
                if(item is ToolStripSeparator||item is ToolStripControlHost)continue;
                string orig=GetOriginalText(item);
                string tr=ZenQOffice.Core.AppL10n.Tr(orig);
                if(!string.Equals(item.Text,tr,StringComparison.Ordinal))item.Text=tr;
                if(item is ToolStripMenuItem sub&&sub.HasDropDownItems&&!sub.DropDown.Visible)
                    LocalizeItems(sub.DropDown);
            }
        }
        internal static void PopulateOnDemand(ToolStripMenuItem item,Action populate)
        {
            item.DropDownItems.Add(new ToolStripMenuItem("…"));
            bool populated=false;
            item.DropDownOpening+=(s,e)=>
            {
                if(populated)return;
                item.DropDown.SuspendLayout();
                try{item.DropDownItems.Clear();populate();populated=true;}
                finally{item.DropDown.ResumeLayout(true);}
                Configure(item.DropDown);
            };
        }
        private static void ClipCorners(ToolStrip menu)
        {
            if(menu.IsDisposed||menu.Disposing||menu.Width<2||menu.Height<2)return;
            float radius=menu is FluentContextMenuStrip fluent?fluent.CornerRadius:8f;
            using var path=AiVectorGlassRenderer.CreateRoundedPath(new RectangleF(0,0,menu.Width,menu.Height),radius);
            var previous=menu.Region;menu.Region=new Region(path);previous?.Dispose();
            FluentUi.RoundWindow(menu,radius<=4);
        }
        private static void EnterItem(object sender,EventArgs e)
        {
            if(sender is not ToolStripMenuItem item||!item.Enabled||item.Owner==null||!item.Owner.Visible)return;
            if(HoverItems.TryGetValue(item.Owner,out var previous)&&previous==item&&(!item.HasDropDownItems||item.DropDown.Visible))return;
            HoverItems.Remove(item.Owner);HoverItems.Add(item.Owner,item);
            foreach(ToolStripItem sibling in item.Owner.Items)
                if(sibling!=item&&sibling is ToolStripMenuItem other&&other.HasDropDownItems&&other.DropDown.Visible)other.HideDropDown();
            if(item.HasDropDownItems&&!item.DropDown.Visible)item.ShowDropDown();
        }
        public FluentContextMenuStrip(System.ComponentModel.IContainer container):this(){container?.Add(this);}
        internal static void Configure(ToolStripDropDown menu)
        {
            menu.Renderer=FluentUi.MenuRenderer;menu.BackColor=Color.FromArgb(253,254,255);menu.DropShadowEnabled=false;
            LocalizeItems(menu);
            if(!ConfiguredMenus.TryGetValue(menu,out _))
            {
                ConfiguredMenus.Add(menu,new object());
                menu.SizeChanged+=(s,e)=>ClipCorners(menu);
                menu.HandleCreated+=(s,e)=>ClipCorners(menu);
                menu.Opened+=(s,e)=>{FluentUi.OpenMenus.Add(menu);ClipCorners(menu);};
                menu.Closed+=(s,e)=>{HoverItems.Remove(menu);FluentUi.OpenMenus.Remove(menu);};
                menu.Disposed+=(s,e)=>FluentUi.OpenMenus.Remove(menu);
            }
            foreach(ToolStripItem item in menu.Items)
            {
                if(item is ToolStripSeparator||item is ToolStripControlHost)continue;
                item.Padding=menu is FluentContextMenuStrip root?root.ItemPadding:new Padding(2);
                if(!ConfiguredItems.TryGetValue(item,out _))
                {
                    ConfiguredItems.Add(item,new object());item.MouseEnter+=EnterItem;item.MouseMove+=EnterItem;
                    if(item is ToolStripMenuItem sub)sub.DropDownOpening+=(s,e)=>Configure(sub.DropDown);
                }
            }
        }
        protected override void OnOpening(System.ComponentModel.CancelEventArgs e){base.OnOpening(e);if(!e.Cancel)Configure(this);}
    }

    // Keep native editing, data binding, IME, accessibility and keyboard navigation.
    public class FluentComboBox : System.Windows.Forms.ComboBox
    {
        public FluentComboBox(){FlatStyle=FlatStyle.Flat;DrawMode=DrawMode.OwnerDrawFixed;BackColor=Color.White;ForeColor=FluentUi.Ink;ItemHeight=Math.Max(22,Font.Height+8);}
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            using(var g=new D2DGraphics(e.Graphics,Math.Max(1,Math.Max(Width,e.Bounds.Right)),Math.Max(1,Math.Max(Height,e.Bounds.Bottom))))
                FluentUi.Row(g,e.Bounds,e.Index>=0&&e.Index<Items.Count?GetItemText(Items[e.Index]):Text,e.Font,(e.State&DrawItemState.Selected)!=0,Enabled);
            base.OnDrawItem(e); // Existing owner-drawn font/color previews retain their handlers.
        }
        protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);if(DrawMode==DrawMode.OwnerDrawFixed)ItemHeight=Math.Max(ItemHeight,Font.Height+8);}
        protected override void OnDropDown(EventArgs e)
        {
            base.OnDropDown(e);
            var info=new ComboInfo{Size=Marshal.SizeOf<ComboInfo>()};
            if(GetComboBoxInfo(Handle,ref info)&&info.List!=IntPtr.Zero)
            {
                try{int round=2;DwmSetWindowAttribute(info.List,33,ref round,4);}catch(DllNotFoundException){}
            }
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if(m.Msg!=0xF||Width<24||Height<8)return;
            using var native=CreateGraphics();using var g=new D2DGraphics(native,Width,Height);
            using var fill=new SolidBrush(Enabled?Color.FromArgb(247,249,252):SystemColors.Control);
            g.FillRectangle(fill,new Rectangle(Width-23,1,22,Height-2));
            FluentUi.Chevron(g,new RectangleF(Width-22,0,20,Height),Enabled?Color.FromArgb(95,116,139):SystemColors.GrayText);
            Color surround=Parent?.BackColor??SystemColors.Control;
            for(Control ancestor=Parent;surround.A<255&&ancestor!=null;ancestor=ancestor.Parent)surround=ancestor.BackColor;
            if(surround.A<255)surround=SystemColors.Control;
            using(var erase=new SolidBrush(surround))
            {
                // Remove the native square border first; antialias over the real parent color.
                g.FillRectangle(erase,0,0,Width,1);g.FillRectangle(erase,0,Height-1,Width,1);
                g.FillRectangle(erase,0,0,1,Height);g.FillRectangle(erase,Width-1,0,1,Height);
                using var outside=new GraphicsPath(FillMode.Alternate);outside.AddRectangle(ClientRectangle);
                using var rounded=AiVectorGlassRenderer.CreateRoundedPath(new RectangleF(.5f,.5f,Width-1,Height-1),4);
                outside.AddPath(rounded,false);g.FillPath(erase,outside);
            }
            using var pen=new Pen(Focused?Color.FromArgb(96,156,218):FluentUi.Border);g.DrawRoundedRectangle(pen,new RectangleF(.5f,.5f,Width-1,Height-1),4);
        }
        [StructLayout(LayoutKind.Sequential)]struct ComboInfo{public int Size;public Rectangle Item,Button;public int State;public IntPtr Combo,Edit,List;}
        [DllImport("user32.dll")]static extern bool GetComboBoxInfo(IntPtr hwnd,ref ComboInfo info);
        [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    }
    public class FluentListBox : System.Windows.Forms.ListBox
    {
        protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);if(DrawMode==DrawMode.OwnerDrawFixed)ItemHeight=Math.Max(ItemHeight,Font.Height+8);}
        public FluentListBox(){DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=Math.Max(24,Font.Height+8);BackColor=Color.White;ForeColor=FluentUi.Ink;BorderStyle=BorderStyle.FixedSingle;}
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            using(var g=new D2DGraphics(e.Graphics,Math.Max(1,Math.Max(Width,e.Bounds.Right)),Math.Max(1,Math.Max(Height,e.Bounds.Bottom))))
                FluentUi.Row(g,e.Bounds,e.Index>=0&&e.Index<Items.Count?GetItemText(Items[e.Index]):"",e.Font,(e.State&DrawItemState.Selected)!=0,Enabled);
            base.OnDrawItem(e);
        }
    }
    public class FluentTabControl : System.Windows.Forms.TabControl
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        private int _hoverTab = -1;
        private float _indX = -1f, _indW = -1f, _indTargetX = -1f, _indTargetW = -1f;
        private Timer _animTimer;

        public FluentTabControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Padding = new Point(16, 7);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control is TabPage page && page.BackColor == SystemColors.Control) page.BackColor = Color.White;
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            UpdateIndicatorTarget();
            StartAnimIfNeeded();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int maxR = GetTabStripMaxRight();
            int hit = -1;
            for (int i = 0; i < TabCount; i++)
            {
                var r = GetTabRect(i);
                if (r.Left >= maxR) continue;
                var clipped = new Rectangle(r.X, r.Y, Math.Min(r.Right, maxR) - r.X, r.Height);
                if (clipped.Contains(e.Location)) { hit = i; break; }
            }
            if (hit != _hoverTab)
            {
                _hoverTab = hit;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverTab != -1) { _hoverTab = -1; Invalidate(); }
        }

        private void UpdateIndicatorTarget()
        {
            if (IsHandleCreated && SelectedIndex >= 0 && SelectedIndex < TabCount)
            {
                Rectangle r = GetTabRect(SelectedIndex);
                _indTargetX = r.Left + 10f;
                _indTargetW = Math.Max(12f, r.Width - 20f);
                if (_indX < 0f || _indW < 0f) { _indX = _indTargetX; _indW = _indTargetW; }
            }
        }

        private void StartAnimIfNeeded()
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (Math.Abs(_indTargetX - _indX) <= 0.5f && Math.Abs(_indTargetW - _indW) <= 0.5f) return;
            if (_animTimer == null)
            {
                _animTimer = new Timer { Interval = 15 };
                _animTimer.Tick += (s, ev) =>
                {
                    if (IsDisposed) { _animTimer?.Stop(); return; }
                    float dx = _indTargetX - _indX;
                    float dw = _indTargetW - _indW;
                    if (Math.Abs(dx) <= 0.4f && Math.Abs(dw) <= 0.4f)
                    {
                        _indX = _indTargetX;
                        _indW = _indTargetW;
                        _animTimer.Stop();
                    }
                    else
                    {
                        _indX += dx * 0.28f;
                        _indW += dw * 0.28f;
                    }
                    Invalidate();
                };
            }
            if (!_animTimer.Enabled) _animTimer.Start();
        }

        private int GetTabStripMaxRight()
        {
            if (!IsHandleCreated) return Width - 2;
            IntPtr updown = FindWindowEx(Handle, IntPtr.Zero, "msctls_updown32", null);
            if (updown != IntPtr.Zero && IsWindowVisible(updown) && GetWindowRect(updown, out RECT wr))
            {
                Point pt = PointToClient(new Point(wr.Left, wr.Top));
                if (pt.X > 20 && pt.X < Width) return pt.X - 6;
            }
            return Width - 2;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
                _animTimer = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var g = new D2DGraphics(e.Graphics, Width, Height);
            g.Clear(Parent?.BackColor ?? Color.FromArgb(248, 250, 252));

            Rectangle disp = DisplayRectangle;
            using var pageBg = new SolidBrush(Color.White);
            using var border = new Pen(Color.FromArgb(226, 232, 240));
            RectangleF pageBox = new RectangleF(disp.X - 2f, disp.Y - 1f, disp.Width + 3f, disp.Height + 2f);
            g.FillRoundedRectangle(pageBg, pageBox, 8f);
            g.DrawRoundedRectangle(border, pageBox, 8f);

            UpdateIndicatorTarget();
            int maxRight = GetTabStripMaxRight();
            int stripBottom = disp.Y;

            g.PushClip(new Rectangle(2, 0, Math.Max(10, maxRight - 2), Math.Max(24, stripBottom + 2)));
            using var boldFont = new Font(Font.FontFamily, Font.Size, FontStyle.Bold);
            for (int i = 0; i < TabCount; i++)
            {
                var r = GetTabRect(i);
                if (r.Left >= maxRight) continue;
                bool selected = (i == SelectedIndex);
                bool hovered = (i == _hoverTab && !selected);
                float tabW = Math.Min(r.Right - 2f, maxRight - 1f) - (r.X + 2f);
                if (tabW < 10f) continue;
                RectangleF tabRect = new RectangleF(r.X + 2f, r.Y + 2f, tabW, r.Height - 4f);

                if (selected)
                {
                    using var fill = new SolidBrush(Color.White);
                    using var tabPen = new Pen(Color.FromArgb(226, 232, 240));
                    g.FillRoundedRectangle(fill, tabRect, 6f);
                    g.DrawRoundedRectangle(tabPen, tabRect, 6f);
                }
                else if (hovered)
                {
                    using var fill = new SolidBrush(Color.FromArgb(234, 240, 248));
                    g.FillRoundedRectangle(fill, tabRect, 6f);
                }

                Color txtColor = selected
                    ? Color.FromArgb(37, 99, 235)
                    : (hovered ? Color.FromArgb(15, 23, 42) : Color.FromArgb(100, 116, 139));
                string caption = FluentUi.MnemonicText(TabPages[i].Text, out _);
                FluentUi.Text(g, caption, selected ? boldFont : Font, txtColor, tabRect, true);
            }

            if (_indX >= 0f && _indW > 0f && SelectedIndex >= 0 && SelectedIndex < TabCount)
            {
                Rectangle selRect = GetTabRect(SelectedIndex);
                float lineY = selRect.Bottom - 4f;
                using var indBrush = new SolidBrush(Color.FromArgb(37, 99, 235));
                g.FillRoundedRectangle(indBrush, new RectangleF(_indX, lineY, _indW, 2.5f), 1.25f);
            }
            g.PopClip();
        }
    }
}
