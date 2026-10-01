from PIL import Image
import os

img = Image.open('GENOFFICE.png').convert('RGBA')
sizes = [(256,256),(128,128),(64,64),(48,48),(32,32),(16,16)]
frames = []
for s in sizes:
    frame = img.resize(s, Image.LANCZOS)
    frames.append(frame)

frames[0].save('GenOffice.ico', format='ICO',
               sizes=[(s,s) for s in [256,128,64,48,32,16]],
               append_images=frames[1:])

print("ICO saved OK")
print("ICO size:", os.path.getsize("GenOffice.ico"), "bytes")
