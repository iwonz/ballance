#!/usr/bin/env python3
"""Adversarial fixtures for the hardware frame-performance gate; no GPU claim."""
import copy
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("performance", Path(__file__).with_name("check-web-performance.py"))
performance = importlib.util.module_from_spec(spec)
spec.loader.exec_module(performance)


def fixture(fps=60):
    sample = {"warmupMs": 10000, "durationMs": 30000, "width": 1920, "height": 1200,
              "finalWidth": 1920, "finalHeight": 1200, "dpr": 2, "renderer": "ANGLE Metal Apple M3",
              "visible": True, "contextLost": False, "gpuDisjoint": False,
              "frames": [{"timeMs": (i + 1) * 1000 / fps, "cpuMs": 2, "gpuMs": 3, "draws": 50} for i in range(fps * 30)]}
    return {"schema": 1, "gameplayReady": True, "browser": "Chrome fixture", "browserHz": fps, "build": {"sha256": "a" * 64},
            "scenarios": [{"name": name, **copy.deepcopy(sample)} for name in ("menu", "level01")]}


class PerformanceChecks(unittest.TestCase):
    def test_refresh_rates(self):
        for fps in (60, 120, 144):
            with self.subTest(fps=fps):
                self.assertTrue(all(result["passed"] for result in performance.check(fixture(fps))))
        data = fixture()
        for sample in data["scenarios"]:
            for frame in sample["frames"]:
                frame["timeMs"] *= 60 / 59.94
            sample["frames"] = [frame for frame in sample["frames"] if frame["timeMs"] <= 30000]
        self.assertTrue(all(result["passed"] for result in performance.check(data)))

    def test_sustained_slow_frames(self):
        for fps in (30, 45, 58):
            with self.subTest(fps=fps):
                data = fixture(fps)
                data["browserHz"] = 60
                self.assertFalse(performance.check(data)[0]["passed"])

    def test_stall_despite_good_average(self):
        data = fixture(120)
        frames = data["scenarios"][0]["frames"]
        del frames[100:120]
        result = performance.check(data)[0]
        self.assertGreater(result["fps"], 100)
        self.assertFalse(result["passed"])

    def test_gpu_overload_despite_fast_cpu(self):
        data = fixture(120)
        for frame in data["scenarios"][0]["frames"]:
            frame["gpuMs"] = 30
        self.assertFalse(performance.check(data)[0]["passed"])
        for frame in data["scenarios"][0]["frames"]:
            frame["gpuMs"] = 0
        with self.assertRaises(ValueError):
            performance.check(data)
        data = fixture(120)
        data["scenarios"][0]["frames"][100]["gpuMs"] = 200
        self.assertFalse(performance.check(data)[0]["passed"])

    def test_tail_latency_despite_good_average(self):
        data = fixture(120)
        frames = data["scenarios"][0]["frames"]
        # Alternate fast frames and 33 ms stalls; average remains well above 60.
        data["scenarios"][0]["frames"] = [frame for i, frame in enumerate(frames) if i % 10 not in (3, 4, 5)]
        result = performance.check(data)[0]
        self.assertGreater(result["fps"], 60)
        self.assertFalse(result["passed"])

    def test_freeze_at_capture_boundary(self):
        for start in (True, False):
            data = fixture(120)
            frames = data["scenarios"][0]["frames"]
            data["scenarios"][0]["frames"] = frames[30:] if start else frames[:-30]
            self.assertFalse(performance.check(data)[0]["passed"])

    def test_unqualified_capture(self):
        for key, value in (("visible", False), ("contextLost", True), ("gpuDisjoint", True), ("renderer", "SwiftShader"),
                           ("renderer", "llvmpipe"), ("renderer", ""), ("width", 960),
                           ("finalHeight", 600), ("durationMs", 1000), ("warmupMs", 0), ("frames", [])):
            with self.subTest(key=key, value=value):
                data = fixture()
                data["scenarios"][0][key] = value
                with self.assertRaises(ValueError):
                    performance.check(data)

    def test_invalid_frame(self):
        for key, value in (("draws", 0), ("timeMs", float("nan")), ("timeMs", -1),
                           ("timeMs", 999999), ("timeMs", True), ("gpuMs", None), ("cpuMs", float("inf"))):
            with self.subTest(key=key):
                data = fixture()
                data["scenarios"][0]["frames"][0][key] = value
                with self.assertRaises(ValueError):
                    performance.check(data)
        data = fixture()
        data["scenarios"][0]["frames"][1] = data["scenarios"][0]["frames"][0]
        with self.assertRaises(ValueError):
            performance.check(data)

    def test_missing_scenario_or_identity(self):
        for key, value in (("scenarios", []), ("gameplayReady", False), ("build", {}), ("schema", 2), ("browserHz", 30)):
            data = fixture()
            data[key] = value
            with self.assertRaises(ValueError):
                performance.check(data)

    def test_command_exit_status(self):
        import json
        with tempfile.TemporaryDirectory() as directory:
            report = Path(directory) / "report.json"
            slow = fixture(30)
            slow["browserHz"] = 60
            for contents, status in ((json.dumps(fixture()), 0), (json.dumps(slow), 1), ("{bad", 1)):
                report.write_text(contents)
                result = subprocess.run([sys.executable, str(Path(performance.__file__)), str(report)], capture_output=True)
                self.assertEqual(result.returncode, status, result.stderr.decode())


if __name__ == "__main__":
    unittest.main()
