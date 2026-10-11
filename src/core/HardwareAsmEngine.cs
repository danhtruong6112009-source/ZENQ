using System;
using System.Runtime.InteropServices;

namespace ZenQOffice.Core
{
    /// <summary>
    /// HardwareAsmEngine: Lớp thực thi mã máy Assembly x86/x64 ở mức vi mô (Micro-optimization)
    /// Dành cho các thuật toán tính toán cốt lõi yêu cầu tối ưu hóa phần cứng và tốc độ xử lý tối đa.
    /// </summary>
    public static class HardwareAsmEngine
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate double FastSumDelegate(IntPtr data, long count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate double FastDotProductDelegate(IntPtr a, IntPtr b, long count);

        private static IntPtr _asmSumPtr = IntPtr.Zero;
        private static FastSumDelegate _fastSumFunc = null;

        private static IntPtr _asmDotProductPtr = IntPtr.Zero;
        private static FastDotProductDelegate _fastDotProductFunc = null;

        public static bool IsHardwareAccelerated { get; private set; }

        static HardwareAsmEngine()
        {
            InitializeAssemblyKernels();
        }

        private static void InitializeAssemblyKernels()
        {
            try
            {
                if (IntPtr.Size == 8) // 64-bit Architecture (x64)
                {
                    // =========================================================================
                    // 1. Kernel Assembly x64: Tính Tổng Vector Dải Số Thực (SUM / Double Array)
                    //
                    // ASM:
                    //   xorpd   xmm0, xmm0          ; xmm0 = 0.0 (thanh ghi tích lũy)
                    //   test    rdx, rdx            ; kiểm tra count <= 0
                    //   jle     done
                    // loop:
                    //   addsd   xmm0, [rcx]         ; cộng dồn phần tử double 64-bit từ bộ nhớ
                    //   add     rcx, 8              ; tăng con trỏ mảng thêm 8 bytes
                    //   dec     rdx                 ; giảm biến đếm
                    //   jnz     loop                ; lặp lại nếu count > 0
                    // done:
                    //   ret
                    // =========================================================================
                    byte[] x64SumAsm = new byte[]
                    {
                        0x66, 0x0F, 0x57, 0xC0,             // xorpd xmm0, xmm0
                        0x48, 0x85, 0xD2,                   // test rdx, rdx
                        0x7E, 0x0D,                         // jle done (+13)
                        0xF2, 0x0F, 0x58, 0x01,             // addsd xmm0, [rcx]
                        0x48, 0x83, 0xC1, 0x08,             // add rcx, 8
                        0x48, 0xFF, 0xCA,                   // dec rdx
                        0x75, 0xF3,                         // jnz -13 (loop)
                        0xC3                                // ret
                    };

                    _asmSumPtr = VirtualAlloc(IntPtr.Zero, (UIntPtr)x64SumAsm.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                    if (_asmSumPtr != IntPtr.Zero)
                    {
                        Marshal.Copy(x64SumAsm, 0, _asmSumPtr, x64SumAsm.Length);
                        _fastSumFunc = (FastSumDelegate)Marshal.GetDelegateForFunctionPointer(_asmSumPtr, typeof(FastSumDelegate));
                    }

                    // =========================================================================
                    // 2. Kernel Assembly x64: Tính Tích Vô Hướng (SUMPRODUCT / Dot Product)
                    //
                    // ASM:
                    //   xorpd   xmm0, xmm0          ; xmm0 = 0.0 (tích lũy)
                    //   test    r8, r8              ; kiểm tra count <= 0
                    //   jle     done
                    // loop:
                    //   movsd   xmm1, [rcx]         ; xmm1 = a[i]
                    //   mulsd   xmm1, [rdx]         ; xmm1 = a[i] * b[i]
                    //   addsd   xmm0, xmm1          ; xmm0 += xmm1
                    //   add     rcx, 8
                    //   add     rdx, 8
                    //   dec     r8
                    //   jnz     loop
                    // done:
                    //   ret
                    // =========================================================================
                    byte[] x64DotAsm = new byte[]
                    {
                        0x66, 0x0F, 0x57, 0xC0,             // xorpd xmm0, xmm0
                        0x4D, 0x85, 0xC0,                   // test r8, r8
                        0x7E, 0x19,                         // jle done (+25)
                        0xF2, 0x0F, 0x10, 0x09,             // movsd xmm1, [rcx]
                        0xF2, 0x0F, 0x59, 0x0A,             // mulsd xmm1, [rdx]
                        0xF2, 0x0F, 0x58, 0xC1,             // addsd xmm0, xmm1
                        0x48, 0x83, 0xC1, 0x08,             // add rcx, 8
                        0x48, 0x83, 0xC2, 0x08,             // add rdx, 8
                        0x49, 0xFF, 0xC8,                   // dec r8
                        0x75, 0xE7,                         // jnz -25 (loop)
                        0xC3                                // ret
                    };

                    _asmDotProductPtr = VirtualAlloc(IntPtr.Zero, (UIntPtr)x64DotAsm.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                    if (_asmDotProductPtr != IntPtr.Zero)
                    {
                        Marshal.Copy(x64DotAsm, 0, _asmDotProductPtr, x64DotAsm.Length);
                        _fastDotProductFunc = (FastDotProductDelegate)Marshal.GetDelegateForFunctionPointer(_asmDotProductPtr, typeof(FastDotProductDelegate));
                    }

                    IsHardwareAccelerated = (_fastSumFunc != null);
                }
                else
                {
                    IsHardwareAccelerated = false;
                }
            }
            catch
            {
                IsHardwareAccelerated = false;
            }
        }

        /// <summary>
        /// Tính tổng nhanh một mảng double sử dụng trực tiếp chỉ lệnh Assembly phần cứng
        /// </summary>
        public static double FastSum(double[] values, int count = -1)
        {
            if (values == null || values.Length == 0) return 0.0;
            int n = (count >= 0 && count <= values.Length) ? count : values.Length;

            if (IsHardwareAccelerated && _fastSumFunc != null && n >= 4)
            {
                GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
                try
                {
                    return _fastSumFunc(handle.AddrOfPinnedObject(), n);
                }
                finally
                {
                    handle.Free();
                }
            }

            // Managed Fallback
            double sum = 0.0;
            for (int i = 0; i < n; i++) sum += values[i];
            return sum;
        }

        /// <summary>
        /// Tính tích vô hướng (SUMPRODUCT) giữa 2 mảng double với Assembly x64
        /// </summary>
        public static double FastDotProduct(double[] a, double[] b, int count = -1)
        {
            if (a == null || b == null) return 0.0;
            int n = Math.Min(a.Length, b.Length);
            if (count >= 0 && count < n) n = count;
            if (n == 0) return 0.0;

            if (IsHardwareAccelerated && _fastDotProductFunc != null && n >= 4)
            {
                GCHandle hA = GCHandle.Alloc(a, GCHandleType.Pinned);
                GCHandle hB = GCHandle.Alloc(b, GCHandleType.Pinned);
                try
                {
                    return _fastDotProductFunc(hA.AddrOfPinnedObject(), hB.AddrOfPinnedObject(), n);
                }
                finally
                {
                    hA.Free();
                    hB.Free();
                }
            }

            double sum = 0.0;
            for (int i = 0; i < n; i++) sum += a[i] * b[i];
            return sum;
        }

        /// <summary>
        /// Tính trung bình cộng (AVERAGE) tối ưu hóa phần cứng
        /// </summary>
        public static double FastAverage(double[] values, int count = -1)
        {
            if (values == null || values.Length == 0) return 0.0;
            int n = (count >= 0 && count <= values.Length) ? count : values.Length;
            if (n == 0) return 0.0;
            return FastSum(values, n) / n;
        }
    }
}
