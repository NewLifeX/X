#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT="$ROOT/Samples/Mobile.iOS/Mobile.iOS.csproj"
CONFIGURATION="${CONFIGURATION:-Release}"
RUNTIME="${RUNTIME:-iossimulator-arm64}"
BUNDLE_ID="com.newlife.mobile"
TIMEOUT="${TIMEOUT:-120}"
ARTIFACTS="$ROOT/Bin/Mobile.iOS/artifacts"
INCLUDE_STARDUST="${INCLUDE_STARDUST:-false}"

mkdir -p "$ARTIFACTS"

if [ "$INCLUDE_STARDUST" = "true" ]; then
  dotnet build "$PROJECT" -c "$CONFIGURATION" -f net10.0-ios -p:RuntimeIdentifier="$RUNTIME" -p:IncludeStardust=true --nologo
else
  dotnet build "$PROJECT" -c "$CONFIGURATION" -f net10.0-ios -p:RuntimeIdentifier="$RUNTIME" --nologo
fi

APP="$(find "$ROOT/Bin/Mobile.iOS" -maxdepth 5 -name "*.app" -print -quit)"
if [ -z "$APP" ]; then
  echo "iOS app bundle not found."
  exit 1
fi

UDID="$(xcrun simctl list devices available | awk -F '[()]' '/iPhone/ {print $2; exit}')"
if [ -z "$UDID" ]; then
  echo "No iPhone simulator available."
  exit 1
fi

echo "Using simulator $UDID"
xcrun simctl boot "$UDID" 2>/dev/null || true
xcrun simctl bootstatus "$UDID" -b
xcrun simctl install "$UDID" "$APP"
xcrun simctl terminate "$UDID" "$BUNDLE_ID" 2>/dev/null || true
if [ "$INCLUDE_STARDUST" = "true" ]; then
  xcrun simctl launch "$UDID" "$BUNDLE_ID" --smoke --stardust
else
  xcrun simctl launch "$UDID" "$BUNDLE_ID" --smoke
fi

RESULT=""
DEADLINE=$((SECONDS + TIMEOUT))
while [ "$SECONDS" -lt "$DEADLINE" ]; do
  DATA_DIR="$(xcrun simctl get_app_container "$UDID" "$BUNDLE_ID" data 2>/dev/null || true)"
  if [ -n "$DATA_DIR" ]; then
    RESULT="$(find "$DATA_DIR" -name smoke.json -print -quit 2>/dev/null || true)"
    if [ -n "$RESULT" ]; then
      break
    fi
  fi
  sleep 0.5
done

if [ -z "$RESULT" ]; then
  echo "smoke.json not found within ${TIMEOUT}s. Recent app log:"
  xcrun simctl spawn "$UDID" log show --last 2m --style compact --predicate 'process == "Mobile"' || true
  exit 1
fi

cp "$RESULT" "$ARTIFACTS/ios-smoke.json"
cat "$RESULT"

if command -v jq >/dev/null 2>&1; then
  jq -e '.fail == 0' "$RESULT" >/dev/null
else
  grep -q '"fail":0' "$RESULT"
fi

echo "iOS smoke passed."
