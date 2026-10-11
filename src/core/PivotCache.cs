using System;
using System.Collections.Generic;

namespace ZenQOffice.Core
{
    /// <summary>
    /// PivotCache storing multi-dimensional source data for PivotTable aggregations.
    /// </summary>
    public class PivotCache
    {
        public string SourceRange { get; set; }
        public List<string> FieldNames { get; set; }
        public List<Dictionary<string, object>> Records { get; set; }

        public PivotCache()
        {
            FieldNames = new List<string>();
            Records = new List<Dictionary<string, object>>();
        }

        public PivotCache(string sourceRange) : this()
        {
            SourceRange = sourceRange;
        }

        public void AddRecord(Dictionary<string, object> row)
        {
            if (row != null) Records.Add(row);
        }
    }
}
