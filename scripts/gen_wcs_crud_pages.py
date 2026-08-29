# -*- coding: utf-8 -*-
"""Generate CrudPanel Vue pages + empty extensions for WMS/WCS."""
from pathlib import Path

ROOT = Path(r"d:\Junheinrich\Junheinrich.Master\Seven.Master\Seven.Vue3\src")

VUE_TMPL = """<!-- 通用 CrudPanel：{table} -->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="{table}"
      i18n-key="generated.{table}"
      table-name="{table}"
      key-field="id"
      :columns="allColumns"
      :form-fields="formFields"
      :form-defaults="formDefaults"
      :enum-options-map="enumOptionsMap"
      :search-fields="mergedSearchFields"
      :fixed-filter="fixedFilter"
      :detail-tables="mergedDetailTables"
      :extension="pageExtension"
      :depth="0"
      :max-depth="3"
    />
    <component :is="pageExtension.overlay" v-if="pageExtension.overlay" />
  </div>
</template>

<script setup lang="ts">
import {{ computed, onMounted, reactive, watch }} from 'vue'
import {{ useRoute }} from 'vue-router'
import pageExtension from '{extImport}'
import type {{ DetailTableConfig, FormFieldDef, SearchFieldConfig }} from '{typesImport}'
import CrudPanel from '{crudImport}'
import {{ mergeDetailTables, mergeSearchFields }} from '{mergeImport}'
import type {{ ColumnDef }} from '{colImport}'

const route = useRoute()

const allColumns: ColumnDef[] = {columns}

const formFields: FormFieldDef[] = {formFields}

const formDefaults: Record<string, unknown> = {defaults}

const generatedSearchFields: SearchFieldConfig[] = {searchFields}

const generatedDetailTables: DetailTableConfig[] = []

const queryFilterKeys: string[] = []

const enumOptionsMap: Record<string, {{ value: number | string; label: string }}[]> = {{}}

const mergedSearchFields = computed(() =>
  mergeSearchFields(generatedSearchFields, pageExtension.searchFields),
)

const mergedDetailTables = computed(() =>
  mergeDetailTables(generatedDetailTables, pageExtension.detailTables),
)

const fixedFilter = reactive<Record<string, unknown>>({{}})

function applyQueryFilters() {{
  for (const key of Object.keys(fixedFilter)) delete fixedFilter[key]
  for (const key of queryFilterKeys) {{
    const raw = route.query[key]
    const val = Array.isArray(raw) ? raw[0] : raw
    if (val == null || val === '') continue
    const num = Number(val)
    fixedFilter[key] = Number.isFinite(num) && String(num) === String(val) ? num : val
  }}
}}

onMounted(applyQueryFilters)
watch(() => route.query, applyQueryFilters, {{ deep: true }})
</script>
"""

EXT_TMPL = """import type {{ PageExtension }} from '{typesRel}'

const extension: PageExtension = {{
  toolbarButtons: [],
  rowButtons: [],
}}

export default extension
"""


def col(props):
    return "[\n" + ",\n".join(
        f"  {{ prop: '{p}', kind: '{k}', sortable: false }}" for p, k in props
    ) + "\n]"


def form(props):
    return "[\n" + ",\n".join(
        f"  {{ prop: '{p}', kind: '{k}' }}" for p, k in props
    ) + "\n]"


def defaults(d):
    parts = []
    for k, v in d.items():
        if isinstance(v, str):
            parts.append(f"  {k}: '{v}'")
        elif isinstance(v, bool):
            parts.append(f"  {k}: {'true' if v else 'false'}")
        else:
            parts.append(f"  {k}: {v}")
    return "{\n" + ",\n".join(parts) + ",\n}"


def search(props):
    if not props:
        return "[]"
    return "[\n" + ",\n".join(
        f"  {{ prop: '{p}', kind: 'string', operator: 'like' }}" for p in props
    ) + "\n]"


