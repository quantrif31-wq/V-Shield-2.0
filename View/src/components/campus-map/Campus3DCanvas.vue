<template>
    <div ref="containerRef" class="c3d-container" @mouseenter="onMouseEnter" @mouseleave="onMouseLeave">
        <div v-if="!initialized" class="c3d-loading">ƒêang kh·ªüi t·∫°o 3D...</div>

        <div v-if="siteCards.length" class="c3d-site-dock">
            <button
                v-for="site in siteCards"
                :key="site.siteId"
                class="c3d-site-chip"
                :class="{ active: selectedSiteId === site.siteId }"
                :title="`${site.name} | ${site.buildingCount} toa nha | ${site.gateCount} cong | ${site.warningGateCount} canh bao`"
                @click="focusSite(site.siteId, true)"
            >
                <span class="site-chip-dot" :class="{ warning: site.warningGateCount > 0, critical: site.criticalCount > 0 }"></span>
                <span class="site-code">{{ site.code }}</span>
                <span>{{ site.buildingCount }} toa nha ï {{ site.gateCount }} cong</span>
                <span>{{ site.criticalCount }} diem critical ï {{ site.warningGateCount }} canh bao</span>
            </button>
        </div>

        <div ref="tooltipRef" class="c3d-tooltip" :class="tooltip.tone" :style="tooltipStyle" v-show="tooltip.visible">
            <div class="c3d-tooltip-head">
                <span class="c3d-glyph" :class="tooltip.glyphClass"></span>
                <div class="c3d-head-copy">
                    <strong>{{ tooltip.label }}</strong>
                    <div v-if="tooltip.siteName" class="c3d-site">{{ tooltip.siteName }}</div>
                </div>
                <div v-if="tooltip.status" class="c3d-status-badge" :style="{ color: tooltip.statusColor }">
                    <span class="badge-dot" :style="{ background: tooltip.statusColor }"></span>
                    {{ tooltip.status }}
                </div>
            </div>
            <div v-if="tooltip.signals.length" class="c3d-signal-row">
                <div v-for="signal in tooltip.signals" :key="signal.label" class="c3d-signal-pill">
                    <strong>{{ signal.value }}</strong>
                    <span>{{ signal.label }}</span>
                </div>
            </div>
            <div class="c3d-wave-row">
                <span v-for="n in 5" :key="n" class="wave-bar" :class="{ active: n <= tooltip.waveLevel }"></span>
            </div>
            <div v-if="tooltip.detail" class="c3d-detail">{{ tooltip.detail }}</div>
            <div v-for="line in tooltip.meta" :key="line" class="c3d-meta">{{ line }}</div>
        </div>

        <div class="c3d-hint-pill">
            <span class="hint-dot"></span>
            Hover de xem, giu chuot de xoay-pan, WASD de di chuyen
        </div>
    </div>
</template>

<script>
import { markRaw } from 'vue'
import * as THREE from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { MOUSE, TOUCH } from 'three'

const STATUS_COLORS = {
    Normal: 0x22c55e,
    Active: 0x38bdf8,
    Warning: 0xf59e0b,
    Alarm: 0xef4444,
    Offline: 0x64748b,
}

const LEVEL_COLORS = {
    Critical: 0xdc2626,
    Restricted: 0xf59e0b,
    Normal: 0x22c55e,
}

const OBJECT_LABELS = {
    Building: 'Toa nha van hanh',
    GateMarker: 'Cong / diem kiem soat',
    ParkingArea: 'Bai do xe',
    Path: 'Tuyen ket noi noi bo',
    Landmark: 'Moc canh quan',
}

