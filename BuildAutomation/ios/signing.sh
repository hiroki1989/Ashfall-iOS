#!/usr/bin/env bash
set -euo pipefail
umask 077
required=(IOS_P12_BASE64 IOS_P12_PASSWORD IOS_PROFILE_BASE64 APPLE_TEAM_ID)
if [[ "$DISTRIBUTION" == app-store ]]; then required+=(ASC_KEY_P8 ASC_KEY_ID ASC_ISSUER_ID); fi
for name in "${required[@]}"; do
  if [[ -z "${!name:-}" ]]; then echo "::error::Missing repository secret: $name"; exit 1; fi
done
mkdir -p "$IOS_WORK"
python3 BuildAutomation/ios/credentials.py decode
KEYCHAIN_PASSWORD="$(cat "$IOS_WORK/keychain-password")"
security create-keychain -p "$KEYCHAIN_PASSWORD" "$IOS_WORK/signing.keychain-db"
security set-keychain-settings -lut 21600 "$IOS_WORK/signing.keychain-db"
security unlock-keychain -p "$KEYCHAIN_PASSWORD" "$IOS_WORK/signing.keychain-db"
security import "$IOS_WORK/distribution.p12" -P "$IOS_P12_PASSWORD" -k "$IOS_WORK/signing.keychain-db" -T /usr/bin/codesign -T /usr/bin/security
security set-key-partition-list -S apple-tool:,apple:,codesign: -k "$KEYCHAIN_PASSWORD" "$IOS_WORK/signing.keychain-db" >/dev/null
security list-keychains -d user -s "$IOS_WORK/signing.keychain-db" "$HOME/Library/Keychains/login.keychain-db"
security cms -D -i "$IOS_WORK/profile.mobileprovision" > "$IOS_WORK/profile.plist"
python3 BuildAutomation/ios/credentials.py validate