def emit(rel_view: str, rel_ext: str, table: str, columns, forms, defs, searches, depth_view=2):
    # imports relative to view file
    ups = "../" * depth_view
    vue = VUE_TMPL.format(
        table=table,
        extImport=rel_ext,
        typesImport=f"{ups}extension/types",
        crudImport=f"{ups}components/crud/CrudPanel.vue",
        mergeImport=f"{ups}components/crud/mergeExtension",
        colImport=f"{ups}composables/useTableColumns",
        columns=col(columns),
        formFields=form(forms),
        defaults=defaults(defs),
        searchFields=search(searches),
    )
    view_path = ROOT / "views" / rel_view
    view_path.parent.mkdir(parents=True, exist_ok=True)
    view_path.write_text(vue, encoding="utf-8")

    # extension path from views: ../../extension/...
    # rel_ext like ../../extension/Wms/WmsWarehouse
    ext_file = ROOT / "extension" / rel_ext.split("extension/")[-1]
    if not ext_file.suffix:
        ext_file = Path(str(ext_file) + ".ts")
    ext_file.parent.mkdir(parents=True, exist_ok=True)
    # types relative from extension folder
    depth_ext = len(ext_file.relative_to(ROOT / "extension").parts) - 1
    types_rel = "../" * depth_ext + "types" if depth_ext else "./types"
    # fix: from Wms/foo.ts -> ../types; from Wcs/Stacker/foo -> ../../types
    parts = ext_file.relative_to(ROOT / "extension").parts
    types_rel = "../" * (len(parts) - 1) + "types"
    ext_file.write_text(EXT_TMPL.format(typesRel=types_rel), encoding="utf-8")
    print("wrote", view_path.relative_to(ROOT), ext_file.relative_to(ROOT))


# --- definitions ---
pages = []

def add(folder, table, columns, forms, defs, searches, sub=None, depth=2):
    if folder.startswith("Wcs/"):
        depth = 3
        ext = f"../../../extension/{folder}/{table}"
    else:
        depth = 2
        ext = f"../../extension/{folder}/{table}"
    pages.append((f"{folder}/{table}.vue", ext, table, columns, forms, defs, searches, depth))


# WMS master
add("Wms", "WmsWarehouse",
    [("id","number"),("code","string"),("name","string"),("enabledPackIds","string"),("isCycleCountLocked","bool")],
    [("code","string"),("name","string"),("enabledPackIds","string"),("isCycleCountLocked","bool")],
    {"id":0,"code":"","name":"","enabledPackIds":"stacker","isCycleCountLocked":False},
    ["code","name"])

add("Wms", "WmsZone",
    [("id","number"),("warehouseId","number"),("packId","string"),("code","string"),("name","string")],
    [("warehouseId","number"),("packId","string"),("code","string"),("name","string")],
    {"id":0,"warehouseId":0,"packId":"stacker","code":"","name":""},
    ["code","packId"])

add("Wms", "WmsLayer",
    [("id","number"),("warehouseId","number"),("zoneId","number"),("packId","string"),("code","string"),("name","string"),("isAvailable","bool"),("allocationWeight","number")],
    [("warehouseId","number"),("zoneId","number"),("packId","string"),("code","string"),("name","string"),("isAvailable","bool"),("allocationWeight","number")],
    {"id":0,"warehouseId":0,"zoneId":0,"packId":"fourway","code":"","name":"","isAvailable":True,"allocationWeight":1},
    ["code","packId"])

add("Wms", "WmsAisle",
    [("id","number"),("warehouseId","number"),("zoneId","number"),("layerId","number"),("packId","string"),("code","string"),("name","string"),("epPointCode","string"),("isAvailable","bool")],
    [("warehouseId","number"),("zoneId","number"),("layerId","number"),("packId","string"),("code","string"),("name","string"),("epPointCode","string"),("isAvailable","bool")],
    {"id":0,"warehouseId":0,"zoneId":0,"packId":"stacker","code":"","name":"","isAvailable":True},
    ["code","packId"])

add("Wms", "WmsLocation",
    [("id","number"),("code","string"),("warehouseId","number"),("packId","string"),("aisle","string"),("row","string"),("column","string"),("layer","string"),("depth","string"),("isOccupied","bool"),("isBooked","bool"),("isLocked","bool"),("currentContainerCode","string")],
    [("code","string"),("warehouseId","number"),("zoneId","number"),("layerId","number"),("aisleId","number"),("packId","string"),("aisle","string"),("row","string"),("column","string"),("layer","string"),("depth","string"),("isHandover","bool"),("isLocked","bool")],
    {"id":0,"code":"","warehouseId":0,"packId":"stacker","isOccupied":False,"isBooked":False,"isLocked":False,"isHandover":False},
    ["code","packId","aisle"])

add("Wms", "WmsContainerType",
    [("id","number"),("code","string"),("name","string")],
    [("code","string"),("name","string")],
    {"id":0,"code":"","name":""},
    ["code","name"])

add("Wms", "WmsContainer",
    [("id","number"),("code","string"),("locationCode","string"),("status","number")],
    [("code","string"),("locationCode","string"),("containerTypeId","number"),("status","number")],
    {"id":0,"code":"","locationCode":"","status":0},
    ["code","locationCode"])

