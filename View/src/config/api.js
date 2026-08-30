const trimTrailingSlash = (value = "") => value.replace(/\/+$/, "");

const ensureApiBaseUrl = (value, fallbackPort) => {
  const normalizedValue = trimTrailingSlash(value || "");
  if (normalizedValue) {
    return normalizedValue.endsWith("/api") ? normalizedValue : `${normalizedValue}/api`;
  }

  if (typeof window === "undefined") {
    return `http://localhost:${fallbackPort}/api`;
  }

  const { protocol, hostname, port } = window.location;
  // In local development on localhost/loopback, connect directly to the dev backend port
  if (hostname === "localhost" || hostname === "127.0.0.1") {
    return `${protocol}//${hostname}:${fallbackPort}/api`;
  }

  // In production / domain with reverse proxy (e.g. v-shield.site), use origin /api directly
  const portSuffix = port && port !== "80" && port !== "443" && port !== "8080" ? `:${port}` : "";
  return `${protocol}//${hostname}${portSuffix}/api`;
};

const ensureServiceBaseUrl = (value, fallbackPort) => {
  const normalizedValue = trimTrailingSlash(value || "");
  if (normalizedValue) {
    return normalizedValue;
  }

  return ensureApiBaseUrl("", fallbackPort);
};

const stripApiSuffix = (value) => value.replace(/\/api$/, "");

export const API_BASE_URL = ensureApiBaseUrl(import.meta.env.VITE_API_BASE_URL, 5107);
export const API_ORIGIN = stripApiSuffix(API_BASE_URL);

export const PLATE_API_BASE_URL = ensureServiceBaseUrl(import.meta.env.VITE_PLATE_API_BASE_URL, 5002);
export const PLATE_API_ORIGIN = stripApiSuffix(PLATE_API_BASE_URL);
