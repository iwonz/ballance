## Context

See proposal.md for scope. Package APIs return Task and are consumed by startup coroutines and mod/debug code. Built-in content is also loaded under IL2CPP, including single-threaded Web. UI rows retain serialized component names, but pool storage is ordinary managed state.

## Goals / Non-Goals

**Goals:** Remove redundant infrastructure, preserve content/API compatibility where practical, fix observed pool defects, and verify Mono/IL2CPP behavior with regression checks and real player smoke.

**Non-goals:** Replacing IVP with PhysX, introducing ECS/URP/Addressables/UI Toolkit, rewriting saved KeyCode bindings, claiming FPS gains without measurement, or publishing an unsolicited new release.

## Decisions

1. Remove AsyncAwaitUtil and let Unity 6 await AsyncOperation directly. Keep public Task contracts; the five coroutine consumers use WaitUntil followed by GetAwaiter().GetResult(), retaining fault/cancellation propagation without a custom runner. Chunked archive reads use Awaitable.NextFrameAsync. Existing gameplay coroutines remain suitable for their lifecycle.
2. Pin Unity Registry `com.unity.sharp-zip-lib` 1.4.2 and remove the vendored DLL. Use indexed archive reads, library CRC32, and owned streams. Do not substitute the Unity runtime's System.IO.Compression reader: automatic CRC validation only arrived in .NET 11. Package writing streams files through the library instead of allocating a file-sized buffer and computing CRC manually. Keep ZIP compression at level 0 because AssetBundles already use chunk compression.
3. Limit each selected decompressed entry to 512 MiB, allocate its output once, validate exact length and CRC, and yield between 1 MiB chunks. Continue accepting legacy root metadata paths. Close failed network requests/streams and propagate failure through existing package reporting. CRC detects corruption; it is not mod authentication or a sandbox.
4. Use UnityEngine.Pool for UI object and scroll pools. A thin existing GameObjectPool facade tracks rented ownership and the total cap because Unity's maxSize limits retained inactive objects. Keep embedded scene templates explicitly inactive; old parent-wide clearing accidentally hid the package details template. Clear only rented entries, preserve named entry points, and make disposal idempotent. Keep the generic pool API as a small Unity-backed compatibility facade; remove unused FastCachePool and the replaced SimpleObjPool after reference/GUID checks.
5. Use refreshRateRatio and the FullScreenMode overload, preserving exclusive/full-screen/window behavior. File helpers delegate whole-file reads/writes to System.IO; header checks account for short reads and dispose handles.
6. Add focused editor checks to the existing BuildChecks entry point, including corrupt/oversized ZIP data, old/new metadata paths, pool lifecycle/ownership/capacity, and file I/O. Review ratio formatting and saved keys at their UI/startup callers. Use actual player builds/smoke for asynchronous Web behavior rather than pretending synchronous edit-mode assertions cover frame scheduling.
7. A full recompilation exposed obsolete ShaderUtil, object search, refresh-rate, and package-import calls in three vendored tools. Apply direct equivalent Unity 6 APIs, preserving notices; the optional gizmo lookup no longer relies on instance-ID ordering. Remove deprecated com.unity.ide.vscode while retaining com.unity.ide.visualstudio; document these local compatibility patches.

## Risks / Trade-offs

- Vendor namespace/API removal affects source mods referencing implementation helpers: document migration to Unity.SharpZipLib/UnityEngine.Pool/Unity Awaitable; retain package Task contracts and serialized fields.
- Indexed ZIP reads require a seekable stream: local files and downloaded memory buffers satisfy this. Web still downloads a ZIP before reading its selected entry; do not claim network streaming or cryptographic trust.
- Larger official library/new dependencies can offset deleted code in player size: record actual build/file sizes and distinguish them from allocation improvements visible in code.
- Pool callbacks can interact with destruction order: test repeat clear, duplicate/foreign release, capacity exhaustion, and teardown; audit every caller for null results.
- Physical device ergonomics and all-level completion remain separate checks; record untested limits.

## Migration Plan

Record old source/dependency/build sizes, replace implementations and their callers, resolve the pinned package through Unity, run regression checks and content-aware builds, inspect changed paths, then synchronize and archive. Preserve prior build outputs and do not change release version or Pages. Rollback restores the source/dependency change together; no player-data migration is required.

## References

- [Unity Awaitable](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Awaitable.html)
- [Unity ObjectPool](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Pool.ObjectPool_1.html)
- [Unity SharpZipLib](https://docs.unity3d.com/Packages/com.unity.sharp-zip-lib@1.4/manual/index.html)
- [ZIP CRC validation in .NET](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/11/ziparchive-entry-crc32-validation)
- [Screen.SetResolution](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Screen.SetResolution.html)

Unity 6000.6.7f1 also deprecates FindFirstObjectByType. The vendor fallback uses FindAnyObjectByType; no bundled scenes/prefabs reference this optional component, and multi-controller scenes must use the existing explicit reference.
