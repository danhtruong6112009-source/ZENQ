using System;
using GenOffice.UI;

namespace VerifyPresets
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing WpfShapeLibrary...");
            WpfShapeLibrary.EnsureInitialized();
            Console.WriteLine($"Total Categories: {WpfShapeLibrary.Categories.Count}");
            Console.WriteLine($"Total Presets: {WpfShapeLibrary.TotalPresetCount}");

            foreach (var cat in WpfShapeLibrary.Categories)
            {
                Console.WriteLine($"  [{cat.Key}] {cat.Title}: {cat.Items.Count} items");
            }

            // Check if any category has less than expected
            if (WpfShapeLibrary.TotalPresetCount >= 500)
            {
                Console.WriteLine("SUCCESS: Total preset count is >= 500!");
            }
            else
            {
                Console.WriteLine("FAIL: Total preset count is less than 500!");
            }
        }
    }
}
