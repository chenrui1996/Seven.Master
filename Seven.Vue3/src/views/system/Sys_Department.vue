<template>
  <div class="crud-page seven-page menu-page">
    <div class="split-layout">
      <el-card class="tree-panel">
        <template #header>
          <div class="toolbar">
            <span>{{ t('sysDept.treeTitle') }}</span>
            <el-button v-permission="'Sys_Department.Add'" size="small" type="primary" :icon="ActionIcons.addChild" @click="addChild">
              {{ t('sysDept.addChild') }}
            </el-button>
          </div>
        </template>
        <el-tree
          :data="deptTree"
          node-key="departmentId"
          :props="{ label: 'departmentName', children: 'children' }"
          highlight-current
          default-expand-all
          @node-click="onNodeClick"
        />
      </el-card>
      <el-card class="form-panel">
        <template #header>
          <div class="toolbar">
            <span>{{ t('sysDept.formTitle') }}</span>
            <div class="seven-btn-group">
              <el-button v-permission="'Sys_Department.Update'" type="primary" :icon="ActionIcons.save" @click="save">{{ t('common.confirm') }}</el-button>
              <el-button v-permission="'Sys_Department.Delete'" type="danger" :icon="ActionIcons.delete" @click="remove">{{ t('sysDept.delete') }}</el-button>
            </div>
          </div>
        </template>
        <el-form :model="form" label-width="100px">
          <el-form-item :label="t('sysDept.colName')" required>
            <el-input v-model="form.departmentName" />
          </el-form-item>
          <el-form-item :label="t('sysDept.colCode')">
            <el-input v-model="form.departmentCode" />
          </el-form-item>
          <el-form-item :label="t('sysDept.colOrder')">
            <el-input-number v-model="form.orderNo" :min="0" />
          </el-form-item>
          <el-form-item :label="t('sysDept.colStatus')">
            <el-switch v-model="form.enable" :active-value="1" :inactive-value="0" />
          </el-form-item>
        </el-form>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http from '../../api/http'
import { ActionIcons } from '../../constants/actionIcons'

interface DeptItem {
  departmentId: number
  departmentName: string
  departmentCode?: string
  parentId: number
  orderNo?: number
  enable?: number
  children?: DeptItem[]
}

const { t } = useI18n()
const deptTree = ref<DeptItem[]>([])
const flatDepts = ref<DeptItem[]>([])

const form = reactive({
  departmentId: 0,
  departmentName: '',
  departmentCode: '',
  parentId: 0,
  orderNo: 0,
  enable: 1 as number,
})

function buildDeptTree(flat: DeptItem[]): DeptItem[] {
  return flat
    .filter((d) => (d.parentId ?? 0) === 0)
    .sort((a, b) => (a.orderNo ?? 0) - (b.orderNo ?? 0))
    .map((d) => {
      const children = flat
        .filter((c) => c.parentId === d.departmentId)
        .sort((a, b) => (a.orderNo ?? 0) - (b.orderNo ?? 0))
      return children.length ? { ...d, children } : { ...d }
    })
}

async function loadTree() {
  const res = await http.get('/api/Sys_Department/getTree')
  if (res.status && res.data) {
    flatDepts.value = res.data as DeptItem[]
    deptTree.value = buildDeptTree(flatDepts.value)
  }
}

function onNodeClick(data: DeptItem) {
  Object.assign(form, {
    departmentId: data.departmentId,
    departmentName: data.departmentName,
    departmentCode: data.departmentCode ?? '',
    parentId: data.parentId ?? 0,
    orderNo: data.orderNo ?? 0,
    enable: data.enable ?? 1,
  })
}

function addChild() {
  Object.assign(form, {
    departmentId: 0,
    departmentName: '',
    departmentCode: '',
    parentId: form.departmentId || 0,
    orderNo: 0,
    enable: 1,
  })
}

async function save() {
  if (!form.departmentName) {
    ElMessage.warning(t('sysDept.requiredName'))
    return
  }
  const url = form.departmentId ? '/api/Sys_Department/update' : '/api/Sys_Department/add'
  const res = await http.post(url, { ...form })
  if (res.status) {
    ElMessage.success(t('common.success'))
    await loadTree()
    if (!form.departmentId && res.data) {
      form.departmentId = (res.data as DeptItem).departmentId
    }
  }
}

async function remove() {
  if (!form.departmentId) return
  await ElMessageBox.confirm(t('sysDept.deleteConfirm'), t('sysDept.delete'), { type: 'warning' })
  const res = await http.post('/api/Sys_Department/del', form.departmentId)
  if (res.status) {
    ElMessage.success(t('common.success'))
    form.departmentId = 0
    await loadTree()
  }
}

onMounted(loadTree)
</script>
