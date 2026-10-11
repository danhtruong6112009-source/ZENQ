using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace ZenQOffice.Format
{
    public class PsdLayerData
    {
        public string Name { get; set; } = "Layer";
        public int Top { get; set; }
        public int Left { get; set; }
        public int Bottom { get; set; }
        public int Right { get; set; }
        public int Width => Math.Max(0, Right - Left);
        public int Height => Math.Max(0, Bottom - Top);
        public byte Opacity { get; set; } = 255;
        public bool IsVisible { get; set; } = true;
        public string BlendModeKey { get; set; } = "norm";
        public Bitmap Bitmap { get; set; }
    }

    public class PsdDocumentData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Channels { get; set; }
        public int BitDepth { get; set; }
        public Bitmap CompositeImage { get; set; }
        public List<PsdLayerData> Layers { get; set; } = new List<PsdLayerData>();
    }

    public static class PsdHandler
    {
        public static PsdDocumentData LoadPsd(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return LoadPsd(fs);
            }
        }

        public static PsdDocumentData LoadPsd(Stream stream)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true))
                {
                    // 1. Header (26 bytes)
                    string signature = new string(reader.ReadChars(4));
                    if (signature != "8BPS") return null;

                    ushort version = ReadBigEndianUInt16(reader);
                    if (version != 1 && version != 2) return null;

                    reader.BaseStream.Seek(6, SeekOrigin.Current); // 6 reserved bytes

                    ushort channels = ReadBigEndianUInt16(reader);
                    int height = (int)ReadBigEndianUInt32(reader);
                    int width = (int)ReadBigEndianUInt32(reader);
                    ushort depth = ReadBigEndianUInt16(reader);
                    ushort colorMode = ReadBigEndianUInt16(reader); // 3 = RGB, 1 = Grayscale

                    if (width <= 0 || height <= 0 || (depth != 8 && depth != 16)) return null;

                    PsdDocumentData psdDoc = new PsdDocumentData
                    {
                        Width = width,
                        Height = height,
                        Channels = channels,
                        BitDepth = depth
                    };

                    // 2. Color Mode Data
                    uint colorModeLen = ReadBigEndianUInt32(reader);
                    if (colorModeLen > 0)
                    {
                        reader.BaseStream.Seek(colorModeLen, SeekOrigin.Current);
                    }

                    // 3. Image Resources
                    uint imageResourcesLen = ReadBigEndianUInt32(reader);
                    if (imageResourcesLen > 0)
                    {
                        reader.BaseStream.Seek(imageResourcesLen, SeekOrigin.Current);
                    }

                    // 4. Layer and Mask Information Section
                    long layerSectionStart = reader.BaseStream.Position;
                    if (reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
                    {
                        uint layerSectionLen = ReadBigEndianUInt32(reader);
                        long nextSectionPos = reader.BaseStream.Position + layerSectionLen;

                        if (layerSectionLen > 0 && nextSectionPos <= reader.BaseStream.Length)
                        {
                            try
                            {
                                ReadLayerSection(reader, psdDoc, width, height, nextSectionPos);
                            }
                            catch
                            {
                                // Fallback if layer section has custom structures
                            }
                            reader.BaseStream.Seek(nextSectionPos, SeekOrigin.Begin);
                        }
                    }

                    // 5. Image Data Section (Composite Image)
                    if (psdDoc.Layers.Count == 0 && reader.BaseStream.Position + 2 <= reader.BaseStream.Length)
                    {
                        try
                        {
                            psdDoc.CompositeImage = ReadCompositeImageData(reader, width, height, channels, depth);
                        }
                        catch
                        {
                        }
                    }

                    // Ensure at least composite is available or layers are present
                    if (psdDoc.CompositeImage == null && psdDoc.Layers.Count > 0)
                    {
                        psdDoc.CompositeImage = RenderCompositeFromLayers(psdDoc);
                    }

                    return psdDoc;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PSD load exception: " + ex.Message);
                return null;
            }
        }

        private static void ReadLayerSection(BinaryReader reader, PsdDocumentData psdDoc, int docW, int docH, long maxPos)
        {
            if (reader.BaseStream.Position + 4 > maxPos) return;
            uint layerInfoLen = ReadBigEndianUInt32(reader);
            if (layerInfoLen == 0 || reader.BaseStream.Position + 2 > maxPos) return;

            short rawLayerCount = ReadBigEndianInt16(reader);
            int layerCount = Math.Abs(rawLayerCount);
            if (layerCount <= 0 || layerCount > 1000) return;

            List<PsdLayerData> layerRecords = new List<PsdLayerData>();
            List<List<(short channelId, uint channelLen)>> layerChannelInfos = new List<List<(short, uint)>>();

            // Read Layer Records
            for (int i = 0; i < layerCount; i++)
            {
                int top = ReadBigEndianInt32(reader);
                int left = ReadBigEndianInt32(reader);
                int bottom = ReadBigEndianInt32(reader);
                int right = ReadBigEndianInt32(reader);

                ushort channelCount = ReadBigEndianUInt16(reader);
                var channelInfoList = new List<(short, uint)>();
                for (int c = 0; c < channelCount; c++)
                {
                    short chId = ReadBigEndianInt16(reader); // 0=R, 1=G, 2=B, -1=transparency
                    uint chLen = ReadBigEndianUInt32(reader);
                    channelInfoList.Add((chId, chLen));
                }

                string blendSig = new string(reader.ReadChars(4)); // 8BIM
                string blendKey = new string(reader.ReadChars(4)); // norm, mul , etc.
                byte opacity = reader.ReadByte();
                byte clipping = reader.ReadByte();
                byte flags = reader.ReadByte();
                reader.ReadByte(); // filler

                uint extraLen = ReadBigEndianUInt32(reader);
                long extraEnd = reader.BaseStream.Position + extraLen;

                // Mask data
                uint maskLen = ReadBigEndianUInt32(reader);
                reader.BaseStream.Seek(maskLen, SeekOrigin.Current);

                // Blending ranges
                uint blendLen = ReadBigEndianUInt32(reader);
                reader.BaseStream.Seek(blendLen, SeekOrigin.Current);

                // Layer Name (Pascal string padded to 4 bytes)
                string layerName = "Layer " + (i + 1);
                if (reader.BaseStream.Position < extraEnd)
                {
                    byte nameLen = reader.ReadByte();
                    if (nameLen > 0 && reader.BaseStream.Position + nameLen <= extraEnd)
                    {
                        layerName = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
                    }
                    int pad = (4 - ((nameLen + 1) % 4)) % 4;
                    reader.BaseStream.Seek(pad, SeekOrigin.Current);
                }

                reader.BaseStream.Seek(extraEnd, SeekOrigin.Begin);

                bool isVisible = (flags & 0x02) == 0; // bit 1: 0 = visible, 1 = invisible

                PsdLayerData layer = new PsdLayerData
                {
                    Name = layerName,
                    Top = top,
                    Left = left,
                    Bottom = bottom,
                    Right = right,
                    Opacity = opacity,
                    IsVisible = isVisible,
                    BlendModeKey = blendKey
                };

                layerRecords.Add(layer);
                layerChannelInfos.Add(channelInfoList);
            }

            // Read Layer Channel Image Data
            for (int i = 0; i < layerRecords.Count; i++)
            {
                var layer = layerRecords[i];
                var chInfos = layerChannelInfos[i];
                int lw = layer.Width;
                int lh = layer.Height;

                if (lw <= 0 || lh <= 0)
                {
                    // Empty layer
                    layer.Bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
                    psdDoc.Layers.Add(layer);
                    continue;
                }

                byte[] redPlane = new byte[lw * lh];
                byte[] greenPlane = new byte[lw * lh];
                byte[] bluePlane = new byte[lw * lh];
                byte[] alphaPlane = new byte[lw * lh];
                for (int a = 0; a < alphaPlane.Length; a++) alphaPlane[a] = 255;

                foreach (var ch in chInfos)
                {
                    long channelStartPos = reader.BaseStream.Position;
                    short chId = ch.channelId;
                    ushort compType = ReadBigEndianUInt16(reader);

                    byte[] chData = new byte[lw * lh];
                    if (compType == 0) // Raw
                    {
                        reader.Read(chData, 0, Math.Min(chData.Length, (int)ch.channelLen - 2));
                    }
                    else if (compType == 1) // RLE PackBits
                    {
                        ushort[] scanlineLens = new ushort[lh];
                        for (int r = 0; r < lh; r++) scanlineLens[r] = ReadBigEndianUInt16(reader);

                        for (int r = 0; r < lh; r++)
                        {
                            int lineLen = scanlineLens[r];
                            byte[] compLine = reader.ReadBytes(lineLen);
                            DecodePackBits(compLine, chData, r * lw, lw);
                        }
                    }

                    if (chId == 0) redPlane = chData;
                    else if (chId == 1) greenPlane = chData;
                    else if (chId == 2) bluePlane = chData;
                    else if (chId == -1) alphaPlane = chData;

                    // Ensure we advance exactly ch.channelLen bytes
                    long expectedNextPos = channelStartPos + ch.channelLen;
                    if (reader.BaseStream.Position < expectedNextPos)
                    {
                        reader.BaseStream.Seek(expectedNextPos - reader.BaseStream.Position, SeekOrigin.Current);
                    }
                }

                // Construct 32bpp ARGB Bitmap
                Bitmap layerBmp = new Bitmap(lw, lh, PixelFormat.Format32bppArgb);
                BitmapData bData = layerBmp.LockBits(new Rectangle(0, 0, lw, lh), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                unsafe
                {
                    byte* ptr = (byte*)bData.Scan0;
                    for (int y = 0; y < lh; y++)
                    {
                        byte* row = ptr + y * bData.Stride;
                        int pixOffset = y * lw;
                        for (int x = 0; x < lw; x++)
                        {
                            int idx = pixOffset + x;
                            row[x * 4 + 0] = bluePlane[idx];
                            row[x * 4 + 1] = greenPlane[idx];
                            row[x * 4 + 2] = redPlane[idx];
                            row[x * 4 + 3] = alphaPlane[idx];
                        }
                    }
                }
                layerBmp.UnlockBits(bData);
                layer.Bitmap = layerBmp;
                psdDoc.Layers.Add(layer);
            }
        }

        private static Bitmap ReadCompositeImageData(BinaryReader reader, int width, int height, int channels, int depth)
        {
            ushort compType = ReadBigEndianUInt16(reader);
            int totalPixels = width * height;

            byte[] red = new byte[totalPixels];
            byte[] green = new byte[totalPixels];
            byte[] blue = new byte[totalPixels];
            byte[] alpha = new byte[totalPixels];
            for (int i = 0; i < totalPixels; i++) alpha[i] = 255;

            int effectiveChannels = Math.Min(4, channels);

            if (compType == 0) // Raw
            {
                for (int c = 0; c < effectiveChannels; c++)
                {
                    byte[] chPlane = (c == 0) ? red : (c == 1) ? green : (c == 2) ? blue : alpha;
                    reader.Read(chPlane, 0, totalPixels);
                }
            }
            else if (compType == 1) // RLE PackBits
            {
                int totalScanlines = height * channels;
                ushort[] scanlineLens = new ushort[totalScanlines];
                for (int i = 0; i < totalScanlines; i++)
                {
                    scanlineLens[i] = ReadBigEndianUInt16(reader);
                }

                for (int c = 0; c < effectiveChannels; c++)
                {
                    byte[] chPlane = (c == 0) ? red : (c == 1) ? green : (c == 2) ? blue : alpha;
                    for (int y = 0; y < height; y++)
                    {
                        int sIndex = c * height + y;
                        int sLen = scanlineLens[sIndex];
                        byte[] compLine = reader.ReadBytes(sLen);
                        DecodePackBits(compLine, chPlane, y * width, width);
                    }
                }
            }

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData bData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* ptr = (byte*)bData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + y * bData.Stride;
                    int pixOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int idx = pixOffset + x;
                        row[x * 4 + 0] = blue[idx];
                        row[x * 4 + 1] = green[idx];
                        row[x * 4 + 2] = red[idx];
                        row[x * 4 + 3] = (channels >= 4) ? alpha[idx] : (byte)255;
                    }
                }
            }
            bmp.UnlockBits(bData);
            return bmp;
        }

        private static void DecodePackBits(byte[] input, byte[] output, int outOffset, int expectedCount)
        {
            int inPos = 0;
            int outPos = outOffset;
            int endOut = outOffset + expectedCount;

            while (inPos < input.Length && outPos < endOut)
            {
                sbyte n = (sbyte)input[inPos++];
                if (n >= 0) // Copy n + 1 bytes literally
                {
                    int count = n + 1;
                    for (int i = 0; i < count && inPos < input.Length && outPos < endOut; i++)
                    {
                        output[outPos++] = input[inPos++];
                    }
                }
                else if (n >= -127) // Repeat next byte 1 - n times
                {
                    int count = 1 - n;
                    if (inPos < input.Length)
                    {
                        byte b = input[inPos++];
                        for (int i = 0; i < count && outPos < endOut; i++)
                        {
                            output[outPos++] = b;
                        }
                    }
                }
                // n == -128 is a NOP
            }
        }

        public static Bitmap RenderCompositeFromLayers(PsdDocumentData doc)
        {
            if (doc == null || doc.Width <= 0 || doc.Height <= 0) return null;
            Bitmap composite = new Bitmap(doc.Width, doc.Height, PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(composite))
            {
                g.Clear(Color.Transparent);
                for (int i = 0; i < doc.Layers.Count; i++)
                {
                    var layer = doc.Layers[i];
                    if (!layer.IsVisible || layer.Bitmap == null) continue;

                    float opacity = layer.Opacity / 255.0f;
                    using (ImageAttributes attr = new ImageAttributes())
                    {
                        ColorMatrix cm = new ColorMatrix();
                        cm.Matrix33 = opacity;
                        attr.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                        Rectangle destRect = new Rectangle(layer.Left, layer.Top, layer.Width, layer.Height);
                        g.DrawImage(layer.Bitmap, destRect, 0, 0, layer.Width, layer.Height, GraphicsUnit.Pixel, attr);
                    }
                }
            }
            return composite;
        }

        public static bool SavePsd(string filePath, Bitmap bitmap)
        {
            if (bitmap == null) return false;
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (BinaryWriter writer = new BinaryWriter(fs, Encoding.ASCII))
                {
                    int w = bitmap.Width;
                    int h = bitmap.Height;

                    // 1. Header (26 bytes)
                    writer.Write(Encoding.ASCII.GetBytes("8BPS"));
                    WriteBigEndianUInt16(writer, 1); // Version = 1
                    writer.Write(new byte[6]);      // Reserved (6 bytes)
                    WriteBigEndianUInt16(writer, 4); // 4 Channels: R, G, B, A
                    WriteBigEndianUInt32(writer, (uint)h);
                    WriteBigEndianUInt32(writer, (uint)w);
                    WriteBigEndianUInt16(writer, 8); // 8-bit depth
                    WriteBigEndianUInt16(writer, 3); // ColorMode: RGB

                    // 2. Color Mode Data
                    WriteBigEndianUInt32(writer, 0);

                    // 3. Image Resources
                    WriteBigEndianUInt32(writer, 0);

                    // 4. Layer and Mask Information
                    WriteBigEndianUInt32(writer, 0);

                    // 5. Image Data Section (RLE PackBits)
                    WriteBigEndianUInt16(writer, 1); // Compression: RLE

                    // Extract R, G, B, A channels
                    byte[] rPlane = new byte[w * h];
                    byte[] gPlane = new byte[w * h];
                    byte[] bPlane = new byte[w * h];
                    byte[] aPlane = new byte[w * h];

                    BitmapData bData = bitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        byte* ptr = (byte*)bData.Scan0;
                        for (int y = 0; y < h; y++)
                        {
                            byte* row = ptr + y * bData.Stride;
                            int offset = y * w;
                            for (int x = 0; x < w; x++)
                            {
                                int idx = offset + x;
                                bPlane[idx] = row[x * 4 + 0];
                                gPlane[idx] = row[x * 4 + 1];
                                rPlane[idx] = row[x * 4 + 2];
                                aPlane[idx] = row[x * 4 + 3];
                            }
                        }
                    }
                    bitmap.UnlockBits(bData);

                    byte[][] channels = new byte[][] { rPlane, gPlane, bPlane, aPlane };
                    List<byte[]> compressedLines = new List<byte[]>();

                    for (int c = 0; c < 4; c++)
                    {
                        byte[] plane = channels[c];
                        for (int y = 0; y < h; y++)
                        {
                            byte[] comp = EncodePackBits(plane, y * w, w);
                            compressedLines.Add(comp);
                        }
                    }

                    // Write scanline byte lengths (4 channels * h rows * 2 bytes)
                    for (int i = 0; i < compressedLines.Count; i++)
                    {
                        WriteBigEndianUInt16(writer, (ushort)compressedLines[i].Length);
                    }

                    // Write compressed scanlines
                    for (int i = 0; i < compressedLines.Count; i++)
                    {
                        writer.Write(compressedLines[i]);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Save PSD error: " + ex.Message);
                return false;
            }
        }

        private static byte[] EncodePackBits(byte[] src, int offset, int count)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int i = 0;
                while (i < count)
                {
                    // Check for repeated run
                    int runLen = 1;
                    while (i + runLen < count && runLen < 128 && src[offset + i + runLen] == src[offset + i])
                    {
                        runLen++;
                    }

                    if (runLen >= 3)
                    {
                        // RLE run: write (1 - runLen), then the byte
                        ms.WriteByte((byte)(1 - runLen));
                        ms.WriteByte(src[offset + i]);
                        i += runLen;
                    }
                    else
                    {
                        // Literal run: find how many literals until next 3-byte repeat or end
                        int litLen = 0;
                        while (i + litLen < count && litLen < 128)
                        {
                            if (i + litLen + 2 < count &&
                                src[offset + i + litLen] == src[offset + i + litLen + 1] &&
                                src[offset + i + litLen] == src[offset + i + litLen + 2])
                            {
                                break;
                            }
                            litLen++;
                        }
                        if (litLen == 0) litLen = 1;

                        ms.WriteByte((byte)(litLen - 1));
                        ms.Write(src, offset + i, litLen);
                        i += litLen;
                    }
                }
                return ms.ToArray();
            }
        }

        #region Big Endian Helpers
        private static ushort ReadBigEndianUInt16(BinaryReader r)
        {
            byte[] b = r.ReadBytes(2);
            return (ushort)((b[0] << 8) | b[1]);
        }

        private static short ReadBigEndianInt16(BinaryReader r)
        {
            byte[] b = r.ReadBytes(2);
            return (short)((b[0] << 8) | b[1]);
        }

        private static uint ReadBigEndianUInt32(BinaryReader r)
        {
            byte[] b = r.ReadBytes(4);
            return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
        }

        private static int ReadBigEndianInt32(BinaryReader r)
        {
            byte[] b = r.ReadBytes(4);
            return (int)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
        }

        private static void WriteBigEndianUInt16(BinaryWriter w, ushort val)
        {
            w.Write((byte)((val >> 8) & 0xFF));
            w.Write((byte)(val & 0xFF));
        }

        private static void WriteBigEndianUInt32(BinaryWriter w, uint val)
        {
            w.Write((byte)((val >> 24) & 0xFF));
            w.Write((byte)((val >> 16) & 0xFF));
            w.Write((byte)((val >> 8) & 0xFF));
            w.Write((byte)(val & 0xFF));
        }
        #endregion
    }
}
