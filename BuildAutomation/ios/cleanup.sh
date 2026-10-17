#!/usr/bin/env bash
set -uo pipefail
# Paths are restricted to the job-specific runner temporary directory.
if [[ -z "${IOS_WORK:-}" || "$IOS_WORK" != "$RUNNER_TEMP/ashfall-ios" ]]; then
  echo '::error::Unexpected credential cleanup directory'; exit 1
fi
security delete-keychain "$IOS_WORK/signing.keychain-db" 2>/dev/null || true
if [[ -f "$IOS_WORK/installed-profile-path" ]]; then
  profile="$(cat "$IOS_WORK/installed-profile-path")"
  if [[ "$profile" == "$HOME/Library/MobileDevice/Provisioning Profiles/"*.mobileprovision ]]; then
    rm -f -- "$profile"
  fi
fi
rm -f -- "$IOS_WORK/distribution.p12" "$IOS_WORK/profile.mobileprovision" "$IOS_WORK/profile.plist" "$IOS_WORK/asc-api-key.json" "$IOS_WORK/keychain-password" "$IOS_WORK/signing.json" "$IOS_WORK/installed-profile-path"
