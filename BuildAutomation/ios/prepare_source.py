"""Restore executable permissions lost when exporting/zipping on Windows."""
import os
from pathlib import Path

source = Path(os.environ['IOS_WORK']) / 'source'
mach_o = {bytes.fromhex(value) for value in ('feedface', 'cefaedfe', 'feedfacf', 'cffaedfe', 'cafebabe', 'bebafeca', 'cafebabf', 'bfbafeca')}
for path in source.rglob('*'):
    if path.is_file() and not path.is_symlink():
        with path.open('rb') as file:
            header = file.read(4)
        if header in mach_o or header.startswith(b'#!') or path.suffix == '.sh':
            path.chmod(path.stat().st_mode | 0o111)
