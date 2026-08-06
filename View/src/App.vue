<template>
  <router-view />
  <ToastProvider />
</template>

<script setup>
import ToastProvider from './components/ui/ToastProvider.vue'
</script>

<style>
.app-boot {
  min-height: 100vh;
  display: grid;
  place-items: center;
  align-content: center;
  gap: 14px;
  background: var(--surface-app, #eef4f6);
  color: var(--text-secondary, #3f586b);
  font: 600 14px/20px 'Segoe UI', sans-serif;
}
.app-boot__mark {
  display: grid;
  place-items: center;
  width: 52px;
  height: 52px;
  border-radius: 14px;
  background: #0f7c82;
  color: #fff;
  font-size: 24px;
  font-weight: 800;
}
</style>