export default {
    name: 'Campus3DCanvas',
    props: {
        sites: { type: Array, default: () => [] },
        gates: { type: Array, default: () => [] },
        recentEvents: { type: Array, default: () => [] },
        selectedGateId: { type: Number, default: null },
    },
    emits: ['select-gate', 'inspect-object', 'hover-object'],
    data() {
        return {
            initialized: false,
            tooltip: {
                visible: false,
                label: '',
                siteName: '',
                detail: '',
                meta: [],
                status: '',
                statusColor: '#fff',
                tone: 'tone-neutral',
                glyphClass: 'glyph-generic',
                signals: [],
                waveLevel: 2,
            },
            tooltipStyle: { top: '0px', left: '0px' },
            scene: null,
            worldGroup: null,
            camera: null,
            renderer: null,
            controls: null,
            raycaster: markRaw(new THREE.Raycaster()),
            mouse: markRaw(new THREE.Vector2()),
            gateMeshes: markRaw(new Map()),
            siteMeshes: markRaw(new Map()),
            objectRecords: markRaw([]),
            animFrameId: null,
            framedOnce: false,
            selectedSiteId: null,
            hoveredObject: null,
            moveState: { forward: false, backward: false, left: false, right: false },
        }
    },
    computed: {
        siteCards() {
            return this.sites.map((site) => {
                const objects = Array.isArray(site.objects) ? site.objects : []
                const buildingCount = objects.filter((obj) => obj.type === 'Building').length
                const criticalCount = objects.filter((obj) => this.parseProperties(obj.properties).level === 'Critical').length
                const gateNames = objects.filter((obj) => obj.type === 'GateMarker').map((obj) => obj.label || '')
                const warningGateCount = this.gates.filter((gate) =>
                    (gate.status === 'Warning' || gate.status === 'Offline') &&
                    gateNames.some((name) => name.includes(gate.gateName || ''))
                ).length

                return {
                    siteId: site.siteId,
                    name: site.name,
                    code: site.code,
                    buildingCount,
                    gateCount: gateNames.length,
                    criticalCount,
                    warningGateCount,
                }
            })
        },
    },
    watch: {
        gates: {
            deep: true,
            handler() {
                this.updateGateStatus()
            },
        },
        selectedGateId(val) {
            this.highlightGate(val)
            if (val) this.focusGate(val)
        },
        recentEvents: {
            deep: true,
            handler() {
                this.updateEventSignals()
            },
        },
        sites: {
            deep: true,
            handler() {
                if (this.initialized) {
                    this.rebuildWorld()
                }
            },
        },
    },
    methods: {
        initScene() {
            const container = this.$refs.containerRef
            if (!container) return

            const w = container.clientWidth
            const h = container.clientHeight || 680

            this.scene = markRaw(new THREE.Scene())
            this.scene.background = new THREE.Color(0x06111c)
            this.scene.fog = new THREE.FogExp2(0x06111c, 0.0016)

            this.worldGroup = markRaw(new THREE.Group())
            this.scene.add(this.worldGroup)

            this.camera = markRaw(new THREE.PerspectiveCamera(48, w / h, 0.1, 1600))
            this.camera.position.set(120, 95, 160)

            this.renderer = markRaw(new THREE.WebGLRenderer({ antialias: true, alpha: false }))
            this.renderer.setSize(w, h)
            this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
            this.renderer.shadowMap.enabled = true
            this.renderer.shadowMap.type = THREE.PCFShadowMap
            this.renderer.toneMapping = THREE.ACESFilmicToneMapping
            this.renderer.toneMappingExposure = 1.18
            this.renderer.outputColorSpace = THREE.SRGBColorSpace
            container.prepend(this.renderer.domElement)

            this.controls = markRaw(new OrbitControls(this.camera, this.renderer.domElement))
            this.controls.enableDamping = true
            this.controls.dampingFactor = 0.08
            this.controls.enablePan = true
            this.controls.screenSpacePanning = true
            this.controls.panSpeed = 1.15
            this.controls.zoomSpeed = 1.05
            this.controls.mouseButtons = {
                LEFT: MOUSE.ROTATE,
                MIDDLE: MOUSE.DOLLY,
                RIGHT: MOUSE.PAN,
            }
            this.controls.touches = {
                ONE: TOUCH.ROTATE,
                TWO: TOUCH.DOLLY_PAN,
            }
            this.controls.maxPolarAngle = Math.PI / 2.08
            this.controls.minDistance = 25
            this.controls.maxDistance = 420
            this.controls.target.set(0, 0, 0)

            this.addAtmosphere()
            this.addLights()
            this.addGround()
            this.rebuildWorld()

            this.initialized = true
            this.animate()

            window.addEventListener('resize', this.onResize)
        },
        addLights() {
            const ambient = new THREE.AmbientLight(0x9fb7d1, 0.52)
            this.scene.add(ambient)

            const hemi = new THREE.HemisphereLight(0x7dd3fc, 0x102235, 0.7)
            this.scene.add(hemi)

            const key = new THREE.DirectionalLight(0xfff3d6, 1.55)
            key.position.set(160, 220, 110)
            key.castShadow = true
            key.shadow.mapSize.width = 2048
            key.shadow.mapSize.height = 2048
            key.shadow.camera.left = -260
            key.shadow.camera.right = 260
            key.shadow.camera.top = 260
            key.shadow.camera.bottom = -260
            key.shadow.camera.near = 10
            key.shadow.camera.far = 420
            this.scene.add(key)

            const fill = new THREE.DirectionalLight(0x7c93ff, 0.32)
            fill.position.set(-100, 120, -90)
            this.scene.add(fill)

            const rim = new THREE.PointLight(0x22d3ee, 0.28, 500)
            rim.position.set(0, 120, 0)
            this.scene.add(rim)
        },
        addAtmosphere() {
            const sky = new THREE.Mesh(
                new THREE.SphereGeometry(720, 48, 48),
                new THREE.MeshBasicMaterial({
                    color: 0x0a1624,
                    side: THREE.BackSide,
                    transparent: true,
                    opacity: 0.96,
                })
            )
            this.scene.add(sky)

            const haze = new THREE.Mesh(
                new THREE.SphereGeometry(690, 36, 36),
                new THREE.MeshBasicMaterial({
                    color: 0x12324a,
                    side: THREE.BackSide,
                    transparent: true,
                    opacity: 0.12,
                })
            )
            haze.rotation.z = Math.PI / 5
            this.scene.add(haze)

            const starGroup = new THREE.Group()
            for (let i = 0; i < 140; i++) {
                const star = new THREE.Mesh(
                    new THREE.SphereGeometry(0.28 + Math.random() * 0.35, 6, 6),
                    new THREE.MeshBasicMaterial({
                        color: i % 7 === 0 ? 0x7dd3fc : 0xf8fafc,
                        transparent: true,
                        opacity: 0.55 + Math.random() * 0.35,
                    })
                )
                const radius = 420 + Math.random() * 120
                const theta = Math.random() * Math.PI * 2
                const phi = Math.random() * Math.PI * 0.36
                star.position.set(
                    Math.cos(theta) * Math.sin(phi) * radius,
                    210 + Math.cos(phi) * radius * 0.35,
                    Math.sin(theta) * Math.sin(phi) * radius
                )
                star.userData.isStar = true
                star.userData.phase = Math.random() * Math.PI * 2
                starGroup.add(star)
            }
            this.scene.add(starGroup)
        },
        addGround() {
            const ground = new THREE.Mesh(
                new THREE.PlaneGeometry(900, 900),
                new THREE.MeshStandardMaterial({ color: 0x0d2233, roughness: 1, metalness: 0 })
            )
            ground.rotation.x = -Math.PI / 2
            ground.position.y = -1.1
            ground.receiveShadow = true
            this.scene.add(ground)

            const tarmac = new THREE.Mesh(
                new THREE.RingGeometry(220, 420, 96),
                new THREE.MeshBasicMaterial({ color: 0x0b1722, transparent: true, opacity: 0.34, side: THREE.DoubleSide })
            )
            tarmac.rotation.x = -Math.PI / 2
            tarmac.position.y = -1.05
            this.scene.add(tarmac)

            const cityGlow = new THREE.Mesh(
                new THREE.CircleGeometry(300, 80),
                new THREE.MeshBasicMaterial({ color: 0x0f2a3c, transparent: true, opacity: 0.2, side: THREE.DoubleSide })
            )
            cityGlow.rotation.x = -Math.PI / 2
            cityGlow.position.y = -1.03
            this.scene.add(cityGlow)

            for (let i = 0; i < 42; i++) {
                const lightPatch = new THREE.Mesh(
                    new THREE.CircleGeometry(1.2 + Math.random() * 2.4, 18),
                    new THREE.MeshBasicMaterial({
                        color: i % 3 === 0 ? 0x38bdf8 : 0xf8fafc,
                        transparent: true,
                        opacity: 0.06 + Math.random() * 0.06,
                        side: THREE.DoubleSide,
                    })
                )
                lightPatch.rotation.x = -Math.PI / 2
                lightPatch.position.set(-250 + Math.random() * 500, -1.01, -250 + Math.random() * 500)
                this.scene.add(lightPatch)
            }

            const gridHelper = new THREE.GridHelper(800, 80, 0x173047, 0x0d2435)
            gridHelper.position.y = -1.02
            this.scene.add(gridHelper)

            const axesHelper = new THREE.AxesHelper(8)
            axesHelper.position.y = -0.95
            this.scene.add(axesHelper)
        },
        rebuildWorld() {
            if (!this.scene || !this.worldGroup) return

            this.disposeGroup(this.worldGroup)
            this.scene.remove(this.worldGroup)
            this.worldGroup = markRaw(new THREE.Group())
            this.scene.add(this.worldGroup)

            this.gateMeshes.clear()
            this.siteMeshes.clear()
            this.objectRecords.length = 0

            const layouts = this.computeSiteLayouts()
            layouts.forEach((layout) => this.addSiteBase(layout))
            this.addSiteConnections(layouts)

            for (const layout of layouts) {
                const objects = Array.isArray(layout.site.objects) ? layout.site.objects : []
                for (const obj of objects) {
                    this.buildObject(obj, layout)
                }
            }

            this.updateGateStatus()
            this.updateEventSignals()
            this.highlightGate(this.selectedGateId)

            const bounds = new THREE.Box3().setFromObject(this.worldGroup)
            if (bounds.isEmpty()) return

            if (!this.framedOnce) {
                this.frameBounds(bounds, true)
                this.framedOnce = true
            }
        },
        computeSiteLayouts() {
            const sites = Array.isArray(this.sites) ? this.sites : []
            const infos = sites.map((site) => {
                const objects = Array.isArray(site.objects) ? site.objects : []
                let minX = -30
                let maxX = 30
                let minZ = -20
                let maxZ = 20

                if (objects.length) {
                    minX = Math.min(...objects.map((obj) => Number(obj.posX || 0) - Number(obj.width || 6) / 2))
                    maxX = Math.max(...objects.map((obj) => Number(obj.posX || 0) + Number(obj.width || 6) / 2))
                    minZ = Math.min(...objects.map((obj) => Number(obj.posZ || 0) - Number(obj.length || 6) / 2))
                    maxZ = Math.max(...objects.map((obj) => Number(obj.posZ || 0) + Number(obj.length || 6) / 2))
                }

                return {
                    site,
                    minX,
                    maxX,
                    minZ,
                    maxZ,
                    spanX: maxX - minX + 44,
                    spanZ: maxZ - minZ + 54,
                }
            })

            const cols = infos.length <= 2 ? infos.length || 1 : 2
            const cellW = Math.max(...infos.map((info) => info.spanX), 120) + 52
            const cellZ = Math.max(...infos.map((info) => info.spanZ), 90) + 58
            const rows = Math.ceil((infos.length || 1) / cols)

            return infos.map((info, index) => {
                const col = index % cols
                const row = Math.floor(index / cols)
                const offsetX = (col - (cols - 1) / 2) * cellW
                const offsetZ = (row - (rows - 1) / 2) * cellZ

                return {
                    ...info,
                    offsetX,
                    offsetZ,
                    width: info.spanX,
                    depth: info.spanZ,
                    centerX: offsetX + (info.minX + info.maxX) / 2,
                    centerZ: offsetZ + (info.minZ + info.maxZ) / 2,
                }
            })
        },
        addSiteBase(layout) {
            const siteGroup = markRaw(new THREE.Group())
            siteGroup.position.set(layout.centerX, 0, layout.centerZ)
            siteGroup.userData = {
                objectType: 'Site',
                label: layout.site.name,
                siteName: layout.site.name,
                siteCode: layout.site.code,
                siteId: layout.site.siteId,
                metrics: {
                    buildings: Array.isArray(layout.site.objects) ? layout.site.objects.filter((obj) => obj.type === 'Building').length : 0,
                    gates: Array.isArray(layout.site.objects) ? layout.site.objects.filter((obj) => obj.type === 'GateMarker').length : 0,
                },
            }

            const pad = new THREE.Mesh(
                new THREE.BoxGeometry(layout.width, 1.1, layout.depth),
                new THREE.MeshStandardMaterial({
                    color: new THREE.Color().setHSL((layout.site.siteId * 0.17) % 1, 0.38, 0.2),
                    roughness: 0.98,
                    metalness: 0,
                })
            )
            pad.position.set(0, -0.55, 0)
            pad.receiveShadow = true
            siteGroup.add(pad)

            const foundationGlow = new THREE.Mesh(
                new THREE.PlaneGeometry(layout.width - 6, layout.depth - 6),
                new THREE.MeshBasicMaterial({ color: 0x0f2740, transparent: true, opacity: 0.18, side: THREE.DoubleSide })
            )
            foundationGlow.rotation.x = -Math.PI / 2
            foundationGlow.position.y = -0.47
            siteGroup.add(foundationGlow)

            const apron = new THREE.Mesh(
                new THREE.PlaneGeometry(layout.width + 18, layout.depth + 18),
                new THREE.MeshBasicMaterial({ color: 0x17324a, transparent: true, opacity: 0.18, side: THREE.DoubleSide })
            )
            apron.rotation.x = -Math.PI / 2
            apron.position.set(0, -0.48, 0)
            siteGroup.add(apron)

            const borderPoints = [
                new THREE.Vector3(-layout.width / 2, -0.44, -layout.depth / 2),
                new THREE.Vector3(layout.width / 2, -0.44, -layout.depth / 2),
                new THREE.Vector3(layout.width / 2, -0.44, layout.depth / 2),
                new THREE.Vector3(-layout.width / 2, -0.44, layout.depth / 2),
                new THREE.Vector3(-layout.width / 2, -0.44, -layout.depth / 2),
            ]
            const border = new THREE.Line(
                new THREE.BufferGeometry().setFromPoints(borderPoints),
                new THREE.LineBasicMaterial({ color: 0x7dd3fc, transparent: true, opacity: 0.32 })
            )
            siteGroup.add(border)

            this.addLabel(
                siteGroup,
                `${layout.site.code} ï ${layout.site.name}`,
                -layout.width / 2 + 18,
                8,
                -layout.depth / 2 + 10,
                '#dbeafe',
                15,
                2.6
            )

            this.addSiteBeacon(siteGroup, layout)
            this.addPerimeterLights(siteGroup, layout)
            this.addSiteRoadFrame(siteGroup, layout)
            this.worldGroup.add(siteGroup)
            this.siteMeshes.set(layout.site.siteId, siteGroup)
        },
        addSiteBeacon(parent, layout) {
            const mast = new THREE.Mesh(
                new THREE.CylinderGeometry(0.16, 0.22, 7.2, 8),
                new THREE.MeshStandardMaterial({ color: 0x94a3b8, roughness: 0.56, metalness: 0.24 })
            )
            mast.position.set(-layout.width / 2 + 5.5, 3.2, -layout.depth / 2 + 5.5)
            mast.castShadow = true
            parent.add(mast)

            const beacon = new THREE.Mesh(
                new THREE.SphereGeometry(0.72, 18, 18),
                new THREE.MeshBasicMaterial({ color: 0x7dd3fc, transparent: true, opacity: 0.88 })
            )
            beacon.position.set(mast.position.x, 7.2, mast.position.z)
            parent.add(beacon)

            const ring = new THREE.Mesh(
                new THREE.RingGeometry(0.95, 1.65, 32),
                new THREE.MeshBasicMaterial({ color: 0x38bdf8, transparent: true, opacity: 0.34, side: THREE.DoubleSide })
            )
            ring.rotation.x = -Math.PI / 2
            ring.position.set(mast.position.x, 0.06, mast.position.z)
            ring.userData.isSiteBeacon = true
            parent.add(ring)
        },
        addSiteRoadFrame(parent, layout) {
            const road = new THREE.Mesh(
                new THREE.RingGeometry(
                    Math.max(layout.width, layout.depth) * 0.38,
                    Math.max(layout.width, layout.depth) * 0.48,
                    72
                ),
                new THREE.MeshStandardMaterial({
                    color: 0x1e293b,
                    roughness: 0.95,
                    metalness: 0.02,
                    side: THREE.DoubleSide,
                })
            )
            road.rotation.x = -Math.PI / 2
            road.position.y = -0.46
            parent.add(road)
        },
        addSiteConnections(layouts) {
            if (!Array.isArray(layouts) || layouts.length < 2) return

            for (let i = 0; i < layouts.length - 1; i++) {
                const start = layouts[i]
                const end = layouts[i + 1]
                const points = [
                    new THREE.Vector3(start.centerX, 0.28, start.centerZ),
                    new THREE.Vector3((start.centerX + end.centerX) / 2, 0.4, (start.centerZ + end.centerZ) / 2),
                    new THREE.Vector3(end.centerX, 0.28, end.centerZ),
                ]

                const line = new THREE.Line(
                    new THREE.BufferGeometry().setFromPoints(points),
                    new THREE.LineBasicMaterial({ color: 0x38bdf8, transparent: true, opacity: 0.24 })
                )
                this.worldGroup.add(line)

                const pulse = new THREE.Mesh(
                    new THREE.SphereGeometry(0.7, 14, 14),
                    new THREE.MeshBasicMaterial({ color: 0x7dd3fc, transparent: true, opacity: 0.85 })
                )
                pulse.position.copy(points[1])
                pulse.userData.isFlowPulse = true
                pulse.userData.phase = i * 0.9
                this.worldGroup.add(pulse)
            }
        },
        addPerimeterLights(parent, layout) {
            const corners = [
                [-layout.width / 2 + 4, -layout.depth / 2 + 4],
                [layout.width / 2 - 4, -layout.depth / 2 + 4],
                [layout.width / 2 - 4, layout.depth / 2 - 4],
                [-layout.width / 2 + 4, layout.depth / 2 - 4],
            ]
            for (const [x, z] of corners) {
                const pole = new THREE.Mesh(
                    new THREE.CylinderGeometry(0.18, 0.22, 4.5, 6),
                    new THREE.MeshStandardMaterial({ color: 0x7c8fa5, roughness: 0.7 })
                )
                pole.position.set(x, 2.2, z)
                pole.castShadow = true
                parent.add(pole)

                const lamp = new THREE.Mesh(
                    new THREE.SphereGeometry(0.32, 10, 10),
                    new THREE.MeshBasicMaterial({ color: 0xf8fafc })
                )
                lamp.position.set(x, 4.6, z)
                parent.add(lamp)

                const pool = new THREE.Mesh(
                    new THREE.CircleGeometry(1.9, 20),
                    new THREE.MeshBasicMaterial({ color: 0x7dd3fc, transparent: true, opacity: 0.08, side: THREE.DoubleSide })
                )
                pool.rotation.x = -Math.PI / 2
                pool.position.set(x, -0.45, z)
                parent.add(pool)
            }
        },
        buildObject(obj, layout) {
            const type = obj.type
            const properties = this.parseProperties(obj.properties)
            const localX = Number(obj.posX || 0)
            const localZ = Number(obj.posZ || 0)
            const y = Number(obj.posY || 0)
            const w = Number(obj.width || 5)
            const l = Number(obj.length || 5)
            const h = Number(obj.height || 3)
            const rot = Number(obj.rotation || 0)
            const worldX = layout.offsetX + localX
            const worldZ = layout.offsetZ + localZ
            const color = obj.color ? parseInt(String(obj.color).replace('#', ''), 16) : 0x94a3b8
            const group = markRaw(new THREE.Group())

            group.position.set(worldX, y, worldZ)
            group.rotation.y = (rot * Math.PI) / 180
            group.userData = {
                ...obj,
                objectType: type,
                siteName: layout.site.name,
                siteCode: layout.site.code,
                siteId: layout.site.siteId,
                properties,
                worldX,
                worldZ,
                dimensions: { width: w, length: l, height: h },
            }

            if (type === 'Building') {
                const levelColor = LEVEL_COLORS[properties.level] || color
                const shell = new THREE.Mesh(
                    new THREE.BoxGeometry(w, h, l),
                    new THREE.MeshPhysicalMaterial({
                        color,
                        roughness: 0.36,
                        metalness: 0.2,
                        clearcoat: 0.18,
                        clearcoatRoughness: 0.28,
                        transparent: true,
                        opacity: 0.98,
                    })
                )
                shell.position.y = h / 2
                shell.castShadow = true
                shell.receiveShadow = true
                group.add(shell)

                const roof = new THREE.Mesh(
                    new THREE.BoxGeometry(w * 0.94, 0.45, l * 0.94),
                    new THREE.MeshStandardMaterial({ color: levelColor, roughness: 0.44, metalness: 0.14 })
                )
                roof.position.y = h + 0.22
                roof.castShadow = true
                group.add(roof)

                const plinth = new THREE.Mesh(
                    new THREE.BoxGeometry(w + 1.2, 0.45, l + 1.2),
                    new THREE.MeshStandardMaterial({ color: 0x334155, roughness: 0.95 })
                )
                plinth.position.y = 0.22
                plinth.receiveShadow = true
                group.add(plinth)

                const entranceCanopy = new THREE.Mesh(
                    new THREE.BoxGeometry(Math.max(3, w * 0.22), 0.2, 1.4),
                    new THREE.MeshPhysicalMaterial({
                        color: 0xe2e8f0,
                        roughness: 0.18,
                        metalness: 0.35,
                        clearcoat: 0.55,
                    })
                )
                entranceCanopy.position.set(0, 2.4, l / 2 + 0.75)
                entranceCanopy.castShadow = true
                group.add(entranceCanopy)

                this.addWindowBands(group, w, h, l, properties.level)
                this.addRoofEquipment(group, w, l, h, properties.level)

                const floors = obj.floors || 1
                const floorH = h / floors
                for (let f = 1; f < floors; f++) {
                    const line = new THREE.LineLoop(
                        new THREE.BufferGeometry().setFromPoints([
                            new THREE.Vector3(-w / 2, f * floorH, -l / 2),
                            new THREE.Vector3(w / 2, f * floorH, -l / 2),
                            new THREE.Vector3(w / 2, f * floorH, l / 2),
                            new THREE.Vector3(-w / 2, f * floorH, l / 2),
                        ]),
                        new THREE.LineBasicMaterial({ color: 0xe0f2fe, transparent: true, opacity: 0.16 })
                    )
                    group.add(line)
                }

                const badgeColor = properties.level === 'Critical' ? '#fecaca' : properties.level === 'Restricted' ? '#fde68a' : '#dbeafe'
                this.addLabel(group, obj.label || 'Building', 0, h + 2, 0, badgeColor)
            } else if (type === 'GateMarker') {
                const deck = new THREE.Mesh(
                    new THREE.BoxGeometry(w + 2.5, 0.35, l + 4),
                    new THREE.MeshStandardMaterial({ color: 0x1f2937, roughness: 0.95 })
                )
                deck.position.y = 0.18
                deck.receiveShadow = true
                group.add(deck)

                const archBase = new THREE.Mesh(
                    new THREE.BoxGeometry(w, 0.6, l),
                    new THREE.MeshStandardMaterial({ color, roughness: 0.52, metalness: 0.14 })
                )
                archBase.position.y = 0.3
                archBase.receiveShadow = true
                group.add(archBase)

                const pillarGeo = new THREE.BoxGeometry(0.6, h, 0.6)
                const pillarMat = new THREE.MeshStandardMaterial({ color, roughness: 0.58, metalness: 0.16 })
                for (const px of [-w / 2 + 0.6, w / 2 - 0.6]) {
                    const pillar = new THREE.Mesh(pillarGeo, pillarMat)
                    pillar.position.set(px, h / 2, 0)
                    pillar.castShadow = true
                    group.add(pillar)
                }

                const topBar = new THREE.Mesh(
                    new THREE.BoxGeometry(w - 1, 0.4, 0.6),
                    new THREE.MeshStandardMaterial({ color, roughness: 0.42, metalness: 0.2 })
                )
                topBar.position.set(0, h, 0)
                topBar.castShadow = true
                group.add(topBar)

                const barrier = new THREE.Mesh(
                    new THREE.BoxGeometry(Math.max(3.5, w * 0.78), 0.12, 0.12),
                    new THREE.MeshStandardMaterial({ color: 0xf8fafc, roughness: 0.4, metalness: 0.35 })
                )
                barrier.position.set(0.8, 1.05, -1.15)
                barrier.rotation.z = -Math.PI / 10
                barrier.userData.isGateBarrier = true
                group.add(barrier)

                const stripe = new THREE.Mesh(
                    new THREE.BoxGeometry(Math.max(3.4, w * 0.76), 0.05, 0.05),
                    new THREE.MeshBasicMaterial({ color: 0xef4444 })
                )
                stripe.position.copy(barrier.position)
                stripe.position.y += 0.08
                stripe.rotation.z = barrier.rotation.z
                group.add(stripe)

                const cameraPole = new THREE.Mesh(
                    new THREE.CylinderGeometry(0.1, 0.14, h + 0.8, 6),
                    new THREE.MeshStandardMaterial({ color: 0x94a3b8, roughness: 0.72 })
                )
                cameraPole.position.set(-w / 2 - 0.7, (h + 0.8) / 2, -0.6)
                cameraPole.castShadow = true
                group.add(cameraPole)

                const cameraHead = new THREE.Mesh(
                    new THREE.BoxGeometry(0.6, 0.3, 0.35),
                    new THREE.MeshStandardMaterial({ color: 0xe2e8f0, roughness: 0.35, metalness: 0.4 })
                )
                cameraHead.position.set(-w / 2 - 0.42, h + 0.35, -0.25)
                cameraHead.castShadow = true
                group.add(cameraHead)

                const glow = new THREE.Mesh(
                    new THREE.SphereGeometry(1.15, 14, 14),
                    new THREE.MeshBasicMaterial({ color: 0x22c55e, transparent: true, opacity: 0.55 })
                )
                glow.position.set(0, h + 1.05, 0)
                glow.userData.isGateGlow = true
                group.add(glow)

                const halo = new THREE.Mesh(
                    new THREE.RingGeometry(1.2, 1.85, 32),
                    new THREE.MeshBasicMaterial({ color: 0x22c55e, transparent: true, opacity: 0.4, side: THREE.DoubleSide })
                )
                halo.rotation.x = -Math.PI / 2
                halo.position.y = 0.14
                halo.userData.isGateHalo = true
                group.add(halo)

                const signal = new THREE.Mesh(
                    new THREE.RingGeometry(2.1, 3.4, 40),
                    new THREE.MeshBasicMaterial({ color: 0x38bdf8, transparent: true, opacity: 0.24, side: THREE.DoubleSide })
                )
                signal.rotation.x = -Math.PI / 2
                signal.position.y = 0.18
                signal.visible = false
                signal.userData.isEventSignal = true
                signal.userData.eventWeight = 0
                group.add(signal)

                this.gateMeshes.set(obj.id || obj.label, group)
                this.addLabel(group, obj.label || 'Gate', 0, h + 2.9, 0, '#ccfbf1')
            } else if (type === 'ParkingArea') {
                const area = new THREE.Mesh(
                    new THREE.PlaneGeometry(w, l),
                    new THREE.MeshStandardMaterial({
                        color,
                        roughness: 1,
                        metalness: 0,
                        transparent: true,
                        opacity: 0.88,
                        side: THREE.DoubleSide,
                    })
                )
                area.rotation.x = -Math.PI / 2
                area.position.y = -0.08
                area.receiveShadow = true
                group.add(area)

                const stripeCount = Math.max(3, Math.floor(l / 3))
                for (let s = 0; s < stripeCount; s++) {
                    const stripe = new THREE.Mesh(
                        new THREE.PlaneGeometry(w * 0.84, 0.12),
                        new THREE.MeshBasicMaterial({ color: 0xf8fafc, transparent: true, opacity: 0.18 })
                    )
                    stripe.rotation.x = -Math.PI / 2
                    stripe.position.set(0, -0.06, -l / 2 + (s + 0.5) * (l / stripeCount))
                    group.add(stripe)
                }

                this.addVehicles(group, w, l)
                const curb = new THREE.Mesh(
                    new THREE.BoxGeometry(w + 0.8, 0.16, l + 0.8),
                    new THREE.MeshStandardMaterial({ color: 0x475569, roughness: 0.92 })
                )
                curb.position.y = 0.02
                curb.receiveShadow = true
                group.add(curb)
                this.addLabel(group, obj.label || 'Parking', 0, 0.6, 0, '#cbd5e1')
            } else if (type === 'Path') {
                const path = new THREE.Mesh(
                    new THREE.PlaneGeometry(w, l),
                    new THREE.MeshStandardMaterial({
                        color: 0x334155,
                        roughness: 0.95,
                        metalness: 0.04,
                        side: THREE.DoubleSide,
                    })
                )
                path.rotation.x = -Math.PI / 2
                path.position.y = -0.03
                path.receiveShadow = true
                group.add(path)

                const shoulder = new THREE.Mesh(
                    new THREE.PlaneGeometry(w + 0.9, l + 0.9),
                    new THREE.MeshBasicMaterial({ color: 0x0f172a, transparent: true, opacity: 0.26, side: THREE.DoubleSide })
                )
                shoulder.rotation.x = -Math.PI / 2
                shoulder.position.y = -0.035
                group.add(shoulder)

                const centerLine = new THREE.Mesh(
                    new THREE.PlaneGeometry(Math.max(0.4, w * 0.22), l * 0.86),
                    new THREE.MeshBasicMaterial({ color: 0xe2e8f0, transparent: true, opacity: 0.12, side: THREE.DoubleSide })
                )
                centerLine.rotation.x = -Math.PI / 2
                centerLine.position.y = -0.02
                group.add(centerLine)

                const beacon = new THREE.Mesh(
                    new THREE.BoxGeometry(Math.max(0.12, w * 0.06), 0.02, 0.8),
                    new THREE.MeshBasicMaterial({ color: 0x7dd3fc, transparent: true, opacity: 0.12 })
                )
                beacon.position.set(0, 0.02, 0)
                group.add(beacon)
            } else if (type === 'Landmark') {
                const planter = new THREE.Mesh(
                    new THREE.CylinderGeometry(2.15, 2.35, 0.22, 20),
                    new THREE.MeshStandardMaterial({ color: 0x1f3b2b, roughness: 0.95 })
                )
                planter.position.y = 0.11
                planter.receiveShadow = true
                group.add(planter)

                const trunk = new THREE.Mesh(
                    new THREE.CylinderGeometry(0.34, 0.46, 1.8, 7),
                    new THREE.MeshStandardMaterial({ color: 0x7c4a2a, roughness: 0.9 })
                )
                trunk.position.y = 0.9
                trunk.castShadow = true
                group.add(trunk)

                const crown1 = new THREE.Mesh(
                    new THREE.SphereGeometry(1.7, 9, 9),
                    new THREE.MeshStandardMaterial({ color, roughness: 0.92 })
                )
                crown1.position.y = 2.6
                crown1.castShadow = true
                group.add(crown1)

                const crown2 = new THREE.Mesh(
                    new THREE.SphereGeometry(1.2, 9, 9),
                    new THREE.MeshStandardMaterial({ color: 0x166534, roughness: 0.92 })
                )
                crown2.position.set(0.7, 2.1, 0.5)
                crown2.castShadow = true
                group.add(crown2)
            }

            this.worldGroup.add(group)
            this.objectRecords.push(group)
        },
        addWindowBands(group, width, height, depth, level) {
            const floors = Math.max(1, Math.floor(height / 4))
            const tint = level === 'Critical' ? 0xfca5a5 : level === 'Restricted' ? 0xfde68a : 0x93c5fd
            const mat = new THREE.MeshPhysicalMaterial({
                color: tint,
                emissive: tint,
                emissiveIntensity: 0.08,
                roughness: 0.08,
                metalness: 0.12,
                transmission: 0.08,
                transparent: true,
                opacity: 0.42,
            })

            for (let floor = 0; floor < floors; floor++) {
                const y = 1.6 + floor * (height / floors)
                const front = new THREE.Mesh(new THREE.PlaneGeometry(width * 0.8, 0.55), mat)
                front.position.set(0, y, depth / 2 + 0.03)
                group.add(front)

                const back = front.clone()
                back.position.z = -depth / 2 - 0.03
                back.rotation.y = Math.PI
                group.add(back)

                const left = new THREE.Mesh(new THREE.PlaneGeometry(depth * 0.64, 0.52), mat)
                left.position.set(-width / 2 - 0.03, y, 0)
                left.rotation.y = Math.PI / 2
                group.add(left)

                const right = left.clone()
                right.position.x = width / 2 + 0.03
                right.rotation.y = -Math.PI / 2
                group.add(right)
            }
        },
        addRoofEquipment(group, width, depth, height, level) {
            const unitCount = level === 'Critical' ? 3 : 2
            for (let i = 0; i < unitCount; i++) {
                const unit = new THREE.Mesh(
                    new THREE.BoxGeometry(1.6, 0.9, 1.1),
                    new THREE.MeshStandardMaterial({ color: 0x475569, roughness: 0.72, metalness: 0.25 })
                )
                unit.position.set(-width / 4 + i * 2.1, height + 0.75, depth / 5)
                unit.castShadow = true
                group.add(unit)
            }
        },
        addVehicles(group, width, depth) {
            const slots = Math.max(1, Math.floor(depth / 5))
            const colors = [0x2563eb, 0xef4444, 0xeab308, 0x22c55e]
            for (let i = 0; i < Math.min(3, slots); i++) {
                const car = new THREE.Mesh(
                    new THREE.BoxGeometry(1.2, 0.6, 2.2),
                    new THREE.MeshStandardMaterial({
                        color: colors[i % colors.length],
                        roughness: 0.42,
                        metalness: 0.18,
                    })
                )
                car.position.set(width * 0.18 - i * 2.2, 0.3, -depth / 2 + (i + 1) * 3.2)
                car.castShadow = true
                group.add(car)
            }
        },
        addLabel(parent, text, x, y, z, color = '#fff', width = 512, scaleY = 2.5) {
            return
            const canvas = document.createElement('canvas')
            const ctx = canvas.getContext('2d')
            canvas.width = width
            canvas.height = 96
            ctx.fillStyle = 'rgba(6,17,28,0.78)'
            if (typeof ctx.roundRect === 'function') {
                ctx.roundRect(0, 0, width, 96, 18)
                ctx.fill()
            } else {
                ctx.fillRect(0, 0, width, 96)
            }
            ctx.strokeStyle = 'rgba(125,211,252,0.22)'
            ctx.lineWidth = 2
            ctx.strokeRect(6, 6, width - 12, 84)
            ctx.fillStyle = color
            ctx.font = '600 30px "Segoe UI", Arial, sans-serif'
            ctx.textAlign = 'center'
            ctx.textBaseline = 'middle'
            ctx.fillText(text, width / 2, 48)

            const texture = new THREE.CanvasTexture(canvas)
            texture.needsUpdate = true
            const sprite = new THREE.Sprite(
                new THREE.SpriteMaterial({ map: texture, transparent: true, depthTest: false })
            )
            sprite.scale.set(width / 42, scaleY, 1)
            sprite.position.set(x, y, z)
            sprite.userData.isLabel = true
            parent.add(sprite)
        },
        parseProperties(raw) {
            if (!raw) return {}
            if (typeof raw === 'object') return raw
            try {
                return JSON.parse(raw)
            } catch {
                return {}
            }
        },
        resolveGateInfo(data) {
            return this.gates.find((gate) => data.label?.includes(gate.gateName || ''))
        },
        buildTooltipPayload(data) {
            const gate = this.resolveGateInfo(data)
            const properties = data.properties || {}
            const dimensions = data.dimensions || {}
            const meta = []

            if (data.objectType === 'Site') {
                meta.push(`${data.metrics?.buildings || 0} toa nha ï ${data.metrics?.gates || 0} cong`)
            } else if (data.objectType === 'Building') {
                meta.push(`${data.floors || 1} tang ï ${Math.round(dimensions.width || 0)}m x ${Math.round(dimensions.length || 0)}m`)
                if (properties.zone) meta.push(`Zone: ${properties.zone}`)
                if (properties.level) meta.push(`Security level: ${properties.level}`)
            } else if (data.objectType === 'GateMarker') {
                if (gate) meta.push(`${gate.cameraCount || 0} camera ï ${gate.recentAccessCount || 0} s·ª± ki·ªán / 5 ph√∫t`)
                if (gate?.offlineCameraCount) meta.push(`${gate.offlineCameraCount} camera dang offline`)
                if (gate?.lastAccessAt) meta.push(`Truy cap cuoi: ${this.formatDateTime(gate.lastAccessAt)}`)
            } else if (data.objectType === 'ParkingArea') {
                meta.push(`${Math.round(dimensions.width || 0)}m x ${Math.round(dimensions.length || 0)}m`)
            } else if (data.objectType === 'Path') {
                meta.push(`Tuyen lien ket giua cac cum van hanh`)
            } else if (data.objectType === 'Landmark') {
                meta.push(`Moc canh quan / diem nhan nhan dien`)
            }

            return {
                visible: true,
                label: data.label || data.objectType,
                siteName: `${data.siteCode} ï ${data.siteName}`,
                detail: OBJECT_LABELS[data.objectType] || data.objectType,
                meta,
                status: gate ? `Tr?ng th·i c?ng: ${gate.status}` : '',
                statusColor: this.statusColorHex(gate?.status || 'Normal'),
            }
        },
        buildTooltipPayload(data) {
            const gate = this.resolveGateInfo(data)
            const properties = data.properties || {}
            const dimensions = data.dimensions || {}
            const meta = []
            const signals = []
            let tone = 'tone-neutral'
            let glyphClass = 'glyph-generic'
            let waveLevel = 2

            if (data.objectType === 'Site') {
                signals.push(
                    { label: 'BLD', value: data.metrics?.buildings || 0 },
                    { label: 'GATE', value: data.metrics?.gates || 0 }
                )
                meta.push('Cum van hanh / zone tong hop')
                glyphClass = 'glyph-site'
                waveLevel = Math.min(5, Math.max(2, data.metrics?.gates || 0))
            } else if (data.objectType === 'Building') {
                signals.push(
                    { label: 'FLR', value: data.floors || 1 },
                    { label: 'W', value: `${Math.round(dimensions.width || 0)}m` },
                    { label: 'L', value: `${Math.round(dimensions.length || 0)}m` }
                )
                if (properties.zone) meta.push(`Zone: ${properties.zone}`)
                if (properties.level) meta.push(`Security level: ${properties.level}`)
                glyphClass = 'glyph-building'
                tone = properties.level === 'Critical' ? 'tone-danger' : properties.level === 'Restricted' ? 'tone-warn' : 'tone-calm'
                waveLevel = properties.level === 'Critical' ? 5 : properties.level === 'Restricted' ? 4 : 3
            } else if (data.objectType === 'GateMarker') {
                if (gate) {
                    signals.push(
                        { label: 'CAM', value: gate.cameraCount || 0 },
                        { label: 'EVT', value: gate.recentAccessCount || 0 },
                        { label: 'OFF', value: gate.offlineCameraCount || 0 }
                    )
                }
                if (gate?.offlineCameraCount) meta.push(`${gate.offlineCameraCount} camera dang offline`)
                if (gate?.lastAccessAt) meta.push(`Truy cap cuoi: ${this.formatDateTime(gate.lastAccessAt)}`)
                glyphClass = 'glyph-gate'
                tone = gate?.status === 'Offline' || gate?.status === 'Alarm'
                    ? 'tone-danger'
                    : gate?.status === 'Warning'
                        ? 'tone-warn'
                        : gate?.status === 'Active'
                            ? 'tone-active'
                            : 'tone-calm'
                waveLevel = gate?.status === 'Offline' || gate?.status === 'Alarm'
                    ? 5
                    : gate?.status === 'Warning'
                        ? 4
                        : gate?.status === 'Active'
                            ? 3
                            : 2
            } else if (data.objectType === 'ParkingArea') {
                signals.push(
                    { label: 'W', value: `${Math.round(dimensions.width || 0)}m` },
                    { label: 'L', value: `${Math.round(dimensions.length || 0)}m` }
                )
                meta.push('Surface support / luu luong phuong tien')
                glyphClass = 'glyph-parking'
            } else if (data.objectType === 'Path') {
                meta.push('Tuyen lien ket giua cac cum van hanh')
                glyphClass = 'glyph-path'
                tone = 'tone-calm'
                waveLevel = 3
            } else if (data.objectType === 'Landmark') {
                meta.push('Moc canh quan / diem nhan nhan dien')
                glyphClass = 'glyph-landmark'
                waveLevel = 1
            }

            return {
                visible: true,
                label: data.label || data.objectType,
                siteName: `${data.siteCode} - ${data.siteName}`,
                detail: OBJECT_LABELS[data.objectType] || data.objectType,
                meta,
                status: gate ? gate.status : '',
                statusColor: this.statusColorHex(gate?.status || 'Normal'),
                tone,
                glyphClass,
                signals,
                waveLevel,
            }
        },
        statusColorHex(status) {
            const color = STATUS_COLORS[status] || STATUS_COLORS.Normal
            return `#${color.toString(16).padStart(6, '0')}`
        },
        updateGateStatus() {
            for (const [key, mesh] of this.gateMeshes) {
                const gateInfo = this.gates.find((g) => g.gateName && key.includes(g.gateName))
                const status = gateInfo?.status || 'Normal'
                const color = STATUS_COLORS[status] || STATUS_COLORS.Normal
                mesh.traverse((child) => {
                    if (!child.isMesh) return

                    if (child.userData.isGateGlow || child.userData.isGateHalo) {
                        child.material.color?.setHex(color)
                        child.material.opacity = status === 'Offline' ? 0.22 : status === 'Warning' ? 0.48 : status === 'Active' ? 0.54 : 0.36
                    }

                    if (child.userData.isGateBarrier) {
                        child.rotation.z = status === 'Active' ? -Math.PI / 3.5 : -Math.PI / 10
                    }
                })
            }
        },
        highlightGate(gateId) {
            for (const [key, mesh] of this.gateMeshes) {
                const isSelected = gateId && this.gates.some((g) => g.gateId === gateId && key.includes(g.gateName))
                mesh.traverse((child) => {
                    if (!child.isMesh || child.userData.isLabel) return
                    if (child.material.emissive) {
                        child.material.emissive.setHex(isSelected ? 0x1d4ed8 : 0x000000)
                        child.material.emissiveIntensity = isSelected ? 0.35 : 0
                    }
                })
            }
        },
        applyHoverState(target) {
            if (this.hoveredObject === target) return
            this.clearHoverState()
            this.hoveredObject = target
            if (!target) return

            target.traverse?.((child) => {
                if (!child.isMesh || child.userData.isLabel) return
                if (child.material?.emissive) {
                    child.userData.prevEmissive = child.material.emissive.getHex()
                    child.userData.prevEmissiveIntensity = child.material.emissiveIntensity || 0
                    child.material.emissive.setHex(0x7dd3fc)
                    child.material.emissiveIntensity = Math.max(child.userData.prevEmissiveIntensity || 0, 0.22)
                }
            })
        },
        clearHoverState() {
            if (!this.hoveredObject) return
            this.hoveredObject.traverse?.((child) => {
                if (!child.isMesh || child.userData.isLabel) return
                if (child.material?.emissive) {
                    child.material.emissive.setHex(child.userData.prevEmissive ?? 0x000000)
                    child.material.emissiveIntensity = child.userData.prevEmissiveIntensity ?? 0
                }
            })
            this.hoveredObject = null
            this.highlightGate(this.selectedGateId)
        },
        findObjectFromHit(object) {
            let current = object
            while (current && !current.userData?.objectType) {
                current = current.parent
            }
            return current?.userData?.objectType ? current : null
        },
        emitInspection(target) {
            const data = target.userData
            const gate = this.resolveGateInfo(data)
            this.$emit('inspect-object', {
                label: data.label || '',
                objectType: data.objectType,
                siteName: data.siteName,
                siteCode: data.siteCode,
                floors: data.floors || null,
                dimensions: data.dimensions || null,
                properties: data.properties || {},
                gate: gate || null,
            })
        },
        emitHover(target) {
            if (!target) {
                this.$emit('hover-object', null)
                return
            }

            const data = target.userData
            const gate = this.resolveGateInfo(data)
            this.$emit('hover-object', {
                label: data.label || '',
                objectType: data.objectType,
                siteName: data.siteName,
                siteCode: data.siteCode,
                floors: data.floors || null,
                dimensions: data.dimensions || null,
                properties: data.properties || {},
                metrics: data.metrics || null,
                gate: gate || null,
            })
        },
        focusGate(gateId) {
            const record = [...this.gateMeshes.entries()].find(([key]) =>
                this.gates.some((g) => g.gateId === gateId && key.includes(g.gateName))
            )
            if (!record) return
            this.selectedSiteId = record[1].userData.siteId || null
            const box = new THREE.Box3().setFromObject(record[1])
            if (!box.isEmpty()) this.frameBounds(box, false, 0.8)
        },
        focusSite(siteId, inspect = false) {
            const siteGroup = this.siteMeshes.get(siteId)
            if (!siteGroup) return
            this.selectedSiteId = siteId
            const box = new THREE.Box3().setFromObject(siteGroup)
            if (!box.isEmpty()) this.frameBounds(box, false, 0.95)
            if (inspect) this.emitInspection(siteGroup)
        },
        frameBounds(bounds, immediate = false, zoomFactor = 1.2) {
            const size = bounds.getSize(new THREE.Vector3())
            const center = bounds.getCenter(new THREE.Vector3())
            const maxSize = Math.max(size.x, size.y, size.z, 20)
            const distance = maxSize * zoomFactor + 26
            const nextPosition = new THREE.Vector3(center.x + distance, center.y + distance * 0.72, center.z + distance)

            if (immediate) {
                this.camera.position.copy(nextPosition)
                this.controls.target.copy(center)
            } else {
                this.camera.position.lerp(nextPosition, 0.72)
                this.controls.target.lerp(center, 0.72)
            }

            this.camera.near = 0.1
            this.camera.far = Math.max(1600, distance * 18)
            this.camera.updateProjectionMatrix()
            this.controls.update()
        },
        fitToContent() {
            if (!this.worldGroup) return
            this.selectedSiteId = null
            const bounds = new THREE.Box3().setFromObject(this.worldGroup)
            if (!bounds.isEmpty()) this.frameBounds(bounds, false, 1.28)
        },
        updateEventSignals() {
            for (const mesh of this.gateMeshes.values()) {
                mesh.traverse((child) => {
                    if (child.isMesh && child.userData.isEventSignal) {
                        child.visible = false
                        child.userData.eventWeight = 0
                    }
                })
            }

            const grouped = new Map()
            for (const event of this.recentEvents.slice(0, 12)) {
                if (!event?.gateName) continue
                grouped.set(event.gateName, (grouped.get(event.gateName) || 0) + 1)
            }

            for (const [key, mesh] of this.gateMeshes) {
                const gateInfo = this.gates.find((g) => g.gateName && key.includes(g.gateName))
                const eventCount = gateInfo?.gateName ? grouped.get(gateInfo.gateName) || 0 : 0
                mesh.traverse((child) => {
                    if (child.isMesh && child.userData.isEventSignal) {
                        child.visible = eventCount > 0
                        child.userData.eventWeight = eventCount
                        child.material.opacity = Math.min(0.2 + eventCount * 0.08, 0.72)
                    }
                })
            }
        },
        formatDateTime(value) {
            if (!value) return '--'
            return new Date(value).toLocaleString('vi-VN', {
                hour12: false,
                day: '2-digit',
                month: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
            })
        },
        handleKeyDown(event) {
            const key = String(event.key || '').toLowerCase()
            if (['w', 'arrowup'].includes(key)) this.moveState.forward = true
            if (['s', 'arrowdown'].includes(key)) this.moveState.backward = true
            if (['a', 'arrowleft'].includes(key)) this.moveState.left = true
            if (['d', 'arrowright'].includes(key)) this.moveState.right = true
        },
        handleKeyUp(event) {
            const key = String(event.key || '').toLowerCase()
            if (['w', 'arrowup'].includes(key)) this.moveState.forward = false
            if (['s', 'arrowdown'].includes(key)) this.moveState.backward = false
            if (['a', 'arrowleft'].includes(key)) this.moveState.left = false
            if (['d', 'arrowright'].includes(key)) this.moveState.right = false
        },
        updateKeyboardMove() {
            if (!this.camera || !this.controls) return

            const moveVector = new THREE.Vector3()
            const forward = new THREE.Vector3()
            this.camera.getWorldDirection(forward)
            forward.y = 0
            if (forward.lengthSq() === 0) return
            forward.normalize()

            const right = new THREE.Vector3().crossVectors(forward, new THREE.Vector3(0, 1, 0)).normalize()
            const step = 1.8

            if (this.moveState.forward) moveVector.add(forward)
            if (this.moveState.backward) moveVector.sub(forward)
            if (this.moveState.left) moveVector.sub(right)
            if (this.moveState.right) moveVector.add(right)

            if (moveVector.lengthSq() === 0) return
            moveVector.normalize().multiplyScalar(step)
            this.camera.position.add(moveVector)
            this.controls.target.add(moveVector)
        },
        animate() {
            this.animFrameId = requestAnimationFrame(() => this.animate())
            const now = performance.now() * 0.0025

            this.scene?.traverse((child) => {
                if (child.isMesh && child.userData.isStar) {
                    child.material.opacity = 0.42 + (Math.sin(now * 0.7 + child.userData.phase) + 1) * 0.18
                }
                if (child.isMesh && child.userData.isSiteBeacon) {
                    child.rotation.z += 0.008
                    child.material.opacity = 0.22 + (Math.sin(now * 1.6) + 1) * 0.08
                }
                if (child.isMesh && child.userData.isFlowPulse) {
                    const pulse = 1 + Math.sin(now * 2 + child.userData.phase) * 0.18
                    child.scale.setScalar(pulse)
                    child.material.opacity = 0.58 + (Math.sin(now * 2 + child.userData.phase) + 1) * 0.14
                }
            })

            for (const mesh of this.gateMeshes.values()) {
                mesh.traverse((child) => {
                    if (!child.isMesh) return
                    if (child.userData.isGateGlow) {
                        child.scale.setScalar(1 + Math.sin(now * 2.4) * 0.05)
                    }
                    if (child.userData.isGateHalo) {
                        child.rotation.z += 0.003
                    }
                    if (child.userData.isEventSignal && child.visible) {
                        const weight = child.userData.eventWeight || 1
                        const pulse = 1 + Math.sin(now * (1.8 + weight * 0.15)) * 0.16
                        child.scale.setScalar(pulse + weight * 0.02)
                    }
                })
            }

            this.updateKeyboardMove()
            this.controls.update()
            this.renderer.render(this.scene, this.camera)
        },
        onResize() {
            const container = this.$refs.containerRef
            if (!container || !this.camera || !this.renderer) return
            const w = container.clientWidth
            const h = container.clientHeight || 680
            this.camera.aspect = w / h
            this.camera.updateProjectionMatrix()
            this.renderer.setSize(w, h)
        },
        onMouseEnter() {
            if (!this.renderer) return
            this.renderer.domElement.addEventListener('pointermove', this.onPointerMove)
            this.renderer.domElement.addEventListener('click', this.onClick)
        },
        onMouseLeave() {
            if (!this.renderer) return
            this.renderer.domElement.removeEventListener('pointermove', this.onPointerMove)
            this.renderer.domElement.removeEventListener('click', this.onClick)
            this.tooltip.visible = false
            this.clearHoverState()
            this.emitHover(null)
        },
        onPointerMove(event) {
            if (!this.renderer || !this.camera) return
            const rect = this.renderer.domElement.getBoundingClientRect()
            this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1
            this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1

            this.raycaster.setFromCamera(this.mouse, this.camera)
            const intersects = this.raycaster.intersectObjects(this.worldGroup.children, true)
            const target = intersects.map((hit) => this.findObjectFromHit(hit.object)).find(Boolean)

            if (target) {
                this.applyHoverState(target)
                this.emitHover(target)
                this.tooltip = this.buildTooltipPayload(target.userData)
                this.tooltipStyle = {
                    top: `${event.clientY - this.$refs.containerRef.getBoundingClientRect().top + 12}px`,
                    left: `${event.clientX - this.$refs.containerRef.getBoundingClientRect().left + 12}px`,
                }
                this.renderer.domElement.style.cursor = 'pointer'
            } else {
                this.tooltip.visible = false
                this.clearHoverState()
                this.emitHover(null)
                this.renderer.domElement.style.cursor = 'default'
            }
        },
        onClick(event) {
            if (!this.renderer || !this.camera) return
            const rect = this.renderer.domElement.getBoundingClientRect()
            this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1
            this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1

            this.raycaster.setFromCamera(this.mouse, this.camera)
            const intersects = this.raycaster.intersectObjects(this.worldGroup.children, true)
            const target = intersects.map((hit) => this.findObjectFromHit(hit.object)).find(Boolean)
            if (!target) return

            this.emitInspection(target)
            const gate = this.resolveGateInfo(target.userData)
            if (target.userData.objectType === 'Site') {
                this.selectedSiteId = target.userData.siteId
                this.frameBounds(new THREE.Box3().setFromObject(target), false, 1.0)
            } else if (target.userData.objectType === 'GateMarker' && gate) {
                this.$emit('select-gate', gate.gateId)
            } else {
                this.frameBounds(new THREE.Box3().setFromObject(target), false, 0.9)
            }
        },
        disposeMaterial(material) {
            if (!material) return
            if (Array.isArray(material)) {
                material.forEach((item) => this.disposeMaterial(item))
                return
            }
            if (material.map) material.map.dispose()
            material.dispose?.()
        },
        disposeGroup(group) {
            group.traverse((child) => {
                if (child.geometry) child.geometry.dispose?.()
                if (child.material) this.disposeMaterial(child.material)
            })
            group.clear()
        },
        dispose() {
            if (this.animFrameId) cancelAnimationFrame(this.animFrameId)
            this.clearHoverState()
            if (this.worldGroup) this.disposeGroup(this.worldGroup)
            if (this.renderer) {
                this.renderer.dispose()
                this.renderer.domElement.remove()
            }
            window.removeEventListener('resize', this.onResize)
            window.removeEventListener('keydown', this.handleKeyDown)
            window.removeEventListener('keyup', this.handleKeyUp)
        },
    },
    mounted() {
        this.$nextTick(() => this.initScene())
        window.addEventListener('keydown', this.handleKeyDown)
        window.addEventListener('keyup', this.handleKeyUp)
    },
    beforeUnmount() {
        this.dispose()
    },
    expose: ['fitToContent', 'focusGate', 'focusSite'],
}
</script>

