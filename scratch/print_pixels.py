from PIL import Image

im = Image.open(r'C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\zoom_bucket.png')
print(f"Original size: {im.size}")
# Downscale by factor of 8 or whatever the block size is
# Let's see: if size is 128x128, block size is 8. If size is 160x160, block size is 10.
native_w = im.size[0] // 8
native_h = im.size[1] // 8
small = im.resize((native_w, native_h), Image.Resampling.NEAREST)
small = small.convert('RGBA')

print(f"Native size: {native_w}x{native_h}")
for y in range(native_h):
    row = ""
    for x in range(native_w):
        r, g, b, a = small.getpixel((x, y))
        if a > 50 and (r < 220 or g < 220 or b < 220):
            if r < 120:
                row += "#"
            elif r < 170:
                row += "+"
            else:
                row += ":"
        else:
            row += " "
    print(f"{y:2d}: |{row}|")
