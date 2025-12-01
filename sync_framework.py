#!/usr/bin/env python3
"""
Sync Framework source to a Unity project directory.

Default dest:
  D:\\ZippsterStudios\\Games\\Templates\\MMO Game Templates\\Assets\\Scripts\\Framework

Usage examples:
  python tools/sync_framework.py                       # copy from ./Framework -> default dest
  python tools/sync_framework.py --dest "D:/Proj/Assets/Scripts/Framework"
  python tools/sync_framework.py --dry-run
  python tools/sync_framework.py --delete               # mirror (delete extraneous files at dest)

Notes:
  - Excludes any directory named ".git".
  - Optionally excludes common non-runtime folders with --only-code.
  - Preserves timestamps (copy2) and prints a summary.
"""

from __future__ import annotations
import argparse
import os
import shutil
from pathlib import Path

DEFAULT_DEST = r"D:\\ZippsterStudios\\Games\\Templates\\MMO Game Templates\\Assets\\Scripts\\Framework"

EXCLUDED_DIR_NAMES = {".git", "Library", "Temp", ".idea", ".vscode", "obj", "bin", ".vs", "UnityStubs", "Stubs"}
EXCLUDED_FILE_NAMES = {"Thumbs.db", ".DS_Store"}

ONLY_CODE_EXTS = {
    ".cs", ".json", ".bytes", ".xml", ".shader", ".cginc", ".compute",
    ".asmdef", ".asset", ".mat"
}

def should_copy(path: Path, only_code: bool) -> bool:
    if path.name in EXCLUDED_FILE_NAMES:
        return False
    if only_code:
        return path.suffix.lower() in ONLY_CODE_EXTS
    return True

def sync_tree(src: Path, dest: Path, dry_run: bool, delete: bool, only_code: bool) -> tuple[int, int, int]:
    copied = skipped = removed = 0

    # Walk source and copy files
    for root, dirs, files in os.walk(src):
        # prune excluded directories in-place
        dirs[:] = [d for d in dirs if d not in EXCLUDED_DIR_NAMES]

        rel_root = Path(root).relative_to(src)
        dest_root = dest / rel_root
        if not dry_run:
            dest_root.mkdir(parents=True, exist_ok=True)

        for fname in files:
            s = Path(root) / fname
            if not should_copy(s, only_code):
                skipped += 1
                continue
            d = dest_root / fname
            if d.exists():
                try:
                    s_stat = s.stat()
                    d_stat = d.stat()
                    # If size and mtime match closely, skip copy
                    if s_stat.st_size == d_stat.st_size and int(s_stat.st_mtime) == int(d_stat.st_mtime):
                        skipped += 1
                        continue
                except OSError:
                    pass
            print(f"COPY {s} -> {d}")
            if not dry_run:
                d.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(s, d)
            copied += 1

    # Optionally delete files at dest that aren't in src
    if delete:
        for root, dirs, files in os.walk(dest):
            # prune excluded directories in-place (don't delete .git etc.)
            dirs[:] = [d for d in dirs if d not in EXCLUDED_DIR_NAMES]
            rel_root = Path(root).relative_to(dest)
            src_root = src / rel_root
            for fname in files:
                d = Path(root) / fname
                s = src_root / fname
                if not s.exists() or (only_code and not should_copy(s, only_code=True)):
                    print(f"DEL  {d}")
                    if not dry_run:
                        try:
                            d.unlink()
                            removed += 1
                        except OSError:
                            pass
    return copied, skipped, removed

def main() -> int:
    parser = argparse.ArgumentParser(description="Sync Framework to Unity Assets")
    parser.add_argument("--src", type=Path, default=Path(__file__).resolve().parents[1] / "Framework",
                        help="Source folder (default: ./Framework)")
    parser.add_argument("--dest", type=Path, default=Path(DEFAULT_DEST),
                        help="Destination folder (Unity Assets/Scripts/Framework)")
    parser.add_argument("--dry-run", action="store_true", help="Print actions without copying")
    parser.add_argument("--delete", action="store_true", help="Delete files at dest not present in src")
    parser.add_argument("--only-code", action="store_true", help="Copy only code/data files (e.g., .cs, .json)")
    args = parser.parse_args()

    src = args.src.resolve()
    dest = args.dest
    print(f"Syncing\n  src:  {src}\n  dest: {dest}\n  dry:  {args.dry_run}\n  del:  {args.delete}\n  only_code: {args.only_code}")
    if not src.exists() or not src.is_dir():
        print(f"ERROR: Source does not exist or is not a directory: {src}")
        return 2
    if not args.dry_run:
        dest.mkdir(parents=True, exist_ok=True)

    copied, skipped, removed = sync_tree(src, dest, args.dry_run, args.delete, args.only_code)
    print(f"Done. Copied: {copied}, Skipped: {skipped}, Removed: {removed}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
