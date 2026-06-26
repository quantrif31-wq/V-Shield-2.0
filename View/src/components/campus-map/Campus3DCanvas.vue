<template>
    <div ref="containerRef" class="c3d-container" @mouseenter="onMouseEnter" @mouseleave="onMouseLeave">
        <div v-if="!initialized" class="c3d-loading">Dang khoi tao 3D...</div>
        <div ref="tooltipRef" class="c3d-tooltip" :style="tooltipStyle" v-show="tooltip.visible">
            <strong>{{ tooltip.label }}</strong>
            <div v-if="tooltip.detail" class="c3d-detail">{{ tooltip.detail }}</div>
            <div v-if="tooltip.status" class="c3d-status" :style="{ color: tooltip.statusColor }">
                {{ tooltip.status }}
            </div>
        </div>
        <div class="c3d-legend">
            <div class="c3d-legend-item"><span class="dot" style="background:#2563eb"></span> Toa nha</div>
            <div class="c3d-legend-item"><span class="dot" style="background:#0f766e"></span> Cong</div>
            <div class="c3d-legend-item"><span class="dot" style="background:#22c55e"></span> Cay xanh</div>
            <div class="c3d-legend-item"><span class="dot" style="background:#64748b"></span> Bai do xe</div>
            <div class="c3d-legend-item"><span class="dot" style="background:#475569"></span> Loi di</div>
        </div>
    </div>
</template>

<script>
import { markRaw } from 'vue'
import * as THREE from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'

const MATERIALS = {
    Building: new THREE.MeshPhongMaterial({ color: 0x2563eb, transparent: true, opacity: 0.85 }),
    GateMarker: new THREE.MeshPhongMaterial({ color: 0x0f766e, emissive: 0x0f766e, emissiveIntensity: 0.3 }),
    ParkingArea: new THREE.MeshPhongMaterial({ color: 0x64748b, transparent: true, opacity: 0.5 }),
    Path: new THREE.MeshPhongMaterial({ color: 0x475569, transparent: true, opacity: 0.6 }),
    Landmark: new THREE.MeshPhongMaterial({ color: 0x22c55e }),
}

