#!/usr/bin/env bash
set -e

echo "=== Office Rumble: Post-build TestFlight Upload ==="
WORKSPACE_DIR="$1"
OUTPUT_DIR="$3"

echo "Workspace dir: $WORKSPACE_DIR"
echo "Output dir: $OUTPUT_DIR"

IPA_PATH="$OUTPUT_DIR/build.ipa"
if [ ! -f "$IPA_PATH" ]; then
  IPA_PATH=$(find "$OUTPUT_DIR" -name "*.ipa" | head -n 1)
fi

if [ -z "$IPA_PATH" ] || [ ! -f "$IPA_PATH" ]; then
  echo "Error: No .ipa file found in $OUTPUT_DIR"
  ls -la "$OUTPUT_DIR"
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
