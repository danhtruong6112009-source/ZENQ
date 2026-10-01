from PIL import Image

im = Image.open(r'C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\zoom_bucket.png')
native_w = im.size[0] // 8
native_h = im.size[1] // 8
small = im.resize((native_w, native_h), Image.Resampling.NEAREST)
small = small.convert('RGBA')

print("Colors in zoom_bucket.png:")
for y in range(native_h):
    row_chars = []
    for x in range(native_w):
        r, g, b, a = small.getpixel((x, y))
        if a < 30 or (r > 240 and g > 240 and b > 240):
            row_chars.append("  ")
        else:
            # Check hue
            if max(r, g, b) - min(r, g, b) < 20: # Grayscale
                # Gray level
                if r < 100:
                    row_chars.append("##")
                elif r < 160:
                    row_chars.append("==")
                else:
                    row_chars.append("::")
            else: # Colored
                row_chars.append("CC")
    line = "".join(row_chars)
    if line.strip():
        print(f"{y:2d}: {line}")
