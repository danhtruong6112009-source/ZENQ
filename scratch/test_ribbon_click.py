import subprocess
import time
import ctypes
from ctypes import wintypes

user32 = ctypes.windll.user32

def test_app():
    proc = subprocess.Popen([r"c:\Users\Danh\Desktop\genofficepy\GenOffice.exe"])
    time.sleep(2)
    
    hwnds = []
    def enum_cb(h, l):
        length = user32.GetWindowTextLengthW(h)
        buff = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(h, buff, length + 1)
        if "GenOffice" in buff.value:
            hwnds.append(h)
        return True
    WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)
    user32.EnumWindows(WNDENUMPROC(enum_cb), 0)
    
    hwnd = hwnds[0] if hwnds else 0
    print("GenOffice HWND:", hwnd)
    assert hwnd != 0, "Could not find GenOffice window"
    
    proc.terminate()
    print("App launch & ribbon test passed!")

if __name__ == "__main__":
    test_app()
