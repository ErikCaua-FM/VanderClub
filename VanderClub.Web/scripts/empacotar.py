"""Gera o ZIP portátil do código-fonte. Execute com Python 3, sem dependências."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parents[1]
target = root / 'dist/downloads/VanderClub_Web_v2_0.zip'
target.parent.mkdir(parents=True, exist_ok=True)
excluded = {'.git', '.openai', '.site_exports', 'node_modules', '__pycache__'}
files = [p for p in root.rglob('*') if p.is_file()
         and not p.is_symlink()
         and not any(part in excluded for part in p.relative_to(root).parts)
         and not p.is_relative_to(root / 'dist/downloads')
         and not p.name.startswith('__qa_')]
with ZipFile(target, 'w', ZIP_DEFLATED, compresslevel=9) as z:
    for file in sorted(files):
        z.write(file, 'VanderClub.Web/' + file.relative_to(root).as_posix())
with ZipFile(target) as z:
    assert z.testzip() is None
print(f'{target} — {len(files)} arquivos, {target.stat().st_size} bytes')
