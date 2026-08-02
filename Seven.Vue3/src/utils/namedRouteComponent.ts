import { defineAsyncComponent, defineComponent, h, type Component } from 'vue'

/** 为 script-setup 页面包装 keep-alive 可用的组件 name */
export function namedRouteComponent(
  loader: () => Promise<{ default: Component }>,
  name: string,
): Component {
  const AsyncInner = defineAsyncComponent(loader)
  return defineComponent({
    name,
    setup() {
      return () => h(AsyncInner)
    },
  })
}
