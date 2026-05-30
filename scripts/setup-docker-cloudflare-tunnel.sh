#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${ENV_FILE:-.env}"
TUNNEL_NAME="${TUNNEL_NAME:-cam-tunnel}"
MODE="${1:-prompt}" # prompt|manual|auto

step() { echo; echo "==> $1"; }
ok() { echo "[OK] $1"; }
warn() { echo "[WARN] $1"; }
err() { echo "[ERR] $1"; }

ensure_env_file() {
  if [[ -f "$ENV_FILE" ]]; then return; fi
  if [[ -f ".env.example" ]]; then cp .env.example "$ENV_FILE"; return; fi
  if [[ -f ".env.docker.example" ]]; then cp .env.docker.example "$ENV_FILE"; return; fi
  err "Khong tim thay .env.example hoac .env.docker.example"
  exit 1
}

normalize_host() {
  local h="$1"
  h="${h#http://}"
  h="${h#https://}"
  h="${h#//}"
  h="${h%/}"
  echo "$h"
}

set_or_add_env() {
  local key="$1"
  local value="$2"
  if grep -qE "^[[:space:]]*${key}=" "$ENV_FILE"; then
    sed -i.bak -E "s|^[[:space:]]*${key}=.*$|${key}=${value}|g" "$ENV_FILE"
  else
    echo "${key}=${value}" >> "$ENV_FILE"
  fi
}

is_headless() {
  [[ -z "${DISPLAY:-}" && -z "${WAYLAND_DISPLAY:-}" ]]
}

ensure_cloudflared() {
  command -v cloudflared >/dev/null 2>&1 || { err "Khong tim thay cloudflared trong PATH."; exit 1; }
}

ensure_tunnel_exists() {
  local name="$1"
  if cloudflared tunnel list --output json | grep -q "\"name\":\"${name}\""; then
    ok "Tunnel da ton tai: ${name}"
    return
  fi
  cloudflared tunnel create "$name" >/dev/null
  ok "Da tao tunnel: ${name}"
}

ensure_dns_route() {
  local name="$1"
  local host="$2"
  local out
  set +e
  out="$(cloudflared tunnel route dns "$name" "$host" 2>&1)"
  local rc=$?
  set -e
  if [[ $rc -eq 0 ]]; then
    ok "DNS route san sang: $host"
    return
  fi
  if echo "$out" | grep -qi "already exists"; then
    warn "DNS route da ton tai: $host"
    return
  fi
  err "Khong the tao DNS route: $out"
  exit 1
}

get_tunnel_token() {
  local name="$1"
  local token
  token="$(cloudflared tunnel token "$name" | tr -d '\r' | tail -n 1)"
  [[ -n "$token" ]] || { err "Khong lay duoc tunnel token."; exit 1; }
  echo "$token"
}

wait_api_healthy() {
  local timeout="${1:-90}"
  local elapsed=0
  while [[ $elapsed -lt $timeout ]]; do
    if curl -fsS "http://localhost:5107/health" >/dev/null 2>&1; then return 0; fi
    sleep 2
    elapsed=$((elapsed + 2))
  done
  return 1
}

patch_appsettings_layout_preserving() {
  local appsettings_path="$1"
  local tunnel_name="$2"
  local public_host="$3"
  local target_service="$4"
  local backup="${appsettings_path}.bak.public-domain"

  [[ -f "$appsettings_path" ]] || { err "Missing appsettings: $appsettings_path"; return 1; }
  cp "$appsettings_path" "$backup"

  python3 - "$appsettings_path" "$tunnel_name" "$public_host" "$target_service" <<'PY'
import io
import json
import re
import sys

path, tunnel_name, public_host, target_service = sys.argv[1:5]
with io.open(path, "r", encoding="utf-8") as f:
    content = f.read()
json.loads(content)

def repl(text, key, value):
    pattern = r'("%s"\s*:\s*")([^"]*)(")' % re.escape(key)
    return re.sub(pattern, r'\1%s\3' % value, text, count=1)

updated = content
updated = repl(updated, "TunnelName", tunnel_name)
updated = repl(updated, "PublicHostname", public_host)
updated = repl(updated, "TargetService", target_service)
updated = repl(updated, "Go2RtcPublicBaseUrl", f"https://{public_host}")
updated = repl(updated, "FrontendUrl", f"https://{public_host}")
json.loads(updated)

with io.open(path, "w", encoding="utf-8", newline="") as f:
    f.write(updated)
PY
}

