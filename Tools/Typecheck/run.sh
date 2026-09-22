#!/usr/bin/env bash
# Compiles every OutpostZero assembly against the Unity 6 managed DLLs and runs the
# EditMode tests on .NET. No Unity license is needed. Tests that reach native engine
# calls (JsonUtility, AssetDatabase) cannot run here and are reported, not failed.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
WORK="$REPO/.typecheck"
DOTNET="${DOTNET:-dotnet}"
VERSION="$(sed -n 's/^m_EditorVersionWithRevision: \([^ ]*\) (\([0-9a-f]*\))/\1 \2/p' "$REPO/ProjectSettings/ProjectVersion.txt")"
REVISION="${VERSION#* }"
UNITY_DATA="${UNITY_DATA:-$WORK/unity/Editor/Data}"

mkdir -p "$WORK/pkgs"

if [ ! -f "$UNITY_DATA/Managed/UnityEngine/UnityEngine.CoreModule.dll" ]; then
  echo "Fetching Unity ${VERSION% *} managed assemblies"
  mkdir -p "$WORK/unity"
  curl -sSL "https://download.unity3d.com/download_unity/$REVISION/LinuxEditorInstaller/Unity.tar.xz" \
    | tar -xJ -C "$WORK/unity" --wildcards \
      'Editor/Data/Managed/UnityEngine/*' \
      'Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/*'
fi

fetch() {
  local name="$1" version
  version="$(python3 -c "import json,sys;print(json.load(open('$REPO/Packages/manifest.json'))['dependencies']['$name'])")"
  if [ ! -f "$WORK/pkgs/$name/.version" ] || [ "$(cat "$WORK/pkgs/$name/.version")" != "$version" ]; then
    rm -rf "$WORK/pkgs/$name" && mkdir -p "$WORK/pkgs/$name"
    curl -sSL "https://download.packages.unity.com/$name/-/$name-$version.tgz" | tar -xz -C "$WORK/pkgs/$name"
    echo "$version" > "$WORK/pkgs/$name/.version"
  fi
}
fetch com.unity.inputsystem
fetch com.unity.ai.navigation
fetch com.unity.test-framework

props=(-p:UnityData="$UNITY_DATA" -p:PkgRoot="$WORK/pkgs" -nologo -v:q)
status=0
for project in Game GameEditor Editor Tests Runner; do
  echo "== build $project"
  log="$WORK/build-$project.log"
  if "$DOTNET" build "$HERE/$project/$project.csproj" "${props[@]}" > "$log" 2>&1; then
    echo "ok"
  else
    status=1
    grep -E " error " "$log" | sed "s|$REPO/||; s| \[.*||" | sort -u
  fi
done
[ "$status" = "0" ] || { echo "Compile failed"; exit 1; }

echo "== EditMode tests"
rm -rf "$WORK/results"
REPO_ROOT="$REPO" "$DOTNET" test "$HERE/Runner/Runner.csproj" --no-build "${props[@]}" \
  --results-directory "$WORK/results" --logger "trx;LogFileName=editmode.trx" >/dev/null 2>&1 || true
python3 "$HERE/triage.py" "$WORK/results/editmode.trx"
