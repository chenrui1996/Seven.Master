from pathlib import Path
p = Path(r"d:\Junheinrich\Junheinrich.Master\Seven.Master\scripts\gen_wcs_crud_pages.py")
t = p.read_text(encoding="utf-8")
t = t.replace('"switch"', '"bool"').replace('"datetime"', '"date"')
p.write_text(t, encoding="utf-8")
print("patched kinds")
