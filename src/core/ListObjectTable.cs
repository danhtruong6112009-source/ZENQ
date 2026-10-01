using System;
using System.Collections.Generic;
using GenOffice.UI;

namespace GenOffice.Core
{
    /// <summary>
    /// Excel ListObject / Table definition providing structured references and auto-expansion.
    /// </summary>
    public class ListObjectTable : ExcelTableInfo
    {
        public ListObjectTable() : base("Table1", 1, 10, 1, 5, true, 1) { }

        public ListObjectTable(string name, uint startRow, uint endRow, uint startCol, uint endCol, bool hasHeaders = true, int styleIndex = 1)
            : base(name, startRow, endRow, startCol, endCol, hasHeaders, styleIndex)
        {
        }
    }
}
