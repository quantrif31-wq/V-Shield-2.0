import argparse
from pathlib import Path

import torch
from ultralytics import YOLO


def parse_args():
    parser = argparse.ArgumentParser(description="Train fire/smoke detector on YOLOv8 Small (yolov8s)")
    parser.add_argument("--data", type=str, default="dataset/indoor_fire_smoke/data.yaml", help="Path to data.yaml")
    parser.add_argument("--weights", type=str, default="models/yolov8s.pt", help="Initial YOLOv8s weights")
    parser.add_argument("--epochs", type=int, default=100)
    parser.add_argument("--imgsz", type=int, default=640)
    parser.add_argument("--batch", type=int, default=16)
    parser.add_argument("--project", type=str, default="runs/fire_smoke")
    parser.add_argument("--name", type=str, default="yolov8s_indoor_fire_smoke")
    parser.add_argument("--workers", type=int, default=8)
    parser.add_argument("--patience", type=int, default=30)
    parser.add_argument("--device", type=str, default="auto", help="auto | cpu | 0 | 0,1")
    parser.add_argument("--amp", action="store_true", help="Enable mixed precision (recommended on CUDA)")
    return parser.parse_args()


def resolve_device(device_arg: str) -> str:
    if device_arg != "auto":
        return device_arg
    return "0" if torch.cuda.is_available() else "cpu"


def main():
    args = parse_args()

    data_path = Path(args.data)
    weights_path = Path(args.weights)

    if not data_path.exists():
        raise FileNotFoundError(f"data.yaml not found: {data_path}")
    if not weights_path.exists():
        raise FileNotFoundError(
            f"weights not found: {weights_path}. Download YOLOv8s first (models/yolov8s.pt)."
        )

    device = resolve_device(args.device)
    cuda_ok = torch.cuda.is_available()
    print(f"[INFO] torch.cuda.is_available = {cuda_ok}")
    if cuda_ok:
        print(f"[INFO] GPU = {torch.cuda.get_device_name(0)}")
    print(f"[INFO] train device = {device}")

    model = YOLO(str(weights_path))

    model.train(
        data=str(data_path),
        epochs=args.epochs,
        imgsz=args.imgsz,
        batch=args.batch,
        project=args.project,
        name=args.name,
        workers=args.workers,
        patience=args.patience,
        device=device,
        amp=args.amp,
        pretrained=True,
        exist_ok=True,
    )

    print("[DONE] Training finished.")
    print(f"[DONE] Best model: {Path(args.project) / args.name / 'weights' / 'best.pt'}")


if __name__ == "__main__":
    main()
