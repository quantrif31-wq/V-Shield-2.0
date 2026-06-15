<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Enterprise devices</span>
                <h1 class="page-title">Device Topology</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadTopology">Refresh</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Overviews</span><h2 class="panel-title">Device Landscape</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading topology...</div>
                <div v-else>
                    <div class="kpi-row">
                        <div class="kpi-card"><strong>{{ topology.length }}</strong><span>Tổng devices</span></div>
                        <div class="kpi-card"><strong>{{ topology.filter(d => d.status === 'Ok').length }}</strong><span>Online</span></div>
                        <div class="kpi-card"><strong>{{ topology.filter(d => d.status !== 'Ok').length }}</strong><span>Non-OK</span></div>
                    </div>
                    <div class="table-container">
                        <table class="data-table">
                            <thead>
                                <tr>
                                    <th>Device</th>
                                    <th>Type</th>
                                    <th>Status</th>
                                    <th>Controller</th>
                                    <th>R/S</th>
                                    <th>Site</th>
                                    <th>Health</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="d in topology" :key="d.securityDeviceId">
                                    <td><strong>{{ d.name }}</strong></td>
                                    <td><span class="badge badge-info">{{ d.deviceType }}</span></td>
                                    <td><span class="status-dot" :class="statusClass(d.status)"></span>{{ d.status }}</td>
                                    <td>{{ d.controller?.protocol || '—' }}</td>
                                    <td>{{ d.readerCount }}/{{ d.relayCount }}/{{ d.sensorCount }}</td>
                                    <td>{{ d.siteId || '—' }}</td>
                                    <td>{{ d.healthStatus?.status || '—' }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Adapters</span><h2 class="panel-title">Connector & Adapter Status</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else>
                    <div class="table-container">
                        <table class="data-table">
                            <thead><tr><th>Protocol</th><th>Type</th><th>Status</th></tr></thead>
                            <tbody>
                                <tr v-for="a in adapters" :key="a.protocol">
                                    <td>{{ a.protocol }}</td>
                                    <td>{{ a.type }}</td>
                                    <td><span class="status-dot" :class="a.status === 'Simulated' ? 'status-ok' : 'status-warn'"></span>{{ a.status }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </article>
        </section>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const topology = ref([])
const adapters = ref([])
const loading = ref(true)

async function loadTopology() {
    loading.value = true
    try {
        const [topoRes, adaptRes] = await Promise.all([
            enterpriseApi.getTopology(),
            enterpriseApi.getAdapters().catch(() => ({ data: { adapters: [] } }))
        ])
        topology.value = Array.isArray(topoRes.data) ? topoRes.data : []
        adapters.value = adaptRes.data?.adapters || []
    } catch { topology.value = []; adapters.value = [] }
    finally { loading.value = false }
}

function statusClass(s) {
    if (s === 'Ok' || s === 'Online') return 'status-ok'
    if (s === 'Tamper' || s === 'Fault') return 'status-danger'
    return 'status-warn'
}

onMounted(loadTopology)
</script>
