import os, re, sys

with open('scratch/remaining_audit.txt', 'w', encoding='utf-8') as out:
    for root, dirs, files in os.walk('src'):
        for f in files:
            if f.endswith('.cs'):
                p = os.path.join(root, f)
                with open(p, 'r', encoding='utf-8', errors='ignore') as fp:
                    for i, line in enumerate(fp):
                        stripped = line.strip()
                        if re.match(r'^(using|namespace)\s+GenOffice', stripped):
                            continue
                        if re.search(r'"[^"]*genoffice', stripped, re.I):
                            out.write(f'{p}:{i+1}: {stripped}\n')

print('Done writing scratch/remaining_audit.txt')
