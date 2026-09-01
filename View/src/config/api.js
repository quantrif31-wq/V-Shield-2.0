const trimTrailingSlash = (value = "") => value.replace(/\/+$/, "");

const ensureApiBaseUrl = (value, fallbackPort) => {
  const normalizedValue = trimTrailingSlash(value || "");

  if (typeof window !== "undefined") {
    const { protocol, hostname, port } = window.location;

    // In local development on localhost/loopback:
    if (hostname === "localhost" || hostname === "127.0.0.1") {
      // If configured value is pointing to the remote production domain, rewrite to /api
      // so local requests are routed through local Nginx/Vite reverse proxy instead of hitting CORS blocks.
      if (normalizedValue.includes("v-shield.site")) {
        return "/api";
      }

      if (normalizedValue.startsWith("/")) {
        return normalizedValue.endsWith("/api") ? normalizedValue : `${normalizedValue}/api`;
      }

      if (normalizedValue) {
        return normalizedValue.endsWith("/api") ? normalizedValue : `${normalizedValue}/api`;
      }

      // If running on local server ports, use same-origin /api
      if (port === "5173" || port === "5174" || port === "5175" || port === "8080" || port === "80") {
        return "/api";
      }

      return `${protocol}//${hostname}:${fallbackPort}/api`;
    }

    // In production or custom domain (e.g. v-shield.site):
    if (!normalizedValue || normalizedValue.startsWith("/")) {
      const portSuffix = port && port !== "80" && port !== "443" && port !== "8080" ? `:${port}` : "";
      return normalizedValue ? (normalizedValue.endsWith("/api") ? normalizedValue : `${normalizedValue}/api`) : `${protocol}//${hostname}${portSuffix}/api`;
    }
  }

  if (normalizedValue) {
    return normalizedValue.endsWith("/api") ? normalizedValue : `${normalizedValue}/api`;
  }

  return `http://localhost:${fallbackPort}/api`;
};

const ensureServiceBaseUrl = (value, fallbackPort) => {
  const normalizedValue = trimTrailingSlash(value || "");

  if (typeof window !== "undefined") {
    const { hostname } = window.location;
    if ((hostname === "localhost" || hostname === "127.0.0.1") && normalizedValue.includes("v-shield.site")) {
      return "/api/PlateCamera";
    }
  }

  if (normalizedValue) {
    return normalizedValue;
  }

  return ensureApiBaseUrl("", fallbackPort);
};

const stripApiSuffix = (value) => value.replace(/\/api$/, "");

export const API_BASE_URL = ensureApiBaseUrl(import.meta.env.VITE_API_BASE_URL, 5107);
export const API_ORIGIN = stripApiSuffix(API_BASE_URL) || (typeof window !== "undefined" ? window.location.origin : "http://localhost:5107");

export const PLATE_API_BASE_URL = ensureServiceBaseUrl(import.meta.env.VITE_PLATE_API_BASE_URL, 5002);
export const PLATE_API_ORIGIN = stripApiSuffix(PLATE_API_BASE_URL) || (typeof window !== "undefined" ? window.location.origin : "http://localhost:5002");

