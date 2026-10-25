#!/usr/bin/env python3
"""Rebuild the pinned IVP bridge with the target's documented native toolchain."""
import argparse
import ast
from concurrent.futures import ThreadPoolExecutor
import os
import hashlib
import json
import platform as host
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


def build(platform, sdk, bridge=None, destination_override=None):
    sdk = sdk.resolve() if sdk else None
    if platform == "WebGL" and not (sdk / "emscripten/emcc.py").is_file():
        raise RuntimeError(f"Install Unity 6000.6.7f1 Web Build Support: {sdk}")
    env = dict(os.environ)
    if platform == "WebGL": env["EM_CONFIG"] = str(sdk / ".emscripten")
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

    compiler = ["python3", str(sdk / "emscripten/emcc.py")] if platform == "WebGL" else [os.environ.get("CXX", "clang++")]
    cross_windows = platform == "Windows" and os.name != "nt"
    flags = ["-std=c++11", "-O2", "-DNDEBUG",
             "-DBALLANCE_SOURCE_IVP", "-fno-exceptions", "-fno-strict-aliasing",
             "-ffp-contract=off",
             "-Wno-deprecated", "-Wno-invalid-source-encoding", "-Wno-null-dereference"]
    if platform == "Windows":
        if cross_windows:
            compiler = ["x86_64-w64-mingw32-g++"]
            if not shutil.which(compiler[0]) or not Path("/usr/lib/wine/wine64").is_file():
                raise RuntimeError("Windows cross-build requires the documented Linux MinGW/Wine image")
        flags += ["-DWIN32", "-D_WINDOWS", "-D_WIN64", "-DPLATFORM_64BITS", "-DNOMINMAX"]
    else:
        flags += ["-DLINUX", "-D_LINUX", "-DPOSIX"]
    if platform == "macOS":
        if host.system() != "Darwin": raise RuntimeError("macOS requires Xcode Command Line Tools")
        flags += ["-arch", "arm64", "-mmacosx-version-min=11.0", "-DOSX", "-DPLATFORM_64BITS", "-fPIC"]
    if platform == "Linux":
        if host.system() != "Linux" or host.machine() not in ("x86_64", "AMD64"):
            raise RuntimeError("Linux requires a native x86-64 clang++ host")
        flags += ["-DPLATFORM_64BITS", "-fPIC"]
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
    bridge = bridge or ROOT / "BallancePhysics/ballance_physics.cpp"
    files = [bridge]
    files += sources(source / "ivp_physics/wscript") + sources(source / "ivp_compact_builder/wscript")
    with tempfile.TemporaryDirectory(dir=cache, prefix="compile-") as temporary:
        output = Path(temporary)

        def compile_file(item):
            index, file = item
            obj = output / f"{index}.o"
            # Upstream Windows diagnostics intentionally print truncated pointer IDs.
            compatibility = ["-fpermissive"] if cross_windows and file != bridge else []
            run(*compiler, "-c", "-x", "c++", *flags, *compatibility, file, "-o", obj, env=env)
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
        elif platform == "Android":
            library = output / "libbphysics.so"
            run(*compiler, "-shared", "-static-libstdc++", "-Wl,-z,max-page-size=16384",
                "-Wl,--no-undefined", "-Wl,-soname,libbphysics.so", *objects, "-o", library)
            headers = subprocess.check_output([str(ndk_bin / "llvm-readelf"), "-lW", str(library)], text=True)
            segments = [line for line in headers.splitlines() if line.strip().startswith("LOAD ")]
            if not segments or any(int(line.split()[-1], 16) < 16384 for line in segments):
                raise RuntimeError("Android physics must support 16 KB memory pages")
            destination = ROOT / "Assets/BallancePhysics/Plugins/Android/libs/arm64-v8a/libbphysics.so"
        else:
            filename = {"macOS": "libbphysics.dylib", "Linux": "libbphysics.so", "Windows": "bphysics.dll"}[platform]
            library = output / filename
            link = ["-dynamiclib", "-arch", "arm64", "-mmacosx-version-min=11.0", "-Wl,-install_name,@rpath/libbphysics.dylib"] if platform == "macOS" else ["-shared"]
            if platform == "Linux": link += ["-Wl,--no-undefined"]
            if cross_windows: link += ["-static", "-static-libstdc++", "-static-libgcc"]
            run(*compiler, *link, *objects, "-o", library, *(["-ldbghelp", "-luser32"] if platform == "Windows" else []))
            smoke = output / ("smoke.exe" if platform == "Windows" else "smoke")
            smoke_flags = ["-arch", "arm64"] if platform == "macOS" else []
            if cross_windows: smoke_flags += ["-static", "-static-libstdc++", "-static-libgcc"]
            run(*compiler, "-std=c++11", *smoke_flags, ROOT / "scripts/physics-smoke.cpp", "-o", smoke,
                *(["-ldl"] if platform == "Linux" else []))
            run(*(["/usr/lib/wine/wine64"] if cross_windows else []), smoke, library)
            folder = {"macOS": "macOS", "Linux": "x86_64", "Windows": "x86_64"}[platform]
            destination = ROOT / "Assets/BallancePhysics/Plugins" / folder / filename
        if destination_override:
            destination = destination_override.resolve()
            if destination.exists(): raise RuntimeError("Refusing to replace diagnostic output")
        elif not destination.is_file():
            raise RuntimeError(f"Refusing an unexpected plugin destination: {destination}")
        shutil.copyfile(library, destination)
        if destination_override:
            print(f"Built diagnostic physics: {destination}")
            return
        if platform == "Windows": shutil.copyfile(library, destination.with_name("bphysics_unity.dll"))
        provenance = {
            "platform": platform, "upstreamRevision": IVP_REVISION, "abi": 2304,
            "patchSha256": hashlib.sha256(patch.read_bytes()).hexdigest(),
            "bridgeSha256": hashlib.sha256((ROOT / "BallancePhysics/ballance_physics.cpp").read_bytes()).hexdigest(),
            "headerSha256": hashlib.sha256((ROOT / "BallancePhysics/ballance_physics.h").read_bytes()).hexdigest(),
            "librarySha256": hashlib.sha256(destination.read_bytes()).hexdigest(),
            "compiler": subprocess.check_output([*compiler, "--version"], env=env, text=True).splitlines()[0],
            "floatingPointContraction": "off",
            "smokeExecuted": platform != "Android",
            "smokeRuntime": "Wine" if cross_windows else "Node" if platform == "WebGL" else "not executed" if platform == "Android" else "host",
        }
        record = ROOT / "BallancePhysics/provenance" / (platform + ".json")
        record.parent.mkdir(exist_ok=True)
        record.write_text(json.dumps(provenance, indent=2) + "\n")
        print(f"Built and checked {platform} physics: {destination}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform", choices=("WebGL", "Android", "macOS", "Windows", "Linux"))
    parser.add_argument("--sdk", type=Path)
    args = parser.parse_args()
    toolchain = "WebGLSupport/BuildTools/Emscripten" if args.platform == "WebGL" else "AndroidPlayer/NDK"
    sdk = args.sdk or (Path("/Applications/Unity/Hub/Editor/6000.6.7f1/PlaybackEngines") / toolchain if args.platform in ("WebGL", "Android") else None)
    build(args.platform, sdk)
