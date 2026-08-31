/**
 * 补齐 WMS/WCS CrudPanel generated.*、menu.*，以及运营页 wmsOps/wcsOps/platform 键。
 * 以 zh-CN 为源，同步写入 en-US / ja-JP（手工词典，不依赖外部 API）。
 *
 * 用法: node scripts/gen-wms-wcs-i18n.mjs
 */
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const LANG_DIR = path.resolve(__dirname, '../src/locales/lang')
const VIEWS = path.resolve(__dirname, '../src/views')

const COMMON = {
  actions: { zh: '操作', en: 'Actions', ja: '操作' },
  edit: { zh: '编辑', en: 'Edit', ja: '編集' },
  add: { zh: '新增', en: 'Add', ja: '追加' },
  delete: { zh: '删除', en: 'Delete', ja: '削除' },
  deleteConfirm: {
    zh: '确定删除该记录吗？',
    en: 'Delete this record?',
    ja: 'このレコードを削除しますか？',
  },
}

/** 实体中文标题（listTitle） */
const ENTITY_TITLE = {
  WmsWarehouse: { zh: '仓库', en: 'Warehouse', ja: '倉庫' },
  WmsZone: { zh: '库区', en: 'Zone', ja: 'ゾーン' },
  WmsLayer: { zh: '层', en: 'Layer', ja: '層' },
  WmsAisle: { zh: '巷道', en: 'Aisle', ja: '通路' },
  WmsLocation: { zh: '库位', en: 'Location', ja: 'ロケーション' },
  WmsContainerType: { zh: '容器类型', en: 'Container Type', ja: '容器タイプ' },
  WmsContainer: { zh: '容器', en: 'Container', ja: '容器' },
  WmsHandoverLink: { zh: '交接位', en: 'Handover Link', ja: 'ハンドオーバー' },
  WmsStock: { zh: '库存', en: 'Stock', ja: '在庫' },
  WmsStockLedger: { zh: '库存流水', en: 'Stock Ledger', ja: '在庫台帳' },
  StkRequestPoint: { zh: '申请点', en: 'Request Point', ja: '申請点' },
  StkAssignmentPolicy: { zh: '巷道策略', en: 'Assignment Policy', ja: '割当ポリシー' },
  StkLocationProfile: { zh: '双深配置', en: 'Location Profile', ja: 'ロケーション設定' },
  StkRoute: { zh: '路网', en: 'Route', ja: 'ルート' },
  StkDeviceCoder: { zh: '点码映射', en: 'Device Coder', ja: '点コード' },
  StkPutAwayTask: { zh: '上架任务', en: 'Put-away Task', ja: '入庫タスク' },
  StkRetrievalTask: { zh: '取货任务', en: 'Retrieval Task', ja: '出庫タスク' },
  StkDeviceTask: { zh: '设备段任务', en: 'Device Segment Task', ja: '設備セグメント' },
  FwLayerPolicy: { zh: '层策略', en: 'Layer Policy', ja: '層ポリシー' },
  FwAislePolicy: { zh: '巷策略', en: 'Aisle Policy', ja: '通路ポリシー' },
  FwRequestPoint: { zh: '申请点', en: 'Request Point', ja: '申請点' },
  FwMapVersion: { zh: '地图版本', en: 'Map Version', ja: '地図バージョン' },
  FwNode: { zh: '节点', en: 'Node', ja: 'ノード' },
  FwRoute: { zh: '路网边', en: 'Route Edge', ja: 'ルート辺' },
  FwParkingLedger: { zh: '停车账本', en: 'Parking Ledger', ja: '駐車台帳' },
  FwHoistDevice: { zh: '提升机', en: 'Hoist Device', ja: 'ホイスト' },
  FwHoistLayerPoint: { zh: '提升机层口', en: 'Hoist Layer Point', ja: 'ホイスト層口' },
  FwPutAwayTask: { zh: '上架任务', en: 'Put-away Task', ja: '入庫タスク' },
  FwRetrievalTask: { zh: '取货任务', en: 'Retrieval Task', ja: '出庫タスク' },
  FwShuttleTask: { zh: '穿梭任务', en: 'Shuttle Task', ja: 'シャトルタスク' },
  FwHoistTask: { zh: '提升任务', en: 'Hoist Task', ja: 'ホイストタスク' },
  FwHoistExecTask: { zh: '提升执行段', en: 'Hoist Exec Task', ja: 'ホイスト実行段' },
}

