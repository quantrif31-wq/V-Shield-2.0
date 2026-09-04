<template>
  <!--
    Keep a boundary at the application root as well as inside MainLayout.
    The inner boundary protects individual pages; this one also protects the
    shell itself (Header/Sidebar/lazy layout) so a transient route exception
    can never leave the browser with an empty root element.
  -->
  <RouteErrorBoundary>
    <router-view />
  </RouteErrorBoundary>
  <RouteProgress />
  <ToastProvider />
  <GlobalCallOverlay />
</template>

<script setup>
import ToastProvider from './components/ui/ToastProvider.vue'
import RouteProgress from './components/ui/RouteProgress.vue'
import GlobalCallOverlay from './components/Call/GlobalCallOverlay.vue'
import RouteErrorBoundary from './components/ui/RouteErrorBoundary.vue'
</script>
