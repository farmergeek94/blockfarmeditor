#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

projects=(
	"block-editor"
	"definitions-workspace"
	"property-editor"
	"settings-dashboard"
)

for project in "${projects[@]}"; do
	echo "==> ${project}"
	cd "${ROOT_DIR}/${project}"
	npm install
	npm run build
done
