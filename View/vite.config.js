import { defineConfig, loadEnv } from "vite"
import vue from "@vitejs/plugin-vue"

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "")
  const apiProxyTarget = env.VITE_DEV_PROXY_TARGET || "http://127.0.0.1:5107"

  return {
    plugins: [vue()],
    test: {
      environment: "jsdom",
      css: true,
      include: ["src/**/*.{test,spec}.{js,ts}"],
      exclude: ["e2e/**", "node_modules/**", "dist/**"]
    },
    build: {
      chunkSizeWarningLimit: 600,
      rollupOptions: {
        onwarn(warning, warn) {
          // SignalR 10 ships a dependency annotation Rollup cannot place after transpilation.
          // It does not affect runtime/tree-shaking; suppress only that exact vendor warning.
          if (warning.code === "INVALID_ANNOTATION" && String(warning.id || "").includes("@microsoft/signalr")) return
          warn(warning)
        },
        output: {
          manualChunks(id) {
            if (id.includes("node_modules")) {
              if (id.includes("@microsoft/signalr")) return "signalr-vendor"
              if (id.includes("maplibre-gl")) return "maplibre-vendor"
              if (id.includes("three")) return "three-vendor"
              if (id.includes("html5-qrcode") || id.includes("jsqr")) return "qr-scanner-vendor"
              if (id.includes("qrcode")) return "qrcode-vendor"
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
