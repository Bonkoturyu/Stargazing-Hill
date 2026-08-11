#!/usr/bin/env python3
"""Create the tracked HYG v4.1 bright-star subset used by the Unity baker."""

import argparse
import csv
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--magnitude", type=float, default=6.8)
    args = parser.parse_args()

    args.destination.parent.mkdir(parents=True, exist_ok=True)
    count = 0
    with args.source.open("r", encoding="utf-8", newline="") as source:
        reader = csv.DictReader(source)
        with args.destination.open("w", encoding="utf-8", newline="") as destination:
            writer = csv.writer(destination, lineterminator="\n")
            writer.writerow(("rarad", "decrad", "mag", "ci"))
            for row in reader:
                try:
                    magnitude = float(row["mag"])
                    distance = float(row["dist"])
                    ra = float(row["rarad"])
                    dec = float(row["decrad"])
                except (KeyError, TypeError, ValueError):
                    continue
                if distance <= 0.0 or magnitude > args.magnitude:
                    continue
                writer.writerow((format(ra, ".12g"), format(dec, ".12g"),
                                 format(magnitude, ".6g"), row.get("ci", "")))
                count += 1

    print(f"Wrote {count} stars to {args.destination}")


if __name__ == "__main__":
    main()