/** 字段标签词典（未命中则用 camelCase 拆分） */
const FIELD = {
  id: { zh: 'Id', en: 'Id', ja: 'Id' },
  code: { zh: '编码', en: 'Code', ja: 'コード' },
  name: { zh: '名称', en: 'Name', ja: '名称' },
  enabledPackIds: { zh: '启用 Pack', en: 'Enabled Packs', ja: '有効 Pack' },
  isCycleCountLocked: { zh: '盘点锁定', en: 'Cycle Count Locked', ja: '棚卸ロック' },
  warehouseId: { zh: '仓库Id', en: 'Warehouse Id', ja: '倉庫Id' },
  zoneId: { zh: '库区Id', en: 'Zone Id', ja: 'ゾーンId' },
  layerId: { zh: '层Id', en: 'Layer Id', ja: '層Id' },
  aisleId: { zh: '巷道Id', en: 'Aisle Id', ja: '通路Id' },
  packId: { zh: 'PackId', en: 'Pack Id', ja: 'PackId' },
  packPrefix: { zh: 'Pack 前缀', en: 'Pack Prefix', ja: 'Pack接頭辞' },
  length: { zh: '长', en: 'Length', ja: '長さ' },
  width: { zh: '宽', en: 'Width', ja: '幅' },
  height: { zh: '高', en: 'Height', ja: '高さ' },
  maxWeight: { zh: '最大重量', en: 'Max Weight', ja: '最大重量' },
  status: { zh: '状态', en: 'Status', ja: '状態' },
  locationCode: { zh: '库位', en: 'Location', ja: 'ロケーション' },
  materialCode: { zh: '物料', en: 'Material', ja: '品目' },
  qty: { zh: '数量', en: 'Qty', ja: '数量' },
  containerCode: { zh: '容器', en: 'Container', ja: '容器' },
  containerTypeId: { zh: '容器类型Id', en: 'Container Type Id', ja: '容器タイプId' },
  containerTypeCode: { zh: '容器类型', en: 'Container Type', ja: '容器タイプ' },
  fromLocationCode: { zh: '起点库位', en: 'From Location', ja: '起点' },
  toLocationCode: { zh: '终点库位', en: 'To Location', ja: '終点' },
  fromCode: { zh: '起点', en: 'From', ja: '起点' },
  toCode: { zh: '终点', en: 'To', ja: '終点' },
  fromPoint: { zh: '起点', en: 'From Point', ja: '起点' },
  toPoint: { zh: '终点', en: 'To Point', ja: '終点' },
  pointCode: { zh: '点位', en: 'Point Code', ja: 'ポイント' },
  deviceCode: { zh: '设备编码', en: 'Device Code', ja: '設備コード' },
  deviceId: { zh: '设备Id', en: 'Device Id', ja: '設備Id' },
  taskNo: { zh: '任务号', en: 'Task No', ja: 'タスク番号' },
  busOrderId: { zh: '运输单Id', en: 'Bus Order Id', ja: '輸送単Id' },
  busLegId: { zh: 'Leg Id', en: 'Bus Leg Id', ja: 'Leg Id' },
  priority: { zh: '优先级', en: 'Priority', ja: '優先度' },
  remark: { zh: '备注', en: 'Remark', ja: '備考' },
  enabled: { zh: '启用', en: 'Enabled', ja: '有効' },
  version: { zh: '版本', en: 'Version', ja: 'バージョン' },
  nodeCode: { zh: '节点编码', en: 'Node Code', ja: 'ノードコード' },
  nodeType: { zh: '节点类型', en: 'Node Type', ja: 'ノード種別' },
  x: { zh: 'X', en: 'X', ja: 'X' },
  y: { zh: 'Y', en: 'Y', ja: 'Y' },
  layerNo: { zh: '层号', en: 'Layer No', ja: '層番号' },
  aisleNo: { zh: '巷道号', en: 'Aisle No', ja: '通路番号' },
  rowNo: { zh: '排', en: 'Row', ja: '列' },
  bayNo: { zh: '列', en: 'Bay', ja: 'ベイ' },
  depth: { zh: '深度', en: 'Depth', ja: '深さ' },
  isLocked: { zh: '锁定', en: 'Locked', ja: 'ロック' },
  isEmpty: { zh: '空闲', en: 'Empty', ja: '空' },
  occupancy: { zh: '占用', en: 'Occupancy', ja: '占有' },
  changeType: { zh: '变动类型', en: 'Change Type', ja: '変動種別' },
  beforeQty: { zh: '变动前', en: 'Before Qty', ja: '変動前' },
  afterQty: { zh: '变动后', en: 'After Qty', ja: '変動後' },
  refType: { zh: '来源类型', en: 'Ref Type', ja: '参照種別' },
  refNo: { zh: '来源单号', en: 'Ref No', ja: '参照番号' },
  createDate: { zh: '创建时间', en: 'Created At', ja: '作成日時' },
  modifyDate: { zh: '修改时间', en: 'Modified At', ja: '更新日時' },
  creator: { zh: '创建人', en: 'Creator', ja: '作成者' },
  modifier: { zh: '修改人', en: 'Modifier', ja: '更新者' },
  message: { zh: '消息', en: 'Message', ja: 'メッセージ' },
  failReason: { zh: '失败原因', en: 'Fail Reason', ja: '失敗理由' },
  seq: { zh: '序号', en: 'Seq', ja: '順序' },
  weight: { zh: '重量', en: 'Weight', ja: '重量' },
  policyJson: { zh: '策略 JSON', en: 'Policy JSON', ja: 'ポリシーJSON' },
  mapVersionId: { zh: '地图版本Id', en: 'Map Version Id', ja: '地図Id' },
  hoistCode: { zh: '提升机编码', en: 'Hoist Code', ja: 'ホイストコード' },
  parkingCode: { zh: '停车位', en: 'Parking Code', ja: '駐車コード' },
  shuttleCode: { zh: '穿梭车', en: 'Shuttle Code', ja: 'シャトル' },
  coderValue: { zh: '点码值', en: 'Coder Value', ja: 'コード値' },
  doubleDeep: { zh: '双深', en: 'Double Deep', ja: 'ダブルディープ' },
  frontLocationCode: { zh: '前位', en: 'Front Location', ja: '前ロケ' },
  rearLocationCode: { zh: '后位', en: 'Rear Location', ja: '後ロケ' },
  requestPointCode: { zh: '申请点', en: 'Request Point', ja: '申請点' },
  sourcePointCode: { zh: '源点位', en: 'Source Point', ja: 'ソース点' },
  targetPointCode: { zh: '目标点', en: 'Target Point', ja: '目標点' },
  segmentPointCode: { zh: '段点位', en: 'Segment Point', ja: 'セグメント点' },
  feedbackCode: { zh: '反馈码', en: 'Feedback', ja: 'フィードバック' },
  checkResult: { zh: '校验结果', en: 'Check Result', ja: 'チェック結果' },
  isActive: { zh: '激活', en: 'Active', ja: '有効' },
  sortOrder: { zh: '排序', en: 'Sort', ja: '並び' },
  description: { zh: '说明', en: 'Description', ja: '説明' },
  uom: { zh: '单位', en: 'UoM', ja: '単位' },
  batchNo: { zh: '批次', en: 'Batch', ja: 'ロット' },
  availableQty: { zh: '可用数量', en: 'Available Qty', ja: '利用可能数' },
  lockedQty: { zh: '锁定数量', en: 'Locked Qty', ja: 'ロック数' },
  linkType: { zh: '交接类型', en: 'Link Type', ja: 'リンク種別' },
  leftLocationCode: { zh: '左侧库位', en: 'Left Location', ja: '左ロケ' },
  rightLocationCode: { zh: '右侧库位', en: 'Right Location', ja: '右ロケ' },
  warehouseCode: { zh: '仓库编码', en: 'Warehouse Code', ja: '倉庫コード' },
  zoneCode: { zh: '库区编码', en: 'Zone Code', ja: 'ゾーンコード' },
  layerCode: { zh: '层编码', en: 'Layer Code', ja: '層コード' },
  aisleCode: { zh: '巷道编码', en: 'Aisle Code', ja: '通路コード' },
  capacity: { zh: '容量', en: 'Capacity', ja: '容量' },
  locationType: { zh: '库位类型', en: 'Location Type', ja: 'ロケ種別' },
  isOutboundBlocked: { zh: '禁止出库', en: 'Outbound Blocked', ja: '出庫禁止' },
  isInboundBlocked: { zh: '禁止入库', en: 'Inbound Blocked', ja: '入庫禁止' },
  stockId: { zh: '库存Id', en: 'Stock Id', ja: '在庫Id' },
  taskId: { zh: '任务Id', en: 'Task Id', ja: 'タスクId' },
  parentTaskId: { zh: '父任务Id', en: 'Parent Task Id', ja: '親タスクId' },
  state: { zh: '状态', en: 'State', ja: '状態' },
  startedAt: { zh: '开始时间', en: 'Started At', ja: '開始日時' },
  finishedAt: { zh: '完成时间', en: 'Finished At', ja: '完了日時' },
  errorCode: { zh: '错误码', en: 'Error Code', ja: 'エラーコード' },
  errorMessage: { zh: '错误信息', en: 'Error Message', ja: 'エラーメッセージ' },
  payloadJson: { zh: '载荷 JSON', en: 'Payload JSON', ja: 'ペイロード' },
  direction: { zh: '方向', en: 'Direction', ja: '方向' },
  cost: { zh: '代价', en: 'Cost', ja: 'コスト' },
  blocked: { zh: '封锁', en: 'Blocked', ja: '封鎖' },
  mapCode: { zh: '地图编码', en: 'Map Code', ja: '地図コード' },
  isCurrent: { zh: '当前版本', en: 'Is Current', ja: '現行' },
  publishedAt: { zh: '发布时间', en: 'Published At', ja: '公開日時' },
  layerPointCode: { zh: '层口编码', en: 'Layer Point', ja: '層口コード' },
  hoistDeviceId: { zh: '提升机Id', en: 'Hoist Device Id', ja: 'ホイストId' },
  parkingLedgerId: { zh: '停车账本Id', en: 'Parking Ledger Id', ja: '駐車台帳Id' },
  occupiedBy: { zh: '占用方', en: 'Occupied By', ja: '占有者' },
  reservedBy: { zh: '预留方', en: 'Reserved By', ja: '予約者' },
  assignmentMode: { zh: '分配模式', en: 'Assignment Mode', ja: '割当モード' },
  maxConcurrent: { zh: '最大并发', en: 'Max Concurrent', ja: '最大同時' },
  allowInbound: { zh: '允许入库', en: 'Allow Inbound', ja: '入庫許可' },
  allowOutbound: { zh: '允许出库', en: 'Allow Outbound', ja: '出庫許可' },
  pointType: { zh: '点位类型', en: 'Point Type', ja: 'ポイント種別' },
  mappedLocationCode: { zh: '映射库位', en: 'Mapped Location', ja: '対応ロケ' },
  plcAddress: { zh: 'PLC 地址', en: 'PLC Address', ja: 'PLCアドレス' },
  stackerCode: { zh: '堆垛机', en: 'Stacker Code', ja: 'スタッカー' },
  routeCode: { zh: '路径编码', en: 'Route Code', ja: 'ルートコード' },
  fromNodeCode: { zh: '起点节点', en: 'From Node', ja: '起点ノード' },
  toNodeCode: { zh: '终点节点', en: 'To Node', ja: '終点ノード' },
  distance: { zh: '距离', en: 'Distance', ja: '距離' },
  biDirectional: { zh: '双向', en: 'Bi-directional', ja: '双方向' },
  execSeq: { zh: '执行序号', en: 'Exec Seq', ja: '実行順序' },
  hoistTaskId: { zh: '提升任务Id', en: 'Hoist Task Id', ja: 'ホイストタスクId' },
  shuttleTaskId: { zh: '穿梭任务Id', en: 'Shuttle Task Id', ja: 'シャトルタスクId' },
  putAwayTaskId: { zh: '上架任务Id', en: 'Put-away Task Id', ja: '入庫タスクId' },
  retrievalTaskId: { zh: '取货任务Id', en: 'Retrieval Task Id', ja: '出庫タスクId' },
  deviceTaskId: { zh: '设备任务Id', en: 'Device Task Id', ja: '設備タスクId' },
  targetLocationCode: { zh: '目标库位', en: 'Target Location', ja: '目標ロケ' },
  sourceLocationCode: { zh: '源库位', en: 'Source Location', ja: 'ソースロケ' },
  currentLocationCode: { zh: '当前位置', en: 'Current Location', ja: '現在ロケ' },
  isDeleted: { zh: '已删除', en: 'Deleted', ja: '削除済' },
}

