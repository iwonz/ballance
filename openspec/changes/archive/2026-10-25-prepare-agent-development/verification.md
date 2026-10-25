# Verification: agent-development baseline

Baseline: `52a1835f` (release 1.0.9). This change documents existing behavior and
establishes the development process; it does not implement new gameplay or enable iOS.

## Checks

| Check performed | Result |
| --- | --- |
| `openspec validate --all --strict --no-interactive` after synchronization | 13 current specs plus this change passed |
| `python3 scripts/check-agent-context.py` | Language boundaries, local links/anchors, source-map coverage, required skills, pinned version, and record structure passed |
| `python3 scripts/test-agent-context.py` | 5 tests passed, including non-English rejection, localized fixtures, missing links, incomplete evidence, nested deltas, and Git delivery cases |
| `python3 scripts/check-agent-context.py --base 52a1835f` before archive | Rejected the delivery as expected because no new completed archive existed yet |
| The same delivery check after archive | Passed with the new completed record in the diff |
| `openspec archive prepare-agent-development --skip-specs --yes` | Completed with all 12 tasks finished; archived as `2026-10-25-prepare-agent-development` |
| Strict OpenSpec validation and `openspec list --json` after archive | All 13 current specs passed; no active changes remained |
| `bash scripts/setup-openspec.sh` repeated | All 10 skill files reproduced byte-for-byte; project and global OpenSpec configuration fingerprints unchanged |
| `python3 scripts/check-localization.py` | 294 Russian translations across 12 resources passed placeholder/language checks |
| `python3 scripts/check-physics-abi.py` | 89 managed delegates matched native declarations |
| `python3 scripts/test-release-tools.py` | 5 tests passed, covering valid Web extraction and corruption/unsafe/incomplete input rejection |
| `bash -n scripts/build.sh scripts/build-macos.sh scripts/setup-openspec.sh` | Passed |
| `actionlint` against all workflows | Passed, including the new hosted context job |
| `git diff --check` | Passed after removing inherited trailing whitespace on edited lines |
| Unity 6000.6.7f1 batch compilation and `Ballance2.Editor.BuildChecks.Run` | Exit 0; six module/path checks, virtual input checks, and localization checks passed |
| ARM64 native physics smoke, compiled with `-std=c++11 -Wall -Wextra -Werror` | Passed API 2302, vectors, simulation/collision, fixed constraint, and cleanup |

Unity was run with `-batchmode -nographics -quit -buildTarget StandaloneOSX`.
Its log reported `LOCALIZATION_CHECKS_PASSED: 205 prefab labels` and the successful
input/build completion messages. The six existing CS0618 warnings concern obsolete
resolution APIs; no C# compile errors or exception markers were found. The iOS
module availability check did not export or publish iOS.

The native smoke executable was compiled from `scripts/physics-smoke.cpp` and loaded
the existing `Assets/BallancePhysics/Plugins/macOS/libbphysics.dylib`; no native
library was rebuilt or replaced.

## Results

### Completeness

The 13 capabilities contain 56 requirements and 77 scenarios. All were reviewed
against the implementation paths in the [source map](/docs/architecture.md).
The baseline represents inspected behavior, not 77 newly executed gameplay tests.

| Capability | Requirements / scenarios | Evidence for this change |
| --- | --- | --- |
| agent-development | 5 / 8 | Full bootstrap lifecycle, setup reproduction, context self-tests, failure gate, workflow lint |
| runtime-lifecycle | 4 / 6 | Startup/service/package/scene/quit source and callers reviewed; Unity compilation |
| package-management | 4 / 6 | Discovery, metadata, dependency/refcount, backend branches, and trust UI inspected |
| level-loading | 5 / 7 | Native/NMO loaders, validation, preview, mechanism construction, and teardown inspected |
| gameplay | 5 / 9 | Ball, sector, transformation, score/life, pause, restart, and completion paths inspected |
| physics-runtime | 4 / 4 | Managed/native ownership and callback review, 89-signature check, ARM64 smoke |
| player-input | 4 / 5 | Binding/touch/layout callers reviewed; Unity virtual keyboard, mouse, and touch checks |
| user-interface | 4 / 6 | Page stack, dialogs, prefab bindings, HUD, and sound lifecycle reviewed |
| localization | 4 / 5 | Python resource checks; Unity fallback, glyph, language-button, and 205-label checks |
| player-persistence | 4 / 5 | PlayerPrefs namespaces/types, score/pass-state JSON, package XML, and quit handlers reviewed |
| developer-tools | 4 / 5 | Console, editor packers, mechanism hooks, and debug/help text reviewed; Unity compilation |
| platform-builds | 5 / 5 | Build/packaging entry points and workflows reviewed; existing packaging checks and module checks |
| web-publication | 4 / 6 | Pages preparation/workflow reviewed; extraction safety tests and workflow lint |

