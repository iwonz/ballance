#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$unity_editor" ]]; then
  echo 'Install the macOS ARM64 editor for Unity 6000.6.7f1, or set UNITY_EDITOR to its executable.' >&2
  exit 1
fi

cd "$project_dir"
version="$(sed -n 's/^  bundleVersion: //p' ProjectSettings/ProjectSettings.asset)"
if [[ ! "$version" =~ ^[a-zA-Z0-9._-]+$ ]]; then
  echo 'Invalid bundleVersion for a release filename.' >&2
  exit 1
fi
archive="Builds/Ballance-${version}-macOS-AppleSilicon.zip"
if [[ -e Builds/macOS/Ballance.app || -e "$archive" || -e "$archive.sha256" ]]; then
  echo 'Move previous build outputs aside before rebuilding.' >&2
  exit 1
fi
"$unity_editor" -batchmode -nographics -quit \
  -projectPath "$project_dir" -buildTarget StandaloneOSX \
  -executeMethod Ballance2.Editor.MacOSBuild.BuildAppleSilicon \
  -customBuildPath "$project_dir/Builds/macOS/Ballance.app" -logFile -
python3 scripts/package-macos.py Builds/macOS/Ballance.app "$archive"
