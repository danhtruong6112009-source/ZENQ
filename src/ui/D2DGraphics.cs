using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DCommon;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using PointF = System.Drawing.PointF;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using D2DTextAntialiasMode = Vortice.Direct2D1.TextAntialiasMode;

namespace ZenQOffice.UI
{
    public class D2DGraphics : IDisposable, System.Drawing.IDeviceContext
    {
        public IntPtr GetHdc()
        {
            if (_gdiInterop != null && _renderTarget != null && _isDrawing)
            {
                if (!_inGdiDc)
                {
                    try
                    {
                        _renderTarget.Flush(out _, out _);
                        _interopHdc = _gdiInterop.GetDC(DcInitializeMode.Copy);
                        _inGdiDc = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("GdiInterop GetDC error: " + ex.Message);
                    }
                }
                return _interopHdc;
            }

            if (_renderTarget != null && _isDrawing)
            {
                try { _renderTarget.Flush(out _, out _); } catch { }
            }
            return _gdiHdc;
        }

        public void ReleaseHdc()
        {
            if (_gdiInterop != null && _inGdiDc)
            {
                try
                {
                    _gdiInterop.ReleaseDC(null);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("GdiInterop ReleaseDC error: " + ex.Message);
                }
                _inGdiDc = false;
                _interopHdc = IntPtr.Zero;
            }
        }

        private static readonly ID2D1Factory _d2dFactory;
        private static readonly IDWriteFactory _dwriteFactory;
        private static readonly IDWriteFactory5 _dwriteFactory5;
        private static IDWriteFontCollection _customFontCollection;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, IDWriteTextFormat> _textFormatCache = new System.Collections.Concurrent.ConcurrentDictionary<string, IDWriteTextFormat>();
        public static ID2D1Factory D2DFactory => _d2dFactory;

        static D2DGraphics()
        {
            _d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory>();
            _dwriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
            try { _dwriteFactory5 = DWrite.DWriteCreateFactory<IDWriteFactory5>(); } catch { }
        }


        [DllImport("gdi32.dll")]
        private static extern IntPtr GetCurrentObject(IntPtr hdc, uint uObjectType);

        [DllImport("gdi32.dll")]
        private static extern int GetObject(IntPtr hgdiobj, int cbBuffer, out BITMAP lpvObject);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }

        private const uint OBJ_BITMAP = 7;
        private const int HORZRES = 8;
        private const int VERTRES = 10;

        private ID2D1RenderTarget _renderTarget;
        private ID2D1HwndRenderTarget _hwndRenderTarget;
        private ID2D1GdiInteropRenderTarget _gdiInterop;
        private bool _isUsingSharedDc = false;
        private bool _inGdiDc = false;
        private IntPtr _interopHdc = IntPtr.Zero;
        private IntPtr _gdiHdc = IntPtr.Zero;
        private bool _ownsGdiHdc = false;
        private bool _ownsHdc => _ownsGdiHdc;
        private bool _isDrawing = false;
        private Graphics _gdiGraphics;
        private bool _ownsGraphics = false;
        private float _dpiX = 96.0f;
        private float _dpiY = 96.0f;

        public D2DGraphics()
        {
        }

        public D2DGraphics(Graphics g, bool pureGdi)
        {
            _gdiGraphics = g;
            _ownsGraphics = false;
            _ownsGdiHdc = false;
            _renderTarget = null;
            if (g != null)
            {
                try { _dpiX = g.DpiX; _dpiY = g.DpiY; } catch { }
            }
        }

        public D2DGraphics(Graphics g, int viewportWidth = 0, int viewportHeight = 0)
        {
            _gdiGraphics = g;
            _ownsGraphics = false;
            _ownsGdiHdc = false;
            _renderTarget = null;
            if (g != null)
            {
                try { _dpiX = g.DpiX; _dpiY = g.DpiY; } catch { }
            }
        }

        public D2DGraphics(IntPtr hwnd, int width, int height)
        {
            if (hwnd != IntPtr.Zero && width > 0 && height > 0)
            {
                try
                {
                    var props = new RenderTargetProperties(
                        RenderTargetType.Default,
                        new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                        96.0f, 96.0f,
                        RenderTargetUsage.GdiCompatible,
                        FeatureLevel.Default
                    );
                    var hwndProps = new HwndRenderTargetProperties
                    {
                        Hwnd = hwnd,
                        PixelSize = new Vortice.Mathematics.SizeI(Math.Max(1, width), Math.Max(1, height)),
                        PresentOptions = PresentOptions.Immediately
                    };
                    _hwndRenderTarget = _d2dFactory.CreateHwndRenderTarget(props, hwndProps);
                    _renderTarget = _hwndRenderTarget;
                    try { _gdiInterop = _hwndRenderTarget.QueryInterface<ID2D1GdiInteropRenderTarget>(); } catch { }
                    _renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
                    _renderTarget.TextAntialiasMode = D2DTextAntialiasMode.Cleartype;
                    _renderTarget.BeginDraw();
                    _isDrawing = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("D2D HWND fallback: " + ex.Message);
                    _gdiGraphics = Graphics.FromHwnd(hwnd);
                    _ownsGraphics = true;
                }
            }
        }

        public void Resize(int width, int height)
        {
            if (_hwndRenderTarget != null && width > 0 && height > 0)
            {
                try
                {
                    if (_inGdiDc) ReleaseHdc();
                    if (_isDrawing)
                    {
                        try { _renderTarget.EndDraw(); } catch { }
                        _isDrawing = false;
                    }
                    _hwndRenderTarget.Resize(new Vortice.Mathematics.SizeI(width, height));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("D2D Resize error: " + ex.Message);
                }
            }
        }

        public void DrawGdi(Action<Graphics> drawAction)
        {
            if (drawAction == null) return;
            if (_gdiInterop != null && _renderTarget != null && _isDrawing)
            {
                IntPtr hdc = GetHdc();
                if (hdc != IntPtr.Zero)
                {
                    try
                    {
                        using (Graphics g = Graphics.FromHdc(hdc))
                        {
                            drawAction(g);
                        }
                    }
                    finally
                    {
                        ReleaseHdc();
                    }
                    return;
                }
            }

            if (_gdiGraphics != null)
            {
                drawAction(_gdiGraphics);
            }
        }

        public void BeginDraw()
        {
            if (_renderTarget != null && !_isDrawing)
            {
                _renderTarget.BeginDraw();
                _isDrawing = true;
            }
        }

        public void EndDraw()
        {
            if (_inGdiDc)
            {
                ReleaseHdc();
            }
            if (_renderTarget != null && _isDrawing)
            {
                _renderTarget.EndDraw();
                _isDrawing = false;
            }
        }

        public void Dispose()
        {
            while (_clipDepth > 0 && _renderTarget != null)
            {
                try { _renderTarget.PopAxisAlignedClip(); } catch { }
                _clipDepth--;
            }

            if (_inGdiDc && _gdiInterop != null)
            {
                try { _gdiInterop.ReleaseDC(null); } catch { }
                _inGdiDc = false;
                _interopHdc = IntPtr.Zero;
            }

            if (_isDrawing && _renderTarget != null)
            {
                try 
                { 
                    _renderTarget.EndDraw(); 
                } 
                catch 
                { 
                }
                _isDrawing = false;
            }

            if (_gdiInterop != null)
            {
                try { _gdiInterop.Dispose(); } catch { }
                _gdiInterop = null;
            }

            if (_ownsGdiHdc && _gdiGraphics != null && _gdiHdc != IntPtr.Zero)
            {
                try { _gdiGraphics.ReleaseHdc(_gdiHdc); } catch { }
                _gdiHdc = IntPtr.Zero;
                _ownsGdiHdc = false;
            }

            if (_hwndRenderTarget != null)
            {
                _hwndRenderTarget.Dispose();
                _hwndRenderTarget = null;
            }
            _renderTarget = null;

            foreach (var bmp in _bitmapCache.Values)
            {
                try { bmp?.Dispose(); } catch { }
            }
            _bitmapCache.Clear();

            if (_ownsGraphics && _gdiGraphics != null)
            {
                _gdiGraphics.Dispose();
                _gdiGraphics = null;
            }
        }

        public static implicit operator D2DGraphics(Graphics g) 
        {
            if (g == null) return null;
            return new D2DGraphics(g, pureGdi: true);
        }

