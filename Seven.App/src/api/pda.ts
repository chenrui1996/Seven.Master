import { request } from './http'

export function login(userName: string, password: string) {
  return request<{ token?: string; accessToken?: string; refreshToken?: string }>({
    url: '/api/Auth/login',
    method: 'POST',
    data: { userName, password },
  })
}

export function fetchPdaMenu() {
  return request<Array<{ code: string; title: string; path: string }>>({
    url: '/api/pda/menu',
  })
}

export function fetchPendingInbound() {
  return request<
    Array<{
      id: number
      orderNo: string
      status: number
      lines: Array<{
        lineNo: number
        materialCode: string
        qty: number
        remainingQty: number
        containerCode?: string
        fromLocation?: string
      }>
    }>
  >({ url: '/api/pda/inbound/pending' })
}

export function receiveFloor(
  orderId: number,
  body: { lineNo: number; qty: number; containerCode: string; receiveLocationCode: string },
) {
  return request({
    url: `/api/pda/inbound/${orderId}/receive`,
    method: 'POST',
    data: body,
  })
}

export function fetchPendingPutaway() {
  return request<
    Array<{
      detailId: number
      orderNo: string
      detailNo: number
      materialCode: string
      qty: number
      containerCode: string
      receiveLocationCode: string
    }>
  >({ url: '/api/pda/putaway/pending' })
}

export function confirmPutaway(body: {
  containerCode: string
  toLocationCode: string
  detailId?: number
}) {
  return request({
    url: '/api/pda/putaway/confirm',
    method: 'POST',
    data: body,
  })
}

export function fetchPendingCycleCount() {
  return request<
    Array<{
      id: number
      orderNo: string
      status: number
      totalLines: number
      countedLines: number
    }>
  >({ url: '/api/pda/cyclecount/pending' })
}

export function fetchCycleCount(id: number) {
  return request<{
    id: number
    orderNo: string
    status: number
    lines: Array<{
      lineNo: number
      locationCode: string
      materialCode: string
      containerCode?: string
      bookQty: number
      countQty: number
      diffQty: number
      counted: boolean
    }>
  }>({ url: `/api/pda/cyclecount/${id}` })
}

export function recordCycleCount(
  orderId: number,
  body: { lineNo: number; countQty: number; locationCode?: string; containerCode?: string },
) {
  return request({
    url: `/api/pda/cyclecount/${orderId}/record`,
    method: 'POST',
    data: body,
  })
}

export function confirmCycleCount(orderId: number) {
  return request({
    url: `/api/pda/cyclecount/${orderId}/confirm`,
    method: 'POST',
  })
}

export function fetchPendingPicking() {
  return request<
    Array<{
      id: number
      taskNo: string
      outboundOrderId: number
      orderNo: string
      materialCode: string
      bookQty: number
      pickQty: number
      fromLocation?: string
      toLocation?: string
      containerCode?: string
      status: number
    }>
  >({ url: '/api/pda/picking/pending' })
}

export function confirmPick(body: {
  pickingTaskId: number
  pickQty?: number
  containerCode?: string
  fromLocation?: string
}) {
  return request({
    url: '/api/pda/picking/confirm',
    method: 'POST',
    data: body,
  })
}

export function fetchPendingTransfer() {
  return request<
    Array<{
      id: number
      orderNo: string
      status: number
      remark?: string
      lines: Array<{
        lineNo: number
        materialCode: string
        qty: number
        completedQty: number
        fromLocation?: string
        toLocation?: string
        containerCode?: string
      }>
    }>
  >({ url: '/api/pda/biz/transfer/pending' })
}

export function completeTransfer(id: number) {
  return request({
    url: `/api/pda/biz/transfer/complete/${id}`,
    method: 'POST',
  })
}
