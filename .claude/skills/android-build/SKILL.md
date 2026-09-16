---
name: android-build
description: Reference for building and deploying this Android fork of Daggerfall Unity via the in-editor Multi-Build Tool. Use when asked how to build, package, or deploy an APK, or when explaining Android build/verification steps.
---

This project has no CLI or CI build — everything goes through the Unity Editor. Do not suggest `gradlew`,
`unity -batchmode`-style commands, or npm/cargo-style build commands unless the user has introduced them
separately.

## Building

1. Open the project in Unity 2022.3.62f3.
2. Menu: **Daggerfall Tools → Android → Multi-Build Tool** (implemented in
   `Assets/Android/Editor/AndroidBuildTool.cs`).
3. The tool supports:
   - Target architecture: armv7 and/or arm64
   - Scripting backend: IL2CPP
   - Development build toggle
   - Per-configuration enable/disable, so multiple build variants can be queued in one pass

## Verifying a change

There's no automated test suite. The standard way to confirm a change works is to build via the Multi-Build
Tool above and run the resulting APK on a physical Android device or emulator. For changes that aren't
Android-specific, a quicker check is Play Mode in the Editor, but anything touching `Assets/Android/`,
storage/permissions, or mod loading should be confirmed with a real build.

## Related gotchas

- Scoped storage / "all files access" permission handling: `AndroidUtils.cs`, `ModLoaderInterfaceWindow.cs`,
  `SaveLoadManager.cs`.
- Mod (`.dfmod`) imports are validated for Android-compatible asset bundles — see
  `Assets/Scripts/Game/Addons/ModSupport/ModLoaderInterfaceWindow.cs`.
- Game data path / DOS Daggerfall asset location on device: `Paths.cs`.
