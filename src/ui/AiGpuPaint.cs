using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    // Each AI surface owns a D3D11/D2D target; GpuEngine selects WARP when needed.
    // WinForms still hosts native edit controls, but never rasterizes these custom visuals.
    internal sealed class AiGpuPaint : IDisposable
    {
        private static readonly ConditionalWeakTable<Control, TargetLifetime> Registered = new();
        private sealed class TargetLifetime { public IntPtr Handle; public int FailedFrames; }
        private readonly GpuHwndSurface _surface;
        private readonly Control _control;
        private readonly PaintEventArgs _paint;
        public D2DGraphics Graphics { get; }

        private AiGpuPaint(Control control, PaintEventArgs e)
        {
            _control=control;_paint=e;
            if (control.IsHandleCreated)
            {
                if (!Registered.TryGetValue(control, out var lifetime))
                {
                    lifetime = new TargetLifetime();
                    Registered.Add(control, lifetime);
                    control.HandleDestroyed += (s, args) => { GpuEngine.DestroySurface(lifetime.Handle); lifetime.Handle = IntPtr.Zero; };
                }
                lifetime.Handle = control.Handle;
                try
                {
                    _surface = GpuEngine.GetOrCreateSurface(control.Handle, control.Width, control.Height);
                    Graphics = D2DGraphics.FromGpuSurface(_surface);
                }
                catch { _surface = null; }
            }
            Graphics ??= new D2DGraphics(e.Graphics, control.Width, control.Height);
            Color background = control.BackColor;
            for (Control parent = control.Parent; background.A < 255 && parent != null; parent = parent.Parent)
                background = parent.BackColor;
            Graphics.Clear(background.A == 255 ? background : Color.FromArgb(248, 250, 252));
            if (control.Parent is AiChatSidebarPanel sidebar && sidebar.UsesRoundedFrame &&
                (control.Top == 0 || control.Bottom == sidebar.Height))
            {
                Graphics.Clear(sidebar.Parent?.BackColor ?? Color.FromArgb(240, 241, 243));
                using var outline = CreateSidebarChildOutline(control, sidebar, 1f);
                using var fill = new SolidBrush(background);
                Graphics.FillPath(fill, outline);
                // Clip every subsequent draw as well, including Canvas's opaque background.
                Graphics.SetClip(outline, CombineMode.Intersect);
            }
        }

        public static AiGpuPaint Begin(Control control, PaintEventArgs e) => new(control, e);

        internal static GraphicsPath CreateSidebarChildOutline(Control child, AiChatSidebarPanel sidebar, float inset)
        {
            var bounds = new RectangleF(inset, inset, Math.Max(1, child.Width - inset * 2), Math.Max(1, child.Height - inset * 2));
            float radius = Math.Min(12f - inset, Math.Min(bounds.Width, bounds.Height) / 2f);
            var path = AiVectorGlassRenderer.CreateRoundedPath(bounds, radius);
            path.FillMode = FillMode.Winding;
            bool left = child.Left == 0, right = child.Right == sidebar.Width;
            bool top = child.Top == 0, bottom = child.Bottom == sidebar.Height;
            if (!(left && top)) path.AddRectangle(new RectangleF(bounds.Left, bounds.Top, radius, radius));
            if (!(right && top)) path.AddRectangle(new RectangleF(bounds.Right - radius, bounds.Top, radius, radius));
            if (!(left && bottom)) path.AddRectangle(new RectangleF(bounds.Left, bounds.Bottom - radius, radius, radius));
            if (!(right && bottom)) path.AddRectangle(new RectangleF(bounds.Right - radius, bounds.Bottom - radius, radius, radius));
            return path;
        }

        public static void DrawText(D2DGraphics g, string text, Font font, Rectangle bounds, Color color, TextFormatFlags flags)
        {
            using var brush = new SolidBrush(color);
            using var format = new StringFormat
            {
                Alignment = (flags & TextFormatFlags.HorizontalCenter) != 0 ? StringAlignment.Center : StringAlignment.Near,
                LineAlignment = (flags & TextFormatFlags.VerticalCenter) != 0 ? StringAlignment.Center : StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            g.DrawString(text, font, brush, bounds, format);
        }

        public void Dispose()
        {
            Graphics.Dispose();
            bool presented = _surface == null || _surface.TryPresent(vsync:false);
            if(Registered.TryGetValue(_control,out var lifetime))
            {
                if(Graphics.LastDrawFailed || !presented)
                {
                    GpuEngine.DestroySurface(lifetime.Handle);lifetime.Handle=IntPtr.Zero;
                    Color background=_control.BackColor;for(Control parent=_control.Parent;background.A<255&&parent!=null;parent=parent.Parent)background=parent.BackColor;
                    _paint.Graphics.Clear(background.A==255?background:Color.FromArgb(248,250,252));
                    if(++lifetime.FailedFrames<=2 && !_control.IsDisposed && _control.IsHandleCreated)_control.BeginInvoke(new Action(()=>{if(!_control.IsDisposed)_control.Invalidate();}));
                }
                else lifetime.FailedFrames=0;
            }
        }
    }

    public class AiGpuControl : Control
    {
        public AiGpuControl() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        protected override void OnPaintBackground(PaintEventArgs e) { }
    }

    public class AiGpuPanel : Panel
    {
        public AiGpuPanel() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            using (var frame = AiGpuPaint.Begin(this, e)) { }
            base.OnPaint(e);
        }
    }

    public class AiGpuButton : Button
    {
        public AiGpuButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
        }
    }
}
