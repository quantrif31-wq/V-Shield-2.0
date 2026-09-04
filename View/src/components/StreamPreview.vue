<template>
    <div 
        class="stream-preview" 
        :class="[`mode-${playerMode}`, { loading: isLoading }]"
        ref="containerRef"
        @dblclick="handleDoubleClick"
        @contextmenu="handleRightClick"
        :style="{ cursor: isStreamReady ? 'pointer' : 'default' }"
    >
        <img
            v-if="playerMode === 'image'"
            :src="resolvedUrl"
            :alt="label"
            class="stream-media"
            @load="handleReady"
            @error="handleError('Không tải được ảnh preview từ camera.')"
        />

        <video
            v-else-if="playerMode === 'video' || playerMode === 'hls'"
            ref="videoRef"
            class="stream-media"
            :controls="showControls"
            autoplay
            muted
            playsinline
            disablepictureinpicture
            controlslist="nodownload noplaybackrate noremoteplayback"
            @loadeddata="handleReady"
            @loadedmetadata="syncDvrPosition"
            @durationchange="syncDvrPosition"
            @timeupdate="syncDvrPosition"
            @ended="handleEnded"
            @error="handleError('Không tải được luồng video trên trình duyệt.')"
        ></video>

        <div v-else-if="playerMode === 'rtsp'" class="preview-message">
            <strong>RTSP đã sẵn sàng cho AI</strong>
            <p>Trình duyệt không phát trực tiếp RTSP. Hãy thêm Preview URL dạng HLS/WebRTC/MJPEG để xem trên web.</p>
        </div>

        <div v-else-if="playerMode === 'empty'" class="preview-message">
            <strong>Chưa có preview</strong>
            <p>Hãy nhập URL xem trên web trong phần quản lý camera để hiển thị tại đây.</p>
        </div>

        <div v-else class="preview-message">
            <strong>Không hỗ trợ xem trực tiếp</strong>
            <p>{{ errorMessage || 'URL này cần một bridge để chuyển sang định dạng trình duyệt phát được.' }}</p>
        </div>

        <div v-if="dvrMode && playerMode === 'hls'" class="dvr-transport" :class="{ pending: !dvrTimelineReady }">
            <button type="button" :disabled="!dvrTimelineReady" @click="seekDvr(0)">Đầu ngày</button>
            <input
                class="dvr-scrubber"
                type="range"
                min="0"
                :max="Math.max(dvrDuration, 1)"
                step="1"
                :value="dvrPosition"
                :disabled="!dvrTimelineReady"
                aria-label="Tua DVR"
                @input="seekDvr(Number($event.target.value))"
            />
            <span>{{ dvrTimelineReady ? `${formatDvrTime(dvrPosition)} / ${formatDvrTime(dvrDuration)}` : 'Đang nạp timeline…' }}</span>
            <button type="button" :disabled="!dvrTimelineReady" @click="goLive">Trực tiếp</button>
        </div>

        <div v-if="isLoading" class="preview-overlay">Đang tải stream...</div>
        <div v-else-if="errorMessage && playerMode !== 'unsupported'" class="preview-overlay error">{{ errorMessage }}</div>
    </div>
</template>

<script setup>
import HlsLibrary from 'hls.js'
import { computed, nextTick, onActivated, onBeforeUnmount, onDeactivated, ref, watch } from 'vue'
import { isBrowserVideoCameraUrl, isHlsCameraUrl, isHttpCameraUrl, isRtspCameraUrl } from '../utils/cameraNetwork'

const props = defineProps({
    url: {
        type: String,
        default: '',
    },
    label: {
        type: String,
        default: 'Camera preview',
    },
    showControls: {
        type: Boolean,
        default: false,
    },
    dvrMode: {
        type: Boolean,
        default: false,
    },
})

const emit = defineEmits(['ready', 'error'])

const videoRef = ref(null)
const containerRef = ref(null)
const errorMessage = ref('')
const isLoading = ref(false)
const dvrDuration = ref(0)
const dvrPosition = ref(0)
let hlsInstance = null
let dvrTimelineRequest = 0
let dvrTimelineTimer = null
let dvrLiveStartPending = false
const DVR_LIVE_EDGE_MARGIN_SECONDS = 2

const dvrTimelineReady = computed(() => dvrDuration.value > 0)

