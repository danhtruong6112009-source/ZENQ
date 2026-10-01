import glob, re

for f in sorted(glob.glob('scratch/cat*.py')):
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    
    modified = False
    new_lines = []
    for line in lines:
        def replacer(match):
            prefix = match.group(1)
            wfunc = match.group(2)
            rest = match.group(3)
            gfunc = "G" + wfunc[1:]
            return f"{prefix}{gfunc}{rest}"

        new_line = re.sub(r'(AddItem\([^,]+,\s*"[^"]+",\s*"[^"]+",\s*\([^\)]*\)\s*=>\s*)W([A-Za-z0-9_]+)(.*)', replacer, line)
        if new_line != line:
            modified = True
        new_lines.append(new_line)
    
    if modified:
        with open(f, 'w', encoding='utf-8') as fp:
            fp.writelines(new_lines)
        print(f"Fixed {f}")

print("Finished fixing bad generator calls.")
