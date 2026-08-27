using System;
using System.Diagnostics;
using GenOffice.Core;

namespace GenOffice.Tests
{
    public class TestAsmBenchmark
    {
        public static void Main()
        {
            Console.WriteLine("==============================================================");
            Console.WriteLine("  KIỂM THỬ THUẬT TOÁN ASSEMBLY CỐT LÕI TỐI ƯU HÓA PHẦN CỨNG  ");
            Console.WriteLine("==============================================================");

            Console.WriteLine("Phần cứng tăng tốc: " + (HardwareAsmEngine.IsHardwareAccelerated ? "CÓ (x64 Native SIMD Kernel)" : "KHÔNG (Fallback)"));

            int n = 1000000;
            Console.WriteLine("Khởi tạo dải dữ liệu thử nghiệm: " + n.ToString("N0") + " phần tử...");
            double[] arrA = new double[n];
            double[] arrB = new double[n];
            for (int i = 0; i < n; i++)
            {
                arrA[i] = 1.25;
                arrB[i] = 2.0;
            }

            // Warmup
            HardwareAsmEngine.FastSum(arrA, 100);

            // Test Assembly FastSum
            Stopwatch sw = Stopwatch.StartNew();
            double sumAsm = HardwareAsmEngine.FastSum(arrA);
            sw.Stop();
            long asmTicks = sw.ElapsedTicks;
            double asmMil = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine(string.Format("[Assembly FastSum] Tổng: {0:N2} | Thời gian: {1:F3} ms ({2} ticks)", sumAsm, asmMil, asmTicks));

            // Test Assembly DotProduct
            sw.Restart();
            double dotAsm = HardwareAsmEngine.FastDotProduct(arrA, arrB);
            sw.Stop();
            Console.WriteLine(string.Format("[Assembly SUMPRODUCT] Tích: {0:N2} | Thời gian: {1:F3} ms", dotAsm, sw.Elapsed.TotalMilliseconds));

            // Test Assembly FastAverage
            sw.Restart();
            double avgAsm = HardwareAsmEngine.FastAverage(arrA);
            sw.Stop();
            Console.WriteLine(string.Format("[Assembly AVERAGE] Trung bình: {0:N2} | Thời gian: {1:F3} ms", avgAsm, sw.Elapsed.TotalMilliseconds));

            bool passed = Math.Abs(sumAsm - 1250000.0) < 0.001 && Math.Abs(dotAsm - 2500000.0) < 0.001 && Math.Abs(avgAsm - 1.25) < 0.0001;
            Console.WriteLine("==============================================================");
            Console.WriteLine("Kết quả: " + (passed ? "100% CHÍNH XÁC & SIÊU TỐC ĐỘ PHẦN CỨNG!" : "THẤT BẠI!"));
            Console.WriteLine("==============================================================");
        }
    }
}
