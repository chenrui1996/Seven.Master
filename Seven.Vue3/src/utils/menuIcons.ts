import type { Component } from 'vue'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import {
  Box,
  Collection,
  Connection,
  Cpu,
  DataLine,
  Document,
  Files,
  Grid,
  HomeFilled,
  List,
  Location,
  Monitor,
  Notebook,
  OfficeBuilding,
  Operation,
  Setting,
  ShoppingCart,
  Sort,
  TakeawayBox,
  Tools,
  TrendCharts,
  User,
  Van,
  Warning,
} from '@element-plus/icons-vue'

/** 菜单名称 → Element Plus 图标（后端 Icon 字段优先） */
const menuIconMap: Record<string, Component> = {
  首页: HomeFilled,
  系统管理: Setting,
  用户管理: User,
  角色管理: List,
  菜单管理: Grid,
  部门管理: OfficeBuilding,
  字典管理: Document,
  日志管理: DataLine,
  代码生成: Operation,
  设备监控: Cpu,
  告警管理: Warning,
  大屏看板: TrendCharts,
  工作流定义: Operation,
  我的审批: Files,
  定时任务: Operation,

  仓储管理: Box,
  仓储WMS: Box,
  主数据: OfficeBuilding,
  库存容器: TakeawayBox,
  单据作业: Document,
  仓库: OfficeBuilding,
  库区: Grid,
  层: List,
  巷道: Operation,
  库位: Location,
  容器类型: Box,
  容器: TakeawayBox,
  交接位: Connection,
  库存: Box,
  库存流水: DataLine,
  入库单: ShoppingCart,
  出库单: Van,
  盘点单: Notebook,
  拣选任务: Collection,
  入库快捷: ShoppingCart,
  出库快捷: Van,

  业务扩展: Files,
  仓内调拨: Sort,
  仿真与监控: Monitor,
  仿真: Operation,
  策略: Setting,
  任务: Collection,

  任务调度: Operation,

  立库WCS: Cpu,
  堆垛仿真触发: Operation,
  运输单监控: DataLine,
  申请点: Location,
  巷道策略: Operation,
  双深配置: Grid,
  路网: Connection,
  点码映射: List,
  上架任务: TakeawayBox,
  取货任务: Van,
  设备段任务: Cpu,

  四向车WCS: Van,
  四向仿真触发: Operation,
  层策略: List,
  巷策略: Operation,
  地图版本: Document,
  节点: Location,
  路网边: Connection,
  停车账本: Notebook,
  提升机: Cpu,
  提升机层口: Location,
  穿梭任务: Van,
  提升任务: Operation,
  提升执行段: Files,

  执行运维: Tools,
  '运行模式/联锁': Setting,
  接口日志: DataLine,
  '2D看板': Monitor,
  设备通讯: Connection,
  通讯连接: Connection,
  通讯点位: Location,
  通讯规则: Operation,
  通讯运行态: Monitor,
}

const iconsByName = ElementPlusIconsVue as Record<string, Component>
const defaultIcon = Box

/** 根据菜单名 / 后端 Icon 字段获取图标组件 */
export function getMenuIcon(name?: string, icon?: string): Component {
  if (icon) {
    const key = icon.replace(/^el-icon-/i, '')
    const pascal = key.charAt(0).toUpperCase() + key.slice(1)
    if (iconsByName[pascal]) return iconsByName[pascal]
    if (iconsByName[key]) return iconsByName[key]
  }
  if (!name) return defaultIcon
  return menuIconMap[name] ?? defaultIcon
}
