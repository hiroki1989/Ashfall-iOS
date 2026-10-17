"""Select the newest installed stable Xcode; do not assume an image path."""
import os
from pathlib import Path
import plistlib
import re

candidates = []
for app in Path('/Applications').glob('Xcode*.app'):
    if 'beta' in app.name.lower() or 'rc' in app.name.lower():
        continue
    with (app / 'Contents/version.plist').open('rb') as file:
        version = plistlib.load(file)['CFBundleShortVersionString']
    if re.fullmatch(r'\d+(\.\d+)*', version):
        candidates.append((tuple(map(int, version.split('.'))), app))
if not candidates:
    raise SystemExit('No stable installed Xcode found on macos-15.')
version, app = max(candidates)
if version < (26,):
    raise SystemExit('An installed stable Xcode 26 or newer is required. Update the runner image when available.')
with open(os.environ['GITHUB_ENV'], 'a') as env:
    env.write('DEVELOPER_DIR=' + str(app / 'Contents/Developer') + '\n')
print('Selected ' + app.name)
