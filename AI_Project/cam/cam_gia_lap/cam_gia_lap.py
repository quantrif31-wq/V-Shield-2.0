import os
import subprocess
import threading
import time
import socket
from tkinter import *
from tkinter import ttk

VIDEO_DIR = "video"
RTSP_URL = "rtsp://127.0.0.1:8556"
MEDIAMTX_DIR = "mediamtx_v1.17.1_windows_amd64"

# ---------------- CHECK PORT ----------------
def check_port(port=8556):
    s = socket.socket()
    try:
        s.connect(("127.0.0.1", port))
        return True
    except:
        return False
    finally:
        s.close()

# ---------------- MEDIAMTX ----------------
class MediaMTX:
    def __init__(self):
        self.proc = None

    def start(self):
        if self.proc:
            return

        try:
            path = os.path.abspath(MEDIAMTX_DIR)

            cmd = f'start cmd /k "cd /d {path} && mediamtx.exe"'
            self.proc = subprocess.Popen(cmd, shell=True)

            print("🚀 Starting MediaMTX...")

            # 🔥 đợi server ready thật
            for i in range(10):
                if check_port(8556):
                    print("✅ MediaMTX ready")
                    return
                time.sleep(1)

            print("❌ MediaMTX failed to start")

        except Exception as e:
            print("❌ MediaMTX error:", e)

# ---------------- STREAM ----------------
class StreamManager:
    def __init__(self):
        self.processes = {}

    def start(self, name, video):
        if name in self.processes:
            return

        cmd = [
            "ffmpeg",
            "-re","-stream_loop","-1",
            "-i", video,

            # 🔥 audio giả
            "-f","lavfi",
            "-i","anullsrc=channel_layout=stereo:sample_rate=44100",

            "-map","0:v:0",
            "-map","1:a:0",

            "-vf","scale=1280:720",
            "-c:v","libx264",
            "-preset","ultrafast",
            "-tune","zerolatency",
            "-pix_fmt","yuv420p",
            "-r","25",
            "-g","50",
            "-b:v","1500k",

            "-c:a","aac",
            "-b:a","128k",

            "-f","rtsp",
            "-rtsp_transport","tcp",
            f"{RTSP_URL}/{name}"
        ]

        proc = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )

        self.processes[name] = proc

        threading.Thread(target=self.read_log, args=(name, proc), daemon=True).start()

    def read_log(self, name, proc):
        for line in proc.stderr:
            print(f"[{name}] {line.strip()}")

    def stop(self, name):
        if name in self.processes:
            self.processes[name].terminate()
            del self.processes[name]

    def stop_all(self):
        for p in self.processes.values():
            p.terminate()
        self.processes.clear()

# ---------------- APP ----------------
class App:
    def __init__(self, root):
        self.root = root
        self.root.title("RTSP Camera PRO FINAL")

        self.server = MediaMTX()
        self.stream = StreamManager()

        self.cams = self.load_videos()
        self.status = {}

        self.tree = ttk.Treeview(root, columns=("Video","RTSP","Status"), show="headings")

        self.tree.heading("Video", text="Video")
        self.tree.heading("RTSP", text="RTSP")
        self.tree.heading("Status", text="Status")

        self.tree.pack(fill=BOTH, expand=True)

        btn = Frame(root)
        btn.pack(fill=X)

        Button(btn, text="Start Server", command=self.start_server).pack(side=LEFT)
        Button(btn, text="Start All", command=self.start_all).pack(side=LEFT)
        Button(btn, text="Stop All", command=self.stop_all).pack(side=LEFT)

        self.root.after(1000, self.start_all)
        self.loop()

    def load_videos(self):
        os.makedirs(VIDEO_DIR, exist_ok=True)
        files = [f for f in os.listdir(VIDEO_DIR) if f.endswith(".mp4")]

        cams = []
        for i, f in enumerate(files):
            cams.append({
                "name": f"virtual{i+1}",
                "video": os.path.join(VIDEO_DIR, f)
            })
        return cams

    def start_server(self):
        threading.Thread(target=self.server.start, daemon=True).start()

    def start_all(self):
        self.server.start()

        if not check_port(8556):
            print("❌ Server chưa chạy → không start cam")
            return

        for cam in self.cams:
            self.stream.start(cam["name"], cam["video"])
            self.status[cam["name"]] = "RUNNING"

    def stop_all(self):
        self.stream.stop_all()
        for cam in self.cams:
            self.status[cam["name"]] = "STOPPED"

    def refresh(self):
        self.tree.delete(*self.tree.get_children())

        for cam in self.cams:
            name = cam["name"]
            video = cam["video"]
            rtsp = f"{RTSP_URL}/{name}"
            status = self.status.get(name, "STOPPED")

            self.tree.insert("", END, values=(video, rtsp, status))

    def loop(self):
        self.refresh()
        self.root.after(1000, self.loop)

# ---------------- MAIN ----------------
if __name__ == "__main__":
    root = Tk()
    root.geometry("900x500")
    App(root)
    root.mainloop()