// Stream is considered ready when it has loaded successfully without errors
const isStreamReady = computed(() => {
    const mode = playerMode.value
    const hasPlayableMode = mode === 'video' || mode === 'hls' || mode === 'image'
    return hasPlayableMode && !errorMessage.value && !isLoading.value
})

const handleDoubleClick = async () => {
    try {
        if (!document.fullscreenElement) {
            // Only allow entering fullscreen when the camera stream is ready
            if (!isStreamReady.value) return

            const el = containerRef.value
            if (!el) return
            
            if (el.requestFullscreen) {
                await el.requestFullscreen()
            } else if (el.webkitRequestFullscreen) {
                await el.webkitRequestFullscreen()
            } else if (el.msRequestFullscreen) {
                await el.msRequestFullscreen()
            }
        } else {
            if (document.exitFullscreen) {
                await document.exitFullscreen()
            } else if (document.webkitExitFullscreen) {
                await document.webkitExitFullscreen()
            } else if (document.msExitFullscreen) {
                await document.msExitFullscreen()
            }
        }
    } catch (error) {
        console.error('Lỗi khi chuyển đổi toàn màn hình:', error)
    }
}

const handleRightClick = async (event) => {
    if (document.fullscreenElement) {
        event.preventDefault()
        try {
            if (document.exitFullscreen) {
                await document.exitFullscreen()
            } else if (document.webkitExitFullscreen) {
                await document.webkitExitFullscreen()
            } else if (document.msExitFullscreen) {
                await document.msExitFullscreen()
            }
        } catch (error) {
            console.error('Lỗi khi thoát toàn màn hình:', error)
        }
    }
}

const resolvedUrl = computed(() => String(props.url || '').trim())

const playerMode = computed(() => {
    const url = resolvedUrl.value
    if (!url) return 'empty'
    if (isHlsCameraUrl(url)) return 'hls'
    if (isBrowserVideoCameraUrl(url)) return 'video'
    if (isRtspCameraUrl(url)) return 'rtsp'
    if (isHttpCameraUrl(url)) return 'image'
    return 'unsupported'
})

const resetVideoElement = () => {
    const element = videoRef.value
    if (!element) return
    element.pause()
    element.removeAttribute('src')
    element.load()
}

const destroyHls = () => {
    if (hlsInstance) {
        hlsInstance.destroy()
        hlsInstance = null
    }
}

const resetState = () => {
    dvrTimelineRequest += 1
    if (dvrTimelineTimer) {
        clearInterval(dvrTimelineTimer)
        dvrTimelineTimer = null
    }
    destroyHls()
    resetVideoElement()
    errorMessage.value = ''
    isLoading.value = false
    dvrDuration.value = 0
    dvrPosition.value = 0
    dvrLiveStartPending = false
}

const updateDvrTimeline = (duration) => {
    if (!props.dvrMode) return
    const safeDuration = Number(duration)
    if (Number.isFinite(safeDuration) && safeDuration > 0) {
        dvrDuration.value = Math.floor(safeDuration)
    }
}

const loadDvrPlaylistTimeline = async () => {
    if (!props.dvrMode || !resolvedUrl.value) return

    const requestId = ++dvrTimelineRequest
    try {
        // HLS event playlists are open-ended while recording. Some hls.js
        // versions therefore omit media.duration, although all seekable
        // segments are already listed in the manifest. Sum EXTINF ourselves.
        const response = await fetch(resolvedUrl.value, { cache: 'no-store' })
        if (!response.ok) return
        const manifest = await response.text()
        const duration = manifest
            .split(/\r?\n/)
            .filter((line) => line.startsWith('#EXTINF:'))
            .reduce((total, line) => total + (Number.parseFloat(line.slice(8)) || 0), 0)

        if (requestId === dvrTimelineRequest) {
            updateDvrTimeline(duration)
            void startAtDvrLiveEdge()
        }
    } catch {
        // hls.js can still supply the timeline through LEVEL_LOADED.
    }
}

const startDvrTimelineRefresh = () => {
    if (!props.dvrMode || dvrTimelineTimer) return
    dvrTimelineTimer = setInterval(() => {
        void loadDvrPlaylistTimeline()
    }, 3000)
}

const syncDvrPosition = () => {
    if (!props.dvrMode || !videoRef.value) return
    dvrPosition.value = Math.min(Math.floor(videoRef.value.currentTime || 0), dvrDuration.value || Number.MAX_SAFE_INTEGER)
    updateDvrTimeline(videoRef.value.duration)
}