add("Wms", "WmsHandoverLink",
    [("id","number"),("fromPackId","string"),("toPackId","string"),("locationCode","string")],
    [("fromPackId","string"),("toPackId","string"),("locationCode","string")],
    {"id":0,"fromPackId":"stacker","toPackId":"fourway","locationCode":""},
    ["locationCode"])

add("Wms", "WmsStock",
    [("id","number"),("locationCode","string"),("materialCode","string"),("containerCode","string"),("lot","string"),("qty","number"),("availableQty","number")],
    [("locationCode","string"),("materialCode","string"),("containerCode","string"),("lot","string"),("qty","number"),("availableQty","number")],
    {"id":0,"locationCode":"","materialCode":"","qty":0,"availableQty":0},
    ["locationCode","materialCode","containerCode"])

add("Wms", "WmsStockLedger",
    [("id","number"),("stockId","number"),("changeQty","number"),("reason","string"),("refType","string"),("refId","string"),("createDate","date")],
    [("stockId","number"),("changeQty","number"),("reason","string")],
    {"id":0,"stockId":0,"changeQty":0,"reason":""},
    ["refType","refId"])

# Stacker
for table, cols, forms, defs, sch in [
    ("StkRequestPoint",
     [("id","number"),("pointCode","string"),("pointType","string"),("aisleCode","string"),("isEnabled","bool")],
     [("pointCode","string"),("pointType","string"),("aisleCode","string"),("isEnabled","bool")],
     {"id":0,"pointCode":"","pointType":"AisleRequest","isEnabled":True},
     ["pointCode","aisleCode"]),
    ("StkAssignmentPolicy",
     [("id","number"),("aisleCode","string"),("isAvailable","bool"),("allocationWeight","number"),("destinationPointCode","string"),("minEmptySlots","number")],
     [("aisleCode","string"),("isAvailable","bool"),("allocationWeight","number"),("destinationPointCode","string"),("minEmptySlots","number")],
     {"id":0,"aisleCode":"","isAvailable":True,"allocationWeight":1},
     ["aisleCode"]),
    ("StkLocationProfile",
     [("id","number"),("locationCode","string"),("binGroupCode","string"),("inLockBin","bool")],
     [("locationCode","string"),("binGroupCode","string"),("inLockBin","bool")],
     {"id":0,"locationCode":"","binGroupCode":"","inLockBin":False},
     ["locationCode","binGroupCode"]),
    ("StkRoute",
     [("id","number"),("fromPoint","string"),("toPoint","string"),("exeStackCode","string"),("capacity","number")],
     [("fromPoint","string"),("toPoint","string"),("exeStackCode","string"),("capacity","number")],
     {"id":0,"fromPoint":"","toPoint":"","exeStackCode":"","capacity":1},
     ["fromPoint","toPoint"]),
    ("StkDeviceCoder",
     [("id","number"),("pointCode","string"),("deviceCode","string")],
     [("pointCode","string"),("deviceCode","string")],
     {"id":0,"pointCode":"","deviceCode":""},
     ["pointCode","deviceCode"]),
    ("StkPutAwayTask",
     [("id","string"),("legId","string"),("containerCode","string"),("fromCode","string"),("toCode","string"),("status","number"),("assignedAisle","string"),("assignedLocationCode","string")],
     [("containerCode","string"),("fromCode","string"),("toCode","string"),("status","number")],
     {"id":"","status":0},
     ["containerCode","status"]),
    ("StkRetrievalTask",
     [("id","string"),("legId","string"),("containerCode","string"),("fromCode","string"),("toCode","string"),("status","number"),("wcsGroupNo","string"),("wcsPri","number")],
     [("containerCode","string"),("status","number"),("wcsGroupNo","string"),("wcsPri","number")],
     {"id":"","status":0,"wcsPri":0},
     ["containerCode","wcsGroupNo"]),
    ("StkDeviceTask",
     [("id","string"),("seq","number"),("exeStackCode","string"),("fromPoint","string"),("destPoint","string"),("status","number")],
     [("seq","number"),("exeStackCode","string"),("fromPoint","string"),("destPoint","string"),("status","number")],
     {"id":"","seq":0,"status":0},
     ["exeStackCode","status"]),
]:
    add("Wcs/Stacker", table, cols, forms, defs, sch)

