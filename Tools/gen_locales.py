#!/usr/bin/env python3
"""Regenerates Locales/en.json and Locales/ru.json from the built-in table in Loc.cs.

Run it from the QuayTools folder after adding or changing strings in Loc.cs:
    python3 Tools/gen_locales.py
Crowdin takes Locales/en.json as the source file; the other languages are downloaded as Locales/<code>.json.
"""
import json, re, os

root = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
src = open(os.path.join(root, 'Loc.cs'), encoding='utf-8').read()
start = src.index('Table = new Dictionary<string, string[]>')
body = src[start:src.index('\n        };', start)]

lit = r'"((?:[^"\\]|\\.)*)"'
pat = re.compile(r'\{\s*' + lit + r'\s*,\s*new\[\]\s*\{\s*' + lit + r'\s*,\s*' + lit + r'\s*\}\s*\}')

def unescape(s):
    out, i = [], 0
    while i < len(s):
        c = s[i]
        if c == '\\':
            n = s[i + 1]
            if n == 'n': out.append('\n')
            elif n == 't': out.append('\t')
            elif n == '"': out.append('"')
            elif n == '\\': out.append('\\')
            elif n == 'u':
                out.append(chr(int(s[i + 2:i + 6], 16))); i += 4
            else: out.append(n)
            i += 2
        else:
            out.append(c); i += 1
    return ''.join(out)

en, ru = {}, {}
for m in pat.finditer(body):
    en[m.group(1)] = unescape(m.group(2))
    ru[m.group(1)] = unescape(m.group(3))

os.makedirs(os.path.join(root, 'Locales'), exist_ok=True)
for name, data in (('en', en), ('ru', ru)):
    with open(os.path.join(root, 'Locales', name + '.json'), 'w', encoding='utf-8', newline='\n') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')
print('keys:', len(en))
