import os, sys
from collections import defaultdict

with open('scratch/string_matches.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

by_file = defaultdict(list)
for l in lines:
    l = l.strip()
    idx = l.find('.cs:')
    if idx != -1:
        fn = l[:idx+3]
        rest = l[idx+4:]
        by_file[fn].append(rest)

with open('scratch/string_matches_by_file.txt', 'w', encoding='utf-8') as f:
    for fn, items in sorted(by_file.items()):
        f.write(f'=== {fn} ({len(items)}) ===\n')
        for it in items:
            f.write(f'  {it}\n')
        f.write('\n')

print(f'Done grouping {len(by_file)} files into scratch/string_matches_by_file.txt')
