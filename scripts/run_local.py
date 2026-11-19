"""Run the existing local Task 10 profiles without changing databases or volumes."""
import argparse
import os
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('operation', choices=['start', 'stop', 'restart', 'web'])
parser.add_argument('--profiles', type=Path, default=root / 'config/task10')
args = parser.parse_args()
profiles = args.profiles.resolve()
names = ['disasters', 'integration', 'damage', 'reconstruction', 'gateway', 'gateway-mobile']
if args.operation == 'web':
    environment = os.environ.copy()
    path = profiles / 'razor.env'
    if path.stat().st_mode & 0o077:
        raise SystemExit('razor.env must have mode 600')
    for line in path.read_text().splitlines():
        if line and not line.startswith('#'):
            key, value = line.split('=', 1)
            environment[key] = value
    os.chdir(root / 'disaster-platform-presentation')
    os.execvpe('dotnet', ['dotnet', 'bin/Debug/net10.0/disaster-platform-web.dll'], environment)
if args.operation == 'stop':
    subprocess.run(['docker', 'stop', *['task10-' + name for name in reversed(names)]], check=True)
else:
    for name in names:
        subprocess.run(['docker', 'restart' if args.operation == 'restart' else 'start', 'task10-' + name], check=True)
