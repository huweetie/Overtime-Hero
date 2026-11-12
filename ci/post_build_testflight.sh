#!/usr/bin/env bash
set -e

echo "=== Office Rumble: Post-build TestFlight Upload ==="
echo "Arg 1 (workspace): $1"
echo "Arg 2 (build output): $2"
echo "Arg 3 (platform): $3"
echo "UNITY_PLAYER_PATH: $UNITY_PLAYER_PATH"

IPA_PATH=""
if [ -n "$UNITY_PLAYER_PATH" ] && [ -f "$UNITY_PLAYER_PATH" ]; then
  IPA_PATH="$UNITY_PLAYER_PATH"
elif [ -n "$2" ] && [ -d "$2" ]; then
  IPA_PATH=$(find "$2" -name "*.ipa" | head -n 1)
fi

if [ -z "$IPA_PATH" ] || [ ! -f "$IPA_PATH" ]; then
  # Fallback: search build directories
  IPA_PATH=$(find /BUILD_PATH -name "*.ipa" 2>/dev/null | head -n 1)
fi

if [ -z "$IPA_PATH" ] || [ ! -f "$IPA_PATH" ]; then
  echo "Error: No .ipa file found!"
  exit 1
fi

echo "Found IPA at: $IPA_PATH ($(ls -lh "$IPA_PATH" | awk '{print $5}'))"

mkdir -p ~/.appstoreconnect/private_keys
mkdir -p ~/.private_keys

if [ -n "$ASC_PRIVATE_KEY_BASE64" ]; then
  echo "$ASC_PRIVATE_KEY_BASE64" | base64 -d > ~/.appstoreconnect/private_keys/AuthKey_${ASC_KEY_ID}.p8
  cp ~/.appstoreconnect/private_keys/AuthKey_${ASC_KEY_ID}.p8 ~/.private_keys/AuthKey_${ASC_KEY_ID}.p8
  chmod 600 ~/.appstoreconnect/private_keys/AuthKey_${ASC_KEY_ID}.p8
  chmod 600 ~/.private_keys/AuthKey_${ASC_KEY_ID}.p8
fi

echo "Uploading to App Store Connect TestFlight via xcrun altool..."
xcrun altool --upload-app -f "$IPA_PATH" -t ios --apiKey "$ASC_KEY_ID" --apiIssuer "$ASC_ISSUER_ID" --show-progress

echo "App Store Connect accepted the TestFlight upload successfully!"
