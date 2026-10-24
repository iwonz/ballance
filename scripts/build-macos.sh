#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/2021.3.33f1/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$unity_editor" ]]; then
  echo 'Install Unity 2021.3.33f1 with Mac Build Support (Mono), or set UNITY_EDITOR to its executable.' >&2
  exit 1
fi

cd "$project_dir"
"$unity_editor" -batchmode -nographics -quit \
  -projectPath "$project_dir" -buildTarget StandaloneOSX \
  -executeMethod Ballance2.Editor.MacOSBuild.BuildAppleSilicon \
  -customBuildPath "$project_dir/Builds/macOS/Ballance.app" -logFile -
python3 scripts/package-macos.py Builds/macOS/Ballance.app Builds/Ballance-macOS-AppleSilicon.zip