# FourWay
for table, cols, forms, defs, sch in [
    ("FwLayerPolicy",
     [("id","number"),("warehouseCode","string"),("zoneCode","string"),("layerCode","string"),("isAvailable","bool"),("allocationWeight","number"),("maxHeight","number")],
     [("warehouseCode","string"),("zoneCode","string"),("layerCode","string"),("isAvailable","bool"),("allocationWeight","number"),("maxHeight","number"),("maxWeight","number")],
     {"id":0,"warehouseCode":"","zoneCode":"","layerCode":"","isAvailable":True,"allocationWeight":1,"maxHeight":9999,"maxWeight":99999},
     ["layerCode","warehouseCode"]),
    ("FwAislePolicy",
     [("id","number"),("layerCode","string"),("aisleCode","string"),("isAvailable","bool"),("allocationWeight","number"),("destinationPointCode","string"),("minEmptySlots","number"),("maxShuttleCount","number")],
     [("layerCode","string"),("aisleCode","string"),("isAvailable","bool"),("allocationWeight","number"),("destinationPointCode","string"),("minEmptySlots","number"),("maxShuttleCount","number")],
     {"id":0,"layerCode":"","aisleCode":"","isAvailable":True,"allocationWeight":1},
     ["layerCode","aisleCode"]),
    ("FwRequestPoint",
     [("id","number"),("pointCode","string"),("pointType","string"),("layerCode","string"),("isEnabled","bool")],
     [("pointCode","string"),("pointType","string"),("layerCode","string"),("isEnabled","bool")],
     {"id":0,"pointCode":"","pointType":"LayerRequest","isEnabled":True},
     ["pointCode","layerCode"]),
    ("FwMapVersion",
     [("id","number"),("layerCode","string"),("versionName","string"),("isActive","bool")],
     [("layerCode","string"),("versionName","string"),("isActive","bool")],
     {"id":0,"layerCode":"","versionName":"","isActive":True},
     ["layerCode","versionName"]),
    ("FwNode",
     [("id","number"),("mapVersionId","number"),("nodeCode","string"),("nodeType","string")],
     [("mapVersionId","number"),("nodeCode","string"),("nodeType","string")],
     {"id":0,"mapVersionId":0,"nodeCode":"","nodeType":""},
     ["nodeCode"]),
    ("FwRoute",
     [("id","number"),("mapVersionId","number"),("fromNode","string"),("toNode","string"),("capacity","number")],
     [("mapVersionId","number"),("fromNode","string"),("toNode","string"),("capacity","number")],
     {"id":0,"mapVersionId":0,"fromNode":"","toNode":"","capacity":1},
     ["fromNode","toNode"]),
    ("FwParkingLedger",
     [("id","number"),("parkingCode","string"),("layerCode","string"),("aisleCode","string"),("status","string")],
     [("parkingCode","string"),("layerCode","string"),("aisleCode","string"),("status","string")],
     {"id":0,"parkingCode":"","status":"Free"},
     ["parkingCode","status"]),
    ("FwHoistDevice",
     [("id","number"),("deviceCode","string"),("isAvailable","bool")],
     [("deviceCode","string"),("isAvailable","bool")],
     {"id":0,"deviceCode":"","isAvailable":True},
     ["deviceCode"]),
    ("FwHoistLayerPoint",
     [("id","number"),("hoistDeviceId","number"),("layerCode","string"),("apPointCode","string"),("epPointCode","string")],
     [("hoistDeviceId","number"),("layerCode","string"),("apPointCode","string"),("epPointCode","string")],
     {"id":0,"hoistDeviceId":0,"layerCode":"","apPointCode":"","epPointCode":""},
     ["layerCode"]),
    ("FwPutAwayTask",
     [("id","string"),("containerCode","string"),("fromCode","string"),("toCode","string"),("status","number"),("assignedLayer","string"),("assignedAisle","string")],
     [("containerCode","string"),("status","number")],
     {"id":"","status":0},
     ["containerCode","status"]),
    ("FwRetrievalTask",
     [("id","string"),("containerCode","string"),("fromCode","string"),("toCode","string"),("status","number"),("wcsGroupNo","string"),("wcsPri","number")],
     [("containerCode","string"),("status","number"),("wcsGroupNo","string"),("wcsPri","number")],
     {"id":"","status":0,"wcsPri":0},
     ["containerCode","wcsGroupNo"]),
    ("FwShuttleTask",
     [("id","string"),("containerCode","string"),("status","number")],
     [("containerCode","string"),("status","number")],
     {"id":"","status":0},
     ["containerCode","status"]),
    ("FwHoistTask",
     [("id","string"),("containerCode","string"),("status","number")],
     [("containerCode","string"),("status","number")],
     {"id":"","status":0},
     ["containerCode","status"]),
    ("FwHoistExecTask",
     [("id","string"),("hoistTaskId","string"),("status","number")],
     [("status","number")],
     {"id":"","status":0},
     ["status"]),
]:
    add("Wcs/FourWay", table, cols, forms, defs, sch)

for args in pages:
    emit(*args)

print(f"generated {len(pages)} pages")
