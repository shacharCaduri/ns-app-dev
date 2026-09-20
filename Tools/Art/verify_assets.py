"""Verify deliverables without modifying images. Requires Pillow."""
import json
from pathlib import Path
from collections import Counter
from PIL import Image
entries=json.loads(Path('Assets/Art/asset_inventory.json').read_text())
paths=[e['path'] for e in entries]
assert len(paths)==len(set(paths)), 'Duplicate asset paths'
for e in entries:
    p=Path(e['path'])
    assert p.exists(),p
    assert p.stem==p.stem.lower() and all(c.isalnum() or c=='_' for c in p.stem),p
    with Image.open(p) as im:
        assert im.mode=='RGBA',p
        assert im.size==(e['width'],e['height']),p
        lo,hi=im.getchannel('A').getextrema()
        assert lo==0 and hi>0, (p,'missing transparency or empty sprite')
        if '/Wizard/' in str(p):assert im.size==(384,256),p
        if '/Enemies/' in str(p):assert im.size==(240,224),p
print(f'PASS: {len(paths)} unique, named, nonempty RGBA sprites; dimensions and transparency verified.')
print(dict(Counter(p.split('/')[2] for p in paths)))
