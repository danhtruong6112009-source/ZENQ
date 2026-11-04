using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using D3DFeatureLevel = Vortice.Direct3D.FeatureLevel;
using D2DFeatureLevel = Vortice.Direct2D1.FeatureLevel;
using DxgiFormat = Vortice.DXGI.Format;
using DxgiAlphaMode = Vortice.DXGI.AlphaMode;
using DCommonAlphaMode = Vortice.DCommon.AlphaMode;
using VorticePixelFormat = Vortice.DCommon.PixelFormat;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Pure GPU Direct3D 11 + DXGI Flip-Model + Direct2D 1.3 Hardware Rendering Engine.
    /// Completely bypasses GDI/GDI+ with 0% intermediate copy overhead.
    /// Includes automatic fallback to Microsoft WARP (SIMD CPU rasterizer) for machines without GPU.
    /// </summary>
    public static class GpuEngine
    {
        private static readonly object _initLock = new object();
        private static bool _initialized = false;
        private static ID3D11Device _d3dDevice;
        private static ID3D11DeviceContext _d3dContext;
        private static ID2D1Factory1 _d2dFactory1;
        private static ID2D1Device _d2dDevice;
        private static bool _isWarp = false;

        public static bool IsHardwareAccelerated => _d3dDevice != null && !_isWarp;
        public static bool IsWarpActive => _isWarp;
        public static ID3D11Device D3DDevice => _d3dDevice;
        public static ID2D1Factory1 D2DFactory => _d2dFactory1;

        private static readonly ConcurrentDictionary<IntPtr, GpuHwndSurface> _surfaces = new ConcurrentDictionary<IntPtr, GpuHwndSurface>();

        public static void Initialize()
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;

                D3DFeatureLevel[] featureLevels = new[]
                {
                    D3DFeatureLevel.Level_11_1,
                    D3DFeatureLevel.Level_11_0,
                    D3DFeatureLevel.Level_10_1,
                    D3DFeatureLevel.Level_10_0
                };

                // 1. Try Direct3D 11 Hardware Device
                try
                {
                    var res = D3D11.D3D11CreateDevice(
                        null,
                        DriverType.Hardware,
                        DeviceCreationFlags.BgraSupport,
                        featureLevels,
                        out _d3dDevice,
                        out _d3dContext
                    );

                    if (res.Success && _d3dDevice != null)
                    {
                        _isWarp = false;
                        Debug.WriteLine("[GpuEngine] Direct3D 11 Hardware Device created successfully.");
                    }
                }
                catch (Exception exHw)
                {
                    Debug.WriteLine("[GpuEngine] Direct3D 11 Hardware creation failed: " + exHw.Message);
                    _d3dDevice = null;
                }

                // 2. Fallback to Microsoft WARP (High-performance multi-threaded SIMD CPU rasterizer - Office standard)
                if (_d3dDevice == null)
                {
                    try
                    {
                        var res = D3D11.D3D11CreateDevice(
                            null,
                            DriverType.Warp,
                            DeviceCreationFlags.BgraSupport,
                            featureLevels,
                            out _d3dDevice,
                            out _d3dContext
                        );

                        if (res.Success && _d3dDevice != null)
                        {
                            _isWarp = true;
                            Debug.WriteLine("[GpuEngine] Microsoft WARP Device created successfully (No GPU safe mode).");
                        }
                    }
                    catch (Exception exWarp)
                    {
                        Debug.WriteLine("[GpuEngine] Microsoft WARP Device creation failed: " + exWarp.Message);
                        _d3dDevice = null;
                    }
                }

                // 3. Initialize Direct2D 1.3 Device
                if (_d3dDevice != null)
                {
                    try
                    {
                        using var dxgiDevice = _d3dDevice.QueryInterface<IDXGIDevice1>();
                        try { dxgiDevice.MaximumFrameLatency = 1; } catch { }

                        _d2dFactory1 = D2DGraphics.D2DFactory ?? D2D1.D2D1CreateFactory<ID2D1Factory1>(FactoryType.SingleThreaded);
                        _d2dDevice = _d2dFactory1.CreateDevice(dxgiDevice);
                        Debug.WriteLine("[GpuEngine] Direct2D 1.3 Device linked to DXGI device.");
                    }
                    catch (Exception exD2D)
                    {
                        Debug.WriteLine("[GpuEngine] Direct2D Device creation failed: " + exD2D.Message);
                    }
                }

                _initialized = true;
            }
        }

        public static GpuHwndSurface GetOrCreateSurface(IntPtr hwnd, int width, int height)
        {
            if (hwnd == IntPtr.Zero) return null;
            Initialize();

            width = Math.Max(1, width);
            height = Math.Max(1, height);

            if (_surfaces.TryGetValue(hwnd, out var surface))
            {
                if (surface.Width != width || surface.Height != height)
                {
                    surface.Resize(width, height);
                }
                return surface;
            }

            var newSurface = new GpuHwndSurface(hwnd, width, height, _d3dDevice, _d2dDevice, _d2dFactory1, _isWarp);
            _surfaces[hwnd] = newSurface;
            return newSurface;
        }

        public static void DestroySurface(IntPtr hwnd)
        {
            if (_surfaces.TryRemove(hwnd, out var surface))
            {
                try { surface.Dispose(); } catch { }
            }
        }
    }

    /// <summary>
    /// Manages the DXGI Flip-Model SwapChain and Direct2D DeviceContext for a specific HWND.
    /// </summary>
    public sealed class GpuHwndSurface : IDisposable
    {
        public IntPtr Hwnd { get; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsWarp { get; }

        private readonly ID3D11Device _d3dDevice;
        private readonly ID2D1Device _d2dDevice;
        private readonly ID2D1Factory1 _d2dFactory;

        private IDXGISwapChain1 _swapChain;
        private ID2D1DeviceContext _d2dContext;
        private ID2D1Bitmap1 _targetBitmap;

        // Fallback target if SwapChain is unavailable
        private ID2D1HwndRenderTarget _fallbackHwndTarget;

        public ID2D1RenderTarget ActiveRenderTarget => (ID2D1RenderTarget)_d2dContext ?? _fallbackHwndTarget;
        public ID2D1DeviceContext DeviceContext => _d2dContext;
        public bool UsesFlipModel => _swapChain != null;

        public GpuHwndSurface(IntPtr hwnd, int width, int height, ID3D11Device d3d, ID2D1Device d2d, ID2D1Factory1 factory, bool isWarp)
        {
            Hwnd = hwnd;
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            _d3dDevice = d3d;
            _d2dDevice = d2d;
            _d2dFactory = factory;
            IsWarp = isWarp;

            CreateGpuPipeline();
        }

        private void CreateGpuPipeline()
        {
            if (_d3dDevice != null && _d2dDevice != null)
            {
                try
                {
                    using var dxgiDevice = _d3dDevice.QueryInterface<IDXGIDevice1>();
                    using var dxgiAdapter = dxgiDevice.GetAdapter();
                    using var dxgiFactory = dxgiAdapter.GetParent<IDXGIFactory2>();

                    var swapChainDesc = new SwapChainDescription1
                    {
                        Width = (uint)Width,
                        Height = (uint)Height,
                        Format = DxgiFormat.B8G8R8A8_UNorm,
                        Stereo = false,
                        SampleDescription = new SampleDescription(1, 0),
                        BufferUsage = Usage.RenderTargetOutput,
                        BufferCount = 2,
                        Scaling = Scaling.Stretch,
                        SwapEffect = SwapEffect.FlipSequential,
                        AlphaMode = DxgiAlphaMode.Ignore,
                        Flags = SwapChainFlags.None
                    };

                    _swapChain = dxgiFactory.CreateSwapChainForHwnd(_d3dDevice, Hwnd, swapChainDesc);
                    _d2dContext = _d2dDevice.CreateDeviceContext(DeviceContextOptions.EnableMultithreadedOptimizations);

                    BindBackBuffer();
                    Debug.WriteLine($"[GpuEngine] Surface created for HWND 0x{Hwnd:X}: {Width}x{Height} (DXGI Flip-Sequential).");
                    return;
                }
                catch (Exception exFlip)
                {
                    Debug.WriteLine("[GpuEngine] DXGI Flip-Model creation failed, trying fallback: " + exFlip.Message);
                    DisposeSwapChainResources();
                }
            }

            // Fallback to ID2D1HwndRenderTarget
            CreateFallbackHwndTarget();
        }

        private void BindBackBuffer()
        {
            if (_swapChain == null || _d2dContext == null) return;

            using var dxgiSurface = _swapChain.GetBuffer<IDXGISurface>(0);
            var bmpProps = new BitmapProperties1(
                new VorticePixelFormat(DxgiFormat.B8G8R8A8_UNorm, DCommonAlphaMode.Premultiplied),
                96f, 96f,
                BitmapOptions.Target | BitmapOptions.CannotDraw
            );

            _targetBitmap = _d2dContext.CreateBitmapFromDxgiSurface(dxgiSurface, bmpProps);
            _d2dContext.Target = _targetBitmap;
        }

        private void CreateFallbackHwndTarget()
        {
            DisposeFallbackResources();

            var hwndProps = new HwndRenderTargetProperties
            {
                Hwnd = Hwnd,
                PixelSize = new SizeI(Width, Height),
                PresentOptions = PresentOptions.Immediately
            };

            var props = new RenderTargetProperties(
                IsWarp ? RenderTargetType.Software : RenderTargetType.Default,
                new VorticePixelFormat(DxgiFormat.B8G8R8A8_UNorm, DCommonAlphaMode.Ignore),
                96.0f, 96.0f,
                RenderTargetUsage.None,
                D2DFeatureLevel.Default
            );

            try
            {
                _fallbackHwndTarget = _d2dFactory.CreateHwndRenderTarget(props, hwndProps);
                Debug.WriteLine($"[GpuEngine] Fallback HwndRenderTarget created for HWND 0x{Hwnd:X}: {Width}x{Height}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[GpuEngine] Fallback HwndRenderTarget creation error: " + ex.Message);
            }
        }

        public void Resize(int newWidth, int newHeight)
        {
            if (newWidth <= 0 || newHeight <= 0) return;
            if (Width == newWidth && Height == newHeight) return;

            Width = newWidth;
            Height = newHeight;

            if (_swapChain != null && _d2dContext != null)
            {
                try
                {
                    _d2dContext.Target = null;
                    _targetBitmap?.Dispose();
                    _targetBitmap = null;

                    _swapChain.ResizeBuffers(2, (uint)Width, (uint)Height, DxgiFormat.B8G8R8A8_UNorm, SwapChainFlags.None);
                    BindBackBuffer();
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[GpuEngine] ResizeBuffers error: " + ex.Message);
                    DisposeSwapChainResources();
                    CreateFallbackHwndTarget();
                }
            }

            if (_fallbackHwndTarget != null)
            {
                try
                {
                    _fallbackHwndTarget.Resize(new SizeI(Width, Height));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[GpuEngine] Fallback resize error: " + ex.Message);
                }
            }
        }

        public void Present(bool vsync = true) => TryPresent(vsync);
        public bool TryPresent(bool vsync = true)
        {
            if (_swapChain == null) return ActiveRenderTarget != null;
            try { return _swapChain.Present((uint)(vsync ? 1 : 0), PresentFlags.None).Success; }
            catch(Exception ex) { Debug.WriteLine("[GpuEngine] Present error: "+ex.Message); return false; }
        }

        private void DisposeSwapChainResources()
        {
            if(_d2dContext!=null) D2DGraphics.ReleaseRenderTargetCaches(_d2dContext.NativePointer);
            try { _d2dContext?.Target = null; } catch { }
            try { _targetBitmap?.Dispose(); } catch { }
            try { _d2dContext?.Dispose(); } catch { }
            try { _swapChain?.Dispose(); } catch { }
            _targetBitmap = null;
            _d2dContext = null;
            _swapChain = null;
        }

        private void DisposeFallbackResources()
        {
            if(_fallbackHwndTarget!=null) D2DGraphics.ReleaseRenderTargetCaches(_fallbackHwndTarget.NativePointer);
            try { _fallbackHwndTarget?.Dispose(); } catch { }
            _fallbackHwndTarget = null;
        }

        public void Dispose()
        {
            DisposeSwapChainResources();
            DisposeFallbackResources();
        }
    }
}
