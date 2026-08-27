using System;
using System.Runtime.InteropServices;

namespace GenOffice.Core
{
    public class TestAsm
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
        public delegate double FastSumDelegate(IntPtr data, long count);

        public static void Main()
        {
            Console.WriteLine("=== Testing Assembly Micro-Optimization in GenOffice ===");
            Console.WriteLine("Process Architecture: " + (IntPtr.Size == 8 ? "64-bit (x64)" : "32-bit (x86)"));

            if (IntPtr.Size == 8)
            {
                // x64 Machine Code:
                // xorpd xmm0, xmm0
                // test rdx, rdx
                // jle done
                // loop:
                // addsd xmm0, [rcx]
                // add rcx, 8
                // dec rdx
                // jnz loop
                // done:
                // ret
                byte[] x64Asm = new byte[] {
                    0x66, 0x0F, 0x57, 0xC0,             // xorpd xmm0, xmm0
                    0x48, 0x85, 0xD2,                   // test rdx, rdx
                    0x7E, 0x0D,                         // jle done (+13)
                    0xF2, 0x0F, 0x58, 0x01,             // addsd xmm0, [rcx]
                    0x48, 0x83, 0xC1, 0x08,             // add rcx, 8
                    0x48, 0xFF, 0xCA,                   // dec rdx
                    0x75, 0xF3,                         // jnz -13 (loop)
                    0xC3                                // ret
                };

                IntPtr pMem = VirtualAlloc(IntPtr.Zero, (UIntPtr)x64Asm.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                if (pMem == IntPtr.Zero)
                {
                    Console.WriteLine("VirtualAlloc failed!");
                    return;
                }

                Marshal.Copy(x64Asm, 0, pMem, x64Asm.Length);
                FastSumDelegate fastSum = (FastSumDelegate)Marshal.GetDelegateForFunctionPointer(pMem, typeof(FastSumDelegate));

                double[] testData = new double[] { 10.5, 20.25, 30.75, 40.0, 50.5 };
                GCHandle handle = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    double sum = fastSum(handle.AddrOfPinnedObject(), testData.Length);
                    Console.WriteLine("Assembly Computed Sum = " + sum + " (Expected: 152.0)");
                    if (Math.Abs(sum - 152.0) < 0.0001)
                    {
                        Console.WriteLine("[SUCCESS] Assembly execution verified 100% on CPU hardware!");
                    }
                    else
                    {
                        Console.WriteLine("[FAILURE] Result mismatch!");
                    }
                }
                finally
                {
                    handle.Free();
                    VirtualFree(pMem, UIntPtr.Zero, MEM_RELEASE);
                }
            }
        }
    }
}
