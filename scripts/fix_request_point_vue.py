from pathlib import Path

stk = Path(r"d:\Junheinrich\Junheinrich.Master\Seven.Master\Seven.Vue3\src\views\Wcs\Stacker\StkRequestPoint.vue")
t = stk.read_text(encoding="utf-8")
t = t.replace("pointCode", "code")
stk.write_text(t, encoding="utf-8")

fw = Path(r"d:\Junheinrich\Junheinrich.Master\Seven.Master\Seven.Vue3\src\views\Wcs\FourWay\FwRequestPoint.vue")
t = fw.read_text(encoding="utf-8")
t = t.replace("pointCode", "code")
fw.write_text(t, encoding="utf-8")
print("fixed request point fields")
