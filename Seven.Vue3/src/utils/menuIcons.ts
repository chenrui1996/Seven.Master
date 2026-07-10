import type { Component } from 'vue'
import {
  Box,
  Cpu,
  DataLine,
  Document,
  Grid,
  HomeFilled,
  List,
  Monitor,
  OfficeBuilding,
  Operation,
  Setting,
  TrendCharts,
  User,
  Warning,
} from '@element-plus/icons-vue'

/** 菜单名称 → 工业风图标映射 */
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
  设备管理: Monitor,
  设备监控: Cpu,
  告警管理: Warning,
  大屏看板: TrendCharts,
  仓储管理: Box,
  任务调度: Operation,
}

const defaultIcon = Box

/** 根据菜单名获取图标组件 */
export function getMenuIcon(name?: string): Component {
  if (!name) return defaultIcon
  return menuIconMap[name] ?? defaultIcon
}

/** 系统模块快捷入口（i18n 键） */
export const moduleShortcutKeys = [
  { titleKey: 'home.moduleWms', descKey: 'home.moduleWmsDesc', iconKey: 'wms', icon: Box, color: '#f97316' },
  { titleKey: 'home.moduleWcs', descKey: 'home.moduleWcsDesc', iconKey: 'wcs', icon: Operation, color: '#0ea5e9' },
  { titleKey: 'home.moduleDevice', descKey: 'home.moduleDeviceDesc', iconKey: 'device', icon: Monitor, color: '#16a34a' },
  { titleKey: 'home.moduleAlert', descKey: 'home.moduleAlertDesc', iconKey: 'alert', icon: Warning, color: '#dc2626' },
]