export default {
    name: 'Campus3DCanvas',
    props: {
        sites: { type: Array, default: () => [] },
        gates: { type: Array, default: () => [] },
        selectedGateId: { type: Number, default: null },
    },
    emits: ['select-gate'],
    data() {
        return {
            initialized: false,
            containerRef: null,
            tooltipRef: null,
            tooltip: { visible: false, label: '', detail: '', status: '', statusColor: '#fff' },
            tooltipStyle: { top: '0px', left: '0px' },
            scene: null,
            camera: null,
            renderer: null,
            controls: null,
            raycaster: markRaw(new THREE.Raycaster()),
            mouse: markRaw(new THREE.Vector2()),
            hoveredObject: null,
            gateMeshes: markRaw(new Map()),
            animFrameId: null,
        }
    },
    watch: {
        gates: {
            deep: true,
            handler() { this.updateGateStatus() },
        },
        selectedGateId(val) {
            this.highlightGate(val)
        },
    },
    methods: {
        initScene() {
            const container = this.$refs.containerRef
            if (!container) return

            const w = container.clientWidth
            const h = container.clientHeight || 600

            this.scene = markRaw(new THREE.Scene())
            this.scene.background = new THREE.Color(0x0a1929)

            this.camera = markRaw(new THREE.PerspectiveCamera(45, w / h, 1, 1000))
            this.camera.position.set(80, 70, 120)
            this.camera.lookAt(0, 0, 0)

            this.renderer = markRaw(new THREE.WebGLRenderer({ antialias: true }))
            this.renderer.setSize(w, h)
            this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
            this.renderer.shadowMap.enabled = true
            this.renderer.shadowMap.type = THREE.PCFShadowMap
            this.renderer.toneMapping = THREE.ACESFilmicToneMapping
            this.renderer.toneMappingExposure = 1.2
            container.prepend(this.renderer.domElement)

            this.controls = markRaw(new OrbitControls(this.camera, this.renderer.domElement))
            this.controls.enableDamping = true
            this.controls.dampingFactor = 0.08
            this.controls.maxPolarAngle = Math.PI / 2.2
            this.controls.minDistance = 20
            this.controls.maxDistance = 250
            this.controls.target.set(0, 0, 0)

            this.addLights()
            this.addGround()
            this.buildScene()

            this.initialized = true
            this.animate()

            window.addEventListener('resize', this.onResize)
        },
        addLights() {
            const ambient = new THREE.AmbientLight(0x404060, 0.5)
            this.scene.add(ambient)

            const hemi = new THREE.HemisphereLight(0x87ceeb, 0x362d1e, 0.6)
            this.scene.add(hemi)

            const dir = new THREE.DirectionalLight(0xffeedd, 1.2)
            dir.position.set(60, 100, 40)
            dir.castShadow = true
            dir.shadow.mapSize.width = 2048
            dir.shadow.mapSize.height = 2048
            const d = 200
            dir.shadow.camera.left = -d
            dir.shadow.camera.right = d
            dir.shadow.camera.top = d
            dir.shadow.camera.bottom = -d
            dir.shadow.camera.near = 1
            dir.shadow.camera.far = 200
            this.scene.add(dir)

            const fill = new THREE.DirectionalLight(0x8888ff, 0.3)
            fill.position.set(-40, 60, -60)
            this.scene.add(fill)
        },
        addGround() {
            const groundGeo = new THREE.PlaneGeometry(300, 300)
            const groundMat = new THREE.MeshPhongMaterial({
                color: 0x1a2a3a,
            })
            const ground = new THREE.Mesh(groundGeo, groundMat)
            ground.rotation.x = -Math.PI / 2
            ground.position.y = -0.5
            ground.receiveShadow = true
            this.scene.add(ground)

            const gridHelper = new THREE.GridHelper(300, 40, 0x2a4a6a, 0x1a3a5a)
            gridHelper.position.y = -0.45
            this.scene.add(gridHelper)

            for (let r = -140; r <= 140; r += 10) {
                for (let c = -140; c <= 140; c += 10) {
                    if (Math.random() < 0.02) {
                        const dot = new THREE.Mesh(
                            new THREE.CircleGeometry(0.15, 6),
                            new THREE.MeshBasicMaterial({ color: 0x3a6a9a, transparent: true, opacity: 0.4 })
                        )
                        dot.rotation.x = -Math.PI / 2
                        dot.position.set(r + Math.random() * 8 - 4, -0.48, c + Math.random() * 8 - 4)
                        this.scene.add(dot)
                    }
                }
            }

            const axesHelper = new THREE.AxesHelper(5)
            axesHelper.position.y = 0
            this.scene.add(axesHelper)
        },
        buildScene() {
            const colorMap = {
                'HN-HQ': 0x1e3a5f,
                'BN-FAC': 0x2d4a3a,
                'HP-LOG': 0x3a2d4a,
            }

            for (const site of this.sites) {
                const siteColor = colorMap[site.code] || 0x2a3a4a

                const perimeterGeo = new THREE.PlaneGeometry(120, 60)
                const perimeterMat = new THREE.MeshBasicMaterial({
                    color: siteColor,
                    transparent: true,
                    opacity: 0.1,
                    side: THREE.DoubleSide,
                })
                const perimeter = new THREE.Mesh(perimeterGeo, perimeterMat)
                perimeter.rotation.x = -Math.PI / 2
                perimeter.position.set(site.code === 'HN-HQ' ? 0 : site.code === 'BN-FAC' ? 120 : -130, -0.48, site.code === 'HN-HQ' ? 0 : 0)
                this.scene.add(perimeter)

                const border = new THREE.LineLoop(
                    new THREE.EdgesGeometry(new THREE.PlaneGeometry(120, 60)),
                    new THREE.LineBasicMaterial({ color: siteColor, transparent: true, opacity: 0.3 })
                )
                border.rotation.x = -Math.PI / 2
                border.position.copy(perimeter.position)
                border.position.y = -0.47
                this.scene.add(border)

                if (site.objects) {
                    for (const obj of site.objects) {
                        this.buildObject(obj)
                    }
                }
            }

            this.updateGateStatus()
        },
        buildObject(obj) {
            const type = obj.type
            const color = obj.color ? parseInt(obj.color.replace('#', ''), 16) : 0x888888
            const x = parseFloat(obj.posX)
            const z = parseFloat(obj.posZ)
            const y = parseFloat(obj.posY) || 0
            const w = parseFloat(obj.width) || 5
            const l = parseFloat(obj.length) || 5
            const h = parseFloat(obj.height) || 3
            const rot = parseFloat(obj.rotation) || 0

            const group = new THREE.Group()

            if (type === 'Building') {
                const blockGeo = new THREE.BoxGeometry(w, h, l)
                const blockMat = new THREE.MeshPhongMaterial({
                    color,
                    transparent: true,
                    opacity: 0.85,
                })
                const block = new THREE.Mesh(blockGeo, blockMat)
                block.position.y = h / 2
                block.castShadow = true
                block.receiveShadow = true
                group.add(block)

                const edges = new THREE.EdgesGeometry(blockGeo)
                const edgeMat = new THREE.LineBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.15 })
                const wireframe = new THREE.LineSegments(edges, edgeMat)
                wireframe.position.y = h / 2
                group.add(wireframe)

                const floors = obj.floors || 1
                const floorH = h / floors
                for (let f = 1; f < floors; f++) {
                    const lineGeo = new THREE.BufferGeometry().setFromPoints([
                        new THREE.Vector3(-w / 2, f * floorH, -l / 2),
                        new THREE.Vector3(w / 2, f * floorH, -l / 2),
                        new THREE.Vector3(w / 2, f * floorH, l / 2),
                        new THREE.Vector3(-w / 2, f * floorH, l / 2),
                        new THREE.Vector3(-w / 2, f * floorH, -l / 2),
                    ])
                    const line = new THREE.Line(lineGeo, new THREE.LineBasicMaterial({ color: 0x88ccff, transparent: true, opacity: 0.25 }))
                    group.add(line)
                }

                if (obj.label) {
                    this.addLabel(group, obj.label, 0, h + 1.5, 0, '#fff')
                }
            } else if (type === 'GateMarker') {
                const archBase = new THREE.Mesh(
                    new THREE.BoxGeometry(w, 0.5, l),
                    new THREE.MeshPhongMaterial({ color })
                )
                archBase.position.y = 0.25
                archBase.receiveShadow = true
                group.add(archBase)

                const pillarGeo = new THREE.BoxGeometry(0.5, h, 0.5)
                const pillarMat = new THREE.MeshPhongMaterial({ color })
                const p1 = new THREE.Mesh(pillarGeo, pillarMat)
                p1.position.set(-w / 2 + 0.5, h / 2, 0)
                group.add(p1)
                const p2 = new THREE.Mesh(pillarGeo, pillarMat)
                p2.position.set(w / 2 - 0.5, h / 2, 0)
                group.add(p2)

                const topBar = new THREE.Mesh(
                    new THREE.BoxGeometry(w - 1, 0.3, 0.5),
                    new THREE.MeshPhongMaterial({ color })
                )
                topBar.position.set(0, h, 0)
                group.add(topBar)

                const glow = new THREE.Mesh(
                    new THREE.SphereGeometry(1.2, 12, 12),
                    new THREE.MeshBasicMaterial({ color: 0x22c55e, transparent: true, opacity: 0.6 })
                )
                glow.position.set(0, h + 0.8, 0)
                glow.userData.isGateGlow = true
                group.add(glow)

                this.gateMeshes.set(obj.id || obj.label, group)

                if (obj.label) {
                    this.addLabel(group, obj.label, 0, h + 2.5, 0, '#0f766e')
                }
            } else if (type === 'ParkingArea') {
                const areaGeo = new THREE.PlaneGeometry(w, l)
                const areaMat = new THREE.MeshPhongMaterial({
                    color,
                    transparent: true,
                    opacity: 0.35,
                    side: THREE.DoubleSide,
                })
                const area = new THREE.Mesh(areaGeo, areaMat)
                area.rotation.x = -Math.PI / 2
                area.position.y = -0.45
                area.receiveShadow = true
                group.add(area)

                const stripeCount = Math.floor(l / 3)
                for (let s = 0; s < stripeCount; s++) {
                    const stripe = new THREE.Mesh(
                        new THREE.PlaneGeometry(w * 0.85, 0.1),
                        new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.15 })
                    )
                    stripe.rotation.x = -Math.PI / 2
                    stripe.position.set(0, -0.44, -l / 2 + (s + 0.5) * (l / stripeCount))
                    group.add(stripe)
                }

                if (obj.label) {
                    this.addLabel(group, obj.label, 0, 0.3, 0, '#94a3b8')
                }
            } else if (type === 'Path') {
                const pathGeo = new THREE.PlaneGeometry(w, l)
                const pathMat = new THREE.MeshPhongMaterial({
                    color,
                    transparent: true,
                    opacity: 0.5,
                    side: THREE.DoubleSide,
                })
                const path = new THREE.Mesh(pathGeo, pathMat)
                path.rotation.x = -Math.PI / 2
                path.position.y = -0.44
                path.receiveShadow = true
                group.add(path)

                const dashCount = Math.floor(l / 0.8)
                for (let d = 0; d < dashCount; d += 2) {
                    if (d >= dashCount) break
                    const dash = new THREE.Mesh(
                        new THREE.PlaneGeometry(w * 0.6, 0.2),
                        new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.1 })
                    )
                    dash.rotation.x = -Math.PI / 2
                    dash.position.set(0, -0.43, -l / 2 + (d + 0.5) * (l / dashCount))
                    group.add(dash)
                }
            } else if (type === 'Landmark') {
                const trunk = new THREE.Mesh(
                    new THREE.CylinderGeometry(0.3, 0.4, 1.5, 6),
                    new THREE.MeshPhongMaterial({ color: 0x8B4513 })
                )
                trunk.position.y = 0.75
                trunk.castShadow = true
                group.add(trunk)

                const crown = new THREE.Mesh(
                    new THREE.SphereGeometry(1.8, 8, 8),
                    new THREE.MeshPhongMaterial({ color })
                )
                crown.position.y = 2.5 + Math.random() * 1.5
                crown.castShadow = true
                group.add(crown)

                const crown2 = new THREE.Mesh(
                    new THREE.SphereGeometry(1.3, 8, 8),
                    new THREE.MeshPhongMaterial({ color: 0x166534 })
                )
                crown2.position.set(0.6, 2.0 + Math.random() * 1.0, 0.5)
                crown2.castShadow = true
                group.add(crown2)
            }

            group.position.set(x, y, z)
            group.rotation.y = (rot * Math.PI) / 180
            group.userData = { ...obj, objectType: type }

            this.scene.add(group)
        },
        addLabel(parent, text, x, y, z, color = '#fff') {
            const canvas = document.createElement('canvas')
            const ctx = canvas.getContext('2d')
            canvas.width = 512
            canvas.height = 96
            ctx.fillStyle = 'rgba(0,0,0,0.6)'
            if (typeof ctx.roundRect === 'function') {
                ctx.roundRect(0, 0, 512, 96, 16)
                ctx.fill()
            } else {
                ctx.fillRect(0, 0, 512, 96)
            }
            ctx.fillStyle = color
            ctx.font = 'bold 32px Arial, sans-serif'
            ctx.textAlign = 'center'
            ctx.textBaseline = 'middle'
            ctx.fillText(text, 256, 48)

            const texture = new THREE.CanvasTexture(canvas)
            texture.needsUpdate = true
            const spriteMat = new THREE.SpriteMaterial({ map: texture, transparent: true, depthTest: false })
            const sprite = new THREE.Sprite(spriteMat)
            sprite.scale.set(12, 2.5, 1)
            sprite.position.set(x, y, z)
            sprite.userData.isLabel = true
            parent.add(sprite)
        },
        updateGateStatus() {
            const statusColors = { Normal: 0x22c55e, Warning: 0xf59e0b, Alarm: 0xef4444, Offline: 0x64748b }
            for (const [key, mesh] of this.gateMeshes) {
                const gateInfo = this.gates.find(g => g.gateName && key.includes(g.gateName))
                const status = gateInfo?.status || 'Normal'
                const color = statusColors[status] || 0x22c55e
                mesh.traverse((child) => {
                    if (child.isMesh && child.userData.isGateGlow) {
                        child.material.color.setHex(color)
                        child.material.opacity = status === 'Alarm' ? 0.9 : 0.5
                    }
                })
            }
        },
        highlightGate(gateId) {
            for (const [key, mesh] of this.gateMeshes) {
                const isSelected = gateId && this.gates.some(g => g.gateId === gateId && key.includes(g.gateName))
                mesh.traverse((child) => {
                    if (child.isMesh && !child.userData.isLabel && !child.userData.isGateGlow) {
                        child.material.emissive?.setHex(isSelected ? 0x4444ff : 0x000000)
                        child.material.emissiveIntensity = isSelected ? 0.3 : 0
                    }
                })
            }
        },
        animate() {
            this.animFrameId = requestAnimationFrame(() => this.animate())
            this.controls.update()
            this.renderer.render(this.scene, this.camera)
        },
        onResize() {
            const container = this.$refs.containerRef
            if (!container) return
            const w = container.clientWidth
            const h = container.clientHeight || 600
            this.camera.aspect = w / h
            this.camera.updateProjectionMatrix()
            this.renderer.setSize(w, h)
        },
        onMouseEnter() {
            if (this.renderer) {
                this.renderer.domElement.addEventListener('pointermove', this.onPointerMove)
                this.renderer.domElement.addEventListener('click', this.onClick)
            }
        },
        onMouseLeave() {
            if (this.renderer) {
                this.renderer.domElement.removeEventListener('pointermove', this.onPointerMove)
                this.renderer.domElement.removeEventListener('click', this.onClick)
            }
            this.tooltip.visible = false
        },
        onPointerMove(event) {
            const rect = this.renderer.domElement.getBoundingClientRect()
            this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1
            this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1

            this.raycaster.setFromCamera(this.mouse, this.camera)
            const intersects = this.raycaster.intersectObjects(this.scene.children, true)

            let found = null
            for (const hit of intersects) {
                let obj = hit.object
                while (obj.parent && !obj.parent.userData?.objectType) {
                    obj = obj.parent
                }
                if (obj.parent?.userData?.objectType) {
                    found = obj.parent
                    break
                }
            }

            if (found) {
                const data = found.userData
                this.tooltip = {
                    visible: true,
                    label: data.label || '',
                    detail: data.objectType === 'Building' ? `${data.floors || 1} tang` :
                        data.objectType === 'GateMarker' ? 'Cong ra vao' :
                            data.objectType === 'ParkingArea' ? 'Bai do xe' :
                                data.objectType === 'Path' ? 'Loi di' :
                                    data.objectType === 'Landmark' ? 'Cay xanh' : '',
                    status: '',
                    statusColor: '#fff',
                }

                const gate = this.gates.find(g => data.label?.includes(g.gateName))
                if (gate) {
                    this.tooltip.status = `Trang thai: ${gate.status}`
                    this.tooltip.statusColor = gate.status === 'Normal' ? '#22c55e' : gate.status === 'Warning' ? '#f59e0b' : '#ef4444'
                }

                this.tooltipStyle = {
                    top: `${event.clientY - this.$refs.containerRef.getBoundingClientRect().top + 12}px`,
                    left: `${event.clientX - this.$refs.containerRef.getBoundingClientRect().left + 12}px`,
                }
                this.renderer.domElement.style.cursor = 'pointer'
            } else {
                this.tooltip.visible = false
                this.renderer.domElement.style.cursor = 'default'
            }
        },
        onClick(event) {
            const rect = this.renderer.domElement.getBoundingClientRect()
            this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1
            this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1

            this.raycaster.setFromCamera(this.mouse, this.camera)
            const intersects = this.raycaster.intersectObjects(this.scene.children, true)

            for (const hit of intersects) {
                let obj = hit.object
                while (obj.parent && !obj.parent.userData?.objectType) {
                    obj = obj.parent
                }
                if (obj.parent?.userData?.objectType === 'GateMarker') {
                    const gate = this.gates.find(g => obj.parent.userData.label?.includes(g.gateName))
                    if (gate) {
                        this.$emit('select-gate', gate.gateId)
                    }
                    return
                }
            }
        },
        dispose() {
            if (this.animFrameId) cancelAnimationFrame(this.animFrameId)
            if (this.renderer) {
                this.renderer.dispose()
                this.renderer.domElement.remove()
            }
            window.removeEventListener('resize', this.onResize)
        },
    },
    mounted() {
        this.$nextTick(() => this.initScene())
    },
    beforeUnmount() {
        this.dispose()
    },
}
</script>

