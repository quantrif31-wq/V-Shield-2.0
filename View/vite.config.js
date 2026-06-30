import { defineConfig, loadEnv } from "vite"
import vue from "@vitejs/plugin-vue"

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "")
  const apiProxyTarget = env.VITE_DEV_PROXY_TARGET || "http://127.0.0.1:5107"

  return {
    plugins: [vue()],
    build: {
      chunkSizeWarningLimit: 1700,
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (id.includes("node_modules")) {
              if (id.includes("@microsoft/signalr")) return "signalr-vendor"
              if (id.includes("maplibre-gl") || id.includes("three")) return "map-vendor"
              if (id.includes("html5-qrcode") || id.includes("jsqr") || id.includes("qrcode")) return "qr-vendor"
            }
          }
        }
      }
    },
    server: {
      host: "0.0.0.0",
      port: 5173,
      proxy: {
        "/api": {
          target: apiProxyTarget,
          changeOrigin: true,
          secure: false
        }
      }
    }
  }
})
