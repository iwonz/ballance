## Why

The user replaced the shared-repository/distribution arrangement with independent
projects. iwonz/ballance must again contain only the maintained classic game;
Reballance development and unfinished acceptance belong in iwonz/reballance.

## What Changes

- Preserve Reballance history in its own remote before restoring the classic tracked
  tree from original f4acc137 on main with a normal commit.
- Preserve existing release files/tags and archive the original branch tip before
  removing the redundant remote branch. Never merge the two game implementations.
- Restore standalone Ballance releases/Pages and classic-only README/screenshots.
  Keep upstream-first Credits, the verbatim rights notice and all vendor licenses.
- Separate local project caches/build outputs without changing installed apps or saves.

## Capabilities

### Modified Capabilities
- `platform-builds`: independent classic repository and product identity.
- `web-publication`: Ballance-only releases, unchanged public storage directory.

## Impact

Tracked source is restored from the existing classic maintenance branch. Packaging,
Pages workflow, README and operating guides change; gameplay/native files remain
byte-identical to the classic branch. Reballance implementation and its open change
move to their own repository; they are not declared complete here. No binary rebuild,
new game version, acceptance waiver or historical release rewrite is required.
