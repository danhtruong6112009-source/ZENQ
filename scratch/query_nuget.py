import urllib.request
import json

url = 'https://azuresearch-usnc.nuget.org/query?q=Tags:%22openxml%22&take=25'
req = urllib.request.Request(url, headers={'User-Agent': 'GenOffice-Client'})
try:
    with urllib.request.urlopen(req) as resp:
        data = json.loads(resp.read().decode('utf-8'))
        print(f"Total found: {data.get('totalHits')}")
        for item in data.get('data', []):
            pkg_id = item.get('id')
            ver = item.get('version')
            downloads = item.get('totalDownloads')
            desc = (item.get('description') or '').replace('\n', ' ')[:100]
            print(f"- {pkg_id} ({ver}) [{downloads:,} dl]: {desc}")
except Exception as e:
    print('Error:', e)
