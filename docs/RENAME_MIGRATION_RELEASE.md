# Firulai Security Agent 0.20.15: migration release

The already installed `RSAgent` updater downloads `FirulaiAgent.exe` through the
existing signed manifest. The installer recognizes the old service and performs
the migration without invoking the uninstaller or marking the System disconnected.
It copies configuration and state, preserves the API base and locale, starts
`firulaiAgent`, verifies the UUID, and then removes the old service. A startup
recovery task restores `RSAgent` if setup is interrupted before confirmation.
The old automatic-update worker is still running while the installer finishes,
so a separate cleanup helper removes the old directories after that worker
exits; a boot task retries cleanup if needed.

## Release sequence

1. Test from a disposable Windows machine with the currently published agent:
   successful automatic update, same UUID/token/state, one running service,
   one uninstall entry, and no old directories. Also interrupt setup before
   confirmation and verify recovery on reboot.
2. Commit and push the code. Create and push tag `0.20.15` pointing to that
   commit. The workflow requires this exact tag format and version match.
3. Run `Build, sign and verify Windows release` with tag `0.20.15` and
   `include_update_manifest=true`. This signs the agent payload and installer,
   creates `agent-update.json` and `agent-update.json.sig`, and verifies the
   downloaded artifact. Keep the existing RSA signing key.
4. Create the GitHub Release for `0.20.15` and upload the four extracted files
   from `signed-firulai-release`: `FirulaiAgent.exe`, `agent-update.json`,
   `agent-update.json.sig`, and `SHA256SUMS.txt`. Do not rebuild the installer
   after signing.
5. Verify signatures, hashes, version, manifest URL, and download from the
   actual release. Only then change the Firulai catalog Windows version to
   `0.20.15`. Monitor update logs and the installed service.

The local unsigned `Output/FirulaiAgent.exe` is for compilation checks only.
It is not the release asset.
