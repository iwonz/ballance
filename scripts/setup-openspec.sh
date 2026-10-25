#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
version=1.11.0
if ! command -v openspec >/dev/null || [[ "$(openspec --version)" != "$version" ]]; then
  echo "Install OpenSpec $version with Node >=20.19.0: npm install -g @fission-ai/openspec@$version" >&2
  exit 1
fi

# Isolate the generation profile so repository setup cannot change user preferences.
config_dir=$(mktemp -d "${TMPDIR:-/tmp}/ballance-openspec.XXXXXX")
trap 'rm -rf "$config_dir"' EXIT
mkdir -p "$config_dir/openspec"
cat > "$config_dir/openspec/config.json" <<'JSON'
{
  "profile": "custom",
  "delivery": "skills",
  "workflows": ["explore", "propose", "new", "continue", "ff", "apply", "update", "verify", "sync", "archive"]
}
JSON
XDG_CONFIG_HOME="$config_dir" OPENSPEC_TELEMETRY=0 openspec init --tools agents --profile custom --no-animation
