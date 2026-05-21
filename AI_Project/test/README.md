# Commercial Fire/Smoke Guard (YOLOv8 Small)

## 1) Activate venv (Python 3.10.11)
```powershell
cd C:\DoAnTotNghiep\V-Shield\V-Shield\AI_Project\test
.\.venv\Scripts\Activate.ps1
```

## 2) Run GUI
```powershell
python app.py
```

## 3) 3-layer stability design
- Layer 1 (AI): YOLOv8 Small detects fire/smoke candidates.
- Layer 2 (Tracking): each detection gets a track ID and is validated by persistence (time + hit count).
- Layer 3 (Business Logic): ROI rules + sensor fusion decide silent alert vs loud alarm.

## 4) Important settings
- `Confidence`: detection confidence threshold.
- `Persistence sec`: each tracked object must survive this long.
- `Min hits per ID`: minimum tracked hits for a valid event.
- `Alert ROI`: only events inside these rectangles can trigger camera alarm.
- `Ignore ROI`: detections in these rectangles are ignored.
- `Enable sensor fusion`: combine camera result with temperature and smoke IoT status.

ROI format:
- `x1,y1,x2,y2; x1,y1,x2,y2`
- Example Alert ROI: `100,80,600,420`
- Example Ignore ROI: `0,0,180,120`

## 5) Alarm logic
When sensor fusion ON:
- Loud alarm: stable fire/smoke (camera) + (high temperature OR smoke sensor ON)
- Silent alert: only one signal appears (camera-only or sensor-only)

When sensor fusion OFF:
- Loud alarm: stable fire/smoke from camera only

## 6) Model note
- Default model is `models/yolov8s.pt` (YOLOv8 Small backbone).
- Base YOLOv8s does not include fire/smoke classes by default.
- After you train your own fire/smoke model on YOLOv8s, update `Model path` in GUI.
