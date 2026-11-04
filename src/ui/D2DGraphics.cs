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

        private static readonly ID2D1Factory1 _d2dFactory;
        private static readonly IDWriteFactory _dwriteFactory;
        private static readonly IDWriteFactory5 _dwriteFactory5;
        private static IDWriteFontCollection _customFontCollection;
        private static readonly Vortice.WIC.IWICImagingFactory _wicFactory = new Vortice.WIC.IWICImagingFactory();
        public static Vortice.WIC.IWICImagingFactory WicFactory => _wicFactory;

        private readonly struct PathBmpKey : IEquatable<PathBmpKey>
        {
            public readonly IntPtr Target;
            public readonly string Path;
            public PathBmpKey(IntPtr target, string path)
            {
                Target = target;
                Path = path ?? string.Empty;
            }
            public bool Equals(PathBmpKey other) => Target == other.Target && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
            public override bool Equals(object obj) => obj is PathBmpKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Target, StringComparer.OrdinalIgnoreCase.GetHashCode(Path));
        }

        private static readonly Dictionary<PathBmpKey, ID2D1Bitmap> _pathBitmapCache = new Dictionary<PathBmpKey, ID2D1Bitmap>();
        private static readonly LinkedList<PathBmpKey> _pathBitmapLru = new LinkedList<PathBmpKey>();
        private static readonly Dictionary<PathBmpKey, (ID2D1Bitmap[] Frames, int[] Delays)> _pathGifCache = new Dictionary<PathBmpKey, (ID2D1Bitmap[], int[])>();
        private static readonly object _pathBitmapCacheLock = new object();
        private const int MaxPathBitmapCache = 512;

        private static void PathCacheRemoveTarget_NoLock(IntPtr targetPtr)
        {
            if (targetPtr == IntPtr.Zero) return;
            var toRemoveBmp = new List<PathBmpKey>();
            foreach (var k in _pathBitmapCache.Keys)
            {
                if (k.Target == targetPtr) toRemoveBmp.Add(k);
            }
            foreach (var k in toRemoveBmp)
            {
                if (_pathBitmapCache.Remove(k, out var b)) { try { b?.Dispose(); } catch { } }
                _pathBitmapLru.Remove(k);
            }
            var toRemoveGif = new List<PathBmpKey>();
            foreach (var k in _pathGifCache.Keys)
            {
                if (k.Target == targetPtr) toRemoveGif.Add(k);
            }
            foreach (var k in toRemoveGif)
            {
                if (_pathGifCache.Remove(k, out var g) && g.Frames != null)
                {
                    foreach (var f in g.Frames) { try { f?.Dispose(); } catch { } }
                }
            }
        }

        public static void ReleaseRenderTargetCaches(IntPtr target)
        {
            if(target==IntPtr.Zero) return;
            lock(_sharedBitmapCacheLock) SharedCacheRemoveWhere_NoLock(key=>key.Target==target);
            lock(_pathBitmapCacheLock) PathCacheRemoveTarget_NoLock(target);
        }

        public static void InvalidatePathBitmapCache(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            lock (_pathBitmapCacheLock)
            {
                var toRemove = new List<PathBmpKey>();
                foreach (var k in _pathBitmapCache.Keys)
                {
                    if (string.Equals(k.Path, filePath, StringComparison.OrdinalIgnoreCase))
                        toRemove.Add(k);
                }
                foreach (var k in toRemove)
                {
                    if (_pathBitmapCache.Remove(k, out var bmp))
                    {
                        _pathBitmapLru.Remove(k);
                        try { bmp?.Dispose(); } catch { }
                    }
                }
                var toRemoveGif = new List<PathBmpKey>();
                foreach (var k in _pathGifCache.Keys)
                {
                    if (string.Equals(k.Path, filePath, StringComparison.OrdinalIgnoreCase))
                        toRemoveGif.Add(k);
                }
                foreach (var k in toRemoveGif)
                {
                    if (_pathGifCache.Remove(k, out var g) && g.Frames != null)
                    {
                        foreach (var f in g.Frames) { try { f?.Dispose(); } catch { } }
                    }
                }
            }
        }

        public static ID2D1Bitmap LoadD2DBitmapFromFileDirect(ID2D1RenderTarget target, string filePath)
        {
            if (target == null || string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return null;

            IntPtr targetPtr = target.NativePointer;
            bool canUseGlobalCache = (GpuEngine.D3DDevice != null && target is ID2D1DeviceContext);
            var key = new PathBmpKey(targetPtr, filePath);

            if (canUseGlobalCache)
            {
                lock (_pathBitmapCacheLock)
                {
                    if (_pathBitmapCache.TryGetValue(key, out var cached))
                    {
                        if (cached != null && cached.NativePointer != IntPtr.Zero)
                        {
                            _pathBitmapLru.Remove(key);
                            _pathBitmapLru.AddLast(key);
                            return cached;
                        }
                        _pathBitmapCache.Remove(key);
                        _pathBitmapLru.Remove(key);
                    }
                }
            }

            try
            {
                using var wicStream = _wicFactory.CreateStream(filePath, System.IO.FileAccess.Read);
                using var decoder = _wicFactory.CreateDecoderFromStream(wicStream, Vortice.WIC.DecodeOptions.CacheOnDemand);
                if (decoder.FrameCount <= 0) return null;
                using var frame = decoder.GetFrame(0);
                using var converter = _wicFactory.CreateFormatConverter();
                converter.Initialize(frame, Vortice.WIC.PixelFormat.Format32bppPBGRA);
                var bmp = target.CreateBitmapFromWicBitmap(converter);

                // Lưu vào _pathBitmapCache theo từng RenderTarget (resource domain riêng biệt)
                if (bmp != null && canUseGlobalCache)
                {
                    lock (_pathBitmapCacheLock)
                    {
                        if (_pathBitmapCache.Count >= MaxPathBitmapCache && _pathBitmapLru.First != null)
                        {
                            var oldest = _pathBitmapLru.First.Value;
                            _pathBitmapLru.RemoveFirst();
                            if (_pathBitmapCache.TryGetValue(oldest, out var oldBmp))
                            {
                                _pathBitmapCache.Remove(oldest);
                                try { oldBmp?.Dispose(); } catch { }
                            }
                        }
                        _pathBitmapCache[key] = bmp;
                        _pathBitmapLru.Remove(key);
                        _pathBitmapLru.AddLast(key);
                    }
                }
                return bmp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WIC Direct Load error: " + ex.Message);
                return null;
            }
        }

        public static ID2D1Bitmap CreateBitmapFromStreamDirect(ID2D1RenderTarget target, System.IO.Stream stream)
        {
            if (target == null || stream == null) return null;
            try
            {
                using var wicStream = _wicFactory.CreateStream(stream);
                using var decoder = _wicFactory.CreateDecoderFromStream(wicStream, Vortice.WIC.DecodeOptions.CacheOnDemand);
                if (decoder.FrameCount <= 0) return null;
                using var frame = decoder.GetFrame(0);
                using var converter = _wicFactory.CreateFormatConverter();
                converter.Initialize(frame, Vortice.WIC.PixelFormat.Format32bppPBGRA);
                return target.CreateBitmapFromWicBitmap(converter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WIC Stream Load error: " + ex.Message);
                return null;
            }
        }

        public bool DrawImageOpacityMask(Image image, RectangleF rectangle, LinearGradientBrush opacityMask)
        {
            if(_renderTarget==null || image==null) return false;
            var bitmap=GetOrCreateD2DBitmap(image);
            if(bitmap==null) return false;
            using var brush=GetD2DBrush(opacityMask);
            var layer=new LayerParameters {
                ContentBounds=new Vortice.RawRectF(rectangle.Left,rectangle.Top,rectangle.Right,rectangle.Bottom),
                MaskAntialiasMode=AntialiasMode.PerPrimitive,MaskTransform=System.Numerics.Matrix3x2.Identity,
                Opacity=1,OpacityBrush=brush,LayerOptions=LayerOptions.None
            };
            _renderTarget.PushLayer(layer,null);
            try
            {
                DrawD2DBitmapInternal(bitmap,new Vortice.Mathematics.Rect(rectangle.X,rectangle.Y,rectangle.Width,rectangle.Height),1f,null);
            }
            finally { _renderTarget.PopLayer(); }
            return true;
        }
        private readonly Dictionary<Image,ID2D1BitmapBrush> _meshImageBrushes=new();
        public bool FillTexturedTriangle(Image image,GraphicsPath triangle,Matrix unitMapping,float shade)
        {
            if(_renderTarget==null || image==null) return false;
            if(!_meshImageBrushes.TryGetValue(image,out var brush))
            {
                var bitmap=GetOrCreateD2DBitmap(image);
                if(bitmap==null) return false;
                brush=_renderTarget.CreateBitmapBrush(bitmap,new BitmapBrushProperties {
                    ExtendModeX=ExtendMode.Clamp,ExtendModeY=ExtendMode.Clamp,InterpolationMode=BitmapInterpolationMode.Linear });
                _meshImageBrushes[image]=brush;
            }
            var m=unitMapping.Elements;
            brush.Transform=new System.Numerics.Matrix3x2(m[0]/image.Width,m[1]/image.Width,m[2]/image.Height,m[3]/image.Height,m[4],m[5]);
            using var geometry=ConvertPath(triangle,_renderTarget.Factory??(ID2D1Factory)_d2dFactory);
            var antialias=_renderTarget.AntialiasMode;
            try
            {
                // A textured geometry avoids allocating a full intermediate layer for every face.
                _renderTarget.AntialiasMode=AntialiasMode.Aliased;
                _renderTarget.FillGeometry(geometry,brush);
                if(shade>0)
                {
                    using var tint=_renderTarget.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0,0,0,Math.Clamp(shade,0,150)/255f));
                    _renderTarget.FillGeometry(geometry,tint);
                }
            }
            finally { _renderTarget.AntialiasMode=antialias; }
            return true;
        }

        public void PrepareImage(Image image) { if (image != null) GetOrCreateD2DBitmap(image); }
        public static Bitmap CopyDecodedGifFrame(string path, int index)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;
            string key=path+"|"+System.IO.File.GetLastWriteTimeUtc(path).Ticks;
            if (!_decodedGifs.TryGetValue(key,out var task) || !task.IsCompletedSuccessfully || task.Result==null) return null;
            var gif=task.Result;
            var bitmap=new Bitmap(gif.Width,gif.Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            var data=bitmap.LockBits(new Rectangle(0,0,gif.Width,gif.Height),System.Drawing.Imaging.ImageLockMode.WriteOnly,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            try { System.Runtime.InteropServices.Marshal.Copy(gif.Pixels[Math.Clamp(index,0,gif.Pixels.Length-1)],0,data.Scan0,gif.Width*gif.Height*4); }
            finally { bitmap.UnlockBits(data); }
            return bitmap;
        }
        private sealed class DecodedGif
        {
            public int Width, Height;
            public byte[][] Pixels;
            public int[] Delays;
        }
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Threading.Tasks.Task<DecodedGif>> _decodedGifs = new();
        private static DecodedGif DecodeGif(string path)
        {
            using var image = Image.FromFile(path);
            var dimension = new System.Drawing.Imaging.FrameDimension(image.FrameDimensionsList[0]);
            int count = image.GetFrameCount(dimension);
            if (count < 1) return null;
            // Bound preview memory to ~32 MB per animation and 1024 pixels per side.
            double scale = Math.Min(1d, Math.Min(1024d / Math.Max(image.Width, image.Height),
                Math.Sqrt((32d * 1024 * 1024) / (count * 4d * image.Width * image.Height))));
            int width = Math.Max(1, (int)(image.Width * scale)), height = Math.Max(1, (int)(image.Height * scale));
            var decoded = new DecodedGif { Width = width, Height = height, Pixels = new byte[count][], Delays = new int[count] };
            byte[] timing = null;
            try { timing = image.GetPropertyItem(0x5100)?.Value; } catch { }
            using var canvas = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using var graphics = Graphics.FromImage(canvas);
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            for (int i = 0; i < count; i++)
            {
                int delay = timing != null && i * 4 + 3 < timing.Length ? BitConverter.ToInt32(timing, i * 4) * 10 : 100;
                decoded.Delays[i] = Math.Clamp(delay <= 10 ? 100 : delay, 20, 10000);
                image.SelectActiveFrame(dimension, i);
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(image, 0, 0, width, height);
                var data = canvas.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                try
                {
                    decoded.Pixels[i] = new byte[width * height * 4];
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, decoded.Pixels[i], 0, decoded.Pixels[i].Length);
                }
                finally { canvas.UnlockBits(data); }
            }
            return decoded;
        }
        public static (ID2D1Bitmap[] frames, int[] delaysMs) LoadGifFramesDirect(ID2D1RenderTarget target, string filePath)
        {
            if (target == null || string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return (null, null);
            var key = new PathBmpKey(target.NativePointer, filePath);
            lock (_pathBitmapCacheLock)
                if (_pathGifCache.TryGetValue(key, out var cached) && cached.Frames?.Length > 0 && cached.Frames[0] != null && cached.Frames[0].NativePointer != IntPtr.Zero)
                    return cached;
            string decodeKey = filePath + "|" + System.IO.File.GetLastWriteTimeUtc(filePath).Ticks;
            if (_decodedGifs.Count > 8)
                foreach (var entry in _decodedGifs)
                    if (entry.Key != decodeKey && entry.Value.IsCompleted) { _decodedGifs.TryRemove(entry.Key, out _); break; }
            var task = _decodedGifs.GetOrAdd(decodeKey, _ => System.Threading.Tasks.Task.Run(() => DecodeGif(filePath)));
            if (!task.IsCompletedSuccessfully || task.Result == null) return (null, null);
            var decoded = task.Result;
            var frames = new ID2D1Bitmap[decoded.Pixels.Length];
            try
            {
                var props = new BitmapProperties(new Vortice.DCommon.PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied));
                for (int i = 0; i < frames.Length; i++)
                {
                    unsafe
                    {
                        fixed (byte* pixels = decoded.Pixels[i])
                            frames[i] = target.CreateBitmap(new Vortice.Mathematics.SizeI(decoded.Width, decoded.Height), (IntPtr)pixels, (uint)(decoded.Width * 4), props);
                    }
                }
                var result = (frames, decoded.Delays);
                lock (_pathBitmapCacheLock) _pathGifCache[key] = result;
                return result;
            }
            catch
            {
                foreach (var frame in frames) frame?.Dispose();
                return (null, null);
            }
        }
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string Family,string Name,float Dip,int Style), IDWriteTextFormat> _textFormatCache = new();
        public static ID2D1Factory1 D2DFactory => _d2dFactory;

        static D2DGraphics()
        {
            _d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>(Vortice.Direct2D1.FactoryType.SingleThreaded);
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
        private ID2D1DeviceContext _deviceContext;
        private ID2D1HwndRenderTarget _hwndRenderTarget;
        private ID2D1DCRenderTarget _dcRenderTarget;
        private static ID2D1DCRenderTarget _sharedDcRenderTarget;
        private static bool _sharedDcInUse = false;
        private static readonly object _sharedDcLock = new object();

        public static void ResetSharedDcTarget()
        {
            lock (_sharedDcLock)
            {
                if (_sharedDcRenderTarget != null)
                {
                    try { _sharedDcRenderTarget.Dispose(); } catch { }
                    _sharedDcRenderTarget = null;
                }
                _sharedDcInUse = false;
            }
            ClearSharedBitmapCache();
        }
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
        private bool _ownsRenderTarget = false;
        private bool _usePersistentBitmapCache = false;
        private static IntPtr _sharedBitmapOwnerTarget = IntPtr.Zero;
        private float _dpiX = 96.0f;
        private float _dpiY = 96.0f;

        public bool IsWindowGpuSurface { get; private set; }
        public bool IsDirectXActive => _renderTarget != null;
        public ID2D1RenderTarget RenderTarget => _renderTarget;
        public static ID2D1DCRenderTarget SharedDcRenderTarget => _sharedDcRenderTarget;

        private static ID2D1DCRenderTarget CreateDCRenderTargetInternal()
        {
            // 1. Try Hardware D3D11 Acceleration
            try
            {
                var props = new RenderTargetProperties(
                    RenderTargetType.Default,
                    new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                    96.0f, 96.0f,
                    RenderTargetUsage.GdiCompatible,
                    FeatureLevel.Default
                );
                return _d2dFactory.CreateDCRenderTarget(props);
            }
            catch (Exception exHw)
            {
                System.Diagnostics.Debug.WriteLine("D2D Hardware DC target not available, activating Microsoft WARP: " + exHw.Message);
            }

            // 2. Fallback to Microsoft WARP (High-performance multi-threaded SIMD CPU rasterizer - MS Office Standard)
            try
            {
                var warpProps = new RenderTargetProperties(
                    RenderTargetType.Software,
                    new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                    96.0f, 96.0f,
                    RenderTargetUsage.GdiCompatible,
                    FeatureLevel.Default
                );
                return _d2dFactory.CreateDCRenderTarget(warpProps);
            }
            catch (Exception exWarp)
            {
                System.Diagnostics.Debug.WriteLine("D2D WARP DC target creation failed: " + exWarp.Message);
                return null;
            }
        }

        public D2DGraphics()
        {
        }

        public D2DGraphics(ID2D1RenderTarget target, int width, int height, bool ownsTarget = false)
        {
            _renderTarget = target;
            _ownsRenderTarget = ownsTarget;
            _usePersistentBitmapCache = !ownsTarget; // Giữ cache xuyên suốt các frame cho GpuHwndSurface
            _ownsGraphics = false;
            _gdiGraphics = null;
            _dpiX = 96.0f;
            _dpiY = 96.0f;
            if (_renderTarget != null)
            {
                try { _renderTarget.GetDpi(out _dpiX, out _dpiY); } catch { }
                if (_dpiX <= 0f) _dpiX = 96.0f;
                if (_dpiY <= 0f) _dpiY = 96.0f;
                try { _deviceContext = _renderTarget.QueryInterfaceOrNull<ID2D1DeviceContext>(); } catch { _deviceContext = null; }
                try { _gdiInterop = _renderTarget.QueryInterfaceOrNull<ID2D1GdiInteropRenderTarget>(); } catch { _gdiInterop = null; }
                _renderTarget.Transform = System.Numerics.Matrix3x2.Identity;
                _currentTransform = System.Numerics.Matrix3x2.Identity;
                _renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
                _renderTarget.TextAntialiasMode = D2DTextAntialiasMode.Grayscale;
                _renderTarget.BeginDraw();
                _isDrawing = true;
            }
        }

        public static D2DGraphics FromGpuSurface(GpuHwndSurface surface)
        {
            if (surface == null) return null;
            var target = surface.ActiveRenderTarget;
            if (target == null) return null;
            return new D2DGraphics(target, surface.Width, surface.Height, ownsTarget: false) { IsWindowGpuSurface = true };
        }

        public D2DGraphics(Graphics g, bool pureGdi)
            : this(g, pureGdi, 0, 0)
        {
        }

        public D2DGraphics(Graphics g, int viewportWidth = 0, int viewportHeight = 0)
            : this(g, false, viewportWidth, viewportHeight, false)
        {
        }

        public D2DGraphics(Graphics g, bool pureGdi, int viewportWidth, int viewportHeight)
            : this(g, pureGdi, viewportWidth, viewportHeight, false)
        {
        }

        public D2DGraphics(Graphics g, bool pureGdi, int viewportWidth, int viewportHeight, bool ownsGraphics)
        {
            _gdiGraphics = g;
            _ownsGraphics = ownsGraphics;
            _ownsGdiHdc = false;
            _renderTarget = null;
            if (g != null)
            {
                try { _dpiX = g.DpiX; _dpiY = g.DpiY; } catch { }

                if (!pureGdi)
                {
                    try
                    {
                        _gdiHdc = g.GetHdc();
                        _ownsGdiHdc = true;

                        int w = viewportWidth;
                        int h = viewportHeight;

                        if (w <= 0 || h <= 0)
                        {
                            IntPtr hBmp = GetCurrentObject(_gdiHdc, OBJ_BITMAP);
                            if (hBmp != IntPtr.Zero && GetObject(hBmp, Marshal.SizeOf<BITMAP>(), out BITMAP bmpObj) > 0 && bmpObj.bmWidth > 0 && bmpObj.bmHeight > 0)
                            {
                                w = bmpObj.bmWidth;
                                h = bmpObj.bmHeight;
                            }
                        }

                        if (w <= 0 || h <= 0)
                        {
                            try
                            {
                                var clip = g.VisibleClipBounds;
                                w = Math.Max(1, (int)Math.Ceiling(clip.Right));
                                h = Math.Max(1, (int)Math.Ceiling(clip.Bottom));
                            }
                            catch { }
                        }

                        if (w <= 0 || h <= 0)
                        {
                            int devW = GetDeviceCaps(_gdiHdc, HORZRES);
                            int devH = GetDeviceCaps(_gdiHdc, VERTRES);
                            w = devW > 0 ? devW : 1920;
                            h = devH > 0 ? devH : 1080;
                        }

                        w = Math.Max(1, w);
                        h = Math.Max(1, h);

                        lock (_sharedDcLock)
                        {
                            if (!_sharedDcInUse && _sharedDcRenderTarget != null)
                            {
                                _dcRenderTarget = _sharedDcRenderTarget;
                                _isUsingSharedDc = true;
                                _sharedDcInUse = true;
                            }
                        }

                        if (_dcRenderTarget == null)
                        {
                            _dcRenderTarget = CreateDCRenderTargetInternal();
                            if (!_isUsingSharedDc && _sharedDcRenderTarget == null && _dcRenderTarget != null)
                            {
                                lock (_sharedDcLock)
                                {
                                    if (_sharedDcRenderTarget == null)
                                    {
                                        _sharedDcRenderTarget = _dcRenderTarget;
                                        _isUsingSharedDc = true;
                                        _sharedDcInUse = true;
                                    }
                                }
                            }
                        }

                        if (_dcRenderTarget != null)
                        {
                            _dcRenderTarget.BindDC(_gdiHdc, new Vortice.Mathematics.RectI(0, 0, w, h));
                            _renderTarget = _dcRenderTarget;
                            _renderTarget.Transform = System.Numerics.Matrix3x2.Identity;
                            _currentTransform = System.Numerics.Matrix3x2.Identity;
                            try { _gdiInterop = _renderTarget.QueryInterface<ID2D1GdiInteropRenderTarget>(); } catch { }
                            try { _deviceContext = _renderTarget.QueryInterfaceOrNull<ID2D1DeviceContext>(); } catch { _deviceContext = null; }
                            _renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
                            _renderTarget.TextAntialiasMode = D2DTextAntialiasMode.Cleartype;
                            _renderTarget.BeginDraw();
                            _isDrawing = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("D2D DC init fallback: " + ex.Message);
                        if (_ownsGdiHdc && _gdiHdc != IntPtr.Zero)
                        {
                            try { g.ReleaseHdc(_gdiHdc); } catch { }
                            _gdiHdc = IntPtr.Zero;
                            _ownsGdiHdc = false;
                        }
                        _renderTarget = null;
                        _dcRenderTarget = null;
                    }
                }
            }
        }

        public D2DGraphics(IntPtr hwnd, int width, int height)
        {
            if (hwnd != IntPtr.Zero && width > 0 && height > 0)
            {
                var hwndProps = new HwndRenderTargetProperties
                {
                    Hwnd = hwnd,
                    PixelSize = new Vortice.Mathematics.SizeI(Math.Max(1, width), Math.Max(1, height)),
                    PresentOptions = PresentOptions.Immediately
                };

                // 1. Try Hardware D3D11 Acceleration
                try
                {
                    var props = new RenderTargetProperties(
                        RenderTargetType.Default,
                        new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                        96.0f, 96.0f,
                        RenderTargetUsage.GdiCompatible,
                        FeatureLevel.Default
                    );
                    _hwndRenderTarget = _d2dFactory.CreateHwndRenderTarget(props, hwndProps);
                }
                catch (Exception exHw)
                {
                    System.Diagnostics.Debug.WriteLine("D2D Hardware HWND target not available, activating Microsoft WARP: " + exHw.Message);
                    _hwndRenderTarget = null;
                }

                // 2. Fallback to Microsoft WARP (Software SIMD Direct2D - MS Office Standard)
                if (_hwndRenderTarget == null)
                {
                    try
                    {
                        var warpProps = new RenderTargetProperties(
                            RenderTargetType.Software,
                            new PixelFormat(Vortice.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                            96.0f, 96.0f,
                            RenderTargetUsage.GdiCompatible,
                            FeatureLevel.Default
                        );
                        _hwndRenderTarget = _d2dFactory.CreateHwndRenderTarget(warpProps, hwndProps);
                    }
                    catch (Exception exWarp)
                    {
                        System.Diagnostics.Debug.WriteLine("D2D WARP HWND target creation failed: " + exWarp.Message);
                        _hwndRenderTarget = null;
                    }
                }

                if (_hwndRenderTarget != null)
                {
                    _renderTarget = _hwndRenderTarget;
                    try { _gdiInterop = _hwndRenderTarget.QueryInterface<ID2D1GdiInteropRenderTarget>(); } catch { }
                    try { _deviceContext = _hwndRenderTarget.QueryInterfaceOrNull<ID2D1DeviceContext>(); } catch { _deviceContext = null; }
                    _renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
                    _renderTarget.TextAntialiasMode = D2DTextAntialiasMode.Cleartype;
                    _renderTarget.BeginDraw();
                    _isDrawing = true;
                }
                else
                {
                    _gdiGraphics = Graphics.FromHwnd(hwnd);
                    _ownsGraphics = true;
                }
            }
        }

        public static D2DGraphics FromHwnd(IntPtr hwnd)
        {
            return new D2DGraphics(hwnd, 1, 1);
        }

        public static D2DGraphics FromImage(System.Drawing.Image image)
        {
            if (image == null) return null;
            var g = Graphics.FromImage(image);
            // Dùng pureGdi: true cho Bitmap offscreen/Thumbnail để tránh xung đột Resource Domain với GPU Surface
            return new D2DGraphics(g, pureGdi: true, viewportWidth: image.Width, viewportHeight: image.Height, ownsGraphics: true);
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

        public bool LastDrawFailed { get; private set; }
        public void EndDraw()
        {
            if (_inGdiDc)
            {
                ReleaseHdc();
            }
            if (_renderTarget != null && _isDrawing)
            {
                try
                {
                    _renderTarget.EndDraw().CheckError();
                }
                catch (SharpGen.Runtime.SharpGenException ex)
                {
                    LastDrawFailed = true;
                    if (ex.ResultCode == Vortice.Direct2D1.ResultCode.RecreateTarget || ex.HResult == unchecked((int)0x8899000C))
                    {
                        ResetSharedDcTarget();
                    }
                }
                catch
                {
                    LastDrawFailed = true;
                }
                finally
                {
                    _isDrawing = false;
                }
            }
        }

        public void Dispose()
        {
            foreach(var meshBrush in _meshImageBrushes.Values) meshBrush.Dispose();
            _meshImageBrushes.Clear();
            while (_clipDepth > 0 && _renderTarget != null)
            {
                try { PopNativeClip(); } catch { }
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
                    _renderTarget.EndDraw().CheckError(); 
                } 
                catch (SharpGen.Runtime.SharpGenException ex)
                {
                    LastDrawFailed = true;
                    if (ex.ResultCode == Vortice.Direct2D1.ResultCode.RecreateTarget || ex.HResult == unchecked((int)0x8899000C))
                    {
                        ResetSharedDcTarget();
                    }
                }
                catch 
                { 
                }
                finally
                {
                    _isDrawing = false;
                }
            }

            if (_ownsRenderTarget)
            {
                if (_deviceContext != null)
                {
                    try { _deviceContext.Dispose(); } catch { }
                    _deviceContext = null;
                }

                if (_gdiInterop != null)
                {
                    try { _gdiInterop.Dispose(); } catch { }
                    _gdiInterop = null;
                }

                if (_hwndRenderTarget != null)
                {
                    try { _hwndRenderTarget.Dispose(); } catch { }
                    _hwndRenderTarget = null;
                }

                try { _renderTarget?.Dispose(); } catch { }
            }
            else
            {
                _deviceContext = null;
                _gdiInterop = null;
                _hwndRenderTarget = null;
            }
            _renderTarget = null;

            foreach (var bmp in _bitmapCache.Values)
            {
                try { bmp?.Dispose(); } catch { }
            }
            _bitmapCache.Clear();

            if (_isUsingSharedDc)
            {
                lock (_sharedDcLock)
                {
                    _sharedDcInUse = false;
                }
                _dcRenderTarget = null;
                _isUsingSharedDc = false;
            }
            else if (_dcRenderTarget != null)
            {
                try { _dcRenderTarget.Dispose(); } catch { }
                _dcRenderTarget = null;
            }

            if (_ownsGdiHdc && _gdiGraphics != null && _gdiHdc != IntPtr.Zero)
            {
                try { _gdiGraphics.ReleaseHdc(_gdiHdc); } catch { }
                _gdiHdc = IntPtr.Zero;
                _ownsGdiHdc = false;
            }

            if (_ownsGraphics && _gdiGraphics != null)
            {
                try { _gdiGraphics.Dispose(); } catch { }
                _gdiGraphics = null;
                _ownsGraphics = false;
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
                if (_gdiGraphics != null && !_ownsGdiHdc) return _gdiGraphics;
                return null;
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

        private (ID2D1Brush brush, float width, ID2D1StrokeStyle strokeStyle) GetD2DPen(Pen pen, ID2D1Factory customFactory = null)
        {
            if (_renderTarget == null || pen == null) return (null, 1.0f, null);
            ID2D1Brush b = _renderTarget.CreateSolidColorBrush(ToColor4(pen.Color));
            ID2D1StrokeStyle stroke = null;
            var factory = customFactory ?? _renderTarget.Factory ?? (ID2D1Factory)_d2dFactory;
            if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dash)
            {
                try { stroke = factory.CreateStrokeStyle(new StrokeStyleProperties { DashStyle = Vortice.Direct2D1.DashStyle.Dash }); } catch { }
            }
            else if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dot)
            {
                try { stroke = factory.CreateStrokeStyle(new StrokeStyleProperties { DashStyle = Vortice.Direct2D1.DashStyle.Dot }); } catch { }
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
                        ClearFontCache();
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
            _textBaselines.Clear();
            _textInkBounds.Clear();
        }
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string,float,int,GraphicsUnit,string),RectangleF> _textInkBounds = new();
        public static RectangleF MeasureTextInkBounds(string text,Font font)
        {
            if(font==null)return RectangleF.Empty;
            text=(text??"").Replace("\r","").Replace("\n","");
            if(string.IsNullOrWhiteSpace(text))text="Ag";
            var key=(font.Name,font.Size,(int)font.Style,font.Unit,text);
            if(_textInkBounds.TryGetValue(key,out var cached))return cached;
            try
            {
                const float extent=10000;
                using var layout=_dwriteFactory.CreateTextLayout(text,GetTextFormat(font),extent,extent);
                layout.WordWrapping=WordWrapping.NoWrap;
                var ink=layout.OverhangMetrics;
                var bounds=RectangleF.FromLTRB(-ink.Left,-ink.Top,extent+ink.Right,extent+ink.Bottom);
                if(_textInkBounds.Count>=512)_textInkBounds.Clear();
                _textInkBounds[key]=bounds;return bounds;
            }
            catch{return new RectangleF(0,0,font.Size,font.Size*1.22f);}
        }

        private static IDWriteTextFormat GetTextFormat(Font font)
        {
            if (font == null || _dwriteFactory == null) return null;
            string famName = font.FontFamily?.Name ?? font.Name;
            string fontName = font.Name ?? famName;
            // Writer stores document fonts in points, while equation layout uses pixels.
            // Key by the physical size used by DirectWrite, so 14 pt cannot reuse 14 px.
            float fontSizeDip = font.Unit == GraphicsUnit.Pixel ? font.Size : font.SizeInPoints * (96f / 72f);
            var key = (famName,fontName,(float)Math.Round(fontSizeDip,3),(int)font.Style);
            if (_textFormatCache.Count > 4096) _textFormatCache.Clear();
            return _textFormatCache.GetOrAdd(key, _ =>
            {
                var style = font.Italic ? Vortice.DirectWrite.FontStyle.Italic : Vortice.DirectWrite.FontStyle.Normal;

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

        public static ID2D1PathGeometry ConvertPath(GraphicsPath path, ID2D1Factory customFactory = null)
        {
            var factory = customFactory ?? (ID2D1Factory)_d2dFactory;
            if (path == null || factory == null || path.PointCount == 0) return null;
            try
            {
                var geometry = factory.CreatePathGeometry();
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
        // Key includes the owning render target: a D2D bitmap is only valid on the target (resource domain)
        // that created it. Previously the GPU surface path and the shared DC path shared one Image-keyed cache,
        // so bitmaps created on one target were drawn on the other (D2DERR_WRONG_RESOURCE_DOMAIN -> crash),
        // and every target switch flushed the whole cache (re-uploading all images -> lag).
        private readonly record struct SharedBmpKey(IntPtr Target, Image Image);
        private static readonly Dictionary<SharedBmpKey, ID2D1Bitmap> _sharedBitmapCache = new Dictionary<SharedBmpKey, ID2D1Bitmap>();
        private static readonly LinkedList<SharedBmpKey> _sharedBitmapLruList = new LinkedList<SharedBmpKey>();
        private static readonly Dictionary<SharedBmpKey, LinkedListNode<SharedBmpKey>> _sharedBitmapLruNodes = new Dictionary<SharedBmpKey, LinkedListNode<SharedBmpKey>>();
        private static readonly object _sharedBitmapCacheLock = new object();
        private const int MaxSharedBitmapCacheSize = 256;

        private static void SharedCacheRemoveWhere_NoLock(Func<SharedBmpKey, bool> pred)
        {
            var toRemove = new List<SharedBmpKey>();
            foreach (var k in _sharedBitmapCache.Keys) if (pred(k)) toRemove.Add(k);
            foreach (var k in toRemove)
            {
                if (_sharedBitmapCache.Remove(k, out var b)) { try { b?.Dispose(); } catch { } }
                if (_sharedBitmapLruNodes.Remove(k, out var node)) _sharedBitmapLruList.Remove(node);
            }
        }

        private static void SharedCacheTouch_NoLock(SharedBmpKey key)
        {
            if (_sharedBitmapLruNodes.TryGetValue(key, out var node))
            {
                _sharedBitmapLruList.Remove(node);
                _sharedBitmapLruList.AddLast(node);
            }
        }

        private static void SharedCacheAdd_NoLock(SharedBmpKey key, ID2D1Bitmap bmp)
        {
            if (_sharedBitmapCache.TryGetValue(key, out var existing) && !ReferenceEquals(existing, bmp))
            {
                try { existing?.Dispose(); } catch { }
            }
            while (_sharedBitmapCache.Count >= MaxSharedBitmapCacheSize && _sharedBitmapLruList.First != null)
            {
                var oldest = _sharedBitmapLruList.First.Value;
                _sharedBitmapLruList.RemoveFirst();
                _sharedBitmapLruNodes.Remove(oldest);
                if (_sharedBitmapCache.Remove(oldest, out var oldBmp)) { try { oldBmp?.Dispose(); } catch { } }
            }
            _sharedBitmapCache[key] = bmp;
            if (_sharedBitmapLruNodes.TryGetValue(key, out var n)) _sharedBitmapLruList.Remove(n);
            _sharedBitmapLruNodes[key] = _sharedBitmapLruList.AddLast(key);
        }

        public static void InvalidateBitmapCache(Image image)
        {
            if (image == null) return;
            lock (_sharedBitmapCacheLock)
            {
                SharedCacheRemoveWhere_NoLock(k => ReferenceEquals(k.Image, image));
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
                _sharedBitmapLruNodes.Clear();
                _sharedBitmapOwnerTarget = IntPtr.Zero;
            }
            lock (_pathBitmapCacheLock)
            {
                foreach (var bmp in _pathBitmapCache.Values)
                {
                    try { bmp?.Dispose(); } catch { }
                }
                _pathBitmapCache.Clear();
                _pathBitmapLru.Clear();
                foreach (var g in _pathGifCache.Values)
                {
                    if (g.Frames != null)
                    {
                        foreach (var f in g.Frames) { try { f?.Dispose(); } catch { } }
                    }
                }
                _pathGifCache.Clear();
            }
        }

        private ID2D1Bitmap GetOrCreateD2DBitmap(Image image)
        {
            if (image == null || _renderTarget == null) return null;

            if (_usePersistentBitmapCache && _renderTarget != null)
            {
                lock (_sharedBitmapCacheLock)
                {
                    IntPtr curTarget = _renderTarget.NativePointer;
                    // Keys already include the render target. Presenter and audience surfaces
                    // must retain their own textures rather than evicting each other every paint.
                    var key = new SharedBmpKey(curTarget, image);
                    if (_sharedBitmapCache.TryGetValue(key, out var cachedShared))
                    {
                        if (cachedShared != null && cachedShared.NativePointer != IntPtr.Zero)
                        {
                            SharedCacheTouch_NoLock(key);
                            return cachedShared;
                        }
                        SharedCacheRemoveWhere_NoLock(k => k.Equals(key));
                    }
                }
            }
            else if (_isUsingSharedDc)
            {
                lock (_sharedBitmapCacheLock)
                {
                    var key = new SharedBmpKey(_renderTarget.NativePointer, image);
                    if (_sharedBitmapCache.TryGetValue(key, out var cachedShared))
                    {
                        if (cachedShared != null && cachedShared.NativePointer != IntPtr.Zero)
                        {
                            SharedCacheTouch_NoLock(key);
                            return cachedShared;
                        }
                        SharedCacheRemoveWhere_NoLock(k => k.Equals(key));
                    }
                }
            }
            else
            {
                if (_bitmapCache.TryGetValue(image, out var cached) && cached != null && cached.NativePointer != IntPtr.Zero) return cached;
            }

            ID2D1Bitmap createdBmp = null;

            if (image is Bitmap bmp)
            {
                lock (bmp)
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
                        else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb || bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
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
                        else
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
                if ((_usePersistentBitmapCache || _isUsingSharedDc) && _renderTarget != null)
                {
                    lock (_sharedBitmapCacheLock)
                    {
                        SharedCacheAdd_NoLock(new SharedBmpKey(_renderTarget.NativePointer, image), createdBmp);
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
        // Axis clips and geometry masks must be popped in the same order they were pushed.
        private readonly List<ID2D1PathGeometry> _nativeClipMasks = new();
        private void PopNativeClip()
        {
            int index = _nativeClipMasks.Count - 1;
            if (index < 0) return;
            var mask = _nativeClipMasks[index];
            if (mask == null) _renderTarget.PopAxisAlignedClip();
            else { _renderTarget.PopLayer(); mask.Dispose(); }
            _nativeClipMasks.RemoveAt(index);
        }
        private static bool IsAxisAlignedRectangle(GraphicsPath path)
        {
            var points=path.PathPoints;var types=path.PathTypes;
            int n=points.Length;if(n==5&&points[0]==points[4])n=4;if(n!=4)return false;
            if((types[0]&7)!=0||(types[points.Length-1]&128)==0)return false;
            for(int i=1;i<points.Length;i++)if((types[i]&7)!=1)return false;
            for(int i=0;i<4;i++){var a=points[i];var b=points[(i+1)%4];if(a.X!=b.X&&a.Y!=b.Y)return false;}
            var r=path.GetBounds();return r.Width>0&&r.Height>0&&Math.Abs((points[1].X-points[0].X)*(points[2].Y-points[1].Y)-(points[1].Y-points[0].Y)*(points[2].X-points[1].X))>0;
        }
        private void PushPathClip(GraphicsPath path, bool replace, AntialiasMode antialias = AntialiasMode.PerPrimitive)
        {
            if (replace) ResetClip();
            // Rectangular picture fills need only a scissor, rather than an offscreen mask layer.
            if (Math.Abs(_currentTransform.M12) < .00001f && Math.Abs(_currentTransform.M21) < .00001f && IsAxisAlignedRectangle(path))
            {
                var r = path.GetBounds();
                _renderTarget.PushAxisAlignedClip(new Vortice.RawRectF(r.Left,r.Top,r.Right,r.Bottom),AntialiasMode.Aliased);
                _nativeClipMasks.Add(null);_clipDepth++;return;
            }
            var geometry = ConvertPath(path, _renderTarget.Factory ?? (ID2D1Factory)_d2dFactory);
            if (geometry == null) return;
            var bounds = path.GetBounds();
            var parameters = new LayerParameters {
                ContentBounds = new Vortice.RawRectF(bounds.Left - 1, bounds.Top - 1, bounds.Right + 1, bounds.Bottom + 1),
                GeometricMask = geometry, MaskAntialiasMode = antialias,
                MaskTransform = System.Numerics.Matrix3x2.Identity, Opacity = 1f,
                LayerOptions = LayerOptions.None
            };
            _renderTarget.PushLayer(parameters, null);
            _nativeClipMasks.Add(geometry);
            _clipDepth++;
        }


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
                    try
                    {
                        _renderTarget.FillRoundedRectangle(new RoundedRectangle { Rect = new Vortice.Mathematics.Rect(rect.X, rect.Y, rect.Width, rect.Height), RadiusX = rx, RadiusY = ry }, b);
                        return;
                    }
                    catch { }
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
                var factory = _renderTarget.Factory ?? (ID2D1Factory)_d2dFactory;
                var (b, w, stroke) = GetD2DPen(pen, factory);
                if (b != null)
                {
                    float rx = Math.Min(radius, rect.Width / 2f);
                    float ry = Math.Min(radius, rect.Height / 2f);
                    try
                    {
                        _renderTarget.DrawRoundedRectangle(new RoundedRectangle { Rect = new Vortice.Mathematics.Rect(rect.X, rect.Y, rect.Width, rect.Height), RadiusX = rx, RadiusY = ry }, b, w, stroke);
                        return;
                    }
                    catch { }
                    finally
                    {
                        b.Dispose();
                        stroke?.Dispose();
                    }
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
        internal float? MathBaseline;
        internal float MathLineTop;
        internal bool DrawingMath;
        internal int MathTextStart = -1;
        internal Action<int,char,RectangleF> MathPainted;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string,float,System.Drawing.FontStyle,GraphicsUnit),float> _textBaselines = new();
        internal float TextBaseline(Font font)
        {
            var key=(font.Name,font.Size,font.Style,font.Unit);
            if(_textBaselines.TryGetValue(key,out float baseline))return baseline;
            try
            {
                using var layout = _dwriteFactory.CreateTextLayout("Mg", GetTextFormat(font), 10000, 10000);
                var lines = layout.LineMetrics;
                if (lines.Length > 0) { _textBaselines[key]=lines[0].Baseline; return lines[0].Baseline; }
            }
            catch { }
            return (font.Unit == GraphicsUnit.Pixel ? font.Size : font.SizeInPoints * 96f / 72f) * .9f;
        }
        public void DrawString(string s, Font font, Brush brush, float x, float y)
        {
            if(!string.IsNullOrEmpty(s)&&font!=null&&WriterMath.TryDraw(this,s,font,brush,new RectangleF(x,y,100000,100000),null))return;
            if (string.IsNullOrEmpty(s)) return;
            if (_renderTarget != null)
            {
                using var b = GetD2DBrush(brush);
                var tf = GetTextFormat(font);
                if (b != null && tf != null)
                {
                    try
                    {
                        _renderTarget.DrawText(s, tf, new Vortice.Mathematics.Rect(x, y, 10000.0f, 10000.0f), b, DrawTextOptions.NoSnap);
                        return;
                    }
                    catch { }
                }
            }
            else if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawString(s, font, brush, x, y);
        }

        public void DrawString(string s, Font font, Brush brush, PointF point) => DrawString(s, font, brush, point.X, point.Y);

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format)
            => DrawStringCore(s, font, brush, layoutRectangle, format, false, 0f);

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format, float characterSpacing)
            => DrawStringCore(s, font, brush, layoutRectangle, format, false, characterSpacing);

        private void DrawStringCore(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format, bool forceNoWrap, float characterSpacing = 0f)
        {
            if(!string.IsNullOrEmpty(s)&&font!=null&&WriterMath.TryDraw(this,s,font,brush,layoutRectangle,format))return;
            if (string.IsNullOrEmpty(s)) return;
            if (_renderTarget != null)
            {
                using var b = GetD2DBrush(brush);
                var tf = GetTextFormat(font);
                if (b != null && tf != null)
                {
                    float w = Math.Max(1.0f, layoutRectangle.Width);
                    float h = Math.Max(1.0f, layoutRectangle.Height);
                    try
                    {
                        using var layout = _dwriteFactory.CreateTextLayout(s, tf, w, h);
                        if (characterSpacing != 0f)
                        {
                            using var layout1 = layout.QueryInterfaceOrNull<IDWriteTextLayout1>();
                            if (layout1 != null)
                            {
                                layout1.SetCharacterSpacing(0f, characterSpacing, 0f, new TextRange(0, (uint)s.Length));
                            }
                        }
                        if (forceNoWrap) layout.WordWrapping = WordWrapping.NoWrap;
                        if (format != null)
                        {
                            if ((format.FormatFlags & StringFormatFlags.NoWrap) != 0)
                            {
                                layout.WordWrapping = WordWrapping.NoWrap;
                            }

                            if (format.Alignment == StringAlignment.Center) layout.TextAlignment = TextAlignment.Center;
                            else if (format.Alignment == StringAlignment.Far) layout.TextAlignment = TextAlignment.Trailing;
                            else layout.TextAlignment = TextAlignment.Leading;

                            if (format.LineAlignment == StringAlignment.Center) layout.ParagraphAlignment = ParagraphAlignment.Center;
                            else if (format.LineAlignment == StringAlignment.Far) layout.ParagraphAlignment = ParagraphAlignment.Far;
                            else layout.ParagraphAlignment = ParagraphAlignment.Near;

                            if (format.Trimming == StringTrimming.EllipsisCharacter ||
                                format.Trimming == StringTrimming.EllipsisWord ||
                                format.Trimming == StringTrimming.EllipsisPath)
                            {
                                try
                                {
                                    var trimming = new Trimming
                                    {
                                        Granularity = (format.Trimming == StringTrimming.EllipsisWord)
                                            ? TrimmingGranularity.Word
                                            : TrimmingGranularity.Character,
                                        Delimiter = 0,
                                        DelimiterCount = 0
                                    };
                                    using var sign = _dwriteFactory.CreateEllipsisTrimmingSign(tf);
                                    layout.SetTrimming(trimming, sign);
                                }
                                catch { }
                            }
                        }
                        _renderTarget.DrawTextLayout(new System.Numerics.Vector2(layoutRectangle.X, layoutRectangle.Y), layout, b, DrawTextOptions.NoSnap);
                        return;
                    }
                    catch
                    {
                        try
                        {
                            _renderTarget.DrawText(s, tf, new Vortice.Mathematics.Rect(layoutRectangle.X, layoutRectangle.Y, w, h), b, DrawTextOptions.NoSnap);
                            return;
                        }
                        catch { }
                    }
                }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawString(s, font, brush, layoutRectangle, format);
        }

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle) => DrawString(s, font, brush, layoutRectangle, null, 0f);
        public void DrawString(string s, Font font, Brush brush, Rectangle layoutRectangle) => DrawString(s, font, brush, (RectangleF)layoutRectangle, null, 0f);
        public void DrawString(string s, Font font, Brush brush, Rectangle layoutRectangle, StringFormat format) => DrawString(s, font, brush, (RectangleF)layoutRectangle, format, 0f);

        public void DrawString(string s, Font font, Brush brush, float x, float y, StringFormat format)
            => DrawString(s, font, brush, x, y, format, 0f);

        public void DrawString(string s, Font font, Brush brush, float x, float y, float characterSpacing)
            => DrawString(s, font, brush, x, y, null, characterSpacing);

        public void DrawString(string s, Font font, Brush brush, float x, float y, StringFormat format, float characterSpacing)
        {
            if(!DrawingMath&&!string.IsNullOrEmpty(s)&&font!=null&&(MathBaseline.HasValue||WriterMath.IsMathText(s,font)))
            {
                var mathSize=WriterMath.Measure(this,s,font);
                float mathX=x,mathY=y;
                if(format?.Alignment==StringAlignment.Center)mathX-=mathSize.Width/2;
                else if(format?.Alignment==StringAlignment.Far)mathX-=mathSize.Width;
                if(format?.LineAlignment==StringAlignment.Center)mathY-=mathSize.Height/2;
                else if(format?.LineAlignment==StringAlignment.Far)mathY-=mathSize.Height;
                if(WriterMath.TryDraw(this,s,font,brush,new RectangleF(mathX,mathY,mathSize.Width,mathSize.Height),null))return;
            }
            if (string.IsNullOrEmpty(s)) return;
            if (format != null)
            {
                SizeF sz = MeasureString(s, font, characterSpacing);
                float layoutX = x;
                float layoutY = y;
                if (format.Alignment == StringAlignment.Center) layoutX = x - sz.Width / 2.0f;
                else if (format.Alignment == StringAlignment.Far) layoutX = x - sz.Width;

                if (format.LineAlignment == StringAlignment.Center) layoutY = y - sz.Height / 2.0f;
                else if (format.LineAlignment == StringAlignment.Far) layoutY = y - sz.Height;

                float layoutW = (format.Alignment == StringAlignment.Center || format.Alignment == StringAlignment.Far)
                    ? Math.Max(1.0f, sz.Width + 20.0f)
                    : 10000.0f;
                float layoutH = (format.LineAlignment == StringAlignment.Center || format.LineAlignment == StringAlignment.Far)
                    ? Math.Max(1.0f, sz.Height + 20.0f)
                    : 10000.0f;

                DrawStringCore(s, font, brush, new RectangleF(layoutX, layoutY, layoutW, layoutH), format, true, characterSpacing);
            }
            else
            {
                DrawStringCore(s, font, brush, new RectangleF(x, y, 10000.0f, 10000.0f), null, true, characterSpacing);
            }
        }

        public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format) => DrawString(s, font, brush, point.X, point.Y, format, 0f);

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<float, IDWriteTextFormat> _iconTextFormatCache =
            new System.Collections.Concurrent.ConcurrentDictionary<float, IDWriteTextFormat>();

        public static IDWriteTextFormat GetIconTextFormat(float fontSizeDip)
        {
            if (_dwriteFactory == null) return null;
            float rounded = (float)Math.Round(fontSizeDip, 1);
            return _iconTextFormatCache.GetOrAdd(rounded, s =>
            {
                string fontFam = FluentIconRenderer.FontFamilyName;
                IDWriteTextFormat tf = null;
                if (_customFontCollection != null)
                {
                    try
                    {
                        if (_customFontCollection.FindFamilyName(fontFam, out _))
                        {
                            tf = _dwriteFactory.CreateTextFormat(fontFam, _customFontCollection, FontWeight.Regular, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal, s);
                        }
                    }
                    catch { }
                }
                if (tf == null)
                {
                    try
                    {
                        tf = _dwriteFactory.CreateTextFormat(fontFam, null, FontWeight.Regular, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal, s);
                    }
                    catch { }
                }
                if (tf == null)
                {
                    try
                    {
                        tf = _dwriteFactory.CreateTextFormat("Segoe MDL2 Assets", null, FontWeight.Regular, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal, s);
                    }
                    catch { }
                }
                if (tf == null)
                {
                    try
                    {
                        tf = _dwriteFactory.CreateTextFormat("Segoe UI", null, FontWeight.Regular, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal, s);
                    }
                    catch { }
                }
                if (tf != null)
                {
                    tf.TextAlignment = TextAlignment.Center;
                    tf.ParagraphAlignment = ParagraphAlignment.Center;
                }
                return tf;
            });
        }

        public void DrawGlyph(char glyph, RectangleF bounds, Color color, float fontSize = 0)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var target = _renderTarget ?? _dcRenderTarget;
            if (target != null)
            {
                if (glyph == '\uF51E')
                {
                    // Align Justify: 3 crisp vector bars rendered directly on GPU
                    float barW = bounds.Width * 0.65f;
                    float startX = bounds.X + (bounds.Width - barW) / 2f;
                    float barH = Math.Max(1.2f, bounds.Height * 0.08f);
                    float startY = bounds.Y + bounds.Height * 0.28f;
                    float stepY = bounds.Height * 0.22f;
                    using var b = target.CreateSolidColorBrush(ToColor4(color));
                    for (int i = 0; i < 3; i++)
                    {
                        target.FillRectangle(new Vortice.Mathematics.Rect(startX, startY + i * stepY, barW, barH), b);
                    }
                    return;
                }

                float fontSz = (fontSize > 0) ? Math.Min(fontSize, bounds.Height * 0.88f) : (bounds.Height * 0.75f);
                var tf = GetIconTextFormat(fontSz);
                if (tf != null)
                {
                    using var b = target.CreateSolidColorBrush(ToColor4(color));
                    var rect = new Vortice.Mathematics.Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    target.DrawText(glyph.ToString(), tf, rect, b, DrawTextOptions.None);
                    return;
                }
            }

            if (_gdiGraphics != null && !_ownsHdc)
            {
                var bmp = FluentIconRenderer.GetGlyphBitmap(glyph, (int)bounds.Width, (int)bounds.Height, color, fontSize);
                if (bmp != null)
                {
                    _gdiGraphics.DrawImage(bmp, new Rectangle((int)Math.Round(bounds.X), (int)Math.Round(bounds.Y), (int)bounds.Width, (int)bounds.Height), 0, 0, (int)bounds.Width, (int)bounds.Height, GraphicsUnit.Pixel);
                }
            }
        }

        public void DrawGlyph(char glyph, Rectangle bounds, Color color, float fontSize = 0)
            => DrawGlyph(glyph, (RectangleF)bounds, color, fontSize);

        public void DrawGlyph(char glyph, int x, int y, int size, Color color, float fontSize = 0)
            => DrawGlyph(glyph, new RectangleF(x, y, size, size), color, fontSize);

        public SizeF MeasureString(string text, Font font)
            => MeasureString(text, font, 0f);

        public SizeF MeasureString(string text, Font font, float characterSpacing)
        {
            if(!DrawingMath&&font!=null&&WriterMath.IsMathText(text,font))return WriterMath.Measure(this,text,font);
            if (string.IsNullOrEmpty(text) || font == null) return SizeF.Empty;
            try
            {
                var tf = GetTextFormat(font);
                if (tf != null)
                {
                    using var layout = _dwriteFactory.CreateTextLayout(text, tf, 10000.0f, 10000.0f);
                    if (characterSpacing != 0f)
                    {
                        using var layout1 = layout.QueryInterfaceOrNull<IDWriteTextLayout1>();
                        if (layout1 != null)
                        {
                            layout1.SetCharacterSpacing(0f, characterSpacing, 0f, new TextRange(0, (uint)text.Length));
                        }
                    }
                    var m = layout.Metrics;
                    return new SizeF(m.WidthIncludingTrailingWhitespace, m.Height);
                }
            }
            catch { }
            if (_gdiGraphics != null && !_ownsHdc)
            {
                var sz = _gdiGraphics.MeasureString(text, font);
                return new SizeF(Math.Max(0f, sz.Width + text.Length * characterSpacing), sz.Height);
            }
            return SizeF.Empty;
        }

        public SizeF MeasureString(string text, Font font, int width)
            => MeasureString(text, font, width, 0f);

        public SizeF MeasureString(string text, Font font, int width, float characterSpacing)
        {
            if(!DrawingMath&&font!=null&&WriterMath.IsMathText(text,font))return WriterMath.Measure(this,text,font);
            if (string.IsNullOrEmpty(text) || font == null) return SizeF.Empty;
            try
            {
                var tf = GetTextFormat(font);
                if (tf != null)
                {
                    using var layout = _dwriteFactory.CreateTextLayout(text, tf, Math.Max(1, width), 10000.0f);
                    if (characterSpacing != 0f)
                    {
                        using var layout1 = layout.QueryInterfaceOrNull<IDWriteTextLayout1>();
                        if (layout1 != null)
                        {
                            layout1.SetCharacterSpacing(0f, characterSpacing, 0f, new TextRange(0, (uint)text.Length));
                        }
                    }
                    var m = layout.Metrics;
                    return new SizeF(m.WidthIncludingTrailingWhitespace, m.Height);
                }
            }
            catch { }
            return (_gdiGraphics != null && !_ownsHdc) ? _gdiGraphics.MeasureString(text, font, width) : SizeF.Empty;
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea) => MeasureString(text, font, (int)layoutArea.Width, 0f);
        public SizeF MeasureString(string text, Font font, int width, StringFormat stringFormat) => MeasureString(text, font, width, 0f);
        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat) => MeasureString(text, font, (int)layoutArea.Width, 0f);
        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat, out int charactersFitted, out int linesFilled)
        {
            charactersFitted = text?.Length ?? 0;
            linesFilled = 1;
            return MeasureString(text, font, (int)layoutArea.Width, 0f);
        }
        public SizeF MeasureString(string text, Font font, PointF origin, StringFormat stringFormat) => MeasureString(text, font, 0f);
        public SizeF MeasureString(string text, Font font, PointF origin, StringFormat stringFormat, float characterSpacing) => MeasureString(text, font, characterSpacing);

        private float _currentImageOpacity = 1.0f;
        public float CurrentImageOpacity
        {
            get => _currentImageOpacity;
            set => _currentImageOpacity = Math.Max(0.0f, Math.Min(1.0f, value));
        }

        // --- Hardware Direct2D Bitmap Acceleration ---
        private void DrawD2DBitmapInternal(ID2D1Bitmap d2dBmp, Vortice.Mathematics.Rect destRect, float opacity, Vortice.Mathematics.Rect? srcRect = null)
        {
            if (d2dBmp == null || d2dBmp.NativePointer == IntPtr.Zero) return;
            if (_deviceContext != null)
            {
                Vortice.Direct2D1.InterpolationMode interpMode = Vortice.Direct2D1.InterpolationMode.HighQualityCubic;
                if (_interpolationMode == System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor)
                {
                    interpMode = Vortice.Direct2D1.InterpolationMode.NearestNeighbor;
                }
                else if (_interpolationMode == System.Drawing.Drawing2D.InterpolationMode.Low)
                {
                    interpMode = Vortice.Direct2D1.InterpolationMode.Linear;
                }
                else
                {
                    float srcW = srcRect.HasValue ? srcRect.Value.Width : d2dBmp.Size.Width;
                    float srcH = srcRect.HasValue ? srcRect.Value.Height : d2dBmp.Size.Height;
                    bool isUnrotated = Math.Abs(_currentTransform.M12) < 0.0001f && Math.Abs(_currentTransform.M21) < 0.0001f &&
                                       Math.Abs(_currentTransform.M11 - 1.0f) < 0.001f && Math.Abs(_currentTransform.M22 - 1.0f) < 0.001f;
                    bool isExactSize = Math.Abs(srcW - destRect.Width) < 0.5f && Math.Abs(srcH - destRect.Height) < 0.5f;
                    interpMode = (isUnrotated && isExactSize)
                        ? Vortice.Direct2D1.InterpolationMode.MultiSampleLinear
                        : Vortice.Direct2D1.InterpolationMode.HighQualityCubic;
                }
                _deviceContext.DrawBitmap(d2dBmp, destRect, opacity, interpMode, srcRect, null);
                return;
            }
            _renderTarget.DrawBitmap(d2dBmp, destRect, opacity, BitmapInterpolationMode.Linear, srcRect);
        }

        public void DrawD2DBitmap(ID2D1Bitmap d2dBmp, float x, float y, float width, float height, float opacity = -1.0f)
        {
            if (d2dBmp == null) return;
            float finalOp = (opacity >= 0.0f) ? Math.Max(0.0f, Math.Min(1.0f, opacity)) : _currentImageOpacity;
            if (_renderTarget != null)
            {
                DrawD2DBitmapInternal(d2dBmp, new Vortice.Mathematics.Rect(x, y, width, height), finalOp, null);
            }
        }

        public void DrawD2DBitmap(ID2D1Bitmap d2dBmp, RectangleF destRect, RectangleF? srcRect = null, float opacity = -1.0f)
        {
            if (d2dBmp == null) return;
            float finalOp = (opacity >= 0.0f) ? Math.Max(0.0f, Math.Min(1.0f, opacity)) : _currentImageOpacity;
            if (_renderTarget != null)
            {
                Vortice.Mathematics.Rect dRect = new Vortice.Mathematics.Rect(destRect.X, destRect.Y, destRect.Width, destRect.Height);
                Vortice.Mathematics.Rect? sRect = srcRect.HasValue
                    ? new Vortice.Mathematics.Rect(srcRect.Value.X, srcRect.Value.Y, srcRect.Value.Width, srcRect.Value.Height)
                    : null;
                DrawD2DBitmapInternal(d2dBmp, dRect, finalOp, sRect);
            }
        }

        public void DrawD2DBitmap(ID2D1Bitmap d2dBmp, PointF[] destPoints, RectangleF? srcRect = null, float opacity = -1.0f)
        {
            if (d2dBmp == null) return;
            float finalOp = (opacity >= 0.0f) ? Math.Max(0.0f, Math.Min(1.0f, opacity)) : _currentImageOpacity;
            if (_renderTarget != null)
            {
                float destX = 0, destY = 0, destW = d2dBmp.Size.Width, destH = d2dBmp.Size.Height;
                if (destPoints != null && destPoints.Length >= 3)
                {
                    destX = destPoints[0].X;
                    destY = destPoints[0].Y;
                    destW = destPoints[1].X - destPoints[0].X;
                    destH = destPoints[2].Y - destPoints[0].Y;
                }
                if (destW < 0) { destX += destW; destW = -destW; }
                if (destH < 0) { destY += destH; destH = -destH; }

                Vortice.Mathematics.Rect dRect = new Vortice.Mathematics.Rect(destX, destY, destW, destH);
                Vortice.Mathematics.Rect? sRect = srcRect.HasValue
                    ? new Vortice.Mathematics.Rect(srcRect.Value.X, srcRect.Value.Y, srcRect.Value.Width, srcRect.Value.Height)
                    : null;
                DrawD2DBitmapInternal(d2dBmp, dRect, finalOp, sRect);
            }
        }

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
                        DrawD2DBitmapInternal(d2dBmp, new Vortice.Mathematics.Rect(x, y, image.Width, image.Height), finalOp, null);
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc)
            {
                try { _gdiGraphics.DrawImage(image, x, y); } catch { }
            }
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
                        DrawD2DBitmapInternal(d2dBmp, new Vortice.Mathematics.Rect(x, y, width, height), finalOp, null);
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc)
            {
                try { _gdiGraphics.DrawImage(image, x, y, width, height); } catch { }
            }
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
                        DrawD2DBitmapInternal(
                            d2dBmp,
                            new Vortice.Mathematics.Rect(destRect.X, destRect.Y, destRect.Width, destRect.Height),
                            finalOp,
                            new Vortice.Mathematics.Rect(srcX, srcY, srcWidth, srcHeight)
                        );
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc)
            {
                try { _gdiGraphics.DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit); } catch { }
            }
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
                        DrawD2DBitmapInternal(
                            d2dBmp,
                            new Vortice.Mathematics.Rect(destRect.X, destRect.Y, destRect.Width, destRect.Height),
                            finalOp,
                            new Vortice.Mathematics.Rect(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height)
                        );
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc)
            {
                try { _gdiGraphics.DrawImage(image, destRect, srcRect, srcUnit); } catch { }
            }
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
                        if (destPoints == null || destPoints.Length < 3 || srcRect.Width <= 0 || srcRect.Height <= 0) return;
                        var p = destPoints;
                        using var mapping = new Matrix(
                            (p[1].X - p[0].X) / srcRect.Width, (p[1].Y - p[0].Y) / srcRect.Width,
                            (p[2].X - p[0].X) / srcRect.Height, (p[2].Y - p[0].Y) / srcRect.Height,
                            p[0].X, p[0].Y);
                        if (Math.Abs(mapping.Elements[0] * mapping.Elements[3] - mapping.Elements[1] * mapping.Elements[2]) < 0.000001f) return;
                        var state = Save();
                        try
                        {
                            MultiplyTransform(mapping);
                            DrawD2DBitmapInternal(d2dBmp,
                                new Vortice.Mathematics.Rect(0, 0, srcRect.Width, srcRect.Height), finalOp,
                                new Vortice.Mathematics.Rect(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height));
                        }
                        finally { Restore(state); }
                        return;
                    }
                }
                catch
                {
                }
            }
            if (_gdiGraphics != null && !_ownsHdc)
            {
                try { _gdiGraphics.DrawImage(image, destPoints, srcRect, srcUnit, imageAttr); } catch { }
            }
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
                        DrawD2DBitmapInternal(
                            d2dBmp,
                            new Vortice.Mathematics.Rect(destRect.X, destRect.Y, destRect.Width, destRect.Height),
                            finalOp,
                            new Vortice.Mathematics.Rect(srcX, srcY, srcWidth, srcHeight)
                        );
                        return;
                    }
                }
                catch { }
            }
            if (_gdiGraphics != null && !_ownsHdc)
            {
                try { _gdiGraphics.DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttr); } catch { }
            }
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr, float opacity = -1.0f)
            => DrawImage(image, destRect, (int)srcX, (int)srcY, (int)srcWidth, (int)srcHeight, srcUnit, imageAttr, opacity);

        // --- Vector Geometry Paths ---
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            if (_renderTarget != null && pen != null && path != null)
            {
                var factory = _renderTarget.Factory ?? (ID2D1Factory)_d2dFactory;
                using var geom = ConvertPath(path, factory);
                var (b, w, stroke) = GetD2DPen(pen, factory);
                if (b != null && geom != null)
                {
                    try
                    {
                        _renderTarget.DrawGeometry(geom, b, w, stroke);
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("DrawPath D2D error: " + ex.Message);
                    }
                    finally
                    {
                        b.Dispose();
                        stroke?.Dispose();
                    }
                }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.DrawPath(pen, path);
        }

        public void FillPath(Brush brush, GraphicsPath path)
        {
            if (_renderTarget != null && brush != null && path != null)
            {
                var factory = _renderTarget.Factory ?? (ID2D1Factory)_d2dFactory;
                using var geom = ConvertPath(path, factory);
                using var b = GetD2DBrush(brush);
                if (b != null && geom != null)
                {
                    try
                    {
                        _renderTarget.FillGeometry(geom, b);
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("FillPath D2D error: " + ex.Message);
                    }
                }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.FillPath(brush, path);
        }

        public void FillRegion(Brush brush, Region region)
        {
            if (brush == null || region == null) return;
            if (_renderTarget != null)
            {
                try
                {
                    using (var mx = new Matrix())
                    {
                        RectangleF[] rects = region.GetRegionScans(mx);
                        if (rects != null && rects.Length > 0)
                        {
                            foreach (var rf in rects)
                            {
                                FillRectangle(brush, rf);
                            }
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("D2D FillRegion error: " + ex.Message);
                }
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.FillRegion(brush, region);
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
                        try { PopNativeClip(); } catch { }
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
                    try { _gdiGraphics.SmoothingMode = value; } catch { }
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
                        : (value == TextRenderingHint.ClearTypeGridFit ? D2DTextAntialiasMode.Cleartype : D2DTextAntialiasMode.Grayscale);
                }
                else if (_gdiGraphics != null && !_ownsHdc)
                {
                    try { _gdiGraphics.TextRenderingHint = value; } catch { }
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
                if (_gdiGraphics != null && !_ownsHdc)
                {
                    try { _gdiGraphics.InterpolationMode = value; } catch { }
                }
            }
        }

        private PixelOffsetMode _pixelOffsetMode = PixelOffsetMode.Default;
        public PixelOffsetMode PixelOffsetMode
        {
            get => _pixelOffsetMode;
            set
            {
                _pixelOffsetMode = value;
                if (_gdiGraphics != null && !_ownsHdc)
                {
                    try { _gdiGraphics.PixelOffsetMode = value; } catch { }
                }
            }
        }

        private CompositingQuality _compositingQuality = CompositingQuality.Default;
        public CompositingQuality CompositingQuality
        {
            get => _compositingQuality;
            set
            {
                _compositingQuality = value;
                if (_gdiGraphics != null && !_ownsHdc)
                {
                    try { _gdiGraphics.CompositingQuality = value; } catch { }
                }
            }
        }

        private RectangleF _currentClipBounds = RectangleF.Empty;

        public RectangleF ClipBounds
        {
            get
            {
                if (_clipDepth > 0 && !_currentClipBounds.IsEmpty) return _currentClipBounds;
                if (_renderTarget != null)
                {
                    var sz = _renderTarget.Size;
                    return new RectangleF(0, 0, sz.Width, sz.Height);
                }
                return _gdiGraphics != null ? _gdiGraphics.ClipBounds : RectangleF.Empty;
            }
        }

        // --- Clipping Acceleration ---
        public void SetClip(Rectangle rect) => SetClip(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
        public void SetClip(RectangleF rect)
        {
            _currentClipBounds = rect;
            if (_renderTarget != null)
            {
                int baseClip = (_d2dStateStack.Count > 0) ? _d2dStateStack[_d2dStateStack.Count - 1].Layer.ClipDepth : 0;
                while (_clipDepth > baseClip)
                {
                    try { PopNativeClip(); } catch { }
                    _clipDepth--;
                }
                _renderTarget.PushAxisAlignedClip(new Vortice.Mathematics.Rect(rect.X, rect.Y, rect.Width, rect.Height), AntialiasMode.PerPrimitive);
                _nativeClipMasks.Add(null);
                _clipDepth++;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc)
            {
                try { _gdiGraphics.SetClip(rect); } catch { }
            }
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

        // Adjacent textured mesh triangles share exact edges; coverage antialiasing each
        // triangle separately leaves visible cracks. Use the rasterizer's shared-edge rule.
        public void IntersectMeshClip(GraphicsPath path)
        {
            if (_renderTarget != null) PushPathClip(path, false, AntialiasMode.Aliased);
            else if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.SetClip(path, System.Drawing.Drawing2D.CombineMode.Intersect);
        }

        public void SetClip(GraphicsPath path)
        {
            if (path == null) return;
            if (_renderTarget != null)
            {
                PushPathClip(path, true);
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.SetClip(path);
        }

        public void SetClip(GraphicsPath path, System.Drawing.Drawing2D.CombineMode combineMode)
        {
            if (path == null) return;
            if (_renderTarget != null)
            {
                PushPathClip(path, combineMode != System.Drawing.Drawing2D.CombineMode.Intersect);
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
                _nativeClipMasks.Add(null);
                _clipDepth++;
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.IntersectClip(rect);
        }
        public void PushClip(GraphicsPath path)
        {
            if (path == null) return;
            if (_renderTarget != null)
            {
                PushPathClip(path, false);
                return;
            }
            if (_gdiGraphics != null && !_ownsHdc) _gdiGraphics.IntersectClip(new Region(path));
        }

        public void PopClip()
        {
            if (_renderTarget != null)
            {
                int baseClip = (_d2dStateStack.Count > 0) ? _d2dStateStack[_d2dStateStack.Count - 1].Layer.ClipDepth : 0;
                if (_clipDepth > baseClip)
                {
                    try { PopNativeClip(); } catch { }
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
                _nativeClipMasks.Add(null);
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
                    try { PopNativeClip(); } catch { }
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

                // Restore transform FIRST so that PopAxisAlignedClip matches transform at push time
                _currentTransform = targetLayer.Transform;
                if (_renderTarget != null)
                {
                    _renderTarget.Transform = _currentTransform;
                }

                // Pop clips down to targetLayer.ClipDepth
                while (_clipDepth > targetLayer.ClipDepth)
                {
                    try { PopNativeClip(); } catch { }
                    _clipDepth--;
                }
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
