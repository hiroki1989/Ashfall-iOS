require 'json'
require 'xcodeproj'

work = ENV.fetch('IOS_WORK')
signing = JSON.parse(File.read(File.join(work, 'signing.json')))
project = Xcodeproj::Project.open(File.join(work, 'source/Unity-iPhone.xcodeproj'))
app = project.targets.find { |target| target.name == 'Unity-iPhone' }
raise 'Unity-iPhone application target is missing' unless app
build = (Integer(ENV.fetch('GITHUB_RUN_NUMBER')) + 100).to_s
project.targets.each do |target|
  target.build_configurations.each do |config|
    settings = config.build_settings
    settings['ENABLE_BITCODE'] = 'NO'
    settings['DEBUG_INFORMATION_FORMAT'] = 'dwarf-with-dsym'
    settings['COPY_PHASE_STRIP'] = 'YES' if config.name == 'Release'
    settings.keys.grep(/\A(PROVISIONING_PROFILE|CODE_SIGN_IDENTITY|DEVELOPMENT_TEAM|CODE_SIGN_STYLE)/).each { |key| settings.delete(key) }
    if target == app
      settings['PRODUCT_BUNDLE_IDENTIFIER'] = ENV.fetch('BUNDLE_ID')
      settings['CURRENT_PROJECT_VERSION'] = build
      settings['CODE_SIGN_STYLE'] = 'Manual'
      settings['DEVELOPMENT_TEAM'] = signing.fetch('team')
      settings['CODE_SIGN_IDENTITY'] = signing.fetch('identity')
      settings['PROVISIONING_PROFILE_SPECIFIER'] = signing.fetch('uuid')
      settings['CODE_SIGNING_ALLOWED'] = 'YES'
      settings['CODE_SIGNING_REQUIRED'] = 'YES'
    else
      settings['CODE_SIGNING_ALLOWED'] = 'NO'
      settings['CODE_SIGNING_REQUIRED'] = 'NO'
    end
  end
end
project.save
info = File.join(work, 'source/Info.plist')
plist = Xcodeproj::Plist.read_from_path(info)
plist['CFBundleIdentifier'] = ENV.fetch('BUNDLE_ID')
plist['CFBundleVersion'] = build
Xcodeproj::Plist.write_to_path(plist, info)
