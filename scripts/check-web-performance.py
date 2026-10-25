#!/usr/bin/env python3
"""Fail closed on unqualified captures or violations of the documented 60 FPS budget."""
import argparse
import json
import math
from pathlib import Path
import re
import sys


def number(value, label, minimum=0):
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or value < minimum:
        raise ValueError(f"Invalid {label}")
    return value


def percentile(values, fraction):
    return sorted(values)[max(0, math.ceil(len(values) * fraction) - 1)]


def check(report):
    if report.get("schema") != 2 or report.get("gameplayReady") is not True:
        raise ValueError("Incomplete gameplay capture")
    if not report.get("browser") or not re.fullmatch(r"[0-9a-f]{64}", report.get("build", {}).get("sha256", "")):
        raise ValueError("Missing browser or build identity")
    number(report.get("browserHz"), "idle browser cadence (at least 59 Hz required)", 59)
    samples = report.get("scenarios", [])
    if len(samples) != 2 or {s.get("name") for s in samples} != {"menu", "level01"}:
        raise ValueError("Expected menu and level01 captures")
    results = []
    for sample in samples:
        name = sample["name"]
        renderer = sample.get("renderer", "")
        if not renderer or re.search(r"swiftshader|llvmpipe|softpipe|software|microsoft basic", renderer, re.I):
            raise ValueError(f"{name}: hardware renderer required")
        if sample.get("powerPreference") != "high-performance":
            raise ValueError(f"{name}: high-performance WebGL context preference required")
        if sample.get("visible") is not True or sample.get("contextLost") is not False:
            raise ValueError(f"{name}: hidden tab or lost graphics context")
        if sample.get("gpuDisjoint") is not False:
            raise ValueError(f"{name}: invalid GPU timing sequence")
        if (sample.get("width"), sample.get("height"), sample.get("finalWidth"), sample.get("finalHeight"), sample.get("dpr")) != (1920, 1200, 1920, 1200, 2):
            raise ValueError(f"{name}: reference profile requires a stable 1920x1200 canvas at DPR 2")
        number(sample.get("warmupMs"), "warm-up", 10000)
        duration = number(sample.get("durationMs"), "capture duration", 29900)
        frames = sample.get("frames", [])
        if len(frames) < 2:
            raise ValueError(f"{name}: no rendered frame sequence")
        previous = 0
        intervals, cpu, gpu = [], [], []
        for frame in frames:
            timestamp = number(frame.get("timeMs"), "frame timestamp")
            if timestamp <= previous or timestamp > duration:
                raise ValueError(f"{name}: non-monotonic or out-of-window frame")
            number(frame.get("draws"), "draw submissions", 1)
            cpu.append(number(frame.get("cpuMs"), "CPU callback time"))
            gpu.append(number(frame.get("gpuMs"), "GPU elapsed time"))
            intervals.append(timestamp - previous)
            previous = timestamp
        # Include capture boundaries: a renderer that freezes at the end must fail.
        intervals.append(duration - previous)
        if not any(gpu):
            raise ValueError(f"{name}: GPU timer returned only zero values")
        fps = len(frames) * 1000 / duration
        p95, p99 = percentile(intervals, .95), percentile(intervals, .99)
        stalls = sum(value > 33.34 for value in intervals) / len(intervals)
        gpu95, gpu99 = percentile(gpu, .95), percentile(gpu, .99)
        gpu_stalls = sum(value > 33.34 for value in gpu) / len(gpu)
        passed = (fps >= 59 and p95 <= 20 and p99 <= 25 and stalls <= .001
                  and max(intervals) <= 100 and gpu95 <= 16.67 and gpu99 <= 20
                  and max(gpu) <= 100 and gpu_stalls <= .001)
        results.append({"scenario": name, "passed": passed, "fps": round(fps, 2),
                        "p95Ms": round(p95, 2), "p99Ms": round(p99, 2), "maxMs": round(max(intervals), 2),
                        "stallsPercent": round(stalls * 100, 3), "cpuP95Ms": round(percentile(cpu, .95), 2),
                        "gpuP95Ms": round(gpu95, 2), "gpuP99Ms": round(gpu99, 2),
                        "gpuMaxMs": round(max(gpu), 2), "gpuStallsPercent": round(gpu_stalls * 100, 3)})
    return results


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    args = parser.parse_args()
    try:
        results = check(json.loads(args.report.read_text()))
        print(json.dumps(results, indent=2))
        return 0 if all(result["passed"] for result in results) else 1
    except (ValueError, TypeError, KeyError, AttributeError, OSError) as error:
        print(f"Invalid performance evidence: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
