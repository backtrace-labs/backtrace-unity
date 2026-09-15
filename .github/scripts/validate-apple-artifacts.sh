#!/bin/bash
# Maintainer/CI validation only. Not invoked automatically from Unity builds.
set -euo pipefail
[[ $(uname -s) == Darwin ]] || { echo 'macOS/Xcode required' >&2; exit 1; }
root="${1:?usage: validate-apple-artifacts.sh REPO [mac|ios|all] [MAC_BUNDLE]}"
mode="${2:-all}"
work=$(mktemp -d); trap 'rm -rf "$work"' EXIT
fail() { echo "Backtrace artifact validation: $*" >&2; exit 1; }
plist() { /usr/libexec/PlistBuddy -c "Print :$2" "$1"; }
contains() { grep -Fq "$2" "$1" || fail "missing $2 in $1"; }
no_links() {
  [[ -d "$1" && ! -L "$1" ]] || fail "missing or aliased directory: $1"
  [[ -z $(find "$1" -type l -print -quit) ]] || fail "symlink in $1"
}
if [[ $mode == mac || $mode == all ]]; then
  b="${3:-$root/Mac/BacktraceMacUnity.bundle}"
  no_links "$b"
  [[ ! -d "$b/Contents/Frameworks" ]] || fail 'nested macOS Frameworks directory'
  [[ -z $(find "$b" \( -name '*.framework' -o -name Versions \) -print -quit) ]] || fail 'versioned framework'
  [[ $(plist "$b/Contents/Info.plist" CFBundleIdentifier) == io.backtrace.unity.macos ]] || fail 'macOS bundle ID'
  [[ $(plist "$b/Contents/Info.plist" CFBundleShortVersionString) == 2.2.0 ]] || fail 'macOS version'
  for f in Model.mom ModelV2.mom VersionInfo.plist; do
    test -f "$b/Contents/Resources/Model.momd/$f" || fail "missing model $f"
  done
  test -f "$b/Contents/Resources/PrivacyInfo.xcprivacy" || fail 'missing macOS privacy'
  binary="$b/Contents/MacOS/BacktraceMacUnity"
  actual=$(lipo -archs "$binary" | tr ' ' '\n' | sort | xargs)
  [[ $actual == 'arm64 x86_64' ]] || fail "unexpected macOS architectures: $actual"
  for arch in arm64 x86_64; do
    nm -arch "$arch" -gU "$binary" > "$work/exports"
    for symbol in BacktraceUnityBridgeVersion StartBacktraceIntegrationV3 GetAttributes FreeAttributes AddAttribute NativeReport Disable; do
      grep -Eq "[[:space:]]_${symbol}$" "$work/exports" || fail "missing $symbol"
    done
    nm -arch "$arch" -U "$binary" > "$work/all"
    contains "$work/all" '_OBJC_CLASS_$_BTUnityPLCrashReporter'
    if grep -Eq '[[:space:]]_OBJC_(CLASS|METACLASS)_\$_PLCrash' "$work/all" ||
       grep -Eq '[[:space:]](_plcrash_|_PLCrash)' "$work/exports"; then fail 'unprefixed macOS symbols'; fi
    otool -arch "$arch" -hv "$binary" > "$work/header"
    contains "$work/header" BUNDLE
    xcrun vtool -arch "$arch" -show-build "$binary" > "$work/build"
    grep -Eq 'platform[[:space:]]+MACOS$' "$work/build" || fail 'macOS platform'
    grep -Eq 'minos[[:space:]]+12\.0$' "$work/build" || fail 'macOS minimum'
  done
  while IFS= read -r dependency; do
    case "$dependency" in /System/Library/*|/usr/lib/*) ;; *) fail "non-system macOS dependency: $dependency" ;; esac
  done < <(otool -L "$binary" | awk '/^\t/ {print $1}')
  for notice in PLCrashReporter-LICENSE.txt PLCrashReporter-ThirdPartyNotices.txt; do
    test -s "$b/Contents/Resources/ThirdPartyNotices/$notice" || fail "missing notice $notice"
  done
  codesign --verify --all-architectures --strict "$b"
fi
if [[ $mode == ios || $mode == all ]]; then
  for framework in Backtrace CrashReporter; do
    xc="$root/iOS/$framework.xcframework"
    no_links "$xc"
    info="$xc/Info.plist"
    device=0; simulator=0; index=0
    while id=$(plist "$info" "AvailableLibraries:$index:LibraryIdentifier" 2>/dev/null); do
      platform=$(plist "$info" "AvailableLibraries:$index:SupportedPlatform")
      variant=$(plist "$info" "AvailableLibraries:$index:SupportedPlatformVariant" 2>/dev/null || true)
      lib=$(plist "$info" "AvailableLibraries:$index:LibraryPath")
      [[ $platform == ios && ( -z $variant || $variant == simulator ) ]] || fail 'use Unity-only iOS archive'
      [[ $id != */* && $id != . && $id != .. && $lib == "$framework.framework" ]] || fail 'invalid slice path'
      if [[ -z $variant ]]; then device=$((device+1)); expectedPlatform=IOS; else simulator=$((simulator+1)); expectedPlatform=IOSSIMULATOR; fi
      f="$xc/$id/$lib"; binary="$f/$framework"
      test -f "$binary" || fail "missing $binary"
      arches=$(lipo -archs "$binary")
      [[ " $arches " == *' arm64 '* ]] || fail "missing arm64 $framework $variant"
      if [[ $framework == Backtrace ]]; then
        [[ $(plist "$f/Info.plist" CFBundleShortVersionString) == 2.2.0 ]] || fail 'iOS Backtrace version'
        for arch in $arches; do
          otool -arch "$arch" -hv "$binary" > "$work/header"
          contains "$work/header" DYLIB
          xcrun vtool -arch "$arch" -show-build "$binary" > "$work/build"
          grep -Eq "platform[[:space:]]+$expectedPlatform\$" "$work/build" || fail 'wrong platform variant'
          grep -Eq 'minos[[:space:]]+15\.0$' "$work/build" || fail 'iOS minimum must match 15.0'
          # Header-only CrashReporter integration depends on these classes being
          # incorporated into the dynamic Backtrace binary. Fail before installing
          # if a differently built artifact changes that contract.
          nm -arch "$arch" -U "$binary" > "$work/all"
          contains "$work/all" '_OBJC_CLASS_$_PLCrashReporterConfig'
          contains "$work/all" '_OBJC_CLASS_$_PLCrashReporter'
          strings -a "$binary" > "$work/strings"
          contains "$work/strings" shutdownForNativeBridge
          otool -arch "$arch" -L "$binary" > "$work/deps"
          if grep -q '@rpath/CrashReporter.framework' "$work/deps"; then fail 'unexpected dynamic CrashReporter dependency'; fi
        done
        test -n "$(find "$f" -name ModelV2.mom -print -quit)" || fail 'missing iOS ModelV2'
        test -n "$(find "$f" -name Model.mom -print -quit)" || fail 'missing iOS ModelV1'
        test -n "$(find "$f" -name PrivacyInfo.xcprivacy -print -quit)" || fail 'missing iOS privacy resources'
      else
        for header in CrashReporter.h PLCrashReporter.h PLCrashReporterConfig.h; do
          test -s "$f/Headers/$header" || fail "missing static runtime header $header"
        done
        test -s "$f/Modules/module.modulemap" || fail 'missing static runtime module map'
        test -s "$f/PrivacyInfo.xcprivacy" || fail 'missing static runtime slice privacy manifest'
        # Static universal or thin archives; they must not be embedded as dylibs.
        lipo "$binary" -thin arm64 -output "$work/crash-arm64.a" 2>/dev/null || cp "$binary" "$work/crash-arm64.a"
        [[ $(head -c 8 "$work/crash-arm64.a") == '!<arch>' ]] || fail 'CrashReporter is not the expected static archive'
      fi
      index=$((index+1))
    done
    [[ $device == 1 && $simulator == 1 && $index == 2 ]] || fail 'device/simulator slice pair required'
    # The official archive signs each XCFramework container. Device framework
    # slices can be unsigned until Xcode embeds and signs them with the host app.
    # Verify the container seal, including its nested files, without modifying it.
    codesign --verify --strict "$xc"
  done
  test -f "$root/iOS/CrashReporter.xcframework/PrivacyInfo.xcprivacy" || fail 'missing static-runtime privacy manifest'
  for notice in PLCrashReporter-LICENSE.txt PLCrashReporter-ThirdPartyNotices.txt; do
    test -s "$root/iOS/ThirdPartyNotices/$notice" || fail "missing iOS notice $notice"
  done
fi
[[ $mode == mac || $mode == ios || $mode == all ]] || fail 'unknown validation mode'
printf 'PASS: %s Apple native artifacts\n' "$mode"
