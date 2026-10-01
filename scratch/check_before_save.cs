using System;
using GenOffice.UI;

namespace GenOffice.Test
{
    class CheckBeforeSave
    {
        [STAThread]
        static void Main()
        {
            WriterView writer = new WriterView();
            string[] testTypes = new string[] {
                "Hierarchy_Tree",
                "Process_BasicChevron",
                "Cycle_Basic",
                "Rel_Balance",
                "Matrix_Cycle",
                "Pyramid_Inverted"
            };

            for (int i = 0; i < testTypes.Length; i++)
            {
                writer.InsertSmartArt(testTypes[i], "Title " + i);
            }

            for (int i = 0; i < writer.DocumentShapes.Count; i++)
            {
                var s = writer.DocumentShapes[i];
                Console.WriteLine(string.Format("Before save [{0}]: Id={1}, SmartArtType={2}, TextContent={3}",
                    i, s.Id, s.SmartArtType, s.TextContent));
            }
        }
    }
}
