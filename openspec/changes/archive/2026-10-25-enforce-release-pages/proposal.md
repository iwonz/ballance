## Why

Release publication currently skips GitHub Pages silently when a Web asset is absent. The maintainer requires every release to include the matching Web build and considers delivery complete only when that version is live on Pages.

## What Changes

- Make Pages preparation mandatory for every published release; missing or mismatched Web assets fail the workflow.
- Verify the release tag against the archive name and embedded Unity Web version, then publish a release identity file and check it after deployment.
- Update agent/release instructions so publication includes successful Pages rollout and browser verification.
- Prepare version 1.0.10 for Windows, Linux, macOS ARM64, Android ARM64, and Web using Unity 6000.6.7f1; iOS remains deferred.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-publication`: Mandatory deployment, release/version matching, and verification of the live release identity.

## Impact

Pages workflow, preparation script and its callers/tests, agent/release documentation, and Player Settings version metadata. Existing extraction/checksum protections stay intact. No gameplay logic or native ABI changes. Publishing remains an external delivery step after the implementation and build-readiness record are complete; its workflow and release notes retain live deployment evidence.
