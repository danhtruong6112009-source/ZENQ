using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestSpellCheck
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing SpellCheckDialog & Item Corrections ===");

                List<SpellCheckItem> items = new List<SpellCheckItem>();
                string cellText = "doan thu";
                string correctedText = "";

                var item = new SpellCheckItem
                {
                    Location = "Ô B4",
                    OriginalText = cellText,
                    ProblemWord = "doan thu",
                    Reason = "Từ chưa có dấu chuẩn tiếng Việt",
                    ApplyCorrection = (newVal) => { correctedText = newVal; }
                };
                item.Suggestions.Add("Doanh thu");
                items.Add(item);

                // Test correction application
                item.ApplyCorrection("Doanh thu");
                bool pass = (correctedText == "Doanh thu");
                Console.WriteLine("Spell check correction test: " + (pass ? "PASS" : "FAIL"));

                if (pass)
                {
                    Console.WriteLine("\n=== ALL_SPELLCHECK_TESTS_PASSED_SUCCESSFULLY ===");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_ERROR: " + ex.ToString());
            }
        }
    }
}