const seekDvr = async (position) => {
    const element = videoRef.value
    if (!element || !dvrDuration.value) return
    element.currentTime = Math.min(Math.max(Number(position) || 0, 0), dvrDuration.value)
    dvrPosition.value = Math.floor(element.currentTime)
    try { await element.play() } catch { /* controls remain usable */ }
}

const goLive = async () => {
    const element = videoRef.value
    if (!element) return

    // The end of an open EVENT playlist is not a playable instant yet. Stay a
    // small distance behind it so the browser has a complete fragment to play.
    const seekableEnd = element.seekable?.length
        ? element.seekable.end(element.seekable.length - 1)
        : 0
    const timelineEnd = Math.max(seekableEnd, dvrDuration.value, Number(element.duration) || 0)
    const livePosition = Math.max(0, timelineEnd - DVR_LIVE_EDGE_MARGIN_SECONDS)
    element.currentTime = livePosition
    dvrPosition.value = Math.floor(livePosition)
    try { await element.play() } catch { /* controls remain usable */ }
}

// The first HLS callback can arrive before the browser exposes a seekable
// range.  Starting there puts an EVENT DVR at second 0 instead of at the
// current camera frame.  Wait until either the manifest duration or browser
// range is known, then make one (and only one) initial jump to live.
const startAtDvrLiveEdge = async () => {
    if (!dvrLiveStartPending) return
    const element = videoRef.value
    const hasSeekableRange = Boolean(element?.seekable?.length)
    const hasTimeline = dvrDuration.value > 0 || Number(element?.duration) > 0
    if (!hasSeekableRange && !hasTimeline) return

    dvrLiveStartPending = false
    await goLive()
}

