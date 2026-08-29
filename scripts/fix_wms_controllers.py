from pathlib import Path
import re

p = Path(r"d:\Junheinrich\Junheinrich.Master\Seven.Master\Seven.Net8\Seven.WebApi\Controllers\Wms\WmsControllers.cs")
raw = p.read_bytes()
# try utf-8 then latin-1
try:
    text = raw.decode("utf-8")
except Exception:
    text = raw.decode("latin-1")

def fix_ok(m):
    return 'WebResponseContent.Ok("OK"'

def fix_err(m):
    inner = m.group(0)
    if "不存在" in inner or "not found" in inner.lower():
        return m.group(0)
    return 'WebResponseContent.Error("failed"'

# Fix broken Ok strings that contain mojibake (å) or control chars
text = re.sub(r'WebResponseContent\.Ok\("[^"]*"', lambda m: 'WebResponseContent.Ok("OK"' if ("å" in m.group(0) or any(ord(c) < 32 for c in m.group(0)[24:])) else m.group(0), text)
text = re.sub(r'WebResponseContent\.Error\("[^"]*"', lambda m: 'WebResponseContent.Error("not found"' if "å" in m.group(0) else m.group(0), text)

# Specific clean messages
repls = [
    ('WebResponseContent.Ok("OK"', 'WebResponseContent.Ok("操作成功"'),
    ('WebResponseContent.Error("not found"', 'WebResponseContent.Error("不存在"'),
]
for a, b in repls:
    text = text.replace(a, b)

# Fix buildPallet which already has good Chinese
# Ensure no broken newlines inside strings
lines = []
for line in text.splitlines(True):
    if 'WebResponseContent.Ok("' in line and line.count('"') % 2 != 0:
        # broken line - replace whole return
        indent = line[: len(line) - len(line.lstrip())]
        line = indent + 'return WebResponseContent.Ok("操作成功");\n'
    lines.append(line)

p.write_text("".join(lines), encoding="utf-8")
print("fixed")