<style scoped>
.c3d-container {
    position: relative;
    width: 100%;
    min-height: 600px;
    height: 70vh;
    border-radius: 14px;
    overflow: hidden;
    background: #0a1929;
    border: 1px solid var(--border-color, #1e3a5f);
}

.c3d-loading {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #88ccff;
    font-size: 18px;
    background: rgba(10, 25, 41, 0.9);
    z-index: 10;
}

.c3d-tooltip {
    position: absolute;
    z-index: 20;
    background: rgba(10, 25, 41, 0.92);
    border: 1px solid #2a4a6a;
    border-radius: 10px;
    padding: 8px 14px;
    color: #e2e8f0;
    font-size: 13px;
    pointer-events: none;
    backdrop-filter: blur(4px);
    min-width: 120px;
}

.c3d-tooltip strong {
    display: block;
    font-size: 14px;
    color: #fff;
    margin-bottom: 2px;
}

.c3d-detail {
    color: #94a3b8;
    font-size: 12px;
}

.c3d-status {
    font-size: 12px;
    margin-top: 2px;
    font-weight: 600;
}

.c3d-legend {
    position: absolute;
    bottom: 16px;
    right: 16px;
    z-index: 15;
    background: rgba(10, 25, 41, 0.85);
    border: 1px solid #2a4a6a;
    border-radius: 10px;
    padding: 10px 14px;
    display: grid;
    gap: 4px;
    backdrop-filter: blur(4px);
}

.c3d-legend-item {
    display: flex;
    align-items: center;
    gap: 8px;
    color: #94a3b8;
    font-size: 12px;
}

.dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
}
</style>
