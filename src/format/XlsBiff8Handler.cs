using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ZenQOffice.Core;
using ZenQOffice.UI;

namespace ZenQOffice.Format
{
    /// <summary>
    /// Legacy Excel 97-2003 (.xls) BIFF8 binary workbook parser.
    /// Parses OLE2 Compound File Binary containers and extracts BIFF8 record streams.
    /// </summary>
    public static class XlsBiff8Handler
    {
        private static readonly byte[] Ole2Magic = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        public static bool LoadXls(CalcView calcView, string filePath)
        {
            if (calcView == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                if (fileBytes.Length < 512) return false;

                // Validate OLE2 signature
                for (int i = 0; i < Ole2Magic.Length; i++)
                {
                    if (fileBytes[i] != Ole2Magic[i]) return false;
                }

                // Extract Workbook stream from OLE2 directory
                byte[] workbookBytes = ExtractWorkbookStream(fileBytes);
                if (workbookBytes == null || workbookBytes.Length == 0) return false;

                // Parse BIFF8 records
                return ParseBiff8Stream(calcView, workbookBytes);
            }
            catch
            {
                return false;
            }
        }

        private static byte[] ExtractWorkbookStream(byte[] data)
        {
            try
            {
                int sectorSize = 1 << BitConverter.ToUInt16(data, 30); // usually 512
                int dirStartSector = BitConverter.ToInt32(data, 48);

                // Simple sector walker to locate directory and find "Workbook" or "Book"
                int offset = (dirStartSector + 1) * sectorSize;
                while (offset + 128 <= data.Length)
                {
                    string entryName = Encoding.Unicode.GetString(data, offset, 64).TrimEnd('\0');
                    if (entryName.Equals("Workbook", StringComparison.OrdinalIgnoreCase) ||
                        entryName.Equals("Book", StringComparison.OrdinalIgnoreCase))
                    {
                        int streamStartSector = BitConverter.ToInt32(data, offset + 116);
                        int streamSize = BitConverter.ToInt32(data, offset + 120);

                        if (streamStartSector >= 0 && streamSize > 0)
                        {
                            int streamOffset = (streamStartSector + 1) * sectorSize;
                            if (streamOffset + streamSize <= data.Length)
                            {
                                byte[] stream = new byte[streamSize];
                                Array.Copy(data, streamOffset, stream, 0, streamSize);
                                return stream;
                            }
                        }
                    }
                    offset += 128;
                }

                // Fallback: search for BIFF8 BOF signature (0x0809) anywhere in sectors
                for (int i = sectorSize; i < data.Length - 16; i += sectorSize)
                {
                    if (data[i] == 0x09 && data[i + 1] == 0x08)
                    {
                        int len = Math.Min(data.Length - i, 1024 * 1024);
                        byte[] stream = new byte[len];
                        Array.Copy(data, i, stream, 0, len);
                        return stream;
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool ParseBiff8Stream(CalcView calcView, byte[] stream)
        {
            List<string> sheetNames = new List<string>();
            List<string> sstStrings = new List<string>();
            SparseMatrix activeSheet = null;

            calcView.Sheets.Clear();
            calcView.SheetNames.Clear();
            if (calcView.SheetStates == null) calcView.SheetStates = new List<CalcSheetState>();
            else calcView.SheetStates.Clear();

            int pos = 0;
            while (pos + 4 <= stream.Length)
            {
                ushort recId = BitConverter.ToUInt16(stream, pos);
                ushort recLen = BitConverter.ToUInt16(stream, pos + 2);
                pos += 4;

                if (pos + recLen > stream.Length) break;

                switch (recId)
                {
                    case 0x0809: // BOF (Beginning of File / Substream)
                        ushort subType = recLen >= 2 ? BitConverter.ToUInt16(stream, pos) : (ushort)0;
                        if (subType == 0x0010) // Worksheet
                        {
                            activeSheet = new SparseMatrix();
                            calcView.Sheets.Add(activeSheet);
                            string name = sheetNames.Count >= calcView.Sheets.Count ? sheetNames[calcView.Sheets.Count - 1] : ("Sheet" + calcView.Sheets.Count);
                            calcView.SheetNames.Add(name);

                            var st = new CalcSheetState();
                            st.CustomColWidths = new Dictionary<uint, int>();
                            st.CustomRowHeights = new Dictionary<uint, int>();
                            st.Shapes = new List<DocShapeObject>();
                            calcView.SheetStates.Add(st);
                        }
                        break;

                    case 0x0085: // BOUNDSHEET (Sheet name & location)
                        if (recLen >= 8)
                        {
                            byte nameLen = stream[pos + 6];
                            byte flags = stream[pos + 7];
                            bool isUnicode = (flags & 0x01) != 0;
                            string name;
                            if (isUnicode && pos + 8 + nameLen * 2 <= stream.Length)
                            {
                                name = Encoding.Unicode.GetString(stream, pos + 8, nameLen * 2);
                            }
                            else if (pos + 8 + nameLen <= stream.Length)
                            {
                                name = Encoding.ASCII.GetString(stream, pos + 8, nameLen);
                            }
                            else
                            {
                                name = "Sheet" + (sheetNames.Count + 1);
                            }
                            sheetNames.Add(name);
                        }
                        break;

                    case 0x00FC: // SST (Shared String Table)
                        ParseSstRecord(stream, pos, recLen, sstStrings);
                        break;

                    case 0x0203: // NUMBER (IEEE 754 64-bit float)
                        if (activeSheet != null && recLen >= 14)
                        {
                            uint row = (uint)(BitConverter.ToUInt16(stream, pos) + 1);
                            uint col = (uint)(BitConverter.ToUInt16(stream, pos + 2) + 1);
                            double val = BitConverter.ToDouble(stream, pos + 6);
                            activeSheet.SetCellNumber(row, col, val);
                        }
                        break;

                    case 0x027E: // RK (Compressed number)
                        if (activeSheet != null && recLen >= 10)
                        {
                            uint row = (uint)(BitConverter.ToUInt16(stream, pos) + 1);
                            uint col = (uint)(BitConverter.ToUInt16(stream, pos + 2) + 1);
                            uint rk = BitConverter.ToUInt32(stream, pos + 6);
                            double val = DecodeRk(rk);
                            activeSheet.SetCellNumber(row, col, val);
                        }
                        break;

                    case 0x00FD: // LABELSST (Cell text referencing SST)
                        if (activeSheet != null && recLen >= 10)
                        {
                            uint row = (uint)(BitConverter.ToUInt16(stream, pos) + 1);
                            uint col = (uint)(BitConverter.ToUInt16(stream, pos + 2) + 1);
                            int sstIndex = BitConverter.ToInt32(stream, pos + 6);
                            if (sstIndex >= 0 && sstIndex < sstStrings.Count)
                            {
                                activeSheet.SetCellString(row, col, sstStrings[sstIndex]);
                            }
                        }
                        break;

                    case 0x0006: // FORMULA
                        if (activeSheet != null && recLen >= 20)
                        {
                            uint row = (uint)(BitConverter.ToUInt16(stream, pos) + 1);
                            uint col = (uint)(BitConverter.ToUInt16(stream, pos + 2) + 1);
                            double numVal = BitConverter.ToDouble(stream, pos + 6);
                            if (!double.IsNaN(numVal))
                            {
                                activeSheet.SetCellNumber(row, col, numVal);
                            }
                        }
                        break;

                    case 0x000A: // EOF
                        break;
                }

                pos += recLen;
            }

            if (calcView.Sheets.Count == 0)
            {
                var defaultSheet = new SparseMatrix();
                calcView.Sheets.Add(defaultSheet);
                calcView.SheetNames.Add("Sheet1");
                calcView.SheetStates.Add(new CalcSheetState());
            }

            calcView.ActiveSheetIndex = 0;
            calcView.Invalidate();
            return true;
        }

        private static void ParseSstRecord(byte[] data, int offset, int length, List<string> output)
        {
            try
            {
                if (length < 8) return;
                int totalStrings = BitConverter.ToInt32(data, offset);
                int uniqueStrings = BitConverter.ToInt32(data, offset + 4);
                int p = offset + 8;
                int end = offset + length;

                while (p + 3 <= end && output.Count < uniqueStrings)
                {
                    ushort charCount = BitConverter.ToUInt16(data, p);
                    byte flags = data[p + 2];
                    p += 3;

                    bool isUnicode = (flags & 0x01) != 0;
                    bool hasRich = (flags & 0x08) != 0;
                    bool hasExt = (flags & 0x04) != 0;

                    if (hasRich) p += 2;
                    if (hasExt) p += 4;

                    int byteLen = charCount * (isUnicode ? 2 : 1);
                    if (p + byteLen > end) break;

                    string str = isUnicode ? Encoding.Unicode.GetString(data, p, byteLen) : Encoding.ASCII.GetString(data, p, byteLen);
                    output.Add(str);
                    p += byteLen;
                }
            }
            catch { }
        }

        private static double DecodeRk(uint rk)
        {
            bool isFloat = (rk & 0x02) == 0;
            bool isDividedBy100 = (rk & 0x01) != 0;
            double val;

            if (isFloat)
            {
                ulong bits = ((ulong)(rk & 0xFFFFFFFC)) << 32;
                val = BitConverter.Int64BitsToDouble((long)bits);
            }
            else
            {
                int intVal = (int)(rk >> 2);
                val = (double)intVal;
            }

            if (isDividedBy100) val /= 100.0;
            return val;
        }
    }
}
