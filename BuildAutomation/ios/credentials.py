"""Decode CI secrets locally and validate App Store distribution credentials."""
import base64
import datetime
import hashlib
import json
import os
from pathlib import Path
import plistlib
import re
import secrets
import shutil
import subprocess
import sys

work = Path(os.environ['IOS_WORK'])
if sys.argv[1] == 'decode':
    for key, name in [('IOS_P12_BASE64', 'distribution.p12'), ('IOS_PROFILE_BASE64', 'profile.mobileprovision')]:
        (work / name).write_bytes(base64.b64decode(os.environ[key], validate=True))
    if os.environ['DISTRIBUTION'] == 'app-store':
        key = os.environ['ASC_KEY_P8'].strip().replace('\\n', '\n')
        if not key.startswith('-----BEGIN PRIVATE KEY-----'):
            raise SystemExit('ASC_KEY_P8 must contain the raw .p8 PEM private key.')
        (work / 'asc-api-key.json').write_text(json.dumps({
            'key_id': os.environ['ASC_KEY_ID'], 'issuer_id': os.environ['ASC_ISSUER_ID'],
            'key': key, 'in_house': False,
        }))
    (work / 'keychain-password').write_text(secrets.token_urlsafe(32))
else:
    profile = plistlib.loads((work / 'profile.plist').read_bytes())
    ent = profile['Entitlements']
    team = os.environ['APPLE_TEAM_ID']
    bundle = os.environ['BUNDLE_ID']
    def require(condition, message):
        if not condition:
            raise SystemExit(message)
    mode = os.environ['DISTRIBUTION']
    require(mode in ('adhoc', 'app-store'), 'Unknown distribution mode.')
    if mode == 'app-store':
        require('ProvisionedDevices' not in profile, 'Device provisioning profile cannot be used for App Store Connect.')
    else:
        require(bool(profile.get('ProvisionedDevices')), 'Ad Hoc distribution requires a profile with registered devices.')
    require(not profile.get('ProvisionsAllDevices'), 'Enterprise profile cannot be used.')
    require(ent.get('get-task-allow') is False, 'Profile must explicitly disable get-task-allow.')
    require(team in profile.get('TeamIdentifier', []), 'Profile team mismatch.')
    require(ent.get('com.apple.developer.team-identifier') == team, 'Profile entitlement team mismatch.')
    require(ent.get('application-identifier') in [p + '.' + bundle for p in profile.get('ApplicationIdentifierPrefix', [])], 'Profile must match the explicit bundle ID.')
    require(profile['ExpirationDate'] > datetime.datetime.now(datetime.timezone.utc).replace(tzinfo=None), 'Provisioning profile has expired.')
    identities = subprocess.check_output(['security', 'find-identity', '-v', '-p', 'codesigning', str(work / 'signing.keychain-db')], text=True)
    hashes = {hashlib.sha1(cert).hexdigest().upper() for cert in profile['DeveloperCertificates']}
    matches = [(digest, name) for digest, name in re.findall(r'([A-F0-9]{40}) "([^"]+)"', identities) if digest in hashes and ('Apple Distribution:' in name or 'iPhone Distribution:' in name)]
    require(bool(matches), 'No valid distribution identity matches a profile certificate and imported private key.')
    uuid = profile['UUID']
    require(bool(re.fullmatch(r'[A-Fa-f0-9-]+', uuid)), 'Invalid profile UUID.')
    destination = Path.home() / 'Library/MobileDevice/Provisioning Profiles' / (uuid + '.mobileprovision')
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(work / 'profile.mobileprovision', destination)
    (work / 'installed-profile-path').write_text(str(destination))
    (work / 'signing.json').write_text(json.dumps({'team': team, 'uuid': uuid, 'identity': matches[0][0]}))
    with open(os.environ['GITHUB_ENV'], 'a') as env:
        env.write('APPLE_TEAM_ID=' + team + '\n')
    plistlib.dump({'method': 'app-store-connect' if mode == 'app-store' else 'release-testing', 'teamID': team, 'signingStyle': 'manual',
                  'signingCertificate': matches[0][0], 'provisioningProfiles': {bundle: uuid},
                  'manageAppVersionAndBuildNumber': False, 'uploadSymbols': True},
                 (work / 'ExportOptions.plist').open('wb'))
