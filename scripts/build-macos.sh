#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
bash "$project_dir/scripts/build.sh" macOS
python3 "$project_dir/scripts/package-build.py" macOS
