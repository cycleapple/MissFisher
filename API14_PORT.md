# MissFisher API14 localization port

The main plugin is closed source. This branch localizes a real official API14 binary.

- Stable upstream 1.9.20.0, author MistyDai; preserved mirror commit `225828c5f5d980515d0e025ebcf962ff9a23fc25` (2026-04-10).
- [Pinned archive](https://raw.githubusercontent.com/QianChangUwU/MissFisher/225828c5f5d980515d0e025ebcf962ff9a23fc25/latest.zip)
- SHA256 `f3d7ce58e5414730b5fde79e4976d9132801cb91c8a7b6a169964833a7d10966`.
- Actual references: Dalamud 14.0.4.5, System.Runtime 10.0.0.0.
- Localized 1.9.20.1: 1007/1007 keys, 981 retained translations and 26 new OpenCC s2twp conversions. Converted wording still needs review.
- The later 1.9.21.0 is a preview requiring Dalamud 14.0.5.2; it is not used with the pinned final stable 14.0.5.1 SDK.

Build: `./tools/Build-Api14.ps1 -DalamudHome C:/path/to/api14 -Dotnet C:/path/to/dotnet.exe`.
The recipe verifies the input archive hash and actual API14/.NET10 references, and verifies that method bodies and all nine non-language resources remain unchanged. Output and verification are in `.api14/artifacts/`.
Legacy root API13 archives/resources are not inputs to this recipe. TC runtime verification remains pending; no public feed entry is enabled.
