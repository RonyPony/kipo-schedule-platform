import re
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
result = subprocess.run(['git', 'ls-files', '--cached', '--others', '--exclude-standard', '-z'], cwd=root, check=True, capture_output=True)
errors = []
extensions = {'.cs', '.csproj', '.props', '.json', '.ts', '.mjs', '.html', '.scss', '.md', '.yaml', '.yml', '.py', '.ps1', '.slnx'}
secret_patterns = [r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----', r'gh[pousr]_[A-Za-z0-9]{30,}', r'AKIA[A-Z0-9]{16}']
for name in result.stdout.decode('utf-8').split('\0'):
    if not name:
        continue
    path = root / name
    if path.suffix not in extensions or not path.is_file():
        continue
    try:
        text = path.read_text(encoding='utf-8-sig')
    except UnicodeDecodeError:
        errors.append(f'{name}: codificación no válida; se requiere UTF-8.')
        continue
    if '\ufffd' in text or re.search(r'\u00c3[\u0080-\u00bf]|\u00c2[\u0080-\u00bf]|\u00e2\u20ac', text):
        errors.append(f'{name}: posible texto mal decodificado.')
    if any(re.search(pattern, text) for pattern in secret_patterns):
        errors.append(f'{name}: posible credencial privada.')
if errors:
    raise SystemExit('\n'.join(errors))
print('Revisión UTF-8 y patrones de secretos: correcta.')
