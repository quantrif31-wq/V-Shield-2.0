<template>
    <ForcePasswordChange @changed="goNext" />
</template>

<script setup>
import { useRoute, useRouter } from 'vue-router'
import ForcePasswordChange from '../components/auth/ForcePasswordChange.vue'

const router = useRouter()
const route = useRoute()

async function goNext() {
    const requestedPath = String(route.query.redirect || '')
    router.replace(requestedPath.startsWith('/') && !requestedPath.startsWith('//') ? requestedPath : '/')
}
</script>