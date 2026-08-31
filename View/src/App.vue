<template>
  <router-view />
  <RouteProgress />
  <ToastProvider />
  <GlobalCallOverlay />
</template>

<script setup>
import ToastProvider from './components/ui/ToastProvider.vue'
import RouteProgress from './components/ui/RouteProgress.vue'
import GlobalCallOverlay from './components/Call/GlobalCallOverlay.vue'
</script>