# Signed Windows releases

For the **existing manual release process**, first create and push a stable tag in `X.Y.Z` format. The tag must point to a commit that contains this workflow and must match `AgentConfig.AgentVersion` and `MyAppVersion`. Run `.github/workflows/build-sign-release.yml` manually with that tag and select `include_update_manifest`. The workflow checks out the tagged commit, compiles and signs `RsAgent.exe`, `FirulaiAgent.exe`, and the Inno uninstaller, then signs the RSA update manifest against the final installer bytes. It delivers the downloadable `signed-firulai-release` artifact containing `FirulaiAgent.exe`, `agent-update.json`, `agent-update.json.sig`, and `SHA256SUMS.txt`. A separate job downloads the artifact and checks its Authenticode signature, hashes, RSA signature, version, and installer size. Upload these four extracted files manually to the GitHub release for the same tag. Do not rebuild or modify the installer after signing; update the RSM catalog only after all assets are published and checked.

To test Authenticode alone without preparing a release, leave `include_update_manifest` unchecked. This creates `signed-firulai-installer` with only `FirulaiAgent.exe` and `SHA256SUMS.txt`. It must not be used as an automatic-update release because existing agents require the RSA manifest and signature.

Environment variables: `AS_ACCOUNT_NAME`, `AS_CERT_PROFILE`, `PUBLISHER_NAME`, and `PUBLISHER_URL`. `PUBLISHER_NAME` must match the certificate's CN or O field exactly. The signing endpoint is configured for West Europe in the workflow.

Environment secrets for the first Authenticode build: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID`. The existing Azure signing account and certificate profile supply the code-signing credentials.

Additional environment secret for an automatic-update bundle: `WINDOWS_UPDATE_PRIVATE_KEY_XML_B64`. Optional `VERIFY_UUID` and `VERIFY_TOKEN` identify a disposable test System; when both are set, the verification job also installs and uninstalls the downloaded installer. Without them, the job reports that this live check was skipped. The workflow never uploads assets to a GitHub release.

`WINDOWS_UPDATE_PRIVATE_KEY_XML_B64` is the Base64 encoding of the **existing** private RSA XML key paired with `src/RsAgent/UpdatePublicKey.xml`. Do not create a new key for this workflow. The key is decoded to a runner temporary file only while the manifest is signed, then removed. To produce the secret value locally without printing it, run the following on a trusted Windows machine and paste the result directly into the GitHub environment secret editor:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes('<path-to-existing-private-key.xml>'))
```

Inno signs the installer and its uninstaller during compilation. The agent executable is signed before packaging. The RSA manifest is generated after signing the final installer so its hash matches the published bytes. The workflow verifies the downloaded Actions artifact; after manual release upload, the published GitHub assets still need a final check against `SHA256SUMS.txt` and the RSA manifest.
