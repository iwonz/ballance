#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_dir"
unity_version="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$unity_version/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$unity_editor" ]]; then
  echo "Install Unity $unity_version and its target module, or set UNITY_EDITOR to its executable." >&2
  exit 1
fi
case "${1:-}" in
  macOS) target=StandaloneOSX ;;
  Windows) target=StandaloneWindows64 ;;
  Linux) target=StandaloneLinux64 ;;
  Android) target=Android ;;
  iOS) target=iOS ;;
  WebGL) target=WebGL ;;
  *) echo 'Usage: bash scripts/build.sh {macOS|Windows|Linux|Android|iOS|WebGL}' >&2; exit 2 ;;
esac
mkdir -p Logs
"$unity_editor" -batchmode -nographics -quit \
  -projectPath "$project_dir" -buildTarget "$target" \
  -executeMethod Ballance2.Editor.PlayerBuild.Build -logFile "$project_dir/Logs/build-$1.log"
echo "Built $1. See Builds/$1 and Logs/build-$1.log."