const MENUS = {
  仓储WMS: { zh: '仓储WMS', en: 'WMS', ja: '倉庫WMS' },
  立库WCS: { zh: '立库WCS', en: 'AS/RS WCS', ja: '立体庫WCS' },
  四向车WCS: { zh: '四向车WCS', en: 'Four-Way WCS', ja: '四方向WCS' },
  执行运维: { zh: '执行运维', en: 'Ops & Platform', ja: '実行・運用' },
  仓库: { zh: '仓库', en: 'Warehouse', ja: '倉庫' },
  库区: { zh: '库区', en: 'Zone', ja: 'ゾーン' },
  层: { zh: '层', en: 'Layer', ja: '層' },
  巷道: { zh: '巷道', en: 'Aisle', ja: '通路' },
  库位: { zh: '库位', en: 'Location', ja: 'ロケーション' },
  容器类型: { zh: '容器类型', en: 'Container Type', ja: '容器タイプ' },
  容器: { zh: '容器', en: 'Container', ja: '容器' },
  交接位: { zh: '交接位', en: 'Handover', ja: 'ハンドオーバー' },
  库存: { zh: '库存', en: 'Stock', ja: '在庫' },
  库存流水: { zh: '库存流水', en: 'Stock Ledger', ja: '在庫台帳' },
  入库单: { zh: '入库单', en: 'Inbound Order', ja: '入庫伝票' },
  出库单: { zh: '出库单', en: 'Outbound Order', ja: '出庫伝票' },
  盘点单: { zh: '盘点单', en: 'Cycle Count', ja: '棚卸伝票' },
  堆垛仿真触发: { zh: '堆垛仿真触发', en: 'Stacker Trigger', ja: 'スタッカーシミュ' },
  运输单监控: { zh: '运输单监控', en: 'Transport Monitor', ja: '輸送モニタ' },
  申请点: { zh: '申请点', en: 'Request Point', ja: '申請点' },
  巷道策略: { zh: '巷道策略', en: 'Aisle Policy', ja: '通路ポリシー' },
  双深配置: { zh: '双深配置', en: 'Double-Deep Profile', ja: 'ダブルディープ' },
  路网: { zh: '路网', en: 'Route Network', ja: 'ルート網' },
  点码映射: { zh: '点码映射', en: 'Point Coder', ja: '点コード' },
  上架任务: { zh: '上架任务', en: 'Put-away Tasks', ja: '入庫タスク' },
  取货任务: { zh: '取货任务', en: 'Retrieval Tasks', ja: '出庫タスク' },
  设备段任务: { zh: '设备段任务', en: 'Device Segment Tasks', ja: '設備セグメント' },
  四向仿真触发: { zh: '四向仿真触发', en: 'Four-Way Trigger', ja: '四方向シミュ' },
  层策略: { zh: '层策略', en: 'Layer Policy', ja: '層ポリシー' },
  巷策略: { zh: '巷策略', en: 'Aisle Policy', ja: '通路ポリシー' },
  地图版本: { zh: '地图版本', en: 'Map Version', ja: '地図バージョン' },
  节点: { zh: '节点', en: 'Nodes', ja: 'ノード' },
  路网边: { zh: '路网边', en: 'Route Edges', ja: 'ルート辺' },
  停车账本: { zh: '停车账本', en: 'Parking Ledger', ja: '駐車台帳' },
  提升机: { zh: '提升机', en: 'Hoist', ja: 'ホイスト' },
  提升机层口: { zh: '提升机层口', en: 'Hoist Layer Points', ja: 'ホイスト層口' },
  穿梭任务: { zh: '穿梭任务', en: 'Shuttle Tasks', ja: 'シャトルタスク' },
  提升任务: { zh: '提升任务', en: 'Hoist Tasks', ja: 'ホイストタスク' },
  提升执行段: { zh: '提升执行段', en: 'Hoist Exec Segments', ja: 'ホイスト実行段' },
  '运行模式/联锁': { zh: '运行模式/联锁', en: 'Control Mode', ja: '運転モード' },
  接口日志: { zh: '接口日志', en: 'Interface Logs', ja: 'IFログ' },
  '2D看板': { zh: '2D看板', en: '2D Board', ja: '2Dボード' },
  子设备: { zh: '子设备', en: 'Sub Devices', ja: '子設備' },
  工作流定义: { zh: '工作流定义', en: 'Workflow Def', ja: 'ワークフロー定義' },
}