        public Graphics UnderlyingGraphics 
        {
            get
            {
                return _gdiGraphics;
            }
        }

        // --- Direct2D & DirectWrite Hardware Helpers ---
        private static Vortice.Mathematics.Color4 ToColor4(Color c)
        {
            return new Vortice.Mathematics.Color4(c.R / 255.0f, c.G / 255.0f, c.B / 255.0f, c.A / 255.0f);
        }

        private ID2D1Brush GetD2DBrush(Brush brush)
        {
            if (_renderTarget == null || brush == null) return null;
            if (brush is SolidBrush sb)
            {
                return _renderTarget.CreateSolidColorBrush(ToColor4(sb.Color));
            }
            if (brush is LinearGradientBrush lgb)
            {
                GradientStop[] stops = null;
                try
                {
                    var blend = lgb.InterpolationColors;
                    if (blend != null && blend.Colors != null && blend.Colors.Length >= 2 && blend.Positions != null && blend.Positions.Length == blend.Colors.Length)
                    {
                        stops = new GradientStop[blend.Colors.Length];
                        for (int i = 0; i < blend.Colors.Length; i++)
                        {
                            stops[i] = new GradientStop
                            {
                                Position = Math.Max(0.0f, Math.Min(1.0f, blend.Positions[i])),
                                Color = ToColor4(blend.Colors[i])
                            };
                        }
                    }
                }
                catch { }

                if (stops == null || stops.Length < 2)
                {
                    var colors = lgb.LinearColors;
                    stops = new GradientStop[]
                    {
                        new GradientStop { Position = 0.0f, Color = ToColor4(colors[0]) },
                        new GradientStop { Position = 1.0f, Color = ToColor4(colors[1]) }
                    };
                }

                ExtendMode extMode = ExtendMode.Clamp;
                try
                {
                    if (lgb.WrapMode == WrapMode.TileFlipX || lgb.WrapMode == WrapMode.TileFlipY || lgb.WrapMode == WrapMode.TileFlipXY)
                        extMode = ExtendMode.Mirror;
                }
                catch { }

                using var stopCollection = _renderTarget.CreateGradientStopCollection(stops, Gamma.StandardRgb, extMode);
                var r = lgb.Rectangle;
                PointF[] pts = new PointF[]
                {
                    new PointF(r.Left, r.Top + r.Height / 2f),
                    new PointF(r.Right, r.Top + r.Height / 2f)
                };
                try
                {
                    if (lgb.Transform != null)
                    {
                        lgb.Transform.TransformPoints(pts);
                    }
                }
                catch { }

                return _renderTarget.CreateLinearGradientBrush(
                    new LinearGradientBrushProperties
                    {
                        StartPoint = new System.Numerics.Vector2(pts[0].X, pts[0].Y),
                        EndPoint = new System.Numerics.Vector2(pts[1].X, pts[1].Y)
                    },
                    stopCollection
                );
            }
            if (brush is PathGradientBrush pgb)
            {
                GradientStop[] stops = null;
                try
                {
                    var blend = pgb.InterpolationColors;
                    if (blend != null && blend.Colors != null && blend.Colors.Length >= 2 && blend.Positions != null && blend.Positions.Length == blend.Colors.Length)
                    {
                        int n = blend.Colors.Length;
                        stops = new GradientStop[n];
                        // In GDI+, position 0.0 is boundary and 1.0 is center.
                        // In Direct2D, position 0.0 is center and 1.0 is boundary.
                        // Reverse stops so Direct2D correctly maps 0.0 (center) to 1.0 (boundary).
                        for (int i = 0; i < n; i++)
                        {
                            int srcIdx = n - 1 - i;
                            stops[i] = new GradientStop
                            {
                                Position = Math.Max(0.0f, Math.Min(1.0f, 1.0f - blend.Positions[srcIdx])),
                                Color = ToColor4(blend.Colors[srcIdx])
                            };
                        }
                    }
                }
                catch { }

                if (stops == null || stops.Length < 2)
                {
                    Color centerC = pgb.CenterColor;
                    Color surroundC = (pgb.SurroundColors != null && pgb.SurroundColors.Length > 0) ? pgb.SurroundColors[0] : Color.Transparent;
                    stops = new GradientStop[]
                    {
                        new GradientStop { Position = 0.0f, Color = ToColor4(centerC) },
                        new GradientStop { Position = 1.0f, Color = ToColor4(surroundC) }
                    };
                }

                ExtendMode extMode = ExtendMode.Clamp;
                try
                {
                    if (pgb.WrapMode == WrapMode.Tile) extMode = ExtendMode.Wrap;
                    else if (pgb.WrapMode == WrapMode.TileFlipX || pgb.WrapMode == WrapMode.TileFlipY || pgb.WrapMode == WrapMode.TileFlipXY) extMode = ExtendMode.Mirror;
                }
                catch { }

                using var stopCollection = _renderTarget.CreateGradientStopCollection(stops, Gamma.StandardRgb, extMode);
                var r = pgb.Rectangle;
                PointF cp = pgb.CenterPoint;
                try
                {
                    if (pgb.Transform != null && !pgb.Transform.IsIdentity)
                    {
                        PointF[] pts = new PointF[] { cp };
                        pgb.Transform.TransformPoints(pts);
                        cp = pts[0];
                    }
                }
                catch { }

                return _renderTarget.CreateRadialGradientBrush(
                    new RadialGradientBrushProperties
                    {
                        Center = new System.Numerics.Vector2(cp.X, cp.Y),
                        GradientOriginOffset = System.Numerics.Vector2.Zero,
                        RadiusX = Math.Max(1.0f, r.Width / 2.0f),
                        RadiusY = Math.Max(1.0f, r.Height / 2.0f)
                    },
                    stopCollection
                );
            }
            if (brush is TextureBrush tb && tb.Image != null)
            {
                var d2dBmp = GetOrCreateD2DBitmap(tb.Image);
                if (d2dBmp != null)
                {
                    return _renderTarget.CreateBitmapBrush(d2dBmp, new BitmapBrushProperties
                    {
                        ExtendModeX = ExtendMode.Wrap,
                        ExtendModeY = ExtendMode.Wrap,
                        InterpolationMode = BitmapInterpolationMode.Linear
                    });
                }
            }
            if (brush is HatchBrush hb)
            {
                return _renderTarget.CreateSolidColorBrush(ToColor4(hb.ForegroundColor));
            }
            return _renderTarget.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0, 0, 0, 1));
        }

        private (ID2D1Brush brush, float width, ID2D1StrokeStyle strokeStyle) GetD2DPen(Pen pen)
        {
            if (_renderTarget == null || pen == null) return (null, 1.0f, null);
            ID2D1Brush b = _renderTarget.CreateSolidColorBrush(ToColor4(pen.Color));
            ID2D1StrokeStyle stroke = null;
            if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dash)
            {
                stroke = _d2dFactory.CreateStrokeStyle(new StrokeStyleProperties { DashStyle = Vortice.Direct2D1.DashStyle.Dash });
            }
            else if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dot)
            {
                stroke = _d2dFactory.CreateStrokeStyle(new StrokeStyleProperties { DashStyle = Vortice.Direct2D1.DashStyle.Dot });
            }
            return (b, Math.Max(1.0f, pen.Width), stroke);
        }

        private static readonly HashSet<string> _registeredFontFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _customFamilyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _customFamilyAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void RegisterCustomFamilyName(string familyName, string targetFamilyName = null)
        {
            if (string.IsNullOrEmpty(familyName)) return;
            lock (_customFamilyNames)
            {
                _customFamilyNames.Add(familyName);
            }
            if (!string.IsNullOrEmpty(targetFamilyName) && !string.Equals(familyName, targetFamilyName, StringComparison.OrdinalIgnoreCase))
            {
                lock (_customFamilyAliases)
                {
                    bool targetValid = (_customFontCollection != null && _customFontCollection.FindFamilyName(targetFamilyName, out uint _));
                    if (targetValid || !_customFamilyAliases.ContainsKey(familyName))
                    {
                        _customFamilyAliases[familyName] = targetFamilyName;
                    }
                }
            }
        }

        public static void RegisterCustomFontFile(string filePath, string typeface = null)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;
            if (_dwriteFactory5 == null) return;

            lock (_registeredFontFiles)
            {
                bool isNew = !_registeredFontFiles.Contains(filePath);
                if (isNew)
                {
                    _registeredFontFiles.Add(filePath);
                }

                string baseName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                lock (_customFamilyNames)
                {
                    _customFamilyNames.Add(baseName);
                    if (!string.IsNullOrEmpty(typeface)) _customFamilyNames.Add(typeface);
                }

                // Discover DirectWrite family names inside this font file for aliasing
                try
                {
                    using var fileRef = _dwriteFactory.CreateFontFileReference(filePath);
                    using var singleBuilder = _dwriteFactory5.CreateFontSetBuilder();
                    singleBuilder.AddFontFile(fileRef);
                    using var singleSet = singleBuilder.CreateFontSet();
                    using var singleColl = _dwriteFactory5.CreateFontCollectionFromFontSet(singleSet);
                    if (singleColl != null && singleColl.FontFamilyCount > 0)
                    {
                        var fam = singleColl.GetFontFamily(0);
                        string primaryDwName = fam.FamilyNames.GetString(0);
                        lock (_customFamilyAliases)
                        {
                            if (!string.IsNullOrEmpty(typeface))
                            {
                                _customFamilyAliases[typeface] = primaryDwName;
                            }
                            _customFamilyAliases[primaryDwName] = primaryDwName;
                            _customFamilyAliases[baseName] = primaryDwName;
                            for (uint j = 0; j < (uint)fam.FamilyNames.Count; j++)
                            {
                                string fn = fam.FamilyNames.GetString(j);
                                _customFamilyAliases[fn] = primaryDwName;
                                lock (_customFamilyNames) { _customFamilyNames.Add(fn); }
                            }
                        }
                    }
                }
                catch { }

                if (isNew)
                {
                    try
                    {
                        using var builder = _dwriteFactory5.CreateFontSetBuilder();
                        foreach (var file in _registeredFontFiles)
                        {
                            if (System.IO.File.Exists(file))
                            {
                                try
                                {
                                    using var fontRef = _dwriteFactory.CreateFontFileReference(file);
                                    builder.AddFontFile(fontRef);
                                }
                                catch { }
                            }
                        }
                        using var fontSet = builder.CreateFontSet();
                        var newCollection = _dwriteFactory5.CreateFontCollectionFromFontSet(fontSet);
                        _customFontCollection?.Dispose();
                        _customFontCollection = newCollection;
                        _textFormatCache.Clear();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("DirectWrite RegisterCustomFontFile error: " + ex.Message);
                    }
                }
            }
        }

        public static void ClearFontCache()
        {
            _textFormatCache.Clear();
        }

        private static IDWriteTextFormat GetTextFormat(Font font)
        {
            if (font == null || _dwriteFactory == null) return null;
            string famName = font.FontFamily?.Name ?? font.Name;
            string fontName = font.Name ?? famName;
            string key = $"{famName}_{fontName}_{font.Size}_{(int)font.Style}";
            return _textFormatCache.GetOrAdd(key, _ =>
            {
                var style = font.Italic ? Vortice.DirectWrite.FontStyle.Italic : Vortice.DirectWrite.FontStyle.Normal;
                float fontSizeDip = font.SizeInPoints * (96.0f / 72.0f);

                // Determine effective weight from style and font name keywords
                var weight = font.Bold ? FontWeight.Bold : FontWeight.Normal;
                string combinedName = (famName + " " + fontName);
                if (combinedName.IndexOf("Heavy", StringComparison.OrdinalIgnoreCase) >= 0 || combinedName.IndexOf("Black", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    weight = FontWeight.Black;
                }
                else if (combinedName.IndexOf("ExtraBold", StringComparison.OrdinalIgnoreCase) >= 0 || combinedName.IndexOf("UltraBold", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    weight = FontWeight.ExtraBold;
                }
                else if (combinedName.IndexOf("SemiBold", StringComparison.OrdinalIgnoreCase) >= 0 || combinedName.IndexOf("DemiBold", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    weight = FontWeight.SemiBold;
                }
                else if (combinedName.IndexOf("Medium", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    weight = FontWeight.Medium;
                }
                else if (combinedName.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    weight = FontWeight.Light;
                }
                else if (combinedName.IndexOf("Thin", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    weight = FontWeight.Thin;
                }
                else if (font.Bold && weight < FontWeight.Bold)
                {
                    weight = FontWeight.Bold;
                }

                // Try to resolve custom font in _customFontCollection
                if (_customFontCollection != null)
                {
                    string resolvedDwFam = null;

                    // 1. Check alias dictionary
                    lock (_customFamilyAliases)
                    {
                        if (_customFamilyAliases.TryGetValue(famName, out string a1)) resolvedDwFam = a1;
                        else if (_customFamilyAliases.TryGetValue(fontName, out string a2)) resolvedDwFam = a2;
                    }
                    if (resolvedDwFam != null && !_customFontCollection.FindFamilyName(resolvedDwFam, out uint _))
                    {
                        resolvedDwFam = null;
                    }

                    // 2. Direct match in _customFontCollection
                    if (resolvedDwFam == null)
                    {
                        if (_customFontCollection.FindFamilyName(famName, out uint famIndex1)) resolvedDwFam = famName;
                        else if (_customFontCollection.FindFamilyName(fontName, out uint famIndex2)) resolvedDwFam = fontName;
                    }

                    // 3. Fuzzy match against all families in _customFontCollection
                    if (resolvedDwFam == null && (_customFamilyNames.Contains(famName) || _customFamilyNames.Contains(fontName)))
                    {
                        uint count = _customFontCollection.FontFamilyCount;
                        for (uint i = 0; i < count; i++)
                        {
                            var cf = _customFontCollection.GetFontFamily(i);
                            for (uint j = 0; j < (uint)cf.FamilyNames.Count; j++)
                            {
                                string cand = cf.FamilyNames.GetString(j);
                                if (famName.IndexOf(cand, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    cand.IndexOf(famName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    fontName.IndexOf(cand, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    cand.IndexOf(fontName, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    resolvedDwFam = cand;
                                    lock (_customFamilyAliases)
                                    {
                                        _customFamilyAliases[famName] = cand;
                                        _customFamilyAliases[fontName] = cand;
                                    }
                                    break;
                                }
                            }
                            if (resolvedDwFam != null) break;
                        }
                    }

                    if (resolvedDwFam != null)
                    {
                        try
                        {
                            var customFmt = _dwriteFactory.CreateTextFormat(
                                resolvedDwFam,
                                _customFontCollection,
                                weight,
                                style,
                                FontStretch.Normal,
                                fontSizeDip
                            );
                            if (customFmt != null) return customFmt;
                        }
                        catch { }
                    }
                }

                // Standard system font lookup
                try
                {
                    return _dwriteFactory.CreateTextFormat(
                        famName,
                        null,
                        weight,
                        style,
                        FontStretch.Normal,
                        fontSizeDip
                    );
                }
                catch { }

                try
                {
                    return _dwriteFactory.CreateTextFormat(
                        fontName,
                        null,
                        weight,
                        style,
                        FontStretch.Normal,
                        fontSizeDip
                    );
                }
                catch { }

                // Fallback to Segoe UI
                return _dwriteFactory.CreateTextFormat(
                    "Segoe UI",
                    null,
                    weight,
                    style,
                    FontStretch.Normal,
                    fontSizeDip
                );
            });
        }

        public static ID2D1PathGeometry ConvertPath(GraphicsPath path)
        {
            if (path == null || _d2dFactory == null || path.PointCount == 0) return null;
            try
            {
                var geometry = _d2dFactory.CreatePathGeometry();
                using var sink = geometry.Open();
                if (path.FillMode == System.Drawing.Drawing2D.FillMode.Winding)
                {
                    sink.SetFillMode(Vortice.Direct2D1.FillMode.Winding);
                }
                else
                {
                    sink.SetFillMode(Vortice.Direct2D1.FillMode.Alternate);
                }

                var points = path.PathPoints;
                var types = path.PathTypes;
                bool figureStarted = false;

                for (int i = 0; i < points.Length; i++)
                {
                    byte rawType = types[i];
                    byte type = (byte)(rawType & 0x07);
                    bool isClose = (rawType & 0x80) != 0;
                    var pt = new System.Numerics.Vector2(points[i].X, points[i].Y);

                    if (type == 0) // Start point
                    {
                        if (figureStarted)
                        {
                            sink.EndFigure(FigureEnd.Open);
                            figureStarted = false;
                        }
                        sink.BeginFigure(pt, FigureBegin.Filled);
                        figureStarted = true;
                    }
                    else if (type == 1) // Line point
                    {
                        sink.AddLine(pt);
                    }
                    else if (type == 3 && i + 2 < points.Length) // Bezier
                    {
                        bool bezierClose = (types[i + 2] & 0x80) != 0;
                        sink.AddBezier(new BezierSegment
                        {
                            Point1 = pt,
                            Point2 = new System.Numerics.Vector2(points[i + 1].X, points[i + 1].Y),
                            Point3 = new System.Numerics.Vector2(points[i + 2].X, points[i + 2].Y)
                        });
                        i += 2;
                        if (bezierClose && figureStarted)
                        {
                            sink.EndFigure(FigureEnd.Closed);
                            figureStarted = false;
                        }
                        continue;
                    }

                    if (isClose && figureStarted)
                    {
                        sink.EndFigure(FigureEnd.Closed);
                        figureStarted = false;
                    }
                }

                if (figureStarted)
                {
                    sink.EndFigure(FigureEnd.Open);
                }
                sink.Close();
                return geometry;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("D2D ConvertPath error: " + ex.Message);
                return null;
            }
        }

        private readonly Dictionary<Image, ID2D1Bitmap> _bitmapCache = new Dictionary<Image, ID2D1Bitmap>();

        // Static hardware bitmap cache shared across frames when using _sharedDcRenderTarget
        private static readonly Dictionary<Image, ID2D1Bitmap> _sharedBitmapCache = new Dictionary<Image, ID2D1Bitmap>();
        private static readonly LinkedList<Image> _sharedBitmapLruList = new LinkedList<Image>();
        private static readonly object _sharedBitmapCacheLock = new object();
        private const int MaxSharedBitmapCacheSize = 256;

        public static void InvalidateBitmapCache(Image image)
        {
            if (image == null) return;
            lock (_sharedBitmapCacheLock)
            {
                if (_sharedBitmapCache.TryGetValue(image, out var d2dBmp))
                {
                    _sharedBitmapCache.Remove(image);
                    _sharedBitmapLruList.Remove(image);
                    try { d2dBmp?.Dispose(); } catch { }
                }
            }
        }

        public static void ClearSharedBitmapCache()
        {
            lock (_sharedBitmapCacheLock)
            {
                foreach (var bmp in _sharedBitmapCache.Values)
                {
                    try { bmp?.Dispose(); } catch { }
                }
                _sharedBitmapCache.Clear();
                _sharedBitmapLruList.Clear();
            }
        }

        private ID2D1Bitmap GetOrCreateD2DBitmap(Image image)
        {
            if (image == null || _renderTarget == null) return null;

            if (_isUsingSharedDc)
            {
                lock (_sharedBitmapCacheLock)
                {
                    if (_sharedBitmapCache.TryGetValue(image, out var cachedShared))
                    {
                        _sharedBitmapLruList.Remove(image);
                        _sharedBitmapLruList.AddLast(image);
                        return cachedShared;
                    }
                }
            }
            else
            {
                if (_bitmapCache.TryGetValue(image, out var cached)) return cached;
            }

            ID2D1Bitmap createdBmp = null;

            if (image is Bitmap bmp)
            {
                try
                {
                    bool is32Rgb = (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    if (is32Rgb)
                    {
                        using (var clone = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                        {
                            var srcData = bmp.LockBits(
                                new Rectangle(0, 0, bmp.Width, bmp.Height),
                                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                System.Drawing.Imaging.PixelFormat.Format32bppRgb
                            );
                            var dstData = clone.LockBits(
                                new Rectangle(0, 0, clone.Width, clone.Height),
                                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                            );
                            try
                            {
                                unsafe
                                {
                                    int w = bmp.Width;
                                    int h = bmp.Height;
                                    for (int y = 0; y < h; y++)
                                    {
                                        uint* pSrc = (uint*)IntPtr.Add(srcData.Scan0, y * srcData.Stride);
                                        uint* pDst = (uint*)IntPtr.Add(dstData.Scan0, y * dstData.Stride);
                                        for (int x = 0; x < w; x++)
                                        {
                                            pDst[x] = pSrc[x] | 0xFF000000;
                                        }
                                    }
                                }
                                var props = new BitmapProperties(new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
                                createdBmp = _renderTarget.CreateBitmap(
                                    new Vortice.Mathematics.SizeI(clone.Width, clone.Height),
                                    dstData.Scan0,
                                    (uint)dstData.Stride,
                                    props
                                );
                            }
                            finally
                            {
                                bmp.UnlockBits(srcData);
                                clone.UnlockBits(dstData);
                            }
                        }
                    }
                    else
                    {
                        var data = bmp.LockBits(
                            new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                        );
                        try
                        {
                            var props = new BitmapProperties(new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
                            createdBmp = _renderTarget.CreateBitmap(
                                new Vortice.Mathematics.SizeI(bmp.Width, bmp.Height),
                                data.Scan0,
                                (uint)data.Stride,
                                props
                            );
                        }
                        finally
                        {
                            bmp.UnlockBits(data);
                        }
                    }
                }
                catch
                {
                    // Fallback: Convert indexed / non-standard bitmap to 32bppPArgb
                    try
                    {
                        using (var clone = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                        {
                            using (var cg = Graphics.FromImage(clone))
                            {
                                cg.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                            }
                            var data = clone.LockBits(
                                new Rectangle(0, 0, clone.Width, clone.Height),
                                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                            );
                            try
                            {
                                var props = new BitmapProperties(new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
                                createdBmp = _renderTarget.CreateBitmap(
                                    new Vortice.Mathematics.SizeI(clone.Width, clone.Height),
                                    data.Scan0,
                                    (uint)data.Stride,
                                    props
                                );
                            }
                            finally
                            {
                                clone.UnlockBits(data);
                            }
                        }
                    }
                    catch { }
                }
            }
            else
            {
                // Non-Bitmap (Metafile / EMF / Icon / etc.)
                try
                {
                    using (var clone = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                    {
                        using (var cg = Graphics.FromImage(clone))
                        {
                            cg.DrawImage(image, 0, 0, image.Width, image.Height);
                        }
                        var data = clone.LockBits(
                            new Rectangle(0, 0, clone.Width, clone.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                        );
                        try
                        {
                            var props = new BitmapProperties(new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
                            createdBmp = _renderTarget.CreateBitmap(
                                new Vortice.Mathematics.SizeI(clone.Width, clone.Height),
                                data.Scan0,
                                (uint)data.Stride,
                                props
                            );
                        }
                        finally
                        {
                            clone.UnlockBits(data);
                        }
                    }
                }
                catch { }
            }

            if (createdBmp != null)
            {
                if (_isUsingSharedDc)
                {
                    lock (_sharedBitmapCacheLock)
                    {
                        if (_sharedBitmapCache.Count >= MaxSharedBitmapCacheSize)
                        {
                            var oldest = _sharedBitmapLruList.First?.Value;
                            if (oldest != null)
                            {
                                _sharedBitmapLruList.RemoveFirst();
                                if (_sharedBitmapCache.TryGetValue(oldest, out var oldBmp))
                                {
                                    _sharedBitmapCache.Remove(oldest);
                                    try { oldBmp?.Dispose(); } catch { }
                                }
                            }
                        }
                        _sharedBitmapCache[image] = createdBmp;
                        _sharedBitmapLruList.AddLast(image);
                    }
                }
                else
                {
                    _bitmapCache[image] = createdBmp;
                }
            }

            return createdBmp;
        }

        private System.Numerics.Matrix3x2 _currentTransform = System.Numerics.Matrix3x2.Identity;
        private readonly Stack<System.Numerics.Matrix3x2> _transformStack = new Stack<System.Numerics.Matrix3x2>();
        private int _clipDepth = 0;

        // --- Core Drawing Methods (Hardware Direct2D + Fallback) ---
        public void Clear(Color color)
        {
            if (_renderTarget != null) _renderTarget.Clear(ToColor4(color));
            else if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.Clear(color);
        }

        public void FillRectangle(Brush brush, float x, float y, float width, float height)
        {
            if (_renderTarget != null)
            {
                using var b = GetD2DBrush(brush);
                if (b != null) _renderTarget.FillRectangle(new RectangleF(x, y, width, height), b);
            }
            else if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.FillRectangle(brush, x, y, width, height);
        }

        public void FillRectangle(Brush brush, RectangleF rect) => FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        public void FillRectangle(Brush brush, Rectangle rect) => FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        public void FillRectangle(Brush brush, int x, int y, int width, int height) => FillRectangle(brush, (float)x, (float)y, (float)width, (float)height);

        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            if (_renderTarget != null && pen != null)
            {
                var (b, w, stroke) = GetD2DPen(pen);
                if (b != null)
                {
                    _renderTarget.DrawRectangle(new RectangleF(x, y, width, height), b, w, stroke);
                    b.Dispose();
                    stroke?.Dispose();
                }
            }
            else if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawRectangle(pen, x, y, width, height);
        }

        public void DrawRectangle(Pen pen, Rectangle rect) => DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        public void DrawRectangle(Pen pen, int x, int y, int width, int height) => DrawRectangle(pen, (float)x, (float)y, (float)width, (float)height);

        public void FillRoundedRectangle(Brush brush, RectangleF rect, float radius)
        {
            if (radius <= 0.001f)
            {
                FillRectangle(brush, rect);
                return;
            }
            if (_renderTarget != null && brush != null)
            {
                using var b = GetD2DBrush(brush);
                if (b != null)
                {
                    float rx = Math.Min(radius, rect.Width / 2f);
                    float ry = Math.Min(radius, rect.Height / 2f);
                    _renderTarget.FillRoundedRectangle(new RoundedRectangle { Rect = new Vortice.Mathematics.Rect(rect.X, rect.Y, rect.Width, rect.Height), RadiusX = rx, RadiusY = ry }, b);
                    return;
                }
            }
            using var path = CreateRoundedRectanglePath(rect, radius);
            FillPath(brush, path);
        }

        public void FillRoundedRectangle(Brush brush, Rectangle rect, float radius) => FillRoundedRectangle(brush, (RectangleF)rect, radius);

        public void DrawRoundedRectangle(Pen pen, RectangleF rect, float radius)
        {
            if (radius <= 0.001f)
            {
                DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                return;
            }
            if (_renderTarget != null && pen != null)
            {
                var (b, w, stroke) = GetD2DPen(pen);
                if (b != null)
                {
                    float rx = Math.Min(radius, rect.Width / 2f);
                    float ry = Math.Min(radius, rect.Height / 2f);
                    _renderTarget.DrawRoundedRectangle(new RoundedRectangle { Rect = new Vortice.Mathematics.Rect(rect.X, rect.Y, rect.Width, rect.Height), RadiusX = rx, RadiusY = ry }, b, w, stroke);
                    b.Dispose();
                    stroke?.Dispose();
                    return;
                }
            }
            using var path = CreateRoundedRectanglePath(rect, radius);
            DrawPath(pen, path);
        }

        public void DrawRoundedRectangle(Pen pen, Rectangle rect, float radius) => DrawRoundedRectangle(pen, (RectangleF)rect, radius);

        public static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            if (d <= 0.001f)
            {
                path.AddRectangle(rect);
                return path;
            }
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            if (_renderTarget != null && pen != null)
            {
                var (b, w, stroke) = GetD2DPen(pen);
                if (b != null)
                {
                    _renderTarget.DrawLine(new System.Numerics.Vector2(x1, y1), new System.Numerics.Vector2(x2, y2), b, w, stroke);
                    b.Dispose();
                    stroke?.Dispose();
                }
            }
            else if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawLine(pen, x1, y1, x2, y2);
        }

        public void DrawLine(Pen pen, PointF pt1, PointF pt2) => DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2) => DrawLine(pen, (float)x1, (float)y1, (float)x2, (float)y2);

        public void DrawEllipse(Pen pen, float x, float y, float width, float height)
        {
            if (_renderTarget != null && pen != null)
            {
                var (b, w, stroke) = GetD2DPen(pen);
                if (b != null)
                {
                    float rx = width / 2.0f;
                    float ry = height / 2.0f;
                    var ellipse = new Ellipse(new System.Numerics.Vector2(x + rx, y + ry), rx, ry);
                    _renderTarget.DrawEllipse(ellipse, b, w, stroke);
                    b.Dispose();
                    stroke?.Dispose();
                }
            }
            else if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawEllipse(pen, x, y, width, height);
        }

        public void DrawEllipse(Pen pen, Rectangle rect) => DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        public void DrawEllipse(Pen pen, RectangleF rect) => DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);

        public void FillEllipse(Brush brush, float x, float y, float width, float height)
        {
            if (_renderTarget != null)
            {
                using var b = GetD2DBrush(brush);
                if (b != null)
                {
                    float rx = width / 2.0f;
                    float ry = height / 2.0f;
                    var ellipse = new Ellipse(new System.Numerics.Vector2(x + rx, y + ry), rx, ry);
                    _renderTarget.FillEllipse(ellipse, b);
                }
            }
            else if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.FillEllipse(brush, x, y, width, height);
        }

        public void FillEllipse(Brush brush, RectangleF rect) => FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        public void FillEllipse(Brush brush, Rectangle rect) => FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);

        // --- DirectWrite Typography Engine ---
        public void DrawString(string s, Font font, Brush brush, float x, float y)
        {
            if (string.IsNullOrEmpty(s)) return;
            if (_renderTarget != null)
            {
                using var b = GetD2DBrush(brush);
                var tf = GetTextFormat(font);
                if (b != null && tf != null)
                {
                    _renderTarget.DrawText(s, tf, new Vortice.Mathematics.Rect(x, y, 10000.0f, 10000.0f), b);
                }
            }
            else if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawString(s, font, brush, x, y);
        }

        public void DrawString(string s, Font font, Brush brush, PointF point) => DrawString(s, font, brush, point.X, point.Y);

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format)
        {
            if (string.IsNullOrEmpty(s)) return;
            if (_renderTarget != null)
            {
                using var b = GetD2DBrush(brush);
                var tf = GetTextFormat(font);
                if (b != null && tf != null)
                {
                    float w = Math.Max(1.0f, layoutRectangle.Width);
                    float h = Math.Max(1.0f, layoutRectangle.Height);
                    using var layout = _dwriteFactory.CreateTextLayout(s, tf, w, h);
                    if (format != null)
                    {
                        if (format.Alignment == StringAlignment.Center) layout.TextAlignment = TextAlignment.Center;
                        else if (format.Alignment == StringAlignment.Far) layout.TextAlignment = TextAlignment.Trailing;
                        else layout.TextAlignment = TextAlignment.Leading;

                        if (format.LineAlignment == StringAlignment.Center) layout.ParagraphAlignment = ParagraphAlignment.Center;
                        else if (format.LineAlignment == StringAlignment.Far) layout.ParagraphAlignment = ParagraphAlignment.Far;
                        else layout.ParagraphAlignment = ParagraphAlignment.Near;
                    }
                    _renderTarget.DrawTextLayout(new System.Numerics.Vector2(layoutRectangle.X, layoutRectangle.Y), layout, b);
                    return;
                }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawString(s, font, brush, layoutRectangle, format);
        }

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle) => DrawString(s, font, brush, layoutRectangle, null);
        public void DrawString(string s, Font font, Brush brush, float x, float y, StringFormat format) => DrawString(s, font, brush, new RectangleF(x, y, 10000, 10000), format);
        public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format) => DrawString(s, font, brush, new RectangleF(point.X, point.Y, 10000, 10000), format);

        public SizeF MeasureString(string text, Font font)
        {
            if (string.IsNullOrEmpty(text) || font == null) return SizeF.Empty;
            try
            {
                var tf = GetTextFormat(font);
                if (tf != null)
                {
                    using var layout = _dwriteFactory.CreateTextLayout(text, tf, 10000.0f, 10000.0f);
                    var m = layout.Metrics;
                    return new SizeF(m.WidthIncludingTrailingWhitespace, m.Height);
                }
            }
            catch { }
            return (_gdiGraphics != null && !_ownsHdc) ? _gdiGraphics.MeasureString(text, font) : SizeF.Empty;
        }

        public SizeF MeasureString(string text, Font font, int width)
        {
            if (string.IsNullOrEmpty(text) || font == null) return SizeF.Empty;
            try
            {
                var tf = GetTextFormat(font);
                if (tf != null)
                {
                    using var layout = _dwriteFactory.CreateTextLayout(text, tf, Math.Max(1, width), 10000.0f);
                    var m = layout.Metrics;
                    return new SizeF(m.WidthIncludingTrailingWhitespace, m.Height);
                }
            }
            catch { }
            return (_gdiGraphics != null && !_ownsHdc) ? _gdiGraphics.MeasureString(text, font, width) : SizeF.Empty;
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea) => MeasureString(text, font, (int)layoutArea.Width);
        public SizeF MeasureString(string text, Font font, int width, StringFormat stringFormat) => MeasureString(text, font, width);
        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat) => MeasureString(text, font, (int)layoutArea.Width);
        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat, out int charactersFitted, out int linesFilled)
        {
            charactersFitted = text?.Length ?? 0;
            linesFilled = 1;
            return MeasureString(text, font, (int)layoutArea.Width);
        }
        public SizeF MeasureString(string text, Font font, PointF origin, StringFormat stringFormat) => MeasureString(text, font);

        private float _currentImageOpacity = 1.0f;
        public float CurrentImageOpacity
        {
            get => _currentImageOpacity;
            set => _currentImageOpacity = Math.Max(0.0f, Math.Min(1.0f, value));
        }

        // --- Hardware Direct2D Bitmap Acceleration ---
        public void DrawImage(Image image, float x, float y, float opacity = -1.0f)
        {
            if (image == null) return;
            float finalOp = (opacity >= 0.0f) ? Math.Max(0.0f, Math.Min(1.0f, opacity)) : _currentImageOpacity;
            if (_renderTarget != null)
            {
                try
                {
                    var d2dBmp = GetOrCreateD2DBitmap(image);
                    if (d2dBmp != null)
                    {
                        _renderTarget.DrawBitmap(d2dBmp, new Vortice.Mathematics.Rect(x, y, image.Width, image.Height), finalOp, BitmapInterpolationMode.Linear, null);
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawImage(image, x, y);
        }

        public void DrawImage(Image image, float x, float y, float width, float height, float opacity = -1.0f)
        {
            if (image == null) return;
            float finalOp = (opacity >= 0.0f) ? Math.Max(0.0f, Math.Min(1.0f, opacity)) : _currentImageOpacity;
            if (_renderTarget != null)
            {
                try
                {
                    var d2dBmp = GetOrCreateD2DBitmap(image);
                    if (d2dBmp != null)
                    {
                        _renderTarget.DrawBitmap(d2dBmp, new Vortice.Mathematics.Rect(x, y, width, height), finalOp, BitmapInterpolationMode.Linear, null);
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawImage(image, x, y, width, height);
        }

        public void DrawImage(Image image, Rectangle rect, float opacity = -1.0f) => DrawImage(image, (float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height, opacity);
        public void DrawImage(Image image, RectangleF rect, float opacity = -1.0f) => DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height, opacity);
        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, float opacity = -1.0f)
        {
            if (image == null) return;
            float finalOp = (opacity >= 0.0f) ? Math.Max(0.0f, Math.Min(1.0f, opacity)) : _currentImageOpacity;
            if (_renderTarget != null)
            {
                try
                {
                    var d2dBmp = GetOrCreateD2DBitmap(image);
                    if (d2dBmp != null)
                    {
                        _renderTarget.DrawBitmap(
                            d2dBmp,
                            new Vortice.Mathematics.Rect(destRect.X, destRect.Y, destRect.Width, destRect.Height),
                            finalOp,
                            BitmapInterpolationMode.Linear,
                            new Vortice.Mathematics.Rect(srcX, srcY, srcWidth, srcHeight)
                        );
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, float opacity = -1.0f)
            => DrawImage(image, destRect, (int)srcX, (int)srcY, (int)srcWidth, (int)srcHeight, srcUnit, opacity);

        public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit = GraphicsUnit.Pixel, float opacity = -1.0f)
        {
            if (image == null || destRect.Width <= 0.001f || destRect.Height <= 0.001f || srcRect.Width <= 0.001f || srcRect.Height <= 0.001f) return;
            float finalOp = (opacity >= 0.0f) ? Math.Max(0.0f, Math.Min(1.0f, opacity)) : _currentImageOpacity;
            if (_renderTarget != null)
            {
                try
                {
                    var d2dBmp = GetOrCreateD2DBitmap(image);
                    if (d2dBmp != null)
                    {
                        _renderTarget.DrawBitmap(
                            d2dBmp,
                            new Vortice.Mathematics.Rect(destRect.X, destRect.Y, destRect.Width, destRect.Height),
                            finalOp,
                            BitmapInterpolationMode.Linear,
                            new Vortice.Mathematics.Rect(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height)
                        );
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawImage(image, destRect, srcRect, srcUnit);
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr, float opacity = -1.0f)
        {
            if (image == null) return;
            float finalOp = (opacity >= 0.0f) ? Math.Max(0.0f, Math.Min(1.0f, opacity)) : _currentImageOpacity;
            if (_renderTarget != null)
            {
                try
                {
                    var d2dBmp = GetOrCreateD2DBitmap(image);
                    if (d2dBmp != null)
                    {
                        float destX = 0, destY = 0, destW = image.Width, destH = image.Height;
                        if (destPoints != null && destPoints.Length >= 3)
                        {
                            destX = destPoints[0].X;
                            destY = destPoints[0].Y;
                            destW = destPoints[1].X - destPoints[0].X;
                            destH = destPoints[2].Y - destPoints[0].Y;
                        }
                        if (destW < 0) { destX += destW; destW = -destW; }
                        if (destH < 0) { destY += destH; destH = -destH; }

                        Vortice.Mathematics.Rect sRect = (srcRect.Width > 0 && srcRect.Height > 0)
                            ? new Vortice.Mathematics.Rect(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height)
                            : new Vortice.Mathematics.Rect(0, 0, image.Width, image.Height);

                        _renderTarget.DrawBitmap(
                            d2dBmp,
                            new Vortice.Mathematics.Rect(destX, destY, destW, destH),
                            finalOp,
                            BitmapInterpolationMode.Linear,
                            sRect
                        );
                        return;
                    }
                }
                catch
                {
                }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawImage(image, destPoints, srcRect, srcUnit, imageAttr);
        }

        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr, float opacity = -1.0f)
        {
            if (image == null) return;
            float finalOp = (opacity >= 0.0f) ? Math.Max(0.0f, Math.Min(1.0f, opacity)) : _currentImageOpacity;
            if (_renderTarget != null)
            {
                try
                {
                    var d2dBmp = GetOrCreateD2DBitmap(image);
                    if (d2dBmp != null)
                    {
                        _renderTarget.DrawBitmap(
                            d2dBmp,
                            new Vortice.Mathematics.Rect(destRect.X, destRect.Y, destRect.Width, destRect.Height),
                            finalOp,
                            BitmapInterpolationMode.Linear,
                            new Vortice.Mathematics.Rect(srcX, srcY, srcWidth, srcHeight)
                        );
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttr);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr, float opacity = -1.0f)
            => DrawImage(image, destRect, (int)srcX, (int)srcY, (int)srcWidth, (int)srcHeight, srcUnit, imageAttr, opacity);

        // --- Vector Geometry Paths ---
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            if (_renderTarget != null && pen != null && path != null)
            {
                using var geom = ConvertPath(path);
                var (b, w, stroke) = GetD2DPen(pen);
                if (b != null && geom != null)
                {
                    _renderTarget.DrawGeometry(geom, b, w, stroke);
                    b.Dispose();
                    stroke?.Dispose();
                    return;
                }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawPath(pen, path);
        }

        public void FillPath(Brush brush, GraphicsPath path)
        {
            if (_renderTarget != null && brush != null && path != null)
            {
                using var geom = ConvertPath(path);
                using var b = GetD2DBrush(brush);
                if (b != null && geom != null)
                {
                    _renderTarget.FillGeometry(geom, b);
                    return;
                }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.FillPath(brush, path);
        }
        public float DpiX => _dpiX;
        public float DpiY => _dpiY;

        private readonly ConditionalWeakTable<Region, StrongBox<int>> _regionDepthMap = new ConditionalWeakTable<Region, StrongBox<int>>();

        public Region Clip
        {
            get
            {
                if (_renderTarget != null)
                {
                    Region r = (_gdiGraphics != null && !_ownsHdc) ? _gdiGraphics.Clip : new Region();
                    _regionDepthMap.AddOrUpdate(r, new StrongBox<int>(_clipDepth));
                    return r;
                }
                return (_gdiGraphics != null && !_ownsHdc) ? _gdiGraphics.Clip : new Region();
            }
            set
            {
                if (_renderTarget != null)
                {
                    int baseClip = (_d2dStateStack.Count > 0) ? _d2dStateStack[_d2dStateStack.Count - 1].Layer.ClipDepth : 0;
                    int targetDepth = baseClip;
                    if (value != null && _regionDepthMap.TryGetValue(value, out var box))
                    {
                        targetDepth = Math.Max(baseClip, box.Value);
                    }
                    while (_clipDepth > targetDepth)
                    {
                        try { _renderTarget.PopAxisAlignedClip(); } catch { }
                        _clipDepth--;
                    }
                    return;
                }
                if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.Clip = value;
            }
        }

        public Matrix Transform
        {
            get
            {
                if (_renderTarget != null)
                {
                    return new Matrix(_currentTransform.M11, _currentTransform.M12, _currentTransform.M21, _currentTransform.M22, _currentTransform.M31, _currentTransform.M32);
                }
                return (_gdiGraphics != null && !_ownsHdc) ? _gdiGraphics.Transform : new Matrix();
            }
            set
            {
                if (value != null)
                {
                    if (_renderTarget != null)
                    {
                        var el = value.Elements;
                        _currentTransform = new System.Numerics.Matrix3x2(el[0], el[1], el[2], el[3], el[4], el[5]);
                        _renderTarget.Transform = _currentTransform;
                        return;
                    }
                    if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.Transform = value;
                }
            }
        }

        private SmoothingMode _smoothingMode = SmoothingMode.AntiAlias;
        public SmoothingMode SmoothingMode
        {
            get => _smoothingMode;
            set
            {
                _smoothingMode = value;
                if (_renderTarget != null)
                {
                    _renderTarget.AntialiasMode = (value == SmoothingMode.None || value == SmoothingMode.HighSpeed)
                        ? AntialiasMode.Aliased
                        : AntialiasMode.PerPrimitive;
                }
                else if (_gdiGraphics != null && !_ownsHdc)
                {
                    _gdiGraphics.SmoothingMode = value;
                }
            }
        }

        private TextRenderingHint _textRenderingHint = TextRenderingHint.ClearTypeGridFit;
        public TextRenderingHint TextRenderingHint
        {
            get => _textRenderingHint;
            set
            {
                _textRenderingHint = value;
                if (_renderTarget != null)
                {
                    _renderTarget.TextAntialiasMode = (value == TextRenderingHint.SingleBitPerPixel || value == TextRenderingHint.SingleBitPerPixelGridFit)
                        ? D2DTextAntialiasMode.Aliased
                        : D2DTextAntialiasMode.Cleartype;
                }
                else if (_gdiGraphics != null && !_ownsHdc)
                {
                    _gdiGraphics.TextRenderingHint = value;
                }
            }
        }

        private System.Drawing.Drawing2D.InterpolationMode _interpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        public System.Drawing.Drawing2D.InterpolationMode InterpolationMode
        {
            get => _interpolationMode;
            set
            {
                _interpolationMode = value;
                if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.InterpolationMode = value;
            }
        }

        private PixelOffsetMode _pixelOffsetMode = PixelOffsetMode.Default;
        public PixelOffsetMode PixelOffsetMode
        {
            get => _pixelOffsetMode;
            set
            {
                _pixelOffsetMode = value;
                if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.PixelOffsetMode = value;
            }
        }

        private CompositingQuality _compositingQuality = CompositingQuality.Default;
        public CompositingQuality CompositingQuality
        {
            get => _compositingQuality;
            set
            {
                _compositingQuality = value;
                if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.CompositingQuality = value;
            }
        }

        // --- Clipping Acceleration ---
        public void SetClip(Rectangle rect) => SetClip(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
        public void SetClip(RectangleF rect)
        {
            if (_renderTarget != null)
            {
                int baseClip = (_d2dStateStack.Count > 0) ? _d2dStateStack[_d2dStateStack.Count - 1].Layer.ClipDepth : 0;
                while (_clipDepth > baseClip)
                {
                    try { _renderTarget.PopAxisAlignedClip(); } catch { }
                    _clipDepth--;
                }
                _renderTarget.PushAxisAlignedClip(new Vortice.Mathematics.Rect(rect.X, rect.Y, rect.Width, rect.Height), AntialiasMode.PerPrimitive);
                _clipDepth++;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.SetClip(rect);
        }

        public void SetClip(Rectangle rect, System.Drawing.Drawing2D.CombineMode combineMode)
        {
            if (combineMode == System.Drawing.Drawing2D.CombineMode.Intersect) IntersectClip(rect);
            else SetClip(rect);
        }
        public void SetClip(RectangleF rect, System.Drawing.Drawing2D.CombineMode combineMode)
        {
            if (combineMode == System.Drawing.Drawing2D.CombineMode.Intersect) IntersectClip(rect);
            else SetClip(rect);
        }

        public void SetClip(GraphicsPath path)
        {
            if (path == null) return;
            if (_renderTarget != null)
            {
                SetClip(path.GetBounds());
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.SetClip(path);
        }

        public void SetClip(GraphicsPath path, System.Drawing.Drawing2D.CombineMode combineMode)
        {
            if (path == null) return;
            if (_renderTarget != null)
            {
                if (combineMode == System.Drawing.Drawing2D.CombineMode.Intersect) IntersectClip(path.GetBounds());
                else SetClip(path.GetBounds());
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.SetClip(path, combineMode);
        }

        public void SetClip(Region region, System.Drawing.Drawing2D.CombineMode combineMode)
        {
            if (_renderTarget != null)
            {
                if (combineMode == System.Drawing.Drawing2D.CombineMode.Replace)
                {
                    Clip = region;
                }
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.SetClip(region, combineMode);
        }

        public void SetClip(Region region) => SetClip(region, System.Drawing.Drawing2D.CombineMode.Replace);

        public void PushClip(Rectangle rect) => PushClip(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
        public void PushClip(RectangleF rect)
        {
            if (_renderTarget != null)
            {
                _renderTarget.PushAxisAlignedClip(new Vortice.Mathematics.Rect(rect.X, rect.Y, rect.Width, rect.Height), AntialiasMode.PerPrimitive);
                _clipDepth++;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.IntersectClip(rect);
        }

        public void PopClip()
        {
            if (_renderTarget != null)
            {
                int baseClip = (_d2dStateStack.Count > 0) ? _d2dStateStack[_d2dStateStack.Count - 1].Layer.ClipDepth : 0;
                if (_clipDepth > baseClip)
                {
                    try { _renderTarget.PopAxisAlignedClip(); } catch { }
                    _clipDepth--;
                }
                return;
            }
        }

        public void IntersectClip(Rectangle rect) => IntersectClip(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
        public void IntersectClip(RectangleF rect)
        {
            if (_renderTarget != null)
            {
                _renderTarget.PushAxisAlignedClip(new Vortice.Mathematics.Rect(rect.X, rect.Y, rect.Width, rect.Height), AntialiasMode.PerPrimitive);
                _clipDepth++;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.IntersectClip(rect);
        }
        public void IntersectClip(Region region)
        {
            if (_renderTarget != null) return;
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.IntersectClip(region);
        }

        public void ResetClip()
        {
            if (_renderTarget != null)
            {
                int baseClip = (_d2dStateStack.Count > 0) ? _d2dStateStack[_d2dStateStack.Count - 1].Layer.ClipDepth : 0;
                while (_clipDepth > baseClip)
                {
                    try { _renderTarget.PopAxisAlignedClip(); } catch { }
                    _clipDepth--;
                }
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.ResetClip();
        }

        // --- Matrix Transformations ---
        public void TranslateTransform(float dx, float dy)
        {
            if (_renderTarget != null)
            {
                _currentTransform = System.Numerics.Matrix3x2.CreateTranslation(dx, dy) * _currentTransform;
                _renderTarget.Transform = _currentTransform;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.TranslateTransform(dx, dy);
        }

        public void ScaleTransform(float sx, float sy)
        {
            if (_renderTarget != null)
            {
                _currentTransform = System.Numerics.Matrix3x2.CreateScale(sx, sy) * _currentTransform;
                _renderTarget.Transform = _currentTransform;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.ScaleTransform(sx, sy);
        }

        public void RotateTransform(float angle)
        {
            if (_renderTarget != null)
            {
                float rad = angle * (float)Math.PI / 180.0f;
                _currentTransform = System.Numerics.Matrix3x2.CreateRotation(rad) * _currentTransform;
                _renderTarget.Transform = _currentTransform;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.RotateTransform(angle);
        }

        public void MultiplyTransform(Matrix matrix)
        {
            if (matrix != null)
            {
                if (_renderTarget != null)
                {
                    var elements = matrix.Elements;
                    var m = new System.Numerics.Matrix3x2(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
                    _currentTransform = m * _currentTransform;
                    _renderTarget.Transform = _currentTransform;
                    return;
                }
                if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.MultiplyTransform(matrix);
            }
        }

        public void ResetTransform()
        {
            _currentTransform = System.Numerics.Matrix3x2.Identity;
            if (_renderTarget != null)
            {
                _renderTarget.Transform = _currentTransform;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.ResetTransform();
        }

        private struct D2DLayerState
        {
            public System.Numerics.Matrix3x2 Transform;
            public int ClipDepth;
        }
        private readonly List<(GraphicsState State, D2DLayerState Layer)> _d2dStateStack = new List<(GraphicsState, D2DLayerState)>();
        public int DebugClipDepth => _clipDepth;
        public int DebugStateStackCount => _d2dStateStack.Count;
        public System.Numerics.Matrix3x2 DebugCurrentTransform => _currentTransform;

        public GraphicsState Save()
        {
            if (_renderTarget != null)
            {
                var gs = (GraphicsState)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GraphicsState));
                _d2dStateStack.Add((gs, new D2DLayerState { Transform = _currentTransform, ClipDepth = _clipDepth }));
                return gs;
            }
            return (_gdiGraphics != null && !_ownsHdc) ? _gdiGraphics.Save() : null;
        }

        public void Restore(GraphicsState gstate)
        {
            if (_renderTarget != null)
            {
                if (_d2dStateStack.Count == 0) return;

                int targetIndex = -1;
                if (gstate != null)
                {
                    for (int i = _d2dStateStack.Count - 1; i >= 0; i--)
                    {
                        if (ReferenceEquals(_d2dStateStack[i].State, gstate))
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                }

                if (targetIndex < 0)
                {
                    targetIndex = _d2dStateStack.Count - 1; // Pop top
                }

                var targetLayer = _d2dStateStack[targetIndex].Layer;
                
                // Pop all states down to targetIndex
                _d2dStateStack.RemoveRange(targetIndex, _d2dStateStack.Count - targetIndex);

                // Pop clips down to targetLayer.ClipDepth
                while (_clipDepth > targetLayer.ClipDepth)
                {
                    try { _renderTarget.PopAxisAlignedClip(); } catch { }
                    _clipDepth--;
                }

                // Restore transform
                _currentTransform = targetLayer.Transform;
                _renderTarget.Transform = _currentTransform;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.Restore(gstate);
        }

        // --- Polygonal & Curves via GPU Direct2D ---
        public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (width <= 0 || height <= 0) return;
            using GraphicsPath p = new GraphicsPath();
            p.AddArc(x, y, width, height, startAngle, sweepAngle);
            DrawPath(pen, p);
        }

        public void DrawArc(Pen pen, RectangleF rect, float startAngle, float sweepAngle) => DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        public void DrawArc(Pen pen, Rectangle rect) => DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, 0, 360);

        public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (width <= 0 || height <= 0) return;
            using GraphicsPath p = new GraphicsPath();
            p.AddPie(x, y, width, height, startAngle, sweepAngle);
            FillPath(brush, p);
        }

        public void FillPie(Brush brush, Rectangle rect, float startAngle, float sweepAngle) => FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);

        public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (width <= 0 || height <= 0 || pen == null) return;
            using GraphicsPath p = new GraphicsPath();
            p.AddPie(x, y, width, height, startAngle, sweepAngle);
            DrawPath(pen, p);
        }

        public void DrawPie(Pen pen, Rectangle rect, float startAngle, float sweepAngle) => DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);

        public void DrawPolygon(Pen pen, PointF[] points)
        {
            if (points == null || points.Length < 2) return;
            using GraphicsPath p = new GraphicsPath();
            p.AddPolygon(points);
            DrawPath(pen, p);
        }

        public void DrawPolygon(Pen pen, Point[] points)
        {
            if (points == null || points.Length < 2) return;
            PointF[] pts = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++) pts[i] = new PointF(points[i].X, points[i].Y);
            DrawPolygon(pen, pts);
        }

        public void FillPolygon(Brush brush, PointF[] points)
        {
            if (points == null || points.Length < 2) return;
            using GraphicsPath p = new GraphicsPath();
            p.AddPolygon(points);
            FillPath(brush, p);
        }

        public void FillPolygon(Brush brush, Point[] points)
        {
            if (points == null || points.Length < 2) return;
            PointF[] pts = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++) pts[i] = new PointF(points[i].X, points[i].Y);
            FillPolygon(brush, pts);
        }

        public void DrawLines(Pen pen, PointF[] points)
        {
            if (points == null || points.Length < 2) return;
            using GraphicsPath p = new GraphicsPath();
            p.AddLines(points);
            DrawPath(pen, p);
        }

        public void DrawLines(Pen pen, Point[] points)
        {
            if (points == null || points.Length < 2) return;
            PointF[] pts = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++) pts[i] = new PointF(points[i].X, points[i].Y);
            DrawLines(pen, pts);
        }

        public void DrawBeziers(Pen pen, PointF[] points)
        {
            if (points == null || points.Length < 4) return;
            using GraphicsPath p = new GraphicsPath();
            p.AddBeziers(points);
            DrawPath(pen, p);
        }

        public void DrawBeziers(Pen pen, Point[] points)
        {
            if (points == null || points.Length < 4) return;
            PointF[] pts = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++) pts[i] = new PointF(points[i].X, points[i].Y);
            DrawBeziers(pen, pts);
        }

        public void DrawBezier(Pen pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            DrawBeziers(pen, new PointF[] { new PointF(x1, y1), new PointF(x2, y2), new PointF(x3, y3), new PointF(x4, y4) });
        }

        public void DrawBezier(Pen pen, PointF pt1, PointF pt2, PointF pt3, PointF pt4)
            => DrawBezier(pen, pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);

        public void DrawCurve(Pen pen, PointF[] points)
        {
            if (points == null || points.Length < 2) return;
            using GraphicsPath p = new GraphicsPath();
            p.AddCurve(points);
            DrawPath(pen, p);
        }

        public void DrawCurve(Pen pen, Point[] points)
        {
            if (points == null || points.Length < 2) return;
            PointF[] pts = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++) pts[i] = new PointF(points[i].X, points[i].Y);
            DrawCurve(pen, pts);
        }

        public void DrawCurve(Pen pen, PointF[] points, float tension)
        {
            if (points == null || points.Length < 2) return;
            using GraphicsPath p = new GraphicsPath();
            p.AddCurve(points, tension);
            DrawPath(pen, p);
        }

        public void DrawCurve(Pen pen, Point[] points, float tension)
        {
            if (points == null || points.Length < 2) return;
            PointF[] pts = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++) pts[i] = new PointF(points[i].X, points[i].Y);
            DrawCurve(pen, pts, tension);
        }
    }

    public static class D2DExtensions
    {
        public static float GetHeight(this Font font, D2DGraphics g)
        {
            if (font == null) return 15.0f;
            try
            {
                return font.GetHeight(96.0f);
            }
            catch
            {
                return font.SizeInPoints * (96.0f / 72.0f) * 1.15f;
            }
        }
    }
}
