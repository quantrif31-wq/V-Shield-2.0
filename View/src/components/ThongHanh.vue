<template>
  <GateTransitMonitor credential-mode="FACEID" />
</template>

<script>
import GateTransitMonitor from "./GateTransitMonitor.vue"

export default {
  name: "FacePlateTransitMonitor",
  components: { GateTransitMonitor }
}
</script>
