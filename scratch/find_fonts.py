import zlib, re

with open(r"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf", "rb") as f:
    data = f.read()

# Find all streams and decompress
matches = [m.start() for m in re.finditer(b"stream\r\n|stream\n", data)]
print("Streams count:", len(matches))

for i, m in enumerate(matches):
    start = data.find(b"\n", m) + 1
    end = data.find(b"endstream", start)
    if end > start:
        stream_data = data[start:end].rstrip(b"\r\n")
        try:
            decomp = zlib.decompress(stream_data)
            # check for font names
            for font_match in re.finditer(rb"/BaseFont\s*/([A-Za-z0-9\+\-_]+)", decomp):
                print(f"Stream {i} font:", font_match.group(1).decode('ascii', 'ignore'))
            for tf_match in re.finditer(rb"/FontDescriptor.*?/FontName\s*/([A-Za-z0-9\+\-_]+)", decomp, re.DOTALL):
                print(f"Stream {i} desc:", tf_match.group(1).decode('ascii', 'ignore'))
            if b"/Font" in decomp:
                # print snippet
                pos = decomp.find(b"/Font")
                print(f"Stream {i} /Font snippet:", decomp[pos:pos+150])
        except Exception:
            pass
