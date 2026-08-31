<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <div class="sim-shell">
    <header class="sim-header">
      <div class="brand">Seven Simulator</div>
      <nav>
        <RouterLink to="/features">① Features</RouterLink>
        <RouterLink to="/map">② 地图</RouterLink>
        <RouterLink to="/player">③ 仿真</RouterLink>
        <RouterLink to="/promote">④ 生产</RouterLink>
      </nav>
    </header>
    <main class="sim-main">
      <RouterView />
    </main>
  </div>
</template>

<style>
:root {
  font-family: 'Segoe UI', system-ui, sans-serif;
  color: #1a1d21;
  background: #eef1f4;
}
* { box-sizing: border-box; }
body { margin: 0; }
.sim-shell { min-height: 100vh; display: flex; flex-direction: column; }
.sim-header {
  display: flex; align-items: center; gap: 2rem;
  padding: 0.75rem 1.25rem;
  background: #0f2744;
  color: #f4f7fb;
}
.brand { font-weight: 700; letter-spacing: 0.02em; }
nav { display: flex; gap: 1rem; flex-wrap: wrap; }
nav a { color: #b8c7d9; text-decoration: none; }
nav a.router-link-active { color: #fff; border-bottom: 2px solid #3d8bfd; }
.sim-main { flex: 1; padding: 1.25rem; }
.sim-page {
  background: #fff;
  border-radius: 8px;
  padding: 1.25rem;
  box-shadow: 0 1px 2px rgba(15, 39, 68, 0.06);
}
.sim-page h1 { margin: 0 0 0.5rem; font-size: 1.25rem; }
.sim-page .hint { color: #5b6570; margin-bottom: 1rem; font-size: 0.9rem; }
</style>
