# Pre-launch checklist

From the Mobile Game SOP section 15. `auto` items are checked by `ShipAudit.cs` or `source_audit.py`; `manual` items run on a device build and are printed for the user. Feature gates: `iap`, `ads`, `analytics` (any SDK listed), `saves` (always), `always`.

## Build (always)
- auto: build number bumped past the last shipped one; a duplicate is rejected after you have waited for processing
- auto: development-build flag off
- auto: scenes in build, bundle ids, company and product set, version is semver
- auto: Android IL2CPP and ARM64, minification on for release, managed stripping not disabled
- manual: device pass on ads, IAP, remote config, analytics, notifications after a release build with stripping and minification
- manual: `usage.txt` and `mapping.txt` grepped for every string-referenced class (analytics SDKs, consent bridge, native plugins); a name in `usage.txt` means deleted
- manual: deobfuscation `mapping.txt` uploaded to the store console
- manual: every dev override unticked and absent from the release binary

## Content (always)
- auto: no hardcoded economy figures in player-visible copy (heuristic scan; confirm each hit)
- auto: all strings within the font's character set (non-ASCII scan when the descriptor says asciiOnly)
- auto: no platform-, store- or SDK-naming literal outside a platform define
- auto: `StopCoroutine(Method())` no-ops
- manual: every store claim traced to its call sites

## Devices and layout (always)
- auto: one CanvasScaler configuration across scenes and prefabs
- auto: a SafeArea object or SafeAreaFitter script present
- auto: app icons set per platform
- manual: smallest and largest supported screens; tablets are where layout breaks
- manual: notched device, nothing under the inset; gesture-bar device, nothing under the bar
- manual: split-screen or foldable if resizeableActivity is on
- manual: every modal dismissable at every aspect ratio

## Saves (always)
- manual: the chosen scheme matches the section 13 answers in the descriptor
- manual: uninstall and reinstall behaves as designed, and you know exactly what is lost
- manual: migration from the previous save version tested with a real old save
- manual: a corrupt save does not brick startup

## Performance (always)
- manual: profiled on the lowest-spec target device, not the dev machine
- manual: no unpooled spawning in hot paths; frame time stable at the busiest moment; memory stable across a long session

## Money (iap)
- manual: fresh purchase delivers, popup fires once, figures match the constants
- manual: reinstall and restore returns entitlements and delivers nothing twice
- manual: update over the previous build (the migration case)
- manual: receipt validation runs on device and its result is used: product id in the payload matches the grant
- manual: replay defence: the same transaction cannot grant twice; consumables dedupe on transaction id, counted by log lines
- manual: Complete only after the grant is durable; interrupted purchase resolves; non-consumable re-delivery double-fires nothing
- manual: purchase feedback shows the true total; ad-free entitlement read from persisted state and holds offline
- manual: dev-only overrides inside a development-build define

## Analytics (analytics)
- manual: every event appears in the dashboard, one of each sent from a test build
- manual: revenue carries localized price and ISO currency and the correct store per platform; first-purchase event fires
- manual: tutorial funnel start, every step, complete, abandon; progression start, complete, fail; ad request, fill, impression, completion, click, failures; economy sources and sinks
- manual: nothing collects before consent; re-audited since the last SDK upgrade
- manual: each number lands in the specific report you plan to read; you can name the authoritative SDK per revenue number
- manual: every SDK allow-list declared in the settings asset and cross-checked against the constants
- manual: segmentation properties set: notification permission, install age, payer status
- manual: no choke-point event fires per unit; batching verified by event count

## Consent and privacy (ads or analytics)
- manual: ATT prompt on iOS with its usage description; CMP or GDPR flow on every platform shipped
- manual: store privacy declarations match what the SDKs collect

## iOS after the Unity build

    cd Builds/iOS
    xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release -destination 'generic/platform=iOS' -archivePath ../iOS-archive/App.xcarchive archive
    xcodebuild -exportArchive -archivePath ../iOS-archive/App.xcarchive -exportOptionsPlist ExportOptions.plist -exportPath ../iOS-export

Signing, the export options plist and the upload (Transporter or `xcrun altool`) are the user's steps.

## Android shrinker greps after a release build

    grep -E "<ClassName>" Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/mapping/release/usage.txt
    grep -E "<ClassName>" Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/mapping/release/mapping.txt

A hit in `usage.txt` means the class was removed. Fix with keep rules, never by turning minification off.
