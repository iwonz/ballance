#!/usr/bin/env python3
"""Replay the shipped three-ball parameters at a fixed IVP tick, without Unity."""
import argparse
import ctypes as c
import hashlib
import json
import math
from pathlib import Path
import platform

ROOT = Path(__file__).resolve().parent.parent
P, F, I = c.c_void_p, c.c_float, c.c_int


class Init(c.Structure):
    _fields_ = [("console", I), ("pool", I), ("key", c.c_char * 33)]


class Replay:
    def __init__(self, library):
        self.library = c.CDLL(str(library.resolve()))
        self.entry = self.library.ballance_physics_entry
        self.entry.argtypes, self.entry.restype = [I, c.POINTER(Init)], P
        options = Init(0, 32, b"666dccad4ae697b45aac145f18f49c5b")
        table = self.entry(1, c.byref(options))
        if not table:
            raise RuntimeError("IVP initialization failed")
        self.table = c.cast(table, c.POINTER(P))
        self.version = self.fn(0, I)()
        if self.version not in (2302, 2303, 2304):
            raise RuntimeError("Unexpected IVP ABI")
        self.point = self.fn(9, P, F, F, F)
        self.quat = self.fn(10, P, F, F, F, F)
        self.free_point = self.fn(11, None, P)
        self.free_quat = self.fn(12, None, P)
        self.world = self.fn(1, P, P, F, I, P, P)
        self.free_world = self.fn(2, None, P)
        self.step = self.fn(3, None, P, F)
        self.physicalize = self.fn(18, P, P, c.c_char_p, I, I, I, I,
                                  F, F, F, F, F, F, I, I, I, I, I, I,
                                  P, P, P, I, c.c_char_p, I, P, I, P, F, I)
        self.remove = self.fn(19, None, P, P, I)
        self.impulse = self.fn(34, None, P, P, P)
        self.points = self.fn(53, P, I, P)
        self.free_points = self.fn(55, None, P)
        self.update = self.fn(57, None, P)
        self.contact = self.fn(39, I, P, P)

    def fn(self, index, result, *args):
        return c.CFUNCTYPE(result, *args)(self.table[index])

    def run(self, ball, vertices, fps, ticks=660):
        points, bodies, buffers = [], [], []
        rotation = self.quat(0, 0, 0, 1)
        def point(x, y, z):
            value = self.point(x, y, z)
            points.append(value)
            return value
        gravity = point(0, -20, 0)
        masks = (I * 32)(*([-1] * 32))
        world = self.world(gravity, 1 / 66, -1, masks, None)
        if not world:
            raise RuntimeError("World creation failed")
        def body(name, data, position, fixed, mesh):
            convex = None
            if mesh:
                flat = [v for vertex in mesh for v in vertex]
                buffer = self.points(len(mesh), (F * len(flat))(*flat))
                buffers.append(buffer)
                convex = (P * 1)(buffer)
            value = self.physicalize(world, name.encode(), 1, 0, 0, 0,
                data["Mass"], data["Friction"], data["Elasticity"], data["LinearDamp"],
                data["RotDamp"], data["BallRadius"], int(not mesh), 1, 1, 1, 0, int(fixed),
                point(*position), None, rotation, 0, b"", int(bool(mesh)), convex, 0, None, 0, 0)
            if not value:
                raise RuntimeError("Body creation failed: " + name)
            bodies.append(value)
            return value
        try:
            floor_data = dict(Mass=1, Friction=.7, Elasticity=.3, LinearDamp=0, RotDamp=0, BallRadius=0)
            floor = body("replay-floor", floor_data, (0, -2, 0), True,
                         [(x, y, z) for x in (-300, 300) for y in (-2, 2) for z in (-300, 300)])
            moving = body("replay-ball", ball, (0, 2.5, 0), False, vertices if ball["BallRadius"] == 0 else None)
            # Use the exact bridge transform prefix consumed by PhysicsEnvironment.cs.
            transform = (F * 7).from_address(moving + c.sizeof(I))
            samples, tick, frame, contacts = [], 0, 0, 0
            while tick < ticks:
                frame += 1
                due = min(ticks, frame * 66 // fps)
                while tick < due:
                    self.step(world, 1 / 66)
                    self.update(world)
                    contacts += bool(self.contact(moving, floor))
                    # Match the game: impulses at the ball center after simulation.
                    force = ball["Force"] if 66 <= tick < 264 else -ball["Force"] if 330 <= tick < 396 else 0
                    if force:
                        pos, push = self.point(*transform[:3]), self.point(0, 0, force)
                        self.impulse(moving, pos, push)
                        self.free_point(push)
                        self.free_point(pos)
                    tick += 1
                    if tick % 66 == 0:
                        sample = list(transform)
                        if not all(math.isfinite(value) for value in sample):
                            raise RuntimeError("Non-finite replay transform")
                        samples.append(sample)
            if contacts < ticks // 2:
                raise RuntimeError("Replay did not maintain floor contact")
            return {"fps": fps, "ticks": ticks, "contactTicks": contacts, "samples": samples}
        finally:
            for value in reversed(bodies):
                self.remove(world, value, 1)
            for value in buffers:
                self.free_points(value)
            self.free_world(world)
            for value in points:
                self.free_point(value)
            self.free_quat(rotation)

    def close(self):
        self.entry(0, None)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("library", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--compare", type=Path)
    args = parser.parse_args()
    if args.output.exists():
        raise SystemExit("Refusing to replace an existing measurement")
    balls_path = ROOT / "Assets/Game/GamePlay/Resources/GamePhysBallData.json"
    mesh_path = ROOT / "Assets/Import/Moduls/P_Ball_Paper_MF.obj"
    vertices = [tuple(float(v) for v in line.split()[1:4]) for line in mesh_path.read_text().splitlines() if line.startswith("v ")]
    report = {"schema": 1, "host": platform.platform(), "abi": None,
              "librarySha256": hashlib.sha256(args.library.read_bytes()).hexdigest(),
              "parametersSha256": hashlib.sha256(balls_path.read_bytes()).hexdigest(),
              "paperMeshSha256": hashlib.sha256(mesh_path.read_bytes()).hexdigest(),
              "scope": "Native flat-course replay; excludes Unity input sampling, camera, rendering and level mechanisms.",
              "runs": {}}
    engine = Replay(args.library)
    try:
        report["abi"] = engine.version
        for name, ball in json.loads(balls_path.read_text()).items():
            report["runs"][name] = [engine.run(ball, vertices, fps) for fps in (30, 60, 120, 144) for _ in range(3)]
    finally:
        engine.close()
    report["maximumRepeatDelta"] = max(abs(a - b) for runs in report["runs"].values()
        for run in runs for sample, first in zip(run["samples"], runs[0]["samples"])
        for a, b in zip(sample, first))
    # The floor allows float representation noise, not an arbitrary gameplay tolerance.
    report["comparisonTolerance"] = max(1e-6, report["maximumRepeatDelta"] * 2)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n")
    if args.compare:
        baseline = json.loads(args.compare.read_text())
        for key in ("parametersSha256", "paperMeshSha256"):
            if report[key] != baseline[key]:
                raise SystemExit("Replay fixture differs: " + key)
        delta = max(abs(a - b) for name, runs in report["runs"].items()
            for run, before in zip(runs, baseline["runs"][name])
            for sample, previous in zip(run["samples"], before["samples"])
            for a, b in zip(sample, previous))
        if delta > baseline["comparisonTolerance"]:
            raise SystemExit(f"Dry replay regression: {delta} > {baseline['comparisonTolerance']}")
        print(f"Dry replay comparison passed: maximum delta {delta:g}")
    print(f"Captured 36 three-ball replays; repeat delta {report['maximumRepeatDelta']:g}: {args.output}")


if __name__ == "__main__":
    main()
