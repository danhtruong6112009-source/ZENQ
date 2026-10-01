import numpy as np
from PIL import Image, ImageDraw

# Create 20x20 image
im = Image.new('RGBA', (20, 20), (255, 255, 255, 255))
draw = ImageDraw.Draw(im)

priColor = (55, 65, 81, 255)
accColor = (0, 120, 212, 255)

# 1. Swatch bar
draw.rectangle([4, 12, 17, 14], fill=accColor)

# 2. Handle
draw.arc([8, 2, 12, 7], start=180, end=0, fill=priColor, width=1)

# 3. Bucket body (Single clean polygon, white fill, dark outline)
bucket_poly = [
    (8, 5),   # top-left rim
    (6, 7),   # upper-left body
    (6, 8),   # lower-left body
    (8, 10),  # bottom-left base
    (11, 10), # bottom-right base
    (12, 7),  # right lip
    (10, 5)   # top-right rim
]
draw.polygon(bucket_poly, fill=(255, 255, 255, 255), outline=priColor)

# 4. Pour stream (flows from lip (12, 6) down to (15, 10))
draw.line([(12, 6), (14, 6), (14, 9), (14, 11)], fill=accColor, width=2)

# Save test
big = im.resize((160, 160), Image.Resampling.NEAREST)
big.save('scratch/simulated_bucket.png')
print("Saved simulated_bucket.png")
