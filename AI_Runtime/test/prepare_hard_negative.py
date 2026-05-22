from pathlib import Path
import argparse
import random
import shutil

IMG_EXTS = {".jpg", ".jpeg", ".png", ".bmp"}


def collect_images(src: Path):
    return sorted([p for p in src.iterdir() if p.is_file() and p.suffix.lower() in IMG_EXTS])


def ensure_dirs(base: Path):
    for split in ["train", "valid", "test"]:
        (base / split / "images").mkdir(parents=True, exist_ok=True)
        (base / split / "labels").mkdir(parents=True, exist_ok=True)


def split_items(items, train_ratio=0.8, valid_ratio=0.1):
    n = len(items)
    n_train = int(n * train_ratio)
    n_valid = int(n * valid_ratio)
    return {
        "train": items[:n_train],
        "valid": items[n_train:n_train + n_valid],
        "test": items[n_train + n_valid:],
    }


def append_negative_samples(src_dir: Path, target_dir: Path, seed=42):
    imgs = collect_images(src_dir)
    if not imgs:
        raise ValueError(f"No images found in {src_dir}")

    random.seed(seed)
    random.shuffle(imgs)
    split_map = split_items(imgs)

    ensure_dirs(target_dir)
    for split, items in split_map.items():
        for p in items:
            dst_img = target_dir / split / "images" / p.name
            shutil.copy2(p, dst_img)
            (target_dir / split / "labels" / (p.stem + ".txt")).write_text("", encoding="ascii")

    return split_map


def count_split(base: Path, split: str):
    img_count = len(list((base / split / "images").glob("*")))
    lbl_count = len(list((base / split / "labels").glob("*.txt")))
    return img_count, lbl_count


def main():
    parser = argparse.ArgumentParser(description="Append hard-negative (no fire/smoke) images to YOLO dataset")
    parser.add_argument("--src", default="retrain_capture", help="Folder containing raw false-alarm images")
    parser.add_argument("--main", default="dataset/indoor_fire_smoke", help="Main YOLO dataset root")
    parser.add_argument("--neg", default="dataset/hard_negative_no_fire_smoke", help="Dedicated negative-only dataset root")
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    root = Path.cwd()
    src = (root / args.src).resolve()
    main_ds = (root / args.main).resolve()
    neg_ds = (root / args.neg).resolve()

    if not src.exists():
        raise FileNotFoundError(f"Source not found: {src}")

    append_negative_samples(src, neg_ds, seed=args.seed)
    append_negative_samples(src, main_ds, seed=args.seed)

    print("[OK] Added hard-negative samples.")
    print(f"Source: {src}")
    print(f"Negative dataset: {neg_ds}")
    print(f"Main dataset: {main_ds}")
    for split in ["train", "valid", "test"]:
        ni, nl = count_split(neg_ds, split)
        mi, ml = count_split(main_ds, split)
        print(f"split={split} neg(images={ni},labels={nl}) main(images={mi},labels={ml})")


if __name__ == "__main__":
    main()
