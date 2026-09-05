#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
: "${PICO_SDK_PATH:?Set PICO_SDK_PATH to a Pico SDK 2.3.0 or newer checkout}"

build() {
  local project="$1"
  local board="$2"
  local source="$repo_root/firmware/$project"
  local output="$source/build"

  cmake -S "$source" -B "$output" -DPICO_BOARD="$board" -DCMAKE_BUILD_TYPE=Release
  cmake --build "$output" --parallel
}

build pico_portal_simulator pico2_w
build pico_portal_xsm3_sidecar pico2
