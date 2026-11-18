import argparse
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
NOTES = ROOT / '.github' / 'release-notes'

UNSIGNED = """> [!WARNING]
> **Not code signed yet.** Windows SmartScreen may show *Windows protected your PC*: choose **More info**, then **Run anyway**.
> Signing through the SignPath Foundation is on its way, see the [code signing policy](https://github.com/{repo}#code-signing-policy)."""

SIGNED = """> [!NOTE]
> **Signed release.** Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).
> See the [code signing policy](https://github.com/{repo}#code-signing-policy)."""


def size(path):
    if not path or not Path(path).is_file():
        return '—'
    return f'{Path(path).stat().st_size / 1048576:.1f} MB'


def changes(repo, tag):
    result = subprocess.run(
        ['gh', 'api', f'repos/{repo}/releases/generate-notes', '-f', f'tag_name={tag}'],
        capture_output=True, text=True, check=True)
    body = json.loads(result.stdout)['body']
    lines = [line for line in body.splitlines() if not line.startswith('<!-- ')]
    return '\n'.join(lines).strip()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag', required=True)
    parser.add_argument('--repo', required=True)
    parser.add_argument('--signed', action='store_true')
    parser.add_argument('--setup', default='')
    parser.add_argument('--archive', default='')
    parser.add_argument('--output', required=True)
    args = parser.parse_args()

    version = args.tag.lstrip('v')
    highlights_file = NOTES / f'{args.tag}.md'
    highlights = highlights_file.read_text(encoding='utf-8').strip() if highlights_file.is_file() else ''
    values = {
        'VERSION': version,
        'REPO': args.repo,
        'DOWNLOAD': f'https://github.com/{args.repo}/releases/download/{args.tag}',
        'HIGHLIGHTS': highlights,
        'SETUP_SIZE': size(args.setup),
        'ZIP_SIZE': size(args.archive),
        'SIGNING': (SIGNED if args.signed else UNSIGNED).format(repo=args.repo),
        'CHANGES': changes(args.repo, args.tag),
    }
    text = (NOTES / 'template.md').read_text(encoding='utf-8')
    for key, value in values.items():
        text = text.replace('{{' + key + '}}', value)
    while '\n\n\n' in text:
        text = text.replace('\n\n\n', '\n\n')
    Path(args.output).write_text(text, encoding='utf-8')


if __name__ == '__main__':
    os.environ.setdefault('GH_PAGER', '')
    main()