const PAGE_KEYS = {
  wmsOps: {
    refresh: { zh: '刷新', en: 'Refresh', ja: '更新' },
    actions: { zh: '操作', en: 'Actions', ja: '操作' },
    cancel: { zh: '取消', en: 'Cancel', ja: 'キャンセル' },
    create: { zh: '创建', en: 'Create', ja: '作成' },
    save: { zh: '保存', en: 'Save', ja: '保存' },
    failed: { zh: '失败', en: 'Failed', ja: '失敗' },
    createSuccess: { zh: '创建成功', en: 'Created', ja: '作成しました' },
    approveSuccess: { zh: '审核成功', en: 'Approved', ja: '承認しました' },
    orderNo: { zh: '单号', en: 'Order No', ja: '伝票番号' },
    type: { zh: '类型', en: 'Type', ja: '種別' },
    status: { zh: '状态', en: 'Status', ja: '状態' },
    createDate: { zh: '创建时间', en: 'Created At', ja: '作成日時' },
    material: { zh: '物料', en: 'Material', ja: '品目' },
    qty: { zh: '数量', en: 'Qty', ja: '数量' },
    container: { zh: '容器', en: 'Container', ja: '容器' },
    line: { zh: '行', en: 'Line', ja: '行' },
    approve: { zh: '审核', en: 'Approve', ja: '承認' },
    inbound: {
      title: { zh: '入库单', en: 'Inbound Orders', ja: '入庫伝票' },
      subtitle: {
        zh: '建单 · 审核 · 组盘（收货位/目标/容器）· 收货兼容入口',
        en: 'Create · Approve · Build pallet · Receive',
        ja: '作成・承認・組盤・入荷',
      },
      create: { zh: '新建入库单', en: 'New Inbound', ja: '入庫伝票作成' },
      pallet: { zh: '组盘', en: 'Build Pallet', ja: '組盤' },
      receive: { zh: '收货', en: 'Receive', ja: '入荷' },
      details: { zh: '明细', en: 'Lines', ja: '明細' },
      palletDetails: { zh: '组盘 Detail', en: 'Pallet Details', ja: '組盤明細' },
      planned: { zh: '计划', en: 'Planned', ja: '計画' },
      completed: { zh: '完成', en: 'Done', ja: '完了' },
      receiveLoc: { zh: '收货位', en: 'Receive Loc', ja: '入荷ロケ' },
      targetLoc: { zh: '目标位', en: 'Target Loc', ja: '目標ロケ' },
      lineNo: { zh: '行号', en: 'Line No', ja: '行番号' },
      allocate: { zh: '自动分配', en: 'Auto Allocate', ja: '自動割当' },
      submitPallet: { zh: '提交组盘', en: 'Submit Pallet', ja: '組盤送信' },
      targetPlaceholder: { zh: '空则 Allocator', en: 'Empty = Allocator', ja: '空=Allocator' },
      receiveSuccess: { zh: '收货成功', en: 'Received', ja: '入荷しました' },
      palletSuccess: { zh: '组盘成功', en: 'Pallet built', ja: '組盤しました' },
    },
    outbound: {
      title: { zh: '出库单', en: 'Outbound Orders', ja: '出庫伝票' },
      subtitle: {
        zh: '建单（组号 / Pri）· 审核建运 · 平库发运',
        en: 'Create (group / Pri) · Approve transport · Ship',
        ja: '作成（組番号/Pri）・承認・出荷',
      },
      create: { zh: '新建出库单', en: 'New Outbound', ja: '出庫伝票作成' },
      groupNo: { zh: '组号', en: 'Group No', ja: '組番号' },
      groupPlaceholder: { zh: '默认=单号', en: 'Default = order no', ja: '既定=伝票番号' },
      ship: { zh: '发运', en: 'Ship', ja: '出荷' },
      lines: { zh: '行', en: 'Lines', ja: '行' },
      shipSuccess: { zh: '发运成功', en: 'Shipped', ja: '出荷しました' },
    },
    cycleCount: {
      title: { zh: '盘点', en: 'Cycle Count', ja: '棚卸' },
      subtitle: { zh: '建计划 · 录实盘 · 确认调账', en: 'Plan · Record · Confirm adjust', ja: '計画・実棚・確定' },
      create: { zh: '新建盘点', en: 'New Count', ja: '棚卸作成' },
      createTitle: { zh: '新建盘点计划', en: 'New Count Plan', ja: '棚卸計画' },
      confirm: { zh: '确认调账', en: 'Confirm Adjust', ja: '差異確定' },
      lines: { zh: '行', en: 'Lines', ja: '行' },
      bookQty: { zh: '账面', en: 'Book', ja: '帳簿' },
      countQty: { zh: '实盘', en: 'Counted', ja: '実棚' },
      diffQty: { zh: '差异', en: 'Diff', ja: '差異' },
      record: { zh: '录入', en: 'Record', ja: '入力' },
      location: { zh: '库位', en: 'Location', ja: 'ロケーション' },
      recorded: { zh: '已录实盘', en: 'Counted saved', ja: '実棚を保存' },
      adjustSuccess: { zh: '调账成功', en: 'Adjusted', ja: '調整しました' },
    },
  },
  wcsOps: {
    transport: {
      title: { zh: '运输单监控', en: 'Transport Monitor', ja: '輸送モニタ' },
      subtitle: {
        zh: 'Bus 运输单与 Leg 时间线 · 失败原因',
        en: 'Bus orders, leg timeline · fail reasons',
        ja: 'Bus輸送とLegタイムライン・失敗理由',
      },
      refresh: { zh: '刷新', en: 'Refresh', ja: '更新' },
      container: { zh: '容器', en: 'Container', ja: '容器' },
      from: { zh: '起点', en: 'From', ja: '起点' },
      to: { zh: '终点', en: 'To', ja: '終点' },
      status: { zh: '状态', en: 'Status', ja: '状態' },
      refType: { zh: '来源', en: 'Source', ja: '参照' },
      failReason: { zh: '失败原因', en: 'Fail Reason', ja: '失敗理由' },
      legs: { zh: 'Legs', en: 'Legs', ja: 'Legs' },
      legStatus: { zh: '状态 {status}', en: 'Status {status}', ja: '状態 {status}' },
      noLegs: { zh: '无 Leg', en: 'No legs', ja: 'Legなし' },
    },
    stackerTrigger: {
      title: { zh: '堆垛机仿真触发', en: 'Stacker Simulation Trigger', ja: 'スタッカーシミュ' },
      subtitle: {
        zh: 'SUDR 目的地申请 · 段反馈（InMemory TriggerPort）',
        en: 'SUDR destination request · segment feedback',
        ja: 'SUDR目的地申請・セグメントフィードバック',
      },
      destTitle: { zh: '目的地申请 (SUDR)', en: 'Destination Request (SUDR)', ja: '目的地申請 (SUDR)' },
      segTitle: { zh: '段反馈', en: 'Segment Feedback', ja: 'セグメントFB' },
      container: { zh: '容器', en: 'Container', ja: '容器' },
      sourcePoint: { zh: '源点位', en: 'Source Point', ja: 'ソース点' },
      sourcePlaceholder: { zh: 'Stk. 申请点', en: 'Stk. request point', ja: 'Stk.申請点' },
      height: { zh: '高度', en: 'Height', ja: '高さ' },
      weight: { zh: '重量', en: 'Weight', ja: '重量' },
      checkResult: { zh: '校验结果', en: 'Check Result', ja: 'チェック結果' },
      submitDest: { zh: '提交申请', en: 'Submit Request', ja: '申請送信' },
      segmentPoint: { zh: '段点位', en: 'Segment Point', ja: 'セグメント点' },
      feedback: { zh: '反馈码', en: 'Feedback', ja: 'フィードバック' },
      submitSeg: { zh: '提交反馈', en: 'Submit Feedback', ja: 'FB送信' },
      submitted: { zh: '已提交', en: 'Submitted', ja: '送信しました' },
      failed: { zh: '失败', en: 'Failed', ja: '失敗' },
    },
    fourWayTrigger: {
      title: { zh: '四向车仿真触发', en: 'Four-Way Simulation Trigger', ja: '四方向シミュ' },
      subtitle: {
        zh: '同一 TriggerPort 契约 · 默认点位偏 Fw. 前缀',
        en: 'Same TriggerPort contract · default Fw. prefix',
        ja: '同一 TriggerPort ・既定 Fw. 接頭辞',
      },
      destTitle: { zh: '目的地申请 (SUDR)', en: 'Destination Request (SUDR)', ja: '目的地申請 (SUDR)' },
      segTitle: {
        zh: '段反馈 / 提升机反馈',
        en: 'Segment / Hoist Feedback',
        ja: 'セグメント/ホイストFB',
      },
      sourcePlaceholder: { zh: 'Fw. 层/巷申请点', en: 'Fw. layer/aisle request point', ja: 'Fw.層/通路申請点' },
      segmentPlaceholder: {
        zh: '层内点或 Hoist AP/EP',
        en: 'In-layer point or Hoist AP/EP',
        ja: '層内点または Hoist AP/EP',
      },
    },
  },
  deviceComm: {
    refresh: { zh: '刷新状态', en: 'Refresh Status', ja: '状態更新' },
    reload: { zh: '重载连接/规则', en: 'Reload Connections/Rules', ja: '接続/規則リロード' },
    hubConnected: { zh: '已连接', en: 'Connected', ja: '接続済' },
    hubDisconnected: { zh: '未连接', en: 'Disconnected', ja: '未接続' },
    name: { zh: '名称', en: 'Name', ja: '名称' },
    protocol: { zh: '协议', en: 'Protocol', ja: 'プロトコル' },
    status: { zh: '状态', en: 'Status', ja: '状態' },
    reconnectCount: { zh: '重连次数', en: 'Reconnects', ja: '再接続回数' },
    ioFail: { zh: 'IO失败', en: 'IO Fail', ja: 'IO失敗' },
    lastError: { zh: '最近错误', en: 'Last Error', ja: '直近エラー' },
    actions: { zh: '操作', en: 'Actions', ja: '操作' },
    connect: { zh: '连接', en: 'Connect', ja: '接続' },
    disconnect: { zh: '断开', en: 'Disconnect', ja: '切断' },
    reconnect: { zh: '重连', en: 'Reconnect', ja: '再接続' },
    recentEvents: { zh: '最近规则事件', en: 'Recent Rule Events', ja: '最近の規則イベント' },
    time: { zh: '时间', en: 'Time', ja: '時刻' },
    rule: { zh: '规则', en: 'Rule', ja: '規則' },
    event: { zh: '事件', en: 'Event', ja: 'イベント' },
    success: { zh: '成功', en: 'Success', ja: '成功' },
    yes: { zh: '是', en: 'Yes', ja: 'はい' },
    no: { zh: '否', en: 'No', ja: 'いいえ' },
    message: { zh: '消息', en: 'Message', ja: 'メッセージ' },
    values: { zh: '值', en: 'Values', ja: '値' },
    stateDisconnected: { zh: '断开', en: 'Disconnected', ja: '切断' },
    stateConnecting: { zh: '连接中', en: 'Connecting', ja: '接続中' },
    stateConnected: { zh: '已连接', en: 'Connected', ja: '接続済' },
    stateFault: { zh: '故障', en: 'Fault', ja: '故障' },
    stateReconnecting: { zh: '重连中', en: 'Reconnecting', ja: '再接続中' },
    reloaded: { zh: '已重载', en: 'Reloaded', ja: 'リロード済' },
    done: { zh: '完成', en: 'Done', ja: '完了' },
  },
  scada: {
    selectView: { zh: '选择视图', en: 'Select view', ja: 'ビュー選択' },
    refresh: { zh: '刷新', en: 'Refresh', ja: '更新' },
    empty: { zh: '请选择 SCADA 视图', en: 'Select a SCADA view', ja: 'SCADAビューを選択' },
  },
  platform: {
    refresh: { zh: '刷新', en: 'Refresh', ja: '更新' },
    controlMode: {
      scopePlaceholder: { zh: '作用域', en: 'Scope', ja: 'スコープ' },
      currentState: { zh: '当前状态 — {scope}', en: 'Current — {scope}', ja: '現在状態 — {scope}' },
      mode: { zh: '模式：', en: 'Mode:', ja: 'モード：' },
      eStop: { zh: '急停：', en: 'E-Stop:', ja: '非常停止：' },
      updatedAt: { zh: '更新时间：', en: 'Updated:', ja: '更新日時：' },
      setMode: { zh: '设置模式：', en: 'Set mode:', ja: 'モード設定：' },
      auto: { zh: '自动', en: 'Auto', ja: '自動' },
      semi: { zh: '半自动', en: 'Semi-auto', ja: '半自動' },
      manual: { zh: '手动', en: 'Manual', ja: '手動' },
      eStopOn: { zh: '急停 ON', en: 'E-Stop ON', ja: '非常停止 ON' },
      eStopOff: { zh: '急停 OFF', en: 'E-Stop OFF', ja: '非常停止 OFF' },
      loading: { zh: '加载中…', en: 'Loading…', ja: '読込中…' },
      modeUpdated: { zh: '模式已更新', en: 'Mode updated', ja: 'モードを更新' },
      eStopUpdated: { zh: '急停状态已更新', en: 'E-Stop updated', ja: '非常停止を更新' },
      updateFailed: { zh: '更新失败', en: 'Update failed', ja: '更新失敗' },
    },
    interfaceLog: {
      direction: { zh: '方向', en: 'Direction', ja: '方向' },
      system: { zh: '系统', en: 'System', ja: 'システム' },
      path: { zh: '路径', en: 'Path', ja: 'パス' },
      orderNo: { zh: '单号', en: 'Order No', ja: '伝票番号' },
      durationMs: { zh: '耗时ms', en: 'Duration ms', ja: '所要ms' },
      success: { zh: '成功', en: 'Success', ja: '成功' },
      yes: { zh: '是', en: 'Yes', ja: 'はい' },
      no: { zh: '否', en: 'No', ja: 'いいえ' },
      error: { zh: '错误', en: 'Error', ja: 'エラー' },
      time: { zh: '时间', en: 'Time', ja: '時刻' },
    },
  },
}

