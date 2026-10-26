#!/usr/bin/env python3
"""Rebuild WebGL or Android ARM64 physics using Unity's bundled toolchain."""
import argparse
import ast
from concurrent.futures import ThreadPoolExecutor
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parent.parent
IVP_REVISION = "47533475e01cbff05fbc3bbe8b4edc485f292cea"


def run(*args, **kwargs):
    subprocess.run([str(arg) for arg in args], check=True, **kwargs)


def sources(script):
    for node in ast.walk(ast.parse(script.read_text())):
        if (isinstance(node, ast.Assign) and isinstance(node.targets[0], ast.Name)
                and node.targets[0].id == "source"):
            return [(script.parent / name).resolve() for name in ast.literal_eval(node.value)]
    raise RuntimeError(f"No source list in {script}")


def build(platform, sdk):
    sdk = sdk.resolve()
    if platform == "WebGL" and not (sdk / "emscripten/emcc.py").is_file():
        raise RuntimeError(f"Install Unity 6000.6.7f1 Web Build Support: {sdk}")
    env = dict(os.environ, EM_CONFIG=str(sdk / ".emscripten"))
    cache = ROOT / "Library/NativePhysics"
    cache.mkdir(parents=True, exist_ok=True)
    source = cache / "source"
    if not source.exists():
        with tempfile.TemporaryDirectory(dir=cache, prefix="download-") as temporary:
            checkout = Path(temporary) / "source"
            run("git", "init", checkout)
            run("git", "-C", checkout, "fetch", "--depth=1",
                "https://github.com/nillerusr/source-physics.git", IVP_REVISION)
            run("git", "-C", checkout, "checkout", "--detach", "FETCH_HEAD")
            checkout.rename(source)
    revision = subprocess.check_output(["git", "-C", str(source), "rev-parse", "HEAD"], text=True).strip()
    if revision != IVP_REVISION:
        raise RuntimeError("Unexpected IVP revision; move Library/NativePhysics/source aside and retry")
    patch = ROOT / "BallancePhysics/source-physics.patch"
    applied = subprocess.run(["git", "-C", str(source), "apply", "--reverse", "--check", str(patch)],
                             capture_output=True).returncode == 0
    if not applied:
        run("git", "-C", source, "apply", "--check", patch)
        run("git", "-C", source, "apply", patch)

    compiler = ["python3", str(sdk / "emscripten/emcc.py")]
    flags = ["-std=c++11", "-O2", "-DNDEBUG", "-DLINUX", "-D_LINUX", "-DPOSIX",
             "-DBALLANCE_SOURCE_IVP", "-fno-exceptions", "-fno-strict-aliasing",
             "-Wno-deprecated", "-Wno-invalid-source-encoding", "-Wno-null-dereference"]
    # Convex builders use setjmp; match the Wasm 2023 player's native longjmp ABI.
    web_flags = ["-sSUPPORT_LONGJMP=wasm", "-msimd128"] if platform == "WebGL" else []
    flags += web_flags
    if platform == "Android":
        ndk_bin = next((sdk / "toolchains/llvm/prebuilt").glob("*/bin"))
        compiler = [str(ndk_bin / "aarch64-linux-android26-clang++")]
        flags += ["-DPLATFORM_64BITS", "-fPIC"]
    for directory in sorted(source.glob("ivp_*")) + [source / "havana/havok"]:
        if directory.is_dir():
            flags += ["-I", str(directory)]
    files = [ROOT / "BallancePhysics/ballance_physics.cpp"]
    files += sources(source / "ivp_physics/wscript") + sources(source / "ivp_compact_builder/wscript")
    with tempfile.TemporaryDirectory(dir=cache, prefix="compile-") as temporary:
        output = Path(temporary)

        def compile_file(item):
            index, file = item
            obj = output / f"{index}.o"
            run(*compiler, "-c", "-x", "c++", *flags, file, "-o", obj, env=env)
            return obj

        with ThreadPoolExecutor(max_workers=min(os.cpu_count() or 1, 6)) as pool:
            objects = list(pool.map(compile_file, enumerate(files)))
        if platform == "WebGL":
            library = output / "libbphysics.a"
            run("python3", sdk / "emscripten/emar.py", "rcs", library, *objects, env=env)
            smoke = output / "smoke.js"
            run(*compiler, "-x", "c++", "-std=c++11", "-O2", "-fno-exceptions",
                *web_flags,
                ROOT / "scripts/physics-smoke.cpp", "-x", "none", library,
                "-sENVIRONMENT=node", "-o", smoke, env=env)
            node = sdk / "node" / ("node.exe" if os.name == "nt" else "node")
            run(node, smoke)
            destination = ROOT / "Assets/BallancePhysics/Plugins/WebGL/libbphysics.a"
        else:
            library = output / "libbphysics.so"
            run(*compiler, "-shared", "-static-libstdc++", "-Wl,-z,max-page-size=16384",
                "-Wl,--no-undefined", "-Wl,-soname,libbphysics.so", *objects, "-o", library)
            headers = subprocess.check_output([str(ndk_bin / "llvm-readelf"), "-lW", str(library)], text=True)
            segments = [line for line in headers.splitlines() if line.strip().startswith("LOAD ")]
            if not segments or any(int(line.split()[-1], 16) < 16384 for line in segments):
                raise RuntimeError("Android physics must support 16 KB memory pages")
            destination = ROOT / "Assets/BallancePhysics/Plugins/Android/libs/arm64-v8a/libbphysics.so"
        shutil.copyfile(library, destination)
        print(f"Built and checked {platform} physics: {destination}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform", choices=("WebGL", "Android"))
    parser.add_argument("--sdk", type=Path)
    args = parser.parse_args()
    toolchain = "WebGLSupport/BuildTools/Emscripten" if args.platform == "WebGL" else "AndroidPlayer/NDK"
    sdk = args.sdk or Path("/Applications/Unity/Hub/Editor/6000.6.7f1/PlaybackEngines") / toolchain
    build(args.platform, sdk)
