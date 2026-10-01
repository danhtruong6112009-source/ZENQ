import urllib.request
import re

url = 'https://ecma-international.org/publications-and-standards/standards/ecma-376/'
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
try:
    with urllib.request.urlopen(req, timeout=10) as resp:
        html = resp.read().decode('utf-8')
        links = re.findall(r'href=[\'"]([^\'"]+\.zip)[\'"]', html, re.I)
        for l in links:
            print('ZIP link:', l)
except Exception as e:
    print('Error:', e)