function walk(dir, acc = []) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name)
    if (e.isDirectory()) walk(p, acc)
    else if (e.name.endsWith('.vue')) acc.push(p)
  }
  return acc
}

function extractEntities() {
  const out = {}
  for (const f of walk(VIEWS)) {
    const t = fs.readFileSync(f, 'utf8')
    const m = t.match(/i18n-key="generated\.([^"]+)"/)
    if (!m) continue
    const key = m[1]
    if (['Device', 'SubDevice', 'CommConnection', 'CommPoint', 'CommRule'].includes(key)) continue
    const props = [...t.matchAll(/\{\s*prop:\s*'([^']+)'/g)].map((x) => x[1])
    out[key] = [...new Set(props)]
  }
  return out
}

function splitCamel(s) {
  return s
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/_/g, ' ')
    .replace(/^./, (c) => c.toUpperCase())
}

function fieldLabel(prop, lang) {
  if (FIELD[prop]) return FIELD[prop][lang]
  return splitCamel(prop)
}

function pickLang(node, lang) {
  if (node == null) return node
  if (typeof node === 'object' && ('zh' in node || 'en' in node || 'ja' in node)) {
    return node[lang] ?? node.zh
  }
  if (typeof node === 'object') {
    const o = {}
    for (const [k, v] of Object.entries(node)) o[k] = pickLang(v, lang)
    return o
  }
  return node
}

