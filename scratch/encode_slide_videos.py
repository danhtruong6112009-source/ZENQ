import os
import glob
from PIL import Image
import cv2

desktop_dir = os.path.join(os.path.expanduser("~"), "Desktop", "GenOffice_Test_All_Modules_Report")
slide_dir = os.path.join(desktop_dir, "01_SLIDES_TEST")
frames_dir = os.path.join(slide_dir, "AnimFrames")
videos_dir = os.path.join(slide_dir, "Videos")
os.makedirs(videos_dir, exist_ok=True)

frame_files = sorted(glob.glob(os.path.join(frames_dir, "*.png")))
print(f"[VIDEO ENCODER] Found {len(frame_files)} animation frames.")

if frame_files:
    # 1. Create MP4 Video (H.264 / mp4v)
    mp4_path = os.path.join(videos_dir, "slides_morph_and_animations.mp4")
    first_frame = cv2.imread(frame_files[0])
    h, w, _ = first_frame.shape
    fps = 30
    
    fourcc = cv2.VideoWriter_fourcc(*'mp4v')
    out = cv2.VideoWriter(mp4_path, fourcc, fps, (w, h))
    
    for fpath in frame_files:
        img = cv2.imread(fpath)
        out.write(img)
    out.release()
    print(f"  -> Generated MP4: {mp4_path} ({os.path.getsize(mp4_path)} bytes)")

    # 2. Create Animated GIF
    gif_path = os.path.join(videos_dir, "slides_morph_and_animations.gif")
    pil_images = []
    # Sample every 2nd frame for smooth compact GIF
    for fpath in frame_files[::2]:
        img = Image.open(fpath).convert("RGB")
        # Resize to 960x540 for fast loading
        img = img.resize((960, 540), Image.Resampling.LANCZOS)
        pil_images.append(img)
    
    if pil_images:
        pil_images[0].save(
            gif_path,
            save_all=True,
            append_images=pil_images[1:],
            duration=40, # ~25fps
            loop=0
        )
        print(f"  -> Generated GIF: {gif_path} ({os.path.getsize(gif_path)} bytes)")

print("[HOÀN TẤT XUẤT VIDEO SLIDE]")
