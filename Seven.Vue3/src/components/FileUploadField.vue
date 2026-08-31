<template>
  <div class="file-upload-field">
    <el-upload
      :show-file-list="false"
      :http-request="doUpload"
      :disabled="disabled || uploading"
      :accept="accept"
    >
      <el-button size="small" :loading="uploading" :disabled="disabled">
        {{ uploading ? t('common.uploading') : t('common.upload') }}
      </el-button>
    </el-upload>
    <div v-if="modelValue" class="file-upload-field__link">
      <a :href="resolvedUrl" target="_blank" rel="noopener">{{ displayName }}</a>
      <el-button v-if="!disabled" link type="danger" size="small" @click="clear">{{ t('common.clear') }}</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { uploadFile } from '../api/http'

const props = withDefaults(
  defineProps<{
    modelValue?: string | null
    disabled?: boolean
    accept?: string
  }>(),
  {
    modelValue: '',
    disabled: false,
    accept: '.png,.jpg,.jpeg,.gif,.webp,.pdf,.xlsx,.xls,.doc,.docx,.zip',
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const { t } = useI18n()
const uploading = ref(false)

const resolvedUrl = computed(() => {
  const v = props.modelValue || ''
  if (!v) return ''
  if (/^https?:\/\//i.test(v)) return v
  return v.startsWith('/') ? v : `/${v}`
})

const displayName = computed(() => {
  const v = props.modelValue || ''
  if (!v) return ''
  const parts = v.split(/[/\\]/)
  return parts[parts.length - 1] || v
})

async function doUpload(option: { file: File }) {
  uploading.value = true
  try {
    const res = await uploadFile('/api/File/upload', option.file)
    if (res.status) {
      const url = (res.data as { url?: string } | undefined)?.url
      if (url) {
        emit('update:modelValue', url)
        ElMessage.success(res.message || t('common.uploadSuccess'))
      } else {
        ElMessage.error(t('common.operationFailed'))
      }
    } else {
      ElMessage.error(res.message || t('common.operationFailed'))
    }
  } catch {
    ElMessage.error(t('common.operationFailed'))
  } finally {
    uploading.value = false
  }
}

function clear() {
  emit('update:modelValue', '')
}
</script>

<style scoped>
.file-upload-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
}
.file-upload-field__link {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  word-break: break-all;
}
</style>