function buildGenerated(entities, lang) {
  const gen = {}
  for (const [entity, props] of Object.entries(entities)) {
    const title = ENTITY_TITLE[entity] || {
      zh: entity,
      en: entity,
      ja: entity,
    }
    const block = {
      title: title[lang],
      listTitle: title[lang],
      actions: COMMON.actions[lang],
      edit: COMMON.edit[lang],
      add: COMMON.add[lang],
      delete: COMMON.delete[lang],
      deleteConfirm: COMMON.deleteConfirm[lang],
    }
    for (const p of props) block[p] = fieldLabel(p, lang)
    gen[entity] = block
  }
  return gen
}

function deepMerge(target, source) {
  for (const [k, v] of Object.entries(source)) {
    if (v && typeof v === 'object' && !Array.isArray(v)) {
      target[k] = target[k] && typeof target[k] === 'object' ? target[k] : {}
      deepMerge(target[k], v)
    } else {
      target[k] = v
    }
  }
  return target
}

function writeLocale(code, lang) {
  const file = path.join(LANG_DIR, `${code}.json`)
  const data = JSON.parse(fs.readFileSync(file, 'utf8'))
  const entities = extractEntities()
  data.generated = data.generated || {}
  deepMerge(data.generated, buildGenerated(entities, lang))

  data.menu = data.menu || {}
  for (const [k, v] of Object.entries(MENUS)) data.menu[k] = v[lang]

  deepMerge(data, pickLang(PAGE_KEYS, lang))

  fs.writeFileSync(file, JSON.stringify(data, null, 2) + '\n', 'utf8')
  console.log(`updated ${code}: generated=${Object.keys(entities).length} menus+=${Object.keys(MENUS).length}`)
}

writeLocale('zh-CN', 'zh')
writeLocale('en-US', 'en')
writeLocale('ja-JP', 'ja')
console.log('done')