### Correctness

The first translation pass changed 2,998 unique comment/region lines across 225
source files (4,068 occurrences). Follow-up inspection covered shader includes,
XML template comments, and legacy Windows resource comments. A separate reviewed
map translated 487 developer-facing string values: inspector help, console help,
diagnostics, and editor labels. Localized assertions, the legacy test-level ID,
and the original editor copyright notice were preserved explicitly.

A lexical comparison against the baseline checked all **237 changed source files**.
After accounting for comments, region labels, the reviewed literal map, and trailing
whitespace on edited lines, the remaining executable source was identical. This
includes C#, C++, headers, shaders/includes, and Windows resource source. The check
preserved strings before identifying comment delimiters, including URL and verbatim
string cases. No type/member names, serialized keys, event IDs, or asset paths were
renamed.

Separate structural comparisons verified the two authoring templates and three
Visual Studio filter files. Only descriptive placeholders/category labels changed;
configuration keys, non-display values, source paths, and GUIDs were retained.
Existing levels, localized resource values, prefab/scene data, `.meta` files, and
shipped binaries were not edited.

### Coherence

Behavior lives in capability specs, navigation and known gaps in the source map,
commands in operating guides, and historical evidence here. The outdated Chinese
README duplicate was removed; its useful current information is covered by the
English entry point and guides. There is no parallel `openspec/docs` tree.

The existing Unity/C#/native/Python tooling remains in place. OpenSpec is pinned
without adding a JavaScript application or modifying global user preferences.
The ten generated shared skills include explicit verify/sync stages. Root and
scoped instructions require full changes even for documentation-only work, with
`skip_specs: true` only when no behavioral delta is appropriate.

All baseline main specs were synchronized from the CLI-reported delta paths. Their
Purpose, requirements, and scenarios match the deltas; only the specification title
and `## Requirements` header differ. Archive uses `--skip-specs` to avoid applying
that verified synchronization twice. No implementation or design discrepancy remains
that blocks completing this preparation change.

OpenSpec emitted a non-blocking suggestion to split changes containing more than
ten deltas. The thirteen deltas are intentionally one atomic bootstrap of the existing
game and its development rules; future feature changes should use their narrower
affected capabilities.

## Limitations

- The 77 scenarios are source-backed contracts, not a complete automated regression
  suite. Future behavior changes must add the relevant runtime evidence using the
  validation guide. Known package-trust, corrupt-save, legacy NMO, and unfinished
  editor limitations are recorded in the current source map rather than hidden.
- No new player build, browser gameplay session, physical Android/Windows/Linux
  gameplay, or 13-level playthrough was performed. Previous release 1.0.9 evidence
  is explicitly historical. No release or Pages deployment was changed.
- Editor labels/tooltips and diagnostic wording compile, but every translated
  editor window was not visually exercised. Review layout when those tools change.
- The new hosted job was linted and its commands were run locally; it has not yet
  run on GitHub for this uncommitted change. No branch-protection settings changed.
- Language checks detect the Cyrillic/CJK maintenance text present in this baseline;
  they cannot prove English fluency or semantic correctness. Localized values,
  stable identifiers, and original vendor/legal material have narrow boundaries.
  Link checks validate current local documentation, not external URL availability
  or historical archive links after future source moves.
- A completed-record gate proves artifacts exist in the delivery, not that an agent
  followed every step or implemented every scenario. Human/agent review remains
  necessary. Six existing obsolete-resolution compiler warnings remain unchanged.
