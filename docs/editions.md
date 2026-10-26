# Edition maintenance

## Source ownership

`original/v1.0.11` is an annotated immutable alias for `d066bfe5`, the existing
`v1.0.11` release. Keep both tags and all existing release assets intact.
The `original` branch owns compatible fixes to the classic experience; `main`
owns Remaster and the shared landing/publication sources. These are complete Unity
projects in separate Git worktrees, not folders imported into the same Unity project.

The local Original worktree is `.worktrees/original`, ignored by the main checkout.
Open only the intended worktree in Unity; each has its own Library, Temp, Builds,
Logs and target configuration. Do not symlink these directories between editions.
Never open the same worktree in two Unity editors or run a batch build against an
editor-open project.

For a shared fix, create a focused commit in the edition where it was found, then
cherry-pick that commit into the other edition only when applicable. Include its
OpenSpec context and verify the receiving edition. Do not merge the complete
original and main branches. Asset GUIDs and serialized identifiers remain stable.

## Identity and compatibility

Ballance retains its historical product name and application identity. Reballance uses
`Reballance` / `com.iwonz.BallanceRemaster` and starts with independent data.
`ProjectSettings/Edition.json` describes the current checkout for the content-aware
builder and packager. A checkout without it is a legacy Original checkout.
The package compatibility version remains separate from the displayed product
version; changing the edition's release version must not invalidate legacy packages.

No automatic save import or synchronization is performed. Original's Web launch page
will remain in the old directory as `original.html`; moving its assets under
`original/` must not change that page directory's persistence identity. Remaster
will use `remaster/`. Verify real browser storage before deploying that relocation.

## Releases

The two games keep independent source versions: `original/v<version>` and
`remaster/v<version>`. `main` develops Reballance and owns distribution tooling;
`original` maintains Ballance. A source tag does not require its own GitHub Release.

Public releases use **`release/v<version>` on main** and contain both games. Each
includes ten platform artifacts and their checksums, the complete shared Pages site
archive and checksum, the source/version manifest, Credits and license. Asset names
remain `Ballance-<game-version>-<platform>` and `Reballance-<game-version>-<platform>`;
the distribution version is separate from either game's version. Historical releases
such as `v1.0.11` and their files remain intact.

`site/releases.json` pins both source tags and all five checksums per edition.
Updating or rolling back one edition leaves the other's source tag and hashes
unchanged; copy its existing verified artifacts into the new distribution. Never
rename old binaries to pretend they are a new game version, or select the companion
edition using “latest”. Follow the [unified release procedure](build-and-release.md#github-pages).
Publication and Pages deployment are owned by main; do not publish a separate
edition release or restore a single-player Pages workflow on original.

The Remaster and two-player site are being implemented in the
[active change](../openspec/changes/build-original-and-remaster-editions/proposal.md).
Until its delivery checks succeed, keep the existing public Original deployment.

The shared build/publication tooling was transferred by focused commit `868f41f9`
and cherry-picked into Original. Its descriptor retains the historical identity;
the branch carries no Remaster runtime or material changes. The immutable baseline
tag remains at d066bfe5 even as maintenance commits advance the branch.

Reballance keeps the pre-rename `com.iwonz.BallanceRemaster` identifier and `ballance.remaster.v1` preference namespace. Desktop file storage resolves through `GamePathManager.PersistentDataPath` to the existing development directory; the macOS native preference domain and Web launch URL remain unchanged. Public names and native output filenames are Ballance and Reballance.
