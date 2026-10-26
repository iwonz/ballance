# Repository independence

## Source ownership

- [iwonz/ballance](https://github.com/iwonz/ballance), `main`: classic Ballance, compatibility and fixes.
- [iwonz/reballance](https://github.com/iwonz/reballance), `main`: Reballance, its environment, presentation, documentation, releases and site.

Use separate clones with their own `Library`, `Temp`, `Builds` and native plugins.
Transfer shared fixes as focused reviewed commits and validate in the receiving
repository. Do not merge the complete projects. The old two-branch and combined
release arrangement is superseded. Existing history is preserved, not rewritten.

## Identity and compatibility

Ballance retains `com.MengyuGame.Ballance`; Reballance retains
`com.iwonz.BallanceRemaster` and the `ballance.remaster.v1` preference namespace.
The technical `remaster` descriptor remains an internal compatibility identifier.
Do not rename package IDs, GUIDs, native ABI contracts or serialized keys to match
the repository name. Reballance uses ABI 2304; Ballance retains ABI 2302.
Desktop data resolves through `GamePathManager.PersistentDataPath`. Existing native
settings and saves remain in place. There is no automatic cross-game import.

Ballance stays at `/ballance/`, preserving existing Web storage. Reballance's first
public player will use `/reballance/play/`; it has no previously published Web
profile to migrate. Repository moves do not move or delete native profiles.

## Releases

Each repository publishes its own `v<version>` tags, five platform archives and
checksums, then deploys its matching Web version to its own Pages site. Asset names
retain the product: `Ballance-...` or `Reballance-...`. A release or rollback of one
project does not depend on the other. See the [release guide](build-and-release.md#github-pages).

Ballance's existing `v1.0.11` and annotated `original/v1.0.11` still point to
`d066bfe5`. Published assets remain untouched. The last `original` branch commit is
preserved in the Ballance repository as `archive/original-before-repository-split`.
Reballance history was copied before classic ownership was restored on Ballance main.
Reballance acceptance is tracked in its own repository and does not gate classic releases.
