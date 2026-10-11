using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using ZenQOffice.Core;

// Explicit aliases to prevent WinForms vs WPF name collisions
using WinForms = System.Windows.Forms;
using WinFormsControl = System.Windows.Forms.Control;
using GdiBitmap = System.Drawing.Bitmap;
using GdiImage = System.Drawing.Image;
using GdiColor = System.Drawing.Color;
using GdiRectangle = System.Drawing.Rectangle;
using ImageLockMode = System.Drawing.Imaging.ImageLockMode;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using BitmapData = System.Drawing.Imaging.BitmapData;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Pure Native Windows Media Foundation (WMF) Video Decoder Engine.
    /// Directly accesses Windows system DLLs (mfplat.dll, mfreadwrite.dll) via COM vtables.
    /// Completely avoids WinForms controls and System.Windows.Media.Imaging (RenderTargetBitmap/DrawingVisual).
    /// </summary>
    public class NativeWmfVideoDecoder : IDisposable
    {
        private const uint MF_SDK_VERSION = 0x0002;
        private const uint MF_API_VERSION = 0x0070;
        private const uint MF_VERSION = (MF_SDK_VERSION << 16) | MF_API_VERSION;
        private const uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;
        private const uint MF_SOURCE_READER_MEDIASOURCE = 0xFFFFFFFF;
        private const uint MF_SOURCE_READERF_ENDOFSTREAM = 0x00000200;

        private static readonly Guid MF_MT_MAJOR_TYPE = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        private static readonly Guid MFMediaType_Video = new Guid("73646976-0000-0010-8000-00aa00389b71");
        private static readonly Guid MF_MT_SUBTYPE = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        private static readonly Guid MFVideoFormat_RGB32 = new Guid("00000016-0000-0010-8000-00aa00389b71");
        private static readonly Guid MF_MT_FRAME_SIZE = new Guid("1652c33d-d6b2-4012-b834-72030849a37d");
        private static readonly Guid MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING = new Guid("fb394f3d-ccf1-42ee-bbb3-f9b845d5681d");
        private static readonly Guid MF_PD_DURATION = new Guid("6c990d33-bb8e-477a-8598-0d5d96fcd88a");

        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        private static extern void MFStartup(uint version, uint dwFlags = 0);

        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        private static extern void MFShutdown();

        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        private static extern void MFCreateAttributes(out IntPtr ppMFAttributes, uint cInitialSize);

        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        private static extern void MFCreateMediaType(out IntPtr ppMFType);

        [DllImport("mfreadwrite.dll", ExactSpelling = true, PreserveSig = false)]
        private static extern void MFCreateSourceReaderFromURL(
            [MarshalAs(UnmanagedType.LPWStr)] string pwszURL,
            IntPtr pAttributes,
            out IntPtr ppSourceReader);

        [StructLayout(LayoutKind.Explicit)]
        public struct PROPVARIANT
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public long hVal;
        }

        [DllImport("ole32.dll")]
        private static extern void PropVariantClear(ref PROPVARIANT pvar);

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetUINT32Delegate(IntPtr thisPtr, ref Guid guidKey, uint unValue);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetUINT64Delegate(IntPtr thisPtr, ref Guid guidKey, ulong unValue);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetUINT64Delegate(IntPtr thisPtr, ref Guid guidKey, out ulong punValue);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetGUIDDelegate(IntPtr thisPtr, ref Guid guidKey, ref Guid guidValue);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetGUIDDelegate(IntPtr thisPtr, ref Guid guidKey, out Guid pguidValue);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetNativeMediaTypeDelegate(IntPtr thisPtr, uint dwStreamIndex, uint dwMediaTypeIndex, out IntPtr ppMediaType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetCurrentMediaTypeDelegate(IntPtr thisPtr, uint dwStreamIndex, IntPtr pdwReserved, IntPtr pMediaType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetCurrentPositionDelegate(IntPtr thisPtr, ref Guid guidTimeFormat, ref PROPVARIANT varPosition);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetPresentationAttributeDelegate(IntPtr thisPtr, uint dwStreamIndex, ref Guid guidAttribute, out PROPVARIANT pvarAttribute);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int ReadSampleDelegate(IntPtr thisPtr, uint dwStreamIndex, uint dwControlFlags, out uint pdwActualStreamIndex, out uint pdwStreamFlags, out long pllTimestamp, out IntPtr ppSample);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int ConvertToContiguousBufferDelegate(IntPtr thisPtr, out IntPtr ppBuffer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int LockBufferDelegate(IntPtr thisPtr, out IntPtr ppbScanline0, out uint pcbMaxLength, out uint pcbCurrentLength);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int UnlockBufferDelegate(IntPtr thisPtr);

        private static readonly Guid IID_IMF2DBuffer = new Guid("7dc9d5f9-9ed9-44ec-9bbf-0600bb589fbb");

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Lock2DDelegate(IntPtr thisPtr, out IntPtr pbScanline0, out int lPitch);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Unlock2DDelegate(IntPtr thisPtr);

        private IntPtr _pReader = IntPtr.Zero;
        private bool _isDisposed = false;
        private int _videoWidth = 0;
        private int _videoHeight = 0;
        private double _durationSeconds = 0;
        private SetCurrentPositionDelegate _setPosFn;
        private ReadSampleDelegate _readSampleFn;

        public int Width => _videoWidth;
        public int Height => _videoHeight;
        public double DurationSeconds => _durationSeconds;
        public bool IsOpen => _pReader != IntPtr.Zero;

        public bool Open(string videoPath)
        {
            Close();
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath)) return false;

            try
            {
                MFStartup(MF_VERSION, 0);

                IntPtr pAttr;
                MFCreateAttributes(out pAttr, 1);
                unsafe
                {
                    IntPtr* attrVTable = *(IntPtr**)pAttr;
                    var setUINT32 = Marshal.GetDelegateForFunctionPointer<SetUINT32Delegate>(attrVTable[21]);
                    Guid kProc = MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING;
                    setUINT32(pAttr, ref kProc, 1);
                }

                MFCreateSourceReaderFromURL(videoPath, pAttr, out _pReader);
                Marshal.Release(pAttr);

                if (_pReader == IntPtr.Zero) return false;

                unsafe
                {
                    IntPtr* readerVTable = *(IntPtr**)_pReader;
                    _setPosFn = Marshal.GetDelegateForFunctionPointer<SetCurrentPositionDelegate>(readerVTable[8]);
                    _readSampleFn = Marshal.GetDelegateForFunctionPointer<ReadSampleDelegate>(readerVTable[9]);

                    // 1. Get native media type to extract frame dimensions
                    var getNativeType = Marshal.GetDelegateForFunctionPointer<GetNativeMediaTypeDelegate>(readerVTable[5]);
                    IntPtr pNative;
                    int hr = getNativeType(_pReader, MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, out pNative);
                    if (hr != 0 || pNative == IntPtr.Zero) return false;

                    IntPtr* nativeVTable = *(IntPtr**)pNative;
                    var getUINT64 = Marshal.GetDelegateForFunctionPointer<GetUINT64Delegate>(nativeVTable[8]);
                    var getGUID = Marshal.GetDelegateForFunctionPointer<GetGUIDDelegate>(nativeVTable[10]);

                    Guid kMaj = MF_MT_MAJOR_TYPE;
                    Guid maj;
                    getGUID(pNative, ref kMaj, out maj);

                    Guid kFS = MF_MT_FRAME_SIZE;
                    ulong frameSize;
                    getUINT64(pNative, ref kFS, out frameSize);
                    int nativeW = (int)(frameSize >> 32);
                    int nativeH = (int)(frameSize & 0xFFFFFFFF);
                    Marshal.Release(pNative);

                    if (nativeW <= 0 || nativeH <= 0)
                    {
                        nativeW = 640;
                        nativeH = 360;
                        frameSize = ((ulong)nativeW << 32) | (uint)nativeH;
                    }

                    _videoWidth = nativeW;
                    _videoHeight = nativeH;

                    // 2. Set output media type to uncompressed 32-bit RGB
                    IntPtr pRgbType;
                    MFCreateMediaType(out pRgbType);
                    IntPtr* rgbVTable = *(IntPtr**)pRgbType;
                    var setGUID = Marshal.GetDelegateForFunctionPointer<SetGUIDDelegate>(rgbVTable[24]);
                    var setUINT64 = Marshal.GetDelegateForFunctionPointer<SetUINT64Delegate>(rgbVTable[22]);

                    setGUID(pRgbType, ref kMaj, ref maj);
                    Guid kSub = MF_MT_SUBTYPE;
                    Guid sub = MFVideoFormat_RGB32;
                    setGUID(pRgbType, ref kSub, ref sub);
                    setUINT64(pRgbType, ref kFS, frameSize);

                    var setMediaType = Marshal.GetDelegateForFunctionPointer<SetCurrentMediaTypeDelegate>(readerVTable[7]);
                    hr = setMediaType(_pReader, MF_SOURCE_READER_FIRST_VIDEO_STREAM, IntPtr.Zero, pRgbType);
                    Marshal.Release(pRgbType);

                    if (hr != 0) return false;

                    // 3. Try to query duration
                    try
                    {
                        var getAttr = Marshal.GetDelegateForFunctionPointer<GetPresentationAttributeDelegate>(readerVTable[12]);
                        Guid kDur = MF_PD_DURATION;
                        PROPVARIANT varDur;
                        hr = getAttr(_pReader, MF_SOURCE_READER_MEDIASOURCE, ref kDur, out varDur);
                        if (hr == 0 && varDur.hVal > 0)
                        {
                            _durationSeconds = varDur.hVal / 10000000.0;
                        }
                        PropVariantClear(ref varDur);
                    }
                    catch { }
                }

                return true;
            }
            catch
            {
                Close();
                return false;
            }
        }

        public bool Seek(double seconds)
        {
            if (_pReader == IntPtr.Zero || _setPosFn == null) return false;
            try
            {
                Guid kEmpty = Guid.Empty;
                PROPVARIANT varPos = new PROPVARIANT
                {
                    vt = 20, // VT_I8
                    hVal = (long)(Math.Max(0.0, seconds) * 10000000.0)
                };
                int hr = _setPosFn(_pReader, ref kEmpty, ref varPos);
                return hr == 0;
            }
            catch
            {
                return false;
            }
        }

        private static int ResolveSourceStride(uint curLen, int width, int height)
        {
            int baseStride = width * 4;
            if (width <= 0 || height <= 0 || curLen < (uint)(baseStride * height)) return baseStride;

            // WMF H.264/HEVC decoders pad BOTH width and height to macroblock boundaries (16, 32, 64, 8, 4, or 2 pixels).
            // For example, a 1934x1080 video has padded surface 1936x1088 (curLen = 1936 * 1088 * 4 = 8,425,472 bytes, stride = 7744).
            int[] candidateHeights = new int[]
            {
                height,
                (height + 15) & ~15,
                (height + 31) & ~31,
                (height + 63) & ~63,
                (height + 7) & ~7,
                (height + 3) & ~3,
                (height + 1) & ~1
            };

            for (int i = 0; i < candidateHeights.Length; i++)
            {
                int ch = candidateHeights[i];
                if (ch > 0 && (curLen % (uint)ch) == 0)
                {
                    int stride = (int)(curLen / (uint)ch);
                    if (stride >= baseStride && stride <= baseStride + 512 && (stride % 4) == 0)
                    {
                        return stride;
                    }
                }
            }

            // General search for any padded height in [height .. height + 128] that evenly divides curLen into a 4-byte pixel stride
            for (int ch = height; ch <= height + 128; ch++)
            {
                if ((curLen % (uint)ch) == 0)
                {
                    int stride = (int)(curLen / (uint)ch);
                    if (stride >= baseStride && stride <= baseStride + 512 && (stride % 4) == 0)
                    {
                        return stride;
                    }
                }
            }

            return ((width + 15) & ~15) * 4;
        }

        private unsafe void CopySampleToBitmap(IntPtr pSample, GdiBitmap targetBitmap)
        {
            if (pSample == IntPtr.Zero || targetBitmap == null) return;

            IntPtr* sVTable = *(IntPtr**)pSample;
            var convertBuf = Marshal.GetDelegateForFunctionPointer<ConvertToContiguousBufferDelegate>(sVTable[41]);
            IntPtr pBuf;
            convertBuf(pSample, out pBuf);

            if (pBuf == IntPtr.Zero) return;

            try
            {
                int copyW = Math.Min(targetBitmap.Width, _videoWidth);
                int copyH = Math.Min(targetBitmap.Height, _videoHeight);
                int copyBytesPerRow = copyW * 4;

                // 1. Prefer IMF2DBuffer::Lock2D if supported by the underlying media buffer
                Guid iid2D = IID_IMF2DBuffer;
                IntPtr p2DBuf = IntPtr.Zero;
                if (Marshal.QueryInterface(pBuf, in iid2D, out p2DBuf) == 0 && p2DBuf != IntPtr.Zero)
                {
                    try
                    {
                        IntPtr* b2dVTable = *(IntPtr**)p2DBuf;
                        var lock2D = Marshal.GetDelegateForFunctionPointer<Lock2DDelegate>(b2dVTable[3]);
                        var unlock2D = Marshal.GetDelegateForFunctionPointer<Unlock2DDelegate>(b2dVTable[4]);

                        IntPtr pbScanline0;
                        int lPitch;
                        if (lock2D(p2DBuf, out pbScanline0, out lPitch) == 0 && pbScanline0 != IntPtr.Zero)
                        {
                            try
                            {
                                BitmapData bData = targetBitmap.LockBits(
                                    new GdiRectangle(0, 0, targetBitmap.Width, targetBitmap.Height),
                                    ImageLockMode.WriteOnly,
                                    PixelFormat.Format32bppPArgb);
                                try
                                {
                                    int copyPixels = Math.Min(Math.Min(bData.Stride / 4, Math.Abs(lPitch) / 4), copyW);
                                    for (int y = 0; y < copyH; y++)
                                    {
                                        uint* srcRow = (uint*)IntPtr.Add(pbScanline0, y * lPitch);
                                        uint* dstRow = (uint*)IntPtr.Add(bData.Scan0, y * bData.Stride);
                                        for (int x = 0; x < copyPixels; x++)
                                        {
                                            dstRow[x] = srcRow[x] | 0xFF000000;
                                        }
                                    }
                                }
                                finally
                                {
                                    targetBitmap.UnlockBits(bData);
                                }
                            }
                            finally
                            {
                                unlock2D(p2DBuf);
                            }
                            return;
                        }
                    }
                    finally
                    {
                        Marshal.Release(p2DBuf);
                    }
                }

                // 2. IMFMediaBuffer::Lock with macroblock-aware stride resolution (handles both padded width & padded height)
                IntPtr* bVTable = *(IntPtr**)pBuf;
                var lockBuf = Marshal.GetDelegateForFunctionPointer<LockBufferDelegate>(bVTable[3]);
                var unlockBuf = Marshal.GetDelegateForFunctionPointer<UnlockBufferDelegate>(bVTable[4]);

                IntPtr pScan0;
                uint maxLen, curLen;
                if (lockBuf(pBuf, out pScan0, out maxLen, out curLen) == 0 && pScan0 != IntPtr.Zero && curLen > 0)
                {
                    try
                    {
                        int srcStride = ResolveSourceStride(curLen, _videoWidth, _videoHeight);

                        BitmapData bData = targetBitmap.LockBits(
                            new GdiRectangle(0, 0, targetBitmap.Width, targetBitmap.Height),
                            ImageLockMode.WriteOnly,
                            PixelFormat.Format32bppPArgb);
                        try
                        {
                            int copyPixels = Math.Min(Math.Min(bData.Stride / 4, srcStride / 4), copyW);
                            for (int y = 0; y < copyH; y++)
                            {
                                uint* srcRow = (uint*)IntPtr.Add(pScan0, y * srcStride);
                                uint* dstRow = (uint*)IntPtr.Add(bData.Scan0, y * bData.Stride);
                                for (int x = 0; x < copyPixels; x++)
                                {
                                    dstRow[x] = srcRow[x] | 0xFF000000;
                                }
                            }
                        }
                        finally
                        {
                            targetBitmap.UnlockBits(bData);
                        }
                    }
                    finally
                    {
                        unlockBuf(pBuf);
                    }
                }
            }
            finally
            {
                Marshal.Release(pBuf);
            }
        }

        public bool ReadNextFrame(GdiBitmap targetBitmap, out double frameTimestampSec)
        {
            return ReadFrameAtOrAfter(targetBitmap, -1.0, 1, out frameTimestampSec);
        }

        public bool ReadFrameAtOrAfter(GdiBitmap targetBitmap, double targetTimeSec, int maxAdvanceFrames, out double frameTimestampSec)
        {
            frameTimestampSec = 0;
            if (_pReader == IntPtr.Zero || _readSampleFn == null || targetBitmap == null) return false;

            IntPtr lastSample = IntPtr.Zero;
            long lastTimestamp = 0;
            int steps = Math.Max(1, maxAdvanceFrames);

            try
            {
                for (int i = 0; i < steps; i++)
                {
                    uint actualStream, flags;
                    long timestamp;
                    IntPtr pSample;
                    int hr = _readSampleFn(_pReader, MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, out actualStream, out flags, out timestamp, out pSample);

                    if ((flags & MF_SOURCE_READERF_ENDOFSTREAM) != 0)
                    {
                        if (pSample != IntPtr.Zero) Marshal.Release(pSample);
                        break;
                    }

                    if (hr != 0 || pSample == IntPtr.Zero)
                    {
                        break;
                    }

                    if (lastSample != IntPtr.Zero)
                    {
                        Marshal.Release(lastSample);
                    }
                    lastSample = pSample;
                    lastTimestamp = timestamp;

                    double tsSec = timestamp / 10000000.0;
                    if (targetTimeSec < 0 || tsSec >= targetTimeSec - 0.015)
                    {
                        break;
                    }
                }

                if (lastSample == IntPtr.Zero) return false;

                frameTimestampSec = lastTimestamp / 10000000.0;
                CopySampleToBitmap(lastSample, targetBitmap);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (lastSample != IntPtr.Zero)
                {
                    Marshal.Release(lastSample);
                }
            }
        }

        public static GdiBitmap ExtractPosterFrame(string videoPath, int maxW = 1280, int maxH = 720)
        {
            try
            {
                using (var dec = new NativeWmfVideoDecoder())
                {
                    if (!dec.Open(videoPath)) return null;

                    int w = dec.Width > 0 ? dec.Width : 640;
                    int h = dec.Height > 0 ? dec.Height : 360;

                    GdiBitmap bmp = new GdiBitmap(w, h, PixelFormat.Format32bppPArgb);
                    double ts;
                    bool ok = dec.ReadNextFrame(bmp, out ts);
                    if (!ok)
                    {
                        dec.Seek(0.1);
                        ok = dec.ReadNextFrame(bmp, out ts);
                    }

                    if (ok) return bmp;
                    bmp.Dispose();
                }
            }
            catch { }
            return null;
        }

        public static GdiBitmap ExtractFrameAtTime(string videoPath, double timeSec)
        {
            try
            {
                using (var dec = new NativeWmfVideoDecoder())
                {
                    if (!dec.Open(videoPath)) return null;

                    int w = dec.Width > 0 ? dec.Width : 640;
                    int h = dec.Height > 0 ? dec.Height : 360;

                    dec.Seek(timeSec);

                    GdiBitmap bmp = new GdiBitmap(w, h, PixelFormat.Format32bppPArgb);
                    double ts;
                    bool ok = dec.ReadNextFrame(bmp, out ts);
                    if (ok) return bmp;
                    bmp.Dispose();
                }
            }
            catch { }
            return null;
        }

        public void Close()
        {
            if (_pReader != IntPtr.Zero)
            {
                try { Marshal.Release(_pReader); } catch { }
                _pReader = IntPtr.Zero;
                try { MFShutdown(); } catch { }
            }
            _setPosFn = null;
            _readSampleFn = null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Close();
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Helper to extract authentic high-resolution video thumbnail frames from video files.
    /// Pure native implementation without any WinForms controls or System.Windows.Media.Imaging.
    /// </summary>
    public static class VideoThumbnailHelper
    {
        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
                [In] SIIGBF flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        private enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public static GdiBitmap GetVideoPosterFrame(string filePath, int width = 1280, int height = 720)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            // 1. Try Windows Shell Item Image Factory (fast, native thumbnail extraction)
            try
            {
                Guid guid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
                IShellItemImageFactory factory;
                SHCreateItemFromParsingName(filePath, IntPtr.Zero, guid, out factory);
                if (factory != null)
                {
                    IntPtr hBitmap;
                    int hr = factory.GetImage(new SIZE(width, height), SIIGBF.SIIGBF_BIGGERSIZEOK, out hBitmap);
                    if (hr != 0 || hBitmap == IntPtr.Zero)
                    {
                        hr = factory.GetImage(new SIZE(width, height), SIIGBF.SIIGBF_THUMBNAILONLY, out hBitmap);
                    }
                    if (hr != 0 || hBitmap == IntPtr.Zero)
                    {
                        hr = factory.GetImage(new SIZE(width, height), SIIGBF.SIIGBF_RESIZETOFIT, out hBitmap);
                    }

                    if (hr == 0 && hBitmap != IntPtr.Zero)
                    {
                        GdiBitmap bmp = GdiImage.FromHbitmap(hBitmap);
                        DeleteObject(hBitmap);
                        try
                        {
                            BitmapData bd = bmp.LockBits(new GdiRectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
                            try
                            {
                                unsafe
                                {
                                    int totalPix = bd.Width * bd.Height;
                                    uint* pPix = (uint*)bd.Scan0;
                                    for (int i = 0; i < totalPix; i++)
                                    {
                                        pPix[i] |= 0xFF000000;
                                    }
                                }
                            }
                            finally
                            {
                                bmp.UnlockBits(bd);
                            }
                        }
                        catch { }
                        return bmp;
                    }
                }
            }
            catch { }

            // 2. Native Windows Media Foundation frame extractor (Direct WMF engine, zero WPF imaging)
            try
            {
                GdiBitmap frame = NativeWmfVideoDecoder.ExtractPosterFrame(filePath, width, height);
                if (frame != null) return frame;
            }
            catch { }

            return null;
        }
    }

    /// <summary>
    /// Pure On-Canvas Native Direct2D/DirectX and Windows Media Foundation (WMF) Video Engine
    /// for Microsoft Office PowerPoint-grade slide presentation.
    /// Renders video frames natively into shape.CachedImage on the Direct2D canvas.
    /// Completely eliminates WinForms child controls, avoiding airspace collision, occlusion of resize handles,
    /// and allowing full support for shape clipping, masks, soft edges, crops, and smooth zoom/pan.
    /// </summary>
    public class SlideVideoPlayerManager
    {
        private static SlideVideoPlayerManager _instance;
        public static SlideVideoPlayerManager Instance
        {
            get
            {
                if (_instance == null) _instance = new SlideVideoPlayerManager();
                return _instance;
            }
        }

        private MediaPlayer _mediaPlayer; // Audio playback engine only (zero visual host)
        private NativeWmfVideoDecoder _decoder;
        private ShapeNode _activeShape;
        private WinFormsControl _parentControl;
        private Action _invalidateCallback;
        private WinForms.Timer _frameTimer;
        private GdiBitmap _frameBitmap;
        private double _lastDecodedTimestamp = -1;
        private bool _isMuted = false;
        private double _previousVolume = 0.8;
        private bool _isRenderingFrame = false;
        private System.Diagnostics.Stopwatch _playbackStopwatch = new System.Diagnostics.Stopwatch();
        private double _playbackBaseOffsetSec = 0.0;
        private DateTime _lastSeekTime = DateTime.MinValue;

        public bool IsPlaying
        {
            get { return _activeShape != null && _activeShape.IsVideoPlaying; }
        }

        public ShapeNode ActiveShape
        {
            get { return _activeShape; }
        }

        public float CurrentTime
        {
            get { return _activeShape != null ? _activeShape.VideoCurrentTime : 0f; }
        }

        public float TotalTime
        {
            get { return _activeShape != null ? _activeShape.VideoTotalTime : 0f; }
        }

        public Action RequestAdvancePresenter { get; set; }
        public Action RequestPreviousPresenter { get; set; }
        public Action RequestStopPresenter { get; set; }
        public Action OnFrameRendered { get; set; }

        public SlideVideoPlayerManager()
        {
            _frameTimer = new WinForms.Timer();
            _frameTimer.Interval = 15; // ~60 FPS high-responsiveness frame timer
            _frameTimer.Tick += OnFrameTimerTick;
        }

        private double GetCurrentPlaybackClockSeconds()
        {
            double swSec = _playbackBaseOffsetSec + _playbackStopwatch.Elapsed.TotalSeconds;
            if (_mediaPlayer != null)
            {
                try
                {
                    // If recently seeked (within 0.6s), do NOT allow lagging MediaPlayer.Position to revert the seek!
                    if ((DateTime.UtcNow - _lastSeekTime).TotalSeconds < 0.6)
                    {
                        return swSec;
                    }

                    double audioSec = _mediaPlayer.Position.TotalSeconds;
                    if (audioSec > 0.05)
                    {
                        if (Math.Abs(audioSec - swSec) > 0.15)
                        {
                            _playbackBaseOffsetSec = audioSec;
                            _playbackStopwatch.Restart();
                            return audioSec;
                        }
                        return audioSec;
                    }
                }
                catch { }
            }
            return swSec;
        }

        public void Play(ShapeNode shape, WinFormsControl container = null, GdiRectangle bounds = default(GdiRectangle), bool isPresentation = false, Action invalidateCallback = null)
        {
            if (shape == null) return;
            if (container != null) _parentControl = container;
            if (invalidateCallback != null) _invalidateCallback = invalidateCallback;

            string videoPath = ResolveVideoPath(shape.VideoPath, _parentControl);
            if (string.IsNullOrEmpty(videoPath) || (!videoPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !File.Exists(videoPath)))
            {
                using (WinForms.OpenFileDialog ofd = new WinForms.OpenFileDialog())
                {
                    ofd.Title = "Chọn tệp video để phát (" + (!string.IsNullOrEmpty(shape.VideoPath) ? Path.GetFileName(shape.VideoPath) : "Video") + ")...";
                    ofd.Filter = "Tệp Video (*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm)|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(_parentControl) == WinForms.DialogResult.OK)
                    {
                        shape.VideoPath = ofd.FileName;
                        videoPath = ofd.FileName;
                        if (shape.CachedImage == null)
                        {
                            shape.CachedImage = VideoThumbnailHelper.GetVideoPosterFrame(videoPath);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (shape.CachedImage == null && File.Exists(videoPath))
            {
                shape.CachedImage = VideoThumbnailHelper.GetVideoPosterFrame(videoPath);
            }

            if (_activeShape == shape && _mediaPlayer != null && _decoder != null && _decoder.IsOpen)
            {
                double effectiveEnd = _activeShape.VideoTrimEnd > 0
                    ? _activeShape.VideoTrimEnd
                    : (_activeShape.VideoTotalTime > 0 ? _activeShape.VideoTotalTime : 0);
                double startSec = _activeShape.VideoTrimStart > 0 ? _activeShape.VideoTrimStart : 0.0;
                double resumeSec = _activeShape.VideoCurrentTime;
                if ((effectiveEnd > 0 && resumeSec >= effectiveEnd - 0.15) || resumeSec < startSec)
                {
                    resumeSec = startSec;
                    _activeShape.VideoCurrentTime = (float)startSec;
                    try { _mediaPlayer.Position = TimeSpan.FromSeconds(startSec); } catch { }
                    _decoder.Seek(startSec);
                    _lastDecodedTimestamp = -1;
                }
                _playbackBaseOffsetSec = resumeSec;
                _playbackStopwatch.Restart();
                _mediaPlayer.Play();
                shape.IsVideoPlaying = true;
                RenderCurrentFrame();
                _frameTimer.Start();
                TriggerInvalidate();
                return;
            }

            Stop();

            _activeShape = shape;
            _activeShape.IsVideoPlaying = true;

            try
            {
                // Initialize Native WMF Video Decoder
                if (_decoder == null) _decoder = new NativeWmfVideoDecoder();
                _decoder.Open(videoPath);

                int targetW = _decoder.Width > 0 ? _decoder.Width : 640;
                int targetH = _decoder.Height > 0 ? _decoder.Height : 360;
                if (_frameBitmap == null || _frameBitmap.Width != targetW || _frameBitmap.Height != targetH)
                {
                    if (_frameBitmap != null) { try { _frameBitmap.Dispose(); } catch { } }
                    _frameBitmap = new GdiBitmap(targetW, targetH, PixelFormat.Format32bppPArgb);
                }

                if (_decoder.DurationSeconds > 0)
                {
                    _activeShape.VideoTotalTime = (float)_decoder.DurationSeconds;
                }

                // Initialize Audio Player
                if (_mediaPlayer == null)
                {
                    _mediaPlayer = new MediaPlayer();
                    _mediaPlayer.MediaOpened += OnMediaOpened;
                    _mediaPlayer.MediaEnded += OnMediaEnded;
                    _mediaPlayer.MediaFailed += OnMediaFailed;
                }

                _mediaPlayer.Open(new Uri(videoPath));
                SetVolumeFromShape(_activeShape);

                double startSec = _activeShape.VideoTrimStart > 0 ? _activeShape.VideoTrimStart : 0.0;
                _playbackBaseOffsetSec = startSec;
                _playbackStopwatch.Restart();
                _lastDecodedTimestamp = -1;

                if (startSec > 0)
                {
                    _mediaPlayer.Position = TimeSpan.FromSeconds(startSec);
                    _decoder.Seek(startSec);
                }

                _mediaPlayer.Play();
                RenderCurrentFrame();
                _frameTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SlideVideoPlayerManager play error: " + ex.Message);
            }

            TriggerInvalidate();
        }

        private void OnMediaOpened(object sender, EventArgs e)
        {
            if (_mediaPlayer == null || _activeShape == null) return;

            if (_mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                double totalSec = _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                _activeShape.VideoTotalTime = (float)totalSec;
            }

            if (_activeShape.VideoTrimStart > 0 && _lastDecodedTimestamp < 0)
            {
                _mediaPlayer.Position = TimeSpan.FromSeconds(_activeShape.VideoTrimStart);
                if (_decoder != null) _decoder.Seek(_activeShape.VideoTrimStart);
                _playbackBaseOffsetSec = _activeShape.VideoTrimStart;
                _playbackStopwatch.Restart();
            }

            SetVolumeFromShape(_activeShape);
            _mediaPlayer.Play();
            _activeShape.IsVideoPlaying = true;
            _frameTimer.Start();

            TriggerInvalidate();
        }

        private void OnMediaEnded(object sender, EventArgs e)
        {
            if (_activeShape == null || _mediaPlayer == null) return;

            if (_activeShape.VideoLoopUntilStopped)
            {
                double startSec = (_activeShape.VideoTrimStart > 0) ? _activeShape.VideoTrimStart : 0.0;
                _mediaPlayer.Position = TimeSpan.FromSeconds(startSec);
                if (_decoder != null) _decoder.Seek(startSec);
                _playbackBaseOffsetSec = startSec;
                _playbackStopwatch.Restart();
                _lastDecodedTimestamp = -1;
                _mediaPlayer.Play();
            }
            else
            {
                Pause();
                if (_activeShape.VideoRewindAfterPlaying)
                {
                    double startSec = (_activeShape.VideoTrimStart > 0) ? _activeShape.VideoTrimStart : 0.0;
                    _mediaPlayer.Position = TimeSpan.FromSeconds(startSec);
                    if (_decoder != null) _decoder.Seek(startSec);
                    _playbackBaseOffsetSec = startSec;
                    _playbackStopwatch.Reset();
                    _lastDecodedTimestamp = -1;
                    _activeShape.VideoCurrentTime = (float)startSec;
                    RenderCurrentFrame();
                }
            }
            TriggerInvalidate();
        }

        private void OnMediaFailed(object sender, ExceptionEventArgs e)
        {
            Console.WriteLine("MediaPlayer error: " + (e.ErrorException != null ? e.ErrorException.Message : "Unknown error"));
        }

        private void OnFrameTimerTick(object sender, EventArgs e)
        {
            if (_activeShape == null || !_activeShape.IsVideoPlaying || _decoder == null || !_decoder.IsOpen)
            {
                _frameTimer.Stop();
                return;
            }

            double pos = GetCurrentPlaybackClockSeconds();
            _activeShape.VideoCurrentTime = (float)pos;

            double effectiveEnd = _activeShape.VideoTrimEnd > 0
                ? _activeShape.VideoTrimEnd
                : (_activeShape.VideoTotalTime > 0 ? _activeShape.VideoTotalTime : 0);

            if (effectiveEnd > 0 && pos >= effectiveEnd)
            {
                if (_activeShape.VideoLoopUntilStopped)
                {
                    double startSec = (_activeShape.VideoTrimStart > 0) ? _activeShape.VideoTrimStart : 0.0;
                    if (_mediaPlayer != null) _mediaPlayer.Position = TimeSpan.FromSeconds(startSec);
                    if (_decoder != null) _decoder.Seek(startSec);
                    _playbackBaseOffsetSec = startSec;
                    _playbackStopwatch.Restart();
                    _lastDecodedTimestamp = -1;
                }
                else
                {
                    Pause();
                    if (_activeShape.VideoRewindAfterPlaying)
                    {
                        double startSec = (_activeShape.VideoTrimStart > 0) ? _activeShape.VideoTrimStart : 0.0;
                        if (_mediaPlayer != null) _mediaPlayer.Position = TimeSpan.FromSeconds(startSec);
                        if (_decoder != null) _decoder.Seek(startSec);
                        _playbackBaseOffsetSec = startSec;
                        _playbackStopwatch.Reset();
                        _lastDecodedTimestamp = -1;
                        _activeShape.VideoCurrentTime = (float)startSec;
                    }
                    TriggerInvalidate();
                    return;
                }
            }

            if (RenderCurrentFrame())
            {
                TriggerInvalidate();
            }
        }

        private bool RenderCurrentFrame()
        {
            if (_isRenderingFrame || _activeShape == null || _decoder == null || !_decoder.IsOpen) return false;
            _isRenderingFrame = true;
            try
            {
                if (_frameBitmap == null)
                {
                    int fw = _decoder.Width > 0 ? _decoder.Width : 640;
                    int fh = _decoder.Height > 0 ? _decoder.Height : 360;
                    _frameBitmap = new GdiBitmap(fw, fh, PixelFormat.Format32bppPArgb);
                }

                double curClockSec = GetCurrentPlaybackClockSeconds();

                // If decoder is already slightly ahead of playback clock (within 1s), wait until clock catches up
                if (_lastDecodedTimestamp >= 0 && _lastDecodedTimestamp > curClockSec + 0.012 && (_lastDecodedTimestamp - curClockSec) < 1.0)
                {
                    return false;
                }

                // Never call IMFSourceReader::SetCurrentPosition during normal forward playback because WMF seeks to the previous keyframe (often 2-5s back!).
                // Only Seek if playback jumped backward or jumped > 3.5 seconds forward.
                if (_lastDecodedTimestamp >= 0 && (curClockSec < _lastDecodedTimestamp - 0.5 || (curClockSec - _lastDecodedTimestamp) > 3.5))
                {
                    _decoder.Seek(curClockSec);
                }

                int maxCatchupFrames = (_lastDecodedTimestamp >= 0 && (curClockSec - _lastDecodedTimestamp) > 0.25) ? 12 : 4;
                double sampleTs;
                bool ok = _decoder.ReadFrameAtOrAfter(_frameBitmap, curClockSec, maxCatchupFrames, out sampleTs);
                if (ok)
                {
                    _lastDecodedTimestamp = sampleTs;
                    _activeShape.CachedImage = _frameBitmap;
                    _activeShape.InvalidateFeatherCache();
                    _activeShape.InvalidateDisplayCache();
                    D2DGraphics.InvalidateBitmapCache(_frameBitmap);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                _isRenderingFrame = false;
            }
        }

        public void Pause()
        {
            _frameTimer.Stop();
            _playbackBaseOffsetSec = GetCurrentPlaybackClockSeconds();
            _playbackStopwatch.Stop();
            _playbackStopwatch.Reset();
            if (_mediaPlayer != null)
            {
                try { _mediaPlayer.Pause(); } catch { }
            }
            if (_activeShape != null)
            {
                _activeShape.IsVideoPlaying = false;
            }
            TriggerInvalidate();
        }

        public void Resume()
        {
            if (_activeShape != null && _mediaPlayer != null)
            {
                _playbackBaseOffsetSec = _activeShape.VideoCurrentTime;
                _playbackStopwatch.Restart();
                _mediaPlayer.Play();
                _activeShape.IsVideoPlaying = true;
                _frameTimer.Start();
                TriggerInvalidate();
            }
        }

        public void Stop()
        {
            _frameTimer.Stop();
            _playbackStopwatch.Stop();
            _playbackStopwatch.Reset();
            _playbackBaseOffsetSec = 0.0;
            if (_mediaPlayer != null)
            {
                try { _mediaPlayer.Stop(); } catch { }
            }
            if (_decoder != null)
            {
                _decoder.Close();
            }
            if (_activeShape != null)
            {
                _activeShape.IsVideoPlaying = false;
                double startSec = (_activeShape.VideoTrimStart > 0) ? _activeShape.VideoTrimStart : 0.0;
                _activeShape.VideoCurrentTime = (float)startSec;
                _activeShape = null;
            }
            if (_frameBitmap != null)
            {
                D2DGraphics.InvalidateBitmapCache(_frameBitmap);
            }
            _lastDecodedTimestamp = -1;
            TriggerInvalidate();
        }

        public void Seek(float seconds)
        {
            double sec = Math.Max(0.0, seconds);
            _playbackBaseOffsetSec = sec;
            _lastSeekTime = DateTime.UtcNow;
            if (_playbackStopwatch.IsRunning) _playbackStopwatch.Restart();
            else _playbackStopwatch.Reset();

            if (_activeShape != null)
            {
                _activeShape.VideoCurrentTime = (float)sec;
            }

            if (_mediaPlayer != null)
            {
                try { _mediaPlayer.Position = TimeSpan.FromSeconds(sec); } catch { }
            }
            if (_decoder != null && _decoder.IsOpen)
            {
                _decoder.Seek(sec);
                if (_frameBitmap != null)
                {
                    double ts;
                    if (_decoder.ReadFrameAtOrAfter(_frameBitmap, sec, 15, out ts))
                    {
                        _lastDecodedTimestamp = ts;
                        if (_activeShape != null)
                        {
                            _activeShape.CachedImage = _frameBitmap;
                            _activeShape.InvalidateFeatherCache();
                            _activeShape.InvalidateDisplayCache();
                        }
                        D2DGraphics.InvalidateBitmapCache(_frameBitmap);
                    }
                }
            }
            TriggerInvalidate();
        }

        public void TogglePlay(ShapeNode shape = null, WinFormsControl container = null, GdiRectangle bounds = default(GdiRectangle), bool isPresentation = false, Action invalidateCallback = null)
        {
            if (shape == null) shape = _activeShape;
            if (shape == null) return;

            if (_activeShape == shape && _activeShape.IsVideoPlaying)
            {
                Pause();
            }
            else
            {
                Play(shape, container, bounds, isPresentation, invalidateCallback);
            }
        }

        public void ToggleVolume()
        {
            if (_mediaPlayer == null) return;
            if (_isMuted)
            {
                _isMuted = false;
                _mediaPlayer.Volume = _previousVolume > 0 ? _previousVolume : 0.8;
                if (_activeShape != null) _activeShape.VideoVolume = VideoVolumeLevel.High;
            }
            else
            {
                _previousVolume = _mediaPlayer.Volume;
                _isMuted = true;
                _mediaPlayer.Volume = 0.0;
                if (_activeShape != null) _activeShape.VideoVolume = VideoVolumeLevel.Mute;
            }
            TriggerInvalidate();
        }

        public void SetVolume(double vol)
        {
            if (_mediaPlayer == null) return;
            _isMuted = (vol <= 0.001);
            _mediaPlayer.Volume = Math.Max(0.0, Math.Min(1.0, vol));
            if (!_isMuted) _previousVolume = _mediaPlayer.Volume;
            TriggerInvalidate();
        }

        public void SetVolumeFromShape(ShapeNode shape)
        {
            if (shape == null || _mediaPlayer == null) return;
            double vol = 0.8;
            if (!string.IsNullOrEmpty(shape.VideoVolumeLevel))
            {
                if (shape.VideoVolumeLevel.Equals("Mute", StringComparison.OrdinalIgnoreCase)) vol = 0.0;
                else if (shape.VideoVolumeLevel.Equals("Low", StringComparison.OrdinalIgnoreCase)) vol = 0.25;
                else if (shape.VideoVolumeLevel.Equals("Medium", StringComparison.OrdinalIgnoreCase)) vol = 0.60;
                else if (shape.VideoVolumeLevel.Equals("High", StringComparison.OrdinalIgnoreCase)) vol = 1.0;
            }
            else
            {
                switch (shape.VideoVolume)
                {
                    case VideoVolumeLevel.Mute: vol = 0.0; break;
                    case VideoVolumeLevel.Low: vol = 0.25; break;
                    case VideoVolumeLevel.Medium: vol = 0.60; break;
                    case VideoVolumeLevel.High: vol = 1.0; break;
                }
            }

            _isMuted = (vol <= 0.001);
            _mediaPlayer.Volume = vol;
            if (!_isMuted) _previousVolume = vol;
        }

        public void UpdateBounds(GdiRectangle bounds)
        {
            // Pure Direct2D on-canvas rendering: shape geometry is rendered directly within canvas matrix
        }

        public void UpdateShape(ShapeNode shape)
        {
            if (shape != null && _activeShape == shape)
            {
                SetVolumeFromShape(shape);
            }
        }

        private void TriggerInvalidate()
        {
            if (_invalidateCallback != null)
            {
                try { _invalidateCallback(); } catch { }
            }
            if (_parentControl != null && !_parentControl.IsDisposed)
            {
                try { _parentControl.Invalidate(); } catch { }
            }
            if (OnFrameRendered != null)
            {
                try { OnFrameRendered(); } catch { }
            }
        }

        public string ResolveVideoPath(string rawPath, WinFormsControl container = null)
        {
            if (string.IsNullOrEmpty(rawPath)) return null;
            if (rawPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || rawPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return rawPath;
            }

            try
            {
                if (File.Exists(rawPath)) return rawPath;
            }
            catch { }

            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string localPath = Path.Combine(appDir, rawPath);
                if (File.Exists(localPath)) return localPath;
            }
            catch { }

            try
            {
                string tempMedia = Path.Combine(Path.GetTempPath(), "ZenQOffice", "Media", Path.GetFileName(rawPath));
                if (File.Exists(tempMedia)) return tempMedia;
            }
            catch { }

            return null;
        }
    }
}
