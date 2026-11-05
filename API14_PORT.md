# MissFisher API14 localization port

The main plugin is closed source. This branch localizes an actual official API14 binary; it does not rebuild the main plugin or relabel the API13 package.

- Original author: MistyDai; upstream version **1.9.17.1**, commit `b7686d625a2835e0caeaa6df6eceb6d1d2f4f799` (2026-03-30), preserved by the QianChangUwU/MissFisher mirror.
- [Pinned archive](https://raw.githubusercontent.com/QianChangUwU/MissFisher/b7686d625a2835e0caeaa6df6eceb6d1d2f4f799/latest.zip)
- SHA256: `fe9a8cec811818a4aff8b9a167459a0025ea4f458811e36a74b435437a12903b`
- Input assembly references Dalamud **14.0.4.3** and System.Runtime **10.0.0.0**; the patcher checks both major versions.
- Localized version **1.9.17.2**; 991 language keys: 577 retained curated translations and 414 newly converted using OpenCC s2twp. New conversions still need terminology review.

Build using .NET 10 and real API14 SDK files:

```powershell
./tools/Build-Api14.ps1 -DalamudHome C:/path/to/api14 -Dotnet C:/path/to/dotnet.exe
```

The pinned ZIP is hash checked. The patcher changes the language resource and version metadata, then verifies that every executable method body and every non-language embedded resource remains unchanged. English/Japanese entries and upstream package dependencies are preserved. Verification evidence and the ZIP are written to `.api14/artifacts/`.

The root `latest.zip`, `lang.json`, and root manifest are legacy API13 inputs and are not used by this recipe. The API14 package is a development artifact; TC API14 runtime testing is pending and no public plugin-feed entry is enabled.
