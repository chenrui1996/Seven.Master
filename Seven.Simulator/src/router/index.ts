import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/features' },
    { path: '/features', name: 'Features', component: () => import('../views/Features.vue') },
    { path: '/map', name: 'Map', component: () => import('../views/MapEditor.vue') },
    { path: '/player', name: 'Player', component: () => import('../views/Player.vue') },
    { path: '/promote', name: 'Promote', component: () => import('../views/Promote.vue') },
  ],
})

export default router