test_cloudflared_stable() {
  local inspect logs status restart_count
  inspect="$(docker inspect vshield-cloudflared --format '{{.State.Status}}|{{.RestartCount}}' 2>/dev/null || true)"
  [[ -n "$inspect" ]] || { err "Khong inspect duoc cloudflared"; return 1; }
  status="${inspect%%|*}"
  restart_count="${inspect##*|}"
  [[ "$status" == "running" ]] || { err "cloudflared status=$status"; return 1; }
  [[ "${restart_count:-0}" == "0" ]] || { err "cloudflared dang restart (RestartCount=$restart_count)"; return 1; }

  logs="$(docker logs --tail 120 vshield-cloudflared 2>&1 || true)"
  if echo "$logs" | grep -qi "flag needs an argument: -token"; then
    err "Token cloudflared dang rong."
    return 1
  fi
  if echo "$logs" | grep -Eqi "authentication failed|invalid token|token is invalid"; then
    err "Token cloudflared khong hop le."
    return 1
  fi
  return 0
}

resolve_mode() {
  local input="$1"
  if [[ "$input" != "prompt" ]]; then echo "$input"; return; fi
  echo
  echo "Chon che do:"
  echo "  1) Manual token (paste token)"
  echo "  2) Auto tunnel (login/create/route/token)"
  read -r -p "Nhap 1 hoac 2: " choice
  [[ "$choice" == "2" ]] && echo "auto" || echo "manual"
}

MODE="$(resolve_mode "$MODE")"

step "Buoc 0: Chuan bi env + input"
ensure_env_file
read -r -p "Nhap domain public (vi du cam.example.com): " public_host
public_host="$(normalize_host "${public_host}")"
[[ -n "$public_host" ]] || { err "Domain public khong duoc de trong."; exit 1; }

token=""
if [[ "$MODE" == "auto" ]]; then
  if is_headless; then
    warn "Moi truong headless khong mo duoc browser. Tu dong chuyen sang manual token."
    MODE="manual"
  fi
fi

if [[ "$MODE" == "auto" ]]; then
  step "Buoc 1: Auto tunnel flow"
  ensure_cloudflared
  set +e
  cloudflared tunnel login
  rc=$?
  set -e
  [[ $rc -eq 0 ]] || warn "cloudflared login tra ve non-zero (co the cert.pem da ton tai). Van tiep tuc."
  ensure_tunnel_exists "$TUNNEL_NAME"
  ensure_dns_route "$TUNNEL_NAME" "$public_host"
  token="$(get_tunnel_token "$TUNNEL_NAME")"
  ok "Lay token tunnel thanh cong."
else
  step "Buoc 1: Manual token flow"
  read -r -p "Nhap CLOUDFLARED_TUNNEL_TOKEN (dang eyJh...): " token
  [[ -n "$token" ]] || { err "Token khong duoc de trong."; exit 1; }
fi

step "Buoc 2: Ghi cau hinh vao .env"
set_or_add_env "CLOUDFLARED_TUNNEL_TOKEN" "$token"
set_or_add_env "APP_FRONTEND_URL" "https://${public_host}"
set_or_add_env "APP_GO2RTC_PUBLIC_BASE_URL" "https://${public_host}"
set_or_add_env "APP_ALLOW_CROSS_ORIGIN_GO2RTC_FRAME" "false"
set_or_add_env "GO2RTC_WEBRTC_CANDIDATES" ""
set_or_add_env "GO2RTC_WEBRTC_PORT" "8555"
set_or_add_env "GO2RTC_STREAM_MODE" "webrtc"
ok "Da cap nhat .env"

step "Buoc 3: Patch appsettings.json"
patch_appsettings_layout_preserving "./API/API/API/appsettings.json" "$TUNNEL_NAME" "$public_host" "http://localhost:5173"
ok "Patch appsettings thanh cong."

step "Buoc 4: Khoi dong core stack"
docker compose up -d --build db go2rtc api frontend

step "Buoc 5: Cho API healthy"
if ! wait_api_healthy 90; then
  err "API khong healthy sau khi khoi dong."
  exit 1
fi
ok "API healthy."

step "Buoc 6: Bat cloudflared profile"
docker compose --profile tunnel up -d --force-recreate cloudflared
sleep 4
test_cloudflared_stable
ok "Cloudflared running on dinh."

step "Buoc 7: Reload go2rtc"
curl -fsS -X POST "http://localhost:5107/api/camera-runtime/reload-go2rtc" >/dev/null
ok "reload-go2rtc done."

step "Buoc 8: Verify nhanh stream endpoint"
if curl -fsS "https://${public_host}/stream.html?src=cam1&mode=webrtc" >/dev/null 2>&1; then
  ok "stream.html reachable."
else
  warn "stream.html chua san sang (co the DNS/tunnel can propagation them)."
fi

echo
echo "================ SUMMARY ================"
echo "Mode           : ${MODE}"
echo "Public domain  : https://${public_host}"
echo "App URL        : https://${public_host}"
echo "Stream sample  : https://${public_host}/stream.html?src=cam1&mode=webrtc"
echo
echo "Lan sau (run nhanh): docker compose up -d"