<style scoped>
.c3d-container {
    position: relative;
    width: 100%;
    min-height: 680px;
    height: 76vh;
    border-radius: 18px;
    overflow: hidden;
    background:
        radial-gradient(circle at top, rgba(56, 189, 248, 0.12), transparent 28%),
        linear-gradient(180deg, #091725 0%, #06111c 100%);
    border: 1px solid rgba(125, 211, 252, 0.16);
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
}

.c3d-loading {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #bae6fd;
    font-size: 18px;
    background: rgba(6, 17, 28, 0.92);
    z-index: 12;
}

.c3d-site-dock {
    position: absolute;
    top: 18px;
    left: 18px;
    z-index: 10;
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    width: min(360px, calc(100% - 36px));
}

.c3d-site-chip {
    appearance: none;
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 10px 12px;
    border-radius: 999px;
    background: rgba(8, 22, 36, 0.82);
    border: 1px solid rgba(125, 211, 252, 0.14);
    backdrop-filter: blur(8px);
    color: #dbeafe;
    cursor: pointer;
    font: inherit;
    transition: transform 0.18s ease, border-color 0.18s ease, background 0.18s ease;
}

.c3d-site-chip:hover,
.c3d-site-chip.active {
    transform: translateY(-1px);
    border-color: rgba(125, 211, 252, 0.38);
    background: rgba(10, 28, 44, 0.92);
}

.site-chip-dot {
    width: 10px;
    height: 10px;
    border-radius: 999px;
    background: #22c55e;
    box-shadow: 0 0 14px rgba(34, 197, 94, 0.5);
}

.site-chip-dot.warning {
    background: #f59e0b;
    box-shadow: 0 0 14px rgba(245, 158, 11, 0.5);
}

.site-chip-dot.critical {
    background: #ef4444;
    box-shadow: 0 0 14px rgba(239, 68, 68, 0.55);
}

.site-code {
    color: #7dd3fc !important;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}

.c3d-site-chip > span:not(.site-chip-dot):not(.site-code) {
    display: none;
}

.c3d-tooltip {
    position: absolute;
    z-index: 20;
    max-width: 300px;
    background: rgba(6, 17, 28, 0.94);
    border: 1px solid rgba(125, 211, 252, 0.18);
    border-radius: 16px;
    padding: 12px 14px;
    color: #e2e8f0;
    font-size: 13px;
    pointer-events: none;
    backdrop-filter: blur(12px);
    box-shadow: 0 14px 40px rgba(2, 8, 23, 0.36);
}

.c3d-tooltip-head {
    display: flex;
    align-items: flex-start;
    gap: 10px;
}

.c3d-head-copy {
    min-width: 0;
    flex: 1;
}

.c3d-tooltip strong {
    display: block;
    font-size: 14px;
    color: #fff;
}

.c3d-glyph {
    position: relative;
    width: 14px;
    height: 14px;
    margin-top: 3px;
    border-radius: 4px;
    background: #7dd3fc;
    box-shadow: 0 0 18px rgba(125, 211, 252, 0.35);
    flex-shrink: 0;
}

.glyph-site {
    border-radius: 999px;
    background: #38bdf8;
}

.glyph-building {
    border-radius: 3px;
    background: linear-gradient(180deg, #dbeafe 0%, #60a5fa 100%);
}

.glyph-gate {
    width: 16px;
    border-radius: 999px 999px 4px 4px;
    background: linear-gradient(180deg, #86efac 0%, #22c55e 100%);
}

.glyph-parking {
    background: linear-gradient(180deg, #e2e8f0 0%, #64748b 100%);
}

.glyph-path {
    width: 18px;
    height: 6px;
    border-radius: 999px;
    background: #38bdf8;
}

.glyph-landmark {
    transform: rotate(45deg);
    border-radius: 3px;
    background: linear-gradient(180deg, #fde68a 0%, #f59e0b 100%);
}

.c3d-status-badge {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 4px 8px;
    border-radius: 999px;
    background: rgba(15, 23, 42, 0.7);
    border: 1px solid rgba(148, 163, 184, 0.12);
    font-size: 11px;
    font-weight: 700;
    white-space: nowrap;
}

.badge-dot {
    width: 7px;
    height: 7px;
    border-radius: 999px;
}

.c3d-site {
    margin-top: 2px;
    color: #7dd3fc;
    font-size: 12px;
}

.c3d-detail {
    margin-top: 10px;
    color: #dbeafe;
    font-size: 12px;
}

.c3d-signal-row {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-top: 12px;
}

.c3d-signal-pill {
    display: grid;
    gap: 2px;
    min-width: 56px;
    padding: 8px 9px;
    border-radius: 12px;
    background: rgba(15, 23, 42, 0.58);
    border: 1px solid rgba(148, 163, 184, 0.1);
}

.c3d-signal-pill strong {
    font-size: 12px;
    line-height: 1;
}

.c3d-signal-pill span {
    color: #94a3b8;
    font-size: 10px;
    letter-spacing: 0.08em;
}

.c3d-wave-row {
    display: flex;
    align-items: flex-end;
    gap: 4px;
    height: 18px;
    margin-top: 10px;
}

.wave-bar {
    width: 8px;
    height: 6px;
    border-radius: 999px;
    background: rgba(71, 85, 105, 0.8);
}

.wave-bar:nth-child(2) {
    height: 9px;
}

.wave-bar:nth-child(3) {
    height: 12px;
}

.wave-bar:nth-child(4) {
    height: 15px;
}

.wave-bar:nth-child(5) {
    height: 18px;
}

.wave-bar.active {
    background: linear-gradient(180deg, #7dd3fc 0%, #38bdf8 100%);
    box-shadow: 0 0 12px rgba(56, 189, 248, 0.28);
}

.c3d-meta {
    margin-top: 4px;
    color: #94a3b8;
    font-size: 12px;
}

.tone-calm {
    border-color: rgba(56, 189, 248, 0.2);
}

.tone-active {
    border-color: rgba(34, 197, 94, 0.28);
}

.tone-warn {
    border-color: rgba(245, 158, 11, 0.28);
}

.tone-danger {
    border-color: rgba(239, 68, 68, 0.28);
}

.tone-neutral {
    border-color: rgba(125, 211, 252, 0.18);
}

.c3d-hint-pill {
    position: absolute;
    right: 18px;
    bottom: 16px;
    z-index: 10;
    display: inline-flex;
    align-items: center;
    gap: 10px;
    padding: 11px 14px;
    border-radius: 999px;
    background: rgba(6, 17, 28, 0.82);
    border: 1px solid rgba(125, 211, 252, 0.14);
    backdrop-filter: blur(8px);
    color: #cbd5e1;
    font-size: 12px;
}

.hint-dot {
    width: 9px;
    height: 9px;
    border-radius: 999px;
    background: #38bdf8;
    box-shadow: 0 0 16px rgba(56, 189, 248, 0.6);
}

@media (max-width: 900px) {
    .c3d-container {
        min-height: 620px;
        height: 72vh;
    }

    .c3d-site-dock {
        width: min(280px, calc(100% - 36px));
    }

    .c3d-hint-pill {
        left: 16px;
        right: 16px;
        justify-content: center;
    }
}
</style>

