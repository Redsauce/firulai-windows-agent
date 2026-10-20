# Windows 0.20.11, 2026-10-20

Published release: https://github.com/Redsauce/firulai-windows-agent/releases/tag/0.20.11

Code commit: e5eba1cde4a0c98d9385284f1de5ca19b95a4d0d.
GitHub release ID: 418105991. The production RSM catalog selects 0.20.11.

Installer SHA256: b9d6d56a0da6ac726905e0fe6f42460c3cdd1486debd807e88f0ef8665983c15.
Installer size: 2295117 bytes. All four downloaded release assets match the
local files. The actual newly built agent verified the public manifest signature,
installer hash, size and version using scripts/test-release-package.ps1.

The release preserves private uninstall retry files after partial local cleanup,
restores the Apps entry and handles retry when the service is already absent.
Recovery configurations use a protected Administrators/SYSTEM-only DACL.
The upgrade regressions and full installer compilation pass. The native Inno
recovery tests previously passed in isolation, including the administrator test
manually run and confirmed by the user. No real RSAgent service was changed by
these tests; this is not a claim of a new manual uninstall of a monitored system.

The Firulai minimum warning threshold remains Windows 0.20.9. Old public assets
were not replaced. Code and evidence remain on the task branch; the release tag
identifies the code above.