const formatDvrTime = (seconds) => {
    const safe = Math.max(0, Math.floor(Number(seconds) || 0))
    const hours = Math.floor(safe / 3600)
    const minutes = Math.floor((safe % 3600) / 60)
    const secs = safe % 60
    return hours > 0
        ? `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
        : `${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
}

const handleReady = () => {
    errorMessage.value = ''
    isLoading.value = false
    emit('ready')
}

const handleError = (message) => {
    errorMessage.value = message
    isLoading.value = false
    emit('error', message)
}

const handleEnded = async () => {
    if (playerMode.value !== 'video' || !resolvedUrl.value) return
    await attachVideoPreview()
}

const ensureHlsLibrary = async () => {
    // The player is bundled with V-Shield. An external CDN is blocked by the
    // application's CSP and makes DVR playback depend on the internet.
    return window.Hls || HlsLibrary
}

const attachVideoPreview = async () => {
    await nextTick()
    const element = videoRef.value
    if (!element) return

    resetVideoElement()
    errorMessage.value = ''
    isLoading.value = true
    element.src = resolvedUrl.value
    try {
        await element.play()
    } catch {
        // Browser autoplay can fail silently; controls remain available.
    }
}

const attachHlsPreview = async () => {
    await nextTick()
    const element = videoRef.value
    if (!element) return

    resetVideoElement()
    errorMessage.value = ''
    isLoading.value = true
    dvrLiveStartPending = Boolean(props.dvrMode)
    void loadDvrPlaylistTimeline()
    startDvrTimelineRefresh()

    if (element.canPlayType('application/vnd.apple.mpegurl')) {
        element.addEventListener('loadedmetadata', () => { void startAtDvrLiveEdge() }, { once: true })
        element.src = resolvedUrl.value
        try {
            await element.play()
        } catch {
            // Browser autoplay can fail silently; controls remain available.
        }
        return
    }

    try {
        const Hls = await ensureHlsLibrary()
        if (!Hls?.isSupported?.()) {
            handleError('Trình duyệt này không hỗ trợ HLS player.')
            return
        }

        hlsInstance = new Hls({
            enableWorker: true,
            // This is a day-long DVR playlist, not an ultra-low-latency call.
            // Preserve a finite timeline so the browser can seek backwards.
            lowLatencyMode: false,
            liveDurationInfinity: false,
            backBufferLength: 60,
            startPosition: -1,
        })

        hlsInstance.on(Hls.Events.ERROR, (_, data) => {
            if (data?.fatal) {
                handleError('Không mở được luồng HLS từ camera.')
            }
        })

        hlsInstance.loadSource(resolvedUrl.value)
        hlsInstance.attachMedia(element)
        if (Hls.Events.LEVEL_LOADED) {
            hlsInstance.on(Hls.Events.LEVEL_LOADED, (_, data) => {
                updateDvrTimeline(data?.details?.totalduration)
                void startAtDvrLiveEdge()
            })
        }
        hlsInstance.on(Hls.Events.MANIFEST_PARSED, async () => {
            updateDvrTimeline(hlsInstance?.levels?.[0]?.details?.totalduration)
            await startAtDvrLiveEdge()
        })
    } catch (error) {
        handleError(error instanceof Error ? error.message : 'Không tải được HLS player.')
    }
}

const attachPreview = async () => {
    resetState()

    if (playerMode.value === 'video') {
        await attachVideoPreview()
        return
    }

    if (playerMode.value === 'hls') {
        await attachHlsPreview()
    }
}

watch(
    () => resolvedUrl.value,
    async () => {
        await attachPreview()
    },
    { immediate: true }
)

onBeforeUnmount(() => {
    resetState()
})

onDeactivated(() => {
    // Pause video when component is deactivated via keep-alive
    const element = videoRef.value
    if (element) {
        try { element.pause() } catch { /* ignore */ }
    }
})

onActivated(() => {
    // Resume video when component is re-activated via keep-alive
    const element = videoRef.value
    if (element && resolvedUrl.value) {
        try { element.play() } catch { /* ignore */ }
    }
})
</script>

<style scoped>
.stream-preview {
    position: relative;
    min-height: 220px;
    border-radius: 18px;
    overflow: hidden;
    background:
        radial-gradient(circle at top left, rgba(31, 94, 143, 0.22), transparent 48%),
        linear-gradient(140deg, #0f172a, #12263f 60%, #17324d);
    border: 1px solid rgba(255, 255, 255, 0.08);
}

.stream-media {
    display: block;
    width: 100%;
    aspect-ratio: 16 / 9;
    object-fit: contain;
    object-position: center;
    background: rgba(15, 23, 42, 0.92);
}

.preview-message {
    display: grid;
    gap: 10px;
    min-height: 220px;
    align-content: center;
    padding: 22px;
    color: rgba(226, 232, 240, 0.94);
}

.preview-message strong {
    font-size: 1rem;
}

.preview-message p {
    color: rgba(191, 209, 229, 0.88);
    line-height: 1.5;
}

.preview-overlay {
    position: absolute;
    right: 12px;
    bottom: 12px;
    padding: 8px 12px;
    border-radius: 999px;
    background: rgba(15, 23, 42, 0.7);
    color: #e2e8f0;
    font-size: 0.78rem;
    font-weight: 700;
    backdrop-filter: blur(8px);
}

.preview-overlay.error {
    left: 12px;
    right: 12px;
    border-radius: 14px;
    background: rgba(127, 29, 29, 0.88);
    color: #fee2e2;
}

.dvr-transport {
    position: absolute;
    right: 12px;
    bottom: 54px;
    left: 12px;
    z-index: 3;
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 12px;
    border: 1px solid rgba(20, 184, 166, 0.45);
    border-radius: 12px;
    background: #0f1d2d;
    color: #e2e8f0;
    font-size: 12px;
    font-weight: 700;
}

.dvr-transport button {
    border: 1px solid rgba(148, 163, 184, 0.65);
    border-radius: 8px;
    padding: 5px 8px;
    background: rgba(15, 118, 110, 0.85);
    color: #fff;
    cursor: pointer;
    font: inherit;
    white-space: nowrap;
}

.dvr-transport button:disabled,
.dvr-scrubber:disabled { opacity: 0.55; cursor: wait; }
.dvr-transport.pending { border-color: rgba(148, 163, 184, 0.5); }

.dvr-scrubber { flex: 1; min-width: 80px; accent-color: #22c55e; }

@media (max-width: 640px) {
    .dvr-transport {
        right: 8px;
        bottom: 48px;
        left: 8px;
        gap: 6px;
        padding: 8px;
    }

    .dvr-transport span { font-size: 10px; }
    .dvr-transport button { padding: 4px 6px; }
}

/* Fullscreen mode */
.stream-preview:fullscreen {
    border-radius: 0;
    width: 100vw;
    height: 100vh;
    border: none;
    background: #000;
}

.stream-preview:fullscreen .stream-media {
    width: 100%;
    height: 100%;
    object-fit: contain;
    aspect-ratio: auto;
    background: #000;
}
</style>
