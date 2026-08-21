# -*- coding: utf-8 -*-
"""
Frame simulator cho test QR gửi xe (V-Shield 2.0).
Không sửa logic app: chỉ phát 4 luồng MJPEG giả lập (qr1, qr2, plate1, plate2)
và cho phép driver set frame (QR / biển số / neutral) qua /control/set.
"""
import io
import json
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

from PIL import Image, ImageDraw, ImageFont
import qrcode

CAMS = ["qr1", "qr2", "plate1", "plate2"]

FRAME_W, FRAME_H = 960, 540

_lock = threading.Lock()
_frames = {cam: None for cam in CAMS}  # jpeg bytes

FONT_BOLD = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
FONT_REG = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"

_plate_font_cache = {}
def plate_font(size):
    key = (FONT_BOLD, size)
    if key not in _plate_font_cache:
        _plate_font_cache[key] = ImageFont.truetype(FONT_BOLD, size)
    return _plate_font_cache[key]

def font_regular(size):
    return ImageFont.truetype(FONT_REG, size)


def jpeg_from(img, quality=88):
    buf = io.BytesIO()
    img.save(buf, format="JPEG", quality=quality)
    return buf.getvalue()


def make_neutral(seed=0):
    img = Image.new("RGB", (FRAME_W, FRAME_H), (18, 20, 26))
    d = ImageDraw.Draw(img)
    for i in range(0, FRAME_W, 40):
        d.line([(i, 0), (i + 60, FRAME_H)], fill=(30, 33, 42), width=1)
    d.text((20, FRAME_H - 30), "SIM", fill=(70, 75, 90), font=font_regular(22))
    return img


def render_qr(payload):
    qr = qrcode.QRCode(
        version=None,
        error_correction=qrcode.constants.ERROR_CORRECT_M,
        box_size=8,
        border=4,
    )
    qr.add_data(payload)
    qr.make(fit=True)
    img = qr.make_image(fill_color="black", back_color="white").convert("RGB")
    # vừa khung, để lề
    scale = min((FRAME_W - 120) / img.width, (FRAME_H - 120) / img.height)
    new_w = int(img.width * scale)
    new_h = int(img.height * scale)
    img = img.resize((new_w, new_h), Image.LANCZOS)
    canvas = Image.new("RGB", (FRAME_W, FRAME_H), (245, 245, 245))
    canvas.paste(img, ((FRAME_W - new_w) // 2, (FRAME_H - new_h) // 2))
    return canvas


def render_plate(plate_text):
    """
    Biển số VN giả lập (geometry đã validate bằng YOLO best.pt + PaddleOCR):
    biển 820x460 tại (70,40), dải xanh trái 178px, chữ đen to ~140 vừa khổ.
    Chuẩn OCR đọc đủ + YOLO conf ~0.87.
    """
    plate_w, plate_h = 820, 460
    x0, y0 = 70, 40

    canvas = Image.new("RGB", (FRAME_W, FRAME_H), (22, 24, 30))
    d = ImageDraw.Draw(canvas)

    d.rounded_rectangle([x0 + 6, y0 + 8, x0 + plate_w + 6, y0 + plate_h + 8],
                        radius=18, fill=(10, 11, 14))
    d.rounded_rectangle([x0, y0, x0 + plate_w, y0 + plate_h],
                        radius=16, fill=(250, 250, 250), outline=(30, 30, 30), width=4)

    band_w = 178
    d.rectangle([x0 + 8, y0 + 8, x0 + band_w, y0 + plate_h - 8], fill=(0, 82, 180))

    text_area_x = x0 + 190
    max_text_w = (x0 + plate_w - 30) - text_area_x
    font_size = 140
    while font_size > 40:
        f = plate_font(font_size)
        tw = d.textbbox((0, 0), plate_text, font=f)[2]
        if tw <= max_text_w:
            break
        font_size -= 4
    d.text((text_area_x, y0 + 30), plate_text, fill=(15, 15, 15), font=plate_font(font_size))

    return canvas


def set_frame(cam, kind, payload="", plate=""):
    with _lock:
        if kind == "qr":
            _frames[cam] = jpeg_from(render_qr(payload))
        elif kind == "plate":
            _frames[cam] = jpeg_from(render_plate(plate))
        else:
            _frames[cam] = jpeg_from(make_neutral())
    return True


class Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def log_message(self, fmt, *args):
        pass

    def _send_json(self, obj, status=200):
        body = json.dumps(obj).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        path = self.path.split("?")[0]
        if path == "/health":
            return self._send_json({"ok": True, "cams": list(_frames.keys())})

        if path.startswith("/mjpeg/"):
            cam = path[len("/mjpeg/"):]
            if cam not in CAMS:
                return self._send_json({"ok": False, "error": "cam khong ton tai"}, 404)
            return self._stream_mjpeg(cam)

        return self._send_json({"ok": False, "error": "not found"}, 404)

    def _stream_mjpeg(self, cam):
        self.send_response(200)
        self.send_header("Content-Type", "multipart/x-mixed-replace; boundary=frame")
        self.send_header("Cache-Control", "no-cache")
        self.send_header("Connection", "close")
        self.end_headers()
        try:
            while True:
                with _lock:
                    data = _frames[cam] if _frames[cam] else jpeg_from(make_neutral())
                self.wfile.write(b"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: " +
                                 str(len(data)).encode() + b"\r\n\r\n")
                self.wfile.write(data)
                self.wfile.write(b"\r\n")
                self.wfile.flush()
                time.sleep(0.05)
        except (BrokenPipeError, ConnectionResetError):
            pass

    def do_POST(self):
        path = self.path.split("?")[0]
        if path == "/control/set":
            try:
                length = int(self.headers.get("Content-Length", 0))
                body = json.loads(self.rfile.read(length) or b"{}")
            except Exception:
                return self._send_json({"ok": False, "error": "bad json"}, 400)

            cam = str(body.get("cam", ""))
            kind = str(body.get("type", "neutral"))
            if cam not in CAMS:
                return self._send_json({"ok": False, "error": "cam khong ton tai"}, 404)
            try:
                set_frame(cam, kind, str(body.get("payload", "")), str(body.get("plate", "")))
            except Exception as e:
                return self._send_json({"ok": False, "error": str(e)}, 500)
            return self._send_json({"ok": True, "cam": cam, "type": kind})
        return self._send_json({"ok": False, "error": "not found"}, 404)


if __name__ == "__main__":
    for c in CAMS:
        set_frame(c, "neutral")
    server = ThreadingHTTPServer(("0.0.0.0", 9400), Handler)
    print("qr-sim listening on 0.0.0.0:9400", flush=True)
    server.serve_forever()