import glob, re, sys

for f in sorted(glob.glob('scratch/cat*.py')):
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    for line_idx, line in enumerate(lines):
        # Look for AddItem(cat, "...", "...", (w, h) => W...
        m_gdi = re.search(r'AddItem\([^,]+,\s*"[^"]+",\s*"[^"]+",\s*\([^)]*\)\s*=>\s*(W[A-Za-z0-9_]+)', line)
        if m_gdi:
            print(f"{f}:{line_idx+1} BAD GDI: calls {m_gdi.group(1)}")
        m_wpf = re.search(r'AddItem\([^,]+,\s*"[^"]+",\s*"[^"]+",\s*[^,]+,\s*\([^)]*\)\s*=>\s*(G[A-Za-z0-9_]+)', line)
        if m_wpf:
            print(f"{f}:{line_idx+1} BAD WPF: calls {m_wpf.group(1)}")
print("Done checking.")
